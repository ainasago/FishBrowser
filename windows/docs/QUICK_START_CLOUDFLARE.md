# Cloudflare 绕过 - 快速开始指南

## ⚡ 5 步创建支持 Cloudflare 绕过的浏览器

### 步骤 1：打开新建浏览器窗口
点击"浏览器管理" → "新建浏览器"

### 步骤 2：选择"从预设生成"模式 ⭐ 重要！
在"指纹配置"区域，选择 **"从预设生成"** 单选按钮

**不要选择"选择已有"！** 旧 Profile 没有防检测数据！

### 步骤 3：点击"一键随机"（可选）
- 如果想自定义配置，可以点击"一键随机"多次，直到满意
- 或者直接使用默认配置

### 步骤 4：填写环境名称
输入一个有意义的名称，例如："Cloudflare-Test"

### 步骤 5：点击"创建"
系统会：
1. 生成随机配置草稿
2. 创建新的 FingerprintProfile
3. **自动生成防检测数据**（Plugins、Languages、Client Hints 等）
4. 保存到数据库
5. 创建浏览器环境

## ✅ 验证成功

### 查看日志

创建成功后，查看日志应该包含：

```
[EnvUI] Creating new environment with generated profile from random draft
[BrowserEnvironmentService] Generated anti-detection data for profile: Profile-xxx
[BrowserEnvironmentService]   - Plugins: 300+ chars
[BrowserEnvironmentService]   - Languages: ["zh-CN","zh","en-US","en"]
[BrowserEnvironmentService]   - HardwareConcurrency: 8
[BrowserEnvironmentService]   - DeviceMemory: 8
[BrowserEnvironmentService]   - Connection: 4g (RTT: 45ms, Downlink: 12.5 Mbps)
[BrowserEnvironmentService]   - SecChUa: "Chromium";v="120", ...
[EnvUI] Created environment xxx with NEW profile xxx (anti-detection data included)
```

### 启动浏览器

启动新创建的浏览器，查看日志：

```
[PlaywrightController] ========== Browser Configuration Summary ==========
[PlaywrightController] --- Anti-Detection Data ---
[PlaywrightController] PluginsJson: ✅ 312 chars
[PlaywrightController] LanguagesJson: ✅ ["zh-CN","zh","en-US","en"]
[PlaywrightController] HardwareConcurrency: 8
[PlaywrightController] DeviceMemory: 8
[PlaywrightController] MaxTouchPoints: 0
[PlaywrightController] ConnectionType: 4g
[PlaywrightController] --- Client Hints ---
[PlaywrightController] SecChUa: ✅ "Chromium";v="120", ...
[PlaywrightController] SecChUaPlatform: ✅ "Windows"
[PlaywrightController] SecChUaMobile: ✅ ?0
```

**所有字段都应该显示 ✅**

### 测试 Cloudflare

在浏览器中访问：
- https://nowsecure.nl
- https://www.cloudflare.com/cdn-cgi/trace

**预期结果**：
- 首次可能出现 "Checking your browser" 页面
- 2-5 秒后自动通过 ✅
- 后续访问直接放行（持久化 cookie）

## ❌ 常见错误

### 错误 1：选择了"选择已有"模式

**症状**：
```
[PlaywrightController] PluginsJson: ❌ NOT SET
[PlaywrightController] ⚠️ WARNING: Anti-detection data is missing!
```

**解决**：
- 删除旧环境
- 重新创建，选择"**从预设生成**"模式

### 错误 2：使用了旧的 Profile

**症状**：
```
[EnvUI] Created environment xxx with existing profile Meta-221948-Profile-222011
```

**解决**：
- 不要选择"选择已有"
- 选择"**从预设生成**"

### 错误 3：没有看到防检测数据生成日志

**症状**：
- 日志中没有 `Generated anti-detection data for profile`

**解决**：
- 确认选择了"从预设生成"模式
- 重新编译并运行

## 🎯 关键点总结

1. ✅ **必须**选择"从预设生成"模式
2. ❌ **不要**选择"选择已有"模式
3. ✅ 创建后查看日志确认防检测数据生成
4. ✅ 启动浏览器查看配置摘要
5. ✅ 测试 Cloudflare 站点

## 🔧 技术细节

### 数据生成流程

```
点击"创建"
  ↓
BuildRandomDraft()  // 生成随机配置草稿
  ↓
BuildProfileFromDraft()  // 从草稿生成 Profile
  ↓
AntiDetectionService.GenerateAntiDetectionData()  // 生成防检测数据
  ↓
保存 Profile 到数据库
  ↓
创建 BrowserEnvironment 并绑定 Profile
```

### 生成的防检测数据

- **Plugins**：根据 UA 判断浏览器类型，生成对应插件列表
- **Languages**：根据 Locale 和 AcceptLanguage 生成语言列表
- **HardwareConcurrency**：加权随机（2/4/6/8/12/16 核）
- **DeviceMemory**：加权随机（4/8/16 GB）
- **MaxTouchPoints**：桌面 0，移动 1-5
- **Connection**：加权随机（4g/wifi/3g）
- **Client Hints**：根据 UA 和 Platform 生成

### 为什么旧 Profile 不能用？

1. **数据库结构已更新**
   - 添加了 11 个新字段
   - 旧记录这些字段都是 NULL

2. **只有新创建的 Profile 才有数据**
   - 通过 `BuildProfileFromDraft()` 创建
   - 自动调用 `AntiDetectionService.GenerateAntiDetectionData()`

3. **Cloudflare 检测这些字段**
   - 缺少 plugins → 检测为自动化
   - 缺少 languages → 检测为自动化
   - 缺少 Client Hints → 检测为自动化

## 📚 相关文档

- `CLOUDFLARE_BYPASS_DATA_DRIVEN.md` - 完整架构文档
- `CLOUDFLARE_TROUBLESHOOTING.md` - 问题排查指南

## 💡 提示

- 每次创建新环境都会生成新的 Profile
- 可以创建多个环境测试不同配置
- 建议定期更新（重新创建）以获得最新的防检测数据
- 如果 Cloudflare 仍然失败，尝试切换不同的 Engine（chrome/firefox/webkit）
