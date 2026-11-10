import { HttpClient } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { Auth } from '../auth';
import { errorText } from '../staff-api';

interface StaffUser { id: string; email: string; displayName: string; roles: string[]; mfaEnabled: boolean; disabled: boolean; }
const ROLES: [string, string][] = [
  ['Admin', 'Admin: everything, including users'], ['Staff', 'Staff: farms, lots, sample requests'],
  ['QualityController', 'Quality controller: grading and lots'], ['FieldAgent', 'Field agent: farm visits and photos'],
];
const newPassword = () => {
  const set = 'ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789';
  const pick = (chars: string) => chars[crypto.getRandomValues(new Uint32Array(1))[0] % chars.length];
  return Array.from({ length: 13 }, () => pick(set)).join('') + pick('ABCDEFGHJKLMNPQRSTUVWXYZ') + pick('abcdefghijkmnpqrstuvwxyz') + pick('23456789');
};

@Component({
  selector: 'bo-users',
  imports: [FormsModule],
  template: `
    <div class="head"><h1>Users</h1><button class="btn btn-primary" (click)="openNew()">New user</button></div>
    <p class="muted">Back-office accounts. A new user signs in with the temporary password you give them, sets up two-factor sign-in, and should then change the password.</p>
    @if (message()) { <p class="form-ok" role="status">{{ message() }}</p> }
    @if (error() && !showNew() && !passwordFor()) { <p class="form-error" role="alert">{{ error() }}</p> }

    <div class="card table-wrap">
      <table>
        <thead><tr><th>Name</th><th>E-mail</th><th>Role</th><th>Two-factor</th><th>Status</th><th></th></tr></thead>
        <tbody>
          @for (u of users(); track u.id) {
            <tr>
              <td><strong>{{ u.displayName }}</strong>@if (u.id === auth.user()?.id) { <span class="muted"> (you)</span> }</td>
              <td>{{ u.email }}</td>
              <td>{{ u.roles.join(', ') }}</td>
              <td>@if (u.mfaEnabled) { <span class="chip">On</span> } @else { <span class="chip chip-gold">Not set up</span> }</td>
              <td>@if (u.disabled) { <span class="chip chip-cinnamon">Disabled</span> } @else { <span class="chip">Active</span> }</td>
              <td class="actions">
                <button class="btn btn-ghost sm" (click)="openPassword(u)">New password</button>
                @if (u.mfaEnabled) { <button class="btn btn-ghost sm" (click)="resetMfa(u)">Reset two-factor</button> }
                @if (u.id !== auth.user()?.id) {
                  @if (u.disabled) { <button class="btn btn-ghost sm" (click)="setDisabled(u, false)">Enable</button> }
                  @else { <button class="btn btn-ghost sm" (click)="setDisabled(u, true)">Disable</button> }
                }
              </td>
            </tr>
          }
        </tbody>
      </table>
    </div>

    @if (showNew()) {
      <div class="overlay" (click)="showNew.set(false)">
        <form class="card dialog" (click)="$event.stopPropagation()" (ngSubmit)="create()" #f="ngForm" role="dialog" aria-labelledby="nu-title">
          <h2 id="nu-title">New user</h2>
          <div class="field"><label for="nu-name">Name</label><input id="nu-name" name="name" [(ngModel)]="n.displayName" required></div>
          <div class="field"><label for="nu-email">E-mail (used to sign in)</label><input id="nu-email" name="email" type="email" [(ngModel)]="n.email" required email></div>
          <div class="field"><label for="nu-role">Role</label>
            <select id="nu-role" name="role" [(ngModel)]="n.role">@for (r of roles; track r[0]) { <option [value]="r[0]">{{ r[1] }}</option> }</select></div>
          <div class="field"><label for="nu-pw">Temporary password</label>
            <div class="pw"><input id="nu-pw" name="pw" [(ngModel)]="n.password" required minlength="12" autocomplete="off"><button type="button" class="btn btn-ghost sm" (click)="n.password = generate()">Generate</button></div>
            <p class="muted small">At least 12 characters with a capital, a lower-case letter and a digit. Pass it on by phone or a private message, not together with the site password.</p></div>
          @if (error()) { <p class="form-error" role="alert">{{ error() }}</p> }
          <div class="buttons"><button type="button" class="btn btn-ghost" (click)="showNew.set(false)">Cancel</button><button class="btn btn-primary" [disabled]="f.invalid">Create user</button></div>
        </form>
      </div>
    }

    @if (passwordFor(); as u) {
      <div class="overlay" (click)="passwordFor.set(null)">
        <form class="card dialog" (click)="$event.stopPropagation()" (ngSubmit)="setPassword(u)" #pf="ngForm" role="dialog" aria-labelledby="pw-title">
          <h2 id="pw-title">New password for {{ u.displayName }}</h2>
          <p class="muted">Their current sessions end within a few minutes.</p>
          <div class="field"><label for="rp-pw">Temporary password</label>
            <div class="pw"><input id="rp-pw" name="pw" [(ngModel)]="resetPw" required minlength="12" autocomplete="off"><button type="button" class="btn btn-ghost sm" (click)="resetPw = generate()">Generate</button></div></div>
          @if (error()) { <p class="form-error" role="alert">{{ error() }}</p> }
          <div class="buttons"><button type="button" class="btn btn-ghost" (click)="passwordFor.set(null)">Cancel</button><button class="btn btn-primary" [disabled]="pf.invalid">Set password</button></div>
        </form>
      </div>
    }
  `,
  styles: `
    .head { display: flex; justify-content: space-between; align-items: center; gap: 1rem; }
    .table-wrap { padding: 0; overflow-x: auto; }
    table { width: 100%; border-collapse: collapse; font-size: .92rem; }
    th, td { text-align: left; padding: .7rem .9rem; border-bottom: 1px solid var(--color-border); vertical-align: middle; }
    th { background: #EFE9D8; font-size: .8rem; text-transform: uppercase; letter-spacing: .04em; }
    .actions { display: flex; gap: .4rem; justify-content: flex-end; flex-wrap: wrap; } .sm { padding: .4rem .7rem; font-size: .85rem; } .small { font-size: .85rem; }
    .overlay { position: fixed; inset: 0; background: rgba(20,37,30,.5); display: grid; place-items: center; padding: 1rem; z-index: 20; }
    .dialog { width: min(520px, 100%); max-height: 90vh; overflow: auto; } .buttons { display: flex; justify-content: flex-end; gap: .5rem; }
    .pw { display: flex; gap: .5rem; } .pw input { font-family: ui-monospace, monospace; }
  `,
})
export class UsersPage {
  private http = inject(HttpClient);
  protected auth = inject(Auth);
  private base = '/api/v1/admin/users';
  protected users = signal<StaffUser[]>([]);
  protected showNew = signal(false);
  protected passwordFor = signal<StaffUser | null>(null);
  protected message = signal<string | null>(null);
  protected error = signal<string | null>(null);
  protected roles = ROLES;
  protected generate = newPassword;
  protected n = { displayName: '', email: '', role: 'Admin', password: '' };
  protected resetPw = '';

  constructor() { this.load(); }
  private async load() { this.users.set(await firstValueFrom(this.http.get<StaffUser[]>(this.base))); }
  private async run(action: () => Promise<unknown>, done: string) {
    this.error.set(null); this.message.set(null);
    try { await action(); this.message.set(done); await this.load(); return true; }
    catch (e) { this.error.set(errorText(e)); return false; }
  }

  openNew() { this.n = { displayName: '', email: '', role: 'Admin', password: newPassword() }; this.error.set(null); this.showNew.set(true); }
  async create() {
    const { email, password } = this.n;
    if (await this.run(() => firstValueFrom(this.http.post(this.base, this.n)), `User ${email} created. Temporary password: ${password}`)) this.showNew.set(false);
  }
  openPassword(u: StaffUser) { this.resetPw = newPassword(); this.error.set(null); this.passwordFor.set(u); }
  async setPassword(u: StaffUser) {
    const pw = this.resetPw;
    if (await this.run(() => firstValueFrom(this.http.post(`${this.base}/${u.id}/password`, { password: pw })), `New temporary password for ${u.email}: ${pw}`)) this.passwordFor.set(null);
  }
  resetMfa(u: StaffUser) {
    if (!confirm(`Reset two-factor sign-in for ${u.displayName}? They set it up again at their next sign-in.`)) return;
    this.run(() => firstValueFrom(this.http.post(`${this.base}/${u.id}/reset-mfa`, {})), `Two-factor sign-in reset for ${u.email}.`);
  }
  setDisabled(u: StaffUser, disabled: boolean) {
    if (disabled && !confirm(`Disable ${u.displayName}? They can no longer sign in.`)) return;
    this.run(() => firstValueFrom(this.http.post(`${this.base}/${u.id}/${disabled ? 'disable' : 'enable'}`, {})), `${u.email} ${disabled ? 'disabled' : 'enabled'}.`);
  }
}
