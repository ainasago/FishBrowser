using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FishBrowser.WPF.Models;

namespace FishBrowser.WPF.Services;

public class AIService
{
    private readonly DatabaseService _dbService;
    private readonly LogService _logService;

    public AIService(DatabaseService dbService, LogService logService)
    {
        _dbService = dbService;
        _logService = logService;
    }

    /// <summary>
    /// 生成文章摘要（占位实现）
    /// </summary>
    public async Task<string> GenerateSummaryAsync(string content, int maxLength = 200)
    {
        try
        {
            // TODO: 接入真实 AI API（OpenAI、Azure 等）
            // 当前为占位实现
            await Task.Delay(100);
            
            var summary = content.Length > maxLength 
                ? content.Substring(0, maxLength) + "..." 
                : content;
            
            _logService.LogInfo("AIService", $"Generated summary (length: {summary.Length})");
            return summary;
        }
        catch (Exception ex)
        {
            _logService.LogError("AIService", $"Failed to generate summary: {ex.Message}", ex.StackTrace);
            throw;
        }
    }

    /// <summary>
    /// 分类文章（占位实现）
    /// </summary>
    public async Task<(string Category, double Confidence)> ClassifyArticleAsync(string title, string content)
    {
        try
        {
            // TODO: 接入真实 AI 分类模型
            // 当前为占位实现
            await Task.Delay(100);
            
            var categories = new[] { "科技", "财经", "体育", "娱乐", "生活" };
            var random = new Random();
            var category = categories[random.Next(categories.Length)];
            var confidence = 0.75 + random.NextDouble() * 0.25;
            
            _logService.LogInfo("AIService", $"Classified article as '{category}' (confidence: {confidence:P})");
            return (category, confidence);
        }
        catch (Exception ex)
        {
            _logService.LogError("AIService", $"Failed to classify article: {ex.Message}", ex.StackTrace);
            throw;
        }
    }

    /// <summary>
    /// 提取关键词（占位实现）
    /// </summary>
    public async Task<List<string>> ExtractKeywordsAsync(string content, int count = 5)
    {
        try
        {
            // TODO: 接入真实关键词提取模型
            await Task.Delay(100);
            
            var keywords = content.Split(' ')
                .Where(w => w.Length > 3)
                .Take(count)
                .ToList();
            
            _logService.LogInfo("AIService", $"Extracted {keywords.Count} keywords");
            return keywords;
        }
        catch (Exception ex)
        {
            _logService.LogError("AIService", $"Failed to extract keywords: {ex.Message}", ex.StackTrace);
            throw;
        }
    }
}
