using System.ComponentModel.DataAnnotations;

namespace LocalPress.Core.Entities;

public class DesignAsset
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public Guid? ProductId { get; set; }
    public Product? Product { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? StorageUrl { get; set; }

    [MaxLength(100)]
    public string? ContentType { get; set; }

    public long? FileSizeBytes { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
