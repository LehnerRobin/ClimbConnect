import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-menu',
  standalone: true,
  imports: [RouterModule],
  templateUrl: './menu.component.html',
  styleUrls: ['./menu.component.css']
})
export class MenuComponent {

  authenticated = false;

  constructor() {
    this.authenticated = !!localStorage.getItem('token');
  }

  login(): void {
    console.log('Navigate to login page');
  }

  logout(): void {
    localStorage.removeItem('token');
    this.authenticated = false;
  }
}