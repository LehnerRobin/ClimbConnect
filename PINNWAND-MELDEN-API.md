# Pinnwand & Melden – API-Vertrag (Frontend ↔ Backend)

Das Frontend ist fertig und erwartet die folgenden Endpoints. Bis sie im Backend existieren,
simuliert `pinnwand-mock.interceptor.ts` sie im Browser.
**Wenn das Backend fertig ist:** in `frontend/src/environments/environment.development.ts`
`mockPinnwand: false` setzen.

Alle Zeitangaben in UTC als ISO-String (z. B. `2026-11-05T10:15:00Z`), JSON in camelCase.
Fehler bitte wie im restlichen Projekt als `{ "error": "Text" }` zurückgeben – der Text wird angezeigt.

---

## 1. Pinnwand eines Termins

Teilnehmer = Eintrag in `AppointmentUsers` **oder** Ersteller des Termins (`CreatedByUserId`).

### `GET /api/appointments/{appointmentId}/posts`  · eingeloggt (`User`)
Beiträge chronologisch, älteste zuerst.

```json
[
  {
    "id": 12,
    "appointmentId": 3,
    "authorId": 7,
    "authorUsername": "lena",
    "text": "Bringt jemand ein zweites Seil mit?",
    "createdAtUtc": "2026-11-05T09:12:00Z",
    "reportedByMe": false
  }
]
```
`reportedByMe` = hat der eingeloggte User diesen Beitrag schon gemeldet (steuert „Melden“ ↔ „Gemeldet“).

| Status | Wann |
|---|---|
| 404 | Termin existiert nicht |

### `POST /api/appointments/{appointmentId}/posts`  · nur Teilnehmer
Body: `{ "text": "..." }` (1–1000 Zeichen) → **201** mit dem neuen Beitrag (Format wie oben).

| Status | Wann |
|---|---|
| 400 | Text leer / zu lang |
| 403 | kein Teilnehmer |

### `DELETE /api/posts/{postId}`  · Verfasser oder Admin
→ **204**. Kopien in einem Fall bleiben erhalten und bekommen `deletedByAuthor = true`.

---

## 2. Beitrag melden

### `POST /api/posts/{postId}/reports`  · nur Teilnehmer des Termins
```json
{ "reason": "Beleidigung", "comment": "optional, max. 500 Zeichen" }
```
`reason` ∈ `Beleidigung` | `Belaestigung` | `UnangemessenerInhalt` | `Sonstiges`
(bei `Sonstiges` ist `comment` Pflicht).

→ **201** `{ "caseId": 4 }`

**Ablauf im Backend:**
1. Gibt es zu diesem Termin noch **keinen** Fall → Fall anlegen und **alle** aktuellen Pinnwand-Beiträge des Termins als Kopie in den Fall übernehmen.
2. Gibt es den Fall schon → **nichts** erneut kopieren. Nur wenn der gemeldete Beitrag nach dem Anlegen des Falls geschrieben wurde (also noch keine Kopie hat), diesen einen Beitrag nachtragen.
3. Meldung (wer, Grund, Kommentar, Zeit) an die Kopie des gemeldeten Beitrags hängen.
4. `LastReportAtUtc` des Falls auf jetzt setzen.

| Status | Wann |
|---|---|
| 400 | eigener Beitrag / ungültiger Grund / `Sonstiges` ohne Kommentar |
| 403 | kein Teilnehmer des Termins |
| 404 | Beitrag existiert nicht |
| 409 | dieser User hat diesen Beitrag schon gemeldet |

> Die Kopien dürfen **keinen** Fremdschlüssel mit Cascade-Delete auf Beitrag oder Termin haben – sie müssen das Löschen überleben. Titel, Datum und Gebietsname des Termins daher im Fall mitspeichern.

---

## 3. Admin: Fälle  · alle Endpoints nur `Admin`

### `GET /api/moderation/cases`
```json
[
  {
    "id": 4,
    "appointmentId": 3,
    "appointmentTitle": "Feierabend-Session",
    "appointmentDate": "2026-11-08T15:00:00Z",
    "areaName": "Traunstein",
    "appointmentDeleted": false,
    "createdAtUtc": "2026-11-05T10:00:00Z",
    "lastReportAtUtc": "2026-11-05T10:20:00Z",
    "deleteAfterUtc": "2026-12-05T10:20:00Z",
    "reportCount": 3,
    "reportedPostCount": 2,
    "postCount": 9
  }
]
```
`deleteAfterUtc` = `lastReportAtUtc + 30 Tage`.

### `GET /api/moderation/cases/{id}`
Wie oben, zusätzlich:
```json
"posts": [
  {
    "id": 51,
    "originalPostId": 12,
    "authorId": 9,
    "authorUsername": "jonas",
    "text": "…",
    "createdAtUtc": "2026-11-05T08:00:00Z",
    "deletedByAuthor": false,
    "reports": [
      {
        "id": 77,
        "reporterId": 7,
        "reporterUsername": "lena",
        "reason": "Beleidigung",
        "comment": null,
        "createdAtUtc": "2026-11-05T10:00:00Z"
      }
    ]
  }
]
```
`reports` leer = Beitrag dient nur als Kontext. 404, wenn der Fall nicht (mehr) existiert.

### `DELETE /api/moderation/cases/{id}` – „Fall erledigen“
Löscht Fall, Kopien und Meldungen → **204**.

Hinweis: Danach darf derselbe User denselben Beitrag wieder melden (neuer Fall). Wer das nicht will,
merkt sich „schon gemeldet“ getrennt vom Fall.

---

## 4. Automatisches Löschen
Fälle mit `LastReportAtUtc` älter als 30 Tage löschen – z. B. per `BackgroundService` einmal täglich,
oder einfach beim Aufruf von `GET /api/moderation/cases` vorher aufräumen.

## Vorschlag Datenmodell
- `PinnwandPost` (Id, AppointmentId → Cascade, UserId, Text, CreatedAtUtc)
- `ModerationCase` (Id, AppointmentId **ohne FK**, AppointmentTitle, AppointmentDate, AreaName, CreatedAtUtc, LastReportAtUtc)
- `CasePost` (Id, CaseId → Cascade, OriginalPostId **ohne FK**, AuthorId, AuthorUsername, Text, CreatedAtUtc, DeletedByAuthor)
- `PostReport` (Id, CasePostId → Cascade, ReporterId, ReporterUsername, Reason, Comment, CreatedAtUtc) – Unique-Index auf (CasePostId, ReporterId)
