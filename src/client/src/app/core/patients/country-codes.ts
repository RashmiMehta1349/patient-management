export interface CountryCode {
  dialCode: string;
  name: string;
  /** National significant number length (digits after the dial code), min and max. */
  minLength: number;
  maxLength: number;
}

/** Common countries with their mobile number dial code and valid national-number digit length.
 *  Not exhaustive of all ITU-assigned dial codes — covers the countries this clinic's patients
 *  are realistically drawn from. Falls back to the general E.164 bound (4-14 digits) for any
 *  other manually-entered dial code via `DEFAULT_COUNTRY_CODE_LENGTH`. */
export const COUNTRY_CODES: CountryCode[] = [
  { dialCode: '+91', name: 'India', minLength: 10, maxLength: 10 },
  { dialCode: '+1', name: 'USA/Canada', minLength: 10, maxLength: 10 },
  { dialCode: '+44', name: 'United Kingdom', minLength: 10, maxLength: 10 },
  { dialCode: '+61', name: 'Australia', minLength: 9, maxLength: 9 },
  { dialCode: '+49', name: 'Germany', minLength: 10, maxLength: 11 },
  { dialCode: '+33', name: 'France', minLength: 9, maxLength: 9 },
  { dialCode: '+86', name: 'China', minLength: 11, maxLength: 11 },
  { dialCode: '+81', name: 'Japan', minLength: 10, maxLength: 10 },
  { dialCode: '+55', name: 'Brazil', minLength: 10, maxLength: 11 },
  { dialCode: '+971', name: 'UAE', minLength: 9, maxLength: 9 },
  { dialCode: '+65', name: 'Singapore', minLength: 8, maxLength: 8 },
  { dialCode: '+27', name: 'South Africa', minLength: 9, maxLength: 9 },
  { dialCode: '+92', name: 'Pakistan', minLength: 10, maxLength: 10 },
  { dialCode: '+880', name: 'Bangladesh', minLength: 10, maxLength: 10 },
  { dialCode: '+94', name: 'Sri Lanka', minLength: 9, maxLength: 9 },
  { dialCode: '+977', name: 'Nepal', minLength: 10, maxLength: 10 },
  { dialCode: '+7', name: 'Russia', minLength: 10, maxLength: 10 },
  { dialCode: '+52', name: 'Mexico', minLength: 10, maxLength: 10 },
  { dialCode: '+39', name: 'Italy', minLength: 9, maxLength: 10 },
  { dialCode: '+34', name: 'Spain', minLength: 9, maxLength: 9 },
  { dialCode: '+31', name: 'Netherlands', minLength: 9, maxLength: 9 },
  { dialCode: '+62', name: 'Indonesia', minLength: 9, maxLength: 12 },
  { dialCode: '+60', name: 'Malaysia', minLength: 9, maxLength: 10 },
  { dialCode: '+63', name: 'Philippines', minLength: 10, maxLength: 10 },
  { dialCode: '+64', name: 'New Zealand', minLength: 8, maxLength: 9 },
  { dialCode: '+966', name: 'Saudi Arabia', minLength: 9, maxLength: 9 },
  { dialCode: '+20', name: 'Egypt', minLength: 10, maxLength: 10 },
  { dialCode: '+254', name: 'Kenya', minLength: 9, maxLength: 9 },
  { dialCode: '+82', name: 'South Korea', minLength: 9, maxLength: 10 }
];

/** ITU E.164 general bound for the national significant number when the dial code isn't in the list above. */
export const DEFAULT_COUNTRY_CODE_LENGTH = { minLength: 4, maxLength: 14 };

export function getCountryCodeLength(dialCode: string): { minLength: number; maxLength: number } {
  const match = COUNTRY_CODES.find((c) => c.dialCode === dialCode);
  return match ?? DEFAULT_COUNTRY_CODE_LENGTH;
}
