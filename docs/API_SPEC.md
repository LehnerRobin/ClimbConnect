# API-Spezifikation (Entwurf)

## Auth
- POST /api/auth/register
- POST /api/auth/login

## Areas & Routes
- GET /api/areas
- GET /api/areas/{id}
- GET /api/areas/{id}/routes
- GET /api/routes/{id}

## Progress
- GET /api/progress/me
- POST /api/progress
- PUT /api/progress/{id}
- DELETE /api/progress/{id}

## Appointments (Subscribe/Terminplaner)
- GET /api/areas/{id}/appointments
- POST /api/areas/{id}/appointments
- POST /api/appointments/{id}/subscribe
- DELETE /api/appointments/{id}/subscribe

## Comments & Reports
- GET /api/areas/{id}/comments
- POST /api/areas/{id}/comments
- GET /api/routes/{id}/comments
- POST /api/routes/{id}/comments
- POST /api/reports  (type: area|route, text, photoId?)

## Photos
- POST /api/routes/{id}/photos
- POST /api/areas/{id}/photos
- GET /api/photos/{id}
