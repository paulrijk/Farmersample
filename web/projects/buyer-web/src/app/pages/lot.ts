import { Component, computed, inject, input } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { DatePipe } from '@angular/common';
import { Title } from '@angular/platform-browser';
import { RouterLink } from '@angular/router';
import { catchError, map, of, startWith, switchMap, tap } from 'rxjs';
import { FarmApi } from '../farm-api';
import { SampleRequestForm } from '../components/sample-request-form';
import { PublicLot, kg, money, monthNames } from '../../../../../shared/api';

type State = { status: 'loading' } | { status: 'missing' } | { status: 'ok'; lot: PublicLot };

@Component({
  selector: 'fs-lot',
  imports: [RouterLink, SampleRequestForm, DatePipe],
  template: `
    <div class="container page">
      @switch (state().status) {
        @case ('loading') { <p class="muted">Loading…</p> }
        @case ('missing') {
          <h1>Lot not available</h1>
          <p>This lot is not (or no longer) offered. It may be sold out or expired.</p>
          <a routerLink="/" fragment="lots" class="btn btn-primary">See current lots</a>
        }
        @case ('ok') {
          @let l = lot()!;
          <p class="origin">Lot · <span class="code">{{ l.code }}</span></p>
          <h1>{{ l.product }} · {{ l.grade }}</h1>
          <p class="lead">{{ kgs(l.volumeKg) }} from {{ l.farms.length === 1 ? 'one producer' : l.farms.length + ' producers' }} in Sri Lanka ·
            indicative {{ price(l.priceMinPerKg, l.currency) }}–{{ price(l.priceMaxPerKg, l.currency) }} per kg, {{ l.incoterm }}</p>
          <div class="layout">
            <div>
              <section class="card verified">
                <h2>Graded at our hub <span class="chip chip-teal">Verified by FarmerSample</span></h2>
                @if (l.hubGrading; as g) {
                  <dl class="facts">
                    <dt>Grade</dt><dd>{{ g.grade }}</dd>
                    @if (g.densityGramsPerLitre) { <dt>Density</dt><dd>{{ g.densityGramsPerLitre }} g/L</dd> }
                    @if (g.moisturePercent !== null) { <dt>Moisture</dt><dd>{{ g.moisturePercent }}%</dd> }
                    @if (g.cleanliness) { <dt>Cleanliness</dt><dd>{{ g.cleanliness }}</dd> }
                    <dt>Graded on</dt><dd>{{ g.gradedOn | date: 'd MMM y' }}</dd>
                  </dl>
                  <p class="muted small">
                    @if (l.bundled) { Measured on the combined sample, mixed in proportion to each producer's volume. Sealed samples are taken from this same mix. }
                    @else { Measured on the producer's sample. Sealed samples are taken from this same sample. }
                    Lab reports (residues, aflatoxin) are added when a buyer is serious about the lot.
                  </p>
                } @else { <p class="muted">Grading in progress.</p> }
              </section>

              <h2>Producers</h2>
              <table class="farms">
                <thead><tr><th>Producer</th><th>Region</th><th>Share of lot</th></tr></thead>
                <tbody>
                  @for (f of l.farms; track f.code) {
                    <tr><td><a [routerLink]="['/farms', f.code]">{{ f.name }}</a></td><td>{{ f.region }}</td><td>{{ f.sharePercent }}%</td></tr>
                  }
                </tbody>
              </table>

              <h2>Offer details</h2>
              <dl class="facts">
                <dt>Volume</dt><dd>{{ kgs(l.volumeKg) }} <span class="muted">(producer-supplied)</span></dd>
                <dt>Harvest</dt><dd>{{ months(l.harvestMonths) || '—' }} <span class="muted">(producer-supplied)</span></dd>
                <dt>Indicative price</dt><dd>{{ price(l.priceMinPerKg, l.currency) }}–{{ price(l.priceMaxPerKg, l.currency) }} per kg</dd>
                <dt>Delivery terms</dt><dd>{{ l.incoterm }}</dd>
                <dt>Offer valid until</dt><dd>{{ l.validUntil | date: 'd MMM y' }}</dd>
              </dl>
            </div>
            <aside class="card">
              <h3>Order a sample of this lot</h3>
              <p class="muted small">We send a sealed, numbered sample from this lot. Paid sample ordering opens in 2027; until then we confirm each request by e-mail.</p>
              <fs-sample-request-form [lotCode]="l.code" />
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
    .lead { font-size: 1.1rem; max-width: 60ch; }
    .layout { display: grid; gap: 2rem; grid-template-columns: minmax(0, 1fr) 360px; align-items: start; margin-top: 1.5rem; }
    @media (max-width: 860px) { .layout { grid-template-columns: 1fr; } }
    .verified { border-left: 4px solid var(--color-teal); margin-bottom: 2rem; }
    .verified h2 { display: flex; gap: .75rem; align-items: center; flex-wrap: wrap; font-size: 1.35rem; }
    .facts { display: grid; grid-template-columns: max-content 1fr; gap: .45rem 1.5rem; margin: 0 0 1rem; }
    .facts dt { font-weight: 600; } .facts dd { margin: 0; }
    .farms { width: 100%; border-collapse: collapse; background: var(--color-surface); border-radius: var(--radius-card); overflow: hidden; margin-bottom: 2rem; }
    .farms th, .farms td { text-align: left; padding: .7rem .9rem; border-bottom: 1px solid var(--color-border); }
    .farms th { background: #EFE9D8; font-size: .85rem; }
    .small { font-size: .88rem; }
    aside { position: sticky; top: 80px; }
  `,
})
export class LotPage {
  private api = inject(FarmApi);
  private title = inject(Title);
  code = input.required<string>();
  protected months = monthNames;
  protected kgs = kg;
  protected price = money;

  protected state = toSignal(
    toObservable(this.code).pipe(
      switchMap(code => this.api.lot(code).pipe(
        map(lot => ({ status: 'ok', lot }) as State),
        catchError(() => of({ status: 'missing' } as State)),
        startWith({ status: 'loading' } as State))),
      tap(s => this.title.setTitle(s.status === 'ok' ? `${s.lot.product} ${s.lot.grade} · lot ${s.lot.code} — FarmerSample.com` : 'FarmerSample.com'))),
    { initialValue: { status: 'loading' } as State });
  protected lot = computed(() => { const s = this.state(); return s.status === 'ok' ? s.lot : null; });
}
