using System;

namespace FishBrowser.WPF.Models;

public class LogEntry
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Level { get; set; } = "INFO"; // DEBUG, INFO, WARN, ERROR
    public string Source { get; set; } = string.Empty; // Scraper, AI, DB, System
    public string Message { get; set; } = string.Empty;
    public string? StackTrace { get; set; }
    public int? TaskId { get; set; }
    public int? ArticleId { get; set; }
}
