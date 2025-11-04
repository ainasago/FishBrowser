using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Win32;
using FishBrowser.WPF.Models;
using FishBrowser.WPF.Services;

namespace FishBrowser.WPF.Views;

public partial class AITaskView : Page
{
    private List<ChatMessage> _chatHistory = new();
    private bool _isProcessing = false;
    private readonly IAIClientService? _aiClient;
    private readonly IAIProviderService? _providerService;
    private readonly ILogService? _logger;
    private int _selectedProviderId = 0;

    public AITaskView()
    {
        InitializeComponent();

        // 从 DI 容器获取 AI 服务
        try
        {
            var host = App.Current.Resources["Host"] as IHost;
            _aiClient = host?.Services.GetService<IAIClientService>();
            _providerService = host?.Services.GetService<IAIProviderService>();
            _logger = host?.Services.GetService<ILogService>();
        }
        catch (Exception ex)
        {
            AddSystemMessage($"⚠️ AI 服务初始化失败：{ex.Message}\n\n请先在「AI 配置」中配置 AI 提供商。");
        }

        Loaded += async (s, e) => await LoadProvidersAsync();
    }

    private async Task LoadProvidersAsync()
    {
        try
        {
            if (_providerService == null)
            {
                AddSystemMessage("⚠️ AI 提供商服务未初始化");
                return;
            }

            var providers = await _providerService.GetAllProvidersAsync();
            if (!providers.Any())
            {
                AddSystemMessage("⚠️ 未找到任何 AI 提供商配置\n\n请先在「AI 配置」中添加 AI 提供商。");
                ProviderComboBox.IsEnabled = false;
                return;
            }

            var providerItems = providers
                .Where(p => p.IsEnabled)
                .Select(p => new { Id = p.Id, Display = $"{p.Name} ({p.ModelId})" })
                .ToList();

            ProviderComboBox.ItemsSource = providerItems;
            ProviderComboBox.DisplayMemberPath = "Display";
            ProviderComboBox.SelectedValuePath = "Id";

            if (providerItems.Any())
            {
                ProviderComboBox.SelectedIndex = 0;
                _selectedProviderId = (int)ProviderComboBox.SelectedValue;
                AddSystemMessage($"✅ 已加载 {providerItems.Count} 个 AI 提供商");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError("AITaskView", $"Failed to load providers: {ex.Message}", ex.StackTrace);
            AddSystemMessage($"❌ 加载 AI 提供商失败：{ex.Message}");
        }
    }

    private void ProviderComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ProviderComboBox.SelectedValue is int providerId)
        {
            _selectedProviderId = providerId;
            _logger?.LogInfo("AITaskView", $"Selected provider: {providerId}");
        }
    }

    private async void Send_Click(object sender, RoutedEventArgs e)
    {
        await SendMessageAsync();
    }

    private async void InputBox_KeyDown(object sender, KeyEventArgs e)
    {
        // Ctrl+Enter 发送
        if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
        {
            e.Handled = true;
            await SendMessageAsync();
        }
    }

    private async Task SendMessageAsync()
    {
        var userInput = InputBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(userInput) || _isProcessing)
            return;

        _isProcessing = true;
        StatusText.Text = "处理中...";
        InputBox.IsEnabled = false;

        try
        {
            // 添加用户消息
            AddUserMessage(userInput);
            InputBox.Clear();

            // 滚动到底部
            await Task.Delay(100);
            ChatScrollViewer.ScrollToEnd();

            // 模拟 AI 思考
            AddSystemMessage("🤔 正在分析你的需求...");
            await Task.Delay(500);

            // 调用 AI 生成 DSL（这里是占位实现）
            var dslScript = await GenerateDslFromPromptAsync(userInput);
            
            // 记录生成的 DSL
            var dslPreview = dslScript.Length > 300 ? dslScript.Substring(0, 300) + "..." : dslScript;
            _logger?.LogInfo("AITaskView", $"Generated DSL:\n{dslPreview}");

            // 添加 AI 回复
            AddAIMessage($"我已经为你生成了任务脚本。请在右侧预览区查看详细内容。\n\n**任务摘要：**\n{GetTaskSummary(dslScript)}");

            // 更新预览区
            DslPreviewBox.Text = dslScript;

            // 滚动到底部
            await Task.Delay(100);
            ChatScrollViewer.ScrollToEnd();
        }
        catch (Exception ex)
        {
            AddSystemMessage($"❌ 错误：{ex.Message}");
        }
        finally
        {
            _isProcessing = false;
            StatusText.Text = "就绪";
            InputBox.IsEnabled = true;
            InputBox.Focus();
        }
    }

    private void AddUserMessage(string text)
    {
        var message = new ChatMessage { Role = "user", Content = text, Timestamp = DateTime.Now };
        _chatHistory.Add(message);

        var border = new Border
        {
            Style = (Style)FindResource("UserMessageStyle")
        };

        var textBlock = new TextBlock
        {
            Text = text,
            Foreground = Brushes.White,
            TextWrapping = TextWrapping.Wrap,
            FontSize = 14
        };

        border.Child = textBlock;
        ChatPanel.Children.Add(border);
    }

    private void AddAIMessage(string text)
    {
        var message = new ChatMessage { Role = "assistant", Content = text, Timestamp = DateTime.Now };
        _chatHistory.Add(message);

        var border = new Border
        {
            Style = (Style)FindResource("AIMessageStyle")
        };

        var textBlock = new TextBlock
        {
            Text = text,
            Foreground = Brushes.Black,
            TextWrapping = TextWrapping.Wrap,
            FontSize = 14
        };

        border.Child = textBlock;
        ChatPanel.Children.Add(border);
    }

    private void AddSystemMessage(string text)
    {
        var border = new Border
        {
            Style = (Style)FindResource("SystemMessageStyle")
        };

        var textBlock = new TextBlock
        {
            Text = text,
            Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102)),
            TextWrapping = TextWrapping.Wrap,
            FontSize = 13
        };

        border.Child = textBlock;
        ChatPanel.Children.Add(border);
    }

    private async Task<string> GenerateDslFromPromptAsync(string prompt)
    {
        // 检查是否选择了提供商
        if (_selectedProviderId == 0)
        {
            AddSystemMessage("⚠️ 请先选择 AI 提供商");
            return GenerateGenericExample(prompt);
        }

        // 使用真实的 AI 服务
        if (_aiClient != null)
        {
            try
            {
                var dsl = await _aiClient.GenerateDslFromPromptAsync(prompt, _selectedProviderId);
                return dsl;
            }
            catch (Exception ex)
            {
                _logger?.LogError("AITaskView", $"AI generation failed: {ex.Message}", ex.StackTrace);
                AddSystemMessage($"⚠️ AI 生成失败：{ex.Message}");
                // 降级到示例
            }
        }

        // 降级：使用示例 DSL
        await Task.Delay(500);

        // 根据关键词生成不同的示例
        if (prompt.Contains("登录") || prompt.ToLower().Contains("login"))
        {
            return GenerateLoginExample();
        }
        else if (prompt.Contains("搜索") || prompt.ToLower().Contains("search"))
        {
            return GenerateSearchExample();
        }
        else if (prompt.Contains("翻页") || prompt.Contains("分页"))
        {
            return GeneratePaginationExample();
        }
        else
        {
            return GenerateGenericExample(prompt);
        }
    }

    private string GenerateLoginExample()
    {
        return @"dslVersion: ""1.0""
id: flow_login_example
name: 网站登录流程
description: 自动登录到指定网站
settings:
  selectorTimeoutMs: 6000
  navTimeoutMs: 15000
vars:
  loginUrl: ""https://example.com/login""
steps:
  - open: { url: ""{{ vars.loginUrl }}"" }
  - waitFor: { selector: { type: css, value: ""input[name=username]"" } }
  - fill: { selector: { type: css, value: ""input[name=username]"" }, value: ""{{ secrets.username }}"" }
  - fill: { selector: { type: css, value: ""input[name=password]"" }, value: ""{{ secrets.password }}"" }
  - click: { selector: { type: css, value: ""button[type=submit]"" } }
  - waitNetworkIdle: {}
  - screenshot: { file: ""login-success.png"" }
  - log: { level: info, message: ""登录成功"" }";
    }

    private string GenerateSearchExample()
    {
        return @"dslVersion: ""1.0""
id: flow_search_example
name: 搜索并抓取结果
description: 在网站搜索关键词并提取结果
settings:
  selectorTimeoutMs: 6000
vars:
  baseUrl: ""https://example.com""
  keywords: [""手机"", ""电脑"", ""耳机""]
steps:
  - open: { url: ""{{ vars.baseUrl }}"" }
  - for:
      item: keyword
      list: ""{{ vars.keywords }}""
      do:
        - type: { selector: { type: css, value: ""input[name=q]"" }, text: ""{{ keyword }}"", delayMs: 50 }
        - click: { selector: { type: css, value: ""button[type=submit]"" } }
        - waitNetworkIdle: {}
        - extract:
            fields:
              results[]:
                sel: { type: css, value: "".result-item"" }
                fields:
                  title: { sel: { type: css, value: ""h3"" }, attr: text }
                  link: { sel: { type: css, value: ""a"" }, attr: href }
                  price: { sel: { type: css, value: "".price"" }, attr: text }
        - emit: { key: ""search_results"", value: ""{{ data.results }}"" }
        - log: { level: info, message: ""已提取 {{ len(data.results) }} 条结果"" }";
    }

    private string GeneratePaginationExample()
    {
        return @"dslVersion: ""1.0""
id: flow_pagination_example
name: 多页数据采集
description: 翻页抓取所有数据
settings:
  selectorTimeoutMs: 6000
vars:
  startUrl: ""https://example.com/products""
  maxPages: 5
steps:
  - open: { url: ""{{ vars.startUrl }}"" }
  - for:
      item: pageNum
      list: [1, 2, 3, 4, 5]
      maxIter: 5
      do:
        - extract:
            fields:
              items[]:
                sel: { type: css, value: "".product"" }
                fields:
                  name: { sel: { type: css, value: ""h2"" }, attr: text }
                  price: { sel: { type: css, value: "".price"" }, attr: text }
        - emit: { key: ""page_data"", value: ""{{ data.items }}"" }
        - if:
            cond: ""{{ pageNum < vars.maxPages }}""
            then:
              - click: { selector: { type: css, value: ""a.next-page"" } }
              - waitNetworkIdle: {}
              - sleep: { ms: 1000 }";
    }

    private string GenerateGenericExample(string prompt)
    {
        return $@"dslVersion: ""1.0""
id: flow_custom_{Guid.NewGuid().ToString("N").Substring(0, 8)}
name: 自定义任务流程
description: 基于需求生成的任务
# 用户需求: {prompt}
settings:
  selectorTimeoutMs: 6000
  navTimeoutMs: 15000
vars:
  targetUrl: ""https://example.com""
steps:
  - open: {{ url: ""{{{{ vars.targetUrl }}}}"" }}
  - waitNetworkIdle: {{}}
  - screenshot: {{ file: ""page.png"" }}
  - log: {{ level: info, message: ""任务完成"" }}

# TODO: 请根据实际需求调整上述步骤";
    }

    private string GetTaskSummary(string dsl)
    {
        // 简单解析 DSL 生成摘要
        var lines = dsl.Split('\n');
        var name = lines.FirstOrDefault(l => l.Trim().StartsWith("name:"))?.Split(':')[1].Trim().Trim('"') ?? "未命名任务";
        var stepCount = lines.Count(l => l.Trim().StartsWith("- "));

        return $"• 任务名称：{name}\n• 步骤数量：{stepCount} 个\n• 格式：YAML (DSL v1.0)";
    }

    private void QuickExample_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string tag)
            return;

        var prompt = tag switch
        {
            "login" => "帮我创建一个登录流程，需要填写用户名和密码，然后点击登录按钮",
            "search" => "创建一个搜索任务，搜索多个关键词并提取结果的标题、链接和价格",
            "pagination" => "创建一个翻页采集任务，抓取前5页的所有商品数据",
            "form" => "创建一个表单填写任务，自动填写姓名、邮箱、电话等字段",
            _ => ""
        };

        InputBox.Text = prompt;
        InputBox.Focus();
    }

    private void ClearChat_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show("确定要清空对话历史吗？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
        {
            // 保留欢迎消息和快捷示例，清除其他消息
            while (ChatPanel.Children.Count > 2)
            {
                ChatPanel.Children.RemoveAt(ChatPanel.Children.Count - 1);
            }
            _chatHistory.Clear();
            DslPreviewBox.Text = "# 等待 AI 生成任务脚本...";
        }
    }

    private async void SaveTask_Click(object sender, RoutedEventArgs e)
    {
        var dsl = DslPreviewBox.Text;
        if (string.IsNullOrWhiteSpace(dsl) || dsl.Contains("等待 AI 生成"))
        {
            MessageBox.Show("请先生成任务脚本", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            // 从 DSL 中提取任务名称
            var lines = dsl.Split('\n');
            var nameLine = lines.FirstOrDefault(l => l.Trim().StartsWith("name:"));
            var taskName = nameLine?.Split(':')[1].Trim().Trim('"') ?? $"AI任务_{DateTime.Now:yyyyMMdd_HHmmss}";

            // 创建任务对象
            var task = new ScrapingTask
            {
                Name = taskName,
                Url = "https://example.com", // 从 DSL 中提取或使用默认值
                DslScript = dsl,
                Status = Models.TaskStatus.Draft,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            // 保存到数据库
            var host = App.Current.Resources["Host"] as IHost;
            var db = host?.Services.GetService<FishBrowser.WPF.Data.WebScraperDbContext>();
            
            if (db != null)
            {
                db.ScrapingTasks.Add(task);
                await db.SaveChangesAsync();

                _logger?.LogInfo("AITaskView", $"Task saved: {taskName}");
                MessageBox.Show($"任务已保存！\n\n任务名称：{taskName}\n\n可在「任务管理」中查看和执行。", "保存成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("数据库服务未初始化", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError("AITaskView", $"Failed to save task: {ex.Message}", ex.StackTrace);
            MessageBox.Show($"保存失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void RunTest_Click(object sender, RoutedEventArgs e)
    {
        var dsl = DslPreviewBox.Text;
        if (string.IsNullOrWhiteSpace(dsl) || dsl.Contains("等待 AI 生成"))
        {
            MessageBox.Show("请先生成任务脚本或编辑任务内容", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // 从 DI 容器获取服务
        var host = App.Current.Resources["Host"] as IHost;
        var testRunner = host?.Services.GetService<TaskTestRunnerService>();
        
        if (testRunner == null)
        {
            MessageBox.Show("测试运行器服务未初始化", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        // 记录编辑后的 DSL
        var dslPreview = dsl.Length > 300 ? dsl.Substring(0, 300) + "..." : dsl;
        _logger?.LogInfo("AITaskView", $"Running test with edited DSL:\n{dslPreview}");

        // 创建进度对话框
        var progressDialog = new Views.Dialogs.TaskTestProgressDialog("AI 生成的任务");
        
        // 创建取消令牌
        var cts = new CancellationTokenSource();
        progressDialog.SetCancellationTokenSource(cts);
        
        // 配置测试选项
        var options = new TestRunOptions
        {
            UseRandomFingerprint = true,
            Headless = false, // 显示浏览器
            TimeoutSeconds = 300,
            SaveScreenshots = true,
            CleanupAfterTest = true
        };
        
        // 创建进度报告器
        var progress = new Progress<TestProgress>(p => progressDialog.UpdateProgress(p));
        
        // 后台运行测试
        _ = Task.Run(async () =>
        {
            try
            {
                var result = await testRunner.RunTestAsync(dsl, options, progress, cts.Token);
                
                // 在 UI 线程显示结果
                await Dispatcher.InvokeAsync(() =>
                {
                    if (result.Success)
                    {
                        MessageBox.Show(
                            $"✅ 测试完成！\n\n" +
                            $"执行时间：{result.Duration.TotalSeconds:F1} 秒\n" +
                            $"执行步骤：{result.StepsExecuted} 个",
                            "测试成功",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                        _logger?.LogInfo("AITaskView", $"Test completed successfully in {result.Duration.TotalSeconds:F1}s");
                    }
                    else
                    {
                        MessageBox.Show(
                            $"❌ 测试失败\n\n错误：{result.ErrorMessage}",
                            "测试失败",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        _logger?.LogError("AITaskView", $"Test failed: {result.ErrorMessage}", null);
                    }
                });
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show(
                        $"⚠️ 测试运行异常：{ex.Message}",
                        "错误",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    _logger?.LogError("AITaskView", $"Test exception: {ex.Message}", ex.StackTrace);
                });
            }
        });
        
        // 显示进度对话框（模态）
        progressDialog.ShowDialog();
    }

    private void CopyDsl_Click(object sender, RoutedEventArgs e)
    {
        var dsl = DslPreviewBox.Text;
        if (string.IsNullOrWhiteSpace(dsl) || dsl.Contains("等待 AI 生成"))
        {
            MessageBox.Show("没有可复制的内容", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        Clipboard.SetText(dsl);
        MessageBox.Show("已复制到剪贴板", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ExportYaml_Click(object sender, RoutedEventArgs e)
    {
        var dsl = DslPreviewBox.Text;
        if (string.IsNullOrWhiteSpace(dsl) || dsl.Contains("等待 AI 生成"))
        {
            MessageBox.Show("请先生成任务脚本", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var sfd = new SaveFileDialog
        {
            Filter = "YAML Files (*.yml)|*.yml|All Files (*.*)|*.*",
            FileName = $"task-flow-{DateTime.Now:yyyyMMdd-HHmmss}.yml",
            Title = "导出任务脚本"
        };

        if (sfd.ShowDialog() == true)
        {
            try
            {
                System.IO.File.WriteAllText(sfd.FileName, dsl);
                MessageBox.Show($"已导出到：\n{sfd.FileName}", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void OpenDebugWorkbench_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _logger?.LogInfo("AITaskView", "Opening AI Debug Workbench");

            // 创建工作台页面
            var workbench = new AIDebugWorkbench();

            // 如果有 DSL 内容，传递给工作台
            var dsl = DslPreviewBox.Text;
            if (!string.IsNullOrWhiteSpace(dsl) && !dsl.Contains("等待 AI 生成"))
            {
                // 将 DSL 内容设置到工作台的编辑器
                workbench.Loaded += (s, args) =>
                {
                    var editor = workbench.FindName("YamlEditor") as System.Windows.Controls.TextBox;
                    if (editor != null)
                    {
                        editor.Text = dsl;
                    }
                };
            }

            // 导航到工作台
            if (NavigationService != null)
            {
                NavigationService.Navigate(workbench);
            }
            else
            {
                // 如果没有 NavigationService，在新窗口打开
                var window = new Window
                {
                    Title = "AI 脚本调试工作台",
                    Content = workbench,
                    Width = 1400,
                    Height = 800,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };
                window.Show();
            }

            _logger?.LogInfo("AITaskView", "AI Debug Workbench opened successfully");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"打开调试工作台失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            _logger?.LogError("AITaskView", $"Failed to open debug workbench: {ex.Message}", ex.StackTrace);
        }
    }

    private async void ShowHistory_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // 从数据库获取任务列表
            var host = App.Current.Resources["Host"] as IHost;
            var db = host?.Services.GetService<FishBrowser.WPF.Data.WebScraperDbContext>();
            
            if (db == null)
            {
                MessageBox.Show("数据库服务未初始化", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var tasks = await db.ScrapingTasks
                .OrderByDescending(t => t.CreatedAt)
                .Take(20)
                .ToListAsync();

            if (!tasks.Any())
            {
                MessageBox.Show("暂无历史任务", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // 构建任务列表消息
            var message = "最近的任务：\n\n";
            foreach (var task in tasks)
            {
                var statusIcon = task.Status switch
                {
                    Models.TaskStatus.Draft => "📝",
                    Models.TaskStatus.Running => "▶️",
                    Models.TaskStatus.Completed => "✅",
                    Models.TaskStatus.Failed => "❌",
                    _ => "❓"
                };
                message += $"{statusIcon} {task.Name}\n";
                message += $"   创建时间：{task.CreatedAt:yyyy-MM-dd HH:mm}\n\n";
            }

            message += "\n请前往「任务管理」查看完整列表和详细信息。";
            MessageBox.Show(message, "历史任务", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            _logger?.LogError("AITaskView", $"Failed to load history: {ex.Message}", ex.StackTrace);
            MessageBox.Show($"加载失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        // TODO: AI 设置（API Key、模型选择、温度等）
        MessageBox.Show("AI 设置功能开发中...\n\n可配置：\n• OpenAI API Key\n• 模型选择（GPT-4/GPT-3.5）\n• 温度参数\n• 提示词模板", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}

// 聊天消息模型
public class ChatMessage
{
    public string Role { get; set; } = ""; // user, assistant, system
    public string Content { get; set; } = "";
    public DateTime Timestamp { get; set; }
}
