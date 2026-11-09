import { Component, computed, effect, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { toDataURL } from 'qrcode';
import { FarmSummary, Lot, StaffApi } from '../staff-api';

interface Label { code: string; title: string; subtitle: string; qr: string; url: string; }

/**
 * Print-ready QR labels on an A4 sheet (3 × 8, 70 × 37 mm — common sticker sheets).
 * Lot labels go on sample pouches and bags; farm labels go on the sign at the field.
 * QR codes point to PublicBaseUrl/q/CODE, so the same label keeps working if a lot is re-published.
 */
@Component({
  selector: 'bo-labels',
  imports: [FormsModule, RouterLink],
  template: `
    <div class="no-print">
      <h1>QR labels</h1>
      <p class="muted">Prints on A4 sticker sheets with 3 × 8 labels (70 × 37 mm). In the print dialog choose A4, scale 100%, no margins/headers.</p>
      <div class="controls card">
        <div class="field"><label for="lb-kind">Label for</label>
          <select id="lb-kind" name="kind" [ngModel]="kind()" (ngModelChange)="kind.set($event); code.set('')"><option value="lot">Lot (sample pouches, bags)</option><option value="farm">Farm (sign at the field)</option></select></div>
        <div class="field"><label for="lb-code">{{ kind() === 'lot' ? 'Lot' : 'Farm' }}</label>
          <select id="lb-code" name="code" [ngModel]="code()" (ngModelChange)="code.set($event)">
            <option value="" disabled>Choose…</option>
            @if (kind() === 'lot') { @for (l of lots(); track l.id) { <option [value]="l.code">{{ l.code }} · {{ l.product }} {{ l.grade }}</option> } }
            @else { @for (f of farms(); track f.id) { <option [value]="f.code">{{ f.code }} · {{ f.name }}</option> } }
          </select></div>
        <div class="field"><label for="lb-n">How many</label><input id="lb-n" name="n" type="number" min="1" max="96" [ngModel]="count()" (ngModelChange)="count.set(+$event)"></div>
        <button class="btn btn-primary" (click)="print()" [disabled]="!labels().length">Print</button>
      </div>
      @if (base() && base()!.startsWith('http://localhost')) {
        <p class="form-error">These QR codes point to {{ base() }}, which only works on this computer. For labels people will scan, set PUBLIC_BASE_URL to the real or tunnel address first.</p>
      }
      <p class="muted small">QR target: {{ labels()[0]?.url ?? '—' }} · <a routerLink="/lots">Back to lots</a></p>
    </div>

    <div class="sheet">
      @for (l of labels(); track $index) {
        <div class="label">
          <img class="qr" [src]="l.qr" alt="">
          <div class="text">
            <img class="mark" src="brand/wordmark.svg" alt="FarmerSample.com">
            <p class="title">{{ l.title }}</p>
            <p class="sub">{{ l.subtitle }}</p>
            <p class="code">{{ l.code }}</p>
          </div>
        </div>
      }
    </div>
  `,
  styles: `
    .controls { display: grid; grid-template-columns: 1fr 2fr 120px auto; gap: 1rem; align-items: end; max-width: 900px; }
    .controls .field { margin: 0; } .small { font-size: .85rem; }
    .sheet { display: grid; grid-template-columns: repeat(3, 70mm); grid-auto-rows: 37mm; margin-top: 1.5rem; background: white; width: 210mm; padding: 0; box-shadow: 0 0 0 1px var(--color-border); }
    .label { box-sizing: border-box; padding: 3mm; display: flex; gap: 2.5mm; align-items: center; overflow: hidden; border: 1px dashed #ddd; }
    .qr { width: 29mm; height: 29mm; flex: none; }
    .text { min-width: 0; display: flex; flex-direction: column; gap: .6mm; }
    .mark { width: 34mm; height: auto; }
    .title { margin: 0; font: 700 9pt/1.15 var(--font-body); color: var(--color-primary); }
    .sub { margin: 0; font: 400 7pt/1.2 var(--font-body); color: var(--color-text); }
    .code { margin: 0; font: 700 9pt/1 ui-monospace, monospace; letter-spacing: .03em; }
    @media print {
      @page { size: A4; margin: 0; }
      :host ::ng-deep { }
      .no-print { display: none; }
      .sheet { margin: 0 auto; box-shadow: none; padding-top: 0; }
      .label { border: none; }
    }
  `,
})
export class LabelsPage implements OnInit {
  private api = inject(StaffApi);
  private route = inject(ActivatedRoute);
  protected kind = signal<'lot' | 'farm'>('lot');
  protected code = signal('');
  protected count = signal(24);
  protected lots = signal<Lot[]>([]);
  protected farms = signal<FarmSummary[]>([]);
  protected base = signal<string | null>(null);
  private qr = signal('');

  constructor() {
    // Regenerate the QR image whenever the chosen code (or base URL) changes.
    effect(() => {
      const code = this.code(), base = this.base();
      if (!code || !base) { this.qr.set(''); return; }
      toDataURL(`${base}/q/${code}`, { margin: 0, width: 300, errorCorrectionLevel: 'M', color: { dark: '#0F3D2E' } }).then(d => this.qr.set(d));
    });
  }

  protected labels = computed<Label[]>(() => {
    const code = this.code(); if (!code || !this.qr()) return [];
    const url = `${this.base()}/q/${code}`;
    let title = code, subtitle = '';
    if (this.kind() === 'lot') {
      const l = this.lots().find(x => x.code === code);
      if (l) { title = `${l.product} · ${l.grade}`; subtitle = 'Sealed sample · Sri Lanka'; }
    } else {
      const f = this.farms().find(x => x.code === code);
      if (f) { title = f.name; subtitle = `${f.region}, Sri Lanka`; }
    }
    return Array.from({ length: Math.min(Math.max(this.count(), 1), 96) }, () => ({ code, title, subtitle, url, qr: this.qr() }));
  });

  async ngOnInit() {
    const [settings, lots, farms] = await Promise.all([
      firstValueFrom(this.api.settings()), firstValueFrom(this.api.lots()), firstValueFrom(this.api.farms())]);
    this.base.set(settings.publicBaseUrl);
    this.lots.set(lots.filter(l => l.status !== 'Closed'));
    this.farms.set(farms);
    const p = this.route.snapshot.queryParamMap;
    if (p.get('farm')) { this.kind.set('farm'); this.code.set(p.get('farm')!); }
    else if (p.get('lot')) { this.kind.set('lot'); this.code.set(p.get('lot')!); }
  }

  print() { window.print(); }
}
