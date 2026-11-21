// Template check: each project template, exported the way a user gets it, must restore, build and run against a PACKED Blux.UI, with nothing of
// this monorepo around it.
//
//   node build/verify-templates.mjs [--keep] [template ...]
//
// For every template (all of them by default):
//   1. exports it to a temporary folder with no Directory.Build.*, Directory.Packages.props or nuget.config in any ancestor (the check fails if
//      the machine has one there), and checks the export: no monorepo path, no absolute path, Blux.UI pinned to the version of build/Version.props;
//   2. restores and builds its web project in Release with warnings as errors, from a local feed holding the packed library;
//   3. runs it: / answers 200, the stylesheet it links contains a utility only the template uses (so Tailwind ran), and the app also works
//      below a path base;
//   4. runs `dotnet run rename.cs Acme` in a second export and builds the result.
// The MAUI heads are not built here: they need workloads, so the `maui` job of the CI workflow builds them.
//
// Needs the .NET 10 SDK and the network (the first build downloads the Tailwind CLI, the fonts and the icons of the template, and NuGet restores
// the framework packages). The pack output is a folder feed; Blux.UI comes only from it, through a global-packages folder of this run.
import { spawn, spawnSync } from "node:child_process";
import { existsSync, mkdirSync, mkdtempSync, readFileSync, readdirSync, rmSync, statSync, writeFileSync } from "node:fs";
import { homedir, tmpdir } from "node:os";
import { basename, dirname, join, resolve, sep } from "node:path";
import { fileURLToPath } from "node:url";
import { exportTemplate } from "./export-template.mjs";

// What each template promises: its web project, and a Tailwind utility that only its own pages use (it is in no precompiled Blux stylesheet).
const templates = [
  {
    name: "starter",
    web: "src/Starter.Web",
    renamedWeb: "src/Acme.Web",
    probe: { utility: "max-w-[37rem]", css: "max-width:37rem" },
    pages: ["/", "/about"],
  },
];

const keep = process.argv.includes("--keep");
const wanted = process.argv.slice(2).filter((a) => !a.startsWith("--"));
const root = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const work = mkdtempSync(join(tmpdir(), "blux-templates-"));
const feed = join(work, "feed");
const failures = [];
const check = (ok, what) => { console.log(`${ok ? "ok  " : "FAIL"} ${what}`); if (!ok) failures.push(what); return ok; };
const env = { NUGET_PACKAGES: join(work, "packages"), DOTNET_NOLOGO: "1", DOTNET_CLI_TELEMETRY_OPTOUT: "1", DOTNET_SKIP_FIRST_TIME_EXPERIENCE: "1" };

const run = (cmd, args, cwd) => {
  const r = spawnSync(cmd, args, { cwd, encoding: "utf8", env: { ...process.env, ...env }, maxBuffer: 1 << 28 });
  return { code: r.status ?? 1, out: `${r.stdout ?? ""}${r.stderr ?? ""}` };
};

/** The text files of a folder (no .git, bin or obj): path and content. A file with a NUL byte is binary and left out. */
const textFiles = (folder) => {
  const found = [];
  const walk = (dir) => {
    for (const entry of readdirSync(dir, { withFileTypes: true })) {
      if (entry.name === ".git" || entry.name === "bin" || entry.name === "obj") continue;
      const path = join(dir, entry.name);
      if (entry.isDirectory()) { walk(path); continue; }
      if (statSync(path).size > 8 * 1024 * 1024) continue;
      const bytes = readFileSync(path);
      if (bytes.subarray(0, 8192).includes(0)) continue;
      found.push({ path, text: bytes.toString("utf8") });
    }
  };
  walk(folder);
  return found;
};

const allPaths = (folder) => {
  const found = [];
  const walk = (dir) => {
    for (const entry of readdirSync(dir, { withFileTypes: true })) {
      if (entry.name === ".git" || entry.name === "bin" || entry.name === "obj") continue;
      const path = join(dir, entry.name);
      found.push(path);
      if (entry.isDirectory()) walk(path);
    }
  };
  walk(folder);
  return found;
};

/** Starts the built web app on a free port; resolves with its address once / answers. */
const start = async (dll, cwd, extraEnv = {}) => {
  const port = 20000 + Math.floor(Math.random() * 20000);
  const url = `http://127.0.0.1:${port}`;
  let log = "";
  // Development: the static web assets of the referenced projects are served from where the build left them, without a publish.
  const child = spawn("dotnet", [dll, "--urls", url], { cwd, env: { ...process.env, ...env, ASPNETCORE_ENVIRONMENT: "Development", ...extraEnv }, stdio: ["ignore", "pipe", "pipe"] });
  child.stdout.on("data", (d) => { log += d; });
  child.stderr.on("data", (d) => { log += d; });
  const probe = extraEnv.PathBase ? `/${extraEnv.PathBase.replace(/^\/|\/$/g, "")}/` : "/";
  for (let i = 0; i < 80; i++) {
    await new Promise((r) => setTimeout(r, 500));
    try { if ((await fetch(url + probe)).ok) return { url, child, log: () => log }; } catch { /* not up yet */ }
  }
  child.kill();
  throw new Error(`the app did not answer at ${url}${probe}:\n${log}`);
};

const expectedVersion = (() => {
  const props = readFileSync(join(root, "build", "Version.props"), "utf8");
  const prefix = /<VersionPrefix>([^<]+)<\/VersionPrefix>/.exec(props)?.[1].trim();
  const suffix = /<VersionSuffix>([^<]*)<\/VersionSuffix>/.exec(props)?.[1].trim();
  return suffix ? `${prefix}-${suffix}` : prefix;
})();

try {
  console.log(`work dir: ${work}`);

  // 0. The machine must not hand the exported repositories settings a real clone would not have.
  const stray = [];
  for (let dir = work; ; dir = dirname(dir)) {
    for (const f of ["Directory.Build.props", "Directory.Build.targets", "Directory.Packages.props", "Directory.Build.rsp", "nuget.config", "NuGet.Config", "NuGet.config"]) {
      if (existsSync(join(dir, f))) stray.push(join(dir, f));
    }
    if (dirname(dir) === dir) break;
  }
  if (!check(stray.length === 0, `no Directory.Build.*, Directory.Packages.props or nuget.config above ${work}${stray.length ? ` (found ${stray.join(", ")})` : ""}`)) {
    throw new Error("an ancestor folder of the temp directory holds MSBuild or NuGet settings: set TMPDIR to a clean folder");
  }

  // 1. Pack the library to a local feed (Blux.UI only comes from there), as the package check does.
  mkdirSync(feed);
  const pack = run("dotnet", ["pack", "src/Blux.UI", "-c", "Release", "-o", feed, "-warnaserror"], root);
  if (!check(pack.code === 0, "dotnet pack src/Blux.UI -c Release succeeds without warnings")) console.log(pack.out);
  const nupkg = readdirSync(feed).find((f) => /^Blux\.UI\.\d.*\.nupkg$/.test(f));
  if (!check(!!nupkg, ".nupkg produced")) throw new Error("pack produced no package");
  const version = nupkg.replace(/^Blux\.UI\./, "").replace(/\.nupkg$/, "");
  check(version === expectedVersion, `the packed library has the version of build/Version.props (${version} / ${expectedVersion})`);

  // The feed and the sources: the local cache serves the framework packages already restored on this machine, nuget.org the others.
  const cache = process.env.NUGET_PACKAGES || join(homedir(), ".nuget", "packages");
  const nugetConfig = `<configuration><packageSources><clear /><add key="blux-feed" value="${feed}" /><add key="local-cache" value="${cache}" /><add key="nuget.org" value="https://api.nuget.org/v3/index.json" /></packageSources>` +
    `<packageSourceMapping><packageSource key="blux-feed"><package pattern="Blux.UI" /></packageSource><packageSource key="local-cache"><package pattern="*" /></packageSource><packageSource key="nuget.org"><package pattern="*" /></packageSource></packageSourceMapping></configuration>`;
  writeFileSync(join(work, "nuget.config"), nugetConfig);

  for (const template of templates.filter((t) => wanted.length === 0 || wanted.includes(t.name))) {
    console.log(`\n== ${template.name} ==`);
    const repo = exportTemplate(template.name, join(work, template.name, "repo"));
    const files = readdirSync(repo);

    // 2. The export is a repository on its own.
    check(!files.some((f) => /^Directory\.Build\./.test(f)) && !allPaths(repo).some((p) => /Directory\.Build\.(props|targets)$/.test(p)), `${template.name}: no Directory.Build.props or Directory.Build.targets in the export`);
    for (const f of ["README.md", "LICENSE", ".gitignore", "global.json", "Directory.Packages.props", "rename.cs", ".vscode/tasks.json", ".github/workflows/ci.yml"]) {
      check(existsSync(join(repo, f)), `${template.name}: the export has ${f}`);
    }
    check(readdirSync(repo).some((f) => f.endsWith(".slnx")), `${template.name}: the export has a .slnx solution`);
    const forbidden = [
      [/src[\\/]Blux\.UI/, "a path into the library source (src/Blux.UI)"],
      [/BluxRepoRoot/, "BluxRepoRoot"],
      [/\.\.[\\/]\.\.[\\/]\.\./, "a path three levels up (..\\..\\..)"],
      [new RegExp(`(^|[^\\w.])(/home/|/Users/|[A-Za-z]:\\\\)`), "an absolute path"],
      [new RegExp(root.replace(/[.*+?^${}()|[\]\\]/g, "\\$&")), "the path of this repository"],
      [new RegExp(tmpdir().replace(/[.*+?^${}()|[\]\\]/g, "\\$&") + "[\\\\/]blux"), "a temp path"],
    ];
    const texts = textFiles(repo);
    for (const [pattern, what] of forbidden) {
      const hits = texts.filter(({ text }) => pattern.test(text)).map(({ path }) => path.slice(repo.length + 1));
      check(hits.length === 0, `${template.name}: no file has ${what}${hits.length ? ` (${hits.join(", ")})` : ""}`);
    }
    const packages = readFileSync(join(repo, "Directory.Packages.props"), "utf8");
    const pinned = /<PackageVersion Include="Blux\.UI" Version="([^"]+)"/.exec(packages)?.[1];
    check(pinned === expectedVersion, `${template.name}: Blux.UI is pinned to the version of build/Version.props (${pinned} / ${expectedVersion})`);

    // 3. Restore, build (Release, warnings as errors), run.
    const web = join(repo, template.web);
    const built = run("dotnet", ["build", web, "-c", "Release", "-warnaserror"], repo);
    if (!check(built.code === 0, `${template.name}: dotnet build ${template.web} -c Release -warnaserror`)) console.log(built.out);
    const assembly = join(web, "bin", "Release", "net10.0", `${basename(template.web)}.dll`);
    if (built.code === 0) {
      const { utility, css } = template.probe;
      check(!readFileSync(join(root, "src", "Blux.UI", "wwwroot", "blux.css"), "utf8").includes(css), `${template.name}: ${utility} is not in the precompiled blux.css, so finding it proves the template's CSS was compiled`);
      for (const [label, extraEnv, prefix] of [["at the root", {}, ""], ["below a path base", { PathBase: "/demo/starter" }, "/demo/starter"]]) {
        let app;
        try {
          app = await start(assembly, web, extraEnv);
        } catch (error) {
          check(false, `${template.name}: runs ${label}`);
          console.log(error.message);
          continue;
        }
        try {
          for (const page of template.pages) {
            const res = await fetch(`${app.url}${prefix}${page}`);
            check(res.status === 200, `${template.name}: GET ${prefix}${page} returns 200 ${label} (${res.status})`);
            if (page !== "/") continue;
            const html = await res.text();
            const base = /<base href="([^"]*)"/.exec(html)?.[1];
            check(base === (prefix ? `${prefix}/` : "/"), `${template.name}: <base href> is ${prefix ? `${prefix}/` : "/"} ${label} (${base})`);
            const sheet = /<link rel="stylesheet" href="([^"]*app[^"]*\.css)"/.exec(html)?.[1];
            if (!check(!!sheet, `${template.name}: the page links the compiled stylesheet ${label} (${sheet})`)) continue;
            const cssRes = await fetch(new URL(sheet, `${app.url}${prefix}/`));
            const body = await cssRes.text();
            check(cssRes.ok, `${template.name}: the stylesheet is served ${label} (${cssRes.status})`);
            check(body.includes(css), `${template.name}: the stylesheet contains ${utility}, a utility only the template uses ${label}`);
            check(body.includes("--blux-primary"), `${template.name}: the stylesheet has the Blux theme ${label}`);
          }
        } finally {
          app.child.kill();
        }
      }
    }

    // 4. Rename, then build the renamed copy.
    const copy = exportTemplate(template.name, join(work, template.name, "renamed"));
    const renamed = run("dotnet", ["run", "rename.cs", "Acme"], copy);
    if (!check(renamed.code === 0, `${template.name}: dotnet run rename.cs Acme`)) console.log(renamed.out);
    if (renamed.code === 0) {
      check(!existsSync(join(copy, "rename.cs")), `${template.name}: rename.cs deleted itself`);
      check(existsSync(join(copy, "Acme.slnx")), `${template.name}: the solution is Acme.slnx`);
      const left = textFiles(copy).filter(({ text }) => /starter/i.test(text)).map(({ path }) => path.slice(copy.length + 1));
      check(left.length === 0, `${template.name}: no occurrence of the template name is left in a file${left.length ? ` (${left.join(", ")})` : ""}`);
      const leftNames = allPaths(copy).filter((p) => /starter/i.test(p.slice(copy.length))).map((p) => p.slice(copy.length + 1));
      check(leftNames.length === 0, `${template.name}: no file or folder name keeps the template name${leftNames.length ? ` (${leftNames.join(", ")})` : ""}`);
      const maui = readdirSync(join(copy, "src")).filter((d) => d.endsWith(".Maui"));
      if (maui.length) {
        const csproj = readFileSync(join(copy, "src", maui[0], `${maui[0]}.csproj`), "utf8");
        check(csproj.includes("<ApplicationId>com.companyname.acme</ApplicationId>") && csproj.includes("<ApplicationTitle>Acme</ApplicationTitle>") && csproj.includes("<RootNamespace>Acme.Maui</RootNamespace>"), `${template.name}: the MAUI project has the new id, title and root namespace`);
      }
      const rebuilt = run("dotnet", ["build", join(copy, template.renamedWeb), "-c", "Release", "-warnaserror"], copy);
      if (!check(rebuilt.code === 0, `${template.name}: dotnet build ${template.renamedWeb} -c Release -warnaserror after the rename`)) console.log(rebuilt.out);
    }
  }
} catch (error) {
  failures.push(String(error.message ?? error));
  console.error(error);
} finally {
  if (keep) console.log(`kept ${work}`);
  else rmSync(work, { recursive: true, force: true });
}

if (failures.length) {
  console.error(`\n${failures.length} check(s) failed:\n  ${failures.join("\n  ")}`);
  process.exit(1);
}
console.log("\nall template checks passed");
