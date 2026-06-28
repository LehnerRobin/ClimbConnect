using System.Security.Claims;
using ClimbConnect.API.Data;
using ClimbConnect.API.Dtos;
using ClimbConnect.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ClimbConnect.API.Extensions;

/// <summary>Endpoints für Fortschrittseinträge (Begehungen) des eingeloggten Users.</summary>
public static class ProgressEndpoints
{
    public static void MapProgressEndpoints(this WebApplication app)
    {
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
    }
}
