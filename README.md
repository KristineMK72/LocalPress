# LocalPress — Print & POD Operating System

Multi-tenant SaaS for **independent print shops**: catalog, spatial delivery zones, and a full production order pipeline. Built to compete with Printful on **local production, lower fees, and map-native zones**.

Part of the **Spatialytics** family.

## Quick start (SQLite — zero config)

Requires [.NET 8 SDK](https://dotnet.microsoft.com/download).

```bash
cd LocalPress
dotnet restore LocalPress.sln
dotnet run --project src/LocalPress.Api
```

Open:

- **Swagger**: http://localhost:5080/swagger  
- **Health**: http://localhost:5080/

Demo tenant slug: **`lakes-area-print`**

### Try it

```bash
# List shops
curl http://localhost:5080/api/tenants

# Get demo shop
curl http://localhost:5080/api/tenants/lakes-area-print

# Products (replace TENANT_ID from above)
curl http://localhost:5080/api/tenants/TENANT_ID/products

# Orders
curl http://localhost:5080/api/tenants/TENANT_ID/orders

# Zone match (Brainerd area — should hit "Brainerd Same-Day")
curl -X POST http://localhost:5080/api/tenants/TENANT_ID/zones/match \
  -H "Content-Type: application/json" \
  -d '{"latitude":46.36,"longitude":-94.20}'
```

## Optional: Postgres + PostGIS

```bash
docker compose up -d
```

Set in `src/LocalPress.Api/appsettings.json`:

```json
{
  "DatabaseProvider": "Postgres",
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=localpress;Username=localpress;Password=localpress"
  }
}
```

## Solution layout

```
LocalPress/
├── LocalPress.sln
├── docker-compose.yml
├── docs/architecture.md
└── src/
    ├── LocalPress.Api/           # HTTP API + Swagger
    ├── LocalPress.Core/          # Entities
    └── LocalPress.Infrastructure/# EF Core, seed
```

## Domain

| Entity | Role |
|--------|------|
| **Tenant** | Print shop (multi-tenant root) |
| **Product / Variant** | Catalog + sizes/colors |
| **Order / LineItem / StatusHistory** | Pipeline: Draft → Paid → Production → Shipped/Pickup |
| **Zone** | Polygon delivery area + shipping rules |
| **DesignAsset** | Print files / mockups |
| **InventoryItem** | Blanks & supplies |

## Competitive edge vs Printful

| | Printful | LocalPress |
|--|----------|------------|
| Production | Global network | **Your shop / local** |
| Fees | Higher platform take | **Shop-first** |
| Zones | Limited | **Native map zones** |
| Multi-shop networks | No | **Yes** |
| Spatialytics fit | No | **Yes (GIS + local economy)** |

## Next build steps

1. Auth (shop owner login) + tenant context middleware  
2. Stripe Checkout / Connect  
3. File upload for designs (S3/Azure Blob)  
4. Shop dashboard UI (Next.js or Blazor) on `localpress.spatialytics.space`  
5. PostGIS spatial indexes for large zone sets  

## License

Proprietary — Spatialytics / LocalPress.
