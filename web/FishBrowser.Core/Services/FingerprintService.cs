using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using FishBrowser.WPF.Data;
using FishBrowser.WPF.Models;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace FishBrowser.WPF.Services;

public class FingerprintService
{
    private readonly DatabaseService _dbService;
    private readonly WebScraperDbContext _dbContext;
    private readonly ILogService _log;

    public FingerprintService(DatabaseService dbService, WebScraperDbContext dbContext, ILogService log)
    {
        _dbService = dbService;
        _dbContext = dbContext;
        _log = log;
    }

    /// <summary>
    /// 生成指纹注入脚本
    /// </summary>
    public string GenerateInjectionScript(FingerprintProfile profile)
    {
        // 若已有编译脚本，优先返回简单拼接初始化脚本
        if (!string.IsNullOrWhiteSpace(profile.CompiledScriptsJson))
        {
            return $@"() => {{
    const cfg = {profile.CompiledScriptsJson};
    // 示例：根据 cfg.canvas.noiseSeed/cfg.webgl 等插入补丁
}}";
        }
        var script = $@"() => {{
    // 覆盖 webdriver 检测
    Object.defineProperty(navigator, 'webdriver', {{ get: () => undefined }});
    
    // 覆盖 plugins
    Object.defineProperty(navigator, 'plugins', {{ 
        get: () => [
            {{ name: 'Chrome PDF Plugin', description: 'Portable Document Format' }},
            {{ name: 'Chrome PDF Viewer', description: '' }}
        ] 
    }});
    
    // 覆盖 languages
    Object.defineProperty(navigator, 'languages', {{ 
        get: () => ['{profile.Locale}', '{profile.Locale.Split('-')[0]}'] 
    }});
    
    // 覆盖 userAgent
    Object.defineProperty(navigator, 'userAgent', {{ 
        get: () => '{EscapeJavaScript(profile.UserAgent)}' 
    }});
    
    // 覆盖 platform
    Object.defineProperty(navigator, 'platform', {{ 
        get: () => '{profile.Platform}' 
    }});
    
    // Canvas 指纹伪装
    const originalToDataURL = HTMLCanvasElement.prototype.toDataURL;
    HTMLCanvasElement.prototype.toDataURL = function() {{
        if (this.width === 280 && this.height === 60) {{
            return 'data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAABgAAAA8CAYAAAA/T+UzAAAA...';
        }}
        return originalToDataURL.apply(this, arguments);
    }};
    
    // WebGL 指纹伪装
    const originalGetParameter = WebGLRenderingContext.prototype.getParameter;
    WebGLRenderingContext.prototype.getParameter = function(parameter) {{
        if (parameter === 37445) {{
            return 'Intel Inc.';
        }}
        if (parameter === 37446) {{
            return 'Intel Iris OpenGL Engine';
        }}
        return originalGetParameter.apply(this, arguments);
    }};
}}";
        return script;
    }

    /// <summary>
    /// 确保 Profile 具备编译产物（Headers/Scripts/ContextOptions）
    /// </summary>
    public void EnsureCompiled(FingerprintProfile profile)
    {
        if (!string.IsNullOrWhiteSpace(profile.CompiledHeadersJson) &&
            !string.IsNullOrWhiteSpace(profile.CompiledScriptsJson) &&
            !string.IsNullOrWhiteSpace(profile.CompiledContextOptionsJson))
        {
            return;
        }

        // 构建生成器（避免依赖 DI 的解析问题）
        var generator = new FingerprintGeneratorService(_dbContext, _log, new DependencyResolutionService(_dbContext));

        // 有 MetaProfile 则基于元配置编译
        if (profile.MetaProfileId.HasValue)
        {
            var meta = _dbContext.FingerprintMetaProfiles.FirstOrDefault(m => m.Id == profile.MetaProfileId.Value);
            if (meta != null)
            {
                var traits = ParseJson<Dictionary<string, object?>>(meta.TraitsJson) ?? new();
                generator.ApplyCompiledOutputs(profile, meta, traits);
                return;
            }
        }

        // 无元配置：以现有 Profile 字段合成最小 traits 并编译
        var fallbackTraits = new Dictionary<string, object?>
        {
            ["browser.userAgent"] = profile.UserAgent,
            ["browser.acceptLanguage"] = profile.AcceptLanguage,
            ["browser.platform"] = profile.Platform,
            ["system.locale"] = profile.Locale,
            ["system.timezone"] = profile.Timezone,
            ["device.viewport.width"] = profile.ViewportWidth,
            ["device.viewport.height"] = profile.ViewportHeight,
            ["graphics.canvas.noiseSeed"] = 0
        };
        var pseudoMeta = new FingerprintMetaProfile { Id = 0, Name = "fallback" };
        generator.ApplyCompiledOutputs(profile, pseudoMeta, fallbackTraits);
    }

    private static T? ParseJson<T>(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return default;
        try { return JsonSerializer.Deserialize<T>(json!); } catch { return default; }
    }

    /// <summary>
    /// 获取预设指纹模板
    /// </summary>
    public List<FingerprintProfile> GetPresetProfiles()
    {
        return new List<FingerprintProfile>
        {
            new FingerprintProfile
            {
                Name = "Windows Desktop Chrome",
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
                ViewportWidth = 1366,
                ViewportHeight = 768,
                Locale = "zh-CN",
                Timezone = "Asia/Shanghai",
                Platform = "Win32",
                IsPreset = true
            },
            new FingerprintProfile
            {
                Name = "Mac Desktop Chrome",
                UserAgent = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
                ViewportWidth = 1440,
                ViewportHeight = 900,
                Locale = "en-US",
                Timezone = "America/New_York",
                Platform = "MacIntel",
                IsPreset = true
            },
            new FingerprintProfile
            {
                Name = "iPhone 14",
                UserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1",
                ViewportWidth = 390,
                ViewportHeight = 844,
                Locale = "zh-CN",
                Timezone = "Asia/Shanghai",
                Platform = "iPhone",
                IsPreset = true
            }
        };
    }

    private string EscapeJavaScript(string input)
    {
        return input
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("'", "\\'")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r");
    }
}
