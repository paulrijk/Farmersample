import { Routes } from '@angular/router';
import { authGuard } from './auth';

export const routes: Routes = [
  { path: 'login', loadComponent: () => import('./pages/login').then(m => m.LoginPage), title: 'Sign in — FarmerSample back office' },
  {
    path: '', canActivate: [authGuard], loadComponent: () => import('./shell').then(m => m.Shell),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'farms' },
      { path: 'farms', loadComponent: () => import('./pages/farms').then(m => m.FarmsPage), title: 'Farms — back office' },
      { path: 'farms/new', loadComponent: () => import('./pages/new-farm').then(m => m.NewFarmPage), title: 'New farm — back office' },
      { path: 'sample-requests', loadComponent: () => import('./pages/sample-requests').then(m => m.SampleRequestsPage), title: 'Sample requests — back office' },
      { path: 'mfa', loadComponent: () => import('./pages/mfa').then(m => m.MfaPage), title: 'Two-factor sign-in — back office' },
    ],
  },
  { path: '**', redirectTo: '' },
];
