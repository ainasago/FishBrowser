# 浏览器指纹信息显示功能

## 🎯 功能说明

启动 UndetectedChrome 浏览器时，会自动在新标签页中显示当前浏览器的指纹信息，包括：

1. **配置的指纹特征**（已规范化）
2. **实时检测的指纹**（JavaScript 读取）
3. **快捷测试按钮**

---

## 📁 文件结构

```
WebScraperApp/
├── assets/
│   └── templates/
│       └── fingerprint-info.html    ← HTML 模板文件
└── Services/
    └── UndetectedChromeLauncher.cs  ← 加载和显示逻辑
```

---

## 🔧 实现方式

### 1. HTML 模板（可编辑）

**文件位置**：`assets/templates/fingerprint-info.html`

**占位符**：
- `{{USER_AGENT}}` - User-Agent 字符串
- `{{LANGUAGES}}` - 语言列表 JSON
- `{{TIMEZONE}}` - 时区
- `{{PLATFORM}}` - 平台
- `{{SCREEN_RESOLUTION}}` - 屏幕分辨率
- `{{VIEWPORT_SIZE}}` - 视口大小

**优点**：
- ✅ 不需要重新编译即可修改样式
- ✅ 支持自定义 HTML/CSS/JavaScript
- ✅ 易于维护和更新

---

### 2. 加载逻辑

```csharp
private string GenerateFingerprintInfoHtml()
{
    // 1. 读取模板文件
    var templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, 
        "assets", "templates", "fingerprint-info.html");
    var html = File.ReadAllText(templatePath);

    // 2. 替换占位符
    html = html.Replace("{{USER_AGENT}}", userAgent)
               .Replace("{{LANGUAGES}}", languages)
               .Replace("{{TIMEZONE}}", timezone)
               // ...

    return html;
}
```

---

### 3. 显示逻辑

```csharp
private async Task ShowFingerprintInfoAsync()
{
    // 1. 打开新标签页
    js.ExecuteScript("window.open('about:blank', '_blank');");
    
    // 2. 切换到新标签页
    var handles = _driver.WindowHandles;
    _driver.SwitchTo().Window(handles[handles.Count - 1]);
    
    // 3. 写入 HTML
    js.ExecuteScript($@"
        document.open();
        document.write({JsonSerializer.Serialize(html)});
        document.close();
    ");
    
    // 4. 切换回主标签页
    _driver.SwitchTo().Window(handles[0]);
}
```

---

## 🎨 页面功能

### 1. 配置的指纹特征

显示从 `FingerprintProfile` 读取的配置值：

- ✅ User-Agent（已规范化）
- ✅ Languages
- ✅ Timezone
- ✅ Platform
- ✅ Screen Resolution
- ✅ Viewport Size

**状态标记**：
- 🟢 **已规范化** - 表示指纹已经过智能验证

---

### 2. 实时指纹检测

通过 JavaScript 实时读取浏览器指纹：

```javascript
const fingerprint = {
    'User-Agent': navigator.userAgent,
    'Platform': navigator.platform,
    'Languages': navigator.languages,
    'Language': navigator.language,
    'Timezone': Intl.DateTimeFormat().resolvedOptions().timeZone,
    'Timezone Offset': new Date().getTimezoneOffset(),
    'Screen Resolution': `${screen.width}x${screen.height}`,
    'Hardware Concurrency': navigator.hardwareConcurrency,
    'Device Memory': navigator.deviceMemory,
    'Max Touch Points': navigator.maxTouchPoints,
    'WebDriver': navigator.webdriver,  // ← 应该显示 undefined
    'Plugins Count': navigator.plugins.length,
    // ...
};
```

**关键检测**：
- ✅ `webdriver` 应该是 `undefined`（已隐藏）
- ✅ 其他值应该与配置一致

---

### 3. 快捷测试按钮

#### 🔄 重新检测
- 重新读取当前浏览器指纹
- 验证防检测脚本是否生效

#### 🛡️ 测试 Cloudflare
- 在新标签页打开 `https://www.iyf.tv/`
- 快速测试 Cloudflare 验证

#### 🌐 查看详细信息
- 打开 `https://www.whatismybrowser.com/`
- 查看完整的浏览器信息

---

## 📝 自定义模板

### 修改样式

编辑 `assets/templates/fingerprint-info.html` 中的 CSS：

```css
.header {
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    /* 修改为你喜欢的颜色 */
}
```

### 添加新字段

1. 在 HTML 中添加占位符：
```html
<div class='info-item'>
    <div class='info-label'>新字段</div>
    <div class='info-value'>{{NEW_FIELD}}</div>
</div>
```

2. 在 C# 中替换占位符：
```csharp
html = html.Replace("{{NEW_FIELD}}", newFieldValue);
```

### 添加新功能

在 `<script>` 标签中添加 JavaScript：

```javascript
function customFunction() {
    // 自定义功能
}
```

---

## 🔍 验证指纹

### 1. 检查配置值

**配置的指纹特征** 部分应该显示：
- ✅ User-Agent 版本号真实（例如：Chrome/130.x）
- ✅ Languages 与配置一致
- ✅ Timezone 与配置一致

### 2. 检查实时值

**实时指纹检测** 部分应该显示：
- ✅ `WebDriver: ✅ 已隐藏`（undefined）
- ✅ User-Agent 与配置一致
- ✅ Languages 与配置一致
- ✅ Timezone 与配置一致

### 3. 对比差异

如果配置值和实时值不一致：
- ⚠️ 检查防检测脚本是否正确注入
- ⚠️ 检查 Chrome 参数是否正确设置
- ⚠️ 查看浏览器控制台是否有错误

---

## 🐛 故障排除

### 问题 1：模板未找到

**错误**：`Template not found`

**解决**：
1. 检查文件是否存在：`assets/templates/fingerprint-info.html`
2. 重新编译项目（F6）
3. 检查输出目录：`bin/Debug/net9.0-windows/assets/templates/`

---

### 问题 2：占位符未替换

**现象**：页面显示 `{{USER_AGENT}}` 而不是实际值

**解决**：
1. 检查占位符拼写是否正确
2. 检查 C# 代码中的 `Replace` 调用
3. 查看日志是否有错误

---

### 问题 3：实时检测显示错误值

**现象**：`WebDriver: true`（应该是 undefined）

**解决**：
1. 检查 `cloudflare-anti-detection.js` 是否正确加载
2. 检查 `InjectCustomFingerprint()` 是否执行
3. 在浏览器控制台手动执行：
   ```javascript
   console.log(navigator.webdriver);  // 应该是 undefined
   ```

---

## 💡 使用技巧

### 1. 快速验证指纹

启动浏览器后：
1. 查看自动打开的指纹信息页面
2. 检查 **WebDriver** 是否已隐藏
3. 点击 **🛡️ 测试 Cloudflare** 验证

### 2. 对比不同环境

创建多个浏览器环境：
1. 环境 A：日本指纹
2. 环境 B：美国指纹
3. 启动后对比指纹信息页面

### 3. 调试指纹问题

如果 Cloudflare 验证失败：
1. 查看指纹信息页面
2. 检查哪些值不真实
3. 调整 Profile 配置
4. 重新启动验证

---

## 📊 页面截图说明

### 配置的指纹特征
```
┌─────────────────────────────────────────┐
│ 📋 配置的指纹特征 [已规范化]           │
├─────────────────────────────────────────┤
│ User-Agent:                             │
│ Mozilla/5.0 ... Chrome/130.0.4166.21 .. │
│                                         │
│ Languages:                              │
│ ["ja-JP", "ja", "en"]                   │
│                                         │
│ Timezone:                               │
│ Asia/Tokyo                              │
└─────────────────────────────────────────┘
```

### 实时指纹检测
```
┌─────────────────────────────────────────┐
│ 🔬 实时指纹检测                         │
├─────────────────────────────────────────┤
│ User-Agent: Mozilla/5.0 ... Chrome/130..│
│ Platform: Win32                         │
│ Languages: ja-JP,ja,en                  │
│ Timezone: Asia/Tokyo                    │
│ WebDriver: ✅ 已隐藏                    │
│                                         │
│ [🔄 重新检测] [🛡️ 测试 Cloudflare]     │
│ [🌐 查看详细信息]                       │
└─────────────────────────────────────────┘
```

---

## ✅ 总结

### 优点
- ✅ 自动显示指纹信息，无需手动检查
- ✅ 实时对比配置值和实际值
- ✅ 快捷测试按钮，提高效率
- ✅ 模板化设计，易于自定义

### 使用场景
1. **开发调试**：验证指纹配置是否正确
2. **测试验证**：快速测试 Cloudflare 绕过
3. **问题排查**：对比配置值和实际值找出问题
4. **用户演示**：展示指纹浏览器功能

---

**现在启动浏览器时，会自动显示漂亮的指纹信息页面！** 🎉
