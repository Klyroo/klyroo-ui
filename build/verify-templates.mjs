// Template check: each project template, exported the way a user gets it, must restore, build and run against a PACKED Lux.UI, with nothing of
// this monorepo around it.
//
//   node build/verify-templates.mjs [--keep] [--no-browser] [template ...]
//
// For every template (all of them by default):
//   1. exports it to a temporary folder with no Directory.Build.*, Directory.Packages.props or nuget.config in any ancestor (the check fails if
//      the machine has one there), and checks the export: no monorepo path, no absolute path, Lux.UI pinned to the version of build/Version.props;
//   2. restores and builds its web project in Release with warnings as errors, from a local feed holding the packed library;
//   3. runs it: / answers 200, the stylesheet it links contains a utility only the template uses (so Tailwind ran), and the app also works
//      below a path base;
//   4. publishes it (`dotnet publish -c Release`) and runs the published app in Production, at the root and below a path base: the same
//      checks, the framework files are served, and a real headless browser (the E2E project, tests/Lux.UI.E2E/TemplateTests.cs) sees the
//      page become interactive, the counter increment, and the Auto mode hand off to WebAssembly (Starter), or every page of the shell, the home figures, the customers table (search, sort, filter, selection, new customer), the inbox, the settings and the command palette work (Dashboard), or the sections, the pricing switch, the accordion and the color mode work with no circuit (Landing), or the pages, navigation, table of contents, search and copy buttons work with no circuit (Docs), or the landing page, the pricing switch and table, the blog with its tags, the docs, the changelog, the search of all three and the UI-only sign-in and sign-up forms work with no circuit (SaaS), or the home page, the work list and a project page, the about timeline, the writing list with its tags and an article, the speaking page, the search and the UI-only contact form work with no circuit (Portfolio), or the list, the pagination, a release page, the feed and the copy buttons work with no circuit and no network (Changelog: the sample releases, Changelog:Source=File) (--no-browser skips that part);
//   5. renames a second export with `rename.cs`, to a plain name (Acme) and to one that contains the template name in another case
//      (starterApp), builds both, checks that invalid names are refused without touching anything, and that a rename that failed half way
//      is finished by running it again.
// The MAUI heads are not built here: they need workloads, so the `maui` job of the CI workflow builds them.
//
// Needs the .NET 10 SDK and the network (the first build downloads the Tailwind CLI, the fonts and the icons of the template, and NuGet restores
// the framework packages from nuget.org). The pack output is a folder feed; Lux.UI comes only from it, through a global-packages folder of this
// run. The browser check needs Chromium installed for Playwright (pwsh tests/Lux.UI.E2E/bin/Debug/net10.0/playwright.ps1 install chromium).
import { spawn, spawnSync } from "node:child_process";
import { chmodSync, existsSync, mkdirSync, mkdtempSync, readFileSync, readdirSync, rmSync, statSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { basename, dirname, join, resolve, sep } from "node:path";
import { fileURLToPath } from "node:url";
import { exportTemplate } from "./export-template.mjs";

// What each template promises: its web project, and a Tailwind utility that only its own pages use (it is in no precompiled Blux stylesheet).
const templates = [
  {
    name: "starter",
    web: "src/Starter.Web",
    name_in_files: "Starter",
    renames: ["Acme", "starterApp"],
    probe: { utility: "max-w-[37rem]", css: "max-width:37rem" },
    pages: ["/", "/about"],
    // The setup method of the shared project, named after the template.
    services: true,
    browserClass: "Lux.UI.E2E.TemplateTests",
  },
  {
    name: "dashboard",
    web: "src/Dashboard.Web",
    name_in_files: "Dashboard",
    renames: ["Acme", "dashboardApp"],
    probe: { utility: "max-w-[72rem]", css: "max-width:72rem" },
    // Lux.UI.Tools has no stylesheet of its own: its classes must be in the one the template compiles (a rule only the data list writes).
    toolsProbe: "webkit-search-cancel-button",
    pages: ["/", "/customers", "/inbox", "/inbox/3", "/settings", "/settings/members", "/settings/notifications", "/settings/security"],
    services: true,
    // Blux's own components (BxDashboardGroup, BxDashboardSidebar...) are not the template name: rename.cs leaves them alone.
    keep: /BxDashboard[A-Za-z]*/g,
    browserClass: "Lux.UI.E2E.DashboardTemplateTests",
  },
  {
    name: "landing",
    web: "src/Landing.Web",
    name_in_files: "Landing",
    renames: ["Acme", "landingApp"],
    probe: { utility: "max-w-[27rem]", css: "max-width:27rem" },
    pages: ["/"],
    browserClass: "Lux.UI.E2E.LandingTemplateTests",
  },
  {
    name: "docs",
    web: "src/Docs.Web",
    name_in_files: "Docs",
    renames: ["Acme", "docsApp"],
    probe: { utility: "max-w-[29rem]", css: "max-width:29rem" },
    pages: ["/", "/getting-started/installation", "/guides/configuration"],
    browserClass: "Lux.UI.E2E.DocsTemplateTests",
  },
  {
    name: "saas",
    web: "src/SaaS.Web",
    name_in_files: "SaaS",
    renames: ["Acme", "saasApp"],
    probe: { utility: "max-w-[28rem]", css: "max-width:28rem" },
    pages: ["/", "/pricing", "/blog", "/blog/grace-periods-without-the-guesswork", "/docs", "/docs/getting-started/quick-start", "/changelog", "/changelog/2-0", "/login", "/signup"],
    browserClass: "Lux.UI.E2E.SaasTemplateTests",
  },
  {
    name: "portfolio",
    web: "src/Portfolio.Web",
    name_in_files: "Portfolio",
    renames: ["Acme", "portfolioApp"],
    probe: { utility: "max-w-[26rem]", css: "max-width:26rem" },
    pages: ["/", "/work", "/work/quire", "/about", "/writing", "/writing/spacing-scales-with-fewer-steps", "/speaking", "/contact", "/uses"],
    browserClass: "Lux.UI.E2E.PortfolioTemplateTests",
  },
  {
    name: "changelog",
    web: "src/Changelog.Web",
    name_in_files: "Changelog",
    renames: ["Acme", "changelogApp"],
    probe: { utility: "max-w-[46rem]", css: "max-width:46rem" },
    pages: ["/", "/?page=2", "/releases/v1.4.0", "/feed.xml"],
    // No network: the releases are the sample ones of the template, with a repository for the links to point at.
    env: { Changelog__Source: "File", Changelog__Repository: "tern-cli/tern" },
    // Blux's own components (BxChangelogVersions) are not the template name: rename.cs leaves them alone.
    keep: /Bx(?:Changelog)Version(?:s)?/g,
    browserClass: "Lux.UI.E2E.ChangelogTemplateTests",
  },
  {
    name: "chat",
    web: "src/Chat.Web",
    name_in_files: "Chat",
    renames: ["Acme", "chatApp"],
    probe: { utility: "max-w-[34rem]", css: "max-width:34rem" },
    pages: ["/", "/c/0123456789abcdef0123456789abcdef"],
    services: true,
    // The simulated provider: no key, no network. (The template itself defaults to a real service that needs a key.)
    env: { Assistant__Provider: "Simulated" },
    // Blux's own chat components, types and element attributes (BxChatMessages, ChatRole, ChatMessagesUi, data-bx-chat-input) are not the template name: rename.cs leaves them alone.
    keep: /chat\/completions|(?:Bx)?Chat(?:Role|Status|Side|Message|Messages|Prompt|Chevron|Tool|Attachment|Palette|Reasoning|Shimmer)[A-Za-z]*|bx-chat-[a-z-]*|data-bx-chat[a-z-]*/g,
    browserClass: "Lux.UI.E2E.ChatTemplateTests",
  },
  {
    name: "editor",
    web: "src/Editor.Web",
    name_in_files: "Editor",
    renames: ["Acme", "editorApp"],
    // A class that appears only in the markup (Home.razor, DocumentPage.razor): the .document-column rule of app.css writes max-width:46rem by hand, so it would prove nothing about Tailwind scanning the Razor files.
    probe: { utility: "max-w-[34rem]", css: "max-width:34rem" },
    pages: ["/", "/d/0123456789abcdef0123456789abcdef"],
    services: true,
    // Blux's own editor components, types and elements (BxEditor, EditorAction, EditorToolbarItem, bx-editor) are not the template name: rename.cs leaves them alone.
    keep: /Sanitize(?=Editor)Editor[A-Za-z]*|(?<!Add)(?:Bx)?Editor(?:Action|Toolbar|Ui|Suggestion|Mention|Emoji|DragHandle|Menu)[A-Za-z]*|Bx(?:Editor)[A-Za-z]*|bx-editor[a-z-]*/g,
    browserClass: "Lux.UI.E2E.EditorTemplateTests",
  },
];

// templates/_shared holds what the templates copy (the Markdown content code); it is not a template. Every other folder is, and must be listed above.
const templateFolders = readdirSync(join(dirname(fileURLToPath(import.meta.url)), "..", "templates"), { withFileTypes: true })
  .filter((entry) => entry.isDirectory() && !entry.name.startsWith("_"))
  .map((entry) => entry.name);
const unlisted = templateFolders.filter((folder) => !templates.some((t) => t.name === folder));
if (unlisted.length) {
  console.error(`templates/${unlisted.join(", templates/")} is not in the list of build/verify-templates.mjs`);
  process.exit(1);
}

// A source file that a .gitignore swallows never reaches the repository, and the template then builds on the machine of its author only (this happened with a
// folder named Releases, which the Visual Studio pattern [Rr]eleases/ ignores). So no file of a template may be ignored by git, and no folder of a template
// may be named after a pattern that would ignore it. Build output and tool folders are ignored on purpose.
const repoRoot = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const generatedFolders = new Set(["bin", "obj", "node_modules", "TestResults", ".vs"]);
const underGenerated = (path) => path.split(/[\\/]/).some((segment) => generatedFolders.has(segment));
const git = (args, input) => {
  const r = spawnSync("git", args, { cwd: repoRoot, encoding: "utf8", input, maxBuffer: 1 << 28 });
  return { code: r.status ?? 1, out: r.stdout ?? "", err: r.stderr ?? "" };
};
const ignoredSources = [];
for (const folder of templateFolders) {
  const listed = git(["ls-files", "--others", "--ignored", "--exclude-standard", "--", `templates/${folder}`]);
  if (listed.code !== 0) {
    console.error(`git ls-files failed for templates/${folder}: ${listed.err.trim()}`);
    process.exit(1);
  }
  ignoredSources.push(...listed.out.split("\n").filter((file) => file && !underGenerated(file)));
  // The folders by name, whether or not a file sits in them yet: --no-index matches the patterns without looking at what is tracked.
  const directories = [];
  const walk = (dir) => {
    for (const entry of readdirSync(dir, { withFileTypes: true })) {
      if (!entry.isDirectory() || entry.name === ".git" || generatedFolders.has(entry.name)) continue;
      const path = join(dir, entry.name);
      directories.push(`${path.slice(repoRoot.length + 1).split(sep).join("/")}/`);
      walk(path);
    }
  };
  walk(join(repoRoot, "templates", folder));
  if (directories.length) {
    const ignored = git(["check-ignore", "--no-index", "--stdin"], directories.join("\n"));
    ignoredSources.push(...ignored.out.split("\n").filter(Boolean).map((path) => `${path} (folder)`));
  }
}
if (ignoredSources.length) {
  console.error("These files or folders of a template are ignored by git, so they would never be committed:");
  for (const file of [...new Set(ignoredSources)]) console.error(`  ${file}`);
  console.error("Rename the folder (for instance Releases becomes ReleaseNotes) or fix the .gitignore.");
  process.exit(1);
}

const keep = process.argv.includes("--keep");
const browser = !process.argv.includes("--no-browser");
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

/** Runs a command with the environment of the developer (their NuGet cache), for the repository's own projects. */
const runPlain = (cmd, args, cwd, extraEnv = {}) => {
  const r = spawnSync(cmd, args, { cwd, encoding: "utf8", env: { ...process.env, DOTNET_NOLOGO: "1", DOTNET_CLI_TELEMETRY_OPTOUT: "1", ...extraEnv }, maxBuffer: 1 << 28 });
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

  // 1. Pack the library to a local feed (Lux.UI only comes from there), as the package check does.
  mkdirSync(feed);
  const pack = run("dotnet", ["pack", "src/Lux.UI", "-c", "Release", "-o", feed, "-warnaserror"], root);
  if (!check(pack.code === 0, "dotnet pack src/Lux.UI -c Release succeeds without warnings")) console.log(pack.out);
  // The data list packages too, for the templates that use it (Dashboard): Lux.UI.Tools.Query first, Lux.UI.Tools depends on it and on Lux.UI at exactly this version.
  for (const project of ["src/Lux.UI.Tools.Query", "src/Lux.UI.Tools", "src/Lux.UI.Server"]) {
    const packed = run("dotnet", ["pack", project, "-c", "Release", "-o", feed, "-warnaserror"], root);
    if (!check(packed.code === 0, `dotnet pack ${project} -c Release succeeds without warnings`)) console.log(packed.out);
  }
  const nupkg = readdirSync(feed).find((f) => /^Blux\.UI\.\d.*\.nupkg$/.test(f));
  if (!check(!!nupkg, ".nupkg produced")) throw new Error("pack produced no package");
  const version = nupkg.replace(/^Blux\.UI\./, "").replace(/\.nupkg$/, "");
  check(version === expectedVersion, `the packed library has the version of build/Version.props (${version} / ${expectedVersion})`);

  // The sources: the feed for Lux.UI and nuget.org for everything else, with source mapping. The developer's global packages folder is not a source.
  const nugetConfig = `<configuration><packageSources><clear /><add key="blux-feed" value="${feed}" /><add key="nuget.org" value="https://api.nuget.org/v3/index.json" /></packageSources>` +
    `<packageSourceMapping><packageSource key="blux-feed"><package pattern="Lux.UI" /><package pattern="Lux.UI.Tools" /><package pattern="Lux.UI.Tools.Query" /><package pattern="Lux.UI.Server" /></packageSource><packageSource key="nuget.org"><package pattern="*" /></packageSource></packageSourceMapping></configuration>`;
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
      [/src[\\/]Blux\.UI/, "a path into the library source (src/Lux.UI)"],
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
    check(pinned === expectedVersion, `${template.name}: Lux.UI is pinned to the version of build/Version.props (${pinned} / ${expectedVersion})`);
    const serverPinned = /<PackageVersion Include="Blux\.UI\.Server" Version="([^"]+)"/.exec(packages)?.[1];
    if (serverPinned !== undefined) check(serverPinned === expectedVersion, `${template.name}: Lux.UI.Server is pinned to the version of build/Version.props, the one of Lux.UI (${serverPinned} / ${expectedVersion})`);
    const toolsPinned = /<PackageVersion Include="Blux\.UI\.Tools" Version="([^"]+)"/.exec(packages)?.[1];
    if (toolsPinned !== undefined) check(toolsPinned === expectedVersion, `${template.name}: Lux.UI.Tools is pinned to the version of build/Version.props, the one of Lux.UI (${toolsPinned} / ${expectedVersion})`);

    // 3. Restore, build (Release, warnings as errors), run.
    const web = join(repo, template.web);
    const built = run("dotnet", ["build", web, "-c", "Release", "-warnaserror"], repo);
    if (!check(built.code === 0, `${template.name}: dotnet build ${template.web} -c Release -warnaserror`)) console.log(built.out);
    const assembly = join(web, "bin", "Release", "net10.0", `${basename(template.web)}.dll`);
    if (built.code === 0) {
      const { utility, css } = template.probe;
      check(!readFileSync(join(root, "src", "Lux.UI", "wwwroot", "lux.css"), "utf8").includes(css), `${template.name}: ${utility} is not in the precompiled lux.css, so finding it proves the template's CSS was compiled`);
      for (const [label, extraEnv, prefix] of [["at the root", {}, ""], ["below a path base", { PathBase: `/demo/${template.name}` }, `/demo/${template.name}`]]) {
        let app;
        try {
          app = await start(assembly, web, { ...template.env, ...extraEnv });
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
            if (template.toolsProbe) check(body.includes(template.toolsProbe), `${template.name}: the stylesheet has the classes of Lux.UI.Tools (${template.toolsProbe}) ${label}`);
          }
        } finally {
          app.child.kill();
        }
      }
    }

    // 4. Publish, run the published app in Production.
    const published = join(work, template.name, "published");
    const publish = run("dotnet", ["publish", web, "-c", "Release", "-warnaserror", "-o", published], repo);
    if (!check(publish.code === 0, `${template.name}: dotnet publish ${template.web} -c Release -warnaserror`)) console.log(publish.out);
    if (publish.code === 0) {
      const publishedDll = join(published, `${basename(template.web)}.dll`);
      const { utility, css } = template.probe;
      for (const [label, extraEnv, prefix] of [["at the root", {}, ""], ["below a path base", { PathBase: `/demo/${template.name}` }, `/demo/${template.name}`]]) {
        let app;
        try {
          app = await start(publishedDll, published, { ...template.env, ...extraEnv, ASPNETCORE_ENVIRONMENT: "Production" });
        } catch (error) {
          check(false, `${template.name}: the published app runs in Production ${label}`);
          console.log(error.message);
          continue;
        }
        try {
          for (const page of template.pages) {
            const res = await fetch(`${app.url}${prefix}${page}`);
            check(res.status === 200, `${template.name}: published, GET ${prefix}${page} returns 200 in Production ${label} (${res.status})`);
            if (page !== "/") continue;
            const html = await res.text();
            const sheet = /<link rel="stylesheet" href="([^"]*app[^"]*\.css)"/.exec(html)?.[1];
            const cssRes = sheet ? await fetch(new URL(sheet, `${app.url}${prefix}/`)) : null;
            check(!!cssRes?.ok && (await cssRes.text()).includes(css), `${template.name}: published, the stylesheet is served with ${utility} in Production ${label}`);
            const script = /<script src="([^"]*blazor\.web[^"]*\.js)"/.exec(html)?.[1];
            const scriptRes = script ? await fetch(new URL(script, `${app.url}${prefix}/`)) : null;
            check(!!scriptRes?.ok && /javascript/.test(scriptRes.headers.get("content-type") ?? ""), `${template.name}: published, the Blazor script is served ${label} (${script})`);
          }
        } finally {
          app.child.kill();
        }
      }
      if (browser) {
        // A real browser: the E2E project of this repository, pointed at the published folder.
        const e2e = { BLUX_TEMPLATE_PUBLISHED: published, BLUX_TEMPLATE_ASSEMBLY: `${basename(template.web)}.dll`, BLUX_TEMPLATE_PROBE: template.probe.css };
        const e2eBuild = runPlain("dotnet", ["build", "tests/Lux.UI.E2E"], root);
        if (!check(e2eBuild.code === 0, `${template.name}: tests/Lux.UI.E2E builds`)) console.log(e2eBuild.out);
        if (e2eBuild.code === 0) {
          const browserRun = runPlain("dotnet", ["test", "--project", "tests/Lux.UI.E2E", "--no-build", "--", "--filter-class", template.browserClass], root, e2e);
          const count = (label) => Number(new RegExp(`${label}: (\\d+)`).exec(browserRun.out)?.[1] ?? NaN);
          const [total, passed, failed, skipped] = [count("total"), count("succeeded"), count("failed"), count("skipped")];
          if (!check(browserRun.code === 0 && total > 0 && failed === 0 && skipped === 0 && passed === total, `${template.name}: in a browser, the published app passes ${template.browserClass} (root and path base): ${passed} of ${total} tests passed, ${failed} failed, ${skipped} skipped`)) console.log(browserRun.out);
        }
      }
    }

    // 5. Rename. Names the script must refuse change nothing.
    const refused = exportTemplate(template.name, join(work, template.name, "refused"));
    for (const bad of ["System", "Task", "Microsoft", "Blux", "my_app", "9lives", "Web", "class", template.name_in_files]) {
      const r = run("dotnet", ["run", "rename.cs", bad], refused);
      const untouched = spawnSync("git", ["status", "--porcelain"], { cwd: refused, encoding: "utf8" }).stdout.trim() === "";
      check(r.code !== 0 && /rename: /.test(r.out) && untouched && existsSync(join(refused, "rename.cs")), `${template.name}: rename.cs refuses '${bad}' and changes nothing`);
    }
    for (const newName of template.renames) {
      const lower = newName.toLowerCase();
      const copy = exportTemplate(template.name, join(work, template.name, `renamed-${newName}`));
      const renamed = run("dotnet", ["run", "rename.cs", newName], copy);
      if (!check(renamed.code === 0, `${template.name}: dotnet run rename.cs ${newName}`)) console.log(renamed.out);
      if (renamed.code !== 0) continue;
      check(!existsSync(join(copy, "rename.cs")) && !existsSync(join(copy, ".rename-state")), `${template.name}: ${newName}: rename.cs and its state file are gone`);
      check(existsSync(join(copy, `${newName}.slnx`)), `${template.name}: ${newName}: the solution is ${newName}.slnx`);
      check(renamed.out.includes(`dotnet run --project src/${newName}.Web`) && existsSync(join(copy, "src", `${newName}.Web`)), `${template.name}: ${newName}: the hint points to a folder that exists`);
      // What is left of the template name once the new name (which may contain it) is taken out.
      const strip = (text) => text.replace(template.keep ?? /$^/, "").replace(new RegExp(newName, "gi"), "");
      const left = textFiles(copy).filter(({ text }) => new RegExp(template.name, "i").test(strip(text))).map(({ path }) => path.slice(copy.length + 1));
      check(left.length === 0, `${template.name}: ${newName}: no occurrence of the template name is left in a file${left.length ? ` (${left.join(", ")})` : ""}`);
      const leftNames = allPaths(copy).filter((p) => new RegExp(template.name, "i").test(strip(p.slice(copy.length)))).map((p) => p.slice(copy.length + 1));
      check(leftNames.length === 0, `${template.name}: ${newName}: no file or folder name keeps the template name${leftNames.length ? ` (${leftNames.join(", ")})` : ""}`);
      // The names the template builds from its own name, never one the other spellings rewrote twice.
      if (template.services) {
        const services = textFiles(copy).find(({ path }) => path.endsWith(`${sep}${newName}Services.cs`));
        check(!!services && services.text.includes(`Add${newName}Ui`) && services.text.includes(`namespace ${newName}.Shared;`), `${template.name}: ${newName}: the setup method is Add${newName}Ui in the namespace ${newName}.Shared`);
      }
      const maui = readdirSync(join(copy, "src")).filter((d) => d.endsWith(".Maui"));
      if (maui.length) {
        const csproj = readFileSync(join(copy, "src", maui[0], `${maui[0]}.csproj`), "utf8");
        check(csproj.includes(`<ApplicationId>com.companyname.${lower}</ApplicationId>`) && csproj.includes(`<ApplicationTitle>${newName}</ApplicationTitle>`) && csproj.includes(`<RootNamespace>${newName}.Maui</RootNamespace>`), `${template.name}: ${newName}: the MAUI project has the new id, title and root namespace`);
      }
      const renamedWeb = template.web.replace(template.name_in_files, newName);
      const rebuilt = run("dotnet", ["build", join(copy, renamedWeb), "-c", "Release", "-warnaserror"], copy);
      if (!check(rebuilt.code === 0, `${template.name}: dotnet build ${renamedWeb} -c Release -warnaserror after the rename to ${newName}`)) console.log(rebuilt.out);
    }

    // A rename that fails half way (a folder that cannot be written) says so, and the same command finishes it without renaming twice, even
    // for a name that contains the template name. Not on Windows (no chmod) and not as root (it ignores permissions).
    if (process.platform !== "win32" && process.getuid?.() !== 0) {
      const partial = exportTemplate(template.name, join(work, template.name, "partial"));
      const src = join(partial, "src");
      const newName = `My${template.name_in_files}`;
      chmodSync(src, 0o555);
      const first = run("dotnet", ["run", "rename.cs", newName], partial);
      chmodSync(src, 0o755);
      check(first.code !== 0 && /could not rename 'src\/[^']+' to 'src\/[^']+'/.test(first.out) && /partly renamed/.test(first.out) && first.out.includes(`dotnet run rename.cs ${newName}`), `${template.name}: a rename that fails half way names the path, says the repository is partly renamed and how to continue`);
      check(existsSync(join(partial, ".rename-state")) && existsSync(join(partial, "rename.cs")), `${template.name}: the progress is kept for the next run`);
      const second = run("dotnet", ["run", "rename.cs", newName], partial);
      check(second.code === 0, `${template.name}: running the same command again finishes the rename`);
      if (second.code !== 0) console.log(second.out);
      check(existsSync(join(partial, "src", `${newName}.Web`)) && !existsSync(join(partial, "src", `${template.name_in_files}.Web`)) && !existsSync(join(partial, "rename.cs")) && !existsSync(join(partial, ".rename-state")), `${template.name}: after the second run every folder has the new name`);
      const twice = textFiles(partial).filter(({ text }) => text.includes(`My${newName}`) || text.includes(`my${newName.toLowerCase()}`)).map(({ path }) => path.slice(partial.length + 1));
      check(twice.length === 0 && !allPaths(partial).some((p) => p.includes(`My${newName}`)), `${template.name}: nothing was renamed twice${twice.length ? ` (${twice.join(", ")})` : ""}`);
    } else {
      console.log("skip  partial rename check (needs a POSIX file system and a user that is not root)");
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
