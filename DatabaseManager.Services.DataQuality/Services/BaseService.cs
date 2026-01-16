using DatabaseManager.Services.DataQuality.Models;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace DatabaseManager.Services.DataQuality.Services;

public class BaseService : IBaseService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public BaseService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<T> SendAsync<T>(ApiRequest apiRequest)
    {
        var client = _httpClientFactory.CreateClient("DatabaseManager");

        using var message = new HttpRequestMessage(
            apiRequest.ApiType switch
            {
                SD.ApiType.POST => HttpMethod.Post,
                SD.ApiType.PUT => HttpMethod.Put,
                SD.ApiType.DELETE => HttpMethod.Delete,
                _ => HttpMethod.Get
            },
            apiRequest.Url);

        message.Headers.Add("Accept", "application/json");

        if (apiRequest.Data != null)
        {
            message.Content = new StringContent(
                JsonSerializer.Serialize(apiRequest.Data),
                Encoding.UTF8,
                "application/json");
        }

        if (!string.IsNullOrEmpty(apiRequest.AzureStorage))
        {
            message.Headers.Add("azurestorageconnection", apiRequest.AzureStorage);
        }

        var response = await client.SendAsync(message);
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Request failed ({response.StatusCode}): {content}");
        }

        return JsonSerializer.Deserialize<T>(
            content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }
}


