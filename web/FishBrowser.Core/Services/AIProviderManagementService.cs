using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using FishBrowser.WPF.Models;
using FishBrowser.WPF.Services;

namespace FishBrowser.WPF.Services;

public class AIProviderManagementService
{
    private readonly IAIProviderService _providerService;
    private readonly LogService _logService;

    public AIProviderManagementService(IAIProviderService providerService, LogService logService)
    {
        _providerService = providerService;
        _logService = logService;
    }

    public async Task<List<AIProviderDto>> GetAllProvidersAsync()
    {
        var providers = await _providerService.GetAllProvidersAsync();
        return providers.Select(p => new AIProviderDto
        {
            Id = p.Id,
            Name = p.Name,
            ProviderType = p.ProviderType,
            ModelId = p.ModelId,
            BaseUrl = p.BaseUrl,
            IsEnabled = p.IsEnabled,
            Priority = p.Priority,
            ApiKeyCount = p.ApiKeys?.Count ?? 0,
            TodayUsage = p.ApiKeys?.Sum(k => k.TodayUsage) ?? 0,
            Settings = p.Settings == null ? null : new AIProviderSettingsDto
            {
                Temperature = p.Settings.Temperature,
                MaxTokens = p.Settings.MaxTokens,
                TimeoutSeconds = p.Settings.TimeoutSeconds,
                MaxRetries = p.Settings.MaxRetries,
                RpmLimit = p.Settings.RpmLimit
            },
            ApiKeys = p.ApiKeys?.Select(k => new ApiKeyDto
            {
                Id = k.Id,
                KeyName = k.KeyName,
                MaskedKey = MaskApiKey(k.EncryptedKey),
                UsageCount = k.UsageCount,
                TodayUsage = k.TodayUsage,
                DailyLimit = k.DailyLimit,
                IsActive = k.IsActive,
                CreatedAt = k.CreatedAt
            }).ToList() ?? new(),
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt
        }).ToList();
    }

    public async Task<AIProviderDto?> GetProviderByIdAsync(int id)
    {
        var provider = await _providerService.GetProviderByIdAsync(id);
        if (provider == null) return null;

        return new AIProviderDto
        {
            Id = provider.Id,
            Name = provider.Name,
            ProviderType = provider.ProviderType,
            ModelId = provider.ModelId,
            BaseUrl = provider.BaseUrl,
            IsEnabled = provider.IsEnabled,
            Priority = provider.Priority,
            ApiKeyCount = provider.ApiKeys?.Count ?? 0,
            TodayUsage = provider.ApiKeys?.Sum(k => k.TodayUsage) ?? 0,
            Settings = provider.Settings == null ? null : new AIProviderSettingsDto
            {
                Temperature = provider.Settings.Temperature,
                MaxTokens = provider.Settings.MaxTokens,
                TimeoutSeconds = provider.Settings.TimeoutSeconds,
                MaxRetries = provider.Settings.MaxRetries,
                RpmLimit = provider.Settings.RpmLimit
            },
            ApiKeys = provider.ApiKeys?.Select(k => new ApiKeyDto
            {
                Id = k.Id,
                KeyName = k.KeyName,
                MaskedKey = MaskApiKey(k.EncryptedKey),
                UsageCount = k.UsageCount,
                TodayUsage = k.TodayUsage,
                DailyLimit = k.DailyLimit,
                IsActive = k.IsActive,
                CreatedAt = k.CreatedAt
            }).ToList() ?? new(),
            CreatedAt = provider.CreatedAt,
            UpdatedAt = provider.UpdatedAt
        };
    }

    public async Task<AIProviderDto> CreateProviderAsync(CreateProviderRequest request)
    {
        var config = new AIProviderConfig
        {
            Name = request.Name,
            ProviderType = request.ProviderType,
            ModelId = request.ModelId,
            BaseUrl = request.BaseUrl,
            IsEnabled = request.IsEnabled,
            Priority = request.Priority,
            Settings = request.Settings == null ? null : new AIProviderSettings
            {
                Temperature = request.Settings.Temperature,
                MaxTokens = request.Settings.MaxTokens,
                TimeoutSeconds = request.Settings.TimeoutSeconds,
                MaxRetries = request.Settings.MaxRetries,
                RpmLimit = request.Settings.RpmLimit
            }
        };

        await _providerService.CreateProviderAsync(config);
        _logService.LogInfo("AIProviderManagement", $"Created provider: {config.Name}");

        return (await GetProviderByIdAsync(config.Id))!;
    }

    public async Task<AIProviderDto> UpdateProviderAsync(int id, UpdateProviderRequest request)
    {
        var provider = await _providerService.GetProviderByIdAsync(id);
        if (provider == null) throw new Exception("Provider not found");

        provider.Name = request.Name;
        provider.ModelId = request.ModelId;
        provider.BaseUrl = request.BaseUrl;
        provider.IsEnabled = request.IsEnabled;
        provider.Priority = request.Priority;

        if (request.Settings != null)
        {
            if (provider.Settings == null)
            {
                provider.Settings = new AIProviderSettings { ProviderId = id };
            }
            provider.Settings.Temperature = request.Settings.Temperature;
            provider.Settings.MaxTokens = request.Settings.MaxTokens;
            provider.Settings.TimeoutSeconds = request.Settings.TimeoutSeconds;
            provider.Settings.MaxRetries = request.Settings.MaxRetries;
            provider.Settings.RpmLimit = request.Settings.RpmLimit;
        }

        await _providerService.UpdateProviderAsync(provider);
        _logService.LogInfo("AIProviderManagement", $"Updated provider: {provider.Name}");

        return (await GetProviderByIdAsync(id))!;
    }

    public async Task DeleteProviderAsync(int id)
    {
        await _providerService.DeleteProviderAsync(id);
        _logService.LogInfo("AIProviderManagement", $"Deleted provider: {id}");
    }

    public async Task<ApiKeyDto> AddApiKeyAsync(int providerId, AddApiKeyRequest request)
    {
        var apiKey = await _providerService.AddApiKeyAsync(providerId, request.KeyName, request.ApiKey);
        if (request.DailyLimit.HasValue)
        {
            apiKey.DailyLimit = request.DailyLimit.Value;
            await _providerService.UpdateApiKeyAsync(apiKey);
        }

        _logService.LogInfo("AIProviderManagement", $"Added API key: {request.KeyName} for provider {providerId}");

        return new ApiKeyDto
        {
            Id = apiKey.Id,
            KeyName = apiKey.KeyName,
            MaskedKey = MaskApiKey(apiKey.EncryptedKey),
            UsageCount = apiKey.UsageCount,
            TodayUsage = apiKey.TodayUsage,
            DailyLimit = apiKey.DailyLimit,
            IsActive = apiKey.IsActive,
            CreatedAt = apiKey.CreatedAt
        };
    }

    public async Task DeleteApiKeyAsync(int keyId)
    {
        await _providerService.DeleteApiKeyAsync(keyId);
        _logService.LogInfo("AIProviderManagement", $"Deleted API key: {keyId}");
    }

    public async Task<TestConnectionResult> TestConnectionAsync(int providerId)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var isHealthy = await _providerService.TestConnectionAsync(providerId);
            sw.Stop();

            return new TestConnectionResult
            {
                IsHealthy = isHealthy,
                Message = isHealthy ? "连接成功" : "连接失败",
                ResponseTimeMs = (int)sw.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logService.LogError("AIProviderManagement", $"Test connection failed: {ex.Message}");
            return new TestConnectionResult
            {
                IsHealthy = false,
                Message = ex.Message,
                ResponseTimeMs = (int)sw.ElapsedMilliseconds
            };
        }
    }

    private static string MaskApiKey(string encryptedKey)
    {
        if (string.IsNullOrEmpty(encryptedKey)) return "";
        if (encryptedKey.Length <= 8) return "***";
        return encryptedKey.Substring(0, 4) + "***" + encryptedKey.Substring(encryptedKey.Length - 4);
    }

    public static string GetProviderTypeDisplay(AIProviderType type)
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

    public static string GetProviderTypeBadgeColor(AIProviderType type)
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

    public static string GetDefaultBaseUrl(AIProviderType type)
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
}
