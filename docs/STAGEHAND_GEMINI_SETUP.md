# 🔑 Stagehand Gemini API Key 配置指南

## ✅ 已完成的功能

### 1. Web 界面 Gemini 设置面板
- ✅ 顶部添加"Gemini 设置"按钮
- ✅ 可展开/收起的设置面板
- ✅ API Key 输入（支持显示/隐藏）
- ✅ 模型选择（Gemini 2.0/2.5/1.5 系列）
- ✅ 保存到浏览器本地存储
- ✅ 测试连接功能
- ✅ 清除设置功能

### 2. 后端环境变量传递
- ✅ 接收前端传递的 Gemini 设置
- ✅ 设置 `GOOGLE_GENERATIVE_AI_API_KEY` 环境变量
- ✅ 设置 `STAGEHAND_MODEL` 环境变量
- ✅ 传递给 Node.js 进程

### 3. Stagehand 3.x API 更新
- ✅ 更新系统提示词使用 `context.pages()[0]`
- ✅ 更新脚本模板
- ✅ 更新脚本修复逻辑

## 📋 使用流程

### 1. 获取 Gemini API Key
```
访问：https://aistudio.google.com/app/apikey
登录 Google 账号
创建 API Key
复制 API Key
```

### 2. 配置 Gemini 设置
```
1. 打开 Stagehand AI 任务页面
2. 点击顶部"Gemini 设置"按钮
3. 输入 Gemini API Key
4. 选择模型（推荐 Gemini 2.0 Flash）
5. 点击"保存设置"
6. （可选）点击"测试连接"验证
```

### 3. 执行脚本
```
1. 生成 Stagehand 脚本
2. 点击"运行脚本"
3. 系统自动传递 Gemini 设置给 Node.js
4. Stagehand 使用 Gemini AI 执行智能操作
```

## 🔧 技术实现

### 前端（Index.cshtml）

#### 设置面板
```html
<div id="geminiSettingsPanel" class="card mb-3">
    <div class="card-body">
        <!-- API Key 输入 -->
        <input type="password" id="geminiApiKey" />
        
        <!-- 模型选择 -->
        <select id="geminiModel">
            <option value="google/gemini-2.0-flash-exp">Gemini 2.0 Flash</option>
            <option value="google/gemini-2.5-flash">Gemini 2.5 Flash</option>
            ...
        </select>
        
        <!-- 操作按钮 -->
        <button onclick="saveGeminiSettings()">保存</button>
        <button onclick="testGeminiConnection()">测试</button>
    </div>
</div>
```

#### JavaScript 函数
```javascript
// 保存到 localStorage
function saveGeminiSettings() {
    localStorage.setItem('stagehand_gemini_api_key', apiKey);
    localStorage.setItem('stagehand_gemini_model', model);
}

// 执行脚本时传递
async function executeScript() {
    const geminiSettings = getGeminiSettings();
    
    await fetch('/StagehandTask/ExecuteScript', {
        body: JSON.stringify({
            script: script,
            geminiApiKey: geminiSettings.apiKey,
            geminiModel: geminiSettings.model
        })
    });
}
```

### 后端

#### DTO (StagehandTaskDto.cs)
```csharp
public class ExecuteScriptRequest
{
    public string Script { get; set; } = "";
    public bool Debug { get; set; }
    public string? GeminiApiKey { get; set; }
    public string? GeminiModel { get; set; }
}
```

#### NodeExecutionService.cs
```csharp
public async Task<ExecutionResult> ExecuteScriptAsync(
    string script, 
    bool debug = false, 
    string? geminiApiKey = null, 
    string? geminiModel = null)
{
    var startInfo = new ProcessStartInfo { ... };
    
    // 设置环境变量
    if (!string.IsNullOrEmpty(geminiApiKey))
    {
        startInfo.EnvironmentVariables["GOOGLE_GENERATIVE_AI_API_KEY"] = geminiApiKey;
    }
    
    if (!string.IsNullOrEmpty(geminiModel))
    {
        startInfo.EnvironmentVariables["STAGEHAND_MODEL"] = geminiModel;
    }
    
    // 执行 Node.js 脚本
    ...
}
```

## 🎯 环境变量说明

### GOOGLE_GENERATIVE_AI_API_KEY
- **作用**：Gemini API 认证
- **必需**：是（用于 Stagehand 的 AI 功能）
- **来源**：用户在 Web 界面配置
- **传递方式**：通过 ProcessStartInfo.EnvironmentVariables

### STAGEHAND_MODEL
- **作用**：指定使用的 Gemini 模型
- **必需**：否（有默认值）
- **默认值**：`google/gemini-2.0-flash-exp`
- **可选值**：
  - `google/gemini-2.0-flash-exp` (推荐)
  - `google/gemini-2.5-flash`
  - `google/gemini-1.5-pro`
  - `google/gemini-1.5-flash`

### NODE_PATH
- **作用**：让 Node.js 找到全局安装的模块
- **必需**：是
- **值**：`C:\Users\{用户}\AppData\Roaming\npm\node_modules`
- **自动设置**：是

## 📊 Stagehand 配置示例

### 在生成的脚本中（自动）
```javascript
const { Stagehand } = require('@browserbasehq/stagehand');

const stagehand = new Stagehand({
    env: 'LOCAL',  // 使用本地浏览器
    verbose: 1,
    debugDom: true
    // model 会自动从 STAGEHAND_MODEL 环境变量读取
    // API key 会自动从 GOOGLE_GENERATIVE_AI_API_KEY 环境变量读取
});

await stagehand.init();
const page = stagehand.context.pages()[0];  // Stagehand 3.x API

// 使用 Gemini AI 执行智能操作
await stagehand.act('点击登录按钮');
await stagehand.extract('提取商品信息', schema);
```

## ⚠️ 注意事项

### 1. API Key 安全
- ✅ API Key 保存在浏览器本地存储
- ✅ 不会上传到服务器数据库
- ✅ 仅在脚本执行时作为环境变量传递
- ⚠️ 建议不要在公共电脑上保存

### 2. 模型选择
- **Gemini 2.0 Flash**：速度快，效果好（推荐）
- **Gemini 2.5 Flash**：最新版本
- **Gemini 1.5 Pro**：更强大，但速度较慢
- **Gemini 1.5 Flash**：平衡选择

### 3. 成本控制
- Gemini API 有免费额度
- 超出后按使用量计费
- 建议在 Google AI Studio 查看使用情况

## 🔍 故障排查

### 问题 1：脚本执行失败，提示 API Key 无效
**解决**：
1. 检查 API Key 是否正确
2. 点击"测试连接"验证
3. 确认 API Key 在 Google AI Studio 中是否启用

### 问题 2：Stagehand 无法执行智能操作
**解决**：
1. 确认已配置 Gemini API Key
2. 检查网络连接
3. 查看控制台日志中的环境变量设置

### 问题 3：找不到 Stagehand 模块
**解决**：
1. 确认 Stagehand 已全局安装
2. 检查 NODE_PATH 环境变量
3. 运行 `npm list -g @browserbasehq/stagehand`

## 📚 相关文档

- [Stagehand 官方文档](https://docs.stagehand.dev)
- [Gemini API 文档](https://ai.google.dev/gemini-api/docs)
- [获取 Gemini API Key](https://aistudio.google.com/app/apikey)

## ✅ 完成清单

- ✅ Gemini 设置面板
- ✅ API Key 输入和保存
- ✅ 模型选择
- ✅ 测试连接功能
- ✅ 环境变量传递
- ✅ Stagehand 3.x API 支持
- ✅ 脚本自动修复
- ✅ 错误提示和验证

**现在可以使用 Gemini AI 驱动的 Stagehand 自动化了！** 🎉✨
