using AssetFlow.Domain.Entities;
using AssetFlow.Domain.Enums;
using AssetFlow.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AssetFlow.UnitTests;

public sealed class AssetRepositoryTests
{
    [Fact]
    public async Task GetAllAsync_SearchesNameModelAndSerialNumberAndAppliesFilters()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AssetFlowDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var context = new AssetFlowDbContext(options);
        await context.Database.EnsureCreatedAsync();
        context.Assets.AddRange(
            CreateAsset("Engineering Laptop", "DELL-100", AssetCategory.Laptop, "Dell", "Latitude 7440", AssetStatus.Assigned),
            CreateAsset("Conference Display", "DELL-200", AssetCategory.Monitor, "Dell", "UltraSharp U2723", AssetStatus.Available),
            CreateAsset("Support Notebook", "LEN-300", AssetCategory.Laptop, "Lenovo", "ThinkPad X1", AssetStatus.Available));
        await context.SaveChangesAsync();
        var repository = new AssetRepository(context);

        (await repository.GetAllAsync(search: "engineering")).Select(asset => asset.SerialNumber)
            .Should().ContainSingle().Which.Should().Be("DELL-100");
        (await repository.GetAllAsync(search: "u2723")).Select(asset => asset.SerialNumber)
            .Should().ContainSingle().Which.Should().Be("DELL-200");
        (await repository.GetAllAsync(search: "len-300")).Select(asset => asset.SerialNumber)
            .Should().ContainSingle().Which.Should().Be("LEN-300");
        (await repository.GetAllAsync(category: AssetCategory.Laptop, status: AssetStatus.Available))
            .Select(asset => asset.SerialNumber).Should().ContainSingle().Which.Should().Be("LEN-300");
        (await repository.GetAllAsync(manufacturer: "dell"))
            .Should().HaveCount(2);
    }

    [Fact]
    public async Task GetExpiringWarrantiesAndStatistics_UseTheRequestedDateWindow()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AssetFlowDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var context = new AssetFlowDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var today = new DateOnly(2026, 11, 16);
        context.Assets.AddRange(
            CreateAsset("Expiring Laptop", "EXP-1", AssetCategory.Laptop, "Dell", "Latitude", AssetStatus.Available, today.AddDays(30)),
            CreateAsset("Later Monitor", "EXP-2", AssetCategory.Monitor, "Samsung", "ViewFinity", AssetStatus.Assigned, today.AddDays(31)),
            CreateAsset("Expired Phone", "EXP-3", AssetCategory.Mobile, "Apple", "iPhone", AssetStatus.InRepair, today.AddDays(-1)));
        await context.SaveChangesAsync();
        var repository = new AssetRepository(context);

        var expiring = await repository.GetExpiringWarrantiesAsync(30, today);
        var stats = await repository.GetStatisticsAsync(today, 30);

        expiring.Select(asset => asset.SerialNumber).Should().ContainSingle().Which.Should().Be("EXP-1");
        stats.TotalAssets.Should().Be(3);
        stats.AvailableCount.Should().Be(1);
        stats.AssignedCount.Should().Be(1);
        stats.InRepairCount.Should().Be(1);
        stats.ExpiringWithinDaysCount.Should().Be(1);
    }

    private static Asset CreateAsset(
        string name,
        string serialNumber,
        AssetCategory category,
        string manufacturer,
        string model,
        AssetStatus status,
        DateOnly? warrantyExpirationDate = null)
    {
        var warranty = warrantyExpirationDate ?? new DateOnly(2028, 1, 1);
        return new Asset(
            name,
            serialNumber,
            category,
            manufacturer,
            model,
            status,
            "London",
            warranty.AddYears(-1),
            warranty);
    }
}
