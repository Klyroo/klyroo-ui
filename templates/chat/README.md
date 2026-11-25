# Chat

An AI assistant app on Klyroo.UI: a sidebar with the history of conversations (new, search, rename, delete, grouped by the day they were last used), answers that stream in as Markdown with highlighted code blocks and Copy buttons, a prompt that sends with Enter and turns into a Stop button while an answer arrives, a model picker, a greeting with suggestions, and light and dark mode. The same pages run on the web (server first, then WebAssembly) and in a native app (MAUI Blazor Hybrid).

The answers come from any service that speaks the OpenAI API (`POST /chat/completions` with `stream: true`), and the key never reaches a browser. Until you configure one, the app answers from a handful of canned replies (see [Try it without a key](#try-it-without-a-key)).

<!-- rename:start -->
## Make it yours

This repository is a GitHub template. After you create your repository from it, give the app its own name once, from the root of the repository:

```bash
dotnet run rename.cs MyApp
```

`rename.cs` is a .NET 10 file-based app. It renames the solution, the projects and their folders, the namespaces, the MAUI application title and id, and every other occurrence of the template name, then deletes itself. The new name is one identifier: letters and digits, starting with a letter.

<!-- rename:end -->
## Run

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

```bash
dotnet run --project src/Chat.Web
```

The first build downloads three things and keeps them in caches: the Tailwind CLI (once per machine, checked against a SHA-256 that ships with the Klyroo.UI package), the fonts of `Chat.Web.csproj` and the icons your pages name. Later builds, and builds without a network, reuse them.

Out of the box the provider is `OpenAi` and nothing is configured, so the page tells you so. Pick one of the two ways below.

## Try it without a key

Set the provider to `Simulated`:

```bash
Assistant__Provider=Simulated dotnet run --project src/Chat.Web
```

The simulated provider answers from five prepared Markdown replies (a table, code, a list, a quoted email), a word at a time, and a second model sends a few lines of "thinking" first. It makes no network call and holds no state. The suggestions on the greeting, and a question about locks, a debounce method, an email or SQLite, each get their own reply. It is what the live demo of this template runs.

## Use a real service

The `Assistant` section of `src/Chat.Web/appsettings.json` has the settings. The key goes in [user secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets), never in a file that is committed:

```bash
cd src/Chat.Web
dotnet user-secrets init
dotnet user-secrets set "Assistant:ApiKey" "<your key>"
dotnet user-secrets set "Assistant:Models:0:Id" "<the id of a model of your service>"
dotnet user-secrets set "Assistant:Models:0:Name" "<the name shown in the picker>"
```

Or in the environment, which is how a deployment does it: `Assistant__ApiKey`, `Assistant__BaseUrl`, `Assistant__Models__0__Id`.

| Setting | What it does |
| --- | --- |
| `Assistant:Provider` | `OpenAi` (default) or `Simulated`. |
| `Assistant:BaseUrl` | The address of the API, without `/chat/completions`. The default is `https://api.openai.com/v1`; a compatible service (a local server, a gateway) has its own. A plain `http` address is accepted for the local machine only when a key is set. |
| `Assistant:ApiKey` | The key, sent as `Authorization: Bearer`. Empty for a service that needs none. |
| `Assistant:Models` | The models of the picker: `Id` (what the service knows it by) and `Name`. The first is the default. Nothing else is accepted from a browser. |
| `Assistant:SystemPrompt` | A system message put in front of every conversation. |
| `Assistant:TimeoutSeconds`, `MaxOutputCharacters` | How long one answer may take (120 s) and how long it may be (32,000 characters): past it the answer is cut and kept. |
| `Assistant:MaxMessages`, `MaxInputCharacters` | The most a conversation sent to the service may hold (60 messages, 60,000 characters). They are also the limits the pages keep a conversation to (`ConversationLimits`), so keep the two in step if you change one. |
| `Assistant:MaxOutputTokens`, `MaxTokensField` | Sent to the service as the cap on the length of an answer (1,024 tokens, `0` sends none), so the service stops it too. `MaxTokensField` is `max_tokens`, or `max_completion_tokens` for the models that want that name. |
| `Assistant:RequestsPerMinute`, `MaxConcurrentPerCaller`, `MaxConcurrentStreams` | The answers one caller may start in a minute (20), one caller may have streaming at once (2), and everyone together may have streaming at once (16). |
| `Assistant:DailyRequestsPerCaller`, `DailyOutputCharactersPerCaller`, `DailyRequestsGlobal`, `DailyOutputCharactersGlobal` | The budget of a UTC day, per caller (200 answers, 200,000 characters) and for the whole process (5,000 answers, 2,000,000 characters). A token is about four characters. Reaching the caller's budget answers 429, reaching the global one 503. |
| `Assistant:AuthorizationPolicy` | The name of an ASP.NET Core authorization policy that you define (`AddAuthorization`). When set, both endpoints and the Server render mode require it, and the app refuses to start if the policy does not exist. |

The app does not guess model names: list the ids your service offers. If something is wrong with the settings (no model, an address that is not a plain http or https address, a key sent over plain `http` to another machine) the app starts anyway, logs what is wrong (the page only says the assistant is not available).

The OpenAI provider is `HttpClient` and a small parser of the event stream (`Assistant/OpenAiCompletionProvider.cs`, `Assistant/SseReader.cs`), with no SDK: the part of the API the app uses is one request and one stream, and an SDK would add its dependencies to the WebAssembly download and to the native app. It also reads the thinking some services send (`reasoning_content`) and shows it above the answer. To talk to a service that is not OpenAI compatible, write a class that implements `ICompletionProvider` (two members) and register it where `Program.cs` calls `AddAssistantProvider`.

## How the key stays on the server

The browser never talks to the AI service.

- In the **server** render mode the page calls the provider in the host process.
- In the **WebAssembly** render mode the page calls `RemoteCompletionProvider`, which calls the endpoints of the host, `GET api/assistant/models` and `POST api/assistant/stream` (`Chat.Web/AssistantEndpoints.cs`), and the host calls the service.

What a request can carry is a model id from the list and a conversation. The address, the key and the system prompt are settings of the host: nothing in a request changes them, so the endpoint cannot be made to call another address, and the model list it gives back holds ids and names only. The host refuses what the app would never send (a system message, a model that is not on offer, a conversation over the limits, a body over 512 KB, anything but `application/json`: a page of another site cannot post that without a preflight request, which the host does not answer).

**What protects a real key.** The limits above bound what a caller can spend, but they do not make the key safe on their own:

- `AssistantGuard` counts per caller (the address of the request, IPv6 grouped by /64; in the Server render mode the address of the connection that opened the circuit, or one shared key for all circuits whose address cannot be read): answers per minute, answers in flight, and a daily budget of answers and of characters of output, plus the same daily budgets for the whole process. The service is also told the cap on the length of an answer (`MaxOutputTokens`). The daily budgets apply to a real service only, not to the simulated provider.
- **What the budgets count, and where they overshoot.** They count the characters of output, and they are checked when an answer starts, not while it streams. A caller can therefore go past its budget by what its answers in flight still send: up to `MaxConcurrentPerCaller` times `MaxOutputCharacters` (2 x 32,000 by default), and the whole process by up to `MaxConcurrentStreams` times `MaxOutputCharacters` (16 x 32,000). **Input is not budgeted**: what a caller sends is bounded per request (`MaxInputCharacters`, `MaxMessages`) but a day of requests of the largest size is billed as input by the service. Take these figures, not the budgets alone, when you size what a day can cost.
- The counters are in memory, in one process. They start again when the process restarts and are not shared between replicas, so each replica allows the full budgets. The table keeps 10,000 callers; when it is full, the caller whose last request is the oldest, among those with no answer in flight, is forgotten to make room, and starts again with empty counters if it comes back. An attacker with many addresses can therefore reset the counters of idle callers, not of ones that are answering.
- Per address means an address can be shared (an office, a mobile network) and one person can have several. These limits slow abuse down and bound a day as described above; they are not an identity.
- **A public deployment with a real key should add authentication** and name a policy in `Assistant:AuthorizationPolicy`. When a real `Assistant:ApiKey` is set and no policy is, the app logs a warning at startup.

When the service refuses or fails, the visitor reads a generic message; which setting to fix (the key, the address, the model id) is in the log of the host.

Behind a reverse proxy, turn on `ForwardedHeaders:Enabled` and list your proxies in `ForwardedHeaders:KnownProxies` or `ForwardedHeaders:KnownNetworks` (see below). Without it every visitor has the address of the proxy and shares one budget; with a proxy that is not listed, the headers are not believed, for the same reason.

## Where the conversations are kept

In the local storage of the browser (the web view in the native app): one key per conversation, prefixed with the path base of the app, so two apps on one site never read each other's. The server holds nothing, which is also why the visitors of a public deployment cannot see each other's conversations. At most 100 conversations of 60 messages are kept; the oldest goes when a new one needs room. What is read back is checked field by field, because the user can edit it. If the browser blocks or fills its storage, the app says so once and carries on for the visit.

To keep them in a database behind your own sign-in, implement `IConversationStore` (`Services/ConversationStore.cs`, four methods) and register it before `AddChatUi()`. The pages only use `ConversationState`.

## How an answer is rendered

The text of an answer is Markdown, rendered with [Markdig](https://github.com/xoofx/markdig) and treated as hostile, because a model reads text from anywhere. `Markdown/MessageMarkdown.cs` uses the renderer of `Markdown/MarkdownRenderer.cs` with its rules for content from outside:

- raw HTML is shown as text, never rendered;
- links are `http`, `https` or `mailto` only, and open with `rel="noopener noreferrer nofollow"`; anything else loses its address and keeps its text;
- images are dropped, with their alternative text: an address chosen by the model and loaded by the browser is a way to carry the conversation out;
- headings start at level 2, and their ids cannot take an id of the page;
- code blocks are highlighted for a few languages (bash, json, yaml, toml, C#, html) and have a Copy button, brought to life by `installCodeCopy` in `wwwroot/host.js`.

The three files in `Markdown/` besides `MessageMarkdown.cs` are copies of the Markdown code shared by the content templates of Klyroo (`MarkdownRenderer.cs`, `ContentUrl.cs`, `SyntaxHighlighter.cs`): change `MessageMarkdown.cs`, not the copies.

## What is in it

```text
src/
  Chat.Shared/      Razor class library: pages, layout, providers, conversations, styles (Styles/app.css) and the AddChatUi() setup
  Chat.Web/         ASP.NET Core host, global Auto interactivity (server, then WebAssembly), the endpoints of the assistant
  Chat.Web.Client/  the WebAssembly half of the web host
  Chat.Maui/        MAUI Blazor Hybrid app for Android, iOS, Mac Catalyst and Windows
```

| Where | What it holds |
| --- | --- |
| `Pages/Home.razor` | The conversation: navbar, messages, prompt. `/` is a new conversation, `/c/{id}` one from the list. |
| `Layout/` | The sidebar with the history, the rename and delete dialogs. |
| `Components/` | The greeting with its suggestions, one message, the Markdown view. |
| `Assistant/` | `ICompletionProvider` and its three implementations (OpenAI compatible, simulated, remote), the limits (`AssistantGuard`), the settings. |
| `Data/`, `Services/` | The conversations, their JSON and the storage. |

## Develop

Run the app with `dotnet watch`, and Tailwind in a second terminal, so a new utility class shows up at the next reload:

```bash
dotnet watch --project src/Chat.Web
dotnet build src/Chat.Shared -t:KlyrooTailwindWatch
```

In VS Code, the `dev` task (Terminal, Run Task) builds once and starts both. `dotnet watch` does not follow the generated CSS by itself, and it does not reload the page for a CSS-only edit: reload by hand after an edit.

## Native app

The MAUI project needs the MAUI workload of the platform you build for:

```bash
dotnet workload install maui-android      # or maui-ios, maui-maccatalyst, maui-windows
dotnet build src/Chat.Maui -f net10.0-android
```

Because `Chat.slnx` includes the MAUI project, building the solution needs a workload too. To build only the web app, build `src/Chat.Web`, as above. The app targets phones and desktops; Android TV is not set up.

An app on a phone has no server to keep a key: the native app starts with the simulated provider. In `MauiProgram.cs`, point it at the web host of this solution (`AddRemoteAssistant`, the key stays on the server) or, for an app that is yours alone, give it the service and the key directly (`AddAssistantProvider`; anyone who has the app can read a key inside it).

## Serve below a path

Set the `PathBase` setting (an environment variable `PathBase=/assistant`, or `appsettings.json`) to serve the whole app below a prefix, behind a reverse proxy for example. Links in the pages are relative, so they follow, and so do the endpoints of the assistant. The host does not redirect to HTTPS, so that a proxy that terminates TLS works as is; if the app is exposed directly, add `app.UseHttpsRedirection()` and `app.UseHsts()` to `Program.cs`.

### Behind a reverse proxy

A proxy that terminates TLS hands the app plain HTTP on its own address, and tells it the real ones in `X-Forwarded-For`, `X-Forwarded-Proto` and `X-Forwarded-Host`. The app ignores those headers unless you turn them on, because a visitor could send them too. Set `ForwardedHeaders:Enabled` to `true` (the environment variable `ForwardedHeaders__Enabled=true`) and list the addresses of your proxies in `ForwardedHeaders:KnownProxies` (`ForwardedHeaders__KnownProxies__0=10.0.0.5`) and/or whole ranges in `ForwardedHeaders:KnownNetworks`, in CIDR notation (`ForwardedHeaders__KnownNetworks__0=10.0.0.0/8`); a proxy on the same machine (loopback) is trusted without being listed. Headers from any other sender are not believed. If the proxy passes the original `Host` header, also set `AllowedHosts` in `appsettings.json` to your domain. A response that streams must not be buffered by the proxy: the endpoint sends `X-Accel-Buffering: no` for nginx.

## License

MIT, see `LICENSE`.
