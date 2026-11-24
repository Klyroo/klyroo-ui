using System.Collections;
using Lux.UI.Build;
using Microsoft.Build.Framework;

namespace Lux.UI.Tests.Tailwind;

public class TailwindAssetTests
{
    [Theory]
    [InlineData("linux", "x64", false, "tailwindcss-linux-x64")]
    [InlineData("linux", "x64", true, "tailwindcss-linux-x64-musl")]
    [InlineData("linux", "arm64", false, "tailwindcss-linux-arm64")]
    [InlineData("linux", "arm64", true, "tailwindcss-linux-arm64-musl")]
    [InlineData("macos", "x64", false, "tailwindcss-macos-x64")]
    [InlineData("macos", "arm64", false, "tailwindcss-macos-arm64")]
    [InlineData("windows", "x64", false, "tailwindcss-windows-x64.exe")]
    // The release has no windows-arm64 binary: Windows on ARM runs the x64 one.
    [InlineData("windows", "arm64", false, "tailwindcss-windows-x64.exe")]
    public void Asset_follows_os_arch_and_libc(string os, string arch, bool musl, string expected)
        => Assert.Equal(expected, TailwindAssets.SelectAsset(new TailwindPlatform(os, arch, musl)));

    [Theory]
    [InlineData("windows", "x86")]
    [InlineData("linux", "arm")]
    [InlineData("other", "x64")]
    [InlineData("macos", "riscv64")]
    public void Unsupported_machine_names_the_override(string os, string arch)
    {
        var e = Assert.Throws<BuildToolException>(() => TailwindAssets.SelectAsset(new TailwindPlatform(os, arch, false)));
        Assert.Contains("LuxTailwindExecutable", e.Message);
    }

    [Fact]
    public void The_asset_of_the_current_machine_has_an_embedded_checksum()
    {
        var current = TailwindAssets.SelectAsset(TailwindPlatform.Current());
        Assert.Matches("^[0-9a-f]{64}$", TailwindAssets.ExpectedSha256(TailwindAssets.DefaultVersion, current)!);
    }

    [Fact]
    public void Every_asset_that_can_be_picked_has_an_embedded_checksum()
    {
        foreach (var os in new[] { "linux", "macos", "windows" })
        foreach (var arch in new[] { "x64", "arm64" })
        foreach (var musl in os == "linux" ? new[] { false, true } : new[] { false })
        {
            var asset = TailwindAssets.SelectAsset(new TailwindPlatform(os, arch, musl));
            Assert.NotNull(TailwindAssets.ExpectedSha256(TailwindAssets.DefaultVersion, asset));
        }
    }

    [Fact]
    public void Download_url_is_the_official_release()
        => Assert.Equal("https://github.com/tailwindlabs/tailwindcss/releases/download/v4.3.5/tailwindcss-linux-x64", TailwindAssets.DownloadUrl("4.3.5", "tailwindcss-linux-x64"));

    [Fact]
    public void Sha256_is_lowercase_hex()
        => Assert.Equal("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad", TailwindAssets.Sha256("abc"u8.ToArray()));

    [Fact]
    public void Verify_accepts_matching_bytes_and_rejects_others()
    {
        var bytes = "abc"u8.ToArray();
        static string? Hash(string v, string a) => "BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD";
        TailwindAssets.Verify("1", "a", bytes, Hash);
        var e = Assert.Throws<BuildToolException>(() => TailwindAssets.Verify("1", "a", "abd"u8.ToArray(), Hash));
        Assert.Contains("does not match", e.Message);
        Assert.Contains("LuxTailwindExecutable", e.Message);
    }

    [Fact]
    public void Verify_refuses_a_version_without_checksums()
    {
        var e = Assert.Throws<BuildToolException>(() => TailwindAssets.Verify("9.9.9", "tailwindcss-linux-x64", [1, 2, 3]));
        Assert.Contains("no embedded checksum", e.Message);
    }
}

public class TailwindCliResolveTests : IDisposable
{
    private readonly string _cache = Path.Combine(Path.GetTempPath(), "blux-tw-" + Guid.NewGuid().ToString("N"));
    private static readonly TailwindPlatform Windows = new("windows", "x64", false); // no chmod involved
    private static readonly byte[] Binary = "fake tailwind"u8.ToArray();
    private static string? Hash(string version, string asset) => TailwindAssets.Sha256(Binary);

    private sealed class Net(byte[]? bytes, Exception? error = null) : INetwork
    {
        public List<string> Requests { get; } = [];
        public string GetText(string url, string? userAgent) => throw new NotSupportedException();
        public byte[] GetBytes(string url)
        {
            Requests.Add(url);
            if (error != null) throw error;
            return bytes!;
        }
    }

    public void Dispose()
    {
        try { Directory.Delete(_cache, true); } catch (IOException) { }
    }

    [Fact]
    public void Downloads_once_verifies_and_then_uses_the_cache()
    {
        var net = new Net(Binary);
        var path = TailwindCli.Resolve(net, "1.0.0", _cache, false, Windows, _ => { }, Hash);
        Assert.Equal(Path.Combine(_cache, "1.0.0", "tailwindcss-windows-x64.exe"), path);
        Assert.Equal(Binary, File.ReadAllBytes(path));
        Assert.Equal(["https://github.com/tailwindlabs/tailwindcss/releases/download/v1.0.0/tailwindcss-windows-x64.exe"], net.Requests);

        var again = TailwindCli.Resolve(net, "1.0.0", _cache, false, Windows, _ => { }, Hash);
        Assert.Equal(path, again);
        Assert.Single(net.Requests);
        Assert.Empty(Directory.GetFiles(Path.GetDirectoryName(path)!, "*.tmp"));
    }

    [Fact]
    public void A_checksum_mismatch_is_rejected_and_nothing_is_cached()
    {
        var net = new Net("tampered"u8.ToArray());
        var e = Assert.Throws<BuildToolException>(() => TailwindCli.Resolve(net, "1.0.0", _cache, false, Windows, _ => { }, Hash));
        Assert.Contains("does not match", e.Message);
        Assert.False(File.Exists(Path.Combine(_cache, "1.0.0", "tailwindcss-windows-x64.exe")));
    }

    [Fact]
    public void Offline_without_cache_names_the_override_property()
    {
        var net = new Net(Binary);
        var e = Assert.Throws<BuildToolException>(() => TailwindCli.Resolve(net, "1.0.0", _cache, true, Windows, _ => { }, Hash));
        Assert.Contains("LuxTailwindExecutable", e.Message);
        Assert.Empty(net.Requests);
    }

    [Fact]
    public void A_failed_download_names_the_override_property()
    {
        var net = new Net(null, new HttpRequestException("no route"));
        var e = Assert.Throws<BuildToolException>(() => TailwindCli.Resolve(net, "1.0.0", _cache, false, Windows, _ => { }, Hash));
        Assert.Contains("LuxTailwindExecutable", e.Message);
        Assert.Contains("no route", e.Message);
    }

    [Fact]
    public void An_unpinned_version_is_refused_before_any_request()
    {
        var net = new Net(Binary);
        var e = Assert.Throws<BuildToolException>(() => TailwindCli.Resolve(net, "9.9.9", _cache, false, Windows, _ => { }));
        Assert.Contains("checksums", e.Message);
        Assert.Empty(net.Requests);
    }

    private string Fill(Net net)
    {
        var path = TailwindCli.Resolve(net, "1.0.0", _cache, false, Windows, _ => { }, Hash);
        return path;
    }

    [Fact]
    public void A_cache_hit_with_a_matching_stamp_is_not_hashed_again()
    {
        var net = new Net(Binary);
        var path = Fill(net);
        Assert.True(File.Exists(path + ".verified"));
        // Same size and modification time, other content: the stamp still describes the file, so it is trusted without a rehash.
        var time = File.GetLastWriteTimeUtc(path);
        File.WriteAllBytes(path, new byte[Binary.Length]);
        File.SetLastWriteTimeUtc(path, time);
        Assert.Equal(path, TailwindCli.Resolve(net, "1.0.0", _cache, false, Windows, _ => { }, Hash));
        Assert.Single(net.Requests);
    }

    [Fact]
    public void A_changed_modification_time_rehashes_and_a_mismatch_is_downloaded_again()
    {
        var net = new Net(Binary);
        var path = Fill(net);
        File.WriteAllBytes(path, "tampered binary"u8.ToArray());
        var logs = new List<string>();
        Assert.Equal(path, TailwindCli.Resolve(net, "1.0.0", _cache, false, Windows, logs.Add, Hash));
        Assert.Equal(2, net.Requests.Count);
        Assert.Equal(Binary, File.ReadAllBytes(path));
        Assert.Contains(logs, l => l.Contains("does not match"));
        Assert.True(TailwindCli.StampMatches(path));
    }

    [Fact]
    public void A_changed_file_that_is_still_the_binary_is_accepted_and_restamped()
    {
        var net = new Net(Binary);
        var path = Fill(net);
        File.SetLastWriteTimeUtc(path, File.GetLastWriteTimeUtc(path).AddMinutes(5));
        Assert.False(TailwindCli.StampMatches(path));
        Assert.Equal(path, TailwindCli.Resolve(net, "1.0.0", _cache, false, Windows, _ => { }, Hash));
        Assert.Single(net.Requests);
        Assert.True(TailwindCli.StampMatches(path));
    }

    [Fact]
    public void A_cached_file_without_a_stamp_is_hashed_before_it_is_used()
    {
        var net = new Net(Binary);
        var path = Fill(net);
        File.Delete(path + ".verified");
        File.WriteAllBytes(path, "tampered binary"u8.ToArray());
        TailwindCli.Resolve(net, "1.0.0", _cache, false, Windows, _ => { }, Hash);
        Assert.Equal(Binary, File.ReadAllBytes(path));
    }

    [Fact]
    public void Offline_a_tampered_cached_file_is_rejected_and_deleted_never_returned()
    {
        var net = new Net(Binary);
        var path = Fill(net);
        File.WriteAllBytes(path, "tampered binary"u8.ToArray());
        var e = Assert.Throws<BuildToolException>(() => TailwindCli.Resolve(net, "1.0.0", _cache, true, Windows, _ => { }, Hash));
        Assert.Contains("LuxTailwindExecutable", e.Message);
        Assert.False(File.Exists(path));
        Assert.Single(net.Requests);
    }

    [Theory]
    [InlineData(false, "C:\\Users\\a\\AppData\\Local", "/home/a", "/home/a/.blux/tailwind")]
    [InlineData(true, "C:\\Users\\a\\AppData\\Local", "C:\\Users\\a", "C:\\Users\\a\\AppData\\Local/Blux/tailwind")]
    public void The_default_cache_is_per_user_and_shared(bool windows, string local, string home, string expected)
    {
        var actual = TailwindCli.DefaultCacheDir(windows, local, home)!;
        Assert.Equal(expected.Replace('/', Path.DirectorySeparatorChar), actual.Replace('/', Path.DirectorySeparatorChar));
    }

    [Theory]
    [InlineData(false, "", "")]
    [InlineData(true, "", "/home/a")]
    public void No_profile_folder_means_no_default(bool windows, string local, string home)
        => Assert.Null(TailwindCli.DefaultCacheDir(windows, local, home));

    [Fact]
    public void A_unix_download_gets_the_executable_bit()
    {
        if (OperatingSystem.IsWindows()) return;
        var net = new Net(Binary);
        var path = TailwindCli.Resolve(net, "1.0.0", _cache, false, new TailwindPlatform("linux", "x64", false), _ => { }, Hash);
        Assert.True((File.GetUnixFileMode(path) & UnixFileMode.UserExecute) != 0);
    }
}

public class LuxResolveTailwindTaskTests
{
    private sealed class Engine : IBuildEngine
    {
        public List<string> Errors { get; } = [];
        public bool ContinueOnError => false;
        public int LineNumberOfTaskNode => 0;
        public int ColumnNumberOfTaskNode => 0;
        public string ProjectFileOfTaskNode => "test.csproj";
        public void LogErrorEvent(BuildErrorEventArgs e) => Errors.Add(e.Code + ": " + e.Message);
        public void LogWarningEvent(BuildWarningEventArgs e) { }
        public void LogMessageEvent(BuildMessageEventArgs e) { }
        public void LogCustomEvent(CustomBuildEventArgs e) { }
        public bool BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties, IDictionary targetOutputs) => true;
    }

    [Fact]
    public void The_override_is_used_as_it_is()
    {
        var file = Path.GetTempFileName();
        try
        {
            var task = new LuxResolveTailwind { BuildEngine = new Engine(), Executable = file, Version = "4.3.5", CacheDir = "unused" };
            Assert.True(task.Execute());
            Assert.Equal(file, task.ExecutablePath);
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact]
    public void A_missing_override_is_an_error_naming_the_property()
    {
        var engine = new Engine();
        var task = new LuxResolveTailwind { BuildEngine = engine, Executable = "/nonexistent/tailwindcss", Version = "4.3.5", CacheDir = "unused" };
        Assert.False(task.Execute());
        Assert.Contains(engine.Errors, e => e.StartsWith("BLUXTW001") && e.Contains("LuxTailwindExecutable"));
    }

    [Fact]
    public void Offline_with_an_empty_cache_is_an_error_naming_the_property()
    {
        var engine = new Engine();
        var cache = Path.Combine(Path.GetTempPath(), "blux-tw-" + Guid.NewGuid().ToString("N"));
        var task = new LuxResolveTailwind { BuildEngine = engine, Version = "4.3.5", CacheDir = cache, Offline = true };
        Assert.False(task.Execute());
        Assert.Contains(engine.Errors, e => e.Contains("Offline") && e.Contains("LuxTailwindExecutable"));
    }
}
