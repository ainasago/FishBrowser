# AI 提供商配置系统设计文档

## 1. 概述

### 目标
提供一个统一、易用的 AI 配置系统，支持国内外主流大模型，用户只需点击 2-3 次即可完成配置。

### 核心特性
- ✅ 支持多家 AI 提供商（OpenAI、Azure、Google、Anthropic、国内厂商等）
- ✅ 预设模型列表，下拉选择即可
- ✅ 多 API Key 配置与轮询
- ✅ 自动健康检查
- ✅ 统一调用接口
- ✅ 配置导入导出
- ✅ 加密存储敏感信息

## 2. 支持的 AI 提供商

### 国际厂商
| 提供商 | 支持模型 | 特点 |
|--------|---------|------|
| **OpenAI** | GPT-4, GPT-4-Turbo, GPT-3.5-Turbo | 最强大，价格较高 |
| **Azure OpenAI** | GPT-4, GPT-3.5 | 企业级，需要申请 |
| **Google Gemini** | Gemini Pro, Gemini Ultra | 免费额度大 |
| **Anthropic Claude** | Claude 3 Opus/Sonnet/Haiku | 长上下文，安全性高 |
| **Cohere** | Command, Command-Light | 企业级 NLP |
| **Mistral AI** | Mistral Large/Medium/Small | 欧洲开源 |

### 国内厂商
| 提供商 | 支持模型 | 特点 |
|--------|---------|------|
| **阿里云通义千问** | qwen-turbo, qwen-plus, qwen-max | 中文优秀 |
| **百度文心一言** | ERNIE-Bot 4.0/3.5 | 中文理解强 |
| **腾讯混元** | hunyuan-lite/standard/pro | 腾讯生态 |
| **智谱 AI** | GLM-4, GLM-3-Turbo | 清华背景 |
| **讯飞星火** | Spark 3.5/3.0 | 语音结合 |
| **月之暗面 Moonshot** | moonshot-v1-8k/32k/128k | 超长上下文 |
| **MiniMax** | abab5.5/6 | 多模态 |
| **零一万物** | Yi-Large/Medium | 开源友好 |

### 本地部署
| 提供商 | 支持模型 | 特点 |
|--------|---------|------|
| **Ollama** | Llama 3, Mistral, Qwen 等 | 本地免费 |
| **LM Studio** | 各种开源模型 | 图形化界面 |
| **vLLM** | 自定义模型 | 高性能推理 |
| **LocalAI** | OpenAI 兼容 | 本地替代 |

## 3. 数据模型

### AIProviderConfig（AI 提供商配置）
```csharp
public class AIProviderConfig
{
    public int Id { get; set; }
    public string Name { get; set; }                    // 配置名称（用户自定义）
    public AIProviderType ProviderType { get; set; }    // 提供商类型
    public string ModelId { get; set; }                 // 模型 ID
    public string BaseUrl { get; set; }                 // API 基础 URL
    public bool IsEnabled { get; set; }                 // 是否启用
    public int Priority { get; set; }                   // 优先级（用于轮询）
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // 导航属性
    public List<AIApiKey> ApiKeys { get; set; }
    public AIProviderSettings Settings { get; set; }
}

public enum AIProviderType
{
    OpenAI,
    AzureOpenAI,
    GoogleGemini,
    AnthropicClaude,
    Cohere,
    MistralAI,
    
    // 国内
    AlibabaQwen,
    BaiduErnie,
    TencentHunyuan,
    ZhipuGLM,
    XunfeiSpark,
    MoonshotAI,
    MiniMax,
    ZeroOneYi,
    
    // 本地
    Ollama,
    LMStudio,
    LocalAI,
    Custom
}
```

### AIApiKey（API 密钥）
```csharp
public class AIApiKey
{
    public int Id { get; set; }
    public int ProviderId { get; set; }
    public string KeyName { get; set; }                 // 密钥名称（如 "主密钥"）
    public string EncryptedKey { get; set; }            // 加密后的 API Key
    public int UsageCount { get; set; }                 // 使用次数
    public DateTime? LastUsedAt { get; set; }           // 最后使用时间
    public bool IsActive { get; set; }                  // 是否激活
    public int? DailyLimit { get; set; }                // 每日限额
    public int TodayUsage { get; set; }                 // 今日使用量
    public DateTime CreatedAt { get; set; }
}
```

### AIProviderSettings（提供商设置）
```csharp
public class AIProviderSettings
{
    public int Id { get; set; }
    public int ProviderId { get; set; }
    
    // 通用参数
    public double Temperature { get; set; } = 0.7;      // 温度（0-2）
    public int MaxTokens { get; set; } = 2000;          // 最大 token
    public double TopP { get; set; } = 1.0;             // Top-p 采样
    public double FrequencyPenalty { get; set; } = 0;   // 频率惩罚
    public double PresencePenalty { get; set; } = 0;    // 存在惩罚
    
    // 超时与重试
    public int TimeoutSeconds { get; set; } = 60;
    public int MaxRetries { get; set; } = 3;
    public int RetryDelayMs { get; set; } = 1000;
    
    // 速率限制
    public int? RpmLimit { get; set; }                  // 每分钟请求数
    public int? TpmLimit { get; set; }                  // 每分钟 token 数
    
    // 自定义参数（JSON）
    public string CustomParametersJson { get; set; }
}
```

### AIModelDefinition（模型定义）
```csharp
public class AIModelDefinition
{
    public string ModelId { get; set; }
    public string DisplayName { get; set; }
    public AIProviderType ProviderType { get; set; }
    public int ContextWindow { get; set; }              // 上下文窗口
    public decimal InputPricePer1K { get; set; }        // 输入价格/1K tokens
    public decimal OutputPricePer1K { get; set; }       // 输出价格/1K tokens
    public bool SupportsStreaming { get; set; }
    public bool SupportsFunctionCalling { get; set; }
    public bool SupportsVision { get; set; }
    public string[] Capabilities { get; set; }
}
```

### AIUsageLog（使用日志）
```csharp
public class AIUsageLog
{
    public int Id { get; set; }
    public int ProviderId { get; set; }
    public int ApiKeyId { get; set; }
    public string ModelId { get; set; }
    public DateTime Timestamp { get; set; }
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }
    public decimal Cost { get; set; }
    public int DurationMs { get; set; }
    public bool Success { get; set; }
    public string ErrorMessage { get; set; }
    public string RequestId { get; set; }
}
```

## 4. 服务层设计

### AIProviderService（核心服务）
```csharp
public interface IAIProviderService
{
    // 配置管理
    Task<List<AIProviderConfig>> GetAllProvidersAsync();
    Task<AIProviderConfig> GetProviderByIdAsync(int id);
    Task<AIProviderConfig> CreateProviderAsync(AIProviderConfig config);
    Task UpdateProviderAsync(AIProviderConfig config);
    Task DeleteProviderAsync(int id);
    Task<bool> TestConnectionAsync(int providerId);
    
    // API Key 管理
    Task<AIApiKey> AddApiKeyAsync(int providerId, string keyName, string apiKey);
    Task UpdateApiKeyAsync(AIApiKey apiKey);
    Task DeleteApiKeyAsync(int keyId);
    Task<string> GetNextApiKeyAsync(int providerId); // 轮询获取
    
    // 模型查询
    Task<List<AIModelDefinition>> GetAvailableModelsAsync(AIProviderType providerType);
    Task<AIModelDefinition> GetModelDefinitionAsync(string modelId);
    
    // 使用统计
    Task<AIUsageStats> GetUsageStatsAsync(int providerId, DateTime from, DateTime to);
}
```

### AIClientService（统一调用接口）
```csharp
public interface IAIClientService
{
    // 统一调用
    Task<AIResponse> GenerateAsync(AIRequest request);
    Task<IAsyncEnumerable<string>> StreamGenerateAsync(AIRequest request);
    
    // DSL 专用
    Task<string> GenerateDslFromPromptAsync(string prompt, int? providerId = null);
    Task<string> OptimizeDslAsync(string dsl, string feedback);
    Task<string> ExplainDslAsync(string dsl);
}

public class AIRequest
{
    public int? ProviderId { get; set; }        // null = 使用默认
    public string SystemPrompt { get; set; }
    public string UserPrompt { get; set; }
    public List<ChatMessage> History { get; set; }
    public double? Temperature { get; set; }
    public int? MaxTokens { get; set; }
    public bool Stream { get; set; }
}

public class AIResponse
{
    public string Content { get; set; }
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }
    public decimal Cost { get; set; }
    public string ModelUsed { get; set; }
    public int DurationMs { get; set; }
}
```

### AIProviderAdapter（适配器模式）
```csharp
public interface IAIProviderAdapter
{
    Task<AIResponse> CallAsync(AIProviderConfig config, AIRequest request);
    Task<bool> ValidateAsync(AIProviderConfig config);
}

// 每个提供商实现自己的适配器
public class OpenAIAdapter : IAIProviderAdapter { }
public class GeminiAdapter : IAIProviderAdapter { }
public class QwenAdapter : IAIProviderAdapter { }
// ...
```

### AIKeyRotationService（密钥轮询服务）
```csharp
public interface IAIKeyRotationService
{
    Task<string> GetNextKeyAsync(int providerId);
    Task RecordUsageAsync(int keyId, int tokens);
    Task MarkKeyFailedAsync(int keyId, string error);
    Task ResetDailyUsageAsync(); // 定时任务
}
```

### AIHealthCheckService（健康检查服务）
```csharp
public interface IAIHealthCheckService
{
    Task<HealthCheckResult> CheckProviderAsync(int providerId);
    Task<List<HealthCheckResult>> CheckAllProvidersAsync();
    Task SchedulePeriodicCheckAsync(); // 后台定时检查
}

public class HealthCheckResult
{
    public int ProviderId { get; set; }
    public bool IsHealthy { get; set; }
    public int ResponseTimeMs { get; set; }
    public string ErrorMessage { get; set; }
    public DateTime CheckedAt { get; set; }
}
```

## 5. UI 设计

### 主界面（AIProviderManagementView）
```
┌─────────────────────────────────────────────────────────┐
│  AI 提供商配置                                    [+新建] │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  ┌─────────────────────────────────────────────────┐   │
│  │ 已配置的提供商 (3)                               │   │
│  ├─────────────────────────────────────────────────┤   │
│  │ ✅ OpenAI GPT-4                    [编辑][测试]  │   │
│  │    • 2 个 API Key                               │   │
│  │    • 今日使用: 150 次                           │   │
│  │    • 健康状态: 正常 (120ms)                     │   │
│  ├─────────────────────────────────────────────────┤   │
│  │ ✅ Google Gemini Pro               [编辑][测试]  │   │
│  │    • 1 个 API Key                               │   │
│  │    • 今日使用: 50 次                            │   │
│  │    • 健康状态: 正常 (200ms)                     │   │
│  ├─────────────────────────────────────────────────┤   │
│  │ ⚠️ 阿里云通义千问                  [编辑][测试]  │   │
│  │    • 1 个 API Key                               │   │
│  │    • 今日使用: 0 次                             │   │
│  │    • 健康状态: 未测试                           │   │
│  └─────────────────────────────────────────────────┘   │
│                                                         │
│  [全部测试] [导入配置] [导出配置] [使用统计]             │
└─────────────────────────────────────────────────────────┘
```

### 新建/编辑对话框（AIProviderEditDialog）
```
┌─────────────────────────────────────────────────────────┐
│  配置 AI 提供商                              [保存][取消] │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  基本信息                                               │
│  ┌─────────────────────────────────────────────────┐   │
│  │ 配置名称: [我的 OpenAI 配置____________]         │   │
│  │                                                 │   │
│  │ 提供商:   [OpenAI ▼]  ← 下拉选择                │   │
│  │           • OpenAI                              │   │
│  │           • Azure OpenAI                        │   │
│  │           • Google Gemini                       │   │
│  │           • Anthropic Claude                    │   │
│  │           • 阿里云通义千问                       │   │
│  │           • 百度文心一言                         │   │
│  │           • ...                                 │   │
│  │                                                 │   │
│  │ 模型:     [GPT-4 Turbo ▼]  ← 自动加载模型列表    │   │
│  │           • GPT-4 Turbo (128K)                  │   │
│  │           • GPT-4 (8K)                          │   │
│  │           • GPT-3.5 Turbo (16K)                 │   │
│  │                                                 │   │
│  │ API 地址: [https://api.openai.com/v1_______]    │   │
│  │           (自动填充，可修改)                     │   │
│  └─────────────────────────────────────────────────┘   │
│                                                         │
│  API 密钥 (支持多个，自动轮询)                           │
│  ┌─────────────────────────────────────────────────┐   │
│  │ [+添加密钥]                                      │   │
│  │                                                 │   │
│  │ 密钥 1: 主密钥                          [删除]   │   │
│  │ sk-proj-***************************             │   │
│  │ 每日限额: [1000] 次  今日已用: 150 次           │   │
│  │ ☑️ 启用                                         │   │
│  │                                                 │   │
│  │ 密钥 2: 备用密钥                        [删除]   │   │
│  │ sk-proj-***************************             │   │
│  │ 每日限额: [500] 次   今日已用: 0 次             │   │
│  │ ☑️ 启用                                         │   │
│  └─────────────────────────────────────────────────┘   │
│                                                         │
│  高级设置 (可选)                          [展开/收起 ▼] │
│  ┌─────────────────────────────────────────────────┐   │
│  │ Temperature:  [0.7____] (0-2, 越高越随机)       │   │
│  │ Max Tokens:   [2000___] (最大生成长度)          │   │
│  │ Top P:        [1.0____] (0-1, 核采样)           │   │
│  │ 超时时间:     [60_____] 秒                      │   │
│  │ 重试次数:     [3______] 次                      │   │
│  │ RPM 限制:     [60_____] 次/分钟                 │   │
│  └─────────────────────────────────────────────────┘   │
│                                                         │
│  [测试连接] [保存] [取消]                               │
└─────────────────────────────────────────────────────────┘
```

### 快速配置向导（AIQuickSetupWizard）
```
┌─────────────────────────────────────────────────────────┐
│  快速配置 AI 提供商 (步骤 1/3)                           │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  选择你想使用的 AI 提供商:                               │
│                                                         │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐               │
│  │ OpenAI   │ │ Gemini   │ │ Claude   │               │
│  │   🤖     │ │   🌟     │ │   🧠     │               │
│  │ 最强大   │ │ 免费额度 │ │ 长上下文 │               │
│  └──────────┘ └──────────┘ └──────────┘               │
│                                                         │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐               │
│  │ 通义千问 │ │ 文心一言 │ │ 本地模型 │               │
│  │   🇨🇳     │ │   🇨🇳     │ │   💻     │               │
│  │ 中文优秀 │ │ 百度出品 │ │ 完全免费 │               │
│  └──────────┘ └──────────┘ └──────────┘               │
│                                                         │
│                                    [下一步 →]           │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│  快速配置 AI 提供商 (步骤 2/3)                           │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  已选择: OpenAI                                         │
│                                                         │
│  选择模型:                                              │
│  ┌─────────────────────────────────────────────────┐   │
│  │ ⚪ GPT-4 Turbo                                   │   │
│  │    • 最强大，适合复杂任务                        │   │
│  │    • 价格: $0.01/1K tokens                      │   │
│  │                                                 │   │
│  │ 🔘 GPT-3.5 Turbo (推荐)                         │   │
│  │    • 性价比高，适合日常使用                      │   │
│  │    • 价格: $0.0005/1K tokens                    │   │
│  └─────────────────────────────────────────────────┘   │
│                                                         │
│  [← 上一步]                           [下一步 →]        │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│  快速配置 AI 提供商 (步骤 3/3)                           │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  输入 API 密钥:                                         │
│  ┌─────────────────────────────────────────────────┐   │
│  │ API Key: [sk-proj-___________________]  [粘贴]  │   │
│  │                                                 │   │
│  │ 💡 在哪里获取 API Key?                           │   │
│  │    访问 https://platform.openai.com/api-keys    │   │
│  │    点击 "Create new secret key"                 │   │
│  └─────────────────────────────────────────────────┘   │
│                                                         │
│  可选: 添加更多密钥用于轮询                              │
│  [+ 添加备用密钥]                                       │
│                                                         │
│  [← 上一步]  [测试并保存]                               │
└─────────────────────────────────────────────────────────┘
```

## 6. 预设模型数据

### OpenAI 模型
```json
{
  "models": [
    {
      "modelId": "gpt-4-turbo",
      "displayName": "GPT-4 Turbo",
      "contextWindow": 128000,
      "inputPrice": 0.01,
      "outputPrice": 0.03,
      "capabilities": ["chat", "function-calling", "vision"]
    },
    {
      "modelId": "gpt-4",
      "displayName": "GPT-4",
      "contextWindow": 8192,
      "inputPrice": 0.03,
      "outputPrice": 0.06
    },
    {
      "modelId": "gpt-3.5-turbo",
      "displayName": "GPT-3.5 Turbo",
      "contextWindow": 16385,
      "inputPrice": 0.0005,
      "outputPrice": 0.0015
    }
  ]
}
```

### 国内模型（示例）
```json
{
  "qwen": [
    {"modelId": "qwen-max", "displayName": "通义千问-Max"},
    {"modelId": "qwen-plus", "displayName": "通义千问-Plus"},
    {"modelId": "qwen-turbo", "displayName": "通义千问-Turbo"}
  ],
  "ernie": [
    {"modelId": "ernie-4.0", "displayName": "文心一言 4.0"},
    {"modelId": "ernie-3.5", "displayName": "文心一言 3.5"}
  ]
}
```

## 7. 实现优先级

### Phase 1: 核心功能 (1-2 天)
- ✅ 数据模型与数据库迁移
- ✅ AIProviderService 基础 CRUD
- ✅ API Key 加密存储
- ✅ 预设模型数据加载
- ✅ 配置管理 UI

### Phase 2: 适配器实现 (2-3 天)
- ✅ OpenAI 适配器
- ✅ Gemini 适配器
- ✅ 通义千问适配器
- ✅ 统一调用接口
- ✅ 健康检查

### Phase 3: 高级功能 (1-2 天)
- ✅ 密钥轮询
- ✅ 使用统计
- ✅ 快速配置向导
- ✅ 导入导出

### Phase 4: 集成与优化 (1 天)
- ✅ 集成到 AI 任务界面
- ✅ 错误处理与重试
- ✅ 性能优化

## 8. 安全考虑

### API Key 加密
- 使用 Windows DPAPI 加密存储
- 内存中明文使用后立即清除
- 日志中脱敏显示（sk-***）

### 权限控制
- 配置修改需要确认
- 敏感操作记录审计日志
- 导出配置时可选是否包含密钥

## 9. 用户体验优化

### 智能提示
- 选择提供商后自动加载模型列表
- 自动填充默认 API 地址
- 实时显示价格估算
- 健康状态实时更新

### 错误处理
- 友好的错误提示
- 自动重试机制
- 降级策略（主 Key 失败自动切换备用）

### 性能优化
- 配置缓存
- 异步加载
- 批量健康检查

## 10. 测试计划

### 单元测试
- 各适配器的调用测试
- 密钥轮询逻辑测试
- 加密解密测试

### 集成测试
- 端到端 AI 调用测试
- 多提供商切换测试
- 故障转移测试

### 用户测试
- 配置流程易用性测试
- 错误恢复测试
- 性能压力测试
