using System.Security.Claims;
using ClimbConnect.API.Data;
using ClimbConnect.API.Dtos;
using ClimbConnect.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ClimbConnect.API.Extensions;

/// <summary>
/// Pinnwand-Beiträge melden und Fälle für den Admin (#264).
/// Ablauf: Die erste Meldung zu einem Termin legt einen Fall an und kopiert die ganze Pinnwand
/// hinein. Weitere Meldungen kopieren nur Beiträge dazu, die noch fehlen. Der Fall wird
/// 30 Tage nach der letzten Meldung gelöscht oder früher, wenn der Admin ihn erledigt.
/// </summary>
public static class ModerationEndpoints
{
    public static void MapModerationEndpoints(this IEndpointRouteBuilder app)
    {
        // Beitrag melden – nur Teilnehmer des Termins, nicht den eigenen Beitrag
        app.MapPost("/api/posts/{id:int}/reports", async (int id, PostReportCreateDto dto, ClaimsPrincipal user, AppDbContext db) =>
        {
            if (!int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Results.Unauthorized();

            var post = await db.PinnwandPosts
                .Include(p => p.Appointment).ThenInclude(a => a.Area)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (post is null) return Results.NotFound();

            if (!await PinnwandEndpoints.IsParticipantAsync(db, post.AppointmentId, userId))
                return PinnwandEndpoints.Forbidden("Melden können nur Teilnehmer des Termins.");
            if (post.UserId == userId)
                return Results.BadRequest(new { error = "Eigene Beiträge kannst du nicht melden – lösche sie stattdessen." });

            var error = ValidateReport(dto, out var comment);
            if (error is not null) return Results.BadRequest(new { error });

            var now = DateTime.UtcNow;
            var modCase = await db.ModerationCases
                .Include(c => c.Posts)
                .FirstOrDefaultAsync(c => c.AppointmentId == post.AppointmentId);

            // Schon gemeldet? Dann nichts kopieren und 409 zurückgeben.
            var existingCopy = modCase?.Posts.FirstOrDefault(cp => cp.OriginalPostId == post.Id);
            if (existingCopy is not null &&
                await db.PostReports.AnyAsync(r => r.CasePostId == existingCopy.Id && r.ReporterId == userId))
                return Results.Conflict(new { error = "Du hast diesen Beitrag schon gemeldet." });

            if (modCase is null)
            {
                // Erste Meldung zu diesem Termin: Fall anlegen, Termin-Daten mitkopieren
                modCase = new ModerationCase
                {
                    AppointmentId    = post.AppointmentId,
                    AppointmentTitle = post.Appointment.Title,
                    AppointmentDate  = post.Appointment.Date,
                    AreaName         = post.Appointment.Area?.Name,
                    CreatedAtUtc     = now
                };
                db.ModerationCases.Add(modCase);
            }

            // Alle Beiträge kopieren, die noch nicht im Fall sind – keine doppelten Kopien
            var alreadyCopied = modCase.Posts.Select(cp => cp.OriginalPostId).ToHashSet();
            var missing = await db.PinnwandPosts
                .Where(p => p.AppointmentId == post.AppointmentId)
                .Select(p => new { p.Id, p.UserId, p.User.Username, p.Text, p.CreatedAtUtc })
                .ToListAsync();
            foreach (var p in missing.Where(p => !alreadyCopied.Contains(p.Id)))
            {
                modCase.Posts.Add(new CasePost
                {
                    OriginalPostId = p.Id,
                    AuthorId       = p.UserId,
                    AuthorUsername = p.Username,
                    Text           = p.Text,
                    CreatedAtUtc   = p.CreatedAtUtc
                });
            }

            var reporterName = await db.Users.Where(u => u.Id == userId).Select(u => u.Username).FirstAsync();
            var target = modCase.Posts.First(cp => cp.OriginalPostId == post.Id);
            target.Reports.Add(new PostReport
            {
                ReporterId       = userId,
                ReporterUsername = reporterName,
                Reason           = dto.Reason,
                Comment          = comment,
                CreatedAtUtc     = now
            });
            modCase.LastReportAtUtc = now;

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Zwei Meldungen gleichzeitig (Doppelklick) – der Unique-Index hat die zweite verhindert
                return Results.Conflict(new { error = "Du hast diesen Beitrag schon gemeldet." });
            }

            return Results.Created($"/api/moderation/cases/{modCase.Id}", new { caseId = modCase.Id });
        })
        .WithName("ReportPinnwandPost")
        .WithTags("Moderation")
        .RequireAuthorization("User");

        // Admin: alle Fälle, neueste Meldung zuerst
        app.MapGet("/api/moderation/cases", async (AppDbContext db) =>
        {
            // Abgelaufene Fälle vorher löschen – zusätzlich zum täglichen Aufräumen im Hintergrund
            await DeleteExpiredCasesAsync(db, DateTime.UtcNow);

            var cases = await db.ModerationCases
                .Include(c => c.Posts).ThenInclude(cp => cp.Reports)
                .AsNoTracking()
                .ToListAsync();
            var existing = await ExistingAppointmentIdsAsync(db, cases);

            var result = cases
                .OrderByDescending(c => c.LastReportAtUtc)
                .Select(c => ToSummary(c, existing.Contains(c.AppointmentId)))
                .ToList();
            return Results.Ok(result);
        })
        .WithName("GetModerationCases")
        .WithTags("Moderation")
        .RequireAuthorization("Admin");

        // Admin: ein Fall mit dem ganzen Verlauf
        app.MapGet("/api/moderation/cases/{id:int}", async (int id, AppDbContext db) =>
        {
            await DeleteExpiredCasesAsync(db, DateTime.UtcNow);

            var c = await db.ModerationCases
                .Include(x => x.Posts).ThenInclude(cp => cp.Reports)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);
            if (c is null) return Results.NotFound();

            var s = ToSummary(c, await db.Appointments.AnyAsync(a => a.Id == c.AppointmentId));
            var posts = c.Posts
                .OrderBy(cp => cp.CreatedAtUtc).ThenBy(cp => cp.OriginalPostId)
                .Select(cp => new CasePostDto(
                    cp.Id, cp.OriginalPostId, cp.AuthorId, cp.AuthorUsername, cp.Text, cp.CreatedAtUtc,
                    cp.DeletedByAuthor,
                    cp.Reports.OrderBy(r => r.CreatedAtUtc)
                        .Select(r => new CaseReportDto(r.Id, r.ReporterId, r.ReporterUsername, r.Reason, r.Comment, r.CreatedAtUtc))
                        .ToList()))
                .ToList();

            return Results.Ok(new ModerationCaseDetailDto(
                s.Id, s.AppointmentId, s.AppointmentTitle, s.AppointmentDate, s.AreaName, s.AppointmentDeleted,
                s.CreatedAtUtc, s.LastReportAtUtc, s.DeleteAfterUtc, s.ReportCount, s.ReportedPostCount, s.PostCount,
                posts));
        })
        .WithName("GetModerationCase")
        .WithTags("Moderation")
        .RequireAuthorization("Admin");

        // Admin: Fall erledigen = Fall, Kopien und Meldungen löschen
        app.MapDelete("/api/moderation/cases/{id:int}", async (int id, AppDbContext db) =>
        {
            var c = await db.ModerationCases
                .Include(x => x.Posts).ThenInclude(cp => cp.Reports)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (c is null) return Results.NotFound();

            db.ModerationCases.Remove(c);
            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithName("ResolveModerationCase")
        .WithTags("Moderation")
        .RequireAuthorization("Admin");
    }

    /// <summary>
    /// Löscht alle Fälle, deren letzte Meldung länger als <see cref="ModerationCase.RetentionDays"/>
    /// Tage her ist (Speicherbegrenzung nach DSGVO). Wird vom Hintergrund-Dienst und vor jedem
    /// Lesen der Fälle aufgerufen. Gibt die Anzahl gelöschter Fälle zurück.
    /// </summary>
    public static async Task<int> DeleteExpiredCasesAsync(AppDbContext db, DateTime nowUtc)
    {
        var limit = nowUtc.AddDays(-ModerationCase.RetentionDays);
        var expired = await db.ModerationCases
            .Include(c => c.Posts).ThenInclude(cp => cp.Reports)
            .Where(c => c.LastReportAtUtc < limit)
            .ToListAsync();
        if (expired.Count == 0) return 0;

        db.ModerationCases.RemoveRange(expired);
        await db.SaveChangesAsync();
        return expired.Count;
    }

    /// <summary>
    /// Prüft Grund und Kommentar der Meldung. Gibt einen Fehlertext zurück oder null, wenn alles passt.
    /// Der Kommentar wird getrimmt; ein leerer Kommentar wird zu null.
    /// </summary>
    private static string? ValidateReport(PostReportCreateDto dto, out string? comment)
    {
        comment = string.IsNullOrWhiteSpace(dto.Comment) ? null : dto.Comment.Trim();

        if (!PostReport.Reasons.Contains(dto.Reason))
            return "Bitte wähle einen gültigen Grund.";
        if (dto.Reason == PostReport.OtherReason && comment is null)
            return "Bei „Sonstiges\" beschreibe bitte kurz, worum es geht.";
        return null;
    }

    /// <summary>Welche Termine der Fälle es noch gibt – eine Abfrage für die ganze Liste.</summary>
    private static async Task<HashSet<int>> ExistingAppointmentIdsAsync(AppDbContext db, List<ModerationCase> cases)
    {
        var ids = cases.Select(c => c.AppointmentId).Distinct().ToList();
        return (await db.Appointments.Where(a => ids.Contains(a.Id)).Select(a => a.Id).ToListAsync()).ToHashSet();
    }

    /// <summary>Kennzahlen eines Falls für die Admin-Liste.</summary>
    private static ModerationCaseSummaryDto ToSummary(ModerationCase c, bool appointmentExists) => new(
        c.Id, c.AppointmentId, c.AppointmentTitle, c.AppointmentDate, c.AreaName,
        AppointmentDeleted: !appointmentExists,
        c.CreatedAtUtc, c.LastReportAtUtc,
        DeleteAfterUtc:    c.LastReportAtUtc.AddDays(ModerationCase.RetentionDays),
        ReportCount:       c.Posts.Sum(cp => cp.Reports.Count),
        ReportedPostCount: c.Posts.Count(cp => cp.Reports.Count > 0),
        PostCount:         c.Posts.Count);
}
