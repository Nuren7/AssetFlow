using AssetFlow.Domain.Enums;

namespace AssetFlow.Application.DTOs;

public sealed record AssetDto(
    Guid Id,
    string Name,
    string SerialNumber,
    AssetCategory Category,
    string Manufacturer,
    string Model,
    AssetStatus Status,
    string Location,
    DateOnly PurchaseDate,
    DateOnly WarrantyExpirationDate,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
