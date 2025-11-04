using FishBrowser.WPF.Models;
using FishBrowser.WPF.Services;

namespace FishBrowser.WPF.Services;

/// <summary>
/// 浏览器随机生成服务 - 复用 WPF 应用中的完整随机逻辑
/// </summary>
public class BrowserRandomGenerator
{
    private readonly ChromeVersionDatabase _chromeVersionDb;
    private readonly GpuCatalogService _gpuCatalog;
    private readonly FontService _fontSvc;
    private readonly AntiDetectionService _antiDetectSvc;
    private readonly ILogService _log;
    private readonly Random _random = new Random();

    public BrowserRandomGenerator(
        ChromeVersionDatabase chromeVersionDb,
        GpuCatalogService gpuCatalog,
        FontService fontSvc,
        AntiDetectionService antiDetectSvc,
        ILogService log)
    {
        _chromeVersionDb = chromeVersionDb;
        _gpuCatalog = gpuCatalog;
        _fontSvc = fontSvc;
        _antiDetectSvc = antiDetectSvc;
        _log = log;
    }

    /// <summary>
    /// 生成完全随机的浏览器环境（复用 WPF 的 GenerateFullyRandomFingerprint 逻辑）
    /// </summary>
    public async Task<(BrowserEnvironment browser, FingerprintProfile profile)> GenerateRandomBrowserAsync()
    {
        try
        {
            // 随机选择操作系统（包括移动端）
            var osList = new[] { "Windows", "MacOS", "Linux", "Android", "iOS" };
            var selectedOS = osList[_random.Next(osList.Length)];

            // 根据操作系统随机选择合适的分辨率
            string[] resolutions;
            if (selectedOS == "Android")
            {
                resolutions = new[] { "360x800", "412x915", "1080x2400" };
            }
            else if (selectedOS == "iOS")
            {
                resolutions = new[] { "390x844", "393x852", "428x926", "820x1180", "1024x1366" };
            }
            else
            {
                resolutions = new[] { "1920x1080", "1366x768", "1280x720", "2560x1440", "3840x2160" };
            }
            var selectedRes = resolutions[_random.Next(resolutions.Length)];
            var resParts = selectedRes.Split('x');
            var width = int.Parse(resParts[0]);
            var height = int.Parse(resParts[1]);

            // 随机选择语言
            var locales = new[] { "zh-CN", "en-US", "ja-JP", "ko-KR" };
            var selectedLocale = locales[_random.Next(locales.Length)];

            // 生成 Timezone
            var timezone = selectedLocale switch
            {
                "zh-CN" => "Asia/Shanghai",
                "en-US" => "America/New_York",
                "ja-JP" => "Asia/Tokyo",
                "ko-KR" => "Asia/Seoul",
                _ => "Asia/Shanghai"
            };

            // 使用 ChromeVersionDatabase 获取真实版本号
            var chromeVersion = _chromeVersionDb.GetRandomVersion(selectedOS);
            if (chromeVersion == null)
            {
                _log.LogWarn("BrowserRandomGenerator", $"No Chrome version found for OS: {selectedOS}, using Windows");
                chromeVersion = _chromeVersionDb.GetRandomVersion("Windows");
            }

            if (chromeVersion == null)
            {
                throw new InvalidOperationException("No Chrome versions available");
            }

            // 生成 User-Agent
            var userAgent = selectedOS switch
            {
                "Windows" => $"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/{chromeVersion.Version} Safari/537.36",
                "MacOS" => $"Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/{chromeVersion.Version} Safari/537.36",
                "Linux" => $"Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/{chromeVersion.Version} Safari/537.36",
                "Android" => $"Mozilla/5.0 (Linux; Android 13; SM-S901B) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/{chromeVersion.Version} Mobile Safari/537.36",
                "iOS" => "Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1",
                _ => $"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/{chromeVersion.Version} Safari/537.36"
            };

            // 生成 Platform
            var platform = selectedOS switch
            {
                "Windows" => "Win32",
                "MacOS" => "MacIntel",
                "Linux" => "Linux x86_64",
                "Android" => "Linux armv8l",
                "iOS" => "iPhone",
                _ => "Win32"
            };

            // 生成硬件配置（根据 OS 使用真实配置）
            int hardwareConcurrency, deviceMemory, maxTouchPoints;
            
            if (selectedOS == "iOS")
            {
                // iOS 真实硬件配置
                var iosCores = new[] { 4, 6 };  // iPhone: A15/A16 是 6 核，A14 是 4 核
                var iosMemory = new[] { 4, 6, 8 };  // iPhone: 4GB (标准), 6GB (Pro), 8GB (Pro Max)
                hardwareConcurrency = iosCores[_random.Next(iosCores.Length)];
                deviceMemory = iosMemory[_random.Next(iosMemory.Length)];
                maxTouchPoints = 5;  // iPhone 固定 5 个触摸点
            }
            else if (selectedOS == "Android")
            {
                // Android 配置
                var androidCores = new[] { 4, 6, 8 };
                var androidMemory = new[] { 4, 6, 8, 12 };
                hardwareConcurrency = androidCores[_random.Next(androidCores.Length)];
                deviceMemory = androidMemory[_random.Next(androidMemory.Length)];
                maxTouchPoints = _random.Next(5, 11);
            }
            else
            {
                // 桌面操作系统（Windows/Mac/Linux）
                var commonCores = new[] { 4, 6, 8, 12, 16 };
                var commonMemory = new[] { 8, 16, 32 };
                hardwareConcurrency = commonCores[_random.Next(commonCores.Length)];
                deviceMemory = commonMemory[_random.Next(commonMemory.Length)];
                maxTouchPoints = 0;  // 桌面设备无触摸点
            }

            // 使用 GpuCatalogService 获取真实 GPU 配置
            var osKey = selectedOS switch
            {
                "Windows" => "Windows",
                "MacOS" => "macOS",
                "Linux" => "Linux",
                "Android" => "Android",
                "iOS" => "iOS",
                _ => "Windows"
            };
            
            var gpus = await _gpuCatalog.RandomSubsetAsync(osKey, 1);
            string webglVendor = "Google Inc.";
            string webglRenderer = "ANGLE (Intel)";
            
            if (gpus.Count > 0)
            {
                webglVendor = gpus[0].Vendor;
                webglRenderer = gpus[0].Renderer;
            }

            // 随机字体（30-50个）
            var fontOsKey = selectedOS switch
            {
                "Windows" => "windows",
                "MacOS" => "macos",
                "Linux" => "linux",
                "Android" => "android",
                "iOS" => "ios",
                _ => "windows"
            };
            var fontCount = _random.Next(30, 51);
            var selectedFonts = await _fontSvc.RandomSubsetAsync(fontOsKey, fontCount);
            var fontsJson = System.Text.Json.JsonSerializer.Serialize(selectedFonts);

            // 创建临时 Profile 用于生成防检测数据
            var tempProfile = new FingerprintProfile
            {
                UserAgent = userAgent,
                Platform = platform,
                Locale = selectedLocale,
                Timezone = timezone,
                HardwareConcurrency = hardwareConcurrency,
                DeviceMemory = deviceMemory,
                MaxTouchPoints = maxTouchPoints,
                ViewportWidth = width,
                ViewportHeight = height,
                WebGLVendor = webglVendor,
                WebGLRenderer = webglRenderer,
                FontsJson = fontsJson
            };

            // 使用 AntiDetectionService 生成防检测数据
            _antiDetectSvc.GenerateAntiDetectionData(tempProfile);

            // 创建 BrowserEnvironment
            var browser = new BrowserEnvironment
            {
                Name = $"随机浏览器 {DateTime.Now:yyyyMMdd-HHmmss}",
                Engine = "UndetectedChrome", // 默认使用最佳引擎
                OS = selectedOS.ToLower(),
                UserAgent = userAgent,
                Platform = platform,
                Locale = selectedLocale,
                Timezone = timezone,
                ViewportWidth = width,
                ViewportHeight = height,
                HardwareConcurrency = hardwareConcurrency,
                DeviceMemory = deviceMemory,
                MaxTouchPoints = maxTouchPoints,
                WebGLVendor = webglVendor,
                WebGLRenderer = webglRenderer,
                FontsJson = fontsJson,
                LanguagesJson = tempProfile.LanguagesJson,
                PluginsJson = tempProfile.PluginsJson,
                SecChUa = tempProfile.SecChUa,
                SecChUaPlatform = tempProfile.SecChUaPlatform,
                SecChUaMobile = tempProfile.SecChUaMobile,
                ConnectionType = tempProfile.ConnectionType,
                ConnectionRtt = tempProfile.ConnectionRtt,
                ConnectionDownlink = tempProfile.ConnectionDownlink,
                EnablePersistence = true,
                Headless = false,
                ProxyMode = "none",
                CanvasMode = "noise",
                WebGLImageMode = "noise",
                WebGLInfoMode = "ua_based",
                AudioContextMode = "noise",
                WebRtcMode = "hide",
                WebdriverMode = "undefined",
                CreatedAt = DateTime.UtcNow,
                Notes = "通过一键随机生成功能创建"
            };

            // 创建对应的 FingerprintProfile
            var profile = new FingerprintProfile
            {
                Name = $"指纹-{browser.Name}",
                UserAgent = userAgent,
                Platform = platform,
                Locale = selectedLocale,
                Timezone = timezone,
                ViewportWidth = width,
                ViewportHeight = height,
                HardwareConcurrency = hardwareConcurrency,
                DeviceMemory = deviceMemory,
                MaxTouchPoints = maxTouchPoints,
                WebGLVendor = webglVendor,
                WebGLRenderer = webglRenderer,
                FontsJson = fontsJson,
                LanguagesJson = tempProfile.LanguagesJson,
                PluginsJson = tempProfile.PluginsJson,
                SecChUa = tempProfile.SecChUa,
                SecChUaPlatform = tempProfile.SecChUaPlatform,
                SecChUaMobile = tempProfile.SecChUaMobile,
                ConnectionType = tempProfile.ConnectionType,
                ConnectionRtt = tempProfile.ConnectionRtt,
                ConnectionDownlink = tempProfile.ConnectionDownlink,
                AcceptLanguage = selectedLocale.StartsWith("zh") ? "zh-CN,zh;q=0.9,en;q=0.8" : "en-US,en;q=0.9",
                FontsMode = "real",
                WebGLImageMode = "noise",
                WebGLInfoMode = "ua_based",
                AudioContextMode = "noise",
                DisableWebRTC = false,
                CreatedAt = DateTime.UtcNow
            };

            _log.LogInfo("BrowserRandomGenerator", 
                $"Generated random browser: OS={selectedOS}, Chrome={chromeVersion.Version}, " +
                $"GPU={webglVendor}/{webglRenderer}, Fonts={selectedFonts.Count}");

            return (browser, profile);
        }
        catch (Exception ex)
        {
            _log.LogError("BrowserRandomGenerator", $"Failed to generate random browser: {ex.Message}");
            throw;
        }
    }
}
