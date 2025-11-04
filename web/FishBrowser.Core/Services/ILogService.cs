using System.Collections.Generic;
using FishBrowser.WPF.Models;

namespace FishBrowser.WPF.Services;

public interface ILogService
{
    void Log(string level, string source, string message, string? stackTrace = null, int? taskId = null, int? articleId = null);
    void LogInfo(string source, string message);
    void LogWarn(string source, string message);
    void LogError(string source, string message, string? stackTrace = null);

    List<LogEntry> GetLogs(int limit = 100);
    List<LogEntry> GetLogsByLevel(string level, int limit = 100);
}
