# 快速启动指南

## 第一步：环境检查

确保已安装以下软件：
- .NET 9 SDK: `dotnet --version` 应输出 `9.0.x` 或更高
- WebView2 Runtime: [下载](https://developer.microsoft.com/en-us/microsoft-edge/webview2/)

## 第二步：项目初始化

```powershell
cd d:\1Dev\webscraper\windows\WebScraperApp

# 恢复 NuGet 包
dotnet restore

# 安装 Playwright 浏览器（首次运行需要，耗时 2-5 分钟）
pwsh bin/Debug/net9.0/playwright.ps1 install
```

## 第三步：运行应用

```powershell
dotnet run
```

应用启动后，你会看到：
- 主窗口显示导航菜单（左侧）
- 仪表盘页面（中间）
- 实时日志面板（底部）

## 第四步：测试采集流程

### 方式 1：通过 UI（后续版本）
当前版本 UI 为框架，任务管理功能待实现。

### 方式 2：通过代码测试（开发者）

在 `Program.cs` 的 `Main` 方法中添加测试代码：

```csharp
// 初始化数据库后，添加以下代码
using (var scope = host.Services.CreateScope())
{
    var dbService = scope.ServiceProvider.GetRequiredService<DatabaseService>();
    var scraperService = scope.ServiceProvider.GetRequiredService<ScraperService>();
    var logService = scope.ServiceProvider.GetRequiredService<LogService>();

    // 创建指纹配置
    var fingerprint = dbService.CreateFingerprintProfile(
        name: "Test Fingerprint",
        userAgent: "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36",
        locale: "zh-CN",
        timezone: "Asia/Shanghai"
    );

    // 创建采集任务
    var task = dbService.CreateTask(
        url: "https://example.com",
        fingerprintProfileId: fingerprint.Id
    );

    logService.LogInfo("Test", $"Created task {task.Id}");

    // 执行采集（异步）
    // await scraperService.ExecuteTaskAsync(task);
}
```

## 常见问题

### Q: Playwright 安装失败
**A**: 
```powershell
# 清除缓存
rm -r ~/.cache/ms-playwright

# 重新安装
pwsh bin/Debug/net9.0/playwright.ps1 install
```

### Q: 数据库文件在哪里？
**A**: `webscraper.db` 在项目根目录（`bin/Debug/net9.0/` 下）。

### Q: 如何查看日志？
**A**: 
- UI 底部日志面板实时显示
- 或查看数据库中的 `LogEntries` 表

### Q: 如何修改配置？
**A**: 编辑 `appsettings.json`，修改后重启应用。

## 下一步

1. **实现 UI 功能**（M2）
   - 任务创建与管理
   - 指纹模板编辑
   - 代理池管理

2. **集成 AI 模块**（M3）
   - 接入 OpenAI API
   - 实现摘要与分类

3. **性能优化**（M4）
   - 内存管理
   - 数据库优化
   - 并发控制

## 文档

- 详细架构：`../../docs/architecture.md`
- 指纹策略：`../../docs/fingerprints.md`
- 开发手册：`../../docs/setup.md`
