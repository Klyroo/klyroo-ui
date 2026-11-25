using System.Net;
using System.Text;

namespace Klyroo.UI.Images.Local.Tests;

public class GeometryTests
{
    [Theory]
    [InlineData(1000, 3000, KlyImageFit.Cover)]
    [InlineData(1000, 3000, KlyImageFit.Contain)]
    [InlineData(1000, 3000, KlyImageFit.Fill)]
    [InlineData(1000, 3000, KlyImageFit.Inside)]
    [InlineData(1000, 3000, KlyImageFit.Outside)]
    [InlineData(2, 1000, KlyImageFit.Cover)]
    [InlineData(2, 1000, KlyImageFit.Contain)]
    [InlineData(2, 1000, KlyImageFit.Outside)]
    [InlineData(100000, 10, KlyImageFit.Cover)]
    [InlineData(100000, 10, KlyImageFit.Outside)]
    [InlineData(100000, 10, KlyImageFit.Contain)]
    [InlineData(1, 1, KlyImageFit.Outside)]
    [InlineData(1, 1, KlyImageFit.Cover)]
    public void No_plan_exceeds_the_maximum_for_any_request(int sourceWidth, int sourceHeight, KlyImageFit fit)
    {
        int?[] sides = [null, 1, 16, 384, 1000, 4096];
        foreach (var w in sides)
        {
            foreach (var h in sides)
            {
                var plan = KlyImageGeometry.Plan(sourceWidth, sourceHeight, w, h, fit, 4096, 4096);

                Assert.InRange(plan.Width, 1, 4096);
                Assert.InRange(plan.Height, 1, 4096);
                if (plan.Kind == KlyPlanKind.Letterbox)
                {
                    Assert.InRange(plan.InnerWidth, 1, plan.Width);
                    Assert.InRange(plan.InnerHeight, 1, plan.Height);
                }
                if (plan.Kind == KlyPlanKind.CropFromSource)
                {
                    Assert.InRange(plan.CropWidth, 1, sourceWidth);
                    Assert.InRange(plan.CropHeight, 1, sourceHeight);
                    Assert.InRange(plan.CropX + plan.CropWidth, 1, sourceWidth);
                    Assert.InRange(plan.CropY + plan.CropHeight, 1, sourceHeight);
                }
            }
        }
    }

    [Fact]
    public void Without_a_size_a_huge_source_is_scaled_down_to_the_maximum()
    {
        var plan = KlyImageGeometry.Plan(10000, 5000, null, null, KlyImageFit.Cover, 4096, 4096);

        Assert.Equal((4096, 2048), (plan.Width, plan.Height));
    }

    [Fact]
    public void A_cover_whose_covering_size_is_enormous_cuts_the_region_out_of_the_source_first()
    {
        var plan = KlyImageGeometry.Plan(2, 1000, 384, 384, KlyImageFit.Cover, 4096, 4096);

        Assert.Equal(KlyPlanKind.CropFromSource, plan.Kind);
        Assert.Equal((384, 384), (plan.Width, plan.Height));
        Assert.Equal((2, 2), (plan.CropWidth, plan.CropHeight));
    }
}

public class SvgSnifferTests
{
    private static bool Is(string text) => KlySvgSniffer.IsSvg(Encoding.UTF8.GetBytes(text));

    [Theory]
    [InlineData("<svg xmlns=\"http://www.w3.org/2000/svg\"/>")]
    [InlineData("  \n<svg>")]
    [InlineData("<?xml version=\"1.0\" encoding=\"UTF-8\"?><svg width=\"1\"></svg>")]
    [InlineData("<!-- a comment with <svg inside --><svg/>")]
    [InlineData("<!DOCTYPE svg PUBLIC \"-//W3C//DTD SVG 1.1//EN\" \"http://www.w3.org/Graphics/SVG/1.1/DTD/svg11.dtd\"><svg/>")]
    [InlineData("<!DOCTYPE svg [ <!ENTITY a \"x > y\"> <!-- ] --> ]><svg/>")]
    [InlineData("<?xml version=\"1.0\"?><?xml-stylesheet href=\"a.css\"?><svg/>")]
    [InlineData("<s:svg xmlns:s=\"http://www.w3.org/2000/svg\"/>")]
    [InlineData("﻿<svg/>")]
    public void Documents_whose_root_element_is_svg_are_svg(string text) => Assert.True(Is(text));

    [Theory]
    [InlineData("")]
    [InlineData("<html><body><svg/></body></html>")]
    [InlineData("<?xml version=\"1.0\"?><feed/>")]
    [InlineData("<svgfoo/>")]
    [InlineData("<SVG/>")]
    [InlineData("not xml <svg/>")]
    [InlineData("<!-- never closed <svg/>")]
    [InlineData("<?xml version=\"1.0\"")]
    public void Anything_else_is_not(string text) => Assert.False(Is(text));

    [Fact]
    public void A_png_with_markup_in_a_text_chunk_is_not_an_svg()
    {
        var png = EndpointFixture.WithTextChunk(EndpointFixture.Png(20, 20), "<svg xmlns=\"http://www.w3.org/2000/svg\"/>");

        Assert.False(KlySvgSniffer.IsSvg(png));
    }

    [Fact]
    public void The_prolog_may_be_long_but_not_longer_than_the_bound()
    {
        Assert.True(Is("<!--" + new string('x', 30_000) + "--><svg/>"));
        Assert.False(Is("<!--" + new string('x', 70_000) + "--><svg/>"));
    }

    [Fact]
    public void Utf16_documents_are_recognized()
    {
        Assert.True(KlySvgSniffer.IsSvg(Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes("<svg/>")).ToArray()));
        Assert.True(KlySvgSniffer.IsSvg(Encoding.BigEndianUnicode.GetPreamble().Concat(Encoding.BigEndianUnicode.GetBytes("<svg/>")).ToArray()));
    }
}

public class FlightTests
{
    [Fact]
    public async Task Callers_of_one_key_share_one_run_and_a_new_key_starts_another()
    {
        var flights = new KlyFlights<int>();
        var runs = 0;
        var gate = new TaskCompletionSource();
        Task<int> Run(string key) => flights.RunAsync(key, async _ => { Interlocked.Increment(ref runs); await gate.Task; return runs; }, CancellationToken.None);

        var a = Run("a");
        var b = Run("a");
        var c = Run("c");
        await Task.Delay(100);
        gate.SetResult();

        Assert.Equal(2, runs);
        Assert.Equal(await a, await b);
    }

    [Fact]
    public async Task Work_is_canceled_only_when_the_last_caller_leaves_and_a_later_caller_starts_fresh()
    {
        var flights = new KlyFlights<int>();
        var started = 0;
        var tokens = new List<CancellationToken>();
        Task<int> Run(CancellationToken caller) => flights.RunAsync("k", async token =>
        {
            lock (tokens) tokens.Add(token);
            Interlocked.Increment(ref started);
            await Task.Delay(Timeout.Infinite, token);
            return 0;
        }, caller);
        using var first = new CancellationTokenSource();
        using var second = new CancellationTokenSource();

        var a = Run(first.Token);
        var b = Run(second.Token);
        await Task.Delay(100);
        first.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => a);
        Assert.False(tokens[0].IsCancellationRequested);
        second.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => b);

        Assert.True(tokens[0].IsCancellationRequested);
        using var third = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Run(third.Token));
        Assert.Equal(2, started);
        Assert.True(tokens[1].IsCancellationRequested);
    }

    [Fact]
    public async Task A_failure_reaches_every_caller_and_is_not_remembered()
    {
        var flights = new KlyFlights<int>();
        var attempts = 0;
        Task<int> Run() => flights.RunAsync("k", async _ =>
        {
            await Task.Delay(50);
            if (Interlocked.Increment(ref attempts) == 1) throw new InvalidOperationException("boom");
            return 7;
        }, CancellationToken.None);

        var first = Run();
        var second = Run();

        await Assert.ThrowsAsync<InvalidOperationException>(() => first);
        await Assert.ThrowsAsync<InvalidOperationException>(() => second);
        Assert.Equal(7, await Run());
    }
}

public class RateLimiterTests
{
    [Theory]
    [InlineData("203.0.113.7", "203.0.113.7")]
    [InlineData("::ffff:203.0.113.7", "203.0.113.7")]
    public void An_ipv4_client_is_its_address(string address, string expected) => Assert.Equal(expected, KlyImageRateLimiter.ClientOf(IPAddress.Parse(address)));

    [Fact]
    public void An_ipv6_client_is_its_64_bit_prefix_and_no_address_is_one_client()
    {
        Assert.Equal(KlyImageRateLimiter.ClientOf(IPAddress.Parse("2001:db8:1:2::1")), KlyImageRateLimiter.ClientOf(IPAddress.Parse("2001:db8:1:2:ffff:ffff:ffff:ffff")));
        Assert.NotEqual(KlyImageRateLimiter.ClientOf(IPAddress.Parse("2001:db8:1:2::1")), KlyImageRateLimiter.ClientOf(IPAddress.Parse("2001:db8:1:3::1")));
        Assert.Equal("unknown", KlyImageRateLimiter.ClientOf(null));
    }
}
