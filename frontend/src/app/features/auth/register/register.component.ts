import { Component } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../../services/auth.service';

/** Einfache Prüfung auf das Format name@domain.tld */
const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

/** Fehlermeldungen pro Feld, sie werden direkt unter dem jeweiligen Eingabefeld angezeigt. */
interface FieldErrors {
  username?: string;
  email?: string;
  password?: string;
  confirmPassword?: string;
}

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './register.component.html',
  styleUrl: './register.component.css'
})
export class RegisterComponent {

  /** Allgemeine Meldung über dem Button (z. B. Server nicht erreichbar). */
  errorMessage = '';
  fieldErrors: FieldErrors = {};

  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  /** Prüft alle Felder, zeigt Fehler am jeweiligen Feld und registriert sonst den User. */
  register(username: string, email: string, password: string, confirmPassword: string): void {
    this.errorMessage = '';
    username = username.trim();
    email = email.trim();

    // Alle Felder auf einmal prüfen, damit man alle Fehler gleichzeitig sieht
    const errors: FieldErrors = {};

    if (username.length < 3) {
      errors.username = 'Der Benutzername muss mindestens 3 Zeichen haben.';
    } else if (username.length > 50) {
      errors.username = 'Der Benutzername darf höchstens 50 Zeichen haben.';
    }

    if (!email) {
      errors.email = 'Bitte gib deine E-Mail-Adresse ein.';
    } else if (!EMAIL_PATTERN.test(email)) {
      errors.email = 'Das ist keine gültige E-Mail-Adresse (z. B. name@example.com).';
    }

    if (password.length < 8) {
      errors.password = 'Das Passwort muss mindestens 8 Zeichen haben.';
    }
    if (password !== confirmPassword) {
      errors.confirmPassword = 'Die Passwörter stimmen nicht überein.';
    }

    this.fieldErrors = errors;
    if (Object.keys(errors).length > 0) return;

    this.authService.register({ username, email, password }).subscribe({
      next: () => this.router.navigate(['/home']),
      error: (error: any) => {
        const message: string | undefined = error.error?.error;
        const field: string | undefined = error.error?.field;

        // Das Backend sagt bei 409 mit "field", welches Feld schon vergeben ist
        if (error.status === 409 && field === 'email') {
          this.fieldErrors = { email: message ?? 'Diese E-Mail-Adresse ist bereits vergeben.' };
        } else if (error.status === 409 && field === 'username') {
          this.fieldErrors = { username: message ?? 'Dieser Benutzername ist bereits vergeben.' };
        } else if (error.status === 400 || error.status === 409) {
          this.errorMessage = message ?? 'Bitte überprüfe deine Eingaben.';
        } else if (error.status === 0) {
          this.errorMessage = 'Der Server ist gerade nicht erreichbar. Bitte versuche es gleich noch einmal.';
        } else {
          this.errorMessage = 'Registrierung fehlgeschlagen. Bitte versuche es noch einmal.';
        }
      }
    });
  }
}
