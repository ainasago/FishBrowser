using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FishBrowser.WPF.Models;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace FishBrowser.WPF.Services.AIProviderAdapters;

/// <summary>
/// AI 提供商适配器接口
/// </summary>
public interface IAIProviderAdapter
{
    Task<AIResponse> CallAsync(AIProviderConfig config, AIRequest request, string apiKey);
    Task<bool> ValidateAsync(AIProviderConfig config, string apiKey);
}

/// <summary>
/// AI 提供商适配器基类
/// </summary>
public abstract class BaseAdapter : IAIProviderAdapter
{
    protected readonly HttpClient _httpClient;
    protected readonly ILogService _logger;

    protected BaseAdapter(ILogService logger)
    {
        _logger = logger;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(60)
        };
    }

    public abstract Task<AIResponse> CallAsync(AIProviderConfig config, AIRequest request, string apiKey);

    public virtual async Task<bool> ValidateAsync(AIProviderConfig config, string apiKey)
    {
        try
        {
            var testRequest = new AIRequest
            {
                SystemPrompt = "You are a helpful assistant.",
                UserPrompt = "Hello",
                MaxTokens = 10
            };

            var response = await CallAsync(config, testRequest, apiKey);
            return response.Success;
        }
        catch
        {
            return false;
        }
    }

    protected async Task<T?> PostJsonAsync<T>(string url, object payload, Action<HttpRequestMessage>? configureRequest = null)
    {
        try
        {
            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            configureRequest?.Invoke(request);

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("AIAdapter", $"API call failed: {response.StatusCode} - {responseContent}");
                return default;
            }

            return JsonSerializer.Deserialize<T>(responseContent, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("AIAdapter", $"HTTP request failed: {ex.Message}", ex.StackTrace);
            throw;
        }
    }

    protected decimal CalculateCost(int promptTokens, int completionTokens, AIModelDefinition? model)
    {
        if (model == null) return 0;

        var inputCost = (promptTokens / 1000m) * model.InputPricePer1K;
        var outputCost = (completionTokens / 1000m) * model.OutputPricePer1K;

        return inputCost + outputCost;
    }
}
