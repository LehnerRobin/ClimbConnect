using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using ClimbConnect.API.Data;
using ClimbConnect.API.Extensions;
using ClimbConnect.API.Services;

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
// JWT AUTH
// --------------------
// Der Platzhalter aus appsettings.json ist öffentlich bekannt. Außerhalb von Entwicklung
// und Tests muss ein eigener Key gesetzt sein (Umgebungsvariable Jwt__Key), sonst könnte
// jeder gültige Tokens erzeugen.
var jwtKey = builder.Configuration["Jwt:Key"];
if (!builder.Environment.IsDevelopment() && !builder.Environment.IsEnvironment("Testing") &&
    (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.StartsWith("CHANGE_ME") || jwtKey.Length < 32))
{
    throw new InvalidOperationException(
        "Jwt:Key ist nicht gesetzt. Bitte über die Umgebungsvariable Jwt__Key einen eigenen " +
        "geheimen Schlüssel mit mindestens 32 Zeichen setzen.");
}

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
app.UseHttpLogging();

// Statische Dateien für hochgeladene Bilder
var uploadsPath = Path.Combine(
    AppDomain.CurrentDomain.GetData("DataDirectory") as string
        ?? AppDomain.CurrentDomain.BaseDirectory,
    "uploads");
Directory.CreateDirectory(uploadsPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsPath),
    RequestPath  = "/uploads",
    // Browser sollen den Dateityp nicht selbst erraten (Schutz vor als Bild getarnten Skripten)
    OnPrepareResponse = ctx => ctx.Context.Response.Headers["X-Content-Type-Options"] = "nosniff"
});

// Data-Ordner sicherstellen (SQLite braucht das Verzeichnis)
var dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
Directory.CreateDirectory(dataDir);
AppDomain.CurrentDomain.SetData("DataDirectory", AppDomain.CurrentDomain.BaseDirectory);

// Migrations und Seed-Daten beim Start
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    // Migrate() nur bei relationalen DBs (SQLite) — nicht bei InMemory (Tests)
    if (db.Database.IsRelational())
        db.Database.Migrate();
    else
        db.Database.EnsureCreated();
    await SeedData.InitAsync(db);
}

// --------------------
// ENDPOINTS
// --------------------
app.MapGet("/api/health", () => Results.Ok(new { status = "ok" })).WithName("Health");

// Alle API-Endpoints laufen durch den ValidationFilter, der die DataAnnotations der DTOs prüft
var api = app.MapGroup(string.Empty).AddEndpointFilter<ValidationFilter>();

api.MapAuthEndpoints();
api.MapAreaEndpoints();
api.MapSectorEndpoints();
api.MapRouteEndpoints();
api.MapProgressEndpoints();
api.MapAppointmentEndpoints();
api.MapCommentEndpoints();
api.MapReportEndpoints();
api.MapUserEndpoints();
api.MapUploadEndpoints();

app.Run();

// Damit WebApplicationFactory in Tests auf die Program-Klasse zugreifen kann
public partial class Program { }
