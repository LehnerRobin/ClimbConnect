# Roadmap – ClimbConnect

Stand: 11.10.2026 · Live: https://frontend-production-47f4.up.railway.app/

Diese Datei beschreibt, was bereits umgesetzt ist und was als Nächstes kommt.
Die Meilensteine aus Benutzersicht stehen in [meilensteine.md](meilensteine.md),
die einzelnen Aufgaben als Issues im GitHub-Repo.

---

## Meilensteine – Stand

| # | Meilenstein | Stand |
|---|---|---|
| 1 | Die App läuft und ich kann mich anmelden (live deployed) | ✅ live auf Railway, Registrierung, Login, Profil mit Gradskala |
| 2 | Klettergebiete und Routen nachschlagen | 🟡 Gebietsliste mit Besucherandrang, Karte, Sektoren, Routen, Gebietssuche, Grade in drei Skalen – **Routensuche fehlt im Frontend** (Backend: `GET /api/routes?search=`) |
| 3 | Gebiete, Sektoren und Routen sind gepflegt | ✅ Admin-Bereich, Grad-Auswahl aus einer zentralen Grad-Tabelle, Community-Grad |
| 4 | Kletterfortschritt dokumentieren | ✅ Begehungen mit Status, Begehungsart, Versuchen, subjektivem Grad; öffentliche Profile |
| 5 | Verbesserung über Zeit sehen | ✅ Grad-Diagramm, Anzahl Routen, offene Projekte, Lieblingsgebiet |
| 6 | Informationen zu Routen und Gebieten teilen | ✅ Kommentare mit Foto, Safety-Reports mit Schweregrad |
| 7 | Kletterpartner finden | ✅ Termine erstellen, beitreten, austreten, bearbeiten; Ø-Grad der Gruppe |

Die Meilensteine sind bis auf die Routensuche im Frontend (Meilenstein 2) funktional
erfüllt. Die weitere Arbeit betrifft diese Lücke, Qualität, Moderation und Feinschliff.

---

## Erledigt im Sprint bis 12.10.2026

- **Hosting:** Deployment auf Railway mit Volume für Datenbank und Bilder, Anleitung in
  [DEPLOYMENT.md](DEPLOYMENT.md) (#183)
- **Sicherheit:** Passwort-Hash und E-Mail nicht mehr über die API, DTO-Validierung,
  Upload-Prüfung am Dateiinhalt, eigener JWT-Key Pflicht, sicheres Admin-Passwort in
  Production (#186–#189, #214)
- **Bugs:** Löschen von Gebieten/Routen mit Kommentaren, Grad-Diagramm, Teilnehmerzahl,
  Umlaute, 20-Einträge-Grenze, Termin-Zeitzonen, Grad-Listen, toter Admin-Knopf
  (#190–#198)
- **Verbesserungen:** Endpoints für eigene und nächste Termine (#201), sichtbare
  Fehlermeldungen und zentrale Gradskala (#202)
- **Infrastruktur:** Docker-Setup, CI mit Backend- und Frontend-Tests bei jedem Push/PR

---

## Sprint 2 – geplant

GitHub-Meilenstein **„Sprint 2"**:

| Issue | Thema | SP |
|---|---|---|
| #222 | Klare Meldungen bei Login und Registrierung | 2 |
| #223 | Admin verwaltet alle Termine und Kommentare | 5 |
| #224 | Kommentare moderieren (baut auf #223 auf) | 5 |
| #203 | Integrationstests für Termine, Kommentare, Reports, Upload, Statistik | 8 |
| #199 | Refactoring: doppelten Code im Backend zusammenführen | 5 |
| #200 | Refactoring: doppelten Code, CSS und toten Code im Frontend aufräumen | 5 |
| #181 | Favoriten im Backend speichern statt im Browser | 5 |

Noch im laufenden Sprint offen: #180 (manuelle Testfälle), #179 (Gebiet-Besuche in der
Statistik), #184 (Lazy Loading).

---

## Danach

- Routensuche im Frontend (Meilenstein 2) – Backend-Endpoint existiert bereits
- Feinschliff anhand der Ergebnisse der manuellen Tests (#180)
- Dokumentation für die Diplomarbeit (Architektur, Entscheidungen wie JWT statt Keycloak,
  Deployment)
- Präsentation mit der Live-Seite als Demo
