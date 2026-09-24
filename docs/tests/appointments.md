# Manuelle Testfälle – Appointments

Bereich: **Klettersessions / Termine erstellen, anzeigen, beitreten und austreten**

## AP-001 – Termine auf Area-Detailseite anzeigen

| Feld | Beschreibung |
|---|---|
| Voraussetzung | Area existiert. Es gibt mindestens einen Termin oder keine Termine. |
| Schritte | 1. `/areas/{id}` öffnen. 2. Abschnitt „Kommende Termine“ prüfen. |
| Erwartetes Ergebnis | Vorhandene Termine werden mit Titel, Datum und Teilnehmeranzahl angezeigt. Wenn keine Termine vorhanden sind, steht „Keine Termine vorhanden.“ |
| Status | Offen |

## AP-002 – Button „Klettersession erstellen“ nur mit Login

| Feld | Beschreibung |
|---|---|
| Voraussetzung | Area existiert. |
| Schritte | 1. Ausloggen. 2. `/areas/{id}` öffnen. 3. Einloggen. 4. Seite erneut öffnen. |
| Erwartetes Ergebnis | Ohne Login ist der Button nicht sichtbar. Mit Login ist „Klettersession erstellen“ sichtbar. |
| Status | Offen |

## AP-003 – Direkter Zugriff ohne Login blockiert

| Feld | Beschreibung |
|---|---|
| Voraussetzung | User ist ausgeloggt. |
| Schritte | 1. URL `/areas/{id}/appointments/new` öffnen. |
| Erwartetes Ergebnis | User wird zur Login-Seite weitergeleitet. |
| Status | Offen |

## AP-004 – Formular mit Standardwerten öffnen

| Feld | Beschreibung |
|---|---|
| Voraussetzung | User ist eingeloggt. Area existiert. |
| Schritte | 1. Area-Detailseite öffnen. 2. „Klettersession erstellen“ klicken. |
| Erwartetes Ergebnis | Formular öffnet sich. Datum ist heute, Uhrzeit ist `18:00`, minimale Teilnehmer ist `1`, maximale Teilnehmer ist `4`. |
| Status | Offen |

## AP-005 – Titel ist Pflichtfeld

| Feld | Beschreibung |
|---|---|
| Voraussetzung | Appointment-Formular ist geöffnet. |
| Schritte | 1. Titel leer lassen. 2. „Klettersession erstellen“ klicken. |
| Erwartetes Ergebnis | Es erscheint „Bitte gib einen Titel ein.“ Es wird kein Termin erstellt. |
| Status | Offen |

## AP-006 – Datum und Uhrzeit sind Pflichtfelder

| Feld | Beschreibung |
|---|---|
| Voraussetzung | Appointment-Formular ist geöffnet. |
| Schritte | 1. Titel eintragen. 2. Datum oder Uhrzeit löschen. 3. Speichern klicken. |
| Erwartetes Ergebnis | Es erscheint „Bitte gib Datum und Uhrzeit ein.“ Es wird kein Termin erstellt. |
| Status | Offen |

## AP-007 – Min. Teilnehmer größer als Max. Teilnehmer

| Feld | Beschreibung |
|---|---|
| Voraussetzung | Appointment-Formular ist geöffnet. |
| Schritte | 1. Titel eintragen. 2. Min. Teilnehmer auf `5` setzen. 3. Max. Teilnehmer auf `2` setzen. 4. Speichern klicken. |
| Erwartetes Ergebnis | Es erscheint „Minimale Teilnehmerzahl darf nicht größer als maximale Teilnehmerzahl sein.“ |
| Status | Offen |

## AP-008 – Termin erfolgreich erstellen

| Feld | Beschreibung |
|---|---|
| Voraussetzung | User ist eingeloggt. Backend läuft. |
| Schritte | 1. Appointment-Formular öffnen. 2. Gültigen Titel, Datum, Uhrzeit und optionale Felder ausfüllen. 3. „Klettersession erstellen“ klicken. |
| Erwartetes Ergebnis | Termin wird gespeichert. User wird zurück zur Area-Detailseite geleitet. Neuer Termin erscheint in „Kommende Termine“. |
| Status | Offen |

## AP-009 – Termin beitreten

| Feld | Beschreibung |
|---|---|
| Voraussetzung | User ist eingeloggt. Es gibt einen Termin. |
| Schritte | 1. Area-Detailseite öffnen. 2. Bei einem Termin „Beitreten“ klicken. |
| Erwartetes Ergebnis | Button ändert sich zu „Austreten“. Teilnehmeranzahl erhöht sich um `1`. |
| Status | Offen |

## AP-010 – Termin austreten

| Feld | Beschreibung |
|---|---|
| Voraussetzung | User ist eingeloggt und ist einem Termin beigetreten. |
| Schritte | 1. Bei diesem Termin „Austreten“ klicken. |
| Erwartetes Ergebnis | Button ändert sich zu „Beitreten“. Teilnehmeranzahl verringert sich um `1`, aber nicht unter `0`. |
| Status | Offen |

## AP-011 – Backend-Fehler beim Erstellen

| Feld | Beschreibung |
|---|---|
| Voraussetzung | User ist eingeloggt. Backend liefert Fehler oder ist nicht erreichbar. |
| Schritte | 1. Appointment-Formular gültig ausfüllen. 2. Backend stoppen oder Fehler provozieren. 3. Speichern klicken. |
| Erwartetes Ergebnis | Es erscheint „Klettersession konnte nicht erstellt werden.“ oder bei Auth-Problem „Du musst eingeloggt sein, um eine Klettersession zu erstellen.“ |
| Status | Offen |
