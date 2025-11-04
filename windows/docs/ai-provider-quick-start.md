# AI 提供商配置 - 快速开始指南

## 🚀 5 分钟快速开始

### 步骤 1: 运行数据库迁移

```bash
cd d:\1Dev\webscraper\windows\WebScraperApp
dotnet ef migrations add AddAIProvider
dotnet ef database update
```

或者**简单方式**（开发环境）：
1. 关闭应用
2. 删除 `webscraper.db` 文件
3. 重新启动应用（自动创建新表）

### 步骤 2: 配置 AI 提供商（通过代码）

在应用启动后，通过代码配置（临时方案，等待 UI 完成）：

```csharp
// 在 App.xaml.cs 的 OnStartup 中添加
using (var scope = Host.Services.CreateScope())
{
    var providerService = scope.ServiceProvider.GetRequiredService<IAIProviderService>();
    
    // 检查是否已有配置
    var existing = await providerService.GetAllProvidersAsync();
    if (!existing.Any())
    {
        // 创建 OpenAI 配置
        var openai = new AIProviderConfig
        {
            Name = "OpenAI GPT-4",
            ProviderType = AIProviderType.OpenAI,
            ModelId = "gpt-4-turbo",
            BaseUrl = "https://api.openai.com/v1",
            IsEnabled = true,
            Priority = 1,
            Settings = new AIProviderSettings
            {
                Temperature = 0.7,
                MaxTokens = 2000,
                TimeoutSeconds = 60,
                MaxRetries = 3
            }
        };
        
        await providerService.CreateProviderAsync(openai);
        
        // 添加 API Key（替换为你的真实 Key）
        await providerService.AddApiKeyAsync(
            openai.Id, 
            "主密钥", 
            "sk-proj-YOUR_API_KEY_HERE"
        );
        
        Console.WriteLine("✅ OpenAI 配置完成");
    }
}
```

### 步骤 3: 使用 AI 任务界面

1. 启动应用
2. 点击侧边栏"AI 任务"
3. 输入需求：
   ```
   帮我创建一个登录任务：
   1. 打开 https://example.com/login
   2. 填写用户名和密码
   3. 点击登录按钮
   4. 等待跳转
   5. 截图保存
   ```
4. 点击"发送 ➤"
5. 查看右侧生成的 DSL 脚本
6. 点击"保存任务"或"复制脚本"

## 📋 支持的 AI 提供商配置示例

### OpenAI
```csharp
var config = new AIProviderConfig
{
    Name = "OpenAI GPT-4",
    ProviderType = AIProviderType.OpenAI,
    ModelId = "gpt-4-turbo",
    BaseUrl = "https://api.openai.com/v1",
    IsEnabled = true
};
```

### Google Gemini
```csharp
var config = new AIProviderConfig
{
    Name = "Google Gemini Pro",
    ProviderType = AIProviderType.GoogleGemini,
    ModelId = "gemini-pro",
    BaseUrl = "https://generativelanguage.googleapis.com/v1beta",
    IsEnabled = true
};
```

### 阿里云通义千问
```csharp
var config = new AIProviderConfig
{
    Name = "通义千问 Max",
    ProviderType = AIProviderType.AlibabaQwen,
    ModelId = "qwen-max",
    BaseUrl = "https://dashscope.aliyuncs.com/api/v1",
    IsEnabled = true
};
```

### Ollama（本地）
```csharp
var config = new AIProviderConfig
{
    Name = "Ollama Llama3",
    ProviderType = AIProviderType.Ollama,
    ModelId = "llama3",
    BaseUrl = "http://localhost:11434",
    IsEnabled = true
};
// Ollama 不需要 API Key
```

## 🔑 获取 API Key

### OpenAI
1. 访问 https://platform.openai.com/api-keys
2. 点击 "Create new secret key"
3. 复制密钥（以 `sk-proj-` 开头）

### Google Gemini
1. 访问 https://ai.google.dev
2. 点击 "Get API key"
3. 创建或选择项目
4. 复制 API Key

### 阿里云通义千问
1. 访问 https://dashscope.aliyun.com
2. 登录阿里云账号
3. 创建 API Key
4. 复制密钥

### Ollama（本地）
1. 下载安装 Ollama: https://ollama.ai
2. 运行 `ollama pull llama3`
3. 启动服务（自动运行在 localhost:11434）
4. 无需 API Key

## 💡 使用技巧

### 1. 配置多个 API Key（轮询）
```csharp
await providerService.AddApiKeyAsync(config.Id, "主密钥", "sk-proj-xxx");
await providerService.AddApiKeyAsync(config.Id, "备用密钥", "sk-proj-yyy");
await providerService.AddApiKeyAsync(config.Id, "测试密钥", "sk-proj-zzz");
```

系统会自动轮询使用，避免单个 Key 超限。

### 2. 设置每日限额
```csharp
var key = await providerService.AddApiKeyAsync(config.Id, "限额密钥", "sk-proj-xxx");
key.DailyLimit = 1000; // 每天最多 1000 次
await providerService.UpdateApiKeyAsync(key);
```

### 3. 查看使用统计
```csharp
var stats = await providerService.GetUsageStatsAsync(
    config.Id, 
    DateTime.Today, 
    DateTime.Now
);

Console.WriteLine($"今日使用: {stats.TotalRequests} 次");
Console.WriteLine($"总 Token: {stats.TotalTokens}");
Console.WriteLine($"总成本: ${stats.TotalCost:F4}");
```

### 4. 测试连接
```csharp
var isHealthy = await providerService.TestConnectionAsync(config.Id);
if (isHealthy)
{
    Console.WriteLine("✅ 连接正常");
}
else
{
    Console.WriteLine("❌ 连接失败，请检查配置");
}
```

## 🐛 常见问题

### Q: 提示"No available AI provider configured"
**A**: 需要先配置 AI 提供商。参考步骤 2。

### Q: 提示"No available API key"
**A**: 需要为提供商添加 API Key。

### Q: API 调用失败
**A**: 检查：
1. API Key 是否正确
2. 网络连接是否正常
3. 提供商服务是否可用
4. 是否超出配额限制

### Q: 生成的 DSL 不符合预期
**A**: 可以：
1. 更详细地描述需求
2. 指定选择器类型（CSS/XPath）
3. 提供示例 URL
4. 多次生成选择最佳结果

### Q: 想使用本地模型
**A**: 推荐使用 Ollama：
```bash
# 安装 Ollama
winget install Ollama.Ollama

# 下载模型
ollama pull llama3

# 配置（无需 API Key）
var config = new AIProviderConfig
{
    ProviderType = AIProviderType.Ollama,
    ModelId = "llama3",
    BaseUrl = "http://localhost:11434"
};
```

## 📊 成本估算

### OpenAI 价格（USD/1M tokens）
| 模型 | 输入 | 输出 |
|------|------|------|
| GPT-4 Turbo | $10 | $30 |
| GPT-3.5 Turbo | $0.50 | $1.50 |

### 示例计算
生成一个 DSL 脚本：
- 输入（系统提示词 + 用户需求）: ~1000 tokens
- 输出（DSL 脚本）: ~500 tokens

**GPT-4 Turbo 成本**: 
- 输入: 1000/1000000 * $10 = $0.01
- 输出: 500/1000000 * $30 = $0.015
- **总计**: ~$0.025 (约 ¥0.18)

**GPT-3.5 Turbo 成本**:
- **总计**: ~$0.0013 (约 ¥0.01)

### 省钱技巧
1. 使用 GPT-3.5 Turbo（性价比高）
2. 使用本地模型（完全免费）
3. 设置每日限额
4. 配置多个 Key 轮询

## 🎯 下一步

### 立即可用
- ✅ 配置 AI 提供商
- ✅ 在 AI 任务界面生成 DSL
- ✅ 查看使用统计
- ✅ 测试连接

### 等待 UI 完成后
- ⏳ 图形化配置界面
- ⏳ 可视化使用统计
- ⏳ 健康状态监控
- ⏳ 快速配置向导

## 📞 获取帮助

### 文档
- `ai-provider-config-design.md` - 完整设计
- `ai-provider-api-reference.md` - API 参考
- `ai-provider-final-summary.md` - 实现总结

### 代码示例
- `Services/AIProviderService.cs` - 核心服务
- `Services/AIClientService.cs` - 统一调用
- `Views/AITaskView.xaml.cs` - UI 集成

---

**提示**: 当前通过代码配置，UI 管理界面开发中。核心功能已完全可用！
