using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using ClimbConnect.API.Models;
using ClimbConnect.API.Dtos;
using ClimbConnect.API.Services;
using ClimbConnect.API.Data;
using Route = ClimbConnect.API.Models.Route;

var builder = WebApplication.CreateBuilder(args);

// --------------------
// LOGGING
// --------------------
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// --------------------
// SWAGGER
// --------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ClimbConnect API", Version = "v1" });

    // JWT Bearer in Swagger einbinden
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = SecuritySchemeType.Http,
        Scheme       = "bearer",
        BearerFormat = "JWT",
        In           = ParameterLocation.Header,
        Description  = "JWT Token eingeben: Bearer {token}"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// --------------------
// CORS
// --------------------
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()));

// --------------------
// HTTP LOGGING
// --------------------
builder.Services.AddHttpLogging(logging =>
{
    logging.LoggingFields = Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.RequestMethod
                          | Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.RequestPath
                          | Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.ResponseStatusCode
                          | Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.Duration;
});

// --------------------
// JWT AUTH (eigenes System, ersetzt Keycloak für jetzt)
// --------------------
builder.Services.AddSingleton<JwtService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = builder.Configuration["Jwt:Issuer"],
            ValidAudience            = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("admin"));
    options.AddPolicy("User",  policy => policy.RequireRole("user", "admin"));
});

// Zirkuläre Referenzen bei JSON-Serialisierung ignorieren
builder.Services.ConfigureHttpJsonOptions(opts =>
    opts.SerializerOptions.ReferenceHandler =
        System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles);

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
// GLOBALES ERROR-HANDLING
// --------------------
app.UseExceptionHandler(errApp => errApp.Run(async ctx =>
{
    ctx.Response.StatusCode  = 500;
    ctx.Response.ContentType = "application/json";

    var ex = ctx.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;

    // In Development: echten Fehler zurückgeben (hilft beim Debuggen)
    if (app.Environment.IsDevelopment() && ex is not null)
    {
        await ctx.Response.WriteAsJsonAsync(new { error = ex.Message, detail = ex.StackTrace });
        return;
    }

    await ctx.Response.WriteAsJsonAsync(new { error = "Ein unerwarteter Fehler ist aufgetreten." });
}));

// --------------------
// SWAGGER UI (nur in Development)
// --------------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// HTTP-Request-Logging
app.UseHttpLogging();

// Statische Dateien für hochgeladene Bilder (erreichbar unter /uploads/...)
var uploadsPath = Path.Combine(
    AppDomain.CurrentDomain.GetData("DataDirectory") as string
        ?? AppDomain.CurrentDomain.BaseDirectory,
    "uploads");
Directory.CreateDirectory(uploadsPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsPath),
    RequestPath  = "/uploads"
});

// Data-Ordner sicherstellen (SQLite braucht das Verzeichnis)
var dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
Directory.CreateDirectory(dataDir);
AppDomain.CurrentDomain.SetData("DataDirectory", AppDomain.CurrentDomain.BaseDirectory);

// Migrations bei App-Start automatisch anwenden + Seed-Daten einspielen
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    await SeedData.InitAsync(db);
}


// --------------------
// HEALTH
// --------------------
app.MapGet("/api/health", () =>
    Results.Ok(new { status = "ok" }))
   .WithName("Health");


// --------------------
// UPLOAD
// --------------------
app.MapPost("/api/upload", async (IFormFile file, HttpContext ctx) =>
{
    var allowed = new[] { "image/jpeg", "image/png", "image/webp", "image/gif" };
    if (!allowed.Contains(file.ContentType))
        return Results.BadRequest(new { error = "Nur JPG, PNG, WebP und GIF sind erlaubt." });

    if (file.Length > 5 * 1024 * 1024)
        return Results.BadRequest(new { error = "Maximale Dateigröße ist 5 MB." });

    var ext      = Path.GetExtension(file.FileName).ToLower();
    var fileName = $"{Guid.NewGuid()}{ext}";
    var savePath = Path.Combine(
        AppDomain.CurrentDomain.GetData("DataDirectory") as string
            ?? AppDomain.CurrentDomain.BaseDirectory,
        "uploads", fileName);

    await using var stream = File.Create(savePath);
    await file.CopyToAsync(stream);

    var url = $"/uploads/{fileName}";
    return Results.Ok(new { url });
})
.WithName("UploadImage")
.WithTags("Upload")
.RequireAuthorization("User")
.DisableAntiforgery();


// --------------------
// AUTH
// --------------------
app.MapPost("/api/auth/register", async (RegisterDto dto, AppDbContext db, JwtService jwt) =>
{
    if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
        return Results.BadRequest(new { error = "Username, Email und Passwort sind erforderlich" });

    if (dto.Password.Length < 8)
        return Results.BadRequest(new { error = "Passwort muss mindestens 8 Zeichen lang sein" });

    if (await db.Users.AnyAsync(u => u.Email == dto.Email.ToLower()))
        return Results.Conflict(new { error = "E-Mail bereits vergeben" });

    if (await db.Users.AnyAsync(u => u.Username == dto.Username))
        return Results.Conflict(new { error = "Username bereits vergeben" });

    var user = new User
    {
        Username     = dto.Username.Trim(),
        Email        = dto.Email.ToLower().Trim(),
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
        Role         = "user"
    };

    db.Users.Add(user);
    await db.SaveChangesAsync();

    return Results.Ok(new { token = jwt.GenerateToken(user), user.Id, user.Username, user.Email, user.Role });
})
.WithName("Register")
.WithTags("Auth");

app.MapPost("/api/auth/login", async (LoginDto dto, AppDbContext db, JwtService jwt) =>
{
    if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
        return Results.BadRequest(new { error = "E-Mail und Passwort sind erforderlich" });

    var user = await db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email.ToLower());
    if (user is null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        return Results.Unauthorized();

    return Results.Ok(new { token = jwt.GenerateToken(user), user.Id, user.Username, user.Email, user.Role });
})
.WithName("Login")
.WithTags("Auth");


// --------------------
// AREAS (Admin: POST/PUT/DELETE, alle: GET)
// --------------------
app.MapGet("/api/areas", async (AppDbContext db) =>
{
    var today = DateTime.UtcNow.Date;
    var tomorrow = today.AddDays(1);

    var areas = await db.Areas.OrderBy(a => a.Name).ToListAsync();

    // Für jedes Gebiet: wie viele Leute sind heute dort, wie viele kommen geplant
    var todayAppointments = await db.Appointments
        .Where(a => a.Date >= today && a.Date < tomorrow)
        .Include(a => a.AppointmentUsers)
        .ToListAsync();

    var result = areas.Select(a => new
    {
        a.Id, a.Name, a.Location, a.Description, a.CreatedAtUtc,
        TodayVisitors  = todayAppointments
            .Where(ap => ap.AreaId == a.Id)
            .Sum(ap => ap.AppointmentUsers.Count),
        TodayAppointments = todayAppointments.Count(ap => ap.AreaId == a.Id)
    });

    return Results.Ok(result);
})
.WithName("GetAreas")
.WithTags("Areas");

app.MapGet("/api/areas/{id:int}", async (int id, AppDbContext db) =>
{
    var area = await db.Areas
        .Include(a => a.Sectors)
        .FirstOrDefaultAsync(a => a.Id == id);
    return area is null ? Results.NotFound() : Results.Ok(area);
})
.WithName("GetAreaById")
.WithTags("Areas");

app.MapPost("/api/areas", async (AreaCreateDto dto, AppDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(dto.Name))
        return Results.BadRequest(new { error = "Name ist erforderlich" });

    var area = new Area
    {
        Name        = dto.Name.Trim(),
        Location    = string.IsNullOrWhiteSpace(dto.Location)    ? null : dto.Location.Trim(),
        Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim()
    };
    db.Areas.Add(area);
    await db.SaveChangesAsync();
    return Results.Created($"/api/areas/{area.Id}", area);
})
.WithName("CreateArea")
.WithTags("Areas")
.RequireAuthorization("Admin");

app.MapPut("/api/areas/{id:int}", async (int id, AreaUpdateDto dto, AppDbContext db) =>
{
    var area = await db.Areas.FindAsync(id);
    if (area is null) return Results.NotFound();
    if (string.IsNullOrWhiteSpace(dto.Name))
        return Results.BadRequest(new { error = "Name ist erforderlich" });

    area.Name        = dto.Name.Trim();
    area.Location    = string.IsNullOrWhiteSpace(dto.Location)    ? null : dto.Location.Trim();
    area.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();

    await db.SaveChangesAsync();
    return Results.Ok(area);
})
.WithName("UpdateArea")
.WithTags("Areas")
.RequireAuthorization("Admin");

app.MapDelete("/api/areas/{id:int}", async (int id, AppDbContext db) =>
{
    var area = await db.Areas.FindAsync(id);
    if (area is null) return Results.NotFound();
    db.Areas.Remove(area);
    await db.SaveChangesAsync();
    return Results.NoContent();
})
.WithName("DeleteArea")
.WithTags("Areas")
.RequireAuthorization("Admin");


// --------------------
// SECTORS (Admin: POST/PUT/DELETE, alle: GET)
// --------------------
app.MapGet("/api/areas/{id:int}/sectors", async (int id, AppDbContext db) =>
{
    if (!await db.Areas.AnyAsync(a => a.Id == id)) return Results.NotFound();

    var sectors = await db.Sectors
        .Where(s => s.AreaId == id)
        .OrderBy(s => s.Name)
        .ToListAsync();

    return Results.Ok(sectors);
})
.WithName("GetSectorsByArea")
.WithTags("Sectors");

app.MapGet("/api/sectors/{id:int}", async (int id, AppDbContext db) =>
{
    var sector = await db.Sectors
        .Include(s => s.Routes)
        .FirstOrDefaultAsync(s => s.Id == id);
    return sector is null ? Results.NotFound() : Results.Ok(sector);
})
.WithName("GetSectorById")
.WithTags("Sectors");

app.MapPost("/api/areas/{id:int}/sectors", async (int id, SectorCreateDto dto, AppDbContext db) =>
{
    if (!await db.Areas.AnyAsync(a => a.Id == id)) return Results.NotFound();
    if (string.IsNullOrWhiteSpace(dto.Name))
        return Results.BadRequest(new { error = "Name ist erforderlich" });

    var sector = new Sector
    {
        AreaId      = id,
        Name        = dto.Name.Trim(),
        Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim()
    };
    db.Sectors.Add(sector);
    await db.SaveChangesAsync();
    return Results.Created($"/api/sectors/{sector.Id}", sector);
})
.WithName("CreateSector")
.WithTags("Sectors")
.RequireAuthorization("Admin");

app.MapPut("/api/sectors/{id:int}", async (int id, SectorUpdateDto dto, AppDbContext db) =>
{
    var sector = await db.Sectors.FindAsync(id);
    if (sector is null) return Results.NotFound();
    if (string.IsNullOrWhiteSpace(dto.Name))
        return Results.BadRequest(new { error = "Name ist erforderlich" });

    sector.Name        = dto.Name.Trim();
    sector.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();

    await db.SaveChangesAsync();
    return Results.Ok(sector);
})
.WithName("UpdateSector")
.WithTags("Sectors")
.RequireAuthorization("Admin");

app.MapDelete("/api/sectors/{id:int}", async (int id, AppDbContext db) =>
{
    var sector = await db.Sectors.FindAsync(id);
    if (sector is null) return Results.NotFound();
    db.Sectors.Remove(sector);
    await db.SaveChangesAsync();
    return Results.NoContent();
})
.WithName("DeleteSector")
.WithTags("Sectors")
.RequireAuthorization("Admin");


// --------------------
// ROUTES (Admin: POST/PUT/DELETE, alle: GET)
// --------------------
app.MapGet("/api/sectors/{id:int}/routes", async (int id, string? scale, AppDbContext db) =>
{
    if (!await db.Sectors.AnyAsync(s => s.Id == id)) return Results.NotFound();

    var routes = await db.Routes
        .Where(r => r.SectorId == id)
        .OrderBy(r => r.Name)
        .ToListAsync();

    // Grad in gewünschter Skala ausgeben (Standard: französisch)
    var result = routes.Select(r => new
    {
        r.Id, r.SectorId, r.Name,
        Grade       = GradeConversionService.Convert(r.Grade, scale ?? "french"),
        r.LengthMeters, r.Style, r.Description, r.CreatedAtUtc
    });

    return Results.Ok(result);
})
.WithName("GetRoutesBySector")
.WithTags("Routes");

app.MapGet("/api/routes/{id:int}", async (int id, string? scale, AppDbContext db) =>
{
    var route = await db.Routes
        .Include(r => r.Sector)
        .FirstOrDefaultAsync(r => r.Id == id);
    if (route is null) return Results.NotFound();

    var result = new
    {
        route.Id, route.SectorId,
        SectorName  = route.Sector.Name,
        route.Name,
        Grade       = GradeConversionService.Convert(route.Grade, scale ?? "french"),
        route.LengthMeters, route.Style, route.Description, route.CreatedAtUtc
    };
    return Results.Ok(result);
})
.WithName("GetRouteById")
.WithTags("Routes");

app.MapPost("/api/sectors/{id:int}/routes", async (int id, RouteCreateDto dto, AppDbContext db) =>
{
    if (!await db.Sectors.AnyAsync(s => s.Id == id)) return Results.NotFound();
    if (string.IsNullOrWhiteSpace(dto.Name))
        return Results.BadRequest(new { error = "Name ist erforderlich" });

    var route = new Route
    {
        SectorId    = id,
        Name        = dto.Name.Trim(),
        Grade       = string.IsNullOrWhiteSpace(dto.Grade)       ? null : dto.Grade.Trim().ToLower(),
        LengthMeters = dto.LengthMeters,
        Style       = string.IsNullOrWhiteSpace(dto.Style)       ? null : dto.Style.Trim(),
        Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim()
    };
    db.Routes.Add(route);
    await db.SaveChangesAsync();
    return Results.Created($"/api/routes/{route.Id}", route);
})
.WithName("CreateRoute")
.WithTags("Routes")
.RequireAuthorization("Admin");

app.MapPut("/api/routes/{id:int}", async (int id, RouteUpdateDto dto, AppDbContext db) =>
{
    var route = await db.Routes.FindAsync(id);
    if (route is null) return Results.NotFound();
    if (string.IsNullOrWhiteSpace(dto.Name))
        return Results.BadRequest(new { error = "Name ist erforderlich" });

    route.Name        = dto.Name.Trim();
    route.Grade       = string.IsNullOrWhiteSpace(dto.Grade)       ? null : dto.Grade.Trim().ToLower();
    route.LengthMeters = dto.LengthMeters;
    route.Style       = string.IsNullOrWhiteSpace(dto.Style)       ? null : dto.Style.Trim();
    route.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();

    await db.SaveChangesAsync();
    return Results.Ok(route);
})
.WithName("UpdateRoute")
.WithTags("Routes")
.RequireAuthorization("Admin");

app.MapDelete("/api/routes/{id:int}", async (int id, AppDbContext db) =>
{
    var route = await db.Routes.FindAsync(id);
    if (route is null) return Results.NotFound();
    db.Routes.Remove(route);
    await db.SaveChangesAsync();
    return Results.NoContent();
})
.WithName("DeleteRoute")
.WithTags("Routes")
.RequireAuthorization("Admin");


// --------------------
// PROGRESS
// --------------------
app.MapGet("/api/progress/me", async (ClaimsPrincipal user, AppDbContext db) =>
{
    if (!int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        return Results.Unauthorized();

    var entries = await db.Progresses
        .Where(p => p.UserId == userId)
        .Include(p => p.Route)
        .OrderByDescending(p => p.Date)
        .ToListAsync();
    return Results.Ok(entries);
})
.WithName("GetMyProgress")
.WithTags("Progress")
.RequireAuthorization("User");

app.MapGet("/api/progress/{id:int}", async (int id, ClaimsPrincipal user, AppDbContext db) =>
{
    if (!int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        return Results.Unauthorized();

    var progress = await db.Progresses
        .Include(p => p.Route)
        .FirstOrDefaultAsync(p => p.Id == id);
    if (progress is null) return Results.NotFound();
    if (progress.UserId != userId) return Results.Forbid();

    return Results.Ok(progress);
})
.WithName("GetProgressById")
.WithTags("Progress")
.RequireAuthorization("User");

app.MapPost("/api/progress", async (ProgressCreateDto dto, ClaimsPrincipal user, AppDbContext db) =>
{
    if (!int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        return Results.Unauthorized();

    if (!ProgressConst.Statuses.Contains(dto.Status))
        return Results.BadRequest(new { error = $"Status muss einer von: {string.Join(", ", ProgressConst.Statuses)} sein" });
    if (!ProgressConst.Styles.Contains(dto.ClimbingStyle))
        return Results.BadRequest(new { error = $"Begehungsart muss einer von: {string.Join(", ", ProgressConst.Styles)} sein" });
    if (dto.Attempts < 1)
        return Results.BadRequest(new { error = "Anzahl Versuche muss mindestens 1 sein" });
    if (!await db.Routes.AnyAsync(r => r.Id == dto.RouteId))
        return Results.BadRequest(new { error = "Route nicht gefunden" });

    var progress = new Progress
    {
        UserId                 = userId,
        RouteId                = dto.RouteId,
        Status                 = dto.Status,
        ClimbingStyle          = dto.ClimbingStyle,
        Attempts               = dto.Attempts,
        Notes                  = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim(),
        Date                   = dto.Date,
        SubjectiveGrade        = string.IsNullOrWhiteSpace(dto.SubjectiveGrade) ? null : dto.SubjectiveGrade.Trim().ToLower(),
        SubjectiveGradeComment = string.IsNullOrWhiteSpace(dto.SubjectiveGradeComment) ? null : dto.SubjectiveGradeComment.Trim()
    };
    db.Progresses.Add(progress);
    await db.SaveChangesAsync();
    return Results.Created($"/api/progress/{progress.Id}", progress);
})
.WithName("CreateProgress")
.WithTags("Progress")
.RequireAuthorization("User");

app.MapPut("/api/progress/{id:int}", async (int id, ProgressUpdateDto dto, ClaimsPrincipal user, AppDbContext db) =>
{
    if (!int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        return Results.Unauthorized();

    var progress = await db.Progresses.FindAsync(id);
    if (progress is null) return Results.NotFound();
    if (progress.UserId != userId) return Results.Forbid();

    if (!ProgressConst.Statuses.Contains(dto.Status))
        return Results.BadRequest(new { error = $"Status muss einer von: {string.Join(", ", ProgressConst.Statuses)} sein" });
    if (!ProgressConst.Styles.Contains(dto.ClimbingStyle))
        return Results.BadRequest(new { error = $"Begehungsart muss einer von: {string.Join(", ", ProgressConst.Styles)} sein" });
    if (dto.Attempts < 1)
        return Results.BadRequest(new { error = "Anzahl Versuche muss mindestens 1 sein" });

    progress.Status                 = dto.Status;
    progress.ClimbingStyle          = dto.ClimbingStyle;
    progress.Attempts               = dto.Attempts;
    progress.Notes                  = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim();
    progress.Date                   = dto.Date;
    progress.SubjectiveGrade        = string.IsNullOrWhiteSpace(dto.SubjectiveGrade) ? null : dto.SubjectiveGrade.Trim().ToLower();
    progress.SubjectiveGradeComment = string.IsNullOrWhiteSpace(dto.SubjectiveGradeComment) ? null : dto.SubjectiveGradeComment.Trim();

    await db.SaveChangesAsync();
    return Results.Ok(progress);
})
.WithName("UpdateProgress")
.WithTags("Progress")
.RequireAuthorization("User");

app.MapDelete("/api/progress/{id:int}", async (int id, ClaimsPrincipal user, AppDbContext db) =>
{
    if (!int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        return Results.Unauthorized();

    var progress = await db.Progresses.FindAsync(id);
    if (progress is null) return Results.NotFound();
    if (progress.UserId != userId) return Results.Forbid();

    db.Progresses.Remove(progress);
    await db.SaveChangesAsync();
    return Results.NoContent();
})
.WithName("DeleteProgress")
.WithTags("Progress")
.RequireAuthorization("User");


// --------------------
// APPOINTMENTS
// --------------------
app.MapGet("/api/areas/{id:int}/appointments", async (int id, bool? all, AppDbContext db) =>
{
    if (!await db.Areas.AnyAsync(a => a.Id == id)) return Results.NotFound();

    // Standardmäßig nur zukünftige Termine; mit ?all=true auch vergangene
    var cutoff = (all == true) ? DateTime.MinValue : DateTime.UtcNow;

    var appointments = await db.Appointments
        .Where(a => a.AreaId == id && a.Date >= cutoff)
        .Include(a => a.AppointmentUsers)
        .OrderBy(a => a.Date)
        .ToListAsync();
    return Results.Ok(appointments);
})
.WithName("GetAppointmentsByArea")
.WithTags("Appointments");

app.MapGet("/api/appointments/{id:int}", async (int id, string? scale, AppDbContext db) =>
{
    var appointment = await db.Appointments
        .Include(a => a.AppointmentUsers)
            .ThenInclude(au => au.User)
        .FirstOrDefaultAsync(a => a.Id == id);
    if (appointment is null) return Results.NotFound();

    var targetScale    = scale ?? "french";
    var participantIds = appointment.AppointmentUsers.Select(au => au.UserId).ToList();

    // Besten Grad pro Teilnehmer direkt per DB-Query — kein vollständiges Progresses-Load
    var bestGradePerUser = await db.Progresses
        .Where(p => participantIds.Contains(p.UserId)
                 && p.Status != "Projekt"
                 && p.Route.Grade != null)
        .Select(p => new { p.UserId, p.Route.Grade })
        .ToListAsync();

    var participantRanks = bestGradePerUser
        .GroupBy(p => p.UserId)
        .Select(g => g.Max(p => GradeConversionService.Rank(p.Grade)))
        .Where(r => r >= 0)
        .ToList();

    string? avgGrade = null;
    if (participantRanks.Count > 0)
    {
        var avgRank    = (int)Math.Round(participantRanks.Average());
        var allGrades  = GradeConversionService.GetAllGrades();
        var frenchGrade = avgRank < allGrades.Count ? allGrades[avgRank] : null;
        avgGrade = GradeConversionService.Convert(frenchGrade, targetScale);
    }

    return Results.Ok(new
    {
        appointment.Id,
        appointment.AreaId,
        appointment.CreatedByUserId,
        appointment.Title,
        appointment.Date,
        appointment.MeetingPoint,
        appointment.Description,
        appointment.MinParticipants,
        appointment.MaxParticipants,
        ParticipantCount = appointment.AppointmentUsers.Count,
        AverageGrade     = avgGrade,
        Participants     = appointment.AppointmentUsers.Select(au => new
        {
            au.UserId,
            au.User.Username,
            au.Comment
        })
    });
})
.WithName("GetAppointmentById")
.WithTags("Appointments");

app.MapPost("/api/areas/{id:int}/appointments", async (int id, AppointmentCreateDto dto, ClaimsPrincipal user, AppDbContext db) =>
{
    if (!int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        return Results.Unauthorized();

    var areaExists = await db.Areas.AnyAsync(a => a.Id == id);
    if (!areaExists)
        return Results.NotFound();

    var currentUser = await db.Users.FindAsync(userId);
    if (currentUser is null)
        return Results.Unauthorized();

    if (string.IsNullOrWhiteSpace(dto.Title))
        return Results.BadRequest(new { error = "Titel ist erforderlich" });

    if (dto.MinParticipants.HasValue && dto.MaxParticipants.HasValue &&
        dto.MinParticipants.Value > dto.MaxParticipants.Value)
        return Results.BadRequest(new { error = "Minimale Teilnehmerzahl darf nicht größer als maximale Teilnehmerzahl sein" });

    var appointment = new Appointment
    {
        AreaId          = id,
        CreatedByUserId = userId,
        CreatedBy       = currentUser,
        Title           = dto.Title.Trim(),
        Date            = dto.Date,
        MeetingPoint    = string.IsNullOrWhiteSpace(dto.MeetingPoint) ? null : dto.MeetingPoint.Trim(),
        Description     = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
        MinParticipants = dto.MinParticipants,
        MaxParticipants = dto.MaxParticipants
    };

    db.Appointments.Add(appointment);
    await db.SaveChangesAsync();

    return Results.Created($"/api/appointments/{appointment.Id}", appointment);
})
.WithName("CreateAppointment")
.WithTags("Appointments")
.RequireAuthorization("User");


app.MapDelete("/api/appointments/{id:int}/subscribe", async (int id, ClaimsPrincipal user, AppDbContext db) =>
{
    if (!int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        return Results.Unauthorized();

    var subscription = await db.AppointmentUsers
        .FirstOrDefaultAsync(au => au.AppointmentId == id && au.UserId == userId);
    if (subscription is null) return Results.NotFound();
    db.AppointmentUsers.Remove(subscription);
    await db.SaveChangesAsync();
    return Results.NoContent();
})
.WithName("UnsubscribeFromAppointment")
.WithTags("Appointments")
.RequireAuthorization("User");

app.MapPut("/api/appointments/{id:int}", async (int id, AppointmentUpdateDto dto, ClaimsPrincipal user, AppDbContext db) =>
{
    if (!int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        return Results.Unauthorized();

    var appointment = await db.Appointments.FindAsync(id);
    if (appointment is null) return Results.NotFound();

    // Nur Ersteller oder Admin darf bearbeiten
    if (appointment.CreatedByUserId != userId && !user.IsInRole("admin"))
        return Results.Forbid();

    if (string.IsNullOrWhiteSpace(dto.Title))
        return Results.BadRequest(new { error = "Titel ist erforderlich" });

    appointment.Title           = dto.Title.Trim();
    appointment.Date            = dto.Date;
    appointment.MeetingPoint    = string.IsNullOrWhiteSpace(dto.MeetingPoint)  ? null : dto.MeetingPoint.Trim();
    appointment.Description     = string.IsNullOrWhiteSpace(dto.Description)   ? null : dto.Description.Trim();
    appointment.MinParticipants = dto.MinParticipants;
    appointment.MaxParticipants = dto.MaxParticipants;

    await db.SaveChangesAsync();
    return Results.Ok(appointment);
})
.WithName("UpdateAppointment")
.WithTags("Appointments")
.RequireAuthorization("User");

app.MapDelete("/api/appointments/{id:int}", async (int id, ClaimsPrincipal user, AppDbContext db) =>
{
    if (!int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        return Results.Unauthorized();

    var appointment = await db.Appointments.FindAsync(id);
    if (appointment is null) return Results.NotFound();

    // Nur Ersteller oder Admin darf löschen
    if (appointment.CreatedByUserId != userId && !user.IsInRole("admin"))
        return Results.Forbid();

    db.Appointments.Remove(appointment);
    await db.SaveChangesAsync();
    return Results.NoContent();
})
.WithName("DeleteAppointment")
.WithTags("Appointments")
.RequireAuthorization("User");


// --------------------
// COMMENTS
// --------------------
app.MapGet("/api/areas/{id:int}/comments", async (int id, AppDbContext db) =>
{
    if (!await db.Areas.AnyAsync(a => a.Id == id)) return Results.NotFound();
    var comments = await db.Comments
        .Where(c => c.AreaId == id)
        .Include(c => c.User)
        .OrderByDescending(c => c.CreatedAtUtc)
        .ToListAsync();
    return Results.Ok(comments);
})
.WithName("GetCommentsByArea")
.WithTags("Comments");

app.MapPost("/api/areas/{id:int}/comments", async (int id, CommentCreateDto dto, ClaimsPrincipal user, AppDbContext db) =>
{
    if (!int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        return Results.Unauthorized();
    if (!await db.Areas.AnyAsync(a => a.Id == id)) return Results.NotFound();
    if (string.IsNullOrWhiteSpace(dto.Text))
        return Results.BadRequest(new { error = "Text ist erforderlich" });

    var comment = new Comment
    {
        UserId   = userId,
        AreaId   = id,
        Text     = dto.Text.Trim(),
        PhotoUrl = string.IsNullOrWhiteSpace(dto.PhotoUrl) ? null : dto.PhotoUrl.Trim()
    };
    db.Comments.Add(comment);
    await db.SaveChangesAsync();
    return Results.Created($"/api/areas/{id}/comments", comment);
})
.WithName("CreateCommentForArea")
.WithTags("Comments")
.RequireAuthorization("User");

app.MapGet("/api/routes/{id:int}/comments", async (int id, AppDbContext db) =>
{
    if (!await db.Routes.AnyAsync(r => r.Id == id)) return Results.NotFound();
    var comments = await db.Comments
        .Where(c => c.RouteId == id)
        .Include(c => c.User)
        .OrderByDescending(c => c.CreatedAtUtc)
        .ToListAsync();
    return Results.Ok(comments);
})
.WithName("GetCommentsByRoute")
.WithTags("Comments");

app.MapPost("/api/routes/{id:int}/comments", async (int id, CommentCreateDto dto, ClaimsPrincipal user, AppDbContext db) =>
{
    if (!int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        return Results.Unauthorized();
    if (!await db.Routes.AnyAsync(r => r.Id == id)) return Results.NotFound();
    if (string.IsNullOrWhiteSpace(dto.Text))
        return Results.BadRequest(new { error = "Text ist erforderlich" });

    var comment = new Comment
    {
        UserId   = userId,
        RouteId  = id,
        Text     = dto.Text.Trim(),
        PhotoUrl = string.IsNullOrWhiteSpace(dto.PhotoUrl) ? null : dto.PhotoUrl.Trim()
    };
    db.Comments.Add(comment);
    await db.SaveChangesAsync();
    return Results.Created($"/api/routes/{id}/comments", comment);
})
.WithName("CreateCommentForRoute")
.WithTags("Comments")
.RequireAuthorization("User");

app.MapDelete("/api/comments/{id:int}", async (int id, ClaimsPrincipal user, AppDbContext db) =>
{
    if (!int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        return Results.Unauthorized();

    var comment = await db.Comments.FindAsync(id);
    if (comment is null) return Results.NotFound();
    if (comment.UserId != userId) return Results.Forbid();

    db.Comments.Remove(comment);
    await db.SaveChangesAsync();
    return Results.NoContent();
})
.WithName("DeleteComment")
.WithTags("Comments")
.RequireAuthorization("User");


// --------------------
// REPORTS
// --------------------
app.MapPost("/api/reports", async (ReportCreateDto dto, ClaimsPrincipal user, AppDbContext db) =>
{
    if (!int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        return Results.Unauthorized();

    var validSeverities = new[] { "Low", "Medium", "High" };
    if (!validSeverities.Contains(dto.Severity))
        return Results.BadRequest(new { error = "Severity muss Low, Medium oder High sein" });
    if (string.IsNullOrWhiteSpace(dto.Text))
        return Results.BadRequest(new { error = "Text ist erforderlich" });
    if (dto.AreaId is null && dto.RouteId is null)
        return Results.BadRequest(new { error = "AreaId oder RouteId muss angegeben werden" });
    if (dto.AreaId is not null && !await db.Areas.AnyAsync(a => a.Id == dto.AreaId))
        return Results.BadRequest(new { error = "Area nicht gefunden" });
    if (dto.RouteId is not null && !await db.Routes.AnyAsync(r => r.Id == dto.RouteId))
        return Results.BadRequest(new { error = "Route nicht gefunden" });

    var report = new Report
    {
        UserId   = userId,
        AreaId   = dto.AreaId,
        RouteId  = dto.RouteId,
        Text     = dto.Text.Trim(),
        PhotoUrl = string.IsNullOrWhiteSpace(dto.PhotoUrl) ? null : dto.PhotoUrl.Trim(),
        Severity = dto.Severity,
        Status   = "Open"
    };
    db.Reports.Add(report);
    await db.SaveChangesAsync();
    return Results.Created($"/api/reports/{report.Id}", report);
})
.WithName("CreateReport")
.WithTags("Reports")
.RequireAuthorization("User");

app.MapGet("/api/reports", async (AppDbContext db) =>
{
    var reports = await db.Reports
        .Include(r => r.User)
        .OrderByDescending(r => r.CreatedAtUtc)
        .ToListAsync();
    return Results.Ok(reports);
})
.WithName("GetReports")
.WithTags("Reports")
.RequireAuthorization("Admin");

app.MapPut("/api/reports/{id:int}/status", async (int id, ReportStatusUpdateDto dto, AppDbContext db) =>
{
    var report = await db.Reports.FindAsync(id);
    if (report is null) return Results.NotFound();
    if (!new[] { "Open", "Resolved" }.Contains(dto.Status))
        return Results.BadRequest(new { error = "Status muss 'Open' oder 'Resolved' sein" });
    report.Status = dto.Status;
    await db.SaveChangesAsync();
    return Results.Ok(report);
})
.WithName("UpdateReportStatus")
.WithTags("Reports")
.RequireAuthorization("Admin");

// Sicherheitsmeldungen zu einem Gebiet (für alle User sichtbar, nur offene)
app.MapGet("/api/areas/{id:int}/reports", async (int id, AppDbContext db) =>
{
    if (!await db.Areas.AnyAsync(a => a.Id == id)) return Results.NotFound();
    var reports = await db.Reports
        .Where(r => r.AreaId == id && r.Status == "Open")
        .OrderByDescending(r => r.CreatedAtUtc)
        .Select(r => new { r.Id, r.Text, r.Severity, r.PhotoUrl, r.CreatedAtUtc })
        .ToListAsync();
    return Results.Ok(reports);
})
.WithName("GetReportsByArea")
.WithTags("Reports");

// Sicherheitsmeldungen zu einer Route (für alle User sichtbar, nur offene)
app.MapGet("/api/routes/{id:int}/reports", async (int id, AppDbContext db) =>
{
    if (!await db.Routes.AnyAsync(r => r.Id == id)) return Results.NotFound();
    var reports = await db.Reports
        .Where(r => r.RouteId == id && r.Status == "Open")
        .OrderByDescending(r => r.CreatedAtUtc)
        .Select(r => new { r.Id, r.Text, r.Severity, r.PhotoUrl, r.CreatedAtUtc })
        .ToListAsync();
    return Results.Ok(reports);
})
.WithName("GetReportsByRoute")
.WithTags("Reports");


// --------------------
// EIGENES PROFIL
// --------------------

// Eigenes Profil abrufen (eingeloggter User)
app.MapGet("/api/users/me", async (ClaimsPrincipal user, AppDbContext db) =>
{
    if (!int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        return Results.Unauthorized();

    var u = await db.Users.FindAsync(userId);
    if (u is null) return Results.NotFound();

    return Results.Ok(new
    {
        u.Id, u.Username, u.Email, u.Role,
        u.Bio, u.PreferredGradeScale,
        MemberSince = u.CreatedAtUtc.ToString("yyyy-MM-dd")
    });
})
.WithName("GetOwnProfile")
.WithTags("Users")
.RequireAuthorization("User");

// Eigenes Profil bearbeiten (Bio und bevorzugte Gradskala)
app.MapPut("/api/users/me/profile", async (UserProfileUpdateDto dto, ClaimsPrincipal user, AppDbContext db) =>
{
    if (!int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        return Results.Unauthorized();

    var u = await db.Users.FindAsync(userId);
    if (u is null) return Results.NotFound();

    var validScales = new[] { "french", "uiaa", "american" };
    if (!string.IsNullOrWhiteSpace(dto.PreferredGradeScale) &&
        !validScales.Contains(dto.PreferredGradeScale.ToLower()))
        return Results.BadRequest(new { error = "PreferredGradeScale muss french, uiaa oder american sein" });

    u.Bio                 = string.IsNullOrWhiteSpace(dto.Bio) ? null : dto.Bio.Trim();
    u.PreferredGradeScale = string.IsNullOrWhiteSpace(dto.PreferredGradeScale)
                            ? null : dto.PreferredGradeScale.ToLower().Trim();

    await db.SaveChangesAsync();
    return Results.Ok(new
    {
        u.Id, u.Username, u.Email, u.Role,
        u.Bio, u.PreferredGradeScale
    });
})
.WithName("UpdateOwnProfile")
.WithTags("Users")
.RequireAuthorization("User");


// --------------------
// USER STATISTIKEN
// --------------------
app.MapGet("/api/users/{id:int}/stats", async (int id, string? scale, AppDbContext db) =>
{
    if (!await db.Users.AnyAsync(u => u.Id == id)) return Results.NotFound();

    var progresses = await db.Progresses
        .Where(p => p.UserId == id)
        .Include(p => p.Route)
            .ThenInclude(r => r.Sector)
                .ThenInclude(s => s.Area)
        .ToListAsync();

    // Gekletterte Routen (alles außer Projekt)
    var ascents   = progresses.Where(p => p.Status != "Projekt").ToList();
    var projects  = progresses.Where(p => p.Status == "Projekt").ToList();

    // Lieblingsgebiet: Gebiet mit den meisten Begehungen
    var favoriteArea = ascents
        .GroupBy(p => p.Route.Sector.Area.Name)
        .OrderByDescending(g => g.Count())
        .Select(g => g.Key)
        .FirstOrDefault();

    // Grad-Entwicklung: höchster Rotpunkt-/Flash-/Onsight-Grad pro Monat
    var targetScale = scale ?? "french";
    var gradeProgression = ascents
        .Where(p => p.Route.Grade != null)
        .GroupBy(p => new { p.Date.Year, p.Date.Month })
        .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
        .Select(g =>
        {
            var best = g.OrderByDescending(p => GradeConversionService.Rank(p.Route.Grade)).First();
            return new
            {
                Month = $"{g.Key.Year:0000}-{g.Key.Month:00}",
                Grade = GradeConversionService.Convert(best.Route.Grade, targetScale)
            };
        })
        .ToList();

    return Results.Ok(new
    {
        TotalClimbed     = ascents.Count,
        OpenProjects     = projects.Count,
        FavoriteArea     = favoriteArea,
        GradeProgression = gradeProgression
    });
})
.WithName("GetUserStats")
.WithTags("Users");


// --------------------
// OEFFENTLICHE PROFILE
// --------------------
app.MapGet("/api/users/{id:int}/profile", async (int id, string? scale, AppDbContext db) =>
{
    var user = await db.Users.FindAsync(id);
    if (user is null) return Results.NotFound();

    var targetScale = scale ?? "french";

    // Letzte 10 Begehungen (kein Projekt)
    var recentAscents = await db.Progresses
        .Where(p => p.UserId == id && p.Status != "Projekt")
        .Include(p => p.Route)
            .ThenInclude(r => r.Sector)
                .ThenInclude(s => s.Area)
        .OrderByDescending(p => p.Date)
        .Take(10)
        .Select(p => new
        {
            RouteName = p.Route.Name,
            Grade     = GradeConversionService.Convert(p.Route.Grade, targetScale),
            Area      = p.Route.Sector.Area.Name,
            p.Date,
            p.Status
        })
        .ToListAsync();

    return Results.Ok(new
    {
        user.Id,
        user.Username,
        MemberSince   = user.CreatedAtUtc.ToString("yyyy-MM-dd"),
        RecentAscents = recentAscents
    });
})
.WithName("GetUserProfile")
.WithTags("Users");


// --------------------
// COMMUNITY-GRAD
// --------------------
app.MapGet("/api/routes/{id:int}/community-grade", async (int id, string? scale, AppDbContext db) =>
{
    if (!await db.Routes.AnyAsync(r => r.Id == id)) return Results.NotFound();

    var grades = await db.Progresses
        .Where(p => p.RouteId == id && p.SubjectiveGrade != null)
        .Select(p => p.SubjectiveGrade!)
        .ToListAsync();

    if (grades.Count == 0)
        return Results.Ok(new { CommunityGrade = (string?)null, VoteCount = 0 });

    // Durchschnitt über den numerischen Rang berechnen
    var targetScale = scale ?? "french";
    var avgRank     = (int)Math.Round(grades.Select(GradeConversionService.Rank).Where(r => r >= 0).DefaultIfEmpty(-1).Average());
    var allGrades   = GradeConversionService.GetAllGrades();
    var frenchGrade = avgRank >= 0 && avgRank < allGrades.Count ? allGrades[avgRank] : null;

    return Results.Ok(new
    {
        CommunityGrade = GradeConversionService.Convert(frenchGrade, targetScale),
        VoteCount      = grades.Count
    });
})
.WithName("GetCommunityGrade")
.WithTags("Routes");


// --------------------
// USER-LISTE
// --------------------
app.MapGet("/api/users", async (AppDbContext db) =>
{
    var users = await db.Users
        .OrderBy(u => u.Username)
        .Select(u => new { u.Id, u.Username, MemberSince = u.CreatedAtUtc.ToString("yyyy-MM-dd") })
        .ToListAsync();
    return Results.Ok(users);
})
.WithName("GetUsers")
.WithTags("Users");


app.Run();


// --------------------
// KONSTANTEN
// --------------------
static class ProgressConst
{
    public static readonly string[] Statuses = ["Projekt", "Rotpunkt", "Flash", "Onsight"];
    public static readonly string[] Styles   = ["Toprope", "Vorstieg"];
}


// --------------------
// DB CONTEXT
// --------------------
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Area>            Areas            => Set<Area>();
    public DbSet<Sector>          Sectors          => Set<Sector>();
    public DbSet<Route>           Routes           => Set<Route>();
    public DbSet<User>            Users            => Set<User>();
    public DbSet<Progress>        Progresses       => Set<Progress>();
    public DbSet<Appointment>     Appointments     => Set<Appointment>();
    public DbSet<AppointmentUser> AppointmentUsers => Set<AppointmentUser>();
    public DbSet<Comment>         Comments         => Set<Comment>();
    public DbSet<Report>          Reports          => Set<Report>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Zusammengesetzter Primärschlüssel für die Subscribe-Tabelle
        modelBuilder.Entity<AppointmentUser>()
            .HasKey(au => new { au.AppointmentId, au.UserId });
    }
}