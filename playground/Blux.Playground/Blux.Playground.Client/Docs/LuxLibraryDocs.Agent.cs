using Blux.UI.Tools;

namespace Blux.Playground.Client.Docs;

public static partial class LuxLibraryDocs
{
    private static LibraryDocTree BuildAgent() => new(
        "agent", "Lux.Agent", "#6366f1",
        [
            new("", "Introduction", "Getting Started",
                "Semantic AI Agent Bridge (snapshot, query, act, observe), 12 composable headless DOM behaviors (lux-behaviors.js), WebMCP tool registration (navigator.modelContext), automatic [redacted] secret masking, permission policies, and bounded UndoStack<T> — inspired by ui-sdk.dev.",
                [
                    new("overview", "What is Lux.Agent?",
                        Paragraphs:
                        [
                            "Classic UI automation scrapes fragile CSS selectors, burns tokens on raw DOM/screenshots, cannot inspect closed select options without clicking popups, and risks leaking password inputs into LLM transcripts.",
                            "Lux.Agent implements the ui-sdk.dev semantic bridge pattern for .NET, Blazor, and .NET MAUI: controllers register their controls once and expose three concise, deterministic tools (snapshot, query, act) plus a unified Observe change stream with guaranteed [redacted] secret masking and explicit permission gates."
                        ],
                        LiveDemoKey: "agent-bridge",
                        LiveDemoTitle: "Lux.Agent · Live Semantic Snapshot, ActAsync & UndoStack<T>"),
                    new("layers", "Two-Layer Package Split",
                        TableHeaders: ["Package", "Primary Exports", "Compatibility"],
                        TableRows:
                        [
                            new("Lux.Agent.Core", "AgentComponentSnapshot, AgentQueryRequest, AgentQueryMatch, AgentChoiceItem, AgentActStep, AgentActResult, ObservedChange, AgentToolDefinition, AgentChangeSource, UndoStack<T>", "Zero dependencies · IsAotCompatible=true"),
                            new("Lux.Agent", "AgentBridgeService, AddLuxAgent(), _content/Lux.Agent/lux-agent.js & _content/Lux.Agent/lux-behaviors.js", "browser, ios, android, maccatalyst, windows")
                        ])
                ]),
            new("installation", "Installation", "Getting Started",
                "Install Lux.Agent, register AgentBridgeService in DI, and optionally load the static browser scripts for WebMCP.",
                [
                    new("nuget", "1. Install Packages",
                        CodeSamples:
                        [
                            new("Terminal", """
                                dotnet add package Lux.Agent.Core   # Zero-dependency NativeAOT models & UndoStack<T>
                                dotnet add package Lux.Agent        # AgentBridgeService + WebMCP & Behaviors JS assets
                                """)
                        ]),
                    new("di", "2. Register Services",
                        CodeSamples: [new("Program.cs / MauiProgram.cs", "builder.Services.AddLuxAgent();")]),
                    new("assets", "3. Static Web Assets (Optional Browser Bridge)",
                        Paragraphs: ["Lux.Agent serves two zero-dependency ES modules directly at _content/Lux.Agent/lux-agent.js and _content/Lux.Agent/lux-behaviors.js for browser-side WebMCP and composable DOM primitives."])
                ]),
            new("semantic-tools", "Snapshot, Query & Act", "Semantic Bridge",
                "Expose three standard MCP / WebMCP tools via AgentBridgeService.GetToolDefinitions() with accent-insensitive label resolution and safety checks.",
                [
                    new("tools", "Tool Contracts & Safety Hints",
                        TableHeaders: ["Tool", "Safety Hints", "Behavior & Limits"],
                        TableRows:
                        [
                            new("snapshot", "ReadOnlyHint=true, ConsequentialHint=false, UntrustedContentHint=true", "Returns up to 100 AgentComponentSnapshot entries (Ref, Component, Label, Value, Actions, Disabled, OffPage, ConfirmRequired)"),
                            new("query", "ReadOnlyHint=true, ConsequentialHint=false, UntrustedContentHint=true", "Finds controls accent-insensitively by Within, Text, or Kind and lists up to 50 AgentChoiceItem entries (+ More overflow count) without opening popups"),
                            new("act", "ReadOnlyHint=false, ConsequentialHint=true, UntrustedContentHint=true", "Executes AgentActStep[] sequentially by Ref (c1) or normalized Label; halts with clear error on not-found, ambiguous label, disabled control, unsupported action, or policy refusal")
                        ],
                        LiveDemoKey: "agent-bridge",
                        LiveDemoTitle: "Try running ActAsync & inspecting the redacted Snapshot",
                        CodeSamples:
                        [
                            new("SemanticLoop.cs", """
                                // Expose tool metadata directly to your LLM / MCP tool registry
                                IReadOnlyList<AgentToolDefinition> tools = AgentBridgeService.GetToolDefinitions();

                                // 1. Inspect current state (pass includeOffPage: false to omit hidden conditional fields)
                                var snap = agent.Snapshot(includeOffPage: false);

                                // 2. Query select options accent-insensitively ("region" matches "Région") without opening any popup
                                var matches = agent.Query(new AgentQueryRequest { Within = "region", Text = "Europe" });

                                // 3. Execute multi-step semantic actions
                                AgentActResult res = await agent.ActAsync([
                                    new AgentActStep("project name", "setValue", ["Atlas"]),
                                    new AgentActStep("region", "setValue", ["eu-west"])
                                ]);
                                """)
                        ]),
                    new("error-handling", "Deterministic Failure Diagnostics",
                        Paragraphs: ["When a step cannot run, ActAsync() stops immediately and returns AgentActResult(Ok: false, Steps, Values, Error) with an actionable error message so an AI agent can self-correct without guessing:"],
                        Bullets:
                        [
                            "Not found: \"Step 1: component \\\"foo\\\" not found.\"",
                            "Ambiguous label: \"Step 1: ambiguous label \\\"Status\\\" matched 2 components (c2, c5). Use a specific ref.\"",
                            "Disabled control: \"Step 1: component \\\"Company\\\" (c3) is disabled.\"",
                            "Unsupported action: \"Step 1: action \\\"submit\\\" is not supported by \\\"Project name\\\" (c1).\"",
                            "Policy refusal: \"Step 1: action \\\"submit\\\" on \\\"Project Settings\\\" was refused by policy.\""
                        ])
                ]),
            new("registrations", "Registering Controllers", "Semantic Bridge",
                "Bind form, dashboard, and spreadsheet controllers to AgentBridgeService in one call each.",
                [
                    new("matrix", "Exact Components & Actions Exposed per Registration",
                        TableHeaders: ["Method", "Exposed Component Kinds", "Supported Semantic Actions"],
                        TableRows:
                        [
                            new("RegisterForm(form, confirmSubmit)", "input, select, checkbox, number, form", "Fields: setValue, clear, toggle, reset · Form root: submit, reset, nextStep, previousStep"),
                            new("RegisterDashboard(dashboard)", "select (non-headless filters), dashboard", "Filters: setValue, toggle, clear, reset · Dashboard root: setView, resetFilters, refresh"),
                            new("RegisterSpreadsheet(importer)", "spreadsheet", "editCell(rowIndex, fieldKey, text), nextStep, previousStep, submit, clearFile")
                        ],
                        CodeSamples:
                        [
                            new("RegisterControllers.cs", """
                                agent.RegisterForm(profileForm, confirmSubmit: true);
                                agent.RegisterDashboard(revenueDashboard);
                                agent.RegisterSpreadsheet(ratesImporter);
                                """)
                        ])
                ]),
            new("policies", "Permission Policies (AllowPolicy)", "Semantic Bridge",
                "Gate consequential actions globally or per call with async user confirmation workflows.",
                [
                    new("gate", "Global AllowPolicy & Per-Call allow Callback",
                        Paragraphs:
                        [
                            "Before executing every AgentActStep, AgentBridgeService awaits both agent.AllowPolicy (if set) and the optional per-call allow delegate passed to ActAsync(steps, allow).",
                            "Both callbacks receive (AgentActStep step, bool confirmRequired), allowing you to auto-approve safe edits while prompting a human confirmation modal for consequential operations like submit."
                        ],
                        CodeSamples:
                        [
                            new("AllowPolicy.cs", """
                                // Global policy: block consequential submit actions unless approved by user
                                agent.AllowPolicy = async (step, confirmRequired) =>
                                {
                                    if (!confirmRequired) return true;
                                    return await confirmDialog.AskAsync($"Allow AI assistant to run '{step.Action}' on '{step.Ref}'?");
                                };

                                // Per-call override (e.g. read-only dry-run mode):
                                var dryRun = await agent.ActAsync(steps, allow: (step, _) => Task.FromResult(false));
                                """)
                        ])
                ]),
            new("observe-and-redaction", "Observe Stream & Secret Redaction", "Semantic Bridge",
                "Subscribe to a unified ObservedChange stream with guaranteed [redacted] protection for sensitive fields.",
                [
                    new("sources", "All 7 AgentChangeSource Values",
                        TableHeaders: ["AgentChangeSource", "Origin", "Usage"],
                        TableRows:
                        [
                            new("Keyboard / Mouse / Touch / Pen", "Direct human input device", "Distinguish user edits from automated mutations"),
                            new("Virtual", "Assistive technology / screen reader", "Preserves accessibility provenance"),
                            new("Programmatic", "Application C# or JS code", "Identifies state changes triggered by app logic"),
                            new("Agent", "AgentBridgeService.ActAsync()", "Automatically tagged on every agent-driven step")
                        ]),
                    new("observe", "Subscribing & Automatic Redaction",
                        CodeSamples:
                        [
                            new("ObserveStream.cs", """
                                using IDisposable sub = agent.Observe(change =>
                                {
                                    logger.LogInformation("[{Source}] {Component} ({Ref}) '{Label}': {Previous} -> {Value} via {Reason}",
                                        change.Source, change.Component, change.Ref, change.Label, change.Previous, change.Value, change.Reason);
                                });
                                """)
                        ],
                        Callout: "Password and OneTimeCode fields always emit AgentBridgeService.RedactedValue (\"[redacted]\") in Snapshot(), ActResult.Values, and ObservedChange — plaintext secrets are never exposed to observers or LLMs.")
                ]),
            new("undo-stack", "UndoStack<T>", "State & Browser",
                "Zero-dependency, bounded generic undo/redo stack in Lux.Agent.Core (NativeAOT compatible).",
                [
                    new("undo", "Complete UndoStack<T> API",
                        Paragraphs: ["UndoStack<T> manages an immutable snapshot timeline with configurable max depth (default 50). Recording a value equal to Current is a no-op; recording a new state appends Current to past history and clears future redo states."],
                        TableHeaders: ["Member", "Signature", "Behavior"],
                        TableRows:
                        [
                            new("Current", "T Current", "Active state at the top of the timeline"),
                            new("CanUndo / CanRedo", "bool", "Whether past / future snapshots are available"),
                            new("Record(next)", "T Record(T next)", "Pushes Current onto past stack (clamped to limit), sets Current = next, clears redo stack"),
                            new("Undo() / Redo()", "T Undo() / T Redo()", "Moves backward or forward one snapshot; safe no-op when empty"),
                            new("Clear()", "void Clear()", "Empties both past and future history while preserving Current")
                        ],
                        CodeSamples:
                        [
                            new("UndoHistory.cs", """
                                var history = new UndoStack<string>("Initial", limit: 50);
                                history.Record("Draft v2");
                                history.Record("Published v3");

                                Assert.True(history.CanUndo);
                                Assert.Equal("Draft v2", history.Undo());
                                Assert.Equal("Published v3", history.Redo());
                                history.Clear();
                                Assert.False(history.CanUndo);
                                """)
                        ])
                ]),
            new("webmcp-and-behaviors", "WebMCP & Composable Behaviors", "State & Browser",
                "Zero-dependency browser ES modules for WebMCP (navigator.modelContext) and 12 composable headless DOM behaviors.",
                [
                    new("behaviors", "All 12 Composable Behaviors in _content/Lux.Agent/lux-behaviors.js",
                        TableHeaders: ["Exported Function", "Options", "Purpose"],
                        TableRows:
                        [
                            new("announce(message, politeness)", "politeness = 'polite' | 'assertive'", "Live-region screen-reader announcements"),
                            new("createUndoStack(opts)", "{ limit = 50, initial = null }", "Browser-side bounded undo/redo stack"),
                            new("createRovingFocus(ctx, opts)", "{ vertical = true, selector }", "Arrow/Home/End roving tabindex across toolbars, menus & tabs"),
                            new("createGridFocus(ctx, opts)", "{ rowSelector, cellSelector }", "2D arrow-key focus navigation across data tables & grids"),
                            new("createTypeahead(opts)", "{ timeout = 500, onMatch }", "Buffered keyboard typeahead matching for selects & lists"),
                            new("createSelection / createPopup", "{ multiple } / { placement, distance, matchWidth }", "Single/multi selection state & positioned floating popups"),
                            new("createReorder / createResize / createViewport / createInlineEdit", "Keyboard + pointer controllers", "Drag/keyboard reordering, column/pane resizing, zoomable canvas, inline cell editing"),
                            new("definePrimitive(definition)", "Custom element factory", "Composes multiple behaviors into one declarative Web Component")
                        ]),
                    new("webmcp", "All 5 Agent Exports in _content/Lux.Agent/lux-agent.js",
                        CodeSamples:
                        [
                            new("WebMcpSetup.js", """
                                import { createAgent, registerWebMcp, observe } from './_content/Lux.Agent/lux-agent.js';

                                const agent = createAgent({ allow: (step, confirm) => !confirm || window.confirm(`Run ${step.action}?`) });
                                registerWebMcp(agent); // Registers snapshot, query, and act on navigator.modelContext

                                const snap = agent.snapshot();
                                const res  = await agent.act([{ ref: 'Project name', action: 'setValue', args: ['Atlas'] }]);
                                const stop = observe(evt => console.log(evt));
                                """)
                        ])
                ]),
            MauiAndAotPage("Lux.Agent", "Lux.Agent.Core", "Lux.Agent", "builder.Services.AddLuxAgent();",
                "var history = new UndoStack<DocumentState>(initial, limit: 50);"),
            new("recipes", "Testing & Recipes", "Platforms",
                "Verify secret redaction, accent-insensitive queries, ambiguity rejection, and policy gates in plain xUnit.",
                [
                    new("xunit", "Complete xUnit Verification Suite",
                        CodeSamples:
                        [
                            new("AgentUiSdkTests.cs", """
                                var services = new ServiceCollection();
                                services.AddLuxAgent();
                                using var provider = services.BuildServiceProvider();
                                var agent = provider.GetRequiredService<AgentBridgeService>();

                                agent.RegisterForm(form, confirmSubmit: true);
                                agent.RegisterDashboard(dashboard);

                                // 1. Passwords are always redacted
                                var snap = agent.Snapshot();
                                Assert.Equal(AgentBridgeService.RedactedValue, snap.First(s => s.Label == "API Secret").Value);

                                // 2. Accent-insensitive query ("region" matches "Région") without opening popups
                                var q = agent.Query(new AgentQueryRequest { Within = "region", Text = "Europe" });
                                Assert.Equal("eu-west", q[0].Items[0].Value);

                                // 3. Policy refusal blocks consequential submit
                                agent.AllowPolicy = (_, confirmRequired) => Task.FromResult(!confirmRequired);
                                var refused = await agent.ActAsync([new AgentActStep("Project Settings", "submit")]);
                                Assert.False(refused.Ok);
                                """)
                        ])
                ]),
            new("api", "API Reference", "Reference",
                "Complete public API reference for Lux.Agent.Core and Lux.Agent.",
                [],
                ApiTypes: [typeof(AgentBridgeService), typeof(AgentComponentSnapshot), typeof(AgentQueryRequest), typeof(AgentQueryMatch), typeof(AgentChoiceItem), typeof(AgentActStep), typeof(AgentActResult), typeof(ObservedChange), typeof(AgentToolDefinition), typeof(UndoStack<object>)])
        ]);
}
