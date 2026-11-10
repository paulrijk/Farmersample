import { HttpClient, HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, firstValueFrom, of, throwError } from 'rxjs';

export interface CurrentUser { id: string; email: string; displayName: string; roles: string[]; mfaEnabled: boolean; mfaSatisfied: boolean; }
export type LoginStatus = 'ok' | 'mfa_required' | 'invalid' | 'locked_out';

@Injectable({ providedIn: 'root' })
export class Auth {
  private http = inject(HttpClient);
  readonly user = signal<CurrentUser | null>(null);

  async init() {
    await firstValueFrom(this.http.get('/api/v1/auth/antiforgery').pipe(catchError(() => of(null))));
    await this.refresh();
  }

  async refresh() {
    this.user.set(await firstValueFrom(this.http.get<CurrentUser>('/api/v1/auth/me').pipe(catchError(() => of(null)))));
  }

  async login(email: string, password: string): Promise<LoginStatus> {
    const r = await firstValueFrom(this.http.post<{ status: LoginStatus }>('/api/v1/auth/login', { email, password }));
    if (r.status === 'ok') await this.refresh();
    return r.status;
  }

  async loginMfa(code: string): Promise<LoginStatus> {
    const r = await firstValueFrom(this.http.post<{ status: LoginStatus }>('/api/v1/auth/login/mfa', { code }));
    if (r.status === 'ok') await this.refresh();
    return r.status;
  }

  async logout() {
    await firstValueFrom(this.http.post('/api/v1/auth/logout', {}).pipe(catchError(() => of(null))));
    this.user.set(null);
  }
}

export const authGuard: CanActivateFn = () => {
  const auth = inject(Auth);
  return auth.user() ? true : inject(Router).createUrlTree(['/login']);
};

/** 401 → login; 403 on staff APIs while the session has no second factor → two-factor set-up. */
export const authErrorInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);
  const auth = inject(Auth);
  return next(req).pipe(catchError((e: HttpErrorResponse) => {
    if (req.url.startsWith('/api/v1/staff') || req.url.startsWith('/api/v1/admin')) {
      if (e.status === 401) { auth.user.set(null); router.navigate(['/login']); }
      if (e.status === 403 && auth.user() && !auth.user()!.mfaSatisfied) router.navigate(['/mfa']);
    }
    return throwError(() => e);
  }));
};
