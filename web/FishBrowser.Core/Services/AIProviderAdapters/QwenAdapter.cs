using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using FishBrowser.WPF.Models;

namespace FishBrowser.WPF.Services.AIProviderAdapters;

/// <summary>
/// 阿里云通义千问适配器
/// </summary>
public class QwenAdapter : BaseAdapter
{
    public QwenAdapter(ILogService logger) : base(logger)
    {
    }

    public override async Task<AIResponse> CallAsync(AIProviderConfig config, AIRequest request, string apiKey)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            var url = $"{config.BaseUrl.TrimEnd('/')}/services/aigc/text-generation/generation";

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
                input = new { messages },
                parameters = new
                {
                    temperature = request.Temperature ?? config.Settings?.Temperature ?? 0.7,
                    max_tokens = request.MaxTokens ?? config.Settings?.MaxTokens ?? 2000,
                    top_p = config.Settings?.TopP ?? 1.0
                }
            };

            var response = await PostJsonAsync<QwenResponse>(url, payload, req =>
            {
                req.Headers.Add("Authorization", $"Bearer {apiKey}");
            });

            sw.Stop();

            if (response?.Output?.Text == null)
            {
                return new AIResponse
                {
                    Success = false,
                    ErrorMessage = response?.Message ?? "No response from Qwen",
                    DurationMs = (int)sw.ElapsedMilliseconds
                };
            }

            var usage = response.Usage;

            return new AIResponse
            {
                Content = response.Output.Text,
                PromptTokens = usage?.InputTokens ?? 0,
                CompletionTokens = usage?.OutputTokens ?? 0,
                TotalTokens = usage?.TotalTokens ?? 0,
                Cost = 0,
                ModelUsed = config.ModelId,
                DurationMs = (int)sw.ElapsedMilliseconds,
                Success = true
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError("QwenAdapter", $"API call failed: {ex.Message}", ex.StackTrace);

            return new AIResponse
            {
                Success = false,
                ErrorMessage = ex.Message,
                DurationMs = (int)sw.ElapsedMilliseconds
            };
        }
    }

    #region Response Models

    private class QwenResponse
    {
        public QwenOutput? Output { get; set; }
        public QwenUsage? Usage { get; set; }
        public string? RequestId { get; set; }
        public string? Message { get; set; }
    }

    private class QwenOutput
    {
        public string? Text { get; set; }
        public string? FinishReason { get; set; }
    }

    private class QwenUsage
    {
        public int InputTokens { get; set; }
        public int OutputTokens { get; set; }
        public int TotalTokens { get; set; }
    }

    #endregion
}
