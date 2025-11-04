# FishBrowser 技术设计文档

## 1. 系统架构

### 1.1 整体架构图

```
┌─────────────────────────────────────────────────────────────┐
│                     FishBrowser.Web (WPF)                   │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐     │
│  │  Login View  │  │ Browser Mgmt │  │  Other Views │     │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘     │
│         │                 │                  │              │
│         └─────────────────┴──────────────────┘              │
│                           │                                 │
│                  ┌────────▼────────┐                        │
│                  │  API Service    │                        │
│                  │  (HttpClient)   │                        │
│                  └────────┬────────┘                        │
└───────────────────────────┼─────────────────────────────────┘
                            │ HTTP/REST + JWT
┌───────────────────────────▼─────────────────────────────────┐
│                  FishBrowser.Api (ASP.NET Core)             │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐     │
│  │Auth Controller│ │Browser Ctrl  │  │ Group Ctrl   │     │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘     │
│         │                 │                  │              │
│         └─────────────────┴──────────────────┘              │
│                           │                                 │
│                  ┌────────▼────────┐                        │
│                  │   Services      │                        │
│                  └────────┬────────┘                        │
└───────────────────────────┼─────────────────────────────────┘
                            │
┌───────────────────────────▼─────────────────────────────────┐
│                    FishBrowser.Core                         │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐     │
│  │   Models     │  │   Services   │  │    Engine    │     │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘     │
│         │                 │                  │              │
│         └─────────────────┴──────────────────┘              │
│                           │                                 │
│                  ┌────────▼────────┐                        │
│                  │   DbContext     │                        │
│                  └────────┬────────┘                        │
└───────────────────────────┼─────────────────────────────────┘
                            │
                    ┌───────▼────────┐
                    │  SQLite DB     │
                    └────────────────┘
```

### 1.2 项目结构

#### FishBrowser.Core (核心业务逻辑层)
```
FishBrowser.Core/
├── Models/                  # 数据模型
│   ├── User.cs             # 用户模型
│   ├── BrowserEnvironment.cs
│   ├── BrowserGroup.cs
│   ├── FingerprintProfile.cs
│   └── ...
├── Services/               # 业务服务
│   ├── BrowserEnvironmentService.cs
│   ├── BrowserSessionService.cs
│   ├── FingerprintService.cs
│   └── ...
├── Engine/                 # 浏览器引擎
│   ├── PlaywrightController.cs
│   ├── BrowserControllerAdapter.cs
│   └── ...
├── Data/                   # 数据访问
│   └── WebScraperDbContext.cs
└── Infrastructure/         # 基础设施
    └── ...
```

#### FishBrowser.Api (Web API 层)
```
FishBrowser.Api/
├── Controllers/            # API 控制器
│   ├── AuthController.cs
│   ├── BrowsersController.cs
│   ├── GroupsController.cs
│   └── FingerprintsController.cs
├── Services/              # API 特定服务
│   ├── AuthService.cs
│   ├── TokenService.cs
│   └── PasswordHasher.cs
├── Middleware/            # 中间件
│   ├── JwtMiddleware.cs
│   └── ExceptionMiddleware.cs
├── DTOs/                  # 数据传输对象
│   ├── LoginRequest.cs
│   ├── LoginResponse.cs
│   ├── BrowserDto.cs
│   └── ...
├── Configuration/         # 配置
│   └── JwtSettings.cs
├── Program.cs
└── appsettings.json
```

#### FishBrowser.Web (WPF UI 层)
```
FishBrowser.Web/
├── Views/                 # 视图
│   ├── LoginWindow.xaml
│   ├── MainWindow.xaml
│   └── BrowserManagementView.xaml
├── ViewModels/           # 视图模型
│   ├── LoginViewModel.cs
│   ├── MainViewModel.cs
│   └── BrowserManagementViewModel.cs
├── Services/             # UI 服务
│   ├── ApiService.cs
│   ├── AuthenticationService.cs
│   └── NavigationService.cs
├── Models/               # UI 模型
│   └── BrowserViewModel.cs
├── Converters/           # 值转换器
├── App.xaml
└── App.xaml.cs
```

## 2. 技术选型

### 2.1 后端技术栈
- **框架**: ASP.NET Core 9.0 Web API
- **ORM**: Entity Framework Core 9.0
- **数据库**: SQLite
- **认证**: JWT (System.IdentityModel.Tokens.Jwt)
- **密码加密**: BCrypt.Net-Next
- **日志**: Serilog
- **API 文档**: Swagger/OpenAPI

### 2.2 前端技术栈
- **框架**: WPF (.NET 9.0)
- **MVVM**: CommunityToolkit.Mvvm
- **HTTP 客户端**: System.Net.Http.HttpClient
- **JSON 序列化**: System.Text.Json
- **UI 组件**: ModernWpf (可选)

### 2.3 共享技术
- **依赖注入**: Microsoft.Extensions.DependencyInjection
- **配置管理**: Microsoft.Extensions.Configuration

## 3. 核心功能设计

### 3.1 认证系统

#### 3.1.1 JWT Token 结构
```json
{
  "sub": "1",
  "username": "admin",
  "email": "admin@example.com",
  "role": "Admin",
  "exp": 1699000000,
  "iat": 1698913600
}
```

#### 3.1.2 Token 生成流程
```csharp
public class TokenService
{
    public string GenerateToken(User user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

#### 3.1.3 密码加密
```csharp
public class PasswordHasher
{
    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
    }

    public bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}
```

#### 3.1.4 登录流程
1. 用户提交用户名和密码
2. API 验证用户凭证
3. 检查账户状态（是否锁定）
4. 验证密码
5. 生成 JWT Token 和 Refresh Token
6. 更新最后登录时间
7. 返回 Token 和用户信息

#### 3.1.5 Token 验证中间件
```csharp
public class JwtMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var token = context.Request.Headers["Authorization"]
            .FirstOrDefault()?.Split(" ").Last();

        if (token != null)
            await AttachUserToContext(context, token);

        await next(context);
    }

    private async Task AttachUserToContext(HttpContext context, string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtSettings.Secret);
            
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtSettings.Audience,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            var jwtToken = (JwtSecurityToken)validatedToken;
            var userId = int.Parse(jwtToken.Claims.First(x => x.Type == "sub").Value);

            context.Items["User"] = await _userService.GetByIdAsync(userId);
        }
        catch
        {
            // Token 验证失败，不附加用户到上下文
        }
    }
}
```

### 3.2 浏览器管理系统

#### 3.2.1 浏览器服务接口
```csharp
public interface IBrowserService
{
    Task<PagedResult<BrowserDto>> GetBrowsersAsync(BrowserQueryParams queryParams);
    Task<BrowserDto> GetBrowserByIdAsync(int id);
    Task<BrowserDto> CreateBrowserAsync(CreateBrowserRequest request);
    Task<BrowserDto> UpdateBrowserAsync(int id, UpdateBrowserRequest request);
    Task DeleteBrowserAsync(int id);
    Task<LaunchResult> LaunchBrowserAsync(int id);
    Task<BatchResult> BatchLaunchAsync(int[] ids);
    Task<BatchResult> BatchDeleteAsync(int[] ids);
    Task<BatchResult> BatchMoveAsync(int[] ids, int? groupId);
    Task ChangeFingerprintAsync(int id, int fingerprintId);
}
```

#### 3.2.2 分页查询
```csharp
public class BrowserQueryParams
{
    public int? GroupId { get; set; }
    public string? Search { get; set; }
    public string SortBy { get; set; } = "name";
    public string SortOrder { get; set; } = "asc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class PagedResult<T>
{
    public List<T> Items { get; set; }
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
```

#### 3.2.3 DTO 设计
```csharp
public class BrowserDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int? GroupId { get; set; }
    public string? GroupName { get; set; }
    public string Engine { get; set; }
    public string OS { get; set; }
    public string UserAgent { get; set; }
    public int LaunchCount { get; set; }
    public bool EnablePersistence { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLaunchedAt { get; set; }
    public FingerprintDto? Fingerprint { get; set; }
}

public class CreateBrowserRequest
{
    public string Name { get; set; }
    public int? GroupId { get; set; }
    public string Engine { get; set; } = "chrome";
    public string OS { get; set; } = "windows";
    public int? FingerprintProfileId { get; set; }
    public bool EnablePersistence { get; set; } = true;
    // ... 其他配置
}
```

### 3.3 前端 API 服务

#### 3.3.1 API 服务基类
```csharp
public class ApiService
{
    private readonly HttpClient _httpClient;
    private readonly IAuthenticationService _authService;

    public ApiService(HttpClient httpClient, IAuthenticationService authService)
    {
        _httpClient = httpClient;
        _authService = authService;
    }

    protected async Task<T> GetAsync<T>(string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        await AddAuthHeaderAsync(request);
        
        var response = await _httpClient.SendAsync(request);
        await HandleResponseAsync(response);
        
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content);
    }

    protected async Task<T> PostAsync<T>(string url, object data)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(data),
                Encoding.UTF8,
                "application/json")
        };
        
        await AddAuthHeaderAsync(request);
        
        var response = await _httpClient.SendAsync(request);
        await HandleResponseAsync(response);
        
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content);
    }

    private async Task AddAuthHeaderAsync(HttpRequestMessage request)
    {
        var token = await _authService.GetTokenAsync();
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = 
                new AuthenticationHeaderValue("Bearer", token);
        }
    }

    private async Task HandleResponseAsync(HttpResponseMessage response)
    {
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            await _authService.LogoutAsync();
            throw new UnauthorizedException("Session expired. Please login again.");
        }

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new ApiException($"API Error: {error}");
        }
    }
}
```

#### 3.3.2 认证服务
```csharp
public class AuthenticationService : IAuthenticationService
{
    private readonly HttpClient _httpClient;
    private string? _token;
    private string? _refreshToken;

    public async Task<LoginResult> LoginAsync(string username, string password)
    {
        var request = new LoginRequest { Username = username, Password = password };
        var response = await _httpClient.PostAsJsonAsync("/api/auth/login", request);
        
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
            _token = result.Token;
            _refreshToken = result.RefreshToken;
            
            // 保存到本地存储
            await SaveTokenAsync(_token, _refreshToken);
            
            return new LoginResult { Success = true, User = result.User };
        }
        
        return new LoginResult { Success = false, Error = "Invalid credentials" };
    }

    public async Task<string?> GetTokenAsync()
    {
        if (string.IsNullOrEmpty(_token))
        {
            _token = await LoadTokenAsync();
        }
        
        // 检查 Token 是否过期
        if (IsTokenExpired(_token))
        {
            await RefreshTokenAsync();
        }
        
        return _token;
    }

    private async Task RefreshTokenAsync()
    {
        var request = new RefreshTokenRequest { RefreshToken = _refreshToken };
        var response = await _httpClient.PostAsJsonAsync("/api/auth/refresh", request);
        
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<RefreshTokenResponse>();
            _token = result.Token;
            _refreshToken = result.RefreshToken;
            await SaveTokenAsync(_token, _refreshToken);
        }
        else
        {
            await LogoutAsync();
        }
    }
}
```

### 3.4 MVVM 架构

#### 3.4.1 ViewModel 基类
```csharp
public abstract class ViewModelBase : ObservableObject
{
    private bool _isBusy;
    private string? _errorMessage;

    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    protected async Task ExecuteAsync(Func<Task> operation, string? errorMessage = null)
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;
            await operation();
        }
        catch (Exception ex)
        {
            ErrorMessage = errorMessage ?? ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
```

#### 3.4.2 浏览器管理 ViewModel
```csharp
public class BrowserManagementViewModel : ViewModelBase
{
    private readonly IBrowserApiService _browserService;
    private readonly IGroupApiService _groupService;

    public ObservableCollection<BrowserViewModel> Browsers { get; }
    public ObservableCollection<GroupViewModel> Groups { get; }
    
    public ICommand LoadBrowsersCommand { get; }
    public ICommand CreateBrowserCommand { get; }
    public ICommand DeleteBrowserCommand { get; }
    public ICommand LaunchBrowserCommand { get; }
    public ICommand RefreshCommand { get; }

    public BrowserManagementViewModel(
        IBrowserApiService browserService,
        IGroupApiService groupService)
    {
        _browserService = browserService;
        _groupService = groupService;
        
        Browsers = new ObservableCollection<BrowserViewModel>();
        Groups = new ObservableCollection<GroupViewModel>();
        
        LoadBrowsersCommand = new AsyncRelayCommand(LoadBrowsersAsync);
        CreateBrowserCommand = new AsyncRelayCommand(CreateBrowserAsync);
        DeleteBrowserCommand = new AsyncRelayCommand<int>(DeleteBrowserAsync);
        LaunchBrowserCommand = new AsyncRelayCommand<int>(LaunchBrowserAsync);
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
    }

    private async Task LoadBrowsersAsync()
    {
        await ExecuteAsync(async () =>
        {
            var result = await _browserService.GetBrowsersAsync(new BrowserQueryParams());
            
            Browsers.Clear();
            foreach (var browser in result.Items)
            {
                Browsers.Add(new BrowserViewModel(browser));
            }
        });
    }

    private async Task LaunchBrowserAsync(int id)
    {
        await ExecuteAsync(async () =>
        {
            await _browserService.LaunchBrowserAsync(id);
        }, "Failed to launch browser");
    }
}
```

## 4. 数据库设计

### 4.1 新增表结构

#### Users 表
```sql
CREATE TABLE Users (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Username TEXT NOT NULL UNIQUE,
    Email TEXT NOT NULL UNIQUE,
    PasswordHash TEXT NOT NULL,
    Role TEXT NOT NULL DEFAULT 'User',
    IsActive INTEGER NOT NULL DEFAULT 1,
    CreatedAt TEXT NOT NULL,
    LastLoginAt TEXT,
    LoginFailedCount INTEGER NOT NULL DEFAULT 0,
    LockoutEndTime TEXT
);
```

#### RefreshTokens 表
```sql
CREATE TABLE RefreshTokens (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId INTEGER NOT NULL,
    Token TEXT NOT NULL UNIQUE,
    ExpiresAt TEXT NOT NULL,
    CreatedAt TEXT NOT NULL,
    RevokedAt TEXT,
    FOREIGN KEY (UserId) REFERENCES Users(Id)
);
```

### 4.2 DbContext 更新
```csharp
public class WebScraperDbContext : DbContext
{
    // 现有表
    public DbSet<BrowserEnvironment> BrowserEnvironments { get; set; }
    public DbSet<BrowserGroup> BrowserGroups { get; set; }
    public DbSet<FingerprintProfile> FingerprintProfiles { get; set; }
    
    // 新增表
    public DbSet<User> Users { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // User 配置
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Role).HasDefaultValue("User");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });
        
        // RefreshToken 配置
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(e => e.UserId);
        });
    }
}
```

## 5. 安全性设计

### 5.1 密码策略
- 最小长度: 8 位
- 必须包含: 字母和数字
- 可选: 特殊字符
- 密码历史: 不能重复最近 3 次使用的密码

### 5.2 账户锁定策略
- 连续失败次数: 5 次
- 锁定时间: 15 分钟
- 自动解锁: 锁定时间到期后自动解锁

### 5.3 Token 安全
- Access Token 有效期: 24 小时
- Refresh Token 有效期: 7 天
- Token 存储: 加密存储在本地
- Token 传输: HTTPS only

### 5.4 API 安全
- 所有 API 需要认证（除登录接口）
- 使用 HTTPS 传输
- 防止 SQL 注入（使用参数化查询）
- 防止 XSS 攻击（输入验证和输出编码）
- 请求频率限制（防止 DDoS）

## 6. 错误处理

### 6.1 API 错误响应格式
```json
{
  "success": false,
  "error": {
    "code": "INVALID_CREDENTIALS",
    "message": "Invalid username or password",
    "details": null
  }
}
```

### 6.2 错误代码
- `INVALID_CREDENTIALS`: 无效的用户名或密码
- `ACCOUNT_LOCKED`: 账户已锁定
- `TOKEN_EXPIRED`: Token 已过期
- `UNAUTHORIZED`: 未授权
- `NOT_FOUND`: 资源不存在
- `VALIDATION_ERROR`: 验证错误
- `INTERNAL_ERROR`: 内部错误

### 6.3 全局异常处理
```csharp
public class ExceptionMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var code = HttpStatusCode.InternalServerError;
        var errorCode = "INTERNAL_ERROR";

        if (exception is UnauthorizedException)
        {
            code = HttpStatusCode.Unauthorized;
            errorCode = "UNAUTHORIZED";
        }
        else if (exception is NotFoundException)
        {
            code = HttpStatusCode.NotFound;
            errorCode = "NOT_FOUND";
        }
        else if (exception is ValidationException)
        {
            code = HttpStatusCode.BadRequest;
            errorCode = "VALIDATION_ERROR";
        }

        var result = JsonSerializer.Serialize(new
        {
            success = false,
            error = new
            {
                code = errorCode,
                message = exception.Message
            }
        });

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)code;
        return context.Response.WriteAsync(result);
    }
}
```

## 7. 性能优化

### 7.1 数据库优化
- 添加适当的索引
- 使用分页查询
- 避免 N+1 查询问题
- 使用异步查询

### 7.2 API 优化
- 响应压缩（Gzip）
- 缓存策略（内存缓存）
- 异步处理
- 连接池管理

### 7.3 前端优化
- 虚拟化长列表
- 延迟加载
- 缓存 API 响应
- 防抖和节流

## 8. 日志和监控

### 8.1 日志级别
- **Trace**: 详细的调试信息
- **Debug**: 调试信息
- **Information**: 一般信息
- **Warning**: 警告信息
- **Error**: 错误信息
- **Critical**: 严重错误

### 8.2 日志内容
- 用户登录/登出
- API 请求和响应
- 浏览器启动/停止
- 错误和异常
- 性能指标

### 8.3 日志配置
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "logs/fishbrowser-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30
        }
      },
      {
        "Name": "Console"
      }
    ]
  }
}
```

## 9. 部署架构

### 9.1 开发环境
- API: http://localhost:5000
- Database: SQLite (本地文件)
- WPF: 本地运行

### 9.2 生产环境
- API: HTTPS + 反向代理（Nginx/IIS）
- Database: SQLite（可迁移到 PostgreSQL/MySQL）
- WPF: 客户端安装包

## 10. 测试策略

### 10.1 单元测试
- 服务层测试
- 控制器测试
- ViewModel 测试
- 工具类测试

### 10.2 集成测试
- API 端到端测试
- 数据库集成测试
- 认证流程测试

### 10.3 UI 测试
- 登录流程测试
- 浏览器管理功能测试
- 错误处理测试

---

**文档版本**: 1.0  
**创建日期**: 2025-11-03  
**最后更新**: 2025-11-03
