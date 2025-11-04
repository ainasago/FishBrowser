using Microsoft.AspNetCore.Mvc;
using FishBrowser.WPF.Services;

namespace FishBrowser.Api.Controllers;

/// <summary>
/// 指纹数据服务 API - 提供 Chrome 版本、GPU、字体等数据
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class FingerprintDataController : ControllerBase
{
    private readonly ChromeVersionDatabase _chromeVersionDb;
    private readonly GpuCatalogService _gpuCatalog;
    private readonly FontService _fontService;
    private readonly AntiDetectionService _antiDetectionService;
    private readonly ILogger<FingerprintDataController> _logger;

    public FingerprintDataController(
        ChromeVersionDatabase chromeVersionDb,
        GpuCatalogService gpuCatalog,
        FontService fontService,
        AntiDetectionService antiDetectionService,
        ILogger<FingerprintDataController> logger)
    {
        _chromeVersionDb = chromeVersionDb;
        _gpuCatalog = gpuCatalog;
        _fontService = fontService;
        _antiDetectionService = antiDetectionService;
        _logger = logger;
    }

    /// <summary>
    /// 获取随机 Chrome 版本
    /// </summary>
    [HttpGet("chrome-version")]
    public ActionResult<object> GetRandomChromeVersion([FromQuery] string os = "Windows")
    {
        try
        {
            // 映射前端 OS 名称到 ChromeVersionDatabase 的 key
            var osKey = os switch
            {
                "Windows" => "Windows",
                "MacOS" or "Mac" => "Mac",
                "Linux" => "Linux",
                "Android" => "Linux", // Android 使用 Linux 的 Chrome 版本
                "iOS" => "Mac", // iOS 使用 Safari，但如果需要 Chrome 版本则用 Mac
                _ => "Windows"
            };

            var version = _chromeVersionDb.GetRandomVersion(osKey);
            if (version == null)
            {
                return NotFound(new { success = false, error = $"No Chrome version found for OS: {os}" });
            }

            return Ok(new
            {
                success = true,
                data = new
                {
                    version = version.Version,
                    releaseDate = version.ReleaseDate
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Chrome version for OS: {OS}", os);
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// 获取随机 GPU 配置
    /// </summary>
    [HttpGet("gpu")]
    public async Task<ActionResult<object>> GetRandomGpu([FromQuery] string os = "Windows", [FromQuery] int count = 1)
    {
        try
        {
            var osKey = os switch
            {
                "Windows" => "Windows",
                "MacOS" or "Mac" => "macOS",
                "Linux" => "Linux",
                "Android" => "Android",
                "iOS" => "iOS",
                _ => "Windows"
            };

            var gpus = await _gpuCatalog.RandomSubsetAsync(osKey, count);
            
            return Ok(new
            {
                success = true,
                data = gpus.Select(g => new
                {
                    vendor = g.Vendor,
                    renderer = g.Renderer
                }).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting GPU for OS: {OS}", os);
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// 获取随机字体列表
    /// </summary>
    [HttpGet("fonts")]
    public async Task<ActionResult<object>> GetRandomFonts([FromQuery] string os = "windows", [FromQuery] int count = 40)
    {
        try
        {
            var osKey = os.ToLower() switch
            {
                "windows" => "windows",
                "macos" or "mac" => "macos",
                "linux" => "linux",
                "android" => "android",
                "ios" => "ios",
                _ => "windows"
            };

            var fonts = await _fontService.RandomSubsetAsync(osKey, count);
            
            return Ok(new
            {
                success = true,
                data = fonts
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting fonts for OS: {OS}", os);
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// 生成防检测数据（Languages, Plugins, Sec-CH-UA 等）
    /// </summary>
    [HttpPost("anti-detection")]
    public ActionResult<object> GenerateAntiDetectionData([FromBody] AntiDetectionRequest request)
    {
        try
        {
            var profile = new FishBrowser.WPF.Models.FingerprintProfile
            {
                UserAgent = request.UserAgent,
                Platform = request.Platform,
                Locale = request.Locale,
                HardwareConcurrency = request.HardwareConcurrency,
                DeviceMemory = request.DeviceMemory,
                MaxTouchPoints = request.MaxTouchPoints
            };

            _antiDetectionService.GenerateAntiDetectionData(profile);

            return Ok(new
            {
                success = true,
                data = new
                {
                    languagesJson = profile.LanguagesJson,
                    pluginsJson = profile.PluginsJson,
                    secChUa = profile.SecChUa,
                    secChUaPlatform = profile.SecChUaPlatform,
                    secChUaMobile = profile.SecChUaMobile,
                    connectionType = profile.ConnectionType,
                    connectionRtt = profile.ConnectionRtt,
                    connectionDownlink = profile.ConnectionDownlink
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating anti-detection data");
            return BadRequest(new { success = false, error = ex.Message });
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
