# AI 脚本调试工作台 - 架构设计

## 📅 创建日期
2025-10-31

## 🏗️ 系统架构

### 分层设计

```
┌─────────────────────────────────────────────────────────────┐
│                      Presentation Layer                      │
│  ┌──────────────────────────────────────────────────────┐   │
│  │         AIDebugWorkbench.xaml (WPF View)             │   │
│  │  ┌──────────┬──────────────────┬──────────────────┐  │   │
│  │  │  YAML    │   WebView2       │   AI Chat        │  │   │
│  │  │  Editor  │   Browser        │   Panel          │  │   │
│  │  └──────────┴──────────────────┴──────────────────┘  │   │
│  └──────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│                      Service Layer                           │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │ DslExecutor  │  │ AIDebugger   │  │ Recorder     │      │
│  │   Service    │  │   Service    │  │   Service    │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│                    Browser Abstraction Layer                 │
│  ┌──────────────────────────────────────────────────────┐   │
│  │              IBrowserController (Interface)          │   │
│  ├──────────────────────────────────────────────────────┤   │
│  │  ┌────────────────────┐  ┌──────────────────────┐   │   │
│  │  │ PlaywrightController│  │ WebView2Controller   │   │   │
│  │  │  (Production)       │  │  (Debug Mode)        │   │   │
│  │  └────────────────────┘  └──────────────────────┘   │   │
│  └──────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│                      Browser Engines                         │
│  ┌────────────────────┐  ┌──────────────────────────────┐   │
│  │  Playwright        │  │  WebView2 (Edge Chromium)    │   │
│  │  (Chromium/FF/WK)  │  │  + DevTools Protocol         │   │
│  └────────────────────┘  └──────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

---

## 🧩 核心组件

### 1. AIDebugWorkbench (View)

**职责**：主 UI 容器，协调三栏交互

**组件**：
- `YamlEditorPanel`：左侧 YAML 编辑器
  - TextBox + 语法高亮（AvalonEdit 或自定义）
  - 实时校验（调用 `DslParser`）
  - 错误/警告标记
  - 当前执行行高亮
- `BrowserPanel`：中间浏览器面板
  - WebView2 控件
  - 工具栏：拾取、录制、截图、刷新
  - 覆盖层：元素高亮、选择器显示
- `AIChatPanel`：右侧 AI 助手
  - 对话历史（ScrollViewer + ItemsControl）
  - 上下文卡片（截图、日志、错误）
  - 输入框 + 发送按钮
  - 操作按钮：应用补丁、重新生成

**关键方法**：
```csharp
public partial class AIDebugWorkbench : Page
{
    private WebView2Controller _browserController;
    private DslExecutor _executor;
    private AIDebuggerService _aiDebugger;
    private RecorderService _recorder;
    
    // 运行整个脚本
    private async Task RunScript();
    
    // 单步执行
    private async Task StepNext();
    
    // 停止执行
    private void StopExecution();
    
    // 开启选择器拾取模式
    private void StartPickerMode();
    
    // 开启录制模式
    private void StartRecording();
    
    // 与 AI 分享当前上下文
    private async Task ShareContextWithAI();
    
    // 应用 AI 建议的补丁
    private void ApplyAIPatch(string yamlPatch);
}
```

---

### 2. IBrowserController (Interface)

**职责**：统一浏览器控制 API，支持多种后端

**接口定义**：
```csharp
public interface IBrowserController : IAsyncDisposable
{
    // 初始化
    Task InitializeAsync(FingerprintProfile? fingerprint = null, bool headless = false);
    
    // 导航
    Task NavigateAsync(string url, int timeoutMs = 30000);
    
    // 元素交互
    Task ClickAsync(string selector, int timeoutMs = 30000);
    Task FillAsync(string selector, string value, int timeoutMs = 30000);
    Task TypeAsync(string selector, string text, int delayMs = 100);
    
    // 等待
    Task WaitForSelectorAsync(string selector, int timeoutMs = 30000);
    Task WaitForNavigationAsync(int timeoutMs = 30000);
    Task WaitForLoadStateAsync(LoadState state = LoadState.NetworkIdle);
    
    // 内容获取
    Task<string> GetContentAsync();
    Task<string> GetTextContentAsync(string selector);
    Task<string> GetAttributeAsync(string selector, string attribute);
    
    // 截图
    Task<byte[]> ScreenshotAsync(string? filePath = null);
    Task<byte[]> ScreenshotElementAsync(string selector);
    
    // 脚本执行
    Task<object?> EvaluateAsync(string script);
    Task<T?> EvaluateAsync<T>(string script);
    
    // 事件
    event EventHandler<ConsoleMessageEventArgs> ConsoleMessage;
    event EventHandler<RequestEventArgs> RequestSent;
    event EventHandler<ResponseEventArgs> ResponseReceived;
    event EventHandler<string> PageLoaded;
    
    // 状态
    string? CurrentUrl { get; }
    bool IsInitialized { get; }
}
```

---

### 3. WebView2Controller (Implementation)

**职责**：在 WPF WebView2 控件中实现 IBrowserController

**核心实现**：
```csharp
public class WebView2Controller : IBrowserController
{
    private readonly WebView2 _webView;
    private readonly ILogService _logger;
    
    public WebView2Controller(WebView2 webView, ILogService logger)
    {
        _webView = webView;
        _logger = logger;
    }
    
    public async Task InitializeAsync(FingerprintProfile? fingerprint = null, bool headless = false)
    {
        await _webView.EnsureCoreWebView2Async();
        
        // 订阅事件
        _webView.CoreWebView2.ConsoleMessageReceived += OnConsoleMessage;
        _webView.CoreWebView2.WebResourceRequested += OnResourceRequested;
        
        // 注入指纹脚本（如果提供）
        if (fingerprint != null)
        {
            var script = new FingerprintManager().GenerateInjectionScript(fingerprint);
            await _webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(script);
        }
        
        _logger.LogInfo("WebView2Controller", "Initialized");
    }
    
    public async Task NavigateAsync(string url, int timeoutMs = 30000)
    {
        var tcs = new TaskCompletionSource<bool>();
        var cts = new CancellationTokenSource(timeoutMs);
        
        void OnNavigationCompleted(object? s, CoreWebView2NavigationCompletedEventArgs e)
        {
            _webView.CoreWebView2.NavigationCompleted -= OnNavigationCompleted;
            tcs.TrySetResult(e.IsSuccess);
        }
        
        _webView.CoreWebView2.NavigationCompleted += OnNavigationCompleted;
        _webView.CoreWebView2.Navigate(url);
        
        await tcs.Task;
    }
    
    public async Task ClickAsync(string selector, int timeoutMs = 30000)
    {
        // 使用 DevTools Protocol 或 JS 执行
        var script = $@"
            (function() {{
                const el = document.querySelector('{selector}');
                if (!el) throw new Error('Element not found: {selector}');
                el.click();
                return true;
            }})();
        ";
        await EvaluateAsync(script);
    }
    
    public async Task FillAsync(string selector, string value, int timeoutMs = 30000)
    {
        var escapedValue = value.Replace("'", "\\'");
        var script = $@"
            (function() {{
                const el = document.querySelector('{selector}');
                if (!el) throw new Error('Element not found: {selector}');
                el.value = '{escapedValue}';
                el.dispatchEvent(new Event('input', {{ bubbles: true }}));
                el.dispatchEvent(new Event('change', {{ bubbles: true }}));
                return true;
            }})();
        ";
        await EvaluateAsync(script);
    }
    
    public async Task<byte[]> ScreenshotAsync(string? filePath = null)
    {
        // WebView2 截图需要使用 DevTools Protocol
        var result = await _webView.CoreWebView2.CallDevToolsProtocolMethodAsync(
            "Page.captureScreenshot",
            "{\"format\":\"png\",\"quality\":90}"
        );
        
        var json = JsonDocument.Parse(result);
        var base64 = json.RootElement.GetProperty("data").GetString();
        var bytes = Convert.FromBase64String(base64!);
        
        if (!string.IsNullOrEmpty(filePath))
        {
            await File.WriteAllBytesAsync(filePath, bytes);
        }
        
        return bytes;
    }
    
    public async Task<T?> EvaluateAsync<T>(string script)
    {
        var result = await _webView.CoreWebView2.ExecuteScriptAsync(script);
        return JsonSerializer.Deserialize<T>(result);
    }
    
    // ... 其他方法实现
}
```

---

### 4. AIDebuggerService

**职责**：构建调试上下文，与 AI 交互，解析 AI 建议

**核心方法**：
```csharp
public class AIDebuggerService
{
    private readonly AIClientService _aiClient;
    private readonly ILogService _logger;
    
    // 构建调试上下文
    public async Task<DebugContext> BuildContextAsync(
        DslFlow flow,
        int failedStepIndex,
        Exception error,
        IBrowserController browser,
        List<string> recentLogs)
    {
        var screenshot = await browser.ScreenshotAsync();
        var dom = await GetDOMSummaryAsync(browser);
        
        return new DebugContext
        {
            FlowName = flow.Name,
            FailedStep = flow.Steps[failedStepIndex],
            FailedStepIndex = failedStepIndex,
            ErrorMessage = error.Message,
            ErrorStack = error.StackTrace,
            CurrentUrl = browser.CurrentUrl,
            Screenshot = screenshot,
            DOMSummary = dom,
            RecentLogs = recentLogs.TakeLast(50).ToList(),
            PreviousSteps = flow.Steps.Take(failedStepIndex).ToList(),
            NextSteps = flow.Steps.Skip(failedStepIndex + 1).Take(2).ToList()
        };
    }
    
    // 请求 AI 修复建议
    public async Task<AIDebugSuggestion> GetFixSuggestionAsync(DebugContext context)
    {
        var prompt = BuildDebugPrompt(context);
        var response = await _aiClient.SendMessageAsync(prompt);
        
        return ParseAISuggestion(response);
    }
    
    // 应用 YAML 补丁
    public string ApplyYamlPatch(string originalYaml, string patch)
    {
        // 简单实现：替换特定行或步骤
        // 复杂实现：使用 YAML diff/patch 库
        return patch; // 临时返回整个补丁
    }
    
    private string BuildDebugPrompt(DebugContext context)
    {
        return $@"
你是一个网页自动化脚本调试专家。用户的 DSL 脚本在执行时失败了，请分析问题并提供修复建议。

**任务信息**：
- 任务名称：{context.FlowName}
- 当前 URL：{context.CurrentUrl}

**失败步骤**（第 {context.FailedStepIndex + 1} 步）：
```yaml
{SerializeStep(context.FailedStep)}
```

**错误信息**：
{context.ErrorMessage}

**最近日志**（最后 10 行）：
{string.Join("\n", context.RecentLogs.TakeLast(10))}

**页面状态**：
- 截图已提供（见附件）
- DOM 摘要：{context.DOMSummary}

**请提供**：
1. 问题诊断（为什么失败）
2. 修复建议（如何修改 DSL）
3. 如果需要更换选择器，提供新的 selector 建议

**输出格式**（JSON）：
{{
  ""diagnosis"": ""问题原因分析"",
  ""suggestion"": ""修复建议说明"",
  ""patch"": ""修复后的完整 YAML 脚本"",
  ""selectorSuggestions"": [
    {{ ""type"": ""css"", ""value"": ""..."", ""confidence"": 0.9 }}
  ]
}}
";
    }
}
```

---

### 5. RecorderService

**职责**：录制用户在浏览器中的操作，生成 DSL

**核心逻辑**：
```csharp
public class RecorderService
{
    private readonly IBrowserController _browser;
    private readonly List<RecordedAction> _actions = new();
    private bool _isRecording;
    
    public async Task StartRecordingAsync()
    {
        _isRecording = true;
        _actions.Clear();
        
        // 注入录制脚本
        await _browser.EvaluateAsync(@"
            window.__recorder = {
                actions: [],
                record: function(type, data) {
                    this.actions.push({ type, data, timestamp: Date.now() });
                    window.chrome.webview.postMessage({ 
                        type: 'recorded_action', 
                        action: { type, data } 
                    });
                }
            };
            
            // 监听点击
            document.addEventListener('click', (e) => {
                const selector = getSelector(e.target);
                window.__recorder.record('click', { selector });
            }, true);
            
            // 监听输入
            document.addEventListener('input', (e) => {
                if (e.target.tagName === 'INPUT' || e.target.tagName === 'TEXTAREA') {
                    const selector = getSelector(e.target);
                    window.__recorder.record('fill', { 
                        selector, 
                        value: e.target.value 
                    });
                }
            }, true);
            
            // 监听导航
            const originalPushState = history.pushState;
            history.pushState = function() {
                originalPushState.apply(this, arguments);
                window.__recorder.record('navigate', { url: location.href });
            };
            
            function getSelector(el) {
                // 生成稳健的选择器
                if (el.id) return '#' + el.id;
                if (el.getAttribute('data-testid')) 
                    return '[data-testid=""' + el.getAttribute('data-testid') + '""]';
                // ... 更多策略
                return getCssPath(el);
            }
        ");
    }
    
    public async Task<string> StopRecordingAsync()
    {
        _isRecording = false;
        
        // 转换为 DSL
        var dsl = ConvertActionsToDsl(_actions);
        return dsl;
    }
    
    private string ConvertActionsToDsl(List<RecordedAction> actions)
    {
        var sb = new StringBuilder();
        sb.AppendLine("dslVersion: v1.0");
        sb.AppendLine($"id: recorded_{DateTime.Now:yyyyMMddHHmmss}");
        sb.AppendLine("name: 录制的任务");
        sb.AppendLine("steps:");
        
        foreach (var action in actions)
        {
            sb.AppendLine($"  - type: {action.Type}");
            if (action.Type == "navigate")
            {
                sb.AppendLine($"    url: {action.Data["url"]}");
            }
            else if (action.Type == "click")
            {
                sb.AppendLine($"    selector:");
                sb.AppendLine($"      type: css");
                sb.AppendLine($"      value: {action.Data["selector"]}");
            }
            else if (action.Type == "fill")
            {
                sb.AppendLine($"    selector:");
                sb.AppendLine($"      type: css");
                sb.AppendLine($"      value: {action.Data["selector"]}");
                sb.AppendLine($"    value: {action.Data["value"]}");
            }
        }
        
        return sb.ToString();
    }
}
```

---

## 🔄 数据流

### 执行流程
```
用户点击"运行"
    ↓
AIDebugWorkbench.RunScript()
    ↓
DslParser.ValidateAndParseAsync(yaml)
    ↓
DslExecutor.ExecuteAsync(flow, webView2Controller, progress)
    ↓
[每个步骤]
    ↓
WebView2Controller.ClickAsync/FillAsync/...
    ↓
WebView2 执行 JS → 页面更新
    ↓
[如果失败]
    ↓
AIDebuggerService.BuildContextAsync(...)
    ↓
AIDebuggerService.GetFixSuggestionAsync(context)
    ↓
显示 AI 建议 → 用户确认
    ↓
ApplyYamlPatch(yaml, patch)
    ↓
重新运行
```

### 录制流程
```
用户点击"录制"
    ↓
RecorderService.StartRecordingAsync()
    ↓
注入监听脚本到 WebView2
    ↓
用户在浏览器中操作
    ↓
JS 捕获事件 → postMessage 到 .NET
    ↓
RecorderService 收集 actions
    ↓
用户点击"停止"
    ↓
RecorderService.StopRecordingAsync()
    ↓
ConvertActionsToDsl(actions)
    ↓
YAML 显示在左侧编辑器
```

---

## 📦 文件结构

```
WebScraperApp/
├── Views/
│   ├── AIDebugWorkbench.xaml
│   ├── AIDebugWorkbench.xaml.cs
│   └── Controls/
│       ├── YamlEditorControl.xaml
│       ├── BrowserPanelControl.xaml
│       └── AIChatPanelControl.xaml
├── Services/
│   ├── IBrowserController.cs
│   ├── WebView2Controller.cs
│   ├── AIDebuggerService.cs
│   └── RecorderService.cs
├── Models/
│   ├── DebugContext.cs
│   ├── AIDebugSuggestion.cs
│   └── RecordedAction.cs
└── Assets/
    └── Scripts/
        ├── selector-picker.js
        ├── recorder.js
        └── element-highlighter.js
```

---

## 🔧 配置与扩展

### 配置项
```json
{
  "debugger": {
    "maxScreenshotSize": 512000,
    "maxDOMTextLength": 51200,
    "maxLogLines": 100,
    "enableDataSanitization": true,
    "sanitizationRules": [
      { "pattern": "password", "action": "redact" },
      { "pattern": "token", "action": "redact" }
    ]
  }
}
```

### 扩展点
- **自定义选择器策略**：实现 `ISelectorStrategy`
- **自定义录制过滤器**：实现 `IRecordingFilter`
- **AI 提示词模板**：可配置的 Prompt 模板

---

**状态**：设计完成
**下一步**：开始实现 M1 基础功能
