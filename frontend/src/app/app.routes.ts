import { Routes } from '@angular/router';
import { HomeComponent } from './home/home.component';
import { AdminComponent } from './admin/admin.component';
import { ForbiddenComponent } from './forbidden/forbidden.component';
import { UserProfileComponent } from './user-profile/user-profile.component';
import { canActivateAuthRole } from './guards/auth-role.guard';
import { AreasPageComponent } from './features/areas/areas-page.component';

export const routes: Routes = [
  { path: '',        redirectTo: '/home', pathMatch: 'full' },
  { path: 'home',   component: HomeComponent },
  { path: 'areas',  component: AreasPageComponent },
  { path: 'admin',   component: AdminComponent,     canActivate: [canActivateAuthRole], data: { role: 'myAdminRole' } },
  { path: 'profile', component: UserProfileComponent, canActivate: [canActivateAuthRole], data: { role: 'view-profile' } },
  { path: 'forbidden', component: ForbiddenComponent },
  { path: '**', redirectTo: '/home' }
];
