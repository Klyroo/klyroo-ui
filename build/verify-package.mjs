// Packaging check: packs Blux.UI (and the image packages Blux.UI.Images and Blux.UI.Images.Local) in Release, inspects the .nupkg and
// .snupkg files, then builds a throwaway Blazor Web App (Auto interactivity) that consumes the packages from a local folder feed, in both CSS modes.
//
//   node build/verify-package.mjs [--online] [--keep]
//
// Offline by default: the consumer restores from the pack output folder plus the local NuGet cache (a global-packages
// folder is a valid folder feed), with nuget.org removed. If a framework package is missing from the cache, pass
// --online to add nuget.org as a source. Needs the .NET 10 SDK and the committed generated assets (npm run build).
import { spawnSync } from "node:child_process";
import { inflateRawSync } from "node:zlib";
import { existsSync, mkdirSync, mkdtempSync, readFileSync, readdirSync, rmSync, writeFileSync } from "node:fs";
import { homedir, tmpdir } from "node:os";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const online = process.argv.includes("--online");
const keep = process.argv.includes("--keep");
const root = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const work = mkdtempSync(join(tmpdir(), "blux-pkgcheck-"));
const feed = join(work, "feed");
const failures = [];
const check = (ok, what) => { console.log(`${ok ? "ok  " : "FAIL"} ${what}`); if (!ok) failures.push(what); return ok; };

const run = (cmd, args, cwd, env = {}) => {
  const r = spawnSync(cmd, args, { cwd, encoding: "utf8", env: { ...process.env, DOTNET_NOLOGO: "1", DOTNET_CLI_TELEMETRY_OPTOUT: "1", ...env }, maxBuffer: 1 << 28 });
  return { code: r.status ?? 1, out: `${r.stdout ?? ""}${r.stderr ?? ""}` };
};

// Minimal zip reader (central directory + raw deflate): no dependency, works on every OS.
const readZip = (file) => {
  const buf = readFileSync(file);
  let eocd = buf.length - 22;
  while (eocd >= 0 && buf.readUInt32LE(eocd) !== 0x06054b50) eocd--;
  const count = buf.readUInt16LE(eocd + 10);
  let p = buf.readUInt32LE(eocd + 16);
  const entries = new Map();
  for (let i = 0; i < count; i++) {
    const method = buf.readUInt16LE(p + 10), size = buf.readUInt32LE(p + 20);
    const nameLen = buf.readUInt16LE(p + 28), extraLen = buf.readUInt16LE(p + 30), commentLen = buf.readUInt16LE(p + 32);
    const local = buf.readUInt32LE(p + 42);
    const name = decodeURIComponent(buf.toString("utf8", p + 46, p + 46 + nameLen));
    entries.set(name, () => {
      const start = local + 30 + buf.readUInt16LE(local + 26) + buf.readUInt16LE(local + 28);
      const data = buf.subarray(start, start + size);
      return method === 0 ? data : inflateRawSync(data);
    });
    p += 46 + nameLen + extraLen + commentLen;
  }
  return entries;
};

try {
  console.log(`work dir: ${work}`);

  // 1. Pack, treating warnings as errors: an empty repository URL must not warn.
  mkdirSync(feed);
  const pack = run("dotnet", ["pack", "src/Blux.UI", "-c", "Release", "-o", feed, "-warnaserror"], root);
  if (!check(pack.code === 0, "dotnet pack -c Release succeeds without warnings")) console.log(pack.out);
  const nupkgName = readdirSync(feed).find((f) => /^Blux\.UI\.\d.*\.nupkg$/.test(f));
  const snupkgName = readdirSync(feed).find((f) => /^Blux\.UI\.\d.*\.snupkg$/.test(f));
  check(!!nupkgName, ".nupkg produced");
  check(!!snupkgName, ".snupkg produced");
  if (!nupkgName || !snupkgName) throw new Error("pack produced no package");
  const version = nupkgName.replace(/^Blux\.UI\./, "").replace(/\.nupkg$/, "");
  console.log(`version: ${version}`);

  // 2. .nupkg contents.
  const nupkg = readZip(join(feed, nupkgName));
  const names = [...nupkg.keys()];
  for (const n of ["lib/net10.0/Blux.UI.dll", "lib/net10.0/Blux.UI.xml", "staticwebassets/blux.css", "staticwebassets/blux.preflight.css",
    "staticwebassets/blux-editor.js", "staticwebassets/blux.theme.css",
    "buildTransitive/Blux.UI.targets", "build/Blux.UI.targets", "README.md", "CHANGELOG.md", "icon.png", "Blux.UI.nuspec"]) {
    check(nupkg.has(n), `nupkg contains ${n}`);
  }
  check(names.some((n) => /^staticwebassets\/Blux\.UI(\.[a-z0-9]+)?\.lib\.module\.js$/.test(n)), "nupkg contains the JS initializer (Blux.UI.lib.module.js, fingerprinted)");
  check(names.some((n) => /^build\/.*StaticWebAssets/i.test(n)), "nupkg declares its static web assets for consumers (build/*StaticWebAssets*)");
  check(!names.some((n) => /^(js|styles)\//.test(n)), "sources of generated assets (js/, styles/) are not shipped");
  check(!names.some((n) => n.endsWith(".pdb")), "no .pdb in the .nupkg (symbols go to the .snupkg)");
  const nuspec = nupkg.has("Blux.UI.nuspec") ? nupkg.get("Blux.UI.nuspec")().toString("utf8") : "";
  check(/<license type="expression">MIT<\/license>/.test(nuspec), "license expression is MIT");
  check(/<readme>README\.md<\/readme>/.test(nuspec), "nuspec points to README.md");
  check(/<icon>icon\.png<\/icon>/.test(nuspec), "nuspec points to icon.png");
  check(/<repository type="git"/.test(nuspec), "repository type is git");
  check(!/<projectUrl>\s*<\/projectUrl>/.test(nuspec), "no empty projectUrl element");
  check(/<releaseNotes>[^<]*CHANGELOG\.md/.test(nuspec), "release notes point to CHANGELOG.md");
  if (nupkg.has("icon.png")) {
    const png = nupkg.get("icon.png")();
    const w = png.readUInt32BE(16), h = png.readUInt32BE(20);
    check(png.subarray(1, 4).toString() === "PNG" && w === h && w >= 128, `icon is a square PNG of at least 128px (${w}x${h})`);
  }
  console.log(`nupkg entries:\n${names.map((n) => `  ${n}`).join("\n")}`);

  // 3. .snupkg contents.
  const snupkg = readZip(join(feed, snupkgName));
  check(snupkg.has("lib/net10.0/Blux.UI.pdb"), "snupkg contains lib/net10.0/Blux.UI.pdb");
  check([...snupkg.keys()].every((n) => !n.endsWith(".dll")), "snupkg contains no dll");

  // 3b. The image packages: same version, readme, icon, documentation, and the dependencies they promise.
  for (const id of ["Blux.UI.Images", "Blux.UI.Images.Local"]) {
    const imagePack = run("dotnet", ["pack", `src/${id}`, "-c", "Release", "-o", feed, "-warnaserror"], root);
    if (!check(imagePack.code === 0, `${id}: dotnet pack -c Release succeeds without warnings`)) console.log(imagePack.out);
    const file = `${id}.${version}.nupkg`;
    if (!check(existsSync(join(feed, file)), `${id}: .nupkg has the version of Blux.UI (${version})`)) continue;
    check(existsSync(join(feed, `${id}.${version}.snupkg`)), `${id}: .snupkg produced`);
    const pkg = readZip(join(feed, file));
    for (const n of [`lib/net10.0/${id}.dll`, `lib/net10.0/${id}.xml`, "README.md", "icon.png", `${id}.nuspec`]) check(pkg.has(n), `${id}: nupkg contains ${n}`);
    check([...pkg.keys()].every((n) => !n.endsWith(".pdb")), `${id}: no .pdb in the .nupkg`);
    const spec = pkg.has(`${id}.nuspec`) ? pkg.get(`${id}.nuspec`)() .toString("utf8") : "";
    check(/<license type="expression">MIT<\/license>/.test(spec), `${id}: license expression is MIT`);
    // The local optimizer reaches Blux.UI through Blux.UI.Images.
    const dependency = id.endsWith(".Local") ? "Blux\\.UI\\.Images" : "Blux\\.UI";
    check(new RegExp(`<dependency id="${dependency}"[^>]*version="\\[?${version.replace(/\./g, "\\.")}`).test(spec), `${id}: depends on ${dependency.replace(/\\/g, "")} ${version}`);
    if (id.endsWith(".Local")) {
      check(/<dependency id="NetVips" /.test(spec) && /<dependency id="NetVips\.Native" /.test(spec), `${id}: depends on NetVips and NetVips.Native`);
      check(/<frameworkReference name="Microsoft\.AspNetCore\.App"/.test(spec), `${id}: uses the ASP.NET Core shared framework`);
      check(/LGPL/.test(pkg.get("README.md")().toString("utf8")), `${id}: the readme mentions the LGPL licence of libvips`);
    }
  }

  // 4. Consumer app, from the folder feed and the local NuGet cache.
  const cache = process.env.NUGET_PACKAGES || join(homedir(), ".nuget", "packages");
  const app = join(work, "app");
  mkdirSync(app);
  writeFileSync(join(app, "global.json"), JSON.stringify({ sdk: { version: "10.0.100", rollForward: "latestFeature" } }));
  const sources = [`<add key="blux-feed" value="${feed}" />`, `<add key="local-cache" value="${cache}" />`];
  if (online) sources.push(`<add key="nuget.org" value="https://api.nuget.org/v3/index.json" />`);
  // Blux.UI comes only from the pack output: the local cache may hold an older build of the same version, which would shadow it.
  // The consumer also gets its own empty global-packages folder (NUGET_PACKAGES) for the same reason.
  const mapping = `<packageSourceMapping><packageSource key="blux-feed"><package pattern="Blux.UI" /><package pattern="Blux.UI.Images" /><package pattern="Blux.UI.Images.Local" /></packageSource><packageSource key="local-cache"><package pattern="*" /></packageSource>${online ? `<packageSource key="nuget.org"><package pattern="*" /></packageSource>` : ""}</packageSourceMapping>`;
  writeFileSync(join(app, "nuget.config"), `<configuration><packageSources><clear />${sources.join("")}</packageSources>${mapping}</configuration>`);
  const consumerEnv = { NUGET_PACKAGES: join(work, "consumer-packages") };
  const created = run("dotnet", ["new", "blazor", "-n", "Site", "-f", "net10.0", "--interactivity", "Auto", "--no-restore"], app, consumerEnv);
  if (!check(created.code === 0, "dotnet new blazor (Auto)")) { console.log(created.out); throw new Error("template failed"); }
  const server = join(app, "Site", "Site"), client = join(app, "Site", "Site.Client");
  // The server also uses the local image optimizer; the client only needs the URL builder.
  for (const [proj, extra] of [[join(server, "Site.csproj"), "Blux.UI.Images.Local"], [join(client, "Site.Client.csproj"), "Blux.UI.Images"]]) {
    writeFileSync(proj, readFileSync(proj, "utf8").replace("</Project>", `  <ItemGroup><PackageReference Include="Blux.UI" Version="${version}" /><PackageReference Include="${extra}" Version="${version}" /></ItemGroup>\n</Project>`));
  }
  const prog = join(server, "Program.cs");
  writeFileSync(prog, readFileSync(prog, "utf8").replace("var app = builder.Build();", "builder.Services.AddBluxUi();\nbuilder.Services.AddBluxImagesLocal();\n\nvar app = builder.Build();\napp.MapBluxImages();").replace("using Site.Components;", "using Site.Components;\nusing Blux.UI;"));
  const home = join(server, "Components", "Pages", "Home.razor");
  writeFileSync(home, `${readFileSync(home, "utf8")}\n@using Blux.UI\n<BxButton Label="Save" />\n<BxSwitch Label="Notify" />\n`);
  const appRazor = join(server, "Components", "App.razor");
  const original = readFileSync(appRazor, "utf8");
  const themeCss = readFileSync(join(root, "src", "Blux.UI", "wwwroot", "blux.theme.css"), "utf8");
  check(nupkg.has("staticwebassets/blux.theme.css") && nupkg.get("staticwebassets/blux.theme.css")().toString("utf8") === themeCss, "packed blux.theme.css equals the committed one");

  const build = (label) => {
    const r = run("dotnet", ["build", "-c", "Release", join(app, "Site", "Site.sln")], app, consumerEnv);
    check(r.code === 0, `${label}: consumer builds`);
    if (r.code !== 0) console.log(r.out);
    check(!/warning[^\n]*Blux/i.test(r.out), `${label}: no Blux.UI warning in the consumer build`);
  };

  // Mode A: precompiled CSS.
  writeFileSync(appRazor, original.replace(/ *<link rel="stylesheet" href="@Assets\["lib\/bootstrap[^\n]*\n/, `    <link rel="stylesheet" href="@Assets["_content/Blux.UI/blux.preflight.css"]" />\n    <link rel="stylesheet" href="@Assets["_content/Blux.UI/blux.css"]" />\n`));
  build("precompiled mode");
  const manifestFile = join(server, "obj", "Release", "net10.0", "staticwebassets.build.json");
  const manifest = existsSync(manifestFile) ? readFileSync(manifestFile, "utf8") : "";
  for (const f of ["blux.css", "blux.preflight.css", "Blux.UI.lib.module.js", "blux-editor.js"]) {
    check(manifest.includes(`_content/Blux.UI/${f}`), `precompiled mode: static web asset _content/Blux.UI/${f}`);
  }

  // Mode B: Tailwind compiled by the app. The buildTransitive targets must copy the theme to obj/blux/ on every build.
  for (const dir of [server, client]) rmSync(join(dir, "obj", "blux"), { recursive: true, force: true });
  mkdirSync(join(server, "Styles"));
  writeFileSync(join(server, "Styles", "app.tailwind.css"), `@import "tailwindcss";\n@import "../obj/blux/blux.theme.css";\n`);
  build("tailwind mode");
  for (const [label, dir] of [["server", server], ["client", client]]) {
    const copied = join(dir, "obj", "blux", "blux.theme.css");
    check(existsSync(copied) && readFileSync(copied, "utf8") === themeCss, `tailwind mode: ${label} obj/blux/blux.theme.css copied by the targets, identical to the package's`);
  }
} catch (e) {
  failures.push(String(e.message ?? e));
  console.log(String(e.stack ?? e));
} finally {
  if (keep) console.log(`kept ${work}`); else rmSync(work, { recursive: true, force: true });
}

console.log(failures.length ? `\nPACKAGE CHECK FAILED (${failures.length}):\n  ${failures.join("\n  ")}` : "\nPACKAGE CHECK PASSED");
process.exit(failures.length ? 1 : 0);
