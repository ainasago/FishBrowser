# 🎭 Stagehand AI 任务完整指南

## ✅ 已完成的功能

### WPF 版本
- ✅ 完整的桌面应用界面
- ✅ AI 脚本生成
- ✅ 脚本执行
- ✅ 快捷示例
- ✅ 主菜单集成

### Web 版本
- ✅ 响应式 Web 界面
- ✅ API 端点
- ✅ AI 脚本生成
- ✅ 脚本执行
- ✅ 快捷示例
- ✅ 主菜单集成

## 🔧 已修复的问题

### 1. Markdown 代码块清理 ✅
**问题**：AI 返回的脚本包含 \`\`\`javascript 标记

**解决**：添加 `CleanScript` 方法自动清理

### 2. DbContext 并发错误 ✅
**问题**：多线程访问 DbContext 导致错误

**解决**：事件处理器中使用 `Console.WriteLine`

### 3. 模块找不到错误 ✅
**问题**：`Cannot find module '@browserbasehq/stagehand'`

**解决**：设置 `NODE_PATH` 环境变量

### 4. Page 对象未定义 ✅
**问题**：`Cannot read properties of undefined (reading 'goto')`

**解决**：强化系统提示词，强调必须先 `await stagehand.init()`

## 📋 核心代码

### 1. 脚本清理
```csharp
private string CleanScript(string script)
{
    // 去掉 markdown 代码块标记
    script = Regex.Replace(script, @"^```(javascript|js)\s*\n", "", RegexOptions.Multiline);
    script = Regex.Replace(script, @"\n```\s*$", "", RegexOptions.Multiline);
    script = script.Replace("```javascript", "").Replace("```js", "").Replace("```", "");
    return script.Trim();
}
```

### 2. NODE_PATH 设置
```csharp
// 获取全局 node_modules 路径
var globalNodeModules = GetGlobalNodeModulesPath();

// 设置环境变量
if (!string.IsNullOrEmpty(globalNodeModules))
{
    startInfo.EnvironmentVariables["NODE_PATH"] = globalNodeModules;
}
```

### 3. 系统提示词（关键部分）
```
## 生成规则

1. **必须使用完整的脚本模板**，包含 IIFE 和 async/await
2. **必须先调用 await stagehand.init()** 才能使用 stagehand.page
3. 包含完整的错误处理（try-catch-finally）
4. **必须在 finally 中调用 await stagehand.close()**

⚠️ **常见错误**：
- ❌ 忘记 await stagehand.init()
- ❌ 在 init() 之前使用 stagehand.page
- ❌ 忘记使用 async/await
- ❌ 忘记在 finally 中关闭 stagehand

✅ **正确示例**：
const { Stagehand } = require('@browserbasehq/stagehand');

(async () => {
    const stagehand = new Stagehand({ env: 'LOCAL', verbose: 1 });
    
    try {
        await stagehand.init();  // ⚠️ 必须先初始化
        await stagehand.page.goto('https://example.com');
        await stagehand.act('点击按钮');
        console.log('完成！');
    } catch (error) {
        console.error('失败:', error);
    } finally {
        await stagehand.close();  // ⚠️ 必须关闭
    }
})();
```

## 🎯 正确的脚本模板

### 基础模板
```javascript
const { Stagehand } = require('@browserbasehq/stagehand');

(async () => {
    const stagehand = new Stagehand({
        env: 'LOCAL',
        verbose: 1,
        debugDom: true
    });
    
    try {
        // 1. 必须先初始化
        await stagehand.init();
        
        // 2. 导航到目标网站
        await stagehand.page.goto('https://example.com');
        
        // 3. 执行操作
        await stagehand.act('你的操作指令');
        
        // 4. 提取数据（可选）
        const data = await stagehand.extract('提取指令', {
            field1: 'string',
            field2: 'number'
        });
        
        console.log('任务完成！', data);
        
    } catch (error) {
        console.error('任务失败:', error);
    } finally {
        // 5. 必须关闭
        await stagehand.close();
    }
})();
```

### 登录示例
```javascript
const { Stagehand } = require('@browserbasehq/stagehand');

(async () => {
    const stagehand = new Stagehand({
        env: 'LOCAL',
        verbose: 1,
        debugDom: true
    });
    
    try {
        await stagehand.init();
        
        // 导航到 GitHub
        await stagehand.page.goto('https://github.com/login');
        
        // 填写用户名
        await stagehand.act('在用户名框输入 myusername');
        
        // 填写密码
        await stagehand.act('在密码框输入 mypassword');
        
        // 点击登录按钮
        await stagehand.act('点击登录按钮');
        
        // 等待登录完成
        await stagehand.page.waitForTimeout(3000);
        
        console.log('登录成功！');
        
    } catch (error) {
        console.error('登录失败:', error);
    } finally {
        await stagehand.close();
    }
})();
```

### 数据提取示例
```javascript
const { Stagehand } = require('@browserbasehq/stagehand');

(async () => {
    const stagehand = new Stagehand({
        env: 'LOCAL',
        verbose: 1,
        debugDom: true
    });
    
    try {
        await stagehand.init();
        
        // 导航到 Hacker News
        await stagehand.page.goto('https://news.ycombinator.com');
        
        // 提取新闻列表
        const news = await stagehand.extract('提取前 10 条新闻', {
            title: 'string',
            score: 'number',
            url: 'string'
        });
        
        console.log('提取到的新闻：', news);
        
    } catch (error) {
        console.error('提取失败:', error);
    } finally {
        await stagehand.close();
    }
})();
```

## 🚀 使用流程

### Web 版本
```
1. 访问：http://localhost:5001/StagehandTask/Index
2. 选择 AI 提供商
3. 输入任务描述或点击快捷示例
4. 点击"生成脚本 ✨"
5. 查看生成的脚本
6. 点击"▶️ 运行脚本"
7. 查看执行结果
```

### WPF 版本
```
1. 启动应用
2. 点击"🎭 Stagehand AI"
3. 选择 AI 提供商
4. 输入任务描述或点击快捷示例
5. 点击"生成脚本 ✨"
6. 查看生成的脚本
7. 点击"▶️ 运行脚本"
8. 查看执行结果
```

## 📊 快捷示例

### 1. 🔐 智能登录
```
创建一个登录 GitHub 的脚本：
打开 github.com，点击 Sign in，
填写用户名和密码，点击登录按钮
```

### 2. 🔍 搜索提取
```
创建一个搜索脚本：
打开 Google，搜索 'Stagehand AI'，
提取前 5 个搜索结果的标题和链接
```

### 3. 🧭 智能导航
```
创建一个导航脚本：
打开 Amazon，依次点击 Books 分类，
然后点击 Best Sellers
```

### 4. 📊 数据提取
```
创建一个数据提取脚本：
打开 Hacker News 首页，
提取前 10 条新闻的标题、分数和评论数
```

### 5. 📝 表单填写
```
创建一个表单填写脚本：
打开一个联系表单，
填写姓名、邮箱和消息内容，然后提交
```

### 6. 🛒 购物流程
```
创建一个购物脚本：
在 Amazon 搜索 'laptop'，
点击第一个商品，提取商品名称和价格，
然后加入购物车
```

## 🔍 调试技巧

### 1. 启用详细日志
```javascript
const stagehand = new Stagehand({
    env: 'LOCAL',
    verbose: 2,  // 增加日志级别
    debugDom: true
});
```

### 2. 添加等待时间
```javascript
// 等待页面加载
await stagehand.page.waitForTimeout(2000);

// 等待特定元素
await stagehand.page.waitForSelector('.my-element');
```

### 3. 截图调试
```javascript
// 保存截图
await stagehand.page.screenshot({ path: 'debug.png' });
```

### 4. 控制台输出
```javascript
// 输出当前 URL
console.log('Current URL:', stagehand.page.url());

// 输出页面标题
console.log('Page title:', await stagehand.page.title());
```

## ⚠️ 常见错误和解决方案

### 错误 1: Cannot find module '@browserbasehq/stagehand'
**原因**：Stagehand 未安装或 NODE_PATH 未设置

**解决**：
```bash
# 安装 Stagehand
npm install -g @browserbasehq/stagehand

# 验证安装
npm list -g @browserbasehq/stagehand
```

### 错误 2: Cannot read properties of undefined (reading 'goto')
**原因**：未调用 `await stagehand.init()` 或未等待初始化完成

**解决**：
```javascript
// ❌ 错误
const stagehand = new Stagehand({ env: 'LOCAL' });
await stagehand.page.goto('https://example.com');  // page 是 undefined

// ✅ 正确
const stagehand = new Stagehand({ env: 'LOCAL' });
await stagehand.init();  // 必须先初始化
await stagehand.page.goto('https://example.com');
```

### 错误 3: Timeout waiting for element
**原因**：页面加载慢或元素不存在

**解决**：
```javascript
// 增加等待时间
await stagehand.page.waitForTimeout(3000);

// 或使用更具体的指令
await stagehand.act('等待页面加载完成后，点击登录按钮');
```

### 错误 4: DbContext 并发错误
**原因**：多线程访问 DbContext

**解决**：已在 `NodeExecutionService` 中修复，使用 `Console.WriteLine`

## 📚 相关文档

- `STAGEHAND_IMPLEMENTATION.md` - 实现文档
- `STAGEHAND_TASK_UI.md` - UI 设计文档
- `STAGEHAND_WEB_INTEGRATION.md` - Web 集成文档
- `STAGEHAND_FIXES_WEB.md` - 问题修复文档
- `STAGEHAND_COMPLETE_GUIDE.md` - 完整指南（本文档）

## ✅ 检查清单

### 部署前检查
- ✅ Node.js 已安装（v18+）
- ✅ npm 已安装（v8+）
- ✅ Stagehand 已全局安装
- ✅ Playwright 浏览器已安装
- ✅ AI 提供商已配置
- ✅ 服务已注册到 DI 容器

### 功能测试
- ✅ 状态检查正常
- ✅ AI 提供商可选择
- ✅ 快捷示例可点击
- ✅ 脚本生成成功
- ✅ 脚本无 markdown 标记
- ✅ 脚本可编辑
- ✅ 脚本可执行
- ✅ 执行结果正确显示
- ✅ 脚本可复制
- ✅ 脚本可导出

## 🎉 完成状态

### 已完成 ✅
- ✅ WPF 版本完整实现
- ✅ Web 版本完整实现
- ✅ 所有编译错误已修复
- ✅ Markdown 清理已实现
- ✅ DbContext 并发已修复
- ✅ 模块路径已修复
- ✅ 系统提示词已优化
- ✅ 文档已完善

### 待优化 ⏳
- ⏳ 任务存储（数据库）
- ⏳ 历史记录
- ⏳ 任务分享
- ⏳ 定时执行
- ⏳ 批量执行
- ⏳ 调试模式增强

---

## 🚀 现在可以完全使用了！

**Stagehand AI 任务功能已完全实现并可以正常使用！**

### 快速开始

#### Web 版本
```bash
# 启动服务
dotnet run --project web/FishBrowser.Api
dotnet run --project web/FishBrowser.Web

# 访问
http://localhost:5001/StagehandTask/Index
```

#### WPF 版本
```bash
# 编译运行
dotnet run --project windows/WebScraperApp
```

**享受 AI 驱动的浏览器自动化！** 🎭✨
