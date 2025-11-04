using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ColumnAttribute = System.ComponentModel.DataAnnotations.Schema.ColumnAttribute;

namespace FishBrowser.WPF.Models;

/// <summary>
/// AI 提供商配置
/// </summary>
public class AIProviderConfig
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = "";

    [Required]
    public AIProviderType ProviderType { get; set; }

    [Required]
    [MaxLength(100)]
    public string ModelId { get; set; } = "";

    [Required]
    [MaxLength(500)]
    public string BaseUrl { get; set; } = "";

    public bool IsEnabled { get; set; } = true;

    public int Priority { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    // 导航属性
    public List<AIApiKey> ApiKeys { get; set; } = new();
    public AIProviderSettings? Settings { get; set; }
}

/// <summary>
/// AI 提供商类型
/// </summary>
public enum AIProviderType
{
    // 国际厂商
    OpenAI = 1,
    AzureOpenAI = 2,
    GoogleGemini = 3,
    AnthropicClaude = 4,
    Cohere = 5,
    MistralAI = 6,

    // 国内厂商
    AlibabaQwen = 101,
    BaiduErnie = 102,
    TencentHunyuan = 103,
    ZhipuGLM = 104,
    XunfeiSpark = 105,
    MoonshotAI = 106,
    MiniMax = 107,
    ZeroOneYi = 108,
    ModelScope = 109,
    SiliconFlow = 110,

    // 本地部署
    Ollama = 201,
    LMStudio = 202,
    LocalAI = 203,
    Custom = 999
}

/// <summary>
/// API 密钥
/// </summary>
public class AIApiKey
{
    [Key]
    public int Id { get; set; }

    public int ProviderId { get; set; }

    [Required]
    [MaxLength(100)]
    public string KeyName { get; set; } = "";

    [Required]
    public string EncryptedKey { get; set; } = "";

    public int UsageCount { get; set; } = 0;

    public DateTime? LastUsedAt { get; set; }

    public bool IsActive { get; set; } = true;

    public int? DailyLimit { get; set; }

    public int TodayUsage { get; set; } = 0;

    public DateTime LastResetDate { get; set; } = DateTime.Today;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // 导航属性
    [ForeignKey(nameof(ProviderId))]
    public AIProviderConfig? Provider { get; set; }
}

/// <summary>
/// AI 提供商设置
/// </summary>
public class AIProviderSettings
{
    [Key]
    public int Id { get; set; }

    public int ProviderId { get; set; }

    // 通用参数
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 2000;
    public double TopP { get; set; } = 1.0;
    public double FrequencyPenalty { get; set; } = 0;
    public double PresencePenalty { get; set; } = 0;

    // 超时与重试
    public int TimeoutSeconds { get; set; } = 60;
    public int MaxRetries { get; set; } = 3;
    public int RetryDelayMs { get; set; } = 1000;

    // 速率限制
    public int? RpmLimit { get; set; }
    public int? TpmLimit { get; set; }

    // 自定义参数
    public string? CustomParametersJson { get; set; }

    // 导航属性
    [ForeignKey(nameof(ProviderId))]
    public AIProviderConfig? Provider { get; set; }
}

/// <summary>
/// AI 使用日志
/// </summary>
public class AIUsageLog
{
    [Key]
    public int Id { get; set; }

    public int ProviderId { get; set; }
    public int ApiKeyId { get; set; }

    [MaxLength(100)]
    public string ModelId { get; set; } = "";

    public DateTime Timestamp { get; set; } = DateTime.Now;

    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }

    [Column(TypeName = "decimal(10,6)")]
    public decimal Cost { get; set; }

    public int DurationMs { get; set; }

    public bool Success { get; set; }

    [MaxLength(500)]
    public string? ErrorMessage { get; set; }

    [MaxLength(100)]
    public string? RequestId { get; set; }
}

/// <summary>
/// AI 模型定义
/// </summary>
public class AIModelDefinition
{
    [Key]
    [MaxLength(100)]
    public string ModelId { get; set; } = "";

    [Required]
    [MaxLength(200)]
    public string DisplayName { get; set; } = "";

    public AIProviderType ProviderType { get; set; }

    public int ContextWindow { get; set; }

    [Column(TypeName = "decimal(10,6)")]
    public decimal InputPricePer1K { get; set; }

    [Column(TypeName = "decimal(10,6)")]
    public decimal OutputPricePer1K { get; set; }

    public bool SupportsStreaming { get; set; } = true;
    public bool SupportsFunctionCalling { get; set; } = false;
    public bool SupportsVision { get; set; } = false;

    [MaxLength(500)]
    public string? CapabilitiesJson { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
