using Newtonsoft.Json;
using System.Runtime.CompilerServices;
using System.Text;

namespace DatabaseManager.ServerLessClient.Services
{
    public class LLMBaseService : ILLMBaseService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public LLMBaseService(
            IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<string> SendRawAsync(
            HttpMethod method,
            string url,
            object? body = null,
            Dictionary<string, string>? headers = null,
            CancellationToken ct = default)
        {
            var client = _httpClientFactory.CreateClient();

            using var request =
                new HttpRequestMessage(method, url);

            if (headers != null)
            {
                foreach (var header in headers)
                {
                    request.Headers.Add(
                        header.Key,
                        header.Value);
                }
            }

            if (body != null)
            {
                var json =
                    Newtonsoft.Json.JsonConvert.SerializeObject(body);

                request.Content =
                    new StringContent(
                        json,
                        Encoding.UTF8,
                        "application/json");
            }

            var response =
                await client.SendAsync(request, ct);

            var responseText =
                await response.Content.ReadAsStringAsync(ct);

            response.EnsureSuccessStatusCode();

            return responseText;
        }

        public async IAsyncEnumerable<string> StreamRawAsync(
            string url,
            object? body = null,
            Dictionary<string, string>? headers = null,
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            var client = _httpClientFactory.CreateClient();

            using var request = new HttpRequestMessage(HttpMethod.Post, url);

            if (headers != null)
            {
                foreach (var header in headers)
                    request.Headers.Add(header.Key, header.Value);
            }

            if (body != null)
            {
                var json = JsonConvert.SerializeObject(body);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            // HttpCompletionOption.ResponseHeadersRead is critical —
            // without it HttpClient buffers the entire response before returning
            using var response = await client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                ct);

            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream && !ct.IsCancellationRequested)
            {
                string? line = await reader.ReadLineAsync(ct);

                if (string.IsNullOrWhiteSpace(line)) continue;

                yield return line;
            }
        }
    }
}
