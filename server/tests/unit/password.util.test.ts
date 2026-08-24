import { hashPassword, verifyPassword, validatePasswordPolicy } from '../../src/modules/auth/password.util';

describe('password.util', () => {
  it('hashes a password and verifies the correct password against it', async () => {
    const hash = await hashPassword('CorrectHorse1');
    expect(hash).not.toEqual('CorrectHorse1');
    const ok = await verifyPassword('CorrectHorse1', hash);
    expect(ok).toBe(true);
  });

  it('rejects an incorrect password against a hash', async () => {
    const hash = await hashPassword('CorrectHorse1');
    const ok = await verifyPassword('WrongPassword1', hash);
    expect(ok).toBe(false);
  });

  it('accepts a password meeting the minimum complexity policy', () => {
    const result = validatePasswordPolicy('Password1');
    expect(result.valid).toBe(true);
  });

  it('rejects a password shorter than 8 characters', () => {
    const result = validatePasswordPolicy('Ab1');
    expect(result.valid).toBe(false);
  });

  it('rejects a password without a number', () => {
    const result = validatePasswordPolicy('PasswordOnly');
    expect(result.valid).toBe(false);
  });

  it('rejects a password without a letter', () => {
    const result = validatePasswordPolicy('12345678');
    expect(result.valid).toBe(false);
  });
});
