import { Component } from '@angular/core';
import { AuthService } from '../../../../services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})
export class LoginComponent {

  constructor(private authService: AuthService) {}

login(email: string, password: string): void {
  this.authService.login(email, password).subscribe({
    next: (response: any) => {
      console.log('Login erfolgreich', response);
    },
    error: (error: any) => {
      console.error('Login fehlgeschlagen', error);
    }
  });
}
}
