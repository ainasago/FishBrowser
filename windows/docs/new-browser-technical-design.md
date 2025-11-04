# 新建指纹浏览器 - 技术方案

## 架构
- UI：NewBrowserEnvironmentWindow（WPF，分组/折叠 + 右侧预览）
- 服务：BrowserEnvironmentService（收集 UI → 组装 Traits → 调用 UA 生成器 + 指纹编译 → 保存）
- 模型：BrowserEnvironment（关联 Meta 与 Profile）
- 生成：FingerprintGeneratorService（已存在）+ UserAgentCompositionService（已存在）
- 启动：PlaywrightController 使用 CompiledContextOptions/Headers/Scripts 注入

## 数据模型 BrowserEnvironment（建议）
- 基本：Id, Name, Engine, OS, UaMode, CustomUserAgent
- 代理：ProxyMode, ProxyRefId, ProxyApiUrl, ProxyApiAuth, ProxyType, ProxyAccount
- 偏好：Region, Vendor, DeviceClass
- 高级：语言/时区/地理/分辨率/字体/Canvas/WebGL/WebRTC/WebGPU/Audio/Speech/媒体/隐私/安全/启动参数/Cookie/多开/通知/媒体拦截 等对应字段
- 关联：FingerprintMetaProfileId, FingerprintProfileId
- 其它：PreviewJson, CreatedAt, UpdatedAt

## Traits 映射（示例）
- UA/平台
  - browser.userAgent
  - browser.uach.platform / browser.uach.arch / browser.uach.model
  - browser.platform
- 语言/时区/地理
  - system.locale, browser.acceptLanguage
  - system.timezone
  - permissions.geolocation, geolocation.coords
- 分辨率/字体
  - device.viewport.width/height
  - fonts.list, fonts.fingerprint.mode
- WebRTC / Canvas / WebGL / WebGPU / Audio / Speech / 媒体设备
  - network.webrtc.mode, network.webrtc.iceServers
  - graphics.canvas.mode, graphics.canvas.noiseSeed
  - graphics.webgl.image.mode, graphics.webgl.vendor, graphics.webgl.renderer, graphics.webgl.info.mode
  - graphics.webgpu.mode
  - audio.context.mode
  - media.speechVoices.enabled, media.devices.mode
- 设备资源 / 隐私安全
  - device.hardwareConcurrency, device.deviceMemory
  - privacy.dnt, device.battery.mode, network.portscan.protect, network.tls.policy
- 启动/头
  - browser.launch.args
  - headers.order, headers.extra

## 服务流程（序列）
1) 收集 UI → 组装 Traits 字典（支持“跟随IP/匹配/自定义”）
2) 调用 UserAgentCompositionService（Chrome+Windows）生成 UA + 相关 traits（若 UaMode 是随机/匹配）
3) 合并语言/时区/地理/viewport 等 traits
4) 调用 FingerprintGeneratorService.GenerateFromMeta(meta, name, seed)
5) 保存 FingerprintProfile，建立 BrowserEnvironment 关联
6) 预览卡片：提取核心 traits，生成稳定哈希作为指纹ID，写入 PreviewJson

## UI 设计
- 顶部：引擎、OS、UA 模式 + 代理方式/类型/账号
- 中部：高级设置分组（Canvas/WebGL/WebRTC/Audio/Speech/Fonts/Viewport/Device/Privacy/Security/Headers/LaunchArgs）
- 右侧：预览卡（核心摘要），按钮：创建/取消
- 多数控件使用 ComboBox + 可编辑 + 自动补全（复用 TraitOptions）

## 集成点
- DI：注册 BrowserEnvironmentService（Scoped）
- DB：DbContext 增加 DbSet<BrowserEnvironment> 与迁移
- 生成/启动：
  - Generate：BrowserEnvironmentService → FingerprintGeneratorService
  - Launch：通过 FingerprintService/PlaywrightController 应用 Compiled* 产物

## 扩展点
- 多内核/移动端：扩展 UA 生成器与 traits 映射
- TLS/JA3：代理/mitm 层对接
- 团队设置：统一策略覆盖

## 测试
- 单元：Traits 组装、UA 合并逻辑
- 集成：创建环境 → 启动浏览器 → 验证 UA/Locale/Timezone/Viewport/Canvas/WebGL/WebRTC 生效
