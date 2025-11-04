# 指纹元数据库设计与实施计划

## 目标
- 构建可扩展、可更新的指纹元数据库（Fingerprint Meta-DB），覆盖浏览器/设备/网络/系统等多维度特征。
- 提供精细化配置、依赖/冲突校验、随机生成与预设模板；平滑集成到 Playwright 注入链路。

## 范围
- Trait Catalog（特征目录）与 Generator Engine（生成引擎）。
- 版本化、导入导出、远程更新与回滚、迁移。

## 数据模型（EF Core）
- TraitCategory(Id, Name, Description, Order)
- TraitDefinition(Id, Key, DisplayName, CategoryId, ValueType, DefaultValueJson, ConstraintsJson, DependenciesJson, ConflictsJson, DistributionsJson, DocUrl, Notes, IsExperimental, VersionIntroduced)
- TraitOption(Id, TraitDefinitionId, ValueJson, Label, Weight, Region, Vendor, DeviceClass)
- TraitGroupPreset(Id, Name, Description, Tags, Version, ItemsJson)
- FingerprintMetaProfile(Id, Name, Version, Source, BasePresetId?, TraitsJson, LockFlagsJson, RandomizationPlanJson)
- CatalogVersion(Id, Version, PublishedAt, Changelog, SourceUrl, Signature)
- 兼容现有 FingerprintProfile：增加 MetaProfileId、CompiledScriptsJson、CompiledHeadersJson、LastGeneratedAt、GeneratorVersion

## Trait 目录（示例）
- Browser: UserAgent, UA-CH, Accept-Language, Platform, DoNotTrack, Webdriver
- Device: DeviceMemory, HardwareConcurrency, TouchSupport, Screen/Viewport, PixelRatio, Fonts
- Graphics: Canvas(扰动种子/算法), WebGL Vendor/Renderer/Extensions, WebGPU(预留)
- Audio: AudioContext 指纹、采样率、漂移参数
- Network: WebRTC、DNS 泄露、Connection.downlink/effectiveType
- Storage/Permissions: Local/Session/Cookie、权限模拟（Geo/Camera/Mic/Notifications）
- Privacy/Headers: 请求头模板（顺序/大小写/空格）、Referer/Origin/UA-CH 策略
- System: Timezone、Locale、Intl、Navigator 属性
- Plugins/Sensors: Plugin 列表、Battery、DeviceOrientation、媒体设备

## 生成引擎（Generator Engine）
- 输入：Preset、RandomizationPlan、锁定键、上下文（Region、DeviceClass）
- 流程：依赖解析→取值（锁定>Preset>OptionPool>默认）→约束校验→联动修复→编译产物
- 分布：Uniform/Normal/WeightedDiscrete/Conditional
- 随机种子：可复现
- 冲突/依赖：如 UA 与 UA-CH 一致性、WebGL 与 GPU 家族匹配、Region 影响语言与字体

## UI/UX（WPF）
- Catalog 浏览器：分类树+列表、搜索、筛选（必需/实验/可随机）
- Trait 编辑器：动态表单、依赖/冲突提示、即时报错
- Preset 管理：官方与自定义、差异对比、复制发布
- 随机生成：选择 Preset → 配置随机策略 → 预览 → 应用为 FingerprintProfile
- 验证面板：一键校验报告
- 批量工具：批量生成/导出/去重

## 导入/导出/更新
- JSON Schema 版本化（schemaVersion, catalogVersion, exportedAt）
- 远程 Feed：签名校验、回滚
- 迁移：Key 重命名、值域映射、废弃处理

## Playwright 集成
- 产物：Headers、Scripts（Canvas/WebGL/Audio/Intl/Navigator）、ContextOptions、Permissions/Storage
- 时机：创建 context 前合成 Headers/Options；Page 创建后 addInitScript
- 兼容：FingerprintProfile 引用/展开编译产物

## 里程碑
- M3（2–3 周）：表结构、核心 30+ traits、生成器 V1、基础 UI、可注入
- M4（3–4 周）：扩展到 60+ traits、条件分布/批量生成、远程 Feed、Preset 管理、高级编辑器
- M5（2 周）：稳定性/性能、文档与可观测性、注入回归覆盖

## 风险与对策
- 依赖复杂：DSL+拓扑排序；冲突解释
- 浏览器变动：Catalog 版本+迁移
- 性能：预编译和缓存；批量异步限流
- 维护：远程 Feed+自动化测试（规则、注入）

## 本次提交（M3-Phase1 目标）
- 新增实体与 DbSet，OnModelCreating 关系与索引
- 预留导入/导出接口形状（后续实现）
- 种子数据计划：Chrome-120/Windows/CN 基础集（后续提交）
