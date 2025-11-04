using System;

namespace FishBrowser.WPF.Application.DTOs;

/// <summary>
/// 爬虫任务数据传输对象
/// </summary>
public class TaskDTO
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Url { get; set; }
    public string Status { get; set; }
    public int FingerprintProfileId { get; set; }
    public int? ProxyServerId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int ArticlesCount { get; set; }
    public string ErrorMessage { get; set; }
}
