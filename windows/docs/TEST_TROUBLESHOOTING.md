# 🔧 测试脚本问题排查指南

## 问题描述

运行 `dotnet test WebScraperApp.Tests` 时出现以下错误：

```
System.ArgumentException: Can not instantiate proxy of class: WebScraperApp.Services.LogService.
Could not find a parameterless constructor.
```

## 根本原因分析

### 原因 1: 目标框架不匹配 ✅ 已修复

**问题**: 
- 主项目 (WebScraperApp): `net9.0-windows`
- 测试项目 (WebScraperApp.Tests): `net9.0`

**解决方案**:
修改 `WebScraperApp.Tests.csproj`:
```xml
<TargetFramework>net9.0-windows</TargetFramework>
```

### 原因 2: Moq 无法创建代理 ❌ 未解决

**问题**:
Moq 使用 Castle DynamicProxy 来创建 Mock 对象。当类没有无参构造函数时，Castle 无法创建代理。

**受影响的类**:
- `DatabaseService` - 需要 `WebScraperDbContext` 参数
- `LogService` - 需要 `ILogger` 参数
- `FingerprintPresetService` - 需要 `DatabaseService` 参数

**错误堆栈**:
```
at Castle.DynamicProxy.ProxyGenerator.CreateClassProxyInstance(...)
at Moq.CastleProxyFactory.CreateProxy(...)
at Moq.Mock`1.InitializeInstance()
```

## 解决方案

### 方案 1: 创建接口 (推荐) ⭐

为这些服务类创建接口，然后在测试中 Mock 接口而不是具体类。

**步骤**:

1. 创建接口:
```csharp
// Services/IDatabaseService.cs
public interface IDatabaseService
{
    List<FingerprintProfile> GetAllFingerprintProfiles();
    // ... 其他方法
}

// Services/ILogService.cs
public interface ILogService
{
    void LogInfo(string source, string message);
    // ... 其他方法
}
```

2. 让类实现接口:
```csharp
public class DatabaseService : IDatabaseService
{
    // 实现接口
}

public class LogService : ILogService
{
    // 实现接口
}
```

3. 更新测试:
```csharp
var mockDatabaseService = new Mock<IDatabaseService>();
var mockLogService = new Mock<ILogService>();
```

**优点**:
- ✅ 完全解决 Mock 问题
- ✅ 遵循依赖倒置原则
- ✅ 改进代码设计

**缺点**:
- ❌ 需要修改现有代码
- ❌ 工作量较大

### 方案 2: 使用 NSubstitute (替代方案)

使用 NSubstitute 而不是 Moq，它对 Mock 创建的限制较少。

**步骤**:

1. 安装 NSubstitute:
```bash
dotnet add package NSubstitute
```

2. 更新测试:
```csharp
using NSubstitute;

var mockDatabaseService = Substitute.For<DatabaseService>();
var mockLogService = Substitute.For<LogService>();
```

**优点**:
- ✅ 无需修改现有代码
- ✅ 快速解决问题

**缺点**:
- ❌ 需要学习新的 Mock 库
- ❌ 项目中混用两个 Mock 库

### 方案 3: 暂时禁用测试 (临时方案)

注释掉有问题的测试，专注于其他功能的开发。

**步骤**:

```csharp
// [Fact]
// public void Test_Name()
// {
//     // 测试代码
// }
```

**优点**:
- ✅ 快速解决问题
- ✅ 无需修改代码

**缺点**:
- ❌ 测试无法运行
- ❌ 不是长期解决方案

## 推荐方案

**立即行动**: 方案 3 (暂时禁用)
- 快速解决问题
- 允许继续开发其他功能

**长期方案**: 方案 1 (创建接口)
- 改进代码设计
- 完全解决 Mock 问题
- 遵循 SOLID 原则

## 实施步骤

### 短期 (今天)
1. ✅ 修改测试项目目标框架 (已完成)
2. ⏳ 禁用有问题的测试
3. ⏳ 运行其他测试验证

### 中期 (本周)
1. ⏳ 为服务类创建接口
2. ⏳ 更新依赖注入配置
3. ⏳ 更新测试使用接口

### 长期 (本月)
1. ⏳ 完成所有接口创建
2. ⏳ 完成所有测试更新
3. ⏳ 运行完整的测试套件

## 当前状态

| 项目 | 状态 | 备注 |
|------|------|------|
| 目标框架修复 | ✅ 完成 | net9.0-windows |
| TestMockFactory | ✅ 创建 | 但 MockBehavior.Loose 无效 |
| 测试运行 | ❌ 失败 | 36 个测试都失败 |
| 接口创建 | ⏳ 待做 | 需要重构 |

## 相关文件

- `WebScraperApp.Tests/WebScraperApp.Tests.csproj` - 测试项目配置
- `WebScraperApp.Tests/TestFixtures/MockFactory.cs` - Mock 工厂
- `WebScraperApp.Tests/Presentation/ViewModels/FingerprintConfigViewModelTests.cs` - ViewModel 测试
- `WebScraperApp.Tests/Application/Services/FingerprintApplicationServiceTests.cs` - 应用服务测试
- `WebScraperApp.Tests/Domain/Services/FingerprintDomainServiceTests.cs` - 领域服务测试

## 参考资源

- [Moq 文档](https://github.com/moq/moq4)
- [Castle DynamicProxy](https://github.com/castleproject/Core)
- [NSubstitute 文档](https://nsubstitute.github.io/)
- [SOLID 原则](https://en.wikipedia.org/wiki/SOLID)

## 下一步行动

1. 选择解决方案 (推荐方案 1)
2. 为服务类创建接口
3. 更新依赖注入配置
4. 更新测试代码
5. 运行测试验证

---

**版本**: 1.0  
**最后更新**: 2025-10-28  
**状态**: 问题已识别，等待解决方案实施
