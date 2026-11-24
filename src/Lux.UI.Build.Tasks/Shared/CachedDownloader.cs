using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Lux.UI.Build
{
    /// <summary>
    /// A failure the person building can act on. Its message is shown as it is, so it says what was asked, what went wrong and
    /// what to do. Every task of this assembly derives its own exceptions from it.
    /// </summary>
    internal class BuildToolException : Exception
    {
        public BuildToolException(string message) : base(message) { }
        public BuildToolException(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary>A request that failed: the status code is kept so a caller can tell "not found" from "unreachable".</summary>
    internal sealed class DownloadException : BuildToolException
    {
        public DownloadException(string message, int statusCode = 0) : base(message) { StatusCode = statusCode; }
        /// <summary>0 when the server was not reached.</summary>
        public int StatusCode { get; }
    }

    /// <summary>The only place that touches the network, so unit tests can replace it with saved responses.</summary>
    internal interface INetwork
    {
        string GetText(string url, string? userAgent);
        byte[] GetBytes(string url);
    }

    internal sealed class HttpNetwork : INetwork
    {
        // Providers pick the format from the user agent: a current Chrome gets woff2 and the unicode-range subsets.
        public const string BrowserUserAgent = "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36";

        private static readonly HttpClient SharedClient = Create(TimeSpan.FromSeconds(60));

        private readonly HttpClient Client;

        public HttpNetwork() : this(null) { }

        /// <summary>A longer timeout than the default 60 seconds, for downloads of a hundred megabytes (a tool binary).</summary>
        public HttpNetwork(TimeSpan? timeout)
        {
            Client = timeout == null ? SharedClient : Create(timeout.Value);
        }

        private static HttpClient Create(TimeSpan timeout)
        {
            var handler = new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate };
            return new HttpClient(handler) { Timeout = timeout };
        }

        public string GetText(string url, string? userAgent)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.TryAddWithoutValidation("User-Agent", userAgent ?? "Lux.UI.Build");
            using var response = Client.SendAsync(request).GetAwaiter().GetResult();
            var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode)
            {
                throw new DownloadException($"{(int)response.StatusCode} {response.ReasonPhrase} for {url}{Snippet(body)}", (int)response.StatusCode);
            }
            return body;
        }

        public byte[] GetBytes(string url)
        {
            using var response = Client.GetAsync(url).GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode)
            {
                throw new DownloadException($"{(int)response.StatusCode} {response.ReasonPhrase} for {url}", (int)response.StatusCode);
            }
            return response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
        }

        private static string Snippet(string body)
        {
            // Google answers an unknown family or axis with a small HTML page whose <ins> says what is wrong.
            var start = body.IndexOf("<p><b>", StringComparison.Ordinal);
            var text = start >= 0 ? System.Text.RegularExpressions.Regex.Replace(body.Substring(start), "<[^>]+>", " ") : body;
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();
            return text.Length == 0 ? "" : ": " + (text.Length > 240 ? text.Substring(0, 240) + "..." : text);
        }
    }

    /// <summary>
    /// The conventions every Blux build task that downloads follows:
    /// <list type="bullet">
    /// <item>answers of an API go to <c>meta/</c> in the cache folder and are reused for <c>maxAge</c>; when the network fails the
    /// cached answer is used however old it is;</item>
    /// <item>files go to <c>files/</c>, keyed by URL, and are never downloaded twice;</item>
    /// <item><c>offline</c> never touches the network: whatever is not in the cache fails with a message that says so;</item>
    /// <item>failures are <see cref="DownloadException"/>s; the task decides whether they are warnings (Debug) or errors (Release).</item>
    /// </list>
    /// A new task takes its folder from <c>$(LuxCacheDir)</c> (a sub folder of its own) and its offline switch from <c>$(LuxOffline)</c>.
    /// </summary>
    internal sealed class CachedDownloader
    {
        private readonly INetwork _network;
        private readonly string _cacheDir;
        private readonly TimeSpan _maxAge;
        private readonly bool _offline;
        private readonly Action<string> _log;

        public CachedDownloader(INetwork network, string cacheDir, TimeSpan maxAge, Action<string> log, bool offline = false)
        {
            _network = network;
            _cacheDir = cacheDir;
            _maxAge = maxAge;
            _offline = offline;
            _log = log;
        }

        /// <summary>
        /// Answer of a text API. <paramref name="validate"/> (optional) throws a <see cref="BuildToolException"/> when the body is
        /// not what the caller can use: an answer that fails it is never cached, and a cached answer that fails it (written by an
        /// earlier build) is evicted, so a bad body (an HTML error page served with 200) cannot stay for <c>maxAge</c>.
        /// </summary>
        public string GetText(string url, string? userAgent = null, Action<string>? validate = null)
        {
            var path = Path.Combine(_cacheDir, "meta", Key(url + "\n" + userAgent) + ".txt");
            var cached = TryReadText(path);
            if (cached != null && IsAcceptable(path, cached, validate))
            {
                if (_offline || DateTime.UtcNow - File.GetLastWriteTimeUtc(path) < _maxAge) return cached;
            }
            else
            {
                cached = null;
            }
            if (_offline)
            {
                throw new DownloadException($"Offline build and no cached answer for {url} (cache: {_cacheDir}).");
            }
            string text;
            try
            {
                text = _network.GetText(url, userAgent);
            }
            catch (Exception e) when (cached != null && IsNetworkFailure(e))
            {
                _log($"Network unavailable ({e.Message}); using the cached answer for {url}");
                return cached;
            }
            catch (Exception e) when (IsNetworkFailure(e))
            {
                throw Wrap(e, $"Could not reach {url}");
            }
            validate?.Invoke(text);
            Store(path, Encoding.UTF8.GetBytes(text), overwrite: true);
            return text;
        }

        /// <summary>
        /// A file, downloaded once. <paramref name="validate"/> (optional) throws a <see cref="BuildToolException"/> when the bytes
        /// are not what was expected (for a font: not a font); such a file is never cached, and a cached one that fails it is evicted.
        /// </summary>
        public byte[] GetBytes(string url, Action<byte[]>? validate = null)
        {
            var path = Path.Combine(_cacheDir, "files", Key(url) + ".bin");
            var cached = TryReadBytes(path);
            if (cached != null && IsAcceptable(path, cached, validate)) return cached;
            if (_offline)
            {
                throw new DownloadException($"Offline build and {url} is not in the cache ({_cacheDir}).");
            }
            byte[] bytes;
            try
            {
                bytes = _network.GetBytes(url);
            }
            catch (Exception e) when (IsNetworkFailure(e))
            {
                throw Wrap(e, $"Could not download {url}");
            }
            if (bytes.Length == 0)
            {
                throw new DownloadException($"Empty response for {url}");
            }
            validate?.Invoke(bytes);
            // Keyed by URL, so a file that is already there is the same download: it stays (never deleted under a reader).
            Store(path, bytes, overwrite: false);
            return bytes;
        }

        private Exception Wrap(Exception e, string what) =>
            e is DownloadException { StatusCode: > 0 } ? e
            : new DownloadException($"{what}: {Flatten(e)}. Without network it must already be in the cache ({_cacheDir}); set LuxOffline=true to build from the cache only.", 0);

        private static bool IsNetworkFailure(Exception e) =>
            e is DownloadException || e is HttpRequestException || e is TaskCanceledException || e is IOException || e is WebException
            || e is InvalidOperationException || e is NotSupportedException || e is UriFormatException || e is ArgumentException;

        private static string Flatten(Exception e) => e.InnerException == null ? e.Message : e.Message + " (" + Flatten(e.InnerException) + ")";

        /// <summary>True when the cached content passes; otherwise it is deleted (best effort) so it is fetched again.</summary>
        private static bool IsAcceptable<T>(string path, T content, Action<T>? validate)
        {
            if (validate == null) return true;
            try
            {
                validate(content);
                return true;
            }
            catch (BuildToolException)
            {
                try { File.Delete(path); } catch (Exception e) when (e is IOException || e is UnauthorizedAccessException) { }
                return false;
            }
        }

        // Another build (another project of the solution) may delete or replace a file between the check and the read: a file
        // that vanished is a cache miss, not a failure.
        internal static byte[]? TryReadBytes(string path)
        {
            try
            {
                return File.ReadAllBytes(path);
            }
            catch (Exception e) when (e is FileNotFoundException || e is DirectoryNotFoundException)
            {
                return null;
            }
        }

        private static string? TryReadText(string path)
        {
            var bytes = TryReadBytes(path);
            return bytes == null ? null : Encoding.UTF8.GetString(bytes);
        }

        /// <summary>
        /// Writes through a temporary file, then moves it into place. <c>overwrite: false</c> (files, keyed by URL): an existing
        /// destination is the same download, so the temporary file is dropped. <c>overwrite: true</c> (API answers): the old
        /// answer is replaced; if two writers meet, the last one wins and both wrote a valid answer.
        /// </summary>
        internal static void Store(string path, byte[] bytes, bool overwrite = false)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                // Inside the try: a write that fails half way (disk full) must not leave the temporary file behind.
                File.WriteAllBytes(temp, bytes);
                if (!File.Exists(path))
                {
                    try
                    {
                        File.Move(temp, path);
                        return;
                    }
                    catch (IOException)
                    {
                        // Another writer got there first (the destination appeared between the check and the move).
                    }
                }
                if (overwrite)
                {
                    try
                    {
                        File.Replace(temp, path, null);
                    }
                    catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
                    {
                        // A reader or another writer holds it: the existing answer is as good as this one.
                    }
                }
            }
            finally
            {
                try { if (File.Exists(temp)) File.Delete(temp); } catch (Exception e) when (e is IOException || e is UnauthorizedAccessException) { }
            }
        }

        private static string Key(string text)
        {
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(text));
            return BitConverter.ToString(hash, 0, 12).Replace("-", "").ToLowerInvariant();
        }
    }
}
