using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Wpf;
using Microsoft.Web.WebView2.Core;
using FishBrowser.WPF.Models;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace FishBrowser.WPF.Services;

/// <summary>
/// WebView2 浏览器控制器 - 用于调试模式的内嵌浏览器
/// </summary>
public class WebView2Controller : IBrowserController
{
    private readonly WebView2 _webView;
    private readonly ILogService _logger;
    private bool _isInitialized;

    public WebView2Controller(WebView2 webView, ILogService logger)
    {
        _webView = webView ?? throw new ArgumentNullException(nameof(webView));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string? CurrentUrl => _webView.Source?.ToString();
    public bool IsInitialized => _isInitialized;
    public string ControllerType => "WebView2";

    public event EventHandler<ConsoleMessageEventArgs>? ConsoleMessage;
    public event EventHandler<RequestEventArgs>? RequestSent;
    public event EventHandler<ResponseEventArgs>? ResponseReceived;
    public event EventHandler<string>? PageLoaded;

    public async Task InitializeAsync(FingerprintProfile? fingerprint = null, ProxyServer? proxy = null, bool headless = false, string? userDataPath = null)
    {
        try
        {
            _logger.LogInfo("WebView2Controller", "Initializing WebView2...");

            // 确保 CoreWebView2 已初始化
            //await _webView.EnsureCoreWebView2Async();

            // 订阅事件
            _webView.CoreWebView2.NavigationCompleted += OnNavigationCompleted;
            _webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;

            // Console 消息
            _webView.CoreWebView2.WebResourceRequested += (s, e) =>
            {
                RequestSent?.Invoke(this, new RequestEventArgs
                {
                    Url = e.Request.Uri,
                    Method = e.Request.Method,
                    Timestamp = DateTime.Now
                });
            };

            // 注入指纹脚本（如果提供）
            if (fingerprint != null)
            {
                var injectionScript = GenerateFingerprintScript(fingerprint);
                await _webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(injectionScript);
                _logger.LogInfo("WebView2Controller", "Fingerprint script injected");
            }

            _isInitialized = true;
            _logger.LogInfo("WebView2Controller", "WebView2 initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError("WebView2Controller", $"Failed to initialize: {ex.Message}", ex.StackTrace);
            throw;
        }
    }

    public async Task NavigateAsync(string url, int timeoutMs = 30000)
    {
        try
        {
            _logger.LogInfo("WebView2Controller", $"Navigating to: {url}");

            var tcs = new TaskCompletionSource<bool>();
            var cts = new System.Threading.CancellationTokenSource(timeoutMs);

            void OnCompleted(object? s, CoreWebView2NavigationCompletedEventArgs e)
            {
                _webView.CoreWebView2.NavigationCompleted -= OnCompleted;
                tcs.TrySetResult(e.IsSuccess);
            }

            _webView.CoreWebView2.NavigationCompleted += OnCompleted;
            _webView.CoreWebView2.Navigate(url);

            var success = await tcs.Task;
            if (!success)
            {
                throw new Exception("Navigation failed");
            }

            _logger.LogInfo("WebView2Controller", $"Navigation completed: {url}");
        }
        catch (Exception ex)
        {
            _logger.LogError("WebView2Controller", $"Navigation failed: {ex.Message}", ex.StackTrace);
            throw;
        }
    }

    public async Task ClickAsync(string selector, int timeoutMs = 30000)
    {
        try
        {
            _logger.LogInfo("WebView2Controller", $"Clicking: {selector}");

            var script = $@"
                (function() {{
                    const el = document.querySelector('{EscapeSelector(selector)}');
                    if (!el) throw new Error('Element not found: {selector}');
                    el.click();
                    return true;
                }})();
            ";

            await EvaluateAsync(script);
            _logger.LogInfo("WebView2Controller", $"Click completed: {selector}");
        }
        catch (Exception ex)
        {
            _logger.LogError("WebView2Controller", $"Click failed: {ex.Message}", ex.StackTrace);
            throw;
        }
    }

    public async Task FillAsync(string selector, string value, int timeoutMs = 30000)
    {
        try
        {
            _logger.LogInfo("WebView2Controller", $"Filling: {selector}");

            var escapedValue = value.Replace("'", "\\'").Replace("\n", "\\n");
            var script = $@"
                (function() {{
                    const el = document.querySelector('{EscapeSelector(selector)}');
                    if (!el) throw new Error('Element not found: {selector}');
                    el.value = '{escapedValue}';
                    el.dispatchEvent(new Event('input', {{ bubbles: true }}));
                    el.dispatchEvent(new Event('change', {{ bubbles: true }}));
                    return true;
                }})();
            ";

            await EvaluateAsync(script);
            _logger.LogInfo("WebView2Controller", $"Fill completed: {selector}");
        }
        catch (Exception ex)
        {
            _logger.LogError("WebView2Controller", $"Fill failed: {ex.Message}", ex.StackTrace);
            throw;
        }
    }

    public async Task TypeAsync(string selector, string text, int delayMs = 100, int timeoutMs = 30000)
    {
        try
        {
            _logger.LogInfo("WebView2Controller", $"Typing: {selector}");

            foreach (var ch in text)
            {
                var script = $@"
                    (function() {{
                        const el = document.querySelector('{EscapeSelector(selector)}');
                        if (!el) throw new Error('Element not found: {selector}');
                        el.value += '{ch}';
                        el.dispatchEvent(new Event('input', {{ bubbles: true }}));
                        return true;
                    }})();
                ";
                await EvaluateAsync(script);
                await Task.Delay(delayMs);
            }

            _logger.LogInfo("WebView2Controller", $"Type completed: {selector}");
        }
        catch (Exception ex)
        {
            _logger.LogError("WebView2Controller", $"Type failed: {ex.Message}", ex.StackTrace);
            throw;
        }
    }

    public async Task WaitForSelectorAsync(string selector, int timeoutMs = 30000)
    {
        try
        {
            _logger.LogInfo("WebView2Controller", $"Waiting for selector: {selector}");

            var script = $@"
                new Promise((resolve, reject) => {{
                    const timeout = setTimeout(() => reject(new Error('Timeout')), {timeoutMs});
                    const check = () => {{
                        const el = document.querySelector('{EscapeSelector(selector)}');
                        if (el && el.offsetParent !== null) {{
                            clearTimeout(timeout);
                            resolve(true);
                        }} else {{
                            setTimeout(check, 100);
                        }}
                    }};
                    check();
                }});
            ";

            await EvaluateAsync(script);
            _logger.LogInfo("WebView2Controller", $"Selector found: {selector}");
        }
        catch (Exception ex)
        {
            _logger.LogError("WebView2Controller", $"Wait for selector failed: {ex.Message}", ex.StackTrace);
            throw;
        }
    }

    public async Task WaitForLoadStateAsync(string state = "networkidle", int timeoutMs = 30000)
    {
        try
        {
            _logger.LogInfo("WebView2Controller", $"Waiting for load state: {state}");

            if (state == "networkidle")
            {
                // 简单实现：等待一段时间
                await Task.Delay(1000);
            }

            _logger.LogInfo("WebView2Controller", $"Load state reached: {state}");
        }
        catch (Exception ex)
        {
            _logger.LogError("WebView2Controller", $"Wait for load state failed: {ex.Message}", ex.StackTrace);
            throw;
        }
    }

    public async Task<string> GetContentAsync()
    {
        try
        {
            var script = "document.documentElement.outerHTML";
            var result = await EvaluateAsync<string>(script);
            return result ?? "";
        }
        catch (Exception ex)
        {
            _logger.LogError("WebView2Controller", $"Get content failed: {ex.Message}", ex.StackTrace);
            throw;
        }
    }

    public async Task<string> GetTextContentAsync(string selector)
    {
        try
        {
            var script = $@"
                (function() {{
                    const el = document.querySelector('{EscapeSelector(selector)}');
                    return el ? el.textContent : '';
                }})();
            ";
            var result = await EvaluateAsync<string>(script);
            return result ?? "";
        }
        catch (Exception ex)
        {
            _logger.LogError("WebView2Controller", $"Get text content failed: {ex.Message}", ex.StackTrace);
            throw;
        }
    }

    public async Task<string?> GetAttributeAsync(string selector, string attribute)
    {
        try
        {
            var script = $@"
                (function() {{
                    const el = document.querySelector('{EscapeSelector(selector)}');
                    return el ? el.getAttribute('{attribute}') : null;
                }})();
            ";
            return await EvaluateAsync<string>(script);
        }
        catch (Exception ex)
        {
            _logger.LogError("WebView2Controller", $"Get attribute failed: {ex.Message}", ex.StackTrace);
            throw;
        }
    }

    public async Task<byte[]> ScreenshotAsync(string? filePath = null)
    {
        try
        {
            _logger.LogInfo("WebView2Controller", "Taking screenshot...");

            var result = await _webView.CoreWebView2.CallDevToolsProtocolMethodAsync(
                "Page.captureScreenshot",
                "{\"format\":\"png\",\"quality\":90}"
            );

            var json = JsonDocument.Parse(result);
            var base64 = json.RootElement.GetProperty("data").GetString();
            var bytes = Convert.FromBase64String(base64!);

            if (!string.IsNullOrEmpty(filePath))
            {
                await System.IO.File.WriteAllBytesAsync(filePath, bytes);
                _logger.LogInfo("WebView2Controller", $"Screenshot saved: {filePath}");
            }

            return bytes;
        }
        catch (Exception ex)
        {
            _logger.LogError("WebView2Controller", $"Screenshot failed: {ex.Message}", ex.StackTrace);
            throw;
        }
    }

    public async Task<byte[]> ScreenshotElementAsync(string selector)
    {
        try
        {
            _logger.LogInfo("WebView2Controller", $"Taking element screenshot: {selector}");

            // 获取元素位置和大小
            var script = $@"
                (function() {{
                    const el = document.querySelector('{EscapeSelector(selector)}');
                    if (!el) throw new Error('Element not found');
                    const rect = el.getBoundingClientRect();
                    return {{
                        x: rect.left,
                        y: rect.top,
                        width: rect.width,
                        height: rect.height
                    }};
                }})();
            ";

            var rectJson = await EvaluateAsync<string>(script);
            var rect = JsonSerializer.Deserialize<JsonElement>(rectJson!);

            var parameters = JsonSerializer.Serialize(new
            {
                format = "png",
                quality = 90,
                clip = new
                {
                    x = rect.GetProperty("x").GetDouble(),
                    y = rect.GetProperty("y").GetDouble(),
                    width = rect.GetProperty("width").GetDouble(),
                    height = rect.GetProperty("height").GetDouble(),
                    scale = 1
                }
            });

            var result = await _webView.CoreWebView2.CallDevToolsProtocolMethodAsync(
                "Page.captureScreenshot",
                parameters
            );

            var json = JsonDocument.Parse(result);
            var base64 = json.RootElement.GetProperty("data").GetString();
            return Convert.FromBase64String(base64!);
        }
        catch (Exception ex)
        {
            _logger.LogError("WebView2Controller", $"Element screenshot failed: {ex.Message}", ex.StackTrace);
            throw;
        }
    }

    public async Task<object?> EvaluateAsync(string script)
    {
        try
        {
            var result = await _webView.CoreWebView2.ExecuteScriptAsync(script);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError("WebView2Controller", $"Evaluate failed: {ex.Message}", ex.StackTrace);
            throw;
        }
    }

    public async Task<T?> EvaluateAsync<T>(string script)
    {
        try
        {
            var result = await _webView.CoreWebView2.ExecuteScriptAsync(script);
            if (string.IsNullOrEmpty(result) || result == "null")
            {
                return default;
            }

            // 移除 JSON 字符串的引号
            if (typeof(T) == typeof(string) && result.StartsWith("\"") && result.EndsWith("\""))
            {
                result = result.Substring(1, result.Length - 2);
                result = result.Replace("\\\"", "\"").Replace("\\n", "\n");
                return (T)(object)result;
            }

            return JsonSerializer.Deserialize<T>(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("WebView2Controller", $"Evaluate<T> failed: {ex.Message}", ex.StackTrace);
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_webView.CoreWebView2 != null)
            {
                _webView.CoreWebView2.NavigationCompleted -= OnNavigationCompleted;
                _webView.CoreWebView2.WebMessageReceived -= OnWebMessageReceived;
            }

            _logger.LogInfo("WebView2Controller", "Disposed");
        }
        catch (Exception ex)
        {
            _logger.LogError("WebView2Controller", $"Dispose failed: {ex.Message}", ex.StackTrace);
        }

        await Task.CompletedTask;
    }

    private void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (e.IsSuccess)
        {
            PageLoaded?.Invoke(this, CurrentUrl ?? "");
            _logger.LogInfo("WebView2Controller", $"Page loaded: {CurrentUrl}");
        }
    }

    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            var message = e.TryGetWebMessageAsString();
            _logger.LogInfo("WebView2Controller", $"Web message received: {message}");
        }
        catch (Exception ex)
        {
            _logger.LogError("WebView2Controller", $"Web message handling failed: {ex.Message}", ex.StackTrace);
        }
    }

    private string EscapeSelector(string selector)
    {
        return selector.Replace("'", "\\'");
    }

    private string GenerateFingerprintScript(FingerprintProfile fingerprint)
    {
        // 简化的指纹注入脚本
        return $@"
            (function() {{
                // User-Agent 已由浏览器设置
                
                // Navigator 属性
                Object.defineProperty(navigator, 'platform', {{
                    get: () => '{fingerprint.Platform}'
                }});
                
                Object.defineProperty(navigator, 'language', {{
                    get: () => '{fingerprint.Locale}'
                }});
                
                Object.defineProperty(navigator, 'languages', {{
                    get: () => ['{fingerprint.Locale}', 'en-US', 'en']
                }});
                
                // 隐藏 webdriver
                Object.defineProperty(navigator, 'webdriver', {{
                    get: () => false
                }});
                
                console.log('Fingerprint injected: {fingerprint.Name}');
            }})();
        ";
    }
}
