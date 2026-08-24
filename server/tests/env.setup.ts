process.env.NODE_ENV = 'test';
process.env.SESSION_JWT_SECRET = 'test-secret-do-not-use-in-production';
process.env.SESSION_INACTIVITY_TIMEOUT_MINUTES = '15';
process.env.SESSION_HARD_TTL_HOURS = '12';
process.env.PASSWORD_RESET_TOKEN_TTL_MINUTES = '30';
process.env.DATABASE_URL = process.env.DATABASE_URL || 'postgresql://user:pass@localhost:5432/pma_test';
