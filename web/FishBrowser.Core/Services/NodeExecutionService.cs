using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FishBrowser.WPF.Services
{
    /// <summary>
    /// Node.js 脚本执行服务
    /// </summary>
    public class NodeExecutionService
    {
        private readonly LogService _logService;

        public NodeExecutionService(LogService logService)
        {
            _logService = logService;
        }

        /// <summary>
        /// 执行 Node.js 脚本
        /// </summary>
        public async Task<ExecutionResult> ExecuteScriptAsync(string script, bool debug = false, string? geminiApiKey = null, string? geminiModel = null)
        {
            var result = new ExecutionResult();
            var tempFile = Path.Combine(Path.GetTempPath(), $"stagehand-{Guid.NewGuid()}.js");

            try
            {
                _logService.LogInfo("NodeExecution", $"Creating temp script file: {tempFile}");
                
                // 写入脚本到临时文件
                await File.WriteAllTextAsync(tempFile, script, Encoding.UTF8);
                
                // 记录脚本内容（用于调试）
                if (debug)
                {
                    _logService.LogInfo("NodeExecution", $"Script content:\n{script}");
                }

                // 获取全局 node_modules 路径
                var globalNodeModules = GetGlobalNodeModulesPath();
                
                // 执行脚本
                var startInfo = new ProcessStartInfo
                {
                    FileName = "node",
                    Arguments = $"\"{tempFile}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };

                // 设置 NODE_PATH 环境变量，让 Node.js 能找到全局模块
                if (!string.IsNullOrEmpty(globalNodeModules))
                {
                    startInfo.EnvironmentVariables["NODE_PATH"] = globalNodeModules;
                    _logService.LogInfo("NodeExecution", $"Set NODE_PATH to: {globalNodeModules}");
                }
                
                // 设置 Gemini API Key 环境变量
                if (!string.IsNullOrEmpty(geminiApiKey))
                {
                    startInfo.EnvironmentVariables["GOOGLE_GENERATIVE_AI_API_KEY"] = geminiApiKey;
                    _logService.LogInfo("NodeExecution", "Set GOOGLE_GENERATIVE_AI_API_KEY");
                }
                
                // 设置 Gemini 模型环境变量（可选）
                if (!string.IsNullOrEmpty(geminiModel))
                {
                    startInfo.EnvironmentVariables["STAGEHAND_MODEL"] = geminiModel;
                    _logService.LogInfo("NodeExecution", $"Set STAGEHAND_MODEL to: {geminiModel}");
                }

                var outputBuilder = new StringBuilder();
                var errorBuilder = new StringBuilder();

                using (var process = new Process { StartInfo = startInfo })
                {
                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            outputBuilder.AppendLine(e.Data);
                            if (debug)
                            {
                                // 使用 Console 避免 DbContext 并发问题
                                Console.WriteLine($"[NodeExecution] [STDOUT] {e.Data}");
                            }
                        }
                    };

                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            errorBuilder.AppendLine(e.Data);
                            if (debug)
                            {
                                // 使用 Console 避免 DbContext 并发问题
                                Console.WriteLine($"[NodeExecution] [STDERR] {e.Data}");
                            }
                        }
                    };

                    _logService.LogInfo("NodeExecution", "Starting Node.js process...");
                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    // 等待执行完成（最多 5 分钟）
                    var completed = await Task.Run(() => process.WaitForExit(300000));

                    if (!completed)
                    {
                        process.Kill();
                        throw new TimeoutException("Script execution timed out after 5 minutes");
                    }

                    result.ExitCode = process.ExitCode;
                    result.Output = outputBuilder.ToString();
                    result.Error = errorBuilder.ToString();
                    result.Success = process.ExitCode == 0;

                    if (result.Success)
                    {
                        _logService.LogInfo("NodeExecution", $"Script executed successfully. Exit code: {result.ExitCode}");
                    }
                    else
                    {
                        _logService.LogError("NodeExecution", $"Script execution failed. Exit code: {result.ExitCode}\nError: {result.Error}");
                    }
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = $"Execution error: {ex.Message}";
                _logService.LogError("NodeExecution", $"Failed to execute script: {ex.Message}", ex.StackTrace);
            }
            finally
            {
                // 清理临时文件（调试模式下保留）
                if (!debug)
                {
                    try
                    {
                        if (File.Exists(tempFile))
                        {
                            File.Delete(tempFile);
                            _logService.LogInfo("NodeExecution", $"Temp file deleted: {tempFile}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logService.LogWarn("NodeExecution", $"Failed to delete temp file: {ex.Message}");
                    }
                }
                else
                {
                    _logService.LogInfo("NodeExecution", $"Debug mode: Temp file kept at {tempFile}");
                }
            }

            return result;
        }

        /// <summary>
        /// 检查 Node.js 是否已安装
        /// </summary>
        public async Task<bool> IsNodeInstalledAsync()
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "node",
                    Arguments = "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(startInfo))
                {
                    if (process != null)
                    {
                        var output = await process.StandardOutput.ReadToEndAsync();
                        await Task.Run(() => process.WaitForExit());
                        return process.ExitCode == 0 && !string.IsNullOrEmpty(output);
                    }
                }
            }
            catch
            {
                // Node.js 未安装
            }

            return false;
        }

        /// <summary>
        /// 获取 Node.js 版本
        /// </summary>
        public async Task<string?> GetNodeVersionAsync()
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "node",
                    Arguments = "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(startInfo))
                {
                    if (process != null)
                    {
                        var output = await process.StandardOutput.ReadToEndAsync();
                        await Task.Run(() => process.WaitForExit());
                        return output.Trim();
                    }
                }
            }
            catch
            {
                // 忽略错误
            }

            return null;
        }

        /// <summary>
        /// 获取全局 node_modules 路径
        /// </summary>
        private string? GetGlobalNodeModulesPath()
        {
            try
            {
                // Windows: %APPDATA%\npm\node_modules
                // Linux/Mac: /usr/local/lib/node_modules 或 ~/.npm-global/lib/node_modules
                
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    var npmPath = Path.Combine(appData, "npm", "node_modules");
                    
                    if (Directory.Exists(npmPath))
                    {
                        return npmPath;
                    }
                }
                else
                {
                    // Linux/Mac
                    var paths = new[]
                    {
                        "/usr/local/lib/node_modules",
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".npm-global", "lib", "node_modules")
                    };
                    
                    foreach (var path in paths)
                    {
                        if (Directory.Exists(path))
                        {
                            return path;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logService.LogWarn("NodeExecution", $"Failed to get global node_modules path: {ex.Message}");
            }
            
            return null;
        }
    }

    /// <summary>
    /// 脚本执行结果
    /// </summary>
    public class ExecutionResult
    {
        public bool Success { get; set; }
        public int ExitCode { get; set; }
        public string Output { get; set; } = "";
        public string Error { get; set; } = "";
        public DateTime ExecutedAt { get; set; } = DateTime.Now;
    }
}
