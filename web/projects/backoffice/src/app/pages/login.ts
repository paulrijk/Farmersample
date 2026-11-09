import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Auth } from '../auth';

@Component({
  selector: 'bo-login',
  imports: [FormsModule],
  template: `
    <div class="wrap">
      <div class="card box">
        <img class="logo" src="brand/wordmark.svg" alt="FarmerSample.com" width="336" height="34">
        <h1>Back office</h1>
        @if (step() === 'password') {
          <form (ngSubmit)="submitPassword()">
            <div class="field"><label for="email">E-mail</label><input id="email" name="email" type="email" [(ngModel)]="email" required autocomplete="username"></div>
            <div class="field"><label for="password">Password</label><input id="password" name="password" type="password" [(ngModel)]="password" required autocomplete="current-password"></div>
            @if (error()) { <p class="form-error" role="alert">{{ error() }}</p> }
            <button class="btn btn-primary" [disabled]="busy()">Sign in</button>
          </form>
        } @else {
          <form (ngSubmit)="submitCode()">
            <p>Enter the 6-digit code from your authenticator app.</p>
            <div class="field"><label for="code">Code</label><input id="code" name="code" [(ngModel)]="code" required inputmode="numeric" autocomplete="one-time-code" maxlength="7"></div>
            @if (error()) { <p class="form-error" role="alert">{{ error() }}</p> }
            <button class="btn btn-primary" [disabled]="busy()">Verify</button>
          </form>
        }
      </div>
    </div>
  `,
  styles: `
    .wrap { min-height: 100vh; display: grid; place-items: center; padding: 1rem; background: linear-gradient(135deg, #0F3D2E, #2E7D7A); }
    .box { width: min(400px, 100%); padding: 2rem; }
    .logo { width: 100%; height: auto; display: block; margin-bottom: 1rem; }
    h1 { font-size: 1.5rem; margin-top: .25rem; }
    button { width: 100%; }
  `,
})
export class LoginPage {
  private auth = inject(Auth);
  private router = inject(Router);
  protected step = signal<'password' | 'code'>('password');
  protected busy = signal(false);
  protected error = signal<string | null>(null);
  protected email = ''; protected password = ''; protected code = '';

  async submitPassword() { await this.run(() => this.auth.login(this.email, this.password)); }
  async submitCode() { await this.run(() => this.auth.loginMfa(this.code)); }

  private async run(action: () => Promise<string>) {
    this.busy.set(true); this.error.set(null);
    try {
      const status = await action();
      if (status === 'ok') await this.router.navigate(['/farms']);
      else if (status === 'mfa_required') this.step.set('code');
      else this.error.set(status === 'locked_out' ? 'Too many attempts. Try again in 15 minutes.' : 'Those details are not correct.');
    } catch { this.error.set('Sign-in failed. Please try again.'); }
    finally { this.busy.set(false); }
  }
}
