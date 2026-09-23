using System.ComponentModel.DataAnnotations;

namespace LocalPress.Core.Entities;

/// <summary>
/// Multi-tenant root. Each independent print shop is a Tenant.
/// </summary>
public class Tenant
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public stringSlug { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Tagline { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    [MaxLength(500)]
    public string? LogoUrl { get; set; }

    [MaxLength(20)]
    public string? PrimaryColor { get; set; } = "#0f766e";

    [MaxLength(20)]
    public string? SecondaryColor { get; set; }

    [MaxLength(300)]
    public string? AddressLine1 { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(50)]
    public string? State { get; set; }

    [MaxLength(20)]
    public string? PostalCode { get; set; }

    [MaxLength(2)]
    public string Country { get; set; } = "US";

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    [MaxLength(100)]
    public string? StripeAccountId { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsOnboarded { get; set; } = false;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    public ICollection<Product> Products { get; set; } = new List<Product>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<Zone> Zones { get; set; } = new List<Zone>();
    public ICollection<InventoryItem> InventoryItems { get; set; } = new List<InventoryItem>();
}
