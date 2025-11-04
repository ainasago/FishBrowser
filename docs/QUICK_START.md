# FishBrowser 快速启动指南

## 项目概述

FishBrowser 是一个浏览器指纹管理系统，采用前后端分离架构：
- **后端 API**: ASP.NET Core 9.0 Web API (JWT 认证)
- **前端**: ASP.NET Core MVC + AdminLTE 3.x
- **数据库**: SQLite

## 快速启动

### 1. 启动后端 API

```bash
cd d:\1Dev\webbrowser\web\FishBrowser.Api
dotnet run --urls="http://localhost:5000"
```

API 将在 `http://localhost:5000` 启动
Swagger 文档: `http://localhost:5000/swagger`

### 2. 启动前端 Web

```bash
cd d:\1Dev\webbrowser\web\FishBrowser.Web
dotnet run --urls="http://localhost:5001"
```

Web 应用将在 `http://localhost:5001` 启动

## 默认管理员账户

- **用户名**: `admin`
- **密码**: `Admin@123`
- **角色**: `Admin`

## API 端点

### 认证接口

- `POST /api/auth/login` - 用户登录
- `POST /api/auth/refresh` - 刷新令牌
- `POST /api/auth/logout` - 用户登出
- `GET /api/auth/validate` - 验证令牌

### 浏览器管理接口

- `GET /api/browsers` - 获取浏览器列表
  - 查询参数: `groupId`, `search`, `sortBy`, `sortOrder`, `page`, `pageSize`
- `GET /api/browsers/statistics` - 获取统计信息
- `DELETE /api/browsers/{id}` - 删除浏览器

### 分组管理接口

- `GET /api/groups` - 获取所有分组

## 数据库

数据库文件位置: `d:\1Dev\webbrowser\windows\WebScraperApp\webscraper.db`

首次启动时会自动创建默认管理员账户。

## 开发工具

### Swagger UI
访问 `http://localhost:5000/swagger` 可以查看和测试所有 API 接口。

### 测试登录

```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d "{\"username\":\"admin\",\"password\":\"Admin@123\"}"
```

响应示例:
```json
{
  "success": true,
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "base64_encoded_token",
  "user": {
    "id": 1,
    "username": "admin",
    "email": "admin@fishbrowser.com",
    "role": "Admin"
  }
}
```

### 测试获取浏览器列表

```bash
curl -X GET http://localhost:5000/api/browsers \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

## 项目结构

```
FishBrowser/
├── web/
│   ├── FishBrowser.Api/          # 后端 API
│   │   ├── Controllers/          # API 控制器
│   │   ├── Services/             # 业务服务
│   │   ├── DTOs/                 # 数据传输对象
│   │   ├── Configuration/        # 配置类
│   │   └── Program.cs            # 启动配置
│   ├── FishBrowser.Web/          # 前端 Web (待实现)
│   └── FishBrowser.Core/         # 核心业务逻辑
│       ├── Models/               # 数据模型
│       ├── Services/             # 核心服务
│       ├── Data/                 # 数据访问
│       └── Engine/               # 浏览器引擎
└── docs/                         # 文档
```

## 下一步

1. ✅ 后端 API 已完成
2. ⏳ 前端 AdminLTE 界面开发中
3. ⏳ 浏览器启动功能集成
4. ⏳ 完整的 CRUD 操作

## 故障排除

### 端口被占用
如果端口 5000 或 5001 被占用，可以修改启动命令：
```bash
dotnet run --urls="http://localhost:6000"
```

### 数据库连接失败
检查 `appsettings.json` 中的连接字符串是否正确指向数据库文件。

### JWT 验证失败
确保 `JwtSettings.Secret` 至少 32 个字符长。

---

**最后更新**: 2025-11-03
