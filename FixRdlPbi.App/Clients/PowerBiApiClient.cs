using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FixRdlPbi.App.Services;

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
}
