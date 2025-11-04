# Stagehand AI 自动化框架 - 本地部署实现

## 📋 项目概述

Stagehand 是一个基于 AI 的浏览器自动化框架，结合自然语言和代码控制浏览器。本实现提供了完整的本地部署管理功能。

## 🏗️ 架构设计

### 核心层（FishBrowser.Core）

#### 1. **StagehandMaintenanceService.cs**
- 位置：`web/FishBrowser.Core/Services/`
- 功能：
  - ✅ 检查 Node.js 和 npm 环境
  - ✅ 安装/更新/卸载 Stagehand
  - ✅ 检查版本信息
  - ✅ 测试 Stagehand 功能
  - ✅ 跨平台支持（Windows/Linux/macOS）

#### 2. **StagehandStatus.cs**
- 位置：`web/FishBrowser.Core/Models/`
- 数据模型：
  ```csharp
  - IsNodeInstalled: bool
  - NodeVersion: string
  - NpmVersion: string
  - IsInstalled: bool
  - InstallPath: string
  - InstalledVersion: string
  - LatestVersion: string
  - PlaywrightInstalled: bool
  - VersionDisplay: string
  - HasUpdate: bool
  ```

### API 层（FishBrowser.Api）

#### 1. **SystemController.cs**
- 新增端点：
  ```
  GET  /api/system/stagehand/status      - 获取状态
  POST /api/system/stagehand/install     - 安装
  POST /api/system/stagehand/update      - 更新
  POST /api/system/stagehand/uninstall   - 卸载
  POST /api/system/stagehand/test        - 测试
  ```

#### 2. **StagehandStatusDto.cs**
- 位置：`web/FishBrowser.Api/DTOs/`
- API 数据传输对象

### Web 层（FishBrowser.Web）

#### 1. **StagehandController.cs**
- 位置：`web/FishBrowser.Web/Controllers/`
- Web 控制器，代理 API 调用

#### 2. **Index.cshtml**
- 位置：`web/FishBrowser.Web/Views/Stagehand/`
- 管理界面，包含：
  - 📊 状态显示（Node.js、npm、Stagehand）
  - 🔧 操作按钮（安装、更新、测试、卸载）
  - 📖 帮助文档
  - 💡 使用示例

#### 3. **菜单集成**
- 位置：`_LayoutAdmin.cshtml`
- 菜单路径：系统设置 → Stagehand AI 框架

## 🔧 核心功能

### 1. 状态检查
```csharp
public async Task<StagehandStatus> GetStatusAsync()
{
    // 检查 Node.js 环境
    // 检查 npm 版本
    // 检查 Stagehand 安装状态
    // 检查 Playwright 依赖
    // 获取最新版本信息
}
```

### 2. 安装流程
```bash
# 1. 检查 Node.js
node --version

# 2. 安装 Stagehand 全局包
npm install -g @browserbasehq/stagehand

# 3. 安装 Playwright 浏览器
npx playwright install
```

### 3. 更新流程
```bash
# 1. 更新 Stagehand
npm update -g @browserbasehq/stagehand

# 2. 更新 Playwright 浏览器
npx playwright install
```

### 4. 测试功能
创建临时测试脚本，验证 Stagehand 是否正常工作：
```javascript
const { Stagehand } = require('@browserbasehq/stagehand');

const stagehand = new Stagehand({ env: 'LOCAL' });
await stagehand.init();
console.log('Stagehand test successful');
await stagehand.close();
```

## 📦 依赖注册

### Core 项目
```csharp
// ServiceCollectionExtensions.cs
services.AddScoped<StagehandMaintenanceService>();
```

### API 项目
```csharp
// Program.cs
builder.Services.AddScoped<StagehandMaintenanceService>();
```

## 🎨 界面特点

### 状态显示
- **Node.js 环境**：显示 Node.js 和 npm 版本
- **Stagehand 状态**：已安装/未安装，版本信息
- **详细信息**：安装路径、版本对比、依赖状态

### 操作按钮
- **安装**：首次安装 Stagehand
- **更新**：更新到最新版本
- **测试**：验证功能是否正常
- **卸载**：完全移除 Stagehand

### 帮助文档
- Stagehand 简介
- 主要特点
- 系统要求
- 使用示例
- 官方文档链接

## 🔄 与 Playwright 的对比

| 特性 | Playwright | Stagehand |
|------|-----------|-----------|
| 语言 | C# (.NET) | JavaScript (Node.js) |
| 安装方式 | dotnet tool | npm global |
| 浏览器 | 自带浏览器 | 依赖 Playwright |
| 控制方式 | 代码 API | AI + 代码 |
| 适用场景 | 精确控制 | 灵活自动化 |

## 🚀 使用流程

### 1. 安装 Node.js
- 访问 https://nodejs.org/
- 下载并安装 v18 或更高版本

### 2. 安装 Stagehand
- 打开 Web 管理界面
- 导航到：系统设置 → Stagehand AI 框架
- 点击「安装 Stagehand」按钮
- 等待安装完成（可能需要几分钟）

### 3. 测试连接
- 点击「测试连接」按钮
- 验证 Stagehand 是否正常工作

### 4. 使用示例
```javascript
const { Stagehand } = require('@browserbasehq/stagehand');

// 初始化
const stagehand = new Stagehand({
    env: 'LOCAL',
    verbose: 0,
    debugDom: false
});
await stagehand.init();

// 导航到页面
const page = stagehand.context.pages()[0];
await page.goto("https://example.com");

// 使用 AI 执行操作
await stagehand.act("点击登录按钮");

// 提取数据
const data = await stagehand.extract(
    "提取文章标题和作者",
    z.object({
        title: z.string(),
        author: z.string()
    })
);

// 关闭
await stagehand.close();
```

## 🛠️ 技术细节

### 跨平台路径处理
```csharp
private static string GetStagehandGlobalPath()
{
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
        // Windows: %APPDATA%\npm\node_modules\@browserbasehq\stagehand
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "npm", "node_modules", "@browserbasehq", "stagehand");
    }
    else
    {
        // Linux/macOS: /usr/local/lib/node_modules/@browserbasehq/stagehand
        return "/usr/local/lib/node_modules/@browserbasehq/stagehand";
    }
}
```

### 进程执行封装
```csharp
private async Task RunProcessAsync(string fileName, string arguments)
{
    var psi = new ProcessStartInfo
    {
        FileName = fileName,
        Arguments = arguments,
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        CreateNoWindow = true
    };
    
    using var process = Process.Start(psi);
    // 处理输出和错误
    // 记录日志
    // 检查退出代码
}
```

## 📝 注意事项

1. **Node.js 版本**：需要 v18 或更高版本
2. **网络连接**：安装时需要访问 npm 仓库
3. **磁盘空间**：Playwright 浏览器需要约 500MB 空间
4. **权限要求**：全局安装需要管理员权限（Windows）或 sudo（Linux）
5. **防火墙**：确保允许 Node.js 和 npm 访问网络

## 🔗 相关链接

- **Stagehand 官方文档**：https://docs.stagehand.dev
- **GitHub 仓库**：https://github.com/browserbase/stagehand
- **Node.js 下载**：https://nodejs.org/
- **Playwright 文档**：https://playwright.dev/

## 📊 项目文件清单

### Core 项目
- ✅ `Services/StagehandMaintenanceService.cs` - 核心服务
- ✅ `Models/StagehandStatus.cs` - 状态模型
- ✅ `Infrastructure/Configuration/ServiceCollectionExtensions.cs` - DI 注册

### API 项目
- ✅ `Controllers/SystemController.cs` - API 端点
- ✅ `DTOs/StagehandStatusDto.cs` - DTO
- ✅ `Program.cs` - 服务注册

### Web 项目
- ✅ `Controllers/StagehandController.cs` - Web 控制器
- ✅ `Views/Stagehand/Index.cshtml` - 管理界面
- ✅ `Views/Shared/_LayoutAdmin.cshtml` - 菜单集成

## ✅ 完成状态

- [x] 核心服务实现
- [x] API 端点实现
- [x] Web 界面实现
- [x] 菜单集成
- [x] 依赖注册
- [x] 文档编写
- [x] 构建测试通过

## 🎯 下一步

1. **WPF 集成**：在 WPF 应用中使用 `StagehandMaintenanceService`
2. **功能增强**：添加配置管理、日志查看等
3. **示例代码**：提供更多使用示例
4. **性能优化**：优化安装和更新流程

---

**实现完成时间**：2025-11-04
**版本**：v1.0
**作者**：Cascade AI
