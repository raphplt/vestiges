# VESTIGES

Roguelite d'exploration-combat nomade en vue isométrique 2D, dans un monde post-apocalyptique en train d'être oublié.
Le joueur avance toujours ; derrière lui, l'**Effacement** dévore la réalité. La question n'est pas « combien de nuits » mais « jusqu'où ».

Moteur Godot 4.7, C# (.NET 10), renderer GL Compatibility.

> Pivot V2 (mars 2026) : jour/nuit, Foyer in-run, craft, construction et ressources matérielles ont été **supprimés**.
> Ne pas les réintroduire, ne pas s'appuyer sur les sections du GDD/Architecture/Roadmap V1 qui les décrivent.

## Documentation (`doc/`)

Ordre d'autorité en cas de conflit :

1. [VESTIGES-STRATEGIE-V2.md](doc/VESTIGES-STRATEGIE-V2.md) : direction actuelle, systèmes V2 (Effacement, Résurgences, Autels, Essence, raretés, endgame), roadmap V2 (§25). **Fait autorité sur le gameplay et la roadmap.**
2. [VESTIGES-GDD.md](doc/VESTIGES-GDD.md) : vision, personnages, bestiaire, score, UI/UX. Valable hors sections obsolètes listées en §3 de la Stratégie V2.
3. [VESTIGES-ARCHITECTURE.md](doc/VESTIGES-ARCHITECTURE.md) : principes d'architecture (couches, data flow). Les sections Base/Craft/Foyer sont obsolètes.
4. [VESTIGES-BIBLE.md](doc/VESTIGES-BIBLE.md) : lore, cosmologie, direction artistique, audio, constellations.
5. [CHARTE-GRAPHIQUE.md](doc/CHARTE-GRAPHIQUE.md) : palettes, conventions sprites, contours, animation, nommage. **Fait autorité sur le visuel.**
6. Références : [ASSET-LIST.md](doc/ASSET-LIST.md), [AUDIO-GUIDE.md](doc/AUDIO-GUIDE.md), [PROGRESSION-SYSTEM.md](doc/PROGRESSION-SYSTEM.md), [PROMPT-PIXEL-ART-BIOMES.md](doc/PROMPT-PIXEL-ART-BIOMES.md).
7. [VESTIGES-ROADMAP.md](doc/VESTIGES-ROADMAP.md) : roadmap V1 (phases 0-6 historiques). Ne plus y ajouter de cases.

Consulter la Stratégie V2 avant de proposer une feature ou un changement architectural.

## Commandes

| Action | Commande |
|--------|----------|
| Build C# | `dotnet build` |
| Smoke test (build + import + boot headless ~10 s) | `tools/smoke_test.sh [frames]` |
| Lancer le jeu | `godot-mono --path .` (la version doit correspondre à `Vestiges.csproj`) |
| Générer des sprites | `python3 tools/<script>.py` ou `python3 scripts/generate_*.py` (Pillow) |

`GODOT_BIN` surcharge le binaire Godot utilisé par `tools/smoke_test.sh`.
En headless, ces avertissements sont normaux : DLL Steam absente, « MixRate mismatch » (driver audio factice), fuites ObjectDB à la fermeture.

Avant de déclarer une tâche terminée : `dotnet build` sans warning, et `tools/smoke_test.sh` vert si la tâche touche scènes, shaders, `project.godot` ou l'initialisation.

## Stack technique

| Composant | Choix |
|-----------|-------|
| Moteur | Godot 4.7.2 (.NET), SDK `Godot.NET.Sdk/4.7.2` |
| Langage | C# 14 / .NET 10 (LTS) |
| Physique | Jolt Physics |
| Renderer | GL Compatibility (OpenGL) |
| Vue | Isométrique 2D |
| Données | JSON dans `data/`, parsé via `Godot.Json` et mappé à la main par les loaders de `scripts/Infrastructure/` ; sauvegardes méta/historique en `System.Text.Json` |
| Plateforme | Steamworks.NET (DLL native non versionnée, Steam désactivé si absente) |

Monter la version de Godot = changer **ensemble** le SDK dans `Vestiges.csproj`, `config/features` dans `project.godot` et le binaire éditeur local.

## Structure du projet

```
vestiges/
├── doc/                     # Design et technique (voir ordre d'autorité)
├── scenes/                  # Scenes par feature (Hub.tscn = main scene, Main.tscn = run)
├── scripts/                 # C# par système (namespace Vestiges.<Dossier>)
│   ├── Core/                # Player, GameManager, EventBus, GroupCache
│   ├── Combat/              # Ennemis, armes, projectiles, VFX, sprite loaders
│   ├── Progression/         # Perks, quêtes, Essence, fragments, objets maudits
│   ├── World/               # Génération procédurale, biomes, props, Effacement, Autels, POI, coffres, lore
│   ├── Spawn/               # SpawnManager, EnemyPool
│   ├── Events/              # Résurgences (CrisisManager), endgame
│   ├── Meta/                # Souvenirs, persistance cross-run
│   ├── Score/               # Calcul du score
│   ├── UI/                  # HUD, Hub, level-up, paramètres, debug
│   ├── Infrastructure/      # Loaders JSON, sauvegarde, audio, Steam, analytics, locale, input
│   └── generate_*.py        # Générateurs de sprites historiques (à migrer vers tools/)
├── data/                    # JSON de gameplay (enemies, weapons, perks, biomes, scaling, quests, ...)
├── assets/                  # Sprites, audio, fonts, shaders, traductions (par feature)
├── tools/                   # Scripts outillage (smoke test, générateurs/pipeline sprites)
└── project.godot
```

Autoloads (`project.godot`) : EventBus, GameManager, AudioManager, GroupCache, SteamManager, LocaleManager, InputRemapManager, ColorBlindFilter, AnalyticsManager.

## Exigences de qualité

Niveau professionnel visé : code propre, optimisé, performant.

- **Performance first** : 60 FPS constants sur hardware mid-range. Pas d'allocation ni de `GetNode`/`GetTree().GetNodesInGroup` par frame dans `_Process`/`_PhysicsProcess` (utiliser `GroupCache`, caches, pools).
- **Data-driven** : stats, recettes de loot, courbes de scaling, tables vivent en JSON, jamais en dur.
- **Modularité** : un système = un module aux responsabilités claires.
- **Découplage** : signaux Godot parent→enfant, `EventBus` pour l'inter-systèmes. Pas de références croisées directes. Garder le coop v2 possible.
- **État explicite** : state de run et state méta séparés, pas d'effets de bord cachés.

### Conventions C#

- [Style guide C# Godot](https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/c_sharp_style_guide.html) : PascalCase (types, méthodes, propriétés), camelCase (locales, paramètres), `_camelCase` (champs privés).
- Composition plutôt qu'héritage.
- Pas de singleton monolithique ; Autoloads réservés aux services globaux légitimes.
- Typage explicite : pas de `var` quand le type n'est pas évident.
- Pas de code mort ni de code commenté. Commentaires = le pourquoi, pas le quoi.
- Pas d'API Godot dépréciée (le build doit rester à 0 warning).

### Conventions Godot

- Scenes et scripts organisés par feature/système.
- Nodes nommés selon leur responsabilité.
- Chaque `.cs` a son `.cs.uid` versionné : le committer avec le `.cs`, le supprimer avec lui.
- Ne jamais éditer `.godot/` ni les `.import` à la main.
- Shaders `canvas_item` : pas de `return` dans `fragment()`.
- Toute action d'input utilisée dans le code est déclarée dans `[input]` de `project.godot`.

### Patterns attendus

Object Pooling (projectiles, créatures, particules) · State Machine (phases de run, comportements) · Observer/EventBus · Factory (entités depuis JSON) · Command (actions réversibles).

## Règles de travail

- **Ne jamais casser le core loop** : explorer → combattre → monter en puissance → fuir l'Effacement.
- **Itérer vite** : placeholders OK, code sale non.
- **Un système à la fois** : finir et valider un lot avant le suivant.
- **Tester le fun tôt** : si ce n'est pas fun en placeholder, ça ne le sera pas en production.
- **Checklist roadmap obligatoire** : cocher (`- [x]`) dans la roadmap V2 (`doc/VESTIGES-STRATEGIE-V2.md` §25) chaque item validé.
- Docs et commentaires en français, identifiants de code en anglais.
