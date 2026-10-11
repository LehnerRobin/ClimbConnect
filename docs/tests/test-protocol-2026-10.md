# Testprotokoll – Testlauf Oktober 2026

Projekt: **ClimbConnect** · Issue: **#180 Manuelle Testfälle gegen das neue Design durchgehen**

Die Testfälle stehen in den Dateien dieses Ordners (`areas.md`, `routes.md`, …). Für jeden
Test die Schritte dort ausführen und hier Status (OK / Fehler / Blockiert) und eine kurze
Notiz eintragen. Für jeden Fehler ein eigenes Bug-Issue anlegen und die Nummer in der
Tabelle „Gefundene Fehler“ eintragen.

## Testlauf

| Feld | Wert |
|---|---|
| Datum | 11.10.2026 – 12.10.2026 |
| Tester | Mohamed Attia |
| Browser (inkl. Version) | Google Chrome |
| Gerät (PC / Handy) | PC (Handy-Ansicht über Chrome DevTools) |
| Testumgebung | lokal ohne Docker (`dotnet run` + `npm start`, `http://localhost:4200`) |
| Stand (Commit auf `main`) | `76c20f6` |

## Ergebnisübersicht

| Bereich | Anzahl Tests | OK | Fehler | Blockiert | Notiz |
|---|---:|---:|---:|---:|---|
| Areas | 12 | 12 | 0 | 0 | AR-004 und AR-008 an neues Design angepasst |
| Routes | 10 | 10 | 0 | 0 |  |
| Progress | 11 | 11 | 0 | 0 | PR-011 neu (#249) |
| Appointments | 14 | 14 | 0 | 0 | AP-002 und AP-004 an neues Design angepasst |
| Comments | 9 | 9 | 0 | 0 | CM-002 an neues Design angepasst |
| **Gesamt** | **56** | **56** | **0** | **0** |  |

## Einzelne Testergebnisse

| Test-ID | Testfall | Status | Notiz / Fehlerbeschreibung |
|---|---|---|---|
| AR-001 | Area-Liste laden | OK |  |
| AR-002 | Loading-Anzeige prüfen | OK |  |
| AR-003 | Fehlerfall beim Laden der Areas | OK |  |
| AR-004 | Keine Gebiete gefunden | OK | Geprüft über Suche nach nicht vorhandenem Begriff |
| AR-005 | Area-Detail öffnen | OK |  |
| AR-006 | Zurück-Link auf Area-Detailseite | OK |  |
| AR-007 | Sektoren aufklappen | OK |  |
| AR-008 | Routen in Sektor anzeigen | OK | Routen werden jetzt als Karten statt Tabelle angezeigt |
| AR-009 | Ungültige Area-ID | OK |  |
| AR-010 | Nicht vorhandene Area-ID | OK |  |
| AR-011 | Startseite zählt alle Routen (#195) | OK |  |
| AR-012 | Meldung, wenn der Server nicht erreichbar ist (#202) | OK | Backend mit Strg + C gestoppt |
| RT-001 | Routendetail laden | OK |  |
| RT-002 | Community Grade anzeigen | OK |  |
| RT-003 | Sicherheitsmeldungen leerer Zustand | OK |  |
| RT-004 | Sicherheitsmeldung erstellen Button nur eingeloggt | OK |  |
| RT-005 | Sicherheitsmeldung erstellen | OK |  |
| RT-006 | Sicherheitsmeldung mit Bild kleiner als 5 MB | OK |  |
| RT-007 | Sicherheitsmeldung Bild größer als 5 MB | OK |  |
| RT-008 | Begehung eintragen Link | OK |  |
| RT-009 | Route nicht vorhanden | OK |  |
| RT-010 | Admin: Grad beim Anlegen einer Route auswählen (#194) | OK |  |
| PR-001 | Progress-Seite ohne Login schützen | OK |  |
| PR-002 | Progress-Formular öffnen | OK |  |
| PR-003 | Standardwerte prüfen | OK |  |
| PR-004 | Ungültige Route-ID | OK |  |
| PR-005 | Begehung speichern | OK |  |
| PR-006 | Begehung im Profil sichtbar | OK |  |
| PR-007 | Abbrechen im Formular | OK |  |
| PR-008 | Backend-Fehler beim Speichern | OK |  |
| PR-009 | Grad-Auswahl (#194) | OK |  |
| PR-010 | Status-Auswahl (#193) | OK |  |
| PR-011 | Flash/Onsight: Anzahl Versuche fix 1 (#249) | OK | Neuer Testfall |
| AP-001 | Termine auf Area-Detailseite anzeigen | OK |  |
| AP-002 | Button „+ Session“ nur mit Login | OK | Button heißt jetzt „+ Session“ |
| AP-003 | Direkter Zugriff ohne Login blockiert | OK |  |
| AP-004 | Formular mit Standardwerten öffnen | OK |  |
| AP-005 | Titel ist Pflichtfeld | OK |  |
| AP-006 | Datum und Uhrzeit sind Pflichtfelder | OK |  |
| AP-007 | Min. Teilnehmer größer als Max. Teilnehmer | OK |  |
| AP-008 | Termin erfolgreich erstellen | OK |  |
| AP-009 | Termin beitreten | OK |  |
| AP-010 | Termin austreten | OK |  |
| AP-011 | Backend-Fehler beim Erstellen | OK |  |
| AP-012 | Termin in der Vergangenheit (#198) | OK |  |
| AP-013 | Uhrzeit bleibt beim Bearbeiten gleich (#198) | OK |  |
| AP-014 | Besucher heute in der Gebietsliste (#198) | OK |  |
| CM-001 | Area-Kommentare anzeigen | OK |  |
| CM-002 | Area-Kommentar Button nur mit Login | OK | Button heißt jetzt „Schreiben“ |
| CM-003 | Route-Kommentare anzeigen | OK |  |
| CM-004 | Route-Kommentar Formular nur mit Login | OK |  |
| CM-005 | Route-Kommentar Text ist Pflicht | OK |  |
| CM-006 | Route-Kommentar ohne Bild speichern | OK |  |
| CM-007 | Route-Kommentar mit Bild kleiner als 5 MB speichern | OK |  |
| CM-008 | Route-Kommentar Bild größer als 5 MB | OK |  |
| CM-009 | Backend-Fehler beim Kommentar speichern | OK |  |

## Gefundene Fehler

Keine. Alle 56 Testfälle waren erfolgreich, daher wurden keine Bug-Issues angelegt.

## Angepasste Testfälle

| Test-ID | Änderung |
|---|---|
| AR-004 | Leere Liste wird über die Suche geprüft (ein leeres Backend ist mit Seed-Daten nicht herstellbar) |
| AR-008 | Routen werden als Karten statt als Tabelle angezeigt |
| AP-002, AP-004 | Button auf der Gebietsseite heißt jetzt „+ Session“ |
| CM-002 | Button auf der Gebietsseite heißt jetzt „Schreiben“ |
| PR-011 | Neu: Flash/Onsight setzt die Anzahl der Versuche fix auf 1 (#249) |