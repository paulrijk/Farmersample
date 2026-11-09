import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', loadComponent: () => import('./pages/home').then(m => m.HomePage), title: 'FarmerSample.com — Verified at origin. Traded worldwide.' },
  { path: 'farms/:code', loadComponent: () => import('./pages/farm').then(m => m.FarmPage) },
  { path: 'lots/:code', loadComponent: () => import('./pages/lot').then(m => m.LotPage) },
  // QR codes on sample pouches, lot labels and farm signs land here and are forwarded to the lot or farm page.
  { path: 'q/:code', loadComponent: () => import('./pages/qr').then(m => m.QrPage), title: 'FarmerSample.com' },
  { path: 'request-sample', loadComponent: () => import('./pages/request-sample').then(m => m.RequestSamplePage), title: 'Request a sample — FarmerSample.com' },
  { path: '**', loadComponent: () => import('./pages/not-found').then(m => m.NotFoundPage), title: 'Not found — FarmerSample.com' },
];
