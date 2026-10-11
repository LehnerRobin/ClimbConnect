import { Component, inject } from '@angular/core';
import { NotificationService } from '../../services/notification.service';

/** Zeigt die aktuelle Meldung aus dem NotificationService unten am Bildschirm an. */
@Component({
  selector: 'app-toast',
  standalone: true,
  template: `
    @if (notifications.message(); as text) {
      <div class="toast" role="alert">
        <span>{{ text }}</span>
        <button type="button" (click)="notifications.dismiss()" aria-label="Meldung schließen">×</button>
      </div>
    }
  `,
  styles: [`
    .toast {
      position: fixed;
      left: 50%;
      bottom: calc(1.25rem + env(safe-area-inset-bottom, 0px));
      transform: translateX(-50%);
      z-index: 1000;
      display: flex;
      align-items: center;
      gap: 0.75rem;
      max-width: min(32rem, calc(100vw - 2rem));
      padding: 0.75rem 1rem;
      border-radius: var(--radius-sm);
      background: var(--brick-100);
      color: var(--brick-700);
      border: 1px solid rgba(161, 64, 45, 0.3);
      box-shadow: var(--shadow-md);
      font-size: 14px;
      line-height: 1.4;
    }
    button {
      border: none;
      background: none;
      color: inherit;
      font-size: 20px;
      line-height: 1;
      cursor: pointer;
      padding: 0 0.25rem;
    }
  `]
})
export class ToastComponent {
  protected readonly notifications = inject(NotificationService);
}
