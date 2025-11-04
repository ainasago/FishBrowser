# 新建指纹浏览器 - 使用手册

## 快速开始

1. **打开窗口**
   - 启动应用
   - 侧边栏点击"新建指纹浏览器"

2. **配置环境（最少输入）**
   - **名称**：留空自动生成（Env-{时间戳}）
   - **浏览器**：Chrome（默认，智能匹配）
   - **操作系统**：Windows（默认）
   - **User Agent**：
     - 随机（默认）：自动生成 Chrome+Windows UA 及相关 traits
     - 匹配：基于 OS 智能匹配
     - 自定义：粘贴自定义 UA 字符串
   - **代理**：无（默认，可选引用已有代理或 API 提取）
   - **区域**：留空默认 CN，或填 US
   - **设备类/厂商**：可选，用于过滤 TraitOptions
   - **高级设置**：
     - 分辨率：真实（默认 1366x768）
     - WebRTC：隐藏（默认）
     - Canvas：噪音（默认）
     - 硬件并发/内存：默认 10/8

3. **创建环境**
   - 点击"创建环境"
   - 服务自动：
     - 组装 Traits（UA、locale、timezone、viewport、Canvas、WebGL、WebRTC 等）
     - 调用 `UserAgentCompositionService` 生成 UA 相关 traits（若随机/匹配）
     - 调用 `FingerprintGeneratorService` 编译为 `FingerprintProfile`（包含 CompiledContextOptions/Headers/Scripts）
     - 保存 `BrowserEnvironment`（关联 MetaProfile 与 Profile）
   - 右侧预览显示：
     - 指纹ID（22位短哈希）
     - OS、UA、语言、时区、分辨率
     - Canvas/WebGL/WebRTC/Audio/Speech 等模块策略
   - "启动浏览器（测试）"按钮启用

4. **验证指纹**
   - 点击"启动浏览器（测试）"
   - 自动：
     - 启动 Playwright（Chromium，非 headless，可见窗口）
     - 应用 `CompiledContextOptions`（UA/locale/timezone/viewport）
     - 注入 `CompiledScripts`（Canvas/WebGL/WebRTC/Navigator 补丁）
     - 打开 https://httpbin.org/headers（查看请求头）
     - 执行 JS 读取 `navigator.userAgent/platform/language/hardwareConcurrency/deviceMemory/webdriver`
   - 右侧预览追加"=== 浏览器验证结果 ==="（JSON）
   - 浏览器窗口保持打开，可手动导航到其他指纹检测站点（如 browserleaks.com）
   - 关闭后自动释放资源

## 使用场景

- **采集任务前测试**：快速创建环境并验证指纹生效
- **多环境管理**：为不同区域/UA/代理组合创建独立环境
- **反检测对比**：创建多个环境（噪音 vs 真实、隐藏 vs 禁用 WebRTC）并对比检测结果

## 高级用法

- **微调 Traits**：
  - 创建环境后，可在"元配置编辑器"中打开关联的 `MetaProfile`
  - 修改任意 TraitKey/Value（支持自动补全、过滤、排序、高亮）
  - 保存后重新生成 `FingerprintProfile`（调用"生成 Profile"按钮）
- **批量生成**：
  - 使用"批量生成"窗口，基于 Preset + 上下文（Region/DeviceClass/Vendor）生成 N 个环境
  - 勾选"使用 UA 生成器覆盖 UA 相关键"自动接入 Chrome+Windows UA
- **代理接入**（后续）：
  - 选择"引用已有代理"并在代理池中配置
  - 或"自定义 API 提取"填写 URL+认证（占位实现）

## 技术细节

- **指纹 ID 生成**：
  - 基于核心 traits（UA/locale/timezone/viewport/webgl vendor/renderer）SHA256 哈希
  - Base64 编码并截取前 22 位，保证全球唯一性
- **Traits → Playwright 映射**：
  - `browser.userAgent` → `BrowserNewContextOptions.UserAgent`
  - `system.locale` → `Locale`
  - `system.timezone` → `TimezoneId`
  - `device.viewport.width/height` → `ViewportSize`
  - `headers.order` → `ExtraHTTPHeaders`（按顺序注入）
  - `graphics.canvas.noiseSeed/graphics.webgl.vendor/renderer` → `CompiledScriptsJson`（注入脚本）
- **启动参数**：
  - `--disable-blink-features=AutomationControlled`（隐藏自动化特征）
  - 可在 LaunchArgs 字段追加自定义 Chromium args

## 故障排除

- **启动失败：Playwright 未安装**
  - 确保已运行 `playwright install chromium`
- **指纹未生效**：
  - 检查 `FingerprintProfile.CompiledContextOptionsJson/CompiledScriptsJson` 是否非空
  - 查看日志（LogsView）确认注入脚本执行
- **代理不可用**：
  - 当前版本仅支持"引用已有代理"，确保代理池中存在可用代理

## 下一步功能

- 多内核支持（Firefox/Edge）
- 移动端 UA（Android/iOS）
- TLS/JA3 指纹控制（代理扩展）
- 字体/SpeechVoices/AudioContext 数据集补全
- 团队策略联动（跟随团队设置）
