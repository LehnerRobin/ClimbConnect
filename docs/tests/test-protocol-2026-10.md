# Testprotokoll – Testlauf Oktober 2026

Projekt: **ClimbConnect** · Issue: **#180 Manuelle Testfälle gegen das neue Design durchgehen**

Die Testfälle stehen in den Dateien dieses Ordners (`areas.md`, `routes.md`, …). Für jeden
Test die Schritte dort ausführen und hier Status (OK / Fehler / Blockiert) und eine kurze
Notiz eintragen. Für jeden Fehler ein eigenes Bug-Issue anlegen und die Nummer in der
Tabelle „Gefundene Fehler“ eintragen.

## Testlauf

| Feld | Wert |
|---|---|
| Datum |  |
| Tester |  |
| Browser (inkl. Version) |  |
| Gerät (PC / Handy) |  |
| Testumgebung | Docker lokal (`http://localhost`) |
| Stand (Commit auf `main`) |  |

## Ergebnisübersicht

| Bereich | Anzahl Tests | OK | Fehler | Blockiert | Notiz |
|---|---:|---:|---:|---:|---|
| Areas | 12 |  |  |  |  |
| Routes | 10 |  |  |  |  |
| Progress | 10 |  |  |  |  |
| Appointments | 14 |  |  |  |  |
| Comments | 9 |  |  |  |  |
| **Gesamt** | **55** |  |  |  |  |

## Einzelne Testergebnisse

| Test-ID | Testfall | Status | Notiz / Fehlerbeschreibung |
|---|---|---|---|
| AR-001 | Area-Liste laden | Offen |  |
| AR-002 | Loading-Anzeige prüfen | Offen |  |
| AR-003 | Fehlerfall beim Laden der Areas | Offen |  |
| AR-004 | Leere Area-Liste | Offen |  |
| AR-005 | Area-Detail öffnen | Offen |  |
| AR-006 | Zurück-Link auf Area-Detailseite | Offen |  |
| AR-007 | Sektoren aufklappen | Offen |  |
| AR-008 | Routen in Sektor anzeigen | Offen |  |
| AR-009 | Ungültige Area-ID | Offen |  |
| AR-010 | Nicht vorhandene Area-ID | Offen |  |
| AR-011 | Startseite zählt alle Routen (#195) | Offen |  |
| AR-012 | Meldung, wenn der Server nicht erreichbar ist (#202) | Offen |  |
| RT-001 | Routendetail laden | Offen |  |
| RT-002 | Community Grade anzeigen | Offen |  |
| RT-003 | Sicherheitsmeldungen leerer Zustand | Offen |  |
| RT-004 | Sicherheitsmeldung erstellen Button nur eingeloggt | Offen |  |
| RT-005 | Sicherheitsmeldung erstellen | Offen |  |
| RT-006 | Sicherheitsmeldung mit Bild kleiner als 5 MB | Offen |  |
| RT-007 | Sicherheitsmeldung Bild größer als 5 MB | Offen |  |
| RT-008 | Begehung eintragen Link | Offen |  |
| RT-009 | Route nicht vorhanden | Offen |  |
| RT-010 | Admin: Grad beim Anlegen einer Route auswählen (#194) | Offen |  |
| PR-001 | Progress-Seite ohne Login schützen | Offen |  |
| PR-002 | Progress-Formular öffnen | Offen |  |
| PR-003 | Standardwerte prüfen | Offen |  |
| PR-004 | Ungültige Route-ID | Offen |  |
| PR-005 | Begehung speichern | Offen |  |
| PR-006 | Begehung im Profil sichtbar | Offen |  |
| PR-007 | Abbrechen im Formular | Offen |  |
| PR-008 | Backend-Fehler beim Speichern | Offen |  |
| PR-009 | Grad-Auswahl (#194) | Offen |  |
| PR-010 | Status-Auswahl (#193) | Offen |  |
| AP-001 | Termine auf Area-Detailseite anzeigen | Offen |  |
| AP-002 | Button „Klettersession erstellen“ nur mit Login | Offen |  |
| AP-003 | Direkter Zugriff ohne Login blockiert | Offen |  |
| AP-004 | Formular mit Standardwerten öffnen | Offen |  |
| AP-005 | Titel ist Pflichtfeld | Offen |  |
| AP-006 | Datum und Uhrzeit sind Pflichtfelder | Offen |  |
| AP-007 | Min. Teilnehmer größer als Max. Teilnehmer | Offen |  |
| AP-008 | Termin erfolgreich erstellen | Offen |  |
| AP-009 | Termin beitreten | Offen |  |
| AP-010 | Termin austreten | Offen |  |
| AP-011 | Backend-Fehler beim Erstellen | Offen |  |
| AP-012 | Termin in der Vergangenheit (#198) | Offen |  |
| AP-013 | Uhrzeit bleibt beim Bearbeiten gleich (#198) | Offen |  |
| AP-014 | Besucher heute in der Gebietsliste (#198) | Offen |  |
| CM-001 | Area-Kommentare anzeigen | Offen |  |
| CM-002 | Area-Kommentar Button nur mit Login | Offen |  |
| CM-003 | Route-Kommentare anzeigen | Offen |  |
| CM-004 | Route-Kommentar Formular nur mit Login | Offen |  |
| CM-005 | Route-Kommentar Text ist Pflicht | Offen |  |
| CM-006 | Route-Kommentar ohne Bild speichern | Offen |  |
| CM-007 | Route-Kommentar mit Bild kleiner als 5 MB speichern | Offen |  |
| CM-008 | Route-Kommentar Bild größer als 5 MB | Offen |  |
| CM-009 | Backend-Fehler beim Kommentar speichern | Offen |  |

## Gefundene Fehler

| Nr. | Test-ID | Beschreibung | Issue | Priorität |
|---|---|---|---|---|
| 1 |  |  |  |  |
| 2 |  |  |  |  |
| 3 |  |  |  |  |
