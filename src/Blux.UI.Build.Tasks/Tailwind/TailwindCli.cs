using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace Blux.UI.Build
{
    /// <summary>The machine the build runs on, as far as choosing a Tailwind binary goes.</summary>
    internal readonly struct TailwindPlatform
    {
        public TailwindPlatform(string os, string arch, bool musl)
        {
            Os = os;
            Arch = arch;
            Musl = musl;
        }

        /// <summary><c>linux</c>, <c>macos</c> or <c>windows</c>.</summary>
        public string Os { get; }

        /// <summary><c>x64</c> or <c>arm64</c>.</summary>
        public string Arch { get; }

        /// <summary>Linux with musl libc (Alpine) instead of glibc.</summary>
        public bool Musl { get; }

        public override string ToString() => Os + "-" + Arch + (Musl ? "-musl" : "");

        public static TailwindPlatform Current()
        {
            var os = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "windows"
                : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "macos"
                : RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "linux"
                : "other";
            // The architecture of this process, not of the machine: the binary has to run next to it (x64 under Rosetta or emulation stays x64).
            var arch = RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X64 => "x64",
                Architecture.Arm64 => "arm64",
                var other => other.ToString().ToLowerInvariant(),
            };
            return new TailwindPlatform(os, arch, os == "linux" && IsMusl(arch));
        }

        // musl systems ship their loader as /lib/ld-musl-<arch>.so.1 and no glibc loader.
        private static bool IsMusl(string arch)
        {
            var muslArch = arch == "arm64" ? "aarch64" : "x86_64";
            var glibcLoader = arch == "arm64" ? "/lib/ld-linux-aarch64.so.1" : "/lib64/ld-linux-x86-64.so.2";
            return File.Exists("/lib/ld-musl-" + muslArch + ".so.1") && !File.Exists(glibcLoader);
        }
    }

    /// <summary>
    /// The official standalone Tailwind CLI: which release asset fits a machine, and the SHA-256 of each asset of the pinned version.
    /// The hashes come from the <c>sha256sums.txt</c> of the GitHub release (and match the digests the GitHub API reports for the
    /// assets); the linux-x64 binary was also downloaded and hashed. A version without hashes here is refused, never downloaded blind.
    /// </summary>
    internal static class TailwindAssets
    {
        /// <summary>The same version as the <c>@tailwindcss/cli</c> npm package the repository builds blux.css with.</summary>
        public const string DefaultVersion = "4.3.5";

        private static readonly Dictionary<string, Dictionary<string, string>> Checksums = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal)
        {
            ["4.3.5"] = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["tailwindcss-linux-arm64"] = "ffcd0d21dbd7c605dc12743ff0c0b47debc963c86b399674084c1c8f2c96ecad",
                ["tailwindcss-linux-arm64-musl"] = "564dad3028c029b0c69042c66a7a79873ed7289b73fcb7b37973fbf79cb9a32f",
                ["tailwindcss-linux-x64"] = "e47695cee3f0d90623a91f3939b08bdee610eb5ad99340adff9d4145b284d6cc",
                ["tailwindcss-linux-x64-musl"] = "bb050edd13f15006d872a9bb1772b908618a8df6806d30bf095cba26a09e640b",
                ["tailwindcss-macos-arm64"] = "f3f4f5d1727e2b693b8f1c9a1492b5dd87008c86ea286bee1589cb458e6235fc",
                ["tailwindcss-macos-x64"] = "a63702686a3ed048b3135d84a73be49ea9d47f880c9b20e54b3ef60934ad8cdf",
                ["tailwindcss-windows-x64.exe"] = "2803583268c53f4713e89943e211081fddb186c0a4a7d07b5c342c6ae853ec95",
            },
        };

        /// <summary>The release asset for a machine. Windows on ARM runs the x64 binary (the release has no arm64 one) through emulation.</summary>
        public static string SelectAsset(TailwindPlatform platform)
        {
            switch (platform.Os)
            {
                case "windows" when platform.Arch == "x64" || platform.Arch == "arm64":
                    return "tailwindcss-windows-x64.exe";
                case "macos" when platform.Arch == "x64" || platform.Arch == "arm64":
                    return "tailwindcss-macos-" + platform.Arch;
                case "linux" when platform.Arch == "x64" || platform.Arch == "arm64":
                    return "tailwindcss-linux-" + platform.Arch + (platform.Musl ? "-musl" : "");
                default:
                    throw new BuildToolException($"The Tailwind standalone CLI has no binary for {platform.Os} {platform.Arch}. Install Tailwind yourself and set BluxTailwindExecutable to its path.");
            }
        }

        public static string DownloadUrl(string version, string asset) =>
            "https://github.com/tailwindlabs/tailwindcss/releases/download/v" + version + "/" + asset;

        /// <summary>The expected hash of an asset, or null when the version is not pinned here.</summary>
        public static string? ExpectedSha256(string version, string asset) =>
            Checksums.TryGetValue(version, out var assets) && assets.TryGetValue(asset, out var hash) ? hash : null;

        public static string Sha256(byte[] bytes)
        {
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(bytes);
            var text = new StringBuilder(hash.Length * 2);
            foreach (var b in hash) text.Append(b.ToString("x2"));
            return text.ToString();
        }

        /// <summary>Throws when the bytes are not the asset the pinned hash is for.</summary>
        public static void Verify(string version, string asset, byte[] bytes, Func<string, string, string?>? expectedHash = null)
        {
            var expected = (expectedHash ?? ExpectedSha256)(version, asset);
            if (expected == null)
            {
                throw new BuildToolException($"Tailwind {version} has no embedded checksum for {asset}, so it is not downloaded. Use BluxTailwindVersion={DefaultVersion}, or install Tailwind yourself and set BluxTailwindExecutable to its path.");
            }
            var actual = Sha256(bytes);
            if (!string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
            {
                throw new BuildToolException($"The downloaded {asset} ({version}) does not match its SHA-256 (expected {expected}, got {actual}); it was discarded. Set BluxTailwindExecutable to a binary you trust to build without the download.");
            }
        }
    }

    /// <summary>Finds the Tailwind binary: the cached one, or the pinned release downloaded once and verified.</summary>
    internal static class TailwindCli
    {
        /// <param name="expectedHash">Replaces the embedded checksums (tests); null uses them.</param>
        public static string Resolve(INetwork network, string version, string cacheDir, bool offline, TailwindPlatform platform, Action<string> log, Func<string, string, string?>? expectedHash = null)
        {
            var asset = TailwindAssets.SelectAsset(platform);
            if ((expectedHash ?? TailwindAssets.ExpectedSha256)(version, asset) == null)
            {
                throw new BuildToolException($"BluxTailwindVersion={version} is not one this Blux.UI knows the checksums of (known: {TailwindAssets.DefaultVersion}). Use BluxTailwindVersion={TailwindAssets.DefaultVersion}, or install Tailwind yourself and set BluxTailwindExecutable to its path.");
            }

            // A file is only ever moved here after its hash matched, so its presence is the proof.
            var exe = Path.Combine(cacheDir, version, asset);
            if (File.Exists(exe)) return exe;

            var url = TailwindAssets.DownloadUrl(version, asset);
            if (offline)
            {
                throw new BuildToolException($"Offline build (BluxOffline=true) and the Tailwind CLI {version} is not in the cache ({exe}). Run one online build to fill it, or set BluxTailwindExecutable to a Tailwind binary.");
            }

            log($"Downloading the Tailwind CLI {version} ({asset}) to {exe}");
            byte[] bytes;
            try
            {
                bytes = network.GetBytes(url);
            }
            catch (Exception e) when (e is DownloadException || e is HttpRequestException || e is TaskCanceledException || e is IOException || e is InvalidOperationException)
            {
                throw new BuildToolException($"Could not download the Tailwind CLI from {url}: {e.Message}. Without network, install Tailwind yourself and set BluxTailwindExecutable to its path, or fill the cache ({cacheDir}) with an online build and set BluxOffline=true.", e);
            }
            TailwindAssets.Verify(version, asset, bytes, expectedHash);

            Directory.CreateDirectory(Path.GetDirectoryName(exe)!);
            var temp = exe + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllBytes(temp, bytes);
                if (platform.Os != "windows") MakeExecutable(temp);
                try
                {
                    File.Move(temp, exe);
                }
                catch (IOException) when (File.Exists(exe))
                {
                    // Another build of the solution put the same verified file there first.
                }
            }
            finally
            {
                try { if (File.Exists(temp)) File.Delete(temp); } catch (Exception e) when (e is IOException || e is UnauthorizedAccessException) { }
            }
            return exe;
        }

        private static void MakeExecutable(string path)
        {
            try
            {
                using var chmod = Process.Start(new ProcessStartInfo("chmod", "+x \"" + path + "\"") { UseShellExecute = false, RedirectStandardError = true, CreateNoWindow = true })!;
                chmod.WaitForExit();
                if (chmod.ExitCode != 0) throw new BuildToolException("chmod +x failed on the downloaded Tailwind CLI: " + chmod.StandardError.ReadToEnd().Trim());
            }
            catch (System.ComponentModel.Win32Exception e)
            {
                throw new BuildToolException("Could not run chmod to make the downloaded Tailwind CLI executable: " + e.Message + ". Set BluxTailwindExecutable to a Tailwind binary.", e);
            }
        }
    }
}
