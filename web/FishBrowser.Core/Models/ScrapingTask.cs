using System;

namespace FishBrowser.WPF.Models;

public class ScrapingTask
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? DslScript { get; set; }
    public TaskStatus Status { get; set; } = TaskStatus.Pending;
    public int FingerprintProfileId { get; set; }
    public int? ProxyServerId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int RetryCount { get; set; } = 0;
    public string? ErrorMessage { get; set; }
    public int? ArticleId { get; set; }

    // 导航属性
    public FingerprintProfile? FingerprintProfile { get; set; }
    public ProxyServer? ProxyServer { get; set; }
    public Article? Article { get; set; }
}

/// <summary>
/// 任务状态枚举
/// </summary>
public enum TaskStatus
{
    Draft,      // 草稿
    Pending,    // 待执行
    Running,    // 执行中
    Completed,  // 已完成
    Failed,     // 失败
    Cancelled   // 已取消
}
