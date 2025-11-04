using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;

namespace FishBrowser.Web.Controllers;

public class LogsController : Controller
{
    private readonly ILogger<LogsController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public LogsController(ILogger<LogsController> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    private HttpClient GetAuthenticatedClient()
    {
        var client = _httpClientFactory.CreateClient("FishBrowserApi");
        var token = HttpContext.Session.GetString("Token");
        
        if (!string.IsNullOrEmpty(token))
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        
        return client;
    }

    public IActionResult Index()
    {
        try
        {
            var token = HttpContext.Session.GetString("Token");
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Login", "Auth");
            }

            ViewBag.Username = HttpContext.Session.GetString("Username");
            ViewBag.Role = HttpContext.Session.GetString("Role");

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading logs page");
            return RedirectToAction("Login", "Auth");
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetLogs()
    {
        try
        {
            var token = HttpContext.Session.GetString("Token");
            if (string.IsNullOrEmpty(token))
            {
                return Unauthorized();
            }

            // 调用 API 获取日志
            var client = GetAuthenticatedClient();
            var response = await client.GetAsync("/api/logs");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return Content(content, "application/json");
            }

            return StatusCode((int)response.StatusCode, new 
            { 
                success = false, 
                error = "无法从 API 获取日志" 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting logs from API");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }
}
