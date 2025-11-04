using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FishBrowser.WPF.Models;
using FishBrowser.WPF.Services;

namespace FishBrowser.WPF.Services
{
    /// <summary>
    /// Stagehand 任务服务
    /// </summary>
    public class StagehandTaskService
    {
        private readonly LogService _logService;

        public StagehandTaskService(LogService logService)
        {
            _logService = logService;
        }

        /// <summary>
        /// 获取快捷示例列表
        /// </summary>
        public List<StagehandExampleDto> GetExamples()
        {
            return new List<StagehandExampleDto>
            {
                new StagehandExampleDto
                {
                    Id = "login",
                    Name = "智能登录",
                    Description = "AI 识别并填写登录表单",
                    Icon = "🔐",
                    Prompt = "创建一个登录 GitHub 的脚本：打开 github.com，点击 Sign in，填写用户名和密码，点击登录按钮"
                },
                new StagehandExampleDto
                {
                    Id = "search",
                    Name = "搜索提取",
                    Description = "搜索并智能提取数据",
                    Icon = "🔍",
                    Prompt = "创建一个搜索脚本：打开 Google，搜索 'Stagehand AI'，提取前 5 个搜索结果的标题和链接"
                },
                new StagehandExampleDto
                {
                    Id = "navigation",
                    Name = "智能导航",
                    Description = "AI 理解页面结构导航",
                    Icon = "🧭",
                    Prompt = "创建一个导航脚本：打开 Amazon，依次点击 Books 分类，然后点击 Best Sellers"
                },
                new StagehandExampleDto
                {
                    Id = "extraction",
                    Name = "数据提取",
                    Description = "提取结构化数据",
                    Icon = "📊",
                    Prompt = "创建一个数据提取脚本：打开 Hacker News 首页，提取前 10 条新闻的标题、分数和评论数"
                },
                new StagehandExampleDto
                {
                    Id = "form",
                    Name = "表单填写",
                    Description = "智能识别并填写表单",
                    Icon = "📝",
                    Prompt = "创建一个表单填写脚本：打开一个联系表单，填写姓名、邮箱和消息内容，然后提交"
                },
                new StagehandExampleDto
                {
                    Id = "shopping",
                    Name = "购物流程",
                    Description = "搜索商品并加入购物车",
                    Icon = "🛒",
                    Prompt = "创建一个购物脚本：在 Amazon 搜索 'laptop'，点击第一个商品，提取商品名称和价格，然后加入购物车"
                }
            };
        }

        /// <summary>
        /// 构建系统提示词
        /// </summary>
        public string BuildSystemPrompt()
        {
            return @"你是一个 Stagehand 脚本生成专家。Stagehand 是一个 AI 驱动的浏览器自动化框架。

## Stagehand 核心 API

⚠️ **重要**：Stagehand 3.x 使用 `stagehand.context.pages()[0]` 获取 page 对象，不再直接使用 `stagehand.page`

1. **获取 Page 对象**
   ```javascript
   const page = stagehand.context.pages()[0];
   await page.goto('https://example.com');
   ```

2. **act(instruction)** - 执行操作
   - 示例：await stagehand.act('点击登录按钮')
   - 示例：await stagehand.act('在搜索框输入 iPhone')

3. **extract(instruction, schema)** - 提取数据
   - 示例：const data = await stagehand.extract('提取商品信息', { name: 'string', price: 'number' })

4. **observe(instruction)** - 观察页面元素
   - 示例：const elements = await stagehand.observe('找到所有商品卡片')

## 脚本模板

```javascript
const { Stagehand } = require('@browserbasehq/stagehand');

(async () => {
    // 初始化 Stagehand
    const stagehand = new Stagehand({
        env: 'LOCAL',
        verbose: 1,
        debugDom: true,
        model: 'google/gemini-2.0-flash-exp'  // 使用 Gemini 模型
    });
    
    try {
        await stagehand.init();
        
        // ⚠️ 获取 page 对象（Stagehand 3.x 新 API）
        const page = stagehand.context.pages()[0];
        
        // 导航到目标网站
        await page.goto('https://example.com');
        
        // 执行操作
        await stagehand.act('你的操作指令');
        
        // 提取数据（如果需要）
        const data = await stagehand.extract('提取指令', {
            // 数据结构定义
        });
        
        console.log('任务完成！', data);
        
    } catch (error) {
        console.error('任务失败:', error);
    } finally {
        await stagehand.close();
    }
})();
```

## 生成规则

1. **必须使用完整的脚本模板**，包含 IIFE 和 async/await
2. **必须先调用 await stagehand.init()** 才能使用 stagehand.page
3. 包含完整的错误处理（try-catch-finally）
4. 使用清晰的注释说明每个步骤
5. act() 指令要具体明确，使用自然语言
6. 合理使用等待和延迟（waitForTimeout）
7. 提取数据时定义清晰的 schema
8. **必须在 finally 中调用 await stagehand.close()**

## 重要提示

⚠️ **常见错误**：
- ❌ 忘记 await stagehand.init()
- ❌ 在 init() 之前使用 stagehand.page
- ❌ 忘记使用 async/await
- ❌ 忘记在 finally 中关闭 stagehand

✅ **正确示例**：
```javascript
const { Stagehand } = require('@browserbasehq/stagehand');

(async () => {
    const stagehand = new Stagehand({ 
        env: 'LOCAL', 
        verbose: 1,
        model: 'google/gemini-2.0-flash-exp'
    });
    
    try {
        await stagehand.init();  // ⚠️ 必须先初始化
        const page = stagehand.context.pages()[0];  // ⚠️ 获取 page 对象（3.x API）
        await page.goto('https://example.com');  // ✅ 现在可以使用 page
        await stagehand.act('点击按钮');
        console.log('完成！');
    } catch (error) {
        console.error('失败:', error);
    } finally {
        await stagehand.close();  // ⚠️ 必须关闭
    }
})();
```

请根据用户需求生成 Stagehand 脚本。只返回 JavaScript 代码，不要有其他解释。";
        }

        /// <summary>
        /// 验证和修复脚本
        /// </summary>
        private string ValidateAndFixScript(string script)
        {
            if (string.IsNullOrEmpty(script))
                return script;

            // 检查是否使用了旧的 API (stagehand.page)
            bool usesOldApi = script.Contains("stagehand.page.");
            
            // 检查是否包含必要的结构
            bool hasRequire = script.Contains("require('@browserbasehq/stagehand')");
            bool hasInit = script.Contains("await stagehand.init()");
            bool hasAsync = script.Contains("(async ()");
            bool hasClose = script.Contains("await stagehand.close()");
            bool hasPageDeclaration = script.Contains("const page = stagehand.context.pages()[0]");

            // 如果使用了旧 API 或缺少 page 声明，需要修复
            if (usesOldApi || (!hasPageDeclaration && script.Contains("stagehand.page")))
            {
                _logService.LogWarn("StagehandTask", "Script uses old API (stagehand.page), fixing...");
                
                // 替换 stagehand.page 为 page
                script = Regex.Replace(script, @"stagehand\.page\.", "page.", RegexOptions.Multiline);
                
                // 如果没有 page 声明，在 init() 后添加
                if (!hasPageDeclaration)
                {
                    script = Regex.Replace(
                        script, 
                        @"(await stagehand\.init\(\);)", 
                        "$1\n        const page = stagehand.context.pages()[0];",
                        RegexOptions.Multiline);
                }
            }
            
            // 检查是否指定了 model 参数
            bool hasModelParam = script.Contains("model:");
            if (!hasModelParam && script.Contains("new Stagehand({"))
            {
                _logService.LogWarn("StagehandTask", "Script missing model parameter, adding...");
                
                // 在 Stagehand 构造函数中添加 model 参数
                script = Regex.Replace(
                    script,
                    @"(new Stagehand\(\{\s*env:\s*'LOCAL',\s*verbose:\s*\d+,?\s*debugDom:\s*true)",
                    "$1,\n        model: 'google/gemini-2.0-flash-exp'",
                    RegexOptions.Multiline);
                
                // 如果上面的模式不匹配，尝试更简单的模式
                if (!script.Contains("model:"))
                {
                    script = Regex.Replace(
                        script,
                        @"(new Stagehand\(\{[^}]+)(}\))",
                        "$1,\n        model: 'google/gemini-2.0-flash-exp'\n    $2",
                        RegexOptions.Multiline);
                }
            }

            // 如果脚本不完整，使用模板包装
            if (!hasRequire || !hasInit || !hasAsync || !hasClose)
            {
                _logService.LogWarn("StagehandTask", "Script is incomplete, wrapping with template");
                
                // 提取核心逻辑（去掉可能存在的不完整包装）
                var coreLogic = ExtractCoreLogic(script);
                
                // 使用完整模板包装
                script = $@"const {{ Stagehand }} = require('@browserbasehq/stagehand');

(async () => {{
    const stagehand = new Stagehand({{
        env: 'LOCAL',
        verbose: 1,
        debugDom: true,
        model: 'google/gemini-2.0-flash-exp'
    }});
    
    try {{
        await stagehand.init();
        const page = stagehand.context.pages()[0];
        
{coreLogic}
        
        console.log('任务完成！');
        
    }} catch (error) {{
        console.error('任务失败:', error);
    }} finally {{
        await stagehand.close();
    }}
}})();";
            }

            return script;
        }

        /// <summary>
        /// 提取核心逻辑
        /// </summary>
        private string ExtractCoreLogic(string script)
        {
            // 移除常见的包装代码
            script = Regex.Replace(script, @"const\s*\{\s*Stagehand\s*\}\s*=\s*require\([^)]+\);?\s*", "", RegexOptions.Multiline);
            script = Regex.Replace(script, @"\(async\s*\(\)\s*=>\s*\{", "", RegexOptions.Multiline);
            script = Regex.Replace(script, @"const\s+stagehand\s*=\s*new\s+Stagehand\([^)]*\);?\s*", "", RegexOptions.Multiline);
            script = Regex.Replace(script, @"await\s+stagehand\.init\(\);?\s*", "", RegexOptions.Multiline);
            script = Regex.Replace(script, @"await\s+stagehand\.close\(\);?\s*", "", RegexOptions.Multiline);
            script = Regex.Replace(script, @"try\s*\{", "", RegexOptions.Multiline);
            script = Regex.Replace(script, @"\}\s*catch\s*\([^)]*\)\s*\{[^}]*\}", "", RegexOptions.Multiline);
            script = Regex.Replace(script, @"\}\s*finally\s*\{[^}]*\}", "", RegexOptions.Multiline);
            script = Regex.Replace(script, @"\}\s*\)\s*\(\s*\);?\s*$", "", RegexOptions.Multiline);
            
            // 确保每行都有适当的缩进
            var lines = script.Split('\n')
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(line => "        " + line);
            
            return string.Join("\n", lines);
        }

        /// <summary>
        /// 清理脚本内容
        /// </summary>
        private string CleanScript(string script)
        {
            if (string.IsNullOrEmpty(script))
                return script;

            // 去掉开头的 ```javascript 或 ```js
            script = Regex.Replace(script, @"^```(javascript|js)\s*\n", "", RegexOptions.Multiline);
            
            // 去掉结尾的 ```
            script = Regex.Replace(script, @"\n```\s*$", "", RegexOptions.Multiline);
            
            // 去掉任何其他的 markdown 代码块标记
            script = script.Replace("```javascript", "").Replace("```js", "").Replace("```", "");
            
            return script.Trim();
        }

        /// <summary>
        /// 分析脚本
        /// </summary>
        public ScriptAnalysis AnalyzeScript(string script)
        {
            var analysis = new ScriptAnalysis();

            if (string.IsNullOrEmpty(script))
            {
                return analysis;
            }

            // 统计操作数
            analysis.ActionCount += Regex.Matches(script, @"\.act\(").Count;
            analysis.ActionCount += Regex.Matches(script, @"\.extract\(").Count;
            analysis.ActionCount += Regex.Matches(script, @"\.observe\(").Count;

            // 估算时间（每个操作约 3-5 秒）
            analysis.EstimatedSeconds = analysis.ActionCount * 4;

            // 复杂度评估
            if (analysis.ActionCount <= 3)
                analysis.Complexity = "简单 ⭐";
            else if (analysis.ActionCount <= 8)
                analysis.Complexity = "中等 ⭐⭐";
            else
                analysis.Complexity = "复杂 ⭐⭐⭐";

            return analysis;
        }

        /// <summary>
        /// 生成脚本
        /// </summary>
        public async Task<GenerateScriptResponse> GenerateScriptAsync(
            GenerateScriptRequest request,
            Func<string, string, int, Task<string>> aiGenerateFunc)
        {
            try
            {
                _logService.LogInfo("StagehandTask", "Starting script generation");

                var systemPrompt = BuildSystemPrompt();

                // 调用 AI 生成脚本（分别传递系统提示词和用户消息）
                var script = await aiGenerateFunc(systemPrompt, request.UserMessage, request.ProviderId);

                if (string.IsNullOrEmpty(script))
                {
                    return new GenerateScriptResponse
                    {
                        Success = false,
                        Message = "AI 未返回脚本内容"
                    };
                }

                // 清理脚本内容，去掉 markdown 代码块标记
                script = CleanScript(script);

                // 验证和修复脚本
                script = ValidateAndFixScript(script);

                // 分析脚本
                var analysis = AnalyzeScript(script);

                _logService.LogInfo("StagehandTask", $"Script generated successfully. Actions: {analysis.ActionCount}");

                return new GenerateScriptResponse
                {
                    Success = true,
                    Script = script,
                    Message = "脚本生成成功",
                    ActionCount = analysis.ActionCount,
                    EstimatedSeconds = analysis.EstimatedSeconds,
                    Complexity = analysis.Complexity
                };
            }
            catch (Exception ex)
            {
                _logService.LogError("StagehandTask", $"Script generation failed: {ex.Message}", ex.StackTrace);
                return new GenerateScriptResponse
                {
                    Success = false,
                    Message = $"生成失败：{ex.Message}"
                };
            }
        }
    }

    /// <summary>
    /// 脚本分析结果
    /// </summary>
    public class ScriptAnalysis
    {
        public int ActionCount { get; set; }
        public int EstimatedSeconds { get; set; }
        public string Complexity { get; set; } = "";
    }
}
