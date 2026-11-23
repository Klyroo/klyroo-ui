namespace Chat.Shared.Assistant;

/// <summary>A model offered in the picker: the id the service knows it by and the name shown.</summary>
public sealed class ModelOptions
{
    public string Id { get; set; } = "";

    /// <summary>The name shown in the picker; the id when empty.</summary>
    public string? Name { get; set; }
}

/// <summary>
/// The settings of the assistant, from the <c>Assistant</c> section of the configuration (appsettings.json, environment variables, user secrets). The web host
/// reads them; the key and the address of the service never reach a browser, because the browser only talks to the host.
/// </summary>
public sealed class AssistantOptions
{
    public const string OpenAi = "OpenAi";
    public const string Simulated = "Simulated";

    /// <summary><c>OpenAi</c> (a service that speaks the OpenAI completions API) or <c>Simulated</c> (canned replies, no network).</summary>
    public string Provider { get; set; } = OpenAi;

    /// <summary>
    /// The address of the API, without <c>/chat/completions</c>: <c>https://api.openai.com/v1</c>, or the one of a compatible service (a local server, a gateway).
    /// Only the operator sets it, from the configuration: no request can change it.
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";

    /// <summary>The key of the service. Keep it in user secrets or in an environment variable (<c>Assistant__ApiKey</c>), never in a file that is committed. Empty for a service that needs none.</summary>
    public string? ApiKey { get; set; }

    /// <summary>The models of the picker, the first one is the default. The ids of the service you use; the picker offers nothing else, and a request for another id is refused.</summary>
    public List<ModelOptions> Models { get; set; } = [];

    /// <summary>A system message put in front of every conversation, or empty for none.</summary>
    public string? SystemPrompt { get; set; }

    /// <summary>How long one reply may take, whole, in seconds.</summary>
    public int TimeoutSeconds { get; set; } = 120;

    /// <summary>A reply longer than this (in characters) is cut: the stream is closed and what arrived is kept.</summary>
    public int MaxOutputCharacters { get; set; } = 32_000;

    /// <summary>The most messages one request may carry, and the most characters in them. A bigger history is refused.</summary>
    public int MaxMessages { get; set; } = Data.ConversationLimits.MaxMessages;

    /// <summary>The defaults are the limits the pages keep a conversation to (<see cref="Data.ConversationLimits"/>), so a conversation the page lets grow is one the host accepts. If you change one, change the other.</summary>
    public int MaxInputCharacters { get; set; } = Data.ConversationLimits.MaxConversationCharacters;

    /// <summary>
    /// Sent to the service as the cap on the length of an answer, in tokens, so the service stops it too and does not bill more (<see cref="MaxTokensField"/> says under which
    /// name). 0 sends none.
    /// </summary>
    public int MaxOutputTokens { get; set; } = 1_024;

    /// <summary>The name of the field that carries <see cref="MaxOutputTokens"/>: <c>max_tokens</c>, or <c>max_completion_tokens</c> for the models that want that one.</summary>
    public string MaxTokensField { get; set; } = "max_tokens";

    /// <summary>The most replies one caller may have streaming at once.</summary>
    public int MaxConcurrentPerCaller { get; set; } = 2;

    /// <summary>The daily budgets (UTC day) of one caller and of the whole process: replies started, and characters of answer sent (a token is about four characters). Reaching one answers 429 (caller) or 503 (everyone).</summary>
    public int DailyRequestsPerCaller { get; set; } = 200;

    public long DailyOutputCharactersPerCaller { get; set; } = 200_000;

    public int DailyRequestsGlobal { get; set; } = 5_000;

    public long DailyOutputCharactersGlobal { get; set; } = 2_000_000;

    /// <summary>
    /// The name of an ASP.NET Core authorization policy (defined by your app: <c>AddAuthorization</c>). When set, both endpoints and the Server render mode require it. Without one,
    /// anybody who can reach the app can use the key, within the limits above.
    /// </summary>
    public string? AuthorizationPolicy { get; set; }

    /// <summary>The most replies streamed at the same time by the host, whoever asks. More are refused with 503 until one ends.</summary>
    public int MaxConcurrentStreams { get; set; } = 16;

    /// <summary>The most replies one caller may start in a minute: the visitor, as the host sees it (the address of the request, or the session of a circuit).</summary>
    public int RequestsPerMinute { get; set; } = 20;

    /// <summary>The pause between two pieces of a simulated reply, in milliseconds. Only the simulated provider reads it.</summary>
    public int SimulatedDelayMilliseconds { get; set; } = 18;

    public bool IsSimulated => string.Equals(Provider, Simulated, StringComparison.OrdinalIgnoreCase);

    public bool IsOpenAi => string.Equals(Provider, OpenAi, StringComparison.OrdinalIgnoreCase);

    /// <summary>The models of the picker as the pages show them: the configured ones, or the simulated ones.</summary>
    public IReadOnlyList<ModelInfo> ModelList() =>
        IsSimulated
            ? SimulatedCompletionProvider.DemoModels
            : [.. Models.Where(m => !string.IsNullOrWhiteSpace(m.Id)).Select(m => new ModelInfo(m.Id.Trim(), string.IsNullOrWhiteSpace(m.Name) ? m.Id.Trim() : m.Name.Trim()))];

    /// <summary>What is wrong with the settings, in words the operator can act on. Empty when they are usable. Never holds the key.</summary>
    public IReadOnlyList<string> Problems()
    {
        var problems = new List<string>();
        if (!IsOpenAi && !IsSimulated)
        {
            problems.Add($"Assistant:Provider is '{Provider}': use '{OpenAi}' or '{Simulated}'.");
            return problems;
        }
        if (IsOpenAi)
        {
            if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https") || !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
            {
                problems.Add("Assistant:BaseUrl must be an absolute http or https address with no user name, query or fragment, for example https://api.openai.com/v1.");
            }
            if (ModelList().Count == 0)
            {
                problems.Add("Assistant:Models is empty: list at least one model, for example \"Models\": [ { \"Id\": \"your-model-id\", \"Name\": \"Your model\" } ].");
            }
            // A key is optional (a local server needs none), but one sent over plain http to a host that is not this machine can be read on the way.
            if (!string.IsNullOrEmpty(ApiKey) && Uri.TryCreate(BaseUrl, UriKind.Absolute, out var target) && target.Scheme == "http" && !target.IsLoopback)
            {
                problems.Add("Assistant:BaseUrl uses http and an Assistant:ApiKey is set: the key would travel unencrypted. Use https.");
            }
        }
        if (TimeoutSeconds is < 1 or > 600)
        {
            problems.Add("Assistant:TimeoutSeconds must be between 1 and 600.");
        }
        if (MaxOutputCharacters < 100 || MaxMessages < 1 || MaxInputCharacters < 100 || MaxConcurrentStreams < 1 || RequestsPerMinute < 1 || MaxConcurrentPerCaller < 1
            || DailyRequestsPerCaller < 1 || DailyOutputCharactersPerCaller < 1 || DailyRequestsGlobal < 1 || DailyOutputCharactersGlobal < 1 || MaxOutputTokens < 0)
        {
            problems.Add("The limits of the Assistant section (MaxOutputCharacters, MaxMessages, MaxInputCharacters, the concurrency and daily budgets, RequestsPerMinute) must be positive.");
        }
        if (MaxTokensField is not ("max_tokens" or "max_completion_tokens"))
        {
            problems.Add("Assistant:MaxTokensField must be max_tokens or max_completion_tokens.");
        }
        return problems;
    }
}
