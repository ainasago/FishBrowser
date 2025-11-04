# 新建指纹浏览器 - 实施计划

## 里程碑
- M1（本周）：
  - 模型与迁移：BrowserEnvironment 实体 + DbContext + 迁移
  - 服务：BrowserEnvironmentService（组装 Traits，调用 UA 生成与指纹编译）
  - UI：NewBrowserEnvironmentWindow（MVP：常用项+预览）
  - 启动：从环境创建并启动一个可用的 Playwright 上下文
- M2（下周）：
  - 扩充数据集：字体、SpeechVoices、AudioContext、TLS 策略
  - 高级脚本：WebRTC/Audio/Speech 注入增强
  - 代理 API 提取（基础实现）
- M3：
  - 多内核/移动端扩展
  - TLS/JA3 指纹（代理/mitm 对接）
  - 团队策略联动

## 任务分解
1) NEW-BROWSER-MODEL
   - 新建 BrowserEnvironment.cs
   - DbContext: 添加 DbSet<BrowserEnvironment>
   - 迁移：生成并应用
2) NEW-BROWSER-SVC
   - 组装 Traits 字典（UI → traits）
   - 调用 UserAgentCompositionService + FingerprintGeneratorService
   - 生成并保存 FingerprintProfile，返回 Id
3) NEW-BROWSER-UI
   - NewBrowserEnvironmentWindow（分组控件 + 预览卡）
   - 关键控件复用自动补全（TraitOptions）
4) NEW-BROWSER-INTEGRATION
   - 从环境启动 Playwright：应用 CompiledContextOptions/Headers/Scripts
   - 代理支持与启动参数

## 进度与验收
- 每个子任务提交 PR，自检通过 + 运行验证
- 验收：能从 UI 创建环境并成功打开页面，核心伪装项生效

## 风险与缓解
- XAML 复杂度：分步交付（先 MVP 分组 + 预览）
- 代理 API 差异：抽象接口，先落“引用已有代理”
- Geo 依赖：不可用则回退默认区域
