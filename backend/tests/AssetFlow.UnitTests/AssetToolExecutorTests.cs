using System.Text.Json;
using AssetFlow.Application.DTOs;
using AssetFlow.Application.Exceptions;
using AssetFlow.Application.Services;
using AssetFlow.Application.Tools;
using AssetFlow.Domain.Enums;
using FluentAssertions;
using Moq;

namespace AssetFlow.UnitTests;

public sealed class AssetToolExecutorTests
{
    private readonly Mock<IAssetService> _assetService = new();
    private readonly AssetToolExecutor _executor;

    public AssetToolExecutorTests()
    {
        _executor = new AssetToolExecutor(_assetService.Object);
    }

    [Fact]
    public async Task GetExpiringWarrantiesWithoutThreshold_UsesThirtyDayDefault()
    {
        _assetService.Setup(service => service.GetExpiringWarrantiesAsync(30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<AssetDto>());

        using var document = JsonDocument.Parse("{}");
        var result = await _executor.ExecuteAsync("get_expiring_warranties", document.RootElement);

        result.Should().Be("[]");
        _assetService.Verify(service => service.GetExpiringWarrantiesAsync(30, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SearchAssets_ParsesCategoryStatusAndManufacturerFilters()
    {
        _assetService.Setup(service => service.GetAllAsync(
                It.Is<AssetFilterParams>(filters =>
                    filters.Search == "ThinkPad" &&
                    filters.Category == AssetCategory.Laptop &&
                    filters.Status == AssetStatus.Assigned &&
                    filters.Manufacturer == "Lenovo"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<AssetDto>());

        using var document = JsonDocument.Parse(
            """{"search":"ThinkPad","category":"Laptop","status":"Assigned","manufacturer":"Lenovo"}""");
        var result = await _executor.ExecuteAsync("search_assets", document.RootElement);

        result.Should().Be("[]");
        _assetService.Verify(service => service.GetAllAsync(It.IsAny<AssetFilterParams>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAssetBySerialNumber_RequiresAnExactSerialNumber()
    {
        _assetService.Setup(service => service.GetBySerialNumberAsync("SN-42", It.IsAny<CancellationToken>()))
            .ReturnsAsync((AssetDto?)null);
        using var document = JsonDocument.Parse("""{"serialNumber":"SN-42"}""");

        var result = await _executor.ExecuteAsync("get_asset_by_serial_number", document.RootElement);

        result.Should().Contain("No asset was found");
        _assetService.Verify(service => service.GetBySerialNumberAsync("SN-42", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("""{"serialNumber":3}""")]
    [InlineData("""{}""")]
    public async Task GetAssetBySerialNumber_WithInvalidArguments_ThrowsToolArgumentException(string json)
    {
        using var document = JsonDocument.Parse(json);

        var act = () => _executor.ExecuteAsync("get_asset_by_serial_number", document.RootElement);

        await act.Should().ThrowAsync<ToolArgumentException>();
    }

    [Fact]
    public async Task GetExpiringWarranties_RejectsOutOfRangeThreshold()
    {
        using var document = JsonDocument.Parse("""{"daysThreshold":500}""");

        var act = () => _executor.ExecuteAsync("get_expiring_warranties", document.RootElement);

        await act.Should().ThrowAsync<ToolArgumentException>();
    }
}
