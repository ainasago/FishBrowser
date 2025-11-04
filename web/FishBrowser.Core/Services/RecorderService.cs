using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FishBrowser.WPF.Models;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace FishBrowser.WPF.Services;

/// <summary>
/// 录制服务 - 将用户操作转换为 DSL 脚本
/// </summary>
public class RecorderService
{
    private readonly ILogService _logger;
    private readonly List<RecordedAction> _actions = new();
    private bool _isRecording;

    public RecorderService(ILogService logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public bool IsRecording => _isRecording;
    public int ActionCount => _actions.Count;

    /// <summary>
    /// 开始录制
    /// </summary>
    public void Start()
    {
        _isRecording = true;
        _actions.Clear();
        _logger.LogInfo("RecorderService", "Recording started");
    }

    /// <summary>
    /// 停止录制
    /// </summary>
    public void Stop()
    {
        _isRecording = false;
        _logger.LogInfo("RecorderService", $"Recording stopped, total actions: {_actions.Count}");
    }

    /// <summary>
    /// 添加动作
    /// </summary>
    public void AddAction(RecordedAction action)
    {
        if (!_isRecording) return;

        _actions.Add(action);
        _logger.LogInfo("RecorderService", $"Action recorded: {action.Type} - {action.Selector?.Value}");
    }

    /// <summary>
    /// 处理来自浏览器的消息
    /// </summary>
    public void HandleBrowserMessage(string message)
    {
        try
        {
            var data = JsonSerializer.Deserialize<JsonElement>(message);
            var type = data.GetProperty("type").GetString();

            switch (type)
            {
                case "action_recorded":
                    var actionData = data.GetProperty("action");
                    var action = ParseAction(actionData);
                    if (action != null)
                    {
                        AddAction(action);
                    }
                    break;

                case "recording_started":
                    _logger.LogInfo("RecorderService", "Browser recording started");
                    break;

                case "recording_stopped":
                    _logger.LogInfo("RecorderService", "Browser recording stopped");
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("RecorderService", $"Failed to handle message: {ex.Message}", ex.StackTrace);
        }
    }

    /// <summary>
    /// 解析动作
    /// </summary>
    private RecordedAction? ParseAction(JsonElement actionData)
    {
        try
        {
            var type = actionData.GetProperty("type").GetString() ?? "";
            var timestamp = actionData.GetProperty("timestamp").GetInt64();

            DslSelector? selector = null;
            if (actionData.TryGetProperty("selector", out var selectorData))
            {
                selector = new DslSelector
                {
                    Type = selectorData.GetProperty("type").GetString() ?? "css",
                    Value = selectorData.GetProperty("value").GetString() ?? ""
                };
            }

            string? value = null;
            if (actionData.TryGetProperty("value", out var valueData))
            {
                value = valueData.GetString();
            }

            string? url = null;
            if (actionData.TryGetProperty("url", out var urlData))
            {
                url = urlData.GetString();
            }

            return new RecordedAction
            {
                Type = type,
                Selector = selector,
                Value = value,
                Url = url,
                Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(timestamp).DateTime
            };
        }
        catch (Exception ex)
        {
            _logger.LogError("RecorderService", $"Failed to parse action: {ex.Message}", ex.StackTrace);
            return null;
        }
    }

    /// <summary>
    /// 转换为 DSL 脚本
    /// </summary>
    public string ConvertToDsl(string flowName = "录制的任务")
    {
        var sb = new StringBuilder();
        sb.AppendLine("dslVersion: v1.0");
        sb.AppendLine($"id: recorded_{DateTime.Now:yyyyMMddHHmmss}");
        sb.AppendLine($"name: {flowName}");
        sb.AppendLine("steps:");

        foreach (var action in _actions)
        {
            sb.AppendLine(ConvertActionToDsl(action));
        }

        return sb.ToString();
    }

    /// <summary>
    /// 转换单个动作为 DSL
    /// </summary>
    private string ConvertActionToDsl(RecordedAction action)
    {
        var sb = new StringBuilder();

        switch (action.Type)
        {
            case "navigate":
                sb.AppendLine($"  - type: open");
                sb.AppendLine($"    url: {action.Url}");
                break;

            case "click":
                sb.AppendLine($"  - type: click");
                AppendSelector(sb, action.Selector);
                break;

            case "fill":
            case "fill_password":
                sb.AppendLine($"  - type: fill");
                AppendSelector(sb, action.Selector);
                sb.AppendLine($"    value: \"{EscapeYaml(action.Value ?? "")}\"");
                break;

            case "select":
                sb.AppendLine($"  - type: fill");
                AppendSelector(sb, action.Selector);
                sb.AppendLine($"    value: \"{EscapeYaml(action.Value ?? "")}\"");
                break;

            case "check":
            case "uncheck":
                sb.AppendLine($"  - type: click");
                AppendSelector(sb, action.Selector);
                break;

            case "submit":
                sb.AppendLine($"  - type: click");
                AppendSelector(sb, action.Selector);
                break;

            default:
                _logger.LogWarn("RecorderService", $"Unknown action type: {action.Type}");
                break;
        }

        return sb.ToString();
    }

    /// <summary>
    /// 添加选择器到 YAML
    /// </summary>
    private void AppendSelector(StringBuilder sb, DslSelector? selector)
    {
        if (selector == null) return;

        sb.AppendLine($"    selector:");
        sb.AppendLine($"      type: {selector.Type}");
        sb.AppendLine($"      value: \"{EscapeYaml(selector.Value)}\"");
    }

    /// <summary>
    /// 转义 YAML 字符串
    /// </summary>
    private string EscapeYaml(string value)
    {
        return value.Replace("\"", "\\\"").Replace("\n", "\\n");
    }

    /// <summary>
    /// 获取录制的动作列表
    /// </summary>
    public List<RecordedAction> GetActions()
    {
        return new List<RecordedAction>(_actions);
    }

    /// <summary>
    /// 清空动作
    /// </summary>
    public void Clear()
    {
        _actions.Clear();
        _logger.LogInfo("RecorderService", "Actions cleared");
    }
}

/// <summary>
/// 录制的动作
/// </summary>
public class RecordedAction
{
    public string Type { get; set; } = string.Empty;
    public DslSelector? Selector { get; set; }
    public string? Value { get; set; }
    public string? Url { get; set; }
    public DateTime Timestamp { get; set; }
}
