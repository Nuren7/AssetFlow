using AssetFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AssetFlow.Infrastructure.Persistence;

public sealed class AssetFlowDbContext(DbContextOptions<AssetFlowDbContext> options) : DbContext(options)
{
    public DbSet<Asset> Assets => Set<Asset>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AssetFlowDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
