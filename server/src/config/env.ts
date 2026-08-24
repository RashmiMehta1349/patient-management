import dotenv from 'dotenv';

dotenv.config();

function required(name: string, fallback?: string): string {
  const value = process.env[name] ?? fallback;
  if (value === undefined) {
    throw new Error(`Missing required environment variable: ${name}`);
  }
  return value;
}

export const env = {
  nodeEnv: process.env.NODE_ENV || 'development',
  port: parseInt(process.env.PORT || '3000', 10),

  // HTTPS enforcement: in production, the app rejects/redirects plain HTTP.
  // This is a safety-net at the application layer; the reverse proxy /
  // hosting layer (e.g. TLS termination, HSTS) is the primary enforcement
  // point per Planning\01_Authentication_and_Authorization_Plan.md §0/§11.
  trustProxy: process.env.TRUST_PROXY === 'true',
  forceHttps: process.env.FORCE_HTTPS !== 'false', // default true; disable only for local dev without TLS

  databaseUrl: required('DATABASE_URL', 'postgresql://user:pass@localhost:5432/pma_dev'),

  sessionJwtSecret: required('SESSION_JWT_SECRET', 'dev-only-insecure-secret-change-me'),
  sessionInactivityTimeoutMinutes: parseInt(process.env.SESSION_INACTIVITY_TIMEOUT_MINUTES || '15', 10),
  sessionHardTtlHours: parseInt(process.env.SESSION_HARD_TTL_HOURS || '12', 10),

  passwordResetTokenTtlMinutes: parseInt(process.env.PASSWORD_RESET_TOKEN_TTL_MINUTES || '30', 10),

  bcryptCostFactor: parseInt(process.env.BCRYPT_COST_FACTOR || '12', 10),

  corsOrigin: process.env.CORS_ORIGIN || 'http://localhost:4200'
};
