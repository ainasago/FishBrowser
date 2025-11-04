# 视口显示问题修复指南

## 🔍 问题描述
浏览器启动后，右侧约一半的内容看不到，页面显示不完整。

## 🎯 已实施的修复

### 修复 1：移除强制 DPI 缩放 ✅
**问题**：强制 DPI 缩放导致实际显示区域缩小
**解决**：移除 `--force-device-scale-factor` 和 `DeviceScaleFactor` 设置

### 修复 2：添加窗口大小参数 ✅
**问题**：浏览器窗口太小，无法容纳完整视口
**解决**：添加 `--window-size` 参数，窗口大小 = 视口大小 + UI 空间

```csharp
int windowWidth = viewportWidth + 16;   // 边框
int windowHeight = viewportHeight + 110; // 标题栏、地址栏等
args.Add($"--window-size={windowWidth},{windowHeight}");
```

## 🧪 测试步骤

### 1. 编译项目
```bash
cd d:\1Dev\webscraper\windows
dotnet build
```

### 2. 启动浏览器并检查日志
查找以下日志：
```
System DPI scale detected: 125% (not forcing, using system default)
Setting window size: 1382x878 (viewport: 1366x768)
Using custom viewport width from environment: 1366
Using custom viewport height from environment: 768
```

### 3. 在浏览器中验证
打开开发者工具（F12），在 Console 中运行：
```javascript
console.log('Window inner:', window.innerWidth, 'x', window.innerHeight);
console.log('Window outer:', window.outerWidth, 'x', window.outerHeight);
console.log('Screen:', screen.width, 'x', screen.height);
console.log('Available:', screen.availWidth, 'x', screen.availHeight);
console.log('DPR:', window.devicePixelRatio);
```

**预期结果**：
- `window.innerWidth` 应该等于或接近设置的视口宽度（如 1366）
- `window.outerWidth` 应该等于窗口大小（如 1382）

### 4. 视觉检查
- 访问宽屏网站（如 https://www.google.com）
- 页面应该完全可见，无横向滚动条
- 右侧内容应该完整显示

## 🔧 如果问题仍然存在

### 方案 A：增大窗口大小偏移量
如果浏览器 UI 占用空间更大，可以增加偏移量：

```csharp
int windowWidth = viewportWidth + 32;   // 增加到 32
int windowHeight = viewportHeight + 150; // 增加到 150
```

### 方案 B：禁用 Viewport 设置
让浏览器使用窗口的完整大小：

```csharp
// 注释掉 ViewportSize 设置
// ViewportSize = new ViewportSize { Width = viewportWidth, Height = viewportHeight }
```

### 方案 C：使用 --start-maximized
让浏览器启动时最大化：

```csharp
args.Add("--start-maximized");
```

### 方案 D：禁用浏览器 UI
减少浏览器 UI 占用的空间：

```csharp
args.Add("--app=about:blank");  // 应用模式，无地址栏
```

## 📊 常见 DPI 和窗口大小对照表

| 视口大小 | DPI | 窗口大小（推荐） |
|---------|-----|----------------|
| 1366x768 | 100% | 1382x878 |
| 1366x768 | 125% | 1382x878 |
| 1366x768 | 150% | 1382x878 |
| 1920x1080 | 100% | 1936x1190 |
| 1920x1080 | 125% | 1936x1190 |
| 1920x1080 | 150% | 1936x1190 |

## 🐛 调试技巧

### 1. 检查实际窗口大小
在浏览器中运行：
```javascript
console.log('Viewport:', window.innerWidth, 'x', window.innerHeight);
console.log('Window:', window.outerWidth, 'x', window.outerHeight);
console.log('Diff:', window.outerWidth - window.innerWidth, 'x', window.outerHeight - window.innerHeight);
```

### 2. 检查是否有缩放
```javascript
console.log('Zoom level:', window.devicePixelRatio);
console.log('Document zoom:', document.documentElement.style.zoom || '1');
```

### 3. 检查 CSS 缩放
```javascript
console.log('Body transform:', window.getComputedStyle(document.body).transform);
```

## 📝 相关文件

- `Engine/PlaywrightController.cs` - 浏览器启动逻辑
- `Models/BrowserEnvironment.cs` - 自定义分辨率字段
- `Views/NewBrowserEnvironmentWindow.xaml.cs` - 分辨率 UI

## 🎯 下一步

如果以上所有方案都无效，问题可能是：
1. **系统级缩放**：Windows 显示设置中的缩放影响
2. **显示器分辨率**：物理显示器分辨率限制
3. **Playwright 限制**：Playwright 本身的限制

请提供以下信息以进一步诊断：
- 系统 DPI 设置（Windows 设置 → 显示 → 缩放）
- 显示器物理分辨率
- 浏览器控制台中的 `window.innerWidth` 值
- 日志中的窗口大小设置
