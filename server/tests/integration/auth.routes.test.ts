/**
 * Integration tests for the /api/auth/* HTTP surface.
 *
 * NOTE ON DB DEPENDENCY: this environment has no live PostgreSQL instance
 * available (no local Postgres server, no Docker). Per plan §12 these tests
 * are written as full HTTP-level integration tests, but `../../src/db/prisma`
 * is mocked with the same in-memory fake used by the AuthService unit tests
 * so they can run without a database connection. When a real Postgres
 * instance/test DB is available, this mock can be removed (or swapped for a
 * real Prisma client pointed at a disposable test database) and these tests
 * should continue to pass unchanged against the real DB layer.
 */
// env is read at module-load time, so this must be set before importing app.
process.env.FORCE_HTTPS = 'false';

import request from 'supertest';
import { hashPassword } from '../../src/modules/auth/password.util';
import { hashToken } from '../../src/modules/auth/token.util';

function createFakePrisma() {
  const users = new Map<string, any>();
  const sessions = new Map<string, any>();
  const resetTokens = new Map<string, any>();
  let idCounter = 0;
  const nextId = () => `id-${++idCounter}`;

  return {
    _seedUser(user: { email: string; passwordHash: string }) {
      const id = nextId();
      const record = { id, lastLoginAt: null, createdAt: new Date(), updatedAt: new Date(), ...user };
      users.set(id, record);
      return record;
    },
    user: {
      findUnique: async ({ where }: any) => {
        if (where.email) return [...users.values()].find((u) => u.email === where.email) || null;
        if (where.id) return users.get(where.id) || null;
        return null;
      },
      update: async ({ where, data }: any) => {
        const existing = users.get(where.id);
        const updated = { ...existing, ...data };
        users.set(where.id, updated);
        return updated;
      }
    },
    session: {
      create: async ({ data }: any) => {
        const id = nextId();
        const record = { id, revokedAt: null, ...data };
        sessions.set(id, record);
        return record;
      },
      findUnique: async ({ where }: any) => sessions.get(where.id) || null,
      update: async ({ where, data }: any) => {
        const existing = sessions.get(where.id);
        const updated = { ...existing, ...data };
        sessions.set(where.id, updated);
        return updated;
      },
      updateMany: async ({ where, data }: any) => {
        let count = 0;
        for (const [id, s] of sessions.entries()) {
          if (s.userId === where.userId && s.revokedAt === where.revokedAt) {
            sessions.set(id, { ...s, ...data });
            count++;
          }
        }
        return { count };
      }
    },
    passwordResetToken: {
      create: async ({ data }: any) => {
        const id = nextId();
        const record = { id, usedAt: null, createdAt: new Date(), ...data };
        resetTokens.set(id, record);
        return record;
      },
      findFirst: async ({ where }: any) =>
        [...resetTokens.values()].find((t) => t.tokenHash === where.tokenHash) || null,
      update: async ({ where, data }: any) => {
        const existing = resetTokens.get(where.id);
        const updated = { ...existing, ...data };
        resetTokens.set(where.id, updated);
        return updated;
      }
    },
    $transaction: async (ops: Promise<any>[]) => Promise.all(ops)
  };
}

const fakePrisma = createFakePrisma();

jest.mock('../../src/db/prisma', () => ({
  prisma: fakePrisma
}));

jest.mock('../../src/modules/auth/email.service', () => ({
  emailService: { sendPasswordResetEmail: jest.fn().mockResolvedValue(undefined) }
}));

// Must import app AFTER mocks are set up.
// eslint-disable-next-line import/first
import { createApp } from '../../src/app';

const app = createApp();
const TEST_EMAIL = 'doctor@example.com';
const TEST_PASSWORD = 'CorrectHorse1';

beforeAll(async () => {
  const passwordHash = await hashPassword(TEST_PASSWORD);
  fakePrisma._seedUser({ email: TEST_EMAIL, passwordHash });
});

describe('POST /api/auth/login', () => {
  it('returns 200 and a token for valid credentials', async () => {
    const res = await request(app).post('/api/auth/login').send({ email: TEST_EMAIL, password: TEST_PASSWORD });
    expect(res.status).toBe(200);
    expect(res.body.token).toBeDefined();
    expect(res.body.user.email).toBe(TEST_EMAIL);
  });

  it('returns 401 with a generic message for wrong password', async () => {
    const res = await request(app).post('/api/auth/login').send({ email: TEST_EMAIL, password: 'WrongPassword1' });
    expect(res.status).toBe(401);
    expect(res.body.message).toBe('Invalid email or password');
  });

  it('returns the same generic message for an unknown email', async () => {
    const res = await request(app).post('/api/auth/login').send({ email: 'nobody@example.com', password: 'whatever1' });
    expect(res.status).toBe(401);
    expect(res.body.message).toBe('Invalid email or password');
  });
});

describe('GET /api/auth/me', () => {
  it('returns 401 without a token', async () => {
    const res = await request(app).get('/api/auth/me');
    expect(res.status).toBe(401);
  });

  it('returns user data with a valid token', async () => {
    const loginRes = await request(app).post('/api/auth/login').send({ email: TEST_EMAIL, password: TEST_PASSWORD });
    const token = loginRes.body.token;

    const res = await request(app).get('/api/auth/me').set('Authorization', `Bearer ${token}`);
    expect(res.status).toBe(200);
    expect(res.body.email).toBe(TEST_EMAIL);
  });
});

describe('POST /api/auth/logout', () => {
  it('revokes the session so a subsequent authenticated call returns 401', async () => {
    const loginRes = await request(app).post('/api/auth/login').send({ email: TEST_EMAIL, password: TEST_PASSWORD });
    const token = loginRes.body.token;

    const logoutRes = await request(app).post('/api/auth/logout').set('Authorization', `Bearer ${token}`);
    expect(logoutRes.status).toBe(204);

    const meRes = await request(app).get('/api/auth/me').set('Authorization', `Bearer ${token}`);
    expect(meRes.status).toBe(401);
  });
});

describe('Password reset flow', () => {
  it('request: known email returns 200 with uniform message', async () => {
    const res = await request(app).post('/api/auth/password-reset/request').send({ email: TEST_EMAIL });
    expect(res.status).toBe(200);
    expect(res.body.message).toMatch(/if that email is registered/i);
  });

  it('request: unknown email returns the identical response shape (no enumeration)', async () => {
    const res = await request(app).post('/api/auth/password-reset/request').send({ email: 'ghost@example.com' });
    expect(res.status).toBe(200);
    expect(res.body.message).toMatch(/if that email is registered/i);
  });

  it('confirm: valid token updates password and revokes existing sessions', async () => {
    // Get a session token before reset, to prove it becomes invalid.
    const loginRes = await request(app).post('/api/auth/login').send({ email: TEST_EMAIL, password: TEST_PASSWORD });
    const preResetToken = loginRes.body.token;

    // Directly issue a reset token via the fake store (simulating what the
    // dev-stub email service would have logged).
    const { AuthService } = await import('../../src/modules/auth/auth.service');
    const service = new AuthService(fakePrisma as any);
    const reset = await service.requestPasswordReset(TEST_EMAIL);

    const confirmRes = await request(app).post('/api/auth/password-reset/confirm').send({
      token: reset!.rawToken,
      newPassword: 'BrandNewPassword9',
      confirmPassword: 'BrandNewPassword9'
    });
    expect(confirmRes.status).toBe(200);

    // Pre-reset session token must now be invalid.
    const meRes = await request(app).get('/api/auth/me').set('Authorization', `Bearer ${preResetToken}`);
    expect(meRes.status).toBe(401);

    // Old password no longer works; new one does.
    const oldLogin = await request(app).post('/api/auth/login').send({ email: TEST_EMAIL, password: TEST_PASSWORD });
    expect(oldLogin.status).toBe(401);

    const newLogin = await request(app)
      .post('/api/auth/login')
      .send({ email: TEST_EMAIL, password: 'BrandNewPassword9' });
    expect(newLogin.status).toBe(200);
  });

  it('confirm: rejects mismatched password confirmation', async () => {
    const res = await request(app).post('/api/auth/password-reset/confirm').send({
      token: 'irrelevant',
      newPassword: 'BrandNewPassword9',
      confirmPassword: 'DoesNotMatch1'
    });
    expect(res.status).toBe(400);
  });
});
