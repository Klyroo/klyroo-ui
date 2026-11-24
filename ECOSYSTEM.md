# LuxStack — L'Écosystème Headless & UI pour .NET, Blazor, MAUI & Agents IA

L'écosystème **LuxStack** (inspiré de **TanStack** et **`ui-sdk.dev`**) est structuré autour d'un repository central **[lux.stack](file:///home/gpoumarat/lux.stack/README.md)** et de **8 repositories spécialisés** (`/home/gpoumarat/lux-*`), chacun séparant :
- **`src/Lux.<Produit>.Core`** : Moteur **100 % Headless C# / NativeAOT** (zéro dépendance UI, utilisable côté serveur, Minimal API, CLI ou ViewModels .NET MAUI Natif).
- **`src/Lux.<Produit>`** : Composants **Blazor Web & .NET MAUI Blazor Hybrid (`Bx*`)** (`SupportedPlatform`: `browser`, `ios`, `android`, `maccatalyst`, `windows`).

---

## Cartographie de LuxStack

| Repository | Chemin local | Solution | Packages NuGet / NPM | Template `dotnet new` |
|---|---|---|---|---|
| **Hub Central `lux.stack`** | `/home/gpoumarat/lux.stack` | [Lux.Stack.slnx](file:///home/gpoumarat/lux.stack/Lux.Stack.slnx) | `Lux.Stack.Core`, `Lux.Stack` (`AddLuxStack()`) | — |
| **1. `lux-ui`** | `/home/gpoumarat/lux-ui` (`/home/gpoumarat/blux-ui`) | [Blux.UI.slnx](file:///home/gpoumarat/blux-ui/Blux.UI.slnx) | `Blux.UI`, `Blux.UI.Server`, `Blux.UI.Images`, `Blux.UI.Images.Local`, `Blux.UI.Build.Tasks` | `starter`, `landing`, `docs`, `portfolio`, `changelog`, `editor`, `chat` |
| **2. `lux-query`** | `/home/gpoumarat/lux-query` | [Lux.Query.slnx](file:///home/gpoumarat/lux-query/Lux.Query.slnx) | `Lux.Query.Core`, `Lux.Query` (`AddLuxQuery()`), `Lux.Query.EntityFrameworkCore` | `lux-query` |
| **3. `lux-table`** | `/home/gpoumarat/lux-table` | [Lux.Table.slnx](file:///home/gpoumarat/lux-table/Lux.Table.slnx) | `Lux.Table.Core`, `Lux.Table` (`AddLuxTable()`) | `lux-table-admin` |
| **4. `lux-form`** | `/home/gpoumarat/lux-form` | [Lux.Form.slnx](file:///home/gpoumarat/lux-form/Lux.Form.slnx) | `Lux.Form.Core`, `Lux.Form` (`AddLuxForm()`) | `lux-form-wizard` |
| **5. `lux-dashboard`** | `/home/gpoumarat/lux-dashboard` | [Lux.Dashboard.slnx](file:///home/gpoumarat/lux-dashboard/Lux.Dashboard.slnx) | `Lux.Dashboard.Core`, `Lux.Dashboard` (`AddLuxDashboard()`) | `lux-analytics` |
| **6. `lux-display`** | `/home/gpoumarat/lux-display` | [Lux.Display.slnx](file:///home/gpoumarat/lux-display/Lux.Display.slnx) | `Lux.Display.Core`, `Lux.Display` (`AddLuxDisplay()`) | `lux-display-portal` |
| **7. `lux-spreadsheet`** | `/home/gpoumarat/lux-spreadsheet` | [Lux.Spreadsheet.slnx](file:///home/gpoumarat/lux-spreadsheet/Lux.Spreadsheet.slnx) | `Lux.Spreadsheet.Core`, `Lux.Spreadsheet` (`AddLuxSpreadsheet()`) | `lux-spreadsheet-app` |
| **8. `lux-agent`** | `/home/gpoumarat/lux-agent` | [Lux.Agent.slnx](file:///home/gpoumarat/lux-agent/Lux.Agent.slnx) | `Lux.Agent.Core`, `Lux.Agent` (`AddLuxAgent()`), `@lux/behaviors`, `@lux/agent` | `lux-agent-copilot` |
