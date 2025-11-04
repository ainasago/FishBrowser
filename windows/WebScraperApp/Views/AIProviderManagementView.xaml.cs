using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FishBrowser.WPF.Models;
using FishBrowser.WPF.Services;
using FishBrowser.WPF.Views.Dialogs;

namespace FishBrowser.WPF.Views;

public partial class AIProviderManagementView : Page
{
    private readonly IAIProviderService _providerService;
    private readonly ILogService _logger;

    public AIProviderManagementView()
    {
        InitializeComponent();

        // 从 DI 容器获取服务
        var host = App.Current.Resources["Host"] as IHost;
        _providerService = host?.Services.GetRequiredService<IAIProviderService>()!;
        _logger = host?.Services.GetRequiredService<ILogService>()!;

        Loaded += async (s, e) => await LoadProvidersAsync();
    }

    private async Task LoadProvidersAsync()
    {
        try
        {
            var providers = await _providerService.GetAllProvidersAsync();

            if (!providers.Any())
            {
                EmptyState.Visibility = Visibility.Visible;
                ProvidersList.Visibility = Visibility.Collapsed;
                return;
            }

            EmptyState.Visibility = Visibility.Collapsed;
            ProvidersList.Visibility = Visibility.Visible;

            var viewModels = providers.Select(p => new ProviderViewModel
            {
                Id = p.Id,
                Name = p.Name,
                ProviderType = p.ProviderType,
                ProviderTypeDisplay = GetProviderTypeDisplay(p.ProviderType),
                ProviderTypeBadgeColor = GetProviderTypeBadgeColor(p.ProviderType),
                ModelId = p.ModelId,
                ApiKeyCount = p.ApiKeys?.Count ?? 0,
                TodayUsage = p.ApiKeys?.Sum(k => k.TodayUsage) ?? 0,
                IsEnabled = p.IsEnabled,
                StatusColor = p.IsEnabled ? "#4CAF50" : "#999999",
                HealthStatus = "未测试",
                HealthStatusColor = "#999999",
                ResponseTime = "",
                LastUpdated = $"更新于 {p.UpdatedAt:MM-dd HH:mm}"
            }).ToList();

            ProvidersList.ItemsSource = viewModels;

            _logger.LogInfo("AIProviderManagement", $"Loaded {providers.Count} providers");
        }
        catch (Exception ex)
        {
            _logger.LogError("AIProviderManagement", $"Failed to load providers: {ex.Message}", ex.StackTrace);
            MessageBox.Show($"加载失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void QuickSetup_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new AIProviderQuickSetupDialog();
        if (dialog.ShowDialog() == true)
        {
            _ = LoadProvidersAsync();
        }
    }

    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        _ = LoadProvidersAsync();
    }

    private void ViewStats_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("使用统计功能开发中...", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is int providerId)
        {
            var dialog = new AIProviderEditDialog(providerId);
            if (dialog.ShowDialog() == true)
            {
                _ = LoadProvidersAsync();
            }
        }
    }

    private async void Test_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not int providerId)
            return;

        button.IsEnabled = false;
        button.Content = "测试中...";

        try
        {
            var isHealthy = await _providerService.TestConnectionAsync(providerId);
            
            if (isHealthy)
            {
                MessageBox.Show("✅ 连接测试成功！", "测试结果", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("❌ 连接测试失败，请检查配置", "测试结果", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"测试失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            button.IsEnabled = true;
            button.Content = "🧪 测试";
        }
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not int providerId)
            return;

        var result = MessageBox.Show(
            "确定要删除这个 AI 提供商配置吗？\n\n删除后将无法恢复。",
            "确认删除",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning
        );

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                await _providerService.DeleteProviderAsync(providerId);
                MessageBox.Show("删除成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadProvidersAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"删除失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private string GetProviderTypeDisplay(AIProviderType type)
    {
        return type switch
        {
            AIProviderType.OpenAI => "OpenAI",
            AIProviderType.AzureOpenAI => "Azure OpenAI",
            AIProviderType.GoogleGemini => "Google Gemini",
            AIProviderType.AnthropicClaude => "Claude",
            AIProviderType.AlibabaQwen => "通义千问",
            AIProviderType.ModelScope => "魔塔社区",
            AIProviderType.SiliconFlow => "硅基流动",
            AIProviderType.BaiduErnie => "文心一言",
            AIProviderType.TencentHunyuan => "腾讯混元",
            AIProviderType.ZhipuGLM => "智谱 GLM",
            AIProviderType.XunfeiSpark => "讯飞星火",
            AIProviderType.MoonshotAI => "Moonshot",
            AIProviderType.MiniMax => "MiniMax",
            AIProviderType.ZeroOneYi => "零一万物",
            AIProviderType.Ollama => "Ollama",
            AIProviderType.LMStudio => "LM Studio",
            AIProviderType.LocalAI => "LocalAI",
            _ => type.ToString()
        };
    }

    private string GetProviderTypeBadgeColor(AIProviderType type)
    {
        return type switch
        {
            AIProviderType.OpenAI or AIProviderType.AzureOpenAI => "#10A37F",
            AIProviderType.GoogleGemini => "#4285F4",
            AIProviderType.AnthropicClaude => "#D97757",
            AIProviderType.AlibabaQwen => "#FF6A00",
            AIProviderType.ModelScope => "#624AFF",
            AIProviderType.SiliconFlow => "#00D4AA",
            AIProviderType.BaiduErnie => "#2932E1",
            AIProviderType.TencentHunyuan => "#006EFF",
            AIProviderType.ZhipuGLM => "#1E88E5",
            AIProviderType.MoonshotAI => "#7C3AED",
            AIProviderType.Ollama or AIProviderType.LMStudio or AIProviderType.LocalAI => "#6B7280",
            _ => "#0078D4"
        };
    }
}

// ViewModel for display
public class ProviderViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public AIProviderType ProviderType { get; set; }
    public string ProviderTypeDisplay { get; set; } = "";
    public string ProviderTypeBadgeColor { get; set; } = "";
    public string ModelId { get; set; } = "";
    public int ApiKeyCount { get; set; }
    public int TodayUsage { get; set; }
    public bool IsEnabled { get; set; }
    public string StatusColor { get; set; } = "";
    public string HealthStatus { get; set; } = "";
    public string HealthStatusColor { get; set; } = "";
    public string ResponseTime { get; set; } = "";
    public string LastUpdated { get; set; } = "";
}
