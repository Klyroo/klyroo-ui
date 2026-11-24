using System.Globalization;
using System.Text;

namespace Blux.UI.Tools;

/// <summary>
/// Semantic Agent Bridge inspired by <c>ui-sdk.dev</c> (<c>createAgent</c>, <c>snapshot</c>, <c>query</c>, <c>act</c>, <c>observe</c>).
/// Allows AI agents and in-app assistants to read component state, query option lists without opening popups,
/// execute semantic actions with permission checks, and subscribe to a unified <see cref="ObservedChange"/> stream
/// with automatic redaction of sensitive fields.
/// </summary>
public sealed class AgentBridgeService
{
    public const string RedactedValue = "[redacted]";

    private sealed class BoundTarget
    {
        public required string Ref { get; init; }
        public required string Component { get; init; }
        public required string Label { get; init; }
        public required Func<object?> GetValue { get; init; }
        public required IReadOnlyList<string> Actions { get; init; }
        public required Func<IReadOnlyList<AgentChoiceItem>> GetChoices { get; init; }
        public required Func<string, IReadOnlyList<object?>, Task<bool>> ExecuteAction { get; init; }
        public Func<bool>? IsDisabled { get; init; }
        public Func<bool>? IsOffPage { get; init; }
        public bool Sensitive { get; init; }
        public bool ConfirmRequired { get; init; }
    }

    private readonly List<BoundTarget> _targets = [];
    private readonly List<Action<ObservedChange>> _observers = [];
    private int _refCounter;

    /// <summary>Optional global permission gate checked before every <see cref="ActAsync"/> step.</summary>
    public Func<AgentActStep, bool, Task<bool>>? AllowPolicy { get; set; }

    /// <summary>Returns the three standard <c>ui-sdk.dev</c> agent tools (<c>snapshot</c>, <c>query</c>, <c>act</c>).</summary>
    public static IReadOnlyList<AgentToolDefinition> GetToolDefinitions() =>
    [
        new("snapshot", "Lists the components on the page, their current state, and available actions.", ReadOnlyHint: true, ConsequentialHint: false, UntrustedContentHint: true),
        new("query", "Finds components by label or kind, and lists choices inside a select or filter without opening popups.", ReadOnlyHint: true, ConsequentialHint: false, UntrustedContentHint: true),
        new("act", "Calls semantic actions on components by ref or label (e.g. setValue, clear, toggle, reset, submit).", ReadOnlyHint: false, ConsequentialHint: true, UntrustedContentHint: true),
    ];

    /// <summary>Subscribes to all component state changes across registered controllers. Returns an <see cref="IDisposable"/> to unsubscribe.</summary>
    public IDisposable Observe(Action<ObservedChange> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        _observers.Add(callback);
        return new ObserverSubscription(_observers, callback);
    }

    /// <summary>Registers a <see cref="FormController"/> and all its fields with the agent bridge.</summary>
    public void RegisterForm(FormController form, bool confirmSubmit = false)
    {
        ArgumentNullException.ThrowIfNull(form);
        foreach (var field in form.AllFields)
        {
            if (FormController.IsStateless(field.Kind)) continue;
            var fieldRef = NextRef();
            var isPassword = field.Kind is FormFieldKind.Password or FormFieldKind.OneTimeCode;
            var componentKind = field.Kind switch
            {
                FormFieldKind.Select or FormFieldKind.AutoComplete or FormFieldKind.Radio => "select",
                FormFieldKind.Checkbox or FormFieldKind.Switch => "checkbox",
                FormFieldKind.Number or FormFieldKind.Slider => "number",
                _ => "input",
            };
            var actions = componentKind switch
            {
                "select" => new[] { "setValue", "clear", "reset" },
                "checkbox" => new[] { "toggle", "setValue", "reset" },
                _ => new[] { "setValue", "clear", "reset" },
            };

            _targets.Add(new BoundTarget
            {
                Ref = fieldRef,
                Component = componentKind,
                Label = field.Label ?? field.Key,
                Sensitive = isPassword,
                IsDisabled = () => form.IsFieldDisabled(field),
                IsOffPage = () => !form.ShouldRenderField(field),
                GetValue = () => isPassword ? RedactedValue : form.GetValue(field.Key),
                Actions = actions,
                GetChoices = () => form.GetFieldOptions(field)
                    .Select(o => new AgentChoiceItem(
                        o.Value,
                        o.Label,
                        Selected: Equals(form.GetValue(field.Key)?.ToString(), o.Value),
                        Disabled: o.Disabled))
                    .ToList(),
                ExecuteAction = (action, args) =>
                {
                    switch (action)
                    {
                        case "setValue":
                            form.SetValue(field.Key, args.Count > 0 ? args[0] : null, markTouched: true);
                            return Task.FromResult(true);
                        case "clear":
                            form.SetValue(field.Key, field.Kind == FormFieldKind.Checkbox ? false : null, markTouched: true);
                            return Task.FromResult(true);
                        case "toggle":
                            var cur = form.GetValue<bool>(field.Key);
                            form.SetValue(field.Key, !cur, markTouched: true);
                            return Task.FromResult(true);
                        case "reset":
                            form.ResetField(field.Key);
                            return Task.FromResult(true);
                        default:
                            return Task.FromResult(false);
                    }
                },
            });
        }

        // Also register the form root for submit / reset / nextStep / previousStep
        _targets.Add(new BoundTarget
        {
            Ref = NextRef(),
            Component = "form",
            Label = form.Schema.Title ?? form.Schema.FormKey,
            ConfirmRequired = confirmSubmit,
            GetValue = () => form.IsDirty ? "dirty" : "clean",
            Actions = form.IsStepped ? ["submit", "reset", "nextStep", "previousStep"] : ["submit", "reset"],
            GetChoices = () => [],
            ExecuteAction = async (action, _) =>
            {
                switch (action)
                {
                    case "submit":
                        return await form.SubmitAsync();
                    case "reset":
                        form.Reset();
                        return true;
                    case "nextStep":
                        return await form.NextStepAsync();
                    case "previousStep":
                        return await form.PreviousStepAsync();
                    default:
                        return false;
                }
            },
        });
    }

    /// <summary>Registers a <see cref="DashboardController"/> (its filters, views, and refresh action) with the agent bridge.</summary>
    public void RegisterDashboard(DashboardController dashboard)
    {
        ArgumentNullException.ThrowIfNull(dashboard);
        foreach (var filter in dashboard.Controls)
        {
            if (filter.Headless) continue;
            _targets.Add(new BoundTarget
            {
                Ref = NextRef(),
                Component = "select",
                Label = filter.Label,
                IsDisabled = () => !filter.Enabled,
                GetValue = () => filter.Multiple ? filter.Values : filter.Value,
                Actions = ["setValue", "toggle", "clear", "reset"],
                GetChoices = () => filter.Items
                    .Select(i => new AgentChoiceItem(i.Value, i.Label, filter.IsSelected(i.Value), i.Disabled))
                    .ToList(),
                ExecuteAction = (action, args) =>
                {
                    switch (action)
                    {
                        case "setValue":
                        case "toggle":
                            var val = args.Count > 0 ? args[0]?.ToString() : null;
                            if (val is null) return Task.FromResult(false);
                            filter.Toggle(val);
                            return Task.FromResult(true);
                        case "clear":
                        case "reset":
                            filter.Reset();
                            return Task.FromResult(true);
                        default:
                            return Task.FromResult(false);
                    }
                },
            });
        }

        _targets.Add(new BoundTarget
        {
            Ref = NextRef(),
            Component = "dashboard",
            Label = dashboard.Key,
            GetValue = () => dashboard.CurrentView ?? "default",
            Actions = ["setView", "resetFilters", "refresh"],
            GetChoices = () => dashboard.Views
                .Select(v => new AgentChoiceItem(v.Key, v.Label, Selected: dashboard.CurrentView == v.Key))
                .ToList(),
            ExecuteAction = async (action, args) =>
            {
                switch (action)
                {
                    case "setView":
                        if (args.Count == 0 || args[0] is null) return false;
                        dashboard.CurrentView = args[0]!.ToString();
                        return true;
                    case "resetFilters":
                        dashboard.ResetFilters();
                        return true;
                    case "refresh":
                        await dashboard.RefreshAsync();
                        return true;
                    default:
                        return false;
                }
            },
        });
    }

    /// <summary>Registers a <see cref="SpreadsheetImporter"/> with the agent bridge.</summary>
    public void RegisterSpreadsheet(SpreadsheetImporter importer)
    {
        ArgumentNullException.ThrowIfNull(importer);
        _targets.Add(new BoundTarget
        {
            Ref = NextRef(),
            Component = "spreadsheet",
            Label = importer.Schema.Key,
            GetValue = () => importer.CurrentStepKey,
            Actions = ["editCell", "nextStep", "previousStep", "submit", "clearFile"],
            GetChoices = () => [],
            ExecuteAction = async (action, args) =>
            {
                switch (action)
                {
                    case "editCell":
                        if (args.Count < 3) return false;
                        var rIdx = Convert.ToInt32(args[0], CultureInfo.InvariantCulture);
                        var fKey = args[1]?.ToString() ?? "";
                        var text = args[2]?.ToString() ?? "";
                        importer.EditCell(rIdx, fKey, text);
                        return true;
                    case "nextStep":
                        return importer.NextStep();
                    case "previousStep":
                        return importer.PreviousStep();
                    case "submit":
                        await importer.SubmitAsync();
                        return importer.SubmitStatus == SpreadsheetSubmitStatus.Done;
                    case "clearFile":
                        importer.ClearFile();
                        return true;
                    default:
                        return false;
                }
            },
        });
    }

    /// <summary>Returns a semantic snapshot of up to 100 registered components, their state, and their available actions.</summary>
    public IReadOnlyList<AgentComponentSnapshot> Snapshot(bool includeOffPage = true)
    {
        var results = new List<AgentComponentSnapshot>();
        foreach (var t in _targets)
        {
            var offPage = t.IsOffPage?.Invoke() ?? false;
            if (!includeOffPage && offPage) continue;
            var val = t.Sensitive ? RedactedValue : t.GetValue();
            results.Add(new AgentComponentSnapshot(
                t.Ref,
                t.Component,
                t.Label,
                val,
                t.Actions,
                Disabled: t.IsDisabled?.Invoke() ?? false,
                OffPage: offPage,
                ConfirmRequired: t.ConfirmRequired));
            if (results.Count >= 100) break;
        }
        return results;
    }

    /// <summary>Finds components by label or kind, or queries available choices inside a component without opening popups.</summary>
    public IReadOnlyList<AgentQueryMatch> Query(AgentQueryRequest? request = null)
    {
        var req = request ?? new AgentQueryRequest();
        var normWithin = Normalize(req.Within);
        var normText = Normalize(req.Text);
        var normKind = Normalize(req.Kind);

        if (!string.IsNullOrEmpty(normWithin))
        {
            var target = _targets.FirstOrDefault(t =>
                string.Equals(t.Ref, req.Within, StringComparison.OrdinalIgnoreCase)
                || Normalize(t.Label) == normWithin);
            if (target is null) return [];

            var allChoices = target.GetChoices();
            var filtered = string.IsNullOrEmpty(normText)
                ? allChoices
                : allChoices.Where(c => Normalize(c.Label).Contains(normText, StringComparison.Ordinal)
                                        || Normalize(c.Value).Contains(normText, StringComparison.Ordinal)).ToList();
            return
            [
                new AgentQueryMatch(
                    target.Ref,
                    target.Component,
                    target.Label,
                    filtered.Take(50).ToList(),
                    Math.Max(0, filtered.Count - 50))
            ];
        }

        var matches = new List<AgentQueryMatch>();
        foreach (var t in _targets)
        {
            if (!string.IsNullOrEmpty(normKind) && Normalize(t.Component) != normKind) continue;
            var choices = t.GetChoices();
            var matchedChoices = string.IsNullOrEmpty(normText)
                ? choices
                : choices.Where(c => Normalize(c.Label).Contains(normText, StringComparison.Ordinal)
                                     || Normalize(c.Value).Contains(normText, StringComparison.Ordinal)).ToList();
            var labelMatch = string.IsNullOrEmpty(normText) || Normalize(t.Label).Contains(normText, StringComparison.Ordinal);
            if (labelMatch || matchedChoices.Count > 0)
            {
                matches.Add(new AgentQueryMatch(
                    t.Ref,
                    t.Component,
                    t.Label,
                    matchedChoices.Take(50).ToList(),
                    Math.Max(0, matchedChoices.Count - 50)));
            }
        }
        return matches;
    }

    /// <summary>Executes one or more semantic actions in sequence on registered components by ref or case/accent-insensitive label.</summary>
    public async Task<AgentActResult> ActAsync(
        IReadOnlyList<AgentActStep> steps,
        Func<AgentActStep, bool, Task<bool>>? allow = null)
    {
        ArgumentNullException.ThrowIfNull(steps);
        var outcomes = new List<AgentActStepOutcome>();
        var values = new Dictionary<string, object?>(StringComparer.Ordinal);

        for (var i = 0; i < steps.Count; i++)
        {
            var step = steps[i];
            var byRef = _targets.Where(t => string.Equals(t.Ref, step.Ref, StringComparison.Ordinal)).ToList();
            var matches = byRef.Count == 1
                ? byRef
                : _targets.Where(t => Normalize(t.Label) == Normalize(step.Ref)).ToList();

            if (matches.Count == 0)
            {
                return new AgentActResult(false, outcomes, values, $"Step {i + 1}: component \"{step.Ref}\" not found.");
            }
            if (matches.Count > 1)
            {
                var refs = string.Join(", ", matches.Select(m => m.Ref));
                return new AgentActResult(false, outcomes, values, $"Step {i + 1}: ambiguous label \"{step.Ref}\" matched {matches.Count} components ({refs}). Use a specific ref.");
            }

            var target = matches[0];
            if (target.IsDisabled?.Invoke() == true)
            {
                return new AgentActResult(false, outcomes, values, $"Step {i + 1}: component \"{target.Label}\" ({target.Ref}) is disabled.");
            }
            if (!target.Actions.Contains(step.Action, StringComparer.Ordinal))
            {
                return new AgentActResult(false, outcomes, values, $"Step {i + 1}: action \"{step.Action}\" is not supported by \"{target.Label}\" ({target.Ref}).");
            }

            if (AllowPolicy is not null && !await AllowPolicy(step, target.ConfirmRequired))
            {
                return new AgentActResult(false, outcomes, values, $"Step {i + 1}: action \"{step.Action}\" on \"{target.Label}\" was refused by policy.");
            }
            if (allow is not null && !await allow(step, target.ConfirmRequired))
            {
                return new AgentActResult(false, outcomes, values, $"Step {i + 1}: action \"{step.Action}\" on \"{target.Label}\" was refused by caller policy.");
            }

            var previousValue = target.Sensitive ? RedactedValue : target.GetValue();
            var ok = await target.ExecuteAction(step.Action, step.Args ?? []);
            var currentValue = target.Sensitive ? RedactedValue : target.GetValue();
            values[target.Ref] = currentValue;

            if (!ok)
            {
                return new AgentActResult(false, outcomes, values, $"Step {i + 1}: action \"{step.Action}\" on \"{target.Label}\" returned false.");
            }

            outcomes.Add(new AgentActStepOutcome(target.Ref, step.Action, true, currentValue));
            EmitObservedChange(new ObservedChange(
                target.Component,
                target.Ref,
                target.Label,
                "value",
                previousValue,
                currentValue,
                step.Action,
                AgentChangeSource.Agent));
        }

        return new AgentActResult(true, outcomes, values);
    }

    private void EmitObservedChange(ObservedChange change)
    {
        foreach (var obs in _observers.ToList())
        {
            obs(change);
        }
    }

    private string NextRef() => $"c{++_refCounter}";

    private static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        var formD = text.Trim().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(formD.Length);
        var prevSpace = false;
        foreach (var ch in formD)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) continue;
            if (char.IsWhiteSpace(ch))
            {
                if (!prevSpace && sb.Length > 0)
                {
                    sb.Append(' ');
                    prevSpace = true;
                }
            }
            else
            {
                sb.Append(char.ToLowerInvariant(ch));
                prevSpace = false;
            }
        }
        return sb.ToString().Trim();
    }

    private sealed class ObserverSubscription(List<Action<ObservedChange>> list, Action<ObservedChange> callback) : IDisposable
    {
        public void Dispose() => list.Remove(callback);
    }
}
