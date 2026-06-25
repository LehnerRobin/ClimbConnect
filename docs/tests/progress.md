# Manuelle Testfälle – Progress

Bereich: **Begehung eintragen und Profil-Fortschritt**

## PR-001 – Progress-Seite ohne Login schützen

| Feld | Beschreibung |
|---|---|
| Voraussetzung | User ist ausgeloggt. |
| Schritte | 1. URL `/progress/new?routeId=1&routeName=Test` öffnen. |
| Erwartetes Ergebnis | User wird zur Login-Seite weitergeleitet. |
| Status | Offen |

## PR-002 – Progress-Formular öffnen

| Feld | Beschreibung |
|---|---|
| Voraussetzung | User ist eingeloggt. Gültige Route-ID ist vorhanden. |
| Schritte | 1. Routendetail öffnen. 2. „+ Begehung eintragen“ klicken. |
| Erwartetes Ergebnis | Formular zeigt RouteName an. Felder Status, Begehungsart, Anzahl Versuche, Datum, Notizen und subjektive Gradeinschätzung sind sichtbar. |
| Status | Offen |

## PR-003 – Standardwerte prüfen

| Feld | Beschreibung |
|---|---|
| Voraussetzung | Progress-Formular ist geöffnet. |
| Schritte | 1. Felder im Formular ansehen. |
| Erwartetes Ergebnis | Status ist „Rotpunkt“, Begehungsart ist „Vorstieg“, Versuche ist `1`, Datum ist heutiges Datum. |
| Status | Offen |

## PR-004 – Ungültige Route-ID

| Feld | Beschreibung |
|---|---|
| Voraussetzung | User ist eingeloggt. |
| Schritte | 1. URL `/progress/new` ohne `routeId` öffnen. 2. „Begehung speichern“ klicken. |
| Erwartetes Ergebnis | Es erscheint „Ungültige Routen-ID.“ |
| Status | Offen |

## PR-005 – Begehung speichern

| Feld | Beschreibung |
|---|---|
| Voraussetzung | User ist eingeloggt. Gültige Route-ID ist vorhanden. |
| Schritte | 1. Progress-Formular öffnen. 2. Status auswählen. 3. Begehungsart auswählen. 4. Anzahl Versuche eintragen. 5. Optional Notiz und subjektiven Grad eintragen. 6. „Begehung speichern“ klicken. |
| Erwartetes Ergebnis | Begehung wird gespeichert. User wird zurück zur Route `/routes/{id}` geleitet. |
| Status | Offen |

## PR-006 – Begehung im Profil sichtbar

| Feld | Beschreibung |
|---|---|
| Voraussetzung | PR-005 wurde erfolgreich durchgeführt. |
| Schritte | 1. `/profile` öffnen. 2. Abschnitt „Meine Begehungen“ prüfen. |
| Erwartetes Ergebnis | Die gespeicherte Route erscheint in der Liste. Datum, Status, Begehungsart und Anzahl Versuche stimmen. |
| Status | Offen |

## PR-007 – Abbrechen im Formular

| Feld | Beschreibung |
|---|---|
| Voraussetzung | Progress-Formular ist mit gültiger Route-ID geöffnet. |
| Schritte | 1. „Abbrechen“ klicken. |
| Erwartetes Ergebnis | User wird zurück zur Route `/routes/{id}` geleitet. Es wird nichts gespeichert. |
| Status | Offen |

## PR-008 – Backend-Fehler beim Speichern

| Feld | Beschreibung |
|---|---|
| Voraussetzung | User ist eingeloggt. Backend liefert Fehler oder ist nicht erreichbar. |
| Schritte | 1. Progress-Formular ausfüllen. 2. Backend stoppen oder Fehler provozieren. 3. Speichern klicken. |
| Erwartetes Ergebnis | Es erscheint eine Fehlermeldung, z. B. „Begehung konnte nicht gespeichert werden.“ Button wird wieder benutzbar. |
| Status | Offen |
