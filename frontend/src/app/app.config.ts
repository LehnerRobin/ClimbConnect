import { provideRouter, withComponentInputBinding } from '@angular/router';
import { ApplicationConfig, provideBrowserGlobalErrorListeners, provideZoneChangeDetection } from '@angular/core';
import { provideHttpClient, withInterceptors, withFetch } from '@angular/common/http';
import { includeBearerTokenInterceptor } from 'keycloak-angular';

import { provideKeycloakAngular } from './keycloak.config';
import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideKeycloakAngular(),
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes,withComponentInputBinding()),
    provideHttpClient(withFetch(), withInterceptors([includeBearerTokenInterceptor]))
  ]
};
