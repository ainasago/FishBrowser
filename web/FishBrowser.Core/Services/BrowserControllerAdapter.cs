using FishBrowser.WPF.Models;
using FishBrowser.WPF.Engine;

namespace FishBrowser.WPF.Services
{
    /// <summary>
    /// 浏览器控制器适配器
    /// 将新的 IBrowserLauncher 接口适配到现有的 PlaywrightController 流程
    /// 保持向后兼容性，同时支持新的 UndetectedChrome 引擎
    /// </summary>
    public class BrowserControllerAdapter : IAsyncDisposable
    {
        private readonly ILogService _log;
        private readonly LogService _logService;
        private readonly FingerprintService _fingerprintService;
        private readonly SecretService _secretService;
        private readonly BrowserLauncherFactory _launcherFactory;
        
        private IBrowserLauncher? _launcher;
        private PlaywrightController? _playwrightController;
        private bool _useUndetectedChrome = true; // 默认使用 UndetectedChrome
        private string _browserType = "chromium"; // 默认使用 Chromium (chromium | firefox)

        public BrowserControllerAdapter(
            ILogService log,
            FingerprintService fingerprintService,
            SecretService secretService)
        {
            _log = log;
            _logService = log as LogService ?? throw new ArgumentException("LogService implementation required");
            _fingerprintService = fingerprintService;
            _secretService = secretService;
            _launcherFactory = new BrowserLauncherFactory(log);
        }

        /// <summary>
        /// 设置是否使用 UndetectedChrome（默认 true）
        /// </summary>
        public void SetUseUndetectedChrome(bool use)
        {
            _useUndetectedChrome = use;
            _log.LogInfo("BrowserAdapter", $"UndetectedChrome mode: {use}");
        }

        /// <summary>
        /// 设置浏览器类型（用于 Playwright）
        /// </summary>
        /// <param name="browserType">浏览器类型：chromium | firefox</param>
        public void SetBrowserType(string browserType)
        {
            _browserType = browserType.ToLowerInvariant();
            _log.LogInfo("BrowserAdapter", $"Browser type set to: {_browserType}");
        }

        /// <summary>
        /// 初始化浏览器
        /// </summary>
        public async Task InitializeBrowserAsync(
            FingerprintProfile profile,
            ProxyConfig? proxy = null,
            bool headless = false,
            string? userDataPath = null,
            bool loadAutoma = false,
            BrowserEnvironment? environment = null)
        {
            _log.LogInfo("BrowserAdapter", "========== Initializing Browser ==========");
            _log.LogInfo("BrowserAdapter", $"Mode: {(_useUndetectedChrome ? "UndetectedChrome" : $"Playwright ({_browserType})")}");
            _log.LogInfo("BrowserAdapter", $"Profile: {profile.Name}");
            _log.LogInfo("BrowserAdapter", $"Headless: {headless}");
            _log.LogInfo("BrowserAdapter", $"UserDataPath: {userDataPath ?? "temp"}");

            if (_useUndetectedChrome)
            {
                await InitializeUndetectedChromeAsync(profile, proxy, headless, userDataPath, environment);
            }
            else
            {
                await InitializePlaywrightAsync(profile, proxy, headless, userDataPath, loadAutoma, environment);
            }
        }

        private async Task InitializeUndetectedChromeAsync(
            FingerprintProfile profile,
            ProxyConfig? proxy,
            bool headless,
            string? userDataPath,
            BrowserEnvironment? environment)
        {
            _log.LogInfo("BrowserAdapter", "Using UndetectedChrome launcher");
            
            _launcher = _launcherFactory.CreateRecommendedLauncher(environment);
            await _launcher.LaunchAsync(profile, userDataPath, headless, proxy, environment);
            
            _log.LogInfo("BrowserAdapter", "UndetectedChrome initialized successfully");
        }

        private async Task InitializePlaywrightAsync(
            FingerprintProfile profile,
            ProxyConfig? proxy,
            bool headless,
            string? userDataPath,
            bool loadAutoma,
            BrowserEnvironment? environment)
        {
            _log.LogInfo("BrowserAdapter", $"Using Playwright controller with {_browserType} browser");
            
            _playwrightController = new PlaywrightController(_logService, _fingerprintService, _secretService);
            await _playwrightController.InitializeBrowserAsync(
                profile, 
                proxy: null, // Playwright 使用自己的代理格式
                headless, 
                userDataPath, 
                loadAutoma, 
                environment,
                _browserType); // 传递浏览器类型
            
            _log.LogInfo("BrowserAdapter", $"Playwright {_browserType} initialized successfully");
        }

        /// <summary>
        /// 导航到指定 URL
        /// </summary>
        public async Task NavigateAsync(string url)
        {
            if (_launcher != null)
            {
                await _launcher.NavigateAsync(url);
            }
            else if (_playwrightController != null)
            {
                await _playwrightController.NavigateAsync(url);
            }
            else
            {
                throw new InvalidOperationException("Browser not initialized");
            }
        }

        /// <summary>
        /// 等待浏览器关闭
        /// </summary>
        public async Task WaitForCloseAsync()
        {
            if (_launcher != null)
            {
                await _launcher.WaitForCloseAsync();
            }
            else if (_playwrightController != null)
            {
                await _playwrightController.WaitForCloseAsync();
            }
        }

        /// <summary>
        /// 获取当前页面标题
        /// </summary>
        public async Task<string> GetTitleAsync()
        {
            if (_launcher != null)
            {
                return await _launcher.GetTitleAsync();
            }
            else if (_playwrightController != null)
            {
                // Playwright 没有直接的 GetTitle 方法，需要添加
                return "N/A";
            }
            else
            {
                throw new InvalidOperationException("Browser not initialized");
            }
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                if (_launcher != null)
                {
                    _launcher.Dispose();
                    _launcher = null;
                }

                if (_playwrightController != null)
                {
                    await _playwrightController.DisposeAsync();
                    _playwrightController = null;
                }
            }
            catch (Exception ex)
            {
                _log.LogError("BrowserAdapter", $"Error disposing: {ex.Message}", ex.StackTrace);
            }
        }
    }
}
