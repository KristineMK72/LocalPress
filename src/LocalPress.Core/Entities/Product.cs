using System.ComponentModel.DataAnnotations;

namespace LocalPress.Core.Entities;

public class Product
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Sku { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    public decimal BaseCost { get; set; }
    public decimal BasePrice { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsPrintOnDemand { get; set; } = true;

    [MaxLength(100)]
    public string? Category { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
    public ICollection<DesignAsset> DesignAssets { get; set; } = new List<DesignAsset>();
}

public class ProductVariant
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Size { get; set; }

    [MaxLength(50)]
    public string? Color { get; set; }

    public decimal? PriceOverride { get; set; }
    public decimal? CostOverride { get; set; }

    public int StockQuantity { get; set; } = 0;
    public bool IsActive { get; set; } = true;
}
