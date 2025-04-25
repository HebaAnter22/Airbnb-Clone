import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { FormsModule } from '@angular/forms';
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
import { AppComponent } from './app.component';
import { SocialLoginModule, SocialAuthServiceConfig, GoogleLoginProvider } from '@abacritt/angularx-social-login';
import { environment } from '../environments/environment';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBarModule } from '@angular/material/snack-bar'; // Add for MatSnackBar
import { RouterModule } from '@angular/router';
import { routes } from './app.routes';
import { AuthInterceptor } from './interceptors/auth.interceptor';
import { CheckoutComponent } from './components/checkout/checkout.component';
// import { PaymentSuccessComponent } from './components/payment-success/payment-success.component';
// import { PaymentCancelComponent } from './components/payment-cancel/payment-cancel.component';

@NgModule({
    declarations: [
        AppComponent,
        CheckoutComponent,
        // PaymentSuccessComponent,
        // PaymentCancelComponent,
    ],
    imports: [
        BrowserModule,
        HttpClientModule,
        SocialLoginModule,
        FormsModule,
        MatCardModule,
        MatButtonModule,
        MatProgressSpinnerModule,
        MatSnackBarModule, // Add for MatSnackBar
        RouterModule.forRoot(routes),
    ],
    providers: [
        {
            provide: 'SocialAuthServiceConfig',
            useValue: {
                autoLogin: false,
                providers: [
                    {
                        id: GoogleLoginProvider.PROVIDER_ID,
                        provider: new GoogleLoginProvider(environment.googleClientId),
                    },
                ],
            } as SocialAuthServiceConfig,
        },
        {
            provide: HTTP_INTERCEPTORS,
            useClass: AuthInterceptor,
            multi: true,
        },
    ],
    bootstrap: [AppComponent],
})
export class AppModule {}