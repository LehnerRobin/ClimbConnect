# Deployment – ClimbConnect auf Railway

ClimbConnect läuft öffentlich auf [Railway](https://railway.com):

**👉 https://frontend-production-47f4.up.railway.app/**

Diese Anleitung beschreibt, wie das Deployment aufgebaut ist, welche Einstellungen
nötig sind und wie man typische Fehler behebt.

---

## 1. Aufbau

```
Browser ──HTTPS──▶ frontend (nginx, Port 8080) ──/api, /uploads──▶ api (.NET, Port 8080)
                    öffentliche Domain                              nur intern erreichbar
                                                                    Volume: /app/Data
```

- **frontend**: Angular-App, ausgeliefert von nginx. nginx leitet `/api/` und `/uploads/`
  über das interne Railway-Netzwerk an das Backend weiter. Konfiguration:
  `frontend/nginx.conf.template` und `frontend/Dockerfile`.
- **api**: .NET-Backend mit SQLite. Datenbank und hochgeladene Bilder liegen in einem
  Volume unter `/app/Data`, damit sie Deployments überleben.

Beide Services werden aus diesem GitHub-Repo mit den vorhandenen Dockerfiles gebaut.
Nach jedem Merge auf `main` baut Railway die Services automatisch neu.

---

## 2. Services einrichten

Im Railway-Projekt zwei Services aus dem GitHub-Repo anlegen
(**+ New → GitHub Repo → LehnerRobin/ClimbConnect**), jeweils mit eigenem Root Directory
(**Service → Settings → Source → Root Directory**):

| Service | Root Directory | Dockerfile |
|---|---|---|
| `api` | `backend/ClimbConnect.API` | `Dockerfile` |
| `frontend` | `frontend` | `Dockerfile` |

### Interne Adresse des Backends

Unter **api → Settings → Networking → Private Networking** steht die interne Adresse,
z. B. `climbconnect.railway.internal`. Sie wird beim Anlegen des Services vergeben und
ändert sich **nicht**, wenn man den Service später umbenennt. Genau diesen Wert braucht
das Frontend als `API_HOST`.

### Öffentliche Domain

**frontend → Settings → Networking → Public Networking → Generate Domain**, Port **8080**.
Das Backend braucht keine öffentliche Domain.

---

## 3. Variablen

### api

| Variable | Wert | Wofür |
|---|---|---|
| `Jwt__Key` | zufälliger Text, mind. 32 Zeichen | Signiert die Login-Tokens. Ohne eigenen Key startet das Backend nicht. |
| `Seed__AdminPassword` | eigenes Passwort, mind. 12 Zeichen | Passwort für `admin@climbconnect.at`, nur beim ersten Start mit leerer Datenbank nötig. |
| `Storage__UploadsPath` | `/app/Data/uploads` | Legt die Bilder ins Volume (Railway erlaubt nur ein Volume pro Service). |
| `RAILWAY_RUN_UID` | `0` | Railway hängt Volumes als root ein; das Backend läuft sonst ohne Schreibrechte darauf. |

Einen zufälligen Key erzeugt man z. B. in PowerShell:

```powershell
[guid]::NewGuid().ToString("N") + [guid]::NewGuid().ToString("N")
```

> Den Key und das Admin-Passwort **nie** ins Repo, in Issues oder in Chats kopieren.

Issuer, Audience, Token-Laufzeit und Datenbankpfad kommen aus `appsettings.json` und
müssen nicht gesetzt werden. Die Umgebung ist automatisch `Production`.

### frontend

| Variable | Wert | Wofür |
|---|---|---|
| `API_HOST` | interne Adresse des api-Service, z. B. `climbconnect.railway.internal` | Ziel der nginx-Weiterleitung für `/api/` und `/uploads/`. |

Lokal mit Docker Compose ist `API_HOST` automatisch `api` (Standardwert im Dockerfile).

---

## 4. Volume

**Rechtsklick auf den api-Service → Attach Volume**, Mount Path **`/app/Data`**.

Darin liegen `ClimbConnect.db` (Datenbank) und `uploads/` (Bilder). Ohne Volume ist nach
jedem Deployment alles weg.

---

## 5. Nach dem ersten Deployment

1. Startseite aufrufen und `https://<domain>/api/health` prüfen → `{"status":"ok"}`
2. Mit `admin@climbconnect.at` und dem `Seed__AdminPassword` anmelden
3. Datenbank-Persistenz prüfen: einen User registrieren, api neu deployen, User ist noch da

In Production gibt es **keinen** `testuser`. Die öffentlich bekannten Test-Passwörter aus
`docs/SETUP.md` gelten nur lokal (Development) und in den Tests.

---

## 6. Fehlersuche

Logs: **Service → Deployments → aktuelles Deployment → Deploy Logs** (nicht Build Logs).

| Symptom | Ursache | Lösung |
|---|---|---|
| Build bricht ab wegen `--mount=type=cache` | Railway lehnt Cache-Mounts ohne Service-ID ab | Im Dockerfile kein `RUN --mount=type=cache` verwenden |
| api: `Jwt:Key ist nicht gesetzt` | `Jwt__Key` fehlt oder ist zu kurz | Variable setzen (zwei Unterstriche!) |
| api: `Seed:AdminPassword ist nicht gesetzt` | Leere Datenbank, kein Admin-Passwort | `Seed__AdminPassword` setzen (mind. 12 Zeichen) |
| api: Fehler beim Schreiben in `/app/Data` | Volume gehört root | `RAILWAY_RUN_UID=0` setzen |
| Seite zeigt `Application failed to respond` | Frontend läuft nicht oder falscher Port | Deploy Logs des frontend ansehen; Domain-Port muss 8080 sein |
| frontend: `host not found in upstream` | `API_HOST` stimmt nicht | Wert aus api → Private Networking übernehmen |
| Startseite geht, `/api/...` antwortet mit 502 | Backend läuft nicht | Deploy Logs des api ansehen |
| Daten nach Deployment weg | Kein Volume oder falscher Pfad | Volume auf `/app/Data`, `Storage__UploadsPath=/app/Data/uploads` |

nginx löst die Backend-Adresse zur Laufzeit neu auf (`resolver` in
`nginx.conf.template`). Ein neues Backend-Deployment mit neuer interner IP braucht daher
keinen Neustart des Frontends.

---

## 7. Lokal mit Docker

Dasselbe Setup läuft lokal mit Docker Compose, siehe [SETUP.md](SETUP.md#5-mit-docker-starten-alternativ).
