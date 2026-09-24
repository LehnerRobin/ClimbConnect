# Manuelle Testfälle – Routes

Bereich: **Routendetail, Community Grade und Safety Reports**

Hinweis: Falls es in der UI keinen direkten Link zur Route gibt, kann die Routendetailseite direkt über `/routes/{id}` geöffnet werden.

## RT-001 – Routendetail laden

| Feld | Beschreibung |
|---|---|
| Voraussetzung | Es gibt eine gültige Route-ID. |
| Schritte | 1. URL `http://localhost:4200/routes/{id}` öffnen. |
| Erwartetes Ergebnis | Route wird angezeigt. Name, Sektor, Grad, Stil, Länge und Beschreibung werden korrekt dargestellt. |
| Status | Offen |

## RT-002 – Community Grade anzeigen

| Feld | Beschreibung |
|---|---|
| Voraussetzung | Für die Route gibt es Community-Grade-Daten. |
| Schritte | 1. Routendetailseite öffnen. |
| Erwartetes Ergebnis | Neben dem Routengrad wird „Community: ...“ angezeigt. Wenn kein Wert vorhanden ist, wird nichts Falsches angezeigt. |
| Status | Offen |

## RT-003 – Sicherheitsmeldungen leerer Zustand

| Feld | Beschreibung |
|---|---|
| Voraussetzung | Für die Route gibt es keine offenen Sicherheitsmeldungen. |
| Schritte | 1. Routendetailseite öffnen. 2. Abschnitt „Sicherheitsmeldungen“ prüfen. |
| Erwartetes Ergebnis | Es erscheint „Keine offenen Sicherheitsmeldungen.“ |
| Status | Offen |

## RT-004 – Sicherheitsmeldung erstellen Button nur eingeloggt

| Feld | Beschreibung |
|---|---|
| Voraussetzung | Route existiert. |
| Schritte | 1. Ausloggen. 2. Routendetailseite öffnen. 3. Einloggen und Seite erneut öffnen. |
| Erwartetes Ergebnis | Ohne Login ist der Button „+ Meldung erstellen“ nicht sichtbar. Mit Login ist der Button sichtbar. |
| Status | Offen |

## RT-005 – Sicherheitsmeldung erstellen

| Feld | Beschreibung |
|---|---|
| Voraussetzung | User ist eingeloggt. Route existiert. |
| Schritte | 1. Routendetailseite öffnen. 2. „+ Meldung erstellen“ klicken. 3. Text eingeben. 4. Schweregrad wählen. 5. „Meldung abschicken“ klicken. |
| Erwartetes Ergebnis | Meldung wird gespeichert und direkt in der Liste angezeigt. Text und Schweregrad stimmen. |
| Status | Offen |

## RT-006 – Sicherheitsmeldung mit Bild kleiner als 5 MB

| Feld | Beschreibung |
|---|---|
| Voraussetzung | User ist eingeloggt. Es gibt ein gültiges Bild kleiner als 5 MB. |
| Schritte | 1. Report-Formular öffnen. 2. Text eingeben. 3. Bild hochladen. 4. Meldung abschicken. |
| Erwartetes Ergebnis | Upload funktioniert. Report wird mit Bild angezeigt. |
| Status | Offen |

## RT-007 – Sicherheitsmeldung Bild größer als 5 MB

| Feld | Beschreibung |
|---|---|
| Voraussetzung | User ist eingeloggt. Es gibt ein Bild größer als 5 MB. |
| Schritte | 1. Report-Formular öffnen. 2. Bild größer als 5 MB auswählen. |
| Erwartetes Ergebnis | Es erscheint „Bild darf maximal 5 MB groß sein.“ Report wird nicht automatisch abgeschickt. |
| Status | Offen |

## RT-008 – Begehung eintragen Link

| Feld | Beschreibung |
|---|---|
| Voraussetzung | User ist eingeloggt. Route existiert. |
| Schritte | 1. Routendetailseite öffnen. 2. Auf „+ Begehung eintragen“ klicken. |
| Erwartetes Ergebnis | Formular `/progress/new` wird geöffnet. Query-Parameter `routeId` und `routeName` sind gesetzt. |
| Status | Offen |

## RT-009 – Route nicht vorhanden

| Feld | Beschreibung |
|---|---|
| Voraussetzung | Backend läuft. Route-ID existiert nicht, z. B. `999999`. |
| Schritte | 1. URL `/routes/999999` öffnen. |
| Erwartetes Ergebnis | Es erscheint „Route konnte nicht geladen werden.“ |
| Status | Offen |
