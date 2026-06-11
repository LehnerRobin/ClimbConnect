import { Component } from '@angular/core';
import { AuthService } from '../../../../services/auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  templateUrl: './register.component.html',
  styleUrl: './register.component.css'
})
export class RegisterComponent {

  constructor(private authService: AuthService) {}

  register(
    username: string,
    email: string,
    password: string
  ): void {

    const user = {
      username,
      email,
      password
    };

    this.authService.register(user).subscribe({
      next: (response: any) => {
        console.log('Registrierung erfolgreich', response);
      },
      error: (error: any) => {
        console.error('Registrierung fehlgeschlagen', error);
      }
    });
  }
}