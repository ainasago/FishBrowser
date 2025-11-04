using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;

namespace FishBrowser.Web.Controllers;

public class BrowserController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<BrowserController> _logger;

    public BrowserController(IHttpClientFactory httpClientFactory, ILogger<BrowserController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetRunningBrowsers()
    {
        try
        {
            var client = GetAuthenticatedClient();
            var response = await client.GetAsync("/api/browsers/running");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return Content(content, "application/json");
            }

            return StatusCode((int)response.StatusCode);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting running browsers");
            return StatusCode(500, new { error = ex.Message });
        }
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

    public async Task<IActionResult> Index()
    {
        try
        {
            // 检查登录状态
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
            _logger.LogError(ex, "Error loading browser index");
            return RedirectToAction("Login", "Auth");
        }
    }

    public IActionResult Create()
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
            _logger.LogError(ex, "Error loading browser create page");
            return RedirectToAction("Login", "Auth");
        }
    }

    public async Task<IActionResult> Edit(int id)
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
            ViewBag.BrowserId = id;

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading browser edit page");
            return RedirectToAction("Login", "Auth");
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetBrowsers(int page = 1, int pageSize = 20, string? search = null, int? groupId = null)
    {
        try
        {
            var client = GetAuthenticatedClient();
            var queryParams = $"?page={page}&pageSize={pageSize}";
            
            if (!string.IsNullOrEmpty(search))
                queryParams += $"&search={Uri.EscapeDataString(search)}";
            
            if (groupId.HasValue)
                queryParams += $"&groupId={groupId}";

            var response = await client.GetAsync($"/api/browsers{queryParams}");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return Content(content, "application/json");
            }

            return StatusCode((int)response.StatusCode);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting browsers");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetStatistics()
    {
        try
        {
            var client = GetAuthenticatedClient();
            var response = await client.GetAsync("/api/browsers/statistics");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return Content(content, "application/json");
            }

            return StatusCode((int)response.StatusCode);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting statistics");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetGroups()
    {
        try
        {
            var client = GetAuthenticatedClient();
            var response = await client.GetAsync("/api/groups");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return Content(content, "application/json");
            }

            return StatusCode((int)response.StatusCode);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting groups");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetBrowserDetail(int id)
    {
        try
        {
            var client = GetAuthenticatedClient();
            var response = await client.GetAsync($"/api/browsers/{id}");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return Content(content, "application/json");
            }

            return StatusCode((int)response.StatusCode);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting browser detail");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPut]
    public async Task<IActionResult> UpdateBrowser(int id, [FromBody] UpdateBrowserDto dto)
    {
        try
        {
            var client = GetAuthenticatedClient();
            var json = JsonSerializer.Serialize(dto);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await client.PutAsync($"/api/browsers/{id}", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return Content(responseContent, "application/json");
            }

            return StatusCode((int)response.StatusCode);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating browser");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateBrowser([FromBody] UpdateBrowserDto dto)
    {
        try
        {
            var client = GetAuthenticatedClient();
            var json = JsonSerializer.Serialize(dto);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await client.PostAsync("/api/browsers", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return Content(responseContent, "application/json");
            }

            return StatusCode((int)response.StatusCode);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating browser");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> LaunchBrowser(int id)
    {
        try
        {
            var client = GetAuthenticatedClient();
            var response = await client.PostAsync($"/api/browsers/{id}/launch", null);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return Content(content, "application/json");
            }

            return StatusCode((int)response.StatusCode);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error launching browser");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> StopBrowser(int id)
    {
        try
        {
            var client = GetAuthenticatedClient();
            var response = await client.PostAsync($"/api/browsers/{id}/stop", null);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return Content(content, "application/json");
            }

            return StatusCode((int)response.StatusCode);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping browser");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetBrowserLogs(int id)
    {
        try
        {
            var token = HttpContext.Session.GetString("Token");
            if (string.IsNullOrEmpty(token))
            {
                return Unauthorized();
            }

            // 从日志文件读取浏览器相关日志
            var logPath = Path.Combine(Directory.GetCurrentDirectory(), "logs");
            var logs = new List<dynamic>();

            if (Directory.Exists(logPath))
            {
                var logFiles = Directory.GetFiles(logPath, "*.log")
                    .OrderByDescending(f => System.IO.File.GetLastWriteTime(f))
                    .Take(5);

                foreach (var file in logFiles)
                {
                    try
                    {
                        var lines = System.IO.File.ReadAllLines(file)
                            .Where(l => l.Contains($"Browser {id}") || l.Contains("浏览器"))
                            .TakeLast(50);

                        foreach (var line in lines)
                        {
                            logs.Add(new
                            {
                                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                                level = "INFO",
                                message = line
                            });
                        }
                    }
                    catch { }
                }
            }

            // 如果没有日志文件，返回示例日志
            if (!logs.Any())
            {
                logs.Add(new
                {
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    level = "INFO",
                    message = "浏览器已就绪，等待启动..."
                });
            }

            return Ok(new
            {
                success = true,
                logs = logs.OrderByDescending(l => l.timestamp).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting browser logs");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteBrowser(int id)
    {
        try
        {
            var client = GetAuthenticatedClient();
            var response = await client.DeleteAsync($"/api/browsers/{id}");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return Content(content, "application/json");
            }

            return StatusCode((int)response.StatusCode);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting browser");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateRandomBrowser()
    {
        try
        {
            var client = GetAuthenticatedClient();
            var response = await client.PostAsync("/api/browsers/random", null);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return Content(content, "application/json");
            }

            return StatusCode((int)response.StatusCode);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating random browser");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

public class UpdateBrowserDto
{
    public string? Name { get; set; }
    public string? Notes { get; set; }
    public int? GroupId { get; set; }
    
    // 浏览器配置
    public string? Engine { get; set; }
    public string? OS { get; set; }
    public int? ViewportWidth { get; set; }
    public int? ViewportHeight { get; set; }
    public string? Locale { get; set; }
    public string? Timezone { get; set; }
    public bool? EnablePersistence { get; set; }
    public bool? Headless { get; set; }
    
    // 指纹特征
    public string? UserAgent { get; set; }
    public string? Platform { get; set; }
    public int? HardwareConcurrency { get; set; }
    public int? DeviceMemory { get; set; }
    public int? MaxTouchPoints { get; set; }
    public string? WebGLVendor { get; set; }
    public string? WebGLRenderer { get; set; }
    public string? FontsJson { get; set; }
    
    // 代理配置
    public string? ProxyMode { get; set; }
    public string? ProxyApiUrl { get; set; }
}
