import { PrismaClient } from '@prisma/client';

// Single shared Prisma client instance for the app.
export const prisma = new PrismaClient();
