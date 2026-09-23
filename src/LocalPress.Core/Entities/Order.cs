using System.ComponentModel.DataAnnotations;

namespace LocalPress.Core.Entities;

public enum OrderStatus
{
    Draft = 0,
    PendingPayment = 1,
    Paid = 2,
    InProduction = 3,
    Ready = 4,
    Shipped = 5,
    PickedUp = 6,
    Completed = 7,
    Cancelled = 8,
    Refunded = 9
}

public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    [MaxLength(50)]
    public string OrderNumber { get; set; } = string.Empty;

    public OrderStatus Status { get; set; } = OrderStatus.Draft;

    [MaxLength(200)]
    public string CustomerName { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? CustomerEmail { get; set; }

    [MaxLength(50)]
    public string? CustomerPhone { get; set; }

    [MaxLength(300)]
    public string? ShipAddressLine1 { get; set; }

    [MaxLength(100)]
    public string? ShipCity { get; set; }

    [MaxLength(50)]
    public string? ShipState { get; set; }

    [MaxLength(20)]
    public string? ShipPostalCode { get; set; }

    public double? ShipLatitude { get; set; }
    public double? ShipLongitude { get; set; }

    public Guid? MatchedZoneId { get; set; }
    public Zone? MatchedZone { get; set; }

    public decimal Subtotal { get; set; }
    public decimal ShippingTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal GrandTotal { get; set; }

    [MaxLength(100)]
    public string? StripePaymentIntentId { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    public ICollection<OrderLineItem> LineItems { get; set; } = new List<OrderLineItem>();
    public ICollection<OrderStatusHistory> StatusHistory { get; set; } = new List<OrderStatusHistory>();
}

public class OrderLineItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public Guid? ProductId { get; set; }
    public Guid? ProductVariantId { get; set; }

    [MaxLength(200)]
    public string ProductName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? VariantName { get; set; }

    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

public class OrderStatusHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public OrderStatus FromStatus { get; set; }
    public OrderStatus ToStatus { get; set; }
    [MaxLength(500)]
    public string? Note { get; set; }
    public DateTimeOffset ChangedAt { get; set; } = DateTimeOffset.UtcNow;
}
