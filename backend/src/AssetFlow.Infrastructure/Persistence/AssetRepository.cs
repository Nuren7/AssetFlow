using AssetFlow.Application.Exceptions;
using AssetFlow.Domain.Entities;
using AssetFlow.Domain.Enums;
using AssetFlow.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Npgsql;

namespace AssetFlow.Infrastructure.Persistence;

public sealed class AssetRepository(AssetFlowDbContext dbContext) : IAssetRepository
{
    public async Task<IReadOnlyList<Asset>> GetAllAsync(
        string? search = null,
        AssetCategory? category = null,
        AssetStatus? status = null,
        string? manufacturer = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Asset> query = dbContext.Assets.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(asset =>
                asset.Name.ToLower().Contains(term) ||
                asset.Model.ToLower().Contains(term) ||
                asset.SerialNumber.ToLower().Contains(term));
        }

        if (category.HasValue)
        {
            query = query.Where(asset => asset.Category == category.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(asset => asset.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(manufacturer))
        {
            var manufacturerTerm = manufacturer.Trim().ToLower();
            query = query.Where(asset => asset.Manufacturer.ToLower().Contains(manufacturerTerm));
        }

        return await query.OrderBy(asset => asset.Name).ToListAsync(cancellationToken);
    }

    public Task<Asset?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Assets.FirstOrDefaultAsync(asset => asset.Id == id, cancellationToken);

    public Task<Asset?> GetBySerialNumberAsync(string serialNumber, CancellationToken cancellationToken = default) =>
        dbContext.Assets.FirstOrDefaultAsync(asset => asset.SerialNumber == serialNumber, cancellationToken);

    public async Task<IReadOnlyList<Asset>> GetExpiringWarrantiesAsync(
        int daysThreshold,
        DateOnly today,
        CancellationToken cancellationToken = default)
    {
        var throughDate = today.AddDays(daysThreshold);
        return await dbContext.Assets.AsNoTracking()
            .Where(asset => asset.WarrantyExpirationDate >= today && asset.WarrantyExpirationDate <= throughDate)
            .OrderBy(asset => asset.WarrantyExpirationDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<AssetStatistics> GetStatisticsAsync(
        DateOnly today,
        int expiringWithinDays,
        CancellationToken cancellationToken = default)
    {
        var assets = dbContext.Assets.AsNoTracking();
        var throughDate = today.AddDays(expiringWithinDays);
        var total = await assets.CountAsync(cancellationToken);
        var available = await assets.CountAsync(asset => asset.Status == AssetStatus.Available, cancellationToken);
        var assigned = await assets.CountAsync(asset => asset.Status == AssetStatus.Assigned, cancellationToken);
        var inRepair = await assets.CountAsync(asset => asset.Status == AssetStatus.InRepair, cancellationToken);
        var expiring = await assets.CountAsync(
            asset => asset.WarrantyExpirationDate >= today && asset.WarrantyExpirationDate <= throughDate,
            cancellationToken);
        var byCategory = await assets.GroupBy(asset => asset.Category)
            .Select(group => new { Category = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.Category, item => item.Count, cancellationToken);
        var byStatus = await assets.GroupBy(asset => asset.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.Status, item => item.Count, cancellationToken);

        return new AssetStatistics(total, available, assigned, inRepair, expiring, byCategory, byStatus);
    }

    public async Task AddAsync(Asset asset, CancellationToken cancellationToken = default)
    {
        await dbContext.Assets.AddAsync(asset, cancellationToken);
        await SaveChangesAsync(dbContext.Entry(asset), asset.SerialNumber, cancellationToken);
    }

    public async Task UpdateAsync(Asset asset, CancellationToken cancellationToken = default)
    {
        dbContext.Assets.Update(asset);
        await SaveChangesAsync(dbContext.Entry(asset), asset.SerialNumber, cancellationToken);
    }

    public async Task DeleteAsync(Asset asset, CancellationToken cancellationToken = default)
    {
        dbContext.Assets.Remove(asset);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> ExistsBySerialNumberAsync(string serialNumber, CancellationToken cancellationToken = default) =>
        dbContext.Assets.AnyAsync(asset => asset.SerialNumber == serialNumber, cancellationToken);

    private async Task SaveChangesAsync(EntityEntry<Asset> entry, string serialNumber, CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            entry.State = EntityState.Detached;
            throw new DuplicateSerialNumberException(serialNumber);
        }
    }
}
