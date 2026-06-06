using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.OpenApi.Models;
using ClimbConnect.API.Models;
using ClimbConnect.API.Dtos;
using Keycloak.AuthServices.Authentication;
using Keycloak.AuthServices.Authorization;
using Keycloak.AuthServices.Common;
using Route = ClimbConnect.API.Models.Route;

var builder = WebApplication.CreateBuilder(args);

// --------------------
// SWAGGER
// --------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    var kc = builder.Configuration.GetKeycloakOptions<KeycloakAuthenticationOptions>()!;

    c.AddSecurityDefinition("oidc", new OpenApiSecurityScheme
    {
        Name             = "oauth2",
        Type             = SecuritySchemeType.OpenIdConnect,
        OpenIdConnectUrl = new Uri(kc.OpenIdConnectUrl!)
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "oidc" }
            },
            Array.Empty<string>()
        }
    });

    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ClimbConnect API", Version = "v1" });
});

// --------------------
// CORS
// --------------------
builder.Services.AddCors();

// --------------------
// KEYCLOAK AUTH
// --------------------
builder.Services.AddKeycloakWebApiAuthentication(builder.Configuration);
builder.Services
    .AddAuthorization()
    .AddKeycloakAuthorization(builder.Configuration)
    .AddAuthorizationBuilder()
    .AddPolicy("Admin", b => b.RequireResourceRoles("admin"))
    .AddPolicy("User",  b => b.RequireResourceRoles("user"));

// --------------------
// SQLITE
// --------------------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
var sqliteBuilder    = new SqliteConnectionStringBuilder(connectionString);
sqliteBuilder.DataSource = Path.GetFullPath(
    Path.Combine(
        AppDomain.CurrentDomain.GetData("DataDirectory") as string
            ?? AppDomain.CurrentDomain.BaseDirectory,
        sqliteBuilder.DataSource
    ));
connectionString = sqliteBuilder.ToString();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

var app = builder.Build();

// --------------------
// SWAGGER UI
// --------------------
app.UseSwagger(options =>
{
    options.PreSerializeFilters.Add((doc, request) =>
    {
        if (request.Host.Value!.Contains(".cloud.htl-leonding.ac.at"))
        {
            doc.Servers =
            [
                new OpenApiServer { Url = $"{request.Scheme}s://{request.Host.Value}/climbconnectapi" }
            ];
        }
    });
});
app.UseSwaggerUI(options => options.OAuthClientId("climbconnect-ui"));

app.UseHttpsRedirection();
app.UseCors(b => b.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());
app.UseAuthentication();
app.UseAuthorization();

// DB-Schema anlegen (ohne Migrations)
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
}


// --------------------
// HEALTH
// --------------------
app.MapGet("/api/health", () =>
    Results.Ok(new { status = "ok" }))
   .WithName("Health");


// --------------------
// AREAS
// --------------------
app.MapGet("/api/areas", async (AppDbContext db) =>
    Results.Ok(await db.Areas.OrderBy(a => a.Name).ToListAsync()))
   .WithName("GetAreas")
   .WithTags("Areas");

app.MapGet("/api/areas/{id:int}", async (int id, AppDbContext db) =>
{
    var area = await db.Areas.FindAsync(id);
    return area is null ? Results.NotFound() : Results.Ok(area);
})
.WithName("GetAreaById")
.WithTags("Areas");

app.MapPost("/api/areas", async (AreaCreateDto dto, AppDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(dto.Name))
        return Results.BadRequest(new { error = "Name is required" });

    var area = new Area
    {
        Name     = dto.Name.Trim(),
        Location = string.IsNullOrWhiteSpace(dto.Location) ? null : dto.Location.Trim()
    };

    db.Areas.Add(area);
    await db.SaveChangesAsync();
    return Results.Created($"/api/areas/{area.Id}", area);
})
.WithName("CreateArea")
.WithTags("Areas");


// --------------------
// ROUTES
// --------------------
app.MapPost("/api/routes", async (RouteCreateDto dto, AppDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(dto.Name))
        return Results.BadRequest(new { error = "Name is required" });

    var route = new Route
    {
        Name   = dto.Name.Trim(),
        Grade  = string.IsNullOrWhiteSpace(dto.Grade)  ? null : dto.Grade.Trim(),
        Sector = string.IsNullOrWhiteSpace(dto.Sector) ? null : dto.Sector.Trim()
    };

    db.Routes.Add(route);
    await db.SaveChangesAsync();
    return Results.Created($"/api/routes/{route.Id}", route);
})
.WithName("CreateRoute")
.WithTags("Routes");

app.MapGet("/api/routes/{id:int}", async (int id, AppDbContext db) =>
{
    var route = await db.Routes.FindAsync(id);
    return route is null ? Results.NotFound() : Results.Ok(route);
})
.WithName("GetRouteById")
.WithTags("Routes");


// --------------------
// PROGRESS
// --------------------
// GET /api/progress/me?userId={id}
// TODO: userId aus JWT-Token lesen sobald Keycloak-Auth aktiv ist
app.MapGet("/api/progress/me", async (int userId, AppDbContext db) =>
{
    var entries = await db.Progresses
        .Where(p => p.UserId == userId)
        .Include(p => p.Route)
        .OrderByDescending(p => p.Date)
        .ToListAsync();

    return Results.Ok(entries);
})
.WithName("GetMyProgress")
.WithTags("Progress");

app.MapPost("/api/progress", async (ProgressCreateDto dto, AppDbContext db) =>
{
    var validStatuses = new[] { "Attempted", "Completed", "Flashed", "Onsight" };
    if (!validStatuses.Contains(dto.Status))
        return Results.BadRequest(new { error = $"Status must be one of: {string.Join(", ", validStatuses)}" });

    if (!await db.Users.AnyAsync(u => u.Id == dto.UserId))
        return Results.BadRequest(new { error = "User not found" });

    if (!await db.Routes.AnyAsync(r => r.Id == dto.RouteId))
        return Results.BadRequest(new { error = "Route not found" });

    var progress = new Progress
    {
        UserId   = dto.UserId,
        RouteId  = dto.RouteId,
        Status   = dto.Status,
        Attempts = dto.Attempts,
        Notes    = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim(),
        Date     = dto.Date
    };

    db.Progresses.Add(progress);
    await db.SaveChangesAsync();
    return Results.Created($"/api/progress/{progress.Id}", progress);
})
.WithName("CreateProgress")
.WithTags("Progress");

app.MapPut("/api/progress/{id:int}", async (int id, ProgressUpdateDto dto, AppDbContext db) =>
{
    var progress = await db.Progresses.FindAsync(id);
    if (progress is null) return Results.NotFound();

    var validStatuses = new[] { "Attempted", "Completed", "Flashed", "Onsight" };
    if (!validStatuses.Contains(dto.Status))
        return Results.BadRequest(new { error = $"Status must be one of: {string.Join(", ", validStatuses)}" });

    progress.Status   = dto.Status;
    progress.Attempts = dto.Attempts;
    progress.Notes    = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim();
    progress.Date     = dto.Date;

    await db.SaveChangesAsync();
    return Results.Ok(progress);
})
.WithName("UpdateProgress")
.WithTags("Progress");

app.MapDelete("/api/progress/{id:int}", async (int id, AppDbContext db) =>
{
    var progress = await db.Progresses.FindAsync(id);
    if (progress is null) return Results.NotFound();

    db.Progresses.Remove(progress);
    await db.SaveChangesAsync();
    return Results.NoContent();
})
.WithName("DeleteProgress")
.WithTags("Progress");


app.Run();


// --------------------
// DB CONTEXT
// --------------------
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Ping>            Pings            => Set<Ping>();
    public DbSet<Area>            Areas            => Set<Area>();
    public DbSet<Route>           Routes           => Set<Route>();
    public DbSet<User>            Users            => Set<User>();
    public DbSet<Progress>        Progresses       => Set<Progress>();
    public DbSet<Appointment>     Appointments     => Set<Appointment>();
    public DbSet<AppointmentUser> AppointmentUsers => Set<AppointmentUser>();
    public DbSet<Comment>         Comments         => Set<Comment>();
    public DbSet<Report>          Reports          => Set<Report>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppointmentUser>()
            .HasKey(au => new { au.AppointmentId, au.UserId });
    }
}


// --------------------
// RECORDS
// --------------------
public record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
