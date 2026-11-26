import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { PinnwandComponent } from './pinnwand.component';
import { PinnwandService } from '../../../services/pinnwand.service';
import { AuthService } from '../../../services/auth.service';
import { PinnwandPost } from '../../../models/pinnwand.model';

describe('PinnwandComponent – Löschen', () => {
  const own: PinnwandPost = {
    id: 1, appointmentId: 5, authorId: 7, authorUsername: 'ich', text: 'Hallo', createdAtUtc: '2030-08-01T10:00:00Z', reportedByMe: false
  };
  const foreign: PinnwandPost = { ...own, id: 2, authorId: 8, authorUsername: 'jemand' };

  function create(role: 'user' | 'admin'): PinnwandComponent {
    TestBed.configureTestingModule({
      imports: [PinnwandComponent],
      providers: [
        { provide: PinnwandService, useValue: { getPosts: () => of([]), deletePost: () => of(undefined) } },
        { provide: AuthService, useValue: { getUserId: () => 7, hasRole: (r: string) => r === role } }
      ]
    });
    return TestBed.createComponent(PinnwandComponent).componentInstance;
  }

  it('User dürfen nur eigene Beiträge löschen', () => {
    const pinnwand = create('user');
    expect(pinnwand.canDelete(own)).toBeTrue();
    expect(pinnwand.canDelete(foreign)).toBeFalse();
  });

  it('der Admin darf alle Beiträge löschen (#223)', () => {
    const pinnwand = create('admin');
    expect(pinnwand.canDelete(own)).toBeTrue();
    expect(pinnwand.canDelete(foreign)).toBeTrue();
  });

  it('fragt beim Admin mit dem Namen des Verfassers nach', () => {
    const pinnwand = create('admin');
    const confirm = spyOn(window, 'confirm').and.returnValue(false);
    pinnwand.deletePost(foreign);
    expect(confirm).toHaveBeenCalledWith('Beitrag von jemand als Admin löschen?');
  });
});
