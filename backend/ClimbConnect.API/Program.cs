using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Swagger (Swashbuckle)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// EF Core InMemory
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseInMemoryDatabase("ClimbConnectDb"));

var app = builder.Build();

// Swagger UI (fürs Schul-Setup ruhig immer aktiv lassen)
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

// Health Endpoint (für Issue #6 / Sub-Issue #30)
app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }))
   .WithName("Health");


// Optional: Template-Endpoint (kannst du behalten)
app.MapGet("/weatherforecast", () =>
{
    var summaries = new[]
    {
        "Freezing","Bracing","Chilly","Cool","Mild","Warm","Balmy","Hot","Sweltering","Scorching"
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


app.Run();

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
}

public record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
