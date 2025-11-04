# FishBrowser - 浏览器指纹管理系统

> 基于 AdminLTE 的现代化浏览器指纹管理和自动化平台

## 🎯 项目概述

FishBrowser 是一个功能强大的浏览器指纹管理系统，采用前后端分离架构，提供完整的浏览器环境管理、指纹配置和自动化功能。

### 核心特性

- ✅ **JWT 认证系统** - 安全的管理员登录和权限管理
- ✅ **浏览器管理** - 完整的浏览器环境 CRUD 操作
- ✅ **指纹管理** - 高级浏览器指纹配置和管理
- ✅ **分组管理** - 按场景组织浏览器环境
- ✅ **RESTful API** - 完整的后端 API 接口
- ⏳ **AdminLTE 界面** - 现代化的管理后台（开发中）

## 🏗️ 技术架构

### 后端技术栈
- **框架**: ASP.NET Core 9.0 Web API
- **认证**: JWT Bearer Token
- **ORM**: Entity Framework Core 9.0
- **数据库**: SQLite
- **密码加密**: BCrypt
- **API 文档**: Swagger/OpenAPI

### 前端技术栈（计划）
- **框架**: ASP.NET Core MVC
- **UI 框架**: AdminLTE 3.x
- **CSS**: Bootstrap 5
- **JavaScript**: jQuery + AdminLTE plugins
- **图标**: Font Awesome 6

### 项目结构

```
FishBrowser/
├── web/
│   ├── FishBrowser.Api/          # 后端 API (✅ 已完成)
│   ├── FishBrowser.Web/          # 前端界面 (⏳ 开发中)
│   └── FishBrowser.Core/         # 核心业务逻辑
├── windows/
│   └── WebScraperApp/            # 原 WPF 应用
└── docs/                         # 项目文档
```

## 🚀 快速开始

### 前置要求

- .NET 9.0 SDK
- Visual Studio 2022 或 VS Code
- SQLite

### 启动系统

**1. 启动后端 API**
```bash
cd d:\1Dev\webbrowser\web\FishBrowser.Api
dotnet run
```
- API: http://localhost:5062
- Swagger: http://localhost:5062/swagger

**2. 启动前端 Web**
```bash
cd d:\1Dev\webbrowser\web\FishBrowser.Web
dotnet run
```
- Web: **http://localhost:5208**

### 默认管理员账户

```
用户名: admin
密码: Admin@123
角色: Admin
```

### 访问系统

打开浏览器访问: **http://localhost:5208**

## 📚 API 文档

### 认证接口

| 方法 | 端点 | 描述 |
|------|------|------|
| POST | `/api/auth/login` | 用户登录 |
| POST | `/api/auth/refresh` | 刷新令牌 |
| POST | `/api/auth/logout` | 用户登出 |
| GET | `/api/auth/validate` | 验证令牌 |

### 浏览器管理接口

| 方法 | 端点 | 描述 |
|------|------|------|
| GET | `/api/browsers` | 获取浏览器列表 |
| GET | `/api/browsers/statistics` | 获取统计信息 |
| DELETE | `/api/browsers/{id}` | 删除浏览器 |

### 分组管理接口

| 方法 | 端点 | 描述 |
|------|------|------|
| GET | `/api/groups` | 获取所有分组 |

## 🧪 测试 API

### 使用 Swagger UI

访问 `http://localhost:5062/swagger` 进行交互式 API 测试。

### 使用 curl

**登录**:
```bash
curl -X POST http://localhost:5062/api/auth/login \
  -H "Content-Type: application/json" \
  -d "{\"username\":\"admin\",\"password\":\"Admin@123\"}"
```

**获取浏览器列表**:
```bash
curl -X GET "http://localhost:5062/api/browsers?page=1&pageSize=10" \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

## 📖 详细文档

- [需求文档](docs/ADMIN_LOGIN_REQUIREMENTS.md) - 完整的功能需求说明
- [技术设计](docs/TECHNICAL_DESIGN.md) - 系统架构和技术设计
- [实施指南](docs/ADMINLTE_IMPLEMENTATION.md) - AdminLTE 实施详情
- [快速启动](docs/QUICK_START.md) - 快速启动指南
- [实施状态](docs/IMPLEMENTATION_STATUS.md) - 当前实施进度
- [**Web 使用指南**](docs/WEB_USAGE_GUIDE.md) - **前端使用说明** ⭐
- [**服务整合文档**](docs/SERVICE_INTEGRATION.md) - **完整服务整合说明** ⭐ 新增
- [数据库同步修复](docs/DATABASE_SYNC_FIX.md) - 数据库表同步问题修复

## 🔧 开发指南

### 编译项目

```bash
# 编译整个解决方案
dotnet build FishBrowser.sln

# 仅编译 API
cd web/FishBrowser.Api
dotnet build
```

### 运行测试

```bash
dotnet test
```

### 数据库管理

数据库文件位置: `d:\1Dev\webbrowser\windows\WebScraperApp\webscraper.db`

首次启动时会自动创建 Users 和 RefreshTokens 表，并初始化默认管理员账户。

## 📊 当前状态

### ✅ 已完成

- [x] 项目架构搭建
- [x] JWT 认证系统
- [x] 用户管理模型
- [x] 认证 API 端点
- [x] 浏览器查询 API
- [x] 分组查询 API
- [x] Swagger 文档
- [x] 完整技术文档
- [x] **AdminLTE 登录页面**
- [x] **AdminLTE 主界面布局**
- [x] **浏览器管理页面**
- [x] **搜索和分页功能**
- [x] **删除功能**
- [x] **完整服务整合** ⭐ 新增
  - FreeSql 数据库迁移
  - 所有核心业务服务
  - 浏览器引擎服务
  - 字体和 GPU 种子数据

### ⏳ 进行中

- [ ] 浏览器创建/编辑
- [ ] 浏览器启动功能
- [ ] 批量操作功能

### 📅 计划中

- [ ] 指纹完整管理
- [ ] 代理管理
- [ ] 任务调度
- [ ] 数据可视化
- [ ] 性能优化

## 🤝 贡献

欢迎提交 Issue 和 Pull Request！

## 📄 许可证

本项目采用 MIT 许可证。

## 📞 联系方式

如有问题或建议，请通过 Issue 联系我们。

---

**最后更新**: 2025-11-03 21:35  
**版本**: v1.0-alpha  
**状态**: 🟢 API 运行中 | 🟢 Web 运行中  
**访问地址**: http://localhost:5208
