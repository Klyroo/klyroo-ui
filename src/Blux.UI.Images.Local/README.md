# Blux.UI.Images.Local

An ASP.NET Core endpoint that resizes and converts images on your own server with [libvips](https://www.libvips.org/)
(through [NetVips](https://github.com/kleisauke/net-vips)), and the `local` provider that makes `BxImage`,
`BxAvatar`, `BxBlogPost` and the other image components of [Blux UI](https://www.nuget.org/packages/Blux.UI) use it.
It is the equivalent of IPX in Nuxt Image.

```csharp
builder.Services.AddBluxImagesLocal(o =>
{
    o.CacheDirectory = "/var/cache/my-app/images";   // survives restarts
    o.Version = "2026-11-20";                         // changes when the images change: immutable caching
    o.AllowedRemoteHosts.Add("images.example.com");   // none by default: no remote image is fetched
});

var app = builder.Build();
app.MapBluxImages();                                  // GET /_blux/img?src=/img/team.jpg&w=640&f=webp
```

In a Blazor Web App that renders in WebAssembly, register the URL builder in the shared client code too:
`o.Images.AddLocal("/_blux/img", version: "2026-11-20", makeDefault: true)` (package `Blux.UI.Images`, which has no server dependency).

**Security.** The endpoint is meant to face the internet, so every request is bounded.

- Local sources are files below the web root (or `FileProvider`), image extensions only, with dot segments, backslashes and absolute
  paths rejected (400). `/img/a.png`, `/img//a.png` and `img/a.png` are one source.
- Remote images are fetched only from `AllowedRemoteHosts` (403 otherwise), on the default port of the scheme unless the entry names
  one (`images.example.com:8443`), with a size limit, a timeout, redirects only to the same scheme, host and port, no credentials in the
  URL, a content type check, and a refusal of loopback, private, link-local, carrier-grade NAT and other special-use addresses (including
  the IPv4 address embedded in NAT64, 6to4 and IPv4-compatible IPv6 forms). Fetches have their own limit (`MaxRemoteFetches`) and never hold a
  processing slot.
- Width and height are clamped, no output is larger than `MaxWidth` by `MaxHeight` whatever the ratio of the source or the `fit`, a request
  without a size is scaled down to that maximum, widths are rounded up to a fixed list (the height follows) and qualities to multiples of 5.
- Identical requests share one computation. Processing is limited to `MaxConcurrency` images at a time, queued requests give up with 503, and
  libvips is told to stop when no client waits for the result any more. Each client may start `RateLimitPermits` computations per
  `RateLimitWindow` (default 600 per minute, 429 with `Retry-After` beyond; cached variants are free). Clients are told apart by address, so behind
  a reverse proxy call `UseForwardedHeaders` first.
- Each source keeps at most `MaxVariantsPerSource` variants: a new one evicts the least recently used, it is never refused. The disk cache is
  counted in allocated blocks, trimmed in the background by last access, and tolerates files that disappear.
- SVG is recognized by its root element (not by text near the start of the file), served as it is with a sandboxing `Content-Security-Policy`,
  and never handed to libvips unless `RasterizeSvg` is on (off by default: it runs an SVG renderer on the file) and a raster format is requested.
  libvips only loads JPEG, PNG, WebP, AVIF/HEIC and GIF here; the other loaders, and the ones it flags untrusted, are blocked. Animated GIF and WebP
  files are served untouched.

`fit` follows sharp, like Nuxt Image's IPX: with both sides, `cover` and `outside` fill or cover the box and `contain` and `fill` produce exactly
the box even from a smaller source (`contain` pads with transparency, or white for JPEG), `inside` never enlarges; with one side the ratio is kept
and the image is never enlarged. Output formats: `webp`, `avif`, `jpeg`, `png`, `gif` (a single frame).

**Licensing.** `NetVips.Native` bundles the native libvips binaries and the libraries they link, which are
[LGPL](https://github.com/libvips/libvips/blob/master/LICENSE) licensed. NetVips itself is MIT.
