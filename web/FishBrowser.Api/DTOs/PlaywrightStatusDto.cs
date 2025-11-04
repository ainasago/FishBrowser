using System.Collections.Generic;

namespace FishBrowser.Api.DTOs;

public class PlaywrightStatusDto
{
    public bool IsInstalled { get; set; }
    public string? InstallPath { get; set; }
    public List<string> BrowserList { get; set; } = new();
    public string? PackageVersion { get; set; }
    public string? CliVersion { get; set; }
    public string VersionDisplay { get; set; } = string.Empty;
}
