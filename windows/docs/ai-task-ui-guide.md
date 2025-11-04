# AI 任务界面使用指南

## 概述
AI 任务界面提供了一个 ChatGPT 风格的对话式任务编辑器，让用户通过自然语言描述需求，由 AI 自动生成符合 DSL 规范的任务脚本。

## 界面布局

### 1. 顶部工具栏
- **状态指示器**：显示当前状态（就绪/处理中）
- **历史任务**：查看和加载历史任务
- **清空对话**：清除当前对话历史
- **设置**：配置 AI 参数（API Key、模型等）

### 2. 左侧对话区
- **欢迎消息**：系统介绍和使用提示
- **快捷示例**：4 个预设场景快速开始
  - 🔐 登录流程
  - 🔍 搜索抓取
  - 📄 翻页采集
  - 📝 表单填写
- **对话历史**：用户消息（蓝色气泡）+ AI 回复（灰色气泡）

### 3. 右侧预览区
- **DSL 脚本预览**：实时显示生成的 YAML 脚本
- **操作按钮**：
  - ✅ 保存任务：保存到任务库
  - ▶️ 运行测试：启动浏览器测试执行
  - 📋 复制脚本：复制到剪贴板
  - 📥 导出 YAML：导出为文件

### 4. 底部输入区
- **输入框**：支持多行输入，Ctrl+Enter 发送
- **发送按钮**：提交需求给 AI

## 使用流程

### 快速开始（推荐）
1. 点击快捷示例卡片（如"搜索抓取"）
2. 输入框自动填充示例提示词
3. 点击"发送"或按 Ctrl+Enter
4. AI 生成脚本并显示在右侧预览区
5. 检查脚本，点击"保存任务"或"运行测试"

### 自定义需求
1. 在输入框描述你的任务需求，例如：
   ```
   帮我创建一个任务：
   1. 打开 https://example.com
   2. 登录（用户名和密码从配置读取）
   3. 搜索"手机"、"电脑"、"耳机"三个关键词
   4. 每个关键词提取前10条结果的标题、价格、链接
   5. 保存到数据库
   ```
2. AI 会分析需求并生成对应的 DSL 脚本
3. 右侧预览区显示生成的 YAML 代码
4. 可以继续对话优化脚本（如"增加重试机制"、"添加截图步骤"）

## 提示词技巧

### 清晰描述步骤
✅ 好的提示词：
```
创建一个登录任务：
1. 打开 https://example.com/login
2. 填写用户名（CSS选择器：#username）
3. 填写密码（CSS选择器：#password）
4. 点击登录按钮（文本为"登录"）
5. 等待跳转到首页
6. 截图保存
```

❌ 不好的提示词：
```
帮我做个登录
```

### 指定选择器
如果你知道页面元素的选择器，可以直接提供：
```
搜索框的选择器是 input[name="q"]
搜索按钮是 button.search-btn
结果列表是 .result-item
```

### 说明数据提取需求
```
提取每个商品的：
- 标题（h3 标签）
- 价格（.price 类）
- 链接（a 标签的 href）
- 图片（img 的 src）
```

### 控制流需求
```
需要循环处理 5 页数据
如果没有下一页按钮就停止
每页之间等待 2 秒
```

## 生成的 DSL 脚本

### 脚本结构
```yaml
dslVersion: "1.0"
id: flow_xxx
name: 任务名称
description: 任务描述
settings:
  selectorTimeoutMs: 6000
  navTimeoutMs: 15000
vars:
  # 变量定义
steps:
  # 步骤列表
```

### 常见步骤类型
- `open`: 打开页面
- `click`: 点击元素
- `type/fill`: 输入文本
- `waitFor`: 等待元素
- `extract`: 提取数据
- `if/for/while`: 控制流
- `screenshot`: 截图
- `emit`: 输出数据

## 后续操作

### 保存任务
点击"保存任务"后，脚本会保存到任务库，可以在"任务管理"页面：
- 查看所有任务
- 编辑任务参数
- 批量执行
- 查看执行历史

### 运行测试
点击"运行测试"会：
1. 启动指纹浏览器
2. 逐步执行脚本
3. 实时显示日志
4. 保存截图和抓取数据
5. 显示执行结果

### 导出脚本
导出为 `.yml` 文件后可以：
- 版本控制（Git）
- 分享给团队
- 在其他环境导入
- 手动编辑优化

## 高级功能（开发中）

### 历史任务
- 查看所有生成过的任务
- 搜索和筛选
- 重新编辑和优化
- 复制为新任务

### AI 设置
- 配置 OpenAI API Key
- 选择模型（GPT-4/GPT-3.5/本地模型）
- 调整温度参数（创造性 vs 确定性）
- 自定义系统提示词模板

### 智能优化
- AI 自动检测脚本问题
- 建议优化方案（重试、等待、错误处理）
- 性能优化建议
- 反爬策略建议

## 集成 AI API

### 当前实现
目前使用占位实现，根据关键词返回示例脚本。

### 集成 OpenAI
修改 `AITaskView.xaml.cs` 中的 `GenerateDslFromPromptAsync` 方法：

```csharp
private async Task<string> GenerateDslFromPromptAsync(string prompt)
{
    var apiKey = "your-openai-api-key";
    var client = new HttpClient();
    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

    var systemPrompt = @"你是一个网页自动化任务专家。用户会描述任务需求，你需要生成符合 Task Flow DSL v1.0 规范的 YAML 脚本。

规范要点：
- 必须包含 dslVersion、id、name、steps
- 选择器格式：{ type: css|xpath|text|role, value: string }
- 常用步骤：open、click、type、fill、waitFor、extract、if、for
- 参考文档：d:\1Dev\webscraper\windows\docs\task-flow-dsl-spec.md

只输出 YAML 代码，不要解释。";

    var payload = new
    {
        model = "gpt-4",
        messages = new[]
        {
            new { role = "system", content = systemPrompt },
            new { role = "user", content = prompt }
        },
        temperature = 0.7
    };

    var response = await client.PostAsJsonAsync(
        "https://api.openai.com/v1/chat/completions", 
        payload
    );

    var result = await response.Content.ReadFromJsonAsync<OpenAIResponse>();
    return result?.Choices[0]?.Message?.Content ?? "生成失败";
}
```

### 集成本地模型
可以使用 Ollama、LM Studio 等本地运行的模型：
```csharp
var response = await client.PostAsJsonAsync(
    "http://localhost:11434/api/generate", 
    new { model = "llama2", prompt = systemPrompt + "\n\n" + prompt }
);
```

## 最佳实践

### 1. 迭代优化
- 先生成基础脚本
- 测试运行发现问题
- 继续对话优化（"增加等待时间"、"添加错误处理"）

### 2. 复用模板
- 保存常用任务为模板
- 修改变量和选择器即可复用

### 3. 结合手动编辑
- AI 生成初始脚本
- 手动微调选择器和等待时间
- 添加特定业务逻辑

### 4. 验证与测试
- 先在测试环境运行
- 检查日志和截图
- 确认无误后批量执行

## 故障排查

### AI 生成的脚本无法运行
- 检查选择器是否正确（使用浏览器开发者工具验证）
- 调整等待时间（网络慢时增加 timeout）
- 查看执行日志定位问题步骤

### 脚本格式错误
- 确保 YAML 缩进正确（使用空格，不要用 Tab）
- 检查引号匹配
- 使用在线 YAML 验证器

### 数据提取不完整
- 检查选择器是否匹配所有目标元素
- 使用 `extract` 的数组语法（`results[]:`）
- 添加等待确保元素加载完成

## 下一步

- [ ] 集成真实 AI API（OpenAI/Azure/本地）
- [ ] 实现任务保存到数据库
- [ ] 集成任务执行引擎
- [ ] 添加脚本校验与语法高亮
- [ ] 支持多轮对话上下文
- [ ] 实现历史任务管理
- [ ] 添加 AI 配置界面
