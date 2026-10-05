# ClimbConnect

Webanwendung zur Erfassung und Analyse von Kletterfortschritten in den Seilklettergebieten Oberösterreichs.

Diplomarbeit / Projekt an der HTL Leonding · Team: **Robin Lehner** (Backend & Datenbank), **Mohamed Attia** (Frontend), **Faru Hamid** (Frontend)

---

## 🚀 Überblick

Mit **ClimbConnect** können Kletterer:innen:

- Klettergebiete, Sektoren und Routen in OÖ ansehen (inkl. Karte)
- ihren Fortschritt pro Route dokumentieren (Status, Begehungsart, Versuche, subjektiver Grad)
- Statistiken über ihre Entwicklung ansehen
- Termine pro Gebiet erstellen und beitreten („gemeinsam klettern“)
- Kommentare mit Fotos zu Gebieten und Routen schreiben
- Safety-Reports erfassen (z. B. lockerer Griff, beschädigter Bohrhaken)

Grade werden intern in französischer Skala gespeichert und je nach Benutzereinstellung in **Französisch, UIAA oder Amerikanisch** angezeigt.

Das Projekt wird im **Scrum-Prozess** umgesetzt (User Stories mit Akzeptanzkriterien, Story Points, Sprints im GitHub Project).

---

## 🧩 Architektur

| Schicht | Technologie |
|---|---|
| **Frontend** | Angular (Standalone Components, Services, JWT-Interceptor, Route Guards) |
| **Backend** | .NET 8 Minimal API, REST, Swagger/OpenAPI |
| **Datenbank** | SQLite über Entity Framework Core (Code-First mit Migrationen) |
| **Authentifizierung** | eigene Benutzerverwaltung mit JWT, Rollen `user` und `admin` |
| **Tests** | xUnit-Integrationstests (`backend/ClimbConnect.Tests`) |
| **CI** | GitHub Actions: Build + Tests für Backend und Frontend bei jedem Push / PR |
| **Deployment** | Docker Compose (Frontend über nginx + Backend) |

Details: [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)

---

## ⚡ Schnellstart

Voraussetzungen: .NET SDK 8, Node.js 20+, Angular CLI

```bash
git clone https://github.com/LehnerRobin/ClimbConnect.git
cd ClimbConnect

# Backend (Terminal 1) → http://localhost:5004, Swagger: http://localhost:5004/swagger
cd backend/ClimbConnect.API
dotnet run

# Frontend (Terminal 2) → http://localhost:4200
cd frontend
npm install
ng serve
```

Beim ersten Start wird die SQLite-Datenbank automatisch angelegt und mit Testdaten befüllt.

**Test-Accounts**

| Rolle | E-Mail | Passwort |
|---|---|---|
| Admin | `admin@climbconnect.at` | `Admin1234!` |
| User | `user@climbconnect.at` | `User1234!` |

**Alternativ mit Docker:**

```bash
cp .env.example .env    # JWT_KEY (mind. 32 Zeichen) eintragen
docker compose up --build
# → http://localhost
```

👉 Ausführliche Anleitung inkl. Konfiguration und häufiger Probleme: **[docs/SETUP.md](docs/SETUP.md)**

**Tests ausführen:**

```bash
dotnet test backend/ClimbConnect.Tests/ClimbConnect.Tests.csproj
```

---

## 📁 Projektstruktur

```
ClimbConnect/
├── backend/
│   ├── ClimbConnect.API/      # .NET 8 Minimal API (Models, Endpoints, Services, Migrations)
│   └── ClimbConnect.Tests/    # xUnit-Integrationstests
├── frontend/                  # Angular-App
├── docs/                      # Projektdokumentation
├── .github/                   # Issue-Templates, PR-Template, CI-Workflow
└── docker-compose.yml
```

---

## 📚 Dokumentation

| Dokument | Inhalt |
|---|---|
| [Projektauftrag](docs/Projektauftrag.md) | Ziel, Umfang, Beteiligte, Zeitrahmen, erwartete Ergebnisse |
| [Pitch](docs/pitch.md) | Projektidee und Zielgruppe |
| [Meilensteine](docs/meilensteine.md) | Was der User bei jedem Meilenstein können soll |
| [User Stories](docs/USER_STORIES.md) | Backlog-Übersicht (die Stories selbst liegen als Issues im GitHub Project) |
| [ER-Diagramm](docs/er-diagram.md) | Datenmodell mit allen Entitäten und Beziehungen |
| [UML-Diagramme](docs/UML_OVERVIEW.md) | Use-Case-, Klassen- und Sequenzdiagramm |
| [Architektur](docs/ARCHITECTURE.md) | Systemübersicht, Rollen, Auth, Grad-Konversion |
| [API-Spezifikation](docs/API_SPEC.md) | REST-Endpunkte (vollständig in Swagger) |
| [Setup](docs/SETUP.md) | Lokale Installation und Start |
| [Git-Workflow](docs/GIT_WORKFLOW.md) | Branches, Commits, Pull Requests |
| [Tests](docs/tests/README.md) | Testprotokolle pro Feature |
| [UI-Wireframes](docs/ui-wireframes/README.md) | Entwürfe der Hauptseiten |

---

## 🗂️ Projektmanagement

- **Backlog & Sprints:** [GitHub Project](https://github.com/LehnerRobin/ClimbConnect/projects)
- **User Stories:** als [Issues](https://github.com/LehnerRobin/ClimbConnect/issues) mit Akzeptanzkriterien, Story Points und Assignees
- **Beitragen:** siehe [CONTRIBUTING.md](CONTRIBUTING.md)

---

## 📄 Lizenz

[MIT](LICENSE)
