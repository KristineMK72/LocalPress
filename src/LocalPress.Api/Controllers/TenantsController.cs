using LocalPress.Core.Entities;
using LocalPress.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LocalPress.Api.Controllers;

[ApiController]
[Route("api/tenants")]
public class TenantsController : ControllerBase
{
    private readonly LocalPressDbContext _db;
    public TenantsController(LocalPressDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> List(CancellationToken ct)
    {
        var items = await _db.Tenants.AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .Select(t => new
            {
                t.Id, t.Name, t.Slug, t.Tagline, t.City, t.State,
                t.PrimaryColor, t.IsOnboarded, t.CreatedAt
            })
            .ToListAsync(ct);
        return Ok(items);
    }

    [HttpGet("{slug}")]
    public async Task<ActionResult<object>> GetBySlug(string slug, CancellationToken ct)
    {
        var t = await _db.Tenants.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Slug == slug, ct);
        if (t is null) return NotFound();
        return Ok(new
        {
            t.Id, t.Name, t.Slug, t.Tagline, t.Description,
            t.LogoUrl, t.PrimaryColor, t.SecondaryColor,
            t.AddressLine1, t.City, t.State, t.PostalCode, t.Country,
            t.Latitude, t.Longitude, t.IsOnboarded, t.CreatedAt
        });
    }

    public record CreateTenantRequest(string Name, string Slug, string? Tagline, string? City, string? State);

    [HttpPost]
    public async Task<ActionResult<object>> Create([FromBody] CreateTenantRequest req, CancellationToken ct)
    {
        var slug = req.Slug.Trim().ToLowerInvariant();
        if (await _db.Tenants.AnyAsync(t => t.Slug == slug, ct))
            return Conflict("Slug already in use.");

        var tenant = new Tenant
        {
            Name = req.Name.Trim(),
           Slug = slug,
            Tagline = req.Tagline,
            City = req.City,
            State = req.State
        };
        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetBySlug), new { slug = tenant.Slug }, new { tenant.Id, tenant.Name, tenant.Slug });
    }
}
