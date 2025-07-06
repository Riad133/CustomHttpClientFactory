namespace HttpClientFactoryCustom.Services.HttpServices.Models
{
    public class HttpClientResponse<T>
    {
        public T Data { get; }
        public int StatusCode { get; }
        public string ContentType { get; }

        public HttpClientResponse(object rawData, int statusCode, string contentType)
        {
            StatusCode = statusCode;
            ContentType = contentType;
            Data = rawData switch
            {
                T typed => typed,
                _ => default
            };
        }
    }
}
