using System.ComponentModel.DataAnnotations;
using NetTopologySuite.Geometries;

namespace LocalPress.Core.Entities;

public class Zone
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public Polygon? Boundary { get; set; }

    public decimal BaseShippingPrice { get; set; }
    public decimal? FreeShippingMinimum { get; set; }
    public int EstimatedDaysMin { get; set; } = 1;
    public int EstimatedDaysMax { get; set; } = 5;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
