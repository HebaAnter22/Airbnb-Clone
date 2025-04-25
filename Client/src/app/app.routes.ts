import { Routes } from '@angular/router';
import { LoginComponent } from './components/auth/login/login.component';
import { RegisterComponent } from './components/auth/register/register.component';
import { DashboardComponent } from './components/protected/dashboard/dashboard.component';
import { HostComponent } from './components/protected/host/host.component';
import { authGuard } from './guards/auth.guard';
import { roleGuard } from './guards/role.guard';
import { NotFoundComponent } from './components/not-found/not-found.component';
import { ProfileComponent } from './components/profile/profile.component';
import { SearchBarComponent } from './components/home/search-bar/search-bar.component';
import { HeaderComponent } from './components/home/header/header.component';
import { AddPropertyComponent } from './components/host/add-property/add-property.component';
import { HostPropertiesComponent } from './components/host/host-proprties/host-properties.component';
import { EditPropertyComponent } from './components/host/edit-property/edit-property.component';
import { EditProfileComponent } from './components/edit-profile/edit-profile.component';
import { PropertyDetailsComponent } from './components/host/property-details/property-details.component';
import { PropertyGalleryComponent } from './components/property-gallary/property-gallery.component';
import { WishlistComponent } from './components/wishlist/wishlist.component';
import { BookingComponent } from './components/bookings/bookings.component';
import { VerificationComponent } from './components/verifications/verifications.component';
import { HostVerificationComponent } from './components/host-verification/host-verification.component';
import { AdminComponent } from './components/admin/admin.component';
import { HostDashboardComponent } from './components/host/host-dashboard/host-dashboard.component';
import { BookingDetailsComponent } from './components/host/booking-details/booking-details.component';
import { PropertyBookingDetailsComponent } from './components/host/property-booking-details/property-booking-details.component';
import { VerifinghostComponent } from './components/admin/verifinghost/verifinghost.component';
// import { PaymentComponent } from './components/payment/payment.component';
import { PropertyListingsComponent } from './components/home/property-listing/property-listing.component';
import { CheckoutComponent } from './components/checkout/checkout.component';
import { PaymentSuccessComponent } from './components/payment-success/payment-success.component';
// import { PaymentCancelComponent } from './components/payment-cancel/payment-cancel.component';
export const routes: Routes = [
  {path: 'home', component: PropertyListingsComponent},
  {path: '', redirectTo: 'home', pathMatch: 'full'},
  {path: 'host/add-property', component: AddPropertyComponent,
  canActivate: [authGuard], 
  data: { role: 'Host' },
  },
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },
  { 
    path: 'dashboard', 
    component: DashboardComponent,
    canActivate: [authGuard] 
  },
  { 
      path: 'host', 
      component: HostDashboardComponent,
      canActivate: [authGuard, roleGuard],
      data: { role: 'Host' }
    },

    { path: 'checkout/:id', component: CheckoutComponent },
    { path: 'payment-success', component: PaymentSuccessComponent },
    // { path: 'payment-cancel', component: PaymentCancelComponent }, // Optional
    
    // {
    //   path: 'host/properties',
    //   component: HostPropertiesComponent,
    //   canActivate: [authGuard, roleGuard],
    //   data: { role: 'Host' }
    // },
    {path: 'host/edit/:id',
      component: EditPropertyComponent,
      canActivate: [authGuard, roleGuard],
      data: { role: 'Host' }
    },
    {
      path: 'host/bookings/:id',
      component: PropertyBookingDetailsComponent,
      canActivate: [authGuard, roleGuard],
      data: { role: 'Host' }
    },
    {
      path: 'Booking/details/:id',
      component: PropertyBookingDetailsComponent,
      canActivate: [authGuard, roleGuard],
      data: { role: 'Host' }
    },
    {path: 'booking/:bookingId',
      component: BookingDetailsComponent,
      canActivate: [authGuard]
    },

    { path: 'property/:id', component: PropertyDetailsComponent },
    { path: 'profile/:id', component: ProfileComponent },
    {
      path: 'property/:id/gallery',
      component: PropertyGalleryComponent
    },
    {
      path: 'wishlist',
      component: WishlistComponent,
      canActivate: [authGuard] // If you have an auth guard
    },
    {path: 'bookings', component: BookingComponent, canActivate: [authGuard]},
    { path: 'verification', component: VerificationComponent, canActivate: [authGuard] },
   
    { path: 'editProfile/:id', component: EditProfileComponent, canActivate: [authGuard] },
{
  path: 'admin',
  component: AdminComponent,
  // canActivate: [ roleGuard],
},
{
  path: 'admin/verifinghost/:id',
  component: VerifinghostComponent,
  // canActivate: [authGuard, roleGuard],
  // data: { role: 'Admin' }
},
    { path: 'forbidden', component: NotFoundComponent },
    
  { path: '**',component:NotFoundComponent }
];