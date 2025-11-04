using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FishBrowser.WPF.Models;
using FishBrowser.WPF.Services;
using System.Text.Json;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace FishBrowser.WPF.Engine;

public class PlaywrightController : IAsyncDisposable
{
    private readonly LogService _logService;
    private readonly FingerprintService _fingerprintService;
    private readonly SecretService _secretService;
    private IBrowser? _browser;
    private IBrowserContext? _context;
    private IPage? _page;
    
    // Automa 扩展路径
    private readonly string _automaExtensionPath = @"d:\1Dev\webscraper\automa-main\build";

    public PlaywrightController(LogService logService, FingerprintService fingerprintService, SecretService secretService)
    {
        _logService = logService;
        _fingerprintService = fingerprintService;
        _secretService = secretService;
    }

    // 清除设备度量覆盖，避免固定的 viewport 仿真
    private static async Task ClearDeviceMetricsOverrideAsync(IBrowserContext ctx)
    {
        var pages = ctx.Pages;
        if (pages.Count == 0)
        {
            var p = await ctx.NewPageAsync();
            pages = ctx.Pages; // 触发创建至少一个页面
        }
        foreach (var p in pages)
        {
            var cdp = await ctx.NewCDPSessionAsync(p);
            await cdp.SendAsync("Emulation.clearDeviceMetricsOverride");
        }
    }

    // 使用 CDP 将当前 Chromium 窗口最大化
    private static async Task MaximizeChromiumWindowAsync(IBrowserContext ctx)
    {
        var page = ctx.Pages.FirstOrDefault() ?? await ctx.NewPageAsync();
        var cdp = await ctx.NewCDPSessionAsync(page);
        var res = await cdp.SendAsync("Browser.getWindowForTarget");

        if (res is JsonElement elem && elem.TryGetProperty("windowId", out var idEl))
        {
            int windowId = idEl.GetInt32();
            var boundsArgs = new Dictionary<string, object>
            {
                ["windowId"] = windowId,
                ["bounds"] = new Dictionary<string, object>
                {
                    ["windowState"] = "maximized"
                }
            };
            await cdp.SendAsync("Browser.setWindowBounds", boundsArgs);
        }
    }

    /// <summary>
    /// 从系统获取 DPI 缩放比例（使用多种方法）
    /// </summary>
    private static float GetSystemDpiScale()
    {
        try
        {
            // 方法 1: 使用 Windows API GetDpiForSystem
            float dpiScale = GetDpiScaleFromApi();
            if (dpiScale > 0)
                return dpiScale;

            // 方法 2: 从注册表读取 LogPixels
            dpiScale = GetDpiScaleFromRegistry();
            if (dpiScale > 0)
                return dpiScale;
        }
        catch (Exception ex)
        {
            // 如果所有方法都失败，使用默认值
        }

        return 1.0f; // 默认 100%
    }

    /// <summary>
    /// 使用 Windows API 获取 DPI 缩放
    /// </summary>
    private static float GetDpiScaleFromApi()
    {
        try
        {
            // GetDpiForSystem 返回当前系统 DPI (96 = 100%)
            int systemDpi = GetDpiForSystem();
            if (systemDpi > 0)
            {
                return systemDpi / 96f;
            }
        }
        catch { }
        return 0;
    }

    /// <summary>
    /// 从注册表获取 DPI 缩放
    /// </summary>
    private static float GetDpiScaleFromRegistry()
    {
        try
        {
            // 尝试多个可能的注册表位置
            string[] keyPaths = new[]
            {
                @"Control Panel\Desktop",
                @"Software\Microsoft\Windows NT\CurrentVersion\FontDPI",
            };

            foreach (var keyPath in keyPaths)
            {
                using var key = Registry.CurrentUser.OpenSubKey(keyPath);
                if (key != null)
                {
                    // 尝试 LogPixels
                    object? logPixelsObj = key.GetValue("LogPixels");
                    if (logPixelsObj != null && int.TryParse(logPixelsObj.ToString(), out int dpi) && dpi > 0)
                    {
                        return dpi / 96f;
                    }
                }
            }

            // 尝试本地机器注册表
            using var localKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Hardware Profiles\Current\Software\Fonts");
            if (localKey != null)
            {
                object? logPixelsObj = localKey.GetValue("LogPixels");
                if (logPixelsObj != null && int.TryParse(logPixelsObj.ToString(), out int dpi) && dpi > 0)
                {
                    return dpi / 96f;
                }
            }
        }
        catch { }
        return 0;
    }

    [DllImport("user32.dll")]
    private static extern int GetDpiForSystem();

    private static bool IsMobileUA(string? ua)
    {
        if (string.IsNullOrWhiteSpace(ua)) return false;
        ua = ua.ToLowerInvariant();
        // 简单判断：包含 Mobile/Android/iPhone/iPad 等关键词
        return ua.Contains("mobile") || ua.Contains("android") || ua.Contains("iphone") || ua.Contains("ipad");
    }

    /// <summary>
    /// 初始化浏览器与上下文
    /// </summary>
    /// <param name="fingerprint">指纹配置</param>
    /// <param name="proxy">代理服务器</param>
    /// <param name="headless">是否无头模式</param>
    /// <param name="userDataPath">用户数据目录（用于会话持久化）</param>
    /// <param name="loadAutoma">是否加载 Automa 自动化扩展</param>
    public async Task InitializeBrowserAsync(FingerprintProfile fingerprint, ProxyServer? proxy = null, bool headless = true, string? userDataPath = null, bool loadAutoma = false, BrowserEnvironment? environment = null, string browserType = "chromium")
    {
        try
        {
            var playwright = await Playwright.CreateAsync();
            
            // 如果指定了 userDataPath，使用持久化上下文（LaunchPersistentContextAsync）
            if (!string.IsNullOrWhiteSpace(userDataPath))
            {
                await InitializePersistentContextAsync(playwright, fingerprint, proxy, headless, userDataPath, loadAutoma, environment, browserType);
                return;
            }
            
            // 构建启动参数
            var args = new List<string> { "--disable-blink-features=AutomationControlled" };
            
            // 获取系统 DPI 缩放比例
            float dpiScale = GetSystemDpiScale();
            _logService.LogInfo("PlaywrightController", $"System DPI scale detected: {dpiScale * 100}%");
            
            // 不强制 DPI 设置，让浏览器使用系统默认（像 MVP 一样）
            _logService.LogInfo("PlaywrightController", $"System DPI detected: {dpiScale * 100}%, but not forcing (using browser default)");
            
            // 如果需要加载 Automa 扩展
            if (loadAutoma && System.IO.Directory.Exists(_automaExtensionPath))
            {
                if (headless)
                {
                    _logService.LogWarn("PlaywrightController", "Cannot load extensions in headless mode, switching to headed mode");
                    headless = false;
                }
                
                args.Add($"--disable-extensions-except={_automaExtensionPath}");
                args.Add($"--load-extension={_automaExtensionPath}");
                _logService.LogInfo("PlaywrightController", $"Automa extension will be loaded from: {_automaExtensionPath}");
            }
            else if (loadAutoma)
            {
                _logService.LogWarn("PlaywrightController", $"Automa extension path not found: {_automaExtensionPath}");
            }
            
            // 根据浏览器类型启动对应的浏览器
            _logService.LogInfo("PlaywrightController", $"Launching {browserType} browser");
            
            _browser = browserType.ToLowerInvariant() switch
            {
                "firefox" => await playwright.Firefox.LaunchAsync(new BrowserTypeLaunchOptions
                {
                    Headless = headless,
                    Args = args.ToArray()
                }),
                "chromium" or _ => await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
                {
                    Headless = headless,
                    Args = args.ToArray()
                })
            };

            // 确保已生成编译产物
            _fingerprintService.EnsureCompiled(fingerprint);

            var contextOptions = new BrowserNewContextOptions();

            // 应用编译的 ContextOptions（如果存在）
            if (!string.IsNullOrWhiteSpace(fingerprint.CompiledContextOptionsJson))
            {
                try
                {
                    using var doc = JsonDocument.Parse(fingerprint.CompiledContextOptionsJson);
                    if (doc.RootElement.TryGetProperty("userAgent", out var ua) && ua.ValueKind == JsonValueKind.String)
                        contextOptions.UserAgent = ua.GetString();
                    if (doc.RootElement.TryGetProperty("locale", out var loc) && loc.ValueKind == JsonValueKind.String)
                        contextOptions.Locale = loc.GetString();
                    if (doc.RootElement.TryGetProperty("timezoneId", out var tz) && tz.ValueKind == JsonValueKind.String)
                        contextOptions.TimezoneId = tz.GetString();
                    if (doc.RootElement.TryGetProperty("viewport", out var vp) && vp.ValueKind == JsonValueKind.Object)
                    {
                        int w = vp.TryGetProperty("width", out var vw) && vw.TryGetInt32(out var wv) ? wv : fingerprint.ViewportWidth;
                        int h = vp.TryGetProperty("height", out var vh) && vh.TryGetInt32(out var hv) ? hv : fingerprint.ViewportHeight;
                        contextOptions.ViewportSize = new ViewportSize { Width = w, Height = h };
                    }
                }
                catch { /* ignore parse errors, fallback below */ }
            }

            // 回退：若未由编译产物赋值，则使用 Profile 字段
            contextOptions.UserAgent ??= fingerprint.UserAgent;
            contextOptions.Locale ??= fingerprint.Locale;
            contextOptions.TimezoneId ??= fingerprint.Timezone;
            
            // 不设置固定视口，让视口跟随窗口大小（像 MVP 一样）
            contextOptions.ViewportSize = null;

            // 移动端仿真：根据 UA 推断（Android/iOS）
            if (IsMobileUA(contextOptions.UserAgent))
            {
                contextOptions.IsMobile ??= true;
                contextOptions.HasTouch ??= true;
                contextOptions.DeviceScaleFactor ??= 3;
            }
            else
            {
                // 非移动端：不要设置 DeviceScaleFactor，避免开启视口仿真导致尺寸固定
                // 依赖 Chromium 启动参数 --force-device-scale-factor 实现 DPR
            }

            // 组装请求头：编译产物优先
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(fingerprint.CompiledHeadersJson))
            {
                try
                {
                    var list = System.Text.Json.JsonSerializer.Deserialize<List<KeyValuePair<string, string>>>(fingerprint.CompiledHeadersJson);
                    if (list != null)
                    {
                        foreach (var kv in list)
                        {
                            if (!string.IsNullOrWhiteSpace(kv.Key)) headers[kv.Key] = kv.Value;
                        }
                    }
                }
                catch { /* ignore and fallback */ }
            }
            if (headers.Count == 0)
            {
                headers["Accept-Language"] = fingerprint.AcceptLanguage;
            }
            contextOptions.ExtraHTTPHeaders = headers;

            // 添加代理配置
            if (proxy != null)
            {
                contextOptions.Proxy = new Proxy
                {
                    Server = $"{proxy.Protocol}://{proxy.Address}:{proxy.Port}",
                    Username = proxy.Username,
                    Password = _secretService.Decrypt(proxy.PasswordEncrypted)
                };
            }

            _context = await _browser.NewContextAsync(contextOptions);

            // 注入指纹脚本
            var injectionScript = new FingerprintManager().GenerateInjectionScript(fingerprint);
            // 若 FingerprintService 产生了编译脚本，GenerateInjectionScript 会返回更轻量的包装
            await _context.AddInitScriptAsync(injectionScript);

            _logService.LogInfo("PlaywrightController", $"Browser initialized with fingerprint: {fingerprint.Name} (mode: normal)");
        }
        catch (Exception ex)
        {
            _logService.LogError("PlaywrightController", $"Failed to initialize browser: {ex.Message}", ex.StackTrace);
            throw;
        }
    }

    /// <summary>
    /// 初始化持久化上下文（会话保存）
    /// </summary>
    private async Task InitializePersistentContextAsync(IPlaywright playwright, FingerprintProfile fingerprint, ProxyServer? proxy, bool headless, string userDataPath, bool loadAutoma = false, BrowserEnvironment? environment = null, string browserType = "chromium")
    {
        _logService.LogInfo("PlaywrightController", $"Starting persistent context initialization, userDataPath: {userDataPath}");
        
        // ⭐ 调试日志：检查传入的 FingerprintProfile
        var uaPreview = fingerprint.UserAgent != null && fingerprint.UserAgent.Length > 50 
            ? fingerprint.UserAgent.Substring(0, 50) + "..." 
            : fingerprint.UserAgent ?? "(null)";
        _logService.LogInfo("PlaywrightController", $"[BEFORE COMPILE] FingerprintProfile: Platform={fingerprint.Platform}, UserAgent={uaPreview}");
        
        _fingerprintService.EnsureCompiled(fingerprint);
        
        // ⭐ 调试日志：检查编译后的 FingerprintProfile
        var uaPreview2 = fingerprint.UserAgent != null && fingerprint.UserAgent.Length > 50 
            ? fingerprint.UserAgent.Substring(0, 50) + "..." 
            : fingerprint.UserAgent ?? "(null)";
        _logService.LogInfo("PlaywrightController", $"[AFTER COMPILE] FingerprintProfile: Platform={fingerprint.Platform}, UserAgent={uaPreview2}");

        // 构建启动参数
        var args = new List<string> { "--disable-blink-features=AutomationControlled" };
        
        // 获取系统 DPI 缩放比例
        float dpiScale = GetSystemDpiScale();
        _logService.LogInfo("PlaywrightController", $"System DPI scale detected: {dpiScale * 100}%");
        
        // 不强制 DPI 设置，让浏览器使用系统默认（像 MVP 一样）
        _logService.LogInfo("PlaywrightController", $"System DPI detected: {dpiScale * 100}%, but not forcing (using browser default)");

        // 如果需要加载 Automa 扩展
        if (loadAutoma && System.IO.Directory.Exists(_automaExtensionPath))
        {
            if (headless)
            {
                _logService.LogWarn("PlaywrightController", "Cannot load extensions in headless mode, switching to headed mode");
                headless = false;
            }
            
            args.Add($"--disable-extensions-except={_automaExtensionPath}");
            args.Add($"--load-extension={_automaExtensionPath}");
            _logService.LogInfo("PlaywrightController", $"Automa extension will be loaded from: {_automaExtensionPath}");
        }
        else if (loadAutoma)
        {
            _logService.LogWarn("PlaywrightController", $"Automa extension path not found: {_automaExtensionPath}");
        }

        // 不设置固定视口，让视口跟随窗口大小（像 MVP 一样）
        _logService.LogInfo("PlaywrightController", "ViewportSize set to null - viewport will follow window size");

        var launchOptions = new BrowserTypeLaunchPersistentContextOptions
        {
            Headless = headless,
            // 根据浏览器类型设置 Channel
            // Firefox 不支持 Channel，Chromium 可以使用 "chrome" 获得更真实的 TLS 指纹
            Channel = browserType.ToLowerInvariant() == "firefox" ? null : "chrome",
            Args = args.ToArray(),
            UserAgent = fingerprint.UserAgent,
            Locale = fingerprint.Locale,
            TimezoneId = fingerprint.Timezone,
            ViewportSize = null  // 让视口跟随窗口大小，像 MVP 一样
        };

        // 应用编译的 ContextOptions
        if (!string.IsNullOrWhiteSpace(fingerprint.CompiledContextOptionsJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(fingerprint.CompiledContextOptionsJson);
                if (doc.RootElement.TryGetProperty("userAgent", out var ua) && ua.ValueKind == JsonValueKind.String)
                    launchOptions.UserAgent = ua.GetString();
                if (doc.RootElement.TryGetProperty("locale", out var loc) && loc.ValueKind == JsonValueKind.String)
                    launchOptions.Locale = loc.GetString();
                if (doc.RootElement.TryGetProperty("timezoneId", out var tz) && tz.ValueKind == JsonValueKind.String)
                    launchOptions.TimezoneId = tz.GetString();
                // 忽略编译产物中的 viewport，保持 ViewportSize=null 以便随窗口自适应
                if (doc.RootElement.TryGetProperty("viewport", out var _))
                {
                    _logService.LogInfo("PlaywrightController", "Ignoring compiled viewport to allow window-resizable viewport (ViewportSize=null)");
                }
            }
            catch { /* ignore parse errors */ }
        }

        // Headers
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(fingerprint.CompiledHeadersJson))
        {
            try
            {
                var list = JsonSerializer.Deserialize<List<KeyValuePair<string, string>>>(fingerprint.CompiledHeadersJson);
                if (list != null)
                {
                    foreach (var kv in list)
                    {
                        if (!string.IsNullOrWhiteSpace(kv.Key)) headers[kv.Key] = kv.Value;
                    }
                }
            }
            catch { }
        }
        if (headers.Count == 0)
        {
            headers["Accept-Language"] = fingerprint.AcceptLanguage;
        }
        
        // 添加 Client Hints headers（Cloudflare 会检查）- 从指纹配置读取
        if (!headers.ContainsKey("sec-ch-ua"))
        {
            // 优先使用指纹配置中的 Client Hints，否则自动生成
            if (!string.IsNullOrEmpty(fingerprint.SecChUa))
            {
                headers["sec-ch-ua"] = fingerprint.SecChUa;
                headers["sec-ch-ua-mobile"] = fingerprint.SecChUaMobile ?? "?0";
                headers["sec-ch-ua-platform"] = fingerprint.SecChUaPlatform ?? "\"Windows\"";
                _logService.LogInfo("PlaywrightController", "Using Client Hints from fingerprint profile");
            }
            else
            {
                // 回退：从 UA 提取 Chrome 版本
                var uaMatch = System.Text.RegularExpressions.Regex.Match(fingerprint.UserAgent ?? "", @"Chrome/(\d+)");
                var chromeVersion = uaMatch.Success ? uaMatch.Groups[1].Value : "120";
                headers["sec-ch-ua"] = $"\"Chromium\";v=\"{chromeVersion}\", \"Google Chrome\";v=\"{chromeVersion}\", \"Not-A.Brand\";v=\"99\"";
                headers["sec-ch-ua-mobile"] = "?0";
                headers["sec-ch-ua-platform"] = "\"Windows\"";
                _logService.LogWarn("PlaywrightController", "Client Hints not in fingerprint, using fallback");
            }
        }
        
        launchOptions.ExtraHTTPHeaders = headers;

        // Proxy
        if (proxy != null)
        {
            launchOptions.Proxy = new Proxy
            {
                Server = $"{proxy.Protocol}://{proxy.Address}:{proxy.Port}",
                Username = proxy.Username,
                Password = _secretService.Decrypt(proxy.PasswordEncrypted)
            };
        }

        // 启动持久化上下文
        _logService.LogInfo("PlaywrightController", $"Launching persistent context at: {userDataPath}");
        // 移动端仿真：根据 UA 推断（Android/iOS）
        if (IsMobileUA(launchOptions.UserAgent))
        {
            launchOptions.IsMobile ??= true;
            launchOptions.HasTouch ??= true;
            launchOptions.DeviceScaleFactor = 3; // 移动端强制使用 3.0
        }
        // 注意：非移动端已在第 259 行设置了系统 DPI 缩放

        // 根据浏览器类型启动对应的浏览器
        _logService.LogInfo("PlaywrightController", $"Launching {browserType} persistent context");
        
        _context = browserType.ToLowerInvariant() switch
        {
            "firefox" => await playwright.Firefox.LaunchPersistentContextAsync(userDataPath, launchOptions),
            "chromium" or _ => await playwright.Chromium.LaunchPersistentContextAsync(userDataPath, launchOptions)
        };
        
        _logService.LogInfo("PlaywrightController", $"Persistent context launched successfully, pages count: {_context.Pages.Count}");

        // 清除可能从用户数据目录还原的设备度量覆盖，确保视口不被固定
        try
        {
            await ClearDeviceMetricsOverrideAsync(_context);
            _logService.LogInfo("PlaywrightController", "Cleared device metrics override via CDP");
        }
        catch (Exception ex)
        {
            _logService.LogWarn("PlaywrightController", $"Failed to clear device metrics override: {ex.Message}");
        }

        // 通过 CDP 强制最大化窗口，避免用户数据目录恢复旧窗口尺寸
        try
        {
            await MaximizeChromiumWindowAsync(_context);
            _logService.LogInfo("PlaywrightController", "Window maximized via CDP (Browser.setWindowBounds)");
        }
        catch (Exception ex)
        {
            _logService.LogWarn("PlaywrightController", $"Failed to maximize window via CDP: {ex.Message}");
        }

        // 注入指纹脚本
        var injectionScript = new FingerprintManager().GenerateInjectionScript(fingerprint);
        await _context.AddInitScriptAsync(injectionScript);
        _logService.LogInfo("PlaywrightController", "Fingerprint injection script added");

        // 注入增强防检测脚本（Cloudflare 绕过）- 从指纹配置读取数据
        var antiDetectScript = GenerateAntiDetectScript(fingerprint);
        await _context.AddInitScriptAsync(antiDetectScript);
        _logService.LogInfo("PlaywrightController", $"Anti-detection script added (Cloudflare bypass, data-driven): Plugins={fingerprint.PluginsJson?.Length ?? 0} chars, Languages={fingerprint.LanguagesJson?.Length ?? 0} chars, HW={fingerprint.HardwareConcurrency}, Mem={fingerprint.DeviceMemory}, Touch={fingerprint.MaxTouchPoints}, Conn={fingerprint.ConnectionType}");

        // 注入滚动条修复脚本：确保在窗口变小/内容溢出时显示滚动条
        const string scrollbarInit = @"(() => { try {
            const apply = () => {
                const de = document.documentElement; const b = document.body;
                if (de) de.style.overflow = 'auto';
                if (b) b.style.overflow = 'auto';
            };
            apply();
            document.addEventListener('DOMContentLoaded', apply, { once: true });
        } catch (_) {} })();";
        await _context.AddInitScriptAsync(scrollbarInit);
        _logService.LogInfo("PlaywrightController", "Scrollbar init script added (forces overflow:auto)");

        // 立即对现有页面应用一次，以覆盖站点可能设置的 overflow:hidden
        try
        {
            foreach (var p in _context.Pages)
            {
                await p.EvaluateAsync("document.documentElement.style.overflow='auto'; document.body && (document.body.style.overflow='auto');");
            }
        }
        catch { }

        // 完整的浏览器配置日志（用于 Cloudflare 调试）
        _logService.LogInfo("PlaywrightController", "========== Browser Configuration Summary ==========");
        _logService.LogInfo("PlaywrightController", $"Fingerprint: {fingerprint.Name}");
        _logService.LogInfo("PlaywrightController", $"UserAgent: {fingerprint.UserAgent}");
        _logService.LogInfo("PlaywrightController", $"Platform: {fingerprint.Platform}");
        _logService.LogInfo("PlaywrightController", $"Locale: {fingerprint.Locale}");
        _logService.LogInfo("PlaywrightController", $"AcceptLanguage: {fingerprint.AcceptLanguage}");
        _logService.LogInfo("PlaywrightController", $"Timezone: {fingerprint.Timezone}");
        _logService.LogInfo("PlaywrightController", $"--- Anti-Detection Data ---");
        
        // 检查是否缺少防检测数据
        bool missingAntiDetection = string.IsNullOrEmpty(fingerprint.PluginsJson) || 
                                      string.IsNullOrEmpty(fingerprint.LanguagesJson) || 
                                      string.IsNullOrEmpty(fingerprint.SecChUa);
        if (missingAntiDetection)
        {
            _logService.LogWarn("PlaywrightController", "⚠️ WARNING: Anti-detection data is missing! This profile was created before anti-detection feature was added.");
            _logService.LogWarn("PlaywrightController", "⚠️ Cloudflare bypass may fail. Please create a NEW profile using '一键随机' to get anti-detection data.");
        }
        
        _logService.LogInfo("PlaywrightController", $"PluginsJson: {(string.IsNullOrEmpty(fingerprint.PluginsJson) ? "❌ NOT SET" : $"✅ {fingerprint.PluginsJson.Length} chars")}");
        _logService.LogInfo("PlaywrightController", $"LanguagesJson: {(string.IsNullOrEmpty(fingerprint.LanguagesJson) ? "❌ NOT SET" : $"✅ {fingerprint.LanguagesJson}")}");
        _logService.LogInfo("PlaywrightController", $"HardwareConcurrency: {fingerprint.HardwareConcurrency}");
        _logService.LogInfo("PlaywrightController", $"DeviceMemory: {fingerprint.DeviceMemory}");
        _logService.LogInfo("PlaywrightController", $"MaxTouchPoints: {fingerprint.MaxTouchPoints}");
        _logService.LogInfo("PlaywrightController", $"ConnectionType: {fingerprint.ConnectionType ?? "❌ NOT SET"}");
        _logService.LogInfo("PlaywrightController", $"ConnectionRtt: {fingerprint.ConnectionRtt}");
        _logService.LogInfo("PlaywrightController", $"ConnectionDownlink: {fingerprint.ConnectionDownlink}");
        _logService.LogInfo("PlaywrightController", $"--- Client Hints ---");
        _logService.LogInfo("PlaywrightController", $"SecChUa: {(string.IsNullOrEmpty(fingerprint.SecChUa) ? "❌ NOT SET (using fallback)" : $"✅ {fingerprint.SecChUa}")}");
        _logService.LogInfo("PlaywrightController", $"SecChUaPlatform: {(string.IsNullOrEmpty(fingerprint.SecChUaPlatform) ? "❌ NOT SET" : $"✅ {fingerprint.SecChUaPlatform}")}");
        _logService.LogInfo("PlaywrightController", $"SecChUaMobile: {(string.IsNullOrEmpty(fingerprint.SecChUaMobile) ? "❌ NOT SET" : $"✅ {fingerprint.SecChUaMobile}")}");
        _logService.LogInfo("PlaywrightController", $"--- WebGL ---");
        _logService.LogInfo("PlaywrightController", $"WebGLVendor: {fingerprint.WebGLVendor}");
        _logService.LogInfo("PlaywrightController", $"WebGLRenderer: {fingerprint.WebGLRenderer}");
        _logService.LogInfo("PlaywrightController", $"===================================================");
        _logService.LogInfo("PlaywrightController", $"Browser initialized (mode: persistent, path: {userDataPath})");
    }

    /// <summary>
    /// 导航到 URL
    /// </summary>
    public async Task<string> NavigateAsync(string url, int timeoutMs = 45000)  // 增大默认超时，给 Cloudflare 验证留时间
    {
        try
        {
            if (_context == null)
                throw new InvalidOperationException("Browser context not initialized");

            _page = await _context.NewPageAsync();
            _logService.LogInfo("PlaywrightController", $"Navigating to {url} with timeout {timeoutMs}ms (extended for Cloudflare)");
            await _page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = timeoutMs });
            
            // 等待可能的 Cloudflare 挑战完成
            await Task.Delay(2000);  // 给 JS challenge 额外 2 秒

            _logService.LogInfo("PlaywrightController", $"Navigated to: {url}");
            return await _page.ContentAsync();
        }
        catch (Exception ex)
        {
            _logService.LogError("PlaywrightController", $"Failed to navigate to {url}: {ex.Message}", ex.StackTrace);
            throw;
        }
    }

    /// <summary>
    /// 获取页面内容
    /// </summary>
    public async Task<string> GetPageContentAsync()
    {
        if (_page == null)
            throw new InvalidOperationException("Page not loaded");

        return await _page.ContentAsync();
    }

    /// <summary>
    /// 执行 JavaScript
    /// </summary>
    public async Task<object?> EvaluateAsync(string script)
    {
        if (_page == null)
            throw new InvalidOperationException("Page not loaded");

        return await _page.EvaluateAsync(script);
    }

    /// <summary>
    /// 截图
    /// </summary>
    public async Task<byte[]> ScreenshotAsync(string filePath)
    {
        if (_page == null)
            throw new InvalidOperationException("Page not loaded");

        return await _page.ScreenshotAsync(new PageScreenshotOptions { Path = filePath });
    }

    // ========== Wrappers for DslExecutor compatibility ==========
    public async Task WaitForSelectorAsync(string selector, int timeoutMs = 30000)
    {
        if (_page == null)
            throw new InvalidOperationException("Page not loaded");

        await _page.WaitForSelectorAsync(selector, new PageWaitForSelectorOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = timeoutMs
        });
    }

    public async Task ClickAsync(string selector, int timeoutMs = 30000)
    {
        if (_page == null)
            throw new InvalidOperationException("Page not loaded");

        await _page.ClickAsync(selector, new PageClickOptions
        {
            Timeout = timeoutMs
        });
    }

    public async Task FillAsync(string selector, string value, int timeoutMs = 30000)
    {
        if (_page == null)
            throw new InvalidOperationException("Page not loaded");

        await _page.FillAsync(selector, value, new PageFillOptions
        {
            Timeout = timeoutMs
        });
    }

    public async Task WaitForLoadStateAsync(string state = "networkidle", int timeoutMs = 30000)
    {
        if (_page == null)
            throw new InvalidOperationException("Page not loaded");

        var s = (state ?? "").ToLowerInvariant();
        var ls = s switch
        {
            "load" => LoadState.Load,
            "domcontentloaded" => LoadState.DOMContentLoaded,
            _ => LoadState.NetworkIdle
        };

        await _page.WaitForLoadStateAsync(ls, new PageWaitForLoadStateOptions
        {
            Timeout = timeoutMs
        });
    }

    /// <summary>
    /// 等待浏览器关闭（用于持久化会话保存）
    /// </summary>
    public async Task WaitForCloseAsync()
    {
        if (_context != null)
        {
            _logService.LogInfo("PlaywrightController", "Starting to wait for browser context close...");
            
            // 等待 context 关闭（用户关闭所有浏览器窗口时触发）
            // 使用TaskCompletionSource来等待context关闭事件
            var tcs = new TaskCompletionSource<bool>();
            
            // 监听context的关闭事件
            _context.Close += (sender, e) =>
            {
                _logService.LogInfo("PlaywrightController", "Context Close event triggered!");
                tcs.SetResult(true);
            };
            
            _logService.LogInfo("PlaywrightController", "Close event handler registered, waiting for browser to close...");
            
            // 等待关闭事件
            await tcs.Task;
            
            _logService.LogInfo("PlaywrightController", "Browser context closed, session data should be saved now");
            
            // 给一点时间让数据完全写入磁盘
            await Task.Delay(1000);
            _logService.LogInfo("PlaywrightController", "Session data save completed");
        }
        else
        {
            _logService.LogWarn("PlaywrightController", "WaitForCloseAsync called but context is null");
        }
    }

    /// <summary>
    /// 获取当前页面
    /// </summary>
    public IPage? GetPage()
    {
        return _page;
    }

    /// <summary>
    /// 生成防检测注入脚本（从指纹配置读取数据）
    /// </summary>
    private string GenerateAntiDetectScript(FingerprintProfile fingerprint)
    {
        // 从指纹读取数据
        var plugins = fingerprint.PluginsJson ?? "[]";
        var languages = fingerprint.LanguagesJson ?? "[\"zh-CN\", \"zh\", \"en-US\", \"en\"]";
        var hardwareConcurrency = fingerprint.HardwareConcurrency;
        var deviceMemory = fingerprint.DeviceMemory;
        var maxTouchPoints = fingerprint.MaxTouchPoints;
        var connectionType = fingerprint.ConnectionType ?? "4g";
        var connectionRtt = fingerprint.ConnectionRtt;
        var connectionDownlink = fingerprint.ConnectionDownlink;
        
        // 读取 platform 和 userAgent（转义单引号以避免 JS 字符串错误）
        var platform = (fingerprint.Platform ?? "Win32").Replace("'", "\\'");
        var userAgent = (fingerprint.UserAgent ?? "").Replace("'", "\\'");
        
        // ⭐ 调试日志：检查注入脚本中使用的值
        _logService.LogInfo("PlaywrightController", $"[INJECT SCRIPT] Platform={platform}, UserAgent={userAgent.Substring(0, Math.Min(50, userAgent.Length))}..., MaxTouchPoints={maxTouchPoints}, Plugins={plugins.Length} chars");

        return $@"(() => {{
            try {{
                // 1. 隐藏 webdriver 标识（必须返回 false，不是 undefined）
                Object.defineProperty(navigator, 'webdriver', {{ get: () => false }});
                
                // 2. 伪装 platform（从指纹配置读取）
                Object.defineProperty(navigator, 'platform', {{
                    get: () => '{platform}',
                    configurable: true
                }});
                
                // 3. 伪装 userAgent（从指纹配置读取）
                Object.defineProperty(navigator, 'userAgent', {{
                    get: () => '{userAgent}',
                    configurable: true
                }});
                
                // 4. 伪装 appVersion（必须与 userAgent 一致）
                Object.defineProperty(navigator, 'appVersion', {{
                    get: () => '{userAgent}'.replace('Mozilla/', ''),
                    configurable: true
                }});
                
                // 5. 伪装 plugins（从指纹配置读取）
                Object.defineProperty(navigator, 'plugins', {{
                    get: () => {plugins}
                }});
                
                // 6. 伪装 languages（从指纹配置读取）
                Object.defineProperty(navigator, 'languages', {{
                    get: () => {languages}
                }});
                
                // 7. 伪装 permissions
                const originalQuery = window.navigator.permissions.query;
                window.navigator.permissions.query = (parameters) => (
                    parameters.name === 'notifications' ?
                        Promise.resolve({{ state: Notification.permission }}) :
                        originalQuery(parameters)
                );
                
                // 8. 伪装 chrome 对象（非 headless）
                if (!window.chrome) {{
                    window.chrome = {{ runtime: {{}} }};
                }}
                
                // 9. 伪装 maxTouchPoints（从指纹配置读取）
                Object.defineProperty(navigator, 'maxTouchPoints', {{ get: () => {maxTouchPoints} }});
                
                // 10. 伪装 hardwareConcurrency（从指纹配置读取）
                Object.defineProperty(navigator, 'hardwareConcurrency', {{ get: () => {hardwareConcurrency} }});
                
                // 11. 伪装 deviceMemory（从指纹配置读取）
                Object.defineProperty(navigator, 'deviceMemory', {{ get: () => {deviceMemory} }});
                
                // 12. 伪装 connection（从指纹配置读取）
                Object.defineProperty(navigator, 'connection', {{
                    get: () => ({{ effectiveType: '{connectionType}', rtt: {connectionRtt}, downlink: {connectionDownlink}, saveData: false }})
                }});
                
            }} catch (e) {{ console.error('Anti-detect script error:', e); }}
        }})();";
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_page != null)
            await _page.CloseAsync();

        if (_context != null)
            await _context.CloseAsync();

        if (_browser != null)
            await _browser.CloseAsync();

        _logService.LogInfo("PlaywrightController", "Browser resources released");
    }
}
