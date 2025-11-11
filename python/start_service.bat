@echo off
chcp 65001 >nul
echo ============================================================
echo 🚀 启动 Cloudflare 绕过服务
echo ============================================================
echo.

REM 检查 Python 是否安装
python --version >nul 2>&1
if errorlevel 1 (
    echo ❌ 错误: 未找到 Python
    echo.
    echo 请先安装 Python 3.8+
    echo 下载地址: https://www.python.org/downloads/
    pause
    exit /b 1
)

echo ✅ Python 已安装
echo.

REM 检查依赖是否安装
echo 📦 检查依赖...
pip show undetected-chromedriver >nul 2>&1
if errorlevel 1 (
    echo ⚠️  依赖未安装，正在安装...
    pip install undetected-chromedriver flask requests selenium
    if errorlevel 1 (
        echo ❌ 安装失败
        pause
        exit /b 1
    )
    echo ✅ 依赖安装完成
) else (
    echo ✅ 依赖已安装
)

echo.
echo ============================================================
echo 🌐 启动服务...
echo ============================================================
echo.
echo 服务地址: http://localhost:5000
echo 按 Ctrl+C 停止服务
echo.
echo ============================================================
echo.

REM 启动服务
python cloudflare_bypass_service.py

pause
