import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { HttpClientModule } from '@angular/common/http';
import { AppComponent } from './app.component';
import { SocialLoginModule, SocialAuthServiceConfig, GoogleLoginProvider } from '@abacritt/angularx-social-login';
import { environment } from '../environments/environment';

@NgModule({
  declarations: [
    AppComponent
  ],
  imports: [
    BrowserModule,
    HttpClientModule,
    SocialLoginModule
  ],
  providers: [
    {
      provide: 'SocialAuthServiceConfig',
      useValue: {
        autoLogin: false,
        providers: [
          {
            id: GoogleLoginProvider.PROVIDER_ID,
            provider: new GoogleLoginProvider(environment.googleClientId)
          }
        ]
      } as SocialAuthServiceConfig
    }
  ],
  bootstrap: [AppComponent]
})
export class AppModule { } 