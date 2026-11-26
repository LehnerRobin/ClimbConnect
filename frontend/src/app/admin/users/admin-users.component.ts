import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { Router, RouterLink } from '@angular/router';
import { AdminService, AdminUser, UserRole } from '../../../services/admin.service';
import { AuthService } from '../../../services/auth.service';

/**
 * Admin: Benutzer und Rollen (#237).
 * Jedes Teammitglied bekommt ein eigenes Admin-Konto, statt sich eines zu teilen.
 * Admin-Rechte vergeben und entziehen wirkt im Backend sofort; der letzte Admin bleibt Admin.
 */
@Component({
  selector: 'app-admin-users',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './admin-users.component.html',
  styleUrl: './admin-users.component.css'
})
export class AdminUsersComponent implements OnInit {

  private adminService = inject(AdminService);
  private authService = inject(AuthService);
  private router = inject(Router);

  users: AdminUser[] = [];
  loading = true;
  error = '';
  info = '';
  /** Id des Users, dessen Rolle gerade gespeichert wird (Knopf sperren). */
  busyId: number | null = null;

  /** Suche nach Benutzername. */
  search = '';
  /** null = alle Rollen. */
  roleFilter: UserRole | null = null;

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading = true;
    this.error = '';
    this.adminService.getUsersWithRoles().subscribe({
      next: users => { this.users = users; this.loading = false; },
      error: () => { this.error = 'Benutzer konnten nicht geladen werden.'; this.loading = false; }
    });
  }

  // ── Anzeige ───────────────────────────────────────────────────────────────

  get filtered(): AdminUser[] {
    const term = this.search.trim().toLowerCase();
    return this.users.filter(u =>
      (this.roleFilter === null || u.role === this.roleFilter) &&
      (!term || u.username.toLowerCase().includes(term)));
  }

  get adminCount(): number {
    return this.users.filter(u => u.role === 'admin').length;
  }

  isSelf(user: AdminUser): boolean {
    return user.id === this.authService.getUserId();
  }

  /** Der letzte Admin kann nicht zurückgestuft werden – Knopf gleich sperren. */
  isLastAdmin(user: AdminUser): boolean {
    return user.role === 'admin' && this.adminCount <= 1;
  }

  /** "26.11.2026" aus "2026-11-26". */
  formatDate(value: string | undefined): string {
    if (!value) return '–';
    const [y, m, d] = value.split('-');
    return `${d}.${m}.${y}`;
  }

  // ── Aktionen ──────────────────────────────────────────────────────────────

  makeAdmin(user: AdminUser): void {
    if (!window.confirm(`${user.username} zum Admin machen? Diese Person kann dann Gebiete, Routen, Termine und Benutzer verwalten.`)) return;
    this.changeRole(user, 'admin');
  }

  removeAdmin(user: AdminUser): void {
    const question = this.isSelf(user)
      ? 'Dir selbst die Admin-Rechte entziehen? Du verlierst sofort den Zugriff auf den Admin-Bereich und wirst abgemeldet.'
      : `${user.username} die Admin-Rechte entziehen? Das wirkt sofort.`;
    if (!window.confirm(question)) return;
    this.changeRole(user, 'user');
  }

  private changeRole(user: AdminUser, role: UserRole): void {
    this.busyId = user.id;
    this.error = '';
    this.info = '';
    this.adminService.setUserRole(user.id, role).subscribe({
      next: updated => {
        user.role = updated.role;
        this.busyId = null;

        // Eigene Rechte abgegeben: Das Menü beruht noch auf dem alten Token → neu anmelden
        if (this.isSelf(user) && role === 'user') {
          this.authService.logout();
          this.router.navigate(['/login']);
          return;
        }
        this.info = role === 'admin'
          ? `${user.username} ist jetzt Admin. Das Admin-Menü erscheint bei der Person nach dem nächsten Anmelden – die Rechte gelten aber sofort.`
          : `${user.username} ist kein Admin mehr.`;
      },
      error: (err: HttpErrorResponse) => {
        this.busyId = null;
        this.error = err.error?.error ?? 'Rolle konnte nicht geändert werden.';
      }
    });
  }
}
