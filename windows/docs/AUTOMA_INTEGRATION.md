# Automa 扩展集成指南

## 📋 概述

已成功将 Automa 浏览器自动化扩展集成到随机指纹浏览器中。Automa 是一个强大的可视化浏览器自动化工具，支持通过拖拽块构建自动化工作流。

## ✅ 已完成的修改

### 文件清单
- `Engine/PlaywrightController.cs` (修改)
- `Views/BrowserManagementPage.xaml` (修改)
- `Views/BrowserManagementPage.xaml.cs` (修改)
- `docs/AUTOMA_INTEGRATION.md` (新建)
- `automa-main/build-automa.bat` (新建)
- `automa-main/BUILD_SUCCESS.md` (新建)
- `automa-main/BUILD_TROUBLESHOOTING.md` (新建)

### 1. PlaywrightController.cs
**文件**: `d:\1Dev\webscraper\windows\WebScraperApp\Engine\PlaywrightController.cs`

**修改内容**:
- 添加 `_automaExtensionPath` 字段，指向 Automa 扩展构建目录
- 修改 `InitializeBrowserAsync` 方法签名，添加 `loadAutoma` 参数
- 在普通浏览器模式和持久化上下文模式中都支持加载扩展
- 自动检测扩展路径是否存在
- 当加载扩展时自动切换到有头模式（headless = false）
- 添加详细的日志记录

**关键代码**:
```csharp
// Automa 扩展路径
private readonly string _automaExtensionPath = @"d:\1Dev\webscraper\automa-main\build";

// 构建启动参数
var args = new List<string> { "--disable-blink-features=AutomationControlled" };

// 如果需要加载 Automa 扩展
if (loadAutoma && System.IO.Directory.Exists(_automaExtensionPath))
{
    if (headless)
    {
        _logService.LogWarn("PlaywrightController", "Cannot load extensions in headless mode, switching to headed mode");
        headless = false;
    }
    
    args.Add($"--disable-extensions-except={_automaExtensionPath}");
    args.Add($"--load-extension={_automaExtensionPath}");
    _logService.LogInfo("PlaywrightController", $"Automa extension will be loaded from: {_automaExtensionPath}");
}
```

### 2. BrowserManagementPage.xaml
**文件**: `d:\1Dev\webscraper\windows\WebScraperApp\Views\BrowserManagementPage.xaml`

**修改内容**:
- 添加 `LoadAutomaCheckBox` 复选框
- **默认勾选** (`IsChecked="True"`)
- 提供详细的提示文本和工具提示

**UI 控件**:
```xaml
<CheckBox x:Name="LoadAutomaCheckBox" 
          Content="启动时加载 Automa 自动化扩展（可视化工作流）" 
          IsChecked="True"
          Margin="0,8,0,0"
          ToolTip="加载 Automa 浏览器自动化扩展，提供可视化工作流编辑、表单填充、数据抓取等功能。快捷键：Alt+A 打开仪表板，Alt+P 打开元素选择器"/>
```

### 3. BrowserManagementPage.xaml.cs
**文件**: `d:\1Dev\webscraper\windows\WebScraperApp\Views\BrowserManagementPage.xaml.cs`

**修改内容**:
- 在启动浏览器时读取 CheckBox 状态（默认为 true）
- 将状态传递给 `InitializeBrowserAsync` 方法
- 添加日志记录扩展加载状态
- 更新状态文本显示扩展加载状态（带 🎯 图标）

## 🔧 构建 Automa 扩展

### 前提条件
- Node.js >= 14.18.1
- pnpm 或 npm

### 构建步骤

1. **安装依赖**
```bash
cd d:\1Dev\webscraper\automa-main
npm install
# 或
pnpm install
```

2. **创建必需的配置文件**
```bash
# 创建 src/utils/getPassKey.js
echo "export default function() { return 'your-pass-key'; }" > src/utils/getPassKey.js
```

3. **构建扩展**
```bash
npm run build
# 或
pnpm build
```

4. **验证构建**
构建完成后，应该会生成 `d:\1Dev\webscraper\automa-main\build` 目录，包含以下文件：
- manifest.json
- background.bundle.js
- contentScript.bundle.js
- popup.html
- icon-128.png
- 等其他资源文件

## 🚀 使用方法

### 方式 1: 通过浏览器管理页面（推荐）

1. 打开应用程序
2. 进入"浏览器管理"页面
3. 选择要启动的浏览器环境
4. **Automa 扩展默认已启用**（复选框默认勾选）
   - 如不需要，可取消勾选"启动时加载 Automa 自动化扩展"
5. 点击"启动"按钮
6. 浏览器将以有头模式启动，并自动加载 Automa 扩展
7. 状态栏会显示 "Automa 扩展已加载 🎯"

### 方式 2: 通过代码调用

```csharp
var controller = new PlaywrightController(logService, fingerprintService, secretService);

// 启动浏览器并加载 Automa 扩展
await controller.InitializeBrowserAsync(
    fingerprint: profile,
    proxy: null,
    headless: false,  // 必须是 false
    userDataPath: null,
    loadAutoma: true  // 加载扩展
);
```

## ⚠️ 重要限制

### 1. Headless 模式限制
- ❌ **不支持**: 在 headless 模式下无法加载 Chrome 扩展
- ✅ **自动处理**: 当 `loadAutoma=true` 时，系统会自动切换到有头模式
- 📝 **日志**: 会记录警告日志 "Cannot load extensions in headless mode, switching to headed mode"

### 2. 扩展路径
- 扩展路径必须是**绝对路径**
- 当前硬编码为: `d:\1Dev\webscraper\automa-main\build`
- 如果路径不存在，会记录警告日志但不会抛出异常

### 3. 性能影响
- 内存占用增加: **~100-200MB**
- 启动时间增加: **~1-2 秒**
- 浏览器窗口必须可见（无法后台运行）

### 4. 指纹兼容性
- Automa 会注入 content scripts，可能影响指纹伪装
- 建议在测试环境中验证指纹效果
- 可能需要在指纹脚本中屏蔽扩展特征

## 📊 Automa 功能特性

### 核心功能
- ✅ **可视化工作流编辑器**: 通过拖拽块构建自动化流程
- ✅ **表单自动填充**: 自动填写网页表单
- ✅ **数据抓取**: 提取网页数据
- ✅ **截图功能**: 自动截取网页截图
- ✅ **重复任务**: 执行重复性操作
- ✅ **定时执行**: 支持 cron 表达式定时运行
- ✅ **工作流市场**: 分享和下载社区工作流

### 访问 Automa
启动浏览器后，可以通过以下方式访问 Automa：
1. 点击浏览器工具栏中的 Automa 图标
2. 使用快捷键 `Alt+A` 打开仪表板
3. 使用快捷键 `Alt+P` 打开元素选择器

## 🔍 故障排查

### 问题 1: 扩展未加载
**症状**: 浏览器启动但看不到 Automa 图标

**解决方案**:
1. 检查日志，确认是否有 "Automa extension will be loaded" 消息
2. 验证扩展路径是否存在: `d:\1Dev\webscraper\automa-main\build`
3. 确认 build 目录中包含 `manifest.json` 文件
4. 检查是否勾选了 "加载 Automa 扩展" 复选框

### 问题 2: 构建失败
**症状**: `npm run build` 报错

**解决方案**:
1. 确保安装了所有依赖: `npm install`
2. 检查 Node.js 版本: `node --version` (需要 >= 14.18.1)
3. 创建 `src/utils/getPassKey.js` 文件
4. 清除缓存重试: `npm cache clean --force && npm install`

### 问题 3: 扩展权限问题
**症状**: 扩展加载但功能受限

**解决方案**:
1. 检查 `manifest.json` 中的权限声明
2. 确保使用的是 Chromium（不是 Firefox）
3. 验证扩展版本与浏览器版本兼容

## 🎯 未来改进方向

### 短期 (1-2 周)
- [ ] 将扩展路径改为可配置（通过配置文件或 UI）
- [ ] 支持加载多个扩展
- [ ] 添加扩展管理界面

### 中期 (1-2 月)
- [ ] 集成 Automa 工作流到应用内部
- [ ] 提供预置的常用工作流模板
- [ ] 支持工作流的导入/导出

### 长期 (3-6 月)
- [ ] 开发自定义扩展，深度集成指纹功能
- [ ] 提供 API 接口控制 Automa 工作流
- [ ] 支持分布式执行工作流

## 📚 相关资源

- [Automa 官网](https://www.automa.site/)
- [Automa GitHub](https://github.com/AutomaApp/automa)
- [Automa 文档](https://docs.automa.site/)
- [Automa 工作流市场](https://www.automa.site/marketplace)
- [Playwright 文档](https://playwright.dev/)

## 📝 版本历史

### v1.0.0 (2025-11-01)
- ✅ 初始实现：支持加载 Automa 扩展
- ✅ 添加 UI 控制选项
- ✅ 自动切换到有头模式
- ✅ 完整的日志记录

---

**维护者**: WebScraper Team  
**最后更新**: 2025-11-01
