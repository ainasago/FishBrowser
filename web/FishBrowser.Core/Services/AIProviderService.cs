using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FishBrowser.WPF.Data;
using FishBrowser.WPF.Models;
using FishBrowser.WPF.Services.AIProviderAdapters;

namespace FishBrowser.WPF.Services;

/// <summary>
/// AI 提供商服务接口
/// </summary>
public interface IAIProviderService
{
    // 配置管理
    Task<List<AIProviderConfig>> GetAllProvidersAsync();
    Task<AIProviderConfig?> GetProviderByIdAsync(int id);
    Task<AIProviderConfig> CreateProviderAsync(AIProviderConfig config);
    Task UpdateProviderAsync(AIProviderConfig config);
    Task DeleteProviderAsync(int id);
    Task<bool> TestConnectionAsync(int providerId);

    // API Key 管理
    Task<AIApiKey> AddApiKeyAsync(int providerId, string keyName, string apiKey);
    Task UpdateApiKeyAsync(AIApiKey apiKey);
    Task DeleteApiKeyAsync(int keyId);
    Task<string?> GetNextApiKeyAsync(int providerId);

    // 模型查询
    Task<List<AIModelDefinition>> GetAvailableModelsAsync(AIProviderType providerType);
    Task<AIModelDefinition?> GetModelDefinitionAsync(string modelId);

    // 使用统计
    Task<AIUsageStats> GetUsageStatsAsync(int providerId, DateTime from, DateTime to);
    Task RecordUsageAsync(AIUsageLog log);
}

/// <summary>
/// AI 提供商服务实现
/// </summary>
public class AIProviderService : IAIProviderService
{
    private readonly WebScraperDbContext _db;
    private readonly ILogService _logger;
    private static readonly byte[] _entropy = Encoding.UTF8.GetBytes("WebScraperAI2025");

    public AIProviderService(WebScraperDbContext db, ILogService logger)
    {
        _db = db;
        _logger = logger;
    }

    #region 配置管理

    public async Task<List<AIProviderConfig>> GetAllProvidersAsync()
    {
        try
        {
            // 使用 AsSplitQuery 避免笛卡尔积导致的重复数据
            var providers = await _db.AIProviderConfigs
                .Include(p => p.ApiKeys)
                .Include(p => p.Settings)
                .AsSplitQuery()  // 关键：分离查询，避免重复
                .OrderBy(p => p.Priority)
                .ThenBy(p => p.Name)
                .ToListAsync();

            _logger.LogInfo("AIProvider", $"Retrieved {providers.Count} providers");
            return providers;
        }
        catch (Exception ex)
        {
            _logger.LogError("AIProvider", $"Failed to get providers: {ex.Message}", ex.StackTrace);
            throw;
        }
    }

    public async Task<AIProviderConfig?> GetProviderByIdAsync(int id)
    {
        try
        {
            var provider = await _db.AIProviderConfigs
                .Include(p => p.ApiKeys)
                .Include(p => p.Settings)
                .AsSplitQuery()  // 避免重复数据
                .FirstOrDefaultAsync(p => p.Id == id);

            return provider;
        }
        catch (Exception ex)
        {
            _logger.LogError("AIProvider", $"Failed to get provider {id}: {ex.Message}", ex.StackTrace);
            throw;
        }
    }

    public async Task<AIProviderConfig> CreateProviderAsync(AIProviderConfig config)
    {
        try
        {
            config.CreatedAt = DateTime.Now;
            config.UpdatedAt = DateTime.Now;

            _db.AIProviderConfigs.Add(config);
            await _db.SaveChangesAsync();

            _logger.LogInfo("AIProvider", $"Created provider: {config.Name} ({config.ProviderType})");
            return config;
        }
        catch (Exception ex)
        {
            _logger.LogError("AIProvider", $"Failed to create provider: {ex.Message}", ex.StackTrace);
            throw;
        }
    }

    public async Task UpdateProviderAsync(AIProviderConfig config)
    {
        try
        {
            config.UpdatedAt = DateTime.Now;
            _db.AIProviderConfigs.Update(config);
            await _db.SaveChangesAsync();

            _logger.LogInfo("AIProvider", $"Updated provider: {config.Name}");
        }
        catch (Exception ex)
        {
            _logger.LogError("AIProvider", $"Failed to update provider: {ex.Message}", ex.StackTrace);
            throw;
        }
    }

    public async Task DeleteProviderAsync(int id)
    {
        try
        {
            // 加载完整的 Provider 及其关联数据
            var provider = await _db.AIProviderConfigs
                .Include(p => p.ApiKeys)
                .Include(p => p.Settings)
                .FirstOrDefaultAsync(p => p.Id == id);
                
            if (provider != null)
            {
                // 显式删除关联的 API Keys
                if (provider.ApiKeys != null && provider.ApiKeys.Any())
                {
                    _db.AIApiKeys.RemoveRange(provider.ApiKeys);
                }
                
                // 显式删除关联的 Settings
                if (provider.Settings != null)
                {
                    _db.AIProviderSettings.Remove(provider.Settings);
                }
                
                // 删除 Provider
                _db.AIProviderConfigs.Remove(provider);
                
                // 保存更改
                await _db.SaveChangesAsync();
                
                _logger.LogInfo("AIProvider", $"Deleted provider and its related data: {provider.Name} (ID: {id})");
            }
            else
            {
                _logger.LogWarn("AIProvider", $"Provider {id} not found for deletion");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("AIProvider", $"Failed to delete provider {id}: {ex.Message}", ex.StackTrace);
            throw;
        }
    }

    public async Task<bool> TestConnectionAsync(int providerId)
    {
        try
        {
            var provider = await GetProviderByIdAsync(providerId);
            if (provider == null) return false;

            _logger.LogInfo("AIProvider", $"Testing connection for provider: {provider.Name}");

            // 获取 API Key
            var apiKey = await GetNextApiKeyAsync(providerId);
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogWarn("AIProvider", "No available API key for testing");
                return false;
            }

            // 发送简单的测试请求
            var testPrompt = "Hello";
            var adapter = CreateAdapter(provider.ProviderType);
            if (adapter == null)
            {
                _logger.LogWarn("AIProvider", $"No adapter for provider type: {provider.ProviderType}");
                return false;
            }

            var request = new AIRequest
            {
                SystemPrompt = "You are a helpful assistant.",
                UserPrompt = testPrompt,
                Temperature = 0.3,
                MaxTokens = 10
            };

            var response = await adapter.CallAsync(provider, request, apiKey);
            
            if (response.Success)
            {
                _logger.LogInfo("AIProvider", $"Connection test succeeded for {provider.Name}");
                return true;
            }
            else
            {
                _logger.LogWarn("AIProvider", $"Connection test failed: {response.ErrorMessage}");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("AIProvider", $"Connection test failed: {ex.Message}", ex.StackTrace);
            return false;
        }
    }

    private IAIProviderAdapter? CreateAdapter(AIProviderType providerType)
    {
        return providerType switch
        {
            AIProviderType.OpenAI => new AIProviderAdapters.OpenAIAdapter(_logger),
            AIProviderType.AzureOpenAI => new AIProviderAdapters.OpenAIAdapter(_logger),
            AIProviderType.GoogleGemini => new AIProviderAdapters.GeminiAdapter(_logger),
            AIProviderType.AlibabaQwen => new AIProviderAdapters.QwenAdapter(_logger),
            AIProviderType.ModelScope => new AIProviderAdapters.ModelScopeAdapter(_logger),
            AIProviderType.SiliconFlow => new AIProviderAdapters.OpenAIAdapter(_logger),
            AIProviderType.Ollama => new AIProviderAdapters.OllamaAdapter(_logger),
            AIProviderType.LMStudio => new AIProviderAdapters.OpenAIAdapter(_logger),
            AIProviderType.LocalAI => new AIProviderAdapters.OpenAIAdapter(_logger),
            _ => null
        };
    }

    #endregion

    #region API Key 管理

    public async Task<AIApiKey> AddApiKeyAsync(int providerId, string keyName, string apiKey)
    {
        try
        {
            var encryptedKey = EncryptApiKey(apiKey);

            var key = new AIApiKey
            {
                ProviderId = providerId,
                KeyName = keyName,
                EncryptedKey = encryptedKey,
                IsActive = true,
                CreatedAt = DateTime.Now,
                LastResetDate = DateTime.Today
            };

            _db.AIApiKeys.Add(key);
            await _db.SaveChangesAsync();

            _logger.LogInfo("AIProvider", $"Added API key: {keyName} for provider {providerId}");
            return key;
        }
        catch (Exception ex)
        {
            _logger.LogError("AIProvider", $"Failed to add API key: {ex.Message}", ex.StackTrace);
            throw;
        }
    }

    public async Task UpdateApiKeyAsync(AIApiKey apiKey)
    {
        try
        {
            _db.AIApiKeys.Update(apiKey);
            await _db.SaveChangesAsync();
            _logger.LogInfo("AIProvider", $"Updated API key: {apiKey.KeyName}");
        }
        catch (Exception ex)
        {
            _logger.LogError("AIProvider", $"Failed to update API key: {ex.Message}", ex.StackTrace);
            throw;
        }
    }

    public async Task DeleteApiKeyAsync(int keyId)
    {
        try
        {
            var key = await _db.AIApiKeys.FindAsync(keyId);
            if (key != null)
            {
                _db.AIApiKeys.Remove(key);
                await _db.SaveChangesAsync();
                _logger.LogInfo("AIProvider", $"Deleted API key: {key.KeyName}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("AIProvider", $"Failed to delete API key {keyId}: {ex.Message}", ex.StackTrace);
            throw;
        }
    }

    public async Task<string?> GetNextApiKeyAsync(int providerId)
    {
        try
        {
            // 重置每日使用量（如果是新的一天）
            await ResetDailyUsageIfNeededAsync(providerId);

            // 获取可用的 API Key（按使用次数排序，实现轮询）
            var key = await _db.AIApiKeys
                .Where(k => k.ProviderId == providerId && k.IsActive)
                .Where(k => !k.DailyLimit.HasValue || k.TodayUsage < k.DailyLimit.Value)
                .OrderBy(k => k.UsageCount)
                .ThenBy(k => k.LastUsedAt ?? DateTime.MinValue)
                .FirstOrDefaultAsync();

            if (key == null)
            {
                _logger.LogWarn("AIProvider", $"No available API key for provider {providerId}");
                return null;
            }

            // 更新使用统计
            key.UsageCount++;
            key.TodayUsage++;
            key.LastUsedAt = DateTime.Now;
            await _db.SaveChangesAsync();

            return DecryptApiKey(key.EncryptedKey);
        }
        catch (Exception ex)
        {
            _logger.LogError("AIProvider", $"Failed to get next API key: {ex.Message}", ex.StackTrace);
            throw;
        }
    }

    private async Task ResetDailyUsageIfNeededAsync(int providerId)
    {
        var keys = await _db.AIApiKeys
            .Where(k => k.ProviderId == providerId && k.LastResetDate < DateTime.Today)
            .ToListAsync();

        foreach (var key in keys)
        {
            key.TodayUsage = 0;
            key.LastResetDate = DateTime.Today;
        }

        if (keys.Any())
        {
            await _db.SaveChangesAsync();
            _logger.LogInfo("AIProvider", $"Reset daily usage for {keys.Count} keys");
        }
    }

    #endregion

    #region 模型查询

    public async Task<List<AIModelDefinition>> GetAvailableModelsAsync(AIProviderType providerType)
    {
        try
        {
            var models = await _db.AIModelDefinitions
                .Where(m => m.ProviderType == providerType)
                .OrderBy(m => m.DisplayName)
                .ToListAsync();

            return models;
        }
        catch (Exception ex)
        {
            _logger.LogError("AIProvider", $"Failed to get models: {ex.Message}", ex.StackTrace);
            throw;
        }
    }

    public async Task<AIModelDefinition?> GetModelDefinitionAsync(string modelId)
    {
        try
        {
            return await _db.AIModelDefinitions.FindAsync(modelId);
        }
        catch (Exception ex)
        {
            _logger.LogError("AIProvider", $"Failed to get model definition: {ex.Message}", ex.StackTrace);
            throw;
        }
    }

    #endregion

    #region 使用统计

    public async Task<AIUsageStats> GetUsageStatsAsync(int providerId, DateTime from, DateTime to)
    {
        try
        {
            var logs = await _db.AIUsageLogs
                .Where(l => l.ProviderId == providerId)
                .Where(l => l.Timestamp >= from && l.Timestamp <= to)
                .ToListAsync();

            var stats = new AIUsageStats
            {
                ProviderId = providerId,
                TotalRequests = logs.Count,
                SuccessRequests = logs.Count(l => l.Success),
                FailedRequests = logs.Count(l => !l.Success),
                TotalTokens = logs.Sum(l => l.TotalTokens),
                TotalCost = logs.Sum(l => l.Cost),
                AverageDurationMs = logs.Any() ? (int)logs.Average(l => l.DurationMs) : 0,
                FromDate = from,
                ToDate = to
            };

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError("AIProvider", $"Failed to get usage stats: {ex.Message}", ex.StackTrace);
            throw;
        }
    }

    public async Task RecordUsageAsync(AIUsageLog log)
    {
        try
        {
            _db.AIUsageLogs.Add(log);
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError("AIProvider", $"Failed to record usage: {ex.Message}", ex.StackTrace);
        }
    }

    #endregion

    #region 加密/解密

    private string EncryptApiKey(string plainText)
    {
        try
        {
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var encryptedBytes = ProtectedData.Protect(plainBytes, _entropy, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encryptedBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError("AIProvider", $"Failed to encrypt API key: {ex.Message}", ex.StackTrace);
            throw;
        }
    }

    private string DecryptApiKey(string encryptedText)
    {
        try
        {
            var encryptedBytes = Convert.FromBase64String(encryptedText);
            var plainBytes = ProtectedData.Unprotect(encryptedBytes, _entropy, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(plainBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError("AIProvider", $"Failed to decrypt API key: {ex.Message}", ex.StackTrace);
            throw;
        }
    }

    #endregion
}
