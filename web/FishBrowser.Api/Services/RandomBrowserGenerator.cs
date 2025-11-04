using FishBrowser.WPF.Models;

namespace FishBrowser.Api.Services;

/// <summary>
/// 随机浏览器环境生成器
/// </summary>
public class RandomBrowserGenerator
{
    private static readonly Random _random = new Random();
    
    // 常见的浏览器引擎
    private static readonly string[] Engines = { "UndetectedChrome", "Chromium", "Firefox" };
    
    // 常见的操作系统
    private static readonly string[] OperatingSystems = { "windows", "macos", "linux" };
    
    // Windows User Agents
    private static readonly string[] WindowsUserAgents = 
    {
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:121.0) Gecko/20100101 Firefox/121.0",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:120.0) Gecko/20100101 Firefox/120.0"
    };
    
    // macOS User Agents
    private static readonly string[] MacUserAgents = 
    {
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.1 Safari/605.1.15",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10.15; rv:121.0) Gecko/20100101 Firefox/121.0"
    };
    
    // Linux User Agents
    private static readonly string[] LinuxUserAgents = 
    {
        "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
        "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36",
        "Mozilla/5.0 (X11; Linux x86_64; rv:121.0) Gecko/20100101 Firefox/121.0",
        "Mozilla/5.0 (X11; Ubuntu; Linux x86_64; rv:120.0) Gecko/20100101 Firefox/120.0"
    };
    
    // 常见的分辨率
    private static readonly (int width, int height)[] CommonResolutions = 
    {
        (1920, 1080),
        (1366, 768),
        (1440, 900),
        (1536, 864),
        (1600, 900),
        (2560, 1440),
        (1280, 720)
    };
    
    // 常见的时区
    private static readonly string[] Timezones = 
    {
        "Asia/Shanghai",
        "Asia/Hong_Kong",
        "Asia/Tokyo",
        "America/New_York",
        "America/Los_Angeles",
        "Europe/London",
        "Europe/Paris",
        "Australia/Sydney"
    };
    
    // 常见的语言
    private static readonly string[] Locales = 
    {
        "zh-CN",
        "zh-TW",
        "en-US",
        "en-GB",
        "ja-JP",
        "ko-KR",
        "de-DE",
        "fr-FR"
    };

    /// <summary>
    /// 生成随机浏览器环境
    /// </summary>
    public static BrowserEnvironment GenerateRandomBrowser()
    {
        var os = OperatingSystems[_random.Next(OperatingSystems.Length)];
        var engine = Engines[_random.Next(Engines.Length)];
        var resolution = CommonResolutions[_random.Next(CommonResolutions.Length)];
        var timezone = Timezones[_random.Next(Timezones.Length)];
        var locale = Locales[_random.Next(Locales.Length)];
        
        // 根据操作系统选择合适的 User Agent
        string userAgent = os switch
        {
            "windows" => WindowsUserAgents[_random.Next(WindowsUserAgents.Length)],
            "macos" => MacUserAgents[_random.Next(MacUserAgents.Length)],
            "linux" => LinuxUserAgents[_random.Next(LinuxUserAgents.Length)],
            _ => WindowsUserAgents[0]
        };
        
        // 根据操作系统设置 Platform
        string platform = os switch
        {
            "windows" => "Win32",
            "macos" => "MacIntel",
            "linux" => "Linux x86_64",
            _ => "Win32"
        };
        
        // 随机硬件配置
        int hardwareConcurrency = new[] { 4, 6, 8, 12, 16 }[_random.Next(5)];
        int deviceMemory = new[] { 4, 8, 16, 32 }[_random.Next(4)];
        
        // 随机 WebGL 配置
        var webglVendors = new[] { "Google Inc. (Intel)", "Google Inc. (NVIDIA)", "Google Inc. (AMD)", "Intel Inc.", "NVIDIA Corporation" };
        var webglRenderers = new[] 
        { 
            "ANGLE (Intel, Intel(R) UHD Graphics 630, OpenGL 4.1)",
            "ANGLE (NVIDIA, NVIDIA GeForce GTX 1660 Ti, OpenGL 4.5)",
            "ANGLE (AMD, AMD Radeon RX 580, OpenGL 4.5)",
            "Intel Iris OpenGL Engine",
            "NVIDIA GeForce RTX 3060"
        };
        
        var browser = new BrowserEnvironment
        {
            Name = $"随机浏览器 {DateTime.Now:yyyyMMdd-HHmmss}",
            Engine = engine,
            OS = os,
            UserAgent = userAgent,
            Platform = platform,
            Locale = locale,
            Timezone = timezone,
            ViewportWidth = resolution.width,
            ViewportHeight = resolution.height,
            HardwareConcurrency = hardwareConcurrency,
            DeviceMemory = deviceMemory,
            MaxTouchPoints = 0,
            WebGLVendor = webglVendors[_random.Next(webglVendors.Length)],
            WebGLRenderer = webglRenderers[_random.Next(webglRenderers.Length)],
            EnablePersistence = true,
            Headless = false,
            ProxyMode = "none",
            CanvasMode = "noise",
            WebGLImageMode = "noise",
            WebGLInfoMode = "ua_based",
            AudioContextMode = "noise",
            WebRtcMode = "hide",
            CreatedAt = DateTime.UtcNow,
            Notes = "通过一键随机生成功能创建"
        };
        
        return browser;
    }
    
    /// <summary>
    /// 生成随机指纹配置
    /// </summary>
    public static FingerprintProfile GenerateRandomFingerprintProfile(BrowserEnvironment browser)
    {
        var profile = new FingerprintProfile
        {
            Name = $"指纹-{browser.Name}",
            UserAgent = browser.UserAgent ?? "",
            Platform = browser.Platform ?? "Win32",
            Locale = browser.Locale ?? "zh-CN",
            Timezone = browser.Timezone ?? "Asia/Shanghai",
            ViewportWidth = browser.ViewportWidth,
            ViewportHeight = browser.ViewportHeight,
            HardwareConcurrency = browser.HardwareConcurrency ?? 8,
            DeviceMemory = browser.DeviceMemory ?? 8,
            MaxTouchPoints = browser.MaxTouchPoints ?? 0,
            WebGLVendor = browser.WebGLVendor,
            WebGLRenderer = browser.WebGLRenderer,
            AcceptLanguage = browser.Locale?.StartsWith("zh") == true ? "zh-CN,zh;q=0.9,en;q=0.8" : "en-US,en;q=0.9",
            FontsMode = "real",
            WebGLImageMode = "noise",
            WebGLInfoMode = "ua_based",
            AudioContextMode = "noise",
            DisableWebRTC = false,
            CreatedAt = DateTime.UtcNow
        };
        
        return profile;
    }
}
