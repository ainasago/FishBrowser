# Cloudflare 防检测脚本使用指南

## 📁 文件位置

### 脚本文件
```
WebScraperApp/
├── assets/
│   └── scripts/
│       └── cloudflare-anti-detection.js  ← 防检测脚本
```

### 自动复制
项目配置了自动复制 `assets/**/*.*` 到输出目录：
```xml
<ItemGroup>
  <None Include="assets\**\*.*">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

编译后的位置：
```
bin/Debug/net9.0-windows/
├── assets/
│   └── scripts/
│       └── cloudflare-anti-detection.js
```

## 📝 脚本内容

### 30 项防检测措施

#### Navigator 伪装（12 项）
1. ✅ `navigator.webdriver` = undefined
2. ✅ `navigator.plugins` = 3 个插件
3. ✅ `navigator.mimeTypes` = 2 个类型
4. ✅ `navigator.languages` = ['zh-CN', 'zh', 'en-US', 'en']
5. ✅ `navigator.permissions` 增强
6. ✅ `navigator.hardwareConcurrency` = 8
7. ✅ `navigator.deviceMemory` = 8
8. ✅ `navigator.maxTouchPoints` = 0
9. ✅ `navigator.connection` = 4g
10. ✅ `navigator.platform` = Win32
11. ✅ `navigator.vendor` = Google Inc.
12. ✅ `navigator.appVersion` = Chrome/120

#### Chrome 对象伪装（3 项）
13. ✅ `window.chrome.runtime`
14. ✅ `window.chrome.loadTimes`
15. ✅ `window.chrome.csi`

#### 指纹伪造（3 项）
16. ✅ Canvas 指纹（噪音注入）
17. ✅ WebGL 指纹（Vendor/Renderer）
18. ✅ AudioContext 指纹（噪音注入）

#### Screen/时区/通知（4 项）
19. ✅ Screen 属性（1920x1080）
20. ✅ Date.getTimezoneOffset（UTC+8）
21. ✅ Intl.DateTimeFormat（Asia/Shanghai）
22. ✅ Notification.permission = default

#### Turnstile 专用 API（9 项）
23. ✅ Battery API
24. ✅ MediaDevices API
25. ✅ ServiceWorker API
26. ✅ Bluetooth API
27. ✅ USB API
28. ✅ Presentation API
29. ✅ Credentials API
30. ✅ Keyboard API
31. ✅ MediaSession API

#### 自动化痕迹移除（1 项）
32. ✅ 删除 cdc_* 变量

## 💻 使用方法

### 在 C# 中加载

```csharp
// 1. 获取脚本路径
var scriptPath = Path.Combine(
    AppDomain.CurrentDomain.BaseDirectory, 
    "assets", 
    "scripts", 
    "cloudflare-anti-detection.js"
);

// 2. 检查文件是否存在
if (!File.Exists(scriptPath))
{
    _log.LogError("BrowserMgmt", $"Anti-detection script not found: {scriptPath}");
    return;
}

// 3. 读取脚本内容
var antiDetectionScript = await File.ReadAllTextAsync(scriptPath);

// 4. 注入到 Playwright Context
await context.AddInitScriptAsync(antiDetectionScript);

_log.LogInfo("BrowserMgmt", $"✅ Loaded anti-detection script from: {scriptPath}");
```

### 示例：BrowserManagementPage.xaml.cs

```csharp
private async void LaunchMVP_Click(object sender, RoutedEventArgs e)
{
    var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
    var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
    {
        Headless = false,
        Channel = "chrome"
    });
    
    var context = await browser.NewContextAsync(new BrowserNewContextOptions
    {
        UserAgent = "Mozilla/5.0 ...",
        // ... 其他配置
    });
    
    // 加载防检测脚本
    var scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "scripts", "cloudflare-anti-detection.js");
    var script = await File.ReadAllTextAsync(scriptPath);
    await context.AddInitScriptAsync(script);
    
    var page = await context.NewPageAsync();
    await page.GotoAsync("https://example.com");
}
```

## 🔧 修改脚本

### 1. 编辑脚本文件
```
WebScraperApp/assets/scripts/cloudflare-anti-detection.js
```

### 2. 重新编译
```
生成 → 重新生成解决方案
```

### 3. 验证
脚本会自动复制到输出目录：
```
bin/Debug/net9.0-windows/assets/scripts/cloudflare-anti-detection.js
```

## 📊 验证脚本是否生效

### 在浏览器控制台运行

```javascript
// 1. 验证 webdriver
console.log('webdriver:', navigator.webdriver);  // undefined ✅

// 2. 验证 plugins
console.log('plugins:', navigator.plugins.length);  // 3 ✅

// 3. 验证 chrome 对象
console.log('chrome:', !!window.chrome);  // true ✅
console.log('chrome.runtime:', !!window.chrome?.runtime);  // true ✅

// 4. 验证 Battery API
navigator.getBattery().then(b => console.log('battery:', b.level));  // 1 ✅

// 5. 验证 MediaDevices
navigator.mediaDevices.enumerateDevices().then(d => console.log('devices:', d.length));  // 3 ✅

// 6. 验证 ServiceWorker
console.log('serviceWorker:', !!navigator.serviceWorker);  // true ✅
```

### 预期输出
```
webdriver: undefined
plugins: 3
chrome: true
chrome.runtime: true
battery: 1
devices: 3
serviceWorker: true
```

## 🎯 优势

### 1. 易于维护
- ✅ 脚本独立于代码
- ✅ 修改无需重新编译 C# 代码
- ✅ 可以版本控制

### 2. 易于测试
- ✅ 可以在浏览器控制台直接测试
- ✅ 可以单独调试脚本

### 3. 易于扩展
- ✅ 添加新的防检测措施只需编辑 JS 文件
- ✅ 可以创建多个版本的脚本

### 4. 易于分享
- ✅ 可以分享给其他项目
- ✅ 可以从社区获取更新

## 📚 相关文档

- `CLOUDFLARE_TURNSTILE_BYPASS.md` - Turnstile 绕过指南
- `CLOUDFLARE_BYPASS_GUIDE.md` - 完整绕过指南
- `CLOUDFLARE_FINAL_STATUS.md` - 最终状态报告

## ⚠️ 注意事项

### 1. 文件路径
- ✅ 使用小写 `assets`（不是 `Assets`）
- ✅ 使用 `Path.Combine` 构建路径
- ✅ 检查文件是否存在

### 2. 编码
- ✅ 使用 UTF-8 编码
- ✅ 使用 `File.ReadAllTextAsync`

### 3. 错误处理
- ✅ 检查文件是否存在
- ✅ 记录错误日志
- ✅ 提供友好的错误提示

## 🔄 更新流程

### 1. 修改脚本
```
编辑 assets/scripts/cloudflare-anti-detection.js
```

### 2. 测试
```
重新编译 → 运行程序 → 测试浏览器
```

### 3. 验证
```
查看日志 → 浏览器控制台验证
```

### 4. 提交
```
git add assets/scripts/cloudflare-anti-detection.js
git commit -m "Update anti-detection script"
```

## ✅ 总结

**优点**：
1. ✅ 脚本与代码分离
2. ✅ 易于维护和更新
3. ✅ 可以独立测试
4. ✅ 支持版本控制
5. ✅ 自动复制到输出目录

**使用**：
```csharp
var scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "scripts", "cloudflare-anti-detection.js");
var script = await File.ReadAllTextAsync(scriptPath);
await context.AddInitScriptAsync(script);
```

**验证**：
```javascript
console.log(navigator.webdriver);  // undefined ✅
```

现在防检测脚本已经完全模块化，易于维护和更新！🚀
