export interface CountryCode {
  dialCode: string;
  name: string;
}

/** Common countries this clinic's patients are realistically drawn from. Phone number length is
 *  fixed at a flat max of 10 digits regardless of country (see PatientFormComponent), matching the
 *  server's PhoneNumber column. */
export const COUNTRY_CODES: CountryCode[] = [
  { dialCode: '+91', name: 'India' },
  { dialCode: '+1', name: 'USA/Canada' },
  { dialCode: '+44', name: 'United Kingdom' },
  { dialCode: '+61', name: 'Australia' },
  { dialCode: '+49', name: 'Germany' },
  { dialCode: '+33', name: 'France' },
  { dialCode: '+86', name: 'China' },
  { dialCode: '+81', name: 'Japan' },
  { dialCode: '+55', name: 'Brazil' },
  { dialCode: '+971', name: 'UAE' },
  { dialCode: '+65', name: 'Singapore' },
  { dialCode: '+27', name: 'South Africa' },
  { dialCode: '+92', name: 'Pakistan' },
  { dialCode: '+880', name: 'Bangladesh' },
  { dialCode: '+94', name: 'Sri Lanka' },
  { dialCode: '+977', name: 'Nepal' },
  { dialCode: '+7', name: 'Russia' },
  { dialCode: '+52', name: 'Mexico' },
  { dialCode: '+39', name: 'Italy' },
  { dialCode: '+34', name: 'Spain' },
  { dialCode: '+31', name: 'Netherlands' },
  { dialCode: '+62', name: 'Indonesia' },
  { dialCode: '+60', name: 'Malaysia' },
  { dialCode: '+63', name: 'Philippines' },
  { dialCode: '+64', name: 'New Zealand' },
  { dialCode: '+966', name: 'Saudi Arabia' },
  { dialCode: '+20', name: 'Egypt' },
  { dialCode: '+254', name: 'Kenya' },
  { dialCode: '+82', name: 'South Korea' }
];
