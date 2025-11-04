using System.Text;
using FishBrowser.Api.Configuration;
using FishBrowser.Api.Middleware;
using FishBrowser.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using FishBrowser.WPF.Data;
using FishBrowser.WPF.Services;
using FishBrowser.WPF.Infrastructure.Configuration;
using FishBrowser.WPF.Infrastructure.Data;
using FreeSql;

var builder = WebApplication.CreateBuilder(args);

// 配置 JWT
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()!;
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// 配置 DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<WebScraperDbContext>(options =>
    options.UseSqlite(connectionString));

// 注册 FreeSql (SQLite)
builder.Services.AddSingleton<IFreeSql>(sp =>
{
    var dbPath = builder.Configuration["Database:Path"] ?? connectionString?.Replace("Data Source=", "");
    var fsql = new FreeSqlBuilder()
        .UseConnectionString(FreeSql.DataType.Sqlite, $"Data Source={dbPath}")
        .UseAutoSyncStructure(false)
        .Build();
    return fsql;
});

// 注册迁移管理器为 Scoped（避免 Singleton 依赖 Scoped 的问题）
builder.Services.AddScoped<FreeSqlMigrationManager>();

// 注册基础服务（只注册 API 需要的服务，避免复杂的依赖问题）
builder.Services.AddScoped<LogService>();
builder.Services.AddScoped<ILogService, LogService>();
builder.Services.AddScoped<DatabaseService>();
builder.Services.AddScoped<IDatabaseService, DatabaseService>();
builder.Services.AddScoped<FontService>();
builder.Services.AddScoped<GpuCatalogService>();

// 启动浏览器所需的服务（与 WPF 启动逻辑一致）
builder.Services.AddScoped<FingerprintService>();
builder.Services.AddScoped<SecretService>();
builder.Services.AddScoped<BrowserSessionService>();
builder.Services.AddScoped<ChromeVersionDatabase>();
builder.Services.AddScoped<AntiDetectionService>();

// 注册认证服务
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITokenService, TokenService>();

// 注册浏览器启动服务
builder.Services.AddSingleton<FishBrowser.Api.Services.BrowserLaunchService>();

// 注册随机浏览器生成服务（从 Core 项目）
builder.Services.AddScoped<BrowserRandomGenerator>();

// 注册浏览器环境服务（用于保存浏览器，与 WPF 逻辑一致）
builder.Services.AddScoped<UserAgentCompositionService>();
builder.Services.AddScoped<DependencyResolutionService>();
builder.Services.AddScoped<FingerprintGeneratorService>();
builder.Services.AddScoped<FingerprintCollectorService>(); // ⭐ 指纹收集服务（生成完整 JSON）
builder.Services.AddScoped<BrowserEnvironmentService>();
builder.Services.AddScoped<BrowserSessionService>(); // ⭐ 会话管理服务（持久化目录管理）
builder.Services.AddScoped<BrowserFingerprintService>(); // ⭐ 指纹信息服务（文本和 JSON 生成）
builder.Services.AddScoped<PlaywrightMaintenanceService>();
builder.Services.AddScoped<StagehandMaintenanceService>(); // Stagehand AI 自动化框架维护
builder.Services.AddScoped<StagehandTaskService>(); // Stagehand 任务服务
builder.Services.AddScoped<NodeExecutionService>(); // Node.js 执行服务
builder.Services.AddScoped<AIProviderManagementService>();
builder.Services.AddScoped<IAIProviderService, AIProviderService>();
builder.Services.AddScoped<IAIClientService, AIClientService>(); // AI 客户端服务

// 配置 CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWeb", policy =>
    {
        policy.WithOrigins("http://localhost:5001", "https://localhost:5001")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "FishBrowser API",
        Version = "v1",
        Description = "FishBrowser 管理后台 API"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// 初始化数据库和服务
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        // 1. 使用 FreeSql 迁移管理器初始化数据库（包含所有表）
        logger.LogInformation("开始初始化数据库...");
        var migrationManager = scope.ServiceProvider.GetRequiredService<FreeSqlMigrationManager>();
        migrationManager.InitializeDatabase();
        
        var stats = migrationManager.GetStatistics();
        logger.LogInformation("数据库初始化完成: {TableCount} 个表, 大小: {Size}", 
            stats.TableCount, stats.GetFormattedSize());
        
        // 2. 创建认证表（Users 和 RefreshTokens）
        var context = scope.ServiceProvider.GetRequiredService<WebScraperDbContext>();
        var connection = context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            connection.Open();

        using (var command = connection.CreateCommand())
        {
            // 删除旧的 Users 表（如果存在日期时间格式问题）
            command.CommandText = "DROP TABLE IF EXISTS Users; DROP TABLE IF EXISTS RefreshTokens;";
            command.ExecuteNonQuery();
            
            command.CommandText = @"
                CREATE TABLE Users (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Username TEXT NOT NULL,
                    Email TEXT NOT NULL,
                    PasswordHash TEXT NOT NULL,
                    Role TEXT NOT NULL DEFAULT 'User',
                    IsActive INTEGER NOT NULL DEFAULT 1,
                    CreatedAt TEXT NOT NULL,
                    LastLoginAt TEXT,
                    LoginFailedCount INTEGER NOT NULL DEFAULT 0,
                    LockoutEndTime TEXT
                );
                
                CREATE UNIQUE INDEX IF NOT EXISTS IX_Users_Username ON Users(Username);
                CREATE UNIQUE INDEX IF NOT EXISTS IX_Users_Email ON Users(Email);
                
                CREATE TABLE IF NOT EXISTS RefreshTokens (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserId INTEGER NOT NULL,
                    Token TEXT NOT NULL,
                    ExpiresAt TEXT NOT NULL,
                    CreatedAt TEXT NOT NULL,
                    RevokedAt TEXT
                );
                
                CREATE UNIQUE INDEX IF NOT EXISTS IX_RefreshTokens_Token ON RefreshTokens(Token);
                CREATE INDEX IF NOT EXISTS IX_RefreshTokens_UserId ON RefreshTokens(UserId);
                CREATE INDEX IF NOT EXISTS IX_RefreshTokens_ExpiresAt ON RefreshTokens(ExpiresAt);
            ";
            command.ExecuteNonQuery();
        }
        
        logger.LogInformation("认证表创建完成");
        
        // 3. 初始化默认管理员账户
        DbInitializer.Initialize(context);
        
        // 4. 导入字体种子数据
        try
        {
            var fontService = scope.ServiceProvider.GetRequiredService<FontService>();
            await fontService.ImportSeedAsync();
            logger.LogInformation("字体种子数据导入完成");
        }
        catch (Exception fx)
        {
            logger.LogWarning(fx, "字体种子数据导入失败");
        }
        
        // 5. 导入 GPU 种子数据
        try
        {
            var gpuService = scope.ServiceProvider.GetRequiredService<GpuCatalogService>();
            await gpuService.ImportSeedAsync();
            logger.LogInformation("GPU 种子数据导入完成");
        }
        catch (Exception gx)
        {
            logger.LogWarning(gx, "GPU 种子数据导入失败");
        }
        
        logger.LogInformation("========================================");
        logger.LogInformation("数据库和服务初始化全部完成！");
        logger.LogInformation("========================================");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "数据库初始化失败");
        throw;
    }
}

// Configure the HTTP request pipeline.
// ⭐ 全局异常处理中间件（必须在最前面）
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "FishBrowser API V1");
    });
}

app.UseCors("AllowWeb");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
