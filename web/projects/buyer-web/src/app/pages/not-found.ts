import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'fs-not-found',
  imports: [RouterLink],
  template: `<div class="container" style="padding-top:3rem"><h1>Page not found</h1><a routerLink="/" class="btn btn-primary">Go to the homepage</a></div>`,
})
export class NotFoundPage {}
