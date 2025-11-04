# AI 提供商配置 - 快速参考

## 🚀 快速开始（3 步）

### 步骤 1: 配置 AI 提供商
```
侧边栏 → AI 配置 → AI 提供商 → ➕ 快速配置
```

**选择提供商**（卡片式）:
- 🤖 OpenAI (GPT-4, GPT-3.5)
- 🌟 Google Gemini
- 🧠 Claude
- ☁️ 通义千问
- 🎓 智谱 GLM
- 🌙 Moonshot
- 🦙 Ollama（本地）

**选择模型**（自动加载）:
- 显示推荐模型
- 显示价格信息
- 显示上下文大小

**输入 API Key**:
- 点击 "📋 粘贴" 快速粘贴
- 或手动输入
- Ollama 无需 API Key ✅

### 步骤 2: 测试连接
- 点击 "完成并测试" ✅ 现已可点击
- 系统自动测试连接
- 成功提示或错误提示

### 步骤 3: 在 AI 任务中使用
```
侧边栏 → AI 任务 → 选择 AI 提供商 → 输入需求 → 发送
```

## 📋 常见操作

### 配置 Gemini
1. 快速配置 → 选择 Gemini
2. 选择 "Gemini Pro"
3. 输入 API Key（获取：https://ai.google.dev）
4. 点击 "完成并测试" ✅

### 配置 Ollama（本地）
1. 快速配置 → 选择 Ollama
2. 选择模型（llama3, mistral 等）
3. **无需输入 API Key**
4. 点击 "完成并测试" ✅

### 编辑已有配置
1. AI 提供商 → 列表中点击 "✏️ 编辑"
2. 修改设置
3. 管理 API Keys（添加/删除/启用）
4. 点击 "保存"

### 测试连接
1. AI 提供商 → 列表中点击 "🧪 测试"
2. 系统测试连接
3. 显示成功或失败

### 删除配置
1. AI 提供商 → 列表中点击 "🗑️"
2. 确认删除
3. 配置被删除

## 🎯 使用 AI 任务

### 生成 DSL 脚本
1. 点击 "AI 任务"
2. 顶部选择 AI 提供商
3. 输入需求：
   ```
   创建一个登录任务：
   1. 打开 https://example.com/login
   2. 填写用户名和密码
   3. 点击登录按钮
   4. 等待跳转
   5. 截图保存
   ```
4. 点击 "发送" 或 Ctrl+Enter
5. 右侧预览区显示生成的 DSL

### 快速示例
- 🔐 登录流程
- 🔍 搜索抓取
- 📄 翻页采集
- 📝 表单填写

## 💡 提示

### 快速配置向导
- ✅ 按钮现在在输入 API Key 后立即启用
- ✅ Ollama 用户无需 API Key
- ✅ 自动测试连接
- ✅ 支持多个 API Key（轮询）

### AI 任务
- ✅ 自动加载已配置的提供商
- ✅ 支持随时切换提供商
- ✅ 显示提供商名称和模型
- ✅ 使用选定的提供商生成 DSL

## 🔧 故障排除

### "完成并测试" 按钮无法点击
**解决**: 输入 API Key（Ollama 除外）

### 未找到任何 AI 提供商
**解决**: 先在 "AI 配置" 中添加提供商

### 连接测试失败
**解决**: 检查：
1. API Key 是否正确
2. 网络连接是否正常
3. 提供商服务是否可用

### 生成的 DSL 不符合预期
**解决**: 
1. 更详细地描述需求
2. 指定选择器类型（CSS/XPath）
3. 提供示例 URL
4. 多次生成选择最佳结果

## 📊 支持的提供商

| 提供商 | 模型 | 价格 | 上下文 | 状态 |
|--------|------|------|--------|------|
| OpenAI | GPT-4 Turbo | $0.01/1K | 128K | ✅ |
| Gemini | Gemini Pro | 免费 | 32K | ✅ |
| Claude | Claude 3 | $0.003/1K | 200K | ✅ |
| 通义千问 | Qwen Max | ¥0.04/1K | 8K | ✅ |
| 智谱 GLM | GLM-4 | ¥0.05/1K | 128K | ✅ |
| Moonshot | 128K | ¥0.06/1K | 128K | ✅ |
| Ollama | Llama3 | 免费 | 8K | ✅ |

## 📞 获取帮助

### 文档
- `ai-provider-quick-start.md` - 详细快速开始
- `ai-provider-config-design.md` - 完整设计文档
- `ai-provider-integration-complete.md` - 集成总结

### API Key 获取
- **OpenAI**: https://platform.openai.com/api-keys
- **Gemini**: https://ai.google.dev
- **Claude**: https://console.anthropic.com
- **通义千问**: https://dashscope.aliyun.com
- **智谱 GLM**: https://open.bigmodel.cn
- **Moonshot**: https://platform.moonshot.cn
- **Ollama**: https://ollama.ai（本地）

## ✨ 最新改进

### 快速配置向导
- ✅ 修复：按钮现在在输入 API Key 后立即启用
- ✅ 改进：Ollama 用户体验（无需 API Key）
- ✅ 改进：实时按钮状态反馈

### AI 任务界面
- ✅ 新增：AI 提供商选择器
- ✅ 新增：自动加载提供商列表
- ✅ 改进：使用选定的提供商生成 DSL
- ✅ 改进：支持随时切换提供商

---

**最后更新**: 2025-10-31  
**版本**: 1.0  
**状态**: ✅ 完全可用
