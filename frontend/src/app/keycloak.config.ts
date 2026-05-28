import {
  provideKeycloak,
  createInterceptorCondition,
  IncludeBearerTokenCondition,
  INCLUDE_BEARER_TOKEN_INTERCEPTOR_CONFIG,
  withAutoRefreshToken,
  AutoRefreshTokenService,
  UserActivityService
} from 'keycloak-angular';

const localhostCondition = createInterceptorCondition<IncludeBearerTokenCondition>({
   urlPattern: /^(.*)?$/i,
});

export const provideKeycloakAngular = () =>
  provideKeycloak({
    config: {
      realm: 'myTest',
      url: 'https://auth.aitenbichler.cc',
      clientId: 'myTestUI'
    },
    initOptions: {
      onLoad: 'check-sso',
      silentCheckSsoRedirectUri: document.baseURI.slice(0, -1) + '/silent-check-sso.html',
      redirectUri: document.baseURI.slice(0, -1) + '/'
    },
    features: [
      withAutoRefreshToken({
        onInactivityTimeout: 'logout',
        sessionTimeout: 100
      })
    ],
    providers: [
      AutoRefreshTokenService,
      UserActivityService,
      {
        provide: INCLUDE_BEARER_TOKEN_INTERCEPTOR_CONFIG,
        useValue: [localhostCondition]
      }
    ]
  });
