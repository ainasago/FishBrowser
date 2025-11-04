# AI 提供商 API 参考文档

## 1. 国际提供商 API

### OpenAI
- **官网**: https://platform.openai.com
- **文档**: https://platform.openai.com/docs/api-reference
- **Base URL**: `https://api.openai.com/v1`
- **认证**: `Authorization: Bearer {API_KEY}`
- **端点**: `/chat/completions`

**请求示例**:
```json
{
  "model": "gpt-4-turbo",
  "messages": [
    {"role": "system", "content": "You are a helpful assistant."},
    {"role": "user", "content": "Hello!"}
  ],
  "temperature": 0.7,
  "max_tokens": 2000
}
```

### Azure OpenAI
- **Base URL**: `https://{resource-name}.openai.azure.com/openai/deployments/{deployment-id}`
- **认证**: `api-key: {API_KEY}`
- **端点**: `/chat/completions?api-version=2024-02-15-preview`

### Google Gemini
- **官网**: https://ai.google.dev
- **Base URL**: `https://generativelanguage.googleapis.com/v1beta`
- **认证**: `?key={API_KEY}`
- **端点**: `/models/gemini-pro:generateContent`

**请求示例**:
```json
{
  "contents": [{
    "parts": [{"text": "Hello!"}]
  }],
  "generationConfig": {
    "temperature": 0.7,
    "maxOutputTokens": 2000
  }
}
```

### Anthropic Claude
- **官网**: https://www.anthropic.com
- **Base URL**: `https://api.anthropic.com/v1`
- **认证**: `x-api-key: {API_KEY}`, `anthropic-version: 2023-06-01`
- **端点**: `/messages`

**请求示例**:
```json
{
  "model": "claude-3-opus-20240229",
  "max_tokens": 2000,
  "messages": [
    {"role": "user", "content": "Hello!"}
  ]
}
```

## 2. 国内提供商 API

### 阿里云通义千问
- **官网**: https://dashscope.aliyun.com
- **Base URL**: `https://dashscope.aliyuncs.com/api/v1`
- **认证**: `Authorization: Bearer {API_KEY}`
- **端点**: `/services/aigc/text-generation/generation`

**请求示例**:
```json
{
  "model": "qwen-max",
  "input": {
    "messages": [
      {"role": "user", "content": "你好"}
    ]
  },
  "parameters": {
    "temperature": 0.7,
    "max_tokens": 2000
  }
}
```

### 百度文心一言
- **官网**: https://cloud.baidu.com/product/wenxinworkshop
- **Base URL**: `https://aip.baidubce.com/rpc/2.0/ai_custom/v1/wenxinworkshop/chat`
- **认证**: 先获取 access_token，然后 `?access_token={TOKEN}`
- **端点**: `/completions_pro` (ERNIE-Bot 4.0)

**获取 Token**:
```
POST https://aip.baidubce.com/oauth/2.0/token
?grant_type=client_credentials
&client_id={API_KEY}
&client_secret={SECRET_KEY}
```

**请求示例**:
```json
{
  "messages": [
    {"role": "user", "content": "你好"}
  ],
  "temperature": 0.7,
  "max_output_tokens": 2000
}
```

### 腾讯混元
- **官网**: https://cloud.tencent.com/product/hunyuan
- **Base URL**: `https://hunyuan.tencentcloudapi.com`
- **认证**: 腾讯云签名认证（较复杂）
- **端点**: `/` (使用 Action 参数)

### 智谱 AI (GLM)
- **官网**: https://open.bigmodel.cn
- **Base URL**: `https://open.bigmodel.cn/api/paas/v4`
- **认证**: `Authorization: Bearer {API_KEY}`
- **端点**: `/chat/completions`

**请求示例**:
```json
{
  "model": "glm-4",
  "messages": [
    {"role": "user", "content": "你好"}
  ],
  "temperature": 0.7,
  "max_tokens": 2000
}
```

### 讯飞星火
- **官网**: https://xinghuo.xfyun.cn
- **Base URL**: `wss://spark-api.xf-yun.com/v3.5/chat` (WebSocket)
- **认证**: HMAC-SHA256 签名

### Moonshot AI
- **官网**: https://platform.moonshot.cn
- **Base URL**: `https://api.moonshot.cn/v1`
- **认证**: `Authorization: Bearer {API_KEY}`
- **端点**: `/chat/completions`

**请求示例**:
```json
{
  "model": "moonshot-v1-8k",
  "messages": [
    {"role": "user", "content": "你好"}
  ],
  "temperature": 0.7,
  "max_tokens": 2000
}
```

### MiniMax
- **官网**: https://api.minimax.chat
- **Base URL**: `https://api.minimax.chat/v1`
- **认证**: `Authorization: Bearer {API_KEY}`
- **端点**: `/text/chatcompletion_v2`

### 零一万物 (Yi)
- **官网**: https://platform.lingyiwanwu.com
- **Base URL**: `https://api.lingyiwanwu.com/v1`
- **认证**: `Authorization: Bearer {API_KEY}`
- **端点**: `/chat/completions`

## 3. 本地部署 API

### Ollama
- **官网**: https://ollama.ai
- **Base URL**: `http://localhost:11434`
- **端点**: `/api/generate` 或 `/api/chat`

**请求示例**:
```json
{
  "model": "llama3",
  "messages": [
    {"role": "user", "content": "Hello!"}
  ],
  "stream": false
}
```

### LM Studio
- **Base URL**: `http://localhost:1234/v1`
- **端点**: `/chat/completions` (兼容 OpenAI)

### LocalAI
- **Base URL**: `http://localhost:8080/v1`
- **端点**: `/chat/completions` (兼容 OpenAI)

## 4. 统一响应格式

### 标准响应
```json
{
  "id": "chatcmpl-xxx",
  "object": "chat.completion",
  "created": 1234567890,
  "model": "gpt-4-turbo",
  "choices": [
    {
      "index": 0,
      "message": {
        "role": "assistant",
        "content": "Hello! How can I help you?"
      },
      "finish_reason": "stop"
    }
  ],
  "usage": {
    "prompt_tokens": 10,
    "completion_tokens": 20,
    "total_tokens": 30
  }
}
```

### 流式响应
```
data: {"choices":[{"delta":{"content":"Hello"}}]}
data: {"choices":[{"delta":{"content":"!"}}]}
data: [DONE]
```

## 5. 错误码参考

### OpenAI 错误码
- `401`: 无效的 API Key
- `429`: 速率限制
- `500`: 服务器错误
- `503`: 服务过载

### 通用错误处理
```json
{
  "error": {
    "message": "Invalid API key",
    "type": "invalid_request_error",
    "code": "invalid_api_key"
  }
}
```

## 6. 价格参考 (USD/1M tokens)

### OpenAI
| 模型 | 输入 | 输出 |
|------|------|------|
| GPT-4 Turbo | $10 | $30 |
| GPT-4 | $30 | $60 |
| GPT-3.5 Turbo | $0.50 | $1.50 |

### Google Gemini
| 模型 | 输入 | 输出 |
|------|------|------|
| Gemini Pro | $0.50 | $1.50 |
| Gemini Ultra | $10 | $30 |

### Anthropic Claude
| 模型 | 输入 | 输出 |
|------|------|------|
| Claude 3 Opus | $15 | $75 |
| Claude 3 Sonnet | $3 | $15 |
| Claude 3 Haiku | $0.25 | $1.25 |

### 国内模型（人民币/1M tokens）
| 提供商 | 模型 | 输入 | 输出 |
|--------|------|------|------|
| 阿里云 | qwen-max | ¥40 | ¥120 |
| 百度 | ERNIE-4.0 | ¥30 | ¥90 |
| 智谱 | GLM-4 | ¥50 | ¥50 |
| Moonshot | moonshot-v1-8k | ¥12 | ¥12 |

## 7. 速率限制

### OpenAI
- GPT-4: 10,000 TPM (tokens per minute)
- GPT-3.5: 90,000 TPM
- RPM: 3,500 requests per minute

### Gemini
- Free tier: 60 requests per minute
- Paid tier: 1,000 requests per minute

### 国内厂商
- 通义千问: 100 QPS
- 文心一言: 300 QPS
- GLM-4: 100 QPS

## 8. 上下文窗口

| 模型 | 上下文长度 |
|------|-----------|
| GPT-4 Turbo | 128K |
| GPT-4 | 8K |
| GPT-3.5 Turbo | 16K |
| Gemini Pro | 32K |
| Claude 3 Opus | 200K |
| qwen-max | 8K |
| ERNIE-4.0 | 8K |
| GLM-4 | 128K |
| moonshot-v1-128k | 128K |

## 9. 特殊功能支持

### Function Calling
支持的模型:
- OpenAI: GPT-4, GPT-3.5-Turbo
- Gemini: Gemini Pro
- Claude: Claude 3 系列
- 智谱: GLM-4

### Vision (多模态)
支持的模型:
- OpenAI: GPT-4 Turbo with Vision
- Gemini: Gemini Pro Vision
- Claude: Claude 3 Opus/Sonnet

### Streaming
几乎所有模型都支持流式输出

## 10. SDK 与库

### 官方 SDK
- OpenAI: `openai` (Python), `openai-node` (Node.js)
- Google: `@google/generative-ai` (Node.js)
- Anthropic: `@anthropic-ai/sdk` (Node.js)

### .NET 库
- `Azure.AI.OpenAI`
- `Betalgo.OpenAI`
- 自定义 HttpClient 实现

## 11. 最佳实践

### 错误重试
```csharp
var retryPolicy = Policy
    .Handle<HttpRequestException>()
    .Or<TimeoutException>()
    .WaitAndRetryAsync(3, retryAttempt => 
        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
```

### 超时设置
- 短文本: 30 秒
- 长文本: 60 秒
- 流式输出: 120 秒

### Token 估算
- 英文: ~4 字符 = 1 token
- 中文: ~1.5 字符 = 1 token
- 代码: ~1 字符 = 1 token

### 成本优化
1. 使用更便宜的模型（GPT-3.5 vs GPT-4）
2. 减少系统提示词长度
3. 启用缓存（部分提供商支持）
4. 批量处理请求
