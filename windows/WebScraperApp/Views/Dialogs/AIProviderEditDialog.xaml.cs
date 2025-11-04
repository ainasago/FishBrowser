using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FishBrowser.WPF.Models;
using FishBrowser.WPF.Services;

namespace FishBrowser.WPF.Views.Dialogs
{
    public partial class AIProviderEditDialog : Window
{
    private readonly IAIProviderService _providerService;
    private readonly ILogService _logger;
    private readonly int _providerId;
    private AIProviderConfig? _config;
    private List<AIApiKey> _apiKeys = new();

    public AIProviderEditDialog(int providerId)
    {
        InitializeComponent();

        _providerId = providerId;

        // 从 DI 容器获取服务
        var host = App.Current.Resources["Host"] as IHost;
        _providerService = host?.Services.GetRequiredService<IAIProviderService>()!;
        _logger = host?.Services.GetRequiredService<ILogService>()!;

        InitializeProviderTypes();
        Loaded += async (s, e) => await LoadConfigAsync();
    }

    private void InitializeProviderTypes()
    {
        var providerTypes = new[]
        {
            new { Type = AIProviderType.OpenAI, Display = "OpenAI" },
            new { Type = AIProviderType.AzureOpenAI, Display = "Azure OpenAI" },
            new { Type = AIProviderType.GoogleGemini, Display = "Google Gemini" },
            new { Type = AIProviderType.AnthropicClaude, Display = "Anthropic Claude" },
            new { Type = AIProviderType.AlibabaQwen, Display = "阿里云通义千问" },
            new { Type = AIProviderType.ModelScope, Display = "魔塔社区 ModelScope" },
            new { Type = AIProviderType.SiliconFlow, Display = "硅基流动 SiliconFlow" },
            new { Type = AIProviderType.BaiduErnie, Display = "百度文心一言" },
            new { Type = AIProviderType.ZhipuGLM, Display = "智谱 GLM" },
            new { Type = AIProviderType.MoonshotAI, Display = "Moonshot AI" },
            new { Type = AIProviderType.Ollama, Display = "Ollama（本地）" }
        };

        ProviderTypeComboBox.ItemsSource = providerTypes;
        ProviderTypeComboBox.DisplayMemberPath = "Display";
        ProviderTypeComboBox.SelectedValuePath = "Type";
    }

    private async Task LoadConfigAsync()
    {
        try
        {
            _config = await _providerService.GetProviderByIdAsync(_providerId);
            if (_config == null)
            {
                MessageBox.Show("配置不存在", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
                return;
            }

            // 加载基本信息
            NameTextBox.Text = _config.Name;
            ProviderTypeComboBox.SelectedValue = _config.ProviderType;
            BaseUrlTextBox.Text = _config.BaseUrl;
            EnabledCheckBox.IsChecked = _config.IsEnabled;

            // 加载设置
            if (_config.Settings != null)
            {
                TemperatureTextBox.Text = _config.Settings.Temperature.ToString();
                MaxTokensTextBox.Text = _config.Settings.MaxTokens.ToString();
                TimeoutTextBox.Text = _config.Settings.TimeoutSeconds.ToString();
                MaxRetriesTextBox.Text = _config.Settings.MaxRetries.ToString();
                RpmLimitTextBox.Text = _config.Settings.RpmLimit?.ToString() ?? "";
            }

            PriorityTextBox.Text = _config.Priority.ToString();

            // 加载 API Keys
            _apiKeys = _config.ApiKeys ?? new List<AIApiKey>();
            RefreshApiKeysList();

            _logger.LogInfo("AIProviderEdit", $"Loaded config for provider {_providerId}");
        }
        catch (Exception ex)
        {
            _logger.LogError("AIProviderEdit", $"Failed to load config: {ex.Message}", ex.StackTrace);
            MessageBox.Show($"加载失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ProviderType_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ProviderTypeComboBox.SelectedValue is AIProviderType providerType)
        {
            LoadModelsForProvider(providerType);
            
            // 自动填充 Base URL
            if (string.IsNullOrWhiteSpace(BaseUrlTextBox.Text))
            {
                BaseUrlTextBox.Text = GetDefaultBaseUrl(providerType);
            }
        }
    }

    private void LoadModelsForProvider(AIProviderType providerType)
    {
        var models = providerType switch
        {
            AIProviderType.OpenAI => new[] { "gpt-4-turbo", "gpt-4", "gpt-3.5-turbo" },
            AIProviderType.GoogleGemini => new[] { "gemini-2.5-flash", "gemini-1.5-pro", "gemini-1.5-flash", "gemini-pro" },
            AIProviderType.AnthropicClaude => new[] { "claude-3-opus-20240229", "claude-3-sonnet-20240229", "claude-3-haiku-20240307" },
            AIProviderType.AlibabaQwen => new[] { "qwen-max", "qwen-plus", "qwen-turbo" },
            AIProviderType.ModelScope => new[] { "Qwen/Qwen2.5-72B-Instruct", "Qwen/Qwen2.5-32B-Instruct", "Qwen/Qwen2.5-14B-Instruct", "Qwen/Qwen2.5-7B-Instruct", "Qwen/Qwen2.5-Coder-32B-Instruct", "Qwen/Qwen2-72B-Instruct" },
            AIProviderType.SiliconFlow => new[] { "Qwen/QwQ-32B", "Qwen/Qwen2.5-72B-Instruct", "Qwen/Qwen2.5-32B-Instruct", "Qwen/Qwen2.5-7B-Instruct", "deepseek-ai/DeepSeek-V3", "Pro/Qwen/Qwen2.5-72B-Instruct-128K" },
            AIProviderType.BaiduErnie => new[] { "ernie-4.0", "ernie-3.5" },
            AIProviderType.ZhipuGLM => new[] { "glm-4", "glm-3-turbo" },
            AIProviderType.MoonshotAI => new[] { "moonshot-v1-8k", "moonshot-v1-32k", "moonshot-v1-128k" },
            AIProviderType.Ollama => new[] { "llama3", "mistral", "qwen" },
            _ => Array.Empty<string>()
        };

        ModelComboBox.ItemsSource = models;
        ModelComboBox.IsEditable = true;
        if (_config != null && !string.IsNullOrEmpty(_config.ModelId))
        {
            ModelComboBox.Text = _config.ModelId;
        }
        else if (models.Any())
        {
            ModelComboBox.SelectedIndex = 0;
        }
    }

    private string GetDefaultBaseUrl(AIProviderType type)
    {
        return type switch
        {
            AIProviderType.OpenAI => "https://api.openai.com/v1",
            AIProviderType.GoogleGemini => "https://generativelanguage.googleapis.com/v1beta",
            AIProviderType.AnthropicClaude => "https://api.anthropic.com/v1",
            AIProviderType.AlibabaQwen => "https://dashscope.aliyuncs.com/api/v1",
            AIProviderType.ModelScope => "https://api-inference.modelscope.cn/v1",
            AIProviderType.SiliconFlow => "https://api.siliconflow.cn/v1",
            AIProviderType.BaiduErnie => "https://aip.baidubce.com/rpc/2.0/ai_custom/v1/wenxinworkshop/chat",
            AIProviderType.ZhipuGLM => "https://open.bigmodel.cn/api/paas/v4",
            AIProviderType.MoonshotAI => "https://api.moonshot.cn/v1",
            AIProviderType.Ollama => "http://localhost:11434",
            _ => ""
        };
    }

    private void RefreshApiKeysList()
    {
        var viewModels = _apiKeys.Select(k => new ApiKeyViewModel
        {
            Id = k.Id,
            KeyName = k.KeyName,
            MaskedKey = MaskApiKey(k.EncryptedKey),
            UsageCount = k.UsageCount,
            TodayUsage = k.TodayUsage,
            DailyLimitDisplay = k.DailyLimit?.ToString() ?? "无限制",
            IsActive = k.IsActive,
            StatusColor = k.IsActive ? "#4CAF50" : "#999999",
            StatusText = k.IsActive ? "启用" : "禁用"
        }).ToList();

        ApiKeysList.ItemsSource = viewModels;
    }

    private string MaskApiKey(string encryptedKey)
    {
        if (string.IsNullOrEmpty(encryptedKey)) return "";
        if (encryptedKey.Length <= 8) return "***";
        return encryptedKey.Substring(0, 4) + "***" + encryptedKey.Substring(encryptedKey.Length - 4);
    }

    private void AddApiKey_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new AddApiKeyDialog();
        if (dialog.ShowDialog() == true)
        {
            _ = AddNewApiKeyAsync(dialog.KeyName, dialog.ApiKey, dialog.DailyLimit);
        }
    }

    private async Task AddNewApiKeyAsync(string keyName, string apiKey, int? dailyLimit)
    {
        try
        {
            var newKey = await _providerService.AddApiKeyAsync(_providerId, keyName, apiKey);
            if (dailyLimit.HasValue)
            {
                newKey.DailyLimit = dailyLimit.Value;
                await _providerService.UpdateApiKeyAsync(newKey);
            }

            _apiKeys.Add(newKey);
            RefreshApiKeysList();

            MessageBox.Show("API Key 添加成功", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"添加失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void DeleteApiKey_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not int keyId)
            return;

        var result = MessageBox.Show(
            "确定要删除这个 API Key 吗？",
            "确认删除",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning
        );

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                await _providerService.DeleteApiKeyAsync(keyId);
                _apiKeys.RemoveAll(k => k.Id == keyId);
                RefreshApiKeysList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"删除失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async void Test_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var button = sender as Button;
            if (button != null)
            {
                button.IsEnabled = false;
                button.Content = "测试中...";
            }

            var isHealthy = await _providerService.TestConnectionAsync(_providerId);

            if (isHealthy)
            {
                MessageBox.Show("✅ 连接测试成功！", "测试结果", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("❌ 连接测试失败，请检查配置", "测试结果", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            if (button != null)
            {
                button.IsEnabled = true;
                button.Content = "测试连接";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"测试失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_config == null) return;

            // 更新基本信息
            _config.Name = NameTextBox.Text;
            _config.ModelId = ModelComboBox.Text ?? "";
            _config.BaseUrl = BaseUrlTextBox.Text;
            _config.IsEnabled = EnabledCheckBox.IsChecked ?? true;
            _config.Priority = int.TryParse(PriorityTextBox.Text, out var priority) ? priority : 0;

            // 更新设置
            if (_config.Settings == null)
            {
                _config.Settings = new AIProviderSettings { ProviderId = _config.Id };
            }

            _config.Settings.Temperature = double.TryParse(TemperatureTextBox.Text, out var temp) ? temp : 0.7;
            _config.Settings.MaxTokens = int.TryParse(MaxTokensTextBox.Text, out var maxTokens) ? maxTokens : 2000;
            _config.Settings.TimeoutSeconds = int.TryParse(TimeoutTextBox.Text, out var timeout) ? timeout : 60;
            _config.Settings.MaxRetries = int.TryParse(MaxRetriesTextBox.Text, out var retries) ? retries : 3;
            _config.Settings.RpmLimit = int.TryParse(RpmLimitTextBox.Text, out var rpm) ? rpm : null;

            await _providerService.UpdateProviderAsync(_config);

            MessageBox.Show("保存成功", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"保存失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

    public class ApiKeyViewModel
    {
        public int Id { get; set; }
        public string KeyName { get; set; } = "";
        public string MaskedKey { get; set; } = "";
        public int UsageCount { get; set; }
        public int TodayUsage { get; set; }
        public string DailyLimitDisplay { get; set; } = "";
        public bool IsActive { get; set; }
        public string StatusColor { get; set; } = "";
        public string StatusText { get; set; } = "";
    }

    // 简单的添加 API Key 对话框
    public class AddApiKeyDialog : Window
    {
        public string KeyName { get; private set; } = "";
        public string ApiKey { get; private set; } = "";
        public int? DailyLimit { get; private set; }

        private TextBox _keyNameBox;
        private TextBox _apiKeyBox;
        private TextBox _dailyLimitBox;

        public AddApiKeyDialog()
        {
            Title = "添加 API 密钥";
            Width = 500;
            Height = 300;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;

            var grid = new Grid { Margin = new Thickness(20) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // 密钥名称
            var nameLabel = new TextBlock { Text = "密钥名称", Margin = new Thickness(0, 0, 0, 6) };
            Grid.SetRow(nameLabel, 0);
            grid.Children.Add(nameLabel);

            _keyNameBox = new TextBox { Padding = new Thickness(8), Margin = new Thickness(0, 0, 0, 16) };
            Grid.SetRow(_keyNameBox, 1);
            grid.Children.Add(_keyNameBox);

            // API Key
            var keyLabel = new TextBlock { Text = "API Key", Margin = new Thickness(0, 0, 0, 6) };
            Grid.SetRow(keyLabel, 2);
            grid.Children.Add(keyLabel);

            _apiKeyBox = new TextBox { Padding = new Thickness(8), Margin = new Thickness(0, 0, 0, 16) };
            Grid.SetRow(_apiKeyBox, 3);
            grid.Children.Add(_apiKeyBox);

            // 每日限额
            var limitLabel = new TextBlock { Text = "每日限额（可选）", Margin = new Thickness(0, 0, 0, 6) };
            Grid.SetRow(limitLabel, 4);
            grid.Children.Add(limitLabel);

            _dailyLimitBox = new TextBox { Padding = new Thickness(8), Margin = new Thickness(0, 0, 0, 16) };
            Grid.SetRow(_dailyLimitBox, 5);
            grid.Children.Add(_dailyLimitBox);

            // 按钮
            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            Grid.SetRow(buttonPanel, 6);

            var cancelBtn = new Button { Content = "取消", Padding = new Thickness(20, 8, 20, 8), Margin = new Thickness(0, 0, 12, 0) };
            cancelBtn.Click += (s, e) => { DialogResult = false; Close(); };
            buttonPanel.Children.Add(cancelBtn);

            var okBtn = new Button { Content = "确定", Padding = new Thickness(20, 8, 20, 8) };
            okBtn.Click += (s, e) =>
            {
                KeyName = _keyNameBox.Text.Trim();
                ApiKey = _apiKeyBox.Text.Trim();
                if (int.TryParse(_dailyLimitBox.Text, out var limit))
                {
                    DailyLimit = limit;
                }

                if (string.IsNullOrWhiteSpace(KeyName) || string.IsNullOrWhiteSpace(ApiKey))
                {
                    MessageBox.Show("请填写密钥名称和 API Key", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                DialogResult = true;
                Close();
            };
            buttonPanel.Children.Add(okBtn);

            grid.Children.Add(buttonPanel);
            Content = grid;
        }
    }
}
