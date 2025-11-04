# Stagehand 故障排除指南

## 🔍 问题诊断

### 问题1：状态一直在检测/加载

#### 症状
- 页面显示"正在加载状态..."
- 一直转圈，不显示结果

#### 原因
1. API 请求超时或失败
2. npm 命令执行慢
3. 网络连接问题

#### 解决方案

**步骤1：查看浏览器控制台**
```
按 F12 → Console 标签
查看是否有错误信息
```

**步骤2：检查 API 服务**
```bash
# 确认 API 服务正在运行
# 访问: http://localhost:5062/api/system/stagehand/status
```

**步骤3：查看系统日志**
```
访问：系统设置 → 系统日志
筛选来源：StagehandMaintenance
查看详细错误
```

**步骤4：重启服务**
```bash
# 停止 Web 和 API 服务
# 重新启动
```

#### 已优化
✅ 最新版本检查改为异步后台任务
✅ 不会阻塞状态加载
✅ 添加详细调试日志

---

### 问题2：npm 命令找不到

#### 症状
```
System.ComponentModel.Win32Exception: 系统找不到指定的文件。
Process: npm
```

#### 原因
Windows 上 npm 是批处理文件，需要通过 cmd.exe 执行

#### 解决方案
✅ 已修复：自动使用 `cmd.exe /c npm` 执行命令

**验证修复**：
```bash
npm --version
```

---

### 问题3：安装超时

#### 症状
```
Error: The request was canceled due to the configured HttpClient.Timeout of 30 seconds elapsing.
```

#### 原因
npm 安装需要 5-10 分钟，默认 30 秒超时太短

#### 解决方案
✅ 已修复：HTTP 超时增加到 10 分钟

**加速方案**：
```bash
# 配置 npm 镜像
npm config set registry https://registry.npmmirror.com
```

---

### 问题4：下载速度慢

#### 症状
- 安装进度缓慢
- 经常超时

#### 原因
使用官方 npm 源，国内访问慢

#### 解决方案

**方法1：运行配置脚本**
```powershell
cd d:\1Dev\webbrowser
.\configure-npm-mirror.ps1
```

**方法2：手动配置**
```bash
npm config set registry https://registry.npmmirror.com
```

**方法3：Web 界面**
1. 点击「配置加速镜像」按钮
2. 选择淘宝镜像
3. 复制命令并执行

**验证配置**：
```bash
npm config get registry
# 应该显示: https://registry.npmmirror.com
```

---

### 问题5：Playwright 重复安装

#### 症状
每次安装都下载 Playwright 浏览器

#### 原因
未检测已安装的 Playwright

#### 解决方案
✅ 已修复：智能检测 Playwright 状态

**检测逻辑**：
1. 检查 `npx playwright --version`
2. 检查 Playwright 安装目录
3. 已安装则跳过

---

### 问题6：没有错误提示

#### 症状
安装失败，但看不到任何错误信息

#### 原因
错误信息未传递到前端

#### 解决方案
✅ 已修复：完整的错误处理链

**错误信息流**：
```
后端异常 
  → 详细日志记录 
  → API 错误响应 
  → Web 控制器捕获 
  → 前端显示（不自动消失）
  → 控制台日志
```

**查看错误**：
1. 浏览器控制台（F12）
2. 系统日志页面
3. API 控制台输出

---

## 🛠️ 调试工具

### 1. 测试 npm 环境
```powershell
cd d:\1Dev\webbrowser
.\test-npm.ps1
```

**输出示例**：
```
✓ Node.js 版本: v20.10.0
✓ npm 版本: 10.2.3
✓ npx 版本: 10.2.3
✓ 全局 node_modules 存在
✓ Playwright 已安装
```

### 2. 配置 npm 镜像
```powershell
.\configure-npm-mirror.ps1
```

### 3. 查看浏览器控制台
```
F12 → Console
查看详细日志：
- Loading status from: ...
- Response status: ...
- Status loaded: {...}
```

### 4. 查看系统日志
```
系统设置 → 系统日志
筛选来源：StagehandMaintenance
查看所有操作日志
```

---

## 📋 常见错误代码

### HTTP 500
**原因**：服务器内部错误
**解决**：查看系统日志，检查详细错误

### HTTP 401
**原因**：未登录或 Token 过期
**解决**：重新登录

### HTTP 404
**原因**：API 端点不存在
**解决**：确认服务已启动，路由正确

### HTTP 408/504
**原因**：请求超时
**解决**：
1. 配置 npm 镜像加速
2. 检查网络连接
3. 增加超时时间（已设置 10 分钟）

---

## 🔧 手动验证

### 1. 检查 Node.js
```bash
node --version
# 应该显示: v18.x.x 或更高
```

### 2. 检查 npm
```bash
npm --version
# 应该显示: 8.x.x 或更高
```

### 3. 检查 npm 镜像
```bash
npm config get registry
# 推荐: https://registry.npmmirror.com
```

### 4. 测试 npm 速度
```bash
npm info @browserbasehq/stagehand
# 应该在 1-2 秒内返回结果
```

### 5. 检查 Stagehand
```bash
npm list -g @browserbasehq/stagehand
# 已安装会显示版本号
```

### 6. 检查 Playwright
```bash
npx playwright --version
# 或检查目录
dir %LOCALAPPDATA%\ms-playwright
```

---

## 🎯 完整诊断流程

### 步骤1：环境检查
```powershell
# 运行测试脚本
.\test-npm.ps1

# 检查输出，确认：
# ✓ Node.js 已安装
# ✓ npm 已安装
# ✓ npm 在 PATH 中
```

### 步骤2：配置加速
```powershell
# 配置镜像
.\configure-npm-mirror.ps1

# 验证
npm config get registry
```

### 步骤3：重启服务
```bash
# 停止所有服务
# 重新启动 API 和 Web
```

### 步骤4：测试安装
```
1. 访问 Stagehand 管理页面
2. 按 F12 打开控制台
3. 点击「安装 Stagehand」
4. 观察控制台日志和进度提示
```

### 步骤5：查看日志
```
如果失败：
1. 浏览器控制台 → 查看错误
2. 系统日志 → 筛选 StagehandMaintenance
3. API 控制台 → 查看服务器日志
```

---

## 📞 获取帮助

### 日志位置
1. **浏览器控制台**：F12 → Console
2. **系统日志**：系统设置 → 系统日志
3. **API 日志**：API 服务控制台输出

### 提供信息
报告问题时，请提供：
1. 错误信息（浏览器控制台）
2. 系统日志（StagehandMaintenance）
3. Node.js 版本：`node --version`
4. npm 版本：`npm --version`
5. npm 镜像：`npm config get registry`
6. 操作系统版本

### 相关文档
- `STAGEHAND_IMPLEMENTATION.md` - 实现文档
- `STAGEHAND_FIXES.md` - 修复记录
- `NPM_MIRROR_GUIDE.md` - 镜像配置指南
- `test-npm.ps1` - 环境测试脚本
- `configure-npm-mirror.ps1` - 镜像配置脚本

---

## ✅ 检查清单

安装前检查：
- [ ] Node.js 已安装（v18+）
- [ ] npm 已安装（v8+）
- [ ] npm 在 PATH 中
- [ ] npm 镜像已配置
- [ ] 网络连接正常
- [ ] 磁盘空间充足（>1GB）

安装后检查：
- [ ] Stagehand 已安装
- [ ] 版本信息正确
- [ ] Playwright 已安装
- [ ] 测试连接成功

如果问题仍未解决，请查看系统日志获取详细信息。
