using ClimbConnect.API.Data;
using ClimbConnect.API.Dtos;
using ClimbConnect.API.Models;
using ClimbConnect.API.Services;
using Microsoft.EntityFrameworkCore;

namespace ClimbConnect.API.Extensions;

/// <summary>Registrierungs- und Login-Endpoints.</summary>
public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/api/auth/register", async (RegisterDto dto, AppDbContext db, JwtService jwt) =>
        {
            if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
                return Results.BadRequest(new { error = "Username, Email und Passwort sind erforderlich" });

            if (dto.Password.Length < 8)
                return Results.BadRequest(new { error = "Passwort muss mindestens 8 Zeichen lang sein" });

            if (await db.Users.AnyAsync(u => u.Email == dto.Email.ToLower()))
                return Results.Conflict(new { error = "E-Mail bereits vergeben" });

            if (await db.Users.AnyAsync(u => u.Username == dto.Username))
                return Results.Conflict(new { error = "Username bereits vergeben" });

            var user = new User
            {
                Username     = dto.Username.Trim(),
                Email        = dto.Email.ToLower().Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role         = "user"
            };

            db.Users.Add(user);
            await db.SaveChangesAsync();

            return Results.Ok(new { token = jwt.GenerateToken(user), user.Id, user.Username, user.Email, user.Role });
        })
        .WithName("Register")
        .WithTags("Auth");

        app.MapPost("/api/auth/login", async (LoginDto dto, AppDbContext db, JwtService jwt) =>
        {
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
                return Results.BadRequest(new { error = "E-Mail und Passwort sind erforderlich" });

            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email.ToLower());
            if (user is null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return Results.Unauthorized();

            return Results.Ok(new { token = jwt.GenerateToken(user), user.Id, user.Username, user.Email, user.Role });
        })
        .WithName("Login")
        .WithTags("Auth");
    }
}
