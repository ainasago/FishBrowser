using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using FishBrowser.WPF.Models;

namespace FishBrowser.WPF.Services.AIProviderAdapters;

/// <summary>
/// Ollama 本地模型适配器
/// </summary>
public class OllamaAdapter : BaseAdapter
{
    public OllamaAdapter(ILogService logger) : base(logger)
    {
    }

    public override async Task<AIResponse> CallAsync(AIProviderConfig config, AIRequest request, string apiKey)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            var url = $"{config.BaseUrl.TrimEnd('/')}/api/chat";

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
                stream = false,
                options = new
                {
                    temperature = request.Temperature ?? config.Settings?.Temperature ?? 0.7,
                    num_predict = request.MaxTokens ?? config.Settings?.MaxTokens ?? 2000
                }
            };

            var response = await PostJsonAsync<OllamaResponse>(url, payload);

            sw.Stop();

            if (response?.Message?.Content == null)
            {
                return new AIResponse
                {
                    Success = false,
                    ErrorMessage = "No response from Ollama",
                    DurationMs = (int)sw.ElapsedMilliseconds
                };
            }

            // Ollama 返回的 token 统计
            var promptTokens = response.PromptEvalCount ?? 0;
            var completionTokens = response.EvalCount ?? 0;

            return new AIResponse
            {
                Content = response.Message.Content,
                PromptTokens = promptTokens,
                CompletionTokens = completionTokens,
                TotalTokens = promptTokens + completionTokens,
                Cost = 0, // 本地模型免费
                ModelUsed = response.Model ?? config.ModelId,
                DurationMs = (int)sw.ElapsedMilliseconds,
                Success = true
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError("OllamaAdapter", $"API call failed: {ex.Message}", ex.StackTrace);

            return new AIResponse
            {
                Success = false,
                ErrorMessage = ex.Message,
                DurationMs = (int)sw.ElapsedMilliseconds
            };
        }
    }

    #region Response Models

    private class OllamaResponse
    {
        public string? Model { get; set; }
        public string? CreatedAt { get; set; }
        public OllamaMessage? Message { get; set; }
        public bool Done { get; set; }
        public int? TotalDuration { get; set; }
        public int? LoadDuration { get; set; }
        public int? PromptEvalCount { get; set; }
        public int? PromptEvalDuration { get; set; }
        public int? EvalCount { get; set; }
        public int? EvalDuration { get; set; }
    }

    private class OllamaMessage
    {
        public string? Role { get; set; }
        public string? Content { get; set; }
    }

    #endregion
}
