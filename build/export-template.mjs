// Exports a project template as a repository of its own, ready to push as a GitHub template repository:
//
//   node build/export-template.mjs <name> <out-dir>
//
// Copies templates/<name> without build output (bin, obj, node_modules, TestResults), runs `git init` and makes one commit. It rewrites nothing:
// a template is already a complete repository (no Directory.Build.*, a Blux.UI package reference pinned in its Directory.Packages.props), so
// what is exported is what is in the monorepo. Nothing is pushed.
import { spawnSync } from "node:child_process";
import { cpSync, existsSync, mkdirSync, readdirSync, statSync } from "node:fs";
import { basename, dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const root = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const skipped = new Set(["bin", "obj", "node_modules", "TestResults", ".git"]);

/** Copies the template and commits it. Returns the output folder. */
export function exportTemplate(name, out, { commit = true } = {}) {
  if (name.startsWith("_")) {
    throw new Error(`'${name}' is not a template: folders that start with an underscore hold what the templates share`);
  }
  const source = join(root, "templates", name);
  if (!existsSync(source) || !statSync(source).isDirectory()) {
    throw new Error(`no template named '${name}' in templates/`);
  }
  out = resolve(out);
  if (existsSync(out) && readdirSync(out).length > 0) {
    throw new Error(`${out} exists and is not empty`);
  }
  mkdirSync(out, { recursive: true });
  cpSync(source, out, { recursive: true, filter: (path) => !skipped.has(basename(path)) });
  if (commit) {
    const git = (...args) => {
      const r = spawnSync("git", ["-c", "user.name=blux-ui contributors", "-c", "user.email=noreply@example.invalid", "-c", "commit.gpgsign=false", ...args], { cwd: out, encoding: "utf8" });
      if (r.status !== 0) throw new Error(`git ${args.join(" ")} failed: ${r.stderr || r.stdout}`);
    };
    git("init", "--quiet", "--initial-branch=main");
    git("add", "--all");
    git("commit", "--quiet", "--message", `Initial commit of the ${name} template`);
  }
  return out;
}

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  const [name, out] = process.argv.slice(2);
  if (!name || !out) {
    console.error("usage: node build/export-template.mjs <name> <out-dir>");
    process.exit(1);
  }
  try {
    console.log(`exported ${name} to ${exportTemplate(name, out)}`);
  } catch (error) {
    console.error(error.message);
    process.exit(1);
  }
}
