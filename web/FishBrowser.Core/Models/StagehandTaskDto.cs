using System;
using System.Collections.Generic;

namespace FishBrowser.WPF.Models
{
    /// <summary>
    /// Stagehand 任务 DTO
    /// </summary>
    public class StagehandTaskDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Script { get; set; } = "";
        public int ActionCount { get; set; }
        public int EstimatedSeconds { get; set; }
        public string Complexity { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public DateTime? LastRunAt { get; set; }
        public int RunCount { get; set; }
    }

    /// <summary>
    /// Stagehand 聊天消息
    /// </summary>
    public class StagehandChatMessageDto
    {
        public string Role { get; set; } = "";
        public string Content { get; set; } = "";
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 脚本生成请求
    /// </summary>
    public class GenerateScriptRequest
    {
        public string UserMessage { get; set; } = "";
        public int ProviderId { get; set; }
        public List<StagehandChatMessageDto> History { get; set; } = new();
    }

    /// <summary>
    /// 脚本生成响应
    /// </summary>
    public class GenerateScriptResponse
    {
        public bool Success { get; set; }
        public string Script { get; set; } = "";
        public string Message { get; set; } = "";
        public int ActionCount { get; set; }
        public int EstimatedSeconds { get; set; }
        public string Complexity { get; set; } = "";
    }

    /// <summary>
    /// 脚本执行请求
    /// </summary>
    public class ExecuteScriptRequest
    {
        public string Script { get; set; } = "";
        public bool Debug { get; set; }
        public string? GeminiApiKey { get; set; }
        public string? GeminiModel { get; set; }
    }

    /// <summary>
    /// 脚本执行响应
    /// </summary>
    public class ExecuteScriptResponse
    {
        public bool Success { get; set; }
        public int ExitCode { get; set; }
        public string Output { get; set; } = "";
        public string Error { get; set; } = "";
        public DateTime ExecutedAt { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 快捷示例
    /// </summary>
    public class StagehandExampleDto
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Prompt { get; set; } = "";
        public string Icon { get; set; } = "";
    }
}
