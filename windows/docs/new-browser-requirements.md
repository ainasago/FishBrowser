# 新建指纹浏览器 - 需求文档

## 背景与目标
- 通过“新建指纹浏览器”让用户以最少输入、更多点击完成一个可用的“浏览器环境（Browser Environment）”。
- 该环境可直接用于 Playwright 启动，具备完整指纹伪装（UA/语言/时区/地理/Canvas/WebGL/WebRTC/Audio 等）与代理设置。
- 复用现有 TraitDefinitions/Options、TraitsDatasetSeeder、UserAgentCompositionService 与 FingerprintGeneratorService 的编译管线。

## 范围（Scope）
- 支持 Chromium 内核（优先 Chrome+Windows），Firefox/移动端为后续迭代。
- 支持代理引用与自定义 API 提取（占位实现，首期以引用已有代理为主）。
- 支持“智能匹配”与“跟随IP/匹配/自定义”的便捷选择，减少手工输入。
- 批量生成：复用现有 BatchGeneration 流程（已加入 UA 生成开关）。

## 非目标（Out of Scope）
- TLS/JA3 深度指纹控制（后续通过代理/mitm 扩展）。
- 移动端全量覆盖（先做 Windows 桌面）。
- 团队级策略与多开策略联动（先记录字段，后续实现）。

## 角色与使用场景
- 采集工程师：快速创建多个指纹环境，启动自动化任务。
- 反检测测试：对比多种指纹策略（噪音/真实/关闭）。

## 功能需求
- 浏览器与操作系统
  - 浏览器内核：Chrome（智能匹配/基于 UA/自定义）。
  - 操作系统：Windows（默认），Android/iOS/macOS（迭代）。
  - UA：随机/基于 OS+引擎匹配/自定义文本粘贴。
- 代理信息
  - 方式：引用已有代理 / 自定义 API 提取（URL+认证）。
  - 类型、账号信息：按需要显示输入项。
- 高级设置（均为点击/下拉/开关）
  - 语言：跟随IP / 匹配 / 自定义（accept-language+locale）。
  - 时区：跟随IP / 匹配 / 自定义。
  - 地理位置：询问/禁用/跟随IP/自定义（lat/lon/accuracy/permissions）。
  - 分辨率：真实/自定义（viewport.width/height）。
  - 字体列表：真实/自定义；字体指纹：噪音/真实。
  - WebRTC：转发/隐藏/替换/真实/禁用；iceServers 可选。
  - Canvas：噪音/真实；WebGL 图像：噪音/真实。
  - WebGL Info：基于UA/自定义/关闭硬件加速/真实；Vendor/Renderer 选择。
  - WebGPU：基于WebGL/真实/禁用。
  - AudioContext：噪音/真实；SpeechVoices：开启/关闭；媒体设备：噪音/真实。
  - 硬件并发数：默认10；设备内存：默认8GB。
  - DNT：默认/开启/关闭；电池：噪音/禁止/真实。
  - 端口扫描保护：开启/关闭；TLS 协议：默认/自定义（记录策略）。
  - 启动参数：Chromium args 文本；Cookie 同步：按环境/按用户；多开设置：跟随团队/开启/关闭。
  - 网页通知：询问/禁止/允许；媒体拦截：跟随团队/开启/关闭（图片/视频/声音）。
- 预览与指纹ID
  - 实时预览摘要（OS、UA、语言、时区、地理、分辨率、关键模块策略）。
  - 指纹ID：核心 traits 的稳定哈希，全球唯一。

## 数据与外部依赖
- 复用 `TraitDefinitions`/`TraitOptions`；需要补齐：字体、SpeechVoices、AudioContext、TLS 策略。
- Geo 解析：可通过代理出口 IP 调用第三方（占位），无则回退默认区域。

## 验收标准（MVP）
- 能创建一个 BrowserEnvironment，产出 FingerprintProfile，可成功启动 Playwright 并应用伪装。
- 支持 UA 生成、语言/时区/locale/viewport 与 Canvas/WebGL/WebRTC 的基础覆盖。
- 主要项均为点选/下拉，少量输入（例如 UA 粘贴）。

## 风险与缓解
- 代理 API 提取差异大 → 首期仅支持“引用已有代理”。
- Geo 依赖不可用 → 回退默认 CN/US 集合。
- XAML 复杂度提升 → 分组折叠 + 右侧预览，保持交互友好。
