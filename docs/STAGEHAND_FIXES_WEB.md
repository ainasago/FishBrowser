# ✅ Stagehand Web 版本问题修复

## 🔧 修复的问题

### 1. **AI 生成脚本包含 Markdown 标记** ✅

#### 问题
```javascript
// AI 返回的内容：
```javascript
const { Stagehand } = require('@browserbasehq/stagehand');
...
```
```

#### 解决方案
在 `StagehandTaskService.cs` 中添加 `CleanScript` 方法：

```csharp
private string CleanScript(string script)
{
    if (string.IsNullOrEmpty(script))
        return script;

    // 去掉开头的 ```javascript 或 ```js
    script = Regex.Replace(script, @"^```(javascript|js)\s*\n", "", RegexOptions.Multiline);
    
    // 去掉结尾的 ```
    script = Regex.Replace(script, @"\n```\s*$", "", RegexOptions.Multiline);
    
    // 去掉任何其他的 markdown 代码块标记
    script = script.Replace("```javascript", "").Replace("```js", "").Replace("```", "");
    
    return script.Trim();
}
```

#### 应用位置
```csharp
public async Task<GenerateScriptResponse> GenerateScriptAsync(...)
{
    // 调用 AI 生成脚本
    var script = await aiGenerateFunc(fullPrompt, request.ProviderId);
    
    // 清理脚本内容，去掉 markdown 代码块标记
    script = CleanScript(script);
    
    // 分析脚本
    var analysis = AnalyzeScript(script);
    ...
}
```

---

### 2. **DbContext 并发错误** ✅

#### 问题
```
System.InvalidOperationException: A second operation was started on this 
context instance before a previous operation completed. This is usually 
caused by different threads concurrently using the same instance of DbContext.
```

#### 原因
`NodeExecutionService` 在异步事件处理器中调用 `LogService`，而 `LogService` 使用 `DbContext` 保存日志。多个线程同时访问同一个 `DbContext` 实例导致并发错误。

#### 解决方案
在事件处理器中使用 `Console.WriteLine` 替代 `LogService`：

```csharp
// 修改前
process.OutputDataReceived += (sender, e) =>
{
    if (!string.IsNullOrEmpty(e.Data))
    {
        outputBuilder.AppendLine(e.Data);
        if (debug)
        {
            _logService.LogInfo("NodeExecution", $"[STDOUT] {e.Data}");
        }
    }
};

// 修改后
process.OutputDataReceived += (sender, e) =>
{
    if (!string.IsNullOrEmpty(e.Data))
    {
        outputBuilder.AppendLine(e.Data);
        if (debug)
        {
            // 使用 Console 避免 DbContext 并发问题
            Console.WriteLine($"[NodeExecution] [STDOUT] {e.Data}");
        }
    }
};
```

#### 为什么这样修复
- ✅ 事件处理器在不同线程中执行
- ✅ `Console.WriteLine` 是线程安全的
- ✅ 避免了 `DbContext` 的并发访问
- ✅ 日志仍然可以在控制台查看

---

## 📋 修改的文件

### 1. `StagehandTaskService.cs`
```diff
+ private string CleanScript(string script)
+ {
+     // 去掉 markdown 代码块标记
+     ...
+ }

  public async Task<GenerateScriptResponse> GenerateScriptAsync(...)
  {
      var script = await aiGenerateFunc(fullPrompt, request.ProviderId);
+     script = CleanScript(script);
      var analysis = AnalyzeScript(script);
      ...
  }
```

### 2. `NodeExecutionService.cs`
```diff
  process.OutputDataReceived += (sender, e) =>
  {
      if (!string.IsNullOrEmpty(e.Data))
      {
          outputBuilder.AppendLine(e.Data);
          if (debug)
          {
-             _logService.LogInfo("NodeExecution", $"[STDOUT] {e.Data}");
+             Console.WriteLine($"[NodeExecution] [STDOUT] {e.Data}");
          }
      }
  };

  process.ErrorDataReceived += (sender, e) =>
  {
      if (!string.IsNullOrEmpty(e.Data))
      {
          errorBuilder.AppendLine(e.Data);
          if (debug)
          {
-             _logService.LogWarn("NodeExecution", $"[STDERR] {e.Data}");
+             Console.WriteLine($"[NodeExecution] [STDERR] {e.Data}");
          }
      }
  };
```

---

## 🎯 测试验证

### 测试脚本生成
```
1. 访问 Stagehand AI 任务页面
2. 输入："打开 GitHub，搜索 stagehand，点击第一个结果"
3. 点击"生成脚本 ✨"
4. 验证生成的脚本：
   ✅ 没有 ```javascript 标记
   ✅ 没有 ``` 结尾
   ✅ 是纯净的 JavaScript 代码
```

### 测试脚本执行
```
1. 生成脚本后
2. 点击"▶️ 运行脚本"
3. 验证：
   ✅ 没有 DbContext 并发错误
   ✅ 控制台显示执行日志
   ✅ 脚本正常执行
```

---

## 🚀 现在应该可以正常工作了！

### 预期行为

#### 脚本生成
```
用户输入：打开 GitHub，搜索 stagehand，点击第一个结果

AI 返回：
```javascript
const { Stagehand } = require('@browserbasehq/stagehand');
...
```

清理后：
const { Stagehand } = require('@browserbasehq/stagehand');
...

显示给用户：纯净的 JavaScript 代码
```

#### 脚本执行
```
控制台输出：
[NodeExecution] [STDOUT] Stagehand initialized
[NodeExecution] [STDOUT] Navigating to https://github.com
[NodeExecution] [STDOUT] Searching for 'stagehand'
[NodeExecution] [STDOUT] Clicking first result
[NodeExecution] [STDOUT] Task completed!

用户看到：
✅ 脚本执行成功！

输出：
Stagehand initialized
Navigating to https://github.com
...
```

---

## 📚 相关文档

- `STAGEHAND_WEB_INTEGRATION.md` - Web 集成文档
- `STAGEHAND_INTEGRATION_COMPLETE.md` - WPF 集成文档
- `STAGEHAND_IMPLEMENTATION.md` - 实现文档

---

## ✅ 修复完成

- ✅ AI 生成的脚本自动清理 markdown 标记
- ✅ DbContext 并发问题已解决
- ✅ 脚本可以正常执行
- ✅ 日志正常输出到控制台

**现在可以正常使用 Stagehand AI 任务功能了！** 🎭✨
