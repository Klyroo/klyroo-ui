// Compares the Blazor components of the Klyroo repositories with the native controls of Klyroo.UI.Maui.
// A Blazor component (Kly*.razor) is covered when a type with the same name exists in src/Klyroo.UI.Maui, or when
// build/maui-coverage.json lists it as not applicable with a reason. Exit code 1 when one is neither.
// Usage: node build/maui-coverage.mjs [--list]   (the sibling repositories klyroo-* are read when they sit next to this one)
import { existsSync, readdirSync, readFileSync, statSync } from 'node:fs';
import { dirname, join, relative } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = join(dirname(fileURLToPath(import.meta.url)), '..');
const parent = join(root, '..');

function* walk(dir) {
  for (const name of readdirSync(dir)) {
    if (name === 'obj' || name === 'bin' || name === 'node_modules') continue;
    const path = join(dir, name);
    if (statSync(path).isDirectory()) yield* walk(path);
    else yield path;
  }
}

const blazor = new Map(); // component -> repository
for (const repo of readdirSync(parent).filter((n) => n.startsWith('klyroo-') && existsSync(join(parent, n, 'src')))) {
  for (const file of walk(join(parent, repo, 'src'))) {
    const m = /[\\/](Kly\w+)\.razor$/.exec(file);
    if (m && !blazor.has(m[1])) blazor.set(m[1], repo);
    if (file.endsWith('.cs')) {
      // components written in C# only (KlyInput, KlyHead, KlyColumn, ...): a concrete class that derives from a component base class
      for (const c of readFileSync(file, 'utf8').matchAll(/public (?:sealed |partial )*class (Kly\w+)(?:<[\w, ]+>)?\s*:\s*(?:\w+\.)?(ComponentBase|Kly\w*Base)\b/g)) {
        if (!c[1].endsWith('Base') && !blazor.has(c[1])) blazor.set(c[1], repo);
      }
    }
  }
}

const native = new Set();
for (const file of walk(join(root, 'src/Klyroo.UI.Maui'))) {
  if (!file.endsWith('.cs')) continue;
  for (const m of readFileSync(file, 'utf8').matchAll(/\b(?:class|record|struct|enum|interface)\s+(Kly\w+)/g)) native.add(m[1]);
}

const config = JSON.parse(readFileSync(join(root, 'build/maui-coverage.json'), 'utf8'));
const na = config.notApplicable ?? {};
const rows = [...blazor].sort(([a], [b]) => a.localeCompare(b)).map(([name, repo]) => ({
  name, repo,
  status: native.has(name) ? 'native' : na[name] ? 'n/a' : 'missing',
  note: na[name] ?? '',
}));

const count = (s) => rows.filter((r) => r.status === s).length;
const missing = rows.filter((r) => r.status === 'missing');
console.log(`Blazor components: ${rows.length}  native: ${count('native')}  not applicable: ${count('n/a')}  missing: ${missing.length}`);
const byRepo = Object.groupBy(rows, (r) => r.repo);
for (const [repo, list] of Object.entries(byRepo)) {
  console.log(`  ${repo}: ${list.filter((r) => r.status !== 'missing').length}/${list.length}`);
}
if (process.argv.includes('--list')) for (const r of rows) console.log(`${r.status.padEnd(8)} ${r.repo.padEnd(18)} ${r.name}${r.note ? '  # ' + r.note : ''}`);
else if (missing.length) console.log('missing: ' + missing.map((r) => r.name).join(' '));
for (const name of Object.keys(na)) if (!blazor.has(name)) console.log(`warning: "${name}" is listed as not applicable but is not a Blazor component`);
process.exit(missing.length ? 1 : 0);
