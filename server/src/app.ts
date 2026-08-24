import express, { Express, NextFunction, Request, Response } from 'express';
import cors from 'cors';
import helmet from 'helmet';
import { env } from './config/env';
import { httpsEnforce } from './middleware/https-enforce.middleware';
import authRoutes from './modules/auth/auth.routes';

export function createApp(): Express {
  const app = express();

  app.set('trust proxy', env.trustProxy);

  app.use(
    helmet({
      // HSTS instructs browsers to only use HTTPS for this origin going
      // forward (plan §11 HTTPS enforcement / BRD "data encryption in transit").
      hsts: { maxAge: 15552000, includeSubDomains: true }
    })
  );
  app.use(httpsEnforce);
  app.use(cors({ origin: env.corsOrigin, credentials: true }));
  app.use(express.json());

  app.get('/health', (_req: Request, res: Response) => {
    res.status(200).json({ status: 'ok' });
  });

  app.use('/api/auth', authRoutes);

  app.use((req: Request, res: Response) => {
    res.status(404).json({ error: 'NOT_FOUND', message: `No route for ${req.method} ${req.path}` });
  });

  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  app.use((err: Error, _req: Request, res: Response, _next: NextFunction) => {
    // eslint-disable-next-line no-console
    console.error(err);
    res.status(500).json({ error: 'INTERNAL_ERROR', message: 'An unexpected error occurred.' });
  });

  return app;
}
