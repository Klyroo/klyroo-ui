using System.Text;
using Chat.Shared.Assistant;
using Microsoft.AspNetCore.Http.Features;

namespace Chat.Web;

/// <summary>
/// The two endpoints the WebAssembly half of the app calls (see <see cref="Wire"/> for the messages): the list of models, and the stream of a reply. They are how the
/// browser gets an answer without ever holding the key or the address of the AI service: the host calls the service, the browser calls the host.
/// <para>
/// What a caller can send is a model id from the list, and a conversation. The address of the service, the key and the system prompt are the host's settings; nothing
/// in a request changes them, so the endpoint cannot be made to call another address. The request is checked by <see cref="AssistantGuard"/> (size, roles, model, rate per
/// address, replies in flight) before anything is sent, and the body is read up to <see cref="Wire.MaxRequestBytes"/> only. Only <c>application/json</c> is accepted:
/// a page of another site cannot post that without a preflight request, which this host never answers, so another site cannot use a visitor's browser to call it.
/// </para>
/// </summary>
public static class AssistantEndpoints
{
    public static IEndpointRouteBuilder MapAssistantEndpoints(this IEndpointRouteBuilder app)
    {
        var policy = app.ServiceProvider.GetRequiredService<AssistantOptions>().AuthorizationPolicy;
        var models = app.MapGet("/api/assistant/models", Models);
        var stream = app.MapPost("/api/assistant/stream", Stream);
        if (!string.IsNullOrWhiteSpace(policy))
        {
            // Both endpoints need the policy of the operator (Assistant:AuthorizationPolicy). Nothing else changes: the limits still apply to whoever passes it.
            models.RequireAuthorization(policy);
            stream.RequireAuthorization(policy);
        }
        return app;
    }

    private static async Task Models(HttpContext context, ICompletionProvider provider)
    {
        context.Response.Headers.CacheControl = "no-store";
        try
        {
            var models = await provider.GetModelsAsync(context.RequestAborted);
            context.Response.ContentType = "application/json; charset=utf-8";
            await context.Response.WriteAsync(Wire.Models(models), context.RequestAborted);
        }
        catch (CompletionException error)
        {
            await Refuse(context, error.Status, error.Message);
        }
    }

    private static async Task Stream(HttpContext context, ICompletionProvider provider, CallerKey caller, ILoggerFactory loggers)
    {
        var logger = loggers.CreateLogger("Chat.Web.Assistant");
        context.Response.Headers.CacheControl = "no-store, no-transform";
        // The address of the request, as the host sees it: behind a trusted proxy (ForwardedHeaders:Enabled) it is the visitor's, not the proxy's.
        // IPv6 callers are grouped by /64, see CallerKey.From. The endpoint is only reached when the policy (if there is one) passed.
        caller.Value = CallerKey.From(context.Connection.RemoteIpAddress);
        caller.PreAuthorized = true;

        if (!context.Request.HasJsonContentType())
        {
            await Refuse(context, StatusCodes.Status415UnsupportedMediaType, "Send application/json.");
            return;
        }
        if (context.Request.ContentLength > Wire.MaxRequestBytes)
        {
            await Refuse(context, StatusCodes.Status413PayloadTooLarge, "The request is too big.");
            return;
        }
        var body = await ReadBodyAsync(context.Request.Body, context.RequestAborted);
        if (body is null)
        {
            await Refuse(context, StatusCodes.Status413PayloadTooLarge, "The request is too big.");
            return;
        }
        var request = Wire.ReadRequest(body);
        if (request is null)
        {
            await Refuse(context, StatusCodes.Status400BadRequest, "The request is not a conversation.");
            return;
        }

        // The first piece is read before the answer starts: a request the guard or the service refuses gets its own status, not a stream that ends in an error.
        await using var pieces = provider.StreamAsync(request, context.RequestAborted).GetAsyncEnumerator(context.RequestAborted);
        bool any;
        try
        {
            any = await pieces.MoveNextAsync();
        }
        catch (CompletionException error)
        {
            logger.LogInformation("Reply refused ({Status}): {Message}", error.Status, error.Message);
            await Refuse(context, error.Status, error.Message);
            return;
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception error)
        {
            logger.LogWarning(error, "The assistant failed before it answered");
            await Refuse(context, StatusCodes.Status502BadGateway, "The assistant failed.");
            return;
        }

        context.Response.ContentType = "text/event-stream; charset=utf-8";
        context.Response.Headers["X-Accel-Buffering"] = "no";
        context.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();
        try
        {
            while (any)
            {
                await Write(context, Wire.Event(pieces.Current));
                any = await pieces.MoveNextAsync();
            }
            await Write(context, Wire.Done);
        }
        catch (OperationCanceledException)
        {
            // The browser went away or pressed Stop: nothing more to send.
        }
        catch (CompletionException error)
        {
            await TryWrite(context, Wire.Error(error.Message));
        }
        catch (Exception error)
        {
            logger.LogWarning(error, "The answer was interrupted");
            await TryWrite(context, Wire.Error("The connection to the assistant was interrupted."));
        }
    }

    private static async Task Write(HttpContext context, string data)
    {
        await context.Response.WriteAsync($"data: {data}\n\n", Encoding.UTF8, context.RequestAborted);
        await context.Response.Body.FlushAsync(context.RequestAborted);
    }

    private static async Task TryWrite(HttpContext context, string data)
    {
        try
        {
            await Write(context, data);
        }
        catch (Exception)
        {
            // The connection is gone.
        }
    }

    private static async Task Refuse(HttpContext context, int status, string message)
    {
        if (context.Response.HasStarted)
        {
            return;
        }
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json; charset=utf-8";
        if (status == StatusCodes.Status429TooManyRequests)
        {
            context.Response.Headers.RetryAfter = "30";
        }
        await context.Response.WriteAsync(Wire.Error(message), CancellationToken.None);
    }

    /// <summary>The body up to <see cref="Wire.MaxRequestBytes"/>, or null when it is longer (a request can lie about its length, or send none).</summary>
    private static async Task<byte[]?> ReadBodyAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        var chunk = new byte[16 * 1024];
        int read;
        while ((read = await stream.ReadAsync(chunk, cancellationToken)) > 0)
        {
            if (buffer.Length + read > Wire.MaxRequestBytes)
            {
                return null;
            }
            buffer.Write(chunk, 0, read);
        }
        return buffer.ToArray();
    }
}
