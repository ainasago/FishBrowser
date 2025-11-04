# ============================================================
# WebScraper 项目测试脚本 (PowerShell 版本)
# ============================================================
# 功能: 运行所有单元测试和生成覆盖率报告
# 用法: .\run-tests.ps1
# ============================================================

param(
    [switch]$Coverage = $false,
    [switch]$Verbose = $false,
    [switch]$Clean = $false,
    [string]$Filter = ""
)

# 颜色定义
$colors = @{
    Green  = "`e[92m"
    Red    = "`e[91m"
    Yellow = "`e[93m"
    Blue   = "`e[94m"
    Reset  = "`e[0m"
}

function Write-Header {
    param([string]$Message)
    Write-Host ""
    Write-Host "$($colors.Blue)============================================================$($colors.Reset)"
    Write-Host "$($colors.Blue)  $Message$($colors.Reset)"
    Write-Host "$($colors.Blue)============================================================$($colors.Reset)"
    Write-Host ""
}

function Write-Status {
    param([string]$Message, [string]$Type = "Info")
    
    switch ($Type) {
        "Success" { Write-Host "$($colors.Green)[✓]$($colors.Reset) $Message" }
        "Error"   { Write-Host "$($colors.Red)[X]$($colors.Reset) $Message" }
        "Warning" { Write-Host "$($colors.Yellow)[!]$($colors.Reset) $Message" }
        default   { Write-Host "$($colors.Yellow)[*]$($colors.Reset) $Message" }
    }
}

# 主程序开始
Write-Header "WebScraper 项目测试脚本"

# 获取脚本目录
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$testProject = Join-Path $scriptDir "WebScraperApp.Tests"

# 检查 dotnet
Write-Status "检查 dotnet 环境..."
$dotnetVersion = dotnet --version 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Status "未找到 dotnet，请先安装 .NET SDK" "Error"
    exit 1
}
Write-Status "dotnet 版本: $dotnetVersion" "Success"

# 检查测试项目
Write-Status "检查测试项目..."
if (-not (Test-Path $testProject)) {
    Write-Status "测试项目不存在: $testProject" "Error"
    exit 1
}
Write-Status "测试项目检查完成" "Success"

# 清理构建
if ($Clean) {
    Write-Status "清理之前的构建..."
    Push-Location $scriptDir
    dotnet clean $testProject 2>&1 | Out-Null
    Write-Status "清理完成" "Success"
    Pop-Location
}

# 恢复依赖
Write-Status "恢复 NuGet 依赖..."
Push-Location $scriptDir
dotnet restore $testProject 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Status "恢复依赖失败" "Error"
    Pop-Location
    exit 1
}
Write-Status "依赖恢复完成" "Success"

# 构建测试项目
Write-Status "构建测试项目..."
dotnet build $testProject --configuration Release 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Status "构建测试项目失败" "Error"
    Pop-Location
    exit 1
}
Write-Status "构建完成" "Success"

# 运行测试
Write-Status "运行单元测试..."
Write-Host ""

$testArgs = @(
    "test"
    $testProject
    "--configuration", "Release"
    "--verbosity", $(if ($Verbose) { "detailed" } else { "normal" })
)

if ($Filter) {
    $testArgs += "--filter", $Filter
}

if ($Verbose) {
    $testArgs += "--logger", "console;verbosity=detailed"
}

dotnet @testArgs
$testResult = $LASTEXITCODE

Write-Host ""

# 生成覆盖率报告
if ($Coverage) {
    Write-Status "生成代码覆盖率报告..."
    
    $coverageArgs = @(
        "test"
        $testProject
        "--configuration", "Release"
        "/p:CollectCoverage=true"
        "/p:CoverageFormat=opencover"
        "/p:CoverageFileName=coverage.xml"
        "/p:Exclude=`"[*]*.Tests*`""
    )
    
    dotnet @coverageArgs 2>&1 | Out-Null
    
    $coverageFile = Join-Path $testProject "coverage.xml"
    if (Test-Path $coverageFile) {
        Write-Status "覆盖率报告已生成: $coverageFile" "Success"
    } else {
        Write-Status "覆盖率报告生成失败（可选）" "Warning"
    }
}

# 输出最终结果
Write-Host ""
Write-Host "$($colors.Blue)============================================================$($colors.Reset)"

if ($testResult -eq 0) {
    Write-Status "所有测试通过！" "Success"
    Write-Host "$($colors.Blue)============================================================$($colors.Reset)"
    exit 0
} else {
    Write-Status "测试失败，请查看上面的错误信息" "Error"
    Write-Host "$($colors.Blue)============================================================$($colors.Reset)"
    exit 1
}

Pop-Location
