import { DecimalPipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { SampleBatch, StaffApi, errorText, volumeWeightedShares } from '../staff-api';

interface Pick { batch: SampleBatch; selected: boolean; volumeKg: number | null; share: number | null; }

@Component({
  selector: 'bo-new-lot',
  imports: [FormsModule, DecimalPipe, RouterLink],
  template: `
    <h1>New lot</h1>
    <p class="muted">Choose a product and grade, then the graded batches that make up the lot and how many kilos each farm commits.
      With more than one farm, the lot is a combined lot: QC mixes a composite sample in proportion to each farm's volume.</p>

    <section class="card">
      <h2>1. Product and grade</h2>
      <div class="row">
        <div class="field"><label for="nl-prod">Product</label>
          <select id="nl-prod" name="prod" [ngModel]="product()" (ngModelChange)="product.set($event); grade.set('')">
            <option value="" disabled>Choose…</option>@for (p of products(); track p) { <option>{{ p }}</option> }</select></div>
        <div class="field"><label for="nl-grade">Grade</label>
          <select id="nl-grade" name="grade" [ngModel]="grade()" (ngModelChange)="grade.set($event)" [disabled]="!product()">
            <option value="" disabled>Choose…</option>@for (g of grades(); track g) { <option>{{ g }}</option> }</select></div>
      </div>
      @if (product() && !grades().length) { <p class="muted">No graded, unused batches of {{ product() }}. <a routerLink="/sample-batches">Grade a batch first.</a></p> }
    </section>

    @if (grade()) {
      <section class="card">
        <h2>2. Farms and volumes</h2>
        <table>
          <thead><tr><th></th><th>Batch</th><th>Farm</th><th>Grading</th><th>Volume committed (kg)</th><th>Composite share</th></tr></thead>
          <tbody>
            @for (p of picks(); track p.batch.id) {
              <tr>
                <td><input type="checkbox" [name]="'s' + p.batch.id" [(ngModel)]="p.selected" (ngModelChange)="touch()" [attr.aria-label]="'Use batch ' + p.batch.code"></td>
                <td><code>{{ p.batch.code }}</code></td>
                <td>{{ p.batch.farmName }}</td>
                <td>{{ p.batch.grading?.grade }}@if (p.batch.grading?.densityGramsPerLitre) { · {{ p.batch.grading?.densityGramsPerLitre }} g/L }</td>
                <td><input type="number" min="1" class="num" [name]="'v' + p.batch.id" [(ngModel)]="p.volumeKg" (ngModelChange)="touch()" [disabled]="!p.selected"></td>
                <td>
                  @if (p.selected && selected().length > 1) {
                    @if (override()) { <input type="number" step="0.1" min="0.1" class="num" [name]="'sh' + p.batch.id" [(ngModel)]="p.share" (ngModelChange)="touch()"> % }
                    @else { {{ weighted()[selected().indexOf(p)] | number: '1.1-1' }}% }
                  }
                </td>
              </tr>
            }
          </tbody>
        </table>
        <p class="total">Total: <strong>{{ totalKg() | number }} kg</strong> from {{ selected().length }} farm(s)</p>
        @if (selected().length > 1) {
          <label class="check"><input type="checkbox" name="ov" [ngModel]="override()" (ngModelChange)="setOverride($event)"> Use different composite shares than volume-weighted</label>
          @if (override()) {
            <div class="field"><label for="nl-reason">Reason</label><input id="nl-reason" name="reason" [(ngModel)]="reason" placeholder="e.g. not enough sample left from one farm"></div>
          }
        }
      </section>

      <section class="card">
        <h2>3. Offer</h2>
        <div class="row3">
          <div class="field"><label for="nl-min">Price from (per kg)</label><input id="nl-min" name="min" type="number" step="0.01" min="0" [(ngModel)]="priceMin"></div>
          <div class="field"><label for="nl-max">Price to (per kg)</label><input id="nl-max" name="max" type="number" step="0.01" min="0" [(ngModel)]="priceMax"></div>
          <div class="field"><label for="nl-cur">Currency</label><select id="nl-cur" name="cur" [(ngModel)]="currency"><option>EUR</option><option>USD</option></select></div>
        </div>
        <div class="row">
          <div class="field"><label for="nl-inc">Delivery terms</label><select id="nl-inc" name="inc" [(ngModel)]="incoterm"><option>FOB Colombo</option><option>CIF Rotterdam</option><option>CIF Barcelona</option><option>CIF Hamburg</option></select></div>
          <div class="field"><label for="nl-valid">Valid until</label><input id="nl-valid" name="valid" type="date" [(ngModel)]="validUntil"></div>
        </div>
        @if (error()) { <p class="form-error" role="alert">{{ error() }}</p> }
        <button class="btn btn-primary" (click)="save()" [disabled]="!selected().length || busy()">Create lot (as draft)</button>
      </section>
    }
  `,
  styles: `
    section { margin-bottom: 1.25rem; max-width: 900px; } h2 { font-size: 1.2rem; }
    .row { display: grid; grid-template-columns: 1fr 1fr; gap: 1rem; } .row3 { display: grid; grid-template-columns: 1fr 1fr 1fr; gap: 1rem; }
    table { width: 100%; border-collapse: collapse; font-size: .92rem; }
    th, td { text-align: left; padding: .55rem .6rem; border-bottom: 1px solid var(--color-border); vertical-align: middle; }
    th { font-size: .78rem; text-transform: uppercase; letter-spacing: .04em; }
    td input[type=checkbox], .check input { width: auto; } .num { width: 7rem; }
    .check { display: flex; gap: .5rem; align-items: center; font-weight: 400; margin: .75rem 0; }
    .total { margin: .75rem 0 0; }
  `,
})
export class NewLotPage {
  private api = inject(StaffApi);
  private router = inject(Router);
  private batches = toSignal(this.api.batches(), { initialValue: [] as SampleBatch[] });
  private version = signal(0);   // bumps when a pick changes, so computed values refresh
  protected product = signal('');
  protected grade = signal('');
  protected override = signal(false);
  protected busy = signal(false);
  protected error = signal<string | null>(null);
  protected reason = '';
  protected priceMin: number | null = null; protected priceMax: number | null = null;
  protected currency = 'EUR'; protected incoterm = 'FOB Colombo';
  protected validUntil = new Date(Date.now() + 45 * 86400000).toISOString().slice(0, 10);

  private eligible = computed(() => this.batches().filter(b => b.grading && !b.activeLotCode));
  protected products = computed(() => [...new Set(this.eligible().map(b => b.product))].sort());
  protected grades = computed(() => [...new Set(this.eligible().filter(b => b.product === this.product()).map(b => b.grading!.grade))].sort());
  protected picks = computed<Pick[]>(() => this.eligible()
    .filter(b => b.product === this.product() && b.grading!.grade === this.grade())
    .map(batch => ({ batch, selected: false, volumeKg: null, share: null })));
  protected selected = computed(() => { this.version(); return this.picks().filter(p => p.selected); });
  protected weighted = computed(() => { this.version(); return volumeWeightedShares(this.selected().map(p => p.volumeKg ?? 0)); });
  protected totalKg = computed(() => { this.version(); return this.selected().reduce((a, p) => a + (p.volumeKg ?? 0), 0); });

  touch() { this.version.update(v => v + 1); }
  setOverride(on: boolean) {
    this.override.set(on);
    if (on) this.selected().forEach((p, i) => p.share = this.weighted()[i]);
    this.touch();
  }

  async save() {
    this.error.set(null); this.busy.set(true);
    try {
      const lot = await firstValueFrom(this.api.createLot({
        product: this.product(), grade: this.grade(),
        contributions: this.selected().map(p => ({ sampleBatchId: p.batch.id, volumeKg: p.volumeKg ?? 0, compositeSharePercent: this.override() ? p.share : null })),
        shareOverrideReason: this.override() ? this.reason : null,
        priceMinPerKg: this.priceMin ?? 0, priceMaxPerKg: this.priceMax ?? 0, currency: this.currency, incoterm: this.incoterm, validUntil: this.validUntil,
      }));
      await this.router.navigate(['/lots', lot.id]);
    } catch (e) { this.error.set(errorText(e)); }
    finally { this.busy.set(false); }
  }
}
