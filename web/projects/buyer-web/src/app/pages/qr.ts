import { Component, inject, input, OnInit, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FarmApi } from '../farm-api';

/** Landing page for QR codes: forwards to the lot or farm page without leaving a history entry. */
@Component({
  selector: 'fs-qr',
  imports: [RouterLink],
  template: `
    <div class="container" style="padding-top:3rem">
      @if (missing()) {
        <h1>Code not found</h1>
        <p>This code does not match a published lot or producer. Check the code on the label, or browse all lots.</p>
        <a routerLink="/" class="btn btn-primary">Go to the homepage</a>
      } @else { <p class="muted">Opening…</p> }
    </div>
  `,
})
export class QrPage implements OnInit {
  private api = inject(FarmApi);
  private router = inject(Router);
  code = input.required<string>();
  protected missing = signal(false);

  ngOnInit() {
    this.api.resolve(this.code()).subscribe({
      next: r => this.router.navigate([r.kind === 'lot' ? '/lots' : '/farms', r.code], { replaceUrl: true }),
      error: () => this.missing.set(true),
    });
  }
}
