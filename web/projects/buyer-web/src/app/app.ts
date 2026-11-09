import { Component } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink],
  template: `
    <a class="visually-hidden" href="#main">Skip to content</a>
    <header class="site-header">
      <div class="container bar">
        <a routerLink="/" class="brand" aria-label="FarmerSample.com home">
          <picture>
            <source media="(max-width: 640px)" srcset="favicon.svg">
            <img src="brand/wordmark.svg" alt="FarmerSample.com" width="393" height="40">
          </picture>
        </a>
        <nav aria-label="Main">
          <a routerLink="/" fragment="lots">Lots</a>
          <a routerLink="/" fragment="producers">Producers</a>
          <a routerLink="/" fragment="how-it-works">How it works</a>
          <a routerLink="/request-sample" class="btn btn-primary">Order samples</a>
        </nav>
      </div>
    </header>
    <main id="main"><router-outlet /></main>
    <footer class="site-footer on-dark">
      <div class="container foot">
        <div>
          <img src="brand/wordmark-reversed.svg" alt="FarmerSample.com" width="393" height="40" class="foot-logo">
          <p class="tagline">Verified at origin. Traded worldwide.</p>
          <p class="descriptor">Sri Lanka · Spices · Direct from Farmers</p>
        </div>
        <p class="legal">FarmerSample.com is operated by VSR Partners SL (Spain) with Saj Company (Sri Lanka).</p>
      </div>
    </footer>
  `,
  styles: `
    .site-header { background: var(--color-surface); border-bottom: 1px solid var(--color-border); position: sticky; top: 0; z-index: 10; }
    .bar { display: flex; align-items: center; justify-content: space-between; gap: 1rem; min-height: 64px; }
    .brand { display: flex; } .brand img { height: 40px; width: auto; display: block; }
    @media (max-width: 640px) { .brand img { height: 40px; width: 40px; border-radius: 9px; } }
    .foot-logo { height: 40px; width: auto; display: block; }
    nav { display: flex; align-items: center; gap: 1.25rem; }
    nav a:not(.btn) { color: var(--color-primary); text-decoration: none; font-weight: 600; font-size: .95rem; }
    nav a:not(.btn):hover { text-decoration: underline; }
    @media (max-width: 640px) { nav a:not(.btn) { display: none; } }
    .site-footer { background: var(--color-primary); color: var(--color-background); margin-top: 4rem; padding: 2.5rem 0; }
    .foot { display: flex; flex-wrap: wrap; justify-content: space-between; gap: 1.5rem; align-items: flex-end; }
    .tagline { font-family: var(--font-display); font-size: 1.1rem; margin: .75rem 0 .25rem; }
    .descriptor, .legal { opacity: .8; font-size: .85rem; margin: 0; }
  `,
})
export class App {}
