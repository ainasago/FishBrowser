# Automa 扩展崩溃诊断

## 问题描述

启动浏览器时出现错误：
```
Target page, context or browser has been closed
```

浏览器进程在加载 Automa 扩展后立即崩溃。

## 日志分析

```
[2025-11-01 15:58:00] [INF] [PlaywrightController] Automa extension will be loaded from: d:\1Dev\webscraper\automa-main\build
[2025-11-01 15:58:03] [ERR] [PlaywrightController] Failed to initialize browser: Target page, context or browser has been closed
```

## 可能的原因

### 1. 扩展构建问题
- ❌ 扩展未正确构建
- ❌ 缺少必需的文件（manifest.json, background.bundle.js 等）
- ❌ 扩展版本与 Chromium 版本不兼容

### 2. 扩展权限问题
- ❌ 扩展权限声明不完整
- ❌ 扩展尝试访问不允许的 API

### 3. 指纹冲突
- ❌ Automa 注入的脚本与指纹脚本冲突
- ❌ 扩展暴露了 `chrome.runtime` API，被指纹检测识别

### 4. Chromium 兼容性
- ❌ Automa 版本与 Playwright 使用的 Chromium 版本不兼容

## 诊断步骤

### 步骤 1: 禁用 Automa 扩展测试

**当前状态**: ✅ 已禁用（`IsChecked="False"`）

1. 编译项目
2. 启动浏览器
3. 检查是否能正常启动

**预期结果**:
- ✅ 浏览器正常启动
- ✅ 页面正常加载
- ✅ 没有崩溃错误

### 步骤 2: 验证扩展文件

检查 `d:\1Dev\webscraper\automa-main\build\` 目录：

```bash
# 必需文件
- manifest.json          ✅ 必需
- background.bundle.js   ✅ 必需
- contentScript.bundle.js ✅ 必需
- popup.html            ✅ 必需
- icon-128.png          ✅ 必需
```

### 步骤 3: 检查 Chromium 版本

```csharp
// 在浏览器中运行
console.log(navigator.userAgent);
```

应该显示类似：
```
Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.6167.0 Safari/537.36
```

### 步骤 4: 手动加载扩展测试

1. 打开 Chrome 浏览器
2. 访问 `chrome://extensions/`
3. 启用"开发者模式"
4. 点击"加载已解压的扩展程序"
5. 选择 `d:\1Dev\webscraper\automa-main\build`
6. 检查是否加载成功或显示错误

## 解决方案

### 方案 A: 重新构建 Automa 扩展

```bash
cd d:\1Dev\webscraper\automa-main

# 清理旧构建
rmdir /s /q build
rmdir /s /q node_modules

# 重新构建
npm cache clean --force
npm install --legacy-peer-deps
powershell -ExecutionPolicy Bypass -File create-passkey.ps1
npm install ajv@latest --legacy-peer-deps
npm run build
```

### 方案 B: 使用预构建版本

从 Chrome Web Store 下载已验证的 Automa 版本：
1. 访问 [Automa Chrome Web Store](https://chrome.google.com/webstore/detail/automa/infppggnoaenmfagbfknfkancpbljcca)
2. 使用 [CRX Extractor](https://crxextractor.com/) 下载 .crx 文件
3. 解压到 `d:\1Dev\webscraper\automa-main\build`

### 方案 C: 暂时禁用 Automa

**当前状态**: ✅ 已禁用

继续使用禁用状态，等待问题解决。

## 临时解决方案

**已应用**:
- ✅ Automa 扩展默认禁用（`IsChecked="False"`）
- ✅ 用户可手动启用来测试
- ✅ 浏览器应该能正常启动

## 测试清单

- [ ] 禁用 Automa 后浏览器能正常启动
- [ ] 页面能正常加载
- [ ] 指纹伪装正常工作
- [ ] DPI 缩放正常应用
- [ ] 会话保存正常工作

## 下一步

1. **验证基础功能**
   - 编译项目
   - 启动浏览器（Automa 禁用）
   - 确认浏览器正常工作

2. **诊断 Automa 问题**
   - 按照"诊断步骤"逐一测试
   - 检查扩展文件完整性
   - 在 Chrome 中手动测试加载

3. **修复或替换**
   - 重新构建扩展
   - 或使用预构建版本
   - 或寻找替代方案

## 相关文档

- `docs/AUTOMA_INTEGRATION.md` - Automa 集成指南
- `automa-main/BUILD_SUCCESS.md` - 构建成功记录
- `automa-main/BUILD_TROUBLESHOOTING.md` - 构建故障排查

## 联系方式

如需帮助，请提供：
1. 完整的错误日志
2. Chromium 版本信息
3. Automa 构建日志
4. 系统信息（Windows 版本、DPI 设置等）

---

**状态**: 🔧 诊断中  
**最后更新**: 2025-11-01  
**优先级**: 高
