# AI 任务测试运行器 - 设计文档

## 1. 需求分析

### 1.1 核心需求
- 在 AI 任务界面点击"运行测试"按钮
- 自动启动一个**随机生成的新指纹浏览器**
- 执行用户生成的 DSL 脚本
- 实时显示运行状态、进度和日志
- 弹出美观的进度对话框，显示执行反馈

### 1.2 技术要求
- **解耦设计**：核心执行逻辑不依赖 UI
- **通用性**：可被其他模块复用（任务管理、批量执行等）
- **实时反馈**：支持进度回调和日志流
- **资源管理**：自动清理临时浏览器环境

### 1.3 现有资源
- ✅ Playwright CLI 已安装（HelpView.xaml）
- ✅ PlaywrightController 支持浏览器启动和指纹注入
- ✅ BrowserEnvironmentService 支持环境管理
- ✅ FingerprintGeneratorService 支持随机指纹生成
- ✅ DSL 规范完整（task-flow-dsl-spec.md）

---

## 2. 架构设计

### 2.1 分层架构

```
UI Layer (WPF)
  - AITaskView (RunTest按钮)
  - TaskTestProgressDialog (进度对话框)
      ↓
Service Layer (Core)
  - TaskTestRunnerService (协调器)
  - DslExecutor (DSL解析和执行)
      ↓
Engine Layer
  - PlaywrightController (浏览器控制)
```

### 2.2 核心组件

#### TaskTestRunnerService
职责：协调测试执行流程、管理临时环境、提供进度回调

#### DslExecutor
职责：解析DSL、执行步骤、处理控制流

#### TaskTestProgressDialog
职责：显示进度、实时日志、截图预览

---

## 3. 数据模型

### TestRunOptions
```csharp
public class TestRunOptions
{
    public bool UseRandomFingerprint { get; set; } = true;
    public int? FingerprintProfileId { get; set; }
    public bool Headless { get; set; } = false;
    public int TimeoutSeconds { get; set; } = 300;
    public bool SaveScreenshots { get; set; } = true;
    public bool CleanupAfterTest { get; set; } = true;
}
```

### TestProgress
```csharp
public class TestProgress
{
    public TestStage Stage { get; set; }
    public int CurrentStep { get; set; }
    public int TotalSteps { get; set; }
    public string Message { get; set; }
    public LogLevel Level { get; set; }
    public byte[]? Screenshot { get; set; }
}

public enum TestStage
{
    Initializing,
    GeneratingFingerprint,
    StartingBrowser,
    ExecutingSteps,
    Completed,
    Failed,
    CleaningUp
}
```

### TestRunResult
```csharp
public class TestRunResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan Duration { get; set; }
    public int StepsExecuted { get; set; }
    public List<StepResult> StepResults { get; set; }
    public List<string> Screenshots { get; set; }
    public Dictionary<string, object> ExtractedData { get; set; }
}
```

---

## 4. 执行流程

### 4.1 主流程

```
1. 验证DSL → 2. 生成随机指纹 → 3. 创建临时环境 
→ 4. 启动浏览器 → 5. 执行步骤 → 6. 收集结果 → 7. 清理资源
```

### 4.2 关键实现

#### 生成随机指纹
```csharp
private async Task<FingerprintProfile> GenerateRandomFingerprintAsync()
{
    var preset = await _fingerprintPresetService.GetRandomPresetAsync();
    var profile = await _fingerprintGeneratorService.GenerateFromPresetAsync(preset);
    profile.Name = $"Test_{DateTime.Now:yyyyMMdd_HHmmss}";
    return profile;
}
```

#### 创建临时环境
```csharp
private BrowserEnvironment CreateTemporaryEnvironment(FingerprintProfile profile)
{
    var tempPath = Path.Combine(
        Path.GetTempPath(),
        "WebScraperTest",
        $"test_{Guid.NewGuid():N}"
    );
    
    return new BrowserEnvironment
    {
        Name = $"TestEnv_{DateTime.Now:HHmmss}",
        FingerprintProfile = profile,
        UserDataPath = tempPath,
        EnablePersistence = false
    };
}
```

---

## 5. UI 设计

### 5.1 进度对话框布局

```
┌─────────────────────────────────────────┐
│  🧪 测试运行中 - 登录任务          ✖    │
├─────────────────────────────────────────┤
│  当前阶段：执行步骤 (3/5)               │
│  ████████████████░░░░░░░░░░░ 60%       │
│                                         │
│  ┌───────────────────────────────────┐ │
│  │ 📋 执行日志                        │ │
│  ├───────────────────────────────────┤ │
│  │ [14:23:01] ✅ 初始化浏览器         │ │
│  │ [14:23:02] ✅ 生成随机指纹         │ │
│  │ [14:23:03] ▶️  执行步骤 1: open   │ │
│  └───────────────────────────────────┘ │
│                                         │
│  ┌───────────────────────────────────┐ │
│  │ 📸 实时截图                        │ │
│  │   [浏览器截图预览]                 │ │
│  └───────────────────────────────────┘ │
│                                         │
│  ⏸️ 暂停  ⏹️ 停止  📋 复制日志  关闭  │
└─────────────────────────────────────────┘
```

### 5.2 样式规范

- **主色调**: #2196F3 (蓝色)
- **成功色**: #4CAF50 (绿色)
- **警告色**: #FF9800 (橙色)
- **错误色**: #F44336 (红色)
- **背景色**: #F5F5F5
- **边框色**: #E0E0E0
- **字体**: 微软雅黑, Segoe UI
- **代码字体**: Consolas, Courier New

---

## 6. 文件清单

### 新建文件

#### Services
- `TaskTestRunnerService.cs` - 测试运行协调器
- `DslExecutor.cs` - DSL 执行器
- `DslParser.cs` - DSL 解析器

#### Models
- `TestRunOptions.cs` - 测试运行选项
- `TestProgress.cs` - 测试进度
- `TestRunResult.cs` - 测试结果
- `DslFlow.cs` - DSL 流程模型
- `DslStep.cs` - DSL 步骤模型

#### Views/Dialogs
- `TaskTestProgressDialog.xaml` - 进度对话框 UI
- `TaskTestProgressDialog.xaml.cs` - 进度对话框逻辑

#### Engine (扩展)
- `PlaywrightController.cs` - 添加步骤执行方法

### 修改文件

#### Views
- `AITaskView.xaml` - 添加"运行测试"按钮
- `AITaskView.xaml.cs` - 实现 RunTest_Click

---

## 7. 实现步骤

### Phase 1: 核心服务（2天）
1. 创建 DslParser 和 DslExecutor
2. 创建 TaskTestRunnerService
3. 扩展 PlaywrightController 步骤执行方法
4. 单元测试

### Phase 2: UI 实现（1天）
1. 创建 TaskTestProgressDialog
2. 实现进度更新和日志显示
3. 实现截图预览
4. 样式美化

### Phase 3: 集成（1天）
1. 在 AITaskView 中集成
2. 添加"运行测试"按钮
3. 连接服务和 UI
4. 错误处理

### Phase 4: 测试和优化（1天）
1. 端到端测试
2. 性能优化
3. 用户体验优化
4. 文档完善

---

## 8. 技术要点

### 8.1 异步和取消
- 使用 `CancellationToken` 支持取消
- 使用 `IProgress<T>` 报告进度
- 避免阻塞 UI 线程

### 8.2 资源清理
- 使用 `try-finally` 确保清理
- 删除临时文件和目录
- 关闭浏览器和上下文

### 8.3 错误处理
- 捕获所有异常
- 提供详细错误信息
- 支持步骤级重试

### 8.4 性能优化
- 异步执行步骤
- 批量更新 UI
- 截图压缩

---

## 9. 测试计划

### 9.1 单元测试
- DslParser 解析测试
- DslExecutor 步骤执行测试
- TaskTestRunnerService 流程测试

### 9.2 集成测试
- 完整流程测试
- 错误场景测试
- 取消和暂停测试

### 9.3 UI 测试
- 进度更新测试
- 日志显示测试
- 截图预览测试

---

## 10. 后续扩展

### 10.1 高级功能
- 断点调试
- 步骤编辑和重试
- 变量查看器
- 网络请求监控

### 10.2 批量测试
- 多任务并行测试
- 测试报告生成
- 性能基准测试

### 10.3 CI/CD 集成
- 命令行测试运行器
- 测试结果导出
- 自动化测试流水线

---

**版本**: 1.0  
**创建时间**: 2025-10-31  
**状态**: 设计阶段
