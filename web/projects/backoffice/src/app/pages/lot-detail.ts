import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, inject, input, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { Auth } from '../auth';
import { GradingForm } from './grading-form';
import { GradingInput, Lot, StaffApi, errorText, publicSiteUrl } from '../staff-api';

@Component({
  selector: 'bo-lot-detail',
  imports: [DatePipe, DecimalPipe, RouterLink, GradingForm],
  template: `
    @if (lot(); as l) {
      <p class="muted"><a routerLink="/lots">Lots</a> / {{ l.code }}</p>
      <div class="head">
        <h1>{{ l.product }} · {{ l.grade }}</h1>
        <div class="actions">
          <a class="btn btn-ghost" [routerLink]="['/lots', l.id, 'photos']">Photos</a>
          <a class="btn btn-ghost" [routerLink]="['/labels']" [queryParams]="{ lot: l.code }">Print labels</a>
          @if (l.status === 'Published') { <a class="btn btn-ghost" [href]="site + '/lots/' + l.code" target="_blank" rel="noopener">View public page</a> }
          @if (l.status !== 'Closed') { <button class="btn btn-ghost" (click)="withdraw(l)">Withdraw</button> }
          @if (l.status === 'Draft') { <button class="btn btn-primary" (click)="publish(l)" [disabled]="l.publishBlockers.length > 0">Publish</button> }
        </div>
      </div>
      <p><code>{{ l.code }}</code> · <span class="chip" [class.chip-gold]="l.status === 'Draft'" [class.chip-cinnamon]="l.status === 'Closed'">{{ l.status }}@if (l.closedReason) { · {{ l.closedReason }} }</span></p>
      @if (message()) { <p class="form-ok" role="status">{{ message() }}</p> }
      @if (error()) { <p class="form-error" role="alert">{{ error() }}</p> }

      @if (l.status === 'Draft' && l.publishBlockers.length) {
        <div class="card todo"><h2>Before you can publish</h2><ul>@for (b of l.publishBlockers; track b) { <li>{{ b }}</li> }</ul></div>
      }

      <div class="grid">
        <section class="card">
          <h2>Offer</h2>
          <dl class="facts">
            <dt>Volume</dt><dd>{{ l.committedVolumeKg | number }} kg</dd>
            <dt>Price</dt><dd>{{ l.currency }} {{ l.priceMinPerKg | number: '1.2-2' }}–{{ l.priceMaxPerKg | number: '1.2-2' }} per kg</dd>
            <dt>Terms</dt><dd>{{ l.incoterm }}</dd>
            <dt>Valid until</dt><dd>{{ l.validUntil | date: 'd MMM y' }}</dd>
          </dl>
        </section>
        <section class="card">
          <h2>{{ l.sampleKind === 'Composite' ? 'Composite sample' : 'Sample' }}</h2>
          @if (l.lotGrading; as g) {
            <dl class="facts">
              <dt>Grade</dt><dd>{{ g.grade }}</dd>
              @if (g.densityGramsPerLitre) { <dt>Density</dt><dd>{{ g.densityGramsPerLitre }} g/L</dd> }
              @if (g.moisturePercent !== null) { <dt>Moisture</dt><dd>{{ g.moisturePercent }}%</dd> }
              <dt>Graded</dt><dd>{{ g.gradedAt | date: 'd MMM y' }} by {{ g.operator }}@if (g.version > 1) { (v{{ g.version }}: {{ g.correctionReason }}) }</dd>
            </dl>
          } @else if (l.sampleKind === 'Composite') {
            <p>Mix the composite from the farm batches using the shares below, then grade it.</p>
          }
          @if (l.sampleKind === 'Composite' && l.status === 'Draft') {
            @if (grading()) {
              <bo-grading-form [correction]="!!l.lotGrading" [defaultGrade]="l.grade" [operator]="me()" (save)="gradeComposite(l, $event)" (cancel)="grading.set(false)" />
            } @else { <button class="btn btn-ghost" (click)="grading.set(true)">{{ l.lotGrading ? 'Correct composite grading' : 'Grade composite sample' }}</button> }
          }
        </section>
      </div>

      <section class="card">
        <h2>Farms</h2>
        <table>
          <thead><tr><th>Farm</th><th>Batch</th><th>Batch grade</th><th>Volume</th><th>Share of composite</th></tr></thead>
          <tbody>
            @for (c of l.contributions; track c.sampleBatchId) {
              <tr><td>{{ c.farmName }}</td><td><code>{{ c.batchCode }}</code></td><td>{{ c.batchGrade }}</td><td>{{ c.volumeKg | number }} kg</td><td>{{ c.compositeSharePercent | number: '1.1-1' }}%</td></tr>
            }
          </tbody>
        </table>
        @if (l.shareOverrideReason) { <p class="muted">Shares set by QC: {{ l.shareOverrideReason }}</p> }
      </section>
    }
  `,
  styles: `
    .head { display: flex; justify-content: space-between; align-items: center; gap: 1rem; flex-wrap: wrap; } .head h1 { margin: 0; }
    .actions { display: flex; gap: .5rem; flex-wrap: wrap; }
    .grid { display: grid; grid-template-columns: 1fr 1fr; gap: 1.25rem; margin-bottom: 1.25rem; } @media (max-width: 900px) { .grid { grid-template-columns: 1fr; } }
    h2 { font-size: 1.2rem; } .todo { border-left: 4px solid var(--color-accent); margin-bottom: 1.25rem; } .todo ul { margin: 0; }
    .facts { display: grid; grid-template-columns: max-content 1fr; gap: .4rem 1.25rem; margin: 0 0 1rem; } .facts dt { font-weight: 600; } .facts dd { margin: 0; }
    table { width: 100%; border-collapse: collapse; font-size: .92rem; }
    th, td { text-align: left; padding: .55rem .6rem; border-bottom: 1px solid var(--color-border); }
    th { font-size: .78rem; text-transform: uppercase; letter-spacing: .04em; }
  `,
})
export class LotDetailPage implements OnInit {
  private api = inject(StaffApi);
  private auth = inject(Auth);
  id = input.required<string>();
  protected lot = signal<Lot | null>(null);
  protected grading = signal(false);
  protected error = signal<string | null>(null);
  protected message = signal<string | null>(null);
  protected site = publicSiteUrl();
  protected me = () => this.auth.user()?.displayName ?? '';

  async ngOnInit() { this.lot.set(await firstValueFrom(this.api.lot(this.id()))); }

  private async run(action: () => Promise<Lot>, done: string) {
    this.error.set(null); this.message.set(null);
    try { this.lot.set(await action()); this.message.set(done); this.grading.set(false); }
    catch (e) { this.error.set(errorText(e)); }
  }
  publish(l: Lot) { return this.run(() => firstValueFrom(this.api.publishLot(l.id)), 'Published. Buyers can see the lot within a minute.'); }
  withdraw(l: Lot) {
    if (!confirm(`Withdraw lot ${l.code}? It disappears from the site and its batches can be used in a new lot.`)) return;
    return this.run(() => firstValueFrom(this.api.withdrawLot(l.id)), 'Lot withdrawn.');
  }
  gradeComposite(l: Lot, g: GradingInput) { return this.run(() => firstValueFrom(this.api.gradeComposite(l.id, g)), 'Composite grading saved.'); }
}
