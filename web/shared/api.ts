// Types mirror the API's public contracts (allow-listed DTOs). Generated client replaces this in M2.
export interface PublicProduct { product: string; grade: string | null; harvestMonths: number[]; }
export interface PublicFarm {
  code: string; name: string; farmerName: string | null; region: string; country: string;
  hectares: number | null; story: string | null; products: PublicProduct[]; certifications: string[];
}
export interface SampleRequestInput {
  farmCode: string | null; companyName: string; contactName: string; email: string;
  countryCode: string; message: string | null; website: string | null;
}

export const MONTHS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
export const monthNames = (months: number[]) => months.map(m => MONTHS[m - 1]).join(', ');

export const COUNTRIES: [string, string][] = [
  ['ES', 'Spain'], ['NL', 'Netherlands'], ['DE', 'Germany'], ['GB', 'United Kingdom'], ['FR', 'France'], ['BE', 'Belgium'],
  ['IT', 'Italy'], ['US', 'United States'], ['AE', 'United Arab Emirates'], ['SA', 'Saudi Arabia'], ['LK', 'Sri Lanka'], ['IN', 'India'],
];
