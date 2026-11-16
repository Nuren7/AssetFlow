using AssetFlow.Domain.Enums;

namespace AssetFlow.Application.DTOs;

public sealed record CreateAssetRequest
{
    public string Name { get; init; } = string.Empty;
    public string SerialNumber { get; init; } = string.Empty;
    public AssetCategory Category { get; init; }
    public string Manufacturer { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public AssetStatus Status { get; init; }
    public string Location { get; init; } = string.Empty;
    public DateOnly PurchaseDate { get; init; }
    public DateOnly WarrantyExpirationDate { get; init; }
}
