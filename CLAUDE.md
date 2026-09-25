@AGENTS.md

# Claude Code sur Vestiges

`AGENTS.md` (importé ci-dessus) est la source des règles projet, partagée avec les autres agents. Ce fichier ne contient que la façon de travailler propre à Claude Code et les pièges déjà rencontrés. Le garder court : une règle qui ne sert qu'à un type de fichier va dans `.claude/rules/`, une procédure dans un skill.

## Reprise de session

1. `git status` : des fichiers modifiés par Raphaël pendant ses tests sont **volontaires**, ne pas les annuler.
2. Lire `doc/plans/README.md` (état du dossier, mises à jour datées) et `doc/plans/DECISIONS.md` (ce qui est acquis).
3. `git log --oneline -15` pour le dernier lot livré.

Les plans 13 (butin) et 14 (anomalies) ne sont pas arbitrés : ne pas les implémenter sans accord explicite.

## Boucle de travail

- **Un lot à la fois.** Découpage proposé dans le plan concerné avant de coder, puis implémentation, vérification, documentation, commit.
- **Vérifier par le bon moyen :**
  - compile : hook de build automatique en fin de tour (0 warning exigé) ;
  - démarrage : `/smoke-test` ;
  - comportement : `tools/test_movement.sh`, `tools/test_enemy_abilities.sh` ;
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
- **Primitives SDF orientées** (`tools/sprites/sdf.py`) : `local = (p − centre) @ rotation`, donc un point local se place en monde par `rotation @ local`.
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
