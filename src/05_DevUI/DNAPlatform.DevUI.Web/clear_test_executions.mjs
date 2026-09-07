import { PrismaClient } from '@prisma/client';
const prisma = new PrismaClient();
const deleted = await prisma.execution.deleteMany({});
console.log('Deleted executions:', deleted.count);
await prisma.$disconnect();
