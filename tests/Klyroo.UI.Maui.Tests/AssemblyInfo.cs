// Several tests swap process-wide state (Application.Current, UserAppTheme, the dispatcher provider, the in-memory preference store):
// one collection for the whole assembly, so the test classes run one after the other.
[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly)]
