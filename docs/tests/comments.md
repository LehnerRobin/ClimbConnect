# Manuelle Testfälle – Comments

Bereich: **Kommentare auf Area- und Routenseiten**

## CM-001 – Area-Kommentare anzeigen

| Feld | Beschreibung |
|---|---|
| Voraussetzung | Area existiert. Es gibt Kommentare oder keine Kommentare. |
| Schritte | 1. `/areas/{id}` öffnen. 2. Abschnitt „Kommentare“ prüfen. |
| Erwartetes Ergebnis | Vorhandene Kommentare werden mit Autor, Text und Datum angezeigt. Wenn keine Kommentare vorhanden sind, steht „Keine Kommentare vorhanden.“ |
| Status | Offen |

## CM-002 – Area-Kommentar Button nur mit Login

| Feld | Beschreibung |
|---|---|
| Voraussetzung | Area existiert. |
| Schritte | 1. Ausloggen. 2. `/areas/{id}` öffnen. 3. Einloggen und Seite erneut öffnen. |
| Erwartetes Ergebnis | Ohne Login ist der Button „Kommentar schreiben“ nicht sichtbar. Mit Login ist der Button sichtbar. |
| Status | Offen |

## CM-003 – Route-Kommentare anzeigen

| Feld | Beschreibung |
|---|---|
| Voraussetzung | Route existiert. |
| Schritte | 1. `/routes/{id}` öffnen. 2. Abschnitt „Kommentare“ prüfen. |
| Erwartetes Ergebnis | Kommentare werden angezeigt oder es erscheint „Noch keine Kommentare.“ |
| Status | Offen |

## CM-004 – Route-Kommentar Formular nur mit Login

| Feld | Beschreibung |
|---|---|
| Voraussetzung | Route existiert. |
| Schritte | 1. Ausloggen. 2. `/routes/{id}` öffnen. 3. Einloggen und Seite erneut öffnen. |
| Erwartetes Ergebnis | Ohne Login ist kein Kommentarformular sichtbar. Mit Login sind Textfeld, Datei-Upload und Button „Kommentar abschicken“ sichtbar. |
| Status | Offen |

## CM-005 – Route-Kommentar Text ist Pflicht

| Feld | Beschreibung |
|---|---|
| Voraussetzung | User ist eingeloggt. Routendetailseite ist geöffnet. |
| Schritte | 1. Kommentartext leer lassen. 2. Button prüfen. |
| Erwartetes Ergebnis | Button „Kommentar abschicken“ ist deaktiviert, solange kein Text eingegeben wurde. |
| Status | Offen |

## CM-006 – Route-Kommentar ohne Bild speichern

| Feld | Beschreibung |
|---|---|
| Voraussetzung | User ist eingeloggt. Route existiert. |
| Schritte | 1. Kommentartext eingeben. 2. Kein Bild auswählen. 3. „Kommentar abschicken“ klicken. |
| Erwartetes Ergebnis | Kommentar wird gespeichert. Erfolgsmeldung „Kommentar gespeichert!“ erscheint. Kommentar steht oben in der Liste. |
| Status | Offen |

## CM-007 – Route-Kommentar mit Bild kleiner als 5 MB speichern

| Feld | Beschreibung |
|---|---|
| Voraussetzung | User ist eingeloggt. Es gibt ein gültiges Bild kleiner als 5 MB. |
| Schritte | 1. Kommentartext eingeben. 2. Bild auswählen. 3. Vorschau prüfen. 4. Kommentar abschicken. |
| Erwartetes Ergebnis | Bildvorschau wird angezeigt. Kommentar wird mit Bild gespeichert. |
| Status | Offen |

## CM-008 – Route-Kommentar Bild größer als 5 MB

| Feld | Beschreibung |
|---|---|
| Voraussetzung | User ist eingeloggt. Es gibt ein Bild größer als 5 MB. |
| Schritte | 1. Bild größer als 5 MB im Kommentarformular auswählen. |
| Erwartetes Ergebnis | Es erscheint „Bild darf maximal 5 MB groß sein.“ Kommentar wird nicht mit diesem Bild abgeschickt. |
| Status | Offen |

## CM-009 – Backend-Fehler beim Kommentar speichern

| Feld | Beschreibung |
|---|---|
| Voraussetzung | User ist eingeloggt. Backend liefert Fehler oder ist nicht erreichbar. |
| Schritte | 1. Kommentartext eingeben. 2. Backend stoppen oder Fehler provozieren. 3. Kommentar abschicken. |
| Erwartetes Ergebnis | Es erscheint „Kommentar konnte nicht gespeichert werden.“ Button wird wieder benutzbar. |
| Status | Offen |
