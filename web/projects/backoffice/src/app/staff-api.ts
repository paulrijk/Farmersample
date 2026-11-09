import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { PublicProduct } from '../../../../shared/api';

export interface FarmSummary { id: string; code: string; name: string; farmerName: string; region: string; status: string; hasPublicProfileConsent: boolean; }
export interface Region { id: string; countryCode: string; name: string; }
export interface CreateFarm { farmerPublicName: string; farmerLegalName: string | null; farmerPhone: string | null; farmName: string; regionId: string; hectares: number | null; story: string | null; products: PublicProduct[]; }
export interface Consent { purposes: string[]; language: string; consentTextVersion: string; evidence: string; obtainedBy: string; }
export interface SampleRequest { id: string; farmCode: string | null; companyName: string; contactName: string; email: string; countryCode: string; message: string | null; createdAt: string; handled: boolean; }

@Injectable({ providedIn: 'root' })
export class StaffApi {
  private http = inject(HttpClient);
  private base = '/api/v1/staff';
  farms() { return this.http.get<FarmSummary[]>(`${this.base}/farms`); }
  regions() { return this.http.get<Region[]>(`${this.base}/regions`); }
  createFarm(r: CreateFarm) { return this.http.post<FarmSummary>(`${this.base}/farms`, r); }
  recordConsent(id: string, c: Consent) { return this.http.post(`${this.base}/farms/${id}/consents`, c); }
  publish(id: string) { return this.http.post(`${this.base}/farms/${id}/publish`, {}); }
  unpublish(id: string) { return this.http.post(`${this.base}/farms/${id}/unpublish`, {}); }
  sampleRequests() { return this.http.get<SampleRequest[]>(`${this.base}/sample-requests`); }
}

/** Public site lives on the same origin (gateway); in developer mode it runs on port 4200. */
export const publicSiteUrl = () => location.port === '4201' ? 'http://localhost:4200' : '';
