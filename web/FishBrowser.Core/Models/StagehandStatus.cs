using System.Collections.Generic;

namespace FishBrowser.WPF.Models;

/// <summary>
/// Stagehand 状态信息
/// </summary>
public class StagehandStatus
{
    /// <summary>
    /// Node.js 是否已安装
    /// </summary>
    public bool IsNodeInstalled { get; set; }

    /// <summary>
    /// Node.js 版本
    /// </summary>
    public string? NodeVersion { get; set; }

    /// <summary>
    /// npm 版本
    /// </summary>
    public string? NpmVersion { get; set; }

    /// <summary>
    /// Stagehand 是否已安装
    /// </summary>
    public bool IsInstalled { get; set; }

    /// <summary>
    /// 安装路径
    /// </summary>
    public string? InstallPath { get; set; }

    /// <summary>
    /// 已安装的版本
    /// </summary>
    public string? InstalledVersion { get; set; }

    /// <summary>
    /// 最新可用版本
    /// </summary>
    public string? LatestVersion { get; set; }

    /// <summary>
    /// Playwright 是否已安装（Stagehand 的依赖）
    /// </summary>
    public bool PlaywrightInstalled { get; set; }

    /// <summary>
    /// 版本显示文本
    /// </summary>
    public string VersionDisplay
    {
        get
        {
            if (!IsInstalled) return "未安装";
            if (string.IsNullOrEmpty(InstalledVersion)) return "已安装（版本未知）";
            if (!string.IsNullOrEmpty(LatestVersion) && InstalledVersion != LatestVersion)
            {
                return $"{InstalledVersion} (最新: {LatestVersion})";
            }
            return InstalledVersion;
        }
    }

    /// <summary>
    /// 是否有可用更新
    /// </summary>
    public bool HasUpdate => IsInstalled && !string.IsNullOrEmpty(LatestVersion) && InstalledVersion != LatestVersion;
}
