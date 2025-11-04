using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FishBrowser.WPF.Data;
using FishBrowser.WPF.Models;
using FishBrowser.WPF.Services.AIProviderAdapters;

namespace FishBrowser.WPF.Services;

/// <summary>
/// 页面上下文信息
/// </summary>
public class PageContext
{
    public string? Url { get; set; }
    public string? Title { get; set; }
    public string? VisibleText { get; set; }
    public string? SelectionText { get; set; }
}

/// <summary>
/// AI 客户端服务接口
/// </summary>
public interface IAIClientService
{
    Task<AIResponse> GenerateAsync(AIRequest request);
    Task<string> GenerateDslFromPromptAsync(string prompt, int? providerId = null);
    Task<string> OptimizeDslAsync(string dsl, string feedback);
    Task<string> ExplainDslAsync(string dsl);
    Task<string> ChatAsync(string userMessage, PageContext? pageContext = null, int? providerId = null);
}

/// <summary>
/// AI 客户端服务实现
/// </summary>
public class AIClientService : IAIClientService
{
    private readonly IAIProviderService _providerService;
    private readonly WebScraperDbContext _db;
    private readonly ILogService _logger;
    private readonly Dictionary<AIProviderType, IAIProviderAdapter> _adapters;

    public AIClientService(
        IAIProviderService providerService,
        WebScraperDbContext db,
        ILogService logger)
    {
        _providerService = providerService;
        _db = db;
        _logger = logger;

        // 初始化适配器
        _adapters = new Dictionary<AIProviderType, IAIProviderAdapter>
        {
            { AIProviderType.OpenAI, new OpenAIAdapter(logger) },
            { AIProviderType.AzureOpenAI, new OpenAIAdapter(logger) }, // Azure 使用相同格式
            { AIProviderType.GoogleGemini, new GeminiAdapter(logger) },
            { AIProviderType.AlibabaQwen, new QwenAdapter(logger) },
            { AIProviderType.ModelScope, new ModelScopeAdapter(logger) },
            { AIProviderType.SiliconFlow, new OpenAIAdapter(logger) }, // 硅基流动兼容 OpenAI
            { AIProviderType.Ollama, new OllamaAdapter(logger) },
            { AIProviderType.LMStudio, new OpenAIAdapter(logger) }, // LM Studio 兼容 OpenAI
            { AIProviderType.LocalAI, new OpenAIAdapter(logger) } // LocalAI 兼容 OpenAI
        };
    }

    public async Task<AIResponse> GenerateAsync(AIRequest request)
    {
        var startTime = DateTime.Now;

        try
        {
            // 1. 获取提供商配置
            AIProviderConfig? provider;
            if (request.ProviderId.HasValue)
            {
                provider = await _providerService.GetProviderByIdAsync(request.ProviderId.Value);
            }
            else
            {
                // 使用默认提供商（优先级最高且启用的）
                var providers = await _providerService.GetAllProvidersAsync();
                provider = providers.Find(p => p.IsEnabled);
            }

            if (provider == null)
            {
                _logger.LogError("AIClient", "No available AI provider configured");
                return new AIResponse
                {
                    Success = false,
                    ErrorMessage = "No available AI provider configured"
                };
            }

            // 2. 获取 API Key
            var apiKey = await _providerService.GetNextApiKeyAsync(provider.Id);
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogError("AIClient", $"No available API key for provider: {provider.Name}");
                return new AIResponse
                {
                    Success = false,
                    ErrorMessage = "No available API key"
                };
            }

            // 3. 获取适配器
            if (!_adapters.TryGetValue(provider.ProviderType, out var adapter))
            {
                _logger.LogError("AIClient", $"No adapter found for provider type: {provider.ProviderType}");
                return new AIResponse
                {
                    Success = false,
                    ErrorMessage = $"Unsupported provider type: {provider.ProviderType}"
                };
            }

            // 4. 调用 AI API
            _logger.LogInfo("AIClient", $"Calling AI provider: {provider.Name} ({provider.ModelId})");
            var response = await adapter.CallAsync(provider, request, apiKey);

            // 5. 计算成本
            if (response.Success)
            {
                var model = await _providerService.GetModelDefinitionAsync(provider.ModelId);
                response.Cost = CalculateCost(response.PromptTokens, response.CompletionTokens, model);
            }

            // 6. 记录使用日志
            var apiKeyEntity = await _db.AIApiKeys.FirstOrDefaultAsync(k => k.ProviderId == provider.Id && k.IsActive);
            if (apiKeyEntity != null)
            {
                await _providerService.RecordUsageAsync(new AIUsageLog
                {
                    ProviderId = provider.Id,
                    ApiKeyId = apiKeyEntity.Id,
                    ModelId = provider.ModelId,
                    Timestamp = startTime,
                    PromptTokens = response.PromptTokens,
                    CompletionTokens = response.CompletionTokens,
                    TotalTokens = response.TotalTokens,
                    Cost = response.Cost,
                    DurationMs = response.DurationMs,
                    Success = response.Success,
                    ErrorMessage = response.ErrorMessage
                });
            }

            _logger.LogInfo("AIClient", $"AI response received: {response.TotalTokens} tokens, {response.DurationMs}ms");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError("AIClient", $"Generate failed: {ex.Message}", ex.StackTrace);
            return new AIResponse
            {
                Success = false,
                ErrorMessage = ex.Message,
                DurationMs = (int)(DateTime.Now - startTime).TotalMilliseconds
            };
        }
    }

    public async Task<string> GenerateDslFromPromptAsync(string prompt, int? providerId = null)
    {
        try
        {
            var systemPrompt = await LoadDslSystemPromptAsync();

            var request = new AIRequest
            {
                ProviderId = providerId,
                SystemPrompt = systemPrompt,
                UserPrompt = prompt,
                Temperature = 0.7,
                MaxTokens = 3000
            };

            var response = await GenerateAsync(request);

            if (!response.Success)
            {
                _logger.LogError("AIClient", $"DSL generation failed: {response.ErrorMessage}");
                return $"# 生成失败\n# 错误: {response.ErrorMessage}";
            }

            return response.Content;
        }
        catch (Exception ex)
        {
            _logger.LogError("AIClient", $"DSL generation failed: {ex.Message}", ex.StackTrace);
            return $"# 生成失败\n# 错误: {ex.Message}";
        }
    }

    public async Task<string> OptimizeDslAsync(string dsl, string feedback)
    {
        try
        {
            var systemPrompt = await LoadDslSystemPromptAsync();
            var userPrompt = $"请根据以下反馈优化这个 DSL 脚本：\n\n反馈：{feedback}\n\n当前脚本：\n```yaml\n{dsl}\n```\n\n请输出优化后的完整脚本。";

            var request = new AIRequest
            {
                SystemPrompt = systemPrompt,
                UserPrompt = userPrompt,
                Temperature = 0.5,
                MaxTokens = 3000
            };

            var response = await GenerateAsync(request);
            return response.Success ? response.Content : dsl;
        }
        catch (Exception ex)
        {
            _logger.LogError("AIClient", $"DSL optimization failed: {ex.Message}", ex.StackTrace);
            return dsl;
        }
    }

    public async Task<string> ExplainDslAsync(string dsl)
    {
        try
        {
            var systemPrompt = "你是一个网页自动化专家，擅长解释 Task Flow DSL 脚本。";
            var userPrompt = $"请用简洁的中文解释这个 DSL 脚本的功能：\n\n```yaml\n{dsl}\n```";

            var request = new AIRequest
            {
                SystemPrompt = systemPrompt,
                UserPrompt = userPrompt,
                Temperature = 0.3,
                MaxTokens = 1000
            };

            var response = await GenerateAsync(request);
            return response.Success ? response.Content : "解释生成失败";
        }
        catch (Exception ex)
        {
            _logger.LogError("AIClient", $"DSL explanation failed: {ex.Message}", ex.StackTrace);
            return "解释生成失败";
        }
    }

    public async Task<string> ChatAsync(string userMessage, PageContext? pageContext = null, int? providerId = null)
    {
        try
        {
            // 构建系统提示
            var systemPrompt = @"你是一个网页自动化调试助手，帮助用户：
1. 分析和诊断 DSL 脚本执行错误
2. 优化选择器和脚本逻辑
3. 根据页面内容生成或修改 DSL 脚本
4. 解答网页自动化相关问题

回答要求：
- 简洁明了，直接给出解决方案
- 如果涉及代码，使用 YAML 格式
- 如果需要更多信息，明确告知用户";

            // 构建用户提示（包含页面上下文）
            var userPrompt = userMessage;
            if (pageContext != null)
            {
                var contextParts = new List<string>();
                
                if (!string.IsNullOrEmpty(pageContext.Url))
                    contextParts.Add($"**当前页面**: {pageContext.Url}");
                
                if (!string.IsNullOrEmpty(pageContext.Title))
                    contextParts.Add($"**页面标题**: {pageContext.Title}");
                
                if (!string.IsNullOrEmpty(pageContext.SelectionText))
                    contextParts.Add($"**选中元素文本**: {pageContext.SelectionText}");
                
                if (!string.IsNullOrEmpty(pageContext.VisibleText))
                {
                    var text = pageContext.VisibleText.Length > 2000 
                        ? pageContext.VisibleText.Substring(0, 2000) + "..." 
                        : pageContext.VisibleText;
                    contextParts.Add($"**页面可见文本**:\n{text}");
                }

                if (contextParts.Count > 0)
                {
                    userPrompt = $@"【页面上下文】
{string.Join("\n", contextParts)}

【用户问题】
{userMessage}";
                }
            }

            var request = new AIRequest
            {
                ProviderId = providerId,
                SystemPrompt = systemPrompt,
                UserPrompt = userPrompt,
                Temperature = 0.7,
                MaxTokens = 2000
            };

            // 记录发送给 AI 的完整 prompt
            _logger.LogInfo("AIClient", "=== AI Request ===");
            _logger.LogInfo("AIClient", $"System Prompt Length: {systemPrompt?.Length ?? 0} chars");
            _logger.LogInfo("AIClient", $"User Prompt Length: {userPrompt?.Length ?? 0} chars");
            _logger.LogInfo("AIClient", $"--- System Prompt ---");
            _logger.LogInfo("AIClient", systemPrompt ?? "(empty)");
            _logger.LogInfo("AIClient", $"--- User Prompt ---");
            _logger.LogInfo("AIClient", userPrompt ?? "(empty)");
            _logger.LogInfo("AIClient", "==================");

            var response = await GenerateAsync(request);

            if (!response.Success)
            {
                _logger.LogError("AIClient", $"Chat failed: {response.ErrorMessage}");
                return $"抱歉，AI 服务暂时不可用：{response.ErrorMessage}";
            }

            return response.Content;
        }
        catch (Exception ex)
        {
            _logger.LogError("AIClient", $"Chat failed: {ex.Message}", ex.StackTrace);
            return $"抱歉，处理失败：{ex.Message}";
        }
    }

    private async Task<string> LoadDslSystemPromptAsync()
    {
        try
        {
            var specPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "docs", "task-flow-dsl-spec.md");
            if (File.Exists(specPath))
            {
                var spec = await File.ReadAllTextAsync(specPath);
                return $@"你是一个网页自动化任务专家。用户会描述任务需求，你需要生成符合 Task Flow DSL v1.0 规范的 YAML 脚本。

规范文档：
{spec}

要求：
1. 只输出 YAML 代码，不要解释
2. 确保语法正确，缩进使用 2 个空格
3. 选择器优先使用 CSS，必要时使用 XPath
4. 添加适当的等待和错误处理
5. 变量命名清晰，使用小驼峰
6. 添加注释说明关键步骤";
            }
            else
            {
                return @"你是一个网页自动化任务专家。用户会描述任务需求，你需要生成符合 Task Flow DSL v1.0 规范的 YAML 脚本。

规范要点：
- 必须包含 dslVersion、id、name、steps
- 选择器格式：{ type: css|xpath|text|role, value: string }
- 常用步骤：open、click、type、fill、waitFor、extract、if、for
- 变量引用：{{ vars.xxx }} 或 {{ data.xxx }}

只输出 YAML 代码，不要解释。";
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarn("AIClient", $"Failed to load DSL spec: {ex.Message}");
            return "你是一个网页自动化专家，请根据用户需求生成 Task Flow DSL YAML 脚本。";
        }
    }

    private decimal CalculateCost(int promptTokens, int completionTokens, AIModelDefinition? model)
    {
        if (model == null) return 0;

        var inputCost = (promptTokens / 1000m) * model.InputPricePer1K;
        var outputCost = (completionTokens / 1000m) * model.OutputPricePer1K;

        return inputCost + outputCost;
    }
}
