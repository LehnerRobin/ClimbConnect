# ClimbConnect – Claude Code Anweisungen

## Mein Arbeitsstil

- Bevor du eine Datei erstellst oder änderst, erkläre kurz:
  - Was die Datei ist und was sie macht
  - Warum du sie so strukturiert hast
- Warte dann auf meine Bestätigung bevor du weitermachst
- Nach jeder abgeschlossenen Datei erkläre den Code:
  - Was macht jede Klasse / Methode / Funktion?
  - Warum hast du es so gelöst und nicht anders?
  - Was sollte ich als Entwickler darüber wissen?
- Nach jedem abgeschlossenen Issue fasse zusammen:
  - Was wurde implementiert?
  - Welche Dateien wurden erstellt oder verändert?
  - Was hat sich im Projekt dadurch verändert?
  - Was ist der nächste logische Schritt?

## GitHub – Nimm mich bei der Hand

Ich bin kein erfahrener Entwickler. Wenn etwas auf GitHub erledigt werden
muss, sag mir GENAU was ich tun soll. Zum Beispiel:

- "Geh jetzt auf GitHub → Issues → New Issue und erstelle ein Issue mit Titel X"
- "Geh zu Pull Requests und merge Branch X in main"
- "Schließ Issue #72 – der ist jetzt erledigt"

Wenn du einen Commit oder Push machst, sag mir danach explizit:
- Welche Dateien du geändert hast
- Mit welcher Commit-Message
- Ob der Push erfolgreich war (mit GitHub-Link)

Wenn ein Push fehlschlägt, erkläre mir Schritt für Schritt wie ich das behebe.

## Workflow pro Feature

1. Sag mir welches GitHub Issue wir gerade bearbeiten
2. Erkläre kurz was wir implementieren und warum
3. Implementiere Datei für Datei – warte auf meine Bestätigung
4. Erkläre nach jeder Datei den Code verständlich
5. Commit mit sinnvoller Message (z.B. "feat: Areas Endpoints (closes #67)")
6. Push und zeig mir den GitHub-Link
7. Fasse zusammen was sich im Projekt verändert hat
8. Sag mir was der nächste Schritt ist

## Projektziele & Features

### Rollen (Keycloak)
- **Admin** (nur wir): kann Gebiete, Sektoren und Routen anlegen,
  bearbeiten und löschen
- **User** (alle anderen): kann nur lesen, eigenen Fortschritt eintragen,
  Kommentare schreiben, Termine erstellen und beitreten

### Features im Überblick

**Gebiete, Sektoren & Routen (nur Admin)**
- Klettergebiete in OÖ verwalten (Area → Sektor → Route)
- Routen haben: Name, Grad (franz. Skala intern), Stil, Länge, Beschreibung
- Grad wird per API in franz., amerikanischer oder UIAA-Skala ausgegeben
  je nach Benutzereinstellung

**Fortschritt (User)**
- Route eintragen: Status (Projekt/Rotpunkt/Flash/Onsight),
  Begehungsart (Toprope/Vorstieg), Anzahl Versuche, Notiz
- Subjektive Gradbewertung + kurzer Kommentar (fließt in Community-Grad ein)

**Statistiken (User)**
- Gradentwicklung über Zeit als Chart (Rotpunkt-Grad pro Monat)
- Anzahl gekletterte Routen, offene Projekte, Lieblingsgebiet

**Terminplaner (User)**
- Termin erstellen: Gebiet, Datum, Uhrzeit, Beschreibung,
  Mindest- und Maximalanzahl Teilnehmer
- Beitreten / Austreten
- Durchschnittsgrad der Teilnehmer sehen (in Benutzer-Skala)

**Gebietsübersicht**
- Direkt in der Liste: wie viele Leute heute dort sind und
  wie viele vorhaben zu kommen (aus Terminplaner-Daten)

**Kommentare & Bilder (User)**
- Kommentar mit Foto zu Gebiet oder Route (informativ, kein Chat)
- Kein Chat, kein Follow-System, keine Direktnachrichten

**Safety Reports (User)**
- Report zu einer Route: Beschreibung, Schweregrad (Low/Medium/High), Foto

**Öffentliche Profile**
- Andere User können letzte Begehungen eines Profils sehen (Gebiet + Grad)

## Projekt

- Diplomarbeit HTL Leonding
- Referenz-Vorlage: https://github.com/aitenbichler/DemoSyp
- Repo: https://github.com/LehnerRobin/ClimbConnect

## Pflichtlektüre beim Start

Lies diese Dateien bevor du mit der Implementierung beginnst:
- `docs/pitch.md` – Projektbeschreibung und Ziel der App
- `docs/meilensteine.md` – Was der User bei jedem Meilenstein können soll
- `docs/er-diagram.md` – Datenmodell mit allen Entities und Beziehungen

## Stack

- Backend: .NET 8 Minimal API, C#, EF Core, SQLite
- Auth: JWT (eigenes System), Keycloak vorbereitet aber noch nicht aktiv
- Frontend: Angular (anderer Kollege zuständig)
- CI/CD: GitHub Actions
- Grades: intern französische Skala, Konversion zu UIAA und Amerikanisch

## Coding-Stil

- Kommentare auf Deutsch
- XML-Dokumentation (///) bei allen public Methoden und Klassen
- Konsistente Benennung nach bestehendem Code
- Keine unnötigen Abhängigkeiten
- Nach jeder Methode / Klasse kurz erklären was sie tut

## Offene Issues (Backend)

- #67 Endpunkte Areas (nur Admin: POST/PUT/DELETE, alle: GET)
- #68 Endpunkte Sektoren
- #69 Endpunkte Routes
- #70 Endpunkte Progress
- #71 Endpunkte Appointments
- #72 ER-Diagramm ✅
- #73 Models & Tabellen definieren
- #74 Beispiel-Testdaten (Seed)
- #75 Keycloak Integration & Rollen
- #76 Grad-Konversion (franz./UIAA/amerikanisch)
