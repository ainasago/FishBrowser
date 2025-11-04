using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using FishBrowser.WPF.Models;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace FishBrowser.WPF.Services;

/// <summary>
/// 防检测数据生成与校验服务
/// </summary>
public class AntiDetectionService
{
    private readonly Random _random = new();

    /// <summary>
    /// 为指纹配置生成防检测数据（一键随机时调用）
    /// </summary>
    public void GenerateAntiDetectionData(FingerprintProfile profile)
    {
        // 1. 生成 Plugins（根据 UA 判断浏览器类型）
        profile.PluginsJson = GeneratePlugins(profile.UserAgent);

        // 2. 生成 Languages（根据 Locale 和 AcceptLanguage）
        profile.LanguagesJson = GenerateLanguages(profile.Locale, profile.AcceptLanguage);

        // 3. 生成硬件参数（随机但合理）
        profile.HardwareConcurrency = GenerateHardwareConcurrency();
        profile.DeviceMemory = GenerateDeviceMemory();
        profile.MaxTouchPoints = profile.Platform.Contains("Mobile") ? _random.Next(1, 6) : 0;

        // 4. 生成网络参数（随机但合理）
        var connection = GenerateConnection();
        profile.ConnectionType = connection.Type;
        profile.ConnectionRtt = connection.Rtt;
        profile.ConnectionDownlink = connection.Downlink;

        // 5. 生成 Client Hints（根据 UA）
        var hints = GenerateClientHints(profile.UserAgent, profile.Platform);
        profile.SecChUa = hints.SecChUa;
        profile.SecChUaPlatform = hints.SecChUaPlatform;
        profile.SecChUaMobile = hints.SecChUaMobile;
    }

    /// <summary>
    /// 校验并修复指纹配置的一致性（保存时调用）
    /// </summary>
    public List<string> ValidateProfile(FingerprintProfile profile)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        // 1. 校验并修复 UA 与 Platform 一致性
        if (!string.IsNullOrEmpty(profile.UserAgent))
        {
            // 检测 UA 中的操作系统
            bool uaHasWindows = profile.UserAgent.Contains("Windows");
            bool uaHasIPhone = profile.UserAgent.Contains("iPhone");
            bool uaHasIPad = profile.UserAgent.Contains("iPad");
            bool uaHasAndroid = profile.UserAgent.Contains("Android");
            bool uaHasMac = (profile.UserAgent.Contains("Mac") || profile.UserAgent.Contains("Macintosh")) && !uaHasIPhone && !uaHasIPad;
            bool uaHasLinux = profile.UserAgent.Contains("Linux") && !uaHasAndroid;
            
            // 根据 UA 自动修正 Platform
            if (uaHasWindows && profile.Platform != "Win32")
            {
                warnings.Add($"Platform ({profile.Platform}) 与 UA (Windows) 不一致，已自动修正为 Win32");
                profile.Platform = "Win32";
            }
            else if (uaHasIPhone && profile.Platform != "iPhone")
            {
                warnings.Add($"Platform ({profile.Platform}) 与 UA (iPhone) 不一致，已自动修正为 iPhone");
                profile.Platform = "iPhone";
            }
            else if (uaHasIPad && profile.Platform != "iPad")
            {
                warnings.Add($"Platform ({profile.Platform}) 与 UA (iPad) 不一致，已自动修正为 iPad");
                profile.Platform = "iPad";
            }
            else if (uaHasAndroid && profile.Platform != "Linux armv8l")
            {
                warnings.Add($"Platform ({profile.Platform}) 与 UA (Android) 不一致，已自动修正为 Linux armv8l");
                profile.Platform = "Linux armv8l";
            }
            else if (uaHasMac && profile.Platform != "MacIntel")
            {
                warnings.Add($"Platform ({profile.Platform}) 与 UA (Mac) 不一致，已自动修正为 MacIntel");
                profile.Platform = "MacIntel";
            }
            else if (uaHasLinux && !profile.Platform.Contains("Linux"))
            {
                warnings.Add($"Platform ({profile.Platform}) 与 UA (Linux) 不一致，已自动修正为 Linux x86_64");
                profile.Platform = "Linux x86_64";
            }
            
            // 重新生成 Client Hints（基于修正后的 Platform）
            var hints = GenerateClientHints(profile.UserAgent, profile.Platform);
            if (profile.SecChUa != hints.SecChUa || 
                profile.SecChUaPlatform != hints.SecChUaPlatform ||
                profile.SecChUaMobile != hints.SecChUaMobile)
            {
                warnings.Add("Client Hints 与 Platform 不一致，已自动重新生成");
                profile.SecChUa = hints.SecChUa;
                profile.SecChUaPlatform = hints.SecChUaPlatform;
                profile.SecChUaMobile = hints.SecChUaMobile;
            }
        }

        // 2. 校验 Languages 与 Locale 一致性
        if (!string.IsNullOrEmpty(profile.LanguagesJson))
        {
            try
            {
                var languages = JsonSerializer.Deserialize<List<string>>(profile.LanguagesJson);
                if (languages != null && languages.Count > 0)
                {
                    var firstLang = languages[0];
                    if (!string.IsNullOrEmpty(profile.Locale) && !firstLang.StartsWith(profile.Locale.Split('-')[0]))
                        errors.Add($"Languages 首项 ({firstLang}) 与 Locale ({profile.Locale}) 不一致");
                }
            }
            catch
            {
                errors.Add("LanguagesJson 格式错误");
            }
        }

        // 3. 校验 Client Hints 与 UA 一致性
        if (!string.IsNullOrEmpty(profile.SecChUa) && !string.IsNullOrEmpty(profile.UserAgent))
        {
            var uaMatch = Regex.Match(profile.UserAgent, @"Chrome/(\d+)");
            if (uaMatch.Success)
            {
                var version = uaMatch.Groups[1].Value;
                if (!profile.SecChUa.Contains($"v=\"{version}\""))
                    errors.Add($"SecChUa 版本与 UA Chrome 版本 ({version}) 不一致");
            }
        }

        // 4. 校验 HardwareConcurrency 合理性
        if (profile.HardwareConcurrency < 1 || profile.HardwareConcurrency > 32)
            errors.Add($"HardwareConcurrency ({profile.HardwareConcurrency}) 不合理，应在 1-32 之间");

        // 5. 校验 DeviceMemory 合理性
        var validMemory = new[] { 0.25, 0.5, 1, 2, 4, 8, 16, 32 };
        if (!validMemory.Contains(profile.DeviceMemory))
            errors.Add($"DeviceMemory ({profile.DeviceMemory}) 不合理，应为 {string.Join(", ", validMemory)}");

        // 6. 校验 MaxTouchPoints 与 Platform 一致性
        if (!profile.Platform.Contains("Mobile") && profile.MaxTouchPoints > 0)
            errors.Add($"桌面平台 ({profile.Platform}) 不应有触摸点 ({profile.MaxTouchPoints})");

        return errors;
    }

    #region 私有生成方法

    private string GeneratePlugins(string userAgent)
    {
        // ⭐ 重要：Chrome 87+ 已移除 NPAPI 插件支持
        // 任何 plugins 都会被检测为伪造指纹
        // 参考：https://blog.chromium.org/2013/09/saying-goodbye-to-our-old-friend-npapi.html
        
        var plugins = new List<object>();

        // 移动设备（iOS/Android）：不生成任何插件
        if (userAgent.Contains("Mobile") || userAgent.Contains("iPhone") || userAgent.Contains("iPad") || userAgent.Contains("Android"))
        {
            // 空数组 - 移动浏览器不支持插件
            // plugins.count = 0
        }
        // Chrome/Edge：不生成任何插件（Chrome 87+ 已移除插件）
        else if (userAgent.Contains("Chrome") || userAgent.Contains("Edg"))
        {
            // 空数组 - Chrome 87+ 不应该有任何 plugins
            // plugins.count = 0
        }
        // Firefox 插件
        else if (userAgent.Contains("Firefox"))
        {
            plugins.Add(new { name = "PDF Viewer", filename = "internal-pdf-viewer", description = "Portable Document Format" });
        }
        // Safari 插件（仅桌面版 macOS）
        else if (userAgent.Contains("Safari") && !userAgent.Contains("Mobile"))
        {
            plugins.Add(new { name = "PDF", filename = "internal-pdf-viewer", description = "Portable Document Format" });
        }

        return JsonSerializer.Serialize(plugins);
    }

    private string GenerateLanguages(string locale, string acceptLanguage)
    {
        var languages = new List<string>();

        // 从 locale 提取主语言
        if (!string.IsNullOrEmpty(locale))
        {
            languages.Add(locale);  // zh-CN
            var mainLang = locale.Split('-')[0];
            if (!languages.Contains(mainLang))
                languages.Add(mainLang);  // zh
        }

        // 从 Accept-Language 提取其他语言
        if (!string.IsNullOrEmpty(acceptLanguage))
        {
            var parts = acceptLanguage.Split(',');
            foreach (var part in parts)
            {
                var lang = part.Split(';')[0].Trim();
                if (!languages.Contains(lang))
                    languages.Add(lang);
            }
        }

        // 确保至少有英语
        if (!languages.Any(l => l.StartsWith("en")))
        {
            languages.Add("en-US");
            languages.Add("en");
        }

        return JsonSerializer.Serialize(languages.Take(6).ToList());  // 最多 6 个
    }

    private int GenerateHardwareConcurrency()
    {
        // 常见 CPU 核心数：2, 4, 6, 8, 12, 16
        var common = new[] { 2, 4, 6, 8, 12, 16 };
        var weights = new[] { 5, 30, 20, 30, 10, 5 };  // 权重
        var total = weights.Sum();
        var rand = _random.Next(total);
        var sum = 0;
        for (int i = 0; i < common.Length; i++)
        {
            sum += weights[i];
            if (rand < sum) return common[i];
        }
        return 8;  // 默认
    }

    private int GenerateDeviceMemory()
    {
        // 常见内存：4, 8, 16
        var common = new[] { 4, 8, 16 };
        var weights = new[] { 20, 60, 20 };
        var total = weights.Sum();
        var rand = _random.Next(total);
        var sum = 0;
        for (int i = 0; i < common.Length; i++)
        {
            sum += weights[i];
            if (rand < sum) return common[i];
        }
        return 8;
    }

    private (string Type, int Rtt, double Downlink) GenerateConnection()
    {
        var types = new[] { "4g", "wifi", "3g" };
        var weights = new[] { 70, 25, 5 };
        var total = weights.Sum();
        var rand = _random.Next(total);
        var sum = 0;
        string type = "4g";
        for (int i = 0; i < types.Length; i++)
        {
            sum += weights[i];
            if (rand < sum)
            {
                type = types[i];
                break;
            }
        }

        // 根据类型生成合理的 RTT 和 Downlink
        return type switch
        {
            "4g" => ("4g", _random.Next(30, 80), _random.Next(5, 20) + _random.NextDouble()),
            "wifi" => ("wifi", _random.Next(10, 40), _random.Next(20, 100) + _random.NextDouble()),
            "3g" => ("3g", _random.Next(100, 300), _random.Next(1, 5) + _random.NextDouble()),
            _ => ("4g", 50, 10.0)
        };
    }

    private (string SecChUa, string SecChUaPlatform, string SecChUaMobile) GenerateClientHints(string userAgent, string platform)
    {
        // 提取 Chrome 版本
        var uaMatch = Regex.Match(userAgent ?? "", @"Chrome/(\d+)");
        var chromeVersion = uaMatch.Success ? uaMatch.Groups[1].Value : "120";

        // 生成 sec-ch-ua
        var secChUa = $"\"Chromium\";v=\"{chromeVersion}\", \"Google Chrome\";v=\"{chromeVersion}\", \"Not-A.Brand\";v=\"99\"";

        // 生成 sec-ch-ua-platform
        var secChUaPlatform = platform switch
        {
            "Win32" => "\"Windows\"",
            "MacIntel" => "\"macOS\"",
            var p when p.Contains("Linux") => "\"Linux\"",
            var p when p.Contains("Android") => "\"Android\"",
            var p when p.Contains("iOS") => "\"iOS\"",
            _ => "\"Windows\""
        };

        // 生成 sec-ch-ua-mobile
        var secChUaMobile = platform.Contains("Mobile") || platform.Contains("Android") || platform.Contains("iOS") ? "?1" : "?0";

        return (secChUa, secChUaPlatform, secChUaMobile);
    }

    #endregion
}
