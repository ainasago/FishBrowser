# 指纹浏览器采集系统

基于 .NET 9 + WPF 的指纹浏览器采集系统，支持多实例并行采集、AI 分析、代理轮换等功能。

## 快速开始

### 前置条件
- .NET 9 SDK 或更高版本
- Windows 10 (Build 19041) 或更高版本
- WebView2 Runtime

### 安装与运行

1. **克隆或进入项目目录**
```bash
cd d:\1Dev\webscraper\windows\WebScraperApp
```

2. **恢复 NuGet 包**
```bash
dotnet restore
```

3. **安装 Playwright 浏览器**
```bash
pwsh bin/Debug/net9.0/playwright.ps1 install
```

4. **运行应用**
```bash
dotnet run
```

## 项目结构

```
WebScraperApp/
├── Models/                    # 数据模型
├── Data/                      # 数据访问层（EF Core）
├── Services/                  # 业务逻辑层
├── Engine/                    # 核心引擎（Playwright、解析、指纹）
├── Views/                     # WPF 视图
├── ViewModels/                # MVVM ViewModel
├── Config/                    # 配置文件
├── Program.cs                 # 应用入口
├── App.xaml                   # 应用定义
└── MainWindow.xaml            # 主窗口
```

## 核心功能

### 1. 指纹浏览器控制
- 使用 Playwright 驱动 Chromium
- 支持多维指纹注入（UA、Locale、Timezone、Canvas、WebGL 等）
- 支持代理轮换与会话隔离

### 2. 采集与解析
- 页面加载与网络等待
- HTML 解析与正文抽取（AngleSharp）
- 元数据提取（标题、作者、发布时间）

### 3. 任务管理
- 任务队列与并发控制
- 自动重试与速率限制
- 任务状态监控

### 4. AI 分析（占位实现）
- 文本摘要生成
- 文本分类
- 关键词提取

### 5. 数据存储
- SQLite 本地数据库（开发）
- EF Core ORM
- 支持迁移到 PostgreSQL

### 6. 日志与监控
- 实时日志显示
- 多级别日志记录
- 事件追踪

## 配置说明

编辑 `appsettings.json` 配置系统参数：

```json
{
  "Database": {
    "ConnectionString": "Data Source=webscraper.db"
  },
  "Scraper": {
    "MaxConcurrency": 5,
    "TimeoutSeconds": 30,
    "RetryCount": 3
  },
  "AI": {
    "ApiKey": "your-api-key",
    "Model": "gpt-3.5-turbo"
  }
}
```

## 常见问题

### Q: 如何添加代理？
A: 在 ProxyPoolView 中添加代理服务器信息，系统会自动进行健康检查。

### Q: 如何自定义指纹？
A: 在 FingerprintConfigView 中创建新指纹模板，或使用预设模板。

### Q: 如何接入 AI API？
A: 在 `AIService.cs` 中实现真实的 API 调用，替换占位实现。

## 开发指南

### 添加新的采集任务
```csharp
var taskService = serviceProvider.GetRequiredService<TaskService>();
var task = taskService.CreateTask(
    url: "https://example.com/article",
    fingerprintProfileId: 1,
    proxyServerId: 1
);
```

### 执行采集
```csharp
var scraperService = serviceProvider.GetRequiredService<ScraperService>();
await scraperService.ExecuteTaskAsync(task);
```

### 查询结果
```csharp
var dbService = serviceProvider.GetRequiredService<DatabaseService>();
var articles = dbService.GetRecentArticles(limit: 50);
```

## 里程碑

- **M1**: 可运行原型（✅ 完成）
- **M2**: 指纹与代理管理（⏳ 进行中）
- **M3**: AI 集成与结果展示（⏳ 待开始）
- **M4**: 稳定性与监控（⏳ 待开始）
- **M5**: 文档与验收（⏳ 待开始）

## 文档

详见 `../../docs/` 目录：
- `requirements.md`: 需求规格说明书
- `architecture.md`: 技术架构文档
- `ui-design.md`: UI/UX 设计说明
- `fingerprints.md`: 指纹与反爬策略
- `setup.md`: 开发环境与运行手册
- `milestones.md`: 里程碑与验收清单

## 许可证

MIT

## 联系方式

如有问题或建议，请提交 Issue 或 PR。
