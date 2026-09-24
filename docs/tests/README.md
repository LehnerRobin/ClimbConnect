# Manuelle Testfälle – ClimbConnect Frontend

Diese Testdokumentation gehört zu **User Story #25 – Manuelle Testfälle definieren**.

Ziel: Die wichtigsten Hauptfunktionen werden manuell getestet, damit sichtbar ist, ob die Anwendung korrekt und stabil funktioniert.

## Voraussetzung

- Backend läuft lokal.
- Frontend läuft mit `npm start`.
- Browser öffnet `http://localhost:4200`.
- Es gibt mindestens einen normalen Testuser.
- Für Admin-Funktionen ist ein Admin-User optional, aber für diese User Story nicht zwingend notwendig.
- In der Datenbank gibt es Testdaten für Areas, Sectors, Routes, Appointments und Comments.

## Testdaten

| Wert | Beispiel |
|---|---|
| Frontend URL | `http://localhost:4200` |
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
