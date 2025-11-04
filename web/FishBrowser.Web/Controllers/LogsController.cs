using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;

namespace FishBrowser.Web.Controllers;

public class LogsController : Controller
{
    private readonly ILogger<LogsController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public LogsController(ILogger<LogsController> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    private HttpClient GetAuthenticatedClient()
    {
        var client = _httpClientFactory.CreateClient("FishBrowserApi");
        var token = HttpContext.Session.GetString("Token");
        
        if (!string.IsNullOrEmpty(token))
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        
        return client;
    }

    public IActionResult Index()
    {
        try
        {
            var token = HttpContext.Session.GetString("Token");
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Login", "Auth");
            }

            ViewBag.Username = HttpContext.Session.GetString("Username");
            ViewBag.Role = HttpContext.Session.GetString("Role");

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading logs page");
            return RedirectToAction("Login", "Auth");
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetLogs(string source = "web", int lines = 100)
    {
        try
        {
            var token = HttpContext.Session.GetString("Token");
            if (string.IsNullOrEmpty(token))
            {
                return Unauthorized();
            }

            if (source == "api")
            {
                // 调用 API 获取 API 端日志
                var client = GetAuthenticatedClient();
                var response = await client.GetAsync($"/api/logs?lines={lines}");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return Content(content, "application/json");
                }

                return StatusCode((int)response.StatusCode, new 
                { 
                    success = false, 
                    error = "无法从 API 获取日志" 
                });
            }
            else
            {
                // 读取 Web 端本地日志
                var logs = ReadWebLogs(lines);
                return Ok(new { success = true, data = logs });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting logs");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    private List<object> ReadWebLogs(int maxLines = 100)
    {
        var logs = new List<object>();
        var logDirectory = Path.Combine(Directory.GetCurrentDirectory(), "logs");

        if (!Directory.Exists(logDirectory))
        {
            return logs;
        }

        try
        {
            // 获取最新的日志文件
            var logFiles = Directory.GetFiles(logDirectory, "*.log")
                .OrderByDescending(f => System.IO.File.GetLastWriteTime(f))
                .Take(3); // 读取最近3个日志文件

            foreach (var file in logFiles)
            {
                var lines = System.IO.File.ReadAllLines(file)
                    .Reverse()
                    .Take(maxLines)
                    .Reverse();

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    // 解析日志行
                    var parts = line.Split('|');
                    if (parts.Length >= 3)
                    {
                        logs.Add(new
                        {
                            timestamp = parts[0].Trim(),
                            level = parts[1].Trim(),
                            message = string.Join("|", parts.Skip(2)).Trim(),
                            source = "Web"
                        });
                    }
                    else
                    {
                        logs.Add(new
                        {
                            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                            level = "INFO",
                            message = line,
                            source = "Web"
                        });
                    }
                }

                if (logs.Count >= maxLines)
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading web logs");
        }

        return logs.OrderByDescending(l => ((dynamic)l).timestamp).Take(maxLines).ToList();
    }
}
