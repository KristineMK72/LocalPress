using System.ComponentModel.DataAnnotations;

namespace LocalPress.Core.Entities;

public class InventoryItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Sku { get; set; }

    public int QuantityOnHand { get; set; }
    public int ReorderLevel { get; set; }
    public decimal UnitCost { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
