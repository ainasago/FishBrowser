# 字体与 WebGL 指纹方案（迭代 1 规划）

## 背景
- 目标：提供与真实环境一致且可控的字体与 GPU 指纹集，以支撑浏览器环境的伪装与还原。
- 方法：数据模型 + 种子库 + 服务 + UI 占位 + 日志；后续再补全选择对话框与运行时注入（Canvas/WebGL/Audio/Speech）。

## 字体（已完成回顾）
- 模型：`Font(Id, Name, AliasesJson, Category, Vendor, OsSupportJson, CreatedAt, UpdatedAt)`
- 扩展：`FingerprintMetaProfile/ Profile` 新增 `FontsMode(real|custom)`、`FontsJson`。
- 服务：`FontService.ImportSeedAsync / GetAll / Search / RandomSubset`。
- 种子：`assets/fonts/fonts_seed.json`（≥140 中英混合，覆盖 5 OS）；首启导入合并去重。
- UI：新建/编辑窗口加入“字体列表”占位（模式/编辑/换一换/摘要）。
- 日志：导入/搜索/随机均记录详细统计。

## WebGL / WebGPU（本迭代 1 目标）
### 数据模型
- `GpuInfo`
  - 基本：`Id, Vendor, Renderer, Adapter, DriverVersion`
  - 适配：`Api (WebGL|WebGPU), Backend (ANGLE-D3D11|OpenGL|Vulkan|Metal)`, `OsSupportJson`
  - 能力：`LimitsJson, ExtensionsJson`
  - 元：`CreatedAt, UpdatedAt`
- Meta/Profile 扩展
  - `WebGLImageMode: noise|real`
  - `WebGLInfoMode: ua_based|custom|disable_hwaccel|real`
  - `WebGLVendor, WebGLRenderer`
  - `WebGPUmode: match_webgl|real|disable`
  - `AudioContextMode: noise|real`
  - `SpeechVoicesEnabled: bool`

### 种子与覆盖范围
- 文件：`assets/webgl/webgl_gpu_seed.json`（≥300 组合，主流 + 小众）
- 覆盖：NVIDIA / AMD / Intel / Apple M / ARM Mali / Adreno / PowerVR；Windows/macOS/Linux/Android/iOS；ANGLE、OpenGL、Vulkan、Metal。
- 字段：`vendor, renderer, adapter, driverVersion, api, backend, limits, extensions, os`。

### 服务
- `GpuCatalogService`
  - `ImportSeedAsync()`：首启导入，合并去重
  - `SearchWebGL(query, vendor, os)`：查询/筛选
  - `RandomSubset(os, count)`：按 OS 随机
  - 全链路日志：输入参数、命中数、导入数

### UI 占位（新建/编辑浏览器）
- WebGL 图像：按钮“噪音/真实”
- WebGL Info：按钮“基于UA/自定义/关闭硬件加速/真实”；文本框“WebGL厂商/渲染”，右侧“换一换”
- WebGPU：按钮“基于WebGL匹配/真实/禁用”
- AudioContext：按钮“噪音/真实”
- SpeechVoices：按钮“开启/关闭”
- 预览：仅展示“生效值”摘要

### 运行时注入（后续迭代）
- 迭代 2：选择对话框（搜索/筛选/虚拟化/换一换）
- 迭代 3：编译与注入脚本占位
  - WebGL 信息与渲染挂钩（`getParameter`, `getExtension`）
  - WebGPU 能力探测拦截（可选）
  - AudioContext 噪音注入（hash 偏移）
  - SpeechVoices 列表开关

## 日志与验证
- 导入/查询/随机：记 `LogInfo/LogWarn/LogError`
- UI 操作：记 `EnvUI`
- 验证：
  - 首启导入成功（计数 > 0）
  - UI 占位渲染正常；预览展示生效值

## 里程碑
- 迭代 1（✅ 完成）：模型/服务/种子/占位/日志
  - GpuInfo 表 + DbSet
  - GpuCatalogService（导入/搜索/随机）
  - 60+ GPU 种子数据
  - NewBrowserEnvironmentWindow UI 占位
  - 全链路日志
  
- 迭代 2（待开始）：对话框与"换一换"策略
  - GpuPickerDialog：搜索/OS 筛选/类别筛选/虚拟化/多选/换一换
  - 集成到 NewBrowserEnvironmentWindow 与 TraitMetaEditorPage
  - Profile 编译时正确传递 WebGL Vendor/Renderer
  - 预览显示生效值
  
- 迭代 3（待开始）：运行时注入脚本与伪造
  - Canvas 指纹伪造：噪音注入（noiseSeed）
  - WebGL 伪造：
    - 图像模式：getParameter 拦截（vendor/renderer/extensions）
    - Info 模式：UA 匹配或自定义
  - WebGPU：能力探测拦截（可选）
  - AudioContext：采样率/噪音注入
  - SpeechVoices：列表开关（speechSynthesis.getVoices）
  - 实现方式：Playwright AddInitScript + Hook 注入
