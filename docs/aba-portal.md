# ClimbConnect – ABA Portal Text

---

## Ausgangslage

Seilklettern erfreut sich in Österreich wachsender Beliebtheit, doch wer in
Oberösterreich nach verlässlichen Informationen zu Klettergebieten sucht,
stößt schnell auf ein grundlegendes Problem: Informationen zu Gebieten,
Zustiegen, Routen und aktuellen Bedingungen sind häufig veraltet, auf
verschiedene Plattformen verstreut oder gar nicht digital verfügbar. Sogenannte
Kletterführer (Topos) sind oft nur als gedruckte Bücher erhältlich und
spiegeln nicht den aktuellen Zustand eines Gebiets wider. Hinweise auf
gesperrte Zustiege, beschädigte Bohrhaken oder ausgebrochene Griffe werden
bestenfalls in informellen WhatsApp-Gruppen oder Foren geteilt – ohne
zentrale Anlaufstelle.

Ein weiteres Problem betrifft besonders Einsteiger: Es fehlt ein digitales
Werkzeug, das speziell für den Kontext des Kletterns entwickelt wurde und
es ermöglicht, den persönlichen Fortschritt strukturiert zu dokumentieren.
Klettern ist eine sehr individuelle Sportart – die Entwicklung von
Schwierigkeitsgraden, die Art der Begehung (Toprope oder Vorstieg), die
Anzahl der benötigten Versuche und die subjektive Einschätzung einer Route
sind wichtige Parameter, die bisher nirgendwo gebündelt erfasst werden können.

Hinzu kommt, dass es insbesondere für neue Kletterer schwierig ist, einen
geeigneten Seilpartner für Outdoor-Touren zu finden. Wer von der Halle an
den Fels wechseln möchte, benötigt einen Partner auf ähnlichem Niveau –
doch ein passendes digitales Tool, das diese Vernetzung im Kontext von
konkreten Klettergebieten ermöglicht, existiert bisher nicht.

---

## Untersuchungsanliegen der individuellen Themenstellungen

**Robin Lehner – Backend & Systemarchitektur**
Im Rahmen der Diplomarbeit wird untersucht, wie ein domänenspezifisches
Datenmodell für Seilklettergebiete konzipiert und als REST-API umgesetzt
werden kann. Dabei wird analysiert, wie Klettergebiete, Sektoren, Routen
und Schwierigkeitsgrade strukturiert gespeichert und über eine API bereitgestellt
werden können. Ein weiterer Schwerpunkt liegt auf der Integration einer
rollenbasierten Authentifizierung mittels Keycloak sowie der automatisierten
Bereitstellung der Anwendung über eine CI/CD-Pipeline. Zusätzlich wird
untersucht, wie Fortschrittsdaten aggregiert und als Statistiken (z.B.
Gradentwicklung über Zeit) aufbereitet werden können.

**Mohamed Attia – Frontend-Architektur & Datendarstellung**
Im Rahmen der Diplomarbeit wird untersucht, wie eine moderne Single-Page-Application
mit Angular strukturiert und aufgebaut werden kann. Dabei wird analysiert, wie
die Anwendung in wiederverwendbare Komponenten aufgeteilt, die Kommunikation
mit der REST-API über einen zentralen Service-Layer abgewickelt und komplexe
Daten wie Gradentwicklungen als interaktive Charts dargestellt werden können.
Ein weiterer Schwerpunkt liegt auf der Verwaltung von Klettergebieten, Sektoren
und Routen im Frontend sowie auf der Umsetzung einer konsistenten Benutzeroberfläche.

**Faru Hamid – Frontend & Benutzerorientierte Features**
Im Rahmen der Diplomarbeit wird untersucht, wie benutzerorientierte Features
einer Webanwendung – insbesondere Fortschrittstracking, Terminplanung und
Community-Funktionen – in Angular umgesetzt werden können. Dabei wird analysiert,
wie Benutzerinteraktionen wie das Eintragen von Begehungen, das Beitreten zu
Terminen und das Hochladen von Bildern in einer responsiven Oberfläche
ansprechend und intuitiv gestaltet werden können. Ein weiterer Schwerpunkt
liegt auf der nutzerfreundlichen Darstellung von Profil- und Statistikseiten.

---

## Zielsetzung

Ziel der Diplomarbeit ist die Entwicklung der Webplattform ClimbConnect,
die Seilklettergebiete in Oberösterreich zentral verwaltet und dokumentiert.
Die Plattform soll eine verlässliche, aktuell gehaltene Datenbasis für
Klettergebiete, Sektoren und Routen schaffen und gleichzeitig Funktionen
für persönliches Fortschrittstracking sowie Community-Interaktion bieten.

Darüber hinaus soll untersucht werden, wie moderne Webtechnologien –
insbesondere eine .NET 8 REST-API mit Angular-Frontend, Keycloak-Authentifizierung
und containerisiertem Deployment – im Rahmen einer Diplomarbeit eingesetzt
und hinsichtlich Wartbarkeit, Erweiterbarkeit und Benutzerfreundlichkeit
bewertet werden können.

---

## Geplantes Ergebnis der individuellen Themenstellungen

**Robin Lehner – Backend & Systemarchitektur**
Als Ergebnis entsteht eine vollständige REST-API auf Basis von .NET 8 Minimal
API und Entity Framework Core mit PostgreSQL-Datenbank. Die API stellt alle
notwendigen Endpunkte für die Verwaltung von Klettergebieten (Areas, Sektoren,
Routen), das Fortschrittstracking (inkl. Gradentwicklung und subjektiver
Routenbewertung), den Terminplaner sowie das Kommentar- und Meldesystem bereit.
Schwierigkeitsgrade werden intern in der französischen Skala gespeichert und
per API wahlweise in der französischen, amerikanischen oder UIAA-Skala
ausgegeben. Die Anwendung ist durch Keycloak abgesichert und wird über eine
GitHub Actions CI/CD-Pipeline automatisiert deployed.

**Mohamed Attia – Frontend-Architektur & Datendarstellung**
Als Ergebnis entsteht eine strukturierte Angular-Anwendung mit klarer
Komponentenarchitektur, zentralem HTTP-Service-Layer und durchgängigem
Routing-Konzept. Mohamed verantwortet dabei insbesondere die Umsetzung
der Gebiets- und Routenverwaltung im Frontend sowie die Integration von
interaktiven Charts zur Darstellung der Gradentwicklung über Zeit.
Die Anwendung kommuniziert vollständig über die REST-API und ist so
aufgebaut, dass neue Features ohne großen Aufwand ergänzt werden können.

**Faru Hamid – Frontend & Benutzerorientierte Features**
Als Ergebnis entstehen die benutzerorientierten Kernbereiche der Anwendung:
das persönliche Profil mit Fortschrittsübersicht, die Detailansichten für
Routen und Begehungen, der Terminplaner mit Beitritt-Funktion sowie das
Kommentar- und Meldesystem inklusive Bild-Upload. Faru verantwortet
außerdem die responsive Gestaltung der Anwendung, sodass ClimbConnect
sowohl am Desktop als auch am Mobilgerät einwandfrei nutzbar ist.

**Gemeinsamer agiler Ansatz**
Die Entwicklung erfolgt nach einem agilen Vorgehensmodell. Alle drei
Teammitglieder arbeiten iterativ an gemeinsamen User Stories, unterstützen
sich gegenseitig und sind in alle Bereiche des Projekts eingebunden.
Die beschriebenen Schwerpunkte spiegeln die jeweilige Hauptverantwortung
wider, nicht eine strikte Trennung der Aufgaben.

