using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;

namespace FishBrowser.Web.Controllers;

public class SystemController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SystemController> _logger;

    public SystemController(IHttpClientFactory httpClientFactory, ILogger<SystemController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    private HttpClient GetAuthenticatedClient()
    {
        var client = _httpClientFactory.CreateClient("FishBrowserApi");
        var token = HttpContext.Session.GetString("Token");
        if (string.IsNullOrEmpty(token)) throw new UnauthorizedAccessException("未登录");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public IActionResult Playwright()
    {
        var token = HttpContext.Session.GetString("Token");
        if (string.IsNullOrEmpty(token)) return RedirectToAction("Login", "Auth");
        ViewBag.Username = HttpContext.Session.GetString("Username");
        ViewBag.Role = HttpContext.Session.GetString("Role");
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetPlaywrightStatus()
    {
        try
        {
            var client = GetAuthenticatedClient();
            var resp = await client.GetAsync("/api/system/playwright/status");
            var content = await resp.Content.ReadAsStringAsync();
            return StatusCode((int)resp.StatusCode, content);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Playwright status");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> InstallPlaywright()
    {
        try
        {
            var client = GetAuthenticatedClient();
            var resp = await client.PostAsync("/api/system/playwright/install", null);
            var content = await resp.Content.ReadAsStringAsync();
            return StatusCode((int)resp.StatusCode, content);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error installing Playwright");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> UpdatePlaywright()
    {
        try
        {
            var client = GetAuthenticatedClient();
            var resp = await client.PostAsync("/api/system/playwright/update", null);
            var content = await resp.Content.ReadAsStringAsync();
            return StatusCode((int)resp.StatusCode, content);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating Playwright");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> UninstallPlaywright()
    {
        try
        {
            var client = GetAuthenticatedClient();
            var resp = await client.PostAsync("/api/system/playwright/uninstall", null);
            var content = await resp.Content.ReadAsStringAsync();
            return StatusCode((int)resp.StatusCode, content);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uninstalling Playwright");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
