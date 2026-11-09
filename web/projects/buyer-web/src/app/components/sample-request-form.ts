import { Component, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { FarmApi } from '../farm-api';
import { COUNTRIES } from '../../../../../shared/api';

@Component({
  selector: 'fs-sample-request-form',
  imports: [FormsModule],
  template: `
    @if (sent()) {
      <p class="form-ok" role="status">Thank you. We received your request and will reply by e-mail.</p>
    } @else {
      <form (ngSubmit)="submit()" #f="ngForm">
        <div class="field"><label for="company">Company</label><input id="company" name="company" [(ngModel)]="model.companyName" required autocomplete="organization"></div>
        <div class="field"><label for="contact">Your name</label><input id="contact" name="contact" [(ngModel)]="model.contactName" required autocomplete="name"></div>
        <div class="field"><label for="email">Work e-mail</label><input id="email" name="email" type="email" [(ngModel)]="model.email" required email autocomplete="email"></div>
        <div class="field"><label for="country">Country</label>
          <select id="country" name="country" [(ngModel)]="model.countryCode" required>
            <option value="" disabled>Choose…</option>
            @for (c of countries; track c[0]) { <option [value]="c[0]">{{ c[1] }}</option> }
          </select>
        </div>
        <div class="field"><label for="message">What are you looking for? <span class="muted">(optional)</span></label>
          <textarea id="message" name="message" [(ngModel)]="model.message" placeholder="Product, grade, volume, destination port…"></textarea></div>
        <!-- Honeypot: hidden from people, filled by bots. -->
        <div class="visually-hidden" aria-hidden="true"><label for="website">Website</label><input id="website" name="website" tabindex="-1" autocomplete="off" [(ngModel)]="model.website"></div>
        @if (error()) { <p class="form-error" role="alert">{{ error() }}</p> }
        <button class="btn btn-primary" type="submit" [disabled]="busy() || f.invalid">{{ busy() ? 'Sending…' : 'Request sample' }}</button>
        <p class="muted privacy">We use your details only to handle this request.</p>
      </form>
    }
  `,
  styles: `.privacy { font-size: .8rem; margin: .75rem 0 0; } button { width: 100%; }`,
})
export class SampleRequestForm {
  private api = inject(FarmApi);
  farmCode = input<string | null>(null);
  protected countries = COUNTRIES;
  protected model = { companyName: '', contactName: '', email: '', countryCode: '', message: '', website: '' };
  protected busy = signal(false);
  protected sent = signal(false);
  protected error = signal<string | null>(null);

  submit() {
    this.busy.set(true);
    this.error.set(null);
    this.api.requestSample({ ...this.model, farmCode: this.farmCode(), message: this.model.message || null, website: this.model.website || null })
      .subscribe({
        next: () => { this.sent.set(true); this.busy.set(false); },
        error: e => {
          this.busy.set(false);
          this.error.set(e.status === 429 ? 'Too many requests. Please wait a minute and try again.' : 'Please check the fields and try again.');
        },
      });
  }
}
