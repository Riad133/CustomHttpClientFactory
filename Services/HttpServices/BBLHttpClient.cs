using System.Diagnostics;
using System.Text.Json;
using System.Text;
using HttpClientFactoryCustom.Services.HttpServices.Models;

namespace HttpClientFactoryCustom.Services.HttpServices
{
    public class BBLHttpClient
    {
        private readonly ILogger<BBLHttpClient> _logger;
      // private readonly HttpClient _httpClientFactory;
        public BBLHttpClientConfig DefaultConfig = new();
        private HttpClient http;
        private ILogger<BBLHttpClient> clientLogger;
        private BBLHttpClientConfig bBLHttpClientConfig;

        public bool Debug { get; set; }

        private readonly Dictionary<string, Dictionary<string, IEnumerable<string>>> _methodHeaders = new(StringComparer.OrdinalIgnoreCase)
        {
            ["COMMON"] = new(StringComparer.OrdinalIgnoreCase),
            ["GET"] = new(StringComparer.OrdinalIgnoreCase),
            ["POST"] = new(StringComparer.OrdinalIgnoreCase),
            ["PUT"] = new(StringComparer.OrdinalIgnoreCase),
            ["DELETE"] = new(StringComparer.OrdinalIgnoreCase)
        };

        

        public BBLHttpClient(HttpClient http, ILogger<BBLHttpClient> clientLogger, BBLHttpClientConfig bBLHttpClientConfig)
        {
            this.http = http;
            this.clientLogger = clientLogger;
            this.bBLHttpClientConfig = bBLHttpClientConfig;
        }

        public BBLHttpClient Configure(BBLHttpClientConfig config)
        {
            DefaultConfig = config;
            _methodHeaders["COMMON"]["User-Agent"] = new[] { config.UserAgent };
            return this;
        }

        public async Task<HttpClientResponse<T>> Send<T>(
            string method,
            string url = "",
            BBLHttpClientConfig config = null,
            bool ignoreDefaultHeaders = false)
        {
            var sw = Stopwatch.StartNew();
            config = MergeConfig(config, DefaultConfig);
            method = method.ToUpper();

            var httpClient = CreateHttpClient(config);

            using var request = CreateRequest(method, url, config, httpClient.BaseAddress);
            var headers = PrepareHeaders(method, config, ignoreDefaultHeaders);

            SetRequestContent(request, config);
            ApplyHeaders(request, headers);
            config.OnBeforeSend?.Invoke(request, headers, config);

           //f (Debug) LogRequest(request);

            try
            {
                using var response = await httpClient.SendAsync(request);
                var responseData = await ProcessResponse<T>(response, config);
                sw.Stop();

       //       await LogRequestResponse(request, responseData, response.StatusCode, headers, sw.ElapsedMilliseconds);
                return new HttpClientResponse<T>(responseData, (int)response.StatusCode, response.Content.Headers.ContentType?.MediaType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HTTP request failed: {Method} {Url}", method, url);
                return new HttpClientResponse<T>(default, 0, null);
            }
        }

        private HttpClient CreateHttpClient(BBLHttpClientConfig config)
        {
            var client = http;
            if (config.Timeout > 0)
                client.Timeout = TimeSpan.FromMilliseconds(config.Timeout);
                //if (!string.IsNullOrWhiteSpace(config.BaseUrl))
                //    client.BaseAddress = new Uri(config.BaseUrl);
            return client;
        }

        private HttpRequestMessage CreateRequest(string method, string url, BBLHttpClientConfig config, Uri baseAddress)
        {
            var baseUrl = baseAddress?.ToString() ?? "";
            var fullUrl = new Uri(new Uri(baseUrl), BuildUrl(url, config.Query));
            return new HttpRequestMessage(new HttpMethod(method), fullUrl);
        }

        private Dictionary<string, string> PrepareHeaders(string method, BBLHttpClientConfig config, bool ignoreDefaultHeaders)
        {
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (!ignoreDefaultHeaders)
            {
                AddHeaders(headers, _methodHeaders["COMMON"]);
                if (_methodHeaders.TryGetValue(method, out var methodHeaders))
                    AddHeaders(headers, methodHeaders);
            }

            if (config.Headers != null)
                AddHeaders(headers, config.Headers);

            return headers;
        }

        private void SetRequestContent(HttpRequestMessage request, BBLHttpClientConfig config)
        {
            if (config.Data == null) return;

            switch (config.Data)
            {
                case byte[] bytes:
                    request.Content = new ByteArrayContent(bytes);
                    break;

                case Stream stream:
                    request.Content = new StreamContent(stream);
                    break;

                case string str:
                    request.Content = new StringContent(str, Encoding.UTF8, config.ContentType ?? "application/json");
                    break;

                default:
                    var content = config.UseFormEncoding
                                     ? BuildFormContent(config.Data)
                                    : JsonSerializer.Serialize(config.Data);

                    request.Content = new StringContent(
                        content,
                        Encoding.UTF8,
                        config.UseFormEncoding ? "application/x-www-form-urlencoded" : (config.ContentType ?? "application/json")
                    );
                    break;
            }
        }

        private void ApplyHeaders(HttpRequestMessage request, Dictionary<string, string> headers)
        {
            foreach (var h in headers)
            {
                if (!request.Headers.TryAddWithoutValidation(h.Key, h.Value))
                {
                    request.Content?.Headers.TryAddWithoutValidation(h.Key, h.Value);
                }
            }
        }

        private async Task<object> ProcessResponse<T>(HttpResponseMessage response, BBLHttpClientConfig config)
        {
            if (typeof(T) == typeof(byte[]))
                return await response.Content.ReadAsByteArrayAsync();

            if (typeof(T) == typeof(Stream))
                return await response.Content.ReadAsStreamAsync();

            var content = await response.Content.ReadAsStringAsync();

            foreach (var transformer in config.ResponseTransformers)
                content = transformer(content, response);

            return typeof(T) == typeof(string)
                ? content
                : JsonSerializer.Deserialize<T>(content);
        }

        private void AddHeaders(Dictionary<string, string> target, Dictionary<string, IEnumerable<string>> source)
        {
            foreach (var h in source)
                target[h.Key] = string.Join(",", h.Value);
        }

        private string BuildUrl(string url, object query)
        {
            if (query == null) return url;

            var queryParams = query.GetType().GetProperties()
                .Select(p => $"{Uri.EscapeDataString(p.Name)}={Uri.EscapeDataString(p.GetValue(query)?.ToString() ?? "")}");

            return $"{url}{(url.Contains('?') ? '&' : '?')}{string.Join("&", queryParams)}";
        }

        private string BuildFormContent(object data)
        {
            return string.Join("&", data.GetType().GetProperties()
                .Select(p => $"{Uri.EscapeDataString(p.Name)}={Uri.EscapeDataString(p.GetValue(data)?.ToString() ?? "")}"));
        }

        private async Task LogRequestResponse(
            HttpRequestMessage request,
            object responseContent,
            System.Net.HttpStatusCode statusCode,
            Dictionary<string, string> headers,
            long elapsedMs)
        {
            string requestBody = "";
            if (request.Content != null)
            {
                requestBody = request.Content is ByteArrayContent
                    ? $"[Binary Data: {((ByteArrayContent)request.Content).Headers.ContentLength} bytes]"
                    : await request.Content.ReadAsStringAsync();
            }

            _logger.LogInformation("HTTP Request => {@Request}", new
            {
                request.Method,
                request.RequestUri,
                Headers = headers,
                Body = requestBody
            });

            _logger.LogInformation("HTTP Response => {@Response}", new
            {
                StatusCode = (int)statusCode,
                Body = responseContent is byte[] bytes
                    ? $"[Binary Data: {bytes.Length} bytes]"
                    : responseContent,
                ElapsedMs = elapsedMs
            });
        }

        private void LogRequest(HttpRequestMessage request)
        {
            var contentInfo = request.Content switch
            {
                ByteArrayContent bac => $"[Binary Data: {bac.Headers.ContentLength} bytes]",
                StreamContent sc => $"[Stream Data: {sc.Headers.ContentLength} bytes]",
           //     StringContent sc => sc.ReadAsStringAsync().Result,
                _ => ""
            };

            Console.WriteLine($"{request.Method} {request.RequestUri}");
            foreach (var h in request.Headers)
                Console.WriteLine($"{h.Key}: {string.Join(", ", h.Value)}");
            Console.WriteLine($"\n{contentInfo}\n");
        }

        public Task<HttpClientResponse<T>> Get<T>(string url = "", object query = null, BBLHttpClientConfig config = null) =>
            Send<T>("GET", url, config?.WithQuery(query));

        public Task<HttpClientResponse<T>> Post<T>(string url = "", object data = null, string contentType = null, BBLHttpClientConfig config = null)
        {
            config = config?.WithData(data) ?? new BBLHttpClientConfig().WithData(data);
            if (contentType != null) config.ContentType = contentType;
            return Send<T>("POST", url, config);
        }

        public Task<HttpClientResponse<T>> Put<T>(string url = "", object data = null, string contentType = null, BBLHttpClientConfig config = null)
        {
            config = config?.WithData(data) ?? new BBLHttpClientConfig().WithData(data);
            if (contentType != null) config.ContentType = contentType;
            return Send<T>("PUT", url, config);
        }

        public Task<HttpClientResponse<T>> Delete<T>(string url = "", BBLHttpClientConfig config = null) =>
            Send<T>("DELETE", url, config);

        public Task<HttpClientResponse<byte[]>> GetBytes(string url = "", object query = null, BBLHttpClientConfig config = null) =>
            Send<byte[]>("GET", url, config?.WithQuery(query));

        public Task<HttpClientResponse<Stream>> GetStream(string url = "", object query = null, BBLHttpClientConfig config = null) =>
            Send<Stream>("GET", url, config?.WithQuery(query));

        private BBLHttpClientConfig MergeConfig(BBLHttpClientConfig custom, BBLHttpClientConfig defaults)
        {
            if (custom == null) return defaults;

            return new BBLHttpClientConfig
            {
                Timeout = custom.Timeout > 0 ? custom.Timeout : defaults.Timeout,
              //  BaseUrl = custom.BaseUrl ?? defaults.BaseUrl,
                Query = custom.Query ?? defaults.Query,
                Data = custom.Data ?? defaults.Data,
                Headers = MergeDictionaries(defaults.Headers, custom.Headers),
                UserAgent = !string.IsNullOrEmpty(custom.UserAgent) ? custom.UserAgent : defaults.UserAgent,
                UseFormEncoding = custom.UseFormEncoding,
                ContentType = custom.ContentType ?? defaults.ContentType,
                OnBeforeSend = custom.OnBeforeSend ?? defaults.OnBeforeSend,
                ResponseTransformers = custom.ResponseTransformers?.Concat(defaults.ResponseTransformers).ToList()
                                      ?? defaults.ResponseTransformers
            };
        }

        private Dictionary<string, IEnumerable<string>> MergeDictionaries(
            Dictionary<string, IEnumerable<string>> first,
            Dictionary<string, IEnumerable<string>> second)
        {
            var result = new Dictionary<string, IEnumerable<string>>(first, StringComparer.OrdinalIgnoreCase);
            foreach (var kv in second)
            {
                result[kv.Key] = kv.Value;
            }
            return result;
        }
    }
}
