# Klyroo.UI.Images

Image providers for the `KlyImage` component of [Klyroo.UI](https://www.nuget.org/packages/Klyroo.UI): they turn a source
path into the URL of a resized, converted image on an image CDN, so `KlyImage`, `KlyAvatar`, `KlyBlogPost` and the other
image components render a responsive `srcset`.

```csharp
builder.Services.AddKlyrooUi(o =>
{
    o.Images.AddCloudinary("my-cloud", makeDefault: true);
    // also: AddImgix, AddImageKit, AddCloudflareImages, AddNetlify, AddVercel, AddBunny, AddTemplate, AddLocal
});
```

Register the same providers on the server and in the WebAssembly client. Each provider's XML documentation names
the vendor documentation page its URL format comes from. `TemplateImageProvider` covers any other service:

```csharp
o.Images.AddTemplate("acme", "https://img.acme.dev/{width}x{height}/q{quality}.{format}?u={encodedSrc}");
```

Remote sources are only sent to providers that fetch them (Cloudflare, Netlify, Vercel, local) when their host is in
`o.Images.Domains`. To resize on your own server, see `Klyroo.UI.Images.Local`.
