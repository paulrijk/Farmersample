import { Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { catchError, map, of, startWith } from 'rxjs';
import { FarmApi } from '../farm-api';
import { FarmCard } from '../components/farm-card';
import { PublicFarm } from '../../../../../shared/api';

type State = { status: 'loading' } | { status: 'error' } | { status: 'ok'; farms: PublicFarm[] };

@Component({
  selector: 'fs-home',
  imports: [RouterLink, FarmCard],
  template: `
    <section class="hero on-dark">
      <div class="container hero-inner">
        <p class="eyebrow">Sri Lanka · Spices · Direct from Farmers</p>
        <h1>Discover verified spices, directly from origin.</h1>
        <p class="lead">Explore Sri Lankan producers, review grades and order samples before you buy.</p>
        <div class="ctas">
          <a class="btn btn-accent" routerLink="/" fragment="producers">Explore producers</a>
          <a class="btn btn-ghost light" routerLink="/" fragment="how-it-works">How verification works</a>
        </div>
      </div>
    </section>

    <section id="how-it-works" class="container section">
      <h2>How it works</h2>
      <ol class="steps">
        <li class="card"><span class="num">1</span><h3>Farm visit</h3><p>Our team in Sri Lanka visits the farm, records products, volumes and harvest months, and takes a sample.</p></li>
        <li class="card"><span class="num">2</span><h3>Graded at the hub</h3><p>Each sample is dried and graded for moisture and density. Lab tests follow when a buyer is serious.</p></li>
        <li class="card"><span class="num">3</span><h3>Order a sample</h3><p>Review the producer and grade, then request a sealed, numbered sample from that lot.</p></li>
        <li class="card"><span class="num">4</span><h3>Make an offer</h3><p>Send an offer through FarmerSample. We handle contracts, export and shipping with you.</p></li>
      </ol>
    </section>

    <section id="producers" class="container section">
      <div class="section-head">
        <h2>Producers</h2>
        <a routerLink="/request-sample" class="btn btn-primary">Order samples</a>
      </div>
      @switch (state().status) {
        @case ('loading') { <p class="muted">Loading producers…</p> }
        @case ('error') { <p class="form-error">Producers could not be loaded. Please try again later.</p> }
        @case ('ok') {
          @if (farms().length === 0) { <p class="muted">The first producers are being added. Request a sample and we will get back to you.</p> }
          <div class="grid">
            @for (f of farms(); track f.code) { <fs-farm-card [farm]="f" /> }
          </div>
        }
      }
    </section>
  `,
  styles: `
    .hero { background: radial-gradient(circle at 85% 20%, rgba(200,146,47,.35), transparent 45%), linear-gradient(135deg, #0F3D2E 0%, #14513d 60%, #2E7D7A 100%); color: var(--color-background); padding: clamp(3rem, 8vw, 6rem) 0; }
    .hero h1 { color: var(--color-background); max-width: 16ch; }
    .eyebrow { text-transform: uppercase; letter-spacing: .08em; font-size: .8rem; font-weight: 600; color: #E9C989; margin: 0 0 1rem; }
    .lead { font-size: 1.15rem; max-width: 46ch; opacity: .92; }
    .ctas { display: flex; flex-wrap: wrap; gap: .75rem; margin-top: 1.75rem; }
    .btn-ghost.light { color: var(--color-background); border-color: rgba(247,243,232,.7); }
    .btn-ghost.light:hover { background: rgba(247,243,232,.1); }
    .section { padding-top: 3.5rem; }
    .section-head { display: flex; justify-content: space-between; align-items: center; gap: 1rem; flex-wrap: wrap; margin-bottom: 1rem; }
    .steps { list-style: none; padding: 0; margin: 1.5rem 0 0; display: grid; gap: 1rem; grid-template-columns: repeat(auto-fit, minmax(220px, 1fr)); }
    .steps p { margin: 0; color: var(--color-muted); }
    .num { display: inline-grid; place-items: center; width: 2rem; height: 2rem; border-radius: 50%; background: var(--color-accent); color: var(--color-text); font-weight: 700; margin-bottom: .75rem; }
    .grid { display: grid; gap: 1.25rem; grid-template-columns: repeat(auto-fill, minmax(280px, 1fr)); }
  `,
})
export class HomePage {
  private api = inject(FarmApi);
  protected state = toSignal(
    this.api.list().pipe(
      map(farms => ({ status: 'ok', farms }) as State),
      catchError(() => of({ status: 'error' } as State)),
      startWith({ status: 'loading' } as State)),
    { requireSync: true });
  protected farms = () => { const s = this.state(); return s.status === 'ok' ? s.farms : []; };
}
