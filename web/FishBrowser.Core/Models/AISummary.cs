using System;

namespace FishBrowser.WPF.Models;

public class AISummary
{
    public int Id { get; set; }
    public int ArticleId { get; set; }
    public string Summary { get; set; } = string.Empty;
    public int? SummaryLength { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public string? Model { get; set; }

    // 导航属性
    public Article? Article { get; set; }
}
