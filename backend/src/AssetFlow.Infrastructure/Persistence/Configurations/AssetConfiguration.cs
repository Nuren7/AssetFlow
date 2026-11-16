using AssetFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetFlow.Infrastructure.Persistence.Configurations;

public sealed class AssetConfiguration : IEntityTypeConfiguration<Asset>
{
    public void Configure(EntityTypeBuilder<Asset> builder)
    {
        builder.ToTable("assets");
        builder.HasKey(asset => asset.Id);
        builder.Property(asset => asset.Id).ValueGeneratedNever();
        builder.Property(asset => asset.Name).HasMaxLength(200).IsRequired();
        builder.Property(asset => asset.SerialNumber).HasMaxLength(120).IsRequired();
        builder.HasIndex(asset => asset.SerialNumber).IsUnique();
        builder.Property(asset => asset.Category).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(asset => asset.Manufacturer).HasMaxLength(120).IsRequired();
        builder.Property(asset => asset.Model).HasMaxLength(120).IsRequired();
        builder.Property(asset => asset.Status).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(asset => asset.Location).HasMaxLength(200).IsRequired();
        builder.Property(asset => asset.PurchaseDate).IsRequired();
        builder.Property(asset => asset.WarrantyExpirationDate).IsRequired();
        builder.Property(asset => asset.CreatedAtUtc).IsRequired();
    }
}
