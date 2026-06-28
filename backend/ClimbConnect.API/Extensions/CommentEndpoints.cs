using System.Security.Claims;
using ClimbConnect.API.Data;
using ClimbConnect.API.Dtos;
using ClimbConnect.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ClimbConnect.API.Extensions;

/// <summary>Endpoints für Kommentare zu Gebieten und Routen.</summary>
public static class CommentEndpoints
{
    public static void MapCommentEndpoints(this WebApplication app)
    {
        app.MapGet("/api/areas/{id:int}/comments", async (int id, AppDbContext db) =>
        {
            if (!await db.Areas.AnyAsync(a => a.Id == id)) return Results.NotFound();
            var comments = await db.Comments
                .Where(c => c.AreaId == id)
                .Include(c => c.User)
                .OrderByDescending(c => c.CreatedAtUtc)
                .ToListAsync();
            return Results.Ok(comments);
        })
        .WithName("GetCommentsByArea")
        .WithTags("Comments");

        app.MapPost("/api/areas/{id:int}/comments", async (int id, CommentCreateDto dto, ClaimsPrincipal user, AppDbContext db) =>
        {
            if (!int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Results.Unauthorized();
            if (!await db.Areas.AnyAsync(a => a.Id == id)) return Results.NotFound();
            if (string.IsNullOrWhiteSpace(dto.Text))
                return Results.BadRequest(new { error = "Text ist erforderlich" });

            var comment = new Comment
            {
                UserId   = userId,
                AreaId   = id,
                Text     = dto.Text.Trim(),
                PhotoUrl = string.IsNullOrWhiteSpace(dto.PhotoUrl) ? null : dto.PhotoUrl.Trim()
            };
            db.Comments.Add(comment);
            await db.SaveChangesAsync();
            return Results.Created($"/api/areas/{id}/comments", comment);
        })
        .WithName("CreateCommentForArea")
        .WithTags("Comments")
        .RequireAuthorization("User");

        app.MapGet("/api/routes/{id:int}/comments", async (int id, AppDbContext db) =>
        {
            if (!await db.Routes.AnyAsync(r => r.Id == id)) return Results.NotFound();
            var comments = await db.Comments
                .Where(c => c.RouteId == id)
                .Include(c => c.User)
                .OrderByDescending(c => c.CreatedAtUtc)
                .ToListAsync();
            return Results.Ok(comments);
        })
        .WithName("GetCommentsByRoute")
        .WithTags("Comments");

        app.MapPost("/api/routes/{id:int}/comments", async (int id, CommentCreateDto dto, ClaimsPrincipal user, AppDbContext db) =>
        {
            if (!int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Results.Unauthorized();
            if (!await db.Routes.AnyAsync(r => r.Id == id)) return Results.NotFound();
            if (string.IsNullOrWhiteSpace(dto.Text))
                return Results.BadRequest(new { error = "Text ist erforderlich" });

            var comment = new Comment
            {
                UserId   = userId,
                RouteId  = id,
                Text     = dto.Text.Trim(),
                PhotoUrl = string.IsNullOrWhiteSpace(dto.PhotoUrl) ? null : dto.PhotoUrl.Trim()
            };
            db.Comments.Add(comment);
            await db.SaveChangesAsync();
            return Results.Created($"/api/routes/{id}/comments", comment);
        })
        .WithName("CreateCommentForRoute")
        .WithTags("Comments")
        .RequireAuthorization("User");

        app.MapDelete("/api/comments/{id:int}", async (int id, ClaimsPrincipal user, AppDbContext db) =>
        {
            if (!int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Results.Unauthorized();

            var comment = await db.Comments.FindAsync(id);
            if (comment is null) return Results.NotFound();
            if (comment.UserId != userId) return Results.Forbid();

            db.Comments.Remove(comment);
            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithName("DeleteComment")
        .WithTags("Comments")
        .RequireAuthorization("User");
    }
}
