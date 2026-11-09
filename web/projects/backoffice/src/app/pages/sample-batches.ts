import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { Auth } from '../auth';
import { GradingForm } from './grading-form';
import { FarmSummary, GradingInput, PRODUCTS, SampleBatch, StaffApi, errorText } from '../staff-api';

@Component({
  selector: 'bo-sample-batches',
  imports: [FormsModule, DatePipe, DecimalPipe, GradingForm],
  template: `
    <div class="head"><h1>Sample batches</h1><button class="btn btn-primary" (click)="showNew.set(true)">New batch</button></div>
    <p class="muted">One batch = the sample taken from one farm at one visit, for one product. Grade it at the hub before it can go into a lot.</p>
    @if (message()) { <p class="form-ok" role="status">{{ message() }}</p> }

    <div class="card table-wrap">
      <table>
        <thead><tr><th>Batch</th><th>Farm</th><th>Product</th><th>Weight</th><th>Collected</th><th>Grading</th><th>Lot</th><th></th></tr></thead>
        <tbody>
          @for (b of batches(); track b.id) {
            <tr>
              <td><code>{{ b.code }}</code></td>
              <td>{{ b.farmName }}</td>
              <td>{{ b.product }}</td>
              <td>{{ b.weightGrams | number }} g</td>
              <td>{{ b.collectedOn | date: 'd MMM' }} · {{ b.collectedBy }}</td>
              <td>
                @if (b.grading; as g) {
                  <strong>{{ g.grade }}</strong>
                  <span class="muted small">@if (g.densityGramsPerLitre) { · {{ g.densityGramsPerLitre }} g/L }@if (g.moisturePercent !== null) { · {{ g.moisturePercent }}% moisture }@if (g.version > 1) { · v{{ g.version }} }</span>
                } @else { <span class="chip chip-gold">Not graded</span> }
              </td>
              <td>@if (b.activeLotCode) { <code>{{ b.activeLotCode }}</code> }</td>
              <td class="actions">
                @if (!b.activeLotCode) { <button class="btn btn-ghost sm" (click)="gradeFor.set(b)">{{ b.grading ? 'Correct grading' : 'Grade' }}</button> }
              </td>
            </tr>
          } @empty { <tr><td colspan="8" class="muted">No sample batches yet.</td></tr> }
        </tbody>
      </table>
    </div>

    @if (showNew()) {
      <div class="overlay" (click)="showNew.set(false)">
        <form class="card dialog" (click)="$event.stopPropagation()" (ngSubmit)="create()" #f="ngForm" role="dialog" aria-labelledby="nb-title">
          <h2 id="nb-title">New sample batch</h2>
          <div class="field"><label for="nb-farm">Farm</label>
            <select id="nb-farm" name="farm" [(ngModel)]="n.farmId" required><option value="" disabled>Choose…</option>
              @for (f of farms(); track f.id) { <option [value]="f.id">{{ f.name }} ({{ f.region }})</option> }</select></div>
          <div class="row">
            <div class="field"><label for="nb-prod">Product</label><select id="nb-prod" name="prod" [(ngModel)]="n.product" required>@for (p of products; track p) { <option>{{ p }}</option> }</select></div>
            <div class="field"><label for="nb-w">Weight (g)</label><input id="nb-w" name="w" type="number" min="1" [(ngModel)]="n.weightGrams" required></div>
          </div>
          <div class="row">
            <div class="field"><label for="nb-d">Collected on</label><input id="nb-d" name="d" type="date" [(ngModel)]="n.collectedOn" required></div>
            <div class="field"><label for="nb-by">Collected by</label><input id="nb-by" name="by" [(ngModel)]="n.collectedBy" required></div>
          </div>
          <div class="field"><label for="nb-notes">Notes <span class="muted">(optional)</span></label><input id="nb-notes" name="notes" [(ngModel)]="n.notes"></div>
          @if (error()) { <p class="form-error" role="alert">{{ error() }}</p> }
          <div class="buttons"><button type="button" class="btn btn-ghost" (click)="showNew.set(false)">Cancel</button><button class="btn btn-primary" [disabled]="f.invalid">Save batch</button></div>
        </form>
      </div>
    }

    @if (gradeFor(); as b) {
      <div class="overlay" (click)="gradeFor.set(null)">
        <div class="card dialog" (click)="$event.stopPropagation()" role="dialog" aria-labelledby="gr-title">
          <h2 id="gr-title">{{ b.grading ? 'Correct grading' : 'Grade' }} — {{ b.code }}</h2>
          <p class="muted">{{ b.farmName }} · {{ b.product }}</p>
          <bo-grading-form [correction]="!!b.grading" [defaultGrade]="b.grading?.grade ?? ''" [operator]="me()" [error]="error()" (save)="grade(b, $event)" (cancel)="gradeFor.set(null)" />
        </div>
      </div>
    }
  `,
  styles: `
    .head { display: flex; justify-content: space-between; align-items: center; gap: 1rem; }
    .table-wrap { padding: 0; overflow-x: auto; }
    table { width: 100%; border-collapse: collapse; font-size: .92rem; }
    th, td { text-align: left; padding: .7rem .9rem; border-bottom: 1px solid var(--color-border); vertical-align: middle; }
    th { background: #EFE9D8; font-size: .8rem; text-transform: uppercase; letter-spacing: .04em; }
    .small { font-size: .85rem; } .sm { padding: .4rem .7rem; font-size: .85rem; } .actions { text-align: right; }
    .overlay { position: fixed; inset: 0; background: rgba(20,37,30,.5); display: grid; place-items: center; padding: 1rem; z-index: 20; }
    .dialog { width: min(560px, 100%); max-height: 90vh; overflow: auto; }
    .row { display: grid; grid-template-columns: 1fr 1fr; gap: 1rem; } .buttons { display: flex; justify-content: flex-end; gap: .5rem; }
  `,
})
export class SampleBatchesPage {
  private api = inject(StaffApi);
  private auth = inject(Auth);
  protected batches = signal<SampleBatch[]>([]);
  protected farms = signal<FarmSummary[]>([]);
  protected showNew = signal(false);
  protected gradeFor = signal<SampleBatch | null>(null);
  protected error = signal<string | null>(null);
  protected message = signal<string | null>(null);
  protected products = PRODUCTS;
  protected me = () => this.auth.user()?.displayName ?? '';
  protected n = this.blank();

  constructor() { this.load(); firstValueFrom(this.api.farms()).then(f => this.farms.set(f)); }

  private blank() {
    return { farmId: '', product: 'Black pepper', weightGrams: 1500, collectedOn: new Date().toISOString().slice(0, 10), collectedBy: this.auth.user()?.displayName ?? '', notes: '' };
  }
  private async load() { this.batches.set(await firstValueFrom(this.api.batches())); }

  async create() {
    this.error.set(null);
    try {
      const b = await firstValueFrom(this.api.createBatch({ ...this.n, notes: this.n.notes || null }));
      this.showNew.set(false); this.n = this.blank(); this.message.set(`Batch ${b.code} saved. Write this code on the bag.`); await this.load();
    } catch (e) { this.error.set(errorText(e)); }
  }

  async grade(b: SampleBatch, g: GradingInput) {
    this.error.set(null);
    try {
      await firstValueFrom(this.api.gradeBatch(b.id, g));
      this.gradeFor.set(null); this.message.set(`Grading saved for ${b.code}.`); await this.load();
    } catch (e) { this.error.set(errorText(e)); }
  }
}
