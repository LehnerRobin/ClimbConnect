import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { UserService, UserListItem } from '../../../services/user.service';

@Component({
  selector: 'app-climbers-page',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './climbers-page.html',
  styleUrl: './climbers-page.css',
})
export class ClimbersPage implements OnInit {

  allUsers: UserListItem[] = [];
  searchTerm = '';
  loading = true;
  errorMessage = '';

  constructor(private userService: UserService) {}

  ngOnInit(): void {
    this.userService.getUsers().subscribe({
      next: (users) => {
        this.allUsers = users;
        this.loading = false;
      },
      error: () => {
        this.errorMessage = 'Benutzerliste konnte nicht geladen werden.';
        this.loading = false;
      }
    });
  }

  get filteredUsers(): UserListItem[] {
    const term = this.searchTerm.trim().toLowerCase();
    if (!term) return this.allUsers;
    return this.allUsers.filter(u => u.username.toLowerCase().includes(term));
  }
}
