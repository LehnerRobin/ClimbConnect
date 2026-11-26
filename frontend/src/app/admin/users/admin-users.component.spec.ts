import { TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { Router, provideRouter } from '@angular/router';
import { Observable, of, throwError } from 'rxjs';
import { AdminUsersComponent } from './admin-users.component';
import { AdminService, AdminUser, UserRole } from '../../../services/admin.service';
import { AuthService } from '../../../services/auth.service';

describe('AdminUsersComponent (#237)', () => {
  let users: AdminUser[];
  let roleCalls: [number, UserRole][];
  let roleResult: (id: number, role: UserRole) => Observable<AdminUser>;
  let logout: jasmine.Spy;

  /** Eingeloggt als User mit Id 1 (admin). */
  function create(): AdminUsersComponent {
    users = [
      { id: 1, username: 'admin', role: 'admin', memberSince: '2026-09-01' },
      { id: 2, username: 'anna',  role: 'user',  memberSince: '2026-10-15' },
      { id: 3, username: 'ben',   role: 'user',  memberSince: '2026-11-02' }
    ];
    roleCalls = [];
    roleResult = (id, role) => of({ ...users.find(u => u.id === id)!, role });
    logout = jasmine.createSpy('logout');

    TestBed.configureTestingModule({
      imports: [AdminUsersComponent],
      providers: [
        provideRouter([]),
        { provide: AdminService, useValue: {
          getUsersWithRoles: () => of(users.map(u => ({ ...u }))),
          setUserRole: (id: number, role: UserRole) => { roleCalls.push([id, role]); return roleResult(id, role); }
        } },
        { provide: AuthService, useValue: { getUserId: () => 1, logout } }
      ]
    });
    const fixture = TestBed.createComponent(AdminUsersComponent);
    fixture.detectChanges();
    return fixture.componentInstance;
  }

  it('lädt Benutzer und zählt Admins', () => {
    const page = create();
    expect(page.users.length).toBe(3);
    expect(page.adminCount).toBe(1);
  });

  it('filtert nach Name und Rolle', () => {
    const page = create();
    page.search = 'AN';
    expect(page.filtered.map(u => u.username)).toEqual(['anna']);

    page.search = '';
    page.roleFilter = 'admin';
    expect(page.filtered.map(u => u.username)).toEqual(['admin']);
  });

  it('macht nach Bestätigung zum Admin', () => {
    const page = create();
    spyOn(window, 'confirm').and.returnValue(true);

    page.makeAdmin(page.users[1]);

    expect(roleCalls).toEqual([[2, 'admin']]);
    expect(page.users[1].role).toBe('admin');
    expect(page.adminCount).toBe(2);
    expect(page.info).toContain('anna ist jetzt Admin');
  });

  it('ändert nichts, wenn abgebrochen wird', () => {
    const page = create();
    spyOn(window, 'confirm').and.returnValue(false);
    page.makeAdmin(page.users[1]);
    expect(roleCalls).toEqual([]);
  });

  it('sperrt den Knopf beim letzten Admin', () => {
    const page = create();
    expect(page.isLastAdmin(page.users[0])).toBeTrue();

    page.users[1].role = 'admin';
    expect(page.isLastAdmin(page.users[0])).toBeFalse();
  });

  it('meldet ab, wenn man sich selbst die Rechte entzieht', () => {
    const page = create();
    const navigate = spyOn(TestBed.inject(Router), 'navigate').and.resolveTo(true);
    spyOn(window, 'confirm').and.returnValue(true);
    page.users[1].role = 'admin'; // zweiter Admin, damit es erlaubt ist

    page.removeAdmin(page.users[0]);

    expect(roleCalls).toEqual([[1, 'user']]);
    expect(logout).toHaveBeenCalled();
    expect(navigate).toHaveBeenCalledWith(['/login']);
  });

  it('zeigt die Fehlermeldung des Backends', () => {
    const page = create();
    spyOn(window, 'confirm').and.returnValue(true);
    roleResult = () => throwError(() => new HttpErrorResponse({
      status: 409, error: { error: 'Das ist der letzte Admin – die Rechte können nicht entzogen werden.' }
    }));

    page.removeAdmin(page.users[0]);

    expect(page.error).toContain('letzte Admin');
    expect(page.users[0].role).toBe('admin');
    expect(logout).not.toHaveBeenCalled();
  });
});
