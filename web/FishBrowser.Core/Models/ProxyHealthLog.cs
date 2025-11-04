using System;

namespace FishBrowser.WPF.Models;

public class ProxyHealthLog
{
    public long Id { get; set; }
    public int ProxyServerId { get; set; }
    public string TestType { get; set; } = "http"; // http|https|geo|ipv6|dns
    public bool Success { get; set; }
    public int LatencyMs { get; set; }
    public string? ErrorCode { get; set; }
    public string? IPDetected { get; set; }
    public string? CountryDetected { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // 导航属性
    public ProxyServer? ProxyServer { get; set; }
}
