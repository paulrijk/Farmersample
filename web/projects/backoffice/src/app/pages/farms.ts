import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { FarmSummary, StaffApi, publicSiteUrl } from '../staff-api';

const PURPOSES: [string, string][] = [
  ['PublicProfile', 'Public profile (required to publish)'], ['Photos', 'Photos (signed form needed for photos of people)'],
  ['FamilyStory', 'Family story'], ['PriceShare', "Show the farmer's share of the price"], ['LegalName', 'Show legal / plantation name'],
];

@Component({
  selector: 'bo-farms',
  imports: [RouterLink, FormsModule],
  template: `
    <div class="head"><h1>Farms</h1><a routerLink="/farms/new" class="btn btn-primary">New farm</a></div>
    @if (message()) { <p [class]="messageKind()" role="status">{{ message() }}</p> }
    <div class="table-wrap card">
      <table>
        <thead><tr><th>Farm</th><th>Code</th><th>Farmer</th><th>Region</th><th>Consent</th><th>Status</th><th></th></tr></thead>
        <tbody>
          @for (f of farms(); track f.id) {
            <tr>
              <td><strong>{{ f.name }}</strong></td>
              <td><code>{{ f.code }}</code></td>
              <td>{{ f.farmerName }}</td>
              <td>{{ f.region }}</td>
              <td>@if (f.hasPublicProfileConsent) { <span class="chip">Recorded</span> } @else { <span class="chip chip-cinnamon">Missing</span> }</td>
              <td><span class="chip" [class.chip-gold]="f.status !== 'Published'">{{ f.status }}</span></td>
              <td class="actions">
                <button class="btn btn-ghost sm" (click)="consentFor.set(f)">Record consent</button>
                @if (f.status === 'Published') {
                  <a class="btn btn-ghost sm" [href]="site + '/farms/' + f.code" target="_blank" rel="noopener">View</a>
                  <button class="btn btn-ghost sm" (click)="unpublish(f)">Unpublish</button>
                } @else {
                  <button class="btn btn-primary sm" (click)="publish(f)" [disabled]="!f.hasPublicProfileConsent" [title]="f.hasPublicProfileConsent ? '' : 'Record consent first'">Publish</button>
                }
              </td>
            </tr>
          } @empty { <tr><td colspan="7" class="muted">No farms yet.</td></tr> }
        </tbody>
      </table>
    </div>

    @if (consentFor(); as f) {
      <div class="overlay" (click)="consentFor.set(null)">
        <form class="card dialog" (click)="$event.stopPropagation()" (ngSubmit)="saveConsent(f)" role="dialog" aria-labelledby="consent-title">
          <h2 id="consent-title">Consent — {{ f.farmerName }}</h2>
          <p class="muted">Record what the farmer agreed to, in their language. Keep the signed form.</p>
          <fieldset><legend>Purposes</legend>
            @for (p of purposes; track p[0]) {
              <label class="check"><input type="checkbox" [name]="p[0]" [(ngModel)]="consent.purposes[p[0]]"> {{ p[1] }}</label>
            }
          </fieldset>
          <div class="row">
            <div class="field"><label for="lang">Language</label>
              <select id="lang" name="lang" [(ngModel)]="consent.language"><option value="si">Sinhala</option><option value="ta">Tamil</option><option value="en">English</option></select></div>
            <div class="field"><label for="ver">Consent text version</label><input id="ver" name="ver" [(ngModel)]="consent.version" required></div>
          </div>
          <div class="row">
            <div class="field"><label for="ev">Evidence</label>
              <select id="ev" name="ev" [(ngModel)]="consent.evidence"><option value="SignedFormPhoto">Photo of signed form</option><option value="AgentReadAloud">Read aloud by agent (fallback)</option></select></div>
            <div class="field"><label for="by">Obtained by</label><input id="by" name="by" [(ngModel)]="consent.obtainedBy" required></div>
          </div>
          <div class="buttons"><button type="button" class="btn btn-ghost" (click)="consentFor.set(null)">Cancel</button><button class="btn btn-primary">Save consent</button></div>
        </form>
      </div>
    }
  `,
  styles: `
    .head { display: flex; justify-content: space-between; align-items: center; gap: 1rem; }
    .table-wrap { padding: 0; overflow-x: auto; }
    table { width: 100%; border-collapse: collapse; font-size: .92rem; }
    th, td { text-align: left; padding: .7rem .9rem; border-bottom: 1px solid var(--color-border); vertical-align: middle; }
    th { background: #EFE9D8; font-size: .8rem; text-transform: uppercase; letter-spacing: .04em; }
    .actions { display: flex; gap: .4rem; justify-content: flex-end; flex-wrap: wrap; }
    .sm { padding: .4rem .7rem; font-size: .85rem; }
    .overlay { position: fixed; inset: 0; background: rgba(20,37,30,.5); display: grid; place-items: center; padding: 1rem; z-index: 20; }
    .dialog { width: min(560px, 100%); max-height: 90vh; overflow: auto; }
    fieldset { border: 1px solid var(--color-border); border-radius: var(--radius-button); margin: 0 0 1rem; }
    .check { display: flex; gap: .5rem; align-items: center; font-weight: 400; } .check input { width: auto; }
    .row { display: grid; grid-template-columns: 1fr 1fr; gap: 1rem; }
    .buttons { display: flex; justify-content: flex-end; gap: .5rem; }
  `,
})
export class FarmsPage {
  private api = inject(StaffApi);
  protected farms = signal<FarmSummary[]>([]);
  protected consentFor = signal<FarmSummary | null>(null);
  protected message = signal<string | null>(null);
  protected messageKind = signal('form-ok');
  protected purposes = PURPOSES;
  protected site = publicSiteUrl();
  protected consent = this.blankConsent();

  constructor() { this.load(); }

  private blankConsent() {
    return { purposes: { PublicProfile: true, Photos: false, FamilyStory: false, PriceShare: false, LegalName: false } as Record<string, boolean>,
      language: 'si', version: '2026-11', evidence: 'SignedFormPhoto', obtainedBy: '' };
  }

  private async load() { this.farms.set(await firstValueFrom(this.api.farms())); }

  private say(text: string, ok = true) { this.message.set(text); this.messageKind.set(ok ? 'form-ok' : 'form-error'); }

  async publish(f: FarmSummary) {
    try { await firstValueFrom(this.api.publish(f.id)); this.say(`${f.name} is now public.`); }
    catch (e: any) { this.say(e?.error?.detail ?? 'Could not publish.', false); }
    await this.load();
  }

  async unpublish(f: FarmSummary) {
    await firstValueFrom(this.api.unpublish(f.id)); this.say(`${f.name} is no longer public (visible within a minute).`); await this.load();
  }

  async saveConsent(f: FarmSummary) {
    const c = this.consent;
    await firstValueFrom(this.api.recordConsent(f.id, {
      purposes: Object.entries(c.purposes).filter(([, on]) => on).map(([k]) => k),
      language: c.language, consentTextVersion: c.version, evidence: c.evidence, obtainedBy: c.obtainedBy,
    }));
    this.consentFor.set(null); this.consent = this.blankConsent();
    this.say(`Consent recorded for ${f.farmerName}.`); await this.load();
  }
}
