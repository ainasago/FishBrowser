using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FishBrowser.WPF.Services;
using FishBrowser.Api.DTOs;

namespace FishBrowser.Api.Controllers;

[ApiController]
[Route("api/system")]
[Authorize]
public class SystemController : ControllerBase
{
    private readonly PlaywrightMaintenanceService _playwrightService;
    private readonly StagehandMaintenanceService _stagehandService;

    public SystemController(PlaywrightMaintenanceService playwrightService, StagehandMaintenanceService stagehandService)
    {
        _playwrightService = playwrightService;
        _stagehandService = stagehandService;
    }

    [HttpGet("playwright/status")]
    public async Task<ActionResult<PlaywrightStatusDto>> GetPlaywrightStatus()
    {
        var s = await _playwrightService.GetStatusAsync();
        var dto = new PlaywrightStatusDto
        {
            IsInstalled = s.IsInstalled,
            InstallPath = s.InstallPath,
            BrowserList = s.BrowserList,
            PackageVersion = s.PackageVersion,
            CliVersion = s.CliVersion,
            VersionDisplay = s.VersionDisplay
        };
        return Ok(dto);
    }

    [HttpPost("playwright/install")]
    public async Task<IActionResult> InstallPlaywright()
    {
        await _playwrightService.InstallAsync();
        return Ok(new { success = true });
    }

    [HttpPost("playwright/update")]
    public async Task<IActionResult> UpdatePlaywright()
    {
        await _playwrightService.UpdateAsync();
        return Ok(new { success = true });
    }

    [HttpPost("playwright/uninstall")]
    public async Task<IActionResult> UninstallPlaywright()
    {
        await _playwrightService.UninstallAsync();
        return Ok(new { success = true });
    }

    // Stagehand 端点
    [HttpGet("stagehand/status")]
    public async Task<ActionResult<StagehandStatusDto>> GetStagehandStatus()
    {
        var s = await _stagehandService.GetStatusAsync();
        var dto = new StagehandStatusDto
        {
            IsNodeInstalled = s.IsNodeInstalled,
            NodeVersion = s.NodeVersion,
            NpmVersion = s.NpmVersion,
            IsInstalled = s.IsInstalled,
            InstallPath = s.InstallPath,
            InstalledVersion = s.InstalledVersion,
            LatestVersion = s.LatestVersion,
            PlaywrightInstalled = s.PlaywrightInstalled,
            VersionDisplay = s.VersionDisplay,
            HasUpdate = s.HasUpdate
        };
        return Ok(dto);
    }

    [HttpPost("stagehand/install")]
    public async Task<IActionResult> InstallStagehand()
    {
        await _stagehandService.InstallAsync();
        return Ok(new { success = true });
    }

    [HttpPost("stagehand/update")]
    public async Task<IActionResult> UpdateStagehand()
    {
        await _stagehandService.UpdateAsync();
        return Ok(new { success = true });
    }

    [HttpPost("stagehand/uninstall")]
    public async Task<IActionResult> UninstallStagehand()
    {
        await _stagehandService.UninstallAsync();
        return Ok(new { success = true });
    }

    [HttpPost("stagehand/test")]
    public async Task<ActionResult<bool>> TestStagehand()
    {
        var result = await _stagehandService.TestStagehandAsync();
        return Ok(new { success = result });
    }
}
