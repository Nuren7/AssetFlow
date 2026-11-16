using AssetFlow.Domain.Enums;

namespace AssetFlow.Application.DTOs;

public sealed record DashboardStatsDto(
    int TotalAssets,
    int AvailableCount,
    int AssignedCount,
    int InRepairCount,
    int ExpiringWithin30DaysCount,
    IReadOnlyDictionary<AssetCategory, int> CountByCategory,
    IReadOnlyDictionary<AssetStatus, int> CountByStatus);
