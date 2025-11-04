using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Serilog;
using FishBrowser.WPF.Data;
using FishBrowser.WPF.Models;
using ILogger = Serilog.ILogger;

namespace FishBrowser.WPF.Services;

public class LogService : ILogService
{
    private readonly WebScraperDbContext _dbContext;
    private static ILogger? _logger;
    private static readonly object _lockObject = new object();

    public LogService(WebScraperDbContext dbContext)
    {
        _dbContext = dbContext;
        
        // 初始化 Serilog（仅一次）
        if (_logger == null)
        {
            lock (_lockObject)
            {
                if (_logger == null)
                {
                    InitializeSerilog();
                }
            }
        }
    }

    private static void InitializeSerilog()
    {
        // 设置日志文件路径：项目根目录下的 logs 文件夹
        var currentDir = Directory.GetCurrentDirectory();
        var logsDir = Path.Combine(currentDir, "logs");
        
        // 如果在 bin 目录中，向上回溯到项目根目录
        if (currentDir.Contains("bin"))
        {
            var projectRoot = Directory.GetParent(currentDir)?.Parent?.Parent?.FullName 
                ?? currentDir;
            logsDir = Path.Combine(projectRoot, "logs");
        }
        
        // 创建 logs 目录（如果不存在）
        if (!Directory.Exists(logsDir))
        {
            Directory.CreateDirectory(logsDir);
        }
        
        // 配置 Serilog
        var logFilePath = Path.Combine(logsDir, "webscraper.log");
        
        _logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                logFilePath,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
                rollingInterval: RollingInterval.Day,
                fileSizeLimitBytes: 10485760,  // 10MB per file
                retainedFileCountLimit: 30,
                shared: true,  // 允许多个进程写入同一文件
                rollOnFileSizeLimit: true  // 文件大小达到限制时轮转
            )
            .CreateLogger();
    }

    public void Log(string level, string source, string message, string? stackTrace = null, int? taskId = null, int? articleId = null)
    {
        try
        {
            // 使用本地时间而非 UTC，保持与 Serilog 文件日志一致
            var logEntry = new LogEntry
            {
                Timestamp = DateTime.Now,
                Level = level,
                Source = source,
                Message = message,
                StackTrace = stackTrace,
                TaskId = taskId,
                ArticleId = articleId
            };

            _dbContext.LogEntries.Add(logEntry);
            _dbContext.SaveChanges();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to save log to database: {ex.Message}");
        }

        // 使用 Serilog 写入日志
        if (_logger != null)
        {
            var logContext = _logger.ForContext("SourceContext", source);
            
            switch (level.ToUpper())
            {
                case "INFO":
                    logContext.Information(message);
                    break;
                case "WARN":
                    logContext.Warning(message);
                    break;
                case "ERROR":
                    if (!string.IsNullOrEmpty(stackTrace))
                    {
                        logContext.Error(new Exception(stackTrace), message);
                    }
                    else
                    {
                        logContext.Error(message);
                    }
                    break;
                default:
                    logContext.Debug(message);
                    break;
            }
        }
    }

    public void LogInfo(string source, string message) => Log("INFO", source, message);
    public void LogWarn(string source, string message) => Log("WARN", source, message);
    public void LogError(string source, string message, string? stackTrace = null) => Log("ERROR", source, message, stackTrace);

    public List<LogEntry> GetLogs(int limit = 100)
    {
        try
        {
            return _dbContext.LogEntries
                .OrderByDescending(l => l.Timestamp)
                .Take(limit)
                .ToList();
        }
        catch (Exception ex)
        {
            // SQLite 日期解析错误时，返回空列表
            Console.WriteLine($"Failed to load logs: {ex.Message}");
            return new List<LogEntry>();
        }
    }

    public List<LogEntry> GetLogsByLevel(string level, int limit = 100)
    {
        try
        {
            return _dbContext.LogEntries
                .Where(l => l.Level == level)
                .OrderByDescending(l => l.Timestamp)
                .Take(limit)
                .ToList();
        }
        catch (Exception ex)
        {
            // SQLite 日期解析错误时，返回空列表
            Console.WriteLine($"Failed to load logs by level: {ex.Message}");
            return new List<LogEntry>();
        }
    }
}
