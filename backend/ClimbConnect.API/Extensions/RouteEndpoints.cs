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
    public static void MapRouteEndpoints(this WebApplication app)
    {
        // Globale Routen-Suche über alle Sektoren
        app.MapGet("/api/routes", async (string? search, string? scale, AppDbContext db) =>
        {
            var query = db.Routes.Include(r => r.Sector).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(r =>
                    r.Name.ToLower().Contains(term) ||
                    (r.Description != null && r.Description.ToLower().Contains(term)));
            }

            var routes = await query.OrderBy(r => r.Name).ToListAsync();

            var result = routes.Select(r => new
            {
                r.Id, r.SectorId,
                SectorName = r.Sector.Name,
                r.Name,
                Grade      = GradeConversionService.Convert(r.Grade, scale ?? "french"),
                r.LengthMeters, r.Style, r.Description, r.CreatedAtUtc
            });

            return Results.Ok(result);
        })
        .WithName("SearchRoutes")
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
}
