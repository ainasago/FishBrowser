using System;
using System.Collections.Generic;
using System.Linq;
using FishBrowser.WPF.Data;
using FishBrowser.WPF.Models;

namespace FishBrowser.WPF.Services;

public class MetaValidationService
{
    private readonly WebScraperDbContext _db;

    public MetaValidationService(WebScraperDbContext db)
    {
        _db = db;
    }

    public ValidationReport ValidateMetaProfile(FingerprintMetaProfile meta)
    {
        var report = new ValidationReport();
        var traits = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object?>>(meta.TraitsJson ?? "{}") ?? new();

        // 必需键（首版简单规则）
        var requiredKeys = new[]
        {
            "browser.userAgent",
            "system.locale",
            "system.timezone"
        };
        foreach (var key in requiredKeys)
        {
            if (!traits.ContainsKey(key))
                report.Missing.Add(key);
        }

        // 一致性：locale 与 timezone（简单映射）
        if (traits.TryGetValue("system.locale", out var locObj))
        {
            var loc = locObj?.ToString();
            if (loc == "zh-CN" && traits.TryGetValue("system.timezone", out var tzObj))
            {
                var tz = tzObj?.ToString();
                if (tz != "Asia/Shanghai")
                    report.Conflicts.Add("locale zh-CN 应配对 Asia/Shanghai timezone");
            }
        }

        // UA 与 UA-CH Platform 简易检查
        var platform = traits.TryGetValue("browser.platform", out var pf) ? pf?.ToString() : null;
        var uachPlatform = traits.TryGetValue("browser.uach.platform", out var up) ? up?.ToString() : null;
        if (!string.IsNullOrWhiteSpace(uachPlatform))
        {
            if (uachPlatform == "Windows" && platform != "Win32")
                report.Conflicts.Add("UA-CH Platform 为 Windows，建议 platform=Win32");
            if (uachPlatform == "macOS" && platform != "MacIntel")
                report.Conflicts.Add("UA-CH Platform 为 macOS，建议 platform=MacIntel");
        }

        return report;
    }
}

public class ValidationReport
{
    public List<string> Missing { get; } = new();
    public List<string> Conflicts { get; } = new();
    public bool IsOk => Missing.Count == 0 && Conflicts.Count == 0;
}
