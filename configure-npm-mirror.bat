@echo off
chcp 65001 >nul
echo ========================================
echo Configure npm Mirror (Taobao)
echo ========================================
echo.

echo [1/4] Current npm registry:
npm config get registry
echo.

echo [2/4] Setting Taobao mirror...
npm config set registry https://registry.npmmirror.com
echo.

echo [3/4] Verify new configuration:
npm config get registry
echo.

echo [4/4] Test connection speed...
npm info @browserbasehq/stagehand
echo.

echo ========================================
echo Configuration Complete!
echo ========================================
echo.
echo You can now retry installing Stagehand
echo.
pause
