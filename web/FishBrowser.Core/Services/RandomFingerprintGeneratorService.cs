using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FishBrowser.WPF.Models;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace FishBrowser.WPF.Services;

/// <summary>
/// 随机指纹生成器 - 基于真实数据生成逼真的浏览器指纹
/// </summary>
public class RandomFingerprintGeneratorService
{
    private readonly ChromeVersionDatabase _chromeVersionDb;
    private readonly GpuCatalogService _gpuDb;
    private readonly FontService _fontDb;
    private readonly AntiDetectionService _antiDetectionService;
    private readonly ILogService _logService;

    public RandomFingerprintGeneratorService(
        ChromeVersionDatabase chromeVersionDb,
        GpuCatalogService gpuDb,
        FontService fontDb,
        AntiDetectionService antiDetectionService,
        ILogService logService)
    {
        _chromeVersionDb = chromeVersionDb;
        _gpuDb = gpuDb;
        _fontDb = fontDb;
        _antiDetectionService = antiDetectionService;
        _logService = logService;
    }

    /// <summary>
    /// 生成随机指纹
    /// </summary>
    public async Task<FingerprintProfile> GenerateRandomAsync(int? groupId = null)
    {
        try
        {
            _logService.LogInfo("RandomFingerprintGeneratorService", "Starting random fingerprint generation");

            var profile = new FingerprintProfile
            {
                Name = $"Random_{DateTime.UtcNow:yyyyMMdd_HHmmss}",
                GroupId = groupId,
                CreatedAt = DateTime.UtcNow
            };

            // 1. 选择 OS
            var os = SelectRandomOS();
            _logService.LogInfo("RandomFingerprintGeneratorService", $"Selected OS: {os}");

            // 2. 选择 Chrome 版本
            var chromeVersion = _chromeVersionDb.GetRandomVersion(os);
            if (chromeVersion == null)
                throw new InvalidOperationException($"No Chrome version found for OS: {os}");

            // 3. 生成 User-Agent
            profile.UserAgent = GenerateUserAgent(os, chromeVersion.Version);
            _logService.LogInfo("RandomFingerprintGeneratorService", $"Generated UA: {profile.UserAgent}");

            // 4. 设置 Platform
            profile.Platform = GetPlatformFromOS(os);

            // 5. 生成视口大小
            var viewport = SelectRandomViewport();
            profile.ViewportWidth = viewport.width;
            profile.ViewportHeight = viewport.height;

            // 6. 设置时区和语言
            var localeInfo = SelectRandomLocale();
            profile.Locale = localeInfo.locale;
            profile.Timezone = localeInfo.timezone;
            profile.AcceptLanguage = localeInfo.acceptLanguage;

            // 7. 生成硬件配置
            profile.HardwareConcurrency = SelectRandomCoreCount();
            profile.DeviceMemory = SelectRandomMemory();
            profile.MaxTouchPoints = os == "Windows" ? 0 : SelectRandomTouchPoints();

            // 8. 选择 GPU
            var gpus = await _gpuDb.RandomSubsetAsync(os, 1);
            if (gpus.Count > 0)
            {
                var gpu = gpus[0];
                profile.WebGLVendor = gpu.Vendor;
                profile.WebGLRenderer = gpu.Renderer;
            }

            // 9. 选择字体
            var fontNames = await _fontDb.RandomSubsetAsync(os, 30);
            if (fontNames != null && fontNames.Count > 0)
            {
                profile.FontsJson = JsonSerializer.Serialize(fontNames);
                profile.FontsMode = "real";
            }

            // 10. 生成网络配置
            profile.ConnectionType = SelectRandomConnectionType();
            profile.ConnectionRtt = SelectRandomRTT();
            profile.ConnectionDownlink = SelectRandomDownlink();

            // 11. 生成防检测数据
            _antiDetectionService.GenerateAntiDetectionData(profile);

            // 12. 生成 Sec-CH-UA
            profile.SecChUa = GenerateSecChUa(chromeVersion.Version);
            profile.SecChUaPlatform = GenerateSecChUaPlatform(os);
            profile.SecChUaMobile = "?0";

            _logService.LogInfo("RandomFingerprintGeneratorService", 
                $"Random fingerprint generated successfully: {profile.Name}");

            return profile;
        }
        catch (Exception ex)
        {
            _logService.LogError("RandomFingerprintGeneratorService", 
                $"Failed to generate random fingerprint: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 随机选择 OS
    /// </summary>
    private string SelectRandomOS()
    {
        var osOptions = new[] { "Windows", "Mac", "Linux" };
        var weights = new[] { 0.7, 0.2, 0.1 };  // Windows 70%, Mac 20%, Linux 10%
        return SelectWeightedRandom(osOptions, weights);
    }

    /// <summary>
    /// 生成 User-Agent
    /// </summary>
    private string GenerateUserAgent(string os, string version)
    {
        return os switch
        {
            "Windows" => $"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/{version} Safari/537.36",
            // ⭐ macOS: 使用常见的 macOS 版本号
            "Mac" => SelectRandomMacOSUserAgent(version),
            "Linux" => $"Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/{version} Safari/537.36",
            _ => $"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/{version} Safari/537.36"
        };
    }

    /// <summary>
    /// 随机选择 macOS User-Agent（使用常见的 macOS 版本）
    /// </summary>
    private string SelectRandomMacOSUserAgent(string chromeVersion)
    {
        // 常见的 macOS 版本号
        var macOSVersions = new[]
        {
            "10_15_7",  // Catalina (仍然常见)
            "11_7_10",  // Big Sur
            "12_7_6",   // Monterey
            "13_6_9",   // Ventura
            "14_7_1",   // Sonoma (最新)
        };
        
        var random = new Random();
        var macOSVersion = macOSVersions[random.Next(macOSVersions.Length)];
        
        return $"Mozilla/5.0 (Macintosh; Intel Mac OS X {macOSVersion}) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/{chromeVersion} Safari/537.36";
    }

    /// <summary>
    /// 获取 Platform
    /// </summary>
    private string GetPlatformFromOS(string os)
    {
        return os switch
        {
            "Windows" => "Win32",
            "Mac" => "MacIntel",
            "Linux" => "Linux x86_64",
            _ => "Win32"
        };
    }

    /// <summary>
    /// 随机选择视口大小（优先使用常见分辨率）
    /// </summary>
    private (int width, int height) SelectRandomViewport()
    {
        // ⭐ 优先使用 1280x720（成功配置使用的分辨率）
        var viewports = new[]
        {
            (1280, 720),   // 最常见，优先
            (1920, 1080),  // 常见
            (1366, 768),   // 常见
            (1440, 900),
            (1600, 900),
        };

        var random = new Random();
        // 70% 概率选择 1280x720
        if (random.NextDouble() < 0.7)
            return (1280, 720);
        
        return viewports[random.Next(viewports.Length)];
    }

    /// <summary>
    /// 随机选择语言和时区
    /// </summary>
    private (string locale, string timezone, string acceptLanguage) SelectRandomLocale()
    {
        var locales = new[]
        {
            ("zh-CN", "Asia/Shanghai", "zh-CN,zh;q=0.9,en;q=0.8"),
            ("en-US", "America/New_York", "en-US,en;q=0.9"),
            ("en-GB", "Europe/London", "en-GB,en;q=0.9"),
            ("ja-JP", "Asia/Tokyo", "ja-JP,ja;q=0.9,en;q=0.8"),
            ("de-DE", "Europe/Berlin", "de-DE,de;q=0.9,en;q=0.8"),
            ("fr-FR", "Europe/Paris", "fr-FR,fr;q=0.9,en;q=0.8"),
        };

        var random = new Random();
        return locales[random.Next(locales.Length)];
    }

    /// <summary>
    /// 随机选择 CPU 核心数 (使用常见配置)
    /// </summary>
    private int SelectRandomCoreCount()
    {
        // ⭐ 使用真实常见的核心数，避免 11 这种不常见的配置
        var commonCores = new[] { 4, 6, 8, 12, 16 };
        var random = new Random();
        return commonCores[random.Next(commonCores.Length)];
    }

    /// <summary>
    /// 随机选择内存大小 (8-32 GB)
    /// </summary>
    private int SelectRandomMemory()
    {
        var memoryOptions = new[] { 8, 16, 32 };
        var random = new Random();
        return memoryOptions[random.Next(memoryOptions.Length)];
    }

    /// <summary>
    /// 随机选择触摸点数
    /// </summary>
    private int SelectRandomTouchPoints()
    {
        var random = new Random();
        return random.Next(5, 11);  // 5-10
    }

    /// <summary>
    /// 随机选择网络类型
    /// </summary>
    private string SelectRandomConnectionType()
    {
        var types = new[] { "4g", "wifi" };
        var weights = new[] { 0.7, 0.3 };
        return SelectWeightedRandom(types, weights);
    }

    /// <summary>
    /// 随机选择网络延迟 (50-300ms)
    /// </summary>
    private int SelectRandomRTT()
    {
        var random = new Random();
        return random.Next(50, 301);
    }

    /// <summary>
    /// 随机选择下载速度 (1-10 Mbps)
    /// </summary>
    private double SelectRandomDownlink()
    {
        var random = new Random();
        return Math.Round(random.NextDouble() * 9 + 1, 2);
    }

    /// <summary>
    /// 生成 Sec-CH-UA
    /// </summary>
    private string GenerateSecChUa(string version)
    {
        var majorVersion = version.Split('.')[0];
        return $"\"Chromium\";v=\"{majorVersion}\", \"Google Chrome\";v=\"{majorVersion}\", \";Not A Brand\";v=\"99\"";
    }

    /// <summary>
    /// 生成 Sec-CH-UA-Platform
    /// </summary>
    private string GenerateSecChUaPlatform(string os)
    {
        return os switch
        {
            "Windows" => "\"Windows\"",
            "Mac" => "\"macOS\"",
            "Linux" => "\"Linux\"",
            _ => "\"Windows\""
        };
    }

    /// <summary>
    /// 加权随机选择
    /// </summary>
    private string SelectWeightedRandom(string[] options, double[] weights)
    {
        var random = new Random();
        var totalWeight = weights.Sum();
        var randomValue = random.NextDouble() * totalWeight;

        var cumulativeWeight = 0.0;
        for (int i = 0; i < options.Length; i++)
        {
            cumulativeWeight += weights[i];
            if (randomValue <= cumulativeWeight)
                return options[i];
        }

        return options[^1];
    }
}
