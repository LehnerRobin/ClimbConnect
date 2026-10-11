# Roadmap – ClimbConnect

Stand: 11.10.2026 (Sprintplanung Sprint 2) · Live: https://frontend-production-47f4.up.railway.app/

Diese Datei beschreibt, was bereits umgesetzt ist und was als Nächstes kommt.
Die Meilensteine aus Benutzersicht stehen in [meilensteine.md](meilensteine.md),
die einzelnen Aufgaben als Issues im GitHub-Repo.

---

## Meilensteine – Stand

| # | Meilenstein | Stand |
|---|---|---|
| 1 | Die App läuft und ich kann mich anmelden (live deployed) | ✅ live auf Railway, Registrierung, Login, Profil mit Gradskala |
| 2 | Klettergebiete und Routen nachschlagen | 🟡 Gebietsliste mit Besucherandrang, Karte, Sektoren, Routen, Gebietssuche, Grade in drei Skalen – **Routensuche fehlt im Frontend** (#227, Backend: `GET /api/routes?search=`) |
| 3 | Gebiete, Sektoren und Routen sind gepflegt | ✅ Admin-Bereich, Grad-Auswahl aus einer zentralen Grad-Tabelle, Community-Grad |
| 4 | Kletterfortschritt dokumentieren | ✅ Begehungen mit Status, Begehungsart, Versuchen, subjektivem Grad; öffentliche Profile |
| 5 | Verbesserung über Zeit sehen | ✅ Grad-Diagramm, Anzahl Routen, offene Projekte, Lieblingsgebiet |
| 6 | Informationen zu Routen und Gebieten teilen | ✅ Kommentare mit Foto, Safety-Reports mit Schweregrad – wird in Sprint 2 umgebaut (geprüfte Safety-Reports, Feedback statt öffentlicher Kommentare) |
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

GitHub-Meilenstein **„Sprint 2"** – 12 Issues, 35 SP. Aufteilung: Frontend Mohamed Attia
(Ansichten, Admin, Suche) und Faru Hamid (Termine, Auth, Upload), Backend und alles
andere Robin Lehner.

**Recht & Sicherheit** (die Seite ist öffentlich)

| Issue | Thema | SP | Zuständig |
|---|---|---|---|
| #231 | Impressum und Datenschutzerklärung | 2 | Robin, Mohamed |
| #232 | Eigenes Konto löschen | 3 | Robin, Faru |
| #233 | Schutz gegen Passwort-Raten beim Login | 2 | Robin |
| #234 | Foto-Links nur auf eigene Uploads | 1 | Robin |
| #222 | Klare Meldungen bei Login und Registrierung | 2 | Robin, Mohamed |

**Termine** (Reihenfolge: #228 → #229 → #230)

| Issue | Thema | SP | Zuständig |
|---|---|---|---|
| #228 | Teilnehmerzahl und Anmeldestatus überall anzeigen | 3 | Faru |
| #229 | Eigene Detailseite für Termine mit Link zum Gebiet | 3 | Faru |
| #230 | Termin-Kalender mit Anzahl der Leute pro Tag | 5 | Robin, Faru |

**Inhalte & Verwaltung**

| Issue | Thema | SP | Zuständig |
|---|---|---|---|
| #227 | Routensuche im Frontend (Lücke Meilenstein 2) | 3 | Mohamed |
| #224 | Safety-Reports für Gebiet, Sektor und Route mit Freigabe durch den Admin | 5 | Robin, Faru |
| #244 | Feedback-Knopf statt öffentlicher Kommentare | 3 | Robin, Mohamed |
| #223 | Admin verwaltet alle Termine | 3 | Robin, Mohamed |

> **Konzept für Rückmeldungen (entschieden am 11.10.2026):**
> - **Safety-Reports** zu Gebiet, Sektor oder Route werden vom Team geprüft, bei Bedarf
>   angepasst und erst dann als Warnung angezeigt (#224).
> - **Öffentliche Kommentare entfallen.** Die Kommentarfunktion wird zu einem
>   Feedback-Knopf, dessen Einträge nur das Team sieht (#244).
> - **Absprachen** laufen über eine **Pinnwand beim Termin**, die nur Teilnehmer sehen
>   und die sich automatisch aktualisiert (#245). Keine Privat- oder Direktnachrichten,
>   kein Live-Chat.
>
> `docs/meilensteine.md` (Meilenstein 6 und 7) und `docs/pitch.md` sind bereits an dieses
> Konzept angepasst.

Aus dem laufenden Sprint noch offen: #180 (manuelle Testfälle, Protokoll in
`docs/tests/test-protocol-2026-10.md`), #179 (Gebiet-Besuche in der Statistik),
#184 (Lazy Loading).

---

## Backlog

GitHub-Meilenstein **„Backlog"** – wird bei der nächsten Sprintplanung priorisiert.

| Issue | Thema | SP |
|---|---|---|
| #203 | Integrationstests für Termine, Kommentare, Reports, Upload, Statistik | 8 |
| #199 | Refactoring: doppelten Code im Backend zusammenführen | 5 |
| #200 | Refactoring: doppelten Code, CSS und toten Code im Frontend aufräumen | 5 |
| #181 | Favoriten im Backend speichern statt im Browser | 5 |
| #242 | Laufende Termine hervorheben, auschecken, am Tagesende automatisch löschen | 5 |
| #245 | Pinnwand beim Termin mit automatischer Aktualisierung (nach #229) | 3 |
| #243 | E-Mail-Adresse bei der Registrierung überprüfen (Bestätigungs-Link) | 5 |
| #235 | GPS-Daten aus Fotos entfernen und Bilder verkleinern | 3 |
| #237 | Mehrere Admins verwalten | 3 |
| #240 | Routen nach Grad, Stil und Länge filtern | 3 |
| #236 | Backups der Datenbank auf Railway | 2 |
| #238 | Bilder löschen, wenn Inhalte gelöscht werden | 2 |
| #239 | Gebietsliste zeigt, wie viele in den nächsten Tagen kommen | 2 |
| #241 | Ideen-Sammlung (Gebietsinfos, Wetter, .ics-Export, Grad-Pyramide, …) | 1 |

Neue Features bringen ihre eigenen Tests mit; #203 schließt die Lücken bei bestehenden
Endpoints.

---

## Danach

- Feinschliff anhand der Ergebnisse der manuellen Tests (#180)
- Dokumentation für die Diplomarbeit: Architektur und Entscheidungen (eigenes JWT statt
  Keycloak, SQLite statt PostgreSQL, Railway, geprüfte Safety-Reports statt öffentlicher
  Kommentare)
- Präsentation mit der Live-Seite als Demo
