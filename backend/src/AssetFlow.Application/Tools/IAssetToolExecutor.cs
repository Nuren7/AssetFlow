using System.Text.Json;
using AssetFlow.Application.DTOs;

namespace AssetFlow.Application.Tools;

public interface IAssetToolExecutor
{
    Task<DashboardStatsDto> GetAssetsSummaryAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AssetDto>> SearchAssetsAsync(AssetToolSearchParams filters, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AssetDto>> GetExpiringWarrantiesAsync(int daysThreshold = 30, CancellationToken cancellationToken = default);
    Task<AssetDto?> GetAssetBySerialNumberAsync(string serialNumber, CancellationToken cancellationToken = default);
    Task<string> ExecuteAsync(string toolName, JsonElement arguments, CancellationToken cancellationToken = default);
}
