# Cloudflare 绕过 - 数据驱动架构完成

## 🎯 核心改进

从**硬编码**改为**数据驱动**：
- ❌ 之前：防检测数据硬编码在 JavaScript 脚本中
- ✅ 现在：防检测数据存储在 FingerprintProfile，一键随机时自动生成，保存时自动校验

## 📊 架构设计

```
一键随机
  ↓
BrowserEnvironmentService.BuildRandomDraft()
  ↓
BrowserEnvironmentService.BuildProfileFromDraft()
  ↓
AntiDetectionService.GenerateAntiDetectionData()  ← 【新增】生成真实数据
  ↓
FingerprintProfile (数据库)
  ↓
保存时
  ↓
AntiDetectionService.ValidateProfile()  ← 【新增】校验一致性
  ↓
PlaywrightController.InitializePersistentContextAsync()
  ↓
GenerateAntiDetectScript(fingerprint)  ← 【新增】从指纹读取数据
  ↓
注入到浏览器
```

## 🗄️ 数据模型扩展

### FingerprintProfile 新增字段

```csharp
// 防检测配置（Cloudflare 绕过）
public string? PluginsJson { get; set; }  // JSON array: [{name, filename, description}]
public string? LanguagesJson { get; set; }  // JSON array: ["zh-CN", "zh", "en-US", "en"]
public int HardwareConcurrency { get; set; } = 8;  // CPU 核心数
public int MaxTouchPoints { get; set; } = 0;  // 触摸点数（桌面为 0）
public string ConnectionType { get; set; } = "4g";  // 网络类型
public int ConnectionRtt { get; set; } = 50;  // 网络延迟 (ms)
public double ConnectionDownlink { get; set; } = 10.0;  // 下载速度 (Mbps)
public string? SecChUa { get; set; }  // Client Hints
public string? SecChUaPlatform { get; set; }
public string? SecChUaMobile { get; set; }
```

## 🔧 核心服务

### AntiDetectionService

#### 1. GenerateAntiDetectionData()
**功能**：为指纹配置生成防检测数据（一键随机时调用）

**生成逻辑**：
```csharp
// 1. Plugins（根据 UA 判断浏览器类型）
- Chrome/Edge: PDF Plugin, PDF Viewer, Native Client
- Firefox: PDF Viewer
- Safari: PDF

// 2. Languages（根据 Locale 和 AcceptLanguage）
- 从 locale 提取：zh-CN → ["zh-CN", "zh"]
- 从 Accept-Language 提取其他语言
- 确保至少有英语：["en-US", "en"]
- 最多 6 个语言

// 3. HardwareConcurrency（随机但合理）
- 常见值：2, 4, 6, 8, 12, 16
- 权重：5%, 30%, 20%, 30%, 10%, 5%
- 默认：8

// 4. DeviceMemory（随机但合理）
- 常见值：4, 8, 16 GB
- 权重：20%, 60%, 20%
- 默认：8

// 5. MaxTouchPoints
- 桌面：0
- 移动：1-5 随机

// 6. Connection（随机但合理）
- 4g: RTT 30-80ms, Downlink 5-20 Mbps (权重 70%)
- wifi: RTT 10-40ms, Downlink 20-100 Mbps (权重 25%)
- 3g: RTT 100-300ms, Downlink 1-5 Mbps (权重 5%)

// 7. Client Hints（根据 UA 和 Platform）
- 提取 Chrome 版本
- sec-ch-ua: "Chromium";v="120", "Google Chrome";v="120"
- sec-ch-ua-platform: "Windows" / "macOS" / "Linux" / "Android" / "iOS"
- sec-ch-ua-mobile: "?0" (桌面) / "?1" (移动)
```

#### 2. ValidateProfile()
**功能**：校验指纹配置的一致性（保存时调用）

**校验规则**：
```csharp
// 1. UA 与 Platform 一致性
- Platform=Win32 但 UA 不包含 Windows → 错误
- Platform=MacIntel 但 UA 不包含 Mac → 错误
- Platform=Linux 但 UA 不包含 Linux → 错误

// 2. Languages 与 Locale 一致性
- Languages 首项应与 Locale 主语言一致
- 例如：Locale=zh-CN，Languages 首项应为 zh-CN 或 zh

// 3. Client Hints 与 UA 一致性
- SecChUa 版本应与 UA 中的 Chrome 版本一致
- 例如：UA 包含 Chrome/120，SecChUa 应包含 v="120"

// 4. HardwareConcurrency 合理性
- 应在 1-32 之间

// 5. DeviceMemory 合理性
- 应为 0.25, 0.5, 1, 2, 4, 8, 16, 32 之一

// 6. MaxTouchPoints 与 Platform 一致性
- 桌面平台不应有触摸点
```

## 🔄 集成流程

### 1. 一键随机时自动生成

```csharp
// BrowserEnvironmentService.BuildProfileFromDraft()
var profile = new FingerprintProfile { ... };

// 生成防检测数据
_antiDetectionSvc.GenerateAntiDetectionData(profile);

return profile;
```

### 2. PlaywrightController 读取数据

```csharp
// 从指纹读取数据
private string GenerateAntiDetectScript(FingerprintProfile fingerprint)
{
    var plugins = fingerprint.PluginsJson ?? "[]";
    var languages = fingerprint.LanguagesJson ?? "[\"zh-CN\", \"zh\", \"en-US\", \"en\"]";
    var hardwareConcurrency = fingerprint.HardwareConcurrency;
    var deviceMemory = fingerprint.DeviceMemory;
    var maxTouchPoints = fingerprint.MaxTouchPoints;
    var connectionType = fingerprint.ConnectionType ?? "4g";
    var connectionRtt = fingerprint.ConnectionRtt;
    var connectionDownlink = fingerprint.ConnectionDownlink;

    return $@"(() => {{
        // 1. 隐藏 webdriver
        Object.defineProperty(navigator, 'webdriver', {{ get: () => undefined }});
        
        // 2. 伪装 plugins（从指纹读取）
        Object.defineProperty(navigator, 'plugins', {{ get: () => {plugins} }});
        
        // 3. 伪装 languages（从指纹读取）
        Object.defineProperty(navigator, 'languages', {{ get: () => {languages} }});
        
        // 4-9. 其他属性...
    }})();";
}
```

### 3. Client Hints Headers

```csharp
// 优先使用指纹配置中的 Client Hints
if (!string.IsNullOrEmpty(fingerprint.SecChUa))
{
    headers["sec-ch-ua"] = fingerprint.SecChUa;
    headers["sec-ch-ua-mobile"] = fingerprint.SecChUaMobile ?? "?0";
    headers["sec-ch-ua-platform"] = fingerprint.SecChUaPlatform ?? "\"Windows\"";
}
else
{
    // 回退：从 UA 提取
    var chromeVersion = ExtractChromeVersion(fingerprint.UserAgent);
    headers["sec-ch-ua"] = $"\"Chromium\";v=\"{chromeVersion}\", \"Google Chrome\";v=\"{chromeVersion}\"";
}
```

## 📝 使用流程

### 用户视角

1. **一键随机**
   - 点击"一键随机"按钮
   - 系统自动生成所有防检测数据
   - 数据符合真实浏览器特征
   - 数据之间保持一致性

2. **保存时校验**
   - 点击"创建"或"保存"按钮
   - 系统自动校验数据一致性
   - 如有错误，显示具体问题
   - 用户修正后再保存

3. **启动浏览器**
   - 系统从指纹读取防检测数据
   - 注入到浏览器
   - Cloudflare 验证通过 ✅

### 开发者视角

**添加新的防检测字段**：
1. 在 `FingerprintProfile` 添加字段
2. 在 `AntiDetectionService.GenerateAntiDetectionData()` 添加生成逻辑
3. 在 `AntiDetectionService.ValidateProfile()` 添加校验逻辑
4. 在 `PlaywrightController.GenerateAntiDetectScript()` 读取并注入

## 🎯 优势

### 1. 数据驱动
- ✅ 防检测数据存储在数据库
- ✅ 可追溯、可审计
- ✅ 支持版本管理

### 2. 自动生成
- ✅ 一键随机时自动生成
- ✅ 数据符合真实特征
- ✅ 减少人工配置

### 3. 自动校验
- ✅ 保存时自动检查一致性
- ✅ 防止配置错误
- ✅ 提高成功率

### 4. 灵活扩展
- ✅ 易于添加新字段
- ✅ 易于修改生成逻辑
- ✅ 易于调整校验规则

## 📊 数据示例

### 生成的 FingerprintProfile

```json
{
  "Name": "Chrome-Windows-Profile",
  "UserAgent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
  "Platform": "Win32",
  "Locale": "zh-CN",
  "AcceptLanguage": "zh-CN,zh;q=0.9,en-US;q=0.8,en;q=0.7",
  
  "PluginsJson": "[{\"name\":\"Chrome PDF Plugin\",\"filename\":\"internal-pdf-viewer\",\"description\":\"Portable Document Format\"},{\"name\":\"Chrome PDF Viewer\",\"filename\":\"mhjfbmdgcfjbbpaeojofohoefgiehjai\",\"description\":\"\"},{\"name\":\"Native Client\",\"filename\":\"internal-nacl-plugin\",\"description\":\"\"}]",
  
  "LanguagesJson": "[\"zh-CN\",\"zh\",\"en-US\",\"en\"]",
  
  "HardwareConcurrency": 8,
  "DeviceMemory": 8,
  "MaxTouchPoints": 0,
  
  "ConnectionType": "4g",
  "ConnectionRtt": 45,
  "ConnectionDownlink": 12.5,
  
  "SecChUa": "\"Chromium\";v=\"120\", \"Google Chrome\";v=\"120\", \"Not-A.Brand\";v=\"99\"",
  "SecChUaPlatform": "\"Windows\"",
  "SecChUaMobile": "?0"
}
```

### 注入的 JavaScript

```javascript
(() => {
    // 1. 隐藏 webdriver
    Object.defineProperty(navigator, 'webdriver', { get: () => undefined });
    
    // 2. 伪装 plugins
    Object.defineProperty(navigator, 'plugins', {
        get: () => [
            {name: "Chrome PDF Plugin", filename: "internal-pdf-viewer", description: "Portable Document Format"},
            {name: "Chrome PDF Viewer", filename: "mhjfbmdgcfjbbpaeojofohoefgiehjai", description: ""},
            {name: "Native Client", filename: "internal-nacl-plugin", description: ""}
        ]
    });
    
    // 3. 伪装 languages
    Object.defineProperty(navigator, 'languages', {
        get: () => ["zh-CN", "zh", "en-US", "en"]
    });
    
    // 4. 伪装 hardwareConcurrency
    Object.defineProperty(navigator, 'hardwareConcurrency', { get: () => 8 });
    
    // 5. 伪装 deviceMemory
    Object.defineProperty(navigator, 'deviceMemory', { get: () => 8 });
    
    // 6. 伪装 maxTouchPoints
    Object.defineProperty(navigator, 'maxTouchPoints', { get: () => 0 });
    
    // 7. 伪装 connection
    Object.defineProperty(navigator, 'connection', {
        get: () => ({ effectiveType: '4g', rtt: 45, downlink: 12.5, saveData: false })
    });
    
    // 8. 伪装 permissions
    const originalQuery = window.navigator.permissions.query;
    window.navigator.permissions.query = (parameters) => (
        parameters.name === 'notifications' ?
            Promise.resolve({ state: Notification.permission }) :
            originalQuery(parameters)
    );
    
    // 9. 伪装 chrome 对象
    if (!window.chrome) {
        window.chrome = { runtime: {} };
    }
})();
```

### 发送的 Headers

```
sec-ch-ua: "Chromium";v="120", "Google Chrome";v="120", "Not-A.Brand";v="99"
sec-ch-ua-mobile: ?0
sec-ch-ua-platform: "Windows"
accept-language: zh-CN,zh;q=0.9,en-US;q=0.8,en;q=0.7
```

## 🧪 验证方法

### 1. 检查数据生成

```csharp
// 一键随机后
var profile = _envService.BuildProfileFromDraft(draft);

// 检查字段
Assert.NotNull(profile.PluginsJson);
Assert.NotNull(profile.LanguagesJson);
Assert.InRange(profile.HardwareConcurrency, 1, 32);
Assert.Contains(profile.DeviceMemory, new[] { 4, 8, 16 });
```

### 2. 检查数据校验

```csharp
var errors = _antiDetectionService.ValidateProfile(profile);

// 应该没有错误
Assert.Empty(errors);

// 修改为不一致的数据
profile.Platform = "Win32";
profile.UserAgent = "Mozilla/5.0 (Macintosh; ...)";  // Mac UA

errors = _antiDetectionService.ValidateProfile(profile);

// 应该有错误
Assert.NotEmpty(errors);
Assert.Contains("Platform 是 Win32 但 UA 不包含 Windows", errors);
```

### 3. 检查浏览器注入

在浏览器控制台运行：

```javascript
console.log({
  webdriver: navigator.webdriver,  // undefined ✅
  plugins: navigator.plugins.length,  // 3 ✅
  languages: navigator.languages,  // ["zh-CN", "zh", "en-US", "en"] ✅
  hardwareConcurrency: navigator.hardwareConcurrency,  // 8 ✅
  deviceMemory: navigator.deviceMemory,  // 8 ✅
  maxTouchPoints: navigator.maxTouchPoints,  // 0 ✅
  connection: navigator.connection.effectiveType  // "4g" ✅
});
```

## 📁 修改的文件

1. **Models/FingerprintProfile.cs**
   - 添加 11 个防检测字段

2. **Services/AntiDetectionService.cs**（新建）
   - GenerateAntiDetectionData()
   - ValidateProfile()
   - 私有生成方法

3. **Services/BrowserEnvironmentService.cs**
   - 添加 AntiDetectionService 依赖
   - 在 BuildProfileFromDraft() 中调用生成

4. **Engine/PlaywrightController.cs**
   - 添加 GenerateAntiDetectScript() 方法
   - 修改 Client Hints 读取逻辑
   - 从指纹读取数据而非硬编码

5. **Infrastructure/Configuration/ServiceCollectionExtensions.cs**
   - 注册 AntiDetectionService

## 🎯 后续工作

### 必需（保存时校验）
- [ ] 在 NewBrowserEnvironmentWindow.Create_Click() 中调用 ValidateProfile()
- [ ] 显示校验错误给用户
- [ ] 阻止保存不一致的配置

### 可选（UI 增强）
- [ ] 在 UI 中显示防检测数据
- [ ] 允许用户手动编辑
- [ ] 提供"重新生成"按钮

### 可选（高级功能）
- [ ] 支持自定义生成规则
- [ ] 支持导入/导出防检测配置
- [ ] 支持批量校验

## ✅ 总结

### 完成的功能
- ✅ 数据模型扩展（11 个新字段）
- ✅ 自动生成服务（AntiDetectionService）
- ✅ 自动校验服务（ValidateProfile）
- ✅ 一键随机集成（BuildProfileFromDraft）
- ✅ PlaywrightController 读取数据
- ✅ Client Hints 读取数据
- ✅ 服务注册（DI）

### 待完成的功能
- ⏳ 保存时校验集成
- ⏳ UI 显示防检测数据
- ⏳ 数据库迁移（添加新列）

### 预期效果
- ✅ 一键随机时自动生成真实数据
- ✅ 数据之间保持一致性
- ✅ Cloudflare 验证通过率提高
- ✅ 无需手动配置防检测数据
