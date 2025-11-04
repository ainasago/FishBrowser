# 一键创建随机浏览器功能

## 功能概述

新增了两个按钮到浏览器管理页面：
1. **添加浏览器** - 跳转到浏览器创建页面，手动配置浏览器参数
2. **一键创建随机浏览器** - 自动生成一个具有随机配置的浏览器环境

## 重要说明

**本功能完全复用了 WPF 应用中的随机生成逻辑**，确保 Web 端和桌面端生成的浏览器指纹具有相同的质量和真实性。

使用的核心服务：
- `ChromeVersionDatabase` - 真实的 Chrome 版本号数据库
- `GpuCatalogService` - 真实的 GPU 配置数据库
- `FontService` - 操作系统字体数据库
- `AntiDetectionService` - 防检测数据生成服务

## 功能特点

### 随机生成的配置项

一键创建随机浏览器时，系统会自动生成以下随机配置：

#### 1. 浏览器引擎
- UndetectedChrome（推荐，绕过检测能力强）
- Chromium
- Firefox

#### 2. 操作系统
- Windows
- macOS
- Linux

#### 3. User Agent
根据选择的操作系统，从真实的 User Agent 池中随机选择：
- **Windows**: Chrome 119-121, Firefox 120-121
- **macOS**: Chrome 119-120, Safari 17.1, Firefox 121
- **Linux**: Chrome 119-120, Firefox 120-121

#### 4. 屏幕分辨率
从常见分辨率中随机选择：
- 1920x1080 (Full HD)
- 1366x768
- 1440x900
- 1536x864
- 1600x900
- 2560x1440 (2K)
- 1280x720 (HD)

#### 5. 时区
支持全球主要时区：
- Asia/Shanghai (中国)
- Asia/Hong_Kong (香港)
- Asia/Tokyo (日本)
- America/New_York (美国东部)
- America/Los_Angeles (美国西部)
- Europe/London (英国)
- Europe/Paris (法国)
- Australia/Sydney (澳大利亚)

#### 6. 语言/地区
- zh-CN (简体中文)
- zh-TW (繁体中文)
- en-US (美式英语)
- en-GB (英式英语)
- ja-JP (日语)
- ko-KR (韩语)
- de-DE (德语)
- fr-FR (法语)

#### 7. 硬件配置
- **CPU 核心数**: 4, 6, 8, 12, 16 核
- **内存**: 4GB, 8GB, 16GB, 32GB
- **触摸点**: 0（桌面设备）

#### 8. WebGL 配置
随机选择真实的 GPU 配置：
- **Vendor**: Intel, NVIDIA, AMD
- **Renderer**: 
  - Intel UHD Graphics 630
  - NVIDIA GeForce GTX 1660 Ti
  - AMD Radeon RX 580
  - Intel Iris OpenGL Engine
  - NVIDIA GeForce RTX 3060

#### 9. 防检测配置
- Canvas 指纹: 噪声模式
- WebGL 图像: 噪声模式
- WebGL 信息: 基于 UA
- Audio Context: 噪声模式
- WebRTC: 隐藏模式

## 使用方法

### 通过 Web 界面

1. 登录浏览器管理系统
2. 进入"浏览器管理"页面
3. 点击页面右上角的 **"一键创建随机浏览器"** 按钮
4. 确认创建提示
5. 系统自动生成并保存随机浏览器环境
6. 页面自动刷新，显示新创建的浏览器

### 通过 API

```bash
POST /api/browsers/random
Authorization: Bearer {token}
```

**响应示例：**
```json
{
  "success": true,
  "message": "随机浏览器 '随机浏览器 20251104-102600' 创建成功",
  "data": {
    "id": 123,
    "name": "随机浏览器 20251104-102600",
    "engine": "UndetectedChrome",
    "os": "windows"
  }
}
```

## 技术实现

### 文件结构

```
web/FishBrowser.Api/
├── Services/
│   └── RandomBrowserGenerator.cs      # 随机浏览器生成器
├── Controllers/
│   └── BrowsersController.cs          # API 控制器（添加 /random 端点）

web/FishBrowser.Web/
├── Controllers/
│   └── BrowserController.cs           # Web 控制器（代理到 API）
└── Views/Browser/
    └── Index.cshtml                   # 浏览器列表页面（添加按钮）
```

### 核心类：BrowserRandomGenerator

**重要：本服务完全复用 WPF 应用的 `BrowserEnvironmentEditorDialog.GenerateFullyRandomFingerprint()` 逻辑**

```csharp
public class BrowserRandomGenerator
{
    private readonly ChromeVersionDatabase _chromeVersionDb;
    private readonly GpuCatalogService _gpuCatalog;
    private readonly FontService _fontSvc;
    private readonly AntiDetectionService _antiDetectSvc;
    
    // 生成完全随机的浏览器环境（复用 WPF 逻辑）
    public async Task<(BrowserEnvironment browser, FingerprintProfile profile)> GenerateRandomBrowserAsync()
}
```

**与 WPF 的一致性**：
- 使用相同的操作系统选择逻辑（包括移动端）
- 使用相同的分辨率选择策略
- 使用 `ChromeVersionDatabase.GetRandomVersion()` 获取真实版本号
- 使用 `GpuCatalogService.RandomSubsetAsync()` 获取真实 GPU 配置
- 使用 `FontService.RandomSubsetAsync()` 获取真实字体列表（30-50个）
- 使用 `AntiDetectionService.GenerateAntiDetectionData()` 生成防检测数据

### API 端点

**路由**: `POST /api/browsers/random`

**功能**:
1. 调用 `BrowserRandomGenerator.GenerateRandomBrowserAsync()` 生成浏览器环境和指纹配置
2. 保存指纹配置到数据库
3. 关联指纹配置到浏览器环境
4. 保存浏览器环境到数据库
5. 返回创建结果

**实现细节**（完全复用 WPF 逻辑）：
- 随机选择操作系统（Windows/MacOS/Linux/Android/iOS）
- 根据操作系统选择合适的分辨率
- 从 `ChromeVersionDatabase` 获取真实的 Chrome 版本号
- 从 `GpuCatalogService` 获取真实的 GPU 配置
- 从 `FontService` 随机选择 30-50 个字体
- 使用 `AntiDetectionService` 生成 Languages、Plugins、Sec-CH-UA 等防检测数据
- 根据操作系统生成真实的硬件配置（CPU核心数、内存、触摸点）

## 优势

1. **快速创建**: 一键生成，无需手动配置多个参数
2. **真实性高**: 使用真实的 User Agent、GPU 配置等
3. **多样性**: 每次生成的配置都不同，避免指纹重复
4. **防检测**: 自动配置多种防检测措施
5. **即用即走**: 创建后立即可以启动使用

## 适用场景

1. **快速测试**: 需要快速创建浏览器环境进行测试
2. **批量创建**: 需要创建多个不同配置的浏览器
3. **指纹多样化**: 避免使用相同配置被网站识别
4. **新手友好**: 不熟悉配置参数的用户可以快速上手

## 注意事项

1. 随机生成的浏览器名称格式为：`随机浏览器 YYYYMMDD-HHMMSS`
2. 每个随机浏览器都会自动创建对应的指纹配置
3. 默认启用会话持久化（EnablePersistence = true）
4. 默认使用有头模式（Headless = false）
5. 代理模式默认为 none，可以后续手动编辑添加代理

## 后续优化建议

1. 支持批量创建（一次创建多个随机浏览器）
2. 支持指定某些参数（如固定操作系统或引擎）
3. 支持从模板创建（基于现有浏览器生成变体）
4. 添加更多 User Agent 和 GPU 配置
5. 支持移动设备配置（iOS、Android）
