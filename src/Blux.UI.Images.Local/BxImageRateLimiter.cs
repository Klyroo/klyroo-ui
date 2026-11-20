using System.Net;
using System.Net.Sockets;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;

namespace Blux.UI.Images.Local;

/// <summary>
/// A per-client allowance for the requests that cost CPU or bandwidth (cache misses and remote fetches), so junk variants cannot burn the processors.
/// It is used inside the endpoint, so it works without <c>UseRateLimiter()</c> in the pipeline. Idle clients are forgotten by the limiter itself.
/// </summary>
internal sealed class BxImageRateLimiter : IDisposable
{
    private readonly PartitionedRateLimiter<string>? _limiter;
    private readonly int _windowSeconds;

    public BxImageRateLimiter(BxLocalImagesOptions options)
    {
        _windowSeconds = Math.Max(1, (int)Math.Ceiling(options.RateLimitWindow.TotalSeconds));
        if (options.RateLimitPermits <= 0) return;
        _limiter = PartitionedRateLimiter.Create<string, string>(client => RateLimitPartition.GetFixedWindowLimiter(client, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = options.RateLimitPermits,
            Window = options.RateLimitWindow,
            QueueLimit = 0,
            AutoReplenishment = true,
        }));
    }

    /// <summary>Null when the request may go on, otherwise the seconds to wait.</summary>
    public int? Check(HttpContext context)
    {
        if (_limiter is null) return null;
        using var lease = _limiter.AttemptAcquire(ClientOf(context.Connection.RemoteIpAddress));
        if (lease.IsAcquired) return null;
        return lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter) ? Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds)) : _windowSeconds;
    }

    // One client per address; for IPv6 the /64 prefix, which is what a single subscriber gets, so rotating the interface id gains nothing.
    internal static string ClientOf(IPAddress? address)
    {
        if (address is null) return "unknown";
        if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();
        if (address.AddressFamily != AddressFamily.InterNetworkV6) return address.ToString();
        return Convert.ToHexString(address.GetAddressBytes().AsSpan(0, 8));
    }

    public void Dispose() => _limiter?.Dispose();
}
