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

**Security.** Local sources are files below the web root (or `FileProvider`), image extensions only, with dot segments,
backslashes and absolute paths rejected (400). Remote images are fetched only from `AllowedRemoteHosts` (403 otherwise), with
a size limit, a timeout, no redirects to another host, no credentials in the URL, a content type check and a refusal of
loopback and private addresses. Width, height and quality are clamped, widths are rounded up to a fixed list and qualities
to multiples of 5, each image keeps at most 32 variants (429 beyond), processing is limited to one image per processor and
queued requests give up with 503, and the disk cache has a size limit. SVG is served as it is, with a sandboxing
`Content-Security-Policy`, unless a raster format is requested. Animated GIF and WebP files are served untouched.

**Licensing.** `NetVips.Native` bundles the native libvips binaries and the libraries they link, which are
[LGPL](https://github.com/libvips/libvips/blob/master/LICENSE) licensed. NetVips itself is MIT.
