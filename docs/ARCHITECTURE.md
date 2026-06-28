# Architektur – ClimbConnect

## Systemübersicht

ClimbConnect ist eine Web-App für die Klettergemeinschaft in Oberösterreich.
Benutzer können Klettergebiete, Sektoren und Routen einsehen, eigene Begehungen
eintragen, Termine planen und Kommentare hinterlassen.

```
┌──────────────────────────────────────────────────────────────────┐
│                        Browser (Client)                          │
│                   Angular 20 (TypeScript, SPA)                   │
│  ┌────────────┐  ┌────────────┐  ┌──────────────┐  ┌─────────┐ │
│  │  Areas /   │  │  Progress  │  │ Appointments │  │  Admin  │ │
│  │  Routes    │  │  Profile   │  │  Climbers    │  │  Panel  │ │
│  └────────────┘  └────────────┘  └──────────────┘  └─────────┘ │
│           │             JWT-Token (localStorage)                  │
└───────────┼──────────────────────────────────────────────────────┘
            │ HTTP/REST (JSON)
┌───────────▼──────────────────────────────────────────────────────┐
│                    .NET 8 Minimal API (Backend)                   │
│  ┌──────────────────────────────────────────────────────────────┐ │
│  │  AuthEndpoints  AreaEndpoints  RouteEndpoints  ...           │ │
│  │  ProgressEndpoints  AppointmentEndpoints  CommentEndpoints   │ │
│  │  ReportEndpoints  UserEndpoints  UploadEndpoints             │ │
│  └──────────────────────────────────────────────────────────────┘ │
│  ┌──────────────────┐  ┌────────────────────────────────────────┐ │
│  │  AppDbContext    │  │  GradeConversionService                │ │
│  │  (EF Core 8)     │  │  (Franz. → UIAA → Amerikanisch)       │ │
│  └──────────────────┘  └────────────────────────────────────────┘ │
└───────────┬──────────────────────────────────────────────────────┘
            │ EF Core (SQLite)
┌───────────▼──────────────────────────────────────────────────────┐
│                       SQLite-Datenbank                            │
│  Users  Areas  Sectors  Routes  Progresses  Appointments         │
│  AppointmentUsers  Comments  Reports                             │
└──────────────────────────────────────────────────────────────────┘
```

## Stack

| Schicht      | Technologie                                 |
|-------------|---------------------------------------------|
| Frontend    | Angular 20, TypeScript, Standalone Components |
| Backend     | .NET 8 Minimal API, C#                      |
| ORM         | Entity Framework Core 8                     |
| Datenbank   | SQLite (Entwicklung & Produktion)           |
| Auth        | JWT (eigenes System, HS256)                 |
| Tests       | xUnit, Microsoft.AspNetCore.Mvc.Testing, EF InMemory |
| CI/CD       | GitHub Actions                              |

## Backend-Struktur

```
backend/ClimbConnect.API/
├── Data/
│   └── AppDbContext.cs          – EF Core DbContext, 9 DbSets
├── Dtos/                        – Request-Payloads (Records mit DataAnnotations)
├── Extensions/                  – Endpoint-Klassen (je eine Datei pro Ressource)
│   ├── AuthEndpoints.cs         – POST /register, POST /login
│   ├── AreaEndpoints.cs         – CRUD /api/areas (Admin: schreiben)
│   ├── SectorEndpoints.cs       – CRUD /api/sectors
│   ├── RouteEndpoints.cs        – CRUD /api/routes, Community-Grad
│   ├── ProgressEndpoints.cs     – CRUD /api/progress (User: eigene)
│   ├── AppointmentEndpoints.cs  – CRUD /api/appointments, subscribe/unsubscribe
│   ├── CommentEndpoints.cs      – Kommentare zu Areas und Routen
│   ├── ReportEndpoints.cs       – Safety Reports (User erstellt, Admin löst auf)
│   ├── UserEndpoints.cs         – Profil, Statistiken, Passwort, öffentliche Profile
│   └── UploadEndpoints.cs       – POST /api/upload (Bild-Upload)
├── Migrations/                  – EF Core Migrationen
├── Models/                      – Entitäts-Klassen
├── Services/
│   └── GradeConversionService.cs – Grad-Konversion: Französisch ↔ UIAA ↔ Amerikanisch
└── Program.cs                   – App-Konfiguration, Middleware, JWT, Swagger
```

## Frontend-Struktur

```
frontend/src/
├── app/
│   ├── admin/                   – Admin-Panel (Gebiete, Sektoren, Routen, Reports)
│   ├── features/
│   │   ├── areas/               – Gebietsliste + Area-Detailseite
│   │   ├── appointments/        – Termin-Formular
│   │   ├── climbers/            – Benutzer suchen
│   │   ├── auth/                – Login + Registrierung
│   │   ├── home/                – Startseite
│   │   ├── progress/            – Begehung eintragen/bearbeiten
│   │   ├── routes/              – Route-Detailseite
│   │   └── users/               – Öffentliches Profil
│   ├── guards/
│   │   └── auth-role.guard.ts   – Routen-Guard (eingeloggt, Admin)
│   ├── interceptors/
│   │   ├── jwt.interceptor.ts   – Fügt Bearer-Token zu jedem Request hinzu
│   │   └── error.interceptor.ts – 401 → /login, 403 → /forbidden
│   ├── menu/                    – Navigationsleiste
│   └── user-profile/            – Eigenes Profil, Statistiken, Passwort
├── services/
│   ├── areas.service.ts         – HTTP-Calls für Areas, Routes, Progress, Comments, Reports
│   ├── auth.service.ts          – Login/Logout, JWT-Parsing, Rollenverwaltung
│   ├── admin.service.ts         – Admin-spezifische HTTP-Calls
│   ├── favorites.service.ts     – Favoriten-Verwaltung (localStorage)
│   └── user.service.ts          – Profil, Statistiken, Passwort
└── environments/                – API-URL pro Umgebung
```

## Rollen

| Rolle   | Kann                                                               |
|---------|--------------------------------------------------------------------|
| `admin` | Gebiete/Sektoren/Routen anlegen, bearbeiten, löschen; Reports verwalten |
| `user`  | Lesen, Fortschritt eintragen, Kommentare, Termine erstellen/beitreten |

Rollen werden beim Login als JWT-Claim (`role`) gesetzt und vom Backend bei jedem
Request über `.RequireAuthorization("Admin")` / `.RequireAuthorization("User")` geprüft.

## Authentifizierung

```
Browser                          Backend
  │                                │
  │  POST /api/auth/login           │
  │  { email, password }  ────────► │
  │                                 │  BCrypt.Verify → JWT erzeugen
  │  { token: "eyJ..." }  ◄──────── │
  │                                 │
  │  GET /api/areas                 │
  │  Authorization: Bearer eyJ... ► │
  │                                 │  JWT validieren → userId aus Claim
  │  { total, page, items: [...] }  │
  │  ◄────────────────────────────  │
```

## Grad-Konversion

Routen werden intern in der **französischen Skala** gespeichert (z. B. `6a`, `7b+`).
Beim Abrufen wird über `?scale=french|uiaa|american` die gewünschte Ausgabeskala übergeben.

`GradeConversionService` enthält eine geordnete Liste aller Französischen Grade und
Mappings zu UIAA und Amerikanisch. Der Community-Grad berechnet sich aus dem
Durchschnitt aller subjektiven Gradbewertungen der Benutzer.

## Paginierung

Listen-Endpoints (`GET /api/areas`, `GET /api/routes`, `GET /api/progress/me`)
unterstützen `?page=1&pageSize=20` und geben zurück:

```json
{
  "total": 42,
  "page": 1,
  "pageSize": 20,
  "items": [...]
}
```

## Integrationstests

Das Projekt `backend/ClimbConnect.Tests` enthält xUnit-Tests mit einer
In-Memory-SQLite-Datenbank pro Testklasse. Testabdeckung:
- `AuthTests` – Register, Login, Validierung
- `AreaTests` – CRUD, Authentifizierung, Paginierung
- `ProgressTests` – Begehungen, Eigentümerschutz
