using System.Security.Claims;
using ClimbConnect.API.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;

namespace ClimbConnect.API.Services;

/// <summary>
/// Nimmt bei jeder Anfrage die aktuelle Rolle aus der Datenbank statt der Rolle im Token (#237).
///
/// Warum: Die Rolle steht beim Login im JWT, und der gilt 24 Stunden. Ohne diese Prüfung
/// hätte jemand, dem die Admin-Rechte entzogen wurden, mit seinem alten Token noch bis zu
/// einen Tag lang Admin-Zugriff. Umgekehrt wirkt ein neu vergebenes Admin-Recht sofort,
/// ohne neu anmelden. Tokens von gelöschten Konten werden abgelehnt.
/// Kosten: eine kleine Abfrage (nur die Rolle) pro Anfrage mit Token.
/// </summary>
public static class CurrentRoleCheck
{
    /// <summary>Für <see cref="JwtBearerEvents.OnTokenValidated"/>.</summary>
    public static async Task ApplyAsync(TokenValidatedContext context)
    {
        if (context.Principal?.Identity is not ClaimsIdentity identity ||
            !int.TryParse(identity.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
        {
            context.Fail("Token ohne gültige Benutzer-Id");
            return;
        }

        var db   = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
        var role = await db.Users.Where(u => u.Id == userId).Select(u => u.Role).FirstOrDefaultAsync();
        if (role is null)
        {
            context.Fail("Das Konto existiert nicht mehr");
            return;
        }

        // Rolle aus dem Token durch die aktuelle ersetzen
        foreach (var claim in identity.FindAll(identity.RoleClaimType).ToList())
            identity.RemoveClaim(claim);
        identity.AddClaim(new Claim(identity.RoleClaimType, role));
    }
}
