import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { Auth } from './auth';

@Component({
  selector: 'bo-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <div class="layout">
      <aside class="side on-dark">
        <a routerLink="/farms" class="wordmark"><span class="farmer">Farmer</span><span class="sample">Sample</span></a>
        <p class="sub">Back office</p>
        <nav>
          <a routerLink="/farms" routerLinkActive="active" [routerLinkActiveOptions]="{ exact: true }">Farms</a>
          <a routerLink="/farms/new" routerLinkActive="active">New farm</a>
          <a routerLink="/sample-requests" routerLinkActive="active">Sample requests</a>
          <a routerLink="/mfa" routerLinkActive="active">Two-factor sign-in</a>
        </nav>
        <div class="me">
          <p>{{ auth.user()?.displayName }}<br><span>{{ auth.user()?.email }}</span></p>
          <button class="btn btn-ghost light" (click)="logout()">Sign out</button>
        </div>
      </aside>
      <main>
        @if (auth.user() && !auth.user()!.mfaEnabled) {
          <p class="banner">Two-factor sign-in is not set up. It is required outside local development. <a routerLink="/mfa">Set it up now</a>.</p>
        }
        <router-outlet />
      </main>
    </div>
  `,
  styles: `
    .layout { display: grid; grid-template-columns: 240px 1fr; min-height: 100vh; }
    @media (max-width: 760px) { .layout { grid-template-columns: 1fr; } }
    .side { background: var(--color-primary); color: var(--color-background); padding: 1.5rem 1.25rem; display: flex; flex-direction: column; gap: .5rem; }
    .sub { margin: 0 0 1.25rem; font-size: .8rem; letter-spacing: .08em; text-transform: uppercase; opacity: .7; }
    nav { display: flex; flex-direction: column; gap: .25rem; }
    nav a { color: var(--color-background); text-decoration: none; padding: .55rem .75rem; border-radius: var(--radius-button); font-weight: 600; opacity: .85; }
    nav a:hover { background: rgba(247,243,232,.08); opacity: 1; }
    nav a.active { background: rgba(200,146,47,.22); opacity: 1; box-shadow: inset 3px 0 0 var(--color-accent); }
    .me { margin-top: auto; font-size: .85rem; } .me span { opacity: .7; }
    .btn-ghost.light { color: var(--color-background); border-color: rgba(247,243,232,.5); width: 100%; }
    main { padding: 2rem clamp(1rem, 3vw, 2.5rem); min-width: 0; }
    .banner { background: #F6EAD3; border: 1px solid #E8D3A6; padding: .75rem 1rem; border-radius: var(--radius-button); margin: 0 0 1.5rem; }
  `,
})
export class Shell {
  protected auth = inject(Auth);
  private router = inject(Router);
  async logout() { await this.auth.logout(); this.router.navigate(['/login']); }
}
