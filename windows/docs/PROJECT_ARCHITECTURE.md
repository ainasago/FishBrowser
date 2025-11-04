# 🏗️ WebScraper 项目架构规划

## 项目现状分析

### 当前问题
1. ❌ 每次启动都删除数据库 (EnsureDeleted)
2. ❌ 代码混乱，没有清晰的分层
3. ❌ 数据库迁移没有管理
4. ❌ 业务逻辑和 UI 混在一起
5. ❌ 没有统一的错误处理
6. ❌ 配置管理混乱

## 目标架构

### 分层设计 (DDD + Clean Architecture)

```
┌─────────────────────────────────────────────────────┐
│                   Presentation Layer                 │
│  (WPF Views, ViewModels, User Interactions)         │
└────────────────────┬────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────┐
│                Application Layer                     │
│  (Use Cases, DTOs, Service Orchestration)           │
└────────────────────┬────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────┐
│                 Domain Layer                         │
│  (Entities, Value Objects, Domain Services)         │
└────────────────────┬────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────┐
│              Infrastructure Layer                    │
│  (Database, External APIs, File System)             │
└─────────────────────────────────────────────────────┘
```

### 项目结构

```
WebScraperApp/
├── Presentation/                    # 表现层 (WPF)
│   ├── Views/                       # XAML 视图
│   │   ├── DashboardView.xaml
│   │   ├── TaskManagementView.xaml
│   │   ├── FingerprintConfigView.xaml
│   │   ├── ProxyPoolView.xaml
│   │   ├── AIResultsView.xaml
│   │   └── SettingsView.xaml
│   ├── ViewModels/                  # 视图模型
│   │   ├── DashboardViewModel.cs
│   │   ├── TaskManagementViewModel.cs
│   │   ├── FingerprintConfigViewModel.cs
│   │   ├── ProxyPoolViewModel.cs
│   │   ├── AIResultsViewModel.cs
│   │   └── SettingsViewModel.cs
│   └── Converters/                  # 值转换器
│       ├── StatusToColorConverter.cs
│       └── DateTimeConverter.cs
│
├── Application/                     # 应用层
│   ├── Services/                    # 应用服务
│   │   ├── TaskApplicationService.cs
│   │   ├── FingerprintApplicationService.cs
│   │   ├── ProxyApplicationService.cs
│   │   ├── ScrapingApplicationService.cs
│   │   └── AIApplicationService.cs
│   ├── DTOs/                        # 数据传输对象
│   │   ├── TaskDTO.cs
│   │   ├── FingerprintDTO.cs
│   │   ├── ProxyDTO.cs
│   │   └── ArticleDTO.cs
│   ├── Mappers/                     # 对象映射
│   │   ├── TaskMapper.cs
│   │   ├── FingerprintMapper.cs
│   │   └── ProxyMapper.cs
│   └── Validators/                  # 数据验证
│       ├── TaskValidator.cs
│       ├── FingerprintValidator.cs
│       └── ProxyValidator.cs
│
├── Domain/                          # 领域层
│   ├── Entities/                    # 领域实体
│   │   ├── ScrapingTask.cs
│   │   ├── FingerprintProfile.cs
│   │   ├── ProxyServer.cs
│   │   ├── Article.cs
│   │   ├── LogEntry.cs
│   │   ├── AISummary.cs
│   │   └── AIClassification.cs
│   ├── ValueObjects/                # 值对象
│   │   ├── TaskStatus.cs
│   │   ├── ProxyStatus.cs
│   │   └── ScrapingResult.cs
│   ├── Repositories/                # 仓储接口
│   │   ├── ITaskRepository.cs
│   │   ├── IFingerprintRepository.cs
│   │   ├── IProxyRepository.cs
│   │   ├── IArticleRepository.cs
│   │   └── ILogRepository.cs
│   ├── Services/                    # 领域服务
│   │   ├── FingerprintDomainService.cs
│   │   ├── ProxyDomainService.cs
│   │   └── TaskDomainService.cs
│   └── Specifications/              # 查询规范
│       ├── TaskSpecification.cs
│       ├── FingerprintSpecification.cs
│       └── ProxySpecification.cs
│
├── Infrastructure/                  # 基础设施层
│   ├── Data/                        # 数据访问
│   │   ├── WebScraperDbContext.cs
│   │   ├── Migrations/              # 数据库迁移
│   │   │   ├── Migration_001_InitialSchema.cs
│   │   │   ├── Migration_002_AddFingerprintFields.cs
│   │   │   └── ...
│   │   ├── Repositories/            # 仓储实现
│   │   │   ├── TaskRepository.cs
│   │   │   ├── FingerprintRepository.cs
│   │   │   ├── ProxyRepository.cs
│   │   │   └── ...
│   │   └── FreeSqlMigrationManager.cs  # FreeSql 迁移管理
│   ├── External/                    # 外部集成
│   │   ├── PlaywrightController.cs
│   │   ├── HtmlParser.cs
│   │   ├── AIClient.cs
│   │   └── ProxyValidator.cs
│   ├── Configuration/               # 配置管理
│   │   ├── DatabaseConfig.cs
│   │   ├── PlaywrightConfig.cs
│   │   ├── AIConfig.cs
│   │   └── AppSettings.json
│   └── Logging/                     # 日志
│       ├── LogService.cs
│       └── LogEntry.cs
│
├── Engine/                          # 业务引擎
│   ├── TaskScheduler.cs
│   ├── FingerprintManager.cs
│   ├── ProxyManager.cs
│   └── ScrapingEngine.cs
│
├── Common/                          # 公共工具
│   ├── Constants/
│   │   ├── TaskStatusConstants.cs
│   │   ├── ProxyStatusConstants.cs
│   │   └── ErrorMessages.cs
│   ├── Exceptions/
│   │   ├── DomainException.cs
│   │   ├── ApplicationException.cs
│   │   ├── RepositoryException.cs
│   │   └── ExternalServiceException.cs
│   ├── Extensions/
│   │   ├── StringExtensions.cs
│   │   ├── DateTimeExtensions.cs
│   │   └── EnumerableExtensions.cs
│   └── Utilities/
│       ├── JsonHelper.cs
│       ├── ValidationHelper.cs
│       └── DateTimeHelper.cs
│
├── App.xaml
├── App.xaml.cs
├── MainWindow.xaml
├── MainWindow.xaml.cs
├── Program.cs
└── WebScraperApp.csproj
```

## 数据库迁移方案 (FreeSql)

### FreeSql 集成

```csharp
// Infrastructure/Data/FreeSqlMigrationManager.cs
public class FreeSqlMigrationManager
{
    private readonly IFreeSql _fsql;
    private readonly ILogger _logger;

    public FreeSqlMigrationManager(IFreeSql fsql, ILogger logger)
    {
        _fsql = fsql;
        _logger = logger;
    }

    /// <summary>
    /// 初始化数据库 (自动迁移)
    /// </summary>
    public void InitializeDatabase()
    {
        try
        {
            // 自动创建表和列
            _fsql.CodeFirst
                .ConfigEntity<ScrapingTask>(e => e.Name("scraping_tasks"))
                .ConfigEntity<FingerprintProfile>(e => e.Name("fingerprint_profiles"))
                .ConfigEntity<ProxyServer>(e => e.Name("proxy_servers"))
                .ConfigEntity<Article>(e => e.Name("articles"))
                .ConfigEntity<LogEntry>(e => e.Name("log_entries"))
                .ConfigEntity<AISummary>(e => e.Name("ai_summaries"))
                .ConfigEntity<AIClassification>(e => e.Name("ai_classifications"))
                .SyncStructure();  // 同步表结构

            _logger.LogInfo("Database", "Database initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError("Database", $"Failed to initialize database: {ex.Message}", ex.StackTrace);
            throw;
        }
    }

    /// <summary>
    /// 检查并同步数据库表结构
    /// </summary>
    public void SyncSchema()
    {
        try
        {
            _fsql.CodeFirst.SyncStructure();
            _logger.LogInfo("Database", "Database schema synced successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError("Database", $"Failed to sync schema: {ex.Message}", ex.StackTrace);
            throw;
        }
    }
}
```

### 配置示例

```csharp
// Infrastructure/Configuration/DatabaseConfig.cs
public static class DatabaseConfig
{
    public static IServiceCollection AddDatabaseServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        // 注册 FreeSql
        services.AddSingleton<IFreeSql>(sp =>
        {
            var fsql = new FreeSql.FreeSqlBuilder()
                .UseConnectionString(FreeSql.DataType.Sqlite, connectionString)
                .UseAutoSyncStructure(true)  // 自动同步表结构
                .UseNoneCommandTimeout()
                .Build();

            return fsql;
        });

        // 注册迁移管理器
        services.AddSingleton<FreeSqlMigrationManager>();

        // 注册仓储
        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddScoped<IFingerprintRepository, FingerprintRepository>();
        services.AddScoped<IProxyRepository, ProxyRepository>();
        services.AddScoped<IArticleRepository, ArticleRepository>();
        services.AddScoped<ILogRepository, LogRepository>();

        return services;
    }
}
```

## 应用启动流程

### 改进的启动流程

```csharp
// App.xaml.cs
public partial class App : Application
{
    public static IHost? Host { get; set; }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            // 1. 构建主机
            Host = new HostBuilder()
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.SetBasePath(AppDomain.CurrentDomain.BaseDirectory);
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                })
                .ConfigureServices((context, services) =>
                {
                    // 配置数据库
                    services.AddDatabaseServices(context.Configuration);

                    // 配置应用服务
                    services.AddApplicationServices();

                    // 配置领域服务
                    services.AddDomainServices();

                    // 配置基础设施
                    services.AddInfrastructureServices();

                    // 配置日志
                    services.AddLogging(config =>
                    {
                        config.AddConsole();
                        config.AddDebug();
                    });
                })
                .Build();

            // 2. 初始化数据库 (使用 FreeSql 自动迁移)
            using (var scope = Host.Services.CreateScope())
            {
                var migrationManager = scope.ServiceProvider
                    .GetRequiredService<FreeSqlMigrationManager>();
                migrationManager.InitializeDatabase();
            }

            // 3. 启动 Playwright
            await PlaywrightInstaller.EnsurePlaywrightInstalledAsync();

            // 4. 显示主窗口
            this.Resources["Host"] = Host;
            var mainWindow = new MainWindow();
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Application startup failed: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
            this.Shutdown(1);
        }
    }
}
```

## 分层示例

### 1. 领域层 (Domain)

```csharp
// Domain/Entities/ScrapingTask.cs
public class ScrapingTask : AggregateRoot
{
    public string Name { get; set; }
    public string Url { get; set; }
    public TaskStatus Status { get; set; }
    public int FingerprintProfileId { get; set; }
    public int? ProxyServerId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    // 导航属性
    public FingerprintProfile FingerprintProfile { get; set; }
    public ProxyServer ProxyServer { get; set; }
    public ICollection<Article> Articles { get; set; }
}

// Domain/Repositories/ITaskRepository.cs
public interface ITaskRepository : IRepository<ScrapingTask>
{
    Task<ScrapingTask> GetByIdAsync(int id);
    Task<List<ScrapingTask>> GetByStatusAsync(TaskStatus status);
    Task<List<ScrapingTask>> GetAllAsync();
}
```

### 2. 应用层 (Application)

```csharp
// Application/Services/TaskApplicationService.cs
public class TaskApplicationService
{
    private readonly ITaskRepository _taskRepository;
    private readonly IFingerprintRepository _fingerprintRepository;
    private readonly ILogger _logger;

    public TaskApplicationService(
        ITaskRepository taskRepository,
        IFingerprintRepository fingerprintRepository,
        ILogger logger)
    {
        _taskRepository = taskRepository;
        _fingerprintRepository = fingerprintRepository;
        _logger = logger;
    }

    public async Task<TaskDTO> CreateTaskAsync(CreateTaskCommand command)
    {
        // 验证
        var validator = new TaskValidator();
        var validationResult = await validator.ValidateAsync(command);
        if (!validationResult.IsValid)
            throw new ApplicationException(validationResult.ToString());

        // 创建任务
        var task = new ScrapingTask
        {
            Name = command.Name,
            Url = command.Url,
            Status = TaskStatus.Pending,
            FingerprintProfileId = command.FingerprintProfileId,
            CreatedAt = DateTime.UtcNow
        };

        // 保存
        await _taskRepository.AddAsync(task);
        await _taskRepository.SaveChangesAsync();

        _logger.LogInfo("TaskService", $"Task created: {task.Name}");

        // 返回 DTO
        return TaskMapper.ToDTO(task);
    }
}

// Application/DTOs/TaskDTO.cs
public class TaskDTO
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Url { get; set; }
    public string Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### 3. 表现层 (Presentation)

```csharp
// Presentation/ViewModels/TaskManagementViewModel.cs
public class TaskManagementViewModel : INotifyPropertyChanged
{
    private readonly TaskApplicationService _taskService;
    private ObservableCollection<TaskDTO> _tasks;

    public TaskManagementViewModel(TaskApplicationService taskService)
    {
        _taskService = taskService;
        _tasks = new ObservableCollection<TaskDTO>();
    }

    public async Task LoadTasksAsync()
    {
        try
        {
            var tasks = await _taskService.GetAllTasksAsync();
            _tasks.Clear();
            foreach (var task in tasks)
                _tasks.Add(task);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load tasks: {ex.Message}");
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
}
```

## 依赖注入配置

### 扩展方法

```csharp
// Infrastructure/ServiceCollectionExtensions.cs
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDatabaseServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 数据库配置
        return services;
    }

    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services)
    {
        services.AddScoped<TaskApplicationService>();
        services.AddScoped<FingerprintApplicationService>();
        services.AddScoped<ProxyApplicationService>();
        services.AddScoped<ScrapingApplicationService>();
        services.AddScoped<AIApplicationService>();
        return services;
    }

    public static IServiceCollection AddDomainServices(
        this IServiceCollection services)
    {
        services.AddScoped<FingerprintDomainService>();
        services.AddScoped<ProxyDomainService>();
        services.AddScoped<TaskDomainService>();
        return services;
    }

    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services)
    {
        services.AddScoped<PlaywrightController>();
        services.AddScoped<HtmlParser>();
        services.AddScoped<AIClient>();
        return services;
    }
}
```

## 优势

### 1. 清晰的职责分离
- 每层有明确的职责
- 易于测试和维护
- 易于扩展

### 2. 数据库管理
- FreeSql 自动管理表结构
- 不再删除数据库
- 自动同步新字段

### 3. 代码组织
- 逻辑清晰
- 易于导航
- 易于协作

### 4. 可维护性
- 低耦合
- 高内聚
- 易于重构

## 迁移计划

### Phase 1: 基础设施 (1-2 天)
- [ ] 创建项目文件夹结构
- [ ] 集成 FreeSql
- [ ] 配置数据库迁移
- [ ] 创建仓储接口和实现

### Phase 2: 领域层 (1 天)
- [ ] 创建领域实体
- [ ] 创建值对象
- [ ] 创建领域服务

### Phase 3: 应用层 (1-2 天)
- [ ] 创建应用服务
- [ ] 创建 DTO 和映射
- [ ] 创建验证器

### Phase 4: 表现层 (1-2 天)
- [ ] 创建 ViewModel
- [ ] 更新 View
- [ ] 集成应用服务

### Phase 5: 测试和优化 (1 天)
- [ ] 单元测试
- [ ] 集成测试
- [ ] 性能优化

---

**总工作量**: 5-7 天  
**优先级**: 高 (改善代码质量和可维护性)  
**风险**: 低 (逐步迁移，保持功能不变)
