using AssetFlow.Domain.Enums;

namespace AssetFlow.Application.DTOs;

public sealed record AssetToolSearchParams(
    string? Search = null,
    AssetCategory? Category = null,
    AssetStatus? Status = null,
    string? Manufacturer = null);
