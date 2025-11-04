using System;
using System.Collections.Generic;
using System.Linq;

namespace FishBrowser.WPF.Services
{
    /// <summary>
    /// User-Agent 数据库服务
    /// 提供大量真实的 User-Agent 字符串，按操作系统和浏览器版本分类
    /// </summary>
    public class UserAgentDatabase
    {
        private static readonly Random _random = new Random();

        // Chrome 版本范围：120-141（最新）
        private static readonly int[] ChromeVersions = { 120, 121, 122, 123, 124, 125, 126, 127, 128, 129, 130, 131, 132, 133, 134, 135, 136, 137, 138, 139, 140, 141 };

        // Windows 版本
        private static readonly string[] WindowsVersions = 
        {
            "Windows NT 10.0; Win64; x64",  // Windows 10/11
            "Windows NT 6.3; Win64; x64",   // Windows 8.1
            "Windows NT 6.1; Win64; x64",   // Windows 7
        };

        // macOS 版本
        private static readonly string[] MacOSVersions = 
        {
            "Macintosh; Intel Mac OS X 10_15_7",  // Catalina
            "Macintosh; Intel Mac OS X 11_6_0",   // Big Sur
            "Macintosh; Intel Mac OS X 12_5_0",   // Monterey
            "Macintosh; Intel Mac OS X 13_4_0",   // Ventura
            "Macintosh; Intel Mac OS X 14_2_0",   // Sonoma
        };

        // Linux 发行版
        private static readonly string[] LinuxVersions = 
        {
            "X11; Linux x86_64",
            "X11; Ubuntu; Linux x86_64",
            "X11; Fedora; Linux x86_64",
        };

        // Android 版本
        private static readonly string[] AndroidVersions = 
        {
            "Linux; Android 10; SM-G981B",
            "Linux; Android 11; SM-G991B",
            "Linux; Android 12; SM-G996B",
        };

        // iOS 版本
        private static readonly string[] iOSVersions = 
        {
            "iPhone; CPU iPhone OS 14_2 like Mac OS X",
            "iPhone; CPU iPhone OS 15_2 like Mac OS X",
            "iPhone; CPU iPhone OS 16_2 like Mac OS X",
        };

        /// <summary>
        /// 获取随机 User-Agent（指定操作系统）
        /// </summary>
        public string GetRandomUserAgent(string os = "Windows")
        {
            var chromeVersion = ChromeVersions[_random.Next(ChromeVersions.Length)];
            var minorVersion = _random.Next(0, 10); // 0-9
            var buildVersion = _random.Next(1000, 9999); // 1000-9999
            var patchVersion = _random.Next(100, 999); // 100-999

            var fullVersion = $"{chromeVersion}.{minorVersion}.{buildVersion}.{patchVersion}";

            return os switch
            {
                "Windows" => GenerateWindowsUA(fullVersion),
                "MacOS" => GenerateMacOSUA(fullVersion),
                "Linux" => GenerateLinuxUA(fullVersion),
                "Android" => GenerateAndroidUA(fullVersion),
                "iOS" => GenerateiOSUA(fullVersion),
                _ => GenerateWindowsUA(fullVersion)
            };
        }

        /// <summary>
        /// 获取多个随机 User-Agent
        /// </summary>
        public List<string> GetRandomUserAgents(string os = "Windows", int count = 10)
        {
            var userAgents = new List<string>();
            for (int i = 0; i < count; i++)
            {
                userAgents.Add(GetRandomUserAgent(os));
            }
            return userAgents.Distinct().ToList(); // 去重
        }

        /// <summary>
        /// 获取所有支持的操作系统
        /// </summary>
        public List<string> GetSupportedOS()
        {
            return new List<string> { "Windows", "MacOS", "Linux", "Android", "iOS" };
        }

        /// <summary>
        /// 获取所有支持的 Chrome 版本
        /// </summary>
        public List<int> GetSupportedChromeVersions()
        {
            return ChromeVersions.ToList();
        }

        private string GenerateWindowsUA(string chromeVersion)
        {
            var windowsVersion = WindowsVersions[_random.Next(WindowsVersions.Length)];
            return $"Mozilla/5.0 ({windowsVersion}) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/{chromeVersion} Safari/537.36";
        }

        private string GenerateMacOSUA(string chromeVersion)
        {
            var macVersion = MacOSVersions[_random.Next(MacOSVersions.Length)];
            return $"Mozilla/5.0 ({macVersion}) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/{chromeVersion} Safari/537.36";
        }

        private string GenerateLinuxUA(string chromeVersion)
        {
            var linuxVersion = LinuxVersions[_random.Next(LinuxVersions.Length)];
            return $"Mozilla/5.0 ({linuxVersion}) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/{chromeVersion} Safari/537.36";
        }

        private string GenerateAndroidUA(string chromeVersion)
        {
            var androidVersion = AndroidVersions[_random.Next(AndroidVersions.Length)];
            return $"Mozilla/5.0 ({androidVersion}) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/{chromeVersion} Mobile Safari/537.36";
        }

        private string GenerateiOSUA(string chromeVersion)
        {
            var iosVersion = iOSVersions[_random.Next(iOSVersions.Length)];
            return $"Mozilla/5.0 ({iosVersion}) AppleWebKit/605.1.15 (KHTML, like Gecko) CriOS/{chromeVersion} Mobile/15E148 Safari/604.1";
        }

        /// <summary>
        /// 从 User-Agent 提取 Chrome 版本（支持 Chrome 和 CriOS）
        /// </summary>
        public int? ExtractChromeVersion(string userAgent)
        {
            try
            {
                // 尝试匹配 Chrome/xxx（桌面和 Android）
                var match = System.Text.RegularExpressions.Regex.Match(userAgent, @"Chrome/(\d+)\.");
                if (match.Success && int.TryParse(match.Groups[1].Value, out int version))
                {
                    return version;
                }
                
                // 尝试匹配 CriOS/xxx（iOS）
                match = System.Text.RegularExpressions.Regex.Match(userAgent, @"CriOS/(\d+)\.");
                if (match.Success && int.TryParse(match.Groups[1].Value, out int version2))
                {
                    return version2;
                }
            }
            catch { }
            return null;
        }

        /// <summary>
        /// 从 User-Agent 提取操作系统
        /// </summary>
        public string ExtractOS(string userAgent)
        {
            if (userAgent.Contains("Windows")) return "Windows";
            if (userAgent.Contains("Macintosh")) return "MacOS";
            if (userAgent.Contains("Linux")) return "Linux";
            return "Unknown";
        }
    }
}
