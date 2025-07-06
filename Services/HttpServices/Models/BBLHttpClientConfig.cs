namespace HttpClientFactoryCustom.Services.HttpServices.Models
{
    public class BBLHttpClientConfig
    {
        public string UserAgent { get; set; } = "BBL-Connect";
        public int Timeout { get; set; } = 30000;
       // public string BaseUrl { get; set; }
        public bool IgnoreSsl { get; set; } = false; // 🔴 New
        public object Query { get; set; }
        public object Data { get; set; }
        public bool UseFormEncoding { get; set; }
        public string ContentType { get; set; }= "application/json";
        public Dictionary<string, IEnumerable<string>> Headers { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public Action<HttpRequestMessage, Dictionary<string, string>, BBLHttpClientConfig> OnBeforeSend;
        public List<Func<string, HttpResponseMessage, string>> ResponseTransformers { get; set; } = new();

        public BBLHttpClientConfig WithQuery(object query) { Query = query; return this; }
        public BBLHttpClientConfig WithData(object data) { Data = data; return this; }
        public BBLHttpClientConfig WithFormEncoding() { UseFormEncoding = true; return this; }
        public BBLHttpClientConfig WithIgnoreSsl(bool ignore = true) { IgnoreSsl = ignore; return this; }
        public BBLHttpClientConfig WithHeader(string name, string value)
        {
            Headers[name] = new[] { value };
            return this;
        }
    }

}
