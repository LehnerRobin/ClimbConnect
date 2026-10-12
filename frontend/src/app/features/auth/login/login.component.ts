import { Component } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../../services/auth.service';

/** Einfache Prüfung auf das Format name@domain.tld */
const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})
export class LoginComponent {

  errorMessage = '';
  /** Nur bei falschen Zugangsdaten: Link zur Registrierung in der Fehlermeldung anzeigen. */
  showRegisterHint = false;

  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  /** Prüft zuerst die Eingaben im Browser und meldet sich dann beim Backend an. */
  login(email: string, password: string): void {
    this.errorMessage = '';
    this.showRegisterHint = false;
    email = email.trim();

    // Diese Fehler erkennen wir selbst, dafür braucht es keinen Server
    if (!email && !password) {
      this.errorMessage = 'Bitte gib deine E-Mail-Adresse und dein Passwort ein.';
      return;
    }
    if (!email) {
      this.errorMessage = 'Bitte gib deine E-Mail-Adresse ein.';
      return;
    }
    if (!EMAIL_PATTERN.test(email)) {
      this.errorMessage = 'Das ist keine gültige E-Mail-Adresse (z. B. name@example.com).';
      return;
    }
    if (!password) {
      this.errorMessage = 'Bitte gib dein Passwort ein.';
      return;
    }

    this.authService.login(email, password).subscribe({
      next: () => this.router.navigate(['/home']),
      error: (error: any) => {
        if (error.status === 401) {
          // Absichtlich keine Unterscheidung „E-Mail unbekannt“ / „Passwort falsch“:
          // sonst könnte man ausprobieren, welche E-Mails registriert sind (User Enumeration)
          this.errorMessage = 'E-Mail oder Passwort ist falsch.';
          this.showRegisterHint = true;
        } else if (error.status === 0) {
          // Status 0 = keine Antwort vom Server (Server aus oder kein Internet)
          this.errorMessage = 'Der Server ist gerade nicht erreichbar. Bitte versuche es gleich noch einmal.';
        } else {
          this.errorMessage = 'Anmeldung fehlgeschlagen. Bitte versuche es noch einmal.';
        }
      }
    });
  }
}
