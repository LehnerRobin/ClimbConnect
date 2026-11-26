using System.Security.Claims;
using ClimbConnect.API.Data;
using ClimbConnect.API.Dtos;
using ClimbConnect.API.Models;
using ClimbConnect.API.Services;
using Microsoft.EntityFrameworkCore;

namespace ClimbConnect.API.Extensions;

/// <summary>
/// Feedback an das Team (#244) – ersetzt die öffentlichen Kommentare. Schreiben dürfen alle
/// eingeloggten User, lesen und verwalten nur der Admin. Nichts davon ist öffentlich sichtbar.
/// </summary>
public static class FeedbackEndpoints
{
    /// <summary>Fehlermeldung, wenn der Foto-Link nicht auf ein eigenes hochgeladenes Bild zeigt.</summary>
    internal const string PhotoUrlError = "Foto muss über /api/upload hochgeladen werden.";

    /// <summary>Ausgabeformat für den Admin – vom Verfasser nur Id und Username.</summary>
    private static IQueryable<FeedbackDto> ToDto(this IQueryable<Feedback> feedback) =>
        feedback.Select(f => new FeedbackDto(
            f.Id, f.UserId, f.User.Username, f.Text, f.PhotoUrl,
            f.AreaId,  f.Area  != null ? f.Area.Name  : null,
            f.RouteId, f.Route != null ? f.Route.Name : null,
            f.IsRead, f.CreatedAtUtc));

    public static void MapFeedbackEndpoints(this IEndpointRouteBuilder app)
    {
        // Feedback schreiben – alle eingeloggten User
        app.MapPost("/api/feedback", async (FeedbackCreateDto dto, ClaimsPrincipal user, AppDbContext db, UploadStorage storage) =>
        {
            if (!int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Results.Unauthorized();
            if (string.IsNullOrWhiteSpace(dto.Text))
                return Results.BadRequest(new { error = "Bitte schreib, worum es geht." });
            if (!string.IsNullOrWhiteSpace(dto.PhotoUrl) && !storage.IsOwnUpload(dto.PhotoUrl.Trim()))
                return Results.BadRequest(new { error = PhotoUrlError });
            if (dto.AreaId is not null && !await db.Areas.AnyAsync(a => a.Id == dto.AreaId))
                return Results.BadRequest(new { error = "Das Gebiet gibt es nicht." });
            if (dto.RouteId is not null && !await db.Routes.AnyAsync(r => r.Id == dto.RouteId))
                return Results.BadRequest(new { error = "Die Route gibt es nicht." });

            var feedback = new Feedback
            {
                UserId   = userId,
                AreaId   = dto.AreaId,
                RouteId  = dto.RouteId,
                Text     = dto.Text.Trim(),
                PhotoUrl = string.IsNullOrWhiteSpace(dto.PhotoUrl) ? null : dto.PhotoUrl.Trim()
            };
            db.Feedback.Add(feedback);
            await db.SaveChangesAsync();

            // Der User bekommt nur die Bestätigung, nicht die Admin-Ansicht
            return Results.Created($"/api/feedback/{feedback.Id}", new { feedback.Id });
        })
        .WithName("CreateFeedback")
        .WithTags("Feedback")
        .RequireAuthorization("User");

        // Admin: alle Einträge, ungelesene zuerst, dann neueste zuerst
        app.MapGet("/api/feedback", async (AppDbContext db) =>
        {
            var list = await db.Feedback
                .OrderBy(f => f.IsRead)
                .ThenByDescending(f => f.CreatedAtUtc)
                .ToDto()
                .ToListAsync();
            return Results.Ok(list);
        })
        .WithName("GetFeedback")
        .WithTags("Feedback")
        .RequireAuthorization("Admin");

        // Admin: als gelesen / ungelesen markieren
        app.MapPut("/api/feedback/{id:int}/read", async (int id, FeedbackReadDto dto, AppDbContext db) =>
        {
            var feedback = await db.Feedback.FindAsync(id);
            if (feedback is null) return Results.NotFound();

            feedback.IsRead = dto.IsRead;
            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithName("MarkFeedbackRead")
        .WithTags("Feedback")
        .RequireAuthorization("Admin");

        // Admin: löschen – samt Foto (#238)
        app.MapDelete("/api/feedback/{id:int}", async (int id, AppDbContext db, UploadStorage storage, ILogger<Program> logger) =>
        {
            var feedback = await db.Feedback.FindAsync(id);
            if (feedback is null) return Results.NotFound();

            db.Feedback.Remove(feedback);
            await db.SaveChangesAsync();
            await storage.DeleteUnusedAsync(db, [feedback.PhotoUrl], logger);
            return Results.NoContent();
        })
        .WithName("DeleteFeedback")
        .WithTags("Feedback")
        .RequireAuthorization("Admin");
    }
}
