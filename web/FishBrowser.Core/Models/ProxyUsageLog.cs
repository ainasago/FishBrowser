using System;

namespace FishBrowser.WPF.Models;

public class ProxyUsageLog
{
    public long Id { get; set; }
    public int ProxyServerId { get; set; }
    public int? EnvironmentId { get; set; }
    public string? TargetDomain { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
    public bool Success { get; set; }
    public int? StatusCode { get; set; }
    public long? BytesSent { get; set; }
    public long? BytesRecv { get; set; }

    // 导航属性
    public ProxyServer? ProxyServer { get; set; }
    public BrowserEnvironment? Environment { get; set; }
}
