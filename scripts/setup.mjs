// One-time developer setup: .env, .NET tools, web dependencies. Safe to run again.
import { existsSync, copyFileSync } from 'node:fs';
import { execSync } from 'node:child_process';

const run = (cmd, opts = {}) => { console.log(`\n> ${cmd}`); execSync(cmd, { stdio: 'inherit', ...opts }); };

if (!existsSync('.env')) { copyFileSync('.env.example', '.env'); console.log('Created .env from .env.example'); }
run('dotnet tool restore');
run('npm ci', { cwd: 'web' });
console.log('\nDone. Next: npm run infra  →  npm run db  →  npm run api  (and in another terminal) npm run web');
