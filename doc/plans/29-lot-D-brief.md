# Brief de reprise — plan 29, lot D : créatures de la horde sans nœuds

À donner tel quel à l'agent qui reprend le chantier. Écrit le 9 octobre 2026 à la fin du lot C.

**Reporté le 9 octobre (DECISIONS §83) : Raphaël garde les nœuds pour l'instant.** Ce brief reste prêt si le besoin revient (entrée de gamme, foule plus grande).

---

Tu reprends le chantier **performance des foules** de Vestiges (plan 29). Les coûts aberrants sont retirés ; il reste à faire des créatures de la horde des **données** plutôt que des nœuds Godot. Raphaël l'a décidé le 9 octobre (DECISIONS §83) : « je pense que on va passer en noeudless ».

## À lire avant tout

1. `CLAUDE.md` et `AGENTS.md` : règles du projet, reprise de session, pièges connus (dont les rappels C# par nœud et le profileur .NET).
2. `doc/plans/29-performance-des-foules.md` : tout le diagnostic, les lots A, B, C, les mesures et leurs réserves.
3. `doc/plans/DECISIONS.md` §83 : les mots de Raphaël sur ce chantier.
4. `doc/EXPORT.md` : la mesure en export n'est pas encore en place.

## Où on en est

- Plafond de créatures actives : 500 (`data/scaling/peril.json`). Raphaël veut **se calquer sur Vampire Survivors** (≈ 300 avant que l'apparition normale s'arrête, 500 au plus) et **ne pas aller au-delà pour l'instant**. L'objectif du lot D n'est donc pas d'afficher plus de créatures, mais de tenir 500 avec de la marge **sur l'entrée de gamme**.
- Règle de budget : **≤ 8 ms par image sur la machine de Raphaël** (Ryzen 7 5700X, un seul cœur utile) pour viser 60 FPS sur un processeur deux fois plus lent. Aujourd'hui, au banc dense : 400 créatures ≈ 6 ms, 1 000 ≈ 11 ms ; en vraie run, 1 200 créatures ≈ 46 FPS (≈ 22 ms).
- Architecture actuelle des créatures (`scripts/Combat/Enemy.cs`, ≈ 1 800 lignes) : un `Node2D` par créature avec ≈ 8 enfants (sprite animé avec un matériau de shader propre, polygone de secours, marques de statut, aura, plaque…). Déjà sorti du nœud : la boucle (`TickRoster`), la physique (`ObstacleField`, touches par `CrowdIndex`), la séparation (`CrowdSeparation`), l'ombre (`GroundShadowLayer` + `ShadowCaster`). Reste par créature : environ 5 µs de logique par tick (séparation 2, déplacement 1, statuts 0,6) et ≈ 4 µs de rendu par image, surtout du coût par nœud et par lecture de propriété à travers l'interop.

## Le but du lot D

La **piétaille** (créatures sans capacité spéciale, l'essentiel d'une horde) devient une entrée de tableaux C# (structure de tableaux : position, vitesse, PV, statuts, image d'animation, génération) avancée par une seule boucle, et dessinée directement par le `RenderingServer` : un élément de dessin (`CanvasItem` RID) par créature, enfant du conteneur trié en Y pour rester trié avec le joueur et les décors. **Élites, mini-boss, boss et créatures à capacités restent des `Enemy` complets.** Cible : moins d'1 µs de logique et de l'ordre d'1 µs de rendu par créature.

Il n'y a pas de changement visible voulu : même rendu (contour, flash des coups, teintes et marques de statut, animations 8 directions, mort, ombre), même comportement. Toute différence visible passe par une planche validée par Raphaël.

## Points durs, à trancher dans le découpage

- **Façade de cible** : armes, objets, perks, événements (≈ 50 fichiers) manipulent `Enemy`. Introduire une cible commune (interface ou poignée `{index, génération}` pour la horde) que `CrowdIndex` rend, sans copier deux fois les règles de dégâts, statuts et récompenses. Une poignée doit devenir invalide quand la créature meurt et que l'entrée est réutilisée (même idée que `EnemyLife` / `TargetLock`).
- **Shader par créature** : aujourd'hui un `ShaderMaterial` par créature porte flash, statuts et contour. Les shaders `canvas_item` n'ont pas d'uniformes par instance ; piste : un matériau partagé par type, et les paramètres variables passés par la couleur de sommet (`COLOR` lu dans `vertex()`, voir le piège de `entity.gdshader` dans `CLAUDE.md`). Prouver sur planche que le rendu est identique.
- **Animation** : `AnimatedSprite2D` fait le défilement des images ; il faudra le refaire (index d'image, cadence, sens, mort qui ne boucle pas).
- **Tri en Y** : vérifier par capture que des éléments créés par `RenderingServer` sous `EnemyContainer` se trient avec le joueur et les décors exactement comme les nœuds.
- **Tests** : de nombreux tests instancient `Enemy.tscn` et lisent ses champs par réflexion (voir le piège « Scènes de banc » de `CLAUDE.md`). Garder `Enemy` pour eux, ajouter des contrôles dédiés à la horde.

## Découpage proposé (à reprendre dans le plan 29 avant de coder)

- **D0** — Inventaire : quels types de créatures sont de la piétaille (sans capacité) ; quelle part de la horde ils font en run ; qui appelle quoi sur `Enemy`. Mesure de référence au banc et en run réelle (voir plus bas).
- **D1** — Façade de cible commune, sans changement de comportement (`Enemy` seule l'implémente). 32/32 suites.
- **D2** — Simulation de la horde en tableaux (déplacement, séparation, contact, statuts, dégâts, mort, récompenses), sans rendu ; contrôles dédiés.
- **D3** — Rendu par `RenderingServer` (sprite, animation, ombre, contour, flash, statuts) ; planche avant/après à faire valider par Raphaël.
- **D4** — Bascule des apparitions de piétaille vers la horde ; planche en run, mesures avant/après.
- **D5** — Budget entrée de gamme : 500 créatures en vraie run ≤ 8 ms par image ; à-coups d'apparition (le pool d'`Enemy` n'est préchauffé qu'à 20, voir le plan 29).

## Mesurer (méthode qui a marché)

- Toujours hors du quota CPU de Zed : `systemd-run --user --scope --quiet --collect <commande>`. Vérifier `uptime` et `ps -eo pcpu,comm --sort=-pcpu | head` : Raphaël fait tourner jeux, VM et compilations ; sous charge, ne pas conclure sur des FPS.
- Banc dense : `tools/tests/MovementDenseBenchmark` (terrain dégagé par défaut, `--arena origin` pour une foule contre un immeuble, `--churn` pour des morts en continu, `--weapons`, `--ascend`). Le JSON contient `frame_split_ms` (scripts physiques, pas du moteur, `_Process`, rendu, ticks par image). A/B : `tools/bench_ab.sh` ou une base dans un worktree, passes alternées.
- Run réelle : `CAPTURE_EXTRA_ARGS="--frame-stats --nomad" tools/capture_run.sh <dossier> 180 60` (FPS, p99 et pire image par tranche de 30 s, fichier `frames-<seed>.csv`). `--scaling active_enemies_ceiling=N,max_enemies_on_screen=N,…` force une horde pour la mesure, sans toucher aux données.
- Attribuer un coût : chronométrer (`Stopwatch`) ou retirer par expérience ; **ne pas croire le profileur .NET** sur le code natif. Le banc tourne en assembly Debug (JIT non optimisé, ≈ +25 % sur le code C#) : pour une mesure de code C#, `dotnet build -p:Optimize=true --no-incremental`.
- Validation : `systemd-run --user --scope --quiet --collect tools/validate.sh <dossier neuf>` (32 suites, ≈ 25 min) ; ne pas modifier les sources pendant qu'elle tourne.

## Règles de travail

Découpage écrit dans le plan avant de coder, un lot à la fois ; planche avant tout changement visible ; relecture par le sous-agent `godot-reviewer` avant chaque commit de code ; compte rendu dans le plan, retours de Raphaël dans `DECISIONS.md`, tableau de bord à jour ; commits en français (`perf:`, `feat:`…) sans trailer `Co-Authored-By`.
