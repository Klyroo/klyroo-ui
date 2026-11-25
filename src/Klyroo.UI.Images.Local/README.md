# Klyroo.UI.Images.Local

An ASP.NET Core endpoint that resizes and converts images on your own server with [libvips](https://www.libvips.org/)
(through [NetVips](https://github.com/kleisauke/net-vips)), and the `local` provider that makes `KlyImage`, `KlyAvatar`,
`KlyBlogPost` and the other image components of [Klyroo.UI](https://www.nuget.org/packages/Klyroo.UI) use it.

    dotnet add package Klyroo.UI.Images.Local --prerelease

```csharp
using Klyroo.UI;

builder.Services.AddKlyrooUi();
builder.Services.AddKlyrooImagesLocal(o =>
{
    o.CacheDirectory = "/var/cache/my-app/images";   // survives restarts
    o.Version = "2026-11-20";                         // changes when the images change: immutable caching
    o.AllowedRemoteHosts.Add("images.example.com");   // none by default: no remote image is fetched
});

var app = builder.Build();
app.MapKlyrooImages();   // GET _klyroo/img?src=/img/team.jpg&w=640&f=webp, below the path base if any
```

In a Blazor Web App that renders in WebAssembly, register the URL builder in the shared client code too:
`o.Images.AddLocal(makeDefault: true)` from `Klyroo.UI.Images`. The endpoint is relative to `<base href>`, so an app served below
a path base reaches its own optimizer; call `app.UseRouting()` right after `UsePathBase`, and `UseForwardedHeaders` first behind a
reverse proxy. The endpoint is meant to face the internet, so every request is bounded: the
[limits](https://github.com/klyroo/klyroo-ui/blob/main/docs/guides.md#limits-of-the-local-endpoint) are in the guide.

`NetVips.Native` bundles the native libvips binaries and the libraries they link, which are
[LGPL](https://github.com/libvips/libvips/blob/master/LICENSE) licensed. NetVips itself is MIT. See the
[repository README](https://github.com/klyroo/klyroo-ui#readme).
