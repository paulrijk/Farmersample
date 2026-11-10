import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PublicLot, kg, money } from '../../../../../shared/api';

@Component({
  selector: 'fs-lot-card',
  imports: [RouterLink],
  template: `
    @let l = lot();
    <article class="card lot-card">
      @if (l.photos[0]; as ph) { <a [routerLink]="['/lots', l.code]" class="photo" tabindex="-1" aria-hidden="true"><img [src]="ph.url" alt="" loading="lazy" width="400" height="170"></a> }
      <div class="top">
        <span class="chip chip-cinnamon">{{ l.product }}</span>
        @if (l.hubGrading) { <span class="chip chip-teal" title="Graded at the FarmerSample hub">Hub graded</span> }
      </div>
      <h3><a [routerLink]="['/lots', l.code]">{{ l.product }} · {{ l.grade }}</a></h3>
      <p class="origin">{{ regions() }}, Sri Lanka</p>
      <dl>
        <div><dt>Volume</dt><dd>{{ kgs(l.volumeKg) }}</dd></div>
        <div><dt>Indicative price</dt><dd>{{ price(l.priceMinPerKg, l.currency) }}–{{ price(l.priceMaxPerKg, l.currency) }}/kg</dd></div>
        <div><dt>Terms</dt><dd>{{ l.incoterm }}</dd></div>
        <div><dt>Producers</dt><dd>{{ l.farms.length }}@if (l.bundled) { · combined lot }</dd></div>
      </dl>
      <a class="btn btn-primary" [routerLink]="['/lots', l.code]">View lot & order sample</a>
    </article>
  `,
  styles: `
    .lot-card { display: flex; flex-direction: column; gap: .6rem; height: 100%; }
    .photo { margin: -1.25rem -1.25rem .4rem; display: block; } .photo img { width: 100%; height: 170px; object-fit: cover; display: block; border-radius: var(--radius-card) var(--radius-card) 0 0; }
    .top { display: flex; gap: .4rem; flex-wrap: wrap; }
    h3 { margin: 0; } h3 a { color: inherit; text-decoration: none; } h3 a:hover { text-decoration: underline; }
    .origin { margin: 0; font-size: .8rem; font-weight: 600; letter-spacing: .04em; text-transform: uppercase; color: var(--color-cinnamon); }
    dl { display: grid; grid-template-columns: 1fr 1fr; gap: .6rem 1rem; margin: .25rem 0 .5rem; }
    dt { font-size: .75rem; color: var(--color-muted); text-transform: uppercase; letter-spacing: .04em; }
    dd { margin: 0; font-weight: 600; }
    .btn { margin-top: auto; }
  `,
})
export class LotCard {
  lot = input.required<PublicLot>();
  protected regions = () => [...new Set(this.lot().farms.map(f => f.region))].join(' & ');
  protected kgs = kg;
  protected price = money;
}
