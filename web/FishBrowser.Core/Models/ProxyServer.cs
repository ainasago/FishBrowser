using System;
using System.Collections.Generic;

namespace FishBrowser.WPF.Models;

public class ProxyServer
{
    public int Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Protocol { get; set; } = "http"; // http, https, socks5
    public string? Username { get; set; }
    public string? PasswordEncrypted { get; set; }
    public string? Provider { get; set; }
    public int? PoolId { get; set; }
    public string? TagsJson { get; set; }
    public string? Type { get; set; } // datacenter/residential/mobile
    public string? Country { get; set; }
    public string? Region { get; set; }
    public string? City { get; set; }
    public string? ISP { get; set; }
    public bool SupportsHttps { get; set; } = true;
    public bool SupportsIPv6 { get; set; } = false;
    public string? Anonymity { get; set; } // elite/anonymous/transparent
    public bool StickySupported { get; set; } = false;
    public string? StickyKey { get; set; }
    public int RotationSeconds { get; set; } = 0;
    public int MaxConcurrency { get; set; } = 1;
    public int CurrentConcurrency { get; set; } = 0;
    public bool Enabled { get; set; } = true;
    public double Score { get; set; } = 0;
    public long SuccessCount { get; set; } = 0;
    public long FailCount { get; set; } = 0;
    public string Status { get; set; } = "Online"; // Online, Offline, Checking
    public DateTime? LastUsedAt { get; set; }
    public DateTime? LastFailureAt { get; set; }
    public DateTime? LastCheckedAt { get; set; }
    public int ResponseTimeMs { get; set; } = 0;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // 导航属性
    public ProxyPool? Pool { get; set; }
    public ICollection<ProxyHealthLog> HealthLogs { get; set; } = new List<ProxyHealthLog>();
    public ICollection<ProxyUsageLog> UsageLogs { get; set; } = new List<ProxyUsageLog>();
    public ICollection<ScrapingTask> ScrapingTasks { get; set; } = new List<ScrapingTask>();
    public ICollection<Article> Articles { get; set; } = new List<Article>();
}
