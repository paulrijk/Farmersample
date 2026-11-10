import { Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { catchError, map, of, startWith } from 'rxjs';
import { FarmApi } from '../farm-api';
import { FarmCard } from '../components/farm-card';
import { LotCard } from '../components/lot-card';
import { PublicFarm, PublicLot } from '../../../../../shared/api';

type State = { status: 'loading' } | { status: 'error' } | { status: 'ok'; farms: PublicFarm[] };

@Component({
  selector: 'fs-home',
  imports: [RouterLink, FarmCard, LotCard],
  template: `
    <section class="hero">
      <div class="container hero-inner">
        <div class="copy">
          <h1>Discover verified spices, directly from origin.</h1>
          <p class="lead">Explore Sri Lankan producers, review grades and order samples before you buy.</p>
          <div class="ctas">
            <a class="btn btn-accent" routerLink="/" fragment="lots">Explore lots</a>
            <a class="btn btn-ghost" routerLink="/" fragment="how-it-works">How verification works</a>
          </div>
        </div>
        <img class="emblem" src="brand/emblem-1200.webp" srcset="brand/emblem-640.webp 640w, brand/emblem-1200.webp 1200w"
             sizes="(max-width: 860px) 92vw, 560px" width="1200" height="754" fetchpriority="high"
             alt="FarmerSample.com — Sri Lanka · Spices · Direct from Farmers">
      </div>
    </section>

    <section id="how-it-works" class="container section">
      <h2>How it works</h2>
      <ol class="steps">
        <li class="card"><span class="num">1</span><h3>Farm visit</h3><p>Our team in Sri Lanka visits the farm, records products, volumes and harvest months, and takes a sample.</p></li>
        <li class="card"><span class="num">2</span><h3>Processed and graded at the hub</h3><p>We buy raw pepper and process it ourselves. Samples are prepared by hand; bulk orders are machine-processed: drying, destoning, dust removal and size grading. Each sample is graded for moisture and density. Lab tests follow when a buyer is serious.</p></li>
        <li class="card"><span class="num">3</span><h3>Order a sample</h3><p>Review the producer and grade, then request a sealed, numbered sample from that lot.</p></li>
        <li class="card"><span class="num">4</span><h3>Make an offer</h3><p>Send an offer through FarmerSample. We handle contracts, export and shipping with you.</p></li>
      </ol>
    </section>

    <section id="pepper" class="container section">
      <div class="section-head">
        <h2>Pepper, processed at origin</h2>
        <p class="muted">Processing decides the quality, so every bulk order is machine-processed in Sri Lanka for a fast, clean, consistent result.</p>
      </div>
      <ul class="products">
        <li class="card"><h3>Black pepper</h3><p>Dried, destoned, cleaned and size-graded. Density from 550 g/L.</p></li>
        <li class="card"><h3>White pepper</h3><p>The black husk removed, for a clean, mild heat and light-coloured dishes.</p></li>
        <li class="card"><h3>Red and coloured pepper</h3><p>Special processing for colour and character. Ask for current availability.</p></li>
        <li class="card"><h3>White label</h3><p>Ground and packed in Sri Lanka to your brand and pack size.</p></li>
      </ul>
    </section>

    <section id="lots" class="container section">
      <div class="section-head">
        <h2>Lots on offer</h2>
        <p class="muted">Graded at our hub in Sri Lanka. Order a sealed sample before you make an offer.</p>
      </div>
      @if (lots().length) {
        <div class="grid">
          @for (l of lots(); track l.code) { <fs-lot-card [lot]="l" /> }
        </div>
      } @else { <p class="muted">The first lots are being graded. Request a sample and we will tell you when they are ready.</p> }
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
    .hero { background: linear-gradient(180deg, #FBF8F0 0%, var(--color-background) 100%); border-bottom: 1px solid var(--color-border); padding: clamp(2rem, 5vw, 4rem) 0; }
    .hero-inner { display: grid; grid-template-columns: minmax(0, 1fr) minmax(0, 1.1fr); gap: clamp(1.5rem, 4vw, 3rem); align-items: center; }
    @media (max-width: 860px) { .hero-inner { grid-template-columns: 1fr; } .emblem { order: -1; max-width: 520px; margin-inline: auto; } }
    .hero h1 { max-width: 16ch; }
    .lead { font-size: 1.15rem; max-width: 46ch; color: var(--color-text); }
    .ctas { display: flex; flex-wrap: wrap; gap: .75rem; margin-top: 1.75rem; }
    .emblem { width: 100%; height: auto; display: block; }
    .section { padding-top: 3.5rem; }
    .section-head { display: flex; justify-content: space-between; align-items: center; gap: 1rem; flex-wrap: wrap; margin-bottom: 1rem; }
    .steps { list-style: none; padding: 0; margin: 1.5rem 0 0; display: grid; gap: 1rem; grid-template-columns: repeat(auto-fit, minmax(220px, 1fr)); }
    .steps p, .products p { margin: 0; color: var(--color-muted); }
    .products { list-style: none; padding: 0; margin: 0; display: grid; gap: 1rem; grid-template-columns: repeat(auto-fit, minmax(220px, 1fr)); }
    .products h3 { margin-top: 0; }
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
  protected lots = toSignal(this.api.lots().pipe(catchError(() => of([] as PublicLot[]))), { initialValue: [] as PublicLot[] });
  protected farms = () => { const s = this.state(); return s.status === 'ok' ? s.farms : []; };
}
