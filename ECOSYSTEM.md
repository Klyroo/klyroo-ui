# L'Écosystème Blux (Architecture multi-repositories sur le modèle TanStack)

L'écosystème **Blux** est découpé en **8 repositories spécialisés** situés dans des dossiers frères (`/home/gpoumarat/blux-*`), chacun suivant l'architecture **TanStack** :
- **`src/Blux.<Produit>.Core`** : Moteur **100 % Headless C# / NativeAOT** (zéro dépendance UI, testable et utilisable côté serveur, API, CLI ou MAUI).
- **`src/Blux.<Produit>`** : Composants **Blazor (`Bx*`)** branchés sur le design system `Blux.UI`.
- **`tests/`** : Suites de tests unitaires (`.Core`) et composants (`bUnit`).
- **`examples/`** : Applications d'exemples autonomes par cas d'usage.
- **`templates/`** : Templates `dotnet new` prêts au déploiement.
- **`skills/`** : Skill Agent (`SKILL.md`) dédiée au domaine.

---

## Cartographie des 8 Repositories

| Repository | Chemin local | Solution | Packages NuGet / NPM |Template `dotnet new` |
|---|---|---|---|---|
| **1. `blux-ui`** | `/home/gpoumarat/blux-ui` | `Blux.UI.slnx` | `Blux.UI`, `Blux.UI.Server`, `Blux.UI.Images`, `Blux.UI.Images.Local`, `Blux.UI.Build.Tasks` | `starter`, `landing`, `docs`, `portfolio`, `changelog`, `editor`, `chat` |
| **2. `blux-query`** | `/home/gpoumarat/blux-query` | `Blux.Query.slnx` | `Blux.Query.Core`, `Blux.Query`, `Blux.Query.EntityFrameworkCore` | `blux-query` |
| **3. `blux-table`** | `/home/gpoumarat/blux-table` | `Blux.Table.slnx` | `Blux.Table.Core`, `Blux.Table` | `blux-table-admin` |
| **4. `blux-form`** | `/home/gpoumarat/blux-form` | `Blux.Form.slnx` | `Blux.Form.Core`, `Blux.Form` | `blux-form-wizard` |
| **5. `blux-dashboard`** | `/home/gpoumarat/blux-dashboard` | `Blux.Dashboard.slnx` | `Blux.Dashboard.Core`, `Blux.Dashboard` | `blux-analytics` |
| **6. `blux-display`** | `/home/gpoumarat/blux-display` | `Blux.Display.slnx` | `Blux.Display.Core`, `Blux.Display` | `blux-display-portal` |
| **7. `blux-spreadsheet`** | `/home/gpoumarat/blux-spreadsheet` | `Blux.Spreadsheet.slnx` | `Blux.Spreadsheet.Core`, `Blux.Spreadsheet` | `blux-spreadsheet-app` |
| **8. `blux-agent`** | `/home/gpoumarat/blux-agent` | `Blux.Agent.slnx` | `Blux.Agent.Core`, `Blux.Agent`, `@blux/behaviors`, `@blux/agent` | `blux-agent-copilot` |
