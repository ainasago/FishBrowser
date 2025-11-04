using System;

namespace FishBrowser.WPF.Models;

public class AIClassification
{
    public int Id { get; set; }
    public int ArticleId { get; set; }
    public string Category { get; set; } = string.Empty;
    public double Confidence { get; set; } = 0.0;
    public string? Keywords { get; set; } // JSON array
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public string? Model { get; set; }

    // 导航属性
    public Article? Article { get; set; }
}
