# klyroo-ui : plan du MVP

Librairie de composants Blazor open source inspirée de Nuxt UI : composants stylés avec Tailwind CSS v4, accessibles, thémables, livrés en package NuGet `Klyroo.UI`.

## Décisions

### Validées avec l'utilisateur (2026-11-19)

| Sujet | Décision |
|---|---|
| Nom | Package et namespace `Klyroo.UI`, composants préfixés `Kly` (`<KlyButton>`). `Klyroo.UI` est libre sur NuGet. L'ID `Klyroo` appartient à un autre éditeur, donc pas de réservation du préfixe `Klyroo.*`. |
| Navigateurs | Versions récentes (~2 ans) de Chrome, Edge, Firefox, Safari. Plancher Tailwind v4 : Safari 16.4+, Chrome 111+, Firefox 128+. E2E sur Chromium, Firefox et WebKit. |
| Modes de rendu | La lib ne fixe pas le mode. Garantis : Interactive Server, WebAssembly, Auto, avec prérendu. SSR statique garanti pour une liste précise de composants (voir tableau du MVP). |
| Personnalisation | Deux modes documentés et exclusifs. **Sans Tailwind dans l'app** : `klyroo.css` précompilé, personnalisation par tokens CSS (couleurs, rayons, polices) et par CSS classique, pas de surcharge utilitaire via `Class`/`Ui`. **Avec Tailwind v4 dans l'app** : surcharges `Class`/`Ui` complètes, l'app compile tout. |
| Préfixe CSS | Aucun : classes Tailwind standard. La doc impose de retirer Bootstrap du template ou de l'importer dans `@layer(bootstrap)`, sinon son CSS hors layer écrase Tailwind v4. |
| Distribution | Package NuGet (Razor Class Library), assets en static web assets. Node et Tailwind ne servent qu'à construire la lib. |
| Cible .NET | `net10.0` (LTS). CI aussi sur .NET 11 (STS, sorti le 10/11/2026). |

### Proposées par le plan (à contester si besoin)

| Sujet | Décision | Raison |
|---|---|---|
| Etat visuel vs état métier | Hover, focus, élément actif au clavier, positionnement, état ouvert des overlays côté navigateur : CSS ou JS. Valeur, validation : C#. | En Server chaque événement C# coûte un aller-retour SignalR. |
| Un seul package en 0.x | `Klyroo.Primitives` reste un namespace interne de `Klyroo.UI` jusqu'à 1.0. | Une API headless publique est difficile à stabiliser en Blazor (pas d'`asChild`), inutile de figer deux API d'un coup. |
| Couleurs | `KlyColor` extensible (struct adossé à une chaîne, constantes `Primary`, `Neutral`, `Success`, `Warning`, `Error`, couleurs custom déclarées dans le thème). | Un enum bloquerait les couleurs custom, que Nuxt UI permet. |
| Type de `Ui` | Record typé par composant (`ButtonUi { Base, Label, LeadingIcon, ... }`). | Autocomplétion et erreurs à la compilation, contrairement à un dictionnaire. |
| Style des états | Attributs `data-state="open"`, `data-disabled`, `data-highlighted` ciblés par `data-[state=open]:`. | Le JS peut changer l'état sans passer par un rendu C#. |
| Icônes | Paramètres `RenderFragment` (`LeadingIcon`, `TrailingIcon`) + quelques SVG internes (fermer, chevron, check). Intégration d'un jeu d'icônes plus tard. | Pas de dépendance imposée en v1. |
| Textes intégrés | "Fermer", "Aucun résultat"... configurables via les locales (`KlyLocale`, `KlyMessages`). | Localisation sans fork. |
| Dark mode | Stratégie classe `.dark` sur `<html>` (`@custom-variant dark`), script inline dans `<head>` qui applique préférence stockée ou système. | Permet de forcer clair ou sombre quel que soit l'OS. |
| Thème partagé | `AddKlyrooUi(...)` appelé depuis du code partagé dans le projet `.Client`, utilisé par les deux `Program.cs`. | Sinon l'apparence change quand WASM prend le relais en Auto. |
| Assets générés | `klyroo.css`, `klyroo.preflight.css`, `klyroo.sources.css` et les modules JS sont commités. La CI reconstruit et échoue si le résultat diffère. | Évite les problèmes d'ordre entre génération et découverte des static web assets. |
| Version Tailwind | Épinglée au plus à la dernière mineure supportée par TailwindMerge.NET (4.2 aujourd'hui). | Sinon la fusion de classes rate les nouvelles utilitaires. |

## Architecture

```
App Blazor (Server / WASM / Auto / SSR)
  │  <KlyButton>, <KlyModal>, ...           AddKlyrooUi(o => ...)  (code partagé .Client)
  ▼
Klyroo.UI  (composants stylés, public)
  - thèmes par composant : slots, variantes, variantes composées, défauts
  - classes : thème défaut → AddKlyrooUi → Ui → Class, fusion via IClassMerger (TailwindMerge.NET)
  - CSS : klyroo.preflight.css + klyroo.css (mode sans Tailwind)
          klyroo.sources.css = @theme + @source inline(...) (mode Tailwind)
  ▼
Klyroo.UI.Primitives  (namespace interne jusqu'à 1.0)
  - état, ids ARIA, intégration EditContext, patterns WAI-ARIA APG
  - JS : dismiss, focus, roving focus + typeahead, scroll lock, positionnement (Floating UI)
  ▼
Blazor + HTML natif (<dialog>, popover, <input>)
```

### Règles imposées par les retours de revue

- **Overlays natifs pilotés par JS.** Blazor ne rend jamais l'attribut `open` sur `<dialog>` (ça donne un dialog non modal, et `showModal()` lève alors `InvalidStateError`). Le module JS appelle `showModal()`/`close()` et `showPopover()`/`hidePopover()`, et remonte les événements `close`, `cancel`, `toggle` vers C#. Les décisions synchrones (bloquer Escape si `Dismissible=false`) sont poussées au JS à l'avance. Si les handlers Blazor ne couvrent pas ces événements, on passe par `Blazor.registerCustomEventType`.
- **Inputs utilisables hors `EditForm`.** `KlyInput` & co rendent un cœur basé sur `InputBase<T>` quand un `EditContext` est en cascade (validation, `NameAttributeValue` pour le POST en SSR), et un input simple sinon (barre de recherche dans un header, par exemple).
- **Visuels des cases à cocher en CSS.** Checkbox et Switch se stylent via `:checked` (`peer-checked:`, `has-[:checked]:`), pas via la valeur C#, sinon le SSR statique affiche un état faux.
- **Select dans un formulaire statique.** La listbox custom est doublée d'un `<input type="hidden">` qui porte le nom et la valeur.
- **Prérendu.** Chaque composant interactif documente son rendu avant le démarrage du runtime (`RendererInfo.IsInteractive`) : pas de déclencheur qui semble actif mais ne fait rien.
- **Toast.** Toaster piloté par JS, alimenté par `IToastService` depuis n'importe quel runtime. En Auto, l'instance de service n'est pas la même côté Server et côté WASM, donc le JS sert de point commun. Le déclenchement depuis une page SSR statique (après un POST) viendra plus tard par cookie, l'architecture le permet sans casser l'API.
- **Accordion.** Pattern APG (boutons dans des titres + régions), pas `<details>` : `<details>` implémente le pattern disclosure et certains lecteurs d'écran aplatissent un titre placé dans `<summary>`. Donc l'Accordion n'est pas garanti en SSR statique en v1.
- **Preflight séparé.** `klyroo.preflight.css` n'est chargé qu'en mode sans Tailwind, pour éviter un double preflight quand l'app a son propre Tailwind.
- **Fusion de classes isolée.** TailwindMerge.NET est utilisé derrière une interface interne `IClassMerger`, avec un cache borné et thread-safe (singleton partagé en Server). Il faut vérifier en phase 1 qu'on peut lui déclarer les tokens custom (`rounded-(--klyroo-radius)`, tailles de texte).

## Structure de la solution

```
klyroo-ui/
  src/
    Klyroo.UI/                RCL : Components/, Primitives/ (internal), Themes/,
                            styles/ (source Tailwind), wwwroot/ (CSS et JS générés, commités),
                            buildTransitive/Klyroo.UI.targets (copie klyroo.sources.css dans l'app)
  playground/
    Klyroo.Playground/        Blazor Web App (Auto) + .Client, une page par composant,
                            sélecteur de mode (Server / WASM / Auto / SSR statique)
  tests/
    Klyroo.UI.Tests/          bUnit : rendu, paramètres, classes, ARIA, EditContext,
                            couverture des classes (chaque combinaison de variantes → sélecteur présent dans klyroo.css)
    Klyroo.UI.E2E/            Playwright .NET + axe-core
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

- **Primitives en custom elements** (`<kly-popover>`), chargés par le JS initializer `Klyroo.UI.lib.module.js`. Ils marchent
  sans runtime Blazor, donc **les overlays sont garantis en SSR statique** (validé avec l'utilisateur), testés dans les 4 modes.
- **Protocole JS/C#** : événement `kly-toggle` (`open`, `sequence`), `stopPropagation` pour les overlays imbriqués,
  `data-kly-seq` rendu avant `open` pour ignorer un rendu calculé avant le dernier toggle. Sans `@bind-Open`, aucun aller-retour.
- **Couleurs par variables** : `data-color` pose `--kly-color`, `--kly-color-text`, `--kly-color-contrast`. Sept couleurs
  intégrées (primary, secondary, success, info, warning, error, neutral) ; une couleur custom = trois variables CSS.
- **Bouton inerte** : un `KlyButton` avec `OnClick` est désactivé tant que le rendu n'est pas interactif (validé).
- **Mode Tailwind** : le package copie `klyroo.theme.css` (tokens + `@source inline`) dans `obj/klyroo/` à chaque build
  (`buildTransitive`), validé avec l'utilisateur et testé sur un package consommé. Remplace `klyroo.sources.css`.
- **Versions** : Tailwind 4.3.5 (TailwindMerge.NET 1.5.0 supporte jusqu'à 4.3), outillage npm à la racine du repo.
- **Contraste** : success et info pleins passés en nuance 700, rail du Switch `--klyroo-control` (zinc-500, 3:1).

Reste à faire avant la phase 2 :

- Pousser le repo sur GitHub pour faire tourner la CI (WebKit et la passe trimmée n'ont tourné qu'en local pour la seconde).

**Phases 2 à 5 livrées (2026-11-19)** : les 20 composants du MVP, et le playground devenu site de documentation
(accueil, guides, une page par composant avec exemples vivants, code source et table d'API générée depuis la doc XML).

- Primitives navigateur ajoutées : `<kly-modal>` (dialog natif), `<kly-tooltip>`, menu clavier sur `<kly-popover role=menu>`,
  `<kly-tabs>`, `<kly-accordion>` (`hidden="until-found"`), `<kly-select>` (combobox APG, valeur postée par input caché), `<kly-toaster>`.
- Formulaires : base commune `KlyFieldBase<T>` (EditForm, nom posté, `aria-invalid`), contexte `FormFieldContext` cascadé par
  `KlyFormField` (id, label, description, erreur).
- Toasts : `IToastService` appelle `Klyroo.toast(...)` avec des arguments primitifs (sûr au trimming), le toaster est géré par le navigateur.
- Le site compile son propre Tailwind et importe `klyroo.theme.css` : il valide le mode « Tailwind dans l'app ».
- Tests : 134 bUnit (net10 + net11), 191 E2E par navigateur (Chromium et Firefox verts, plus le build publié trimmé) dont un scan axe de chaque page de composant dans les 4 modes.

**Refonte visuelle (2026-11-20)** : palette par défaut sans indigo/violet, inspirée de drizzle-resource.chronicstone.dev
(validé : jaune signal `#ffd23f` en primary, fond quasi noir `#0a0b0c` / papier `#f5f5f2`, secondary teal), polices Geist
et Geist Mono sur le site, accueil avec démo live « dans le navigateur / envoyé à .NET », grille de composants vivants et
pipeline animé. Contrastes vérifiés par axe sur toutes les pages de composants, en clair et en sombre.

**Catalogue Nuxt UI (2026-11-20)** : vagues A à D, 133 composants documentés dans les familles de Nuxt UI (Elements, Form,
Overlay, Navigation, Data, Layout, Page, Dashboard, Chat, Editor), chacun avec sa page et ses exemples dans les 4 modes.
Thèmes figés au premier rendu ; contraste garanti par des tests (3:1 pour les indicateurs, 4,5:1 pour le texte, teintes
jusqu'à 20 %, 4 surfaces, survol et appui) ; le jaune signal garde sa teinte et reçoit un anneau là où il est le seul repère.

**Intégrations à la Nuxt UI (2026-11-20)**, chacune revue par un expert, chaque constat vérifié puis corrigé test d'abord :

- **Design system et variables CSS** : `o.Colors.Primary = KlyPalette.Blue` (26 palettes Tailwind, `KlyPalette.Custom`,
  couleurs sémantiques ajoutées), `<KlyThemeStyle />`, `o.Defaults.Color/Size`, jetons complétés (texte, fonds, bordures,
  conteneur, header, rayons, graphiques), utilitaires Tailwind `text-default`, `bg-elevated`… Une palette qui ne peut pas
  tenir les contrastes lève une erreur (sauf `AllowLowContrast`).
- **i18n** : `KlyLocale` (code, nom, direction, messages), 12 langues dont l'arabe en RTL, choix automatique aligné sur
  `RequestLocalization` (repli par cultures parentes, zh-Hans), formats selon la culture régionale, `KlyCultureMeta` pour
  que le client WASM reprenne la culture du serveur. `KlyrooOptions.Strings` remplacé par `KlyMessages` (cassant).
- **Icônes** : noms Iconify (`i-ph-x`, `ph:x`), 49 icônes de composants remplaçables (`o.Icons.Close`), collecte au build
  des noms utilisés, téléchargement depuis l'API Iconify avec cache, dossiers de SVG perso ; tous les SVG passent par une
  liste blanche, identifiants rendus uniques, enregistrement déterministe en Auto.
- **Polices** : `<KlyrooFont Include="Public Sans" Role="sans" Provider="google" />` télécharge au build et sert depuis le site
  (`/_klyroo/fonts`, cache immuable), polices de repli calibrées, `<KlyFonts />` ; le site de doc n'appelle plus Google Fonts.
- **Images** : `<KlyImage>` (`srcset`/`sizes` à la Nuxt) utilisé par Avatar, BlogPost, ChangelogVersion, ColorModeImage,
  PageLogos, ChatAttachment ; paquets `Klyroo.UI.Images` (Cloudinary, imgix, ImageKit, Cloudflare, Netlify, Vercel, Bunny,
  modèle) et `Klyroo.UI.Images.Local` (endpoint libvips durci : pas de traversée, SSRF bloqué, limites, cache borné).
- **IA** : serveur MCP (`dotnet tool` `klyroo-ui-mcp` et `/mcp` sur le site), `llms.txt` et `llms-full.txt`, skill
  `skills/klyroo-ui`, tous générés depuis le catalogue et la doc XML.
- **Release 0.1.0 préparée** : métadonnées NuGet, symboles, icône, CHANGELOG, contrôle de paquet `build/verify-package.mjs`,
  workflow de release sur tag `v*` (job de publication séparé, environnement `nuget`).

**MAUI Blazor Hybrid (2026-11-20)** : Klyroo tourne dans un `BlazorWebView` (Android testé, Windows/iOS 17+/Mac Catalyst compilés en CI, Android TV expérimental, Tizen non supporté).
Décisions : détection par `RendererInfo.Name == "WebView"` côté .NET (`KlyrooHost`, `IsHybrid`) et par le pont `window.external.sendMessage/receiveMessage` côté JS
(`html[data-kly-host=webview]`) ; l'initialiseur exporte `beforeStart`/`afterStarted` (seuls exécutés dans une WebView) en plus de `afterWebStarted` ; état du sidebar en
localStorage, culture via `IKlyCultureStore` (localStorage par défaut, Preferences dans l'exemple), `KlyHead` + `HeadOutlet` en `head::after` pour le thème et les polices (le
script du mode de couleur est remplacé par l'initialiseur), pas de préchargement d'image, cibles 44 px en `pointer: coarse`, `env(safe-area-inset-*)`, tooltips au appui long,
mode TV (`KlyApp SpatialNavigation`) avec navigation spatiale. Exemple : `samples/Klyroo.Maui` (TFM `net10.0`, `-p:KlyrooSampleTfm=net11.0` avec la charge de travail .NET 11).

**Templates (2026-11-21)** : équivalent de ui.nuxt.com/templates, 10 templates, chacun futur dépôt GitHub template `klyroo-template-<nom>`
(préparés dans `templates/`, exportés par `build/export-template.mjs`, vérifiés contre le paquet NuGet par `build/verify-templates.mjs`, renommage `rename.cs`,
CI de build seule). Apps (Starter, Dashboard, Chat, Editor, Calendar) : RCL partagée + Web (Auto) + MAUI ; sites de contenu (Landing, SaaS, Docs, Portfolio,
Changelog) : Web, SSR statique. Contenu Markdig + YamlDotNet, copie de référence `templates/_shared/Content`. Page `/templates` du site avec démos en direct
(processus enfants derrière YARP, chien de garde si l'hôte meurt). Prérequis livrés dans la librairie : Tailwind sans Node (`KlyrooTailwind`), apps sous un
chemin de base, `KlySkipLink`, fond du `body`, index de recherche chargé à la demande.
Faits : Starter, Landing, SaaS (auth en UI seule), Docs, Portfolio (formulaire de contact en UI seule), Changelog, Dashboard (sur `KlyDataList`, couleurs en cookie lues au rendu serveur), Chat (fournisseur compatible OpenAI, démo simulée par
visiteur, garde-fou de dépenses et politique d'autorisation), Editor (`KlyEditor` avec listes de tâches et images http(s), documents en `localStorage`). Reste : Calendar (sur `KlyScheduler`).

**Port de nuxt-ui-tools (2026-11-21)** : les 11 domaines (spécifications dans `.claude/specs/nuxt-ui-tools/`, non versionnées). Paquets compagnons
`Klyroo.UI.Tools`, `Klyroo.UI.Tools.Query` (sans Blazor), `Klyroo.UI.Tools.EntityFrameworkCore`, plus tard `Klyroo.UI.Spreadsheet` : même version exacte que Klyroo.UI,
compilation Tailwind obligatoire. API d'extension publique dans le cœur. Faits : état d'URL (`QueryState`), modèle de requête et moteur en mémoire, application
sur `IQueryable` (tests différentiels EF Core/SQLite, NativeAOT), `KlyDataList` (tri, pagination, URL, SSR, prérendu persisté sous budget de taille,
recherche, filtres texte/option/booléen/nombre/date avec presets et plages, panneau de filtres en barre latérale ou en tiroir, facettes (compteurs sur les filtres d'option et booléens, modes, tri, limite, conservés pendant la pagination) et filtres d'options distants, fuseau de l'utilisateur via cookie) ; paquet `Klyroo.UI.Server` (`AddKlyrooServer`,
`IKlyRequestCookies` dans le cœur, cookies lus au rendu serveur) ; dans le cœur : case indéterminée, `KlyInput.Debounce`/`SetValueAsync`, classes
`FieldCssClassProvider` partout, `KlyInfiniteScroll`, options avec compteurs/icônes/groupes, options chargées à la demande (`OptionsProvider`, `OptionsKey`)
pour `KlySelectMenu`/`KlyInputMenu` ; sélection de lignes et actions (colonne de cases, tri-état, plage Maj, `SelectedKeys`, portées page/tout, barre d'actions avec confirmation, menu d'actions de barre d'outils, interactif seulement) ; actions de ligne (`DataListRowAction`, boutons et menu avec groupes et sous-menus, feuille sous `md`, liens en rendu statique ; sous-menus dans `KlyDropdownMenu` du cœur) ; pagination par curseur, `KlyDataListInfiniteLoader` (auto ou manuel, erreur/réessai en ligne, lien `?c=` + retour au début en SSR) et virtualisation (`KlyDataListTable.Virtualize`, interactif seulement, hauteurs 36/44/60 px, `aria-rowcount`/`aria-rowindex`) ; panneau de colonnes (`KlyDataListColumnPanel`, visibilité, ordre, épinglage début/fin, redimensionnement souris/clavier, cookie `PreferencesKey` lu côté serveur dès le premier rendu) ; pages de doc du DataList scindées pour tenir sous le budget d'état persisté. Restent : grille,
groupes, arbre, totaux, puis Forms, Analytics, Display/Media/FilePreview, Spreadsheet (ordre alterné avec les templates).

Reste à faire :

- Faire tourner l'exemple sur un émulateur Android (phone et TV), vérifier `HeadOutlet`, les polices servies et les touches D-pad ; builds iOS, Mac Catalyst et Windows seulement en CI.
- Choisir le compte GitHub, pousser, faire tourner la CI (WebKit, passe trimmée) ; renseigner `RepositoryUrl`.
- Héberger le site de doc : URL réelle du MCP HTTP, `Klyroo:SiteUrl` pour `llms.txt`, dépôt cible de `npx skills add`.
- Faire relire les 11 traductions par des locuteurs natifs.
- Polices de repli sous le MSBuild .NET Framework (Visual Studio) : Brotli absent, repli non calibré.
- Signer les commits faits sans GPG (`git rebase --exec 'git commit --amend --no-edit -S' <base>`).

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
