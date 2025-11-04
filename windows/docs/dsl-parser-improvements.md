# DSL Parser 改进说明

## 📅 更新日期
2025-10-31

## 🎯 改进目标
解决 YAML 解析错误，并改进日志记录，便于调试和问题排查。

---

## 🐛 问题描述

### 原始错误
```
[ERR] [DslParser] DSL 解析失败: While scanning for the next token, found character that cannot start any token.
```

### 问题原因
1. AI 生成的 DSL 可能包含 Markdown 代码块标记（```yaml 和 ```）
2. 日志中没有显示 DSL 脚本内容，难以调试
3. 错误信息不够详细，无法定位具体问题

---

## ✅ 改进内容

### 1. **自动清理 Markdown 代码块标记**

**文件**: `Services/DslParser.cs`

**改进**:
```csharp
// 清理 YAML（移除 Markdown 代码块标记）
yaml = yaml.Trim();
if (yaml.StartsWith("```yaml") || yaml.StartsWith("```"))
{
    var lines = yaml.Split('\n');
    yaml = string.Join('\n', lines.Skip(1).SkipLast(1));
    _logger.LogInfo("DslParser", "Removed markdown code block markers");
}
```

**效果**:
- 自动检测并移除 ```yaml 和 ``` 标记
- 支持 AI 直接返回带代码块的 YAML
- 记录清理操作到日志

---

### 2. **增强错误日志**

**文件**: `Services/DslParser.cs`

**改进**:
```csharp
catch (Exception ex)
{
    var errorMsg = $"DSL 解析失败: {ex.Message}";
    var detailMsg = $"错误详情:\n类型: {ex.GetType().Name}\n消息: {ex.Message}";
    
    // 如果是 YAML 解析错误，尝试提供更多上下文
    if (ex is YamlDotNet.Core.YamlException yamlEx)
    {
        detailMsg += $"\n位置: Line {yamlEx.Start.Line}, Column {yamlEx.Start.Column}";
    }
    
    _logger.LogError("DslParser", errorMsg, detailMsg);
    return (false, null, $"YAML 解析错误: {ex.Message}");
}
```

**效果**:
- 显示异常类型
- 显示详细错误消息
- 对于 YAML 错误，显示具体行列位置
- 便于快速定位问题

---

### 3. **添加 DSL 脚本预览日志**

#### TaskTestRunnerService

**文件**: `Services/TaskTestRunnerService.cs`

**改进**:
```csharp
_logger.LogInfo("TaskTestRunner", "Starting test run");

// 记录 DSL 脚本预览
var dslPreview = dslYaml.Length > 500 ? dslYaml.Substring(0, 500) + "..." : dslYaml;
_logger.LogInfo("TaskTestRunner", $"DSL Script Preview:\n{dslPreview}");
```

**效果**:
- 在测试运行开始时记录 DSL 内容
- 限制预览长度（500 字符）避免日志过长
- 便于查看实际传入的 DSL

#### AITaskView

**文件**: `Views/AITaskView.xaml.cs`

**改进**:
```csharp
// 调用 AI 生成 DSL
var dslScript = await GenerateDslFromPromptAsync(userInput);

// 记录生成的 DSL
var dslPreview = dslScript.Length > 300 ? dslScript.Substring(0, 300) + "..." : dslScript;
_logger?.LogInfo("AITaskView", $"Generated DSL:\n{dslPreview}");
```

**效果**:
- 在 AI 生成 DSL 后立即记录
- 限制预览长度（300 字符）
- 便于对比生成内容和解析输入

---

### 4. **增加解析过程日志**

**文件**: `Services/DslParser.cs`

**改进**:
```csharp
_logger.LogInfo("DslParser", $"Parsing YAML ({yaml.Length} chars)");

// ... 解析逻辑 ...

_logger.LogInfo("DslParser", $"DSL 验证成功: {flow.Name} ({flow.Steps.Count} steps)");
```

**效果**:
- 记录 YAML 长度
- 记录解析成功的流程名称和步骤数
- 便于追踪解析过程

---

## 📊 改进效果

### 改进前的日志
```
[INF] [TaskTestRunner] Starting test run
[ERR] [DslParser] DSL 解析失败: While scanning for the next token...
[INF] [TaskTestRunner] Test run completed in 0.23s
```

### 改进后的日志
```
[INF] [TaskTestRunner] Starting test run
[INF] [TaskTestRunner] DSL Script Preview:
dslVersion: "1.0"
id: flow_login_example
name: 网站登录流程
...

[INF] [DslParser] Removed markdown code block markers
[INF] [DslParser] Parsing YAML (523 chars)
[INF] [DslParser] DSL 验证成功: 网站登录流程 (8 steps)
[INF] [TaskTestRunner] Test run completed in 0.45s
```

或者错误情况：
```
[INF] [TaskTestRunner] Starting test run
[INF] [TaskTestRunner] DSL Script Preview:
```yaml
dslVersion: "1.0"
...

[INF] [DslParser] Removed markdown code block markers
[INF] [DslParser] Parsing YAML (523 chars)
[ERR] [DslParser] DSL 解析失败: Invalid YAML syntax
错误详情:
类型: YamlException
消息: Invalid YAML syntax
位置: Line 5, Column 12
```

---

## 🎯 使用建议

### 1. 调试 DSL 解析问题
1. 查看 `[TaskTestRunner] DSL Script Preview` 日志
2. 检查是否有 Markdown 代码块标记
3. 查看 `[DslParser]` 的详细错误信息
4. 根据行列位置定位问题

### 2. AI 生成的 DSL
- 现在支持带 ```yaml 标记的输出
- 自动清理，无需手动处理
- 查看 `[AITaskView] Generated DSL` 日志确认生成内容

### 3. 常见问题排查
- **缩进错误**: 查看错误位置，检查 YAML 缩进（必须是空格，不能是 Tab）
- **特殊字符**: 查看预览日志，检查是否有非法字符
- **格式问题**: 对比示例 DSL，检查结构是否正确

---

## 📝 相关文件

**修改的文件**:
1. `Services/DslParser.cs` - 核心改进
2. `Services/TaskTestRunnerService.cs` - 添加预览日志
3. `Views/AITaskView.xaml.cs` - 添加生成日志

**相关文档**:
- `docs/task-flow-dsl-spec.md` - DSL 规范
- `docs/todo-implementation-summary.md` - TODO 实现总结

---

## 🚀 后续优化

### 可选改进
1. **YAML 格式化**: 在解析前自动格式化 YAML
2. **语法高亮**: 在 UI 中显示 YAML 语法高亮
3. **实时验证**: 在编辑时实时验证 YAML 语法
4. **错误提示**: 在 UI 中显示具体错误位置

### Phase 2 计划
- 完整的 DSL 编辑器
- 可视化 DSL 构建器
- DSL 调试器
- 步骤断点

---

## ✅ 验证清单

- [x] 自动清理 Markdown 代码块
- [x] 详细的错误日志
- [x] DSL 脚本预览
- [x] 解析过程日志
- [x] 行列位置提示
- [x] 异常类型显示

所有改进已完成并测试通过！
