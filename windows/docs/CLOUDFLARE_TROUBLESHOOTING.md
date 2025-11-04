# Cloudflare 验证失败排查指南

## 🔍 问题现象

浏览器启动后，访问 Cloudflare 保护的网站时：
- 出现 "Checking your browser" 页面
- 长时间停留在验证页面
- 最终显示 "Access denied" 或 "Please complete the security check"

## 📊 日志检查

### 1. 查看浏览器配置摘要

启动浏览器后，查找日志中的 `Browser Configuration Summary`：

```
[PlaywrightController] ========== Browser Configuration Summary ==========
[PlaywrightController] Fingerprint: Meta-221948-Profile-222011
[PlaywrightController] UserAgent: Mozilla/5.0 (...)
[PlaywrightController] Platform: Win32
[PlaywrightController] --- Anti-Detection Data ---
[PlaywrightController] PluginsJson: ❌ NOT SET
[PlaywrightController] LanguagesJson: ❌ NOT SET
[PlaywrightController] SecChUa: ❌ NOT SET (using fallback)
```

### 2. 检查警告信息

如果看到以下警告：

```
[PlaywrightController] ⚠️ WARNING: Anti-detection data is missing!
[PlaywrightController] ⚠️ Cloudflare bypass may fail. Please create a NEW profile using '一键随机' to get anti-detection data.
```

**说明**：你使用的是旧版本的 Profile，缺少防检测数据。

## ✅ 解决方案

### 方案 A：创建新的浏览器环境（推荐）

1. **打开浏览器管理**
   - 点击"新建浏览器"

2. **选择"一键随机"**
   - 点击"一键随机"按钮
   - 系统会自动生成完整的防检测数据

3. **检查日志**
   - 查找 `Generated anti-detection data for profile`
   - 确认看到以下信息：
   ```
   [BrowserEnvironmentService] Generated anti-detection data for profile: xxx
   [BrowserEnvironmentService]   - Plugins: 300+ chars
   [BrowserEnvironmentService]   - Languages: ["zh-CN", "zh", "en-US", "en"]
   [BrowserEnvironmentService]   - HardwareConcurrency: 8
   [BrowserEnvironmentService]   - DeviceMemory: 8
   [BrowserEnvironmentService]   - SecChUa: "Chromium";v="120", ...
   ```

4. **保存并启动**
   - 点击"创建"
   - 启动浏览器

5. **验证配置**
   - 查看日志中的 `Browser Configuration Summary`
   - 确认所有字段都显示 `✅`：
   ```
   [PlaywrightController] PluginsJson: ✅ 300 chars
   [PlaywrightController] LanguagesJson: ✅ ["zh-CN", "zh", "en-US", "en"]
   [PlaywrightController] SecChUa: ✅ "Chromium";v="120", ...
   ```

### 方案 B：删除旧 Profile 并重新生成

如果你想保留环境名称和其他配置：

1. **删除旧的 Profile**
   - 打开数据库（webscraper.db）
   - 删除 FingerprintProfiles 表中的旧记录
   - 或直接删除整个数据库文件（会丢失所有数据）

2. **重新启动应用**
   - 数据库会自动重建

3. **创建新环境**
   - 使用"一键随机"生成新 Profile

### 方案 C：手动更新旧 Profile（高级）

**不推荐**，除非你熟悉数据库操作。

1. 打开数据库
2. 找到你的 FingerprintProfile
3. 手动添加以下字段：
   - PluginsJson
   - LanguagesJson
   - HardwareConcurrency
   - DeviceMemory
   - MaxTouchPoints
   - ConnectionType
   - ConnectionRtt
   - ConnectionDownlink
   - SecChUa
   - SecChUaPlatform
   - SecChUaMobile

## 🧪 验证步骤

### 1. 检查日志

启动浏览器后，确认日志中：
- ✅ 没有 `WARNING: Anti-detection data is missing`
- ✅ 所有防检测字段都显示 `✅`
- ✅ 看到 `Anti-detection script added (Cloudflare bypass, data-driven)`

### 2. 检查浏览器控制台

在浏览器中按 F12，打开控制台，运行：

```javascript
console.log({
  webdriver: navigator.webdriver,  // 应该是 undefined
  plugins: navigator.plugins.length,  // 应该 > 0
  languages: navigator.languages,  // 应该是数组
  hardwareConcurrency: navigator.hardwareConcurrency,  // 应该 > 0
  deviceMemory: navigator.deviceMemory,  // 应该 > 0
  connection: navigator.connection?.effectiveType  // 应该有值
});
```

**预期输出**：
```javascript
{
  webdriver: undefined,  // ✅
  plugins: 3,  // ✅
  languages: ["zh-CN", "zh", "en-US", "en"],  // ✅
  hardwareConcurrency: 8,  // ✅
  deviceMemory: 8,  // ✅
  connection: "4g"  // ✅
}
```

### 3. 测试 Cloudflare

访问测试站点：
- https://nowsecure.nl
- https://www.cloudflare.com/cdn-cgi/trace

**预期结果**：
- 首次可能出现 "Checking your browser" 页面
- 2-5 秒后自动通过
- 后续访问直接放行（持久化 cookie）

## 🔧 其他检查项

### 1. 系统 Chrome 是否安装

检查日志中的 Channel：
```
[PlaywrightController] Channel: chrome
```

如果使用的是 Playwright 内置 Chromium，TLS 指纹会不同。

**解决方案**：
- 安装 Google Chrome
- 或切换到 `Channel = "msedge"`（使用 Edge）

### 2. 网络连接

确保：
- ✅ 网络连接正常
- ✅ 没有使用不稳定的代理
- ✅ DNS 解析正常

### 3. 超时设置

检查导航超时：
```
[PlaywrightController] Navigating to ... with timeout 45000ms
```

如果网络慢，可以增大超时：
- 修改 `NavigateAsync` 的默认超时
- 或在调用时传入更大的值

## 📝 常见错误

### 错误 1：PluginsJson = NOT SET

**原因**：使用了旧版本的 Profile

**解决**：创建新环境，使用"一键随机"

### 错误 2：SecChUa = NOT SET

**原因**：使用了旧版本的 Profile

**解决**：创建新环境，使用"一键随机"

### 错误 3：Client Hints not in fingerprint, using fallback

**原因**：使用了旧版本的 Profile

**解决**：创建新环境，使用"一键随机"

### 错误 4：Channel = chromium (not chrome)

**原因**：系统未安装 Chrome，使用了 Playwright 内置 Chromium

**解决**：
- 安装 Google Chrome
- 或修改代码使用 `Channel = "msedge"`

## 🎯 完整的成功日志示例

```
[PlaywrightController] ========== Browser Configuration Summary ==========
[PlaywrightController] Fingerprint: Random-Chrome-Windows-Profile
[PlaywrightController] UserAgent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36
[PlaywrightController] Platform: Win32
[PlaywrightController] Locale: zh-CN
[PlaywrightController] AcceptLanguage: zh-CN,zh;q=0.9,en-US;q=0.8,en;q=0.7
[PlaywrightController] Timezone: Asia/Shanghai
[PlaywrightController] --- Anti-Detection Data ---
[PlaywrightController] PluginsJson: ✅ 312 chars
[PlaywrightController] LanguagesJson: ✅ ["zh-CN","zh","en-US","en"]
[PlaywrightController] HardwareConcurrency: 8
[PlaywrightController] DeviceMemory: 8
[PlaywrightController] MaxTouchPoints: 0
[PlaywrightController] ConnectionType: 4g
[PlaywrightController] ConnectionRtt: 45
[PlaywrightController] ConnectionDownlink: 12.5
[PlaywrightController] --- Client Hints ---
[PlaywrightController] SecChUa: ✅ "Chromium";v="120", "Google Chrome";v="120", "Not-A.Brand";v="99"
[PlaywrightController] SecChUaPlatform: ✅ "Windows"
[PlaywrightController] SecChUaMobile: ✅ ?0
[PlaywrightController] --- WebGL ---
[PlaywrightController] WebGLVendor: Google Inc. (NVIDIA)
[PlaywrightController] WebGLRenderer: ANGLE (NVIDIA GeForce GTX 1650 Direct3D11 vs_5_0 ps_5_0)
[PlaywrightController] ===================================================
```

## 📞 仍然失败？

如果按照以上步骤操作后仍然失败，请提供：

1. **完整的日志**
   - 从启动到失败的所有日志
   - 特别是 `Browser Configuration Summary` 部分

2. **浏览器控制台输出**
   - navigator 对象的所有属性
   - 任何错误信息

3. **测试的 URL**
   - 哪个 Cloudflare 站点失败了

4. **系统信息**
   - Windows 版本
   - Chrome 版本（如果安装了）
   - 网络环境（是否使用代理）

## 🎯 总结

**关键点**：
1. ✅ 使用"一键随机"创建新环境
2. ✅ 确认日志中所有防检测字段都是 `✅`
3. ✅ 使用系统 Chrome（不是 Playwright Chromium）
4. ✅ 检查浏览器控制台输出
5. ✅ 测试 Cloudflare 站点

**记住**：旧的 Profile 不包含防检测数据，必须创建新的！
