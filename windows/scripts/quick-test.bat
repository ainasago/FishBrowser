@echo off
REM ============================================================
REM WebScraper 快速测试脚本
REM ============================================================
REM 功能: 快速运行所有单元测试
REM ============================================================

setlocal enabledelayedexpansion

set "PROJECT_DIR=%~dp0"

echo.
echo ============================================================
echo  WebScraper 快速测试
echo ============================================================
echo.

cd /d "%PROJECT_DIR%"

REM 运行测试
echo [*] 运行单元测试...
echo.
dotnet test WebScraperApp.Tests --verbosity normal

REM 检查结果
if errorlevel 1 (
    echo.
    echo [X] 测试失败
    exit /b 1
) else (
    echo.
    echo [✓] 所有测试通过
    exit /b 0
)
