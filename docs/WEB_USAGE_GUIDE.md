# FishBrowser Web 管理后台使用指南

## 🚀 系统访问

### 启动服务

**1. 启动后端 API**
```bash
cd d:\1Dev\webbrowser\web\FishBrowser.Api
dotnet run
```
- API 地址: http://localhost:5062
- Swagger 文档: http://localhost:5062/swagger

**2. 启动前端 Web**
```bash
cd d:\1Dev\webbrowser\web\FishBrowser.Web
dotnet run
```
- Web 地址: **http://localhost:5208**

### 默认登录账户

```
用户名: admin
密码: Admin@123
```

## 📱 功能说明

### 1. 登录页面

访问 http://localhost:5208 会自动跳转到登录页面。

**功能特性**:
- ✅ 用户名/密码登录
- ✅ 记住我选项
- ✅ 错误提示
- ✅ 美观的渐变背景
- ✅ 响应式设计

**登录流程**:
1. 输入用户名和密码
2. 可选择"记住我"
3. 点击"登录"按钮
4. 成功后自动跳转到浏览器管理页面

### 2. 主界面布局

登录成功后进入 AdminLTE 主界面。

**界面组成**:
- **顶部导航栏**: 显示用户名和退出按钮
- **左侧边栏**: 功能菜单导航
- **主内容区**: 显示当前页面内容
- **底部页脚**: 版权信息

**侧边栏菜单**:
- 🌐 浏览器管理 (已实现)
- 🔍 指纹管理 (待实现)
- 📁 分组管理 (待实现)
- 🌐 代理管理 (待实现)
- ⚙️ 系统设置 (待实现)

### 3. 浏览器管理页面

**统计卡片**:
- **总数**: 显示浏览器总数量
- **运行中**: 显示正在运行的浏览器数量
- **分组数**: 显示分组总数
- **已选中**: 显示当前选中的浏览器数量

**浏览器列表**:
- 表格形式展示所有浏览器
- 支持全选/单选
- 显示信息: ID、名称、分组、引擎、系统、启动次数、创建时间

**功能按钮**:
- ▶️ **启动**: 启动浏览器 (开发中)
- ✏️ **编辑**: 编辑浏览器配置 (开发中)
- 🗑️ **删除**: 删除浏览器 (已实现)

**搜索功能**:
- 在右上角搜索框输入关键词
- 支持搜索浏览器名称、UA 等
- 按回车或点击搜索按钮执行搜索

**分页功能**:
- 每页显示 20 条记录
- 底部显示分页导航
- 支持跳转到指定页面

## 🎨 界面特性

### AdminLTE 主题

- **现代化设计**: 基于 Bootstrap 4 和 AdminLTE 3.2
- **响应式布局**: 自适应各种屏幕尺寸
- **图标系统**: Font Awesome 6.4.0
- **消息提示**: Toastr 通知系统

### 用户体验

- **加载动画**: 数据加载时显示加载动画
- **成功/错误提示**: 操作结果实时提示
- **确认对话框**: 删除等危险操作需要确认
- **自动刷新**: 操作后自动刷新数据

## 🔧 开发者信息

### 技术栈

**后端**:
- ASP.NET Core 9.0 Web API
- JWT 认证
- Entity Framework Core
- SQLite 数据库

**前端**:
- ASP.NET Core MVC
- AdminLTE 3.2
- jQuery 3.6.0
- Bootstrap 4.6.2
- Toastr.js

### API 调用

前端通过 `HttpClient` 调用后端 API：

```csharp
// 示例：获取浏览器列表
var client = _httpClientFactory.CreateClient("FishBrowserApi");
client.DefaultRequestHeaders.Authorization = 
    new AuthenticationHeaderValue("Bearer", token);
var response = await client.GetAsync("/api/browsers");
```

### Session 管理

用户登录后，Token 保存在 Session 中：

```csharp
HttpContext.Session.SetString("Token", result.Token);
HttpContext.Session.SetString("Username", result.User.Username);
HttpContext.Session.SetString("Role", result.User.Role);
```

## 📝 使用流程

### 完整操作流程

1. **启动服务**
   ```bash
   # 终端 1: 启动 API
   cd d:\1Dev\webbrowser\web\FishBrowser.Api
   dotnet run
   
   # 终端 2: 启动 Web
   cd d:\1Dev\webbrowser\web\FishBrowser.Web
   dotnet run
   ```

2. **登录系统**
   - 访问 http://localhost:5208
   - 输入 admin / Admin@123
   - 点击登录

3. **查看浏览器列表**
   - 自动进入浏览器管理页面
   - 查看统计信息
   - 浏览浏览器列表

4. **搜索浏览器**
   - 在搜索框输入关键词
   - 按回车或点击搜索

5. **删除浏览器**
   - 点击删除按钮
   - 确认删除
   - 自动刷新列表

6. **退出系统**
   - 点击右上角"退出"按钮
   - 自动跳转到登录页面

## ⚠️ 注意事项

### 安全性

- ✅ 所有 API 请求需要 JWT Token 认证
- ✅ Token 保存在 Session 中，关闭浏览器后失效
- ✅ 登录失败 5 次后账户锁定 15 分钟
- ✅ 密码使用 BCrypt 加密存储

### 浏览器兼容性

推荐使用以下浏览器：
- Chrome 90+
- Firefox 88+
- Edge 90+
- Safari 14+

### 网络要求

- 前端和后端需要在同一网络环境
- 确保防火墙允许端口 5062 和 5208
- API 和 Web 需要同时运行

## 🐛 故障排除

### 无法登录

**问题**: 输入正确的用户名密码后无法登录

**解决方案**:
1. 检查 API 是否正常运行 (http://localhost:5062/swagger)
2. 检查数据库是否初始化（查看 API 启动日志）
3. 检查浏览器控制台是否有错误信息

### 浏览器列表为空

**问题**: 登录后看不到浏览器列表

**解决方案**:
1. 这是正常的，数据库中可能还没有浏览器数据
2. 可以通过原 WPF 应用创建浏览器
3. 或者等待浏览器创建功能实现

### Token 过期

**问题**: 操作时提示未授权

**解决方案**:
1. Token 有效期为 24 小时
2. 过期后会自动跳转到登录页面
3. 重新登录即可

### CORS 错误

**问题**: 浏览器控制台显示 CORS 错误

**解决方案**:
1. 检查 API 的 CORS 配置
2. 确保 Web 地址在 API 的 CORS 白名单中
3. 重启 API 服务

## 📊 数据统计

### 当前实现功能

- ✅ 用户登录/登出
- ✅ Session 管理
- ✅ 浏览器列表查询
- ✅ 浏览器搜索
- ✅ 浏览器删除
- ✅ 统计信息显示
- ✅ 分页功能

### 待实现功能

- ⏳ 浏览器创建
- ⏳ 浏览器编辑
- ⏳ 浏览器启动
- ⏳ 批量操作
- ⏳ 指纹管理
- ⏳ 分组管理
- ⏳ 代理管理

## 🎯 下一步计划

1. **浏览器 CRUD**: 完整的创建、编辑功能
2. **浏览器启动**: 集成浏览器启动功能
3. **批量操作**: 批量启动、删除、移动
4. **指纹管理**: 完整的指纹配置界面
5. **分组管理**: 分组的增删改查
6. **代理管理**: 代理配置和管理

## 📞 技术支持

如有问题，请查看：
- [需求文档](ADMIN_LOGIN_REQUIREMENTS.md)
- [技术设计](TECHNICAL_DESIGN.md)
- [快速启动](QUICK_START.md)
- [实施状态](IMPLEMENTATION_STATUS.md)

---

**文档版本**: 1.0  
**创建日期**: 2025-11-03  
**最后更新**: 2025-11-03 21:35
