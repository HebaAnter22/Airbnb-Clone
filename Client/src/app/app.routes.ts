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

    { path: 'profile', component: ProfileComponent, canActivate: [authGuard] },
    { path: 'forbidden', component: ForbiddenComponent },
    
  { path: '', redirectTo: '/login', pathMatch: 'full' },
  { path: '**',component:NotFoundComponent }
];