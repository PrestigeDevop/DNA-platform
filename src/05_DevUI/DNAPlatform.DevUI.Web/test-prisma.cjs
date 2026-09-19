// Prisma connectivity test - replicates src/lib/server/db.ts behavior
// Usage: node test-prisma.cjs              -> uses default (needs DATABASE_URL env)
//        node test-prisma.cjs "file:./x"   -> explicit datasource URL (the db.ts fix)
const { PrismaClient } = require('@prisma/client');

const explicitUrl = process.argv[2];
const client = explicitUrl
  ? new PrismaClient({ datasources: { db: { url: explicitUrl } } })
  : new PrismaClient();

client.workflow
  .findMany()
  .then((rows) => {
    console.log('SUCCESS - workflows in DB:', rows.length);
    process.exit(0);
  })
  .catch((e) => {
    console.error('ERROR:', String(e.message).split('\n')[0]);
    process.exit(1);
  });