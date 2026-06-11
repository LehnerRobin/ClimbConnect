import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../../../services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})
export class LoginComponent {

  errorMessage = '';

  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  login(email: string, password: string): void {

    if (!email || !password) {
      this.errorMessage = 'Bitte E-Mail und Passwort eingeben';
      return;
    }

    this.authService.login(email, password).subscribe({
      next: () => {
        this.router.navigate(['/home']);
      },

      error: (error: any) => {

        if (error.status === 401) {
          this.errorMessage = 'Falsche E-Mail oder Passwort';
        } else {
          this.errorMessage = 'Login fehlgeschlagen';
        }

        console.error(error);
      }
    });
  }
}