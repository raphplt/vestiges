@AGENTS.md

# Claude Code sur Vestiges

`AGENTS.md` (importé ci-dessus) est la source des règles projet, partagée avec les autres agents. Ce fichier ne contient que la façon de travailler propre à Claude Code et les pièges déjà rencontrés. Le garder court : une règle qui ne sert qu'à un type de fichier va dans `.claude/rules/`, une procédure dans un skill.

## Reprise de session

1. État de l'arbre et derniers commits : déjà dans le contexte d'ouverture (mod `vestiges-reprise`), sinon `git status` et `git log --oneline -15`. Des fichiers modifiés par Raphaël pendant ses tests sont **volontaires**, ne pas les annuler.
2. Lire `doc/LE-JEU.md` (la forme du jeu, verrouillée, et l'ordre de travail), puis `doc/plans/README.md` (mises à jour datées) et `doc/plans/DECISIONS.md` (ce qui est acquis).
3. Le dernier lot livré : les derniers commits ci-dessus.
4. **Aucun point en suspens** (DECISIONS §190, remplace la règle du §48) : le §2 de `doc/plans/TABLEAU-DE-BORD.md` reste vide. Toute question nouvelle reçoit aussitôt une décision par défaut de l'agent, franche, consignée dans `DECISIONS.md` et réversible par Raphaël, ou une place dans l'ordre de `doc/LE-JEU.md`. Ne poser à Raphaël (avec `AskUserQuestion`, recommandation en premier) que ce qui change la forme du jeu ou ce qu'il doit jouer.

Les plans 13 (butin) et 14 (anomalies) ne sont pas arbitrés : ne pas les implémenter sans accord explicite.

## Boucle de travail

- **Un lot à la fois**, dans l'ordre de `doc/LE-JEU.md`. Découpage proposé dans le plan concerné avant de coder, puis implémentation, vérification, documentation, commit.
- **Estimer en effort** (faible, moyen, élevé), jamais en durée (DECISIONS §190).
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
| `regard` | Sous-agent (Sonnet) qui ouvre les images d'une capture, d'une planche ou d'un générateur et rend un verdict par image ; au-delà de deux ou trois images, pour les garder hors du contexte principal |
| `greffier` | Sous-agent (Sonnet) qui rédige la doc de clôture (plan, registre, README des plans, tableau de bord, roadmap) à partir des faits qu'on lui donne et propose le message de commit ; appelé par `/close-lot`, ne committe pas |
| Hooks | JSON de `data/` et manifestes de décors validés à l'écriture ; `dotnet build` en fin de tour si du C# a changé |
| Mods (`.claude/mods/`, marché `vestiges-mods` déclaré et activé dans `.claude/settings.json`) | Lus dans le dépôt : après une modification, `/reload-plugins` ; vérifier par `claude plugin validate .claude/mods/<mod>` et `claude plugin test .claude/mods/<mod>` |
| Mod `vestiges-garde-fous` | Refuse en expliquant : `pkill -f motif` qui tuerait son propre shell (écrire `'[m]otif'`), `git add -A`/`.`, `git commit -a`, `stash`, `reset --hard`, `checkout .`, `clean -f` ; bancs de FPS au-delà d'un quart des cœurs de charge ; tâches lourdes sous 3 Gio disponibles ; `export_lowpoly.py --jobs N` au-delà de 5 Gio par ouvrier ; `git commit` avec un trailer `Co-Authored-By`, un `.cs` neuf sans `.cs.uid`, un PNG neuf d'`assets/` sans `.import`, un `.cs.uid` orphelin ou un fichier de `.godot/`. `GARDE_FOUS=off` en tête de commande lève la charge et la mémoire, sur décision de Raphaël |
| Mod `vestiges-reprise` | À l'ouverture, après `/clear` ou un compactage : branche, `git status`, 12 derniers commits et dernière mise à jour de `doc/plans/README.md` dans le contexte |
| Mod `vestiges-captures` | Bloque une fois la fin d'un tour qui a produit des PNG (captures, planches, générateurs) sans en ouvrir aucun avec Read |
| Mod `vestiges-moniteur` | Ligne d'état : charge, mémoire disponible, tâches longues en cours ; alerte et prévient Claude quand earlyoom ou le noyau tue un processus ; `/moniteur` pour le détail |
| Mod `vestiges-routage` | Classe chaque demande (perf, C#, Python, JSON, docs, question) et règle l'effort du tour ; le modèle ne change qu'au premier tour de session, ensuite seulement vers le haut (cache de prompt) ; table dans `hooks/routes.ts` ; `/route` : état et cache, `auto`, `off`, `<catégorie>`, `effort <niveau>`, `modele <alias>` |
| `.claude/rules/` | Règles chargées selon les fichiers touchés : C#, JSON, pixel art, monde et décors |
| Plugin `csharp-lsp` | Diagnostics et navigation C# après chaque édition ; serveur `csharp-ls` (dotnet tool global), lancé par `~/.local/bin/csharp-ls` qui fixe `DOTNET_ROOT` |
| MCP `godot` (`.mcp.json` → `tools/mcp/godot.sh`) | Lancer le jeu fenêtré et lire sa sortie de débogage en direct ; ne remplace ni `/capture` ni les régressions. Ne pas appeler `update_project_uids` (uid référencés) |
| MCP `wick` (`.mcp.json`, addon `addons/wick/`) | Pont éditeur (port 6505) : arbre de scènes et propriétés de l'éditeur ouvert ; groupe `runtime` : lance le jeu headless et remonte les exceptions C# avec leur source. Serveur compilé hors dépôt dans `~/.local/share/wick` (MessagePack monté en 3.1.9), lancé par `tools/mcp/wick.sh`. Les lanceurs de `tools/mcp/` trouvent Godot, dotnet et node seuls (Linux et macOS, sans le PATH du shell), pour Claude Desktop comme pour le terminal. Le pont runtime (autoload `mcp_runtime_bridge.gd`, port 7777 sans authentification) n'est pas enregistré : l'ajouter le temps d'une session, jamais dans un commit |
