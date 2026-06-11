import { Routes } from '@angular/router';

import { HomeComponent } from './home/home.component';
import { AdminComponent } from './admin/admin.component';
import { ForbiddenComponent } from './forbidden/forbidden.component';
import { UserProfileComponent } from './user-profile/user-profile.component';
import { AreasPageComponent } from './features/areas/areas-page.component';
import { LoginComponent } from './features/auth/login/login.component';
import { canActivateAuthRole } from './guards/auth-role.guard';
import { RegisterComponent } from './features/auth/register/register.component';
export const routes: Routes = [
  { path: '', redirectTo: '/home', pathMatch: 'full' },

  { path: 'home', component: HomeComponent },

  { path: 'areas', component: AreasPageComponent },

  { path: 'admin', component: AdminComponent },

  { path: 'profile', component: UserProfileComponent },

  { path: 'forbidden', component: ForbiddenComponent },
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },

  { path: '**', redirectTo: '/home' }
];