import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../../../services/auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  templateUrl: './register.component.html',
  styleUrl: './register.component.css'
})
export class RegisterComponent {

  errorMessage = '';

constructor(
  private authService: AuthService,
  private router: Router
) {}

  register(
    username: string,
    email: string,
    password: string,
    confirmPassword: string
  ): void {
      const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

  if (!emailPattern.test(email)) {
    this.errorMessage = 'Bitte eine gültige E-Mail-Adresse eingeben';
    return;
  }
    if (password.length < 8) {
    this.errorMessage = 'Passwort muss mindestens 8 Zeichen haben';
    return;
  }

    if (password !== confirmPassword) {
      this.errorMessage = 'Passwörter stimmen nicht überein';
      return;
    }

    const user = {
      username,
      email,
      password
    };

   this.authService.register(user).subscribe({
  next: (response: any) => {

  localStorage.setItem('token', response.token);

  console.log('Registrierung erfolgreich', response);

  this.router.navigate(['/home']);

},
  error: (error: any) => {

  if (error.status === 409) {

    if (error.error?.message?.includes('email')) {
      this.errorMessage = 'E-Mail bereits vergeben';
    }
    else if (error.error?.message?.includes('username')) {
      this.errorMessage = 'Username bereits vergeben';
    }
    else {
      this.errorMessage = 'Benutzer existiert bereits';
    }

  } else {
    this.errorMessage = 'Registrierung fehlgeschlagen';
  }

  console.error(error);
}
});
  }
}