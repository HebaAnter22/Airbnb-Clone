import { Routes } from '@angular/router';
import { LoginComponent } from './components/auth/login/login.component';
import { RegisterComponent } from './components/auth/register/register.component';
import { DashboardComponent } from './components/protected/dashboard/dashboard.component';
import { HostComponent } from './components/protected/host/host.component';
import { authGuard } from './guards/auth.guard';
import { roleGuard } from './guards/role.guard';
import { NotFoundComponent } from './components/not-found/not-found.component';
import { ForbiddenComponent } from './components/forbidden/forbidden.component';
import { ProfileComponent } from './components/profile/profile.component';
import { SearchBarComponent } from './components/home/search-bar/search-bar.component';
import { PropertyListingsComponent } from './components/home/property-listing/property-listing.component';
import { HeaderComponent } from './components/home/header/header.component';
import { AddPropertyComponent } from './components/host/add-property/add-property.component';
import { HostPropertiesComponent } from './components/host/host-proprties/host-properties.component';
import { EditPropertyComponent } from './components/host/edit-property/edit-property.component';
import { EditProfileComponent } from './components/edit-profile/edit-profile.component';
import { PropertyDetailsComponent } from './components/property-details/property-details.component';
import { PropertyGalleryComponent } from './components/property-gallary/property-gallery.component';
import { WishlistComponent } from './components/wishlist/wishlist.component';
import { HostVerificationComponent } from './components/host-verification/host-verification.component';



export const routes: Routes = [
  {path: 'home', component: PropertyListingsComponent},
  {path: '', redirectTo: 'home', pathMatch: 'full'},
  {path: 'become-a-host', component: AddPropertyComponent,
  // canActivate: [authGuard], 
  // data: { role: 'Host' }
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
      component: HostComponent,
      canActivate: [authGuard, roleGuard],
      data: { role: 'Host' }
    },
    {
      path: 'host/properties',
      component: HostPropertiesComponent,
      canActivate: [authGuard, roleGuard],
      data: { role: 'Host' }
    },
    {path: 'host/properties/edit/:id',
      component: EditPropertyComponent,
      canActivate: [authGuard, roleGuard],
      data: { role: 'Host' }
    },

    { path: 'property/:id', component: PropertyDetailsComponent },
    { path: 'profile/:id', component: ProfileComponent, canActivate: [authGuard] },
    {
      path: 'property/:id/gallery',
      component: PropertyGalleryComponent
    },
    {
      path: 'wishlist',
      component: WishlistComponent,
      canActivate: [authGuard] // If you have an auth guard
    },
    {
      path: 'verification', 
      component: HostVerificationComponent, // Assuming HostComponent handles the verification process
    },
    { path: 'editProfile/:id', component: EditProfileComponent, canActivate: [authGuard] },

    { path: 'forbidden', component: ForbiddenComponent },
    
  { path: '**',component:NotFoundComponent }
];