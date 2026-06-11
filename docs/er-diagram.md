# ER-Diagramm – ClimbConnect (v2)

```mermaid
erDiagram
    AREA {
        int Id PK
        string Name
        string Description
        string Location
        string ImageUrl
    }
    SEKTOR {
        int Id PK
        int AreaId FK
        string Name
        string Description
    }
    ROUTE {
        int Id PK
        int SektorId FK
        string Name
        string Grade
        string Style
        int LengthM
        string Description
    }
    USER {
        int Id PK
        string KeycloakId
        string Username
        string Email
        string Bio
        datetime CreatedAt
    }
    PROGRESS {
        int Id PK
        int UserId FK
        int RouteId FK
        string Status
        string Begehungsart
        int AnzahlVersuche
        datetime Date
        string Notes
    }
    APPOINTMENT {
        int Id PK
        int AreaId FK
        int CreatedById FK
        string Title
        string Description
        datetime Date
        int MaxParticipants
    }
    APPOINTMENT_USER {
        int AppointmentId FK
        int UserId FK
        datetime RegisteredAt
    }
    COMMENT {
        int Id PK
        int UserId FK
        int RouteId FK "nullable"
        int AreaId FK "nullable"
        string Text
        string PhotoUrl
        datetime CreatedAt
    }
    REPORT {
        int Id PK
        int RouteId FK
        int UserId FK
        string Description
        string Severity
        string PhotoUrl
        datetime CreatedAt
    }

    AREA ||--o{ SEKTOR : "hat"
    SEKTOR ||--o{ ROUTE : "hat"
    AREA ||--o{ APPOINTMENT : "hostet"
    USER ||--o{ PROGRESS : "erfasst"
    ROUTE ||--o{ PROGRESS : "hat"
    USER ||--o{ COMMENT : "schreibt"
    ROUTE ||--o{ COMMENT : "hat"
    AREA ||--o{ COMMENT : "hat"
    USER ||--o{ REPORT : "meldet"
    ROUTE ||--o{ REPORT : "hat"
    APPOINTMENT ||--o{ APPOINTMENT_USER : "hat"
    USER ||--o{ APPOINTMENT_USER : "registriert"
    USER ||--o{ APPOINTMENT : "erstellt"
```

## Änderungen gegenüber v1

| Was | Warum |
|---|---|
| `SEKTOR` neu zwischen Area und Route | Laut ABA-Portal: "Sektoren, Routen, Schwierigkeitsgrade" |
| `Route.SektorId` statt `AreaId` | Route gehört zu einem Sektor, nicht direkt zum Gebiet |
| `Progress.Begehungsart` (Toprope/Vorstieg) | Explizit im ABA-Portal erwähnt |
| `Progress.AnzahlVersuche` | "benötigte Versuche" laut ABA-Portal |
| `Progress.Status` = Projekt/Rotpunkt/Flash/Onsight | Standard-Kletterterminologie |
| `Comment.RouteId` + `Comment.AreaId` nullable | Kommentare zu Gebieten ODER Routen möglich |
| `Comment.PhotoUrl` | "Bilder zu Gebieten/Routen" laut ABA-Portal |
| `User.KeycloakId` statt `PasswordHash` | Auth via Keycloak, kein eigenes Passwort |

## Enums

**Progress.Status:** `Projekt` · `Rotpunkt` · `Flash` · `Onsight`

**Progress.Begehungsart:** `Toprope` · `Vorstieg`

**Report.Severity:** `Low` · `Medium` · `High`
