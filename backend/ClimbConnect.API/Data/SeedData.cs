using ClimbConnect.API.Models;
using Microsoft.EntityFrameworkCore;
using Route = ClimbConnect.API.Models.Route;

namespace ClimbConnect.API.Data;

/// <summary>
/// Befüllt die Datenbank mit realistischen OÖ-Klettergebieten und Test-Usern.
/// Wird nur ausgeführt wenn die DB noch leer ist.
/// </summary>
public static class SeedData
{
    public static async Task InitAsync(AppDbContext db)
    {
        if (await db.Users.AnyAsync(u => u.Email == "admin@climbconnect.at")) return;

        // ---- USER ----
        var admin = new User
        {
            Username     = "admin",
            Email        = "admin@climbconnect.at",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin1234!"),
            Role         = "admin"
        };
        var testUser = new User
        {
            Username     = "testuser",
            Email        = "user@climbconnect.at",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("User1234!"),
            Role         = "user"
        };
        db.Users.AddRange(admin, testUser);

        // ---- AREA 1: Sandwand ----
        var sandwand = new Area
        {
            Name        = "Sandwand",
            Location    = "Kirchdorf an der Krems, OÖ",
            Description = "Beliebtes Klettergebiet in der Nähe von Kirchdorf. " +
                          "Kompakter Kalk, viele Routen im mittleren Schwierigkeitsbereich.",
            ImageUrl    = "/assets/areas/sandwand.jpg",
            Latitude    = 47.9022,
            Longitude   = 14.1272
        };

        var sandwandSektor1 = new Sector
        {
            Area        = sandwand,
            Name        = "Hauptwand",
            Description = "Südausrichtung, sonnig, Routen 5c–7b"
        };
        var sandwandSektor2 = new Sector
        {
            Area        = sandwand,
            Name        = "Linke Wand",
            Description = "Halbschatten, kühlere Routen, 6a–7a"
        };

        db.Sectors.AddRange(sandwandSektor1, sandwandSektor2);

        db.Routes.AddRange(
            new Route { Sector = sandwandSektor1, Name = "Einsteigerroute",    Grade = "5c",  LengthMeters = 18, Style = "Vorstieg",  Description = "Guter Einstieg für Anfänger" },
            new Route { Sector = sandwandSektor1, Name = "Sandwandverschneidung", Grade = "6a", LengthMeters = 22, Style = "Vorstieg",  Description = "Klassiker der Wand" },
            new Route { Sector = sandwandSektor1, Name = "Fingerkante",        Grade = "6c",  LengthMeters = 20, Style = "Vorstieg",  Description = "Technische Linie an der Kante" },
            new Route { Sector = sandwandSektor1, Name = "Überhang direkt",    Grade = "7a",  LengthMeters = 15, Style = "Vorstieg",  Description = "Kurzer, kräftiger Boulder-Abschnitt" },
            new Route { Sector = sandwandSektor1, Name = "Projektroute",       Grade = "7b+", LengthMeters = 25, Style = "Vorstieg",  Description = "Noch kein Rotpunkt bekannt" },

            new Route { Sector = sandwandSektor2, Name = "Linke Platte",       Grade = "6a",  LengthMeters = 20, Style = "Toprope",   Description = "Sehr guter Reibungskurs" },
            new Route { Sector = sandwandSektor2, Name = "Riss links",         Grade = "6b",  LengthMeters = 18, Style = "Vorstieg",  Description = "Spaltenkletterei, Handgröße" },
            new Route { Sector = sandwandSektor2, Name = "Tropfstein",         Grade = "6c+", LengthMeters = 22, Style = "Vorstieg",  Description = "Kleine Tropfsteinformen" },
            new Route { Sector = sandwandSektor2, Name = "Schatten-Ausdauer",  Grade = "7a",  LengthMeters = 30, Style = "Vorstieg",  Description = "Lange Ausdauerroute" },
            new Route { Sector = sandwandSektor2, Name = "Steilplatte",        Grade = "6b+", LengthMeters = 19, Style = "Vorstieg",  Description = "Technisch und steil" }
        );

        // ---- AREA 2: Steinwandklamm ----
        var steinwand = new Area
        {
            Name        = "Steinwandklamm",
            Location    = "Gaming, NÖ/OÖ-Grenze",
            Description = "Großartiges Gebiet an der Grenze zu NÖ. " +
                          "Gut erschlossene Wände mit vielen Linien von 5 bis 8a.",
            ImageUrl    = "/assets/areas/steinwandklamm.jpg",
            Latitude    = 47.8859,
            Longitude   = 15.1928
        };

        var steinwandSektor1 = new Sector
        {
            Area        = steinwand,
            Name        = "Eingangswand",
            Description = "Leicht zugänglich, ideal für Warmlaufen, 5a–6b"
        };
        var steinwandSektor2 = new Sector
        {
            Area        = steinwand,
            Name        = "Hauptturm",
            Description = "Steiler Turm mit anspruchsvollen Linien, 7a–8a"
        };

        db.Sectors.AddRange(steinwandSektor1, steinwandSektor2);

        db.Routes.AddRange(
            new Route { Sector = steinwandSektor1, Name = "Kinderroute",        Grade = "5a",  LengthMeters = 12, Style = "Toprope",  Description = "Perfekt für Kinder und Anfänger" },
            new Route { Sector = steinwandSektor1, Name = "Leichte Platte",     Grade = "5c",  LengthMeters = 15, Style = "Toprope",  Description = "Gute Reibungsübung" },
            new Route { Sector = steinwandSektor1, Name = "Klamm-Klassiker",    Grade = "6a+", LengthMeters = 20, Style = "Vorstieg", Description = "Der Einstiegsklassiker des Gebiets" },
            new Route { Sector = steinwandSektor1, Name = "Wanderer",           Grade = "6b",  LengthMeters = 22, Style = "Vorstieg", Description = "Schöne Linienführung, empfehlenswert" },
            new Route { Sector = steinwandSektor1, Name = "Genussroute",        Grade = "6a",  LengthMeters = 18, Style = "Vorstieg", Description = "Immer wieder schön" },

            new Route { Sector = steinwandSektor2, Name = "Turmroute",          Grade = "7a+", LengthMeters = 28, Style = "Vorstieg", Description = "Langer Weg zum Gipfel des Turms" },
            new Route { Sector = steinwandSektor2, Name = "Überhang King",      Grade = "7c",  LengthMeters = 20, Style = "Vorstieg", Description = "Kraftroute an der Überhangzone" },
            new Route { Sector = steinwandSektor2, Name = "Fingerboard",        Grade = "7b",  LengthMeters = 16, Style = "Vorstieg", Description = "Kleine Griffe, viel Kraft" },
            new Route { Sector = steinwandSektor2, Name = "8a Projekt",         Grade = "8a",  LengthMeters = 25, Style = "Vorstieg", Description = "Schwerstes Projekt im Gebiet" },
            new Route { Sector = steinwandSektor2, Name = "Mittelweg Turm",     Grade = "7a",  LengthMeters = 22, Style = "Vorstieg", Description = "Guter Einstieg in die 7er-Welt" }
        );

        // ---- AREA 3: Dürnsteiner Wand ----
        var durnstein = new Area
        {
            Name        = "Dürnsteiner Wand",
            Location    = "Dürnstein, OÖ",
            Description = "Sonnige Kalkwände nahe Dürnstein. " +
                          "Mittlere Schwierigkeiten, familienfreundliches Ambiente.",
            ImageUrl    = "/assets/areas/duernstein.jpg",
            Latitude    = 48.3986,
            Longitude   = 15.5218
        };

        var durnsteinSektor1 = new Sector
        {
            Area        = durnstein,
            Name        = "Sonnenseite",
            Description = "Ganztags Sonne, trocknet schnell, 5b–7a"
        };
        var durnsteinSektor2 = new Sector
        {
            Area        = durnstein,
            Name        = "Schattenwand",
            Description = "Ideal im Sommer, 6a–7b"
        };

        db.Sectors.AddRange(durnsteinSektor1, durnsteinSektor2);

        db.Routes.AddRange(
            new Route { Sector = durnsteinSektor1, Name = "Morgensonne",        Grade = "5b",  LengthMeters = 16, Style = "Toprope",  Description = "Warmup-Route" },
            new Route { Sector = durnsteinSektor1, Name = "Sonnenplatte",       Grade = "6a",  LengthMeters = 18, Style = "Vorstieg", Description = "Schöne Reibungsplatte" },
            new Route { Sector = durnsteinSektor1, Name = "Risse",              Grade = "6b+", LengthMeters = 20, Style = "Vorstieg", Description = "Risskletterei, OÖ-Stil" },
            new Route { Sector = durnsteinSektor1, Name = "Sonnenschein",       Grade = "6c",  LengthMeters = 22, Style = "Vorstieg", Description = "Kompakte Züge" },
            new Route { Sector = durnsteinSektor1, Name = "Dürnstein-Direttissima", Grade = "7a", LengthMeters = 30, Style = "Vorstieg", Description = "Direkter Weg durch die Wand" },

            new Route { Sector = durnsteinSektor2, Name = "Schattenläufer",    Grade = "6a+", LengthMeters = 20, Style = "Vorstieg", Description = "Kühl und angenehm im Sommer" },
            new Route { Sector = durnsteinSektor2, Name = "Kühle Kante",       Grade = "6b",  LengthMeters = 18, Style = "Vorstieg", Description = "Kantenkletterei" },
            new Route { Sector = durnsteinSektor2, Name = "Schattenriß",       Grade = "6c+", LengthMeters = 24, Style = "Vorstieg", Description = "Langer Riss im Schatten" },
            new Route { Sector = durnsteinSektor2, Name = "Abend-Ausdauer",    Grade = "7a+", LengthMeters = 32, Style = "Vorstieg", Description = "Für Ausdauerfans" },
            new Route { Sector = durnsteinSektor2, Name = "Schattenprojekt",   Grade = "7b",  LengthMeters = 26, Style = "Vorstieg", Description = "Klassisches Sommer-Projekt" }
        );

        await db.SaveChangesAsync();
    }
}
