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

        // ---- AREA 3: Weißensteinerwand ----
        var weissenstein = new Area
        {
            Name        = "Weißensteinerwand",
            Location    = "Voralpen bei Steyr, OÖ",
            Description = "Traditionelles Klettergebiet in den Voralpen bei Steyr. " +
                          "Fester Kalk, alpiner Charakter, teils lange Mehrseillängen-Routen.",
            ImageUrl    = "/assets/areas/weissensteinerwand.jpg",
            Latitude    = 47.919428,
            Longitude   = 14.347211
        };

        var weissensteinSektor1 = new Sector
        {
            Area        = weissenstein,
            Name        = "Vorbau",
            Description = "Kurze Einstiegsrouten, ideal zum Einklettern, 5a–6b"
        };
        var weissensteinSektor2 = new Sector
        {
            Area        = weissenstein,
            Name        = "Hauptwand",
            Description = "Anspruchsvolle Mehrseillängen, alpines Flair, 6a–7b"
        };

        db.Sectors.AddRange(weissensteinSektor1, weissensteinSektor2);

        db.Routes.AddRange(
            new Route { Sector = weissensteinSektor1, Name = "Einstieg leicht",       Grade = "5a",  LengthMeters = 15, Style = "Toprope",  Description = "Guter Warmup" },
            new Route { Sector = weissensteinSektor1, Name = "Reibungsplatte",        Grade = "5b+", LengthMeters = 18, Style = "Toprope",  Description = "Reibungskletterei auf Platte" },
            new Route { Sector = weissensteinSektor1, Name = "Steyrtal-Klassiker",    Grade = "5c",  LengthMeters = 20, Style = "Vorstieg", Description = "Beliebter Einstiegsklassiker" },
            new Route { Sector = weissensteinSektor1, Name = "Voralpenkante",         Grade = "6a",  LengthMeters = 22, Style = "Vorstieg", Description = "Schöne Kantenführung" },
            new Route { Sector = weissensteinSektor1, Name = "Erste Seillänge",       Grade = "6a+", LengthMeters = 24, Style = "Vorstieg", Description = "Einstieg in die Hauptwand" },

            new Route { Sector = weissensteinSektor2, Name = "Ausdauerriss",          Grade = "6b+", LengthMeters = 32, Style = "Vorstieg", Description = "Langer Riss, viel Ausdauer nötig" },
            new Route { Sector = weissensteinSektor2, Name = "Weißenstein-Verschneidung", Grade = "6c", LengthMeters = 35, Style = "Vorstieg", Description = "Klassische Verschneidung" },
            new Route { Sector = weissensteinSektor2, Name = "Steyrblick",            Grade = "6c+", LengthMeters = 38, Style = "Vorstieg", Description = "Aussicht ins Steyrtal" },
            new Route { Sector = weissensteinSektor2, Name = "Alpine Kante",          Grade = "7a",  LengthMeters = 40, Style = "Vorstieg", Description = "Alpine Mehrseillängenroute" },
            new Route { Sector = weissensteinSektor2, Name = "Voralpenprojekt",       Grade = "7b",  LengthMeters = 45, Style = "Vorstieg", Description = "Schwerstes Projekt der Wand" }
        );

        // ---- AREA 4: Klettergarten Steinbruch Dörnbach ----
        var doernbach = new Area
        {
            Name        = "Klettergarten Steinbruch Dörnbach",
            Location    = "Dörnbach bei Hörsching, OÖ",
            Description = "Ehemaliger Steinbruch nahe Hörsching, gut von Linz erreichbar. " +
                          "Kompakter Fels, viele kurze Sportkletterrouten.",
            ImageUrl    = "/assets/areas/steinbruch-doernbach.jpg",
            Latitude    = 48.28484,
            Longitude   = 14.21167
        };

        var doernbachSektor1 = new Sector
        {
            Area        = doernbach,
            Name        = "Alter Bruch",
            Description = "Sonnig, kompakter Fels, 4c–6b"
        };
        var doernbachSektor2 = new Sector
        {
            Area        = doernbach,
            Name        = "Neuer Bruch",
            Description = "Steilerer Abschnitt, technisch, 6b–7c"
        };

        db.Sectors.AddRange(doernbachSektor1, doernbachSektor2);

        db.Routes.AddRange(
            new Route { Sector = doernbachSektor1, Name = "Warmup Dörnbach",     Grade = "4c",  LengthMeters = 10, Style = "Toprope",  Description = "Einfache Aufwärmroute" },
            new Route { Sector = doernbachSektor1, Name = "Bruchkante",          Grade = "5a",  LengthMeters = 12, Style = "Vorstieg", Description = "Kante am Bruchrand" },
            new Route { Sector = doernbachSektor1, Name = "Steinbruch-Klassiker", Grade = "5c", LengthMeters = 15, Style = "Vorstieg", Description = "Der Klassiker im alten Bruch" },
            new Route { Sector = doernbachSektor1, Name = "Sonnenwand Dörnbach", Grade = "6a",  LengthMeters = 14, Style = "Vorstieg", Description = "Sonnige Wandflucht" },
            new Route { Sector = doernbachSektor1, Name = "Erstbegehung",        Grade = "6b",  LengthMeters = 16, Style = "Vorstieg", Description = "Technische Erstbegehung" },

            new Route { Sector = doernbachSektor2, Name = "Technikroute",        Grade = "6b+", LengthMeters = 14, Style = "Vorstieg", Description = "Feine Tritte, viel Technik" },
            new Route { Sector = doernbachSektor2, Name = "Steiler Zahn",        Grade = "6c",  LengthMeters = 18, Style = "Vorstieg", Description = "Steiler Zacken im Fels" },
            new Route { Sector = doernbachSektor2, Name = "Überhang Dörnbach",   Grade = "7a",  LengthMeters = 16, Style = "Vorstieg", Description = "Kurzer, kräftiger Überhang" },
            new Route { Sector = doernbachSektor2, Name = "Kraftakt",           Grade = "7b",  LengthMeters = 18, Style = "Vorstieg", Description = "Reine Kraftroute" },
            new Route { Sector = doernbachSektor2, Name = "Projektwand",        Grade = "7c",  LengthMeters = 20, Style = "Vorstieg", Description = "Schwerstes Projekt im Steinbruch" }
        );

        await db.SaveChangesAsync();
    }
}
