using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using FishBrowser.WPF.Data;
using FishBrowser.WPF.Engine;
using FishBrowser.WPF.Models;
using LogLevel = FishBrowser.WPF.Models.LogLevel;

namespace FishBrowser.WPF.Services;

/// <summary>
/// 任务测试运行器服务
/// </summary>
public class TaskTestRunnerService
{
    private readonly WebScraperDbContext _db;
    private readonly ILogService _logger;
    private readonly FingerprintService _fingerprintService;
    private readonly SecretService _secretService;
    private readonly BrowserEnvironmentService _envService;
    private readonly IServiceProvider _serviceProvider;
    private readonly DslParser _dslParser;
    private readonly DslExecutor _dslExecutor;

    public TaskTestRunnerService(
        WebScraperDbContext db,
        ILogService logger,
        FingerprintService fingerprintService,
        SecretService secretService,
        BrowserEnvironmentService envService,
        IServiceProvider serviceProvider,
        DslParser dslParser,
        DslExecutor dslExecutor)
    {
        _db = db;
        _logger = logger;
        _fingerprintService = fingerprintService;
        _secretService = secretService;
        _envService = envService;
        _serviceProvider = serviceProvider;
        _dslParser = dslParser;
        _dslExecutor = dslExecutor;
    }

    /// <summary>
    /// 运行测试
    /// </summary>
    public async Task<TestRunResult> RunTestAsync(
        string dslYaml,
        TestRunOptions options,
        IProgress<TestProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.Now;
        var result = new TestRunResult();
        IBrowserController? controller = null;
        string? tempUserDataPath = null;

        try
        {
            _logger.LogInfo("TaskTestRunner", "Starting test run");
            
            // 记录 DSL 脚本预览
            var dslPreview = dslYaml.Length > 500 ? dslYaml.Substring(0, 500) + "..." : dslYaml;
            _logger.LogInfo("TaskTestRunner", $"DSL Script Preview:\n{dslPreview}");

            // 1. 验证 DSL
            progress?.Report(new TestProgress
            {
                Stage = TestStage.Initializing,
                Message = "验证 DSL 脚本...",
                Level = LogLevel.Info
            });

            var (valid, flow, error) = await ValidateDslAsync(dslYaml);
            if (!valid || flow == null)
            {
                result.Success = false;
                result.ErrorMessage = error ?? "DSL 验证失败";
                progress?.Report(new TestProgress
                {
                    Stage = TestStage.Failed,
                    Message = result.ErrorMessage,
                    Level = LogLevel.Error
                });
                return result;
            }

            result.TotalSteps = flow.Steps?.Count ?? 0;

            // 2. 生成或获取指纹配置
            progress?.Report(new TestProgress
            {
                Stage = TestStage.GeneratingFingerprint,
                Message = "生成随机指纹配置...",
                Level = LogLevel.Info
            });

            FingerprintProfile fingerprint;
            if (options.UseRandomFingerprint)
            {
                fingerprint = GenerateRandomFingerprint();
            }
            else if (options.FingerprintProfileId.HasValue)
            {
                fingerprint = await _db.FingerprintProfiles
                    .FindAsync(options.FingerprintProfileId.Value)
                    ?? throw new Exception($"Fingerprint profile {options.FingerprintProfileId} not found");
                _logger.LogInfo("TaskTestRunner", $"Using existing fingerprint: {fingerprint.Name}");
            }
            else
            {
                throw new Exception("Must specify fingerprint profile or use random");
            }

            // 3. 创建临时环境
            tempUserDataPath = CreateTempUserDataPath();
            _logger.LogInfo("TaskTestRunner", $"Created temp user data path: {tempUserDataPath}");

            // 4. 启动浏览器
            progress?.Report(new TestProgress
            {
                Stage = TestStage.StartingBrowser,
                Message = "启动浏览器...",
                Level = LogLevel.Info
            });

            var pw = new PlaywrightController(_logger as LogService, _fingerprintService, _secretService);
            controller = new PlaywrightControllerAdapter(pw);
            await controller.InitializeAsync(
                fingerprint,
                proxy: null,
                headless: options.Headless,
                userDataPath: options.Headless ? null : tempUserDataPath
            );

            _logger.LogInfo("TaskTestRunner", "Browser started successfully");

            // 5. 执行 DSL 步骤
            progress?.Report(new TestProgress
            {
                Stage = TestStage.ExecutingSteps,
                CurrentStep = 0,
                TotalSteps = result.TotalSteps,
                Message = "开始执行步骤...",
                Level = LogLevel.Info
            });

            // 使用 DslExecutor 执行步骤
            await _dslExecutor.ExecuteAsync(flow, controller, progress, cancellationToken);

            result.Success = true;
            result.StepsExecuted = flow.Steps?.Count ?? 0;

            progress?.Report(new TestProgress
            {
                Stage = TestStage.Completed,
                CurrentStep = result.TotalSteps,
                TotalSteps = result.TotalSteps,
                Message = "测试完成",
                Level = LogLevel.Info
            });

            _logger.LogInfo("TaskTestRunner", "Test completed successfully");
        }
        catch (OperationCanceledException)
        {
            result.Success = false;
            result.ErrorMessage = "测试被取消";
            _logger.LogWarn("TaskTestRunner", "Test was cancelled");

            progress?.Report(new TestProgress
            {
                Stage = TestStage.Failed,
                Message = "测试被取消",
                Level = LogLevel.Warning
            });
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            _logger.LogError("TaskTestRunner", $"Test failed: {ex.Message}", ex.StackTrace);

            progress?.Report(new TestProgress
            {
                Stage = TestStage.Failed,
                Message = $"测试失败: {ex.Message}",
                Level = LogLevel.Error
            });
        }
        finally
        {
            // 6. 清理资源
            progress?.Report(new TestProgress
            {
                Stage = TestStage.CleaningUp,
                Message = "清理资源...",
                Level = LogLevel.Info
            });

            if (controller != null)
            {
                await controller.DisposeAsync();
                _logger.LogInfo("TaskTestRunner", "Browser closed");
            }

            if (options.CleanupAfterTest && tempUserDataPath != null && Directory.Exists(tempUserDataPath))
            {
                try
                {
                    Directory.Delete(tempUserDataPath, recursive: true);
                    _logger.LogInfo("TaskTestRunner", $"Cleaned up temp directory: {tempUserDataPath}");
                }
                catch (Exception ex)
                {
                    _logger.LogWarn("TaskTestRunner", $"Failed to cleanup temp directory: {ex.Message}");
                }
            }

            result.Duration = DateTime.Now - startTime;
            _logger.LogInfo("TaskTestRunner", $"Test run completed in {result.Duration.TotalSeconds:F2}s");
        }

        return result;
    }

    /// <summary>
    /// 验证 DSL
    /// </summary>
    private async Task<(bool valid, DslFlow? flow, string? error)> ValidateDslAsync(string yaml)
    {
        return await _dslParser.ValidateAndParseAsync(yaml);
    }

    /// <summary>
    /// 生成随机指纹（使用与"一键随机"相同的逻辑）
    /// </summary>
    private FingerprintProfile GenerateRandomFingerprint()
    {
        // 使用 BrowserEnvironmentService 的随机生成逻辑
        var opts = new BrowserEnvironmentService.RandomizeOptions();
        var randomEnv = _envService.BuildRandomDraft(opts);
        // 直接从 draft 构建非持久化 FingerprintProfile（完整复用 UA/Traits 逻辑）
        var profile = _envService.BuildProfileFromDraft(randomEnv);
        profile.Name = $"Test_{DateTime.Now:yyyyMMdd_HHmmss}";
        profile.IsPreset = false;
        _logger.LogInfo("TaskTestRunner", $"Generated random fingerprint: {profile.Name} (OS: {randomEnv.OS}, Engine: {randomEnv.Engine})");
        return profile;
    }

    /// <summary>
    /// 创建临时用户数据路径
    /// </summary>
    private string CreateTempUserDataPath()
    {
        var tempPath = Path.Combine(
            Path.GetTempPath(),
            "WebScraperTest",
            $"test_{Guid.NewGuid():N}"
        );

        Directory.CreateDirectory(tempPath);
        return tempPath;
    }
}

