using System;

namespace FishBrowser.WPF.Models;

public class Article
{
    public int Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Author { get; set; }
    public DateTime? PublishedAt { get; set; }
    public string RawHtml { get; set; } = string.Empty;
    public DateTime ScrapedAt { get; set; } = DateTime.UtcNow;
    public int? FingerprintProfileId { get; set; }
    public int? ProxyServerId { get; set; }

    // 导航属性
    public FingerprintProfile? FingerprintProfile { get; set; }
    public ProxyServer? ProxyServer { get; set; }
    public AISummary? AISummary { get; set; }
    public AIClassification? AIClassification { get; set; }
}
