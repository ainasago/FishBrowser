using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace FishBrowser.Web.Controllers;

/// <summary>
/// 指纹数据代理控制器 - 通过 Web 后台调用 API
/// </summary>
public class FingerprintDataController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<FingerprintDataController> _logger;

    public FingerprintDataController(
        IHttpClientFactory httpClientFactory,
        ILogger<FingerprintDataController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
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

    /// <summary>
    /// 获取随机 Chrome 版本
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetChromeVersion(string os = "Windows")
    {
        try
        {
            var client = GetAuthenticatedClient();
            var response = await client.GetAsync($"/api/FingerprintData/chrome-version?os={Uri.EscapeDataString(os)}");
            var content = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                return Content(content, "application/json");
            }
            else
            {
                return Json(new { success = false, error = "获取 Chrome 版本失败" });
            }
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Chrome version for OS: {OS}", os);
            return Json(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// 获取随机 GPU 配置
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetGpu(string os = "Windows", int count = 1)
    {
        try
        {
            var client = GetAuthenticatedClient();
            var response = await client.GetAsync($"/api/FingerprintData/gpu?os={Uri.EscapeDataString(os)}&count={count}");
            var content = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                return Content(content, "application/json");
            }
            else
            {
                return Json(new { success = false, error = "获取 GPU 配置失败" });
            }
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting GPU for OS: {OS}", os);
            return Json(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// 获取随机字体列表
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetFonts(string os = "windows", int count = 40)
    {
        try
        {
            var client = GetAuthenticatedClient();
            var response = await client.GetAsync($"/api/FingerprintData/fonts?os={Uri.EscapeDataString(os)}&count={count}");
            var content = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                return Content(content, "application/json");
            }
            else
            {
                return Json(new { success = false, error = "获取字体列表失败" });
            }
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting fonts for OS: {OS}", os);
            return Json(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// 生成防检测数据
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> GenerateAntiDetection([FromBody] AntiDetectionRequest request)
    {
        try
        {
            var client = GetAuthenticatedClient();
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"/api/FingerprintData/anti-detection", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                return Content(responseContent, "application/json");
            }
            else
            {
                return Json(new { success = false, error = "生成防检测数据失败" });
            }
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating anti-detection data");
            return Json(new { success = false, error = ex.Message });
        }
    }
}

public class AntiDetectionRequest
{
    public string UserAgent { get; set; } = "";
    public string Platform { get; set; } = "Win32";
    public string Locale { get; set; } = "zh-CN";
    public int HardwareConcurrency { get; set; } = 8;
    public int DeviceMemory { get; set; } = 8;
    public int MaxTouchPoints { get; set; } = 0;
}
