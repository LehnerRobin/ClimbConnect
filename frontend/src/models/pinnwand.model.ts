/**
 * Datentypen für die Pinnwand eines Termins und für das Melden von Beiträgen.
 * Der genaue API-Vertrag steht in PINNWAND-MELDEN-API.md im Projektordner.
 */

/** Ein Beitrag auf der Pinnwand eines Termins. */
export interface PinnwandPost {
  id: number;
  appointmentId: number;
  authorId: number;
  authorUsername: string;
  text: string;
  createdAtUtc: string;
  /** true, wenn der eingeloggte User diesen Beitrag schon gemeldet hat. */
  reportedByMe: boolean;
}

/** Erlaubte Meldegründe. "Sonstiges" verlangt einen Kommentar. */
export type ReportReason = 'Beleidigung' | 'Belaestigung' | 'UnangemessenerInhalt' | 'Sonstiges';

export const REPORT_REASONS: { value: ReportReason; label: string; hint: string }[] = [
  { value: 'Beleidigung',          label: 'Beleidigung',             hint: 'Beschimpfungen, abwertende Aussagen' },
  { value: 'Belaestigung',         label: 'Belästigung',             hint: 'Bedrängen, Drohen, unerwünschte Annäherung' },
  { value: 'UnangemessenerInhalt', label: 'Unangemessener Inhalt',   hint: 'Anstößige Inhalte, Spam, Werbung' },
  { value: 'Sonstiges',            label: 'Sonstiges',               hint: 'Bitte kurz beschreiben' }
];

export function reportReasonLabel(reason: string): string {
  return REPORT_REASONS.find(r => r.value === reason)?.label ?? reason;
}

export interface ReportCreateRequest {
  reason: ReportReason;
  comment: string | null;
}

// ── Admin: Fälle ─────────────────────────────────────────────────────────────

/** Ein Fall fasst alle Meldungen zu einem Termin zusammen (Listenansicht). */
export interface ModerationCaseSummary {
  id: number;
  appointmentId: number;
  appointmentTitle: string;
  appointmentDate: string | null;
  areaName: string | null;
  /** true, wenn der Termin inzwischen gelöscht wurde (die Kopie bleibt erhalten). */
  appointmentDeleted: boolean;
  createdAtUtc: string;
  lastReportAtUtc: string;
  /** Automatische Löschung: 30 Tage nach der letzten Meldung. */
  deleteAfterUtc: string;
  reportCount: number;
  reportedPostCount: number;
  postCount: number;
}

/** Eine Meldung zu einem Beitrag – wer, warum, wann. */
export interface CaseReport {
  id: number;
  reporterId: number;
  reporterUsername: string;
  reason: ReportReason;
  comment: string | null;
  createdAtUtc: string;
}

/** Kopie eines Pinnwand-Beitrags im Fall. Bleibt erhalten, auch wenn das Original gelöscht wird. */
export interface CasePost {
  id: number;
  originalPostId: number;
  authorId: number;
  authorUsername: string;
  text: string;
  createdAtUtc: string;
  /** true, wenn der Verfasser das Original auf der Pinnwand gelöscht hat. */
  deletedByAuthor: boolean;
  /** Leer = Beitrag dient nur als Kontext. */
  reports: CaseReport[];
}

export interface ModerationCaseDetail extends ModerationCaseSummary {
  /** Alle kopierten Beiträge, chronologisch (älteste zuerst). */
  posts: CasePost[];
}
