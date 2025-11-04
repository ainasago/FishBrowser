using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using FishBrowser.WPF.Models;

namespace FishBrowser.WPF.Services.AIProviderAdapters;

/// <summary>
/// OpenAI 适配器
/// </summary>
public class OpenAIAdapter : BaseAdapter
{
    public OpenAIAdapter(ILogService logger) : base(logger)
    {
    }

    public override async Task<AIResponse> CallAsync(AIProviderConfig config, AIRequest request, string apiKey)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            var url = $"{config.BaseUrl.TrimEnd('/')}/chat/completions";

            var messages = new List<object>();

            if (!string.IsNullOrEmpty(request.SystemPrompt))
            {
                messages.Add(new { role = "system", content = request.SystemPrompt });
            }

            if (request.History != null && request.History.Any())
            {
                foreach (var msg in request.History)
                {
                    messages.Add(new { role = msg.Role, content = msg.Content });
                }
            }

            messages.Add(new { role = "user", content = request.UserPrompt });

            var payload = new
            {
                model = config.ModelId,
                messages,
                temperature = request.Temperature ?? config.Settings?.Temperature ?? 0.7,
                max_tokens = request.MaxTokens ?? config.Settings?.MaxTokens ?? 2000,
                top_p = config.Settings?.TopP ?? 1.0,
                frequency_penalty = config.Settings?.FrequencyPenalty ?? 0,
                presence_penalty = config.Settings?.PresencePenalty ?? 0,
                stream = request.Stream
            };

            var response = await PostJsonAsync<OpenAIResponse>(url, payload, req =>
            {
                req.Headers.Add("Authorization", $"Bearer {apiKey}");
            });

            sw.Stop();

            if (response?.Choices == null || !response.Choices.Any())
            {
                return new AIResponse
                {
                    Success = false,
                    ErrorMessage = "No response from OpenAI",
                    DurationMs = (int)sw.ElapsedMilliseconds
                };
            }

            var choice = response.Choices[0];
            var usage = response.Usage;

            return new AIResponse
            {
                Content = choice.Message?.Content ?? "",
                PromptTokens = usage?.PromptTokens ?? 0,
                CompletionTokens = usage?.CompletionTokens ?? 0,
                TotalTokens = usage?.TotalTokens ?? 0,
                Cost = 0, // 将在 AIClientService 中计算
                ModelUsed = response.Model ?? config.ModelId,
                DurationMs = (int)sw.ElapsedMilliseconds,
                Success = true
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError("OpenAIAdapter", $"API call failed: {ex.Message}", ex.StackTrace);

            return new AIResponse
            {
                Success = false,
                ErrorMessage = ex.Message,
                DurationMs = (int)sw.ElapsedMilliseconds
            };
        }
    }

    #region Response Models

    private class OpenAIResponse
    {
        public string? Id { get; set; }
        public string? Object { get; set; }
        public long Created { get; set; }
        public string? Model { get; set; }
        public List<OpenAIChoice>? Choices { get; set; }
        public OpenAIUsage? Usage { get; set; }
    }

    private class OpenAIChoice
    {
        public int Index { get; set; }
        public OpenAIMessage? Message { get; set; }
        public string? FinishReason { get; set; }
    }

    private class OpenAIMessage
    {
        public string? Role { get; set; }
        public string? Content { get; set; }
    }

    private class OpenAIUsage
    {
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens { get; set; }
    }

    #endregion
}
