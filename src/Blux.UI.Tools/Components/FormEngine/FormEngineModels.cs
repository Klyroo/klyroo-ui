using System.Globalization;

namespace Blux.UI.Tools;

/// <summary>Validation mode of a <see cref="FormController"/>.</summary>
public enum FormValidationMode
{
    All,
    RequiredOnly,
    ValidatorsOnly,
    None,
}

/// <summary>Validation trigger timing for a <see cref="FormFieldSchema"/>.</summary>
public enum FormValidationTrigger
{
    Blur,
    Input,
    Submit,
}

/// <summary>Shell layout of a <see cref="BxFormEngine"/> or <see cref="FormService"/> overlay.</summary>
public enum FormShellMode
{
    Inline,
    Modal,
    Drawer,
    Fullscreen,
}

/// <summary>Completion status of a <see cref="FormPageSectionSchema"/> in <see cref="BxFormPage"/>.</summary>
public enum FormPageSectionStatus
{
    Complete,
    Pending,
    Invalid,
    Optional,
}

/// <summary>Supported field kinds in the schema-driven Form Engine.</summary>
public enum FormFieldKind
{
    Text,
    Password,
    Textarea,
    Number,
    Checkbox,
    Switch,
    Radio,
    CheckboxGroup,
    Select,
    AutoComplete,
    Date,
    DateTime,
    DateRange,
    Time,
    PhoneNumber,
    Slider,
    Rating,
    ColorPicker,
    OneTimeCode,
    Tag,
    Hidden,
    Info,
    Divider,
    Section,
    Button,
    Group,
    Card,
    Tabs,
    Object,
    ArrayList,
    ArrayTable,
}

/// <summary>An option item for choice and select fields in <see cref="BxFormEngine"/>.</summary>
public sealed record FormOptionItem(
    string Value,
    string Label,
    string? Description = null,
    string? Group = null,
    string? Icon = null,
    bool Disabled = false);

/// <summary>A password requirement rule on a <see cref="FormFieldKind.Password"/> field.</summary>
public sealed record FormPasswordRequirement(
    string Key,
    string Label,
    Func<string, bool> Validate);

/// <summary>A named validator rule on a <see cref="FormFieldSchema"/>.</summary>
public sealed record FormValidatorRule(
    string Key,
    string Message,
    Func<object?, FormController, bool> Validate);

/// <summary>A tab definition inside a <see cref="FormFieldKind.Tabs"/> container field.</summary>
public sealed class FormTabItemSchema
{
    public required string Key { get; init; }
    public required string Label { get; init; }
    public IReadOnlyList<FormFieldSchema> Fields { get; init; } = [];
    public Func<FormController, bool>? Condition { get; init; }
}

/// <summary>Declarative definition of a field in <see cref="FormSchema"/>.</summary>
public sealed class FormFieldSchema
{
    public required string Key { get; init; }
    public FormFieldKind Kind { get; init; } = FormFieldKind.Text;
    public string? Label { get; init; }
    public string? Description { get; init; }
    public string? Hint { get; init; }
    public string? Help { get; init; }
    public string? Placeholder { get; init; }
    public object? DefaultValue { get; init; }
    public Func<FormController, object?>? DefaultFactory { get; init; }

    public bool Required { get; init; }
    public Func<FormController, bool>? RequiredWhen { get; init; }
    public string? RequiredMessage { get; init; }
    public FormValidationTrigger Trigger { get; init; } = FormValidationTrigger.Blur;

    public bool Disabled { get; init; }
    public Func<FormController, bool>? DisabledWhen { get; init; }
    public string? DisabledReason { get; init; }

    public Func<FormController, bool>? Condition { get; init; }
    public bool Ignore { get; init; }
    public bool OmitFromOutput { get; init; }
    public bool DirtyCheck { get; init; }
    public bool Collapsible { get; init; }
    public bool DefaultCollapsed { get; init; }
    public int Span { get; init; } = 12;

    // Bounds & rules
    public double? Min { get; init; }
    public double? Max { get; init; }
    public double? Step { get; init; }
    public int? MaxLength { get; init; }
    public bool Multiple { get; init; }
    public bool Clearable { get; init; }
    public IReadOnlyList<FormOptionItem>? Options { get; init; }
    public Func<FormController, IReadOnlyList<FormOptionItem>>? OptionsFactory { get; init; }
    public Func<string, FormOptionItem?>? OnCreateOption { get; init; }
    public IReadOnlyList<FormPasswordRequirement>? PasswordRequirements { get; init; }
    public IReadOnlyList<FormValidatorRule>? Validators { get; init; }

    // Transforms & watchers
    public Func<object?, object?>? TransformInput { get; init; }
    public Func<object?, object?>? TransformOutput { get; init; }
    public Action<object?, FormController>? Watch { get; init; }

    // Display / Info block
    public DisplayKind? DisplayKind { get; init; }
    public string? DisplayText { get; init; }
    public BxColor? DisplayTone { get; init; }

    // Container / Array children
    public IReadOnlyList<FormFieldSchema>? Fields { get; init; }
    public IReadOnlyList<FormTabItemSchema>? Tabs { get; init; }
    public string? AddItemLabel { get; init; }
}

/// <summary>One step in a multi-step <see cref="FormSchema"/>.</summary>
public sealed class FormStepSchema
{
    public required string Key { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public string? Root { get; init; }
    public IReadOnlyList<FormFieldSchema> Fields { get; init; } = [];
    public Func<FormController, bool>? SkipWhen { get; init; }
}

/// <summary>One section in a <see cref="BxFormPage"/> schema.</summary>
public sealed class FormPageSectionSchema
{
    public required string Key { get; init; }
    public required string Label { get; init; }
    public string? Description { get; init; }
    public bool Optional { get; init; }
    public int Span { get; init; } = 12;
    public Func<FormController, bool>? Condition { get; init; }
    public IReadOnlyList<FormFieldSchema> Fields { get; init; } = [];
}

/// <summary>Computed runtime state of a <see cref="FormPageSectionSchema"/>.</summary>
public sealed record FormPageSectionState(
    string Key,
    string Label,
    string? Description,
    bool Optional,
    int RequiredCount,
    int MissingCount,
    bool Dirty,
    FormPageSectionStatus Status);

/// <summary>Top-level declarative schema for <see cref="BxFormEngine"/>, <see cref="BxFormPage"/> and <see cref="FormService"/>.</summary>
public sealed class FormSchema
{
    public string FormKey { get; init; } = "form";
    public string? Title { get; init; }
    public string? Description { get; init; }
    public string? Eyebrow { get; init; }
    public int Columns { get; init; } = 12;
    public bool ShowStepper { get; init; } = true;
    public FormValidationMode ValidateMode { get; init; } = FormValidationMode.All;
    public IReadOnlyList<string>? IgnoreDirtyPaths { get; init; }
    public IReadOnlyList<FormFieldSchema> Fields { get; init; } = [];
    public IReadOnlyList<FormStepSchema> Steps { get; init; } = [];
    public IReadOnlyList<FormPageSectionSchema> Sections { get; init; } = [];

    public Func<FormController, Task<bool>>? OnBeforeSubmit { get; init; }
    public Func<FormController, Task>? OnSubmit { get; init; }
    public Func<FormController, int, Task<bool>>? OnBeforeNext { get; init; }
    public Func<FormController, int, Task<bool>>? OnBeforePrevious { get; init; }
}

/// <summary>Result returned when a form submit or overlay session completes.</summary>
public sealed record FormSubmitOutcome(
    bool IsCompleted,
    IReadOnlyDictionary<string, object?> FormData);

/// <summary>
/// Stateful runtime controller for a <see cref="FormSchema"/>: manages internal state, initial snapshot,
/// dirty path tracking, touch &amp; validation rules, multi-step navigation, array item management, and submission.
/// </summary>
public sealed class FormController
{
    private readonly Dictionary<string, object?> _state = new(StringComparer.Ordinal);
    private readonly Dictionary<string, object?> _initialSnapshot = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _validationErrors = new(StringComparer.Ordinal);
    private readonly Dictionary<string, (string Message, bool Blocking)> _customErrors = new(StringComparer.Ordinal);
    private readonly HashSet<string> _touchedPaths = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<FormOptionItem>> _createdOptions = new(StringComparer.Ordinal);

    public FormController(FormSchema schema, IReadOnlyDictionary<string, object?>? initialInput = null)
    {
        ArgumentNullException.ThrowIfNull(schema);
        Schema = schema;
        InitializeState(initialInput);
    }

    public event Action? Changed;

    public FormSchema Schema { get; }
    public int CurrentStepIndex { get; private set; }
    public bool IsSubmitting { get; private set; }
    public bool IsStepped => Schema.Steps.Count > 0;
    public FormStepSchema? CurrentStep => IsStepped && CurrentStepIndex < Schema.Steps.Count ? Schema.Steps[CurrentStepIndex] : null;
    public bool IsFirstStep => CurrentStepIndex <= 0;
    public bool IsLastStep => !IsStepped || CurrentStepIndex >= Schema.Steps.Count - 1;

    public IReadOnlyDictionary<string, object?> InternalState => _state;

    public IReadOnlyList<string> DirtyPaths
    {
        get
        {
            var ignored = Schema.IgnoreDirtyPaths ?? [];
            var allKeys = _state.Keys.Union(_initialSnapshot.Keys, StringComparer.Ordinal);
            var dirty = new List<string>();
            foreach (var key in allKeys)
            {
                if (ignored.Any(ig => key.Equals(ig, StringComparison.Ordinal) || key.StartsWith(ig + ".", StringComparison.Ordinal)))
                {
                    continue;
                }
                _state.TryGetValue(key, out var cur);
                _initialSnapshot.TryGetValue(key, out var init);
                if (!ValuesEqual(cur, init))
                {
                    dirty.Add(key);
                }
            }
            return dirty;
        }
    }

    public bool IsDirty => DirtyPaths.Count > 0;

    public IReadOnlyDictionary<string, string> Errors
    {
        get
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var (path, msg) in _validationErrors)
            {
                if (_touchedPaths.Contains(path))
                {
                    result[path] = msg;
                }
            }
            foreach (var (path, (msg, _)) in _customErrors)
            {
                result[path] = msg;
            }
            return result;
        }
    }

    public bool HasBlockingErrors =>
        _validationErrors.Count > 0 || _customErrors.Values.Any(e => e.Blocking);

    public IReadOnlyList<FormFieldSchema> AllFields
    {
        get
        {
            if (IsStepped) return Schema.Steps.SelectMany(s => FlattenFields(s.Fields)).ToList();
            if (Schema.Sections.Count > 0) return Schema.Sections.SelectMany(s => FlattenFields(s.Fields)).ToList();
            return FlattenFields(Schema.Fields).ToList();
        }
    }

    public IReadOnlyList<FormFieldSchema> CurrentVisibleTopFields
    {
        get
        {
            var source = IsStepped ? (CurrentStep?.Fields ?? [])
                : Schema.Sections.Count > 0 ? Schema.Sections.Where(IsSectionVisible).SelectMany(s => s.Fields).ToList()
                : Schema.Fields;
            return source.Where(ShouldRenderField).ToList();
        }
    }

    public object? GetValue(string path) =>
        _state.TryGetValue(path, out var val) ? val : null;

    public T? GetValue<T>(string path)
    {
        var raw = GetValue(path);
        if (raw is T typed) return typed;
        if (raw is null) return default;
        try
        {
            return (T)Convert.ChangeType(raw, typeof(T), CultureInfo.InvariantCulture);
        }
        catch
        {
            return default;
        }
    }

    public object? GetInitialValue(string path) =>
        _initialSnapshot.TryGetValue(path, out var val) ? val : null;

    public void SetValue(string path, object? value, bool markTouched = false)
    {
        _state[path] = value;
        _customErrors.Remove(path);

        var field = FindField(path);
        if (markTouched || field?.Trigger == FormValidationTrigger.Input)
        {
            _touchedPaths.Add(path);
        }

        if (_touchedPaths.Contains(path) && field is not null && field.Trigger != FormValidationTrigger.Submit)
        {
            ValidateField(field);
        }

        field?.Watch?.Invoke(value, this);
        Changed?.Invoke();
    }

    public void MarkTouched(string path)
    {
        _touchedPaths.Add(path);
        var field = FindField(path);
        if (field is not null && field.Trigger != FormValidationTrigger.Submit)
        {
            ValidateField(field);
        }
        Changed?.Invoke();
    }

    public void ResetField(string path)
    {
        _initialSnapshot.TryGetValue(path, out var init);
        _state[path] = CloneValue(init);
        _validationErrors.Remove(path);
        _customErrors.Remove(path);
        _touchedPaths.Remove(path);
        Changed?.Invoke();
    }

    public void ResetSection(string sectionKey)
    {
        var section = Schema.Sections.FirstOrDefault(s => s.Key == sectionKey);
        if (section is null) return;
        foreach (var field in FlattenFields(section.Fields))
        {
            ResetField(field.Key);
        }
        Changed?.Invoke();
    }

    public void SetError(string path, string message, bool blocking = false)
    {
        _customErrors[path] = (message, blocking);
        _touchedPaths.Add(path);
        Changed?.Invoke();
    }

    public void ClearError(string? path = null)
    {
        if (path is null)
        {
            _customErrors.Clear();
            _validationErrors.Clear();
        }
        else
        {
            _customErrors.Remove(path);
            _validationErrors.Remove(path);
        }
        Changed?.Invoke();
    }

    public string? GetError(string path) =>
        Errors.TryGetValue(path, out var err) ? err : null;

    public bool ShouldRenderField(FormFieldSchema field) =>
        !field.Ignore && (field.Condition is null || field.Condition(this));

    public bool IsSectionVisible(FormPageSectionSchema section) =>
        section.Condition is null || section.Condition(this);

    public bool IsFieldRequired(FormFieldSchema field) =>
        field.Required || (field.RequiredWhen is not null && field.RequiredWhen(this));

    public bool IsFieldDisabled(FormFieldSchema field) =>
        field.Disabled || (field.DisabledWhen is not null && field.DisabledWhen(this));

    public IReadOnlyList<FormOptionItem> GetFieldOptions(FormFieldSchema field)
    {
        var baseOpts = field.OptionsFactory is not null
            ? field.OptionsFactory(this)
            : (field.Options ?? []);
        if (_createdOptions.TryGetValue(field.Key, out var extra) && extra.Count > 0)
        {
            return baseOpts.Concat(extra).ToList();
        }
        return baseOpts;
    }

    public FormOptionItem? CreateFieldOption(FormFieldSchema field, string label)
    {
        if (string.IsNullOrWhiteSpace(label) || field.OnCreateOption is null) return null;
        var created = field.OnCreateOption(label.Trim());
        if (created is null) return null;
        if (!_createdOptions.TryGetValue(field.Key, out var list))
        {
            list = [];
            _createdOptions[field.Key] = list;
        }
        if (list.All(o => o.Value != created.Value))
        {
            list.Add(created);
        }
        if (field.Multiple)
        {
            var cur = GetValue<IReadOnlyList<string>>(field.Key)?.ToList() ?? [];
            if (!cur.Contains(created.Value, StringComparer.Ordinal))
            {
                cur.Add(created.Value);
                SetValue(field.Key, cur, markTouched: true);
            }
        }
        else
        {
            SetValue(field.Key, created.Value, markTouched: true);
        }
        return created;
    }

    // Array item management
    public IReadOnlyList<Dictionary<string, object?>> GetArrayItems(string arrayKey)
    {
        if (_state.TryGetValue(arrayKey, out var val) && val is List<Dictionary<string, object?>> list)
        {
            return list;
        }
        return [];
    }

    public void AddArrayItem(FormFieldSchema arrayField)
    {
        if (!_state.TryGetValue(arrayField.Key, out var val) || val is not List<Dictionary<string, object?>> list)
        {
            list = [];
            _state[arrayField.Key] = list;
        }
        var item = new Dictionary<string, object?>(StringComparer.Ordinal);
        if (arrayField.Fields is not null)
        {
            foreach (var child in arrayField.Fields)
            {
                item[child.Key] = child.DefaultFactory is not null ? child.DefaultFactory(this) : child.DefaultValue;
            }
        }
        list.Add(item);
        Changed?.Invoke();
    }

    public void RemoveArrayItem(string arrayKey, int index)
    {
        if (_state.TryGetValue(arrayKey, out var val) && val is List<Dictionary<string, object?>> list
            && index >= 0 && index < list.Count)
        {
            list.RemoveAt(index);
            Changed?.Invoke();
        }
    }

    public void SetArrayItemValue(string arrayKey, int index, string childKey, object? value)
    {
        if (_state.TryGetValue(arrayKey, out var val) && val is List<Dictionary<string, object?>> list
            && index >= 0 && index < list.Count)
        {
            list[index][childKey] = value;
            Changed?.Invoke();
        }
    }

    public bool Validate(bool currentStepOnly = false)
    {
        if (Schema.ValidateMode == FormValidationMode.None)
        {
            _validationErrors.Clear();
            Changed?.Invoke();
            return !_customErrors.Values.Any(e => e.Blocking);
        }

        var fieldsToValidate = currentStepOnly && CurrentStep is not null
            ? FlattenFields(CurrentStep.Fields).ToList()
            : AllFields;

        foreach (var field in fieldsToValidate)
        {
            _touchedPaths.Add(field.Key);
            ValidateField(field);
        }

        Changed?.Invoke();
        var hasStepErrors = fieldsToValidate.Any(f =>
            _validationErrors.ContainsKey(f.Key)
            || (_customErrors.TryGetValue(f.Key, out var ce) && ce.Blocking));
        return !hasStepErrors;
    }

    private void ValidateField(FormFieldSchema field)
    {
        _validationErrors.Remove(field.Key);
        if (!ShouldRenderField(field) || IsStateless(field.Kind)) return;
        if (Schema.ValidateMode == FormValidationMode.None) return;

        var val = GetValue(field.Key);
        var isEmpty = IsEmptyValue(val);

        if (Schema.ValidateMode is FormValidationMode.All or FormValidationMode.RequiredOnly)
        {
            if (IsFieldRequired(field) && isEmpty)
            {
                _validationErrors[field.Key] = field.RequiredMessage ?? "This field is required";
                return;
            }
        }

        if (Schema.ValidateMode == FormValidationMode.RequiredOnly) return;

        // Number bounds
        if (field.Kind == FormFieldKind.Number && !isEmpty && TryConvertDouble(val, out var num))
        {
            if (field.Min is { } min && num < min)
            {
                _validationErrors[field.Key] = $"Value must be at least {min.ToString(CultureInfo.InvariantCulture)}";
                return;
            }
            if (field.Max is { } max && num > max)
            {
                _validationErrors[field.Key] = $"Value must be at most {max.ToString(CultureInfo.InvariantCulture)}";
                return;
            }
        }

        // Password requirements
        if (field.Kind == FormFieldKind.Password && !isEmpty && field.PasswordRequirements is { Count: > 0 })
        {
            var str = val?.ToString() ?? "";
            foreach (var req in field.PasswordRequirements)
            {
                if (!req.Validate(str))
                {
                    _validationErrors[field.Key] = req.Label;
                    return;
                }
            }
        }

        // Custom validators
        if (field.Validators is { Count: > 0 })
        {
            foreach (var rule in field.Validators)
            {
                if (!rule.Validate(val, this))
                {
                    _validationErrors[field.Key] = rule.Message;
                    return;
                }
            }
        }
    }

    /// <summary>Builds the transformed output dictionary excluding hidden, ignored, stateless, and <see cref="FormFieldSchema.OmitFromOutput"/> fields.</summary>
    public IReadOnlyDictionary<string, object?> BuildOutput()
    {
        var output = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var field in AllFields)
        {
            if (!ShouldRenderField(field) || IsStateless(field.Kind) || field.OmitFromOutput)
            {
                continue;
            }
            var val = GetValue(field.Key);
            if (field.TransformOutput is not null)
            {
                val = field.TransformOutput(val);
            }
            output[field.Key] = CloneValue(val);
        }
        return output;
    }

    public async Task<bool> NextStepAsync()
    {
        if (!IsStepped || IsLastStep || IsSubmitting) return false;
        if (!Validate(currentStepOnly: true)) return false;

        if (Schema.OnBeforeNext is not null)
        {
            var ok = await Schema.OnBeforeNext(this, CurrentStepIndex);
            if (!ok) return false;
        }

        var nextIdx = CurrentStepIndex + 1;
        while (nextIdx < Schema.Steps.Count && Schema.Steps[nextIdx].SkipWhen?.Invoke(this) == true)
        {
            nextIdx++;
        }
        if (nextIdx >= Schema.Steps.Count) return false;

        CurrentStepIndex = nextIdx;
        _validationErrors.Clear();
        _touchedPaths.Clear();
        Changed?.Invoke();
        return true;
    }

    public async Task<bool> PreviousStepAsync()
    {
        if (!IsStepped || IsFirstStep || IsSubmitting) return false;
        if (Schema.OnBeforePrevious is not null)
        {
            var ok = await Schema.OnBeforePrevious(this, CurrentStepIndex);
            if (!ok) return false;
        }

        var prevIdx = CurrentStepIndex - 1;
        while (prevIdx >= 0 && Schema.Steps[prevIdx].SkipWhen?.Invoke(this) == true)
        {
            prevIdx--;
        }
        if (prevIdx < 0) return false;

        CurrentStepIndex = prevIdx;
        _validationErrors.Clear();
        _touchedPaths.Clear();
        Changed?.Invoke();
        return true;
    }

    public async Task<bool> SubmitAsync()
    {
        if (IsSubmitting) return false;
        if (!Validate(currentStepOnly: false)) return false;

        IsSubmitting = true;
        Changed?.Invoke();
        try
        {
            if (Schema.OnBeforeSubmit is not null)
            {
                var allowed = await Schema.OnBeforeSubmit(this);
                if (!allowed) return false;
            }

            if (Schema.OnSubmit is not null)
            {
                await Schema.OnSubmit(this);
            }

            // Rebaseline dirty snapshot on success
            _initialSnapshot.Clear();
            foreach (var (k, v) in _state)
            {
                _initialSnapshot[k] = CloneValue(v);
            }
            return true;
        }
        finally
        {
            IsSubmitting = false;
            Changed?.Invoke();
        }
    }

    public void Reset(IReadOnlyDictionary<string, object?>? newInput = null)
    {
        _validationErrors.Clear();
        _customErrors.Clear();
        _touchedPaths.Clear();
        CurrentStepIndex = 0;
        InitializeState(newInput);
        Changed?.Invoke();
    }

    public IReadOnlyList<FormPageSectionState> GetSectionStates()
    {
        var result = new List<FormPageSectionState>();
        var dirtySet = DirtyPaths.ToHashSet(StringComparer.Ordinal);
        foreach (var section in Schema.Sections.Where(IsSectionVisible))
        {
            var leaves = FlattenFields(section.Fields).Where(f => ShouldRenderField(f) && !IsStateless(f.Kind)).ToList();
            var reqCount = 0;
            var missingCount = 0;
            var hasError = false;
            var isDirty = false;
            var hasProvided = false;

            foreach (var leaf in leaves)
            {
                var val = GetValue(leaf.Key);
                var empty = IsEmptyValue(val);
                if (!empty) hasProvided = true;
                if (IsFieldRequired(leaf))
                {
                    reqCount++;
                    if (empty) missingCount++;
                }
                if (_validationErrors.ContainsKey(leaf.Key) || _customErrors.ContainsKey(leaf.Key))
                {
                    hasError = true;
                }
                if (dirtySet.Contains(leaf.Key))
                {
                    isDirty = true;
                }
            }

            var status = hasError ? FormPageSectionStatus.Invalid
                : missingCount > 0 ? FormPageSectionStatus.Pending
                : (section.Optional && !hasProvided) ? FormPageSectionStatus.Optional
                : FormPageSectionStatus.Complete;

            result.Add(new FormPageSectionState(
                section.Key,
                section.Label,
                section.Description,
                section.Optional,
                reqCount,
                missingCount,
                isDirty,
                status));
        }
        return result;
    }

    private void InitializeState(IReadOnlyDictionary<string, object?>? input)
    {
        _state.Clear();
        _initialSnapshot.Clear();

        if (input is not null)
        {
            foreach (var (k, v) in input)
            {
                _state[k] = CloneValue(v);
            }
        }

        foreach (var field in AllFields)
        {
            if (field.Ignore || IsStateless(field.Kind)) continue;
            if (_state.TryGetValue(field.Key, out var existing) && existing is not null)
            {
                if (field.TransformInput is not null)
                {
                    _state[field.Key] = field.TransformInput(existing);
                }
            }
            else
            {
                var def = field.DefaultFactory is not null ? field.DefaultFactory(this) : field.DefaultValue;
                if (def is null && field.Kind is FormFieldKind.ArrayList or FormFieldKind.ArrayTable)
                {
                    def = new List<Dictionary<string, object?>>();
                }
                _state[field.Key] = CloneValue(def);
            }
        }

        foreach (var (k, v) in _state)
        {
            _initialSnapshot[k] = CloneValue(v);
        }
    }

    private FormFieldSchema? FindField(string key) =>
        AllFields.FirstOrDefault(f => f.Key == key);

    private IEnumerable<FormFieldSchema> FlattenFields(IEnumerable<FormFieldSchema> fields)
    {
        foreach (var field in fields)
        {
            if (field.Ignore) continue;
            if (field.Kind is FormFieldKind.Group or FormFieldKind.Card or FormFieldKind.Object && field.Fields is not null)
            {
                foreach (var child in FlattenFields(field.Fields))
                {
                    yield return child;
                }
            }
            else if (field.Kind == FormFieldKind.Tabs && field.Tabs is not null)
            {
                foreach (var tab in field.Tabs)
                {
                    foreach (var child in FlattenFields(tab.Fields))
                    {
                        yield return child;
                    }
                }
            }
            else
            {
                yield return field;
            }
        }
    }

    public static bool IsStateless(FormFieldKind kind) =>
        kind is FormFieldKind.Info or FormFieldKind.Divider or FormFieldKind.Section or FormFieldKind.Button;

    public static bool IsEmptyValue(object? value) => value switch
    {
        null => true,
        string s => string.IsNullOrWhiteSpace(s),
        System.Collections.ICollection c => c.Count == 0,
        IEnumerable<object?> e => !e.Any(),
        _ => false,
    };

    private static bool TryConvertDouble(object? val, out double d)
    {
        switch (val)
        {
            case double dbl:
                d = dbl;
                return true;
            case int i:
                d = i;
                return true;
            case long l:
                d = l;
                return true;
            case decimal m:
                d = (double)m;
                return true;
            default:
                return double.TryParse(val?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out d);
        }
    }

    private static object? CloneValue(object? value)
    {
        if (value is List<Dictionary<string, object?>> dictList)
        {
            return dictList.Select(d => new Dictionary<string, object?>(d, StringComparer.Ordinal)).ToList();
        }
        if (value is IReadOnlyList<string> strList)
        {
            return strList.ToList();
        }
        return value;
    }

    private static bool ValuesEqual(object? a, object? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return false;
        if (a is IReadOnlyList<string> la && b is IReadOnlyList<string> lb)
        {
            return la.SequenceEqual(lb, StringComparer.Ordinal);
        }
        if (a is List<Dictionary<string, object?>> da && b is List<Dictionary<string, object?>> db)
        {
            if (da.Count != db.Count) return false;
            for (var i = 0; i < da.Count; i++)
            {
                if (da[i].Count != db[i].Count) return false;
                foreach (var (k, va) in da[i])
                {
                    if (!db[i].TryGetValue(k, out var vb) || !Equals(va, vb)) return false;
                }
            }
            return true;
        }
        return Equals(a, b);
    }
}

/// <summary>Scoped service managing programmatic overlay forms opened in <see cref="BxFormProvider"/>.</summary>
public sealed class FormService
{
    public event Action? Changed;

    public FormController? ActiveForm { get; private set; }
    public FormShellMode ActiveMode { get; private set; } = FormShellMode.Modal;
    private TaskCompletionSource<FormSubmitOutcome>? _tcs;

    public bool IsOpen => ActiveForm is not null;

    /// <summary>Opens a <see cref="FormSchema"/> inside the mounted <see cref="BxFormProvider"/> and awaits completion or cancellation.</summary>
    public Task<FormSubmitOutcome> CreateFormAsync(
        FormSchema schema,
        IReadOnlyDictionary<string, object?>? input = null,
        FormShellMode mode = FormShellMode.Modal)
    {
        ActiveForm = new FormController(schema, input);
        ActiveMode = mode;
        _tcs = new TaskCompletionSource<FormSubmitOutcome>(TaskCreationOptions.RunContinuationsAsynchronously);
        Changed?.Invoke();
        return _tcs.Task;
    }

    public async Task<bool> SubmitActiveFormAsync()
    {
        if (ActiveForm is null) return false;
        var ok = await ActiveForm.SubmitAsync();
        if (ok)
        {
            var data = ActiveForm.BuildOutput();
            var tcs = _tcs;
            ActiveForm = null;
            _tcs = null;
            Changed?.Invoke();
            tcs?.TrySetResult(new FormSubmitOutcome(true, data));
        }
        return ok;
    }

    public void CloseForm()
    {
        if (ActiveForm is null) return;
        var data = ActiveForm.BuildOutput();
        var tcs = _tcs;
        ActiveForm = null;
        _tcs = null;
        Changed?.Invoke();
        tcs?.TrySetResult(new FormSubmitOutcome(false, data));
    }
}
