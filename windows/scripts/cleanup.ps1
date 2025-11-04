param([switch]$Force)

$docDir = "d:\1Dev\webscraper\windows\docs"
$rootDir = "d:\1Dev\webscraper\windows"

Write-Host "Cleaning up old documentation files..."

$deleteFiles = @(
    "ARCHITECTURE_REFACTORING_COMPLETE.md",
    "COPY_NOTIFICATION_FEATURE.md",
    "DATAGRID_COPY_FIX.md",
    "DOMAIN_LAYER_COMPLETE.md",
    "FINAL_COPY_NOTIFICATION.md",
    "FINAL_VERIFICATION_SUMMARY.md",
    "FINGERPRINT_ANALYSIS_SUMMARY.md",
    "FINGERPRINT_PRESETS_COMPLETE.md",
    "FINGERPRINT_UI_ENHANCEMENT.md",
    "IMPLEMENTATION_CHECKLIST.md",
    "INSTALL_PLAYWRIGHT.md",
    "M1_IMPLEMENTATION_STATUS.md",
    "M2_COMPLETION_SUMMARY.md",
    "M2_IMPLEMENTATION_PLAN.md",
    "M2_PHASE1_COMPLETION.md",
    "M2_PHASE2_COMPLETION.md",
    "M2_PHASE3_COMPLETION.md",
    "PLAYWRIGHT_VERSION_CHECK_FIX.md",
    "PRESENTATION_LAYER_COMPLETE.md",
    "PROJECT_PROGRESS_SUMMARY.md",
    "PROJECT_STRUCTURE.md",
    "PROXY_POOL_COMPLETE.md",
    "SERILOG_GUIDE.md",
    "TASK_MANAGEMENT_IMPROVEMENTS.md",
    "TESTING_FRAMEWORK_COMPLETE.md",
    "TEST_FUNCTIONALITY_GUIDE.md",
    "TEST_VERIFICATION_CHECKLIST.md",
    "cleanup.ps1",
    "do_cleanup.bat"
)

$deletedCount = 0
foreach ($file in $deleteFiles) {
    $path = Join-Path $rootDir $file
    if (Test-Path $path) {
        Remove-Item $path -Force -ErrorAction SilentlyContinue
        Write-Host "Deleted: $file"
        $deletedCount++
    }
}

Write-Host "`nCleanup complete! Deleted $deletedCount files."
