using System;
using System.Threading.Tasks;
using FishBrowser.WPF.Models;

namespace FishBrowser.WPF.Services;

/// <summary>
/// 浏览器控制器接口 - 统一 Playwright 和 WebView2 的控制 API
/// </summary>
public interface IBrowserController : IAsyncDisposable
{
    /// <summary>
    /// 初始化浏览器
    /// </summary>
    Task InitializeAsync(FingerprintProfile? fingerprint = null, ProxyServer? proxy = null, bool headless = false, string? userDataPath = null);

    /// <summary>
    /// 导航到指定 URL
    /// </summary>
    Task NavigateAsync(string url, int timeoutMs = 30000);

    /// <summary>
    /// 点击元素
    /// </summary>
    Task ClickAsync(string selector, int timeoutMs = 30000);

    /// <summary>
    /// 填充表单字段
    /// </summary>
    Task FillAsync(string selector, string value, int timeoutMs = 30000);

    /// <summary>
    /// 逐字输入文本（模拟真实输入）
    /// </summary>
    Task TypeAsync(string selector, string text, int delayMs = 100, int timeoutMs = 30000);

    /// <summary>
    /// 等待选择器出现
    /// </summary>
    Task WaitForSelectorAsync(string selector, int timeoutMs = 30000);

    /// <summary>
    /// 等待页面加载完成
    /// </summary>
    Task WaitForLoadStateAsync(string state = "networkidle", int timeoutMs = 30000);

    /// <summary>
    /// 获取页面 HTML 内容
    /// </summary>
    Task<string> GetContentAsync();

    /// <summary>
    /// 获取元素文本内容
    /// </summary>
    Task<string> GetTextContentAsync(string selector);

    /// <summary>
    /// 获取元素属性
    /// </summary>
    Task<string?> GetAttributeAsync(string selector, string attribute);

    /// <summary>
    /// 截图
    /// </summary>
    Task<byte[]> ScreenshotAsync(string? filePath = null);

    /// <summary>
    /// 元素截图
    /// </summary>
    Task<byte[]> ScreenshotElementAsync(string selector);

    /// <summary>
    /// 执行 JavaScript
    /// </summary>
    Task<object?> EvaluateAsync(string script);

    /// <summary>
    /// 执行 JavaScript 并返回类型化结果
    /// </summary>
    Task<T?> EvaluateAsync<T>(string script);

    /// <summary>
    /// 当前 URL
    /// </summary>
    string? CurrentUrl { get; }

    /// <summary>
    /// 是否已初始化
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// 控制器类型（用于日志和调试）
    /// </summary>
    string ControllerType { get; }

    /// <summary>
    /// Console 消息事件
    /// </summary>
    event EventHandler<ConsoleMessageEventArgs>? ConsoleMessage;

    /// <summary>
    /// 请求发送事件
    /// </summary>
    event EventHandler<RequestEventArgs>? RequestSent;

    /// <summary>
    /// 响应接收事件
    /// </summary>
    event EventHandler<ResponseEventArgs>? ResponseReceived;

    /// <summary>
    /// 页面加载完成事件
    /// </summary>
    event EventHandler<string>? PageLoaded;
}

/// <summary>
/// Console 消息事件参数
/// </summary>
public class ConsoleMessageEventArgs : EventArgs
{
    public string Type { get; set; } = "log"; // log, warn, error, info
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;
}

/// <summary>
/// 请求事件参数
/// </summary>
public class RequestEventArgs : EventArgs
{
    public string Url { get; set; } = string.Empty;
    public string Method { get; set; } = "GET";
    public DateTime Timestamp { get; set; } = DateTime.Now;
}

/// <summary>
/// 响应事件参数
/// </summary>
public class ResponseEventArgs : EventArgs
{
    public string Url { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;
}
