using System.Text;
using FishBrowser.WPF.Models;

namespace FishBrowser.WPF.Services;

/// <summary>
/// 浏览器指纹信息服务 - 生成格式化文本和 JSON
/// </summary>
public class BrowserFingerprintService
{
    private readonly FingerprintCollectorService? _collectorService;

    public BrowserFingerprintService(FingerprintCollectorService? collectorService = null)
    {
        _collectorService = collectorService;
    }

    /// <summary>
    /// 生成格式化文本指纹信息（用于显示和导出）
    /// </summary>
    public string GenerateFingerprintText(BrowserEnvironment browser, FingerprintProfile? profile = null)
    {
        var info = new StringBuilder();
        info.AppendLine("=".PadRight(80, '='));
        info.AppendLine("🔍 浏览器指纹信息");
        info.AppendLine("=".PadRight(80, '='));
        info.AppendLine();

        // 基础信息
        info.AppendLine("📋 基础信息");
        info.AppendLine("-".PadRight(80, '-'));
        
        if (profile != null)
        {
            info.AppendLine($"Profile ID:              {profile.Id}");
            info.AppendLine($"Profile Name:            {profile.Name}");
            info.AppendLine($"Created At:              {profile.CreatedAt:yyyy-MM-dd HH:mm:ss}");
            info.AppendLine($"Updated At:              {profile.UpdatedAt:yyyy-MM-dd HH:mm:ss}");
        }
        else
        {
            info.AppendLine($"Browser ID:              {browser.Id}");
            info.AppendLine($"Browser Name:            {browser.Name}");
            info.AppendLine($"Created At:              {browser.CreatedAt:yyyy-MM-dd HH:mm:ss}");
            info.AppendLine($"Updated At:              {browser.UpdatedAt:yyyy-MM-dd HH:mm:ss}");
        }
        info.AppendLine();

        // User-Agent 信息
        info.AppendLine("🌐 User-Agent");
        info.AppendLine("-".PadRight(80, '-'));
        info.AppendLine($"User-Agent:              {browser.UserAgent ?? profile?.UserAgent}");
        info.AppendLine();

        // 语言和地区
        info.AppendLine("🗣️ 语言和地区");
        info.AppendLine("-".PadRight(80, '-'));
        info.AppendLine($"Locale:                  {browser.Locale ?? profile?.Locale}");
        info.AppendLine($"Languages:               {browser.LanguagesJson ?? profile?.LanguagesJson}");
        info.AppendLine($"Timezone:                {browser.Timezone ?? profile?.Timezone}");
        info.AppendLine();

        // 屏幕和视口
        info.AppendLine("📱 屏幕和视口");
        info.AppendLine("-".PadRight(80, '-'));
        info.AppendLine($"Viewport Width:          {browser.ViewportWidth}");
        info.AppendLine($"Viewport Height:         {browser.ViewportHeight}");
        info.AppendLine();

        // 平台信息
        info.AppendLine("💻 平台信息");
        info.AppendLine("-".PadRight(80, '-'));
        info.AppendLine($"Platform:                {browser.Platform ?? profile?.Platform}");
        info.AppendLine();

        // WebGL 信息
        info.AppendLine("🎮 WebGL 信息");
        info.AppendLine("-".PadRight(80, '-'));
        info.AppendLine($"WebGL Vendor:            {browser.WebGLVendor ?? profile?.WebGLVendor}");
        info.AppendLine($"WebGL Renderer:          {browser.WebGLRenderer ?? profile?.WebGLRenderer}");
        info.AppendLine();

        // 字体信息
        info.AppendLine("🔤 字体信息");
        info.AppendLine("-".PadRight(80, '-'));
        info.AppendLine($"Fonts Mode:              {browser.FontsMode ?? profile?.FontsMode}");
        info.AppendLine($"Fonts JSON:              {browser.FontsJson ?? profile?.FontsJson}");
        info.AppendLine();

        // 硬件信息
        info.AppendLine("⚙️ 硬件信息");
        info.AppendLine("-".PadRight(80, '-'));
        info.AppendLine($"Hardware Concurrency:    {browser.HardwareConcurrency ?? profile?.HardwareConcurrency}");
        info.AppendLine($"Device Memory:           {browser.DeviceMemory ?? profile?.DeviceMemory}");
        info.AppendLine($"Max Touch Points:        {browser.MaxTouchPoints ?? profile?.MaxTouchPoints}");
        info.AppendLine();

        // 网络信息
        info.AppendLine("🌍 网络信息");
        info.AppendLine("-".PadRight(80, '-'));
        info.AppendLine($"Connection Type:         {browser.ConnectionType ?? profile?.ConnectionType}");
        info.AppendLine($"Connection RTT:          {browser.ConnectionRtt ?? profile?.ConnectionRtt}");
        info.AppendLine($"Connection Downlink:     {browser.ConnectionDownlink ?? profile?.ConnectionDownlink}");
        info.AppendLine();

        // Sec-CH-UA 信息
        info.AppendLine("🔐 Sec-CH-UA 信息");
        info.AppendLine("-".PadRight(80, '-'));
        info.AppendLine($"Sec-CH-UA:               {browser.SecChUa ?? profile?.SecChUa}");
        info.AppendLine($"Sec-CH-UA-Platform:      {browser.SecChUaPlatform ?? profile?.SecChUaPlatform}");
        info.AppendLine($"Sec-CH-UA-Mobile:        {browser.SecChUaMobile ?? profile?.SecChUaMobile}");
        info.AppendLine();

        // Plugins 信息
        info.AppendLine("🔌 Plugins 信息");
        info.AppendLine("-".PadRight(80, '-'));
        info.AppendLine($"Plugins JSON:            {browser.PluginsJson ?? profile?.PluginsJson}");
        info.AppendLine();

        // 其他信息
        info.AppendLine("📌 其他信息");
        info.AppendLine("-".PadRight(80, '-'));
        
        var locale = browser.Locale ?? profile?.Locale;
        var acceptLanguage = locale?.StartsWith("zh") == true ? "zh-CN,zh;q=0.9,en;q=0.8" : "en-US,en;q=0.9";
        info.AppendLine($"Accept Language:         {acceptLanguage}");
        
        // Webdriver 配置
        var webdriverMode = browser.WebdriverMode ?? "undefined";
        var webdriverDisplay = webdriverMode switch
        {
            "undefined" or "delete" => "undefined (已隐藏)",
            "true" => "true (显示)",
            "false" => "false (显示)",
            _ => webdriverMode
        };
        info.AppendLine($"Webdriver Mode:          {webdriverDisplay}");
        info.AppendLine();

        info.AppendLine("=".PadRight(80, '='));

        return info.ToString();
    }

    /// <summary>
    /// 生成 JSON 格式的指纹信息（用于 API 和自动化）
    /// ⭐ 与 WPF 的 FingerprintCollectorService 完全一致
    /// </summary>
    public string GenerateFingerprintJson(BrowserEnvironment browser, FingerprintProfile? profile = null)
    {
        // 如果有 FingerprintCollectorService 和 profile，优先使用它
        if (_collectorService != null && profile != null)
        {
            var webdriverMode = browser.WebdriverMode ?? "undefined";
            return _collectorService.GenerateFingerprintJson(profile, webdriverMode);
        }

        // 否则，从 BrowserEnvironment 构建完整的 FingerprintProfile 并使用相同逻辑
        var tempProfile = new FingerprintProfile
        {
            Id = browser.Id,
            Name = browser.Name,
            UserAgent = browser.UserAgent,
            Platform = browser.Platform,
            Locale = browser.Locale,
            Timezone = browser.Timezone,
            ViewportWidth = browser.ViewportWidth,
            ViewportHeight = browser.ViewportHeight,
            HardwareConcurrency = browser.HardwareConcurrency ?? 8,
            DeviceMemory = browser.DeviceMemory ?? 8,
            MaxTouchPoints = browser.MaxTouchPoints ?? 0,
            WebGLVendor = browser.WebGLVendor,
            WebGLRenderer = browser.WebGLRenderer,
            FontsJson = browser.FontsJson,
            FontsMode = browser.FontsMode,
            LanguagesJson = browser.LanguagesJson,
            PluginsJson = browser.PluginsJson,
            ConnectionType = browser.ConnectionType,
            ConnectionRtt = browser.ConnectionRtt ?? 50,
            ConnectionDownlink = browser.ConnectionDownlink ?? 10.0,
            SecChUa = browser.SecChUa,
            SecChUaPlatform = browser.SecChUaPlatform,
            SecChUaMobile = browser.SecChUaMobile,
            CreatedAt = browser.CreatedAt,
            UpdatedAt = browser.UpdatedAt
        };

        // 如果有 FingerprintCollectorService，使用它生成完整 JSON
        if (_collectorService != null)
        {
            var webdriverMode = browser.WebdriverMode ?? "undefined";
            return _collectorService.GenerateFingerprintJson(tempProfile, webdriverMode);
        }

        // 兜底：返回简化的 JSON（不应该走到这里）
        return System.Text.Json.JsonSerializer.Serialize(new
        {
            error = "FingerprintCollectorService not available",
            basicInfo = new
            {
                userAgent = browser.UserAgent,
                platform = browser.Platform
            }
        }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
    }

    private int GetTimezoneOffset(string? timezone)
    {
        if (string.IsNullOrEmpty(timezone)) return -480; // 默认 UTC+8

        return timezone switch
        {
            "Asia/Shanghai" => -480,  // UTC+8
            "America/New_York" => 300, // UTC-5
            "Asia/Tokyo" => -540,      // UTC+9
            "Asia/Seoul" => -540,      // UTC+9
            "Europe/London" => 0,      // UTC+0
            _ => -480
        };
    }
}
