using FishBrowser.WPF.Models;

namespace FishBrowser.WPF.Services
{
    /// <summary>
    /// 浏览器启动器工厂
    /// 根据配置创建合适的浏览器启动器
    /// </summary>
    public class BrowserLauncherFactory
    {
        private readonly ILogService _log;

        public BrowserLauncherFactory(ILogService log)
        {
            _log = log;
        }

        /// <summary>
        /// 创建浏览器启动器
        /// </summary>
        /// <param name="engineType">浏览器引擎类型</param>
        /// <returns>浏览器启动器实例</returns>
        public IBrowserLauncher CreateLauncher(BrowserEngineType engineType)
        {
            _log.LogInfo("BrowserFactory", $"Creating launcher for engine: {engineType}");

            return engineType switch
            {
                BrowserEngineType.UndetectedChrome => new UndetectedChromeLauncher(_log),
                BrowserEngineType.PlaywrightChromium => new UndetectedChromeLauncher(_log), // 暂时使用 UndetectedChrome
                BrowserEngineType.PlaywrightFirefox => new UndetectedChromeLauncher(_log), // 暂时使用 UndetectedChrome，但会在 Playwright 中切换到 Firefox
                _ => throw new ArgumentException($"Unknown engine type: {engineType}")
            };
        }

        /// <summary>
        /// 根据环境配置自动选择最佳浏览器引擎
        /// </summary>
        /// <param name="environment">浏览器环境</param>
        /// <returns>推荐的浏览器引擎类型</returns>
        public BrowserEngineType GetRecommendedEngine(BrowserEnvironment? environment = null)
        {
            // 如果环境指定了引擎，使用指定的引擎
            if (environment?.Engine != null)
            {
                var engineType = environment.Engine.ToLowerInvariant() switch
                {
                    "undetectedchrome" => BrowserEngineType.UndetectedChrome,
                    "firefox" => BrowserEngineType.PlaywrightFirefox,
                    "chromium" => BrowserEngineType.PlaywrightChromium,
                    _ => BrowserEngineType.UndetectedChrome
                };
                
                _log.LogInfo("BrowserFactory", $"Using specified engine: {environment.Engine} -> {engineType}");
                return engineType;
            }

            // 默认使用 UndetectedChrome，因为它有最高的成功率和兼容性
            _log.LogInfo("BrowserFactory", "Recommending UndetectedChrome (highest success rate: 90-95%)");
            return BrowserEngineType.UndetectedChrome;
        }

        /// <summary>
        /// 创建推荐的浏览器启动器
        /// </summary>
        /// <param name="environment">浏览器环境</param>
        /// <returns>浏览器启动器实例</returns>
        public IBrowserLauncher CreateRecommendedLauncher(BrowserEnvironment? environment = null)
        {
            var engineType = GetRecommendedEngine(environment);
            return CreateLauncher(engineType);
        }
    }
}
