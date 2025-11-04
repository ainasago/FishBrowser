using System;
using System.Collections.Generic;
using System.Linq;

namespace FishBrowser.WPF.Services;

public class UserAgentCompositionService
{
    private static readonly string[] ChromeMajorVersions = { "122", "123", "124", "125", "126", "127", "128" };
    private static readonly string[] WindowsBuilds = { "10.0; Win64; x64", "10.0; WOW64", "11.0; Win64; x64" };

    public record UAResult(string UserAgent, Dictionary<string, object?> RelatedTraits);

    public UAResult GenerateChromeWindows(string? locale = null)
    {
        var rnd = new Random();
        var major = Pick(ChromeMajorVersions, rnd);
        var minor = rnd.Next(0, 6000);
        var patch = rnd.Next(0, 200);
        var build = Pick(WindowsBuilds, rnd);

        var chromium = $"{major}.0.{minor}.{patch}";
        var safari = $"537.{rnd.Next(36, 39)}";

        var ua = $"Mozilla/5.0 (Windows NT {build}) AppleWebKit/{safari} (KHTML, like Gecko) Chrome/{chromium} Safari/{safari}";

        var related = new Dictionary<string, object?>
        {
            ["browser.userAgent"] = ua,
            ["browser.platform"] = "Win32",
            ["browser.uach.platform"] = "Windows",
            ["browser.uach.arch"] = "x86",
            ["browser.uach.model"] = "",
            ["device.viewport.width"] = 1920,
            ["device.viewport.height"] = 1080,
            ["system.locale"] = locale ?? "zh-CN",
            ["system.timezone"] = "Asia/Shanghai"
        };
        return new UAResult(ua, related);
    }

    private static T Pick<T>(IReadOnlyList<T> items, Random rnd) => items[rnd.Next(items.Count)];
}
