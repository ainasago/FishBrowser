using System;

namespace FishBrowser.WPF.Models;

/// <summary>
/// 单个校验检查结果
/// </summary>
public class ValidationCheckResult
{
    public string CheckName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;  // consistency | realism | cloudflare_risk
    public bool Passed { get; set; }
    public int Score { get; set; } = 0;  // 0-100
    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }
    public int Weight { get; set; } = 100;
}
