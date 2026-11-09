import { DatePipe } from '@angular/common';
import { Component, inject, input, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { FarmPhoto, FarmSummary, StaffApi, errorText } from '../staff-api';

@Component({
  selector: 'bo-farm-photos',
  imports: [FormsModule, RouterLink, DatePipe],
  template: `
    <p class="muted"><a routerLink="/farms">Farms</a> / {{ farm()?.name }}</p>
    <h1>Photos — {{ farm()?.name }}</h1>
    <p class="muted">Buyers only see a photo after it is reviewed, and only if the farmer gave photo consent with a signed form.
      They get a re-encoded copy without location or camera data; the original stays private.</p>

    <form class="card upload" (ngSubmit)="upload()">
      <div class="field"><label for="ph-file">Photo (JPEG, PNG or WebP, max 15 MB)</label>
        <input id="ph-file" name="file" type="file" accept="image/jpeg,image/png,image/webp" capture="environment" (change)="pick($event)"></div>
      <div class="field"><label for="ph-cap">Caption <span class="muted">(shown to buyers)</span></label><input id="ph-cap" name="cap" [(ngModel)]="caption" maxlength="300" placeholder="e.g. Drying pepper on the estate"></div>
      <label class="check"><input type="checkbox" name="people" [(ngModel)]="showsPeople"> Recognisable people are in this photo</label>
      @if (uploadError()) { <p class="form-error" role="alert">{{ uploadError() }}</p> }
      <button class="btn btn-primary" [disabled]="!file || busy()">{{ busy() ? 'Uploading…' : 'Upload' }}</button>
    </form>

    <div class="grid">
      @for (p of photos(); track p.id) {
        <article class="card photo">
          <img [src]="p.imageUrl" [alt]="p.caption ?? 'Farm photo'" loading="lazy">
          <div class="meta">
            <div class="chips">
              <span class="chip" [class.chip-gold]="p.status === 'PendingReview'" [class.chip-cinnamon]="p.status === 'Rejected'">{{ statusText(p.status) }}</span>
              @if (p.showsPeople) { <span class="chip chip-teal">People</span> }
              @if (p.visibleToBuyers) { <span class="chip">Visible to buyers</span> }
            </div>
            @if (p.caption) { <p>{{ p.caption }}</p> }
            @if (!p.visibleToBuyers && p.notVisibleBecause) { <p class="muted small">Not visible: {{ p.notVisibleBecause }}</p> }
            <p class="muted small">Uploaded {{ p.uploadedAt | date: 'd MMM y' }} by {{ p.uploadedBy }}@if (p.reviewedBy) { · reviewed by {{ p.reviewedBy }} }@if (p.reviewNote) { · "{{ p.reviewNote }}" }</p>
            @if (reviewing() === p.id) {
              <div class="review">
                <label class="check"><input type="checkbox" name="chk" [(ngModel)]="checked"> I checked: no names, signboards, number plates or phone numbers are readable</label>
                <div class="field"><label [for]="'note' + p.id">Note <span class="muted">(required when rejecting)</span></label><input [id]="'note' + p.id" [name]="'note' + p.id" [(ngModel)]="note"></div>
                @if (reviewError()) { <p class="form-error" role="alert">{{ reviewError() }}</p> }
                <div class="buttons">
                  <button class="btn btn-ghost sm" (click)="review(p, false)">Reject</button>
                  <button class="btn btn-primary sm" (click)="review(p, true)" [disabled]="!checked">Mark buyer-safe</button>
                </div>
              </div>
            } @else { <button class="btn btn-ghost sm" (click)="startReview(p)">{{ p.status === 'PendingReview' ? 'Review' : 'Review again' }}</button> }
          </div>
        </article>
      } @empty { <p class="muted">No photos yet.</p> }
    </div>
  `,
  styles: `
    .upload { max-width: 640px; margin-bottom: 1.5rem; } .check { display: flex; gap: .5rem; align-items: flex-start; font-weight: 400; margin-bottom: 1rem; } .check input { width: auto; margin-top: .25rem; }
    .grid { display: grid; gap: 1.25rem; grid-template-columns: repeat(auto-fill, minmax(280px, 1fr)); }
    .photo { padding: 0; overflow: hidden; } .photo img { width: 100%; aspect-ratio: 4/3; object-fit: cover; display: block; background: #eee; }
    .meta { padding: 1rem; } .meta p { margin: .4rem 0 0; } .chips { display: flex; gap: .35rem; flex-wrap: wrap; }
    .small { font-size: .82rem; } .sm { padding: .4rem .7rem; font-size: .85rem; margin-top: .6rem; }
    .review { margin-top: .75rem; border-top: 1px solid var(--color-border); padding-top: .75rem; } .buttons { display: flex; gap: .5rem; justify-content: flex-end; }
  `,
})
export class FarmPhotosPage implements OnInit {
  private api = inject(StaffApi);
  id = input.required<string>();
  protected farm = signal<FarmSummary | null>(null);
  protected photos = signal<FarmPhoto[]>([]);
  protected busy = signal(false);
  protected uploadError = signal<string | null>(null);
  protected reviewing = signal<string | null>(null);
  protected reviewError = signal<string | null>(null);
  protected file: File | null = null;
  protected caption = ''; protected showsPeople = false; protected checked = false; protected note = '';

  async ngOnInit() {
    const farms = await firstValueFrom(this.api.farms());
    this.farm.set(farms.find(f => f.id === this.id()) ?? null);
    await this.load();
  }
  private async load() { this.photos.set(await firstValueFrom(this.api.photos(this.id()))); }
  protected statusText = (s: string) => ({ PendingReview: 'Needs review', BuyerSafe: 'Buyer-safe', Rejected: 'Rejected' } as Record<string, string>)[s] ?? s;

  pick(e: Event) { this.file = (e.target as HTMLInputElement).files?.[0] ?? null; }

  async upload() {
    if (!this.file) return;
    this.busy.set(true); this.uploadError.set(null);
    try {
      await firstValueFrom(this.api.uploadPhoto(this.id(), this.file, this.caption, this.showsPeople));
      this.caption = ''; this.showsPeople = false; this.file = null;
      (document.getElementById('ph-file') as HTMLInputElement).value = '';
      await this.load();
    } catch (e) { this.uploadError.set(errorText(e)); }
    finally { this.busy.set(false); }
  }

  startReview(p: FarmPhoto) { this.reviewing.set(p.id); this.checked = false; this.note = ''; this.reviewError.set(null); }

  async review(p: FarmPhoto, buyerSafe: boolean) {
    this.reviewError.set(null);
    try { await firstValueFrom(this.api.reviewPhoto(p.id, buyerSafe, this.checked, this.note || null)); this.reviewing.set(null); await this.load(); }
    catch (e) { this.reviewError.set(errorText(e)); }
  }
}
