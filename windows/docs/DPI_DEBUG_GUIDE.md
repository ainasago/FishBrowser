# DPI 缩放调试指南

## 问题

浏览器显示 DPI 为 100%（1.0），但系统实际缩放不同。

## 改进的 DPI 检测方法

已更新 `PlaywrightController.cs` 使用**多层次 DPI 检测**：

### 方法优先级

1. **Windows API `GetDpiForSystem()`** ⭐ 最可靠
   - 直接调用 Windows 系统 API
   - 返回当前系统 DPI 值
   - 支持 Windows 7+

2. **注册表 `LogPixels`** 备选方案
   - 尝试多个注册表位置
   - CurrentUser 和 LocalMachine
   - 支持旧版 Windows

3. **默认值 1.0** 最后回退
   - 如果以上都失败
   - 使用 100% 缩放

## 检查你的系统 DPI

### 方法 1: 使用 PowerShell

```powershell
# 检查注册表中的 LogPixels
Get-ItemProperty -Path "HKCU:\Control Panel\Desktop" -Name "LogPixels"

# 输出示例:
# LogPixels : 96   (100%)
# LogPixels : 120  (125%)
# LogPixels : 144  (150%)
# LogPixels : 192  (200%)
```

### 方法 2: 使用 Windows 设置

1. 右键点击桌面
2. 选择"显示设置"
3. 查看"缩放与布局"部分
4. 显示的百分比对应的 DPI：
   - 100% = 96 DPI
   - 125% = 120 DPI
   - 150% = 144 DPI
   - 175% = 168 DPI
   - 200% = 192 DPI

### 方法 3: 在浏览器中检查

启动浏览器后，在浏览器控制台运行：

```javascript
// 检查浏览器检测到的 DPI
console.log("devicePixelRatio:", window.devicePixelRatio);

// 检查屏幕 DPI
console.log("Screen DPI:", window.screen.devicePixelRatio);

// 检查媒体查询
const dpi = window.devicePixelRatio;
console.log(`Detected DPI scale: ${(dpi * 100).toFixed(0)}%`);
```

## 日志中的 DPI 信息

启动浏览器后，查看日志：

```
[INF] [PlaywrightController] System DPI scale detected: 100%
[INF] [PlaywrightController] System DPI scale detected: 125%
[INF] [PlaywrightController] System DPI scale detected: 150%
```

## 故障排查

### 问题 1: DPI 仍然显示为 100%

**可能原因**:
- 系统 DPI 确实是 100%
- Windows API 调用失败
- 注册表值无法读取

**解决方案**:
1. 验证系统 DPI 设置（见上面的检查方法）
2. 检查是否有权限访问注册表
3. 尝试以管理员身份运行应用

### 问题 2: 浏览器显示不正确

**可能原因**:
- DPI 参数未正确传递给 Chromium
- Chromium 版本不支持该 DPI 值
- 浏览器缓存问题

**解决方案**:
1. 清除浏览器会话：`BrowserSessions` 目录
2. 重新启动应用
3. 检查 Chromium 版本是否最新

### 问题 3: 某些 DPI 值不工作

**支持的 DPI 值**:
- ✅ 96 (100%)
- ✅ 120 (125%)
- ✅ 144 (150%)
- ✅ 168 (175%)
- ✅ 192 (200%)
- ✅ 216 (225%)
- ✅ 240 (250%)

**不支持的 DPI 值**:
- ❌ 自定义 DPI（如 110, 130 等）
- ❌ 某些旧版 Chromium 版本

## 代码实现细节

### Windows API 调用

```csharp
[DllImport("user32.dll")]
private static extern int GetDpiForSystem();

// 使用
int systemDpi = GetDpiForSystem();  // 返回 96, 120, 144 等
float scale = systemDpi / 96f;      // 转换为缩放比例
```

### 注册表查询

```csharp
// 查询 CurrentUser
using var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop");
object? logPixelsObj = key?.GetValue("LogPixels");

// 查询 LocalMachine
using var localKey = Registry.LocalMachine.OpenSubKey(
    @"SYSTEM\CurrentControlSet\Hardware Profiles\Current\Software\Fonts");
```

## 验证清单

- [ ] 系统 DPI 设置正确（125%, 150%, 200% 等）
- [ ] 编译项目成功
- [ ] 启动浏览器
- [ ] 查看日志中的 DPI 检测信息
- [ ] 在浏览器控制台检查 `window.devicePixelRatio`
- [ ] 视觉检查页面显示是否正确

## 相关文件

- `Engine/PlaywrightController.cs` - DPI 检测实现
- `docs/DPI_SCALING_FIX.md` - DPI 缩放修复指南

## 技术参考

- [Windows DPI Awareness](https://docs.microsoft.com/en-us/windows/win32/hidpi/high-dpi-desktop-application-development-on-windows)
- [GetDpiForSystem](https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getdpiforsystem)
- [Playwright DeviceScaleFactor](https://playwright.dev/dotnet/docs/api/class-browsernewcontextoptions#browser-new-context-options-device-scale-factor)

---

**最后更新**: 2025-11-01  
**状态**: ✅ 已改进
