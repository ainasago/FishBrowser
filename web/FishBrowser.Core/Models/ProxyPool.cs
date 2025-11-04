using System;
using System.Collections.Generic;

namespace FishBrowser.WPF.Models;

public class ProxyPool
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Strategy { get; set; } = "random"; // random|weighted|round_robin|lowest_latency|geo_match
    public string? GeoPref { get; set; } // CN/US/...
    public int MaxRetries { get; set; } = 2;
    public int CooldownSeconds { get; set; } = 30;
    public int ConcurrencyCap { get; set; } = 100;
    public string? BypassDomainsJson { get; set; }
    public bool RequireHttps { get; set; } = false;
    public bool Enabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // 导航属性
    public ICollection<ProxyServer> Proxies { get; set; } = new List<ProxyServer>();
}
