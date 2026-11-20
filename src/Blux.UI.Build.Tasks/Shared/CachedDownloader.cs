using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Blux.UI.Build
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

        private static readonly HttpClient Client = Create();

        private static HttpClient Create()
        {
            var handler = new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate };
            return new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(60) };
        }

        public string GetText(string url, string? userAgent)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.TryAddWithoutValidation("User-Agent", userAgent ?? "Blux.UI.Build");
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
    /// A new task takes its folder from <c>$(BluxCacheDir)</c> (a sub folder of its own) and its offline switch from <c>$(BluxOffline)</c>.
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

        public string GetText(string url, string? userAgent = null)
        {
            var path = Path.Combine(_cacheDir, "meta", Key(url + "\n" + userAgent) + ".txt");
            if (File.Exists(path) && (_offline || DateTime.UtcNow - File.GetLastWriteTimeUtc(path) < _maxAge))
            {
                return File.ReadAllText(path, Encoding.UTF8);
            }
            if (_offline)
            {
                throw new DownloadException($"Offline build and no cached answer for {url} (cache: {_cacheDir}).");
            }
            try
            {
                var text = _network.GetText(url, userAgent);
                Store(path, Encoding.UTF8.GetBytes(text));
                return text;
            }
            catch (Exception e) when (File.Exists(path) && IsNetworkFailure(e))
            {
                _log($"Network unavailable ({e.Message}); using the cached answer for {url}");
                return File.ReadAllText(path, Encoding.UTF8);
            }
            catch (Exception e) when (IsNetworkFailure(e))
            {
                throw Wrap(e, $"Could not reach {url}");
            }
        }

        public byte[] GetBytes(string url)
        {
            var path = Path.Combine(_cacheDir, "files", Key(url) + ".bin");
            if (File.Exists(path))
            {
                return File.ReadAllBytes(path);
            }
            if (_offline)
            {
                throw new DownloadException($"Offline build and {url} is not in the cache ({_cacheDir}).");
            }
            try
            {
                var bytes = _network.GetBytes(url);
                if (bytes.Length == 0)
                {
                    throw new DownloadException($"Empty response for {url}");
                }
                Store(path, bytes);
                return bytes;
            }
            catch (Exception e) when (IsNetworkFailure(e))
            {
                throw Wrap(e, $"Could not download {url}");
            }
        }

        private Exception Wrap(Exception e, string what) =>
            e is DownloadException { StatusCode: > 0 } ? e
            : new DownloadException($"{what}: {Flatten(e)}. Without network it must already be in the cache ({_cacheDir}); set BluxOffline=true to build from the cache only.", 0);

        private static bool IsNetworkFailure(Exception e) => e is DownloadException || e is HttpRequestException || e is TaskCanceledException || e is IOException || e is WebException;

        private static string Flatten(Exception e) => e.InnerException == null ? e.Message : e.Message + " (" + Flatten(e.InnerException) + ")";

        private static void Store(string path, byte[] bytes)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            File.WriteAllBytes(temp, bytes);
            if (File.Exists(path)) File.Delete(path);
            File.Move(temp, path);
        }

        private static string Key(string text)
        {
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(text));
            return BitConverter.ToString(hash, 0, 12).Replace("-", "").ToLowerInvariant();
        }
    }
}
