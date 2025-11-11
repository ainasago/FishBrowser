using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace FishBrowser.Core.Services
{
    /// <summary>
    /// Cloudflare 绕过服务 - 基于 CF-Ares Python 服务
    /// </summary>
    public class CloudflareAresService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<CloudflareAresService> _logger;
        private readonly string _serviceUrl;

        public CloudflareAresService(ILogger<CloudflareAresService> logger, string serviceUrl = "http://localhost:5000")
        {
            _logger = logger;
            _serviceUrl = serviceUrl;
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(2)
            };
        }

        /// <summary>
        /// 健康检查
        /// </summary>
        public async Task<bool> HealthCheckAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_serviceUrl}/health");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CF-Ares 服务健康检查失败");
                return false;
            }
        }

        /// <summary>
        /// 解决 Cloudflare 挑战
        /// </summary>
        public async Task<CloudflareSolveResult> SolveChallengeAsync(
            string url,
            string? proxy = null,
            bool headless = true,
            string browserEngine = "undetected",
            int timeout = 60)
        {
            try
            {
                _logger.LogInformation("开始解决 Cloudflare 挑战: {Url}", url);

                var requestData = new
                {
                    url = url,
                    proxy = proxy,
                    headless = headless,
                    browser_engine = browserEngine,
                    timeout = timeout
                };

                var json = System.Text.Json.JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_serviceUrl}/solve", content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("CF-Ares 服务返回错误: {StatusCode}, {Body}", response.StatusCode, responseBody);
                    return new CloudflareSolveResult
                    {
                        Success = false,
                        Error = $"服务返回错误: {response.StatusCode}"
                    };
                }

                var result = System.Text.Json.JsonSerializer.Deserialize<CloudflareSolveResult>(responseBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (result?.Success == true)
                {
                    _logger.LogInformation("Cloudflare 挑战成功! Cookies: {Count} 个", result.Cookies?.Count ?? 0);
                }
                else
                {
                    _logger.LogWarning("Cloudflare 挑战失败: {Error}", result?.Error);
                }

                return result ?? new CloudflareSolveResult { Success = false, Error = "解析响应失败" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "解决 Cloudflare 挑战时发生错误");
                return new CloudflareSolveResult
                {
                    Success = false,
                    Error = $"异常: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 获取已保存的会话
        /// </summary>
        public async Task<CloudflareSessionResult> GetSessionAsync(string url)
        {
            try
            {
                var requestData = new { url = url };
                var json = System.Text.Json.JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_serviceUrl}/get_session", content);
                var responseBody = await response.Content.ReadAsStringAsync();

                var result = System.Text.Json.JsonSerializer.Deserialize<CloudflareSessionResult>(responseBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result ?? new CloudflareSessionResult { Success = false };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取会话时发生错误");
                return new CloudflareSessionResult { Success = false };
            }
        }

        /// <summary>
        /// 验证会话是否有效
        /// </summary>
        public async Task<bool> VerifySessionAsync(string url, Dictionary<string, string> cookies, string userAgent)
        {
            try
            {
                var requestData = new
                {
                    url = url,
                    cookies = cookies,
                    user_agent = userAgent
                };

                var json = System.Text.Json.JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_serviceUrl}/verify_session", content);
                var responseBody = await response.Content.ReadAsStringAsync();

                var result = System.Text.Json.JsonSerializer.Deserialize<CloudflareVerifyResult>(responseBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result?.Valid == true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证会话时发生错误");
                return false;
            }
        }

        /// <summary>
        /// 创建带有 Cloudflare cookies 的 HttpClient
        /// </summary>
        public HttpClient CreateHttpClientWithCookies(Dictionary<string, string> cookies, string userAgent, string baseUrl)
        {
            var handler = new HttpClientHandler
            {
                UseCookies = true,
                CookieContainer = new System.Net.CookieContainer()
            };

            // 添加 cookies
            var uri = new Uri(baseUrl);
            foreach (var cookie in cookies)
            {
                handler.CookieContainer.Add(uri, new System.Net.Cookie(cookie.Key, cookie.Value));
            }

            var client = new HttpClient(handler);
            client.DefaultRequestHeaders.Add("User-Agent", userAgent);
            client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
            client.DefaultRequestHeaders.Add("Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8");
            client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");

            return client;
        }
    }

    /// <summary>
    /// Cloudflare 挑战解决结果
    /// </summary>
    public class CloudflareSolveResult
    {
        public bool Success { get; set; }
        public Dictionary<string, string>? Cookies { get; set; }
        public string? UserAgent { get; set; }
        public string? SessionFile { get; set; }
        public string? ClientId { get; set; }
        public int StatusCode { get; set; }
        public string? Message { get; set; }
        public string? Error { get; set; }
    }

    /// <summary>
    /// Cloudflare 会话结果
    /// </summary>
    public class CloudflareSessionResult
    {
        public bool Success { get; set; }
        public bool Exists { get; set; }
        public Dictionary<string, string>? Cookies { get; set; }
        public string? UserAgent { get; set; }
        public string? SessionFile { get; set; }
        public string? Message { get; set; }
    }

    /// <summary>
    /// Cloudflare 会话验证结果
    /// </summary>
    public class CloudflareVerifyResult
    {
        public bool Success { get; set; }
        public bool Valid { get; set; }
        public int StatusCode { get; set; }
        public string? Message { get; set; }
    }
}
