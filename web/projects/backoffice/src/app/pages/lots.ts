import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { Lot, StaffApi } from '../staff-api';

@Component({
  selector: 'bo-lots',
  imports: [RouterLink, DatePipe, DecimalPipe],
  template: `
    <div class="head"><h1>Lots</h1><a routerLink="/lots/new" class="btn btn-primary">New lot</a></div>
    <p class="muted">A lot is one product and one grade, from one or more farms. Buyers see published lots and order samples from them.</p>
    <div class="card table-wrap">
      <table>
        <thead><tr><th>Lot</th><th>Product</th><th>Farms</th><th>Volume</th><th>Price / kg</th><th>Valid until</th><th>Status</th></tr></thead>
        <tbody>
          @for (l of lots(); track l.id) {
            <tr>
              <td><a [routerLink]="['/lots', l.id]"><code>{{ l.code }}</code></a></td>
              <td><strong>{{ l.product }}</strong> · {{ l.grade }}</td>
              <td>{{ farmNames(l) }}</td>
              <td>{{ l.committedVolumeKg | number }} kg</td>
              <td>{{ l.currency }} {{ l.priceMinPerKg | number: '1.2-2' }}–{{ l.priceMaxPerKg | number: '1.2-2' }}</td>
              <td>{{ l.validUntil | date: 'd MMM y' }}</td>
              <td>
                <span class="chip" [class.chip-gold]="l.status === 'Draft'" [class.chip-cinnamon]="l.status === 'Closed'">{{ l.status }}@if (l.closedReason) { · {{ l.closedReason }} }</span>
                @if (l.status === 'Draft' && l.publishBlockers.length) { <span class="muted small"> {{ l.publishBlockers.length }} to do</span> }
              </td>
            </tr>
          } @empty { <tr><td colspan="7" class="muted">No lots yet. Grade some sample batches, then create a lot.</td></tr> }
        </tbody>
      </table>
    </div>
  `,
  styles: `
    .head { display: flex; justify-content: space-between; align-items: center; gap: 1rem; }
    .table-wrap { padding: 0; overflow-x: auto; }
    table { width: 100%; border-collapse: collapse; font-size: .92rem; }
    th, td { text-align: left; padding: .7rem .9rem; border-bottom: 1px solid var(--color-border); }
    th { background: #EFE9D8; font-size: .8rem; text-transform: uppercase; letter-spacing: .04em; } .small { font-size: .85rem; }
  `,
})
export class LotsPage {
  protected lots = toSignal(inject(StaffApi).lots(), { initialValue: [] as Lot[] });
  protected farmNames = (l: Lot) => l.contributions.map(c => c.farmName).join(', ');
}
