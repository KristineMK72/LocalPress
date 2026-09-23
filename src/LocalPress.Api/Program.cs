using LocalPress.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        o.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "LocalPress API",
        Version = "v1",
        Description = "Print & POD OS for independent shops — Spatialytics family"
    });
});

var provider = builder.Configuration["DatabaseProvider"] ?? "Sqlite";
var conn = builder.Configuration.GetConnectionString("DefaultConnection")
           ?? "Data Source=localpress.db";

builder.Services.AddDbContext<LocalPressDbContext>(options =>
{
    if (provider.Equals("Postgres", StringComparison.OrdinalIgnoreCase) ||
        conn.Contains("Host=", StringComparison.OrdinalIgnoreCase))
    {
        options.UseNpgsql(conn, o => o.UseNetTopologySuite());
    }
    else
    {
        options.UseSqlite(conn);
    }
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

await SeedData.InitializeAsync(app.Services);

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "LocalPress v1"));

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseCors();
app.MapControllers();

app.MapGet("/api/health", () => Results.Ok(new
{
    name = "LocalPress API",
    status = "running",
    swagger = "/swagger",
    demoTenant = "lakes-area-print"
}));

app.Run();

public partial class Program { }
