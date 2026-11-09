import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { PublicFarm, PublicLot, SampleRequestInput } from '../../../../shared/api';

@Injectable({ providedIn: 'root' })
export class FarmApi {
  private http = inject(HttpClient);
  list() { return this.http.get<PublicFarm[]>('/api/public/farms'); }
  get(code: string) { return this.http.get<PublicFarm>(`/api/public/farms/${encodeURIComponent(code)}`); }
  lots() { return this.http.get<PublicLot[]>('/api/public/lots'); }
  lot(code: string) { return this.http.get<PublicLot>(`/api/public/lots/${encodeURIComponent(code)}`); }
  resolve(code: string) { return this.http.get<{ kind: 'farm' | 'lot'; code: string }>(`/api/public/q/${encodeURIComponent(code)}`); }
  requestSample(input: SampleRequestInput) { return this.http.post('/api/public/sample-requests', input); }
}
