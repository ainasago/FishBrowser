using FishBrowser.Api.DTOs.Browser;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FishBrowser.WPF.Data;
using FishBrowser.WPF.Services;

namespace FishBrowser.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BrowsersController : ControllerBase
{
    private readonly WebScraperDbContext _context;
    private readonly ILogger<BrowsersController> _logger;
    private readonly FishBrowser.WPF.Services.BrowserEnvironmentService _browserEnvService;
    private readonly FishBrowser.Api.Services.BrowserLaunchService _launchService;
    private readonly FishBrowser.WPF.Services.BrowserSessionService _sessionService;
    private readonly BrowserRandomGenerator _randomGenerator;
    private readonly BrowserFingerprintService _fingerprintService;

    public BrowsersController(
        WebScraperDbContext context,
        ILogger<BrowsersController> logger,
        FishBrowser.WPF.Services.BrowserEnvironmentService browserEnvService,
        FishBrowser.Api.Services.BrowserLaunchService launchService,
        FishBrowser.WPF.Services.BrowserSessionService sessionService,
        BrowserRandomGenerator randomGenerator,
        BrowserFingerprintService fingerprintService)
    {
        _context = context;
        _logger = logger;
        _browserEnvService = browserEnvService;
        _launchService = launchService;
        _sessionService = sessionService;
        _randomGenerator = randomGenerator;
        _fingerprintService = fingerprintService;
    }

    /// <summary>
    /// 获取浏览器列表
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<BrowserDto>>> GetBrowsers([FromQuery] BrowserQueryParams queryParams)
    {
        try
        {
            var query = _context.BrowserEnvironments
                .Include(b => b.Group)
                .Include(b => b.FingerprintProfile)
                .AsQueryable();

            // 过滤
            if (queryParams.GroupId.HasValue)
            {
                if (queryParams.GroupId == -1)
                    query = query.Where(b => b.GroupId == null);
                else
                    query = query.Where(b => b.GroupId == queryParams.GroupId);
            }

            if (!string.IsNullOrEmpty(queryParams.Search))
            {
                var search = queryParams.Search.ToLower();
                query = query.Where(b =>
                    b.Name.ToLower().Contains(search) ||
                    (b.UserAgent != null && b.UserAgent.ToLower().Contains(search)));
            }

            // 排序
            query = queryParams.SortBy.ToLower() switch
            {
                "createdat" => queryParams.SortOrder == "desc"
                    ? query.OrderByDescending(b => b.CreatedAt)
                    : query.OrderBy(b => b.CreatedAt),
                "launchcount" => queryParams.SortOrder == "desc"
                    ? query.OrderByDescending(b => b.LaunchCount)
                    : query.OrderBy(b => b.LaunchCount),
                _ => queryParams.SortOrder == "desc"
                    ? query.OrderByDescending(b => b.Name)
                    : query.OrderBy(b => b.Name)
            };

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((queryParams.Page - 1) * queryParams.PageSize)
                .Take(queryParams.PageSize)
                .Select(b => new BrowserDto
                {
                    Id = b.Id,
                    Name = b.Name,
                    GroupId = b.GroupId,
                    GroupName = b.Group != null ? b.Group.Name : null,
                    Engine = b.Engine,
                    OS = b.OS,
                    UserAgent = b.UserAgent,
                    LaunchCount = b.LaunchCount,
                    EnablePersistence = b.EnablePersistence,
                    CreatedAt = b.CreatedAt,
                    LastLaunchedAt = b.LastLaunchedAt
                })
                .ToListAsync();

            return Ok(new PagedResult<BrowserDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = queryParams.Page,
                PageSize = queryParams.PageSize
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting browsers");
            return StatusCode(500, new { error = "获取浏览器列表失败" });
        }
    }

    /// <summary>
    /// 获取统计信息
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult> GetStatistics()
    {
        try
        {
            var totalCount = await _context.BrowserEnvironments.CountAsync();
            var groupCount = await _context.BrowserGroups.CountAsync();
            var runningCount = 0; // TODO: 实现运行状态检测

            return Ok(new
            {
                totalCount,
                runningCount,
                groupCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting statistics");
            return StatusCode(500, new { error = "获取统计信息失败" });
        }
    }

    /// <summary>
    /// 获取运行中的浏览器列表
    /// </summary>
    [HttpGet("running")]
    public async Task<ActionResult> GetRunningBrowsers()
    {
        try
        {
            var runningBrowsers = _launchService.GetRunningBrowsers();
            var result = new List<object>();
            
            foreach (var kvp in runningBrowsers)
            {
                var browser = await _context.BrowserEnvironments.FindAsync(kvp.Key);
                if (browser != null)
                {
                    result.Add(new
                    {
                        id = browser.Id,
                        name = browser.Name,
                        processId = kvp.Value,
                        engine = browser.Engine
                    });
                }
            }
            
            return Ok(new
            {
                success = true,
                data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting running browsers");
            return StatusCode(500, new { success = false, error = "获取运行中浏览器失败" });
        }
    }

    /// <summary>
    /// 获取浏览器详情（与 WPF LoadExistingEnvironment 逻辑一致）
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult> GetBrowser(int id)
    {
        try
        {
            var browser = await _context.BrowserEnvironments
                .Include(b => b.Group)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (browser == null)
            {
                return NotFound(new { success = false, error = "浏览器不存在" });
            }

            return Ok(new
            {
                success = true,
                data = new
                {
                    // 基本信息
                    id = browser.Id,
                    name = browser.Name,
                    notes = browser.Notes,
                    groupId = browser.GroupId,
                    groupName = browser.Group?.Name,
                    
                    // 浏览器配置
                    engine = browser.Engine,
                    os = browser.OS,
                    viewportWidth = browser.ViewportWidth,
                    viewportHeight = browser.ViewportHeight,
                    locale = browser.Locale,
                    timezone = browser.Timezone,
                    enablePersistence = browser.EnablePersistence,
                    userDataPath = browser.UserDataPath, // ⭐ 用户数据目录
                    
                    // 指纹特征
                    userAgent = browser.UserAgent,
                    platform = browser.Platform,
                    hardwareConcurrency = browser.HardwareConcurrency,
                    deviceMemory = browser.DeviceMemory,
                    maxTouchPoints = browser.MaxTouchPoints,
                    webglVendor = browser.WebGLVendor,
                    webglRenderer = browser.WebGLRenderer,
                    fontsJson = browser.FontsJson,
                    
                    // 防检测数据
                    languagesJson = browser.LanguagesJson,
                    pluginsJson = browser.PluginsJson,
                    secChUa = browser.SecChUa,
                    secChUaPlatform = browser.SecChUaPlatform,
                    secChUaMobile = browser.SecChUaMobile,
                    webdriverMode = browser.WebdriverMode,
                    connectionType = browser.ConnectionType,
                    connectionRtt = browser.ConnectionRtt,
                    connectionDownlink = browser.ConnectionDownlink,
                    
                    // 代理配置
                    proxyMode = browser.ProxyMode,
                    proxyApiUrl = browser.ProxyApiUrl,
                    
                    // 统计信息
                    launchCount = browser.LaunchCount,
                    createdAt = browser.CreatedAt,
                    lastLaunchedAt = browser.LastLaunchedAt
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting browser {Id}", id);
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// 创建浏览器（使用 WPF 相同的保存逻辑）
    /// </summary>
    [HttpPost]
    public ActionResult CreateBrowser([FromBody] CreateBrowserRequest request)
    {
        try
        {
            _logger.LogInformation("创建浏览器: Name={Name}, OS={OS}, Engine={Engine}", request.Name, request.OS, request.Engine);

            // 转换为 SaveBrowserEnvironmentRequest
            var saveRequest = new FishBrowser.WPF.Services.SaveBrowserEnvironmentRequest
            {
                Id = null, // 新建模式
                Name = request.Name ?? "未命名浏览器",
                Notes = request.Notes,
                GroupId = request.GroupId,
                Engine = request.Engine,
                OS = request.OS,
                ViewportWidth = request.ViewportWidth,
                ViewportHeight = request.ViewportHeight,
                Locale = request.Locale,
                Timezone = request.Timezone,
                EnablePersistence = request.EnablePersistence,
                UserAgent = request.UserAgent,
                Platform = request.Platform,
                HardwareConcurrency = request.HardwareConcurrency,
                DeviceMemory = request.DeviceMemory,
                MaxTouchPoints = request.MaxTouchPoints,
                WebGLVendor = request.WebGLVendor,
                WebGLRenderer = request.WebGLRenderer,
                FontsJson = request.FontsJson,
                LanguagesJson = request.LanguagesJson,
                PluginsJson = request.PluginsJson,
                SecChUa = request.SecChUa,
                SecChUaPlatform = request.SecChUaPlatform,
                SecChUaMobile = request.SecChUaMobile,
                WebdriverMode = request.WebdriverMode,
                ConnectionType = request.ConnectionType,
                ConnectionRtt = request.ConnectionRtt,
                ConnectionDownlink = request.ConnectionDownlink,
                ProxyMode = request.ProxyMode,
                ProxyApiUrl = request.ProxyApiUrl
            };

            // 调用 WPF 的保存逻辑
            var browser = _browserEnvService.SaveBrowserEnvironment(saveRequest);

            _logger.LogInformation("浏览器创建成功: Id={Id}, Name={Name}", browser.Id, browser.Name);

            return Ok(new
            {
                success = true,
                message = "浏览器创建成功",
                data = new
                {
                    id = browser.Id,
                    name = browser.Name,
                    groupId = browser.GroupId,
                    engine = browser.Engine,
                    os = browser.OS,
                    userAgent = browser.UserAgent,
                    createdAt = browser.CreatedAt
                }
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("创建浏览器验证失败: {Message}", ex.Message);
            return BadRequest(new { success = false, error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建浏览器失败: {Message}", ex.Message);
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// 一键随机创建浏览器（重用 CreateBrowser 逻辑）
    /// </summary>
    [HttpPost("random")]
    public async Task<ActionResult> CreateRandomBrowser()
    {
        try
        {
            _logger.LogInformation("开始创建随机浏览器");

            // ⭐ 使用 BrowserRandomGenerator 生成随机浏览器环境
            var (randomEnv, profile) = await _randomGenerator.GenerateRandomBrowserAsync();
            
            // ⭐ 转换为 SaveBrowserEnvironmentRequest（重用 CreateBrowser 的保存逻辑）
            var saveRequest = new FishBrowser.WPF.Services.SaveBrowserEnvironmentRequest
            {
                Id = null, // 新建模式
                Name = randomEnv.Name,
                Notes = randomEnv.Notes,
                GroupId = randomEnv.GroupId,
                Engine = randomEnv.Engine,
                OS = randomEnv.OS,
                ViewportWidth = randomEnv.ViewportWidth,
                ViewportHeight = randomEnv.ViewportHeight,
                Locale = randomEnv.Locale,
                Timezone = randomEnv.Timezone,
                EnablePersistence = randomEnv.EnablePersistence, // ⭐ 默认启用持久化
                UserAgent = randomEnv.UserAgent,
                Platform = randomEnv.Platform,
                HardwareConcurrency = randomEnv.HardwareConcurrency,
                DeviceMemory = randomEnv.DeviceMemory,
                MaxTouchPoints = randomEnv.MaxTouchPoints,
                WebGLVendor = randomEnv.WebGLVendor,
                WebGLRenderer = randomEnv.WebGLRenderer,
                FontsJson = randomEnv.FontsJson,
                LanguagesJson = randomEnv.LanguagesJson,
                PluginsJson = randomEnv.PluginsJson,
                SecChUa = randomEnv.SecChUa,
                SecChUaPlatform = randomEnv.SecChUaPlatform,
                SecChUaMobile = randomEnv.SecChUaMobile,
                WebdriverMode = randomEnv.WebdriverMode,
                ConnectionType = randomEnv.ConnectionType,
                ConnectionRtt = randomEnv.ConnectionRtt,
                ConnectionDownlink = randomEnv.ConnectionDownlink,
                ProxyMode = randomEnv.ProxyMode,
                ProxyApiUrl = randomEnv.ProxyApiUrl
            };

            // ⭐ 调用 WPF 的保存逻辑（与手动创建完全一致）
            var browser = _browserEnvService.SaveBrowserEnvironment(saveRequest);

            _logger.LogInformation("随机浏览器创建成功: Id={Id}, Name={Name}, OS={OS}", browser.Id, browser.Name, browser.OS);

            return Ok(new
            {
                success = true,
                message = "随机浏览器创建成功",
                data = new
                {
                    id = browser.Id,
                    name = browser.Name,
                    groupId = browser.GroupId,
                    engine = browser.Engine,
                    os = browser.OS,
                    userAgent = browser.UserAgent,
                    createdAt = browser.CreatedAt
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建随机浏览器失败: {Message}", ex.Message);
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public ActionResult UpdateBrowser(int id, [FromBody] CreateBrowserRequest request)
    {
        try
        {
            _logger.LogInformation("更新浏览器: Id={Id}, Name={Name}, OS={OS}, Engine={Engine}", id, request.Name, request.OS, request.Engine);

            // 转换为 SaveBrowserEnvironmentRequest（编辑模式）
            var saveRequest = new FishBrowser.WPF.Services.SaveBrowserEnvironmentRequest
            {
                Id = id, // 编辑模式
                Name = request.Name ?? "未命名浏览器",
                Notes = request.Notes,
                GroupId = request.GroupId,
                Engine = request.Engine,
                OS = request.OS,
                ViewportWidth = request.ViewportWidth,
                ViewportHeight = request.ViewportHeight,
                Locale = request.Locale,
                Timezone = request.Timezone,
                EnablePersistence = request.EnablePersistence,
                UserAgent = request.UserAgent,
                Platform = request.Platform,
                HardwareConcurrency = request.HardwareConcurrency,
                DeviceMemory = request.DeviceMemory,
                MaxTouchPoints = request.MaxTouchPoints,
                WebGLVendor = request.WebGLVendor,
                WebGLRenderer = request.WebGLRenderer,
                FontsJson = request.FontsJson,
                LanguagesJson = request.LanguagesJson,
                PluginsJson = request.PluginsJson,
                SecChUa = request.SecChUa,
                SecChUaPlatform = request.SecChUaPlatform,
                SecChUaMobile = request.SecChUaMobile,
                WebdriverMode = request.WebdriverMode,
                ConnectionType = request.ConnectionType,
                ConnectionRtt = request.ConnectionRtt,
                ConnectionDownlink = request.ConnectionDownlink,
                ProxyMode = request.ProxyMode,
                ProxyApiUrl = request.ProxyApiUrl
            };

            // 调用 WPF 的保存逻辑（会自动更新现有浏览器）
            var browser = _browserEnvService.SaveBrowserEnvironment(saveRequest);

            _logger.LogInformation("浏览器更新成功: Id={Id}, Name={Name}", browser.Id, browser.Name);

            return Ok(new
            {
                success = true,
                message = "浏览器更新成功",
                data = new
                {
                    id = browser.Id,
                    name = browser.Name,
                    groupId = browser.GroupId,
                    engine = browser.Engine,
                    os = browser.OS,
                    userAgent = browser.UserAgent
                }
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("更新浏览器验证失败: {Message}", ex.Message);
            return BadRequest(new { success = false, error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新浏览器失败: {Message}", ex.Message);
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    [HttpPost("{id}/launch")]
    public async Task<ActionResult> LaunchBrowser(int id)
    {
        try
        {
            var (success, message, processId) = await _launchService.LaunchBrowserAsync(id);
            
            if (success)
            {
                return Ok(new
                {
                    success = true,
                    message = message,
                    data = new
                    {
                        id = id,
                        processId = processId
                    }
                });
            }
            else
            {
                return BadRequest(new { success = false, error = message });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error launching browser {Id}", id);
            return BadRequest(new { success = false, error = ex.Message });
        }
    }
    
    [HttpPost("{id}/stop")]
    public async Task<ActionResult> StopBrowser(int id)
    {
        try
        {
            var success = await _launchService.StopBrowserAsync(id);
            
            if (success)
            {
                return Ok(new
                {
                    success = true,
                    message = "浏览器已停止"
                });
            }
            else
            {
                return NotFound(new { success = false, error = "浏览器未在运行" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping browser {Id}", id);
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// 获取浏览器指纹信息（支持文本和 JSON 格式）
    /// </summary>
    /// <param name="id">浏览器 ID</param>
    /// <param name="format">输出格式：text(默认) 或 json</param>
    [HttpGet("{id}/fingerprint")]
    public async Task<ActionResult> GetBrowserFingerprint(int id, [FromQuery] string format = "text")
    {
        try
        {
            var browser = await _context.BrowserEnvironments.FindAsync(id);
            if (browser == null)
            {
                return NotFound(new { success = false, error = "浏览器不存在" });
            }

            // ⭐ 使用 Core 层的 BrowserFingerprintService
            if (format?.ToLower() == "json")
            {
                // 返回 JSON 格式
                var jsonText = _fingerprintService.GenerateFingerprintJson(browser);
                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        json = jsonText,
                        browser = new
                        {
                            id = browser.Id,
                            name = browser.Name
                        }
                    }
                });
            }
            else
            {
                // 返回文本格式（默认）
                var text = _fingerprintService.GenerateFingerprintText(browser);
                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        text = text,
                        browser = new
                        {
                            id = browser.Id,
                            name = browser.Name
                        }
                    }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting browser fingerprint {Id}", id);
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteBrowser(int id)
    {
        try
        {
            var browser = await _context.BrowserEnvironments.FindAsync(id);
            if (browser == null)
            {
                return NotFound(new { success = false, error = "浏览器不存在" });
            }

            // ⭐ 删除持久化的用户数据目录
            if (browser.EnablePersistence && !string.IsNullOrWhiteSpace(browser.UserDataPath))
            {
                try
                {
                    _logger.LogInformation("删除浏览器会话数据: {Path}", browser.UserDataPath);
                    _sessionService.ClearSession(id);
                    _logger.LogInformation("会话数据已删除");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "删除会话数据失败，继续删除浏览器记录");
                }
            }

            _context.BrowserEnvironments.Remove(browser);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "浏览器已删除" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting browser {Id}", id);
            return BadRequest(new { success = false, error = ex.Message });
        }
    }
}

public class CreateBrowserRequest
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
    public string? LanguagesJson { get; set; }
    public string? PluginsJson { get; set; }
    public string? SecChUa { get; set; }
    public string? SecChUaPlatform { get; set; }
    public string? SecChUaMobile { get; set; }
    public string? WebdriverMode { get; set; }
    public string? ConnectionType { get; set; }
    public int? ConnectionRtt { get; set; }
    public double? ConnectionDownlink { get; set; }
    
    // 代理配置
    public string? ProxyMode { get; set; }
    public string? ProxyApiUrl { get; set; }
}

