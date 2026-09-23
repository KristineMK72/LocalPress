using LocalPress.Core.Entities;
using LocalPress.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace LocalPress.Api.Controllers;

[ApiController]
[Route("api/tenants/{tenantId:guid}/orders")]
public class OrdersController : ControllerBase
{
    private readonly LocalPressDbContext _db;
    public OrdersController(LocalPressDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult> List(Guid tenantId, [FromQuery] OrderStatus? status, CancellationToken ct)
    {
        var q = _db.Orders.AsNoTracking().Where(o => o.TenantId == tenantId);
        if (status.HasValue) q = q.Where(o => o.Status == status.Value);

        var rows = await q
            .OrderByDescending(o => o.CreatedAt)
            .Take(100)
            .ToListAsync(ct);

        var orders = rows.Select(o => new
        {
            o.Id,
            o.OrderNumber,
            Status = o.Status.ToString(),
            o.CustomerName,
            o.CustomerEmail,
            o.ShipCity,
            o.ShipState,
            o.Subtotal,
            o.ShippingTotal,
            o.GrandTotal,
            o.MatchedZoneId,
            o.CreatedAt,
            o.PaidAt
        }).ToList();

        return Ok(orders);
    }

    [HttpGet("{orderId:guid}")]
    public async Task<ActionResult> Get(Guid tenantId, Guid orderId, CancellationToken ct)
    {
        var o = await _db.Orders.AsNoTracking()
            .Include(x => x.LineItems)
            .Include(x => x.StatusHistory)
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == orderId, ct);
        if (o is null) return NotFound();
        return Ok(new
        {
            o.Id,
            o.OrderNumber,
            Status = o.Status.ToString(),
            o.CustomerName,
            o.CustomerEmail,
            o.CustomerPhone,
            o.ShipAddressLine1,
            o.ShipCity,
            o.ShipState,
            o.ShipPostalCode,
            o.ShipLatitude,
            o.ShipLongitude,
            o.MatchedZoneId,
            o.Subtotal,
            o.ShippingTotal,
            o.TaxTotal,
            o.GrandTotal,
            o.CreatedAt,
            o.PaidAt,
            o.CompletedAt,
            LineItems = o.LineItems.Select(li => new
            {
                li.Id, li.ProductName, li.VariantName, li.Quantity, li.UnitPrice, li.LineTotal
            }),
            StatusHistory = o.StatusHistory.OrderBy(h => h.ChangedAt).Select(h => new
            {
                From = h.FromStatus.ToString(),
                To = h.ToStatus.ToString(),
                h.Note,
                h.ChangedAt
            })
        });
    }

    public record CreateLine(Guid? ProductId, string ProductName, string? VariantName, int Quantity, decimal UnitPrice);
    public record CreateOrderRequest(
        string CustomerName, string? CustomerEmail, string? CustomerPhone,
        string? ShipAddressLine1, string? ShipCity, string? ShipState, string? ShipPostalCode,
        double? ShipLatitude, double? ShipLongitude,
        List<CreateLine> Lines);

    [HttpPost]
    public async Task<ActionResult> Create(Guid tenantId, [FromBody] CreateOrderRequest req, CancellationToken ct)
    {
        if (!await _db.Tenants.AnyAsync(t => t.Id == tenantId, ct))
            return NotFound("Tenant not found.");
        if (req.Lines is null || req.Lines.Count == 0)
            return BadRequest("At least one line item required.");

        Guid? zoneId = null;
        decimal shipping = 8.00m;
        if (req.ShipLatitude.HasValue && req.ShipLongitude.HasValue)
        {
            var point = new Point(req.ShipLongitude.Value, req.ShipLatitude.Value) { SRID = 4326 };
            var zones = await _db.Zones.Where(z => z.TenantId == tenantId && z.IsActive)
                .OrderBy(z => z.SortOrder).ToListAsync(ct);
            foreach (var z in zones)
            {
                if (z.Boundary != null && z.Boundary.Contains(point))
                {
                    zoneId = z.Id;
                    shipping = z.BaseShippingPrice;
                    break;
                }
            }
        }

        var subtotal = req.Lines.Sum(l => l.UnitPrice * l.Quantity);
        if (zoneId.HasValue)
        {
            var z = await _db.Zones.FindAsync([zoneId.Value], ct);
            if (z?.FreeShippingMinimum != null && subtotal >= z.FreeShippingMinimum)
                shipping = 0;
        }

        var count = await _db.Orders.CountAsync(o => o.TenantId == tenantId, ct);
        var order = new Order
        {
            TenantId = tenantId,
            OrderNumber = $"LP-{1000 + count + 1}",
            Status = OrderStatus.PendingPayment,
            CustomerName = req.CustomerName,
            CustomerEmail = req.CustomerEmail,
            CustomerPhone = req.CustomerPhone,
            ShipAddressLine1 = req.ShipAddressLine1,
            ShipCity = req.ShipCity,
            ShipState = req.ShipState,
            ShipPostalCode = req.ShipPostalCode,
            ShipLatitude = req.ShipLatitude,
            ShipLongitude = req.ShipLongitude,
            MatchedZoneId = zoneId,
            Subtotal = subtotal,
            ShippingTotal = shipping,
            TaxTotal = Math.Round(subtotal * 0.06875m, 2),
            LineItems = req.Lines.Select(l => new OrderLineItem
            {
                ProductId = l.ProductId,
                ProductName = l.ProductName,
                VariantName = l.VariantName,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                LineTotal = l.UnitPrice * l.Quantity
            }).ToList(),
            StatusHistory =
            {
                new OrderStatusHistory
                {
                    FromStatus = OrderStatus.Draft,
                    ToStatus = OrderStatus.PendingPayment,
                    Note = "Order created"
                }
            }
        };
        order.GrandTotal = order.Subtotal + order.ShippingTotal + order.TaxTotal;

        _db.Orders.Add(order);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { tenantId, orderId = order.Id }, new
        {
            order.Id,
            order.OrderNumber,
            Status = order.Status.ToString(),
            order.GrandTotal,
            order.MatchedZoneId
        });
    }

    public record StatusChangeRequest(OrderStatus Status, string? Note);

    [HttpPost("{orderId:guid}/status")]
    public async Task<ActionResult> ChangeStatus(Guid tenantId, Guid orderId, [FromBody] StatusChangeRequest req, CancellationToken ct)
    {
        var order = await _db.Orders.Include(o => o.StatusHistory)
            .FirstOrDefaultAsync(o => o.TenantId == tenantId && o.Id == orderId, ct);
        if (order is null) return NotFound();

        var from = order.Status;
        order.Status = req.Status;
        order.UpdatedAt = DateTimeOffset.UtcNow;
        if (req.Status == OrderStatus.Paid) order.PaidAt = DateTimeOffset.UtcNow;
        if (req.Status is OrderStatus.Completed or OrderStatus.PickedUp) order.CompletedAt = DateTimeOffset.UtcNow;

        order.StatusHistory.Add(new OrderStatusHistory
        {
            FromStatus = from,
            ToStatus = req.Status,
            Note = req.Note
        });

        await _db.SaveChangesAsync(ct);
        return Ok(new { order.Id, order.OrderNumber, From = from.ToString(), To = order.Status.ToString() });
    }
}
