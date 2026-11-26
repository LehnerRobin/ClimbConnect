import { Component, signal } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { MenuComponent } from './menu/menu.component';
import { ToastComponent } from './toast/toast.component';
import { FeedbackDialogComponent } from './shared/feedback-dialog.component';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, MenuComponent, ToastComponent, FeedbackDialogComponent],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  protected readonly title = signal('demosypui');
  protected readonly currentYear = new Date().getFullYear();
}
