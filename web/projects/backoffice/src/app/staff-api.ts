import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { PublicProduct } from '../../../../shared/api';

export interface FarmSummary { id: string; code: string; name: string; farmerName: string; region: string; status: string; hasPublicProfileConsent: boolean; }
export interface Region { id: string; countryCode: string; name: string; }
export interface CreateFarm { farmerPublicName: string; farmerLegalName: string | null; farmerPhone: string | null; farmName: string; regionId: string; hectares: number | null; story: string | null; products: PublicProduct[]; }
export interface Consent { purposes: string[]; language: string; consentTextVersion: string; evidence: string; obtainedBy: string; }
export interface Grading { id: string; moisturePercent: number | null; densityGramsPerLitre: number | null; cleanliness: string | null; grade: string; operator: string; gradedAt: string; version: number; correctionReason: string | null; }
export interface GradingInput { moisturePercent: number | null; densityGramsPerLitre: number | null; cleanliness: string | null; grade: string; operator: string; correctionReason: string | null; }
export interface SampleBatch { id: string; code: string; farmId: string; farmName: string; farmCode: string; product: string; weightGrams: number; collectedOn: string; collectedBy: string; notes: string | null; status: string; grading: Grading | null; activeLotCode: string | null; }
export interface CreateBatch { farmId: string; product: string; weightGrams: number; collectedOn: string; collectedBy: string; notes: string | null; }
export interface LotContribution { sampleBatchId: string; batchCode: string; farmName: string; farmCode: string; volumeKg: number; compositeSharePercent: number; batchGrade: string | null; }
export interface Lot { id: string; code: string; product: string; grade: string; status: string; closedReason: string | null; committedVolumeKg: number; priceMinPerKg: number; priceMaxPerKg: number; currency: string; incoterm: string; validUntil: string; sampleKind: string; shareOverrideReason: string | null; lotGrading: Grading | null; contributions: LotContribution[]; publishBlockers: string[]; }
export interface CreateLot { product: string; grade: string; contributions: { sampleBatchId: string; volumeKg: number; compositeSharePercent: number | null }[]; shareOverrideReason: string | null; priceMinPerKg: number; priceMaxPerKg: number; currency: string; incoterm: string; validUntil: string; }
export interface SampleRequest { id: string; farmCode: string | null; lotCode: string | null; companyName: string; contactName: string; email: string; countryCode: string; message: string | null; createdAt: string; handled: boolean; }

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
  settings() { return this.http.get<{ publicBaseUrl: string }>(`${this.base}/settings`); }
  batches() { return this.http.get<SampleBatch[]>(`${this.base}/sample-batches`); }
  createBatch(r: CreateBatch) { return this.http.post<SampleBatch>(`${this.base}/sample-batches`, r); }
  gradeBatch(id: string, g: GradingInput) { return this.http.post<SampleBatch>(`${this.base}/sample-batches/${id}/gradings`, g); }
  lots() { return this.http.get<Lot[]>(`${this.base}/lots`); }
  lot(id: string) { return this.http.get<Lot>(`${this.base}/lots/${id}`); }
  createLot(r: CreateLot) { return this.http.post<Lot>(`${this.base}/lots`, r); }
  gradeComposite(id: string, g: GradingInput) { return this.http.post<Lot>(`${this.base}/lots/${id}/composite-gradings`, g); }
  publishLot(id: string) { return this.http.post<Lot>(`${this.base}/lots/${id}/publish`, {}); }
  withdrawLot(id: string) { return this.http.post<Lot>(`${this.base}/lots/${id}/withdraw`, {}); }
  sampleRequests() { return this.http.get<SampleRequest[]>(`${this.base}/sample-requests`); }
}

/** Public site lives on the same origin (gateway); in developer mode it runs on port 4200. */
export const publicSiteUrl = () => location.port === '4201' ? 'http://localhost:4200' : '';

/** The message staff should see from a failed API call (business-rule text from the API when there is one). */
export const errorText = (e: any) => e?.error?.detail ?? e?.error?.errors?.[Object.keys(e?.error?.errors ?? {})[0]]?.[0] ?? 'Something went wrong. Please try again.';

/** Volume-weighted composite shares, mirroring the server (1 decimal, adds up to 100). */
export function volumeWeightedShares(volumes: number[]): number[] {
  const total = volumes.reduce((a, b) => a + b, 0);
  if (!total || volumes.some(v => !(v > 0))) return volumes.map(() => 0);
  const shares = volumes.map(v => Math.round((v / total) * 1000) / 10);
  const drift = Math.round((100 - shares.reduce((a, b) => a + b, 0)) * 10) / 10;
  shares[shares.indexOf(Math.max(...shares))] = Math.round((shares[shares.indexOf(Math.max(...shares))] + drift) * 10) / 10;
  return shares;
}

export const PRODUCTS = ['Black pepper', 'White pepper', 'Ceylon cinnamon', 'Cloves', 'Nutmeg', 'Mace', 'Cardamom', 'Turmeric', 'Ginger', 'Coffee'];
