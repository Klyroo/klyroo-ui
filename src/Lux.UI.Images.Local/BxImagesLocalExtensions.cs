using Lux.UI;
using Lux.UI.Images;
using Lux.UI.Images.Local;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Net.Http.Headers;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class BxImagesLocalServiceCollectionExtensions
    {
        /// <summary>
        /// Registers the local image optimizer and the <c>local</c> provider of <c>BxImage</c> (the default provider unless
        /// <see cref="BxLocalImagesOptions.MakeDefaultProvider"/> is false). Call it where <c>AddLuxUi</c> is called and map the endpoint
        /// with <c>app.MapLuxImages()</c>. In a WebAssembly client, register only the URL builder: <c>o.Images.AddLocal(endpoint)</c>.
        /// </summary>
        public static IServiceCollection AddLuxImagesLocal(this IServiceCollection services, Action<BxLocalImagesOptions>? configure = null)
        {
            var options = new BxLocalImagesOptions();
            configure?.Invoke(options);
            services.AddSingleton(options);
            services.TryAddSingleton<BxImageProcessor>();
            services.TryAddSingleton<BxImageRateLimiter>();
            services.AddLuxUi(o =>
            {
                foreach (var host in options.AllowedRemoteHosts) o.Images.Domains.Add(host);
                o.Images.AddLocal(options.Endpoint, options.Version, options.MakeDefaultProvider);
            });
            return services;
        }
    }
}

namespace Microsoft.AspNetCore.Builder
{
    public static class BxImagesLocalEndpointExtensions
    {
        /// <summary>
        /// Maps the image endpoint (<c>GET /_blux/img?src=/img/a.png&amp;w=640&amp;f=webp</c>). Defaults to the
        /// <see cref="BxLocalImagesOptions.Endpoint"/> that <see cref="BxImagesLocalServiceCollectionExtensions.AddLuxImagesLocal"/> was given.
        /// The pattern is relative to the path base: with <c>app.UsePathBase("/x")</c> the endpoint answers at <c>/x/_blux/img</c>, where the
        /// relative URLs of <see cref="BxLocalImageProvider"/> lead. Call <c>app.UseRouting()</c> right after <c>UsePathBase</c>, because
        /// <c>WebApplication</c> otherwise adds routing before it and the prefix is never stripped.
        /// </summary>
        public static IEndpointConventionBuilder MapLuxImages(this IEndpointRouteBuilder endpoints, string? pattern = null)
        {
            var options = endpoints.ServiceProvider.GetRequiredService<BxLocalImagesOptions>();
            return endpoints.MapGet(pattern ?? options.Endpoint, async (HttpContext context, BxImageProcessor processor, BxImageRateLimiter limiter) =>
            {
                var parsed = BxImageRequestParser.Parse(key => context.Request.Query.TryGetValue(key, out var v) ? v.ToString() : null, options);
                if (parsed.Request is not { } request) return Failure(context, parsed.Status, parsed.Message!);

                try
                {
                    var (file, stamp) = processor.Stat(request);
                    if (!request.IsRemote && file is null) return Failure(context, 404, "Image not found.");

                    if (stamp is not null)
                    {
                        // A validator the client holds is answered without computing anything, but only for a stamp that is still valid:
                        // a remote image past its TTL is asked of its origin again first.
                        var etag = processor.ETagOf(request, stamp);
                        if (Matches(context, etag))
                        {
                            var cached = processor.FindCached(request, stamp);
                            return NotModified(context, request, options, etag, cached is null ? request.Format == "svg" : cached.Value.ContentType == "image/svg+xml");
                        }
                    }

                    // Charged only when this call really has to compute or fetch; the limiter then answers 429 with Retry-After.
                    void Admit()
                    {
                        if (limiter.Check(context) is { } retryAfter) throw new BxImageException(429, "Too many requests.", retryAfter);
                    }

                    // A cache file can be evicted between the lookup and the read: ask again (which recomputes) instead of failing.
                    for (var attempt = 0; attempt < 3; attempt++)
                    {
                        var result = await processor.GetAsync(request, file, stamp, Admit, context.RequestAborted);
                        if (Matches(context, result.ETag)) return NotModified(context, request, options, result.ETag, result.Svg);

                        if (result.Bytes is { } bytes)
                        {
                            Headers(context, request, options, result.ETag, result.ContentType, result.Svg);
                            context.Response.ContentLength = bytes.Length;
                            await context.Response.Body.WriteAsync(bytes, context.RequestAborted);
                            return Results.Empty;
                        }

                        FileStream stream;
                        try
                        {
                            stream = new FileStream(result.Path!, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
                        }
                        catch (Exception e) when (e is FileNotFoundException or DirectoryNotFoundException)
                        {
                            continue;
                        }
                        await using (stream)
                        {
                            Headers(context, request, options, result.ETag, result.ContentType, result.Svg);
                            context.Response.ContentLength = stream.Length;
                            await stream.CopyToAsync(context.Response.Body, context.RequestAborted);
                        }
                        return Results.Empty;
                    }
                    return Failure(context, 503, "The image is being updated, try again.");
                }
                catch (BxImageException e)
                {
                    return Failure(context, e.Status, e.Message, e.RetryAfterSeconds);
                }
                catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
                {
                    return Results.Empty;
                }
            });
        }

        private static bool Matches(HttpContext context, string etag)
        {
            foreach (var value in context.Request.GetTypedHeaders().IfNoneMatch)
            {
                if (value.Tag.Value == etag) return true;
            }
            return false;
        }

        // A 304 carries the headers of the 200 that matter to a cache (validator, caching, policy) and no content type: it has no body, and the
        // type of the stored response stays the one it was stored with.
        private static IResult NotModified(HttpContext context, BxImageRequest request, BxLocalImagesOptions options, string etag, bool svg)
        {
            Headers(context, request, options, etag, null, svg);
            context.Response.Headers.Remove(HeaderNames.ContentDisposition);
            return Results.StatusCode(StatusCodes.Status304NotModified);
        }

        private static IResult Failure(HttpContext context, int status, string message, int? retryAfter = null)
        {
            context.Response.Headers[HeaderNames.CacheControl] = "no-store";
            context.Response.Headers[HeaderNames.XContentTypeOptions] = "nosniff";
            if (retryAfter is { } seconds) context.Response.Headers[HeaderNames.RetryAfter] = seconds.ToString(System.Globalization.CultureInfo.InvariantCulture);
            else if (status == StatusCodes.Status503ServiceUnavailable) context.Response.Headers[HeaderNames.RetryAfter] = "1";
            return Results.Text(message, "text/plain", statusCode: status);
        }

        private static void Headers(HttpContext context, BxImageRequest request, BxLocalImagesOptions options, string etag, string? contentType, bool svg)
        {
            var headers = context.Response.Headers;
            headers[HeaderNames.ETag] = etag;
            headers[HeaderNames.XContentTypeOptions] = "nosniff";
            if (contentType is not null) headers[HeaderNames.ContentType] = contentType;
            headers[HeaderNames.ContentDisposition] = "inline";
            // A token in the URL means the address changes when the images do: it can be cached for good.
            headers[HeaderNames.CacheControl] = request.Versioned
                ? "public, max-age=31536000, immutable"
                : $"public, max-age={(int)options.CacheMaxAge.TotalSeconds}";
            if (svg) headers[HeaderNames.ContentSecurityPolicy] = "default-src 'none'; style-src 'unsafe-inline'; sandbox";
        }
    }
}
