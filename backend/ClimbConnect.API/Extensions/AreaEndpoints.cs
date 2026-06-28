using ClimbConnect.API.Data;
using ClimbConnect.API.Dtos;
using ClimbConnect.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ClimbConnect.API.Extensions;

/// <summary>Endpoints für Klettergebiete (Areas).</summary>
public static class AreaEndpoints
{
    public static void MapAreaEndpoints(this WebApplication app)
    {
        app.MapGet("/api/areas", async (string? search, int? page, int? pageSize, AppDbContext db) =>
        {
            var today    = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            var p  = Math.Max(1, page ?? 1);
            var ps = Math.Clamp(pageSize ?? 20, 1, 100);

            var query = db.Areas.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(a =>
                    a.Name.ToLower().Contains(term) ||
                    (a.Location != null && a.Location.ToLower().Contains(term)) ||
                    (a.Description != null && a.Description.ToLower().Contains(term)));
            }

            var total = await query.CountAsync();
            var areas = await query
                .OrderBy(a => a.Name)
                .Skip((p - 1) * ps)
                .Take(ps)
                .ToListAsync();

            var todayAppointments = await db.Appointments
                .Where(a => a.Date >= today && a.Date < tomorrow)
                .Include(a => a.AppointmentUsers)
                .ToListAsync();

            var result = areas.Select(a => new
            {
                a.Id, a.Name, a.Location, a.Description, a.ImageUrl, a.CreatedAtUtc,
                TodayVisitors     = todayAppointments.Where(ap => ap.AreaId == a.Id).Sum(ap => ap.AppointmentUsers.Count),
                TodayAppointments = todayAppointments.Count(ap => ap.AreaId == a.Id)
            });

            return Results.Ok(new { Total = total, Page = p, PageSize = ps, Items = result });
        })
        .WithName("GetAreas")
        .WithTags("Areas");

        app.MapGet("/api/areas/{id:int}", async (int id, AppDbContext db) =>
        {
            var area = await db.Areas
                .Include(a => a.Sectors)
                .FirstOrDefaultAsync(a => a.Id == id);
            return area is null ? Results.NotFound() : Results.Ok(area);
        })
        .WithName("GetAreaById")
        .WithTags("Areas");

        app.MapPost("/api/areas", async (AreaCreateDto dto, AppDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return Results.BadRequest(new { error = "Name ist erforderlich" });

            var area = new Area
            {
                Name        = dto.Name.Trim(),
                Location    = string.IsNullOrWhiteSpace(dto.Location)    ? null : dto.Location.Trim(),
                Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
                ImageUrl    = string.IsNullOrWhiteSpace(dto.ImageUrl)    ? null : dto.ImageUrl.Trim()
            };
            db.Areas.Add(area);
            await db.SaveChangesAsync();
            return Results.Created($"/api/areas/{area.Id}", area);
        })
        .WithName("CreateArea")
        .WithTags("Areas")
        .RequireAuthorization("Admin");

        app.MapPut("/api/areas/{id:int}", async (int id, AreaUpdateDto dto, AppDbContext db) =>
        {
            var area = await db.Areas.FindAsync(id);
            if (area is null) return Results.NotFound();
            if (string.IsNullOrWhiteSpace(dto.Name))
                return Results.BadRequest(new { error = "Name ist erforderlich" });

            area.Name        = dto.Name.Trim();
            area.Location    = string.IsNullOrWhiteSpace(dto.Location)    ? null : dto.Location.Trim();
            area.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
            area.ImageUrl    = string.IsNullOrWhiteSpace(dto.ImageUrl)    ? null : dto.ImageUrl.Trim();

            await db.SaveChangesAsync();
            return Results.Ok(area);
        })
        .WithName("UpdateArea")
        .WithTags("Areas")
        .RequireAuthorization("Admin");

        app.MapDelete("/api/areas/{id:int}", async (int id, AppDbContext db) =>
        {
            var area = await db.Areas.FindAsync(id);
            if (area is null) return Results.NotFound();
            db.Areas.Remove(area);
            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithName("DeleteArea")
        .WithTags("Areas")
        .RequireAuthorization("Admin");
    }
}
