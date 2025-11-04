using System;
using System.Threading.Tasks;
using FishBrowser.WPF.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace FishBrowser.WPF.Services;

/// <summary>
/// DSL 解析器
/// </summary>
public class DslParser
{
    private readonly ILogService _logger;

    public DslParser(ILogService logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 验证并解析 DSL
    /// </summary>
    public async Task<(bool valid, DslFlow? flow, string? error)> ValidateAndParseAsync(string yaml)
    {
        await Task.CompletedTask;

        try
        {
            if (string.IsNullOrWhiteSpace(yaml))
            {
                _logger.LogWarn("DslParser", "DSL 内容为空");
                return (false, null, "DSL 内容为空");
            }

            // 清理 YAML（移除 Markdown 代码块标记）
            yaml = yaml.Trim();
            if (yaml.StartsWith("```yaml") || yaml.StartsWith("```"))
            {
                var lines = yaml.Split('\n');
                yaml = string.Join('\n', lines.Skip(1).SkipLast(1));
                _logger.LogInfo("DslParser", "Removed markdown code block markers");
            }

            _logger.LogInfo("DslParser", $"Parsing YAML ({yaml.Length} chars)");

            // 解析 YAML
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            var flow = deserializer.Deserialize<DslFlow>(yaml);

            // 基本验证
            if (flow == null)
            {
                return (false, null, "DSL 解析失败：内容为空");
            }

            if (string.IsNullOrWhiteSpace(flow.DslVersion))
            {
                return (false, null, "缺少 dslVersion 字段");
            }

            if (string.IsNullOrWhiteSpace(flow.Id))
            {
                return (false, null, "缺少 id 字段");
            }

            if (flow.Steps == null || flow.Steps.Count == 0)
            {
                return (false, null, "缺少 steps 或 steps 为空");
            }

            _logger.LogInfo("DslParser", $"DSL 验证成功: {flow.Name} ({flow.Steps.Count} steps)");
            return (true, flow, null);
        }
        catch (Exception ex)
        {
            var errorMsg = $"DSL 解析失败: {ex.Message}";
            var detailMsg = $"错误详情:\n类型: {ex.GetType().Name}\n消息: {ex.Message}";
            
            // 如果是 YAML 解析错误，尝试提供更多上下文
            if (ex is YamlDotNet.Core.YamlException yamlEx)
            {
                detailMsg += $"\n位置: Line {yamlEx.Start.Line}, Column {yamlEx.Start.Column}";
            }
            
            _logger.LogError("DslParser", errorMsg, detailMsg);
            return (false, null, $"YAML 解析错误: {ex.Message}");
        }
    }
}
