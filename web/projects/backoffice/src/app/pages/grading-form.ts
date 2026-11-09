import { Component, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { GradingInput } from '../staff-api';

/** Grading entry used for farm batches and composite lot samples. A correction requires a reason. */
@Component({
  selector: 'bo-grading-form',
  imports: [FormsModule],
  template: `
    <form (ngSubmit)="submit()" #f="ngForm">
      <div class="row">
        <div class="field"><label for="g-grade">Grade</label><input id="g-grade" name="grade" [(ngModel)]="m.grade" required placeholder="550 g/L, Alba, C5…"></div>
        <div class="field"><label for="g-density">Density (g/L)</label><input id="g-density" name="density" type="number" min="100" max="1500" [(ngModel)]="m.densityGramsPerLitre"></div>
      </div>
      <div class="row">
        <div class="field"><label for="g-moist">Moisture (%)</label><input id="g-moist" name="moist" type="number" step="0.1" min="0" max="100" [(ngModel)]="m.moisturePercent"></div>
        <div class="field"><label for="g-clean">Cleanliness</label>
          <select id="g-clean" name="clean" [(ngModel)]="m.cleanliness"><option value="">—</option><option>Clean</option><option>Minor foreign matter</option><option>Needs cleaning</option></select></div>
      </div>
      <div class="field"><label for="g-op">Graded by</label><input id="g-op" name="op" [(ngModel)]="m.operator" required></div>
      @if (correction()) {
        <div class="field"><label for="g-reason">Reason for correction</label><input id="g-reason" name="reason" [(ngModel)]="m.correctionReason" required placeholder="e.g. density re-measured"></div>
      }
      @if (error()) { <p class="form-error" role="alert">{{ error() }}</p> }
      <div class="buttons"><button type="button" class="btn btn-ghost" (click)="cancel.emit()">Cancel</button><button class="btn btn-primary" [disabled]="f.invalid">{{ correction() ? 'Save correction' : 'Save grading' }}</button></div>
    </form>
  `,
  styles: `.row { display: grid; grid-template-columns: 1fr 1fr; gap: 1rem; } .buttons { display: flex; justify-content: flex-end; gap: .5rem; }`,
})
export class GradingForm {
  correction = input(false);
  defaultGrade = input('');
  operator = input('');
  error = input<string | null>(null);
  save = output<GradingInput>();
  cancel = output<void>();
  protected m: GradingInput = { grade: '', densityGramsPerLitre: null, moisturePercent: null, cleanliness: '', operator: '', correctionReason: null };

  ngOnInit() { this.m.grade = this.defaultGrade(); this.m.operator = this.operator(); }

  submit() { this.save.emit({ ...this.m, cleanliness: this.m.cleanliness || null }); }
}
