using System;
using System.Collections.Generic;
using FishBrowser.WPF.Models;

namespace FishBrowser.WPF.Models;

public class AIProviderDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public AIProviderType ProviderType { get; set; }
    public string ModelId { get; set; } = "";
    public string BaseUrl { get; set; } = "";
    public bool IsEnabled { get; set; }
    public int Priority { get; set; }
    public int ApiKeyCount { get; set; }
    public int TodayUsage { get; set; }
    public AIProviderSettingsDto? Settings { get; set; }
    public List<ApiKeyDto> ApiKeys { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class AIProviderSettingsDto
{
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 2000;
    public int TimeoutSeconds { get; set; } = 60;
    public int MaxRetries { get; set; } = 3;
    public int? RpmLimit { get; set; }
}

public class ApiKeyDto
{
    public int Id { get; set; }
    public string KeyName { get; set; } = "";
    public string MaskedKey { get; set; } = "";
    public int UsageCount { get; set; }
    public int TodayUsage { get; set; }
    public int? DailyLimit { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class TestConnectionResult
{
    public bool IsHealthy { get; set; }
    public string Message { get; set; } = "";
    public int ResponseTimeMs { get; set; }
}

public class CreateProviderRequest
{
    public string Name { get; set; } = "";
    public AIProviderType ProviderType { get; set; }
    public string ModelId { get; set; } = "";
    public string BaseUrl { get; set; } = "";
    public bool IsEnabled { get; set; } = true;
    public int Priority { get; set; } = 1;
    public AIProviderSettingsDto? Settings { get; set; }
}

public class UpdateProviderRequest
{
    public string Name { get; set; } = "";
    public string ModelId { get; set; } = "";
    public string BaseUrl { get; set; } = "";
    public bool IsEnabled { get; set; }
    public int Priority { get; set; }
    public AIProviderSettingsDto? Settings { get; set; }
}

public class AddApiKeyRequest
{
    public string KeyName { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public int? DailyLimit { get; set; }
}
