using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FishBrowser.WPF.Models;
using FishBrowser.WPF.Services;

namespace FishBrowser.WPF.Views.Dialogs;

public partial class AIProviderQuickSetupDialog : Window
{
    private readonly IAIProviderService _providerService;
    private readonly ILogService _logger;
    private int _currentStep = 1;
    private AIProviderType _selectedProviderType;
    private string _selectedModelId = "";
    private readonly Dictionary<AIProviderType, List<ModelOption>> _modelOptions;

    public AIProviderQuickSetupDialog()
    {
        InitializeComponent();

        // 从 DI 容器获取服务
        var host = App.Current.Resources["Host"] as IHost;
        _providerService = host?.Services.GetRequiredService<IAIProviderService>()!;
        _logger = host?.Services.GetRequiredService<ILogService>()!;

        _modelOptions = InitializeModelOptions();
    }

    private Dictionary<AIProviderType, List<ModelOption>> InitializeModelOptions()
    {
        return new Dictionary<AIProviderType, List<ModelOption>>
        {
            {
                AIProviderType.OpenAI, new List<ModelOption>
                {
                    new() { ModelId = "gpt-4-turbo", DisplayName = "GPT-4 Turbo", Description = "最强大，适合复杂任务", PriceDisplay = "$0.01/1K tokens", ContextDisplay = "128K 上下文", IsRecommended = false },
                    new() { ModelId = "gpt-3.5-turbo", DisplayName = "GPT-3.5 Turbo", Description = "性价比高，适合日常使用（推荐）", PriceDisplay = "$0.0005/1K tokens", ContextDisplay = "16K 上下文", IsRecommended = true, IsSelected = true }
                }
            },
            {
                AIProviderType.GoogleGemini, new List<ModelOption>
                {
                    new() { ModelId = "gemini-pro", DisplayName = "Gemini Pro", Description = "免费额度大，性能优秀（推荐）", PriceDisplay = "免费额度", ContextDisplay = "32K 上下文", IsRecommended = true, IsSelected = true }
                }
            },
            {
                AIProviderType.AnthropicClaude, new List<ModelOption>
                {
                    new() { ModelId = "claude-3-sonnet-20240229", DisplayName = "Claude 3 Sonnet", Description = "平衡性能与成本（推荐）", PriceDisplay = "$0.003/1K tokens", ContextDisplay = "200K 上下文", IsRecommended = true, IsSelected = true },
                    new() { ModelId = "claude-3-opus-20240229", DisplayName = "Claude 3 Opus", Description = "最强大，适合复杂任务", PriceDisplay = "$0.015/1K tokens", ContextDisplay = "200K 上下文", IsRecommended = false }
                }
            },
            {
                AIProviderType.AlibabaQwen, new List<ModelOption>
                {
                    new() { ModelId = "qwen-plus", DisplayName = "通义千问-Plus", Description = "性价比高，中文优秀（推荐）", PriceDisplay = "¥0.004/1K tokens", ContextDisplay = "8K 上下文", IsRecommended = true, IsSelected = true },
                    new() { ModelId = "qwen-max", DisplayName = "通义千问-Max", Description = "最强大，适合复杂任务", PriceDisplay = "¥0.04/1K tokens", ContextDisplay = "8K 上下文", IsRecommended = false }
                }
            },
            {
                AIProviderType.ZhipuGLM, new List<ModelOption>
                {
                    new() { ModelId = "glm-4", DisplayName = "GLM-4", Description = "最新模型，中文优秀（推荐）", PriceDisplay = "¥0.05/1K tokens", ContextDisplay = "128K 上下文", IsRecommended = true, IsSelected = true }
                }
            },
            {
                AIProviderType.MoonshotAI, new List<ModelOption>
                {
                    new() { ModelId = "moonshot-v1-8k", DisplayName = "Moonshot 8K", Description = "性价比高（推荐）", PriceDisplay = "¥0.012/1K tokens", ContextDisplay = "8K 上下文", IsRecommended = true, IsSelected = true },
                    new() { ModelId = "moonshot-v1-32k", DisplayName = "Moonshot 32K", Description = "长上下文", PriceDisplay = "¥0.024/1K tokens", ContextDisplay = "32K 上下文", IsRecommended = false },
                    new() { ModelId = "moonshot-v1-128k", DisplayName = "Moonshot 128K", Description = "超长上下文", PriceDisplay = "¥0.06/1K tokens", ContextDisplay = "128K 上下文", IsRecommended = false }
                }
            },
            {
                AIProviderType.ModelScope, new List<ModelOption>
                {
                    new() { ModelId = "Qwen/Qwen2.5-32B-Instruct", DisplayName = "Qwen2.5-32B", Description = "平衡性能，中文优秀（推荐）", PriceDisplay = "免费使用", ContextDisplay = "32K 上下文", IsRecommended = true, IsSelected = true },
                    new() { ModelId = "Qwen/Qwen2.5-72B-Instruct", DisplayName = "Qwen2.5-72B", Description = "最强性能", PriceDisplay = "免费使用", ContextDisplay = "32K 上下文", IsRecommended = false },
                    new() { ModelId = "Qwen/Qwen2.5-Coder-32B-Instruct", DisplayName = "Qwen2.5-Coder-32B", Description = "代码专用模型", PriceDisplay = "免费使用", ContextDisplay = "32K 上下文", IsRecommended = false }
                }
            },
            {
                AIProviderType.SiliconFlow, new List<ModelOption>
                {
                    new() { ModelId = "Qwen/QwQ-32B", DisplayName = "QwQ-32B", Description = "推理专用模型（推荐）", PriceDisplay = "¥0.42/M tokens", ContextDisplay = "32K 上下文", IsRecommended = true, IsSelected = true },
                    new() { ModelId = "deepseek-ai/DeepSeek-V3", DisplayName = "DeepSeek-V3", Description = "最强性能", PriceDisplay = "¥0.14/M tokens", ContextDisplay = "64K 上下文", IsRecommended = false },
                    new() { ModelId = "Qwen/Qwen2.5-72B-Instruct", DisplayName = "Qwen2.5-72B", Description = "高性能通用", PriceDisplay = "¥0.42/M tokens", ContextDisplay = "32K 上下文", IsRecommended = false }
                }
            },
            {
                AIProviderType.Ollama, new List<ModelOption>
                {
                    new() { ModelId = "llama3", DisplayName = "Llama 3", Description = "Meta 开源模型（推荐）", PriceDisplay = "完全免费", ContextDisplay = "8K 上下文", IsRecommended = true, IsSelected = true },
                    new() { ModelId = "mistral", DisplayName = "Mistral", Description = "欧洲开源模型", PriceDisplay = "完全免费", ContextDisplay = "8K 上下文", IsRecommended = false },
                    new() { ModelId = "qwen", DisplayName = "Qwen", Description = "阿里开源模型", PriceDisplay = "完全免费", ContextDisplay = "8K 上下文", IsRecommended = false }
                }
            }
        };
    }

    private void SelectProvider_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Border border || border.Tag is not string providerTag)
            return;

        _selectedProviderType = Enum.Parse<AIProviderType>(providerTag);
        NextButton.IsEnabled = true;

        // 高亮选中的卡片
        HighlightSelectedCard(border);
    }

    private void HighlightSelectedCard(Border selectedCard)
    {
        // 重置所有卡片
        var panel = selectedCard.Parent as Panel;
        if (panel != null)
        {
            foreach (var child in panel.Children)
            {
                if (child is Border card)
                {
                    card.Style = (Style)FindResource("ProviderCardStyle");
                }
            }
        }

        // 高亮选中的卡片
        selectedCard.Style = (Style)FindResource("SelectedCardStyle");
    }

    private void SelectModel_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Border border || border.Tag is not string modelId)
            return;

        _selectedModelId = modelId;

        // 更新所有模型的选中状态
        var models = ModelsList.ItemsSource as List<ModelOption>;
        if (models != null)
        {
            foreach (var model in models)
            {
                model.IsSelected = model.ModelId == modelId;
            }
            ModelsList.Items.Refresh();
        }

        NextButton.IsEnabled = true;
    }

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        _currentStep--;
        UpdateStep();
    }

    private void Next_Click(object sender, RoutedEventArgs e)
    {
        if (_currentStep < 3)
        {
            _currentStep++;
            UpdateStep();
        }
        else
        {
            // 完成配置
            _ = SaveConfigurationAsync();
        }
    }

    private void UpdateStep()
    {
        Step1Panel.Visibility = _currentStep == 1 ? Visibility.Visible : Visibility.Collapsed;
        Step2Panel.Visibility = _currentStep == 2 ? Visibility.Visible : Visibility.Collapsed;
        Step3Panel.Visibility = _currentStep == 3 ? Visibility.Visible : Visibility.Collapsed;

        BackButton.Visibility = _currentStep > 1 ? Visibility.Visible : Visibility.Collapsed;
        NextButton.IsEnabled = false;

        switch (_currentStep)
        {
            case 1:
                StepTitle.Text = "步骤 1/3：选择 AI 提供商";
                StepDescription.Text = "选择你想使用的 AI 服务提供商";
                NextButton.Content = "下一步 →";
                break;

            case 2:
                StepTitle.Text = "步骤 2/3：选择模型";
                StepDescription.Text = "选择适合你需求的模型";
                NextButton.Content = "下一步 →";
                LoadModels();
                break;

            case 3:
                StepTitle.Text = "步骤 3/3：配置 API 密钥";
                StepDescription.Text = "输入你的 API 密钥完成配置";
                NextButton.Content = "完成并测试";
                LoadApiKeyHelp();
                break;
        }
    }

    private void LoadModels()
    {
        SelectedProviderText.Text = $"已选择：{GetProviderDisplayName(_selectedProviderType)}";

        if (_modelOptions.TryGetValue(_selectedProviderType, out var models))
        {
            ModelsList.ItemsSource = models;
            _selectedModelId = models.FirstOrDefault(m => m.IsSelected)?.ModelId ?? models.First().ModelId;
            NextButton.IsEnabled = true;
        }
    }

    private void LoadApiKeyHelp()
    {
        var providerName = GetProviderDisplayName(_selectedProviderType);
        var modelName = _modelOptions[_selectedProviderType].First(m => m.ModelId == _selectedModelId).DisplayName;
        
        Step3ProviderText.Text = $"配置：{providerName} {modelName}";
        ConfigNameTextBox.Text = $"{providerName} {modelName}";

        ApiKeyHelpText.Text = _selectedProviderType switch
        {
            AIProviderType.OpenAI => "访问 https://platform.openai.com/api-keys → 点击 \"Create new secret key\" → 复制密钥",
            AIProviderType.GoogleGemini => "访问 https://ai.google.dev → 点击 \"Get API key\" → 创建或选择项目 → 复制密钥",
            AIProviderType.AnthropicClaude => "访问 https://console.anthropic.com → 创建 API Key → 复制密钥",
            AIProviderType.AlibabaQwen => "访问 https://dashscope.aliyun.com → 登录阿里云账号 → 创建 API Key → 复制密钥",
            AIProviderType.ModelScope => "访问 https://www.modelscope.cn → 登录账号 → 个人中心 → API-KEY 管理 → 创建密钥 → 复制密钥",
            AIProviderType.SiliconFlow => "访问 https://cloud.siliconflow.cn → 注册/登录 → API 密钥 → 创建新密钥 → 复制密钥",
            AIProviderType.ZhipuGLM => "访问 https://open.bigmodel.cn → 注册账号 → 创建 API Key → 复制密钥",
            AIProviderType.MoonshotAI => "访问 https://platform.moonshot.cn → 注册账号 → 创建 API Key → 复制密钥",
            AIProviderType.Ollama => "Ollama 是本地运行，无需 API Key。\n确保已安装 Ollama 并运行 \"ollama pull " + _selectedModelId + "\"",
            _ => "请参考提供商官网获取 API Key"
        };

        // Ollama 不需要 API Key
        if (_selectedProviderType == AIProviderType.Ollama)
        {
            ApiKeyTextBox.IsEnabled = false;
            ApiKeyTextBox.Text = "本地运行，无需密钥";
            NextButton.IsEnabled = true;
        }
    }

    private void ApiKeyTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        // Ollama 不需要 API Key，其他提供商需要
        if (_selectedProviderType == AIProviderType.Ollama)
        {
            NextButton.IsEnabled = true;
        }
        else
        {
            NextButton.IsEnabled = !string.IsNullOrWhiteSpace(ApiKeyTextBox.Text);
        }
    }

    private void PasteApiKey_Click(object sender, RoutedEventArgs e)
    {
        if (Clipboard.ContainsText())
        {
            ApiKeyTextBox.Text = Clipboard.GetText().Trim();
            NextButton.IsEnabled = !string.IsNullOrWhiteSpace(ApiKeyTextBox.Text);
        }
    }

    private async Task SaveConfigurationAsync()
    {
        try
        {
            NextButton.IsEnabled = false;
            NextButton.Content = "配置中...";

            var baseUrl = GetBaseUrl(_selectedProviderType);
            var configName = string.IsNullOrWhiteSpace(ConfigNameTextBox.Text) 
                ? $"{GetProviderDisplayName(_selectedProviderType)} {_selectedModelId}"
                : ConfigNameTextBox.Text;

            var config = new AIProviderConfig
            {
                Name = configName,
                ProviderType = _selectedProviderType,
                ModelId = _selectedModelId,
                BaseUrl = baseUrl,
                IsEnabled = true,
                Priority = 1,
                Settings = new AIProviderSettings
                {
                    Temperature = 0.7,
                    MaxTokens = 2000,
                    TimeoutSeconds = 60,
                    MaxRetries = 3
                }
            };

            await _providerService.CreateProviderAsync(config);

            // 添加 API Key（Ollama 除外）
            if (_selectedProviderType != AIProviderType.Ollama)
            {
                var apiKey = ApiKeyTextBox.Text.Trim();
                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    await _providerService.AddApiKeyAsync(config.Id, "主密钥", apiKey);
                }
            }

            _logger.LogInfo("AIProviderSetup", $"Created provider: {config.Name}");

            // 测试连接
            NextButton.Content = "测试连接...";
            var isHealthy = await _providerService.TestConnectionAsync(config.Id);

            if (isHealthy)
            {
                MessageBox.Show(
                    $"✅ 配置成功！\n\n提供商：{configName}\n模型：{_selectedModelId}\n\n现在可以在「AI 任务」中使用了。",
                    "配置成功",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
                DialogResult = true;
                Close();
            }
            else
            {
                var result = MessageBox.Show(
                    "⚠️ 配置已保存，但连接测试失败。\n\n可能原因：\n• API Key 无效\n• 网络连接问题\n• 服务暂时不可用\n\n是否保留此配置？",
                    "连接测试失败",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning
                );

                if (result == MessageBoxResult.Yes)
                {
                    DialogResult = true;
                    Close();
                }
                else
                {
                    await _providerService.DeleteProviderAsync(config.Id);
                    NextButton.IsEnabled = true;
                    NextButton.Content = "完成并测试";
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("AIProviderSetup", $"Failed to save configuration: {ex.Message}", ex.StackTrace);
            MessageBox.Show($"配置失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            NextButton.IsEnabled = true;
            NextButton.Content = "完成并测试";
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private string GetProviderDisplayName(AIProviderType type)
    {
        return type switch
        {
            AIProviderType.OpenAI => "OpenAI",
            AIProviderType.GoogleGemini => "Google Gemini",
            AIProviderType.AnthropicClaude => "Claude",
            AIProviderType.AlibabaQwen => "通义千问",
            AIProviderType.ModelScope => "魔塔社区",
            AIProviderType.SiliconFlow => "硅基流动",
            AIProviderType.ZhipuGLM => "智谱 GLM",
            AIProviderType.MoonshotAI => "Moonshot",
            AIProviderType.Ollama => "Ollama",
            _ => type.ToString()
        };
    }

    private string GetBaseUrl(AIProviderType type)
    {
        return type switch
        {
            AIProviderType.OpenAI => "https://api.openai.com/v1",
            AIProviderType.GoogleGemini => "https://generativelanguage.googleapis.com/v1beta",
            AIProviderType.AnthropicClaude => "https://api.anthropic.com/v1",
            AIProviderType.AlibabaQwen => "https://dashscope.aliyuncs.com/api/v1",
            AIProviderType.ModelScope => "https://api-inference.modelscope.cn/v1",
            AIProviderType.SiliconFlow => "https://api.siliconflow.cn/v1",
            AIProviderType.ZhipuGLM => "https://open.bigmodel.cn/api/paas/v4",
            AIProviderType.MoonshotAI => "https://api.moonshot.cn/v1",
            AIProviderType.Ollama => "http://localhost:11434",
            _ => ""
        };
    }
}

public class ModelOption
{
    public string ModelId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Description { get; set; } = "";
    public string PriceDisplay { get; set; } = "";
    public string ContextDisplay { get; set; } = "";
    public bool IsRecommended { get; set; }
    public bool IsSelected { get; set; }
}
