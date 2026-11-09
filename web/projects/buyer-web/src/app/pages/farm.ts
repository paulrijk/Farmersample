import { Component, computed, inject, input } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { Title } from '@angular/platform-browser';
import { RouterLink } from '@angular/router';
import { catchError, map, of, startWith, switchMap, tap } from 'rxjs';
import { FarmApi } from '../farm-api';
import { SampleRequestForm } from '../components/sample-request-form';
import { LotCard } from '../components/lot-card';
import { PublicFarm, PublicLot, monthNames } from '../../../../../shared/api';

type State = { status: 'loading' } | { status: 'missing' } | { status: 'ok'; farm: PublicFarm };

@Component({
  selector: 'fs-farm',
  imports: [RouterLink, SampleRequestForm, LotCard],
  template: `
    <div class="container page">
      @switch (state().status) {
        @case ('loading') { <p class="muted">Loading…</p> }
        @case ('missing') {
          <h1>Producer not found</h1>
          <p>This code does not match a published producer. It may have been mistyped or the profile is no longer public.</p>
          <a routerLink="/" class="btn btn-primary">See all producers</a>
        }
        @case ('ok') {
          @let f = farm()!;
          <p class="origin">{{ f.region }}, Sri Lanka · <span class="code">{{ f.code }}</span></p>
          <h1>{{ f.name }}</h1>
          @if (f.farmerName) { <p class="producer">Producer: <strong>{{ f.farmerName }}</strong></p> }
          <div class="layout">
            <div>
              @if (f.photos.length) {
                <div class="photos">
                  @for (ph of f.photos; track ph.url) {
                    <figure><img [src]="ph.url" [width]="ph.width" [height]="ph.height" [alt]="ph.caption ?? f.name" loading="lazy">@if (ph.caption) { <figcaption>{{ ph.caption }}</figcaption> }</figure>
                  }
                </div>
              }
              @if (f.story) { <p class="story">{{ f.story }}</p> }
              <h2>Products</h2>
              <table class="products">
                <thead><tr><th>Product</th><th>Grade</th><th>Harvest</th></tr></thead>
                <tbody>
                  @for (p of f.products; track p.product) {
                    <tr><td>{{ p.product }}</td><td>{{ p.grade ?? '—' }}</td><td>{{ months(p.harvestMonths) || '—' }}</td></tr>
                  }
                </tbody>
              </table>
              @if (farmLots().length) {
                <h2>Lots with this producer</h2>
                <div class="lots">@for (l of farmLots(); track l.code) { <fs-lot-card [lot]="l" /> }</div>
              }
              <h2>Details</h2>
              <dl class="facts">
                <dt>Region</dt><dd>{{ f.region }}, Sri Lanka</dd>
                @if (f.hectares) { <dt>Farm size</dt><dd>{{ f.hectares }} ha</dd> }
                <dt>Certifications</dt><dd>{{ f.certifications.length ? f.certifications.join(', ') : 'None recorded yet' }}</dd>
              </dl>
              <p class="note muted">Farm details are recorded by our Sri Lanka team during the farm visit. Grades shown are from our hub grading; lab reports are added per lot when available.</p>
            </div>
            <aside class="card">
              <h3>Request a sample</h3>
              <p class="muted small">Tell us who you are and we will send a sealed sample from this producer.</p>
              <fs-sample-request-form [farmCode]="f.code" />
            </aside>
          </div>
        }
      }
    </div>
  `,
  styles: `
    .page { padding-top: 2.5rem; }
    .origin { margin: 0 0 .5rem; font-size: .8rem; font-weight: 600; letter-spacing: .04em; text-transform: uppercase; color: var(--color-cinnamon); }
    .code { font-family: ui-monospace, monospace; letter-spacing: 0; color: var(--color-muted); }
    .producer { margin-top: -.25rem; }
    .layout { display: grid; gap: 2rem; grid-template-columns: minmax(0, 1fr) 360px; align-items: start; margin-top: 1.5rem; }
    @media (max-width: 860px) { .layout { grid-template-columns: 1fr; } }
    .story { font-size: 1.1rem; }
    .photos { display: grid; gap: .75rem; grid-template-columns: repeat(auto-fill, minmax(220px, 1fr)); margin-bottom: 1.5rem; }
    figure { margin: 0; } figure img { width: 100%; height: auto; aspect-ratio: 4/3; object-fit: cover; border-radius: var(--radius-card); display: block; }
    figcaption { font-size: .85rem; color: var(--color-muted); margin-top: .3rem; }
    .products { width: 100%; border-collapse: collapse; background: var(--color-surface); border-radius: var(--radius-card); overflow: hidden; margin-bottom: 2rem; }
    .products th, .products td { text-align: left; padding: .7rem .9rem; border-bottom: 1px solid var(--color-border); }
    .products th { background: #EFE9D8; font-size: .85rem; }
    .facts { display: grid; grid-template-columns: max-content 1fr; gap: .4rem 1.5rem; }
    .facts dt { font-weight: 600; } .facts dd { margin: 0; }
    .note { font-size: .85rem; margin-top: 1.5rem; }
    .small { font-size: .9rem; }
    .lots { display: grid; gap: 1rem; grid-template-columns: repeat(auto-fill, minmax(260px, 1fr)); margin-bottom: 2rem; }
    aside { position: sticky; top: 80px; }
  `,
})
export class FarmPage {
  private api = inject(FarmApi);
  private title = inject(Title);
  code = input.required<string>();
  protected months = monthNames;

  protected state = toSignal(
    toObservable(this.code).pipe(
      switchMap(code => this.api.get(code).pipe(
        map(farm => ({ status: 'ok', farm }) as State),
        catchError(() => of({ status: 'missing' } as State)),
        startWith({ status: 'loading' } as State))),
      tap(s => this.title.setTitle(s.status === 'ok' ? `${s.farm.name} — FarmerSample.com` : 'FarmerSample.com'))),
    { initialValue: { status: 'loading' } as State });
  private allLots = toSignal(this.api.lots().pipe(catchError(() => of([] as PublicLot[]))), { initialValue: [] as PublicLot[] });
  protected farmLots = computed(() => this.allLots().filter(l => l.farms.some(f => f.code === this.farm()?.code)));
  protected farm = computed(() => { const s = this.state(); return s.status === 'ok' ? s.farm : null; });
}
