using AssetFlow.Domain.Enums;

namespace AssetFlow.Domain.Entities;

public sealed class Asset
{
    private Asset()
    {
    }

    public Asset(
        string name,
        string serialNumber,
        AssetCategory category,
        string manufacturer,
        string model,
        AssetStatus status,
        string location,
        DateOnly purchaseDate,
        DateOnly warrantyExpirationDate)
    {
        Id = Guid.NewGuid();
        SetDetails(name, serialNumber, category, manufacturer, model, status, location, purchaseDate, warrantyExpirationDate);
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string SerialNumber { get; private set; } = string.Empty;
    public AssetCategory Category { get; private set; }
    public string Manufacturer { get; private set; } = string.Empty;
    public string Model { get; private set; } = string.Empty;
    public AssetStatus Status { get; private set; }
    public string Location { get; private set; } = string.Empty;
    public DateOnly PurchaseDate { get; private set; }
    public DateOnly WarrantyExpirationDate { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    public void Update(
        string name,
        string serialNumber,
        AssetCategory category,
        string manufacturer,
        string model,
        AssetStatus status,
        string location,
        DateOnly purchaseDate,
        DateOnly warrantyExpirationDate)
    {
        SetDetails(name, serialNumber, category, manufacturer, model, status, location, purchaseDate, warrantyExpirationDate);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private void SetDetails(
        string name,
        string serialNumber,
        AssetCategory category,
        string manufacturer,
        string model,
        AssetStatus status,
        string location,
        DateOnly purchaseDate,
        DateOnly warrantyExpirationDate)
    {
        Name = name;
        SerialNumber = serialNumber;
        Category = category;
        Manufacturer = manufacturer;
        Model = model;
        Status = status;
        Location = location;
        PurchaseDate = purchaseDate;
        WarrantyExpirationDate = warrantyExpirationDate;
    }
}
