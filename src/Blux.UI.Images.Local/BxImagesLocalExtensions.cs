using Blux.UI;
using Blux.UI.Images;
using Blux.UI.Images.Local;
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
        /// <see cref="BxLocalImagesOptions.MakeDefaultProvider"/> is false). Call it where <c>AddBluxUi</c> is called and map the endpoint
        /// with <c>app.MapBluxImages()</c>. In a WebAssembly client, register only the URL builder: <c>o.Images.AddLocal(endpoint)</c>.
        /// </summary>
        public static IServiceCollection AddBluxImagesLocal(this IServiceCollection services, Action<BxLocalImagesOptions>? configure = null)
        {
            var options = new BxLocalImagesOptions();
            configure?.Invoke(options);
            services.AddSingleton(options);
            services.TryAddSingleton<BxImageProcessor>();
            services.AddBluxUi(o =>
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
        /// <see cref="BxLocalImagesOptions.Endpoint"/> that <see cref="BxImagesLocalServiceCollectionExtensions.AddBluxImagesLocal"/> was given.
        /// </summary>
        public static IEndpointConventionBuilder MapBluxImages(this IEndpointRouteBuilder endpoints, string? pattern = null)
        {
            var options = endpoints.ServiceProvider.GetRequiredService<BxLocalImagesOptions>();
            return endpoints.MapGet(pattern ?? options.Endpoint, async (HttpContext context, BxImageProcessor processor) =>
            {
                var parsed = BxImageRequestParser.Parse(key => context.Request.Query.TryGetValue(key, out var v) ? v.ToString() : null, options);
                if (parsed.Request is not { } request) return Failure(context, parsed.Status, parsed.Message!);

                try
                {
                    var (file, stamp) = processor.Stat(request);
                    if (!request.IsRemote && file is null) return Failure(context, 404, "Image not found.");

                    var etag = processor.ETagOf(request, stamp);
                    if (context.Request.Headers.IfNoneMatch.Any(v => v == etag))
                    {
                        Headers(context, request, options, etag, "image/jpeg", false);
                        return Results.StatusCode(StatusCodes.Status304NotModified);
                    }

                    var result = await processor.GetAsync(request, file, stamp, context.RequestAborted);
                    Headers(context, request, options, result.ETag, result.ContentType, result.Svg);
                    context.Response.ContentLength = new FileInfo(result.Path).Length;
                    await context.Response.SendFileAsync(result.Path, context.RequestAborted);
                    return Results.Empty;
                }
                catch (BxImageException e)
                {
                    return Failure(context, e.Status, e.Message);
                }
            });
        }

        private static IResult Failure(HttpContext context, int status, string message)
        {
            context.Response.Headers[HeaderNames.CacheControl] = "no-store";
            context.Response.Headers[HeaderNames.XContentTypeOptions] = "nosniff";
            if (status == StatusCodes.Status503ServiceUnavailable) context.Response.Headers[HeaderNames.RetryAfter] = "1";
            return Results.Text(message, "text/plain", statusCode: status);
        }

        private static void Headers(HttpContext context, BxImageRequest request, BxLocalImagesOptions options, string etag, string contentType, bool svg)
        {
            var headers = context.Response.Headers;
            headers[HeaderNames.ETag] = etag;
            headers[HeaderNames.XContentTypeOptions] = "nosniff";
            headers[HeaderNames.ContentType] = contentType;
            headers[HeaderNames.ContentDisposition] = "inline";
            // A token in the URL means the address changes when the images do: it can be cached for good.
            headers[HeaderNames.CacheControl] = request.Versioned
                ? "public, max-age=31536000, immutable"
                : $"public, max-age={(int)options.CacheMaxAge.TotalSeconds}";
            if (svg) headers[HeaderNames.ContentSecurityPolicy] = "default-src 'none'; style-src 'unsafe-inline'; sandbox";
        }
    }
}
