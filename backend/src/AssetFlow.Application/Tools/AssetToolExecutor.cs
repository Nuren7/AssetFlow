using System.Text.Json;
using System.Text.Json.Serialization;
using AssetFlow.Application.DTOs;
using AssetFlow.Application.Exceptions;
using AssetFlow.Application.Services;
using AssetFlow.Domain.Enums;

namespace AssetFlow.Application.Tools;

public sealed class AssetToolExecutor(IAssetService assetService) : IAssetToolExecutor
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public Task<DashboardStatsDto> GetAssetsSummaryAsync(CancellationToken cancellationToken = default) =>
        assetService.GetDashboardStatsAsync(cancellationToken);

    public Task<IReadOnlyList<AssetDto>> SearchAssetsAsync(
        AssetToolSearchParams filters,
        CancellationToken cancellationToken = default) =>
        assetService.GetAllAsync(new AssetFilterParams(filters.Search, filters.Category, filters.Status, filters.Manufacturer), cancellationToken);

    public Task<IReadOnlyList<AssetDto>> GetExpiringWarrantiesAsync(
        int daysThreshold = 30,
        CancellationToken cancellationToken = default) =>
        assetService.GetExpiringWarrantiesAsync(daysThreshold, cancellationToken);

    public Task<AssetDto?> GetAssetBySerialNumberAsync(string serialNumber, CancellationToken cancellationToken = default) =>
        assetService.GetBySerialNumberAsync(serialNumber, cancellationToken);

    public async Task<string> ExecuteAsync(string toolName, JsonElement arguments, CancellationToken cancellationToken = default)
    {
        if (arguments.ValueKind != JsonValueKind.Object)
        {
            throw new ToolArgumentException("Tool arguments must be a JSON object.");
        }

        object result = toolName switch
        {
            "get_assets_summary" => await GetAssetsSummaryAsync(cancellationToken),
            "search_assets" => await SearchAssetsAsync(ParseSearchFilters(arguments), cancellationToken),
            "get_expiring_warranties" => await GetExpiringWarrantiesAsync(ParseDaysThreshold(arguments), cancellationToken),
            "get_asset_by_serial_number" => await GetAssetLookupResultAsync(
                ParseRequiredString(arguments, "serialNumber"),
                cancellationToken),
            _ => throw new ToolArgumentException($"The tool '{toolName}' is not supported.")
        };

        return JsonSerializer.Serialize(result, JsonOptions);
    }

    private static AssetToolSearchParams ParseSearchFilters(JsonElement arguments)
    {
        return new AssetToolSearchParams(
            GetOptionalString(arguments, "search"),
            ParseOptionalEnum<AssetCategory>(arguments, "category"),
            ParseOptionalEnum<AssetStatus>(arguments, "status"),
            GetOptionalString(arguments, "manufacturer"));
    }

    private async Task<object> GetAssetLookupResultAsync(string serialNumber, CancellationToken cancellationToken)
    {
        return await GetAssetBySerialNumberAsync(serialNumber, cancellationToken)
            ?? (object)new { message = "No asset was found with that serial number." };
    }

    private static int ParseDaysThreshold(JsonElement arguments)
    {
        if (!arguments.TryGetProperty("daysThreshold", out var value))
        {
            return 30;
        }

        if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out var days) || days is < 1 or > 365)
        {
            throw new ToolArgumentException("'daysThreshold' must be an integer between 1 and 365.");
        }

        return days;
    }

    private static string ParseRequiredString(JsonElement arguments, string propertyName)
    {
        var value = GetOptionalString(arguments, propertyName);
        return string.IsNullOrWhiteSpace(value)
            ? throw new ToolArgumentException($"'{propertyName}' is required.")
            : value.Trim();
    }

    private static string? GetOptionalString(JsonElement arguments, string propertyName)
    {
        if (!arguments.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        if (value.ValueKind != JsonValueKind.String)
        {
            throw new ToolArgumentException($"'{propertyName}' must be a string.");
        }

        return value.GetString();
    }

    private static TEnum? ParseOptionalEnum<TEnum>(JsonElement arguments, string propertyName)
        where TEnum : struct, Enum
    {
        var value = GetOptionalString(arguments, propertyName);
        if (value is null)
        {
            return null;
        }

        return Enum.TryParse<TEnum>(value, true, out var parsed) && Enum.IsDefined(parsed)
            ? parsed
            : throw new ToolArgumentException($"'{propertyName}' is not a valid {typeof(TEnum).Name}.");
    }
}
