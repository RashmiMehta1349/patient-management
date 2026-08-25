export const environment = {
  production: true,
  apiBaseUrl: '/api',
  // Must match the server's Auth:AccessTokenLifetimeMinutes (Assumption A2/A4) — kept in sync
  // manually since client and server are deployed/configured independently.
  inactivityTimeoutMinutes: 20
};
