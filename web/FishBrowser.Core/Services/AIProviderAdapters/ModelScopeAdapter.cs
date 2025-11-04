using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using FishBrowser.WPF.Models;

namespace FishBrowser.WPF.Services.AIProviderAdapters;

/// <summary>
/// ModelScope 魔塔社区适配器
/// API 文档: https://www.modelscope.cn/docs/model-service/API-Inference/intro
/// API 端点: https://api-inference.modelscope.cn/v1/models/{model_id}/inference
/// 模型名格式: Qwen/Qwen2.5-Coder-32B-Instruct
/// </summary>
public class ModelScopeAdapter : BaseAdapter
{
    public ModelScopeAdapter(ILogService logger) : base(logger)
    {
    }

    public override async Task<AIResponse> CallAsync(AIProviderConfig config, AIRequest request, string apiKey)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            // ModelScope API 格式: /v1/models/{model_id}/inference
            var url = $"{config.BaseUrl.TrimEnd('/')}/models/{config.ModelId}/inference";

            var inputs = new List<object>();

            if (!string.IsNullOrEmpty(request.SystemPrompt))
            {
                inputs.Add(new { role = "system", content = request.SystemPrompt });
            }

            if (request.History != null && request.History.Any())
            {
                foreach (var msg in request.History)
                {
                    inputs.Add(new { role = msg.Role, content = msg.Content });
                }
            }

            inputs.Add(new { role = "user", content = request.UserPrompt });

            var payload = new
            {
                inputs = inputs,
                parameters = new
                {
                    temperature = request.Temperature ?? config.Settings?.Temperature ?? 0.7,
                    max_new_tokens = request.MaxTokens ?? config.Settings?.MaxTokens ?? 2000,
                    top_p = config.Settings?.TopP ?? 1.0
                }
            };

            var response = await PostJsonAsync<ModelScopeResponse>(url, payload, req =>
            {
                req.Headers.Add("Authorization", $"Bearer {apiKey}");
            });

            sw.Stop();

            if (response?.GeneratedText == null)
            {
                return new AIResponse
                {
                    Success = false,
                    ErrorMessage = response?.Error ?? "No response from ModelScope",
                    DurationMs = (int)sw.ElapsedMilliseconds
                };
            }

            return new AIResponse
            {
                Content = response.GeneratedText,
                PromptTokens = 0, // ModelScope API 可能不返回 token 统计
                CompletionTokens = 0,
                TotalTokens = 0,
                Cost = 0,
                ModelUsed = config.ModelId,
                DurationMs = (int)sw.ElapsedMilliseconds,
                Success = true
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError("ModelScopeAdapter", $"API call failed: {ex.Message}", ex.StackTrace);

            return new AIResponse
            {
                Success = false,
                ErrorMessage = ex.Message,
                DurationMs = (int)sw.ElapsedMilliseconds
            };
        }
    }

    #region Response Models

    private class ModelScopeResponse
    {
        public string? GeneratedText { get; set; }
        public string? Error { get; set; }
        public string? RequestId { get; set; }
    }

    #endregion
}
