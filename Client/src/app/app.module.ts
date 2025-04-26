import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { FormsModule } from '@angular/forms'; // Import FormsModule
import { HttpClientModule } from '@angular/common/http';
import { AppComponent } from './app.component';
import { SocialLoginModule, SocialAuthServiceConfig, GoogleLoginProvider } from '@abacritt/angularx-social-login';
import { environment } from '../environments/environment';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { NgxStripeModule } from 'ngx-stripe';
import { RouterModule } from '@angular/router';
import { routes } from './app.routes';
import { AppRoutingModule } from './app-routing.module';

@NgModule({
  declarations: [
    AppComponent
  ],
  imports: [
    AppRoutingModule,
    BrowserModule,
    HttpClientModule,
    SocialLoginModule,
    FormsModule, // Add FormsModule here
    MatCardModule,
    MatButtonModule,
    MatProgressSpinnerModule
    // NgxStripeModule.forRoot('pk_test_51RG0HC05Xu3Oi1a7gr3XuJf2j0jrYVbLfKoCSSCLuyhGn8ESPKdOnfvtte1tlklH8Mb7EcDquwPoPDk47w2xe9bV00qW62yAKT') 
    ,RouterModule.forRoot(routes) // Add RouterModule with routes

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