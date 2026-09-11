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
                $"Power BI dataset refresh failed to start. HTTP {(int)response.StatusCode}: {responseContent}"
            );
        }

        string refreshId = null;

        if (response.Headers.Location != null)
        {
            string location = response.Headers.Location.ToString().TrimEnd('/');
            int slashIndex = location.LastIndexOf('/');
            if (slashIndex >= 0 && slashIndex < location.Length - 1)
            {
                refreshId = location[(slashIndex + 1)..];
            }
        }

        if (string.IsNullOrWhiteSpace(refreshId) &&
            response.Headers.TryGetValues("x-ms-request-id", out IEnumerable<string> requestIds))
        {
            refreshId = requestIds.FirstOrDefault();
        }

        if (string.IsNullOrWhiteSpace(refreshId))
        {
            throw new InvalidOperationException(
                "Power BI accepted the semantic model refresh, but did not return a refresh identifier."
            );
        }

        progress?.Invoke($"Semantic model refresh started. RefreshId={refreshId}.");

        string detailEndpoint = $"groups/{workspaceId}/datasets/{datasetId}/refreshes/{refreshId}";
        DateTime deadline = DateTime.UtcNow.AddMinutes(30);

        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);

            await PrepareRequestAsync();
            using HttpResponseMessage detailResponse = await _httpClient.GetAsync(detailEndpoint, cancellationToken);
            string detailContent = await detailResponse.Content.ReadAsStringAsync(cancellationToken);

            if (detailResponse.StatusCode == System.Net.HttpStatusCode.Accepted)
            {
                progress?.Invoke($"Semantic model refresh {refreshId} is still running.");
                continue;
            }

            if (!detailResponse.IsSuccessStatusCode)
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

            progress?.Invoke(
                $"Semantic model refresh {refreshId} status: {status}; extendedStatus={detail?.ExtendedStatus}."
            );
        }

        throw new TimeoutException(
            $"Timed out waiting for semantic model refresh {refreshId} after 30 minutes."
        );
    }

}
