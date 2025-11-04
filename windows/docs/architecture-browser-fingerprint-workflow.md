# 浏览器管理 × 指纹配置 × 元数据管理：流程与关系

## 核心实体
- BrowserEnvironment（浏览器环境）
  - 引用 FingerprintProfile（必选，运行时注入 Playwright Context）
  - 可选分组、会话持久化参数
- FingerprintProfile（指纹配置实例，已编译可运行）
  - 来源：Preset/MetaProfile 编译或人工微调
- FingerprintPreset（预设模板）
- FingerprintMetaProfile（元配置，可被编译为 FingerprintProfile）

## 标准流程（推荐）
1) 元数据管理
   - 预设管理 / 元配置编辑器 / 批量生成
   - 产出一个或多个 FingerprintProfile
2) 指纹配置
   - 浏览/微调已生成的 FingerprintProfile
3) 浏览器管理
   - 新建/编辑 BrowserEnvironment 时“选择一个现有 FingerprintProfile”并绑定
   - 启动环境，按 Profile 注入并运行

## 快捷流程（效率优先）
- 浏览器管理 → 新建 → 二选一：
  - 从现有 FingerprintProfile 列表选择
  - 从某预设快速生成一个新的 FingerprintProfile 并绑定（后续在“指纹配置”可微调）

## 关系图
```
Preset ──▶ MetaProfile ──▶ (Compile) ──▶ FingerprintProfile ──▶ (Bind) ──▶ BrowserEnvironment ──▶ Playwright
```

## 需要的产品改进（本次实现）
- 浏览器新建/编辑弹窗：增加“选择现有 FingerprintProfile”的 UI（后续补充“从预设生成”）
- 服务层：BrowserEnvironmentService 增加 attach/detach/switch profile 方法
- 管理列表：显示 FingerprintProfile 名称/更新时间；提供“更换指纹配置…”操作

## 决策与术语
- BrowserEnvironment 必须显式绑定 FingerprintProfile
- 指纹配置的来源与编译逻辑在“元数据管理”，浏览器管理只负责绑定与运行
- 通过更换/重生成 Profile 达到快速切换运行指纹的目的

## 后续增强（未来）
- 在新建弹窗加入“从预设生成并绑定”快速路径
- 支持一键为某分组环境批量切换 Profile
- 为 Profile 版本化与回滚提供支持
