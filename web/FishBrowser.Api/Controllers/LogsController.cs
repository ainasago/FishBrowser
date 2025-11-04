using Microsoft.AspNetCore.Mvc;

namespace FishBrowser.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LogsController : ControllerBase
{
    private readonly ILogger<LogsController> _logger;
    private readonly IConfiguration _configuration;

    public LogsController(ILogger<LogsController> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    [HttpGet]
    public IActionResult GetLogs([FromQuery] int limit = 500, [FromQuery] int lines = 100)
    {
        try
        {
            // 使用 lines 参数，如果没有指定则使用 limit
            var maxLines = lines > 0 ? lines : limit;
            
            // 从配置读取日志路径
            var logPath = Path.Combine(Directory.GetCurrentDirectory(), "logs");
            var logs = new List<object>();

            if (Directory.Exists(logPath))
            {
                var logFiles = Directory.GetFiles(logPath, "*.log")
                    .OrderByDescending(f => System.IO.File.GetLastWriteTime(f))
                    .Take(5);

                foreach (var file in logFiles)
                {
                    try
                    {
                        // 使用 FileShare.ReadWrite 允许其他进程写入
                        using (var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        using (var reader = new StreamReader(fileStream))
                        {
                            var allLines = new List<string>();
                            string? line;
                            while ((line = reader.ReadLine()) != null)
                            {
                                allLines.Add(line);
                            }

                            var logLines = allLines.TakeLast(maxLines);

                            foreach (var logLine in logLines)
                            {
                                if (string.IsNullOrWhiteSpace(logLine)) continue;

                                // 解析日志行格式: [2025-11-03 21:50:00] [INF] [Category] Message
                                var timestamp = "";
                                var level = "INFO";
                                var message = logLine;

                                try
                                {
                                    // 提取时间戳 [2025-11-03 21:50:00]
                                    var timestampMatch = System.Text.RegularExpressions.Regex.Match(logLine, @"\[(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2})\]");
                                    if (timestampMatch.Success)
                                    {
                                        timestamp = timestampMatch.Groups[1].Value;

                                        // 提取日志级别 [INF], [ERR], [WRN]
                                        var levelMatch = System.Text.RegularExpressions.Regex.Match(logLine, @"\]\s*\[([A-Z]{3})\]");
                                        if (levelMatch.Success)
                                        {
                                            var levelCode = levelMatch.Groups[1].Value;
                                            level = levelCode switch
                                            {
                                                "INF" => "INFO",
                                                "ERR" => "ERROR",
                                                "WRN" => "WARN",
                                                _ => "INFO"
                                            };
                                        }

                                        // 提取消息（去掉时间戳和级别部分）
                                        var messageStart = logLine.IndexOf("] [", logLine.IndexOf("] [") + 1) + 3;
                                        if (messageStart > 2)
                                        {
                                            message = logLine.Substring(messageStart).TrimEnd(']');
                                        }
                                    }
                                }
                                catch
                                {
                                    // 如果解析失败，使用原始行
                                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                                    message = logLine;
                                }

                                logs.Add(new
                                {
                                    timestamp,
                                    level,
                                    message,
                                    source = "API"
                                });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error reading log file: {File}", file);
                    }
                }
            }

            // 如果没有日志，返回提示
            if (logs.Count == 0)
            {
                logs.Add(new
                {
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    level = "WARN",
                    message = $"未找到日志文件。日志路径: {logPath}"
                });
            }

            return Ok(new
            {
                success = true,
                data = logs.OrderBy(l => ((dynamic)l).timestamp).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting logs");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }
}
