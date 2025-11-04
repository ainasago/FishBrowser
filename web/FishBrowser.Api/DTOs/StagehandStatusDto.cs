namespace FishBrowser.Api.DTOs;

public class StagehandStatusDto
{
    public bool IsNodeInstalled { get; set; }
    public string? NodeVersion { get; set; }
    public string? NpmVersion { get; set; }
    public bool IsInstalled { get; set; }
    public string? InstallPath { get; set; }
    public string? InstalledVersion { get; set; }
    public string? LatestVersion { get; set; }
    public bool PlaywrightInstalled { get; set; }
    public string VersionDisplay { get; set; } = "";
    public bool HasUpdate { get; set; }
}
