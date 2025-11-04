using Microsoft.AspNetCore.Mvc;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FishBrowser.Web.Controllers
{
    public class StagehandTaskController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<StagehandTaskController> _logger;

        public StagehandTaskController(
            IHttpClientFactory httpClientFactory,
            ILogger<StagehandTaskController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        private HttpClient GetAuthenticatedClient()
        {
            var client = _httpClientFactory.CreateClient("FishBrowserApi");
            var token = HttpContext.Session.GetString("Token");
            if (string.IsNullOrEmpty(token))
            {
                throw new UnauthorizedAccessException("未登录");
            }
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        [HttpGet]
        public async Task<IActionResult> GetStatus()
        {
            try
            {
                var client = GetAuthenticatedClient();
                var response = await client.GetAsync("api/StagehandTask/status");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return Content(content, "application/json");
                }

                return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Get status failed");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetExamples()
        {
            try
            {
                var client = GetAuthenticatedClient();
                var response = await client.GetAsync("api/StagehandTask/examples");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return Content(content, "application/json");
                }

                return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Get examples failed");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> GenerateScript([FromBody] JsonElement request)
        {
            try
            {
                var client = GetAuthenticatedClient();
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync("api/StagehandTask/generate", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return Content(responseContent, "application/json");
                }

                return StatusCode((int)response.StatusCode, responseContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Generate script failed");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ExecuteScript([FromBody] JsonElement request)
        {
            try
            {
                var client = GetAuthenticatedClient();
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync("api/StagehandTask/execute", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return Content(responseContent, "application/json");
                }

                return StatusCode((int)response.StatusCode, responseContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Execute script failed");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
