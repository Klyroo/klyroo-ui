using Blux.UI.Tools;

namespace Blux.Playground.Client.Docs;

public static partial class LuxLibraryDocs
{
    private static LibraryDocTree BuildForm() => new(
        "form", "Lux.Form", "#f59e0b",
        [
            new("", "Introduction", "Getting Started",
                "Declarative, NativeAOT schema-driven form engine with 31 field kinds, reactive visibility/disabled/required rules, dirty path diffing, input/output transforms, repeatable arrays, multi-step wizards with SkipWhen, sectioned FormPage, and programmatic FormService overlays.",
                [
                    new("overview", "What is Lux.Form?",
                        Paragraphs:
                        [
                            "Lux.Form replaces hand-coded form markup and fragile validation state with a strongly structured FormSchema and headless FormController. All business rules — conditional visibility, dynamic required/disabled predicates, watch callbacks, password checklists, array row mutations, dirty tracking, and wizard step skipping — execute in 100% NativeAOT C# with zero UI dependencies.",
                            "Render any FormController declaratively with BxFormEngine, organize large settings screens with BxFormPage, or launch modal/drawer/fullscreen forms imperatively with FormService.CreateFormAsync()."
                        ],
                        LiveDemoKey: "form-reactive",
                        LiveDemoTitle: "Lux.Form · Live Reactive Condition, Password Rules & Dirty Tracking"),
                    new("layers", "Two-Layer Architecture",
                        TableHeaders: ["Package", "Primary Exports", "Compatibility"],
                        TableRows:
                        [
                            new("Lux.Form.Core", "FormSchema, FormFieldSchema, FormStepSchema, FormPageSectionSchema, FormController, FormService", "Headless C# · IsAotCompatible=true"),
                            new("Lux.Form", "BxFormEngine, BxFormPage, BxFormProvider, AddLuxForm()", "browser, ios, android, maccatalyst, windows")
                        ])
                ]),
            new("installation", "Installation", "Getting Started",
                "Install Lux.Form, register FormService, and mount BxFormProvider once for programmatic modal/drawer overlays.",
                [
                    new("nuget", "1. Install Packages",
                        CodeSamples:
                        [
                            new("Terminal", """
                                dotnet add package Lux.Form.Core   # Headless NativeAOT schema & controller
                                dotnet add package Lux.Form        # Blazor Web & .NET MAUI form components
                                """)
                        ]),
                    new("di", "2. Register Services & Mount Overlay Provider",
                        Paragraphs: ["AddLuxForm() registers FormService as a scoped service. Place <BxFormProvider /> once in your root layout so FormService.CreateFormAsync() can open Modal, Drawer, or Fullscreen forms from anywhere."],
                        CodeSamples:
                        [
                            new("Program.cs / MauiProgram.cs", "builder.Services.AddLuxForm();"),
                            new("MainLayout.razor", "<BxFormProvider />")
                        ])
                ]),
            new("field-kinds", "FormSchema & 31 Field Kinds", "Schema & State",
                "Complete catalog of all 31 declarative FormFieldKind values and every FormFieldSchema configuration property.",
                [
                    new("all-kinds", "All 31 FormFieldKind Values",
                        TableHeaders: ["Category", "Exact FormFieldKind Values", "Capabilities"],
                        TableRows:
                        [
                            new("Text & Auth (5)", "Text, Password, Textarea, PhoneNumber, OneTimeCode", "MaxLength, Placeholder, live PasswordRequirements checklist, OTP digits"),
                            new("Numeric & Visual (4)", "Number, Slider, Rating, ColorPicker", "Min, Max, Step precision, visual slider/star/swatch pickers"),
                            new("Boolean & Choice (6)", "Checkbox, Switch, Radio, CheckboxGroup, Select, AutoComplete", "Options, dynamic OptionsFactory, Multiple, Clearable, OnCreateOption"),
                            new("Temporal & Tags (5)", "Date, DateTime, DateRange, Time, Tag", "Calendar/time inputs, ranges, and freeform tag chip editors"),
                            new("Stateless Layout (6)", "Hidden, Info, Divider, Section, Button, Group", "DisplayText/DisplayTone callouts, Collapsible/DefaultCollapsed sections (excluded from BuildOutput)"),
                            new("Composite & Nested (5)", "Card, Tabs, Object, ArrayList, ArrayTable", "FormTabItemSchema tabs, nested child Fields, repeatable AddItemLabel rows")
                        ]),
                    new("field-props", "Key FormFieldSchema Properties",
                        TableHeaders: ["Property", "Type", "Purpose"],
                        TableRows:
                        [
                            new("DefaultValue / DefaultFactory", "object? / Func<FormController, object?>?", "Static initial value or computed default from controller state"),
                            new("Options / OptionsFactory / OnCreateOption", "IReadOnlyList<FormOptionItem>?", "Static choices, reactive dependent choices, and inline option creation"),
                            new("Span / Collapsible / DefaultCollapsed", "int / bool / bool", "Grid column span inside FormSchema.Columns and collapsible section behavior"),
                            new("Ignore / OmitFromOutput", "bool / bool", "Ignore skips field completely; OmitFromOutput renders/validates but strips from BuildOutput()"),
                            new("Watch", "Action<object?, FormController>?", "Side-effect hook invoked whenever this field's value changes")
                        ]),
                    new("arrays", "Repeatable Arrays (ArrayList & ArrayTable)",
                        Paragraphs: ["Use FormFieldKind.ArrayList or ArrayTable with child Fields to let users add, edit, and remove structured rows with full controller API support."],
                        CodeSamples:
                        [
                            new("ArrayFields.cs", """
                                var lineItemsField = new FormFieldSchema
                                {
                                    Key = "items",
                                    Label = "Invoice Line Items",
                                    Kind = FormFieldKind.ArrayTable,
                                    AddItemLabel = "Add line item",
                                    Fields =
                                    [
                                        new FormFieldSchema { Key = "sku", Label = "SKU", Required = true },
                                        new FormFieldSchema { Key = "qty", Label = "Qty", Kind = FormFieldKind.Number, Min = 1, DefaultValue = 1.0 }
                                    ]
                                };

                                // Programmatic array mutations on FormController:
                                controller.AddArrayItem(lineItemsField);
                                controller.SetArrayItemValue("items", index: 0, childKey: "sku", value: "LUX-PRO");
                                var rows = controller.GetArrayItems("items");
                                controller.RemoveArrayItem("items", index: 0);
                                """)
                        ])
                ]),
            new("conditions", "Reactive Conditions & Transforms", "Schema & State",
                "Control visibility, required state, disabled state, dependent options, and input/output normalization dynamically.",
                [
                    new("reactive", "Condition, RequiredWhen, DisabledWhen & Transforms",
                        Paragraphs:
                        [
                            "FormController evaluates Condition, RequiredWhen, DisabledWhen, and OptionsFactory live against current state. When Condition returns false, the field is automatically hidden, bypassed during Validate(), and excluded from BuildOutput().",
                            "Use TransformInput to normalize incoming external records (e.g., converting ISO strings) and TransformOutput to sanitize final payloads before persistence (e.g., trimming whitespace)."
                        ],
                        LiveDemoKey: "form-reactive",
                        LiveDemoTitle: "Set Age below 21 to watch Company hide and drop from validation",
                        CodeSamples:
                        [
                            new("ReactiveSchema.cs", """
                                var schema = new FormSchema
                                {
                                    FormKey = "profile",
                                    Title = "User Profile",
                                    Fields =
                                    [
                                        new FormFieldSchema
                                        {
                                            Key = "name",
                                            Label = "Full Name",
                                            Required = true,
                                            TransformInput = v => v?.ToString(),
                                            TransformOutput = v => v?.ToString()?.Trim(),
                                            Watch = (val, form) => form.SetValue("slug", val?.ToString()?.Trim().ToLowerInvariant().Replace(' ', '-'))
                                        },
                                        new FormFieldSchema { Key = "age", Label = "Age", Kind = FormFieldKind.Number, Min = 18, Max = 120, DefaultValue = 25.0 },
                                        new FormFieldSchema
                                        {
                                            Key = "company",
                                            Label = "Company",
                                            RequiredWhen = f => f.GetValue<double>("age") >= 21,
                                            Condition = f => f.GetValue<double>("age") >= 21,
                                            DisabledWhen = f => f.GetValue<bool>("locked"),
                                            DisabledReason = "Locked by workspace administrator"
                                        }
                                    ]
                                };
                                """)
                        ])
                ]),
            new("validation", "Validation & Password Rules", "Schema & State",
                "Configure validation modes, triggers, custom validators, server-side blocking errors, and password strength checklists.",
                [
                    new("enums", "FormValidationMode & FormValidationTrigger",
                        TableHeaders: ["Enum", "Exact Values", "Behavior"],
                        TableRows:
                        [
                            new("FormValidationMode", "All, RequiredOnly, ValidatorsOnly, None", "Controls which validation passes run on FormSchema.ValidateMode"),
                            new("FormValidationTrigger", "Blur, Input, Submit", "Per-field trigger deciding when validation feedback surfaces")
                        ]),
                    new("custom", "Custom Validators, PasswordRequirements & Server Errors",
                        Paragraphs: ["Call controller.SetError(path, message, blocking: true) to attach server-side errors returned by an API; HasBlockingErrors blocks submission until the field changes or ClearError() is called."],
                        CodeSamples:
                        [
                            new("ValidationRules.cs", """
                                new FormFieldSchema
                                {
                                    Key = "password",
                                    Label = "Password",
                                    Kind = FormFieldKind.Password,
                                    Trigger = FormValidationTrigger.Input,
                                    PasswordRequirements =
                                    [
                                        new FormPasswordRequirement("len", "At least 8 characters", s => s.Length >= 8),
                                        new FormPasswordRequirement("upper", "Contains an uppercase letter", s => s.Any(char.IsUpper)),
                                        new FormPasswordRequirement("digit", "Contains a number", s => s.Any(char.IsDigit))
                                    ]
                                }
                                """)
                        ])
                ]),
            new("dirty-tracking", "Dirty Tracking & Diffing", "Schema & State",
                "Track exact changed field paths against the baseline snapshot, ignore transient paths, and reset fields or sections individually.",
                [
                    new("dirty", "IsDirty, DirtyPaths, ResetField & ResetSection",
                        Paragraphs:
                        [
                            "FormController captures a baseline snapshot when initialized or reset. Any modification updates IsDirty and lists changed keys in DirtyPaths (excluding keys listed in FormSchema.IgnoreDirtyPaths).",
                            "After SubmitAsync() succeeds, the baseline is re-snapshotted automatically so IsDirty resets to false."
                        ],
                        CodeSamples:
                        [
                            new("DirtyApi.cs", """
                                var controller = new FormController(schema, new Dictionary<string, object?> { ["name"] = "Alice" });
                                Assert.False(controller.IsDirty);

                                controller.SetValue("age", 19.0, markTouched: true);
                                Assert.True(controller.IsDirty);
                                Assert.Contains("age", controller.DirtyPaths);

                                // Revert just one field or one page section:
                                controller.ResetField("age");
                                controller.ResetSection("billing");

                                // Full submit re-baselines IsDirty to false:
                                Assert.True(await controller.SubmitAsync());
                                Assert.False(controller.IsDirty);
                                """)
                        ])
                ]),
            new("wizards", "Multi-Step Wizards (SkipWhen)", "Schema & State",
                "Build stepped wizards with conditional step skipping, per-step validation, and async before-step hooks.",
                [
                    new("steps", "FormStepSchema & Async Navigation Hooks",
                        Paragraphs:
                        [
                            "Populate Steps on FormSchema to activate wizard mode (controller.IsStepped == true). NextStepAsync() validates only the current step's visible fields, invokes FormSchema.OnBeforeNext if provided, and skips over any subsequent step whose SkipWhen predicate evaluates to true.",
                            "PreviousStepAsync() similarly respects SkipWhen and FormSchema.OnBeforePrevious."
                        ],
                        CodeSamples:
                        [
                            new("OnboardingWizard.cs", """
                                var wizardSchema = new FormSchema
                                {
                                    FormKey = "onboarding",
                                    Title = "Workspace Onboarding",
                                    ShowStepper = true,
                                    OnBeforeNext = async (form, stepIdx) => await CheckDomainAvailableAsync(form),
                                    Steps =
                                    [
                                        new FormStepSchema
                                        {
                                            Key = "account",
                                            Title = "Account",
                                            Fields = [new FormFieldSchema { Key = "plan", Label = "Plan", Required = true, DefaultValue = "free" }]
                                        },
                                        new FormStepSchema
                                        {
                                            Key = "billing",
                                            Title = "Billing",
                                            SkipWhen = f => f.GetValue<string>("plan") == "free",
                                            Fields = [new FormFieldSchema { Key = "vat", Label = "VAT Number", Required = true }]
                                        },
                                        new FormStepSchema
                                        {
                                            Key = "confirm",
                                            Title = "Review & Launch",
                                            Fields = [new FormFieldSchema { Key = "agree", Label = "Accept terms", Kind = FormFieldKind.Checkbox, Required = true }]
                                        }
                                    ]
                                };
                                """)
                        ])
                ]),
            new("form-engine", "BxFormEngine", "UI Components",
                "Render single-page and multi-step forms automatically in Inline, Modal, Drawer, or Fullscreen shells.",
                [
                    new("engine", "Parameters & Live Demo",
                        LiveDemoKey: "form-reactive",
                        LiveDemoTitle: "BxFormEngine Live Demo",
                        CodeSamples:
                        [
                            new("ProfilePage.razor", """
                                <BxFormEngine Form="@_controller"
                                              Shell="FormShellMode.Inline"
                                              SubmitLabel="Save profile"
                                              ShowReset="true"
                                              OnSubmit="@HandleSubmit"
                                              OnCancel="@HandleCancel" />
                                """)
                        ])
                ]),
            new("form-page-and-service", "BxFormPage & FormService Overlays", "UI Components",
                "Structure long settings pages with section completion statuses or launch programmatic form overlays via FormService.",
                [
                    new("form-page", "Sectioned Pages with FormPageSectionSchema",
                        Paragraphs: ["Define Sections on FormSchema and render <BxFormPage Form=\"@_controller\" />. controller.GetSectionStates() computes FormPageSectionStatus (Complete, Pending, Invalid, Optional) in real time for every section."],
                        CodeSamples:
                        [
                            new("FormPage.razor", """
                                <BxFormPage Form="@_settingsForm" />
                                """)
                        ]),
                    new("service", "Programmatic Overlays with FormService.CreateFormAsync",
                        Paragraphs: ["Inject FormService to open any FormSchema inside a Modal, Drawer, or Fullscreen overlay hosted by <BxFormProvider /> and await the FormSubmitOutcome."],
                        CodeSamples:
                        [
                            new("OverlayCall.cs", """
                                // FormShellMode: Inline, Modal, Drawer, Fullscreen
                                FormSubmitOutcome outcome = await formService.CreateFormAsync(
                                    inviteSchema,
                                    input: new Dictionary<string, object?> { ["role"] = "Editor" },
                                    mode: FormShellMode.Drawer);

                                if (outcome.Submitted)
                                {
                                    await api.InviteAsync(outcome.Data);
                                }
                                """)
                        ])
                ]),
            MauiAndAotPage("Lux.Form", "Lux.Form.Core", "Lux.Form", "builder.Services.AddLuxForm();",
                "var form = new FormController(schema); if (await form.SubmitAsync()) Save(form.BuildOutput());"),
            new("api", "API Reference", "Reference",
                "Complete public API reference for Lux.Form.Core and Lux.Form.",
                [],
                ApiTypes: [typeof(BxFormEngine), typeof(BxFormPage), typeof(BxFormProvider), typeof(FormController), typeof(FormSchema), typeof(FormFieldSchema), typeof(FormStepSchema), typeof(FormService)])
        ]);
}
