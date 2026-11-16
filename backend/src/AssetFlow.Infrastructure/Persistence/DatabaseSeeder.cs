using AssetFlow.Domain.Entities;
using AssetFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AssetFlow.Infrastructure.Persistence;

public sealed class DatabaseSeeder(AssetFlowDbContext dbContext)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);
        if (await dbContext.Assets.AnyAsync(cancellationToken))
        {
            return;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var seedAssets = new[]
        {
            Seed("Engineering Laptop 01", "DELL-LAT-1001", AssetCategory.Laptop, "Dell", "Latitude 7440", AssetStatus.Assigned, "New York", today, 18),
            Seed("Engineering Laptop 02", "LEN-THK-1002", AssetCategory.Laptop, "Lenovo", "ThinkPad X1 Carbon", AssetStatus.Assigned, "London", today, 7),
            Seed("Design MacBook Pro", "APL-MBP-1003", AssetCategory.Laptop, "Apple", "MacBook Pro 14", AssetStatus.Assigned, "San Francisco", today, 120),
            Seed("Finance Laptop", "HP-ELT-1004", AssetCategory.Laptop, "HP", "EliteBook 840", AssetStatus.Available, "New York", today, 240),
            Seed("Conference Room Display", "SAM-MON-2001", AssetCategory.Monitor, "Samsung", "ViewFinity S8", AssetStatus.Available, "London", today, 30),
            Seed("Design Monitor 01", "DEL-MON-2002", AssetCategory.Monitor, "Dell", "UltraSharp U2723QE", AssetStatus.Assigned, "San Francisco", today, 4),
            Seed("Design Monitor 02", "DEL-MON-2003", AssetCategory.Monitor, "Dell", "UltraSharp U2723QE", AssetStatus.InRepair, "San Francisco", today, -45),
            Seed("Executive Phone", "APL-PHN-3001", AssetCategory.Mobile, "Apple", "iPhone 15", AssetStatus.Assigned, "New York", today, 180),
            Seed("Support Phone", "SAM-PHN-3002", AssetCategory.Mobile, "Samsung", "Galaxy S24", AssetStatus.Available, "London", today, 25),
            Seed("USB-C Dock 01", "LEN-DCK-4001", AssetCategory.Accessory, "Lenovo", "ThinkPad Universal USB-C Dock", AssetStatus.Assigned, "London", today, 90),
            Seed("USB-C Dock 02", "DEL-DCK-4002", AssetCategory.Accessory, "Dell", "WD22TB4", AssetStatus.Available, "New York", today, 365),
            Seed("Wireless Headset", "JAB-HDS-4003", AssetCategory.Accessory, "Jabra", "Evolve2 65", AssetStatus.Assigned, "San Francisco", today, 12),
            Seed("Core Switch", "CIS-SWT-5001", AssetCategory.Network, "Cisco", "Catalyst 9300", AssetStatus.Assigned, "New York", today, 500),
            Seed("Office Router", "CIS-RTR-5002", AssetCategory.Network, "Cisco", "Catalyst 8200", AssetStatus.InRepair, "London", today, 60),
            Seed("Wi-Fi Access Point", "UBQ-AP-5003", AssetCategory.Network, "Ubiquiti", "UniFi U6 Pro", AssetStatus.Available, "San Francisco", today, 300),
            Seed("Legacy Laptop", "LEN-THK-1005", AssetCategory.Laptop, "Lenovo", "ThinkPad T480", AssetStatus.Retired, "Storage", today, -500),
            Seed("Meeting Room Webcam", "LOG-CAM-4004", AssetCategory.Accessory, "Logitech", "Brio 4K", AssetStatus.Available, "London", today, 14),
            Seed("Printer", "HP-PRN-6001", AssetCategory.Other, "HP", "LaserJet Enterprise M507", AssetStatus.Assigned, "New York", today, 210)
        };

        await dbContext.Assets.AddRangeAsync(seedAssets, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static Asset Seed(
        string name,
        string serialNumber,
        AssetCategory category,
        string manufacturer,
        string model,
        AssetStatus status,
        string location,
        DateOnly today,
        int warrantyDays)
    {
        var warrantyExpirationDate = today.AddDays(warrantyDays);
        var purchaseDate = warrantyExpirationDate.AddDays(-365);
        return new Asset(name, serialNumber, category, manufacturer, model, status, location, purchaseDate, warrantyExpirationDate);
    }
}
