using Lux.UI.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Lux.UI.Server.Tests;

public class RequestCookiesTests
{
    private static (ILxRequestCookies Cookies, IHttpContextAccessor Accessor) Create()
    {
        var provider = new ServiceCollection().AddLuxServer().BuildServiceProvider();
        return (provider.GetRequiredService<ILxRequestCookies>(), provider.GetRequiredService<IHttpContextAccessor>());
    }

    private static HttpContext Request(string? cookie, string? pathBase = null)
    {
        var http = new DefaultHttpContext();
        if (cookie is not null) http.Request.Headers.Cookie = cookie;
        if (pathBase is not null) http.Request.PathBase = pathBase;
        return http;
    }

    [Fact]
    public void AddLuxServer_registers_the_accessor_and_the_cookies()
    {
        var services = new ServiceCollection().AddLuxServer();

        Assert.Contains(services, d => d.ServiceType == typeof(IHttpContextAccessor));
        Assert.Contains(services, d => d.ServiceType == typeof(ILxRequestCookies));
    }

    [Fact]
    public void AddLuxServer_can_run_twice_and_keeps_one_registration()
    {
        var services = new ServiceCollection().AddLuxServer().AddLuxServer();

        Assert.Single(services, d => d.ServiceType == typeof(ILxRequestCookies));
        Assert.Single(services, d => d.ServiceType == typeof(IHttpContextAccessor));
    }

    [Fact]
    public void A_registration_of_the_app_wins_over_the_default()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILxRequestCookies, Fake>();
        services.AddLuxServer();

        Assert.IsType<Fake>(services.BuildServiceProvider().GetRequiredService<ILxRequestCookies>());
    }

    private sealed class Fake : ILxRequestCookies
    {
        public string? PathBase => null;

        public string? Get(string name) => null;
    }

    [Fact]
    public void It_reads_the_cookies_of_the_current_request()
    {
        var (cookies, accessor) = Create();
        accessor.HttpContext = Request("lux-timezone=Asia/Tokyo; other=1");

        Assert.Equal("Asia/Tokyo", cookies.Get("lux-timezone"));
        Assert.Equal("1", cookies.Get("other"));
        Assert.Null(cookies.Get("missing"));
    }

    [Fact]
    public void It_reports_the_path_base_of_the_request()
    {
        var (cookies, accessor) = Create();
        accessor.HttpContext = Request(null, "/sub");

        Assert.Equal("/sub", cookies.PathBase);
    }

    [Fact]
    public void Without_a_request_it_returns_nothing()
    {
        var (cookies, _) = Create();

        Assert.Null(cookies.Get("lux-timezone"));
        Assert.Null(cookies.PathBase);
    }

    [Fact]
    public void It_follows_the_request_that_is_current_when_it_is_asked()
    {
        var (cookies, accessor) = Create();
        accessor.HttpContext = Request("a=1");
        Assert.Equal("1", cookies.Get("a"));

        accessor.HttpContext = Request("a=2");
        Assert.Equal("2", cookies.Get("a"));

        accessor.HttpContext = null;
        Assert.Null(cookies.Get("a"));
    }

    [Fact]
    public void The_scoped_cookie_name_of_a_path_base_finds_the_cookie_of_that_base()
    {
        var (cookies, accessor) = Create();
        accessor.HttpContext = Request("lux-timezone=Asia/Tokyo; lux-timezone.sub~~=Europe/Paris", "/sub");

        Assert.Equal("Europe/Paris", cookies.Get(LxPathScope.CookieName("lux-timezone", cookies.PathBase)));
    }

    [Fact]
    public void Reading_a_cookie_marks_the_response_Cache_Control_private_and_Vary_Cookie_even_when_the_cookie_is_absent()
    {
        var (cookies, accessor) = Create();
        var http = Request(null);
        accessor.HttpContext = http;

        Assert.Null(cookies.Get("lux-timezone"));
        Assert.Equal("private", http.Response.Headers.CacheControl.ToString());
        Assert.Equal("Cookie", http.Response.Headers.Vary.ToString());

        // Reading another cookie on the same request does not duplicate the directives.
        Assert.Null(cookies.Get("lux-dl-cols"));
        Assert.Equal("private", http.Response.Headers.CacheControl.ToString());
        Assert.Equal("Cookie", http.Response.Headers.Vary.ToString());
    }

    [Fact]
    public void Existing_Cache_Control_and_Vary_headers_are_preserved_or_upgraded_to_private()
    {
        var (cookies, accessor) = Create();

        var withMaxAge = Request("lux-timezone=UTC");
        withMaxAge.Response.Headers.CacheControl = "max-age=60";
        withMaxAge.Response.Headers.Vary = "Accept-Encoding";
        accessor.HttpContext = withMaxAge;
        cookies.Get("lux-timezone");
        Assert.Equal("private, max-age=60", withMaxAge.Response.Headers.CacheControl.ToString());
        Assert.Equal("Accept-Encoding, Cookie", withMaxAge.Response.Headers.Vary.ToString());

        var withPublic = Request("lux-timezone=UTC");
        withPublic.Response.Headers.CacheControl = "public, max-age=120";
        accessor.HttpContext = withPublic;
        cookies.Get("lux-timezone");
        Assert.Equal("private, max-age=120", withPublic.Response.Headers.CacheControl.ToString());

        var withNoStore = Request("lux-timezone=UTC");
        withNoStore.Response.Headers.CacheControl = "no-store, no-cache";
        withNoStore.Response.Headers.Vary = "*";
        accessor.HttpContext = withNoStore;
        cookies.Get("lux-timezone");
        Assert.Equal("no-store, no-cache", withNoStore.Response.Headers.CacheControl.ToString());
        Assert.Equal("*", withNoStore.Response.Headers.Vary.ToString());
    }

    [Fact]
    public void SetCacheHeaders_false_leaves_response_headers_untouched()
    {
        var provider = new ServiceCollection().AddLuxServer(o => o.SetCacheHeaders = false).BuildServiceProvider();
        var cookies = provider.GetRequiredService<ILxRequestCookies>();
        var accessor = provider.GetRequiredService<IHttpContextAccessor>();
        var http = Request("lux-timezone=UTC");
        accessor.HttpContext = http;

        Assert.Equal("UTC", cookies.Get("lux-timezone"));
        Assert.Empty(http.Response.Headers.CacheControl.ToString());
        Assert.Empty(http.Response.Headers.Vary.ToString());
    }
}
