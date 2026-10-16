import { PrismaClient } from '@prisma/client';

const globalForPrisma = globalThis as unknown as {
  prisma: PrismaClient | undefined;
};

const databaseUrl = process.env.DATABASE_URL ?? 'file:./dev.db';

let prismaInstance: PrismaClient;
try {
  prismaInstance =
    globalForPrisma.prisma ??
    new PrismaClient({
      log: ['error'],
      datasources: {
        db: { url: databaseUrl }
      }
    });
} catch (e) {
  console.warn('[AI Studio] Database fallback active:', e);
  const noOp = {
    findMany: async () => [],
    findFirst: async () => null,
    findUnique: async () => null,
    create: async (d: any) => d?.data ?? {},
    update: async (d: any) => d?.data ?? {},
    delete: async () => ({})
  };
  prismaInstance = new Proxy({}, { get: () => noOp }) as any;
}

export const prisma = prismaInstance;

if (process.env.NODE_ENV !== 'production') {
  globalForPrisma.prisma = prisma;
}

export type { Prisma } from '@prisma/client';
