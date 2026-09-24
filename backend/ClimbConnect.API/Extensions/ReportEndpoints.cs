using System.Security.Claims;
using ClimbConnect.API.Data;
using ClimbConnect.API.Dtos;
using ClimbConnect.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ClimbConnect.API.Extensions;

/// <summary>Endpoints für Safety-Reports (Sicherheitsmeldungen).</summary>
public static class ReportEndpoints
{
    public static void MapReportEndpoints(this WebApplication app)
    {
        app.MapPost("/api/reports", async (ReportCreateDto dto, ClaimsPrincipal user, AppDbContext db) =>
        {
            if (!int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Results.Unauthorized();

            var validSeverities = new[] { "Low", "Medium", "High" };
            if (!validSeverities.Contains(dto.Severity))
                return Results.BadRequest(new { error = "Severity muss Low, Medium oder High sein" });
            if (string.IsNullOrWhiteSpace(dto.Text))
                return Results.BadRequest(new { error = "Text ist erforderlich" });
            if (dto.AreaId is null && dto.RouteId is null)
                return Results.BadRequest(new { error = "AreaId oder RouteId muss angegeben werden" });
            if (dto.AreaId is not null && !await db.Areas.AnyAsync(a => a.Id == dto.AreaId))
                return Results.BadRequest(new { error = "Area nicht gefunden" });
            if (dto.RouteId is not null && !await db.Routes.AnyAsync(r => r.Id == dto.RouteId))
                return Results.BadRequest(new { error = "Route nicht gefunden" });

            var report = new Report
            {
                UserId   = userId,
                AreaId   = dto.AreaId,
                RouteId  = dto.RouteId,
                Text     = dto.Text.Trim(),
                PhotoUrl = string.IsNullOrWhiteSpace(dto.PhotoUrl) ? null : dto.PhotoUrl.Trim(),
                Severity = dto.Severity,
                Status   = "Open"
            };
            db.Reports.Add(report);
            await db.SaveChangesAsync();
            return Results.Created($"/api/reports/{report.Id}", report);
        })
        .WithName("CreateReport")
        .WithTags("Reports")
        .RequireAuthorization("User");

        // Alle Reports: nur für Admin
        app.MapGet("/api/reports", async (AppDbContext db) =>
        {
            var reports = await db.Reports
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedAtUtc)
                .ToListAsync();
            return Results.Ok(reports);
        })
        .WithName("GetReports")
        .WithTags("Reports")
        .RequireAuthorization("Admin");

        app.MapPut("/api/reports/{id:int}/status", async (int id, ReportStatusUpdateDto dto, AppDbContext db) =>
        {
            var report = await db.Reports.FindAsync(id);
            if (report is null) return Results.NotFound();
            if (!new[] { "Open", "Resolved" }.Contains(dto.Status))
                return Results.BadRequest(new { error = "Status muss 'Open' oder 'Resolved' sein" });
            report.Status = dto.Status;
            await db.SaveChangesAsync();
            return Results.Ok(report);
        })
        .WithName("UpdateReportStatus")
        .WithTags("Reports")
        .RequireAuthorization("Admin");

        // Offene Reports zu einem Gebiet (für alle sichtbar)
        app.MapGet("/api/areas/{id:int}/reports", async (int id, AppDbContext db) =>
        {
            if (!await db.Areas.AnyAsync(a => a.Id == id)) return Results.NotFound();
            var reports = await db.Reports
                .Where(r => r.AreaId == id && r.Status == "Open")
                .OrderByDescending(r => r.CreatedAtUtc)
                .Select(r => new { r.Id, r.Text, r.Severity, r.PhotoUrl, r.CreatedAtUtc })
                .ToListAsync();
            return Results.Ok(reports);
        })
        .WithName("GetReportsByArea")
        .WithTags("Reports");

        // Offene Reports zu einer Route (für alle sichtbar)
        app.MapGet("/api/routes/{id:int}/reports", async (int id, AppDbContext db) =>
        {
            if (!await db.Routes.AnyAsync(r => r.Id == id)) return Results.NotFound();
            var reports = await db.Reports
                .Where(r => r.RouteId == id && r.Status == "Open")
                .OrderByDescending(r => r.CreatedAtUtc)
                .Select(r => new { r.Id, r.Text, r.Severity, r.PhotoUrl, r.CreatedAtUtc })
                .ToListAsync();
            return Results.Ok(reports);
        })
        .WithName("GetReportsByRoute")
        .WithTags("Reports");
    }
}
