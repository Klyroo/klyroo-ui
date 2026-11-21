using Microsoft.Extensions.Primitives;
using Yarp.ReverseProxy.Configuration;

namespace Blux.Playground.Templates;

/// <summary>
/// The YARP configuration of the template demos: one route and one cluster per template, each cluster with the single destination the supervisor
/// last started the demo on. The port changes when a demo restarts, so the configuration is replaced then (YARP reloads on the change token) instead
/// of being fixed at startup.
/// </summary>
public sealed class TemplateProxyConfigProvider : IProxyConfigProvider
{
    private readonly IReadOnlyList<string> _names;
    private readonly string? _docsPathBase;
    private readonly Dictionary<string, int> _ports = [];
    private readonly object _gate = new();
    private Snapshot _current;

    public TemplateProxyConfigProvider(IEnumerable<string> names, string? docsPathBase)
    {
        _names = [.. names];
        _docsPathBase = string.IsNullOrWhiteSpace(docsPathBase) ? null : "/" + docsPathBase.Trim('/');
        _current = Build();
    }

    public IProxyConfig GetConfig() => _current;

    /// <summary>Points the demo at a port, or at nothing (requests then fail with 503) while its process is down.</summary>
    public void SetPort(string name, int? port)
    {
        Snapshot previous;
        lock (_gate)
        {
            if (port is { } p) _ports[name] = p; else _ports.Remove(name);
            previous = _current;
            _current = Build();
        }
        previous.Expire();
    }

    private Snapshot Build()
    {
        var routes = new List<RouteConfig>();
        var clusters = new List<ClusterConfig>();
        foreach (var name in _names)
        {
            // The docs site can itself be served below a prefix: its UsePathBase strips it, and the demo (started with that prefix in its own
            // path base) expects it back.
            var transforms = new List<IReadOnlyDictionary<string, string>>
            {
                // The demo sees the host the browser asked for, so the absolute addresses it builds are the ones of the docs site.
                new Dictionary<string, string> { ["RequestHeaderOriginalHost"] = "true" },
            };
            if (_docsPathBase is not null) transforms.Insert(0, new Dictionary<string, string> { ["PathPrefix"] = _docsPathBase });

            var path = TemplateLaunch.DemoPath(name);
            // Two routes: the demo with or without the trailing slash, and everything below it.
            routes.Add(new RouteConfig
            {
                RouteId = $"template-{name}",
                ClusterId = $"template-{name}",
                Match = new RouteMatch { Path = path + "/{**rest}" },
                Transforms = transforms,
            });
            routes.Add(new RouteConfig
            {
                RouteId = $"template-{name}-root",
                ClusterId = $"template-{name}",
                Match = new RouteMatch { Path = path },
                Transforms = transforms,
            });
            clusters.Add(new ClusterConfig
            {
                ClusterId = $"template-{name}",
                Destinations = _ports.TryGetValue(name, out var port)
                    ? new Dictionary<string, DestinationConfig> { ["demo"] = new() { Address = TemplateLaunch.Destination(port) } }
                    : null,
            });
        }
        return new Snapshot(routes, clusters);
    }

    private sealed class Snapshot : IProxyConfig
    {
        private readonly CancellationTokenSource _expired = new();

        public Snapshot(IReadOnlyList<RouteConfig> routes, IReadOnlyList<ClusterConfig> clusters)
        {
            Routes = routes;
            Clusters = clusters;
            ChangeToken = new CancellationChangeToken(_expired.Token);
        }

        public IReadOnlyList<RouteConfig> Routes { get; }
        public IReadOnlyList<ClusterConfig> Clusters { get; }
        public IChangeToken ChangeToken { get; }

        public void Expire() => _expired.Cancel();
    }
}
