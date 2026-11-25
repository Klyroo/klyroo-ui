# Klyroo — Headless & UI Ecosystem for .NET 10, Blazor, MAUI & AI Agents

**Klyroo** is structured around the central hub **[klyroo-stack](https://github.com/klyroo/klyroo-stack/blob/main/README.md)** (which also hosts the unified documentation & live playground site `site/Klyroo.Stack.Site`) and **8 specialized repositories** (`klyroo-*`), each cleanly separating:
- **`src/Klyroo.<Product>.Core`**: **100% Headless C# / NativeAOT** engine (zero UI dependency; runs in Minimal APIs, workers, CLIs, and native .NET MAUI ViewModels).
- **`src/Klyroo.<Product>`**: **Blazor Web & .NET MAUI Blazor Hybrid (`Kly*`)** components (`SupportedPlatform`: `browser`, `ios`, `android`, `maccatalyst`, `windows`).

---

## Klyroo Repository Map

| Repository | Local Path | Solution | Packages |
|---|---|---|---|
| **Hub `klyroo-stack`** | `klyroo-stack` | [Klyroo.Stack.slnx](https://github.com/klyroo/klyroo-stack/blob/main/Klyroo.Stack.slnx) | `Klyroo.Stack.Core`, `Klyroo.Stack`, `Klyroo.Stack.Site`, `Klyroo.Stack.Mcp` |
| **1. `klyroo-ui`** | `klyroo-ui` | [Klyroo.UI.slnx](https://github.com/klyroo/klyroo-ui/blob/main/Klyroo.UI.slnx) | `Klyroo.UI`, `Klyroo.UI.Server`, `Klyroo.UI.Images`, `Klyroo.UI.Images.Local`, `Klyroo.UI.Build.Tasks` |
| **2. `klyroo-query`** | `klyroo-query` | [Klyroo.Query.slnx](https://github.com/klyroo/klyroo-query/blob/main/Klyroo.Query.slnx) | `Klyroo.Query.Core`, `Klyroo.Query`, `Klyroo.Query.EntityFrameworkCore` |
| **3. `klyroo-table`** | `klyroo-table` | [Klyroo.Table.slnx](https://github.com/klyroo/klyroo-table/blob/main/Klyroo.Table.slnx) | `Klyroo.Table.Core`, `Klyroo.Table` |
| **4. `klyroo-form`** | `klyroo-form` | [Klyroo.Form.slnx](https://github.com/klyroo/klyroo-form/blob/main/Klyroo.Form.slnx) | `Klyroo.Form.Core`, `Klyroo.Form` |
| **5. `klyroo-dashboard`** | `klyroo-dashboard` | [Klyroo.Dashboard.slnx](https://github.com/klyroo/klyroo-dashboard/blob/main/Klyroo.Dashboard.slnx) | `Klyroo.Dashboard.Core`, `Klyroo.Dashboard` |
| **6. `klyroo-display`** | `klyroo-display` | [Klyroo.Display.slnx](https://github.com/klyroo/klyroo-display/blob/main/Klyroo.Display.slnx) | `Klyroo.Display.Core`, `Klyroo.Display` |
| **7. `klyroo-spreadsheet`** | `klyroo-spreadsheet` | [Klyroo.Spreadsheet.slnx](https://github.com/klyroo/klyroo-spreadsheet/blob/main/Klyroo.Spreadsheet.slnx) | `Klyroo.Spreadsheet.Core`, `Klyroo.Spreadsheet` |
| **8. `klyroo-agent`** | `klyroo-agent` | [Klyroo.Agent.slnx](https://github.com/klyroo/klyroo-agent/blob/main/Klyroo.Agent.slnx) | `Klyroo.Agent.Core`, `Klyroo.Agent` |
