# npm 镜像配置指南 - 加速 Stagehand 安装

## 🚀 快速配置（推荐）

### 方法1：使用配置脚本（最简单）

#### Windows (PowerShell)
```powershell
cd d:\1Dev\webbrowser
.\configure-npm-mirror.ps1
```

#### Windows (CMD)
```cmd
cd d:\1Dev\webbrowser
configure-npm-mirror.bat
```

### 方法2：手动配置

```bash
# 配置淘宝镜像（推荐）
npm config set registry https://registry.npmmirror.com

# 验证配置
npm config get registry
```

### 方法3：Web 界面配置

1. 访问：系统设置 → Stagehand AI 框架
2. 点击「配置加速镜像」按钮
3. 选择镜像源
4. 复制命令并执行

## 📊 镜像源对比

| 镜像源 | URL | 速度 | 推荐度 |
|--------|-----|------|--------|
| **淘宝镜像** | https://registry.npmmirror.com | ⭐⭐⭐⭐⭐ | ✅ 强烈推荐 |
| 腾讯云镜像 | https://mirrors.cloud.tencent.com/npm/ | ⭐⭐⭐⭐ | ✅ 推荐 |
| 华为云镜像 | https://repo.huaweicloud.com/repository/npm/ | ⭐⭐⭐⭐ | ✅ 推荐 |
| 官方镜像 | https://registry.npmjs.org | ⭐⭐ | 国外用户 |

## ⚡ 速度对比

### 未配置镜像（官方源）
```
下载 @browserbasehq/stagehand
预计时间：5-10 分钟
可能超时：❌ 经常超时
```

### 配置淘宝镜像后
```
下载 @browserbasehq/stagehand
预计时间：30-60 秒
可能超时：✅ 很少超时
```

**速度提升：10-20 倍！**

## 🔧 详细配置步骤

### 1. 查看当前配置
```bash
npm config get registry
```

**输出示例**：
```
https://registry.npmjs.org/
```

### 2. 设置淘宝镜像
```bash
npm config set registry https://registry.npmmirror.com
```

### 3. 验证配置
```bash
npm config get registry
```

**输出应该是**：
```
https://registry.npmmirror.com/
```

### 4. 测试连接速度
```bash
npm info @browserbasehq/stagehand
```

**如果配置成功**：
- 响应时间 < 2 秒
- 显示包信息

### 5. 安装 Stagehand
现在可以重新安装，速度会快很多！

## 🔄 恢复默认镜像

如果需要恢复官方镜像：

```bash
npm config set registry https://registry.npmjs.org
```

## 📝 配置文件位置

npm 配置保存在：
- **Windows**: `C:\Users\你的用户名\.npmrc`
- **Linux/macOS**: `~/.npmrc`

可以直接编辑此文件：
```ini
registry=https://registry.npmmirror.com/
```

## 🎯 常见问题

### Q1: 配置后还是很慢？
**A**: 
1. 检查配置是否生效：`npm config get registry`
2. 清除 npm 缓存：`npm cache clean --force`
3. 重试安装

### Q2: 如何查看所有 npm 配置？
**A**: 
```bash
npm config list
```

### Q3: 镜像源会影响包的安全性吗？
**A**: 
- 淘宝镜像是官方包的同步镜像
- 只是加速下载，不修改包内容
- 完全安全可靠

### Q4: 可以为单个项目配置镜像吗？
**A**: 
可以，在项目目录创建 `.npmrc` 文件：
```ini
registry=https://registry.npmmirror.com/
```

### Q5: 如何临时使用镜像？
**A**: 
```bash
npm install @browserbasehq/stagehand --registry=https://registry.npmmirror.com
```

## 🌟 其他加速技巧

### 1. 使用 cnpm（淘宝 npm 客户端）
```bash
# 安装 cnpm
npm install -g cnpm --registry=https://registry.npmmirror.com

# 使用 cnpm 安装
cnpm install -g @browserbasehq/stagehand
```

### 2. 配置代理（如果有）
```bash
npm config set proxy http://proxy.company.com:8080
npm config set https-proxy http://proxy.company.com:8080
```

### 3. 增加超时时间
```bash
npm config set timeout 600000  # 10 分钟
```

## 📊 性能测试

### 测试脚本
```bash
# 测试官方源
time npm info @browserbasehq/stagehand --registry=https://registry.npmjs.org

# 测试淘宝镜像
time npm info @browserbasehq/stagehand --registry=https://registry.npmmirror.com
```

### 预期结果
- **官方源**: 5-10 秒（国内）
- **淘宝镜像**: 0.5-2 秒（国内）

## 🎉 配置完成后

1. ✅ 验证配置：`npm config get registry`
2. ✅ 测试速度：`npm info @browserbasehq/stagehand`
3. ✅ 重新安装 Stagehand
4. ✅ 享受飞速下载！

## 🔗 相关链接

- **淘宝镜像官网**: https://npmmirror.com/
- **npm 官方文档**: https://docs.npmjs.com/
- **Stagehand GitHub**: https://github.com/browserbase/stagehand

---

**配置建议**：
- 🏠 **国内用户**: 强烈推荐淘宝镜像
- 🌍 **国外用户**: 使用官方镜像
- 🏢 **企业用户**: 可搭建私有镜像

**效果对比**：
```
未配置镜像: 😫 5-10 分钟，经常超时
配置镜像后: 😊 30-60 秒，稳定快速
```

立即配置，享受 10-20 倍速度提升！🚀
