# Cloudflare 绕过使用指南

## 🚀 快速开始

### 1. 重新编译项目
```bash
cd d:\1Dev\webbrowser\web
dotnet build
```

### 2. 启动浏览器
- **WPF**：打开 `BrowserManagementPageV2.xaml`，点击"启动"
- **Web**：打开 `Browser/Index.cshtml`，点击"启动"

### 3. 快速检查（在浏览器控制台）
按 `F12` 打开开发者工具，切换到 Console 标签，复制粘贴以下代码：

```javascript
// 快速检查脚本
console.log('========================================');
console.log('🔍 Quick Fingerprint Check');
console.log('========================================\n');

const platform = navigator.platform;
const vendor = navigator.vendor;
const expectedVendor = (platform === 'iPhone' || platform === 'iPad' || platform === 'iPod' || platform === 'MacIntel') 
    ? 'Apple Computer, Inc.' 
    : 'Google Inc.';

console.log('1️⃣ Platform & Vendor:');
console.log(`   Platform: ${platform}`);
console.log(`   Vendor: ${vendor}`);
console.log(`   Expected: ${expectedVendor}`);
console.log(`   Status: ${vendor === expectedVendor ? '✅ MATCH' : '❌ MISMATCH'}\n`);

console.log('2️⃣ webdriver:');
console.log(`   Value: ${navigator.webdriver}`);
console.log(`   Status: ${navigator.webdriver === undefined ? '✅ GOOD' : '❌ BAD'}\n`);

console.log('3️⃣ Chrome object:');
console.log(`   chrome.app: ${window.chrome?.app ? '✅' : '❌'}`);
console.log(`   chrome.csi: ${typeof window.chrome?.csi === 'function' ? '✅' : '❌'}`);
console.log(`   chrome.loadTimes: ${typeof window.chrome?.loadTimes === 'function' ? '✅' : '❌'}\n`);

console.log('========================================');
```

### 4. 预期输出
如果一切正常，你应该看到：
```
========================================
🔍 Quick Fingerprint Check
========================================

1️⃣ Platform & Vendor:
   Platform: iPhone
   Vendor: Apple Computer, Inc.
   Expected: Apple Computer, Inc.
   Status: ✅ MATCH

2️⃣ webdriver:
   Value: undefined
   Status: ✅ GOOD

3️⃣ Chrome object:
   chrome.app: ✅
   chrome.csi: ✅
   chrome.loadTimes: ✅

========================================
```

## 🔍 常见问题排查

### 问题 1：Vendor 不匹配
**症状**：
```
Platform: iPhone
Vendor: Google Inc.  ❌
Expected: Apple Computer, Inc.
```

**解决方案**：
1. 确认已重新编译项目
2. 关闭所有浏览器窗口
3. 重新启动浏览器
4. 检查控制台是否有以下日志：
   ```
   [Turnstile Bypass] ✅ Vendor matches platform: iPhone -> Apple Computer, Inc.
   ```

### 问题 2：webdriver 仍然存在
**症状**：
```
webdriver: true  ❌
```

**解决方案**：
1. 检查是否使用了正确的启动器（UndetectedChrome）
2. 查看控制台日志：
   ```
   [Turnstile Bypass] ✅ webdriver removed
   ```
3. 如果仍然存在，手动执行：
   ```javascript
   delete navigator.webdriver;
   Object.defineProperty(navigator, 'webdriver', {
       get: () => undefined,
       configurable: true
   });
   ```

### 问题 3：Chrome 对象缺失
**症状**：
```
chrome.app: ❌
chrome.csi: ❌
chrome.loadTimes: ❌
```

**解决方案**：
1. 确认脚本已正确注入
2. 查看控制台日志：
   ```
   [Turnstile Bypass] ✅ Chrome object enhanced
   ```
3. 手动验证：
   ```javascript
   console.log(window.chrome.app);
   console.log(window.chrome.csi());
   console.log(window.chrome.loadTimes());
   ```

### 问题 4：Private Access Token 挑战
**症状**：
```
Request for the Private Access Token challenge.
```

**说明**：这是 Cloudflare 的高级验证机制，我们的脚本会自动处理。

**验证**：
查看控制台是否有：
```
[Turnstile Bypass] ✅ PAT support added
[CF Wait Helper] 🕐 Starting Cloudflare verification monitor...
```

**等待时间**：通常需要 5-15 秒完成验证

### 问题 5：403 Forbidden
**症状**：
```
GET https://m.iyf.tv/ 403 (Forbidden)
```

**可能原因**：
1. Vendor 与 Platform 不匹配
2. 缺少关键浏览器特征
3. IP 被封禁

**解决步骤**：
1. 运行快速检查脚本
2. 确认所有项都是 ✅
3. 如果仍然 403，尝试：
   - 更换代理
   - 清除浏览器缓存
   - 使用不同的浏览器配置

## 📊 完整测试脚本

如果需要更详细的测试，在控制台运行：

```javascript
// 加载完整测试脚本
fetch('file:///d:/1Dev/webbrowser/web/FishBrowser.Core/assets/Scripts/fingerprint-test.js')
    .then(r => r.text())
    .then(code => eval(code))
    .catch(e => console.error('Failed to load test script:', e));
```

或者直接打开文件：
```
d:\1Dev\webbrowser\web\FishBrowser.Core\assets\Scripts\fingerprint-test.js
```

## 🎯 成功指标

### 必须满足的条件
- ✅ `navigator.webdriver === undefined`
- ✅ `navigator.vendor` 与 `navigator.platform` 匹配
- ✅ `window.chrome.app` 存在
- ✅ `window.chrome.csi()` 可调用
- ✅ `window.chrome.loadTimes()` 可调用
- ✅ `navigator.plugins.length > 0`
- ✅ 无自动化痕迹（`__playwright`, `$cdc_`, 等）

### 推荐满足的条件
- ⚠️ `navigator.hardwareConcurrency` 在 2-32 之间
- ⚠️ `navigator.deviceMemory` 在 2-16 之间
- ⚠️ User-Agent 不包含 "HeadlessChrome"
- ⚠️ `performance.getEntriesByType('navigation').length > 0`

## 🔧 高级调试

### 查看所有注入的脚本
```javascript
console.log('Injected scripts:');
console.log('1. Turnstile Bypass:', typeof window.fetch !== 'function' ? '❌' : '✅');
console.log('2. Anti-Detection:', typeof navigator.vendor !== 'undefined' ? '✅' : '❌');
console.log('3. Wait Helper:', typeof window.waitForCloudflare === 'function' ? '✅' : '❌');
```

### 监控 Cloudflare 请求
```javascript
// 拦截所有 fetch 请求
const originalFetch = window.fetch;
window.fetch = function(...args) {
    if (args[0].includes('cloudflare')) {
        console.log('🌐 Cloudflare request:', args[0]);
        console.log('   Headers:', args[1]?.headers);
    }
    return originalFetch.apply(this, args);
};
```

### 手动等待验证完成
```javascript
// 如果页面卡在验证界面
if (typeof window.waitForCloudflare === 'function') {
    window.waitForCloudflare().then(() => {
        console.log('✅ Verification completed!');
    }).catch(err => {
        console.error('❌ Verification failed:', err);
    });
}
```

## 📝 日志检查清单

启动浏览器后，在控制台应该看到以下日志（按顺序）：

1. ✅ `undetected chromedriver 1337!`
2. ✅ `[Turnstile Bypass] 🚀 Initializing comprehensive bypass...`
3. ✅ `[Turnstile Bypass] ✅ Vendor matches platform: [Platform] -> [Vendor]`
4. ✅ `[Turnstile Bypass] ✅ webdriver removed`
5. ✅ `[Turnstile Bypass] ✅ Automation traces removed`
6. ✅ `[Turnstile Bypass] ✅ Chrome object enhanced`
7. ✅ `[Turnstile Bypass] ✅ Plugins already exist, skipping fix` 或 `✅ Plugins fixed with Proxy`
8. ✅ `[Turnstile Bypass] ✅ PAT support added`
9. ✅ `[Turnstile Bypass] ✅✅✅ All bypasses applied successfully!`
10. ✅ `[cloudflare-anti-detection.js] vendor getter called - platform: [Platform] -> vendor: [Vendor]`
11. ✅ `[CF Wait Helper] ✅ Monitor initialized`

如果缺少任何一条日志，说明对应的脚本未正确注入。

## 🆘 仍然无法通过？

如果按照以上步骤仍然无法通过 Cloudflare 验证，请提供以下信息：

1. **控制台完整日志**（从启动到失败）
2. **快速检查脚本的输出**
3. **访问的具体网站 URL**
4. **错误信息**（如 403, 600010 等）
5. **浏览器配置**（Platform, User-Agent 等）

## 📚 相关文件

- `cloudflare-turnstile-bypass.js` - 主要绕过脚本（11 大措施）
- `cloudflare-anti-detection.js` - 辅助防检测脚本（30 项措施）
- `cloudflare-wait-helper.js` - 验证等待助手
- `fingerprint-test.js` - 完整测试脚本
- `quick-check.js` - 快速检查脚本
- `UndetectedChromeLauncher.cs` - Selenium 启动器
- `PlaywrightController.cs` - Playwright 启动器

## 🎉 成功案例

如果看到以下输出，说明已成功绕过 Cloudflare：

```
[Turnstile Bypass] ✅ Vendor matches platform: iPhone -> Apple Computer, Inc.
[Turnstile Bypass] ✅✅✅ All bypasses applied successfully!
[CF Wait Helper] ✅ Cloudflare verification completed!
[CF Wait Helper] ✅ Page is ready!
```

页面应该正常加载，不再显示 "Checking your browser" 或 403 错误。
