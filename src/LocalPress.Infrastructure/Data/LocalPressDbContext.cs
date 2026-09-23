using LocalPress.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace LocalPress.Infrastructure.Data;

public class LocalPressDbContext : DbContext
{
    public LocalPressDbContext(DbContextOptions<LocalPressDbContext> options) : base(options) { }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLineItem> OrderLineItems => Set<OrderLineItem>();
    public DbSet<OrderStatusHistory> OrderStatusHistories => Set<OrderStatusHistory>();
    public DbSet<Zone> Zones => Set<Zone>();
    public DbSet<DesignAsset> DesignAssets => Set<DesignAsset>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var wktReader = new WKTReader();
        var polygonConverter = new ValueConverter<Polygon?, string?>(
            v => v == null ? null : v.AsText(),
            v => string.IsNullOrWhiteSpace(v) ? null : (Polygon)wktReader.Read(v)
        );

        modelBuilder.Entity<Tenant>(e =>
        {
            e.HasIndex(t => t.Slug).IsUnique();
            e.Property(t => t.Name).IsRequired();
        });

        modelBuilder.Entity<Product>(e =>
        {
            e.HasOne(p => p.Tenant).WithMany(t => t.Products).HasForeignKey(p => p.TenantId).OnDelete(DeleteBehavior.Cascade);
            e.Property(p => p.BaseCost).HasPrecision(18, 2);
            e.Property(p => p.BasePrice).HasPrecision(18, 2);
            e.HasIndex(p => new { p.TenantId, p.Sku });
        });

        modelBuilder.Entity<ProductVariant>(e =>
        {
            e.HasOne(v => v.Product).WithMany(p => p.Variants).HasForeignKey(v => v.ProductId).OnDelete(DeleteBehavior.Cascade);
            e.Property(v => v.PriceOverride).HasPrecision(18, 2);
            e.Property(v => v.CostOverride).HasPrecision(18, 2);
        });

        modelBuilder.Entity<Order>(e =>
        {
            e.HasOne(o => o.Tenant).WithMany(t => t.Orders).HasForeignKey(o => o.TenantId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(o => o.MatchedZone).WithMany().HasForeignKey(o => o.MatchedZoneId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(o => new { o.TenantId, o.OrderNumber }).IsUnique();
            e.HasIndex(o => new { o.TenantId, o.Status });
            e.Property(o => o.Subtotal).HasPrecision(18, 2);
            e.Property(o => o.ShippingTotal).HasPrecision(18, 2);
            e.Property(o => o.TaxTotal).HasPrecision(18, 2);
            e.Property(o => o.GrandTotal).HasPrecision(18, 2);
        });

        modelBuilder.Entity<OrderLineItem>(e =>
        {
            e.HasOne(li => li.Order).WithMany(o => o.LineItems).HasForeignKey(li => li.OrderId).OnDelete(DeleteBehavior.Cascade);
            e.Property(li => li.UnitPrice).HasPrecision(18, 2);
            e.Property(li => li.LineTotal).HasPrecision(18, 2);
        });

        modelBuilder.Entity<OrderStatusHistory>(e =>
        {
            e.HasOne(h => h.Order).WithMany(o => o.StatusHistory).HasForeignKey(h => h.OrderId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Zone>(e =>
        {
            e.HasOne(z => z.Tenant).WithMany(t => t.Zones).HasForeignKey(z => z.TenantId).OnDelete(DeleteBehavior.Cascade);
            e.Property(z => z.BaseShippingPrice).HasPrecision(18, 2);
            e.Property(z => z.FreeShippingMinimum).HasPrecision(18, 2);
            e.Property(z => z.Boundary).HasConversion(polygonConverter);
        });

        modelBuilder.Entity<DesignAsset>(e =>
        {
            e.HasOne(d => d.Tenant).WithMany().HasForeignKey(d => d.TenantId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(d => d.Product).WithMany(p => p.DesignAssets).HasForeignKey(d => d.ProductId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<InventoryItem>(e =>
        {
            e.HasOne(i => i.Tenant).WithMany(t => t.InventoryItems).HasForeignKey(i => i.TenantId).OnDelete(DeleteBehavior.Cascade);
            e.Property(i => i.UnitCost).HasPrecision(18, 2);
        });
    }
}
