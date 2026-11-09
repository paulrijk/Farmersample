import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PublicFarm } from '../../../../../shared/api';

@Component({
  selector: 'fs-farm-card',
  imports: [RouterLink],
  template: `
    @let f = farm();
    <article class="card farm-card">
      @if (f.photos[0]; as ph) {
        <img class="photo" [src]="ph.url" [alt]="ph.caption ?? f.name" loading="lazy" width="400" height="160">
      } @else {
        <div class="visual" aria-hidden="true"><span>{{ f.products[0]?.product ?? 'Spices' }}</span></div>
      }
      <div class="body">
        <p class="origin">{{ f.region }}, Sri Lanka</p>
        <h3><a [routerLink]="['/farms', f.code]">{{ f.name }}</a></h3>
        @if (f.farmerName) { <p class="muted producer">Producer: {{ f.farmerName }}</p> }
        <div class="chips">
          @for (p of f.products; track p.product) {
            <span class="chip">{{ p.product }}@if (p.grade) { · {{ p.grade }} }</span>
          }
          @for (c of f.certifications; track c) { <span class="chip chip-teal">{{ c }}</span> }
        </div>
        <a class="btn btn-ghost" [routerLink]="['/farms', f.code]">View producer</a>
      </div>
    </article>
  `,
  styles: `
    .farm-card { padding: 0; overflow: hidden; display: flex; flex-direction: column; height: 100%; }
    .photo { width: 100%; height: 160px; object-fit: cover; display: block; }
    .visual { height: 120px; background: linear-gradient(135deg, var(--color-primary), var(--color-teal)); display: flex; align-items: flex-end; padding: .75rem 1.25rem; }
    .visual span { font-family: var(--font-display); color: var(--color-background); font-size: 1.1rem; opacity: .9; }
    .body { padding: 1.25rem; display: flex; flex-direction: column; gap: .5rem; flex: 1; }
    .origin { margin: 0; font-size: .8rem; font-weight: 600; letter-spacing: .04em; text-transform: uppercase; color: var(--color-cinnamon); }
    h3 { margin: 0; } h3 a { color: inherit; text-decoration: none; } h3 a:hover { text-decoration: underline; }
    .producer { margin: 0; font-size: .9rem; }
    .chips { display: flex; flex-wrap: wrap; gap: .4rem; margin: .25rem 0 .75rem; }
    .btn { margin-top: auto; align-self: flex-start; }
  `,
})
export class FarmCard { farm = input.required<PublicFarm>(); }
