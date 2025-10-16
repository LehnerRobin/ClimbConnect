# Architektur (Kurzüberblick)

- **API-First**: REST-API in ASP.NET Core; Frontend konsumiert JSON-Endpunkte.
- **Datenbank**: Oracle SQL; Tabellen u. a. Users, Areas, Routes, Progress, Appointments, Comments, Reports, Photos.
- **Auth**: ASP.NET Identity + JWT (geplant).
- **Storage**: Datei-Uploads (Fotos) in `/uploads/` (Pfade + Metadaten in DB).
- **Frontend**: Prototyp (HTML/CSS/JS), später Angular (Routing, Services, Guards).
