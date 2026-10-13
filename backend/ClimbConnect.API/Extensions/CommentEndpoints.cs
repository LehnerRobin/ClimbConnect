using System.Security.Claims;
using ClimbConnect.API.Data;
using ClimbConnect.API.Dtos;
using ClimbConnect.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ClimbConnect.API.Extensions;

/// <summary>Endpoints für Kommentare zu Gebieten und Routen.</summary>
public static class CommentEndpoints
{
    /// <summary>
    /// Wandelt Kommentare in das Ausgabeformat um. Vom Autor werden nur Id und Username
    /// übernommen, damit weder E-Mail noch Passwort-Hash die API verlassen.
    /// </summary>
    private static IQueryable<CommentDto> ToDto(this IQueryable<Comment> comments) =>
        comments.Select(c => new CommentDto(
            c.Id, c.UserId, c.AreaId, c.RouteId, c.Text, c.PhotoUrl, c.CreatedAtUtc,
            new CommentAuthorDto(c.User.Id, c.User.Username)));

    public static void MapCommentEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/areas/{id:int}/comments", async (int id, AppDbContext db) =>
        {
            if (!await db.Areas.AnyAsync(a => a.Id == id)) return Results.NotFound();
            var comments = await db.Comments
                .Where(c => c.AreaId == id)
                .OrderByDescending(c => c.CreatedAtUtc)
                .ToDto()
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

            var created = await db.Comments.Where(c => c.Id == comment.Id).ToDto().FirstAsync();
            return Results.Created($"/api/areas/{id}/comments", created);
        })
        .WithName("CreateCommentForArea")
        .WithTags("Comments")
        .RequireAuthorization("User");

        app.MapGet("/api/routes/{id:int}/comments", async (int id, AppDbContext db) =>
        {
            if (!await db.Routes.AnyAsync(r => r.Id == id)) return Results.NotFound();
            var comments = await db.Comments
                .Where(c => c.RouteId == id)
                .OrderByDescending(c => c.CreatedAtUtc)
                .ToDto()
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

            var created = await db.Comments.Where(c => c.Id == comment.Id).ToDto().FirstAsync();
            return Results.Created($"/api/routes/{id}/comments", created);
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
