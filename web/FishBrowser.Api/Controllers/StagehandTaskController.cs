using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FishBrowser.WPF.Models;
using FishBrowser.WPF.Services;
using System;
using System.Threading.Tasks;

namespace FishBrowser.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class StagehandTaskController : ControllerBase
    {
        private readonly StagehandTaskService _taskService;
        private readonly StagehandMaintenanceService _maintenanceService;
        private readonly NodeExecutionService _nodeExecutionService;
        private readonly IAIClientService _aiClient;
        private readonly LogService _logService;

        public StagehandTaskController(
            StagehandTaskService taskService,
            StagehandMaintenanceService maintenanceService,
            NodeExecutionService nodeExecutionService,
            IAIClientService aiClient,
            LogService logService)
        {
            _taskService = taskService;
            _maintenanceService = maintenanceService;
            _nodeExecutionService = nodeExecutionService;
            _aiClient = aiClient;
            _logService = logService;
        }

        /// <summary>
        /// 获取 Stagehand 状态
        /// </summary>
        [HttpGet("status")]
        public async Task<IActionResult> GetStatus()
        {
            try
            {
                var nodeInstalled = await _nodeExecutionService.IsNodeInstalledAsync();
                var stagehandStatus = await _maintenanceService.GetStatusAsync();

                return Ok(new
                {
                    nodeInstalled,
                    nodeVersion = await _nodeExecutionService.GetNodeVersionAsync(),
                    stagehandInstalled = stagehandStatus.IsInstalled,
                    stagehandVersion = stagehandStatus.InstalledVersion,
                    playwrightInstalled = stagehandStatus.PlaywrightInstalled
                });
            }
            catch (Exception ex)
            {
                _logService.LogError("StagehandController", $"Get status failed: {ex.Message}", ex.StackTrace);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// 获取快捷示例
        /// </summary>
        [HttpGet("examples")]
        public IActionResult GetExamples()
        {
            try
            {
                var examples = _taskService.GetExamples();
                return Ok(examples);
            }
            catch (Exception ex)
            {
                _logService.LogError("StagehandController", $"Get examples failed: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// 生成脚本
        /// </summary>
        [HttpPost("generate")]
        public async Task<IActionResult> GenerateScript([FromBody] GenerateScriptRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.UserMessage))
                {
                    return BadRequest(new { error = "用户消息不能为空" });
                }

                if (request.ProviderId == 0)
                {
                    return BadRequest(new { error = "请选择 AI 提供商" });
                }

                // 生成脚本
                var response = await _taskService.GenerateScriptAsync(
                    request,
                    async (systemPrompt, userMessage, providerId) =>
                    {
                        // 使用通用的 GenerateAsync 方法，分别传递系统提示词和用户消息
                        var aiRequest = new AIRequest
                        {
                            SystemPrompt = systemPrompt,
                            UserPrompt = userMessage,
                            ProviderId = providerId
                        };
                        
                        var aiResponse = await _aiClient.GenerateAsync(aiRequest);
                        return aiResponse.Content ?? "";
                    });

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logService.LogError("StagehandController", $"Generate script failed: {ex.Message}", ex.StackTrace);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// 执行脚本
        /// </summary>
        [HttpPost("execute")]
        public async Task<IActionResult> ExecuteScript([FromBody] ExecuteScriptRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Script))
                {
                    return BadRequest(new { error = "脚本内容不能为空" });
                }

                // 检查 Node.js
                var nodeInstalled = await _nodeExecutionService.IsNodeInstalledAsync();
                if (!nodeInstalled)
                {
                    return BadRequest(new { error = "Node.js 未安装" });
                }

                // 执行脚本（传递 Gemini 设置）
                var result = await _nodeExecutionService.ExecuteScriptAsync(
                    request.Script, 
                    request.Debug, 
                    request.GeminiApiKey, 
                    request.GeminiModel);

                return Ok(new ExecuteScriptResponse
                {
                    Success = result.Success,
                    ExitCode = result.ExitCode,
                    Output = result.Output,
                    Error = result.Error,
                    ExecutedAt = result.ExecutedAt
                });
            }
            catch (Exception ex)
            {
                _logService.LogError("StagehandController", $"Execute script failed: {ex.Message}", ex.StackTrace);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// 分析脚本
        /// </summary>
        [HttpPost("analyze")]
        public IActionResult AnalyzeScript([FromBody] string script)
        {
            try
            {
                var analysis = _taskService.AnalyzeScript(script);
                return Ok(analysis);
            }
            catch (Exception ex)
            {
                _logService.LogError("StagehandController", $"Analyze script failed: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
