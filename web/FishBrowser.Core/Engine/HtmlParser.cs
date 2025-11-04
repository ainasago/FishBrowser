using AngleSharp;
using AngleSharp.Html.Parser;
using System;
using System.Linq;
using System.Threading.Tasks;
using FishBrowser.WPF.Services;
using AngleSharp.Dom;

namespace FishBrowser.WPF.Engine;

public class HtmlParser
{
    private readonly LogService _logService;
    private readonly IHtmlParser _parser;

    public HtmlParser(LogService logService)
    {
        _logService = logService;
        var config = Configuration.Default;
        _parser = new AngleSharp.Html.Parser.HtmlParser(new HtmlParserOptions { IsEmbedded = false });
    }

    /// <summary>
    /// 抽取文章信息
    /// </summary>
    public async Task<(string Title, string Content, string? Author, DateTime? PublishedAt)> ExtractArticleAsync(string html)
    {
        try
        {
            var document = await _parser.ParseDocumentAsync(html);

            // 抽取标题
            var title = document.QuerySelector("h1")?.TextContent?.Trim() 
                ?? document.QuerySelector("title")?.TextContent?.Trim() 
                ?? "Unknown";

            // 抽取正文（简化实现）
            var contentElements = document.QuerySelectorAll("article p, .content p, .post-content p, main p");
            var content = string.Join("\n", contentElements.Select(e => e.TextContent?.Trim()).Where(t => !string.IsNullOrEmpty(t)));

            // 抽取作者
            var author = document.QuerySelector("[rel='author']")?.TextContent?.Trim()
                ?? document.QuerySelector(".author")?.TextContent?.Trim()
                ?? null;

            // 抽取发布时间
            DateTime? publishedAt = null;
            var timeElement = document.QuerySelector("time");
            if (timeElement != null && DateTime.TryParse(timeElement.GetAttribute("datetime") ?? timeElement.TextContent, out var parsedTime))
            {
                publishedAt = parsedTime;
            }

            _logService.LogInfo("HtmlParser", $"Extracted article: {title} (content length: {content.Length})");

            return (title, content, author, publishedAt);
        }
        catch (Exception ex)
        {
            _logService.LogError("HtmlParser", $"Failed to extract article: {ex.Message}", ex.StackTrace);
            throw;
        }
    }

    /// <summary>
    /// 使用 XPath 自定义抽取
    /// </summary>
    public async Task<string> ExtractByXPathAsync(string html, string xpath)
    {
        try
        {
            var document = await _parser.ParseDocumentAsync(html);
            // AngleSharp 不直接支持 XPath，使用 CSS 选择器替代
            var selector = ConvertXPathToCss(xpath);
            var elements = document.QuerySelectorAll(selector);
            var result = string.Join("\n", elements.Select(e => e.TextContent?.Trim()));
            return result;
        }
        catch (Exception ex)
        {
            _logService.LogError("HtmlParser", $"Failed to extract by XPath: {ex.Message}", ex.StackTrace);
            throw;
        }
    }

    private string ConvertXPathToCss(string xpath)
    {
        // 简化的 XPath 到 CSS 转换（实际应用需要更复杂的转换）
        return xpath.Replace("//", " ").Replace("[@", "[").Replace("]", "]");
    }
}
