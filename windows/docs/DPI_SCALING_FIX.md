# DPI 缩放修复指南

## 问题描述

启动的浏览器画面向右偏移，导致最右边的元素无法正常显示。这是由于 Playwright 未正确应用系统 DPI 缩放设置导致的。

## 解决方案

已在 `PlaywrightController.cs` 中实现自动系统 DPI 检测和应用。

## 📝 实现细节

### 1. 系统 DPI 检测

**方法**: `GetSystemDpiScale()`

从 Windows 注册表读取系统 DPI 设置：
```csharp
private static float GetSystemDpiScale()
{
    try
    {
        const string keyPath = @"Control Panel\Desktop";
        using var key = Registry.CurrentUser.OpenSubKey(keyPath);
        if (key != null)
        {
            object? logPixelsObj = key.GetValue("LogPixels");
            if (logPixelsObj != null && int.TryParse(logPixelsObj.ToString(), out int dpi))
            {
                // 96 DPI = 100% (1.0)
                float scale = dpi / 96f;
                return scale;
            }
        }
    }
    catch (Exception ex)
    {
        // 如果读取失败，使用默认值
    }

    return 1.0f; // 默认 100%
}
```

**DPI 映射关系**:
- 96 DPI = 100% (1.0)
- 120 DPI = 125% (1.25)
- 144 DPI = 150% (1.5)
- 192 DPI = 200% (2.0)

### 2. 浏览器启动时应用 DPI

**位置**: `InitializeBrowserAsync` 方法

```csharp
// 获取系统 DPI 缩放比例并添加到启动参数
float dpiScale = GetSystemDpiScale();
args.Add($"--force-device-scale-factor={dpiScale}");
_logService.LogInfo("PlaywrightController", $"System DPI scale detected: {dpiScale * 100}%");
```

### 3. 浏览器上下文中应用 DPI

**位置**: `BrowserNewContextOptions` 配置

```csharp
// 非移动端：使用系统 DPI 缩放
if (IsMobileUA(contextOptions.UserAgent))
{
    contextOptions.DeviceScaleFactor ??= 3; // 移动端
}
else
{
    contextOptions.DeviceScaleFactor ??= dpiScale; // 桌面端使用系统 DPI
}
```

### 4. 持久化上下文中应用 DPI

**位置**: `InitializePersistentContextAsync` 方法

```csharp
var launchOptions = new BrowserTypeLaunchPersistentContextOptions
{
    // ... 其他配置
    DeviceScaleFactor = dpiScale
};
```

## 🔍 验证方法

### 方式 1: 查看日志

启动浏览器后，查看日志中是否包含：
```
System DPI scale detected: 100%
System DPI scale detected: 125%
System DPI scale detected: 150%
```

### 方式 2: 在浏览器中检查

在浏览器控制台运行：
```javascript
console.log(window.devicePixelRatio);
```

应该显示与系统 DPI 设置相符的值。

### 方式 3: 视觉检查

- ✅ 页面内容应该完全显示，不再向右偏移
- ✅ 最右边的元素应该正常可见
- ✅ 页面布局应该与正常浏览器一致

## 📊 支持的 DPI 设置

| Windows 设置 | DPI | 缩放比例 | 状态 |
|-------------|-----|--------|------|
| 100% | 96 | 1.0 | ✅ 支持 |
| 125% | 120 | 1.25 | ✅ 支持 |
| 150% | 144 | 1.5 | ✅ 支持 |
| 175% | 168 | 1.75 | ✅ 支持 |
| 200% | 192 | 2.0 | ✅ 支持 |
| 225% | 216 | 2.25 | ✅ 支持 |
| 250% | 240 | 2.5 | ✅ 支持 |

## ⚙️ 修改的文件

**文件**: `d:\1Dev\webscraper\windows\WebScraperApp\Engine\PlaywrightController.cs`

**修改内容**:
1. 添加 `using Microsoft.Win32;` 导入
2. 添加 `GetSystemDpiScale()` 方法
3. 在 `InitializeBrowserAsync` 中应用 DPI
4. 在 `BrowserNewContextOptions` 中应用 DPI
5. 在 `InitializePersistentContextAsync` 中应用 DPI

## 🚀 使用方法

无需任何配置，系统会自动：
1. 检测当前系统 DPI 设置
2. 在浏览器启动时应用 DPI 参数
3. 在浏览器上下文中应用 DPI 缩放
4. 记录 DPI 检测信息到日志

## 🔧 故障排查

### 问题 1: 画面仍然偏移

**解决方案**:
1. 检查系统 DPI 设置是否正确
2. 查看日志中 DPI 检测值是否正确
3. 尝试重启应用程序

### 问题 2: 无法读取系统 DPI

**症状**: 日志中没有 DPI 检测信息

**解决方案**:
1. 检查是否有注册表访问权限
2. 确认 Windows 版本支持（Windows 7+）
3. 系统会自动回退到 1.0（100%）

### 问题 3: 某些 DPI 设置不支持

**症状**: 特定 DPI 设置下仍有问题

**解决方案**:
1. 尝试使用标准 DPI 设置（100%, 125%, 150%, 200%）
2. 检查 Chromium 版本是否最新
3. 报告问题并提供 DPI 值

## 📝 技术背景

### 为什么需要 DPI 缩放

Windows 系统允许用户设置显示缩放（DPI），以改善高分辨率显示器上的文本可读性。Chromium 浏览器需要知道这个缩放值，以正确渲染页面。

### 两个关键参数

1. **`--force-device-scale-factor`**: 告诉 Chromium 使用特定的缩放因子
2. **`DeviceScaleFactor`**: 在浏览器上下文中设置 `window.devicePixelRatio`

两个参数必须一致，才能正确显示页面。

## 🔗 相关资源

- [Playwright 文档 - DeviceScaleFactor](https://playwright.dev/dotnet/docs/api/class-browsernewcontextoptions#browser-new-context-options-device-scale-factor)
- [Chromium 参数 - force-device-scale-factor](https://chromium.googlesource.com/chromium/src/+/master/ui/gfx/switches.cc)
- [Windows DPI 设置](https://support.microsoft.com/en-us/windows/set-your-display-scaling-preference-in-windows-10-6c4dfdc0-2e59-0230-e1f6-5f1a76344b13)

## ✅ 验证清单

- [x] 添加 DPI 检测方法
- [x] 在浏览器启动时应用 DPI
- [x] 在浏览器上下文中应用 DPI
- [x] 在持久化上下文中应用 DPI
- [x] 添加日志记录
- [x] 处理异常情况
- [x] 支持所有标准 DPI 设置

## 📅 版本历史

### v1.0.0 (2025-11-01)
- ✅ 初始实现：自动系统 DPI 检测
- ✅ 支持所有标准 DPI 设置
- ✅ 完整的日志记录
- ✅ 异常处理和回退机制

---

**维护者**: WebScraper Team  
**最后更新**: 2025-11-01  
**状态**: ✅ 完成
