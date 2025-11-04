using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Web.WebView2.Wpf;
using FishBrowser.WPF.Models;
using FishBrowser.WPF.Services;

namespace FishBrowser.WPF.Views;

public partial class AIDebugWorkbench : Page
{
    private readonly ILogService? _logger;
    private IBrowserController? _browserController;
    private WebView2? _webView;
    private RecorderService? _recorderService;
    private bool _isRunning;
    private bool _isPickerActive;
    private int _currentStep;
    private int _totalSteps;
    private DateTime _executionStartTime;
    private int _logTotalCount;
    private int _logSuccessCount;
    private int _logErrorCount;
    
    // 单步调试相关
    private bool _isStepMode;
    private DslFlow? _currentFlow;
    private System.Threading.CancellationTokenSource? _stepCts;

    public AIDebugWorkbench()
    {
        InitializeComponent();

        // 获取日志服务
        var host = App.Current.Resources["Host"] as IHost;
        _logger = host?.Services.GetService<ILogService>();

        // 创建录制服务
        if (_logger != null)
        {
            _recorderService = new RecorderService(_logger);
        }

        _logger?.LogInfo("AIDebugWorkbench", "Workbench initialized");

        // 初始化日志统计
        _logTotalCount = 0;
        _logSuccessCount = 0;
        _logErrorCount = 0;
        
        // 初始化单步调试
        _isStepMode = false;
        _currentStep = 0;
        _totalSteps = 0;

        // 初始化
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            _logger?.LogInfo("AIDebugWorkbench", "Workbench loaded, initializing browser...");

            // 创建 WebView2 控件
            _webView = new WebView2
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            // 清空容器并添加 WebView2
            BrowserContainer.Children.Clear();
            BrowserContainer.Children.Add(_webView);

            // 创建 WebView2Controller
            if (_logger != null)
            {
                _browserController = new WebView2Controller(_webView, _logger);
                await _browserController.InitializeAsync();

                // 订阅事件
                _browserController.PageLoaded += OnPageLoaded;
                _browserController.ConsoleMessage += OnConsoleMessage;

                // 订阅 WebView2 消息（用于选择器拾取和录制）
                _webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;

                // 启用 Console API 以接收 console.log
                await _webView.CoreWebView2.CallDevToolsProtocolMethodAsync("Runtime.enable", "{}");
                
                // 订阅 Console 消息
                _webView.CoreWebView2.GetDevToolsProtocolEventReceiver("Runtime.consoleAPICalled")
                    .DevToolsProtocolEventReceived += (s, e) =>
                    {
                        try
                        {
                            var json = System.Text.Json.JsonDocument.Parse(e.ParameterObjectAsJson);
                            var type = json.RootElement.GetProperty("type").GetString();
                            var args = json.RootElement.GetProperty("args");
                            
                            if (args.GetArrayLength() > 0)
                            {
                                var message = args[0].GetProperty("value").GetString();
                                _logger?.LogInfo("Browser Console", $"[{type}] {message}");
                            }
                        }
                        catch { }
                    };

                BrowserStatus.Text = "浏览器就绪";
                _logger.LogInfo("AIDebugWorkbench", "Browser initialized successfully");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError("AIDebugWorkbench", $"Failed to initialize: {ex.Message}", ex.StackTrace);
            MessageBox.Show($"初始化失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnPageLoaded(object? sender, string url)
    {
        Dispatcher.Invoke(() =>
        {
            UrlBox.Text = url;
            BrowserStatus.Text = $"已加载: {url}";
            _logger?.LogInfo("AIDebugWorkbench", $"Page loaded event: {url}");
        });
    }

    private void OnConsoleMessage(object? sender, ConsoleMessageEventArgs e)
    {
        _logger?.LogInfo("AIDebugWorkbench", $"Console [{e.Type}]: {e.Message}");
    }

    private void OnWebMessageReceived(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            var message = e.TryGetWebMessageAsString();
            _logger?.LogInfo("AIDebugWorkbench", $"Web message received: {message}");

            var data = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(message);
            var type = data.GetProperty("type").GetString();

            Dispatcher.Invoke(() =>
            {
                switch (type)
                {
                    case "selector_picked":
                        HandleSelectorPicked(data);
                        break;
                    case "picker_cancelled":
                        HandlePickerCancelled();
                        break;
                    case "action_recorded":
                    case "recording_started":
                    case "recording_stopped":
                        _recorderService?.HandleBrowserMessage(message);
                        if (type == "recording_stopped")
                        {
                            HandleRecordingStopped();
                        }
                        break;
                }
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError("AIDebugWorkbench", $"Failed to handle web message: {ex.Message}", ex.StackTrace);
        }
    }

    private void HandleSelectorPicked(System.Text.Json.JsonElement data)
    {
        try
        {
            var selectorData = data.GetProperty("selector");
            var selectorType = selectorData.GetProperty("type").GetString();
            var selectorValue = selectorData.GetProperty("value").GetString();

            var yaml = YamlEditor.Text ?? "";
            var selectorYaml = $"\n  - selector: {selectorType}:{selectorValue}\n";

            // 在光标位置插入或追加到末尾
            var cursorPosition = YamlEditor.CaretIndex;
            if (cursorPosition > 0)
            {
                YamlEditor.Text = yaml.Insert(cursorPosition, selectorYaml);
            }
            else
            {
                YamlEditor.Text = yaml + selectorYaml;
            }

            YamlStatus.Text = $"已插入选择器: {selectorType}:{selectorValue}";
            _logger?.LogInfo("AIDebugWorkbench", $"Selector inserted: {selectorType}:{selectorValue}");
        }
        catch (Exception ex)
        {
            _logger?.LogError("AIDebugWorkbench", $"Failed to handle selector: {ex.Message}", ex.StackTrace);
            MessageBox.Show($"处理选择器失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void HandlePickerCancelled()
    {
        _isPickerActive = false;
        PickerButton.Content = "🎯 拾取选择器";
        BrowserStatus.Text = "选择器拾取已取消";
        _logger?.LogInfo("AIDebugWorkbench", "Selector picker cancelled");
    }

    private void HandleRecordingStopped()
    {
        if (_recorderService == null) return;

        try
        {
            var dsl = _recorderService.ConvertToDsl();
            YamlEditor.Text = dsl;
            YamlStatus.Text = $"录制完成，共 {_recorderService.ActionCount} 个动作";
            
            MessageBox.Show($"录制完成！\n共捕获 {_recorderService.ActionCount} 个操作", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            
            _logger?.LogInfo("AIDebugWorkbench", $"Recording completed with {_recorderService.ActionCount} actions");
        }
        catch (Exception ex)
        {
            _logger?.LogError("AIDebugWorkbench", $"Failed to convert recording: {ex.Message}", ex.StackTrace);
            MessageBox.Show($"转换录制失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #region 工具栏按钮事件

    private async void Run_Click(object sender, RoutedEventArgs e)
    {
        if (_isRunning)
        {
            MessageBox.Show("脚本正在运行中", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var yaml = YamlEditor.Text;
        if (string.IsNullOrWhiteSpace(yaml) || yaml.Contains("在此编辑"))
        {
            MessageBox.Show("请先编辑 DSL 脚本", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            _isRunning = true;
            _executionStartTime = DateTime.Now;
            UpdateControlStates();

            _logger?.LogInfo("AIDebugWorkbench", "Starting script execution");
            // 解析 DSL
            var parser = new DslParser(_logger);
            var (valid, flow, error) = await parser.ValidateAndParseAsync(yaml);
            
            if (!valid)
            {
                MessageBox.Show($"DSL 验证失败：{error}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                _logger?.LogError("AIDebugWorkbench", $"DSL validation failed: {error}", "");
                return;
            }

            if (flow == null)
            {
                MessageBox.Show("DSL 解析失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 清空日志
            AppendLog("========== 开始执行 ==========", LogLevel.Info);
            AppendLog($"DSL: {flow.Name ?? flow.Id}", LogLevel.Info);
            AppendLog($"步骤总数: {flow.Steps?.Count ?? 0}", LogLevel.Info);

            // 创建进度报告器
            var progress = new Progress<TestProgress>(p =>
            {
                Dispatcher.Invoke(() =>
                {
                    BrowserStatus.Text = p.Message;
                    StepCounter.Text = $"步骤: {p.CurrentStep}/{p.TotalSteps}";
                    
                    var elapsed = DateTime.Now - _executionStartTime;
                    ExecutionTime.Text = $"执行时间: {elapsed.TotalSeconds:F1}s";

                    // 同步到日志面板
                    AppendLog(p.Message, p.Level);
                });
            });

            // 执行 DSL
            var executor = new DslExecutor(_logger);
            var cts = new System.Threading.CancellationTokenSource();
            
            await executor.ExecuteAsync(flow, _browserController, progress, cts.Token);

            var totalTime = DateTime.Now - _executionStartTime;
            BrowserStatus.Text = $"✓ 执行完成 ({totalTime.TotalSeconds:F1}s)";
            _logger?.LogInfo("AIDebugWorkbench", $"Script execution completed in {totalTime.TotalSeconds:F1}s");
            AppendLog($"========== 执行完成 ({totalTime.TotalSeconds:F1}s) ==========", LogLevel.Info);
        }
        catch (Exception ex)
        {
            _logger?.LogError("AIDebugWorkbench", $"Execution failed: {ex.Message}", ex.StackTrace);
            AppendLog($"执行失败: {ex.Message}", LogLevel.Error);
            MessageBox.Show($"执行失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _isRunning = false;
            UpdateControlStates();
        }
    }

    private async void Step_Click(object sender, RoutedEventArgs e)
    {
        // 如果没有在单步模式，先进入单步模式
        if (!_isStepMode)
        {
            await StartStepMode();
            return;
        }
        
        // 如果已在单步模式，执行下一步
        await ExecuteNextStep();
    }

    private void Stop_Click(object sender, RoutedEventArgs e)
    {
        // 停止执行
        _stepCts?.Cancel();
        _isRunning = false;
        _isStepMode = false;
        _currentFlow = null;
        _currentStep = 0;
        
        UpdateControlStates();
        BrowserStatus.Text = "已停止";
        AppendLog("========== 执行已停止 ==========", LogLevel.Warning);
        
        _logger?.LogInfo("AIDebugWorkbench", "Execution stopped");
    }

    private async Task StartStepMode()
    {
        var yaml = YamlEditor.Text;
        if (string.IsNullOrWhiteSpace(yaml) || yaml.Contains("在此编辑"))
        {
            MessageBox.Show("请先编辑 DSL 脚本", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            _logger?.LogInfo("AIDebugWorkbench", "Starting step mode");

            // 解析 DSL
            var parser = new DslParser(_logger);
            var (valid, flow, error) = await parser.ValidateAndParseAsync(yaml);
            
            if (!valid)
            {
                MessageBox.Show($"DSL 验证失败：{error}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                _logger?.LogError("AIDebugWorkbench", $"DSL validation failed: {error}", "");
                return;
            }

            if (flow == null || flow.Steps == null || flow.Steps.Count == 0)
            {
                MessageBox.Show("DSL 解析失败或没有步骤", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 进入单步模式
            _isStepMode = true;
            _isRunning = true;
            _currentFlow = flow;
            _currentStep = 0;
            _totalSteps = flow.Steps.Count;
            _executionStartTime = DateTime.Now;
            _stepCts = new System.Threading.CancellationTokenSource();

            UpdateControlStates();
            
            // 清空日志
            AppendLog("========== 单步调试模式 ==========", LogLevel.Info);
            AppendLog($"DSL: {flow.Name ?? flow.Id}", LogLevel.Info);
            AppendLog($"步骤总数: {_totalSteps}", LogLevel.Info);
            AppendLog("点击 [⏭️ 单步] 按钮执行下一步", LogLevel.Info);

            BrowserStatus.Text = $"单步模式：准备执行第 1/{_totalSteps} 步";
            StepCounter.Text = $"步骤: 0/{_totalSteps}";
            StepButton.Content = "⏭️ 下一步";
        }
        catch (Exception ex)
        {
            _logger?.LogError("AIDebugWorkbench", $"Start step mode failed: {ex.Message}", ex.StackTrace);
            MessageBox.Show($"启动单步模式失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task ExecuteNextStep()
    {
        if (_currentFlow == null || _currentFlow.Steps == null || _stepCts == null)
        {
            MessageBox.Show("单步模式未初始化", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (_currentStep >= _totalSteps)
        {
            // 所有步骤已完成
            var totalTime = DateTime.Now - _executionStartTime;
            BrowserStatus.Text = $"✓ 单步调试完成 ({totalTime.TotalSeconds:F1}s)";
            AppendLog($"========== 单步调试完成 ({totalTime.TotalSeconds:F1}s) ==========", LogLevel.Info);
            
            _isStepMode = false;
            _isRunning = false;
            _currentFlow = null;
            _currentStep = 0;
            StepButton.Content = "⏭️ 单步";
            
            UpdateControlStates();
            MessageBox.Show("所有步骤已执行完成", "完成", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var step = _currentFlow.Steps[_currentStep];
            var stepNum = _currentStep + 1;

            var stepDesc = GetStepDescription(step);
            BrowserStatus.Text = $"执行步骤 {stepNum}/{_totalSteps}: {stepDesc}";
            StepCounter.Text = $"步骤: {stepNum}/{_totalSteps}";
            
            AppendLog($"[步骤 {stepNum}] {stepDesc}", LogLevel.Info);
            _logger?.LogInfo("AIDebugWorkbench", $"Step {stepNum}: {stepDesc}");

            // 执行单个步骤
            var executor = new DslExecutor(_logger);
            var singleStepFlow = new DslFlow
            {
                DslVersion = _currentFlow.DslVersion,
                Id = _currentFlow.Id,
                Name = _currentFlow.Name,
                Steps = new System.Collections.Generic.List<DslStep> { step }
            };

            await executor.ExecuteAsync(singleStepFlow, _browserController, null, _stepCts.Token);

            AppendLog($"✓ 步骤 {stepNum} 完成", LogLevel.Info);
            
            _currentStep++;
            
            var elapsed = DateTime.Now - _executionStartTime;
            ExecutionTime.Text = $"执行时间: {elapsed.TotalSeconds:F1}s";

            if (_currentStep < _totalSteps)
            {
                BrowserStatus.Text = $"单步模式：准备执行第 {_currentStep + 1}/{_totalSteps} 步";
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError("AIDebugWorkbench", $"Step {_currentStep + 1} failed: {ex.Message}", ex.StackTrace);
            AppendLog($"✗ 步骤 {_currentStep + 1} 失败: {ex.Message}", LogLevel.Error);
            MessageBox.Show($"步骤 {_currentStep + 1} 执行失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private string GetStepDescription(DslStep step)
    {
        if (!string.IsNullOrWhiteSpace(step.Step))
        {
            var kind = step.Step.ToLowerInvariant();
            return kind switch
            {
                "open" => $"打开 {step.Url}",
                "click" => $"点击元素 {step.Selector?.Type}:{step.Selector?.Value}",
                "fill" or "type" => $"填写表单 {step.Value} 到 {step.Selector?.Type}:{step.Selector?.Value}",
                "waitfor" => $"等待元素 {step.Selector?.Type}:{step.Selector?.Value}",
                "waitnetworkidle" => "等待网络空闲",
                "screenshot" => "截图",
                "log" => $"日志: {step.Log?.Message}",
                "sleep" => $"等待 {step.Sleep?.Ms}ms",
                _ => "未知步骤"
            };
        }
        
        if (step.Open != null) return $"打开 {step.Open.Url}";
        if (step.Click != null) return $"点击元素 {step.Click.Selector?.Type}:{step.Click.Selector?.Value}";
        if (step.Fill != null) return $"填写表单 {step.Fill.Value} 到 {step.Fill.Selector?.Type}:{step.Fill.Selector?.Value}";
        if (step.TypeAction != null) return $"输入文本 {step.TypeAction.Text} 到 {step.TypeAction.Selector?.Type}:{step.TypeAction.Selector?.Value}";
        if (step.WaitFor != null) return $"等待元素 {step.WaitFor.Selector?.Type}:{step.WaitFor.Selector?.Value}";
        if (step.WaitNetworkIdle != null) return "等待网络空闲";
        if (step.Screenshot != null) return "截图";
        if (step.Log != null) return $"日志: {step.Log.Message}";
        if (step.Sleep != null) return $"等待 {step.Sleep.Ms}ms";
        return "未知步骤";
    }

    private async void Picker_Click(object sender, RoutedEventArgs e)
    {
        if (_browserController == null || _webView?.CoreWebView2 == null)
        {
            MessageBox.Show("浏览器未初始化", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        try
        {
            if (!_isPickerActive)
            {
                // 激活选择器拾取
                _isPickerActive = true;
                PickerButton.Content = "⏹️ 停止拾取";
                BrowserStatus.Text = "选择器拾取模式：悬停元素并点击选择";

                // 读取并注入选择器拾取脚本
                var scriptPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "Scripts", "selector-picker.js");
                _logger?.LogInfo("AIDebugWorkbench", $"Looking for script at: {scriptPath}");
                
                if (System.IO.File.Exists(scriptPath))
                {
                    var script = await System.IO.File.ReadAllTextAsync(scriptPath);
                    _logger?.LogInfo("AIDebugWorkbench", $"Script loaded, length: {script.Length}");
                    
                    var result = await _webView.CoreWebView2.ExecuteScriptAsync(script);
                    _logger?.LogInfo("AIDebugWorkbench", $"Script injection result: {result}");
                    
                    var activateResult = await _webView.CoreWebView2.ExecuteScriptAsync("window.__selectorPicker.activate();");
                    _logger?.LogInfo("AIDebugWorkbench", $"Activate result: {activateResult}");
                    
                    // 测试脚本是否加载
                    var testResult = await _webView.CoreWebView2.ExecuteScriptAsync("typeof window.__selectorPicker");
                    _logger?.LogInfo("AIDebugWorkbench", $"Picker object type: {testResult}");
                    
                    _logger?.LogInfo("AIDebugWorkbench", "Selector picker activated");
                }
                else
                {
                    _logger?.LogError("AIDebugWorkbench", $"Script not found: {scriptPath}", "");
                    MessageBox.Show($"选择器拾取脚本未找到：{scriptPath}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    _isPickerActive = false;
                    PickerButton.Content = "🎯 拾取选择器";
                }
            }
            else
            {
                // 停用选择器拾取
                _isPickerActive = false;
                PickerButton.Content = "🎯 拾取选择器";
                BrowserStatus.Text = "浏览器就绪";
                
                await _webView.CoreWebView2.ExecuteScriptAsync("window.__selectorPicker?.deactivate();");
                _logger?.LogInfo("AIDebugWorkbench", "Selector picker deactivated");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError("AIDebugWorkbench", $"Picker toggle failed: {ex.Message}", ex.StackTrace);
            MessageBox.Show($"选择器拾取失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            _isPickerActive = false;
            PickerButton.Content = "🎯 拾取选择器";
        }
    }

    private async void Record_Click(object sender, RoutedEventArgs e)
    {
        if (_browserController == null || _webView?.CoreWebView2 == null || _recorderService == null)
        {
            MessageBox.Show("浏览器或录制服务未初始化", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        try
        {
            if (!_recorderService.IsRecording)
            {
                // 开始录制
                RecordButton.Content = "⏹️ 停止录制";
                BrowserStatus.Text = "录制模式：操作将被自动捕获";

                // 读取并注入录制脚本
                var scriptPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "Scripts", "recorder.js");
                if (System.IO.File.Exists(scriptPath))
                {
                    var script = await System.IO.File.ReadAllTextAsync(scriptPath);
                    await _webView.CoreWebView2.ExecuteScriptAsync(script);
                    await _webView.CoreWebView2.ExecuteScriptAsync("window.__recorder.start();");
                    
                    _recorderService.Start();
                    _logger?.LogInfo("AIDebugWorkbench", "Recording started");
                }
                else
                {
                    MessageBox.Show($"录制脚本未找到：{scriptPath}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    RecordButton.Content = "⏺️ 录制";
                }
            }
            else
            {
                // 停止录制
                RecordButton.Content = "⏺️ 录制";
                BrowserStatus.Text = "浏览器就绪";
                
                await _webView.CoreWebView2.ExecuteScriptAsync("window.__recorder?.stop();");
                _recorderService.Stop();
                
                _logger?.LogInfo("AIDebugWorkbench", "Recording stopped");
                
                // HandleRecordingStopped 会在收到 recording_stopped 消息时调用
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError("AIDebugWorkbench", $"Recording toggle failed: {ex.Message}", ex.StackTrace);
            MessageBox.Show($"录制失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            RecordButton.Content = "⏺️ 录制";
        }
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        var url = UrlBox.Text;
        if (string.IsNullOrWhiteSpace(url) || url == "about:blank")
        {
            MessageBox.Show("请输入有效的 URL", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (_browserController == null)
        {
            MessageBox.Show("浏览器未初始化", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        try
        {
            _logger?.LogInfo("AIDebugWorkbench", $"Navigating to: {url}");
            BrowserStatus.Text = $"正在加载: {url}";
            
            await _browserController.NavigateAsync(url);
            
            _logger?.LogInfo("AIDebugWorkbench", $"Navigation completed: {url}");
        }
        catch (Exception ex)
        {
            _logger?.LogError("AIDebugWorkbench", $"Navigation failed: {ex.Message}", ex.StackTrace);
            MessageBox.Show($"导航失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            BrowserStatus.Text = "导航失败";
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        if (_isRunning)
        {
            var result = MessageBox.Show(
                "脚本正在运行，确定要关闭吗？",
                "确认",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            _isRunning = false;
        }

        _logger?.LogInfo("AIDebugWorkbench", "Closing workbench");

        // 返回上一页或关闭窗口
        if (NavigationService != null && NavigationService.CanGoBack)
        {
            NavigationService.GoBack();
        }
        else
        {
            Window.GetWindow(this)?.Close();
        }
    }

    #endregion

    #region AI 助手

    private void CopyLog_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!string.IsNullOrEmpty(LogTextBox.Text))
            {
                Clipboard.SetText(LogTextBox.Text);
                AppendLog("[系统] 日志已复制到剪贴板", LogLevel.Info);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError("AIDebugWorkbench", $"Copy log failed: {ex.Message}", ex.StackTrace);
        }
    }

    private void ClearLog_Click(object sender, RoutedEventArgs e)
    {
        LogTextBox.Text = "[系统] 日志已清空";
        _logTotalCount = 0;
        _logSuccessCount = 0;
        _logErrorCount = 0;
        UpdateLogStats();
    }

    private void AppendLog(string message, LogLevel level = LogLevel.Info)
    {
        Dispatcher.Invoke(() =>
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var prefix = level switch
            {
                LogLevel.Error => "[错误]",
                LogLevel.Warning => "[警告]",
                LogLevel.Info => "[信息]",
                _ => "[日志]"
            };

            var logLine = $"[{timestamp}] {prefix} {message}";
            
            if (LogTextBox.Text == "[系统] 等待执行...")
            {
                LogTextBox.Text = logLine;
            }
            else
            {
                LogTextBox.Text += Environment.NewLine + logLine;
            }

            // 自动滚动到底部
            LogScroll.ScrollToEnd();

            // 更新统计
            _logTotalCount++;
            if (level == LogLevel.Error)
                _logErrorCount++;
            else if (level == LogLevel.Info && message.Contains("✓"))
                _logSuccessCount++;

            UpdateLogStats();
        });
    }

    private void UpdateLogStats()
    {
        LogStats.Text = $"总计: {_logTotalCount} 条";
        LogSuccessCount.Text = _logSuccessCount.ToString();
        LogErrorCount.Text = _logErrorCount.ToString();
    }

    private void LogTab_Click(object sender, RoutedEventArgs e)
    {
        // 切换到日志标签
        LogPanel.Visibility = Visibility.Visible;
        AiPanel.Visibility = Visibility.Collapsed;
        LogToolbar.Visibility = Visibility.Visible;
        
        // 更新标签按钮样式
        LogTabButton.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(16, 163, 127));
        LogTabButton.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
        LogTabButton.BorderThickness = new Thickness(0);
        LogTabButton.FontWeight = FontWeights.SemiBold;
        
        AiTabButton.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
        AiTabButton.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black);
        AiTabButton.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(224, 224, 224));
        AiTabButton.BorderThickness = new Thickness(1);
        AiTabButton.FontWeight = FontWeights.Normal;
    }

    private void AiTab_Click(object sender, RoutedEventArgs e)
    {
        // 切换到 AI 助手标签
        LogPanel.Visibility = Visibility.Collapsed;
        AiPanel.Visibility = Visibility.Visible;
        LogToolbar.Visibility = Visibility.Collapsed;
        
        // 更新标签按钮样式
        AiTabButton.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(16, 163, 127));
        AiTabButton.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
        AiTabButton.BorderThickness = new Thickness(0);
        AiTabButton.FontWeight = FontWeights.SemiBold;
        
        LogTabButton.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
        LogTabButton.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black);
        LogTabButton.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(224, 224, 224));
        LogTabButton.BorderThickness = new Thickness(1);
        LogTabButton.FontWeight = FontWeights.Normal;
    }

    private void ClearChat_Click(object sender, RoutedEventArgs e)
    {
        // 清空对话，只保留欢迎消息
        ChatPanel.Children.Clear();
        
        // 重新添加欢迎消息
        var welcomeBorder = new Border
        {
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 249, 255)),
            BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(14, 165, 233)),
            BorderThickness = new Thickness(2),
            CornerRadius = new System.Windows.CornerRadius(12),
            Padding = new Thickness(16),
            Margin = new Thickness(0, 0, 0, 16)
        };
        
        var stackPanel = new StackPanel();
        
        var titleBlock = new TextBlock
        {
            FontSize = 14,
            FontWeight = FontWeights.Bold,
            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(14, 165, 233)),
            Margin = new Thickness(0, 0, 0, 8),
            Text = "👋 你好！我是 AI 脚本助手"
        };
        
        var contentBlock = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            FontSize = 12,
            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(51, 65, 85)),
            Text = "我可以帮你：\n✨ 生成和优化 DSL 脚本\n🔍 诊断和修复执行错误\n🎯 推荐更好的选择器\n💡 解答使用问题\n\n在下方输入你的需求，让我们开始吧！"
        };
        
        stackPanel.Children.Add(titleBlock);
        stackPanel.Children.Add(contentBlock);
        welcomeBorder.Child = stackPanel;
        ChatPanel.Children.Add(welcomeBorder);
        
        _logger?.LogInfo("AIDebugWorkbench", "Chat cleared");
    }

    private async void SendAi_Click(object sender, RoutedEventArgs e)
    {
        _logger?.LogInfo("AIDebugWorkbench", "SendAi_Click called");
        
        var input = AiInputBox.Text?.Trim();
        _logger?.LogInfo("AIDebugWorkbench", $"Input text: {input?.Length ?? 0} chars");
        
        if (string.IsNullOrWhiteSpace(input) || input.Contains("描述你想要"))
        {
            MessageBox.Show("请输入你的需求", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        Border? thinkingBorder = null;
        
        try
        {
            _logger?.LogInfo("AIDebugWorkbench", $"AI request: {input.Substring(0, Math.Min(50, input.Length))}...");

            // 添加用户消息到聊天面板
            _logger?.LogInfo("AIDebugWorkbench", "Adding user message to chat");
            AddChatMessage("user", input);
            AiInputBox.Text = "";
            _logger?.LogInfo("AIDebugWorkbench", "User message added");

            // 显示"正在思考..."
            thinkingBorder = new Border
            {
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 249, 255)),
                BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(14, 165, 233)),
                BorderThickness = new Thickness(2),
                CornerRadius = new System.Windows.CornerRadius(12),
                Padding = new Thickness(16),
                Margin = new Thickness(0, 0, 0, 16)
            };
            var thinkingText = new TextBlock
            {
                Text = "💭 正在思考...",
                TextWrapping = TextWrapping.Wrap,
                FontSize = 13,
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(51, 65, 85)),
                LineHeight = 20
            };
            thinkingBorder.Child = thinkingText;
            ChatPanel.Children.Add(thinkingBorder);
            ChatScroll.ScrollToEnd();

            // 构造页面上下文（如果勾选）
            PageContext? pageContext = null;
            _logger?.LogInfo("AIDebugWorkbench", $"Attach context checked: {AttachPageContextCheckBox.IsChecked == true}");
            
            if (AttachPageContextCheckBox.IsChecked == true && _browserController != null)
            {
                try
                {
                    _logger?.LogInfo("AIDebugWorkbench", "Getting page context...");
                    var url = await _browserController.EvaluateAsync<string>("location.href");
                    var title = await _browserController.EvaluateAsync<string>("document.title");
                    var visibleText = await _browserController.EvaluateAsync<string>(
                        "(function(){ const t = document.body?.innerText || ''; return t.slice(0, 4000); })()");
                    
                    pageContext = new PageContext
                    {
                        Url = url,
                        Title = title,
                        VisibleText = visibleText
                    };
                    
                    _logger?.LogInfo("AIDebugWorkbench", $"Page context attached: {url}");
                }
                catch (Exception ex)
                {
                    _logger?.LogWarn("AIDebugWorkbench", $"Failed to get page context: {ex.Message}");
                }
            }

            // 调用 AI 服务
            _logger?.LogInfo("AIDebugWorkbench", "Getting AI service from DI container...");
            var host = App.Current.Resources["Host"] as IHost;
            var aiService = host?.Services.GetService<IAIClientService>();
            
            if (aiService == null)
            {
                _logger?.LogError("AIDebugWorkbench", "AI service is null");
                ChatPanel.Children.Remove(thinkingBorder);
                AddChatMessage("assistant", "抱歉，AI 服务未初始化。请检查 AI 提供商配置。");
                return;
            }

            _logger?.LogInfo("AIDebugWorkbench", "Calling AI service ChatAsync...");
            
            // 记录发送给 AI 的数据
            if (pageContext != null)
            {
                _logger?.LogInfo("AIDebugWorkbench", $"=== Page Context ===");
                _logger?.LogInfo("AIDebugWorkbench", $"URL: {pageContext.Url}");
                _logger?.LogInfo("AIDebugWorkbench", $"Title: {pageContext.Title}");
                _logger?.LogInfo("AIDebugWorkbench", $"Visible Text Length: {pageContext.VisibleText?.Length ?? 0} chars");
                if (!string.IsNullOrEmpty(pageContext.VisibleText))
                {
                    var preview = pageContext.VisibleText.Length > 200 
                        ? pageContext.VisibleText.Substring(0, 200) + "..." 
                        : pageContext.VisibleText;
                    _logger?.LogInfo("AIDebugWorkbench", $"Text Preview: {preview}");
                }
            }
            else
            {
                _logger?.LogInfo("AIDebugWorkbench", "No page context attached");
            }
            
            var response = await aiService.ChatAsync(input, pageContext);
            _logger?.LogInfo("AIDebugWorkbench", $"AI response received: {response?.Length ?? 0} chars");

            // 移除"正在思考..."
            ChatPanel.Children.Remove(thinkingBorder);

            // 显示 AI 回复
            AddChatMessage("assistant", response);

            _logger?.LogInfo("AIDebugWorkbench", "AI response displayed");
        }
        catch (Exception ex)
        {
            _logger?.LogError("AIDebugWorkbench", $"AI request failed: {ex.Message}", ex.StackTrace);
            
            // 尝试移除 thinking border
            try
            {
                if (ChatPanel.Children.Contains(thinkingBorder))
                    ChatPanel.Children.Remove(thinkingBorder);
            }
            catch { }
            
            AddChatMessage("assistant", $"抱歉，处理失败：{ex.Message}");
        }
    }

    private void AddChatMessage(string role, string content)
    {
        var border = new Border
        {
            Background = role == "user" ? 
                new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 252, 231)) : 
                new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 249, 255)),
            BorderBrush = role == "user" ?
                new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(16, 163, 127)) :
                new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(14, 165, 233)),
            BorderThickness = new Thickness(2),
            CornerRadius = new System.Windows.CornerRadius(12),
            Padding = new Thickness(16),
            Margin = new Thickness(0, 0, 0, 16)
        };

        // 使用 TextBox 而不是 TextBlock，以支持文本选择和复制
        var textBox = new TextBox
        {
            Text = content,
            TextWrapping = TextWrapping.Wrap,
            FontSize = 13,
            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(51, 65, 85)),
            Background = System.Windows.Media.Brushes.Transparent,
            BorderThickness = new Thickness(0),
            IsReadOnly = true,
            IsReadOnlyCaretVisible = false,
            Cursor = System.Windows.Input.Cursors.Arrow,
            Focusable = true
        };
        
        // 允许文本选择
        textBox.IsEnabled = true;

        border.Child = textBox;
        ChatPanel.Children.Add(border);

        // 滚动到底部
        ChatScroll.ScrollToEnd();
    }

    #endregion

    #region 辅助方法

    private void UpdateControlStates()
    {
        // 运行按钮：非运行状态时启用
        RunButton.IsEnabled = !_isRunning;
        
        // 单步按钮：非运行状态或单步模式时启用
        StepButton.IsEnabled = !_isRunning || _isStepMode;
        
        // 停止按钮：运行状态时启用
        StopButton.IsEnabled = _isRunning;
        
        // 拾取和录制按钮：非运行状态时启用
        PickerButton.IsEnabled = !_isRunning;
        RecordButton.IsEnabled = !_isRunning;

        if (_isRunning)
        {
            var elapsed = (DateTime.Now - _executionStartTime).TotalSeconds;
            ExecutionTime.Text = $"执行时间: {elapsed:F1}s";
        }
    }

    #endregion
}
