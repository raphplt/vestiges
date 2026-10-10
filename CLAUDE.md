@AGENTS.md

# Claude Code sur Vestiges

`AGENTS.md` (importé ci-dessus) est la source des règles projet, partagée avec les autres agents. Ce fichier ne contient que la façon de travailler propre à Claude Code et les pièges déjà rencontrés. Le garder court : une règle qui ne sert qu'à un type de fichier va dans `.claude/rules/`, une procédure dans un skill.

## Reprise de session

1. `git status` : des fichiers modifiés par Raphaël pendant ses tests sont **volontaires**, ne pas les annuler.
2. Lire `doc/plans/README.md` (état du dossier, mises à jour datées) et `doc/plans/DECISIONS.md` (ce qui est acquis).
3. `git log --oneline -15` pour le dernier lot livré.
4. **Avant tout travail, poser à Raphaël toutes les questions qui l'attendent** (demande du 1er octobre 2026, DECISIONS §48) : reprendre chaque ligne de `doc/plans/TABLEAU-DE-BORD.md` §2, et les points ouverts du plan concerné, en question concrète avec options et recommandation ; les poser avec `AskUserQuestion` (quatre par appel, autant d'appels qu'il faut). Consigner ses réponses dans `DECISIONS.md` et mettre à jour le tableau de bord, puis seulement choisir un chantier. Une question qu'il reporte reste au §2.

Les plans 13 (butin) et 14 (anomalies) ne sont pas arbitrés : ne pas les implémenter sans accord explicite.

## Boucle de travail

- **Un lot à la fois.** Découpage proposé dans le plan concerné avant de coder, puis implémentation, vérification, documentation, commit.
- **Vérifier par le bon moyen :**
  - compile : hook de build automatique en fin de tour (0 warning exigé) ;
  - démarrage : `/smoke-test` ;
  - comportement : `tools/test_movement.sh`, `tools/test_enemy_abilities.sh`, `tools/test_weapons.sh` ;
  - mesure sans image (densité, coffres vus) : `tools/measure_run.sh`, headless en temps accéléré ;
  - rendu en jeu : `/capture`, puis **ouvrir les PNG avec Read et les regarder** avant de conclure ;
  - coût : `/bench`, avant/après au même commit de base.
- **Mesurer avant de corriger** une régression : trouver le commit en cause (`git log -p -- <fichiers>`), chiffrer, montrer avant/après.
- **Clore un lot avec `/close-lot`** : plan à jour, retours dans `DECISIONS.md`, cases de la roadmap V2 §25 cochées seulement si implémenté et vérifié, commit en français sans `Co-Authored-By`.
- Avant un commit touchant `scripts/`, `scenes/` ou `data/` d'une certaine ampleur : sous-agent `godot-reviewer` en arrière-plan.

## Pièges connus

- **Charge machine** : Raphaël compile souvent d'autres projets en parallèle. Vérifier `uptime` avant un banc de FPS ; sous charge, ne rien conclure sur les FPS et s'appuyer sur des métriques insensibles à la charge (nœuds créés/s, allocations).
- **Grille iso « stacked »** : le TileSet n'est pas en losange. Une cellule x+1 décale de 64 px à l'horizontale, un rang de 16 px en quinconce ; les rues sont horizontales ou verticales à l'écran.
- **Tri en Y de `Main`** : tout Node2D ajouté à la racine se trie avec les entités. Un effet au sol prend `ZIndex = -1`, un texte à lire `ZIndex` ≥ 20 (couches dans `AGENTS.md`).
- **Décors** : ~10 000 `EnvironmentProp` par carte. Aucune boucle par frame sur tous les décors : passer par un index spatial (voir `PropOcclusion`).
- **Sprites générés** : ne jamais supprimer un `.import` existant (son uid est référencé) ; Godot réimporte seul un PNG modifié. Les nouveaux PNG reçoivent leur `.import` au prochain `tools/smoke_test.sh` : les committer ensemble.
- **Shaders `canvas_item` et Modulate** : dans `fragment()`, `COLOR` vaut déjà texture × Modulate. Multiplier une texture lue par `COLOR` l'élève au carré ; l'ignorer perd Modulate. Capter `COLOR` dans `vertex()` (Modulate seul) par un `varying` (voir `entity.gdshader`).
- **Primitives SDF orientées** (`tools/sprites/sdf.py`) : `local = (p − centre) @ rotation`, donc un point local se place en monde par `rotation @ local`.
- **Rappels C# par nœud** : Godot appelle le pont C# pour chaque nœud traité (`_Process`, `_PhysicsProcess`), ≈ 3 à 9 µs par appel, **même si la classe n'a pas la méthode** (`SetProcess(true)` sans `_Process`). Une entité nombreuse (créature, projectile) passe par un `TickRoster` (plan 29), jamais par son propre rappel.
- **Profileur .NET** (`dotnet-trace`) : il impute au code managé le temps natif qui précède l'échantillon suivant. Pour attribuer un coût, chronométrer (`Stopwatch`) ou retirer par expérience ; le champ `frame_split_ms` du banc dense découpe l'image.
- **Shader qui apparaît en cours de run** : l'ajouter à `ShaderWarmup` (chargement). Sinon sa compilation, synchrone en Compatibility sur macOS, peut figer le rendu plusieurs secondes à son apparition, sans message d'erreur (marée de l'Indicible, plan 07 B5c).
- **Scènes de banc** (`tools/tests/`) : elles accèdent parfois à des champs privés par réflexion ; renommer un champ privé peut casser un banc sans erreur de compilation.

## Outillage Claude du projet

| Outil | Usage |
|---|---|
| `/smoke-test` | Build + import + boot headless du Hub |
| `/capture` | Captures en vraie run (événement, carte, décors, bestiaire), à regarder |
| `/bench` | Banc de combat dense avec contrôle de charge et comparaison A/B |
| `/close-lot` | Clôture d'un lot : vérifications, plans, roadmap, commit |
| `/roadmap-sync` | Réaligner la roadmap V2 sur le code |
| `godot-reviewer` | Sous-agent de relecture d'un diff C#/Godot |
| Hooks | JSON de `data/` et manifestes de décors validés à l'écriture ; `dotnet build` en fin de tour si du C# a changé |
| `.claude/rules/` | Règles chargées selon les fichiers touchés : C#, JSON, pixel art, monde et décors |
