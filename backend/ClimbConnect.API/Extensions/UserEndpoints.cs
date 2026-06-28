using System.Security.Claims;
using ClimbConnect.API.Data;
using ClimbConnect.API.Dtos;
using ClimbConnect.API.Services;
using Microsoft.EntityFrameworkCore;

namespace ClimbConnect.API.Extensions;

/// <summary>Endpoints für Benutzerprofile, Statistiken und die Benutzerliste.</summary>
public static class UserEndpoints
{
    public static void MapUserEndpoints(this WebApplication app)
    {
        // Eigenes Profil abrufen
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

        // Statistiken eines Users (öffentlich)
        app.MapGet("/api/users/{id:int}/stats", async (int id, string? scale, AppDbContext db) =>
        {
            if (!await db.Users.AnyAsync(u => u.Id == id)) return Results.NotFound();

            var progresses = await db.Progresses
                .Where(p => p.UserId == id)
                .Include(p => p.Route)
                    .ThenInclude(r => r.Sector)
                        .ThenInclude(s => s.Area)
                .ToListAsync();

            var ascents  = progresses.Where(p => p.Status != "Projekt").ToList();
            var projects = progresses.Where(p => p.Status == "Projekt").ToList();

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

        // Öffentliches Profil: letzte Begehungen
        app.MapGet("/api/users/{id:int}/profile", async (int id, string? scale, AppDbContext db) =>
        {
            var user = await db.Users.FindAsync(id);
            if (user is null) return Results.NotFound();

            var targetScale = scale ?? "french";

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

        // Benutzerliste (öffentlich)
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
    }
}
