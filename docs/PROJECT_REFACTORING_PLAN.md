# 项目重构计划：WebScraperApp 到 FishBrowser.Core/Api 的迁移

## 1. 项目概述

本重构计划旨在将现有的 `WebScraperApp` Windows桌面应用程序重构为三层架构：
- **FishBrowser.Core**: 核心业务逻辑和数据处理层
- **FinshBrowser.Api**: API接口层，提供RESTful服务
- **WebScraperApp**: Windows WPF界面层，调用API服务

## 2. 重构目标

1. **分离关注点**: 将业务逻辑与UI分离，提高代码可维护性
2. **提高可测试性**: 核心逻辑独立于UI，便于单元测试
3. **支持多平台**: 核心逻辑可用于Web、移动端等其他平台
4. **API化**: 通过API提供功能，便于未来扩展

## 3. 项目结构规划

### 3.1 FishBrowser.Core (核心层)
```
FishBrowser.Core/
├── Application/          # 应用服务层
│   ├── DTOs/            # 数据传输对象
│   ├── Mappers/         # 对象映射
│   └── Services/        # 应用服务
├── Domain/              # 领域层
│   ├── Entities/        # 实体
│   ├── Repositories/    # 仓储接口
│   ├── Services/        # 领域服务
│   └── ValueObjects/    # 值对象
├── Infrastructure/      # 基础设施层
│   ├── Configuration/   # 配置
│   └── Data/           # 数据访问实现
├── Engine/             # 引擎层
│   ├── FingerprintManager.cs
│   ├── HtmlParser.cs
│   ├── PlaywrightController.cs
│   └── TaskScheduler.cs
├── Models/             # 数据模型
├── Services/           # 核心服务
├── assets/             # 资源文件
└── FishBrowser.Core.csproj
```

### 3.2 FinshBrowser.Api (API层)
```
FinshBrowser.Api/
├── Controllers/         # API控制器
├── Middleware/          # 中间件
├── Configuration/       # 配置
├── Program.cs          # 应用程序入口
├── appsettings.json    # 配置文件
└── FinshBrowser.Api.csproj
```

### 3.3 WebScraperApp (UI层)
```
WebScraperApp/
├── Views/              # 视图
├── ViewModels/         # 视图模型
├── Presentation/       # 表示层
├── Assets/             # 资源
├── Services/           # UI特定服务
├── MainWindow.xaml     # 主窗口
├── App.xaml           # 应用程序
└── WebScraperApp.csproj
```

## 4. 文件迁移计划

### 4.1 需要迁移到 FishBrowser.Core 的文件/文件夹

#### 核心业务逻辑
- `Application/` - 应用服务层
- `Domain/` - 领域层
- `Infrastructure/` - 基础设施层
- `Engine/` - 引擎层
- `Models/` - 数据模型
- `Services/` - 核心服务
- `Data/` - 数据访问层
- `GlobalUsings.cs` - 全局引用

#### 资源文件
- `assets/Scripts/` - JavaScript脚本
- `assets/fonts/` - 字体文件
- `assets/webgl/` - WebGL相关文件
- `assets/randomization/` - 随机化数据
- `assets/templates/` - 模板文件

### 4.2 保留在 WebScraperApp 的文件/文件夹

#### UI相关
- `Views/` - 所有视图文件
- `ViewModels/` - 视图模型
- `Presentation/` - 表示层
- `MainWindow.xaml` - 主窗口
- `MainWindow.xaml.cs` - 主窗口代码
- `App.xaml` - 应用程序
- `App.xaml.cs` - 应用程序代码

#### UI特定资源
- `assets/app_icon.ico` - 应用图标
- `assets/fingerprint_icon.ico` - 指纹图标
- `assets/fingerprint_icon.png` - 指纹图标
- `assets/fish_browser_icon.png` - 鱼纹浏览器图标
- `assets/icon.ico` - 图标

#### 配置和入口
- `Program.cs` - 程序入口
- `Program.CLI.cs` - CLI入口
- `appsettings.json` - 配置文件
- `WebScraperApp.csproj` - 项目文件

## 5. API设计规划

### 5.1 核心API端点

#### 浏览器管理
- `GET /api/browsers` - 获取浏览器列表
- `POST /api/browsers` - 创建浏览器实例
- `PUT /api/browsers/{id}` - 更新浏览器配置
- `DELETE /api/browsers/{id}` - 删除浏览器实例

#### 指纹管理
- `GET /api/fingerprints` - 获取指纹列表
- `POST /api/fingerprints` - 创建指纹
- `PUT /api/fingerprints/{id}` - 更新指纹
- `DELETE /api/fingerprints/{id}` - 删除指纹

#### 任务管理
- `GET /api/tasks` - 获取任务列表
- `POST /api/tasks` - 创建任务
- `PUT /api/tasks/{id}` - 更新任务
- `DELETE /api/tasks/{id}` - 删除任务
- `POST /api/tasks/{id}/execute` - 执行任务

#### 代理管理
- `GET /api/proxies` - 获取代理列表
- `POST /api/proxies` - 添加代理
- `PUT /api/proxies/{id}` - 更新代理
- `DELETE /api/proxies/{id}` - 删除代理

## 6. 实施步骤

1. **创建FishBrowser.Core项目**
   - 设置项目结构
   - 配置NuGet包引用
   - 创建基础文件夹结构

2. **创建FinshBrowser.Api项目**
   - 设置ASP.NET Core Web API项目
   - 配置依赖注入
   - 创建基础控制器

3. **迁移核心代码**
   - 将非UI代码从WebScraperApp移动到FishBrowser.Core
   - 修复命名空间和引用
   - 确保代码编译通过

4. **实现API接口**
   - 在FinshBrowser.Api中实现核心功能的API端点
   - 配置Swagger文档
   - 实现错误处理中间件

5. **修改WebScraperApp**
   - 移除已迁移的代码
   - 添加对FinshBrowser.Api的HTTP客户端调用
   - 更新依赖注入配置

6. **测试和验证**
   - 单元测试
   - 集成测试
   - 端到端测试

## 7. 技术考虑

### 7.1 依赖注入
- 在FishBrowser.Core中使用依赖注入
- 在FinshBrowser.Api中配置服务注册
- 在WebScraperApp中配置HTTP客户端

### 7.2 数据访问
- 保持现有的FreeSql/EntityFramework Core实现
- 考虑将数据访问层完全移至FishBrowser.Core

### 7.3 配置管理
- 在FishBrowser.Core中使用配置接口
- 在FinshBrowser.Api中实现具体配置
- 在WebScraperApp中配置API端点

### 7.4 错误处理
- 在API层实现统一的错误处理
- 在UI层处理API调用错误

## 8. 时间估算

- **FishBrowser.Core项目创建**: 1-2小时
- **FinshBrowser.Api项目创建**: 1-2小时
- **代码迁移**: 4-6小时
- **API实现**: 6-8小时
- **WebScraperApp修改**: 3-4小时
- **测试和验证**: 2-3小时

**总计**: 约17-25小时

## 9. 风险与缓解措施

### 9.1 风险
- 代码依赖关系复杂，迁移困难
- API设计不当导致性能问题
- 测试覆盖不足导致功能回归

### 9.2 缓解措施
- 分阶段迁移，先迁移核心功能
- API设计评审和性能测试
- 编写全面的单元测试和集成测试

## 10. 后续优化

1. **微服务化**: 将Core进一步拆分为微服务
2. **缓存优化**: 在API层添加缓存机制
3. **认证授权**: 实现API认证和授权
4. **监控日志**: 添加应用监控和日志记录
5. **容器化**: 将API容器化部署

---

本计划将根据实施过程中的实际情况进行调整和优化。