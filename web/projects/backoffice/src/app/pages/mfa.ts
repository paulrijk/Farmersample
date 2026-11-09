import { HttpClient } from '@angular/common/http';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { toDataURL } from 'qrcode';
import { Auth } from '../auth';

@Component({
  selector: 'bo-mfa',
  imports: [FormsModule],
  template: `
    <h1>Two-factor sign-in</h1>
    @if (auth.user()?.mfaEnabled && auth.user()?.mfaSatisfied) {
      <p class="form-ok">Two-factor sign-in is active for your account.</p>
    } @else {
      <ol class="steps">
        <li>Install an authenticator app (Microsoft Authenticator, Google Authenticator, 1Password…).</li>
        <li>Scan this QR code, or type the key by hand.</li>
        <li>Enter the 6-digit code the app shows.</li>
      </ol>
      @if (qr()) {
        <div class="card setup">
          <img [src]="qr()" width="200" height="200" alt="QR code for your authenticator app">
          <div><p class="muted">Key</p><code>{{ key() }}</code></div>
        </div>
      }
      <form (ngSubmit)="enable()" class="code-form">
        <div class="field"><label for="code">Code</label><input id="code" name="code" [(ngModel)]="code" required inputmode="numeric" autocomplete="one-time-code" maxlength="7"></div>
        @if (error()) { <p class="form-error" role="alert">{{ error() }}</p> }
        <button class="btn btn-primary">Turn on two-factor sign-in</button>
      </form>
    }
  `,
  styles: `
    .steps { padding-left: 1.25rem; } .setup { display: flex; gap: 1.5rem; align-items: center; flex-wrap: wrap; max-width: 560px; }
    code { font-size: 1rem; word-break: break-all; } .code-form { max-width: 320px; margin-top: 1.5rem; }
  `,
})
export class MfaPage implements OnInit {
  protected auth = inject(Auth);
  private http = inject(HttpClient);
  protected qr = signal<string | null>(null);
  protected key = signal('');
  protected error = signal<string | null>(null);
  protected code = '';

  async ngOnInit() {
    if (this.auth.user()?.mfaEnabled && this.auth.user()?.mfaSatisfied) return;
    const setup = await firstValueFrom(this.http.get<{ sharedKey: string; authenticatorUri: string }>('/api/v1/auth/mfa/setup'));
    this.key.set(setup.sharedKey.match(/.{1,4}/g)!.join(' '));
    this.qr.set(await toDataURL(setup.authenticatorUri, { margin: 1, width: 200, color: { dark: '#0F3D2E', light: '#FFFFFF' } }));
  }

  async enable() {
    this.error.set(null);
    const r = await firstValueFrom(this.http.post<{ status: string }>('/api/v1/auth/mfa/enable', { code: this.code }));
    if (r.status === 'ok') await this.auth.refresh(); else this.error.set('That code is not valid. Check the time on your phone and try again.');
  }
}
