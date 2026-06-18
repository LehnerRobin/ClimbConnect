import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { AuthService } from '../../services/auth.service';

export const canActivateAuthRole: CanActivateFn = (route) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (!authService.isAuthenticated()) {
    router.navigate(['/login']);
    return false;
  }

  const requiredRole = route.data['requiredRole'] as string | undefined;
  if (requiredRole && !authService.hasRole(requiredRole)) {
    router.navigate(['/forbidden']);
    return false;
  }

  return true;
};
