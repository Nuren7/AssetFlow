using AssetFlow.Application.DTOs;
using AssetFlow.Application.Services;
using AssetFlow.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace AssetFlow.API.Controllers;

[ApiController]
[Route("api/assets")]
public sealed class AssetsController(IAssetService assetService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<AssetDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AssetDto>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] AssetCategory? category,
        [FromQuery] AssetStatus? status,
        CancellationToken cancellationToken)
    {
        var assets = await assetService.GetAllAsync(
            new AssetFilterParams(search, category, status),
            cancellationToken);
        return Ok(assets);
    }

    [HttpGet("stats")]
    [ProducesResponseType<DashboardStatsDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardStatsDto>> GetStats(CancellationToken cancellationToken) =>
        Ok(await assetService.GetDashboardStatsAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType<AssetDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssetDto>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await assetService.GetByIdAsync(id, cancellationToken));

    [HttpPost]
    [ProducesResponseType<AssetDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AssetDto>> Create(
        CreateAssetRequest request,
        CancellationToken cancellationToken)
    {
        var asset = await assetService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = asset.Id }, asset);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<AssetDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AssetDto>> Update(
        Guid id,
        UpdateAssetRequest request,
        CancellationToken cancellationToken) =>
        Ok(await assetService.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await assetService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
