import { DatePipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { StaffApi, publicSiteUrl } from '../staff-api';

@Component({
  selector: 'bo-sample-requests',
  imports: [DatePipe],
  template: `
    <h1>Sample requests</h1>
    <p class="muted">Requests from the public site. Each one also goes to the team inbox.</p>
    <div class="list">
      @for (r of requests(); track r.id) {
        <article class="card">
          <div class="top"><strong>{{ r.companyName }}</strong> <span class="chip chip-teal">{{ r.countryCode }}</span><span class="muted when">{{ r.createdAt | date: 'd MMM y, HH:mm' }}</span></div>
          <p>{{ r.contactName }} · <a [href]="'mailto:' + r.email">{{ r.email }}</a></p>
          @if (r.lotCode) { <p>Lot: <a [href]="site + '/lots/' + r.lotCode" target="_blank" rel="noopener"><code>{{ r.lotCode }}</code></a></p> }
          @if (r.farmCode) { <p>Farm: <a [href]="site + '/farms/' + r.farmCode" target="_blank" rel="noopener"><code>{{ r.farmCode }}</code></a></p> }
          @if (r.message) { <p class="msg">{{ r.message }}</p> }
        </article>
      } @empty { <p class="muted">No requests yet.</p> }
    </div>
  `,
  styles: `.list { display: grid; gap: 1rem; max-width: 760px; } .top { display: flex; gap: .5rem; align-items: center; flex-wrap: wrap; } .when { margin-left: auto; font-size: .85rem; } p { margin: .4rem 0 0; } .msg { white-space: pre-wrap; background: var(--color-background); padding: .6rem .8rem; border-radius: var(--radius-button); }`,
})
export class SampleRequestsPage {
  private api = inject(StaffApi);
  protected requests = toSignal(this.api.sampleRequests(), { initialValue: [] });
  protected site = publicSiteUrl();
}
