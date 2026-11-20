# blux-ui : plan du MVP

Librairie de composants Blazor open source inspirée de Nuxt UI : composants stylés avec Tailwind CSS v4, accessibles, thémables, livrés en package NuGet `Blux.UI`.

## Décisions

### Validées avec l'utilisateur (2026-11-19)

| Sujet | Décision |
|---|---|
| Nom | Package et namespace `Blux.UI`, composants préfixés `Bx` (`<BxButton>`). `Blux.UI` est libre sur NuGet. L'ID `Blux` appartient à un autre éditeur, donc pas de réservation du préfixe `Blux.*`. |
| Navigateurs | Versions récentes (~2 ans) de Chrome, Edge, Firefox, Safari. Plancher Tailwind v4 : Safari 16.4+, Chrome 111+, Firefox 128+. E2E sur Chromium, Firefox et WebKit. |
| Modes de rendu | La lib ne fixe pas le mode. Garantis : Interactive Server, WebAssembly, Auto, avec prérendu. SSR statique garanti pour une liste précise de composants (voir tableau du MVP). |
| Personnalisation | Deux modes documentés et exclusifs. **Sans Tailwind dans l'app** : `blux.css` précompilé, personnalisation par tokens CSS (couleurs, rayons, polices) et par CSS classique, pas de surcharge utilitaire via `Class`/`Ui`. **Avec Tailwind v4 dans l'app** : surcharges `Class`/`Ui` complètes, l'app compile tout. |
| Préfixe CSS | Aucun : classes Tailwind standard. La doc impose de retirer Bootstrap du template ou de l'importer dans `@layer(bootstrap)`, sinon son CSS hors layer écrase Tailwind v4. |
| Distribution | Package NuGet (Razor Class Library), assets en static web assets. Node et Tailwind ne servent qu'à construire la lib. |
| Cible .NET | `net10.0` (LTS). CI aussi sur .NET 11 (STS, sorti le 10/11/2026). |

### Proposées par le plan (à contester si besoin)

| Sujet | Décision | Raison |
|---|---|---|
| Etat visuel vs état métier | Hover, focus, élément actif au clavier, positionnement, état ouvert des overlays côté navigateur : CSS ou JS. Valeur, validation : C#. | En Server chaque événement C# coûte un aller-retour SignalR. |
| Un seul package en 0.x | `Blux.Primitives` reste un namespace interne de `Blux.UI` jusqu'à 1.0. | Une API headless publique est difficile à stabiliser en Blazor (pas d'`asChild`), inutile de figer deux API d'un coup. |
| Couleurs | `BxColor` extensible (struct adossé à une chaîne, constantes `Primary`, `Neutral`, `Success`, `Warning`, `Error`, couleurs custom déclarées dans le thème). | Un enum bloquerait les couleurs custom, que Nuxt UI permet. |
| Type de `Ui` | Record typé par composant (`ButtonUi { Base, Label, LeadingIcon, ... }`). | Autocomplétion et erreurs à la compilation, contrairement à un dictionnaire. |
| Style des états | Attributs `data-state="open"`, `data-disabled`, `data-highlighted` ciblés par `data-[state=open]:`. | Le JS peut changer l'état sans passer par un rendu C#. |
| Icônes | Paramètres `RenderFragment` (`LeadingIcon`, `TrailingIcon`) + quelques SVG internes (fermer, chevron, check). Intégration d'un jeu d'icônes plus tard. | Pas de dépendance imposée en v1. |
| Textes intégrés | "Fermer", "Aucun résultat"... configurables via `BluxOptions.Strings`. | Localisation sans fork. |
| Dark mode | Stratégie classe `.dark` sur `<html>` (`@custom-variant dark`), script inline dans `<head>` qui applique préférence stockée ou système. | Permet de forcer clair ou sombre quel que soit l'OS. |
| Thème partagé | `AddBluxUi(...)` appelé depuis du code partagé dans le projet `.Client`, utilisé par les deux `Program.cs`. | Sinon l'apparence change quand WASM prend le relais en Auto. |
| Assets générés | `blux.css`, `blux.preflight.css`, `blux.sources.css` et les modules JS sont commités. La CI reconstruit et échoue si le résultat diffère. | Évite les problèmes d'ordre entre génération et découverte des static web assets. |
| Version Tailwind | Épinglée au plus à la dernière mineure supportée par TailwindMerge.NET (4.2 aujourd'hui). | Sinon la fusion de classes rate les nouvelles utilitaires. |

## Architecture

```
App Blazor (Server / WASM / Auto / SSR)
  │  <BxButton>, <BxModal>, ...           AddBluxUi(o => ...)  (code partagé .Client)
  ▼
Blux.UI  (composants stylés, public)
  - thèmes par composant : slots, variantes, variantes composées, défauts
  - classes : thème défaut → AddBluxUi → Ui → Class, fusion via IClassMerger (TailwindMerge.NET)
  - CSS : blux.preflight.css + blux.css (mode sans Tailwind)
          blux.sources.css = @theme + @source inline(...) (mode Tailwind)
  ▼
Blux.UI.Primitives  (namespace interne jusqu'à 1.0)
  - état, ids ARIA, intégration EditContext, patterns WAI-ARIA APG
  - JS : dismiss, focus, roving focus + typeahead, scroll lock, positionnement (Floating UI)
  ▼
Blazor + HTML natif (<dialog>, popover, <input>)
```

### Règles imposées par les retours de revue

- **Overlays natifs pilotés par JS.** Blazor ne rend jamais l'attribut `open` sur `<dialog>` (ça donne un dialog non modal, et `showModal()` lève alors `InvalidStateError`). Le module JS appelle `showModal()`/`close()` et `showPopover()`/`hidePopover()`, et remonte les événements `close`, `cancel`, `toggle` vers C#. Les décisions synchrones (bloquer Escape si `Dismissible=false`) sont poussées au JS à l'avance. Si les handlers Blazor ne couvrent pas ces événements, on passe par `Blazor.registerCustomEventType`.
- **Inputs utilisables hors `EditForm`.** `BxInput` & co rendent un cœur basé sur `InputBase<T>` quand un `EditContext` est en cascade (validation, `NameAttributeValue` pour le POST en SSR), et un input simple sinon (barre de recherche dans un header, par exemple).
- **Visuels des cases à cocher en CSS.** Checkbox et Switch se stylent via `:checked` (`peer-checked:`, `has-[:checked]:`), pas via la valeur C#, sinon le SSR statique affiche un état faux.
- **Select dans un formulaire statique.** La listbox custom est doublée d'un `<input type="hidden">` qui porte le nom et la valeur.
- **Prérendu.** Chaque composant interactif documente son rendu avant le démarrage du runtime (`RendererInfo.IsInteractive`) : pas de déclencheur qui semble actif mais ne fait rien.
- **Toast.** Toaster piloté par JS, alimenté par `IToastService` depuis n'importe quel runtime. En Auto, l'instance de service n'est pas la même côté Server et côté WASM, donc le JS sert de point commun. Le déclenchement depuis une page SSR statique (après un POST) viendra plus tard par cookie, l'architecture le permet sans casser l'API.
- **Accordion.** Pattern APG (boutons dans des titres + régions), pas `<details>` : `<details>` implémente le pattern disclosure et certains lecteurs d'écran aplatissent un titre placé dans `<summary>`. Donc l'Accordion n'est pas garanti en SSR statique en v1.
- **Preflight séparé.** `blux.preflight.css` n'est chargé qu'en mode sans Tailwind, pour éviter un double preflight quand l'app a son propre Tailwind.
- **Fusion de classes isolée.** TailwindMerge.NET est utilisé derrière une interface interne `IClassMerger`, avec un cache borné et thread-safe (singleton partagé en Server). Il faut vérifier en phase 1 qu'on peut lui déclarer les tokens custom (`rounded-(--blux-radius)`, tailles de texte).

## Structure de la solution

```
blux-ui/
  src/
    Blux.UI/                RCL : Components/, Primitives/ (internal), Themes/,
                            styles/ (source Tailwind), wwwroot/ (CSS et JS générés, commités),
                            buildTransitive/Blux.UI.targets (copie blux.sources.css dans l'app)
  playground/
    Blux.Playground/        Blazor Web App (Auto) + .Client, une page par composant,
                            sélecteur de mode (Server / WASM / Auto / SSR statique)
  tests/
    Blux.UI.Tests/          bUnit : rendu, paramètres, classes, ARIA, EditContext,
                            couverture des classes (chaque combinaison de variantes → sélecteur présent dans blux.css)
    Blux.UI.E2E/            Playwright .NET + axe-core
  build/                    scripts Tailwind (CLI standalone épinglé) + esbuild pour les modules JS
  .github/workflows/        CI
```

**Matrice E2E** : modes Server, WASM, Auto, SSR statique (prérendu actif) × Chromium, Firefox, WebKit, plus une passe sur un build WASM publié et trimmé (le debug ne teste jamais le trimming, et le code regex de TailwindMerge.NET est un risque à vérifier). Unit tests sur `net10.0` et `net11.0`.

## Composants du MVP

| Groupe | Composants | SSR statique en v1 |
|---|---|---|
| Affichage | Button, Badge, Avatar, Card, Alert | Oui, sauf Alert refermable et fallback d'Avatar sur erreur d'image |
| Formulaires | Input, Textarea, Checkbox, Switch, RadioGroup, FormField | Oui (`EditForm` + `FormName`) |
| Overlays | Modal, Popover, DropdownMenu, Tooltip | Non |
| Composés | Select, Tabs, Accordion, Toast | Non (Select poste sa valeur via l'input caché) |
| Données | Table | Affichage oui, tri interactif seulement. Choix entre composant maison et habillage de QuickGrid en phase 5. |

## Avancement

**Phase 1 terminée (2026-11-19).** Button, Switch et Popover, moteur de thème, pipeline CSS/JS, playground 4 modes,
80 tests bUnit (net10.0 + net11.0) et 41 tests E2E par navigateur (Chromium et Firefox verts en local ; WebKit en CI
seulement, ses dépendances système demandent sudo), plus une passe E2E sur le build publié et trimmé.

Décisions prises pendant le spike :

- **Primitives en custom elements** (`<bx-popover>`), chargés par le JS initializer `Blux.UI.lib.module.js`. Ils marchent
  sans runtime Blazor, donc **les overlays sont garantis en SSR statique** (validé avec l'utilisateur), testés dans les 4 modes.
- **Protocole JS/C#** : événement `bx-toggle` (`open`, `sequence`), `stopPropagation` pour les overlays imbriqués,
  `data-bx-seq` rendu avant `open` pour ignorer un rendu calculé avant le dernier toggle. Sans `@bind-Open`, aucun aller-retour.
- **Couleurs par variables** : `data-color` pose `--bx-color`, `--bx-color-text`, `--bx-color-contrast`. Sept couleurs
  intégrées (primary, secondary, success, info, warning, error, neutral) ; une couleur custom = trois variables CSS.
- **Bouton inerte** : un `BxButton` avec `OnClick` est désactivé tant que le rendu n'est pas interactif (validé).
- **Mode Tailwind** : le package copie `blux.theme.css` (tokens + `@source inline`) dans `obj/blux/` à chaque build
  (`buildTransitive`), validé avec l'utilisateur et testé sur un package consommé. Remplace `blux.sources.css`.
- **Versions** : Tailwind 4.3.5 (TailwindMerge.NET 1.5.0 supporte jusqu'à 4.3), outillage npm à la racine du repo.
- **Contraste** : success et info pleins passés en nuance 700, rail du Switch `--blux-control` (zinc-500, 3:1).

Reste à faire avant la phase 2 :

- Pousser le repo sur GitHub pour faire tourner la CI (WebKit et la passe trimmée n'ont tourné qu'en local pour la seconde).
- La couleur `warning` pleine (amber-500) reste sous 3:1 en contraste non textuel contre un fond blanc pour un Switch coché.

**Phases 2 à 5 livrées (2026-11-19)** : les 20 composants du MVP, et le playground devenu site de documentation
(accueil, guides, une page par composant avec exemples vivants, code source et table d'API générée depuis la doc XML).

- Primitives navigateur ajoutées : `<bx-modal>` (dialog natif), `<bx-tooltip>`, menu clavier sur `<bx-popover role=menu>`,
  `<bx-tabs>`, `<bx-accordion>` (`hidden="until-found"`), `<bx-select>` (combobox APG, valeur postée par input caché), `<bx-toaster>`.
- Formulaires : base commune `BxFieldBase<T>` (EditForm, nom posté, `aria-invalid`), contexte `FormFieldContext` cascadé par
  `BxFormField` (id, label, description, erreur).
- Toasts : `IToastService` appelle `Blux.toast(...)` avec des arguments primitifs (sûr au trimming), le toaster est géré par le navigateur.
- Le site compile son propre Tailwind et importe `blux.theme.css` : il valide le mode « Tailwind dans l'app ».
- Tests : 134 bUnit (net10 + net11), 191 E2E par navigateur (Chromium et Firefox verts, plus le build publié trimmé) dont un scan axe de chaque page de composant dans les 4 modes.

**Refonte visuelle (2026-11-20)** : palette par défaut sans indigo/violet, inspirée de drizzle-resource.chronicstone.dev
(validé : jaune signal `#ffd23f` en primary, fond quasi noir `#0a0b0c` / papier `#f5f5f2`, secondary teal), polices Geist
et Geist Mono sur le site, accueil avec démo live « dans le navigateur / envoyé à .NET », grille de composants vivants et
pipeline animé. Contrastes vérifiés par axe sur toutes les pages de composants, en clair et en sombre.

## Ordre de construction

1. **Fondations + spike.** Solution, RCL, pipeline Tailwind/esbuild et vérification des assets en CI, moteur de thème + `IClassMerger`, tokens, dark mode, playground, CI complète. Le spike construit trois composants qui stressent l'API : **Button** (variantes), **Switch** (formulaire, SSR, `:checked`) et **Popover** (overlay natif piloté par JS, prérendu, Auto). Il tranche aussi un point d'architecture : primitives en **custom elements** (marchent avec et sans runtime Blazor, donc compatibles SSR plus tard) ou en modules isolés `IJSObjectReference` (interactif seulement). Fin de phase : l'API du thème et des primitives est figée.
2. **Affichage.** Badge, Avatar, Card, Alert.
3. **Formulaires.** Input, Textarea, Checkbox, RadioGroup, FormField (Switch fait en phase 1).
4. **Overlays.** Modal, Tooltip, DropdownMenu, sur les primitives validées en phase 1.
5. **Composés et données.** Select, Tabs, Accordion, Toast, Table.
6. **Release 0.1.0.** Packaging NuGet (SourceLink, symboles, README, icône), SemVer, doc minimale (playground publié), annonce.

## Définition de "terminé" pour un composant

- Thème avec variantes, surcharges `Class` et `Ui` testées (mode Tailwind), tokens testés (mode sans Tailwind).
- Pattern WAI-ARIA APG respecté, exceptions écrites dans la doc du composant.
- bUnit + E2E sur toute la matrice, zéro violation axe-core.
- Comportement au prérendu et en SSR statique documenté.
- Dark mode vérifié, y compris après navigation améliorée.
- Page playground + documentation des paramètres.

## Questions encore ouvertes

- Licence (proposition : MIT).
- Hébergement du repo et de la CI (proposition : GitHub public + GitHub Actions).
- Installation du SDK .NET 10 dans le WSL de dev (absent aujourd'hui).
