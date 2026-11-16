using AssetFlow.Domain.Entities;
using AssetFlow.Domain.Enums;

namespace AssetFlow.Domain.Repositories;

public interface IAssetRepository
{
    Task<IReadOnlyList<Asset>> GetAllAsync(
        string? search = null,
        AssetCategory? category = null,
        AssetStatus? status = null,
        string? manufacturer = null,
        CancellationToken cancellationToken = default);

    Task<Asset?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Asset?> GetBySerialNumberAsync(string serialNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Asset>> GetExpiringWarrantiesAsync(int daysThreshold, DateOnly today, CancellationToken cancellationToken = default);
    Task<AssetStatistics> GetStatisticsAsync(DateOnly today, int expiringWithinDays, CancellationToken cancellationToken = default);
    Task AddAsync(Asset asset, CancellationToken cancellationToken = default);
    Task UpdateAsync(Asset asset, CancellationToken cancellationToken = default);
    Task DeleteAsync(Asset asset, CancellationToken cancellationToken = default);
    Task<bool> ExistsBySerialNumberAsync(string serialNumber, CancellationToken cancellationToken = default);
}

public sealed record AssetStatistics(
    int TotalAssets,
    int AvailableCount,
    int AssignedCount,
    int InRepairCount,
    int ExpiringWithinDaysCount,
    IReadOnlyDictionary<AssetCategory, int> CountByCategory,
    IReadOnlyDictionary<AssetStatus, int> CountByStatus);
