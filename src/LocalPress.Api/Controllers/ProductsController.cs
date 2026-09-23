using LocalPress.Core.Entities;
using LocalPress.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LocalPress.Api.Controllers;

[ApiController]
[Route("api/tenants/{tenantId:guid}/products")]
public class ProductsController : ControllerBase
{
    private readonly LocalPressDbContext _db;
    public ProductsController(LocalPressDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> List(Guid tenantId, CancellationToken ct)
    {
        var products = await _db.Products.AsNoTracking()
            .Where(p => p.TenantId == tenantId && p.IsActive)
            .Include(p => p.Variants)
            .OrderBy(p => p.Name)
            .Select(p => new
            {
                p.Id, p.Name, p.Sku, p.Description, p.BasePrice, p.BaseCost,
                p.Category, p.IsPrintOnDemand,
                Variants = p.Variants.Where(v => v.IsActive).Select(v => new
                {
                    v.Id, v.Name, v.Size, v.Color, v.PriceOverride, v.StockQuantity
                })
            })
            .ToListAsync(ct);
        return Ok(products);
    }

    public record CreateProductRequest(
        string Name, string? Sku, string? Description,
        decimal BasePrice, decimal BaseCost, string? Category, bool IsPrintOnDemand = true);

    [HttpPost]
    public async Task<ActionResult<object>> Create(Guid tenantId, [FromBody] CreateProductRequest req, CancellationToken ct)
    {
        if (!await _db.Tenants.AnyAsync(t => t.Id == tenantId, ct))
            return NotFound("Tenant not found.");

        var product = new Product
        {
            TenantId = tenantId,
            Name = req.Name.Trim(),
            Sku = req.Sku,
            Description = req.Description,
            BasePrice = req.BasePrice,
            BaseCost = req.BaseCost,
            Category = req.Category,
            IsPrintOnDemand = req.IsPrintOnDemand
        };
        _db.Products.Add(product);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(List), new { tenantId }, new { product.Id, product.Name, product.BasePrice });
    }
}
