@echo off
REM ============================================================
REM WebScraper 项目测试脚本
REM ============================================================
REM 功能: 运行所有单元测试和生成覆盖率报告
REM 作者: WebScraper Team
REM 日期: 2025-10-28
REM ============================================================

setlocal enabledelayedexpansion

REM 设置颜色
set "GREEN=[92m"
set "RED=[91m"
set "YELLOW=[93m"
set "BLUE=[94m"
set "RESET=[0m"

REM 获取脚本所在目录
set "SCRIPT_DIR=%~dp0"
set "PROJECT_DIR=%SCRIPT_DIR%"
set "TEST_PROJECT=%PROJECT_DIR%WebScraperApp.Tests"

echo.
echo %BLUE%============================================================%RESET%
echo %BLUE%  WebScraper 项目测试脚本%RESET%
echo %BLUE%============================================================%RESET%
echo.

REM 检查是否安装了 dotnet
echo %YELLOW%[*] 检查 dotnet 环境...%RESET%
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo %RED%[X] 错误: 未找到 dotnet，请先安装 .NET SDK%RESET%
    exit /b 1
)
echo %GREEN%[✓] dotnet 环境检查完成%RESET%
echo.

REM 检查测试项目是否存在
echo %YELLOW%[*] 检查测试项目...%RESET%
if not exist "%TEST_PROJECT%" (
    echo %RED%[X] 错误: 测试项目不存在: %TEST_PROJECT%%RESET%
    exit /b 1
)
echo %GREEN%[✓] 测试项目检查完成%RESET%
echo.

REM 清理之前的构建
echo %YELLOW%[*] 清理之前的构建...%RESET%
cd /d "%PROJECT_DIR%"
dotnet clean WebScraperApp.Tests >nul 2>&1
echo %GREEN%[✓] 清理完成%RESET%
echo.

REM 恢复依赖
echo %YELLOW%[*] 恢复 NuGet 依赖...%RESET%
dotnet restore WebScraperApp.Tests >nul 2>&1
if errorlevel 1 (
    echo %RED%[X] 错误: 恢复依赖失败%RESET%
    exit /b 1
)
echo %GREEN%[✓] 依赖恢复完成%RESET%
echo.

REM 构建测试项目
echo %YELLOW%[*] 构建测试项目...%RESET%
dotnet build WebScraperApp.Tests --configuration Release
if errorlevel 1 (
    echo %RED%[X] 错误: 构建测试项目失败%RESET%
    exit /b 1
)
echo %GREEN%[✓] 构建完成%RESET%
echo.

REM 运行测试
echo %YELLOW%[*] 运行单元测试...%RESET%
echo.
dotnet test WebScraperApp.Tests --configuration Release --verbosity normal --logger "console;verbosity=normal"
set "TEST_RESULT=!errorlevel!"
echo.

REM 运行测试并生成覆盖率报告
echo %YELLOW%[*] 生成代码覆盖率报告...%RESET%
dotnet test WebScraperApp.Tests ^
    --configuration Release ^
    /p:CollectCoverage=true ^
    /p:CoverageFormat=opencover ^
    /p:CoverageFileName=coverage.xml ^
    /p:Exclude="[*]*.Tests*" >nul 2>&1

if exist "%TEST_PROJECT%\coverage.xml" (
    echo %GREEN%[✓] 覆盖率报告已生成: %TEST_PROJECT%\coverage.xml%RESET%
) else (
    echo %YELLOW%[!] 覆盖率报告生成失败（可选）%RESET%
)
echo.

REM 输出测试结果
echo %BLUE%============================================================%RESET%
if %TEST_RESULT% equ 0 (
    echo %GREEN%[✓] 所有测试通过！%RESET%
    echo %BLUE%============================================================%RESET%
    exit /b 0
) else (
    echo %RED%[X] 测试失败，请查看上面的错误信息%RESET%
    echo %BLUE%============================================================%RESET%
    exit /b 1
)
