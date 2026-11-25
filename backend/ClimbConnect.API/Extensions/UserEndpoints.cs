using System.Security.Claims;
using ClimbConnect.API.Data;
using ClimbConnect.API.Dtos;
using ClimbConnect.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClimbConnect.API.Extensions;

/// <summary>Endpoints für Benutzerprofile, Statistiken und die Benutzerliste.</summary>
public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
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

            // Grad-Entwicklung: höchster Rotpunkt-/Flash-/Onsight-Grad pro Monat
            var targetScale = scale ?? "french";
            var gradeProgression = ascents
                .Where(p => GradeConversionService.Rank(p.Route.Grade) >= 0)
                .GroupBy(p => new { p.Date.Year, p.Date.Month })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .Select(g =>
                {
                    var best = g.OrderByDescending(p => GradeConversionService.Rank(p.Route.Grade)).First();
                    return new
                    {
                        Month = $"{g.Key.Year:0000}-{g.Key.Month:00}",
                        Grade = GradeConversionService.Convert(best.Route.Grade, targetScale),
                        // Rang = Position in der Schwierigkeits-Reihenfolge, damit das Diagramm die Höhe richtig zeichnet
                        Rank  = GradeConversionService.Rank(best.Route.Grade)
                    };
                })
                .ToList();

            // Besuche pro Gebiet: Anzahl verschiedener Tage mit mindestens einem Eintrag im Gebiet.
            // Projekte zählen mit – wer es probiert hat, war auch dort.
            var areaVisits = progresses
                .GroupBy(p => new { p.Route.Sector.Area.Id, p.Route.Sector.Area.Name })
                .Select(g => new
                {
                    AreaId   = g.Key.Id,
                    AreaName = g.Key.Name,
                    Visits   = g.Select(p => p.Date).Distinct().Count()
                })
                .OrderByDescending(v => v.Visits)
                .ThenBy(v => v.AreaName)
                .ToList();

            // Lieblingsgebiet: Gebiet mit den meisten Besuchen, bei Gleichstand alle (z. B. "Traunstein, Hirschwand")
            var maxVisits = areaVisits.FirstOrDefault()?.Visits ?? 0;
            var favoriteArea = areaVisits.Count == 0
                ? null
                : string.Join(", ", areaVisits.Where(v => v.Visits == maxVisits).Select(v => v.AreaName));

            return Results.Ok(new
            {
                TotalClimbed     = ascents.Count,
                OpenProjects     = projects.Count,
                FavoriteArea     = favoriteArea,
                GradeProgression = gradeProgression,
                AreaVisits       = areaVisits
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

        // Passwort ändern
        app.MapPut("/api/users/me/password", async (ChangePasswordDto dto, ClaimsPrincipal user, AppDbContext db) =>
        {
            if (!int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Results.Unauthorized();

            var u = await db.Users.FindAsync(userId);
            if (u is null) return Results.NotFound();

            if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, u.PasswordHash))
                return Results.BadRequest(new { error = "Aktuelles Passwort ist falsch" });

            u.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithName("ChangePassword")
        .WithTags("Users")
        .RequireAuthorization("User");

        // Eigenes Konto löschen (#232, Recht auf Löschung nach DSGVO). Body: { "password": "..." }
        app.MapDelete("/api/users/me", async ([FromBody] DeleteAccountDto dto, ClaimsPrincipal user,
            AppDbContext db, UploadStorage storage, ILogger<Program> logger) =>
        {
            if (!int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Results.Unauthorized();

            var u = await db.Users.FindAsync(userId);
            if (u is null) return Results.NotFound();

            if (!BCrypt.Net.BCrypt.Verify(dto.Password, u.PasswordHash))
                return Results.BadRequest(new { error = "Das Passwort ist falsch." });

            // Ohne Admin könnte niemand mehr Gebiete und Routen pflegen
            if (u.Role == "admin" && await db.Users.CountAsync(x => x.Role == "admin") <= 1)
                return Results.Conflict(new { error = "Du bist der letzte Admin und kannst dein Konto nicht löschen." });

            var photoUrls = await DeleteUserDataAsync(db, userId);
            await storage.DeleteUnusedAsync(db, photoUrls, logger);
            return Results.NoContent();
        })
        .WithName("DeleteOwnAccount")
        .WithTags("Users")
        .RequireAuthorization("User");

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

    /// <summary>
    /// Löscht ein Konto mit allen zugehörigen Daten in einer Transaktion: Begehungen, Teilnahmen,
    /// eigene Termine (samt deren Teilnehmern und Pinnwand), eigene Pinnwand-Beiträge, Kommentare
    /// und Reports. Gibt die Foto-Links der gelöschten Kommentare und Reports zurück – die
    /// Dateien löscht der Aufrufer danach.
    /// Kopien in gemeldeten Fällen (#264) bleiben absichtlich bis zum Ablauf der 30 Tage erhalten
    /// (Schutz vor Missbrauch, sonst könnte man sich durch Löschen des Kontos der Meldung entziehen).
    /// </summary>
    internal static async Task<List<string?>> DeleteUserDataAsync(AppDbContext db, int userId)
    {
        var comments = await db.Comments.Where(c => c.UserId == userId).ToListAsync();
        var reports  = await db.Reports.Where(r => r.UserId == userId).ToListAsync();
        var photoUrls = comments.Select(c => c.PhotoUrl).Concat(reports.Select(r => r.PhotoUrl)).ToList();

        // Eigene Termine verschwinden mit allem, was daran hängt
        var ownAppointmentIds = await db.Appointments
            .Where(a => a.CreatedByUserId == userId).Select(a => a.Id).ToListAsync();

        // Ausdrücklich löschen statt auf Cascade zu vertrauen – gilt so für jede Datenbank
        db.PinnwandPosts.RemoveRange(db.PinnwandPosts.Where(p => p.UserId == userId || ownAppointmentIds.Contains(p.AppointmentId)));
        db.AppointmentUsers.RemoveRange(db.AppointmentUsers.Where(au => au.UserId == userId || ownAppointmentIds.Contains(au.AppointmentId)));
        db.Appointments.RemoveRange(db.Appointments.Where(a => a.CreatedByUserId == userId));
        db.Progresses.RemoveRange(db.Progresses.Where(p => p.UserId == userId));
        db.Comments.RemoveRange(comments);
        db.Reports.RemoveRange(reports);
        db.Users.Remove(await db.Users.FirstAsync(u => u.Id == userId));

        await db.SaveChangesAsync();
        return photoUrls;
    }
}
