import { Component } from '@angular/core';
import { SampleRequestForm } from '../components/sample-request-form';

@Component({
  selector: 'fs-request-sample',
  imports: [SampleRequestForm],
  template: `
    <div class="container page">
      <h1>Order samples</h1>
      <p class="lead">Paid sample ordering opens in 2027. Until then, tell us what you are looking for and we will send samples from matching producers.</p>
      <div class="card form-card"><fs-sample-request-form /></div>
    </div>
  `,
  styles: `.page { padding-top: 2.5rem; max-width: 640px; } .lead { font-size: 1.1rem; } .form-card { margin-top: 1.5rem; }`,
})
export class RequestSamplePage {}
