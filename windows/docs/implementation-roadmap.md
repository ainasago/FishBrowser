# AI 脚本调试器 - 实现路线图

## 📅 创建日期
2025-10-31

## 🎯 总体目标

在 2-3 周内完成 AI 可视化脚本调试器的核心功能，使普通用户能够通过可视化界面和 AI 辅助创建、调试网页自动化任务。

---

## 📋 里程碑规划

### M1: 基础可视化调试（2-3 天）✅ 当前阶段

**目标**：搭建基础 UI 和浏览器控制能力

#### 任务清单

1. **创建 IBrowserController 接口** ⏳
   - 定义统一的浏览器控制 API
   - 包含：Navigate、Click、Fill、Wait、Screenshot 等
   - 事件：Console、Request、Response
   - 文件：`Services/IBrowserController.cs`

2. **实现 WebView2Controller** ⏳
   - 实现 IBrowserController 接口
   - 集成 WebView2 控件
   - 使用 DevTools Protocol 实现高级功能
   - 文件：`Services/WebView2Controller.cs`

3. **重构 DslExecutor 支持 IBrowserController** ⏳
   - 构造函数注入 IBrowserController
   - 移除对 PlaywrightController 的直接依赖
   - 保持向后兼容
   - 文件：`Services/DslExecutor.cs`

4. **创建 AIDebugWorkbench 视图** ⏳
   - 三栏布局：YAML 编辑器 + WebView2 + AI 面板
   - 基础控制按钮：运行、单步、停止
   - 文件：`Views/AIDebugWorkbench.xaml(.cs)`

5. **在 AITaskView 添加入口** ⏳
   - "AI 脚本助手"按钮
   - 点击打开 AIDebugWorkbench
   - 传递当前 DSL 内容
   - 文件：`Views/AITaskView.xaml(.cs)`

#### 验收标准
- ✅ 可以在 AITaskView 点击按钮打开调试工作台
- ✅ 工作台显示三栏布局
- ✅ 可以在 WebView2 中执行 DSL 脚本
- ✅ 单步执行功能正常
- ✅ 错误能正确捕获和显示

---

### M2: 选择器拾取 & 录制（2-3 天）

**目标**：实现选择器拾取和操作录制功能

#### 任务清单

1. **实现选择器拾取器** ⏳
   - 注入 JS 覆盖层到 WebView2
   - 鼠标悬停高亮元素
   - 点击生成稳健选择器
   - 文件：`Assets/Scripts/selector-picker.js`、`Services/SelectorPickerService.cs`

2. **选择器生成策略** ⏳
   - 优先级：ID > data-testid > unique class > CSS path
   - 支持多种类型：CSS、XPath、Text、Placeholder
   - 稳健性评分
   - 文件：`Services/SelectorGeneratorService.cs`

3. **实现录制模式** ⏳
   - 注入事件监听脚本
   - 捕获：click、input、navigate、submit
   - postMessage 通信到 .NET
   - 文件：`Assets/Scripts/recorder.js`、`Services/RecorderService.cs`

4. **操作转 DSL 逻辑** ⏳
   - 将录制的操作序列转换为 YAML
   - 智能合并相似操作
   - 去重和优化
   - 文件：`Services/RecorderService.cs`

5. **UI 集成** ⏳
   - "拾取选择器"按钮和模式切换
   - "录制"按钮和状态指示
   - 选择器插入到 YAML 编辑器
   - 文件：`Views/AIDebugWorkbench.xaml(.cs)`

#### 验收标准
- ✅ 点击"拾取"后，鼠标悬停元素高亮
- ✅ 点击元素后生成选择器并插入 YAML
- ✅ 点击"录制"后，用户操作被捕获
- ✅ 停止录制后生成完整 DSL 脚本
- ✅ 生成的脚本可以成功执行

---

### M3: AI 辅助回路（2-3 天）

**目标**：实现 AI 观察浏览器并提供修复建议

#### 任务清单

1. **构建 DebugContext** ⏳
   - 收集错误信息、日志、截图、DOM 摘要
   - 数据脱敏（密码、token）
   - 大小限制和压缩
   - 文件：`Models/DebugContext.cs`、`Services/AIDebuggerService.cs`

2. **AI 调试提示词模板** ⏳
   - 设计专门的调试提示词
   - 包含上下文、错误、页面状态
   - 要求结构化 JSON 输出
   - 文件：`Services/AIDebuggerService.cs`

3. **AI 建议解析** ⏳
   - 解析 AI 返回的 JSON
   - 提取：诊断、建议、补丁、选择器候选
   - 验证和安全检查
   - 文件：`Models/AIDebugSuggestion.cs`、`Services/AIDebuggerService.cs`

4. **YAML 补丁应用** ⏳
   - 实现 YAML 补丁逻辑
   - 支持：替换步骤、插入步骤、修改选择器
   - 保持格式和注释
   - 文件：`Services/YamlPatchService.cs`

5. **UI 集成** ⏳
   - 右侧 AI 面板显示建议
   - "应用补丁"按钮
   - "重新生成"按钮
   - 上下文卡片（截图、日志、错误）
   - 文件：`Views/Controls/AIChatPanelControl.xaml(.cs)`

#### 验收标准
- ✅ 脚本失败后自动收集上下文
- ✅ AI 能看到截图和 DOM 信息
- ✅ AI 返回有效的修复建议
- ✅ 用户可以一键应用补丁
- ✅ 应用后脚本能成功执行

---

### M4: 增强功能（1-2 天）

**目标**：完善调试体验和辅助功能

#### 任务清单

1. **YAML 语法高亮** ⏳
   - 集成 AvalonEdit 或自定义高亮
   - 关键字、字符串、数字着色
   - 文件：`Views/Controls/YamlEditorControl.xaml(.cs)`

2. **实时校验和诊断** ⏳
   - 编辑时调用 DslParser 校验
   - 错误/警告下划线标记
   - 悬停显示错误详情
   - 文件：`Views/Controls/YamlEditorControl.xaml.cs`

3. **网络/控制台日志面板** ⏳
   - 订阅 WebView2 事件
   - 显示 HTTP 请求/响应
   - 显示 console.log 输出
   - 文件：`Views/Controls/LogPanelControl.xaml(.cs)`

4. **执行时间线** ⏳
   - 显示每个步骤的执行时间
   - 可视化进度条
   - 失败步骤标红
   - 文件：`Views/Controls/TimelineControl.xaml(.cs)`

5. **断点管理** ⏳
   - 在 YAML 编辑器中设置断点
   - 执行到断点时暂停
   - 继续/跳过/停止
   - 文件：`Views/Controls/YamlEditorControl.xaml.cs`

#### 验收标准
- ✅ YAML 编辑器有语法高亮
- ✅ 输入错误时实时显示提示
- ✅ 可以查看网络请求和控制台日志
- ✅ 时间线清晰显示执行进度
- ✅ 断点功能正常工作

---

## 🗓️ 时间规划

### 第 1 周（M1 + M2 部分）
- **Day 1-2**：M1 基础架构
  - IBrowserController 接口
  - WebView2Controller 实现
  - DslExecutor 重构
- **Day 3-4**：M1 UI + M2 开始
  - AIDebugWorkbench 视图
  - AITaskView 入口
  - 选择器拾取器原型
- **Day 5**：M2 继续
  - 选择器生成策略
  - 录制模式基础

### 第 2 周（M2 完成 + M3）
- **Day 6-7**：M2 完成
  - 录制转 DSL
  - UI 集成和测试
- **Day 8-9**：M3 AI 回路
  - DebugContext 构建
  - AI 提示词和解析
- **Day 10**：M3 完成
  - YAML 补丁应用
  - UI 集成

### 第 3 周（M4 + 测试优化）
- **Day 11-12**：M4 增强功能
  - YAML 高亮和校验
  - 日志面板
- **Day 13-14**：测试和优化
  - 端到端测试
  - 性能优化
  - Bug 修复
- **Day 15**：文档和发布
  - 用户文档
  - 演示视频
  - 发布准备

---

## 📊 当前进度

### 已完成 ✅
- 设计文档编写
- 技术方案确定
- 架构设计完成

### 进行中 ⏳
- M1: 基础可视化调试
  - 即将开始编码

### 待开始 📋
- M2: 选择器拾取 & 录制
- M3: AI 辅助回路
- M4: 增强功能

---

## 🎯 下一步行动

### 立即开始（今天）
1. ✅ 在 AITaskView 添加"AI 脚本助手"按钮
2. ⏳ 创建 IBrowserController 接口
3. ⏳ 创建 AIDebugWorkbench 基础视图
4. ⏳ 安装 WebView2 NuGet 包

### 明天
1. 实现 WebView2Controller 基础功能
2. 重构 DslExecutor 支持接口注入
3. 完成三栏布局 UI

### 本周内
1. 完成 M1 所有功能
2. 开始 M2 选择器拾取器

---

## 🔧 技术准备

### NuGet 包依赖
```xml
<!-- WebView2 -->
<PackageReference Include="Microsoft.Web.WebView2" Version="1.0.2210.55" />

<!-- YAML 编辑器（可选） -->
<PackageReference Include="AvalonEdit" Version="6.3.0" />

<!-- JSON 解析（已有） -->
<PackageReference Include="System.Text.Json" Version="9.0.0" />
```

### 项目结构调整
```
WebScraperApp/
├── Views/
│   ├── AIDebugWorkbench.xaml          [新建]
│   ├── AIDebugWorkbench.xaml.cs       [新建]
│   └── Controls/
│       ├── YamlEditorControl.xaml     [新建]
│       ├── BrowserPanelControl.xaml   [新建]
│       └── AIChatPanelControl.xaml    [新建]
├── Services/
│   ├── IBrowserController.cs          [新建]
│   ├── WebView2Controller.cs          [新建]
│   ├── AIDebuggerService.cs           [新建]
│   ├── RecorderService.cs             [新建]
│   ├── SelectorPickerService.cs       [新建]
│   └── DslExecutor.cs                 [修改]
├── Models/
│   ├── DebugContext.cs                [新建]
│   ├── AIDebugSuggestion.cs           [新建]
│   └── RecordedAction.cs              [新建]
└── Assets/
    └── Scripts/
        ├── selector-picker.js         [新建]
        ├── recorder.js                [新建]
        └── element-highlighter.js     [新建]
```

---

## 📝 注意事项

### 兼容性
- 保持现有 PlaywrightController 功能不变
- DslExecutor 支持两种 Controller
- 新功能不影响现有任务执行

### 性能
- WebView2 初始化可能较慢，需异步处理
- 截图和 DOM 获取要限制大小
- 录制模式下避免过多事件

### 安全
- 脱敏敏感数据（密码、token）
- AI 上下文大小限制
- 用户确认后才应用 AI 建议

### 用户体验
- 加载状态提示
- 错误友好提示
- 操作可撤销
- 快捷键支持

---

**状态**：路线图已完成，准备开始编码
**下一步**：在 AITaskView 添加"AI 脚本助手"按钮
