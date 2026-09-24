using ClimbConnect.API.Data;
using ClimbConnect.API.Dtos;
using ClimbConnect.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ClimbConnect.API.Extensions;

/// <summary>Endpoints für Sektoren innerhalb eines Klettergebiets.</summary>
public static class SectorEndpoints
{
    public static void MapSectorEndpoints(this WebApplication app)
    {
        app.MapGet("/api/areas/{id:int}/sectors", async (int id, AppDbContext db) =>
        {
            if (!await db.Areas.AnyAsync(a => a.Id == id)) return Results.NotFound();

            var sectors = await db.Sectors
                .Where(s => s.AreaId == id)
                .OrderBy(s => s.Name)
                .ToListAsync();

            return Results.Ok(sectors);
        })
        .WithName("GetSectorsByArea")
        .WithTags("Sectors");

        app.MapGet("/api/sectors/{id:int}", async (int id, AppDbContext db) =>
        {
            var sector = await db.Sectors
                .Include(s => s.Routes)
                .FirstOrDefaultAsync(s => s.Id == id);
            return sector is null ? Results.NotFound() : Results.Ok(sector);
        })
        .WithName("GetSectorById")
        .WithTags("Sectors");

        app.MapPost("/api/areas/{id:int}/sectors", async (int id, SectorCreateDto dto, AppDbContext db) =>
        {
            if (!await db.Areas.AnyAsync(a => a.Id == id)) return Results.NotFound();
            if (string.IsNullOrWhiteSpace(dto.Name))
                return Results.BadRequest(new { error = "Name ist erforderlich" });

            var sector = new Sector
            {
                AreaId      = id,
                Name        = dto.Name.Trim(),
                Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim()
            };
            db.Sectors.Add(sector);
            await db.SaveChangesAsync();
            return Results.Created($"/api/sectors/{sector.Id}", sector);
        })
        .WithName("CreateSector")
        .WithTags("Sectors")
        .RequireAuthorization("Admin");

        app.MapPut("/api/sectors/{id:int}", async (int id, SectorUpdateDto dto, AppDbContext db) =>
        {
            var sector = await db.Sectors.FindAsync(id);
            if (sector is null) return Results.NotFound();
            if (string.IsNullOrWhiteSpace(dto.Name))
                return Results.BadRequest(new { error = "Name ist erforderlich" });

            sector.Name        = dto.Name.Trim();
            sector.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();

            await db.SaveChangesAsync();
            return Results.Ok(sector);
        })
        .WithName("UpdateSector")
        .WithTags("Sectors")
        .RequireAuthorization("Admin");

        app.MapDelete("/api/sectors/{id:int}", async (int id, AppDbContext db) =>
        {
            var sector = await db.Sectors.FindAsync(id);
            if (sector is null) return Results.NotFound();
            db.Sectors.Remove(sector);
            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithName("DeleteSector")
        .WithTags("Sectors")
        .RequireAuthorization("Admin");
    }
}
