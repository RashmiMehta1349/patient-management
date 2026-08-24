import { AuthService } from '../../src/modules/auth/auth.service';
import { hashPassword } from '../../src/modules/auth/password.util';
import { hashToken, signSessionToken } from '../../src/modules/auth/token.util';

/**
 * Minimal in-memory fake of the subset of PrismaClient used by AuthService,
 * so this suite exercises real business logic without a live database.
 */
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
    _getSession(id: string) {
      return sessions.get(id);
    },
    _seedResetToken(data: any) {
      const id = nextId();
      const record = { id, usedAt: null, createdAt: new Date(), ...data };
      resetTokens.set(id, record);
      return record;
    },
    user: {
      findUnique: async ({ where }: any) => {
        if (where.email) {
          return [...users.values()].find((u) => u.email === where.email) || null;
        }
        if (where.id) {
          return users.get(where.id) || null;
        }
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
      findFirst: async ({ where }: any) => [...resetTokens.values()].find((t) => t.tokenHash === where.tokenHash) || null,
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

describe('AuthService.login', () => {
  it('returns a session token for correct credentials', async () => {
    const prisma = createFakePrisma();
    const passwordHash = await hashPassword('CorrectHorse1');
    prisma._seedUser({ email: 'doctor@example.com', passwordHash });

    const service = new AuthService(prisma as any);
    const result = await service.login('doctor@example.com', 'CorrectHorse1');

    expect(result.ok).toBe(true);
    expect(result.token).toBeDefined();
    expect(result.user?.email).toBe('doctor@example.com');
  });

  it('fails with a generic result for an unknown email', async () => {
    const prisma = createFakePrisma();
    const service = new AuthService(prisma as any);
    const result = await service.login('nobody@example.com', 'whatever1');
    expect(result.ok).toBe(false);
    expect(result.token).toBeUndefined();
  });

  it('fails with a generic result for a wrong password (same shape as unknown user)', async () => {
    const prisma = createFakePrisma();
    const passwordHash = await hashPassword('CorrectHorse1');
    prisma._seedUser({ email: 'doctor@example.com', passwordHash });

    const service = new AuthService(prisma as any);
    const result = await service.login('doctor@example.com', 'WrongPassword1');

    expect(result.ok).toBe(false);
    expect(result.token).toBeUndefined();
  });
});

describe('AuthService.validateSession', () => {
  it('accepts a freshly created valid session', async () => {
    const prisma = createFakePrisma();
    const passwordHash = await hashPassword('CorrectHorse1');
    prisma._seedUser({ email: 'doctor@example.com', passwordHash });
    const service = new AuthService(prisma as any);
    const login = await service.login('doctor@example.com', 'CorrectHorse1');

    const result = await service.validateSession(login.token!);
    expect(result.ok).toBe(true);
    expect(result.user?.email).toBe('doctor@example.com');
  });

  it('rejects a session past its hard expiry', async () => {
    const prisma = createFakePrisma();
    const user = prisma._seedUser({ email: 'doctor@example.com', passwordHash: 'x' });
    const rawSecret = 'secret123';
    const session = await prisma.session.create({
      data: {
        userId: user.id,
        tokenHash: hashToken(rawSecret),
        createdAt: new Date(Date.now() - 1000 * 60 * 60 * 20),
        lastActivityAt: new Date(),
        expiresAt: new Date(Date.now() - 1000) // already expired
      }
    });
    const token = signSessionToken(session.id, rawSecret);

    const service = new AuthService(prisma as any);
    const result = await service.validateSession(token);
    expect(result.ok).toBe(false);
    expect(result.reason).toBe('expired');
  });

  it('rejects a session idle past the inactivity window', async () => {
    const prisma = createFakePrisma();
    const user = prisma._seedUser({ email: 'doctor@example.com', passwordHash: 'x' });
    const rawSecret = 'secret123';
    const session = await prisma.session.create({
      data: {
        userId: user.id,
        tokenHash: hashToken(rawSecret),
        createdAt: new Date(Date.now() - 1000 * 60 * 60),
        lastActivityAt: new Date(Date.now() - 1000 * 60 * 20), // 20 min idle > 15 min timeout
        expiresAt: new Date(Date.now() + 1000 * 60 * 60)
      }
    });
    const token = signSessionToken(session.id, rawSecret);

    const service = new AuthService(prisma as any);
    const result = await service.validateSession(token);
    expect(result.ok).toBe(false);
    expect(result.reason).toBe('inactive');
  });

  it('rejects a revoked session', async () => {
    const prisma = createFakePrisma();
    const user = prisma._seedUser({ email: 'doctor@example.com', passwordHash: 'x' });
    const rawSecret = 'secret123';
    const session = await prisma.session.create({
      data: {
        userId: user.id,
        tokenHash: hashToken(rawSecret),
        createdAt: new Date(),
        lastActivityAt: new Date(),
        expiresAt: new Date(Date.now() + 1000 * 60 * 60),
        revokedAt: new Date()
      }
    });
    const token = signSessionToken(session.id, rawSecret);

    const service = new AuthService(prisma as any);
    const result = await service.validateSession(token);
    expect(result.ok).toBe(false);
    expect(result.reason).toBe('revoked');
  });

  it('rejects an unsigned/garbage token', async () => {
    const prisma = createFakePrisma();
    const service = new AuthService(prisma as any);
    const result = await service.validateSession('not-a-real-token');
    expect(result.ok).toBe(false);
    expect(result.reason).toBe('invalid_token');
  });
});

describe('AuthService password reset flow', () => {
  it('generates a token on request for a known email and rejects unknown emails', async () => {
    const prisma = createFakePrisma();
    prisma._seedUser({ email: 'doctor@example.com', passwordHash: 'x' });
    const service = new AuthService(prisma as any);

    const known = await service.requestPasswordReset('doctor@example.com');
    expect(known).not.toBeNull();
    expect(known?.rawToken).toBeDefined();

    const unknown = await service.requestPasswordReset('nobody@example.com');
    expect(unknown).toBeNull();
  });

  it('confirms a valid token, updates password, and revokes all existing sessions', async () => {
    const prisma = createFakePrisma();
    const passwordHash = await hashPassword('OldPassword1');
    const user = prisma._seedUser({ email: 'doctor@example.com', passwordHash });
    const service = new AuthService(prisma as any);

    // Create an active session pre-reset.
    const login = await service.login('doctor@example.com', 'OldPassword1');
    expect(login.ok).toBe(true);

    const reset = await service.requestPasswordReset('doctor@example.com');
    const result = await service.confirmPasswordReset(reset!.rawToken, 'NewPassword2');
    expect(result.ok).toBe(true);

    // Old session must now be revoked.
    const sessionCheck = await service.validateSession(login.token!);
    expect(sessionCheck.ok).toBe(false);
    expect(sessionCheck.reason).toBe('revoked');

    // New password works for login.
    const loginWithNew = await service.login('doctor@example.com', 'NewPassword2');
    expect(loginWithNew.ok).toBe(true);
  });

  it('rejects an expired or already-used reset token', async () => {
    const prisma = createFakePrisma();
    const user = prisma._seedUser({ email: 'doctor@example.com', passwordHash: 'x' });
    const service = new AuthService(prisma as any);

    prisma._seedResetToken({
      userId: user.id,
      tokenHash: hashToken('expired-token'),
      expiresAt: new Date(Date.now() - 1000)
    });

    const result = await service.confirmPasswordReset('expired-token', 'NewPassword2');
    expect(result.ok).toBe(false);
    expect((result as any).reason).toBe('expired_or_used');
  });

  it('rejects a new password that fails the complexity policy', async () => {
    const prisma = createFakePrisma();
    const user = prisma._seedUser({ email: 'doctor@example.com', passwordHash: 'x' });
    const service = new AuthService(prisma as any);
    prisma._seedResetToken({
      userId: user.id,
      tokenHash: hashToken('good-token'),
      expiresAt: new Date(Date.now() + 1000 * 60)
    });

    const result = await service.confirmPasswordReset('good-token', 'short');
    expect(result.ok).toBe(false);
    expect((result as any).reason).toBe('weak_password');
  });
});
