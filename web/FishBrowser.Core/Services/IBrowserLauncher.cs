using FishBrowser.WPF.Models;

namespace FishBrowser.WPF.Services
{
    /// <summary>
    /// 浏览器启动器接口
    /// 定义统一的浏览器启动和管理接口
    /// </summary>
    public interface IBrowserLauncher : IDisposable
    {
        /// <summary>
        /// 启动浏览器
        /// </summary>
        /// <param name="profile">指纹配置</param>
        /// <param name="userDataPath">用户数据目录（可选）</param>
        /// <param name="headless">是否无头模式</param>
        /// <param name="proxy">代理配置（可选）</param>
        /// <param name="environment">浏览器环境（可选，用于自定义配置）</param>
        Task LaunchAsync(
            FingerprintProfile profile,
            string? userDataPath = null,
            bool headless = false,
            ProxyConfig? proxy = null,
            BrowserEnvironment? environment = null);

        /// <summary>
        /// 导航到指定 URL
        /// </summary>
        Task NavigateAsync(string url);

        /// <summary>
        /// 获取当前页面标题
        /// </summary>
        Task<string> GetTitleAsync();

        /// <summary>
        /// 获取当前页面源代码
        /// </summary>
        Task<string> GetPageSourceAsync();

        /// <summary>
        /// 检查浏览器是否仍在运行
        /// </summary>
        bool IsRunning();

        /// <summary>
        /// 等待浏览器关闭
        /// </summary>
        Task WaitForCloseAsync();

        /// <summary>
        /// 浏览器引擎类型
        /// </summary>
        BrowserEngineType EngineType { get; }
    }

    /// <summary>
    /// 浏览器引擎类型
    /// </summary>
    public enum BrowserEngineType
    {
        /// <summary>
        /// Playwright Chromium
        /// </summary>
        PlaywrightChromium,

        /// <summary>
        /// Playwright Firefox
        /// </summary>
        PlaywrightFirefox,

        /// <summary>
        /// Selenium UndetectedChromeDriver
        /// </summary>
        UndetectedChrome
    }

    /// <summary>
    /// 代理配置
    /// </summary>
    public class ProxyConfig
    {
        public string? Server { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
    }
}
