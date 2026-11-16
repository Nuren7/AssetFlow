using AssetFlow.Application.DTOs;
using AssetFlow.Application.Exceptions;
using AssetFlow.Application.Services;
using AssetFlow.Domain.Entities;
using AssetFlow.Domain.Enums;
using AssetFlow.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace AssetFlow.UnitTests;

public sealed class AssetServiceTests
{
    private readonly Mock<IAssetRepository> _repository = new();
    private readonly AssetService _service;

    public AssetServiceTests()
    {
        _service = new AssetService(_repository.Object);
    }

    [Fact]
    public async Task CreateAsync_WithValidAsset_PersistsAndReturnsAsset()
    {
        _repository.Setup(repository => repository.ExistsBySerialNumberAsync("SN-100", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _service.CreateAsync(CreateRequest());

        result.SerialNumber.Should().Be("SN-100");
        result.Name.Should().Be("Laptop");
        _repository.Verify(repository => repository.AddAsync(
            It.Is<Asset>(asset => asset.SerialNumber == "SN-100"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateSerialNumber_ThrowsDuplicateException()
    {
        _repository.Setup(repository => repository.ExistsBySerialNumberAsync("SN-100", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var act = () => _service.CreateAsync(CreateRequest());

        await act.Should().ThrowAsync<DuplicateSerialNumberException>();
        _repository.Verify(repository => repository.AddAsync(It.IsAny<Asset>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("", "SN-100", "Dell", "Latitude", "London")]
    [InlineData("Laptop", "", "Dell", "Latitude", "London")]
    [InlineData("Laptop", "SN-100", "", "Latitude", "London")]
    [InlineData("Laptop", "SN-100", "Dell", "", "London")]
    [InlineData("Laptop", "SN-100", "Dell", "Latitude", "")]
    public async Task CreateAsync_WithMissingRequiredFields_ThrowsValidationException(
        string name,
        string serialNumber,
        string manufacturer,
        string model,
        string location)
    {
        var request = CreateRequest() with
        {
            Name = name,
            SerialNumber = serialNumber,
            Manufacturer = manufacturer,
            Model = model,
            Location = location
        };

        var act = () => _service.CreateAsync(request);

        await act.Should().ThrowAsync<AssetValidationException>();
        _repository.Verify(repository => repository.AddAsync(It.IsAny<Asset>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenWarrantyPrecedesPurchase_ThrowsValidationException()
    {
        var request = CreateRequest() with { WarrantyExpirationDate = new DateOnly(2023, 12, 31) };

        var act = () => _service.CreateAsync(request);

        await act.Should().ThrowAsync<AssetValidationException>()
            .Where(exception => exception.Errors.ContainsKey("warrantyExpirationDate"));
    }

    [Fact]
    public async Task CreateAsync_WhenPurchaseDateIsInTheFuture_ThrowsValidationException()
    {
        var futurePurchaseDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
        var request = CreateRequest() with
        {
            PurchaseDate = futurePurchaseDate,
            WarrantyExpirationDate = futurePurchaseDate.AddYears(1)
        };

        var act = () => _service.CreateAsync(request);

        await act.Should().ThrowAsync<AssetValidationException>()
            .Where(exception => exception.Errors.ContainsKey("purchaseDate"));
    }

    [Fact]
    public async Task UpdateAsync_WithValidRequest_UpdatesTheExistingAsset()
    {
        var asset = CreateEntity();
        _repository.Setup(repository => repository.GetByIdAsync(asset.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(asset);
        _repository.Setup(repository => repository.GetBySerialNumberAsync("SN-200", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Asset?)null);

        var result = await _service.UpdateAsync(asset.Id, CreateUpdateRequest() with { SerialNumber = "SN-200" });

        result.SerialNumber.Should().Be("SN-200");
        result.UpdatedAtUtc.Should().NotBeNull();
        _repository.Verify(repository => repository.UpdateAsync(asset, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenSerialNumberBelongsToAnotherAsset_ThrowsDuplicateException()
    {
        var asset = CreateEntity();
        var otherAsset = CreateEntity();
        _repository.Setup(repository => repository.GetByIdAsync(asset.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(asset);
        _repository.Setup(repository => repository.GetBySerialNumberAsync("SN-100", It.IsAny<CancellationToken>()))
            .ReturnsAsync(otherAsset);

        var act = () => _service.UpdateAsync(asset.Id, CreateUpdateRequest());

        await act.Should().ThrowAsync<DuplicateSerialNumberException>();
        _repository.Verify(repository => repository.UpdateAsync(It.IsAny<Asset>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WithMissingRequiredField_ThrowsValidationException()
    {
        var request = CreateUpdateRequest() with { Name = " " };

        var act = () => _service.UpdateAsync(Guid.NewGuid(), request);

        await act.Should().ThrowAsync<AssetValidationException>();
        _repository.Verify(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("Latitude")]
    [InlineData("LAT-7440")]
    [InlineData("SN-100")]
    public async Task GetAllAsync_PassesSearchTermsForNameModelAndSerialNumberToRepository(string search)
    {
        _repository.Setup(repository => repository.GetAllAsync(
                search,
                AssetCategory.Laptop,
                AssetStatus.Available,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Asset>());

        await _service.GetAllAsync(new AssetFilterParams(search, AssetCategory.Laptop, AssetStatus.Available));

        _repository.Verify(repository => repository.GetAllAsync(
            search,
            AssetCategory.Laptop,
            AssetStatus.Available,
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_PassesManufacturerCategoryAndStatusFiltersToRepository()
    {
        _repository.Setup(repository => repository.GetAllAsync(
                "ThinkPad",
                AssetCategory.Laptop,
                AssetStatus.Assigned,
                "Lenovo",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Asset>());

        await _service.GetAllAsync(new AssetFilterParams("ThinkPad", AssetCategory.Laptop, AssetStatus.Assigned, "Lenovo"));

        _repository.Verify(repository => repository.GetAllAsync(
            "ThinkPad",
            AssetCategory.Laptop,
            AssetStatus.Assigned,
            "Lenovo",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private static CreateAssetRequest CreateRequest() => new()
    {
        Name = " Laptop ",
        SerialNumber = " SN-100 ",
        Category = AssetCategory.Laptop,
        Manufacturer = " Dell ",
        Model = " Latitude 7440 ",
        Status = AssetStatus.Available,
        Location = " London ",
        PurchaseDate = new DateOnly(2024, 1, 1),
        WarrantyExpirationDate = new DateOnly(2026, 1, 1)
    };

    private static UpdateAssetRequest CreateUpdateRequest() => new()
    {
        Name = "Laptop",
        SerialNumber = "SN-100",
        Category = AssetCategory.Laptop,
        Manufacturer = "Dell",
        Model = "Latitude 7440",
        Status = AssetStatus.Assigned,
        Location = "London",
        PurchaseDate = new DateOnly(2024, 1, 1),
        WarrantyExpirationDate = new DateOnly(2026, 1, 1)
    };

    private static Asset CreateEntity() => new(
        "Laptop",
        "SN-100",
        AssetCategory.Laptop,
        "Dell",
        "Latitude 7440",
        AssetStatus.Available,
        "London",
        new DateOnly(2024, 1, 1),
        new DateOnly(2026, 1, 1));
}
