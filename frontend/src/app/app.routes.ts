import { Routes } from '@angular/router';

import { HomePageComponent } from './features/home/home-page.component';
import { AdminComponent } from './admin/admin.component';
import { ForbiddenComponent } from './forbidden/forbidden.component';
import { UserProfileComponent } from './user-profile/user-profile.component';
import { AreasPageComponent } from './features/areas/areas-page.component';
import { LoginComponent } from './features/auth/login/login.component';
import { RegisterComponent } from './features/auth/register/register.component';
import { canActivateAuthRole } from './guards/auth-role.guard';
import { AreaDetailComponent } from './features/areas/area-detail.component';
import { AppointmentFormComponent } from './features/appointments/appointment-form.component';
import { RouteDetailComponent } from './features/routes/route-detail.component';
import { PublicProfileComponent } from './features/users/public-profile.component';
import { ProgressFormComponent } from './features/progress/progress-form.component';
import { ClimbersPage } from './features/climbers/climbers-page';

export const routes: Routes = [
  { path: '', redirectTo: '/home', pathMatch: 'full' },

  { path: 'home', component: HomePageComponent },

  { path: 'areas', component: AreasPageComponent },

  { path: 'climbers', component: ClimbersPage },

  {
    path: 'areas/:id/appointments/new',
    component: AppointmentFormComponent,
    canActivate: [canActivateAuthRole]
  },

  {
    path: 'areas/:id/appointments/:appointmentId/edit',
    component: AppointmentFormComponent,
    canActivate: [canActivateAuthRole]
  },

  { path: 'areas/:id', component: AreaDetailComponent },

  { path: 'routes/:id', component: RouteDetailComponent },

  {
    path: 'progress/new',
    component: ProgressFormComponent,
    canActivate: [canActivateAuthRole]
  },

  {
    path: 'progress/:id/edit',
    component: ProgressFormComponent,
    canActivate: [canActivateAuthRole]
  },

  { path: 'users/:id', component: PublicProfileComponent },

  {
    path: 'admin',
    component: AdminComponent,
    canActivate: [canActivateAuthRole],
    data: { requiredRole: 'admin' }
  },

  {
    path: 'profile',
    component: UserProfileComponent,
    canActivate: [canActivateAuthRole]
  },

  { path: 'forbidden', component: ForbiddenComponent },
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },

  { path: '**', redirectTo: '/home' }
];
