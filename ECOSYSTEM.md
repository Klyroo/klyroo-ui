# LuxStack — Headless & UI Ecosystem for .NET 10, Blazor, MAUI & AI Agents

**LuxStack** is structured around the central hub **[lux.stack](file:///home/gpoumarat/lux.stack/README.md)** (which also hosts the unified documentation & live playground site `site/Lux.Stack.Site`) and **8 specialized repositories** (`/home/gpoumarat/lux-*`), each cleanly separating:
- **`src/Lux.<Product>.Core`**: **100% Headless C# / NativeAOT** engine (zero UI dependency; runs in Minimal APIs, workers, CLIs, and native .NET MAUI ViewModels).
- **`src/Lux.<Product>`**: **Blazor Web & .NET MAUI Blazor Hybrid (`Lx*`)** components (`SupportedPlatform`: `browser`, `ios`, `android`, `maccatalyst`, `windows`).

---

## LuxStack Repository Map

| Repository | Local Path | Solution | Packages |
|---|---|---|---|
| **Hub `lux.stack`** | `/home/gpoumarat/lux.stack` | [Lux.Stack.slnx](file:///home/gpoumarat/lux.stack/Lux.Stack.slnx) | `Lux.Stack.Core`, `Lux.Stack`, `Lux.Stack.Site`, `Lux.Stack.Mcp` |
| **1. `lux-ui`** | `/home/gpoumarat/lux-ui` | [Lux.UI.slnx](file:///home/gpoumarat/lux-ui/Lux.UI.slnx) | `Lux.UI`, `Lux.UI.Server`, `Lux.UI.Images`, `Lux.UI.Images.Local`, `Lux.UI.Build.Tasks` |
| **2. `lux-query`** | `/home/gpoumarat/lux-query` | [Lux.Query.slnx](file:///home/gpoumarat/lux-query/Lux.Query.slnx) | `Lux.Query.Core`, `Lux.Query`, `Lux.Query.EntityFrameworkCore` |
| **3. `lux-table`** | `/home/gpoumarat/lux-table` | [Lux.Table.slnx](file:///home/gpoumarat/lux-table/Lux.Table.slnx) | `Lux.Table.Core`, `Lux.Table` |
| **4. `lux-form`** | `/home/gpoumarat/lux-form` | [Lux.Form.slnx](file:///home/gpoumarat/lux-form/Lux.Form.slnx) | `Lux.Form.Core`, `Lux.Form` |
| **5. `lux-dashboard`** | `/home/gpoumarat/lux-dashboard` | [Lux.Dashboard.slnx](file:///home/gpoumarat/lux-dashboard/Lux.Dashboard.slnx) | `Lux.Dashboard.Core`, `Lux.Dashboard` |
| **6. `lux-display`** | `/home/gpoumarat/lux-display` | [Lux.Display.slnx](file:///home/gpoumarat/lux-display/Lux.Display.slnx) | `Lux.Display.Core`, `Lux.Display` |
| **7. `lux-spreadsheet`** | `/home/gpoumarat/lux-spreadsheet` | [Lux.Spreadsheet.slnx](file:///home/gpoumarat/lux-spreadsheet/Lux.Spreadsheet.slnx) | `Lux.Spreadsheet.Core`, `Lux.Spreadsheet` |
| **8. `lux-agent`** | `/home/gpoumarat/lux-agent` | [Lux.Agent.slnx](file:///home/gpoumarat/lux-agent/Lux.Agent.slnx) | `Lux.Agent.Core`, `Lux.Agent` |
