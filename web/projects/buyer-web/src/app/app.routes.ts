import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', loadComponent: () => import('./pages/home').then(m => m.HomePage), title: 'FarmerSample.com — Verified at origin. Traded worldwide.' },
  { path: 'farms/:code', loadComponent: () => import('./pages/farm').then(m => m.FarmPage) },
  // QR codes on pouches and farm signs land here.
  { path: 'q/:code', loadComponent: () => import('./pages/farm').then(m => m.FarmPage) },
  { path: 'request-sample', loadComponent: () => import('./pages/request-sample').then(m => m.RequestSamplePage), title: 'Request a sample — FarmerSample.com' },
  { path: '**', loadComponent: () => import('./pages/not-found').then(m => m.NotFoundPage), title: 'Not found — FarmerSample.com' },
];
