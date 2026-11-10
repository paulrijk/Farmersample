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
      { path: 'farms/:id/photos', loadComponent: () => import('./pages/farm-photos').then(m => m.FarmPhotosPage), title: 'Farm photos — back office' },
      { path: 'sample-batches', loadComponent: () => import('./pages/sample-batches').then(m => m.SampleBatchesPage), title: 'Sample batches — back office' },
      { path: 'lots', loadComponent: () => import('./pages/lots').then(m => m.LotsPage), title: 'Lots — back office' },
      { path: 'lots/new', loadComponent: () => import('./pages/new-lot').then(m => m.NewLotPage), title: 'New lot — back office' },
      { path: 'lots/:id', loadComponent: () => import('./pages/lot-detail').then(m => m.LotDetailPage), title: 'Lot — back office' },
      { path: 'labels', loadComponent: () => import('./pages/labels').then(m => m.LabelsPage), title: 'QR labels — back office' },
      { path: 'sample-requests', loadComponent: () => import('./pages/sample-requests').then(m => m.SampleRequestsPage), title: 'Sample requests — back office' },
      { path: 'users', loadComponent: () => import('./pages/users').then(m => m.UsersPage), title: 'Users — back office' },
      { path: 'password', loadComponent: () => import('./pages/password').then(m => m.PasswordPage), title: 'Change password — back office' },
      { path: 'mfa', loadComponent: () => import('./pages/mfa').then(m => m.MfaPage), title: 'Two-factor sign-in — back office' },
    ],
  },
  { path: '**', redirectTo: '' },
];
