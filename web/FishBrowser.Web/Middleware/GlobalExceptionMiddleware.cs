using System.Net;
using System.Text.Json;

namespace FishBrowser.Web.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Web 全局异常: {Message}, Path: {Path}", ex.Message, context.Request.Path);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        // 如果是 AJAX 请求，返回 JSON
        if (context.Request.Headers["X-Requested-With"] == "XMLHttpRequest" || 
            context.Request.Path.StartsWithSegments("/api") ||
            context.Request.Headers.Accept.ToString().Contains("application/json"))
        {
            var response = new
            {
                success = false,
                error = exception.Message,
                type = exception.GetType().Name,
                stackTrace = exception.StackTrace
            };

            var jsonResponse = JsonSerializer.Serialize(response);
            return context.Response.WriteAsync(jsonResponse);
        }
        else
        {
            // 普通请求，返回错误页面
            context.Response.ContentType = "text/html";
            var html = $@"
<!DOCTYPE html>
<html>
<head>
    <title>错误</title>
    <style>
        body {{ font-family: Arial, sans-serif; padding: 40px; background: #f5f5f5; }}
        .error-container {{ background: white; padding: 30px; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        h1 {{ color: #d32f2f; }}
        .error-details {{ background: #f9f9f9; padding: 15px; border-left: 4px solid #d32f2f; margin-top: 20px; }}
        pre {{ overflow-x: auto; }}
    </style>
</head>
<body>
    <div class='error-container'>
        <h1>❌ 服务器错误</h1>
        <p><strong>错误类型:</strong> {exception.GetType().Name}</p>
        <p><strong>错误消息:</strong> {exception.Message}</p>
        <div class='error-details'>
            <strong>堆栈跟踪:</strong>
            <pre>{exception.StackTrace}</pre>
        </div>
        <p style='margin-top: 20px;'>
            <a href='javascript:history.back()'>← 返回上一页</a> | 
            <a href='/'>返回首页</a>
        </p>
    </div>
</body>
</html>";
            return context.Response.WriteAsync(html);
        }
    }
}
