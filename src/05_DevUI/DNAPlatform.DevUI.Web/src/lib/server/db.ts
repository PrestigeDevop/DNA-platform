import { PrismaClient } from '@prisma/client';

const globalForPrisma = globalThis as unknown as {
  prisma: PrismaClient | undefined;
};

// Explicit datasource URL with fallback — guarantees Prisma works even if
// the .env file fails to load into process.env (fixes Prisma error P1012).
// Note: "file:" paths in the datasource are resolved relative to the
// schema.prisma location (prisma/), so "file:./dev.db" => prisma/dev.db.
const databaseUrl =
  process.env.DATABASE_URL ?? 'file:./dev.db';

export const prisma = globalForPrisma.prisma ?? new PrismaClient({
  log: ['query', 'error'],
  datasources: {
    db: { url: databaseUrl },
  },
});

if (process.env.NODE_ENV !== 'production') {
  globalForPrisma.prisma = prisma;
}

export type { Prisma } from '@prisma/client';
