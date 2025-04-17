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
import { EditProfileComponent } from './components/edit-profile/edit-profile.component';
import { PropertyDetailsComponent } from './components/property-details/property-details.component';
import { PropertyGalleryComponent } from './components/property-gallary/property-gallery.component';
import { WishlistComponent } from './components/wishlist/wishlist.component';
import { HostVerificationComponent } from './components/host-verification/host-verification.component';

export const routes: Routes = [
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
    
  { path: '', redirectTo: '/login', pathMatch: 'full' },
  { path: '**',component:NotFoundComponent }
];