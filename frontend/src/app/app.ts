import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { MenuComponent } from './menu/menu.component';
import { ToastComponent } from './toast/toast.component';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, MenuComponent, ToastComponent],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  protected readonly title = signal('demosypui');
  protected readonly currentYear = new Date().getFullYear();
}
