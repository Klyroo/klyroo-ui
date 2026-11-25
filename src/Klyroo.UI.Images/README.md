# Klyroo.UI.Images

Image providers for the `KlyImage` component of [Klyroo.UI](https://www.nuget.org/packages/Klyroo.UI): they turn a source
path into the URL of a resized, converted image on an image CDN, so `KlyImage`, `KlyAvatar`, `KlyBlogPost` and the other
image components render a responsive `srcset`.

```bash
dotnet add package Klyroo.UI.Images --prerelease
```

Register a provider in `AddKlyrooUi`, on the server and in the WebAssembly client alike:

```csharp
using Klyroo.UI;
using Klyroo.UI.Images;

builder.Services.AddKlyrooUi(o =>
{
    o.Images.AddCloudinary("my-cloud", makeDefault: true);
    // also: AddImgix, AddImageKit, AddCloudflareImages, AddNetlify, AddVercel, AddBunny, AddTemplate, AddLocal
});
```

Each provider's XML documentation names the vendor page its URL format comes from. `AddTemplate` covers any other service:
`o.Images.AddTemplate("acme", "https://img.acme.dev/{width}x{height}/q{quality}.{format}?u={encodedSrc}")`.

A remote source goes only to the providers that fetch remote images (Cloudflare, Netlify, Vercel and local), and only when its
host is in `o.Images.Domains`. To resize on your own server, use `Klyroo.UI.Images.Local`. See the
[repository README](https://github.com/klyroo/klyroo-ui#readme) and the
[guide](https://github.com/klyroo/klyroo-ui/blob/main/docs/guides.md#serve-responsive-images).
