import { Routes } from '@angular/router';

import { HomePageComponent } from './features/home/home-page.component';
import { canActivateAuthRole } from './guards/auth-role.guard';

/**
 * Nur die Startseite wird sofort geladen. Alle anderen Seiten werden mit loadComponent
 * erst beim ersten Aufruf nachgeladen (Lazy Loading). Dadurch landen große Bibliotheken
 * wie Chart.js (Profil) und Leaflet (Karte) nicht mehr im Start-Bundle.
 */
export const routes: Routes = [
  { path: '', redirectTo: '/home', pathMatch: 'full' },

  { path: 'home', component: HomePageComponent },

  {
    path: 'areas',
    loadComponent: () => import('./features/areas/areas-page.component').then(m => m.AreasPageComponent)
  },

  {
    path: 'climbers',
    loadComponent: () => import('./features/climbers/climbers-page').then(m => m.ClimbersPage)
  },

  {
    path: 'areas/:id/appointments/new',
    loadComponent: () => import('./features/appointments/appointment-form.component').then(m => m.AppointmentFormComponent),
    canActivate: [canActivateAuthRole]
  },

  {
    path: 'areas/:id/appointments/:appointmentId/edit',
    loadComponent: () => import('./features/appointments/appointment-form.component').then(m => m.AppointmentFormComponent),
    canActivate: [canActivateAuthRole]
  },

  {
    path: 'areas/:id',
    loadComponent: () => import('./features/areas/area-detail.component').then(m => m.AreaDetailComponent)
  },

  {
    path: 'routes/:id',
    loadComponent: () => import('./features/routes/route-detail.component').then(m => m.RouteDetailComponent)
  },

  {
    path: 'progress/new',
    loadComponent: () => import('./features/progress/progress-form.component').then(m => m.ProgressFormComponent),
    canActivate: [canActivateAuthRole]
  },

  {
    path: 'progress/:id/edit',
    loadComponent: () => import('./features/progress/progress-form.component').then(m => m.ProgressFormComponent),
    canActivate: [canActivateAuthRole]
  },

  {
    path: 'users/:id',
    loadComponent: () => import('./features/users/public-profile.component').then(m => m.PublicProfileComponent)
  },

  {
    path: 'admin',
    loadComponent: () => import('./admin/admin.component').then(m => m.AdminComponent),
    canActivate: [canActivateAuthRole],
    data: { requiredRole: 'admin' }
  },

  {
    path: 'profile',
    loadComponent: () => import('./user-profile/user-profile.component').then(m => m.UserProfileComponent),
    canActivate: [canActivateAuthRole]
  },

  {
    path: 'forbidden',
    loadComponent: () => import('./forbidden/forbidden.component').then(m => m.ForbiddenComponent)
  },

  {
    path: 'login',
    loadComponent: () => import('./features/auth/login/login.component').then(m => m.LoginComponent)
  },

  {
    path: 'register',
    loadComponent: () => import('./features/auth/register/register.component').then(m => m.RegisterComponent)
  },

  { path: '**', redirectTo: '/home' }
];
