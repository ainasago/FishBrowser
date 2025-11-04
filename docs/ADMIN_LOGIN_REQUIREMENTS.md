# FishBrowser 管理员登录功能需求文档

## 1. 项目概述

### 1.1 项目背景
FishBrowser 是一个浏览器指纹管理和自动化系统，当前为 Windows WPF 桌面应用。本需求旨在实现前后端分离架构，添加管理员登录功能，并将 BrowserManagementPageV2 的功能迁移到新的 Web 架构中。

### 1.2 项目目标
1. **前后端分离**: 将业务逻辑与 UI 分离，提高可维护性
2. **管理员认证**: 实现安全的管理员登录和权限管理
3. **功能迁移**: 将 BrowserManagementPageV2 的所有功能迁移到新架构
4. **API 化**: 通过 RESTful API 提供所有功能

### 1.3 技术栈
- **后端**: ASP.NET Core 9.0 Web API
- **前端**: WPF (.NET 9.0)
- **数据库**: SQLite (Entity Framework Core)
- **认证**: JWT (JSON Web Token)
- **通信**: HTTP/REST

## 2. 功能需求

### 2.1 管理员登录功能

#### 2.1.1 登录界面
- **用户名输入框**: 支持用户名或邮箱登录
- **密码输入框**: 密码隐藏显示，支持显示/隐藏切换
- **记住我选项**: 保存登录状态（本地存储 Token）
- **登录按钮**: 提交登录请求
- **错误提示**: 显示登录失败原因

#### 2.1.2 认证流程
1. 用户输入用户名和密码
2. 前端发送登录请求到后端 API
3. 后端验证用户凭证
4. 验证成功后返回 JWT Token
5. 前端保存 Token 并跳转到主界面
6. 后续请求携带 Token 进行身份验证

#### 2.1.3 安全要求
- 密码使用 BCrypt 加密存储
- JWT Token 有效期设置（默认 24 小时）
- 支持 Token 刷新机制
- 登录失败次数限制（防暴力破解）
- 密码强度要求（最少 8 位，包含字母和数字）

### 2.2 浏览器管理功能（从 BrowserManagementPageV2 迁移）

#### 2.2.1 浏览器列表管理
- **获取浏览器列表**: 支持分页、搜索、排序
- **创建浏览器**: 新建浏览器环境配置
- **编辑浏览器**: 修改浏览器配置
- **删除浏览器**: 删除单个或批量删除
- **启动浏览器**: 启动浏览器实例

#### 2.2.2 分组管理
- **获取分组列表**: 显示所有分组
- **创建分组**: 新建浏览器分组
- **编辑分组**: 修改分组信息
- **删除分组**: 删除分组（浏览器移至未分组）
- **移动浏览器**: 将浏览器移动到指定分组

#### 2.2.3 指纹管理
- **查看指纹信息**: 显示浏览器指纹详情
- **更换指纹**: 为浏览器更换指纹配置
- **批量更换指纹**: 批量更新多个浏览器的指纹

#### 2.2.4 批量操作
- **批量启动**: 同时启动多个浏览器
- **批量移动**: 批量移动浏览器到指定分组
- **批量删除**: 批量删除浏览器
- **批量更换指纹**: 批量更新指纹配置

#### 2.2.5 搜索和过滤
- **搜索**: 按名称、UA、分组搜索
- **过滤**: 按分组过滤浏览器
- **排序**: 按名称、创建时间、启动次数排序

#### 2.2.6 视图模式
- **卡片视图**: 以卡片形式展示浏览器
- **列表视图**: 以表格形式展示浏览器

#### 2.2.7 统计信息
- **总数统计**: 显示浏览器总数
- **运行中统计**: 显示正在运行的浏览器数量
- **分组统计**: 显示分组数量

### 2.3 会话管理功能
- **会话持久化**: 保存浏览器会话数据
- **清除会话**: 清除指定浏览器的会话数据
- **会话状态检测**: 检测浏览器是否有保存的会话

## 3. 非功能需求

### 3.1 性能要求
- API 响应时间 < 200ms（正常情况）
- 支持并发 100+ 用户
- 浏览器列表加载时间 < 1s

### 3.2 可用性要求
- 界面友好，操作直观
- 错误提示清晰明确
- 支持键盘快捷键操作

### 3.3 安全性要求
- 所有 API 请求需要身份验证
- 敏感数据加密传输（HTTPS）
- 防止 SQL 注入、XSS 攻击
- 日志记录所有关键操作

### 3.4 可维护性要求
- 代码结构清晰，遵循 SOLID 原则
- 完善的注释和文档
- 单元测试覆盖率 > 70%

## 4. 数据模型

### 4.1 用户表 (Users)
```csharp
public class User
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public string Role { get; set; } // Admin, User
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public int LoginFailedCount { get; set; }
    public DateTime? LockoutEndTime { get; set; }
}
```

### 4.2 浏览器环境表 (BrowserEnvironments)
已存在于 `FishBrowser.Core\Models\BrowserEnvironment.cs`

### 4.3 浏览器分组表 (BrowserGroups)
已存在于 `FishBrowser.Core\Models\BrowserGroup.cs`

### 4.4 指纹配置表 (FingerprintProfiles)
已存在于 `FishBrowser.Core\Models\FingerprintProfile.cs`

## 5. API 接口设计

### 5.1 认证接口

#### POST /api/auth/login
登录接口
- **请求体**:
```json
{
  "username": "admin",
  "password": "password123"
}
```
- **响应**:
```json
{
  "success": true,
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "refresh_token_here",
  "user": {
    "id": 1,
    "username": "admin",
    "email": "admin@example.com",
    "role": "Admin"
  }
}
```

#### POST /api/auth/logout
登出接口
- **请求头**: Authorization: Bearer {token}
- **响应**:
```json
{
  "success": true,
  "message": "Logged out successfully"
}
```

#### POST /api/auth/refresh
刷新 Token
- **请求体**:
```json
{
  "refreshToken": "refresh_token_here"
}
```
- **响应**:
```json
{
  "success": true,
  "token": "new_jwt_token",
  "refreshToken": "new_refresh_token"
}
```

#### GET /api/auth/validate
验证 Token 有效性
- **请求头**: Authorization: Bearer {token}
- **响应**:
```json
{
  "valid": true,
  "user": {
    "id": 1,
    "username": "admin",
    "role": "Admin"
  }
}
```

### 5.2 浏览器管理接口

#### GET /api/browsers
获取浏览器列表
- **查询参数**: 
  - `groupId`: 分组 ID（可选）
  - `search`: 搜索关键词（可选）
  - `sortBy`: 排序字段（name, createdAt, launchCount）
  - `sortOrder`: 排序方向（asc, desc）
  - `page`: 页码（默认 1）
  - `pageSize`: 每页数量（默认 50）

#### POST /api/browsers
创建浏览器

#### PUT /api/browsers/{id}
更新浏览器

#### DELETE /api/browsers/{id}
删除浏览器

#### POST /api/browsers/{id}/launch
启动浏览器

#### POST /api/browsers/batch/launch
批量启动浏览器

#### POST /api/browsers/batch/delete
批量删除浏览器

#### POST /api/browsers/batch/move
批量移动浏览器

#### POST /api/browsers/{id}/change-profile
更换指纹配置

### 5.3 分组管理接口

#### GET /api/groups
获取分组列表

#### POST /api/groups
创建分组

#### PUT /api/groups/{id}
更新分组

#### DELETE /api/groups/{id}
删除分组

### 5.4 指纹管理接口

#### GET /api/fingerprints
获取指纹列表

#### GET /api/fingerprints/{id}
获取指纹详情

#### POST /api/fingerprints
创建指纹

#### PUT /api/fingerprints/{id}
更新指纹

#### DELETE /api/fingerprints/{id}
删除指纹

## 6. 界面设计

### 6.1 登录界面
- 简洁的登录表单
- FishBrowser Logo
- 用户名/密码输入框
- 记住我选项
- 登录按钮
- 错误提示区域

### 6.2 主界面（浏览器管理）
参考 `BrowserManagementPageV2.xaml` 的设计：
- **顶部工具栏**: 搜索框、新建按钮、刷新按钮
- **统计面板**: 总数、运行中、分组数、视图切换
- **左侧分组树**: 显示所有分组
- **右侧浏览器列表**: 卡片视图或列表视图
- **批量操作栏**: 选中浏览器时显示
- **底部工具栏**: 诊断工具和状态信息

## 7. 实施计划

### 阶段 1: 项目结构搭建（2-3 小时）
- 创建 FishBrowser.Api 项目
- 创建 FishBrowser.Web 项目
- 配置项目依赖和引用

### 阶段 2: 数据库和模型（2-3 小时）
- 创建 User 模型
- 更新 DbContext
- 创建数据库迁移
- 初始化管理员账户

### 阶段 3: 后端 API 实现（6-8 小时）
- 实现认证中间件
- 实现认证接口
- 实现浏览器管理接口
- 实现分组管理接口
- 实现指纹管理接口

### 阶段 4: 前端实现（6-8 小时）
- 实现登录界面
- 实现主界面框架
- 实现浏览器管理界面
- 实现 API 调用服务
- 实现数据绑定和交互

### 阶段 5: 测试和优化（2-3 小时）
- 单元测试
- 集成测试
- 性能优化
- Bug 修复

**总计**: 约 18-25 小时

## 8. 风险和挑战

### 8.1 技术风险
- WPF 与 Web API 的集成复杂度
- 浏览器启动的异步处理
- Token 过期处理

### 8.2 缓解措施
- 使用成熟的 HTTP 客户端库（HttpClient）
- 实现完善的错误处理机制
- 添加 Token 自动刷新功能

## 9. 后续优化

1. **多用户支持**: 支持多个管理员账户
2. **权限细化**: 实现基于角色的权限控制
3. **操作日志**: 记录所有用户操作
4. **数据备份**: 自动备份数据库
5. **性能监控**: 添加性能监控和告警
6. **Web 界面**: 开发基于浏览器的 Web 管理界面

---

**文档版本**: 1.0  
**创建日期**: 2025-11-03  
**最后更新**: 2025-11-03
