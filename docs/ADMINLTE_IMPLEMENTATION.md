# FishBrowser AdminLTE 实施指南

## 技术栈

### 后端
- **FishBrowser.Api**: ASP.NET Core 9.0 Web API + JWT认证
- **FishBrowser.Core**: 核心业务逻辑

### 前端  
- **FishBrowser.Web**: ASP.NET Core 9.0 MVC
- **UI框架**: AdminLTE 3.2 (Bootstrap 5 + jQuery)
- **图标**: Font Awesome 6.x
- **图表**: Chart.js

## 项目结构

```
FishBrowser.Web/
├── Controllers/
│   ├── AuthController.cs      # 认证控制器
│   ├── BrowserController.cs   # 浏览器管理
│   └── HomeController.cs      # 首页/仪表板
├── Views/
│   ├── Shared/
│   │   ├── _Layout.cshtml     # AdminLTE主布局
│   │   └── _LoginLayout.cshtml # 登录页布局
│   ├── Auth/
│   │   └── Login.cshtml       # 登录页
│   ├── Browser/
│   │   └── Index.cshtml       # 浏览器管理页
│   └── Home/
│       └── Dashboard.cshtml   # 仪表板
├── wwwroot/
│   ├── adminlte/              # AdminLTE资源
│   ├── css/
│   ├── js/
│   │   └── api-client.js      # API调用封装
│   └── images/
└── Services/
    └── ApiClientService.cs    # HTTP客户端服务
```

## 快速开始

### 1. 创建项目

```bash
# 创建API项目
cd d:\1Dev\webbrowser\web
dotnet new webapi -n FishBrowser.Api -f net9.0
cd FishBrowser.Api
dotnet add reference ../FishBrowser.Core/FishBrowser.Core.csproj
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package BCrypt.Net-Next

# 创建Web项目
cd ..
dotnet new mvc -n FishBrowser.Web -f net9.0
cd FishBrowser.Web
dotnet add package Microsoft.AspNetCore.Authentication.Cookies

# 添加到解决方案
cd ../..
dotnet sln FishBrowser.sln add web/FishBrowser.Api/FishBrowser.Api.csproj
dotnet sln FishBrowser.sln add web/FishBrowser.Web/FishBrowser.Web.csproj
```

### 2. 下载AdminLTE

从 https://adminlte.io/ 下载 AdminLTE 3.2，解压到 `FishBrowser.Web/wwwroot/adminlte/`

### 3. 配置API

**appsettings.json**:
```json
{
  "JwtSettings": {
    "Secret": "YourSuperSecretKeyMin32Chars!",
    "Issuer": "FishBrowser.Api",
    "Audience": "FishBrowser.Web",
    "AccessTokenExpirationMinutes": 1440
  },
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=../../../windows/WebScraperApp/webscraper.db"
  }
}
```

### 4. 配置Web

**appsettings.json**:
```json
{
  "ApiSettings": {
    "BaseUrl": "http://localhost:5000"
  }
}
```

## 核心功能实现

### 登录页面 (AdminLTE)

**Views/Auth/Login.cshtml**:
```html
@{
    Layout = "_LoginLayout";
    ViewData["Title"] = "登录";
}

<div class="login-box">
    <div class="login-logo">
        <b>Fish</b>Browser
    </div>
    <div class="card">
        <div class="card-body login-card-body">
            <p class="login-box-msg">登录管理后台</p>
            <form id="loginForm">
                <div class="input-group mb-3">
                    <input type="text" class="form-control" placeholder="用户名" id="username">
                    <div class="input-group-append">
                        <div class="input-group-text">
                            <span class="fas fa-user"></span>
                        </div>
                    </div>
                </div>
                <div class="input-group mb-3">
                    <input type="password" class="form-control" placeholder="密码" id="password">
                    <div class="input-group-append">
                        <div class="input-group-text">
                            <span class="fas fa-lock"></span>
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col-8">
                        <div class="icheck-primary">
                            <input type="checkbox" id="remember">
                            <label for="remember">记住我</label>
                        </div>
                    </div>
                    <div class="col-4">
                        <button type="submit" class="btn btn-primary btn-block">登录</button>
                    </div>
                </div>
            </form>
            <div id="errorMessage" class="alert alert-danger mt-3" style="display:none;"></div>
        </div>
    </div>
</div>

@section Scripts {
    <script src="~/js/auth.js"></script>
}
```

### 浏览器管理页面

**Views/Browser/Index.cshtml**:
```html
@{
    ViewData["Title"] = "浏览器管理";
}

<div class="content-header">
    <div class="container-fluid">
        <div class="row mb-2">
            <div class="col-sm-6">
                <h1 class="m-0">浏览器管理</h1>
            </div>
        </div>
    </div>
</div>

<section class="content">
    <div class="container-fluid">
        <!-- 统计卡片 -->
        <div class="row">
            <div class="col-lg-3 col-6">
                <div class="small-box bg-info">
                    <div class="inner">
                        <h3 id="totalCount">0</h3>
                        <p>总数</p>
                    </div>
                    <div class="icon">
                        <i class="fas fa-browser"></i>
                    </div>
                </div>
            </div>
            <div class="col-lg-3 col-6">
                <div class="small-box bg-success">
                    <div class="inner">
                        <h3 id="runningCount">0</h3>
                        <p>运行中</p>
                    </div>
                    <div class="icon">
                        <i class="fas fa-play-circle"></i>
                    </div>
                </div>
            </div>
        </div>

        <!-- 浏览器列表 -->
        <div class="card">
            <div class="card-header">
                <h3 class="card-title">浏览器列表</h3>
                <div class="card-tools">
                    <button class="btn btn-primary btn-sm" onclick="createBrowser()">
                        <i class="fas fa-plus"></i> 新建浏览器
                    </button>
                </div>
            </div>
            <div class="card-body">
                <table id="browsersTable" class="table table-bordered table-striped">
                    <thead>
                        <tr>
                            <th>ID</th>
                            <th>名称</th>
                            <th>分组</th>
                            <th>引擎</th>
                            <th>启动次数</th>
                            <th>创建时间</th>
                            <th>操作</th>
                        </tr>
                    </thead>
                    <tbody id="browsersTableBody">
                    </tbody>
                </table>
            </div>
        </div>
    </div>
</section>

@section Scripts {
    <script src="~/js/browser-management.js"></script>
}
```

### JavaScript API客户端

**wwwroot/js/api-client.js**:
```javascript
class ApiClient {
    constructor(baseUrl) {
        this.baseUrl = baseUrl || 'http://localhost:5000';
        this.token = localStorage.getItem('token');
    }

    async request(url, options = {}) {
        const headers = {
            'Content-Type': 'application/json',
            ...options.headers
        };

        if (this.token) {
            headers['Authorization'] = `Bearer ${this.token}`;
        }

        const response = await fetch(`${this.baseUrl}${url}`, {
            ...options,
            headers
        });

        if (response.status === 401) {
            localStorage.removeItem('token');
            window.location.href = '/Auth/Login';
            return;
        }

        return response.json();
    }

    async login(username, password) {
        const data = await this.request('/api/auth/login', {
            method: 'POST',
            body: JSON.stringify({ username, password })
        });

        if (data.success) {
            this.token = data.token;
            localStorage.setItem('token', data.token);
            localStorage.setItem('user', JSON.stringify(data.user));
        }

        return data;
    }

    async getBrowsers(params = {}) {
        const query = new URLSearchParams(params).toString();
        return this.request(`/api/browsers?${query}`);
    }

    async launchBrowser(id) {
        return this.request(`/api/browsers/${id}/launch`, {
            method: 'POST'
        });
    }

    async deleteBrowser(id) {
        return this.request(`/api/browsers/${id}`, {
            method: 'DELETE'
        });
    }
}

const apiClient = new ApiClient();
```

## 默认管理员账户

- **用户名**: admin
- **密码**: Admin@123
- **角色**: Admin

## 运行项目

```bash
# 启动API
cd d:\1Dev\webbrowser\web\FishBrowser.Api
dotnet run --urls="http://localhost:5000"

# 启动Web
cd d:\1Dev\webbrowser\web\FishBrowser.Web
dotnet run --urls="http://localhost:5001"
```

访问: http://localhost:5001

---

**文档版本**: 1.0  
**创建日期**: 2025-11-03
