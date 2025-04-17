import { ApplicationConfig } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { routes } from './app.routes';
import { HTTP_INTERCEPTORS } from '@angular/common/http';
import { AuthInterceptor } from './interceptors/auth.interceptor';
import { ReactiveFormsModule } from '@angular/forms';
import {
  SocialAuthServiceConfig,
  GoogleLoginProvider,
  GoogleInitOptions,
} from '@abacritt/angularx-social-login';
import { AuthService } from './components/auth/auth.service';

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    provideHttpClient(withInterceptorsFromDi()),
    { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true },
    AuthService,
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