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
    private readonly PlaywrightMaintenanceService _service;

    public SystemController(PlaywrightMaintenanceService service)
    {
        _service = service;
    }

    [HttpGet("playwright/status")]
    public async Task<ActionResult<PlaywrightStatusDto>> GetStatus()
    {
        var s = await _service.GetStatusAsync();
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
    public async Task<IActionResult> Install()
    {
        await _service.InstallAsync();
        return Ok(new { success = true });
    }

    [HttpPost("playwright/update")]
    public async Task<IActionResult> Update()
    {
        await _service.UpdateAsync();
        return Ok(new { success = true });
    }

    [HttpPost("playwright/uninstall")]
    public async Task<IActionResult> Uninstall()
    {
        await _service.UninstallAsync();
        return Ok(new { success = true });
    }
}
