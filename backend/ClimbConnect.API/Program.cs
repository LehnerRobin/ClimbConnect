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
// APPOINTMENTS
// --------------------
app.MapGet("/api/areas/{id:int}/appointments", async (int id, AppDbContext db) =>
{
    if (!await db.Areas.AnyAsync(a => a.Id == id)) return Results.NotFound();

    var appointments = await db.Appointments
        .Where(a => a.AreaId == id)
        .Include(a => a.AppointmentUsers)
        .OrderBy(a => a.Date)
        .ToListAsync();

    return Results.Ok(appointments);
})
.WithName("GetAppointmentsByArea")
.WithTags("Appointments");

app.MapPost("/api/areas/{id:int}/appointments", async (int id, AppointmentCreateDto dto, AppDbContext db) =>
{
    if (!await db.Areas.AnyAsync(a => a.Id == id)) return Results.NotFound();
    if (string.IsNullOrWhiteSpace(dto.Title))
        return Results.BadRequest(new { error = "Title is required" });
    if (!await db.Users.AnyAsync(u => u.Id == dto.CreatedByUserId))
        return Results.BadRequest(new { error = "User not found" });

    var appointment = new Appointment
    {
        AreaId           = id,
        CreatedByUserId  = dto.CreatedByUserId,
        Title            = dto.Title.Trim(),
        Date             = dto.Date,
        MeetingPoint     = string.IsNullOrWhiteSpace(dto.MeetingPoint)  ? null : dto.MeetingPoint.Trim(),
        Description      = string.IsNullOrWhiteSpace(dto.Description)   ? null : dto.Description.Trim()
    };

    db.Appointments.Add(appointment);
    await db.SaveChangesAsync();
    return Results.Created($"/api/appointments/{appointment.Id}", appointment);
})
.WithName("CreateAppointment")
.WithTags("Appointments");

app.MapPost("/api/appointments/{id:int}/subscribe", async (int id, AppointmentSubscribeDto dto, AppDbContext db) =>
{
    if (!await db.Appointments.AnyAsync(a => a.Id == id)) return Results.NotFound();
    if (!await db.Users.AnyAsync(u => u.Id == dto.UserId))
        return Results.BadRequest(new { error = "User not found" });

    var alreadySubscribed = await db.AppointmentUsers
        .AnyAsync(au => au.AppointmentId == id && au.UserId == dto.UserId);
    if (alreadySubscribed)
        return Results.Conflict(new { error = "Already subscribed" });

    var subscription = new AppointmentUser
    {
        AppointmentId = id,
        UserId        = dto.UserId,
        Comment       = string.IsNullOrWhiteSpace(dto.Comment) ? null : dto.Comment.Trim()
    };

    db.AppointmentUsers.Add(subscription);
    await db.SaveChangesAsync();
    return Results.Created($"/api/appointments/{id}/subscribe", subscription);
})
.WithName("SubscribeToAppointment")
.WithTags("Appointments");

app.MapDelete("/api/appointments/{id:int}/subscribe", async (int id, int userId, AppDbContext db) =>
{
    var subscription = await db.AppointmentUsers
        .FirstOrDefaultAsync(au => au.AppointmentId == id && au.UserId == userId);

    if (subscription is null) return Results.NotFound();

    db.AppointmentUsers.Remove(subscription);
    await db.SaveChangesAsync();
    return Results.NoContent();
})
.WithName("UnsubscribeFromAppointment")
.WithTags("Appointments");


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
