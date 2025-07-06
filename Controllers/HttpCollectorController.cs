using HttpClientFactoryCustom.Services.HttpServices.Models;
using HttpClientFactoryCustom.Services.HttpServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using HttpClientFactoryCustom.Models;

namespace HttpClientFactoryCustom.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HttpCollectorController : ControllerBase
    {
        private readonly BBLHttpClient _client;
        private readonly ILogger<HttpCollectorController> _logger;

        public HttpCollectorController(IHttpClientFactory httpClientFactory, ILogger<HttpCollectorController> logger, ILogger<BBLHttpClient> clientLogger)
        {
            var http = httpClientFactory.CreateClient("ExternalApiClient");

            _client = new BBLHttpClient(http, clientLogger, new BBLHttpClientConfig
            {
              
                Timeout = 10000
            });

            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<IActionResult> ProxyLogin([FromBody] LoginRequestModel request)
        {
            var config = new BBLHttpClientConfig()
                .WithData(request)
            .WithHeader("X-Proxy", "AuthGateway");

            config.ContentType = "application/json"; // ✅ Important!
            var response = await _client.Post<LoginResponse>(
                url: "api/login",
                request,
                config: config
            );

            if (response.StatusCode == 200 && response.Data != null)
                return Ok(response.Data);

            _logger.LogWarning("Login proxy failed. Status: {Status}", response.StatusCode);
            return StatusCode(response.StatusCode, new { message = "Login failed" });
        }
    }
}
