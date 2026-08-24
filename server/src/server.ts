import { createApp } from './app';
import { env } from './config/env';

const app = createApp();

app.listen(env.port, () => {
  // eslint-disable-next-line no-console
  console.log(`PMA server listening on port ${env.port} (env: ${env.nodeEnv})`);
});
