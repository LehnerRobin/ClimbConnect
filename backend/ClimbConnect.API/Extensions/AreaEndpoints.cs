using ClimbConnect.API.Data;
using ClimbConnect.API.Dtos;
using ClimbConnect.API.Models;
using ClimbConnect.API.Services;
using Microsoft.EntityFrameworkCore;

namespace ClimbConnect.API.Extensions;

/// <summary>Endpoints für Klettergebiete (Areas).</summary>
public static class AreaEndpoints
{
    public static void MapAreaEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/areas", async (string? search, int? page, int? pageSize, AppDbContext db) =>
        {
            // "Heute" und "nächste 7 Tage" (ab morgen, #239) nach österreichischer Ortszeit,
            // als UTC-Zeiträume (Termine sind in UTC gespeichert). Die Woche schließt direkt an heute an.
            var (todayStartUtc, todayEndUtc) = AppTime.TodayInViennaUtc();
            var (_, weekEndUtc)              = AppTime.NextWeekInViennaUtc();

            var p  = Math.Max(1, page ?? 1);
            var ps = Math.Clamp(pageSize ?? 20, 1, 100);

            var query = db.Areas.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(a =>
                    a.Name.ToLower().Contains(term) ||
                    (a.Location != null && a.Location.ToLower().Contains(term)) ||
                    (a.Description != null && a.Description.ToLower().Contains(term)));
            }

            var total = await query.CountAsync();
            var areas = await query
                .OrderBy(a => a.Name)
                .Skip((p - 1) * ps)
                .Take(ps)
                .ToListAsync();

            // Eine Abfrage für heute bis Ende der Woche – nur Gebiet, Datum und Teilnehmerzahl,
            // nur für die Gebiete dieser Seite
            var areaIds = areas.Select(a => a.Id).ToList();
            var appointments = await db.Appointments
                .Where(a => areaIds.Contains(a.AreaId) && a.Date >= todayStartUtc && a.Date < weekEndUtc)
                .Select(a => new { a.AreaId, a.Date, People = a.AppointmentUsers.Count })
                .ToListAsync();

            var today    = appointments.Where(ap => ap.Date < todayEndUtc).ToList();
            var nextWeek = appointments.Where(ap => ap.Date >= todayEndUtc).ToList();

            var result = areas.Select(a => new
            {
                a.Id, a.Name, a.Location, a.Description, a.ImageUrl, a.Latitude, a.Longitude, a.CreatedAtUtc,
                TodayVisitors        = today.Where(ap => ap.AreaId == a.Id).Sum(ap => ap.People),
                TodayAppointments    = today.Count(ap => ap.AreaId == a.Id),
                // Leute, die in den nächsten 7 Tagen (ab morgen) kommen wollen (#239)
                NextWeekVisitors     = nextWeek.Where(ap => ap.AreaId == a.Id).Sum(ap => ap.People),
                NextWeekAppointments = nextWeek.Count(ap => ap.AreaId == a.Id)
            });

            return Results.Ok(new { Total = total, Page = p, PageSize = ps, Items = result });
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
                Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
                ImageUrl    = string.IsNullOrWhiteSpace(dto.ImageUrl)    ? null : dto.ImageUrl.Trim(),
                Latitude    = dto.Latitude,
                Longitude   = dto.Longitude
            };
            db.Areas.Add(area);
            await db.SaveChangesAsync();
            return Results.Created($"/api/areas/{area.Id}", area);
        })
        .WithName("CreateArea")
        .WithTags("Areas")
        .RequireAuthorization("Admin");

        app.MapPut("/api/areas/{id:int}", async (int id, AreaUpdateDto dto, AppDbContext db,
            UploadStorage storage, ILogger<Program> logger) =>
        {
            var area = await db.Areas.FindAsync(id);
            if (area is null) return Results.NotFound();
            if (string.IsNullOrWhiteSpace(dto.Name))
                return Results.BadRequest(new { error = "Name ist erforderlich" });

            // Altes Titelbild merken – wird es ersetzt, kann die Datei weg (#238)
            var oldImageUrl = area.ImageUrl;

            area.Name        = dto.Name.Trim();
            area.Location    = string.IsNullOrWhiteSpace(dto.Location)    ? null : dto.Location.Trim();
            area.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
            area.ImageUrl    = string.IsNullOrWhiteSpace(dto.ImageUrl)    ? null : dto.ImageUrl.Trim();
            area.Latitude    = dto.Latitude;
            area.Longitude   = dto.Longitude;

            await db.SaveChangesAsync();
            if (oldImageUrl != area.ImageUrl)
                await storage.DeleteUnusedAsync(db, [oldImageUrl], logger);
            return Results.Ok(area);
        })
        .WithName("UpdateArea")
        .WithTags("Areas")
        .RequireAuthorization("Admin");

        app.MapDelete("/api/areas/{id:int}", async (int id, AppDbContext db,
            UploadStorage storage, ILogger<Program> logger) =>
        {
            var area = await db.Areas.FindAsync(id);
            if (area is null) return Results.NotFound();

            // Titelbild und Fotos aller Feedback-Einträge/Reports im Gebiet (auch an dessen Routen) – #238
            var photoUrls = await UploadStorage.PhotoUrlsAsync(db,
                c => c.AreaId == id || (c.Route != null && c.Route.Sector.AreaId == id),
                r => r.AreaId == id || (r.Route != null && r.Route.Sector.AreaId == id));
            photoUrls.Add(area.ImageUrl);

            db.Areas.Remove(area);
            await db.SaveChangesAsync();
            await storage.DeleteUnusedAsync(db, photoUrls, logger);
            return Results.NoContent();
        })
        .WithName("DeleteArea")
        .WithTags("Areas")
        .RequireAuthorization("Admin");
    }
}
