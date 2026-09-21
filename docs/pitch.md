# ClimbConnect – Projektpitch

Stell dir vor, du kletterst seit einem Jahr in der Halle und willst endlich
auch mal draußen am Fels klettern. Du suchst online nach Gebieten in
Oberösterreich – findest aber kaum verlässliche Informationen. Die meisten
Daten sind veraltet, verstreut oder nur als gedruckter Kletterführer erhältlich.
Ob ein Zustieg noch frei ist, ob ein Griff ausgebrochen ist oder ob ein
Bohrhaken erneuert wurde – das erfährt man bestenfalls zufällig in einer
WhatsApp-Gruppe.

Dazu kommt: Du weißt nicht, wen du fragen sollst oder wie du jemanden auf
ähnlichem Level findest, der mitkommen will. Für Einsteiger ins Outdoor-Klettern
ist genau das oft die größte Hürde.

**Genau hier setzt ClimbConnect an.**

---

## Was ist ClimbConnect?

ClimbConnect ist eine Webplattform die Seilklettergebiete in Oberösterreich
zentral erfasst, persönliche Fortschritte dokumentiert und Kletterer für
gemeinsame Ausflüge vernetzt. Der Fokus liegt bewusst auf Seilklettern –
nicht auf Bouldern – weil es in OÖ sehr viele Gebiete gibt und die
Informationslage dort besonders lückenhaft ist.

---

## Die wichtigsten Features

**Gebiete, Sektoren & Routen**
Alle Seilklettergebiete in Oberösterreich an einem Ort – strukturiert nach
Gebieten, Sektoren (z.B. „Nordwand") und einzelnen Routen. Jede Route hat
Name, Schwierigkeitsgrad, Stil und Länge. Schwierigkeitsgrade werden in
der gewählten Skala des Benutzers angezeigt: Französisch (6b), UIAA (VII-)
oder Amerikanisch (5.10d) – einmal einstellen, überall angezeigt.

**Fortschritt tracken**
Benutzer können festhalten welche Routen sie geklettert haben: Status
(Rotpunkt, Flash, Onsight oder noch als Projekt offen), Begehungsart
(Toprope oder Vorstieg), Anzahl der Versuche und eine persönliche Notiz.
Wer das Gefühl hat, dass eine Route sich anders anfühlt als ihr offizieller
Grad – zum Beispiel weil die Griffe abgegriffen sind – kann eine subjektive
Bewertung mit kurzem Kommentar abgeben. Aus allen Bewertungen entsteht
automatisch ein Community-Grad pro Route.

**Entwicklung sehen**
Im persönlichen Profil sieht man die eigene Gradentwicklung als Chart über
Zeit: Wann hat man welchen Schwierigkeitsgrad rotpunkt geklettert? So sieht
man auf einen Blick wie man sich über die Saison verbessert hat – und bleibt
motiviert.

**Wer ist heute dort?**
Direkt in der Gebietsübersicht sieht man wie viele Leute laut Terminplaner
heute in einem Gebiet klettern und wie viele vorhaben zu kommen. So weiß man
schon vor dem Fahren ob ein Gebiet gerade voll ist.

**Kletterpartner finden**
Über den Terminplaner kann man für einen bestimmten Tag in einem Gebiet einen
Termin erstellen – mit Mindest- und Maximalanzahl an Teilnehmern. Andere
Benutzer können beitreten. Der Durchschnittsgrad der Gruppe ist für alle
sichtbar, damit man einschätzen kann ob das Level passt. Das ist kein Social
Media – es gibt keinen Chat, kein Follow-System. Nur ein einfaches Tool um
gemeinsam Klettern zu planen.

**Aktuelle Informationen teilen**
Benutzer können Kommentare mit Fotos zu Gebieten oder einzelnen Routen
schreiben – zum Beispiel wenn ein Zustieg gesperrt ist oder sich die
Bedingungen geändert haben. Für Sicherheitsprobleme wie lockere Griffe oder
beschädigte Haken gibt es zusätzlich eine Meldefunktion mit Schweregrad.

---

## Technik

- **Backend:** C# mit .NET 8 Minimal API und Entity Framework Core
- **Datenbank:** PostgreSQL
- **Authentifizierung:** Keycloak (Login, Registrierung, Rollenverwaltung)
- **Frontend:** Angular
- **Deployment:** Automatisiert über GitHub Actions CI/CD Pipeline
- **Skalierung:** Durch modularen Aufbau einfach auf weitere Bundesländer
  oder Sportarten erweiterbar

---

## Individuelle Beiträge

**Robin Lehner – Backend & Systemarchitektur**
Konzeption und Implementierung des domänenspezifischen Datenmodells
(Gebiete → Sektoren → Routen), der REST-API mit allen Endpunkten, der
Keycloak-Authentifizierung sowie der CI/CD-Pipeline und des Deployments.

**Mohamed Attia – Frontend-Architektur & Datendarstellung**
Aufbau der Angular-Anwendung, Komponentenstruktur, Service-Layer für die
API-Kommunikation, Verwaltungsansichten für Gebiete und Routen sowie die
interaktiven Charts für die Gradentwicklung.

**Faru Hamid – Frontend & Benutzerorientierte Features**
Profil und Fortschrittsansichten, Terminplaner-UI, Kommentar- und Meldesystem
mit Bild-Upload sowie responsive Gestaltung für Desktop und Mobilgerät.

Alle drei Teammitglieder arbeiten nach einem agilen Vorgehensmodell –
jeder unterstützt die anderen und ist in alle Bereiche des Projekts eingebunden.

---

## Ausblick

ClimbConnect startet mit Seilklettergebieten in Oberösterreich. Langfristig
ist eine Erweiterung auf andere Bundesländer wie Salzburg oder Niederösterreich
denkbar – sowie die Integration von Bouldergebieten. Die modulare Architektur
macht genau das möglich.

Unser Ziel: Eine Plattform die Kletterer beim Klettern wirklich begleitet –
nicht nur als Informationsquelle, sondern als persönliches Notizbuch,
Trainingstagbuch und Planungstool in einem.

