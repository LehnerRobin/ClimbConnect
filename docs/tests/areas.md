# Manuelle Testfälle – Areas

Bereich: **Areas, Area-Detail, Sektoren und Routenliste**

## AR-001 – Area-Liste laden

| Feld | Beschreibung |
|---|---|
| Voraussetzung | Backend und Frontend laufen. Es gibt mindestens eine Area. |
| Schritte | 1. Browser öffnen. 2. `http://localhost:4200/areas` aufrufen. |
| Erwartetes Ergebnis | Die Seite „Climbing Areas“ wird angezeigt. Area-Karten werden geladen. Name, Standort, Beschreibung und Besucheranzahl werden angezeigt. |
| Status | Offen |

## AR-002 – Loading-Anzeige prüfen

| Feld | Beschreibung |
|---|---|
| Voraussetzung | Backend reagiert langsam oder Seite wird neu geladen. |
| Schritte | 1. `/areas` öffnen. 2. Direkt beim Laden auf die Anzeige achten. |
| Erwartetes Ergebnis | Während des Ladens erscheint „Gebiete werden geladen...“ mit Spinner. |
| Status | Offen |

## AR-003 – Fehlerfall beim Laden der Areas

| Feld | Beschreibung |
|---|---|
| Voraussetzung | Backend ist gestoppt oder nicht erreichbar. |
| Schritte | 1. Backend stoppen. 2. `/areas` öffnen. 3. Button „Nochmal versuchen“ klicken. |
| Erwartetes Ergebnis | Es erscheint „Gebiete konnten nicht geladen werden.“ Der Button versucht die Daten erneut zu laden. |
| Status | Offen |

## AR-004 – Leere Area-Liste

| Feld | Beschreibung |
|---|---|
| Voraussetzung | Backend liefert keine Areas zurück. |
| Schritte | 1. `/areas` öffnen. |
| Erwartetes Ergebnis | Es erscheint „Es wurden keine Gebiete gefunden.“ |
| Status | Offen |

## AR-005 – Area-Detail öffnen

| Feld | Beschreibung |
|---|---|
| Voraussetzung | Es gibt mindestens eine Area in der Liste. |
| Schritte | 1. `/areas` öffnen. 2. Auf eine Area-Karte klicken. |
| Erwartetes Ergebnis | Die Detailseite `/areas/{id}` öffnet sich. Name, Standort, Beschreibung und Bild oder Platzhalter werden angezeigt. |
| Status | Offen |

## AR-006 – Zurück-Link auf Area-Detailseite

| Feld | Beschreibung |
|---|---|
| Voraussetzung | Eine Area-Detailseite ist geöffnet. |
| Schritte | 1. Auf „← Zurück zu Areas“ klicken. |
| Erwartetes Ergebnis | Der Benutzer kommt zurück zur Area-Liste. |
| Status | Offen |

## AR-007 – Sektoren aufklappen

| Feld | Beschreibung |
|---|---|
| Voraussetzung | Area hat mindestens einen Sektor. |
| Schritte | 1. Area-Detailseite öffnen. 2. In „Sektoren & Routen“ auf einen Sektor klicken. |
| Erwartetes Ergebnis | Der Sektor klappt auf. Während des Ladens erscheint „Routen werden geladen...“. Danach werden die Routen angezeigt oder der Hinweis „Keine Routen vorhanden.“ |
| Status | Offen |

## AR-008 – Routen in Sektor anzeigen

| Feld | Beschreibung |
|---|---|
| Voraussetzung | Ein Sektor hat mindestens eine Route. |
| Schritte | 1. Area-Detailseite öffnen. 2. Sektor aufklappen. |
| Erwartetes Ergebnis | Eine Tabelle mit Name, Grad, Länge und Stil wird angezeigt. Fehlende Werte werden mit `-` dargestellt. |
| Status | Offen |

## AR-009 – Ungültige Area-ID

| Feld | Beschreibung |
|---|---|
| Voraussetzung | Frontend läuft. |
| Schritte | 1. URL `/areas/abc` aufrufen. |
| Erwartetes Ergebnis | Es erscheint „Ungültige Area-ID.“ |
| Status | Offen |

## AR-010 – Nicht vorhandene Area-ID

| Feld | Beschreibung |
|---|---|
| Voraussetzung | Backend läuft. Die Area-ID existiert nicht, z. B. `999999`. |
| Schritte | 1. URL `/areas/999999` aufrufen. |
| Erwartetes Ergebnis | Es erscheint „Gebiet konnte nicht geladen werden.“ |
| Status | Offen |
