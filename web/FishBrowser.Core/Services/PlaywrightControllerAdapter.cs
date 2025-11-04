using System;
using System.Threading.Tasks;
using Microsoft.Playwright;
using FishBrowser.WPF.Engine;
using FishBrowser.WPF.Models;

namespace FishBrowser.WPF.Services;

/// <summary>
/// 适配器：将 PlaywrightController 适配为 IBrowserController
/// </summary>
public sealed class PlaywrightControllerAdapter : IBrowserController
{
    private readonly PlaywrightController _inner;

    public PlaywrightControllerAdapter(PlaywrightController inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public string? CurrentUrl => null; // 可按需扩展从 _inner 获取
    public bool IsInitialized => true; // 由 TaskTestRunner 保证初始化
    public string ControllerType => "Playwright";

    public event EventHandler<ConsoleMessageEventArgs>? ConsoleMessage;
    public event EventHandler<RequestEventArgs>? RequestSent;
    public event EventHandler<ResponseEventArgs>? ResponseReceived;
    public event EventHandler<string>? PageLoaded;

    public async Task InitializeAsync(FingerprintProfile? fingerprint = null, ProxyServer? proxy = null, bool headless = false, string? userDataPath = null)
    {
        if (fingerprint == null) throw new ArgumentNullException(nameof(fingerprint));
        await _inner.InitializeBrowserAsync(fingerprint, proxy, headless, userDataPath);
    }

    public async Task NavigateAsync(string url, int timeoutMs = 30000)
    {
        await _inner.NavigateAsync(url, timeoutMs);
        PageLoaded?.Invoke(this, url);
    }

    public async Task ClickAsync(string selector, int timeoutMs = 30000)
    {
        await _inner.ClickAsync(selector, timeoutMs);
    }

    public async Task FillAsync(string selector, string value, int timeoutMs = 30000)
    {
        await _inner.FillAsync(selector, value, timeoutMs);
    }

    public async Task TypeAsync(string selector, string text, int delayMs = 100, int timeoutMs = 30000)
    {
        // PlaywrightController 未提供 TypeAsync，退化为 FillAsync
        await _inner.FillAsync(selector, text, timeoutMs);
    }

    public async Task WaitForSelectorAsync(string selector, int timeoutMs = 30000)
    {
        await _inner.WaitForSelectorAsync(selector, timeoutMs);
    }

    public async Task WaitForLoadStateAsync(string state = "networkidle", int timeoutMs = 30000)
    {
        await _inner.WaitForLoadStateAsync(state, timeoutMs);
    }

    public async Task<string> GetContentAsync()
    {
        return await _inner.GetPageContentAsync();
    }

    public async Task<string> GetTextContentAsync(string selector)
    {
        var js = $"(function(){{ const el = document.querySelector('{Escape(selector)}'); return el ? el.textContent : ''; }})()";
        var obj = await _inner.EvaluateAsync(js);
        return obj?.ToString() ?? string.Empty;
    }

    public async Task<string?> GetAttributeAsync(string selector, string attribute)
    {
        var js = $"(function(){{ const el = document.querySelector('{Escape(selector)}'); return el ? el.getAttribute('{attribute}') : null; }})()";
        var obj = await _inner.EvaluateAsync(js);
        var s = obj?.ToString();
        return string.IsNullOrEmpty(s) ? null : s;
    }

    public async Task<byte[]> ScreenshotAsync(string? filePath = null)
    {
        // PlaywrightController.ScreenshotAsync 需要路径；若未提供，则创建临时文件
        var path = filePath ?? System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"pw_{DateTime.Now:yyyyMMdd_HHmmssfff}.png");
        return await _inner.ScreenshotAsync(path);
    }

    public async Task<byte[]> ScreenshotElementAsync(string selector)
    {
        // 简化：整页截图（PlaywrightController 未提供元素截图）
        // 可扩展为裁剪元素区域。
        return await ScreenshotAsync();
    }

    public async Task<object?> EvaluateAsync(string script)
    {
        return await _inner.EvaluateAsync(script);
    }

    public async Task<T?> EvaluateAsync<T>(string script)
    {
        var obj = await _inner.EvaluateAsync(script);
        if (obj == null) return default;
        try { return (T)Convert.ChangeType(obj, typeof(T)); }
        catch { return default; }
    }

    public async ValueTask DisposeAsync()
    {
        await _inner.DisposeAsync();
    }

    private static string Escape(string s) => s.Replace("'", "\\'");
}
