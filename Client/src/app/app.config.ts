import { ApplicationConfig } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { routes } from './app.routes';
import { authInterceptor } from './interceptors/auth.interceptor';
import {
  SocialAuthServiceConfig,
  GoogleLoginProvider,
  GoogleInitOptions,
} from '@abacritt/angularx-social-login';

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor])),
    {
      provide: 'SocialAuthServiceConfig',
      useValue: {
        autoLogin: false,
        providers: [
          {
            id: GoogleLoginProvider.PROVIDER_ID,
            provider: new GoogleLoginProvider(
              '1031756027306-qt40so8h1a59955ra6huff4f835hn315.apps.googleusercontent.com' // Replace with your actual client ID
            )
          }
        ]
      } as SocialAuthServiceConfig,
    }
  ]
};