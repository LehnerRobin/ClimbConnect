using System.Security.Claims;
using ClimbConnect.API.Data;
using ClimbConnect.API.Dtos;
using ClimbConnect.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ClimbConnect.API.Extensions;

/// <summary>
/// Pinnwand beim Termin (#245): Absprachen nur unter Teilnehmern, kein Chat und keine
/// Direktnachrichten. Lesen und Schreiben nur für Teilnehmer und Ersteller, Löschen für den
/// Verfasser und den Admin.
/// </summary>
public static class PinnwandEndpoints
{
    private const string NotParticipantError = "Die Pinnwand ist nur für Teilnehmer des Termins.";

    public static void MapPinnwandEndpoints(this IEndpointRouteBuilder app)
    {
        // Beiträge eines Termins, älteste zuerst
        app.MapGet("/api/appointments/{id:int}/posts", async (int id, ClaimsPrincipal user, AppDbContext db) =>
        {
            if (!int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Results.Unauthorized();
            if (!await db.Appointments.AnyAsync(a => a.Id == id)) return Results.NotFound();

            // Admin darf mitlesen, um bei Problemen eingreifen zu können
            if (!user.IsInRole("admin") && !await IsParticipantAsync(db, id, userId))
                return Forbidden(NotParticipantError);

            var posts = await db.PinnwandPosts
                .Where(p => p.AppointmentId == id)
                .OrderBy(p => p.CreatedAtUtc).ThenBy(p => p.Id)
                .Select(p => new PinnwandPostDto(
                    p.Id, p.AppointmentId, p.UserId, p.User.Username, p.Text, p.CreatedAtUtc,
                    // Melden kommt mit #264
                    false))
                .ToListAsync();
            return Results.Ok(posts);
        })
        .WithName("GetPinnwandPosts")
        .WithTags("Pinnwand")
        .RequireAuthorization("User");

        // Neuer Beitrag – nur Teilnehmer und Ersteller
        app.MapPost("/api/appointments/{id:int}/posts", async (int id, PinnwandPostCreateDto dto, ClaimsPrincipal user, AppDbContext db) =>
        {
            if (!int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Results.Unauthorized();
            if (!await db.Appointments.AnyAsync(a => a.Id == id)) return Results.NotFound();
            if (!await IsParticipantAsync(db, id, userId))
                return Forbidden(NotParticipantError);

            var text = dto.Text.Trim();
            if (text.Length == 0)
                return Results.BadRequest(new { error = "Der Beitrag darf nicht leer sein." });

            var post = new PinnwandPost { AppointmentId = id, UserId = userId, Text = text };
            db.PinnwandPosts.Add(post);
            await db.SaveChangesAsync();

            var created = await db.PinnwandPosts
                .Where(p => p.Id == post.Id)
                .Select(p => new PinnwandPostDto(
                    p.Id, p.AppointmentId, p.UserId, p.User.Username, p.Text, p.CreatedAtUtc, false))
                .FirstAsync();
            return Results.Created($"/api/posts/{post.Id}", created);
        })
        .WithName("CreatePinnwandPost")
        .WithTags("Pinnwand")
        .RequireAuthorization("User");

        // Beitrag löschen – Verfasser oder Admin. Der Beitrag ist danach wirklich weg.
        app.MapDelete("/api/posts/{id:int}", async (int id, ClaimsPrincipal user, AppDbContext db) =>
        {
            if (!int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Results.Unauthorized();

            var post = await db.PinnwandPosts.FindAsync(id);
            if (post is null) return Results.NotFound();
            if (post.UserId != userId && !user.IsInRole("admin"))
                return Forbidden("Du kannst nur eigene Beiträge löschen.");

            db.PinnwandPosts.Remove(post);
            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithName("DeletePinnwandPost")
        .WithTags("Pinnwand")
        .RequireAuthorization("User");
    }

    /// <summary>
    /// Teilnehmer = beigetreten oder Ersteller des Termins. Der Ersteller ist seit #254
    /// automatisch beigetreten; die zweite Prüfung deckt ältere Termine ab.
    /// </summary>
    internal static async Task<bool> IsParticipantAsync(AppDbContext db, int appointmentId, int userId) =>
        await db.AppointmentUsers.AnyAsync(au => au.AppointmentId == appointmentId && au.UserId == userId)
        || await db.Appointments.AnyAsync(a => a.Id == appointmentId && a.CreatedByUserId == userId);

    /// <summary>403 mit Fehlertext – das Frontend zeigt den Text direkt an.</summary>
    internal static IResult Forbidden(string error) =>
        Results.Json(new { error }, statusCode: StatusCodes.Status403Forbidden);
}
