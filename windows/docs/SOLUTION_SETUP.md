# 📋 WebScraperApp 解决方案配置

## 解决方案文件

**位置**: `D:\1Dev\webscraper\windows\WebScraperApp.sln`

## 包含的项目

### 1. WebScraperApp (主项目)
- **类型**: WPF 应用程序
- **框架**: .NET 9.0
- **GUID**: `{25987395-DD0A-E514-6872-B9E68F4B7648}`
- **路径**: `WebScraperApp\WebScraperApp.csproj`
- **功能**:
  - 指纹浏览器网页爬虫系统
  - 5 层分层架构 (DDD + Clean Architecture)
  - MVVM 模式
  - 完整的依赖注入

### 2. WebScraperApp.Tests (测试项目)
- **类型**: xUnit 单元测试项目
- **框架**: .NET 9.0
- **GUID**: `{A5B8C9D0-1E2F-4A5B-8C9D-0E1F2A3B4C5D}`
- **路径**: `WebScraperApp.Tests\WebScraperApp.Tests.csproj`
- **功能**:
  - 36 个单元测试
  - Presentation 层测试 (12 个)
  - Application 层测试 (11 个)
  - Domain 层测试 (13 个)
  - Moq 模拟框架

## 在 Visual Studio 中打开

### 方法 1: 直接打开 sln 文件
```bash
# 使用 Visual Studio 打开
start WebScraperApp.sln

# 或使用 devenv
devenv WebScraperApp.sln
```

### 方法 2: 使用 Visual Studio IDE
1. 打开 Visual Studio
2. `File` → `Open` → `Project/Solution`
3. 选择 `WebScraperApp.sln`
4. 点击 `Open`

### 方法 3: 使用命令行
```bash
# 在项目目录中
cd d:\1Dev\webscraper\windows

# 使用 dotnet 打开
dotnet sln list

# 或使用 Visual Studio
"C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\devenv.exe" WebScraperApp.sln
```

## 解决方案结构

```
WebScraperApp.sln
├── WebScraperApp (主项目)
│   ├── Presentation/        # 表现层
│   ├── Application/         # 应用层
│   ├── Domain/              # 领域层
│   ├── Infrastructure/      # 基础设施层
│   ├── Views/               # 旧的 View 代码
│   ├── Services/            # 旧的 Service 代码
│   ├── Models/              # 数据模型
│   ├── Engine/              # 业务引擎
│   └── Data/                # 数据访问
│
└── WebScraperApp.Tests (测试项目)
    ├── Presentation/
    │   └── ViewModels/
    │       └── FingerprintConfigViewModelTests.cs
    ├── Application/
    │   └── Services/
    │       └── FingerprintApplicationServiceTests.cs
    └── Domain/
        └── Services/
            └── FingerprintDomainServiceTests.cs
```

## 构建和运行

### 构建解决方案
```bash
# 构建所有项目
dotnet build WebScraperApp.sln

# 构建特定配置
dotnet build WebScraperApp.sln --configuration Release
```

### 运行主应用
```bash
# 运行 WebScraperApp
dotnet run --project WebScraperApp\WebScraperApp.csproj
```

### 运行测试
```bash
# 运行所有测试
dotnet test WebScraperApp.sln

# 运行特定项目的测试
dotnet test WebScraperApp.Tests\WebScraperApp.Tests.csproj

# 使用脚本运行测试
quick-test.bat
run-tests.bat
.\run-tests.ps1
```

## 项目依赖关系

```
WebScraperApp.Tests
    ↓ (引用)
WebScraperApp
    ↓ (包含)
├── Presentation Layer
├── Application Layer
├── Domain Layer
└── Infrastructure Layer
```

## 配置说明

### 调试配置 (Debug)
- 优化: 关闭
- 符号: 完整
- 用途: 开发和调试

### 发布配置 (Release)
- 优化: 启用
- 符号: 仅限 pdb
- 用途: 生产环境

## 常见操作

### 1. 清理解决方案
```bash
dotnet clean WebScraperApp.sln
```

### 2. 恢复依赖
```bash
dotnet restore WebScraperApp.sln
```

### 3. 构建并测试
```bash
dotnet build WebScraperApp.sln && dotnet test WebScraperApp.sln
```

### 4. 发布应用
```bash
dotnet publish WebScraperApp\WebScraperApp.csproj --configuration Release
```

## 在 Visual Studio 中的操作

### 构建
- **快捷键**: `Ctrl + Shift + B`
- **菜单**: `Build` → `Build Solution`

### 运行
- **快捷键**: `F5` (调试) 或 `Ctrl + F5` (不调试)
- **菜单**: `Debug` → `Start Debugging`

### 测试
- **菜单**: `Test` → `Run All Tests`
- **快捷键**: `Ctrl + R, A`

### 清理
- **菜单**: `Build` → `Clean Solution`

## 项目属性

### WebScraperApp
- **输出类型**: Windows Application (WPF)
- **目标框架**: .NET 9.0
- **语言版本**: Latest
- **可空引用**: 启用

### WebScraperApp.Tests
- **输出类型**: Class Library
- **目标框架**: .NET 9.0
- **语言版本**: Latest
- **可空引用**: 启用
- **测试框架**: xUnit

## 故障排除

### 问题 1: "项目加载失败"
**原因**: 项目文件路径不正确或文件不存在

**解决**:
1. 检查项目文件是否存在
2. 检查路径是否正确
3. 重新加载解决方案: `File` → `Reload Solution`

### 问题 2: "找不到引用"
**原因**: NuGet 包未正确恢复

**解决**:
```bash
dotnet restore WebScraperApp.sln
```

### 问题 3: "构建失败"
**原因**: 编译错误或依赖问题

**解决**:
1. 清理解决方案: `dotnet clean`
2. 恢复依赖: `dotnet restore`
3. 重新构建: `dotnet build`

## 下一步

1. ✅ 在 Visual Studio 中打开解决方案
2. ✅ 构建解决方案
3. ✅ 运行测试
4. ✅ 运行主应用
5. ⏳ 开始开发

## 相关文档

- `PROJECT_ARCHITECTURE.md` - 项目架构
- `ARCHITECTURE_COMPLETE_SUMMARY.md` - 架构总结
- `RUN_TESTS_README.md` - 测试脚本指南
- `TEST_COMMANDS.md` - 测试命令参考

---

**版本**: 1.0  
**最后更新**: 2025-10-28  
**项目数**: 2 个 (主项目 + 测试项目)  
**总测试数**: 36 个
