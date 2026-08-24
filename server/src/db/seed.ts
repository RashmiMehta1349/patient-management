/**
 * Seeds the single pre-provisioned doctor account.
 *
 * Per Module 1 doc §5 / BRD: "Exactly one user account exists in Phase 1;
 * there is no in-app account creation UI." This script is the only supported
 * way to provision that account and is intended to be run once during
 * deployment/setup, not exposed via any API.
 *
 * Usage:
 *   SEED_DOCTOR_EMAIL=doctor@example.com SEED_DOCTOR_PASSWORD='ChangeMe123!' npm run prisma:seed
 *
 * If SEED_DOCTOR_EMAIL / SEED_DOCTOR_PASSWORD are not provided, safe
 * documented defaults are used for local development only — these MUST be
 * overridden for any real deployment.
 */
import { prisma } from './prisma';
import { hashPassword, validatePasswordPolicy } from '../modules/auth/password.util';

async function main() {
  const email = process.env.SEED_DOCTOR_EMAIL || 'doctor@example.com';
  const password = process.env.SEED_DOCTOR_PASSWORD || 'ChangeMe123!';

  const policy = validatePasswordPolicy(password);
  if (!policy.valid) {
    throw new Error(`Refusing to seed account: ${policy.message}`);
  }

  const existing = await prisma.user.findUnique({ where: { email } });
  if (existing) {
    console.log(`Seed skipped: a user with email "${email}" already exists (id=${existing.id}).`);
    return;
  }

  const passwordHash = await hashPassword(password);
  const user = await prisma.user.create({
    data: { email, passwordHash }
  });

  console.log(`Seeded pre-provisioned doctor account: ${user.email} (id=${user.id}).`);
  console.log('IMPORTANT: change this password after first login if it was a default/placeholder value.');
}

main()
  .catch((err) => {
    console.error('Seed failed:', err);
    process.exitCode = 1;
  })
  .finally(async () => {
    await prisma.$disconnect();
  });
