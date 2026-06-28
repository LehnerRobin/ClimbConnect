using System.Security.Claims;
using ClimbConnect.API.Data;
using ClimbConnect.API.Dtos;
using ClimbConnect.API.Models;
using ClimbConnect.API.Services;
using Microsoft.EntityFrameworkCore;

namespace ClimbConnect.API.Extensions;

/// <summary>Endpoints für Termine (Appointments) und Teilnahme-Verwaltung.</summary>
public static class AppointmentEndpoints
{
    public static void MapAppointmentEndpoints(this WebApplication app)
    {
        app.MapGet("/api/areas/{id:int}/appointments", async (int id, bool? all, AppDbContext db) =>
        {
            if (!await db.Areas.AnyAsync(a => a.Id == id)) return Results.NotFound();

            // Standardmäßig nur zukünftige Termine; mit ?all=true auch vergangene
            var cutoff = (all == true) ? DateTime.MinValue : DateTime.UtcNow;

            var appointments = await db.Appointments
                .Where(a => a.AreaId == id && a.Date >= cutoff)
                .Include(a => a.AppointmentUsers)
                .OrderBy(a => a.Date)
                .ToListAsync();
            return Results.Ok(appointments);
        })
        .WithName("GetAppointmentsByArea")
        .WithTags("Appointments");

        app.MapGet("/api/appointments/{id:int}", async (int id, string? scale, AppDbContext db) =>
        {
            var appointment = await db.Appointments
                .Include(a => a.AppointmentUsers)
                    .ThenInclude(au => au.User)
                .FirstOrDefaultAsync(a => a.Id == id);
            if (appointment is null) return Results.NotFound();

            var targetScale    = scale ?? "french";
            var participantIds = appointment.AppointmentUsers.Select(au => au.UserId).ToList();

            // Besten Grad pro Teilnehmer direkt per DB-Query — kein vollständiges Progresses-Load
            var bestGradePerUser = await db.Progresses
                .Where(p => participantIds.Contains(p.UserId)
                         && p.Status != "Projekt"
                         && p.Route.Grade != null)
                .Select(p => new { p.UserId, p.Route.Grade })
                .ToListAsync();

            var participantRanks = bestGradePerUser
                .GroupBy(p => p.UserId)
                .Select(g => g.Max(p => GradeConversionService.Rank(p.Grade)))
                .Where(r => r >= 0)
                .ToList();

            string? avgGrade = null;
            if (participantRanks.Count > 0)
            {
                var avgRank     = (int)Math.Round(participantRanks.Average());
                var allGrades   = GradeConversionService.GetAllGrades();
                var frenchGrade = avgRank < allGrades.Count ? allGrades[avgRank] : null;
                avgGrade = GradeConversionService.Convert(frenchGrade, targetScale);
            }

            return Results.Ok(new
            {
                appointment.Id,
                appointment.AreaId,
                appointment.CreatedByUserId,
                appointment.Title,
                appointment.Date,
                appointment.MeetingPoint,
                appointment.Description,
                appointment.MinParticipants,
                appointment.MaxParticipants,
                ParticipantCount = appointment.AppointmentUsers.Count,
                AverageGrade     = avgGrade,
                Participants     = appointment.AppointmentUsers.Select(au => new
                {
                    au.UserId,
                    au.User.Username,
                    au.Comment
                })
            });
        })
        .WithName("GetAppointmentById")
        .WithTags("Appointments");

        app.MapPost("/api/areas/{id:int}/appointments", async (int id, AppointmentCreateDto dto, ClaimsPrincipal user, AppDbContext db) =>
        {
            if (!int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Results.Unauthorized();

            if (!await db.Areas.AnyAsync(a => a.Id == id)) return Results.NotFound();

            var currentUser = await db.Users.FindAsync(userId);
            if (currentUser is null) return Results.Unauthorized();

            if (string.IsNullOrWhiteSpace(dto.Title))
                return Results.BadRequest(new { error = "Titel ist erforderlich" });

            if (dto.MinParticipants.HasValue && dto.MaxParticipants.HasValue &&
                dto.MinParticipants.Value > dto.MaxParticipants.Value)
                return Results.BadRequest(new { error = "Minimale Teilnehmerzahl darf nicht größer als maximale Teilnehmerzahl sein" });

            var appointment = new Appointment
            {
                AreaId          = id,
                CreatedByUserId = userId,
                CreatedBy       = currentUser,
                Title           = dto.Title.Trim(),
                Date            = dto.Date,
                MeetingPoint    = string.IsNullOrWhiteSpace(dto.MeetingPoint) ? null : dto.MeetingPoint.Trim(),
                Description     = string.IsNullOrWhiteSpace(dto.Description)  ? null : dto.Description.Trim(),
                MinParticipants = dto.MinParticipants,
                MaxParticipants = dto.MaxParticipants
            };

            db.Appointments.Add(appointment);
            await db.SaveChangesAsync();
            return Results.Created($"/api/appointments/{appointment.Id}", appointment);
        })
        .WithName("CreateAppointment")
        .WithTags("Appointments")
        .RequireAuthorization("User");

        app.MapPut("/api/appointments/{id:int}", async (int id, AppointmentUpdateDto dto, ClaimsPrincipal user, AppDbContext db) =>
        {
            if (!int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Results.Unauthorized();

            var appointment = await db.Appointments.FindAsync(id);
            if (appointment is null) return Results.NotFound();

            // Nur Ersteller oder Admin darf bearbeiten
            if (appointment.CreatedByUserId != userId && !user.IsInRole("admin"))
                return Results.Forbid();

            if (string.IsNullOrWhiteSpace(dto.Title))
                return Results.BadRequest(new { error = "Titel ist erforderlich" });

            appointment.Title           = dto.Title.Trim();
            appointment.Date            = dto.Date;
            appointment.MeetingPoint    = string.IsNullOrWhiteSpace(dto.MeetingPoint)  ? null : dto.MeetingPoint.Trim();
            appointment.Description     = string.IsNullOrWhiteSpace(dto.Description)   ? null : dto.Description.Trim();
            appointment.MinParticipants = dto.MinParticipants;
            appointment.MaxParticipants = dto.MaxParticipants;

            await db.SaveChangesAsync();
            return Results.Ok(appointment);
        })
        .WithName("UpdateAppointment")
        .WithTags("Appointments")
        .RequireAuthorization("User");

        app.MapDelete("/api/appointments/{id:int}", async (int id, ClaimsPrincipal user, AppDbContext db) =>
        {
            if (!int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Results.Unauthorized();

            var appointment = await db.Appointments.FindAsync(id);
            if (appointment is null) return Results.NotFound();

            // Nur Ersteller oder Admin darf löschen
            if (appointment.CreatedByUserId != userId && !user.IsInRole("admin"))
                return Results.Forbid();

            db.Appointments.Remove(appointment);
            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithName("DeleteAppointment")
        .WithTags("Appointments")
        .RequireAuthorization("User");

        app.MapPost("/api/appointments/{id:int}/subscribe", async (int id, AppointmentSubscribeDto dto, ClaimsPrincipal user, AppDbContext db) =>
        {
            if (!int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Results.Unauthorized();

            var appointment = await db.Appointments
                .Include(a => a.AppointmentUsers)
                .FirstOrDefaultAsync(a => a.Id == id);
            if (appointment is null) return Results.NotFound();

            if (appointment.AppointmentUsers.Any(au => au.UserId == userId))
                return Results.Conflict(new { error = "Du nimmst bereits an diesem Termin teil" });

            if (appointment.MaxParticipants.HasValue &&
                appointment.AppointmentUsers.Count >= appointment.MaxParticipants.Value)
                return Results.BadRequest(new { error = "Termin ist bereits voll" });

            db.AppointmentUsers.Add(new AppointmentUser
            {
                AppointmentId = id,
                UserId        = userId,
                Comment       = string.IsNullOrWhiteSpace(dto.Comment) ? null : dto.Comment.Trim()
            });
            await db.SaveChangesAsync();
            return Results.Ok(new { message = "Erfolgreich beigetreten" });
        })
        .WithName("SubscribeToAppointment")
        .WithTags("Appointments")
        .RequireAuthorization("User");

        app.MapDelete("/api/appointments/{id:int}/subscribe", async (int id, ClaimsPrincipal user, AppDbContext db) =>
        {
            if (!int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Results.Unauthorized();

            var subscription = await db.AppointmentUsers
                .FirstOrDefaultAsync(au => au.AppointmentId == id && au.UserId == userId);
            if (subscription is null) return Results.NotFound();
            db.AppointmentUsers.Remove(subscription);
            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithName("UnsubscribeFromAppointment")
        .WithTags("Appointments")
        .RequireAuthorization("User");
    }
}
