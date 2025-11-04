using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using FishBrowser.WPF.Models;

namespace FishBrowser.WPF.Services.AIProviderAdapters;

/// <summary>
/// Google Gemini 适配器
/// </summary>
public class GeminiAdapter : BaseAdapter
{
    public GeminiAdapter(ILogService logger) : base(logger)
    {
    }

    public override async Task<AIResponse> CallAsync(AIProviderConfig config, AIRequest request, string apiKey)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            // Gemini API URL: https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}
            var url = $"{config.BaseUrl.TrimEnd('/')}/models/{config.ModelId}:generateContent?key={apiKey}";

            // 构建内容
            var parts = new List<object>();

            if (!string.IsNullOrEmpty(request.SystemPrompt))
            {
                parts.Add(new { text = $"System: {request.SystemPrompt}\n\n" });
            }

            if (request.History != null && request.History.Any())
            {
                foreach (var msg in request.History)
                {
                    parts.Add(new { text = $"{msg.Role}: {msg.Content}\n" });
                }
            }

            parts.Add(new { text = request.UserPrompt });

            var payload = new
            {
                contents = new[]
                {
                    new { parts }
                },
                generationConfig = new
                {
                    temperature = request.Temperature ?? config.Settings?.Temperature ?? 0.7,
                    maxOutputTokens = request.MaxTokens ?? config.Settings?.MaxTokens ?? 2000,
                    topP = config.Settings?.TopP ?? 1.0
                }
            };

            var response = await PostJsonAsync<GeminiResponse>(url, payload);

            sw.Stop();

            if (response?.Candidates == null || !response.Candidates.Any())
            {
                return new AIResponse
                {
                    Success = false,
                    ErrorMessage = "No response from Gemini",
                    DurationMs = (int)sw.ElapsedMilliseconds
                };
            }

            var candidate = response.Candidates[0];
            var text = candidate.Content?.Parts?.FirstOrDefault()?.Text ?? "";

            // Gemini 不直接返回 token 使用量，需要估算
            var promptTokens = EstimateTokens(request.UserPrompt);
            var completionTokens = EstimateTokens(text);

            return new AIResponse
            {
                Content = text,
                PromptTokens = promptTokens,
                CompletionTokens = completionTokens,
                TotalTokens = promptTokens + completionTokens,
                Cost = 0,
                ModelUsed = config.ModelId,
                DurationMs = (int)sw.ElapsedMilliseconds,
                Success = true
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError("GeminiAdapter", $"API call failed: {ex.Message}", ex.StackTrace);

            return new AIResponse
            {
                Success = false,
                ErrorMessage = ex.Message,
                DurationMs = (int)sw.ElapsedMilliseconds
            };
        }
    }

    private int EstimateTokens(string text)
    {
        // 简单估算：英文 ~4 字符 = 1 token，中文 ~1.5 字符 = 1 token
        if (string.IsNullOrEmpty(text)) return 0;
        return text.Length / 3;
    }

    #region Response Models

    private class GeminiResponse
    {
        public List<GeminiCandidate>? Candidates { get; set; }
        public GeminiUsageMetadata? UsageMetadata { get; set; }
    }

    private class GeminiCandidate
    {
        public GeminiContent? Content { get; set; }
        public string? FinishReason { get; set; }
        public int Index { get; set; }
    }

    private class GeminiContent
    {
        public List<GeminiPart>? Parts { get; set; }
        public string? Role { get; set; }
    }

    private class GeminiPart
    {
        public string? Text { get; set; }
    }

    private class GeminiUsageMetadata
    {
        public int PromptTokenCount { get; set; }
        public int CandidatesTokenCount { get; set; }
        public int TotalTokenCount { get; set; }
    }

    #endregion
}
