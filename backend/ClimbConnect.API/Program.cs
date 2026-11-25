using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Net;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
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
builder.Services.AddSingleton<UploadStorage>();

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

// --------------------
// ECHTE CLIENT-IP HINTER PROXYS
// --------------------
// Auf Railway läuft jede Anfrage über den Railway-Proxy und danach über das nginx im
// Frontend-Container. Das Backend sieht deshalb nur die IP von nginx. Die echte IP steht
// im Header X-Forwarded-For. Es werden nur die letzten ForwardLimit Einträge ausgewertet –
// die hängen die Proxys selbst an. Einträge weiter vorne könnte der Client fälschen.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit     = builder.Configuration.GetValue("Proxy:ForwardLimit", 2);
    // Die IPs der Railway-Proxys sind nicht fix, deshalb wird jedem Proxy vertraut.
    // Das Backend ist auf Railway nur über das private Netzwerk erreichbar.
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// --------------------
// RATE LIMITING (Schutz gegen Passwort-Raten)
// --------------------
// Login und Registrierung: höchstens AuthPermitLimit Anfragen pro Minute und IP.
// Danach 429 – das macht Durchprobieren von Passwörtern praktisch unmöglich.
var authPermitLimit = builder.Configuration.GetValue("RateLimit:AuthPermitLimit", 10);
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy(RateLimitPolicies.Auth, ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            ctx.Connection.RemoteIpAddress?.ToString() ?? "unbekannt",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = authPermitLimit,
                Window      = TimeSpan.FromMinutes(1),
                QueueLimit  = 0
            }));

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
        await context.HttpContext.Response.WriteAsJsonAsync(
            new { error = "Zu viele Versuche. Bitte warte kurz und versuche es dann noch einmal." }, token);
    };
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

// Muss als Erstes laufen, damit alles danach die echte Client-IP sieht
app.UseForwardedHeaders();

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
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseHttpLogging();

// Statische Dateien für hochgeladene Bilder (Ordner kommt aus UploadStorage)
var uploadsPath = app.Services.GetRequiredService<UploadStorage>().UploadsPath;
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
    await SeedData.InitAsync(db, app.Environment, app.Configuration);
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
api.MapGradeEndpoints();
api.MapBackupEndpoints();
api.MapPinnwandEndpoints();

app.Run();

// Damit WebApplicationFactory in Tests auf die Program-Klasse zugreifen kann
public partial class Program { }
