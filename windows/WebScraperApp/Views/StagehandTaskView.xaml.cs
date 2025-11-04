using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FishBrowser.WPF.Services;
using FishBrowser.WPF.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FishBrowser.WPF.Views
{
    public partial class StagehandTaskView : Page
    {
        private readonly IAIProviderService? _aiProviderService;
        private readonly IAIClientService? _aiClient;
        private readonly ILogService? _logService;
        private readonly NodeExecutionService? _nodeExecutionService;
        private readonly StagehandMaintenanceService? _stagehandService;
        private List<StagehandChatMessage> _chatHistory = new List<StagehandChatMessage>();
        private string _currentScript = "";
        private int _selectedProviderId = 0;

        public StagehandTaskView()
        {
            InitializeComponent();
            
            // 从 DI 容器获取服务
            var host = App.Current.Resources["Host"] as IHost;
            if (host != null)
            {
                _aiProviderService = host.Services.GetService<IAIProviderService>();
                _aiClient = host.Services.GetService<IAIClientService>();
                _logService = host.Services.GetService<ILogService>();
                _nodeExecutionService = new NodeExecutionService(_logService as LogService);
                _stagehandService = host.Services.GetService<StagehandMaintenanceService>();
            }

            Loaded += StagehandTaskView_Loaded;
        }

        private async void StagehandTaskView_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadAIProvidersAsync();
            await CheckStagehandStatusAsync();
        }

        #region AI Provider Management

        private async Task LoadAIProvidersAsync()
        {
            try
            {
                var providers = await _aiProviderService.GetAllProvidersAsync();
                ProviderComboBox.ItemsSource = providers;
                ProviderComboBox.DisplayMemberPath = "Name";
                ProviderComboBox.SelectedValuePath = "Id";

                // 选择默认提供商
                var defaultProvider = providers.FirstOrDefault();
                if (defaultProvider != null)
                {
                    ProviderComboBox.SelectedItem = defaultProvider;
                }
            }
            catch (Exception ex)
            {
                _logService.LogError("StagehandTask", $"Failed to load AI providers: {ex.Message}", ex.StackTrace);
                AddSystemMessage("⚠️ 加载 AI 提供商失败，请检查配置");
            }
        }

        private void ProviderComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProviderComboBox.SelectedItem != null)
            {
                var provider = ProviderComboBox.SelectedItem as AIProviderConfig;
                if (provider != null)
                {
                    _selectedProviderId = provider.Id;
                    _logService?.LogInfo("StagehandTask", $"AI Provider changed to: {provider.Name}");
                }
            }
        }

        #endregion

        #region Stagehand Status

        private async Task CheckStagehandStatusAsync()
        {
            try
            {
                // 检查 Node.js
                var nodeInstalled = await _nodeExecutionService.IsNodeInstalledAsync();
                if (!nodeInstalled)
                {
                    StagehandStatusIcon.Text = "✗";
                    StagehandStatusIcon.Foreground = new SolidColorBrush(Colors.Red);
                    StagehandStatusText.Text = "Node.js 未安装";
                    StagehandStatusText.Foreground = new SolidColorBrush(Colors.Red);
                    AddSystemMessage("⚠️ Node.js 未安装，请先安装 Node.js (https://nodejs.org/)");
                    return;
                }

                // 检查 Stagehand
                var status = await _stagehandService.GetStatusAsync();
                if (status.IsInstalled)
                {
                    StagehandStatusIcon.Text = "✓";
                    StagehandStatusIcon.Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                    StagehandStatusText.Text = $"Stagehand {status.InstalledVersion} 已就绪";
                    StagehandStatusText.Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50));
                }
                else
                {
                    StagehandStatusIcon.Text = "⚠";
                    StagehandStatusIcon.Foreground = new SolidColorBrush(Colors.Orange);
                    StagehandStatusText.Text = "Stagehand 未安装";
                    StagehandStatusText.Foreground = new SolidColorBrush(Colors.Orange);
                    AddSystemMessage("⚠️ Stagehand 未安装，请前往系统设置安装 Stagehand");
                }
            }
            catch (Exception ex)
            {
                StagehandStatusIcon.Text = "✗";
                StagehandStatusIcon.Foreground = new SolidColorBrush(Colors.Red);
                StagehandStatusText.Text = "状态检查失败";
                StagehandStatusText.Foreground = new SolidColorBrush(Colors.Red);
                
                _logService.LogError("StagehandTask", $"Stagehand status check failed: {ex.Message}");
            }
        }

        #endregion

        #region Chat Management

        private void AddUserMessage(string message)
        {
            var border = new Border
            {
                Style = (Style)FindResource("UserMessageStyle")
            };

            var textBlock = new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
                Foreground = Brushes.White,
                FontSize = 14
            };

            border.Child = textBlock;
            ChatPanel.Children.Add(border);

            _chatHistory.Add(new StagehandChatMessage { Role = "user", Content = message });

            ScrollToBottom();
        }

        private void AddAIMessage(string message)
        {
            var border = new Border
            {
                Style = (Style)FindResource("AIMessageStyle")
            };

            var textBlock = new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51)),
                FontSize = 14
            };

            border.Child = textBlock;
            ChatPanel.Children.Add(border);

            _chatHistory.Add(new StagehandChatMessage { Role = "assistant", Content = message });

            ScrollToBottom();
        }

        private void AddSystemMessage(string message)
        {
            var border = new Border
            {
                Style = (Style)FindResource("SystemMessageStyle")
            };

            var textBlock = new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102)),
                FontSize = 13
            };

            border.Child = textBlock;
            ChatPanel.Children.Add(border);

            ScrollToBottom();
        }

        private void ScrollToBottom()
        {
            ChatScrollViewer.ScrollToEnd();
        }

        #endregion

        #region Message Handling

        private async void Send_Click(object sender, RoutedEventArgs e)
        {
            await SendMessageAsync();
        }

        private async void InputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                e.Handled = true;
                await SendMessageAsync();
            }
        }

        private async Task SendMessageAsync()
        {
            var userMessage = InputBox.Text.Trim();
            if (string.IsNullOrEmpty(userMessage)) return;

            InputBox.Text = "";
            InputBox.IsEnabled = false;
            StatusText.Text = "AI 正在生成脚本...";

            AddUserMessage(userMessage);

            try
            {
                // 构建系统提示
                var systemPrompt = BuildSystemPrompt();

                // 调用 AI
                var script = await GenerateStagehandScriptAsync(systemPrompt, userMessage);

                if (!string.IsNullOrEmpty(script))
                {
                    _currentScript = script;
                    ScriptPreviewBox.Text = script;
                    
                    AddAIMessage($"✅ 已生成 Stagehand 脚本！\n\n脚本包含 {CountActions(script)} 个操作步骤。你可以在右侧预览和编辑脚本，然后点击\"运行脚本\"执行。");
                    
                    UpdateScriptInfo(script);
                }
                else
                {
                    AddAIMessage("❌ 脚本生成失败，请重试或换一种描述方式。");
                }
            }
            catch (Exception ex)
            {
                _logService.LogError("StagehandTask", $"Failed to generate script: {ex.Message}", ex.StackTrace);
                AddAIMessage($"❌ 生成脚本时出错：{ex.Message}");
            }
            finally
            {
                InputBox.IsEnabled = true;
                InputBox.Focus();
                StatusText.Text = "就绪";
            }
        }

        private string BuildSystemPrompt()
        {
            return @"你是一个 Stagehand 脚本生成专家。Stagehand 是一个 AI 驱动的浏览器自动化框架。

## Stagehand 核心 API

1. **act(instruction)** - 执行操作
   - 示例：await stagehand.act('点击登录按钮')
   - 示例：await stagehand.act('在搜索框输入 iPhone')

2. **extract(instruction, schema)** - 提取数据
   - 示例：const data = await stagehand.extract('提取商品信息', { name: 'string', price: 'number' })

3. **observe(instruction)** - 观察页面元素
   - 示例：const elements = await stagehand.observe('找到所有商品卡片')

4. **page** - Playwright Page 对象
   - 示例：await stagehand.page.goto('https://example.com')
   - 示例：await stagehand.page.waitForTimeout(2000)

## 脚本模板

```javascript
const { Stagehand } = require('@browserbasehq/stagehand');

(async () => {
    // 初始化 Stagehand
    const stagehand = new Stagehand({
        env: 'LOCAL',
        verbose: 1,
        debugDom: true
    });
    
    try {
        await stagehand.init();
        
        // 导航到目标网站
        await stagehand.page.goto('https://example.com');
        
        // 执行操作
        await stagehand.act('你的操作指令');
        
        // 提取数据（如果需要）
        const data = await stagehand.extract('提取指令', {
            // 数据结构定义
        });
        
        console.log('任务完成！', data);
        
    } catch (error) {
        console.error('任务失败:', error);
    } finally {
        await stagehand.close();
    }
})();
```

## 生成规则

1. 使用完整的可执行脚本格式
2. 包含错误处理
3. 使用清晰的注释
4. act() 指令要具体明确
5. 合理使用等待和延迟
6. 提取数据时定义清晰的 schema

请根据用户需求生成 Stagehand 脚本。只返回 JavaScript 代码，不要有其他解释。";
        }

        private async Task<string> GenerateStagehandScriptAsync(string systemPrompt, string userMessage)
        {
            try
            {
                if (_selectedProviderId == 0)
                {
                    throw new Exception("请先选择 AI 提供商");
                }

                if (_aiClient == null)
                {
                    throw new Exception("AI 服务未初始化");
                }

                // 构建完整的提示词
                var fullPrompt = $"{systemPrompt}\n\n用户需求：{userMessage}";

                // 调用 AI 生成脚本
                var response = await _aiClient.GenerateDslFromPromptAsync(fullPrompt, _selectedProviderId);

                return response?.Trim() ?? "";
            }
            catch (Exception ex)
            {
                _logService?.LogError("StagehandTask", $"AI generation failed: {ex.Message}", ex.StackTrace);
                throw;
            }
        }

        #endregion

        #region Script Management

        private int CountActions(string script)
        {
            if (string.IsNullOrEmpty(script)) return 0;
            
            int count = 0;
            count += System.Text.RegularExpressions.Regex.Matches(script, @"\.act\(").Count;
            count += System.Text.RegularExpressions.Regex.Matches(script, @"\.extract\(").Count;
            count += System.Text.RegularExpressions.Regex.Matches(script, @"\.observe\(").Count;
            
            return count;
        }

        private void UpdateScriptInfo(string script)
        {
            var actionCount = CountActions(script);
            ActionCountText.Text = actionCount.ToString();
            
            // 估算时间（每个操作约 3-5 秒）
            var estimatedSeconds = actionCount * 4;
            EstimatedTimeText.Text = estimatedSeconds > 60 
                ? $"{estimatedSeconds / 60} 分 {estimatedSeconds % 60} 秒" 
                : $"{estimatedSeconds} 秒";
            
            // 复杂度评估
            if (actionCount <= 3)
                ComplexityText.Text = "简单 ⭐";
            else if (actionCount <= 8)
                ComplexityText.Text = "中等 ⭐⭐";
            else
                ComplexityText.Text = "复杂 ⭐⭐⭐";
        }

        #endregion

        #region Quick Examples

        private async void QuickExample_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var tag = button?.Tag as string;

            string prompt = tag switch
            {
                "login" => "创建一个登录 GitHub 的脚本：打开 github.com，点击 Sign in，填写用户名和密码，点击登录按钮",
                "search" => "创建一个搜索脚本：打开 Google，搜索 'Stagehand AI'，提取前 5 个搜索结果的标题和链接",
                "navigation" => "创建一个导航脚本：打开 Amazon，依次点击 Books 分类，然后点击 Best Sellers",
                "extraction" => "创建一个数据提取脚本：打开 Hacker News 首页，提取前 10 条新闻的标题、分数和评论数",
                "form" => "创建一个表单填写脚本：打开一个联系表单，填写姓名、邮箱和消息内容，然后提交",
                "shopping" => "创建一个购物脚本：在 Amazon 搜索 'laptop'，点击第一个商品，提取商品名称和价格，然后加入购物车",
                _ => ""
            };

            if (!string.IsNullOrEmpty(prompt))
            {
                InputBox.Text = prompt;
                await SendMessageAsync();
            }
        }

        #endregion

        #region Button Handlers

        private async void RunScript_Click(object sender, RoutedEventArgs e)
        {
            var script = ScriptPreviewBox.Text.Trim();
            if (string.IsNullOrEmpty(script))
            {
                MessageBox.Show("请先生成脚本", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // 检查 Node.js
            var nodeInstalled = await _nodeExecutionService.IsNodeInstalledAsync();
            if (!nodeInstalled)
            {
                MessageBox.Show("Node.js 未安装，无法执行脚本。\n\n请先安装 Node.js: https://nodejs.org/", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            AddSystemMessage("🚀 开始执行脚本...");
            StatusText.Text = "执行中...";

            var runButton = sender as Button;
            if (runButton != null)
            {
                runButton.IsEnabled = false;
                runButton.Content = "⏳ 执行中...";
            }

            try
            {
                // 执行脚本
                var result = await _nodeExecutionService.ExecuteScriptAsync(script, debug: true);
                
                if (result.Success)
                {
                    AddSystemMessage($"✅ 脚本执行成功！\n\n输出：\n{result.Output}");
                    MessageBox.Show($"脚本执行成功！\n\n{result.Output}", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    AddSystemMessage($"❌ 脚本执行失败\n\n错误：\n{result.Error}");
                    MessageBox.Show($"脚本执行失败：\n\n{result.Error}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                _logService.LogError("StagehandTask", $"Script execution failed: {ex.Message}", ex.StackTrace);
                AddSystemMessage($"❌ 脚本执行失败：{ex.Message}");
                MessageBox.Show($"脚本执行失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                StatusText.Text = "就绪";
                if (runButton != null)
                {
                    runButton.IsEnabled = true;
                    runButton.Content = "▶️ 运行脚本";
                }
            }
        }

        private void DebugScript_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("调试模式功能开发中...", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SaveTask_Click(object sender, RoutedEventArgs e)
        {
            var script = ScriptPreviewBox.Text.Trim();
            if (string.IsNullOrEmpty(script))
            {
                MessageBox.Show("请先生成脚本", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            MessageBox.Show("保存任务功能开发中...", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CopyScript_Click(object sender, RoutedEventArgs e)
        {
            var script = ScriptPreviewBox.Text.Trim();
            if (string.IsNullOrEmpty(script))
            {
                MessageBox.Show("没有可复制的脚本", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Clipboard.SetText(script);
            AddSystemMessage("📋 脚本已复制到剪贴板");
        }

        private void ExportScript_Click(object sender, RoutedEventArgs e)
        {
            var script = ScriptPreviewBox.Text.Trim();
            if (string.IsNullOrEmpty(script))
            {
                MessageBox.Show("请先生成脚本", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "JavaScript files (*.js)|*.js|All files (*.*)|*.*",
                DefaultExt = ".js",
                FileName = "stagehand-task.js"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    System.IO.File.WriteAllText(dialog.FileName, script);
                    AddSystemMessage($"💾 脚本已导出到：{dialog.FileName}");
                    MessageBox.Show("脚本导出成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"导出失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void OptimizeScript_Click(object sender, RoutedEventArgs e)
        {
            var script = ScriptPreviewBox.Text.Trim();
            if (string.IsNullOrEmpty(script))
            {
                MessageBox.Show("请先生成脚本", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            AddSystemMessage("🔧 正在优化脚本...");
            StatusText.Text = "优化中...";

            try
            {
                var optimizationPrompt = $"请优化以下 Stagehand 脚本，使其更高效、更健壮：\n\n{script}";
                var optimizedScript = await GenerateStagehandScriptAsync(BuildSystemPrompt(), optimizationPrompt);
                
                if (!string.IsNullOrEmpty(optimizedScript))
                {
                    ScriptPreviewBox.Text = optimizedScript;
                    _currentScript = optimizedScript;
                    UpdateScriptInfo(optimizedScript);
                    AddAIMessage("✅ 脚本已优化！");
                }
            }
            catch (Exception ex)
            {
                _logService.LogError("StagehandTask", $"Script optimization failed: {ex.Message}");
                AddSystemMessage($"❌ 优化失败：{ex.Message}");
            }
            finally
            {
                StatusText.Text = "就绪";
            }
        }

        private void ShowExamples_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("示例库功能开发中...", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ShowHistory_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("历史任务功能开发中...", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ClearChat_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("确定要清空对话历史吗？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                // 保留欢迎消息和快捷示例，删除其他消息
                var childrenToRemove = ChatPanel.Children.Cast<UIElement>().Skip(2).ToList();
                foreach (var child in childrenToRemove)
                {
                    ChatPanel.Children.Remove(child);
                }
                
                _chatHistory.Clear();
                ScriptPreviewBox.Text = "// 等待 AI 生成 Stagehand 脚本...\n// 你可以用自然语言描述任务需求";
                _currentScript = "";
                
                ActionCountText.Text = "0";
                EstimatedTimeText.Text = "-";
                ComplexityText.Text = "-";
            }
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("设置功能开发中...", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion
    }

    #region Helper Classes

    public class StagehandChatMessage
    {
        public string Role { get; set; } = "";
        public string Content { get; set; } = "";
    }

    #endregion
}
