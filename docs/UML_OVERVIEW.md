# UML-Diagramme – ClimbConnect

Diese Datei enthält:

1. [Use-Case-Diagramm](#1-use-case-diagramm) – wer kann was im System?
2. [Klassendiagramm](#2-klassendiagramm-domain-model) – Entity-Klassen aus `backend/ClimbConnect.API/Models`
3. [Klassendiagramm Backend-Struktur](#3-klassendiagramm-backend-struktur) – Endpoints, Services, DbContext
4. [Sequenzdiagramm](#4-sequenzdiagramm-einem-termin-beitreten) – Ablauf „Einem Termin beitreten“

Alle Diagramme sind in Mermaid geschrieben und werden direkt auf GitHub gerendert.

---

## 1. Use-Case-Diagramm

Akteure:
- **Besucher:in**: nicht eingeloggt
- **User**: eingeloggt, erbt alle Rechte des Besuchers
- **Admin**: erbt alle Rechte des Users

```mermaid
flowchart LR
    Besucher(["👤 Besucher:in"])
    User(["👤 User"])
    Admin(["👤 Admin"])

    User -. erbt .-> Besucher
    Admin -. erbt .-> User

    subgraph ClimbConnect
        direction TB
        UC1(("Registrieren / Einloggen"))
        UC2(("Gebiete, Sektoren &<br/>Routen ansehen"))
        UC3(("Gebiete auf Karte ansehen"))
        UC4(("Öffentliches Profil &<br/>Statistiken ansehen"))

        UC5(("Fortschritt pro Route<br/>erfassen / bearbeiten"))
        UC6(("Eigene Statistiken ansehen"))
        UC7(("Termin erstellen"))
        UC8(("Termin beitreten / austreten"))
        UC9(("Kommentar mit Foto schreiben"))
        UC10(("Safety-Report erstellen"))
        UC11(("Profil & Passwort ändern"))

        UC12(("Gebiete, Sektoren & Routen<br/>anlegen / bearbeiten / löschen"))
        UC13(("Safety-Reports bearbeiten<br/>(Open → Resolved)"))
    end

    Besucher --> UC1
    Besucher --> UC2
    Besucher --> UC3
    Besucher --> UC4

    User --> UC5
    User --> UC6
    User --> UC7
    User --> UC8
    User --> UC9
    User --> UC10
    User --> UC11

    Admin --> UC12
    Admin --> UC13
```

| Use Case | Akteur | API-Endpunkt(e) |
|---|---|---|
| Registrieren / Einloggen | Besucher:in | `POST /api/auth/register`, `POST /api/auth/login` |
| Gebiete, Sektoren & Routen ansehen | Besucher:in | `GET /api/areas`, `GET /api/areas/{id}/sectors`, `GET /api/sectors/{id}/routes`, `GET /api/routes/{id}` |
| Öffentliches Profil ansehen | Besucher:in | `GET /api/users/{id}/profile`, `GET /api/users/{id}/stats` |
| Fortschritt erfassen | User | `POST/PUT/DELETE /api/progress`, `GET /api/progress/me` |
| Termin erstellen | User | `POST /api/areas/{id}/appointments` |
| Termin beitreten / austreten | User | `POST/DELETE /api/appointments/{id}/subscribe` |
| Kommentar schreiben | User | `POST /api/areas/{id}/comments`, `POST /api/routes/{id}/comments`, `POST /api/upload` |
| Safety-Report erstellen | User | `POST /api/reports` |
| Profil & Passwort ändern | User | `PUT /api/users/me/profile`, `PUT /api/users/me/password` |
| Gebiete/Sektoren/Routen verwalten | Admin | `POST/PUT/DELETE` auf `/api/areas`, `/api/sectors`, `/api/routes` |
| Safety-Reports bearbeiten | Admin | `GET /api/reports`, `PUT /api/reports/{id}/status` |

---

## 2. Klassendiagramm (Domain Model)

```mermaid
classDiagram
    direction LR

    class User {
        +int Id
        +string Email
        +string Username
        +string PasswordHash
        +string Role
        +string? Bio
        +string? PreferredGradeScale
        +DateTime CreatedAtUtc
    }
    class Area {
        +int Id
        +string Name
        +string? Location
        +string? Description
        +string? ImageUrl
        +double? Latitude
        +double? Longitude
        +DateTime CreatedAtUtc
    }
    class Sector {
        +int Id
        +int AreaId
        +string Name
        +string? Description
        +DateTime CreatedAtUtc
    }
    class Route {
        +int Id
        +int SectorId
        +string Name
        +string? Grade
        +int? LengthMeters
        +string? Style
        +string? Description
        +DateTime CreatedAtUtc
    }
    class Progress {
        +int Id
        +int UserId
        +int RouteId
        +string Status
        +string ClimbingStyle
        +int Attempts
        +DateOnly Date
        +string? Notes
        +string? SubjectiveGrade
        +string? SubjectiveGradeComment
        +DateTime CreatedAtUtc
    }
    class Appointment {
        +int Id
        +int AreaId
        +int CreatedByUserId
        +string Title
        +DateTime Date
        +string? MeetingPoint
        +string? Description
        +int? MinParticipants
        +int? MaxParticipants
        +DateTime CreatedAtUtc
    }
    class AppointmentUser {
        +int AppointmentId
        +int UserId
        +string? Comment
        +DateTime JoinedAtUtc
    }
    class Comment {
        +int Id
        +int UserId
        +int? AreaId
        +int? RouteId
        +string Text
        +string? PhotoUrl
        +DateTime CreatedAtUtc
    }
    class Report {
        +int Id
        +int UserId
        +int? AreaId
        +int? RouteId
        +string Text
        +string? PhotoUrl
        +string Severity
        +string Status
        +DateTime CreatedAtUtc
    }
    class ProgressConst {
        <<static>>
        +string[] Statuses$
        +string[] Styles$
    }

    Area "1" *-- "0..*" Sector : Sectors
    Sector "1" *-- "0..*" Route : Routes
    Area "1" -- "0..*" Appointment : Appointments
    User "1" -- "0..*" Appointment : CreatedAppointments
    Appointment "1" *-- "0..*" AppointmentUser : AppointmentUsers
    User "1" -- "0..*" AppointmentUser : AppointmentUsers
    User "1" -- "0..*" Progress : Progresses
    Route "1" -- "0..*" Progress : Progresses
    User "1" -- "0..*" Comment : Comments
    Area "0..1" -- "0..*" Comment : Comments
    Route "0..1" -- "0..*" Comment : Comments
    User "1" -- "0..*" Report : Reports
    Area "0..1" -- "0..*" Report : Reports
    Route "0..1" -- "0..*" Report : Reports
    ProgressConst <.. Progress : Status/ClimbingStyle validiert in ProgressEndpoints
```

**Hinweise**
- Komposition (◆) bei `Area → Sector → Route` und `Appointment → AppointmentUser`: Wird das Ganze gelöscht, verschwinden die Teile mit.
- `Comment` und `Report` hängen **entweder** an einem Gebiet **oder** an einer Route (daher `0..1`).

---

## 3. Klassendiagramm Backend-Struktur

Aufbau der .NET 8 Minimal API: Jede Endpoint-Gruppe ist eine statische Extension-Klasse, die in `Program.cs` registriert wird.

```mermaid
classDiagram
    direction TB

    class Program {
        <<entry point>>
    }
    class AppDbContext {
        +DbSet~User~ Users
        +DbSet~Area~ Areas
        +DbSet~Sector~ Sectors
        +DbSet~Route~ Routes
        +DbSet~Progress~ Progresses
        +DbSet~Appointment~ Appointments
        +DbSet~AppointmentUser~ AppointmentUsers
        +DbSet~Comment~ Comments
        +DbSet~Report~ Reports
        #OnModelCreating(ModelBuilder)
    }
    class SeedData {
        <<static>>
        +InitAsync(AppDbContext) Task
    }
    class JwtService {
        +JwtService(IConfiguration)
        +GenerateToken(User) string
    }
    class GradeConversionService {
        <<static>>
        +GetAllGrades() List~string~
        +Rank(string? frenchGrade) int
        +Convert(string? frenchGrade, string scale) string?
    }
    class ValidationFilter {
        <<IEndpointFilter>>
        prüft DataAnnotations der DTOs
    }

    class AuthEndpoints
    class AreaEndpoints
    class SectorEndpoints
    class RouteEndpoints
    class ProgressEndpoints
    class AppointmentEndpoints
    class CommentEndpoints
    class ReportEndpoints
    class UserEndpoints
    class UploadEndpoints
    <<static>> AuthEndpoints
    <<static>> AreaEndpoints
    <<static>> SectorEndpoints
    <<static>> RouteEndpoints
    <<static>> ProgressEndpoints
    <<static>> AppointmentEndpoints
    <<static>> CommentEndpoints
    <<static>> ReportEndpoints
    <<static>> UserEndpoints
    <<static>> UploadEndpoints

    Program --> AuthEndpoints : registriert
    Program --> AreaEndpoints
    Program --> SectorEndpoints
    Program --> RouteEndpoints
    Program --> ProgressEndpoints
    Program --> AppointmentEndpoints
    Program --> CommentEndpoints
    Program --> ReportEndpoints
    Program --> UserEndpoints
    Program --> UploadEndpoints
    Program --> SeedData : beim Start
    Program --> ValidationFilter : für alle Endpoints

    AuthEndpoints ..> JwtService : Token erzeugen
    AuthEndpoints ..> AppDbContext
    AreaEndpoints ..> AppDbContext
    SectorEndpoints ..> AppDbContext
    RouteEndpoints ..> AppDbContext
    RouteEndpoints ..> GradeConversionService : Skala umrechnen
    ProgressEndpoints ..> AppDbContext
    AppointmentEndpoints ..> AppDbContext
    AppointmentEndpoints ..> GradeConversionService
    CommentEndpoints ..> AppDbContext
    ReportEndpoints ..> AppDbContext
    UserEndpoints ..> AppDbContext
    UserEndpoints ..> GradeConversionService
```

---

## 4. Sequenzdiagramm: Einem Termin beitreten

```mermaid
sequenceDiagram
    actor U as User
    participant FE as Angular Frontend
    participant INT as JwtInterceptor
    participant API as AppointmentEndpoints
    participant DB as AppDbContext / SQLite

    U->>FE: Klick auf "Beitreten"
    FE->>INT: POST /api/appointments/{id}/subscribe
    INT->>API: Request + Bearer-Token im Authorization-Header
    API->>API: Policy "User" prüfen (JWT gültig?)
    alt Token ungültig / fehlt
        API-->>FE: 401 Unauthorized
        FE-->>U: Weiterleitung zum Login
    else Token gültig
        API->>DB: Termin inkl. Teilnehmer laden
        alt Termin nicht gefunden
            API-->>FE: 404 Not Found
        else bereits angemeldet
            API-->>FE: 409 Conflict
            FE-->>U: Fehlermeldung anzeigen
        else MaxParticipants erreicht
            API-->>FE: 400 Bad Request (Termin voll)
            FE-->>U: Fehlermeldung anzeigen
        else OK
            API->>DB: AppointmentUser speichern
            DB-->>API: gespeichert
            API-->>FE: 200 OK
            FE-->>U: Teilnehmerliste aktualisiert
        end
    end
```
