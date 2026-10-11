import { Injectable, signal } from '@angular/core';

/**
 * Zeigt kurze Meldungen app-weit an (Toast unten am Bildschirm, siehe ToastComponent).
 * Wird vom errorInterceptor genutzt, wenn der Server nicht erreichbar ist oder einen Fehler meldet.
 */
@Injectable({ providedIn: 'root' })
export class NotificationService {

  /** Aktuell angezeigte Meldung oder null. */
  readonly message = signal<string | null>(null);

  private hideTimer: ReturnType<typeof setTimeout> | null = null;

  /** Zeigt eine Fehlermeldung für einige Sekunden an. */
  showError(text: string): void {
    this.message.set(text);
    if (this.hideTimer) clearTimeout(this.hideTimer);
    this.hideTimer = setTimeout(() => this.message.set(null), 6000);
  }

  /** Blendet die Meldung sofort aus (Schließen-Knopf). */
  dismiss(): void {
    if (this.hideTimer) clearTimeout(this.hideTimer);
    this.message.set(null);
  }
}
