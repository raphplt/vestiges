# VESTIGES

Roguelike de survie et construction en vue isométrique 2D, dans un monde en train d'être oublié.
Moteur Godot 4.6, langage C#, renderer GL Compatibility.

## Documentation

Toute la vision, les mécaniques et l'architecture sont documentées dans `doc/` :

- [VESTIGES-GDD.md](doc/VESTIGES-GDD.md) — Game Design Document (vision, core loop, systèmes de gameplay, score, personnages, bestiaire, UI/UX)
- [VESTIGES-BIBLE.md](doc/VESTIGES-BIBLE.md) — Bible Artistique & Narrative (lore, cosmologie, direction artistique, audio, 6 constellations)
- [VESTIGES-ARCHITECTURE.md](doc/VESTIGES-ARCHITECTURE.md) — Architecture Technique (couches, systèmes, data flow, principes)
- [VESTIGES-ROADMAP.md](doc/VESTIGES-ROADMAP.md) — Roadmap & Milestones (phases 0-5+, lots, critères de validation)

Toujours consulter ces docs avant de proposer une feature ou un changement architectural. Le GDD fait autorité sur le gameplay, l'Architecture sur le code.

## Stack technique

| Composant | Choix |
|-----------|-------|
| Moteur | Godot 4.6 |
| Langage | C# (.NET) |
| Physique | Jolt Physics |
| Renderer | GL Compatibility (OpenGL) |
| Vue | Isométrique 2D |
| Données | JSON (data-driven : ennemis, perks, recettes, loot tables, biomes, scaling) |

## Exigences de qualité

Ce projet vise un niveau professionnel. Le code doit être propre, optimisé et performant, sans compromis.

### Principes fondamentaux

- **Performance first** : cible 60 FPS constant sur hardware mid-range. Profiler avant d'optimiser, mais ne jamais ignorer la perf.
- **Data-driven** : tout le contenu de gameplay (stats, recettes, courbes de scaling, loot tables) vit dans des fichiers JSON, jamais hardcodé.
- **Modularité** : chaque système (combat, craft, construction, spawn, progression, scoring) est un module indépendant avec des responsabilités claires.
- **Découplage** : communication entre systèmes via signaux/events, pas de références directes croisées. Préparer l'architecture pour le coop v2.
- **État explicite** : pas de state implicite ou d'effets de bord cachés. Le state de la run et le state méta sont séparés.

### Conventions C#

- Suivre les [conventions de nommage C# de Godot](https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/c_sharp_style_guide.html) : PascalCase pour les méthodes, propriétés et classes ; camelCase pour les variables locales et paramètres ; _camelCase pour les champs privés.
- Préférer la composition à l'héritage.
- Pas de singleton monolithique. Utiliser les Autoloads Godot uniquement pour les services globaux légitimes (GameManager, EventBus).
- Typage explicite : pas de `var` quand le type n'est pas évident à la lecture.
- Pas de code mort, pas de commentaires de code commenté. Si c'est supprimé, c'est supprimé.
- Les commentaires expliquent le pourquoi, pas le quoi. Le code doit être lisible sans commentaires.

### Conventions Godot

- Organiser les scenes et scripts par feature/système, pas par type de fichier.
- Nommer les nodes clairement : le nom reflète la responsabilité.
- Utiliser les signaux Godot pour la communication parent-enfant et l'EventBus (Autoload) pour la communication inter-systèmes.
- Les ressources importées (sprites, audio) dans des dossiers dédiés par feature.

### Patterns attendus

- **Object Pooling** pour les entités fréquentes (projectiles, créatures, particules).
- **State Machine** pour les états de jeu (jour/crépuscule/nuit/aube) et les comportements d'entités.
- **Observer/Event Bus** pour le découplage inter-systèmes.
- **Factory** pour la création d'entités depuis les données JSON.
- **Command** pour les actions joueur réversibles (placement de structures).

## Structure du projet

```
vestiges/
├── doc/                    # Documentation de design et technique
├── scenes/                 # Scenes Godot organisées par feature
├── scripts/                # Code C# organisé par système
│   ├── Core/               # GameManager, EventBus, state global
│   ├── Combat/             # Auto-attaque, dégâts, statuts
│   ├── Progression/        # XP, level-up, perks
│   ├── World/              # Jour/nuit, génération procédurale, biomes
│   ├── Spawn/              # Spawn de créatures, object pooling
│   ├── Base/               # Construction, craft, inventaire
│   ├── Meta/               # Vestiges, Souvenirs, persistence cross-run
│   ├── Score/              # Calcul et agrégation du score
│   ├── Events/             # Événements aléatoires
│   ├── UI/                 # HUD, menus, feedback
│   └── Infrastructure/     # Chargement JSON, sauvegarde, audio, debug
├── data/                   # Fichiers JSON de gameplay
│   ├── enemies/
│   ├── perks/
│   ├── recipes/
│   ├── loot_tables/
│   ├── biomes/
│   └── scaling/
├── assets/                 # Sprites, audio, fonts
└── project.godot
```

## Règles de travail

- **Ne jamais casser le core loop.** Chaque changement doit servir la boucle gameplay ou préparer un système futur documenté dans la roadmap.
- **Itérer vite, ne pas chercher la perfection.** Placeholders OK, code sale non. Un prototype propre vaut mieux qu'une architecture parfaite qui ne tourne pas.
- **Un système à la fois.** Finir et valider un lot avant de passer au suivant.
- **Tester le fun tôt.** Si ce n'est pas fun en placeholder, ça ne le sera pas en production.
- **Checklist roadmap obligatoire.** Dans `doc/VESTIGES-ROADMAP.md`, cocher chaque case (`- [x]`) dès qu'un lot, un critère ou un rappel est validé.
