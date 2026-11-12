import { Component } from '@angular/core';
import { NavigationEnd, NavigationStart, Router, RouterModule } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-menu',
  standalone: true,
  imports: [RouterModule],
  templateUrl: './menu.component.html',
  styleUrls: ['./menu.component.css']
})
export class MenuComponent {

  /** Steuert das aufklappbare Mobile-Menü (Hamburger). */
  mobileOpen = false;

  /** True auf der Startseite: Dort liegt das Menü als Glas-Leiste über dem großen Foto. */
  isHome = false;

  constructor(
    private authService: AuthService,
    private router: Router
  ) {
    // Menü beim Navigieren automatisch schließen (z. B. nach Tap auf einen Link).
    this.router.events.subscribe(event => {
      if (event instanceof NavigationStart) {
        this.mobileOpen = false;
      }
      // Nach jeder Navigation prüfen, ob wir auf der Startseite sind.
      if (event instanceof NavigationEnd) {
        this.isHome = this.isHomeUrl(event.urlAfterRedirects);
      }
    });
  }

  get authenticated(): boolean {
    return this.authService.isAuthenticated();
  }

  get isAdmin(): boolean {
    return this.authService.hasRole('admin');
  }

  get username(): string | null {
    return this.authService.getUsername();
  }

  /** Erstes Zeichen des Usernamens für den Avatar-Chip. */
  get userInitial(): string {
    return (this.username || '?').charAt(0).toUpperCase();
  }

  /** Prüft, ob eine URL die Startseite ist (Query-Parameter und Anker werden ignoriert). */
  private isHomeUrl(url: string): boolean {
    const path = url.split(/[?#]/)[0];
    return path === '/home' || path === '/';
  }

  toggleMobileMenu(): void {
    this.mobileOpen = !this.mobileOpen;
  }

  closeMobileMenu(): void {
    this.mobileOpen = false;
  }

  logout(): void {
    this.authService.logout();
    this.closeMobileMenu();
    this.router.navigate(['/login']);
  }
}
