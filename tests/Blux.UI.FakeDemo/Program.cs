var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// The same convention as the templates: the PathBase setting prefixes everything, and the page says so in its <base href>.
var pathBase = "/" + (builder.Configuration["PathBase"] ?? "").Trim('/');
if (pathBase != "/") app.UsePathBase(pathBase);
app.UseRouting();

var unhealthy = Path.Combine(AppContext.BaseDirectory, "unhealthy");
app.MapGet("/", () => File.Exists(unhealthy)
    ? Results.Text("<html><body>not the demo</body></html>", "text/html", statusCode: 200)
    : Results.Text($"<!DOCTYPE html><html><head><base href=\"{pathBase.TrimEnd('/')}/\" /><title>Fake</title></head><body>fake demo</body></html>", "text/html"));
app.MapGet("/env", () => Results.Json(Environment.GetEnvironmentVariables().Cast<System.Collections.DictionaryEntry>().ToDictionary(e => (string)e.Key, e => (string?)e.Value)));

app.Run();
