using ClimbConnect.API.Data;
using ClimbConnect.API.Dtos;
using ClimbConnect.API.Models;
using ClimbConnect.API.Services;
using Microsoft.EntityFrameworkCore;
using Route = ClimbConnect.API.Models.Route;

namespace ClimbConnect.API.Extensions;

/// <summary>Endpoints für Kletterrouten.</summary>
public static class RouteEndpoints
{
    public static void MapRouteEndpoints(this IEndpointRouteBuilder app)
    {
        // Globale Routen-Suche über alle Sektoren.
        // Liefert zusätzlich Gebiet (AreaId, AreaName), damit die Suchtreffer im Frontend
        // ohne weitere Anfragen „Gebiet · Sektor" anzeigen können.
        // Filter (#240), alle optional und kombinierbar mit "search":
        //   minGrade/maxGrade – französische Grade, verglichen über den Rang der Grad-Tabelle
        //   style             – Stil, ohne Groß-/Kleinschreibung
        //   minLength/maxLength – Länge in Metern
        app.MapGet("/api/routes", async (string? search, string? scale,
            string? minGrade, string? maxGrade, string? style, int? minLength, int? maxLength,
            int? page, int? pageSize, AppDbContext db) =>
        {
            var filterError = ValidateFilter(minGrade, maxGrade, minLength, maxLength);
            if (filterError is not null) return Results.BadRequest(new { error = filterError });

            var p  = Math.Max(1, page ?? 1);
            var ps = Math.Clamp(pageSize ?? 20, 1, 100);

            var query = db.Routes
                .Include(r => r.Sector)
                .ThenInclude(s => s.Area)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(r =>
                    r.Name.ToLower().Contains(term) ||
                    (r.Description != null && r.Description.ToLower().Contains(term)));
            }

            // Grad: Grade werden als Text gespeichert ("6a+"). Der Vergleich "zwischen 6a und 6b"
            // geht deshalb über die Grad-Tabelle – alle Grade von Rang "von" bis Rang "bis".
            if (minGrade is not null || maxGrade is not null)
            {
                var allGrades = GradeConversionService.GetAllGrades();
                var fromRank  = minGrade is null ? 0 : GradeConversionService.Rank(minGrade);
                var toRank    = maxGrade is null ? allGrades.Count - 1 : GradeConversionService.Rank(maxGrade);
                var allowed   = allGrades.Skip(fromRank).Take(toRank - fromRank + 1).ToList();
                query = query.Where(r => r.Grade != null && allowed.Contains(r.Grade));
            }

            if (!string.IsNullOrWhiteSpace(style))
            {
                var wanted = style.Trim().ToLower();
                query = query.Where(r => r.Style != null && r.Style.ToLower() == wanted);
            }

            // Routen ohne Längenangabe fallen bei einem Längenfilter heraus
            if (minLength is not null) query = query.Where(r => r.LengthMeters != null && r.LengthMeters >= minLength);
            if (maxLength is not null) query = query.Where(r => r.LengthMeters != null && r.LengthMeters <= maxLength);

            var total  = await query.CountAsync();
            var routes = await query
                .OrderBy(r => r.Name)
                .Skip((p - 1) * ps)
                .Take(ps)
                .ToListAsync();

            var items = routes.Select(r => new
            {
                r.Id, r.SectorId,
                SectorName = r.Sector.Name,
                r.Sector.AreaId,
                AreaName   = r.Sector.Area.Name,
                r.Name,
                Grade      = GradeConversionService.Convert(r.Grade, scale ?? "french"),
                r.LengthMeters, r.Style, r.Description, r.CreatedAtUtc
            });

            return Results.Ok(new { Total = total, Page = p, PageSize = ps, Items = items });
        })
        .WithName("SearchRoutes")
        .WithTags("Routes");

        // Alle Stile, die bei Routen tatsächlich vorkommen – für die Stil-Auswahl im Filter (#240).
        // Aus den Daten statt einer festen Liste, weil die Werte uneinheitlich sind
        // (Seed: "Vorstieg"/"Toprope", Admin-Formular: "Sport", "Trad", …).
        app.MapGet("/api/routes/styles", async (AppDbContext db) =>
        {
            var styles = await db.Routes
                .Where(r => r.Style != null && r.Style != "")
                .Select(r => r.Style!)
                .Distinct()
                .ToListAsync();

            // "Sport" und "sport" nur einmal; alphabetisch nach deutscher Sortierung
            var german = StringComparer.Create(new System.Globalization.CultureInfo("de-AT"), ignoreCase: true);
            var result = styles
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .GroupBy(s => s.ToLowerInvariant())
                .Select(g => g.First())
                .OrderBy(s => s, german)
                .ToList();
            return Results.Ok(result);
        })
        .WithName("GetRouteStyles")
        .WithTags("Routes");

        app.MapGet("/api/sectors/{id:int}/routes", async (int id, string? scale, AppDbContext db) =>
        {
            if (!await db.Sectors.AnyAsync(s => s.Id == id)) return Results.NotFound();

            var routes = await db.Routes
                .Where(r => r.SectorId == id)
                .OrderBy(r => r.Name)
                .ToListAsync();

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
                SectorId     = id,
                Name         = dto.Name.Trim(),
                Grade        = string.IsNullOrWhiteSpace(dto.Grade)       ? null : dto.Grade.Trim().ToLower(),
                LengthMeters = dto.LengthMeters,
                Style        = string.IsNullOrWhiteSpace(dto.Style)       ? null : dto.Style.Trim(),
                Description  = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim()
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

        app.MapDelete("/api/routes/{id:int}", async (int id, AppDbContext db,
            UploadStorage storage, ILogger<Program> logger) =>
        {
            var route = await db.Routes.FindAsync(id);
            if (route is null) return Results.NotFound();

            // Fotos von Feedback/Reports an dieser Route – #238
            var photoUrls = await UploadStorage.PhotoUrlsAsync(db, c => c.RouteId == id, r => r.RouteId == id);

            db.Routes.Remove(route);
            await db.SaveChangesAsync();
            await storage.DeleteUnusedAsync(db, photoUrls, logger);
            return Results.NoContent();
        })
        .WithName("DeleteRoute")
        .WithTags("Routes")
        .RequireAuthorization("Admin");

        // Community-Grad: Durchschnitt der subjektiven Gradbewertungen aller User
        app.MapGet("/api/routes/{id:int}/community-grade", async (int id, string? scale, AppDbContext db) =>
        {
            if (!await db.Routes.AnyAsync(r => r.Id == id)) return Results.NotFound();

            var grades = await db.Progresses
                .Where(p => p.RouteId == id && p.SubjectiveGrade != null)
                .Select(p => p.SubjectiveGrade!)
                .ToListAsync();

            if (grades.Count == 0)
                return Results.Ok(new { CommunityGrade = (string?)null, VoteCount = 0 });

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
    }

    /// <summary>
    /// Prüft die Filter der Routensuche (#240). Gibt die Fehlermeldung zurück oder null,
    /// wenn alles passt.
    /// </summary>
    private static string? ValidateFilter(string? minGrade, string? maxGrade, int? minLength, int? maxLength)
    {
        if (minGrade is not null && !GradeConversionService.IsValid(minGrade))
            return $"Unbekannter Grad '{minGrade}' – erwartet wird ein französischer Grad, z. B. 6a+";
        if (maxGrade is not null && !GradeConversionService.IsValid(maxGrade))
            return $"Unbekannter Grad '{maxGrade}' – erwartet wird ein französischer Grad, z. B. 6a+";
        if (minGrade is not null && maxGrade is not null &&
            GradeConversionService.Rank(minGrade) > GradeConversionService.Rank(maxGrade))
            return "Der Grad 'von' darf nicht schwerer sein als 'bis'";
        if (minLength < 0 || maxLength < 0)
            return "Die Länge darf nicht negativ sein";
        if (minLength is not null && maxLength is not null && minLength > maxLength)
            return "Die Länge 'von' darf nicht größer sein als 'bis'";
        return null;
    }
}
