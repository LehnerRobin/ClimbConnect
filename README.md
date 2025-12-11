# ClimbConnect  
Webanwendung zur Erfassung und Analyse von Kletterfortschritten in den Klettergebieten Oberösterreichs

---

## 🚀 Überblick

**ClimbConnect** ist eine Webanwendung, mit der Kletterer:

- ihre Fortschritte pro Route dokumentieren  
- Kommentare & Safety-Reports erfassen  
- Termine je Gebiet einsehen und sich anmelden  
- Gebiets- und Routeninformationen abrufen  
- Statistiken über ihren Fortschritt anzeigen lassen  

Das Projekt wird gemäß den Anforderungen der HTL im Scrum-Prozess umgesetzt  
(**User Stories**, **Akzeptanzkriterien**, **Sprint Planning**, **Sprint Review**, **Backlog Management**).

---

## 🧩 Architekturüberblick

### **Backend – .NET 8 Minimal API**
- REST API-Endpunkte für Areas, Routes, Progress, Appointments, Comments, Reports  
- EF Core mit InMemory-Datenbank für den MVP  
- API-Spezifikation unter `/docs/api-spec.md`  
- Swagger/OpenAPI für automatische Dokumentation  

### **Frontend – Angular**
- Komponentenlogik für Areas, Routes, Profile, Appointments, Progress  
- Services zur API-Kommunikation  
- Routing, Shared Modules, Responsive Layout  

### **Datenbank**
- ER-Diagramm in `/docs/database/`  
- Tabellenentwurf + Felder  
- Seed-Daten für Testzwecke  

