import { HttpClient } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { errorText } from '../staff-api';

@Component({
  selector: 'bo-password',
  imports: [FormsModule],
  template: `
    <h1>Change password</h1>
    <form class="card form" (ngSubmit)="save()" #f="ngForm">
      <div class="field"><label for="cp-cur">Current password</label><input id="cp-cur" name="cur" type="password" [(ngModel)]="current" required autocomplete="current-password"></div>
      <div class="field"><label for="cp-new">New password</label><input id="cp-new" name="new" type="password" [(ngModel)]="next" required minlength="12" autocomplete="new-password">
        <p class="muted small">At least 12 characters with a capital, a lower-case letter and a digit.</p></div>
      <div class="field"><label for="cp-rep">Repeat new password</label><input id="cp-rep" name="rep" type="password" [(ngModel)]="repeat" required autocomplete="new-password"></div>
      @if (error()) { <p class="form-error" role="alert">{{ error() }}</p> }
      @if (done()) { <p class="form-ok" role="status">Password changed.</p> }
      <button class="btn btn-primary" [disabled]="f.invalid">Change password</button>
    </form>
  `,
  styles: `.form { max-width: 420px; } .small { font-size: .85rem; margin: .3rem 0 0; }`,
})
export class PasswordPage {
  private http = inject(HttpClient);
  protected current = ''; protected next = ''; protected repeat = '';
  protected error = signal<string | null>(null);
  protected done = signal(false);

  async save() {
    this.error.set(null); this.done.set(false);
    if (this.next !== this.repeat) { this.error.set('The two new passwords are not the same.'); return; }
    try {
      await firstValueFrom(this.http.post('/api/v1/auth/change-password', { currentPassword: this.current, newPassword: this.next }));
      this.current = this.next = this.repeat = ''; this.done.set(true);
    } catch (e) { this.error.set(errorText(e)); }
  }
}
