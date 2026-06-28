# Setup-Anleitung – ClimbConnect

Vollständige Anleitung zum lokalen Starten der Anwendung.

## Voraussetzungen

| Tool | Version | Download |
|------|---------|----------|
| [.NET SDK](https://dotnet.microsoft.com/download) | 8.0 oder neuer | `dotnet --version` zum Prüfen |
| [Node.js](https://nodejs.org/) | 20 LTS oder neuer | `node --version` zum Prüfen |
| [Angular CLI](https://angular.io/cli) | 17+ | `npm install -g @angular/cli` |
| [Git](https://git-scm.com/) | beliebig | – |

Optional für Docker-Betrieb:
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

---

## 1. Repository klonen

```bash
git clone https://github.com/LehnerRobin/ClimbConnect.git
cd ClimbConnect
```

---

## 2. Backend starten

```bash
cd backend/ClimbConnect.API
dotnet run
```

Das Backend startet auf **http://localhost:5004**.

**Beim ersten Start passiert automatisch:**
- Die SQLite-Datenbank wird erstellt (`backend/ClimbConnect.API/ClimbConnect.db`)
- Alle Migrationen werden angewendet
- Testdaten (Seed) werden eingespielt

### Swagger UI (API-Dokumentation)

Öffne im Browser: **http://localhost:5004/swagger**

Dort kannst du alle Endpoints direkt ausprobieren.

---

## 3. Frontend starten

Neues Terminal öffnen:

```bash
cd frontend
npm install
ng serve
```

Das Frontend startet auf **http://localhost:4200**.

---

## 4. Testdaten & Login

Nach dem ersten Start sind diese Accounts vorhanden:

| Rolle | E-Mail | Passwort |
|-------|--------|----------|
| Admin | `admin@climbconnect.at` | `Admin1234!` |
| User  | `user@climbconnect.at`  | `User1234!`  |

Der **Admin** kann Gebiete, Sektoren und Routen anlegen, bearbeiten und löschen.  
Der **User** kann Begehungen eintragen, Termine erstellen, Kommentare schreiben usw.

### Eingespielt Seed-Daten

- 3 echte Klettergebiete in OÖ (Sandwand Kirchdorf, Steinwandklamm, Dürnsteiner Wand)
- Je 2 Sektoren pro Gebiet
- Je 10 Routen mit realistischen Graden (5a–8a)

---

## 5. Mit Docker starten (alternativ)

```bash
# Im Wurzelverzeichnis des Repos
docker compose up --build
```

| Service  | URL                    |
|----------|------------------------|
| Backend  | http://localhost:5004  |
| Frontend | http://localhost:8080  |

---

## 6. Konfiguration

Die Backend-Konfiguration liegt in `backend/ClimbConnect.API/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=Data/ClimbConnect.db;"
  },
  "Jwt": {
    "Key": "CHANGE_ME_SET_VIA_ENV_VAR_JWT__KEY_MIN_32_CHARS",
    "Issuer": "climbconnect-api",
    "Audience": "climbconnect-ui",
    "ExpiryHours": 24
  },
  "Cors": {
    "AllowedOrigins": [ "http://localhost:4200" ]
  }
}
```

> **Wichtig:** Den JWT-Key in Produktion unbedingt über Umgebungsvariable setzen:  
> `Jwt__Key=dein-geheimes-schluessel-min-32-zeichen`

---

## 7. Neue Migration hinzufügen (nur für Entwickler)

Wenn du das Datenbankmodell änderst:

```bash
cd backend/ClimbConnect.API
dotnet ef migrations add BeschreibungDerÄnderung
dotnet ef database update
```

---

## 8. Häufige Probleme

### Backend startet nicht – "SQLite Error 14"
Das `Data/`-Verzeichnis fehlt. Wird normalerweise automatisch erstellt. Falls nicht:
```bash
mkdir backend/ClimbConnect.API/Data
```

### CORS-Fehler im Browser
Prüfe ob das Frontend auf `http://localhost:4200` läuft und ob dieser Origin in `appsettings.json` unter `Cors:AllowedOrigins` steht.

### `dotnet ef` nicht gefunden
```bash
dotnet tool install --global dotnet-ef
```
