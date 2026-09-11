using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FixRdlPbi.App.Services;
using FixRdlPbi.App.Models;

namespace FixRdlPbi.App.Clients;

public class PowerBiApiClient
{
    private const string BaseUrl = "https://api.powerbi.com/v1.0/myorg/";

    private readonly HttpClient _httpClient;
    private readonly FabricTokenProvider _tokenProvider;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public PowerBiApiClient(HttpClient httpClient, FabricTokenProvider tokenProvider)
    {
        _httpClient = httpClient;
        _tokenProvider = tokenProvider;
        _httpClient.BaseAddress = new Uri(BaseUrl);
    }

    private async Task PrepareRequestAsync()
    {
        string token = await _tokenProvider.GetPowerBiAccessTokenAsync();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }


    public async Task<T> GetAsync<T>(string endpoint)
    {
        await PrepareRequestAsync();
        using HttpResponseMessage response = await _httpClient.GetAsync(endpoint);
        string content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Power BI API GET failed. HTTP {(int)response.StatusCode}: {content}"
            );
        }

        T result = JsonSerializer.Deserialize<T>(content, _jsonOptions);

        if (result == null)
        {
            throw new InvalidOperationException("Power BI API returned an empty response.");
        }

        return result;
    }

    public async Task PostAsync(string endpoint)
    {
        await PrepareRequestAsync();
        using HttpResponseMessage response = await _httpClient.PostAsync(endpoint, null);
        string content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Power BI API POST failed. HTTP {(int)response.StatusCode}: {content}"
            );
        }
    }

    public async Task PostAsync<TRequest>(string endpoint, TRequest request)
    {
        await PrepareRequestAsync();
        string json = JsonSerializer.Serialize(request, _jsonOptions);
        using StringContent content = new(json, Encoding.UTF8, "application/json");
        using HttpResponseMessage response = await _httpClient.PostAsync(endpoint, content);
        string responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Power BI API POST failed. HTTP {(int)response.StatusCode}: {responseContent}"
            );
        }
    }
    public async Task<string> RefreshDatasetAndWaitAsync(
        string workspaceId,
        string datasetId,
        Action<string> progress = null,
        CancellationToken cancellationToken = default)
    {
        PowerBiDatasetInfo dataset = await GetDatasetInfoAsync(workspaceId, datasetId, cancellationToken);

        progress?.Invoke(
            $"Target semantic model resolved by Power BI REST API: {dataset.Name} ({dataset.Id}); " +
            $"IsRefreshable={dataset.IsRefreshable}."
        );

        if (!dataset.IsRefreshable)
        {
            throw new InvalidOperationException(
                $"The target semantic model '{dataset.Name}' ({dataset.Id}) is not refreshable according to Power BI REST API."
            );
        }

        // Snapshot recent refresh request IDs before starting a new refresh. This allows us to
        // correlate the new refresh even when a redirected 202 response does not preserve the
        // Location/x-ms-request-id headers on the final HttpResponseMessage.
        DatasetRefreshHistoryResponse historyBefore = await GetRefreshHistoryAsync(
            workspaceId,
            datasetId,
            10,
            cancellationToken
        );

        HashSet<string> knownRequestIds = historyBefore.Value
            .Where(x => !string.IsNullOrWhiteSpace(x.RequestId))
            .Select(x => x.RequestId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        DateTimeOffset requestStartedUtc = DateTimeOffset.UtcNow;

        await PrepareRequestAsync();

        string endpoint = $"groups/{workspaceId}/datasets/{datasetId}/refreshes";
        var request = new { notifyOption = "NoNotification" };
        string json = JsonSerializer.Serialize(request, _jsonOptions);

        using StringContent content = new(json, Encoding.UTF8, "application/json");
        using HttpResponseMessage response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
        string responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Power BI semantic model refresh failed to start. HTTP {(int)response.StatusCode}: {responseContent}"
            );
        }

        progress?.Invoke(
            $"Power BI accepted the semantic model refresh. HTTP {(int)response.StatusCode} {response.StatusCode}."
        );

        string refreshId = TryGetRefreshIdFromResponse(response);

        if (!string.IsNullOrWhiteSpace(refreshId))
        {
            progress?.Invoke($"Refresh requestId obtained from response headers: {refreshId}.");
        }
        else
        {
            progress?.Invoke(
                "The refresh response did not expose Location/x-ms-request-id. " +
                "Resolving the new requestId from refresh history."
            );

            refreshId = await ResolveRefreshIdFromHistoryAsync(
                workspaceId,
                datasetId,
                knownRequestIds,
                requestStartedUtc,
                progress,
                cancellationToken
            );
        }

        progress?.Invoke($"Semantic model refresh started. requestId={refreshId}.");

        string detailEndpoint = $"groups/{workspaceId}/datasets/{datasetId}/refreshes/{refreshId}";
        DateTimeOffset deadline = DateTimeOffset.UtcNow.AddMinutes(30);

        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);

            await PrepareRequestAsync();
            using HttpResponseMessage detailResponse = await _httpClient.GetAsync(detailEndpoint, cancellationToken);
            string detailContent = await detailResponse.Content.ReadAsStringAsync(cancellationToken);

            if (!detailResponse.IsSuccessStatusCode &&
                detailResponse.StatusCode != System.Net.HttpStatusCode.Accepted)
            {
                throw new HttpRequestException(
                    $"Power BI refresh status check failed. HTTP {(int)detailResponse.StatusCode}: {detailContent}"
                );
            }

            DatasetRefreshDetail detail = JsonSerializer.Deserialize<DatasetRefreshDetail>(detailContent, _jsonOptions);
            string status = detail?.Status ?? string.Empty;

            if (string.Equals(status, "Completed", StringComparison.OrdinalIgnoreCase))
            {
                progress?.Invoke($"Semantic model refresh {refreshId} completed successfully.");
                return refreshId;
            }

            if (string.Equals(status, "Failed", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(status, "Cancelled", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(status, "Disabled", StringComparison.OrdinalIgnoreCase))
            {
                string error = detail?.ServiceExceptionJson ?? string.Empty;
                throw new InvalidOperationException(
                    $"Semantic model refresh {refreshId} finished with status '{status}'. {error}"
                );
            }

            // For on-demand refreshes Power BI can return HTTP 202 with status Unknown while
            // execution is still in progress. Treat that as a running state, not an error.
            if (detailResponse.StatusCode == System.Net.HttpStatusCode.Accepted ||
                string.Equals(status, "Unknown", StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(status))
            {
                progress?.Invoke($"Semantic model refresh {refreshId} is still running. Status={status}.");
                continue;
            }

            progress?.Invoke(
                $"Semantic model refresh {refreshId} status: {status}; extendedStatus={detail?.ExtendedStatus}."
            );
        }

        throw new TimeoutException(
            $"Timed out waiting for semantic model refresh {refreshId} after 30 minutes."
        );
    }

    private async Task<PowerBiDatasetInfo> GetDatasetInfoAsync(
        string workspaceId,
        string datasetId,
        CancellationToken cancellationToken)
    {
        await PrepareRequestAsync();

        string endpoint = $"groups/{workspaceId}/datasets/{datasetId}";
        using HttpResponseMessage response = await _httpClient.GetAsync(endpoint, cancellationToken);
        string content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Power BI dataset lookup failed. HTTP {(int)response.StatusCode}: {content}"
            );
        }

        PowerBiDatasetInfo dataset = JsonSerializer.Deserialize<PowerBiDatasetInfo>(content, _jsonOptions);

        if (dataset == null || string.IsNullOrWhiteSpace(dataset.Id))
        {
            throw new InvalidOperationException(
                $"Power BI REST API returned an invalid semantic model for dataset '{datasetId}'."
            );
        }

        return dataset;
    }

    private async Task<DatasetRefreshHistoryResponse> GetRefreshHistoryAsync(
        string workspaceId,
        string datasetId,
        int top,
        CancellationToken cancellationToken)
    {
        await PrepareRequestAsync();

        string endpoint = $"groups/{workspaceId}/datasets/{datasetId}/refreshes?$top={top}";
        using HttpResponseMessage response = await _httpClient.GetAsync(endpoint, cancellationToken);
        string content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Power BI refresh history lookup failed. HTTP {(int)response.StatusCode}: {content}"
            );
        }

        DatasetRefreshHistoryResponse history = JsonSerializer.Deserialize<DatasetRefreshHistoryResponse>(
            content,
            _jsonOptions
        );

        return history ?? new DatasetRefreshHistoryResponse();
    }

    private static string TryGetRefreshIdFromResponse(HttpResponseMessage response)
    {
        if (response.Headers.Location != null)
        {
            string location = response.Headers.Location.ToString().TrimEnd('/');
            int slashIndex = location.LastIndexOf('/');

            if (slashIndex >= 0 && slashIndex < location.Length - 1)
            {
                string candidate = location[(slashIndex + 1)..];
                if (Guid.TryParse(candidate, out _))
                {
                    return candidate;
                }
            }
        }

        if (response.Headers.TryGetValues("x-ms-request-id", out IEnumerable<string> requestIds))
        {
            string candidate = requestIds.FirstOrDefault(x => Guid.TryParse(x, out _));
            if (!string.IsNullOrWhiteSpace(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private async Task<string> ResolveRefreshIdFromHistoryAsync(
        string workspaceId,
        string datasetId,
        HashSet<string> knownRequestIds,
        DateTimeOffset requestStartedUtc,
        Action<string> progress,
        CancellationToken cancellationToken)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow.AddMinutes(2);

        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            DatasetRefreshHistoryResponse history = await GetRefreshHistoryAsync(
                workspaceId,
                datasetId,
                10,
                cancellationToken
            );

            DatasetRefreshHistoryEntry candidate = history.Value
                .Where(x => !string.IsNullOrWhiteSpace(x.RequestId))
                .Where(x => !knownRequestIds.Contains(x.RequestId))
                .Where(x => IsRefreshStartedNearRequest(x.StartTime, requestStartedUtc))
                .OrderByDescending(x => ParseDateTimeOffset(x.StartTime))
                .FirstOrDefault();

            if (candidate != null)
            {
                progress?.Invoke(
                    $"Refresh requestId resolved from history: {candidate.RequestId}; " +
                    $"refreshType={candidate.RefreshType}; startTime={candidate.StartTime}; status={candidate.Status}."
                );

                return candidate.RequestId;
            }

            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        }

        throw new InvalidOperationException(
            "Power BI accepted the semantic model refresh, but its requestId could not be correlated " +
            "from Location, x-ms-request-id, or the dataset refresh history within 2 minutes."
        );
    }

    private static bool IsRefreshStartedNearRequest(string startTime, DateTimeOffset requestStartedUtc)
    {
        DateTimeOffset parsed = ParseDateTimeOffset(startTime);
        if (parsed == DateTimeOffset.MinValue)
        {
            return true;
        }

        // Allow a small clock/serialization tolerance while excluding unrelated older refreshes.
        return parsed >= requestStartedUtc.AddSeconds(-30);
    }

    private static DateTimeOffset ParseDateTimeOffset(string value)
    {
        if (DateTimeOffset.TryParse(value, out DateTimeOffset parsed))
        {
            return parsed;
        }

        return DateTimeOffset.MinValue;
    }


}
