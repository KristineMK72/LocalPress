using LocalPress.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace LocalPress.Infrastructure.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LocalPressDbContext>();
        await db.Database.EnsureCreatedAsync();

        if (await db.Tenants.AnyAsync()) return;

        var gf = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

        var brainerdRing = gf.CreateLinearRing(new[]
        {
            new Coordinate(-94.30, 46.30),
            new Coordinate(-94.10, 46.30),
            new Coordinate(-94.10, 46.45),
            new Coordinate(-94.30, 46.45),
            new Coordinate(-94.30, 46.30)
        });
        var brainerdPoly = gf.CreatePolygon(brainerdRing);

        var tenant = new Tenant
        {
            Name = "Lakes Area Print Co.",
            Slug = "lakes-area-print",
            Tagline = "Local print. Local jobs. Same-day in the lakes area.",
            Description = "Independent print shop serving Greater Minnesota — apparel, signs, and short-run POD.",
            City = "Brainerd",
            State = "MN",
            PostalCode = "56401",
            Country = "US",
            Latitude = 46.3580,
            Longitude = -94.2008,
            PrimaryColor = "#0f766e",
            IsActive = true,
            IsOnboarded = true
        };

        var tee = new Product
        {
            Tenant = tenant,
            Name = "LocalPress Classic Tee",
            Sku = "LP-TEE-001",
            Description = "Soft cotton tee, printed in-house. Perfect for events and local brands.",
            BaseCost = 8.50m,
            BasePrice = 24.00m,
            Category = "Apparel",
            IsPrintOnDemand = true,
            Variants =
            {
                new ProductVariant { Name = "M / Black", Size = "M", Color = "Black", StockQuantity = 50 },
                new ProductVariant { Name = "L / Black", Size = "L", Color = "Black", StockQuantity = 40 },
                new ProductVariant { Name = "M / White", Size = "M", Color = "White", StockQuantity = 35 }
            }
        };

        var mug = new Product
        {
            Tenant = tenant,
            Name = "Lakes Ceramic Mug",
            Sku = "LP-MUG-001",
            Description = "11oz ceramic mug — dishwasher safe, local print.",
            BaseCost = 4.00m,
            BasePrice = 16.00m,
            Category = "Drinkware",
            IsPrintOnDemand = true
        };

        var zone = new Zone
        {
            Tenant = tenant,
            Name = "Brainerd Same-Day",
            Description = "Free shipping over $50 · 0–1 day",
            Boundary = brainerdPoly,
            BaseShippingPrice = 5.00m,
            FreeShippingMinimum = 50.00m,
            EstimatedDaysMin = 0,
            EstimatedDaysMax = 1,
            SortOrder = 1,
            IsActive = true
        };

        db.Tenants.Add(tenant);
        db.Products.AddRange(tee, mug);
        db.Zones.Add(zone);
        await db.SaveChangesAsync();

        var order = new Order
        {
            TenantId = tenant.Id,
            OrderNumber = "LP-1001",
            Status = OrderStatus.Paid,
            CustomerName = "Alex Johnson",
            CustomerEmail = "alex@example.com",
            ShipCity = "Brainerd",
            ShipState = "MN",
            ShipPostalCode = "56401",
            ShipLatitude = 46.36,
            ShipLongitude = -94.20,
            MatchedZoneId = zone.Id,
            Subtotal = 24.00m,
            ShippingTotal = 0m,
            TaxTotal = 1.75m,
            GrandTotal = 25.75m,
            PaidAt = DateTimeOffset.UtcNow.AddHours(-2),
            LineItems =
            {
                new OrderLineItem
                {
                    ProductId = tee.Id,
                    ProductName = tee.Name,
                    VariantName = "M / Black",
                    Quantity = 1,
                    UnitPrice = 24.00m,
                    LineTotal = 24.00m
                }
            },
            StatusHistory =
            {
                new OrderStatusHistory { FromStatus = OrderStatus.Draft, ToStatus = OrderStatus.PendingPayment, Note = "Checkout started" },
                new OrderStatusHistory { FromStatus = OrderStatus.PendingPayment, ToStatus = OrderStatus.Paid, Note = "Payment received" }
            }
        };

        db.Orders.Add(order);
        await db.SaveChangesAsync();
    }
}
