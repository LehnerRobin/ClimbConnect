import { ApplicationConfig, LOCALE_ID } from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';

import {
  provideHttpClient,
  withFetch,
  withInterceptors
} from '@angular/common/http';

import { routes } from './app.routes';
import { jwtInterceptor } from './interceptors/jwt.interceptor';
import { errorInterceptor } from './interceptors/error.interceptor';
import { pinnwandMockInterceptor } from './interceptors/pinnwand-mock.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    { provide: LOCALE_ID, useValue: 'de' },

    provideRouter(
      routes,
      withComponentInputBinding()
    ),

    provideHttpClient(
      withFetch(),
      // pinnwandMockInterceptor ist nur aktiv, wenn environment.mockPinnwand = true
      withInterceptors([jwtInterceptor, errorInterceptor, pinnwandMockInterceptor])
    )
  ]
};