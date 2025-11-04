# 🧪 WebScraper 测试命令参考

## 快速开始

### 使用 BAT 脚本运行测试

#### 1. 快速测试 (推荐)
```bash
quick-test.bat
```
- 快速运行所有测试
- 显示测试结果
- 简洁的输出

#### 2. 完整测试
```bash
run-tests.bat
```
- 清理之前的构建
- 恢复 NuGet 依赖
- 构建测试项目
- 运行所有测试
- 生成覆盖率报告

## 命令行命令

### 基础命令

#### 运行所有测试
```bash
dotnet test WebScraperApp.Tests
```

#### 运行特定测试类
```bash
dotnet test WebScraperApp.Tests --filter "ClassName=FingerprintConfigViewModelTests"
```

#### 运行特定测试方法
```bash
dotnet test WebScraperApp.Tests --filter "FullyQualifiedName~FingerprintConfigViewModelTests.ViewModel_ShouldInitializeWithEmptyFingerprints"
```

### 详细输出

#### 显示详细信息
```bash
dotnet test WebScraperApp.Tests --verbosity detailed
```

#### 显示诊断信息
```bash
dotnet test WebScraperApp.Tests --verbosity diagnostic
```

### 代码覆盖率

#### 生成覆盖率报告
```bash
dotnet test WebScraperApp.Tests /p:CollectCoverage=true /p:CoverageFormat=opencover
```

#### 生成 HTML 覆盖率报告
```bash
dotnet test WebScraperApp.Tests /p:CollectCoverage=true /p:CoverageFormat=cobertura
```

### 构建和测试

#### 清理构建
```bash
dotnet clean WebScraperApp.Tests
```

#### 恢复依赖
```bash
dotnet restore WebScraperApp.Tests
```

#### 构建测试项目
```bash
dotnet build WebScraperApp.Tests
```

#### 构建并运行测试
```bash
dotnet build WebScraperApp.Tests && dotnet test WebScraperApp.Tests
```

## Visual Studio 中运行测试

### 方法 1: 测试浏览器
1. 打开 Visual Studio
2. 菜单: `Test` → `Test Explorer`
3. 选择要运行的测试
4. 点击 `Run` 按钮

### 方法 2: 快捷键
- **运行所有测试**: `Ctrl + R, Ctrl + A`
- **运行当前测试**: `Ctrl + R, Ctrl + T`
- **调试当前测试**: `Ctrl + R, Ctrl + D`

### 方法 3: 右键菜单
1. 在测试文件中右键
2. 选择 `Run Tests` 或 `Debug Tests`

## 测试项目结构

```
WebScraperApp.Tests/
├── Presentation/
│   └── ViewModels/
│       └── FingerprintConfigViewModelTests.cs (12 个测试)
├── Application/
│   └── Services/
│       └── FingerprintApplicationServiceTests.cs (11 个测试)
├── Domain/
│   └── Services/
│       └── FingerprintDomainServiceTests.cs (13 个测试)
└── WebScraperApp.Tests.csproj
```

## 测试统计

| 类 | 测试数 | 覆盖范围 |
|-----|--------|----------|
| FingerprintConfigViewModelTests | 12 | ViewModel, Commands, Properties |
| FingerprintApplicationServiceTests | 11 | Services, Validation, DTOs |
| FingerprintDomainServiceTests | 13 | Services, Business Rules |
| **总计** | **36** | **核心业务逻辑** |

## 常见问题

### Q: 测试失败怎么办？
A: 
1. 查看错误信息
2. 检查依赖是否正确安装: `dotnet restore`
3. 清理构建: `dotnet clean`
4. 重新构建: `dotnet build`

### Q: 如何调试测试？
A:
1. 在测试方法中设置断点
2. 右键点击测试 → `Debug Test`
3. 或使用快捷键: `Ctrl + R, Ctrl + D`

### Q: 如何查看覆盖率？
A:
1. 运行: `dotnet test WebScraperApp.Tests /p:CollectCoverage=true`
2. 查看生成的 `coverage.xml` 文件
3. 使用工具如 ReportGenerator 生成 HTML 报告

### Q: 如何只运行某个测试类？
A:
```bash
dotnet test WebScraperApp.Tests --filter "ClassName=FingerprintConfigViewModelTests"
```

### Q: 如何并行运行测试？
A:
```bash
dotnet test WebScraperApp.Tests --parallel
```

## 最佳实践

### 1. 定期运行测试
- 每次提交代码前运行测试
- 使用 CI/CD 流水线自动运行测试

### 2. 保持测试简洁
- 每个测试只测试一个功能
- 使用清晰的测试名称

### 3. 使用 AAA 模式
```csharp
[Fact]
public void Test()
{
    // Arrange - 准备
    var input = new FingerprintDTO { Name = "Test" };
    
    // Act - 执行
    var result = _service.CreateFingerprint(input);
    
    // Assert - 验证
    Assert.NotNull(result);
}
```

### 4. 模拟外部依赖
```csharp
var mock = new Mock<IRepository>();
mock.Setup(x => x.GetByIdAsync(1))
    .ReturnsAsync(new FingerprintProfile { Id = 1 });
```

## 脚本文件说明

### run-tests.bat
- **功能**: 完整的测试流程
- **包含**:
  - 环境检查
  - 清理构建
  - 恢复依赖
  - 构建项目
  - 运行测试
  - 生成覆盖率报告
- **用途**: 完整的测试和报告生成

### quick-test.bat
- **功能**: 快速运行测试
- **包含**:
  - 直接运行测试
  - 显示结果
- **用途**: 快速验证代码

## 下一步

- [ ] 运行 `quick-test.bat` 验证所有测试通过
- [ ] 运行 `run-tests.bat` 生成覆盖率报告
- [ ] 检查测试覆盖率
- [ ] 添加更多测试
- [ ] 设置 CI/CD 流水线

---

**版本**: 1.0  
**最后更新**: 2025-10-28  
**总测试数**: 36 个
