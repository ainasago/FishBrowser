using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FishBrowser.WPF.Services;
using FishBrowser.WPF.Models;

namespace FishBrowser.Api.Controllers;

[ApiController]
[Route("api/aiproviders")]
[Authorize]
public class AIProviderController : ControllerBase
{
    private readonly AIProviderManagementService _service;

    public AIProviderController(AIProviderManagementService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<AIProviderDto>>> GetAll()
    {
        var providers = await _service.GetAllProvidersAsync();
        return Ok(providers);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AIProviderDto>> GetById(int id)
    {
        var provider = await _service.GetProviderByIdAsync(id);
        if (provider == null) return NotFound();
        return Ok(provider);
    }

    [HttpPost]
    public async Task<ActionResult<AIProviderDto>> Create([FromBody] CreateProviderRequest request)
    {
        var provider = await _service.CreateProviderAsync(request);
        return Ok(provider);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<AIProviderDto>> Update(int id, [FromBody] UpdateProviderRequest request)
    {
        var provider = await _service.UpdateProviderAsync(id, request);
        return Ok(provider);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteProviderAsync(id);
        return Ok(new { success = true });
    }

    [HttpPost("{id}/apikeys")]
    public async Task<ActionResult<ApiKeyDto>> AddApiKey(int id, [FromBody] AddApiKeyRequest request)
    {
        var apiKey = await _service.AddApiKeyAsync(id, request);
        return Ok(apiKey);
    }

    [HttpDelete("apikeys/{keyId}")]
    public async Task<IActionResult> DeleteApiKey(int keyId)
    {
        await _service.DeleteApiKeyAsync(keyId);
        return Ok(new { success = true });
    }

    [HttpPost("{id}/test")]
    public async Task<ActionResult<TestConnectionResult>> TestConnection(int id)
    {
        var result = await _service.TestConnectionAsync(id);
        return Ok(result);
    }
}
