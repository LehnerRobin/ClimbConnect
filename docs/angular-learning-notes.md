# Angular Learning Notes

## Setup
- Node.js installiert
- Angular CLI installiert (`npm install -g @angular/cli`)
- Projekt erstellt mit:
  `ng new climbconnect-frontend --routing --style=scss`
- Starten mit:
  `ng serve`

## Projektstruktur
- `src/app`
- `features` für fachliche Seiten
- Standalone Components (kein NgModule)

## Components
- Jede Seite ist eine eigene Component
- Beispiel:
  - AreasPage
  - ProfilePage
  - AppointmentsPage

## Routing
- Routing über `app.routes.ts`
- Beispiel:
  - `/areas`
  - `/profile`
  - `/appointments`
- Anzeige über `<router-outlet>`

## Services (Grundidee)
- Services kapseln Logik und API-Zugriffe
- Werden später für HttpClient genutzt

## HttpClient (Grundidee)
- Für API-Anbindung
- Wird über `HttpClientModule` verwendet

## Observables (Grundidee)
- Asynchrone Datenströme
- Rückgabewert von HttpClient
