import { Component, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { StaffApi } from '../staff-api';
import { MONTHS } from '../../../../../shared/api';

interface ProductRow { product: string; grade: string; months: boolean[]; }

@Component({
  selector: 'bo-new-farm',
  imports: [FormsModule],
  template: `
    <h1>New farm</h1>
    <p class="muted">Works on a phone during a farm visit. The farm is saved as "needs review"; publish it after recording consent.</p>
    <form (ngSubmit)="save()" #f="ngForm" class="form">
      <section class="card">
        <h2>Farmer</h2>
        <div class="field"><label for="pn">Name on public profile</label><input id="pn" name="pn" [(ngModel)]="m.farmerPublicName" required placeholder="e.g. Nimal P."></div>
        <div class="row">
          <div class="field"><label for="ln">Legal name <span class="muted">(internal)</span></label><input id="ln" name="ln" [(ngModel)]="m.farmerLegalName"></div>
          <div class="field"><label for="ph">Phone <span class="muted">(internal, never shown to buyers)</span></label><input id="ph" name="ph" type="tel" [(ngModel)]="m.farmerPhone"></div>
        </div>
      </section>
      <section class="card">
        <h2>Farm</h2>
        <div class="field"><label for="fn">Farm name</label><input id="fn" name="fn" [(ngModel)]="m.farmName" required></div>
        <div class="row">
          <div class="field"><label for="rg">Region</label>
            <select id="rg" name="rg" [(ngModel)]="m.regionId" required>
              <option value="" disabled>Choose…</option>
              @for (r of regions(); track r.id) { <option [value]="r.id">{{ r.name }}</option> }
            </select></div>
          <div class="field"><label for="ha">Hectares</label><input id="ha" name="ha" type="number" min="0" step="0.1" [(ngModel)]="m.hectares"></div>
        </div>
        <div class="field"><label for="st">Farm story <span class="muted">(public only with consent)</span></label><textarea id="st" name="st" [(ngModel)]="m.story"></textarea></div>
      </section>
      <section class="card">
        <h2>Products</h2>
        @for (p of products; track $index; let i = $index) {
          <div class="product">
            <div class="row">
              <div class="field"><label [for]="'pr' + i">Product</label>
                <select [id]="'pr' + i" [name]="'pr' + i" [(ngModel)]="p.product" required>
                  <option>Black pepper</option><option>White pepper</option><option>Ceylon cinnamon</option><option>Cloves</option><option>Nutmeg</option><option>Mace</option><option>Cardamom</option><option>Turmeric</option><option>Ginger</option><option>Coffee</option>
                </select></div>
              <div class="field"><label [for]="'gr' + i">Grade</label><input [id]="'gr' + i" [name]="'gr' + i" [(ngModel)]="p.grade" placeholder="e.g. 550 g/L, Alba, C5"></div>
            </div>
            <p class="lbl">Harvest months</p>
            <div class="months">
              @for (mo of monthNames; track mo; let j = $index) {
                <label class="month"><input type="checkbox" [name]="'m' + i + '-' + j" [(ngModel)]="p.months[j]"> {{ mo }}</label>
              }
            </div>
          </div>
        }
        <button type="button" class="btn btn-ghost" (click)="addProduct()">Add product</button>
      </section>
      @if (error()) { <p class="form-error" role="alert">{{ error() }}</p> }
      <button class="btn btn-primary" [disabled]="f.invalid || busy()">Save farm</button>
    </form>
  `,
  styles: `
    .form { display: grid; gap: 1.25rem; max-width: 760px; } h2 { font-size: 1.2rem; }
    .row { display: grid; grid-template-columns: 1fr 1fr; gap: 1rem; } @media (max-width: 600px) { .row { grid-template-columns: 1fr; } }
    .product { border-bottom: 1px dashed var(--color-border); padding-bottom: 1rem; margin-bottom: 1rem; }
    .lbl { font-weight: 600; font-size: .9rem; margin: 0 0 .35rem; }
    .months { display: flex; flex-wrap: wrap; gap: .35rem; }
    .month { display: inline-flex; align-items: center; gap: .3rem; font-weight: 400; border: 1px solid var(--color-border); border-radius: 999px; padding: .25rem .6rem; margin: 0; background: var(--color-surface); }
    .month input { width: auto; } button.btn-primary { justify-self: start; }
  `,
})
export class NewFarmPage {
  private api = inject(StaffApi);
  private router = inject(Router);
  protected regions = toSignal(this.api.regions(), { initialValue: [] });
  protected monthNames = MONTHS;
  protected busy = signal(false);
  protected error = signal<string | null>(null);
  protected m = { farmerPublicName: '', farmerLegalName: '', farmerPhone: '', farmName: '', regionId: '', hectares: null as number | null, story: '' };
  protected products: ProductRow[] = [this.blank()];

  private blank(): ProductRow { return { product: 'Black pepper', grade: '', months: Array(12).fill(false) }; }
  addProduct() { this.products.push(this.blank()); }

  async save() {
    this.busy.set(true); this.error.set(null);
    try {
      await firstValueFrom(this.api.createFarm({
        ...this.m,
        farmerLegalName: this.m.farmerLegalName || null, farmerPhone: this.m.farmerPhone || null, story: this.m.story || null,
        products: this.products.map(p => ({ product: p.product, grade: p.grade || null, harvestMonths: p.months.flatMap((on, i) => on ? [i + 1] : []) })),
      }));
      await this.router.navigate(['/farms']);
    } catch { this.error.set('The farm could not be saved. Check the required fields.'); }
    finally { this.busy.set(false); }
  }
}
