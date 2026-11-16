using AssetFlow.Application.DTOs;

namespace AssetFlow.Application.Services;

public interface IAssetService
{
    Task<IReadOnlyList<AssetDto>> GetAllAsync(AssetFilterParams filters, CancellationToken cancellationToken = default);
    Task<AssetDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AssetDto?> GetBySerialNumberAsync(string serialNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AssetDto>> GetExpiringWarrantiesAsync(int daysThreshold = 30, CancellationToken cancellationToken = default);
    Task<DashboardStatsDto> GetDashboardStatsAsync(CancellationToken cancellationToken = default);
    Task<AssetDto> CreateAsync(CreateAssetRequest request, CancellationToken cancellationToken = default);
    Task<AssetDto> UpdateAsync(Guid id, UpdateAssetRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
