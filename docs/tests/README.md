# Manuelle Testfälle – ClimbConnect Frontend

Diese Testdokumentation gehört zu **User Story #25 – Manuelle Testfälle definieren**.

Ziel: Die wichtigsten Hauptfunktionen werden manuell getestet, damit sichtbar ist, ob die Anwendung korrekt und stabil funktioniert.

## Voraussetzung

Eine der beiden Umgebungen:

- **Lokal mit Docker** (empfohlen für Fehlerfälle wie „Backend stoppen“): `docker compose up --build`, Browser öffnet `http://localhost`. Backend stoppen mit `docker compose stop api`, wieder starten mit `docker compose start api`.
- **Lokal ohne Docker:** Backend mit `dotnet run`, Frontend mit `npm start`, Browser öffnet `http://localhost:4200`. Hier gibt es die Test-Zugänge aus `docs/SETUP.md`.
- **Live** (nur für Tests ohne Fehlerfälle): https://frontend-production-47f4.up.railway.app/ – dort keine Testdaten anlegen, die nicht wieder gelöscht werden.

Außerdem:

- Es gibt mindestens einen normalen Testuser (einfach registrieren).
- Für Admin-Funktionen wird ein Admin-User gebraucht (`admin@climbconnect.at`).
- In der Datenbank gibt es Testdaten für Areas, Sectors, Routes, Appointments und Comments (Seed-Daten).

## Testdaten

| Wert | Beispiel |
|---|---|
| Frontend URL | `http://localhost` (Docker) oder `http://localhost:4200` (`npm start`) |
| Testuser | normaler eingeloggter User |
| Area-ID | eine vorhandene Area, z. B. `1` |
| Route-ID | eine vorhandene Route, z. B. `1` |
| Bilddatei gültig | `.jpg` oder `.png`, kleiner als 5 MB |
| Bilddatei ungültig | Bild größer als 5 MB |

## Dateien in diesem Ordner

| Datei | Inhalt |
|---|---|
| `areas.md` | Testfälle für Area-Liste, Area-Detail, Sektoren und Routenliste |
| `routes.md` | Testfälle für Routendetail, Community Grade und Safety Reports |
| `progress.md` | Testfälle für Begehungen / Progress |
| `appointments.md` | Testfälle für Klettersessions / Termine |
| `comments.md` | Testfälle für Kommentare |
| `test-protocol-template.md` | Vorlage zum Eintragen der Testergebnisse |
| `test-protocol-2026-10.md` | Vorbereitetes Protokoll für den Testlauf im Oktober 2026 (#180) |

## Status-Legende

| Status | Bedeutung |
|---|---|
| Offen | Noch nicht getestet |
| OK | Test erfolgreich |
| Fehler | Test fehlgeschlagen |
| Blockiert | Test konnte nicht durchgeführt werden |

## Wichtig

Bei jedem Test soll eingetragen werden:

- Datum
- Tester
- Browser
- Ergebnis
- kurze Notiz, falls ein Fehler gefunden wurde
