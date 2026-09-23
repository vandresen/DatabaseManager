using DatabaseManager.Services.DataQuality.Models;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace DatabaseManager.Services.DataQuality.Services;

public class BaseService : IBaseService
{
    private readonly IHttpClientFactory _httpClientFactory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public BaseService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<T> SendAsync<T>(ApiRequest apiRequest)
    {
        ArgumentNullException.ThrowIfNull(apiRequest);

        if (string.IsNullOrWhiteSpace(apiRequest.Url))
        {
            throw new ArgumentException(
                "API URL cannot be empty.",
                nameof(apiRequest));
        }

        var client = _httpClientFactory.CreateClient("DatabaseManager");

        var uriBuilder = new UriBuilder(apiRequest.Url);
        var queryParams = System.Web.HttpUtility.ParseQueryString(
            uriBuilder.Query);

        var functionKey = queryParams["code"];

        if (!string.IsNullOrWhiteSpace(functionKey))
        {
            queryParams.Remove("code");
            uriBuilder.Query = queryParams.ToString();
        }

        var method = apiRequest.ApiType switch
        {
            SD.ApiType.POST => HttpMethod.Post,
            SD.ApiType.PUT => HttpMethod.Put,
            SD.ApiType.DELETE => HttpMethod.Delete,
            _ => HttpMethod.Get
        };

        using var message = new HttpRequestMessage(
            method,
            uriBuilder.Uri);

        message.Headers.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        if (!string.IsNullOrWhiteSpace(functionKey))
        {
            message.Headers.Add(
                "x-functions-key",
                functionKey);
        }

        if (apiRequest.Data != null)
        {
            message.Content = new StringContent(
                JsonSerializer.Serialize(apiRequest.Data, JsonOptions),
                Encoding.UTF8,
                "application/json");
        }

        if (!string.IsNullOrWhiteSpace(apiRequest.AzureStorage))
        {
            message.Headers.Add(
                "azurestorageconnection",
                apiRequest.AzureStorage);
        }

        using var response = await client.SendAsync(message);

        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Request failed ({response.StatusCode}): {content}");
        }

        return JsonSerializer.Deserialize<T>(
            content,
            JsonOptions)!;
    }
}