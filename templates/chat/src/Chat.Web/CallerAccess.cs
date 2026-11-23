using Chat.Shared.Assistant;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;

namespace Chat.Web;

/// <summary>
/// Gives the circuit of a visitor the same caller key the endpoint would give the same machine: the address of the connection that opened the circuit (grouped by /64
/// for IPv6; behind a proxy the address from <c>ForwardedHeaders</c>, which is why <c>ForwardedHeaders:KnownProxies</c> matters). When the host cannot tell (no
/// HttpContext at that point) the circuit gets the shared "unknown" key.
/// </summary>
public sealed class CallerCircuitHandler(IHttpContextAccessor accessor, CallerKey caller) : CircuitHandler
{
    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        // Fail closed: a circuit whose address cannot be read shares the "unknown" key with every other one, instead of getting limits of its own.
        caller.Value = CallerKey.From(accessor.HttpContext?.Connection.RemoteIpAddress);
        return Task.CompletedTask;
    }
}

/// <summary>The Server render mode has no endpoint to put the policy on: the circuit asks the same policy about the signed-in user.</summary>
public sealed class PolicyAuthorizer(AuthenticationStateProvider state, IAuthorizationService authorization, AssistantOptions options) : IAssistantAuthorizer
{
    public async Task<bool> IsAllowedAsync()
    {
        var user = (await state.GetAuthenticationStateAsync()).User;
        return (await authorization.AuthorizeAsync(user, options.AuthorizationPolicy!)).Succeeded;
    }
}
