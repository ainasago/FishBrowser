using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;

namespace FishBrowser.Web.Controllers;

public class AIProviderController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AIProviderController> _logger;

    public AIProviderController(IHttpClientFactory httpClientFactory, ILogger<AIProviderController> logger)
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

    public IActionResult Index()
    {
        var token = HttpContext.Session.GetString("Token");
        if (string.IsNullOrEmpty(token)) return RedirectToAction("Login", "Auth");
        ViewBag.Username = HttpContext.Session.GetString("Username");
        ViewBag.Role = HttpContext.Session.GetString("Role");
        return View();
    }

    public IActionResult Edit(int id)
    {
        var token = HttpContext.Session.GetString("Token");
        if (string.IsNullOrEmpty(token)) return RedirectToAction("Login", "Auth");
        ViewBag.Username = HttpContext.Session.GetString("Username");
        ViewBag.Role = HttpContext.Session.GetString("Role");
        ViewBag.ProviderId = id;
        return View();
    }

    public IActionResult QuickSetup()
    {
        var token = HttpContext.Session.GetString("Token");
        if (string.IsNullOrEmpty(token)) return RedirectToAction("Login", "Auth");
        ViewBag.Username = HttpContext.Session.GetString("Username");
        ViewBag.Role = HttpContext.Session.GetString("Role");
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var client = GetAuthenticatedClient();
            var resp = await client.GetAsync("/api/aiproviders");
            var content = await resp.Content.ReadAsStringAsync();
            return StatusCode((int)resp.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting providers");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var client = GetAuthenticatedClient();
            var resp = await client.GetAsync($"/api/aiproviders/{id}");
            var content = await resp.Content.ReadAsStringAsync();
            return StatusCode((int)resp.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting provider");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] object request)
    {
        try
        {
            var client = GetAuthenticatedClient();
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var resp = await client.PostAsync("/api/aiproviders", content);
            var responseContent = await resp.Content.ReadAsStringAsync();
            return StatusCode((int)resp.StatusCode, responseContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating provider");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPut]
    public async Task<IActionResult> Update(int id, [FromBody] object request)
    {
        try
        {
            var client = GetAuthenticatedClient();
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var resp = await client.PutAsync($"/api/aiproviders/{id}", content);
            var responseContent = await resp.Content.ReadAsStringAsync();
            return StatusCode((int)resp.StatusCode, responseContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating provider");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpDelete]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var client = GetAuthenticatedClient();
            var resp = await client.DeleteAsync($"/api/aiproviders/{id}");
            var content = await resp.Content.ReadAsStringAsync();
            return StatusCode((int)resp.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting provider");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> AddApiKey(int id, [FromBody] object request)
    {
        try
        {
            var client = GetAuthenticatedClient();
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var resp = await client.PostAsync($"/api/aiproviders/{id}/apikeys", content);
            var responseContent = await resp.Content.ReadAsStringAsync();
            return StatusCode((int)resp.StatusCode, responseContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding API key");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteApiKey(int keyId)
    {
        try
        {
            var client = GetAuthenticatedClient();
            var resp = await client.DeleteAsync($"/api/aiproviders/apikeys/{keyId}");
            var content = await resp.Content.ReadAsStringAsync();
            return StatusCode((int)resp.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting API key");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> TestConnection(int id)
    {
        try
        {
            var client = GetAuthenticatedClient();
            var resp = await client.PostAsync($"/api/aiproviders/{id}/test", null);
            var content = await resp.Content.ReadAsStringAsync();
            return StatusCode((int)resp.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing connection");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
