# UI-Wireframes für Hauptseiten

## Übersicht

Dieses Dokument enthält einfache Wireframes für die wichtigsten Seiten der ClimbConnect-Anwendung.

Enthaltene Seiten:

- Areas Übersicht
- Area Detailseite
- Profile Seite
- Appointments Seite

Die Wireframes dienen als visuelle Grundlage für das Frontend und zeigen grob den Aufbau der Seiten.

---

# 1. Areas Übersicht

## Ziel

Benutzer sollen verfügbare Klettergebiete sehen und ein Gebiet auswählen können.

## Desktop Wireframe

```txt
+--------------------------------------------------------------+
| ClimbConnect                         Home Areas Profile Login |
+--------------------------------------------------------------+

+--------------------------------------------------------------+
| Climbing Areas                                               |
| Entdecke verfügbare Klettergebiete.                          |
+--------------------------------------------------------------+

+------------------+  +------------------+  +------------------+
|      Bild        |  |      Bild        |  |      Bild        |
|                  |  |                  |  |                  |
+------------------+  +------------------+  +------------------+
| Gebiet Name      |  | Gebiet Name      |  | Gebiet Name      |
| Standort         |  | Standort         |  | Standort         |
| Beschreibung     |  | Beschreibung     |  | Beschreibung     |
| Besucher heute   |  | Besucher heute   |  | Besucher heute   |
+------------------+  +------------------+  +------------------+

+--------------------------------------------------------------+
| Footer                                                       |
+--------------------------------------------------------------+
```






# Mobile Wireframe

```txt

+----------------------+
| ClimbConnect         |
| Home Areas Profile   |
| Login Register       |
+----------------------+

+----------------------+
| Climbing Areas       |
| Beschreibung         |
+----------------------+

+----------------------+
| Bild                 |
| Gebiet Name          |
| Standort             |
| Beschreibung         |
| Besucher heute       |
+----------------------+

+----------------------+
| Bild                 |
| Gebiet Name          |
| Standort             |
| Beschreibung         |
| Besucher heute       |
+----------------------+
```


# 2. Area Detailseite

## Ziel

Benutzer sollen Details zu einem Klettergebiet sehen und Sektoren mit Routen öffnen können.

## Desktop Wireframe


```txt
+--------------------------------------------------------------+
| ClimbConnect                         Home Areas Profile Login |
+--------------------------------------------------------------+

+--------------------------------------------------------------+
| Zurück zu Areas                                              |
+--------------------------------------------------------------+

+----------------------+---------------------------------------+
|        Bild          | Gebiet Name                           |
|                      | Standort                              |
|                      | Beschreibung                          |
+----------------------+---------------------------------------+

+------------------------------------------+-------------------+
| Sektoren & Routen                        | Kommende Termine  |
|                                          |                   |
| +--------------------------------------+ | Keine Termine     |
| | Sektor 1                       v     | | vorhanden         |
| +--------------------------------------+ |                   |
| | Route Name | Grad | Länge | Stil     | +-------------------+
| | Route Name | Grad | Länge | Stil     | | Kommentare        |
| +--------------------------------------+ |                   |
|                                          | Keine Kommentare  |
| +--------------------------------------+ | vorhanden         |
| | Sektor 2                       >     | +-------------------+
| +--------------------------------------+ |
+------------------------------------------+-------------------+

+--------------------------------------------------------------+
| Footer                                                       |
+--------------------------------------------------------------+
```




# Mobile Wireframe

```txt

+----------------------+
| ClimbConnect         |
| Navigation           |
+----------------------+

+----------------------+
| Zurück zu Areas      |
+----------------------+

+----------------------+
| Bild                 |
| Gebiet Name          |
| Standort             |
| Beschreibung         |
+----------------------+

+----------------------+
| Sektoren & Routen    |
| Sektor 1        v    |
| Route | Grad | Stil  |
| Sektor 2        >    |
+----------------------+

+----------------------+
| Kommende Termine     |
| Keine Termine        |
+----------------------+

+----------------------+
| Kommentare           |
| Keine Kommentare     |
+----------------------+
```




# 3. Profile Seite

## Ziel

Benutzer sollen ihr Profil und ihre Kletter-Einstellungen verwalten können.

## Desktop Wireframe

```txt

+--------------------------------------------------------------+
| ClimbConnect                         Home Areas Profile Login |
+--------------------------------------------------------------+

+--------------------------------------------------------------+
| Mein Profil                                                  |
| Verwalte dein Kletterprofil und Einstellungen.               |
+--------------------------------------------------------------+

+------------------------------------------+-------------------+
| Profilinformationen                      | Statistiken       |
|                                          |                   |
| Über mich                                | Begehungen: -     |
| +--------------------------------------+ | Projekte: -       |
| | Textfeld                             | | Lieblingsgebiet:- |
| +--------------------------------------+ |                   |
|                                          |                   |
| Bevorzugte Bewertungsskala              |                   |
| +--------------------------------------+ |                   |
| | UIAA / Französisch / Amerikanisch    | |                   |
| +--------------------------------------+ |                   |
|                                          |                   |
| Durchschnittlicher Grad                 |                   |
| +--------------------------------------+ |                   |
| | Eingabefeld                          | |                   |
| +--------------------------------------+ |                   |
|                                          |                   |
|                         Profil speichern|                   |
+------------------------------------------+-------------------+

+--------------------------------------------------------------+
| Footer                                                       |
+--------------------------------------------------------------+
```




# Mobile Wireframe

```txt

+----------------------+
| ClimbConnect         |
| Navigation           |
+----------------------+

+----------------------+
| Mein Profil          |
| Beschreibung         |
+----------------------+

+----------------------+
| Profilinformationen  |
| Über mich            |
| Textfeld             |
| Bewertungsskala      |
| Auswahlfeld          |
| Durchschnittsgrad    |
| Eingabefeld          |
| Profil speichern     |
+----------------------+

+----------------------+
| Statistiken          |
| Begehungen: -        |
| Projekte: -          |
| Lieblingsgebiet: -   |
+----------------------+
```




# 4. Appointments Seite

## Ziel

Benutzer sollen Termine sehen und später eventuell neue Termine erstellen können.

## Desktop Wireframe

```txt

+--------------------------------------------------------------+
| ClimbConnect                         Home Areas Profile Login |
+--------------------------------------------------------------+

+--------------------------------------------------------------+
| Appointments                                                 |
| Plane oder finde gemeinsame Klettertermine.                  |
+--------------------------------------------------------------+

+------------------------------------------+-------------------+
| Terminliste                              | Filter            |
|                                          |                   |
| +--------------------------------------+ | Gebiet            |
| | Titel                                | | Datum            |
| | Datum / Uhrzeit                      | | Schwierigkeit    |
| | Gebiet                               | |                   |
| | Teilnehmer                           | | [Filter anwenden]|
| +--------------------------------------+ |                   |
|                                          |                   |
| +--------------------------------------+ |                   |
| | Titel                                | |                   |
| | Datum / Uhrzeit                      | |                   |
| | Gebiet                               | |                   |
| | Teilnehmer                           | |                   |
| +--------------------------------------+ |                   |
+------------------------------------------+-------------------+

+--------------------------------------------------------------+
| Footer                                                       |
+--------------------------------------------------------------+
```




# Mobile Wireframe

```txt

+----------------------+
| ClimbConnect         |
| Navigation           |
+----------------------+

+----------------------+
| Appointments         |
| Beschreibung         |
+----------------------+

+----------------------+
| Filter               |
| Gebiet               |
| Datum                |
| Schwierigkeit        |
| Filter anwenden      |
+----------------------+

+----------------------+
| Termin               |
| Titel                |
| Datum / Uhrzeit      |
| Gebiet               |
| Teilnehmer           |
+----------------------+

+----------------------+
| Termin               |
| Titel                |
| Datum / Uhrzeit      |
| Gebiet               |
| Teilnehmer           |
+----------------------+
