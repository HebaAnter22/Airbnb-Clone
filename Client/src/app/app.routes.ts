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
import { AddPropertyComponent } from './components/add-property/add-property.component';
import { HostPropertiesComponent } from './components/host-proprties/host-properties.component';
import { EditPropertyComponent } from './components/edit-property/edit-property.component';
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

    { path: 'profile', component: ProfileComponent, canActivate: [authGuard] },
    { path: 'forbidden', component: ForbiddenComponent },
    
  { path: '**',component:NotFoundComponent }
];