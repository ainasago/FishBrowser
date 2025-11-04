using System.Collections.Generic;

namespace FishBrowser.WPF.Models;

/// <summary>
/// AI 请求
/// </summary>
public class AIRequest
{
    public int? ProviderId { get; set; }
    public string SystemPrompt { get; set; } = "";
    public string UserPrompt { get; set; } = "";
    public List<ChatMessage> History { get; set; } = new();
    public double? Temperature { get; set; }
    public int? MaxTokens { get; set; }
    public bool Stream { get; set; } = false;
}

/// <summary>
/// AI 响应
/// </summary>
public class AIResponse
{
    public string Content { get; set; } = "";
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }
    public decimal Cost { get; set; }
    public string ModelUsed { get; set; } = "";
    public int DurationMs { get; set; }
    public bool Success { get; set; } = true;
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// 聊天消息
/// </summary>
public class ChatMessage
{
    public string Role { get; set; } = ""; // user, assistant, system
    public string Content { get; set; } = "";
}

/// <summary>
/// 健康检查结果
/// </summary>
public class HealthCheckResult
{
    public int ProviderId { get; set; }
    public string ProviderName { get; set; } = "";
    public bool IsHealthy { get; set; }
    public int ResponseTimeMs { get; set; }
    public string? ErrorMessage { get; set; }
    public System.DateTime CheckedAt { get; set; } = System.DateTime.Now;
}

/// <summary>
/// 使用统计
/// </summary>
public class AIUsageStats
{
    public int ProviderId { get; set; }
    public int TotalRequests { get; set; }
    public int SuccessRequests { get; set; }
    public int FailedRequests { get; set; }
    public int TotalTokens { get; set; }
    public decimal TotalCost { get; set; }
    public int AverageDurationMs { get; set; }
    public System.DateTime FromDate { get; set; }
    public System.DateTime ToDate { get; set; }
}
