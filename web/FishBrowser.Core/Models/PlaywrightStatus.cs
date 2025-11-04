using System;
using System.Collections.Generic;

namespace FishBrowser.WPF.Models;

public class PlaywrightStatus
{
    public bool IsInstalled { get; set; }
    public string? InstallPath { get; set; }
    public List<string> BrowserList { get; set; } = new();
    public string? PackageVersion { get; set; }
    public string? CliVersion { get; set; }
    public string VersionDisplay
    {
        get
        {
            if (!string.IsNullOrEmpty(PackageVersion) && !string.IsNullOrEmpty(CliVersion))
                return $"{PackageVersion} (CLI: {CliVersion})";
            if (!string.IsNullOrEmpty(PackageVersion))
                return $"{PackageVersion} (CLI 未安装)";
            if (!string.IsNullOrEmpty(CliVersion))
                return CliVersion;
            return "无法获取版本信息";
        }
    }
}
