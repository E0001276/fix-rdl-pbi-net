using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FixRdlPbi.App.Services;

namespace FixRdlPbi.App.Clients;

public class FabricApiClient
{
    private const string BaseUrl = "https://api.fabric.microsoft.com/v1/";

    private readonly HttpClient _httpClient;
    private readonly FabricTokenProvider _tokenProvider;

    private readonly JsonSerializerOptions _jsonOptions =
        new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

    public FabricApiClient(HttpClient httpClient, FabricTokenProvider tokenProvider)
    {
        _httpClient = httpClient;
        _tokenProvider = tokenProvider;

        _httpClient.BaseAddress = new Uri(BaseUrl);
    }

    private async Task PrepareRequestAsync()
    {
        string token = await _tokenProvider.GetAccessTokenAsync();

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<T> GetAsync<T>(string endpoint)
    {
        await PrepareRequestAsync();

        using HttpResponseMessage response = await _httpClient.GetAsync(endpoint);

        string content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Fabric API GET failed. " + $"HTTP {(int)response.StatusCode}: {content}");
        }

        return JsonSerializer.Deserialize<T>(content, _jsonOptions);
    }

    public async Task<TResponse> PostAsync<TRequest, TResponse>(string endpoint, TRequest request)
    {
        await PrepareRequestAsync();

        string json = JsonSerializer.Serialize(request, _jsonOptions);

        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        using HttpResponseMessage response = await _httpClient.PostAsync(endpoint, content);

        string responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Fabric API POST failed. " + $"HTTP {(int)response.StatusCode}: {responseContent}");
        }

        if (string.IsNullOrWhiteSpace(responseContent))
        {
            return default;
        }

        return JsonSerializer.Deserialize<TResponse>(responseContent, _jsonOptions);
    }

    public async Task<HttpResponseMessage> PostAsync(string endpoint)
    {
        await PrepareRequestAsync();

        HttpResponseMessage response = await _httpClient.PostAsync(endpoint, null);

        return response;
    }

    public async Task<HttpResponseMessage> GetResponseAsync(string endpoint)
    {
        await PrepareRequestAsync();

        return await _httpClient.GetAsync(endpoint);
    }

    public async Task<HttpResponseMessage> PostResponseAsync<TRequest>(string endpoint, TRequest request)
    {
        await PrepareRequestAsync();

        string json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        return await _httpClient.PostAsync(endpoint, content);
    }
}