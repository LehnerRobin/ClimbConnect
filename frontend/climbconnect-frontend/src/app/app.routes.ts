import { Routes } from '@angular/router';

import { AreasPage } from './features/areas/areas-page/areas-page';
import { ProfilePage } from './features/profile/profile-page/profile-page';
import { AppointmentsPage } from './features/appointments/appointments-page/appointments-page';

export const routes: Routes = [
  { path: '', redirectTo: 'areas', pathMatch: 'full' },
  { path: 'areas', component: AreasPage },
  { path: 'profile', component: ProfilePage },
  { path: 'appointments', component: AppointmentsPage },
  { path: '**', redirectTo: 'areas' },
];
