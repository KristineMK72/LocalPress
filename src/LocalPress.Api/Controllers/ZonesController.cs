using LocalPress.Core.Entities;
using LocalPress.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace LocalPress.Api.Controllers;

[ApiController]
[Route("api/tenants/{tenantId:guid}/zones")]
public class ZonesController : ControllerBase
{
    private readonly LocalPressDbContext _db;
    private static readonly GeoJsonReader GeoJsonReader = new();

    public ZonesController(LocalPressDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetZones(Guid tenantId, CancellationToken ct)
    {
        var zones = await _db.Zones.AsNoTracking()
            .Where(z => z.TenantId == tenantId && z.IsActive)
            .OrderBy(z => z.SortOrder)
            .Select(z => new
            {
                z.Id, z.Name, z.Description,
                z.BaseShippingPrice, z.FreeShippingMinimum,
                z.EstimatedDaysMin, z.EstimatedDaysMax, z.SortOrder,
                HasBoundary = z.Boundary != null
            })
            .ToListAsync(ct);
        return Ok(zones);
    }

    public record CreateZoneRequest(
        string Name, string? Description, string? BoundaryGeoJson,
        decimal BaseShippingPrice, decimal? FreeShippingMinimum,
        int EstimatedDaysMin = 1, int EstimatedDaysMax = 5, int SortOrder = 0);

    [HttpPost]
    public async Task<ActionResult<object>> Create(Guid tenantId, [FromBody] CreateZoneRequest request, CancellationToken ct)
    {
        if (!await _db.Tenants.AnyAsync(t => t.Id == tenantId, ct))
            return NotFound("Tenant not found.");

        Polygon? boundary = null;
        if (!string.IsNullOrWhiteSpace(request.BoundaryGeoJson))
        {
            try
            {
                var geometry = GeoJsonReader.Read<Geometry>(request.BoundaryGeoJson);
                boundary = geometry as Polygon ?? (geometry as MultiPolygon)?.Geometries.OfType<Polygon>().FirstOrDefault();
                if (boundary != null) boundary.SRID = 4326;
            }
            catch
            {
                return BadRequest("Invalid GeoJSON Polygon.");
            }
        }

        var zone = new Zone
        {
            TenantId = tenantId,
            Name = request.Name.Trim(),
            Description = request.Description,
            Boundary = boundary,
            BaseShippingPrice = request.BaseShippingPrice,
            FreeShippingMinimum = request.FreeShippingMinimum,
            EstimatedDaysMin = request.EstimatedDaysMin,
            EstimatedDaysMax = request.EstimatedDaysMax,
            SortOrder = request.SortOrder
        };
        _db.Zones.Add(zone);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetZones), new { tenantId }, new { zone.Id, zone.Name });
    }

    public record PointMatchRequest(double Latitude, double Longitude);

    [HttpPost("match")]
    public async Task<ActionResult<object?>> MatchPoint(Guid tenantId, [FromBody] PointMatchRequest request, CancellationToken ct)
    {
        var point = new Point(request.Longitude, request.Latitude) { SRID = 4326 };
        var zones = await _db.Zones
            .Where(z => z.TenantId == tenantId && z.IsActive)
            .OrderBy(z => z.SortOrder)
            .ToListAsync(ct);

        var match = zones.FirstOrDefault(z => z.Boundary != null && z.Boundary.Contains(point));
        if (match is null) return Ok(null);

        return Ok(new
        {
            match.Id, match.Name, match.BaseShippingPrice, match.FreeShippingMinimum,
            match.EstimatedDaysMin, match.EstimatedDaysMax
        });
    }
}
