using Blux.UI.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Blux.UI.Server.Tests;

public class RequestCookiesTests
{
    private static (IBxRequestCookies Cookies, IHttpContextAccessor Accessor) Create()
    {
        var provider = new ServiceCollection().AddBluxServer().BuildServiceProvider();
        return (provider.GetRequiredService<IBxRequestCookies>(), provider.GetRequiredService<IHttpContextAccessor>());
    }

    private static HttpContext Request(string? cookie, string? pathBase = null)
    {
        var http = new DefaultHttpContext();
        if (cookie is not null) http.Request.Headers.Cookie = cookie;
        if (pathBase is not null) http.Request.PathBase = pathBase;
        return http;
    }

    [Fact]
    public void AddBluxServer_registers_the_accessor_and_the_cookies()
    {
        var services = new ServiceCollection().AddBluxServer();

        Assert.Contains(services, d => d.ServiceType == typeof(IHttpContextAccessor));
        Assert.Contains(services, d => d.ServiceType == typeof(IBxRequestCookies));
    }

    [Fact]
    public void AddBluxServer_can_run_twice_and_keeps_one_registration()
    {
        var services = new ServiceCollection().AddBluxServer().AddBluxServer();

        Assert.Single(services, d => d.ServiceType == typeof(IBxRequestCookies));
        Assert.Single(services, d => d.ServiceType == typeof(IHttpContextAccessor));
    }

    [Fact]
    public void A_registration_of_the_app_wins_over_the_default()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IBxRequestCookies, Fake>();
        services.AddBluxServer();

        Assert.IsType<Fake>(services.BuildServiceProvider().GetRequiredService<IBxRequestCookies>());
    }

    private sealed class Fake : IBxRequestCookies
    {
        public string? PathBase => null;

        public string? Get(string name) => null;
    }

    [Fact]
    public void It_reads_the_cookies_of_the_current_request()
    {
        var (cookies, accessor) = Create();
        accessor.HttpContext = Request("blux-timezone=Asia/Tokyo; other=1");

        Assert.Equal("Asia/Tokyo", cookies.Get("blux-timezone"));
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

        Assert.Null(cookies.Get("blux-timezone"));
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
        accessor.HttpContext = Request("blux-timezone=Asia/Tokyo; blux-timezone.sub~~=Europe/Paris", "/sub");

        Assert.Equal("Europe/Paris", cookies.Get(BxPathScope.CookieName("blux-timezone", cookies.PathBase)));
    }
}
