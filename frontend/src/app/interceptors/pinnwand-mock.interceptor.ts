import { HttpErrorResponse, HttpInterceptorFn, HttpRequest, HttpResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Observable, catchError, delay, filter, map, of, switchMap, take, throwError } from 'rxjs';
import { environment } from '../../environments/environment';
import { AuthService } from '../../services/auth.service';
import {
  CasePost, ModerationCaseDetail, ModerationCaseSummary, PinnwandPost, ReportCreateRequest
} from '../../models/pinnwand.model';

/**
 * ─────────────────────────────────────────────────────────────────────────────
 *  NUR FÜR DIE ENTWICKLUNG: Simuliert Pinnwand-, Melde- und Fall-Endpoints im Browser,
 *  solange das Backend sie noch nicht hat. Aktiv nur mit environment.mockPinnwand = true.
 *  Sobald das Backend fertig ist: mockPinnwand in environment.development.ts auf false.
 *
 *  Daten liegen im localStorage ("cc-mock-pinnwand"), damit sie ein Neuladen überleben.
 *  Termin- und Gebietsnamen holt der Mock beim echten Backend.
 * ─────────────────────────────────────────────────────────────────────────────
 */

const STORAGE_KEY = 'cc-mock-pinnwand';
const RETENTION_DAYS = 30;

interface StoredReport {
  id: number; reporterId: number; reporterUsername: string;
  reason: ReportCreateRequest['reason']; comment: string | null; createdAtUtc: string;
}
interface StoredCasePost extends Omit<CasePost, 'reports'> { reports: StoredReport[]; }
interface StoredCase {
  id: number; appointmentId: number; appointmentTitle: string; appointmentDate: string | null;
  areaName: string | null; createdAtUtc: string; lastReportAtUtc: string; posts: StoredCasePost[];
}
interface StoredPost extends Omit<PinnwandPost, 'reportedByMe'> { reportedBy: number[]; }
interface Store {
  nextId: number;
  posts: StoredPost[];
  seededAppointments: number[];
  cases: StoredCase[];
}

function loadStore(): Store {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (raw) return JSON.parse(raw) as Store;
  } catch { /* localStorage nicht verfügbar → leer starten */ }
  return { nextId: 1, posts: [], seededAppointments: [], cases: [] };
}

function saveStore(store: Store): void {
  try { localStorage.setItem(STORAGE_KEY, JSON.stringify(store)); } catch { /* egal im Mock */ }
}

const ok = <T>(body: T, status = 200) => of(new HttpResponse<T>({ status, body })).pipe(delay(250));
const fail = (req: HttpRequest<unknown>, status: number, error: string) =>
  throwError(() => new HttpErrorResponse({ status, error: { error }, url: req.url }));

/** Beispiel-Beiträge anderer Leute, damit man sofort etwas zum Melden hat. */
function seedPosts(store: Store, appointmentId: number): void {
  if (store.seededAppointments.includes(appointmentId)) return;
  store.seededAppointments.push(appointmentId);
  const now = Date.now();
  const samples: [number, string, string, number][] = [
    [900001, 'lena_klettert', 'Freu mich schon! Bringt jemand ein zweites 70m-Seil mit?', 26],
    [900002, 'max_boulder',   'Ich kann eins mitnehmen. Treffpunkt bleibt beim Parkplatz?', 20],
    [900003, 'jonas_x',       'Wer so langsam klettert wie du letztes Mal, soll lieber gleich daheim bleiben. Echt peinlich.', 9],
    [900001, 'lena_klettert', 'Bitte bleib freundlich, Jonas.', 5]
  ];
  for (const [authorId, authorUsername, text, hoursAgo] of samples) {
    store.posts.push({
      id: store.nextId++, appointmentId, authorId, authorUsername, text,
      createdAtUtc: new Date(now - hoursAgo * 3600_000).toISOString(), reportedBy: []
    });
  }
}

function addDays(iso: string, days: number): string {
  return new Date(new Date(iso).getTime() + days * 86_400_000).toISOString();
}

function purgeExpired(store: Store): void {
  const now = Date.now();
  store.cases = store.cases.filter(c => new Date(addDays(c.lastReportAtUtc, RETENTION_DAYS)).getTime() > now);
}

function toSummary(c: StoredCase, store: Store): ModerationCaseSummary {
  const reports = c.posts.flatMap(p => p.reports);
  return {
    id: c.id, appointmentId: c.appointmentId, appointmentTitle: c.appointmentTitle,
    appointmentDate: c.appointmentDate, areaName: c.areaName,
    appointmentDeleted: false, // weiß der Mock nicht – liefert das echte Backend
    createdAtUtc: c.createdAtUtc, lastReportAtUtc: c.lastReportAtUtc,
    deleteAfterUtc: addDays(c.lastReportAtUtc, RETENTION_DAYS),
    reportCount: reports.length,
    reportedPostCount: c.posts.filter(p => p.reports.length > 0).length,
    postCount: c.posts.length
  };
}

function snapshot(store: Store, post: StoredPost): StoredCasePost {
  return {
    id: store.nextId++, originalPostId: post.id, authorId: post.authorId,
    authorUsername: post.authorUsername, text: post.text, createdAtUtc: post.createdAtUtc,
    deletedByAuthor: false, reports: []
  };
}

export const pinnwandMockInterceptor: HttpInterceptorFn = (req, next) => {
  if (!environment.mockPinnwand) return next(req);

  const path = new URL(req.url, window.location.origin).pathname;
  const isPinnwand = /^\/api\/(appointments\/\d+\/posts|posts\/\d+(\/reports)?|moderation\/cases(\/\d+)?)$/.test(path);
  if (!isPinnwand) return next(req);

  const auth   = inject(AuthService);
  const userId = auth.getUserId();
  if (userId === null) return fail(req, 401, 'Nicht eingeloggt');
  const username = auth.getUsername() ?? `user${userId}`;

  const store = loadStore();
  purgeExpired(store);
  let m: RegExpMatchArray | null;

  // ── Pinnwand ──────────────────────────────────────────────────────────────
  if ((m = path.match(/^\/api\/appointments\/(\d+)\/posts$/))) {
    const appointmentId = Number(m[1]);
    seedPosts(store, appointmentId);

    if (req.method === 'GET') {
      saveStore(store);
      const posts: PinnwandPost[] = store.posts
        .filter(p => p.appointmentId === appointmentId)
        .map(({ reportedBy, ...p }) => ({ ...p, reportedByMe: reportedBy.includes(userId) }));
      return ok(posts);
    }
    if (req.method === 'POST') {
      const text = String((req.body as { text?: string })?.text ?? '').trim();
      if (!text) return fail(req, 400, 'Text ist erforderlich');
      const post: StoredPost = {
        id: store.nextId++, appointmentId, authorId: userId, authorUsername: username, text,
        createdAtUtc: new Date().toISOString(), reportedBy: []
      };
      store.posts.push(post);
      saveStore(store);
      const { reportedBy, ...rest } = post;
      return ok<PinnwandPost>({ ...rest, reportedByMe: false }, 201);
    }
  }

  if ((m = path.match(/^\/api\/posts\/(\d+)$/)) && req.method === 'DELETE') {
    const post = store.posts.find(p => p.id === Number(m![1]));
    if (!post) return fail(req, 404, 'Beitrag nicht gefunden');
    if (post.authorId !== userId && !auth.hasRole('admin')) return fail(req, 403, 'Nur eigene Beiträge löschen');
    store.posts = store.posts.filter(p => p.id !== post.id);
    // Kopie im Fall bleibt, wird nur markiert
    store.cases.forEach(c => c.posts.forEach(cp => { if (cp.originalPostId === post.id) cp.deletedByAuthor = true; }));
    saveStore(store);
    return ok(null, 204);
  }

  // ── Melden ────────────────────────────────────────────────────────────────
  if ((m = path.match(/^\/api\/posts\/(\d+)\/reports$/)) && req.method === 'POST') {
    const post = store.posts.find(p => p.id === Number(m![1]));
    const body = req.body as ReportCreateRequest;
    if (!post) return fail(req, 404, 'Beitrag nicht gefunden');
    if (post.authorId === userId) return fail(req, 400, 'Eigene Beiträge können nicht gemeldet werden');
    if (post.reportedBy.includes(userId)) return fail(req, 409, 'Du hast diesen Beitrag bereits gemeldet');
    if (!body?.reason) return fail(req, 400, 'Grund ist erforderlich');
    if (body.reason === 'Sonstiges' && !body.comment?.trim()) return fail(req, 400, 'Bei "Sonstiges" bitte beschreiben');

    const now = new Date().toISOString();
    const existing = store.cases.find(c => c.appointmentId === post.appointmentId);

    // Termin-Infos beim echten Backend holen (nur beim ersten Bericht nötig)
    const appointmentInfo$: Observable<{ title: string; date: string | null; areaName: string | null }> = existing
      ? of({ title: existing.appointmentTitle, date: existing.appointmentDate, areaName: existing.areaName })
      : fetchAppointmentInfo(req, next, post.appointmentId);

    return appointmentInfo$.pipe(
      switchMap(info => {
        let theCase = existing;
        if (!theCase) {
          // Erster Bericht → Fall anlegen und ALLE aktuellen Beiträge kopieren
          theCase = {
            id: store.nextId++, appointmentId: post.appointmentId, appointmentTitle: info.title,
            appointmentDate: info.date, areaName: info.areaName, createdAtUtc: now, lastReportAtUtc: now,
            posts: store.posts.filter(p => p.appointmentId === post.appointmentId).map(p => snapshot(store, p))
          };
          store.cases.push(theCase);
        }
        // Weitere Berichte: nichts erneut kopieren – nur den gemeldeten Beitrag nachtragen, falls er neuer ist
        let target = theCase.posts.find(cp => cp.originalPostId === post.id);
        if (!target) {
          target = snapshot(store, post);
          theCase.posts.push(target);
        }
        target.reports.push({
          id: store.nextId++, reporterId: userId, reporterUsername: username,
          reason: body.reason, comment: body.comment?.trim() || null, createdAtUtc: now
        });
        theCase.lastReportAtUtc = now;
        post.reportedBy.push(userId);
        saveStore(store);
        return ok({ caseId: theCase.id }, 201);
      })
    );
  }

  // ── Admin: Fälle ──────────────────────────────────────────────────────────
  if (path.startsWith('/api/moderation/')) {
    if (!auth.hasRole('admin')) return fail(req, 403, 'Nur für Admins');
    saveStore(store);

    if (path === '/api/moderation/cases' && req.method === 'GET') {
      return ok(store.cases.map(c => toSummary(c, store)));
    }
    if ((m = path.match(/^\/api\/moderation\/cases\/(\d+)$/))) {
      const c = store.cases.find(x => x.id === Number(m![1]));
      if (!c) return fail(req, 404, 'Fall nicht gefunden');
      if (req.method === 'GET') {
        return ok<ModerationCaseDetail>({ ...toSummary(c, store), posts: c.posts });
      }
      if (req.method === 'DELETE') {
        store.cases = store.cases.filter(x => x.id !== c.id);
        saveStore(store);
        return ok(null, 204);
      }
    }
  }

  return fail(req, 404, 'Mock: Endpoint unbekannt');
};

function fetchAppointmentInfo(
  req: HttpRequest<unknown>,
  next: Parameters<HttpInterceptorFn>[1],
  appointmentId: number
): Observable<{ title: string; date: string | null; areaName: string | null }> {
  const base = `${environment.apiUrl}/api`;
  const get = <T>(url: string) =>
    next(req.clone({ method: 'GET', url, body: null })).pipe(
      // nur die eigentliche Antwort, keine Fortschritts-Events
      filter((event): event is HttpResponse<unknown> => event instanceof HttpResponse),
      take(1),
      map(event => event.body as T)
    );

  return get<{ title: string; date: string; areaId: number }>(`${base}/appointments/${appointmentId}`).pipe(
    switchMap(a => get<{ name: string }>(`${base}/areas/${a.areaId}`).pipe(
      map(area => ({ title: a.title, date: a.date, areaName: area.name }))
    )),
    catchError(() => of({ title: `Termin #${appointmentId}`, date: null, areaName: null }))
  );
}
