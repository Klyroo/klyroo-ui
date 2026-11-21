// Packaging check: packs Blux.UI (and the image packages Blux.UI.Images and Blux.UI.Images.Local) in Release, inspects the .nupkg and
// .snupkg files, then builds a throwaway Blazor Web App (Auto interactivity) that consumes the packages from a local folder feed, in both CSS modes.
//
//   node build/verify-package.mjs [--online] [--keep]
//
// Offline by default: the consumer restores from the pack output folder plus the local NuGet cache (a global-packages
// folder is a valid folder feed), with nuget.org removed. If a framework package is missing from the cache, pass
// --online to add nuget.org as a source. Needs the .NET 10 SDK and the committed generated assets (npm run build).
import { spawn, spawnSync } from "node:child_process";
import { inflateRawSync } from "node:zlib";
import { copyFileSync, existsSync, mkdirSync, mkdtempSync, readFileSync, readdirSync, rmSync, statSync, writeFileSync } from "node:fs";
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
    "buildTransitive/Blux.UI.targets", "build/Blux.UI.targets", "README.md",
    "buildTransitive/Blux.UI.Tasks.targets", "buildTransitive/Blux.UI.Fonts.targets", "buildTransitive/Blux.UI.Icons.targets", "buildTransitive/Blux.UI.Tailwind.targets", "buildTransitive/tasks/Blux.UI.Build.Tasks.dll",
    "build/Blux.UI.Tasks.targets", "build/Blux.UI.Fonts.targets", "build/Blux.UI.Icons.targets", "build/Blux.UI.Tailwind.targets", "build/tasks/Blux.UI.Build.Tasks.dll", "CHANGELOG.md", "icon.png", "Blux.UI.nuspec"]) {
    check(nupkg.has(n), `nupkg contains ${n}`);
  }
  check(names.some((n) => /^staticwebassets\/Blux\.UI(\.[a-z0-9]+)?\.lib\.module\.js$/.test(n)), "nupkg contains the JS initializer (Blux.UI.lib.module.js, fingerprinted)");
  check(names.some((n) => /^build\/.*StaticWebAssets/i.test(n)), "nupkg declares its static web assets for consumers (build/*StaticWebAssets*)");
  check(!names.some((n) => /^(js|styles)\//.test(n)), "sources of generated assets (js/, styles/) are not shipped");
  check(!names.some((n) => n.endsWith(".pdb")), "no .pdb in the .nupkg (symbols go to the .snupkg)");
  check(!/<dependency id="Microsoft\.Build/.test(nupkg.has("Blux.UI.nuspec") ? nupkg.get("Blux.UI.nuspec")().toString("utf8") : ""), "the task assembly adds no NuGet dependency");
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
  // Self-hosted fonts, offline: a font of the local provider (an OFL test font committed under build/test-assets).
  mkdirSync(join(server, "Fonts"));
  copyFileSync(join(root, "build", "test-assets", "fonts", "PackageCheck-400.woff2"), join(server, "Fonts", "PackageCheck-400.woff2"));
  // The same font as .ttf and .woff, to see that every font format gets the immutable header (the SDK only knows woff2 and otf).
  copyFileSync(join(root, "tests", "Blux.UI.Tests", "Fonts", "Fixtures", "geist-latin-400.ttf"), join(server, "Fonts", "PackageCheck-700.ttf"));
  copyFileSync(join(root, "tests", "Blux.UI.Tests", "Fonts", "Fixtures", "geist-latin-400.woff"), join(server, "Fonts", "PackageCheck-300.woff"));
  const serverProj = join(server, "Site.csproj");
  writeFileSync(serverProj, readFileSync(serverProj, "utf8").replace("</Project>", `  <ItemGroup><BluxFont Include="PackageCheck" Role="sans" Provider="local" Src="Fonts" /></ItemGroup>\n</Project>`));
  // Bundled icons, offline: a folder of SVG files is a collection, so nothing is downloaded (BluxOffline would fail the build otherwise).
  mkdirSync(join(server, "Icons"));
  writeFileSync(join(server, "Icons", "Check Mark.svg"), `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M2 2h20"/></svg>`);
  writeFileSync(serverProj, readFileSync(serverProj, "utf8").replace("</Project>", `  <PropertyGroup><BluxOffline>true</BluxOffline></PropertyGroup>\n  <ItemGroup><BluxIconCollection Include="Icons" Prefix="pkg" /></ItemGroup>\n</Project>`));
  const imports = join(server, "Components", "_Imports.razor");
  writeFileSync(imports, `${readFileSync(imports, "utf8")}\n@using Blux.UI\n`);
  const prog = join(server, "Program.cs");
  writeFileSync(prog, readFileSync(prog, "utf8").replace("var app = builder.Build();", "builder.Services.AddBluxUi();\nbuilder.Services.AddBluxImagesLocal();\n\nvar app = builder.Build();\napp.MapBluxImages();").replace("using Site.Components;", "using Site.Components;\nusing Blux.UI;"));
  const home = join(server, "Components", "Pages", "Home.razor");
  writeFileSync(home, `${readFileSync(home, "utf8")}\n@using Blux.UI\n<BxButton Label="Save" />\n<BxSwitch Label="Notify" />\n<BxIcon Name="i-pkg-check-mark" Label="Check mark" />\n<BxButton Label="Close" LeadingIcon='BxIcons.Named("pkg:check-mark")' />\n`);
  const appRazor = join(server, "Components", "App.razor");
  const original = readFileSync(appRazor, "utf8").replace("<ResourcePreloader />", "<BxFonts />\n    <ResourcePreloader />");
  check(original.includes("<BxFonts />"), "consumer App.razor has BxFonts");
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

  // Self-hosted fonts: generated by the task from the package's targets, then part of the static web assets.
  const fontsOut = join(server, "obj", "Release", "net10.0", "blux", "fonts");
  const fontsCssFile = join(fontsOut, "blux.fonts.css");
  const fontsCss = existsSync(fontsCssFile) ? readFileSync(fontsCssFile, "utf8") : "";
  check(/font-family: "PackageCheck";/.test(fontsCss) && /font-display: swap;/.test(fontsCss), "fonts: blux.fonts.css declares the local font with font-display");
  check(/font-family: "PackageCheck Fallback";[\s\S]*size-adjust: [\d.]+%;[\s\S]*ascent-override:[\s\S]*descent-override:[\s\S]*line-gap-override:/.test(fontsCss), "fonts: blux.fonts.css has a fallback face with metric overrides");
  check(/--blux-font-sans: "PackageCheck", "PackageCheck Fallback", /.test(fontsCss), "fonts: --blux-font-sans lists the font and its fallback");
  const fontFile = /url\("(PackageCheck-400-normal\.[0-9a-f]{8}\.woff2)"\)/.exec(fontsCss)?.[1];
  check(!!fontFile && existsSync(join(fontsOut, fontFile)), `fonts: the font file is generated with a hashed name (${fontFile})`);
  check(existsSync(join(fontsOut, "..", "BluxFontsManifest.g.cs")) && !existsSync(join(fontsOut, "BluxFontsManifest.g.cs")), "fonts: the manifest source is generated next to the fonts folder, not in it");
  check(!manifest.includes("BluxFontsManifest") && !manifest.includes(".stamp") && !manifest.includes("blux.fonts.json"), "fonts: only fonts and the stylesheet are static web assets");
  check(manifest.includes("_blux/fonts/blux.fonts") && !!fontFile && manifest.includes(`_blux/fonts/${fontFile}`), "fonts: stylesheet and font file are static web assets under _blux/fonts/");
  const endpointsFile = join(server, "obj", "Release", "net10.0", "staticwebassets.build.endpoints.json");
  const endpoints = existsSync(endpointsFile) ? JSON.parse(readFileSync(endpointsFile, "utf8")).Endpoints : [];
  const fontEndpoints = endpoints.filter((e) => fontFile && e.Route.startsWith("_blux/fonts/PackageCheck-400") && e.Route.endsWith(".woff2"));
  check(fontEndpoints.length >= 2 && fontEndpoints.every((e) => e.ResponseHeaders.some((h) => h.Name === "Cache-Control" && h.Value === "max-age=31536000, immutable")), "fonts: every route of the font is served with a year of immutable cache");
  for (const [ext, mime] of [["ttf", "font/ttf"], ["woff", "font/woff"]]) {
    const routes = endpoints.filter((e) => e.Route.startsWith("_blux/fonts/PackageCheck-") && e.Route.endsWith("." + ext));
    check(routes.length >= 2 && routes.every((e) => e.ResponseHeaders.some((h) => h.Name === "Cache-Control" && h.Value === "max-age=31536000, immutable") && e.ResponseHeaders.some((h) => h.Name === "Content-Type" && h.Value === mime)), `fonts: every route of the .${ext} font is served as ${mime} with a year of immutable cache`);
  }
  check(!endpoints.some((e) => /\.(cs|json|stamp)$/.test(e.Route) && e.Route.startsWith("_blux/fonts/")), "fonts: no generated source, manifest or stamp is published as an endpoint");

  // Bundled icons: generated from the folder with no network, then compiled into the app.
  const iconsFile = join(server, "obj", "Release", "net10.0", "blux", "icons", "BluxIcons.g.cs");
  const iconsSource = existsSync(iconsFile) ? readFileSync(iconsFile, "utf8") : "";
  check(/BluxIconBundle\.Add\("pkg", "check-mark", "<g fill=\\"none\\" stroke=\\"currentColor\\" stroke-width=\\"2\\"><path d=\\"M2 2h20\\"\/><\/g>", "0 0 24 24"\)/.test(iconsSource), "icons: the custom collection is generated into BluxIcons.g.cs");
  check(iconsSource.includes("ModuleInitializer"), "icons: the registration is a module initializer");

  // Run the app and fetch what the page links: the font is served from the app, the preload and stylesheet are in the head.
  {
    const port = 20000 + Math.floor(Math.random() * 20000);
    const url = `http://127.0.0.1:${port}`;
    const child = spawn("dotnet", [join(server, "bin", "Release", "net10.0", "Site.dll"), "--urls", url], { cwd: server, env: { ...process.env, ASPNETCORE_ENVIRONMENT: "Development", DOTNET_NOLOGO: "1" }, stdio: "ignore" });
    let html = "";
    for (let i = 0; i < 60 && !html; i++) {
      await new Promise((r) => setTimeout(r, 500));
      try { const res = await fetch(url + "/"); if (res.ok) html = await res.text(); } catch { /* not up yet */ }
    }
    try {
      check(/<svg viewBox="0 0 24 24"[^>]*data-icon="i-pkg-check-mark"[^>]*><g fill="none"[^>]*><path d="M2 2h20"/.test(html) && !html.includes("data-bx-missing-icon"), "icons: the page draws the bundled icon from the custom folder");
      const preload = /<link rel="preload" as="font" type="font\/woff2" crossorigin href="([^"]+)"/.exec(html)?.[1];
      const sheet = /<link rel="stylesheet" href="(_blux\/fonts\/blux\.fonts[^"]*\.css)"/.exec(html)?.[1];
      check(!!preload && preload.startsWith("_blux/fonts/PackageCheck-400-normal."), `fonts: the page preloads the font (${preload})`);
      check(!!sheet, `fonts: the page links the fingerprinted stylesheet (${sheet})`);
      if (preload) { const r = await fetch(`${url}/${preload}`); const b = new Uint8Array(await r.arrayBuffer()); check(r.ok && b.length > 1000 && String.fromCharCode(...b.slice(0, 4)) === "wOF2", "fonts: the font file is served as woff2"); }
      if (sheet) { const r = await fetch(`${url}/${sheet}`); check(r.ok && (await r.text()).includes("PackageCheck Fallback"), "fonts: the stylesheet is served"); }
    } finally {
      child.kill();
    }
  }

  // BluxFonts=false: nothing is generated or served.
  {
    const r = run("dotnet", ["build", "-c", "Release", "-p:BluxFonts=false", join(app, "Site", "Site.sln")], app, consumerEnv);
    check(r.code === 0, "fonts off: consumer builds");
    check(!readFileSync(manifestFile, "utf8").includes("_blux/fonts/"), "fonts off: no font in the static web assets");
    build("fonts back on");
    check(readFileSync(manifestFile, "utf8").includes("_blux/fonts/"), "fonts back on: the fonts are in the static web assets again");
  }

  // BluxIcons=false after a build with icons: the file the earlier build left on disk is not deleted by the test. The generated file must be in
  // the compile inputs hash (it is added before _GenerateCompileDependencyCache), so the switch recompiles the assembly: the initializer is gone from it.
  {
    const dll = join(server, "obj", "Release", "net10.0", "Site.dll");
    const hasInitializer = () => existsSync(dll) && readFileSync(dll).includes(Buffer.from("BluxIconBundleInitializer"));
    check(existsSync(iconsFile), "icons off: the file of the build with icons is still on disk before the switch");
    check(hasInitializer(), "icons off: the assembly of the build with icons holds the icon initializer");
    const r = run("dotnet", ["build", "-c", "Release", "-p:BluxIcons=false", join(app, "Site", "Site.sln")], app, consumerEnv);
    check(r.code === 0, "icons off: consumer builds");
    if (r.code !== 0) console.log(r.out);
    check(!existsSync(iconsFile), "icons off: the leftover BluxIcons.g.cs is removed from disk");
    check(!hasInitializer(), "icons off: the initializer is gone from the assembly (the leftover is not compiled)");
    build("icons back on");
    check(existsSync(iconsFile), "icons back on: BluxIcons.g.cs is generated again");
    check(hasInitializer(), "icons back on: the assembly registers the icons again");
    check(readFileSync(iconsFile, "utf8").includes("[assembly: global::Blux.UI.BluxIconRegistrar("), "icons back on: the generated file announces its registrar with an assembly attribute");
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

  // Tailwind compiled by the app at build time, without Node: BluxTailwind=true downloads the pinned standalone CLI. The cache is a fresh folder
  // of this run, so the first build really downloads, hashes and chmods it; the builds after it are the cached scenario.
  {
    const tailwindCache = join(work, "tailwind-cache");
    // A razor class library with BluxTailwind, referenced by the server: its stylesheet must reach the host, also through publish with no build.
    const rcl = join(app, "Site", "Rcl");
    mkdirSync(join(rcl, "Styles"), { recursive: true });
    writeFileSync(join(rcl, "Rcl.csproj"), `<Project Sdk="Microsoft.NET.Sdk.Razor">\n  <PropertyGroup><TargetFramework>net10.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><BluxTailwind>true</BluxTailwind></PropertyGroup>\n  <ItemGroup><FrameworkReference Include="Microsoft.AspNetCore.App" /><PackageReference Include="Blux.UI" Version="${version}" /></ItemGroup>\n</Project>\n`);
    writeFileSync(join(rcl, "Styles", "app.css"), `@import "tailwindcss";\n@import "../obj/blux/blux.theme.css";\n`);
    writeFileSync(join(rcl, "Widget.razor"), `<p class="tracking-[0.246em]">Rcl widget</p>\n`);
    writeFileSync(serverProj, readFileSync(serverProj, "utf8").replace("</Project>", `  <ItemGroup><ProjectReference Include="..\\Rcl\\Rcl.csproj" /></ItemGroup>\n</Project>`));
    // In the solution, so that a Release build of it builds the library in Release too (a project outside the solution is built as Debug).
    check(run("dotnet", ["sln", join(app, "Site", "Site.sln"), "add", join(rcl, "Rcl.csproj")], app, consumerEnv).code === 0, "tailwind: the library is added to the consumer solution");
    // The server project sets BluxOffline=true for the icons check; the first Tailwind build needs the network (a global property wins over it).
    const flags = ["-p:BluxOffline=false", `-p:BluxTailwindCacheDir=${tailwindCache}`];
    const buildTw = (label, extra = flags) => {
      const r = run("dotnet", ["build", "-c", "Release", ...extra, join(app, "Site", "Site.sln")], app, consumerEnv);
      check(r.code === 0, `${label}: consumer builds`);
      if (r.code !== 0) console.log(r.out);
      check(!/warning[^\n]*Blux/i.test(r.out), `${label}: no Blux.UI warning in the consumer build`);
      return r;
    };
    writeFileSync(serverProj, readFileSync(serverProj, "utf8").replace("</Project>", `  <PropertyGroup><BluxTailwind>true</BluxTailwind></PropertyGroup>\n</Project>`));
    // A shared folder outside the project, read through @source: BluxTailwindAdditionalInput makes edits there rebuild the stylesheet.
    const shared = join(app, "Site", "Shared");
    mkdirSync(shared, { recursive: true });
    writeFileSync(join(shared, "Shared.razor"), `<p class="tracking-[0.555em]">shared</p>\n`);
    writeFileSync(serverProj, readFileSync(serverProj, "utf8").replace("</Project>", `  <ItemGroup><BluxTailwindAdditionalInput Include="../Shared/**/*.razor" /></ItemGroup>\n</Project>`));
    writeFileSync(join(server, "Styles", "app.css"), `@import "tailwindcss";\n@import "../obj/blux/blux.theme.css";\n@source "../../Shared";\n`);
    const homeSource = readFileSync(home, "utf8");
    const withClasses = (classes) => writeFileSync(home, `${homeSource}\n<p class="${classes}">Tailwind check</p>\n`);
    // tracking-[0.123em] is an arbitrary value: it is in no precompiled stylesheet, only a build that scans the app's sources can produce it.
    withClasses("tracking-[0.123em]");
    // A source the scan must not read: obj holds generated files, and this app has no .gitignore to say so.
    writeFileSync(join(server, "obj", "scan-trap.cs"), `// class="tracking-[0.987em]"\n`);
    // The Blazor template ships wwwroot/app.css, the default route of the generated stylesheet: the build says so instead of the SDK's conflict error.
    check(existsSync(join(server, "wwwroot", "app.css")), "tailwind: the template's own wwwroot/app.css is there to collide with");
    {
      const r = run("dotnet", ["build", "-c", "Release", ...flags, join(app, "Site", "Site.sln")], app, consumerEnv);
      check(r.code !== 0 && /BLUXTW004[^\n]*wwwroot\/app\.css[^\n]*BluxTailwindAssetPath/.test(r.out), "tailwind: a wwwroot/app.css of the project is reported as BLUXTW004 naming BluxTailwindAssetPath");
    }
    // The app takes the way out the error names: it serves the generated stylesheet under another name.
    writeFileSync(serverProj, readFileSync(serverProj, "utf8").replace("<BluxTailwind>true</BluxTailwind>", "<BluxTailwind>true</BluxTailwind><BluxTailwindAssetPath>site.tailwind.css</BluxTailwindAssetPath>"));
    buildTw("tailwind: first build");
    const twDir = join(server, "obj", "Release", "net10.0", "blux", "tailwind");
    const twOut = join(twDir, "site.tailwind.css");
    const css = () => (existsSync(twOut) ? readFileSync(twOut, "utf8") : "");
    check(existsSync(twOut), "tailwind: the stylesheet is generated under obj/<config>/<tfm>/blux/tailwind/");
    check(css().includes("letter-spacing:.123em"), "tailwind: the generated CSS has the utility the app uses and blux.css lacks (tracking-[0.123em])");
    check(!readFileSync(join(root, "src", "Blux.UI", "wwwroot", "blux.css"), "utf8").includes("123em"), "tailwind: that utility is not in the precompiled blux.css, so the check proves the app's CSS was compiled");
    check(!css().includes(".987em"), "tailwind: obj is not scanned for class names");
    check(css().includes("--blux-primary") && css().includes(".bg-\\(--blux-bg-elevated\\)"), "tailwind: the Blux theme tokens and the classes of the Blux components are generated too");
    check(css().trim().split("\n").length <= 3, "tailwind: Release output is minified");
    const swa = readFileSync(manifestFile, "utf8");
    check(swa.includes("blux/tailwind/site.tailwind.css") && swa.includes('"RelativePath":"site.tailwind#[.{fingerprint}]?.css"'), "tailwind: the stylesheet (from obj) is a static web asset");
    check(!existsSync(join(server, "wwwroot", "site.tailwind.css")), "tailwind: nothing is written into the source tree (wwwroot)");

    // Incremental: nothing changed, so the CLI does not run again; a source change does.
    const stamp = () => statSync(twOut).mtimeMs;
    const t1 = stamp();
    buildTw("tailwind: second build");
    check(stamp() === t1, "tailwind: unchanged inputs do not run the CLI again");
    withClasses("tracking-[0.123em] leading-[1.234]");
    buildTw("tailwind: after a source change");
    check(stamp() > t1 && css().includes("1.234"), "tailwind: a changed .razor runs the CLI again and its new utility appears");

    // An edit that changes no CSS (a comment in a .cs file): the CLI leaves its output alone, so the target must still settle (its output is touched).
    const programSource = readFileSync(prog, "utf8");
    writeFileSync(prog, `${programSource}\n// edited without a class name\n`);
    buildTw("tailwind: after a .cs edit");
    const t3 = stamp();
    buildTw("tailwind: build after the .cs edit");
    check(stamp() === t3, "tailwind: after an edit that changes no CSS the next build skips the target (it does not rerun on every build)");
    // The minify setting is part of the inputs: switching it within one configuration recompiles.
    buildTw("tailwind: minify off", [...flags, "-p:BluxTailwindMinify=false"]);
    check(css().trim().split("\n").length > 10, "tailwind: BluxTailwindMinify=false in Release gives readable CSS (the setting reruns the CLI)");
    buildTw("tailwind: minify back on");
    check(css().trim().split("\n").length <= 3, "tailwind: minify back on gives minified CSS again");
    // A file outside the project folder, listed in BluxTailwindAdditionalInput, is tracked.
    check(css().includes(".555em"), "tailwind: a class of the shared folder outside the project is generated (@source)");
    writeFileSync(join(shared, "Shared.razor"), `<p class="tracking-[0.555em] leading-[1.666]">shared</p>\n`);
    buildTw("tailwind: after an edit outside the project");
    check(css().includes("1.666"), "tailwind: an edit in a BluxTailwindAdditionalInput file outside the project rebuilds the stylesheet");

    // An override binary: no download, whatever the cache holds. Offline with nothing cached and no override: an error that names the property.
    const cached = existsSync(tailwindCache) ? readdirSync(tailwindCache, { recursive: true }).map((f) => join(tailwindCache, String(f))).find((f) => /tailwindcss-(linux|macos|windows)[^/\\]*$/.test(f) && statSync(f).isFile() && !/\.(tmp|verified)$/.test(f)) : undefined;
    const emptyCache = join(work, "empty-tailwind-cache");
    if (check(!!cached, "tailwind: the verified binary is in the cache folder")) {
      withClasses("tracking-[0.123em] leading-[1.345]");
      const r = run("dotnet", ["build", "-c", "Release", "-p:BluxOffline=true", `-p:BluxTailwindCacheDir=${emptyCache}`, `-p:BluxTailwindExecutable=${cached}`, join(app, "Site", "Site.sln")], app, consumerEnv);
      if (r.code !== 0) console.log(r.out);
      check(r.code === 0 && css().includes("1.345") && !existsSync(emptyCache), "tailwind: BluxTailwindExecutable is used, with no download and nothing written to the cache");
      withClasses("tracking-[0.123em] leading-[1.456]");
      const off = run("dotnet", ["build", "-c", "Release", "-p:BluxOffline=true", `-p:BluxTailwindCacheDir=${emptyCache}`, join(app, "Site", "Site.sln")], app, consumerEnv);
      check(off.code !== 0 && /BLUXTW001[^\n]*BluxTailwindExecutable/.test(off.out), "tailwind: offline with an empty cache fails with an error that names BluxTailwindExecutable");
    }

    // A cached binary that changed after it was verified is never run: offline it is rejected and deleted, online it is downloaded again.
    if (cached) {
      writeFileSync(cached, Buffer.concat([readFileSync(cached), Buffer.from("x")]));
      writeFileSync(home, `${homeSource}\n<p class="tracking-[0.123em] leading-[1.567]">Tailwind check</p>\n`);
      const bad = run("dotnet", ["build", "-c", "Release", "-p:BluxOffline=true", `-p:BluxTailwindCacheDir=${tailwindCache}`, join(app, "Site", "Site.sln")], app, consumerEnv);
      check(bad.code !== 0 && /BLUXTW001[^\n]*(SHA-256|checksum)/.test(bad.out) && !existsSync(cached), "tailwind: a modified cached binary is rejected offline and deleted, never run");
    }

    // The app serves it.
    buildTw("tailwind: final build");
    check(!!cached && existsSync(cached) && existsSync(cached + ".verified"), "tailwind: the binary is downloaded again and stamped after it was rejected");
    // build, then publish with no build: the stylesheets of the server and of the library must be published (the targets must not run again).
    {
      const out = join(server, "bin", "Release", "net10.0", "publish");
      rmSync(out, { recursive: true, force: true });
      const r = run("dotnet", ["publish", join(server, "Site.csproj"), "-c", "Release", "--no-build", ...flags], server, consumerEnv);
      if (r.code !== 0) console.log(r.out);
      check(r.code === 0, "tailwind: dotnet publish --no-build after the build succeeds");
      const published = existsSync(join(out, "wwwroot")) ? readdirSync(join(out, "wwwroot"), { recursive: true }).map(String) : [];
      // The library alone: without the NoBuild guard its definition of the asset ran with an empty base path ("No metadata 'BasePath'").
      const rclPublish = run("dotnet", ["publish", join(rcl, "Rcl.csproj"), "-c", "Release", "--no-build", ...flags], rcl, consumerEnv);
      if (rclPublish.code !== 0) console.log(rclPublish.out);
      check(rclPublish.code === 0, "tailwind: dotnet publish --no-build of the razor class library itself succeeds");
      check(published.some((f) => /^site\.tailwind(\.[^./]+)?\.css$/.test(f)), "tailwind: the web project's stylesheet is published");
      check(published.some((f) => /^_content[/\\]Rcl[/\\]app(\.[^./]+)?\.css$/.test(f)), "tailwind: the library's stylesheet is published at _content/Rcl/app.css");
      check(published.some((f) => /^_blux[/\\]fonts[/\\]blux\.fonts(\.[^./]+)?\.css$/.test(f)), "fonts: the stylesheet is published by publish --no-build too");
    }
    {
      const port = 20000 + Math.floor(Math.random() * 20000);
      const url = `http://127.0.0.1:${port}`;
      const child = spawn("dotnet", [join(server, "bin", "Release", "net10.0", "Site.dll"), "--urls", url], { cwd: server, env: { ...process.env, ASPNETCORE_ENVIRONMENT: "Development", DOTNET_NOLOGO: "1" }, stdio: "ignore" });
      let served = "";
      for (let i = 0; i < 60 && !served; i++) {
        await new Promise((r) => setTimeout(r, 500));
        try { const res = await fetch(url + "/site.tailwind.css"); if (res.ok) served = await res.text(); } catch { /* not up yet */ }
      }
      child.kill();
      check(served.includes("letter-spacing:.123em"), "tailwind: the app serves /site.tailwind.css with the generated utility");
    }
    // BluxTailwind=false: the feature adds nothing to the assets.
    const disabled = run("dotnet", ["build", "-c", "Release", "-p:BluxTailwind=false", "-p:BluxOffline=false", join(app, "Site", "Site.sln")], app, consumerEnv);
    check(disabled.code === 0 && !readFileSync(manifestFile, "utf8").includes("blux/tailwind/site.tailwind.css"), "tailwind off: no generated stylesheet in the static web assets");
  }
} catch (e) {
  failures.push(String(e.message ?? e));
  console.log(String(e.stack ?? e));
} finally {
  if (keep) console.log(`kept ${work}`); else rmSync(work, { recursive: true, force: true });
}

console.log(failures.length ? `\nPACKAGE CHECK FAILED (${failures.length}):\n  ${failures.join("\n  ")}` : "\nPACKAGE CHECK PASSED");
process.exit(failures.length ? 1 : 0);
