# 批量更新命名空间脚本
# 此脚本将把所有FishBrowser.WPF命名空间替换为FishBrowser.Core

$rootPath = "d:\1Dev\webbrowser\web\FishBrowser.Core"

# 获取所有.cs文件
$files = Get-ChildItem -Path $rootPath -Filter "*.cs" -Recurse

foreach ($file in $files) {
    $content = Get-Content -Path $file.FullName -Raw
    
    # 替换各种命名空间
    $content = $content -replace 'namespace FishBrowser.WPF\.Models', 'namespace FishBrowser.Core.Models'
    $content = $content -replace 'namespace FishBrowser.WPF\.Services', 'namespace FishBrowser.Core.Services'
    $content = $content -replace 'namespace FishBrowser.WPF\.Engine', 'namespace FishBrowser.Core.Engine'
    $content = $content -replace 'namespace FishBrowser.WPF\.Domain\.Repositories', 'namespace FishBrowser.Core.Domain.Repositories'
    $content = $content -replace 'namespace FishBrowser.WPF\.Domain\.Services', 'namespace FishBrowser.Core.Domain.Services'
    $content = $content -replace 'namespace FishBrowser.WPF\.Domain\.Entities', 'namespace FishBrowser.Core.Domain.Entities'
    $content = $content -replace 'namespace FishBrowser.WPF\.Domain\.ValueObjects', 'namespace FishBrowser.Core.Domain.ValueObjects'
    $content = $content -replace 'namespace FishBrowser.WPF\.Data', 'namespace FishBrowser.Core.Data'
    $content = $content -replace 'namespace FishBrowser.WPF\.Infrastructure\.Data', 'namespace FishBrowser.Core.Infrastructure.Data'
    $content = $content -replace 'namespace FishBrowser.WPF\.Infrastructure\.Configuration', 'namespace FishBrowser.Core.Infrastructure.Configuration'
    $content = $content -replace 'namespace FishBrowser.WPF\.Application\.DTOs', 'namespace FishBrowser.Core.Application.DTOs'
    $content = $content -replace 'namespace FishBrowser.WPF\.Application\.Services', 'namespace FishBrowser.Core.Application.Services'
    $content = $content -replace 'namespace FishBrowser.WPF\.Application\.Mappers', 'namespace FishBrowser.Core.Application.Mappers'
    $content = $content -replace 'namespace FishBrowser.WPF\.Presentation\.ViewModels', 'namespace FishBrowser.Core.Presentation.ViewModels'
    $content = $content -replace 'namespace FishBrowser.WPF\.Presentation\.Commands', 'namespace FishBrowser.Core.Presentation.Commands'
    $content = $content -replace 'namespace FishBrowser.WPF\.ViewModels', 'namespace FishBrowser.Core.ViewModels'
    $content = $content -replace 'namespace FishBrowser.WPF\.Services\.AIProviderAdapters', 'namespace FishBrowser.Core.Services.AIProviderAdapters'
    
    # 保存文件
    Set-Content -Path $file.FullName -Value $content -NoNewline
}

Write-Host "命名空间更新完成"