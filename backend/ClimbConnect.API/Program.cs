using Microsoft.EntityFrameworkCore;
using ClimbConnect.API.Models;
using ClimbConnect.API.Dtos;
using Route = ClimbConnect.API.Models.Route;

var builder = WebApplication.CreateBuilder(args);

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// EF Core InMemory
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseInMemoryDatabase("ClimbConnectDb"));

var app = builder.Build();

// Swagger UI
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();


// --------------------
// HEALTH
// --------------------
app.MapGet("/api/health", () =>
    Results.Ok(new { status = "ok" }))
   .WithName("Health");


// --------------------
// WEATHER (Demo)
// --------------------
app.MapGet("/weatherforecast", () =>
{
    var summaries = new[]
    {
        "Freezing","Bracing","Chilly","Cool","Mild",
        "Warm","Balmy","Hot","Sweltering","Scorching"
    };

    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast(
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        )
    ).ToArray();

    return forecast;
})
.WithName("GetWeatherForecast");


// --------------------
// PINGS (DB-Test)
// --------------------
app.MapPost("/api/pings", async (AppDbContext db) =>
{
    var ping = new Ping();

    db.Pings.Add(ping);
    await db.SaveChangesAsync();

    return Results.Created($"/api/pings/{ping.Id}", ping);
})
.WithName("CreatePing")
.WithTags("Pings");


// --------------------
// AREAS (ISSUE #7)
// --------------------

// Alle Areas
app.MapGet("/api/areas", async (AppDbContext db) =>
{
    var areas = await db.Areas
        .OrderBy(a => a.Name)
        .ToListAsync();

    return Results.Ok(areas);
})
.WithName("GetAreas")
.WithTags("Areas");


// Area nach ID
app.MapGet("/api/areas/{id:int}", async (int id, AppDbContext db) =>
{
    var area = await db.Areas.FindAsync(id);

    return area is null
        ? Results.NotFound()
        : Results.Ok(area);
})
.WithName("GetAreaById")
.WithTags("Areas");


// Area anlegen
app.MapPost("/api/areas", async (AreaCreateDto dto, AppDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(dto.Name))
        return Results.BadRequest(new { error = "Name is required" });

    var area = new Area
    {
        Name = dto.Name.Trim(),
        Location = string.IsNullOrWhiteSpace(dto.Location)
            ? null
            : dto.Location.Trim()
    };

    db.Areas.Add(area);
    await db.SaveChangesAsync();

    return Results.Created($"/api/areas/{area.Id}", area);
})
.WithName("CreateArea")
.WithTags("Areas");


// --------------------
// ROUTES (ISSUE #8)
// --------------------

// Route anlegen
app.MapPost("/api/routes", async (RouteCreateDto dto, AppDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(dto.Name))
        return Results.BadRequest(new { error = "Name is required" });

    var route = new Route
    {
        Name = dto.Name.Trim(),
        Grade = string.IsNullOrWhiteSpace(dto.Grade) ? null : dto.Grade.Trim(),
        Sector = string.IsNullOrWhiteSpace(dto.Sector) ? null : dto.Sector.Trim()
    };

    db.Routes.Add(route);
    await db.SaveChangesAsync();

    return Results.Created($"/api/routes/{route.Id}", route);
})
.WithName("CreateRoute")
.WithTags("Routes");


// Route Detail
app.MapGet("/api/routes/{id:int}", async (int id, AppDbContext db) =>
{
    var route = await db.Routes.FindAsync(id);

    return route is null
        ? Results.NotFound()
        : Results.Ok(route);
})
.WithName("GetRouteById")
.WithTags("Routes");


app.Run();


// --------------------
// DB CONTEXT
// --------------------
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public DbSet<Ping> Pings => Set<Ping>();
    public DbSet<Area> Areas => Set<Area>();
    public DbSet<Route> Routes => Set<Route>();
}


// --------------------
// RECORDS
// --------------------
public record WeatherForecast(
    DateOnly Date,
    int TemperatureC,
    string? Summary)
{
    public int TemperatureF =>
        32 + (int)(TemperatureC / 0.5556);
}
