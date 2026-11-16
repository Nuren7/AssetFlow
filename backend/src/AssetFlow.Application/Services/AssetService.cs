using AssetFlow.Application.DTOs;
using AssetFlow.Application.Exceptions;
using AssetFlow.Domain.Entities;
using AssetFlow.Domain.Repositories;

namespace AssetFlow.Application.Services;

public sealed class AssetService(IAssetRepository repository) : IAssetService
{
    public async Task<IReadOnlyList<AssetDto>> GetAllAsync(AssetFilterParams filters, CancellationToken cancellationToken = default)
    {
        var assets = await repository.GetAllAsync(
            filters.Search,
            filters.Category,
            filters.Status,
            filters.Manufacturer,
            cancellationToken);
        return assets.Select(Map).ToArray();
    }

    public async Task<AssetDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var asset = await repository.GetByIdAsync(id, cancellationToken);
        return asset is null ? throw new AssetNotFoundException(id) : Map(asset);
    }

    public async Task<AssetDto?> GetBySerialNumberAsync(string serialNumber, CancellationToken cancellationToken = default)
    {
        var asset = await repository.GetBySerialNumberAsync(serialNumber, cancellationToken);
        return asset is null ? null : Map(asset);
    }

    public async Task<IReadOnlyList<AssetDto>> GetExpiringWarrantiesAsync(
        int daysThreshold = 30,
        CancellationToken cancellationToken = default)
    {
        if (daysThreshold is < 1 or > 365)
        {
            throw new AssetValidationException(new Dictionary<string, string[]>
            {
                [nameof(daysThreshold)] = ["The warranty threshold must be between 1 and 365 days."]
            });
        }

        var assets = await repository.GetExpiringWarrantiesAsync(
            daysThreshold,
            DateOnly.FromDateTime(DateTime.UtcNow),
            cancellationToken);
        return assets.Select(Map).ToArray();
    }

    public async Task<DashboardStatsDto> GetDashboardStatsAsync(CancellationToken cancellationToken = default)
    {
        var stats = await repository.GetStatisticsAsync(
            DateOnly.FromDateTime(DateTime.UtcNow),
            30,
            cancellationToken);

        return new DashboardStatsDto(
            stats.TotalAssets,
            stats.AvailableCount,
            stats.AssignedCount,
            stats.InRepairCount,
            stats.ExpiringWithinDaysCount,
            stats.CountByCategory,
            stats.CountByStatus);
    }

    public async Task<AssetDto> CreateAsync(CreateAssetRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request.Name, request.SerialNumber, request.Manufacturer, request.Model, request.Location,
            request.PurchaseDate, request.WarrantyExpirationDate, request.Category, request.Status);

        if (await repository.ExistsBySerialNumberAsync(request.SerialNumber.Trim(), cancellationToken))
        {
            throw new DuplicateSerialNumberException(request.SerialNumber.Trim());
        }

        var asset = new Asset(
            request.Name.Trim(),
            request.SerialNumber.Trim(),
            request.Category,
            request.Manufacturer.Trim(),
            request.Model.Trim(),
            request.Status,
            request.Location.Trim(),
            request.PurchaseDate,
            request.WarrantyExpirationDate);

        await repository.AddAsync(asset, cancellationToken);
        return Map(asset);
    }

    public async Task<AssetDto> UpdateAsync(Guid id, UpdateAssetRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request.Name, request.SerialNumber, request.Manufacturer, request.Model, request.Location,
            request.PurchaseDate, request.WarrantyExpirationDate, request.Category, request.Status);

        var asset = await repository.GetByIdAsync(id, cancellationToken);
        if (asset is null)
        {
            throw new AssetNotFoundException(id);
        }

        var existing = await repository.GetBySerialNumberAsync(request.SerialNumber.Trim(), cancellationToken);
        if (existing is not null && existing.Id != id)
        {
            throw new DuplicateSerialNumberException(request.SerialNumber.Trim());
        }

        asset.Update(
            request.Name.Trim(),
            request.SerialNumber.Trim(),
            request.Category,
            request.Manufacturer.Trim(),
            request.Model.Trim(),
            request.Status,
            request.Location.Trim(),
            request.PurchaseDate,
            request.WarrantyExpirationDate);
        await repository.UpdateAsync(asset, cancellationToken);
        return Map(asset);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var asset = await repository.GetByIdAsync(id, cancellationToken);
        if (asset is null)
        {
            throw new AssetNotFoundException(id);
        }

        await repository.DeleteAsync(asset, cancellationToken);
    }

    private static void Validate(
        string name,
        string serialNumber,
        string manufacturer,
        string model,
        string location,
        DateOnly purchaseDate,
        DateOnly warrantyExpirationDate,
        AssetFlow.Domain.Enums.AssetCategory category,
        AssetFlow.Domain.Enums.AssetStatus status)
    {
        var errors = new Dictionary<string, string[]>();
        AddRequiredError(errors, nameof(name), name);
        AddRequiredError(errors, nameof(serialNumber), serialNumber);
        AddRequiredError(errors, nameof(manufacturer), manufacturer);
        AddRequiredError(errors, nameof(model), model);
        AddRequiredError(errors, nameof(location), location);
        AddMaximumLengthError(errors, nameof(name), name, 200);
        AddMaximumLengthError(errors, nameof(serialNumber), serialNumber, 120);
        AddMaximumLengthError(errors, nameof(manufacturer), manufacturer, 120);
        AddMaximumLengthError(errors, nameof(model), model, 120);
        AddMaximumLengthError(errors, nameof(location), location, 200);

        if (!Enum.IsDefined(category))
        {
            errors[nameof(category)] = ["Category is invalid."];
        }

        if (!Enum.IsDefined(status))
        {
            errors[nameof(status)] = ["Status is invalid."];
        }

        if (purchaseDate == default)
        {
            errors[nameof(purchaseDate)] = ["Purchase date is required."];
        }
        else if (purchaseDate > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            errors[nameof(purchaseDate)] = ["Purchase date cannot be in the future."];
        }

        if (warrantyExpirationDate == default)
        {
            errors[nameof(warrantyExpirationDate)] = ["Warranty expiration date is required."];
        }
        else if (purchaseDate != default && warrantyExpirationDate < purchaseDate)
        {
            errors[nameof(warrantyExpirationDate)] = ["Warranty expiration date cannot be before purchase date."];
        }

        if (errors.Count > 0)
        {
            throw new AssetValidationException(errors);
        }
    }

    private static void AddRequiredError(IDictionary<string, string[]> errors, string field, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors[field] = [$"{field} is required."];
        }
    }

    private static void AddMaximumLengthError(IDictionary<string, string[]> errors, string field, string value, int maximumLength)
    {
        if (value.Length > maximumLength)
        {
            errors[field] = [$"{field} cannot exceed {maximumLength} characters."];
        }
    }

    private static AssetDto Map(Asset asset) => new(
        asset.Id,
        asset.Name,
        asset.SerialNumber,
        asset.Category,
        asset.Manufacturer,
        asset.Model,
        asset.Status,
        asset.Location,
        asset.PurchaseDate,
        asset.WarrantyExpirationDate,
        asset.CreatedAtUtc,
        asset.UpdatedAtUtc);
}
