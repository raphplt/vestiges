# Plan 29 — Performance des foules

9 octobre 2026. Chantier ouvert par Raphaël : « essaie de comprendre ce qui fait qu'on est si limité sur la taille de la foule […] alors que tous les autres jeux (megabonk, vampire survivor) le gèrent très bien. Le but sera de comprendre les failles de notre approche et les corriger. » Origine : plafond de créatures ramené à 200 au plan 28 (240 → 54 FPS au banc du 8 octobre).

## 1. Ce que les mesures disent

Banc de combat dense (`tools/benchmark_movement.sh`, scène `MovementDenseBenchmark`), 1080p, Ryzen 7 5700X, RX 6950 XT, Godot 4.7.2 (exécutable éditeur), une passe par effectif. Sondes temporaires non committées : découpage de l'image (scripts physiques, pas du moteur, scripts `_Process`, rendu) et chronométrage de `Enemy._PhysicsProcess` et de `MoveAndSlide`.

### 1.1 Le banc mesurait une créature coincée dans un immeuble

Bissection sur les 350 commits depuis le 26 septembre, 240 créatures : 122 FPS jusqu'à `bca160cd`, 49 FPS à partir de `1f5106d7` (« six petits lieux, carte agrandie en hauteur », 30 septembre). Ce commit ne ralentit pas le jeu : il change la carte, et le point (0, 0) où le banc pose le joueur tombe désormais **dans un immeuble**. Les 240 créatures passent leur temps à se dégager du mur, et `MoveAndSlide` passe de 2 à plus de 20 µs par créature et par tick.

La mesure du 8 octobre qui a fixé le plafond à 200 (240 → 54 FPS) a donc été prise dans ce cas pathologique.

### 1.2 L'environnement n'explique pas les FPS

Claude Code tourne dans le terminal de Zed, lancé avec `CPUQuota=200%` (2 cœurs au total, bridé 917 s pendant la session). Vérifié en lançant Godot hors de ce quota (`systemd-run --user --scope`) : mêmes FPS (240 → 59 au point d'origine, contre 57). Pendant le banc, Godot consomme **1,05 cœur** : thread principal à 98 %, le reste négligeable. Le quota ne limite donc pas le jeu ; il peut en revanche fausser une mesure si une compilation tourne en parallèle dans le même scope. Les mesures de ce plan sont prises hors quota.

L'assembly C# compilée avec optimisations (`-p:Optimize=true`) ne change rien. Reste non vérifié : l'écart entre l'exécutable éditeur et un export Release (pas de modèles d'export installés).

### 1.3 Ce que coûte une créature aujourd'hui

Banc corrigé (terrain dégagé), créatures en mode `Floating` :

| Créatures | FPS | p99 | Ticks physiques par image |
|---|---|---|---|
| 120 | 187 | 9,2 ms | 0,31 |
| 240 | 92 | 18,8 ms | 0,72 |
| 320 | 63 | 27,6 ms | 0,90 |
| 400 | 39 | 39,7 ms | 1,46 |

Découpage à 400 créatures, par tick physique (≈ 60 par seconde) ou par image :

| Poste | Coût | Par créature |
|---|---|---|
| Appel du `_PhysicsProcess` C# lui-même (pont Godot → C#) | 3,6 ms / tick | **9 µs** |
| Logique de la créature (hors `MoveAndSlide`) | 1,5 ms / tick | 3,7 µs |
| `MoveAndSlide`, terrain dégagé | 1,7 ms / tick | 4,3 µs (15 à 25 µs contre un mur) |
| Pas du moteur physique (corps qui bougent, broadphase) | 3,3 ms / tick | 8 µs |
| Scripts `_Process` | 5,8 ms / image | ≈ 9 µs au-delà de 120 |
| Rendu | 3,2 ms / image | un appel de dessin par créature |

Sonde minimale, 400 nœuds avec un `_PhysicsProcess` vide : nœud sans script 0,01 µs, nœud C# 2,9 µs, `CharacterBody2D` C# 3,8 µs, `Enemy` 9 µs. Retirer ce rappel des 400 créatures gelées fait tomber les scripts physiques de 3,6 à 0,1 ms par tick.

## 2. Les failles de notre approche

Vampire Survivors et Megabonk traitent une créature comme une donnée : un tableau de positions, une boucle, une grille pour les voisins et les coups, un rendu groupé. Chez nous, une créature est un petit arbre de nœuds Godot (≈ 9), avec un corps physique et un script appelé par le moteur. Chaque créature paie donc des coûts fixes qu'une boucle sur un tableau n'aurait pas.

1. **Un rappel moteur par créature.** Godot appelle 400 fois par tick un `_PhysicsProcess` C# ; le seul passage du pont coûte 9 µs, plus que la logique de la créature.
2. **Un corps physique piloté par créature.** `MoveAndSlide` interroge la physique à chaque tick, même en terrain dégagé ; contre un mur, son coût est multiplié par 4 à 6. Le moteur met ensuite à jour sa broadphase pour chaque corps déplacé.
3. **Aucune séparation entre créatures.** Elles visent toutes le centre du joueur et s'empilent. La pile fait grimper les requêtes physiques (chaque corps trouve tous les autres dans sa zone), et l'orientation des sprites bascule d'un tick à l'autre au contact.
4. **La spirale des ticks physiques.** Sous 60 FPS, Godot enchaîne plusieurs ticks physiques par image (1,46 à 400) : tout ce qui est payé par tick est multiplié. D'où la courbe plus que linéaire.
5. **Un matériau de shader par créature.** Le rendu ne regroupe pas les créatures : au moins un appel de dessin chacune.
6. **Tout sur un thread.** Le jeu n'utilise qu'un cœur sur seize.

## 3. Lots

### F1 — Coûts fixes sans changement visible (livré le 9 octobre)

- Banc : combat centré sur le premier terrain sans décor bloquant dans 420 px (`--arena origin` garde l'ancien point), position notée dans le JSON.
- Créatures en `MotionMode` `Floating`, le mode prévu pour une vue de dessus (240 adossées à un mur : 57 → 78 FPS).
- Une seule boucle C# appelle toutes les créatures actives (`SetTicking`), au lieu d'un `_PhysicsProcess` par créature appelé par Godot. Gain attendu : ≈ 9 µs par créature et par tick.
- État du sprite lu à chaque tick (`SpeedScale`, `SpriteFrames`) gardé côté C#.
- Mesure avant/après hors quota, puis plafond `active_enemies_ceiling` refixé au banc corrigé.

### Objectif relevé après F1 (§83)

Raphaël : le jeu doit tenir **des centaines, potentiellement des milliers** de créatures, comme les autres bullet heavens. Corriger le banc et grappiller 50 % ne règle pas le problème ; le plafond reste à 200 tant que l'architecture n'a pas changé.

Mesure de départ après F1 : 600 créatures → 22 FPS, 1 000 → 4 FPS (8 ticks physiques par image, le maximum de Godot). À 1 000, par tick : 14,7 ms de scripts physiques et 11,5 ms de pas du moteur ; par image : 19 ms de scripts `_Process`, 6 ms de rendu seulement. **Ce n'est pas l'affichage qui coince, c'est la simulation** : corps physiques et parcours de listes.

Cible : 1 000 créatures à 60 FPS au banc dense (16,7 ms par image), puis 2 000. Chemin : la créature devient une donnée indexée, sort du moteur physique, puis s'allège en nœuds.

### A — Index de la foule (livré le 9 octobre)

Registre C# des créatures ciblables, positions relevées une fois par tick, grille de hachage. Il remplace les 30 parcours de `GroupCache.GetEnemies()` (ciblage de chaque arme, tête chercheuse de chaque projectile, objets, auras, herbe foulée, bonus de meute, apparitions, musique), qui reconstruisent un tableau Godot de toutes les créatures et le relisent élément par élément à travers l'interop. Coût actuel : armes × créatures, projectiles × créatures, créatures × créatures pour la meute. Invisible.

### F2 — Séparation et arrêt au contact (livré et gardé le 9 octobre, §83)

Sur la grille de A : chaque créature est poussée hors de ses voisines et s'arrête au contact du joueur au lieu de viser son centre. La horde forme un front au lieu d'une pile. Prototype : `MoveAndSlide` contre un mur ÷ 2,5. Captures avant/après à montrer avant de garder.

### B — Créatures hors du moteur physique (livré le 9 octobre, coût à mesurer)

Collision avec les décors par une grille statique construite à la génération (les décors ne bougent pas) ; touches des projectiles du joueur par l'index de A au lieu des `Area2D` ; plus de `CharacterBody2D` ni de `MoveAndSlide` par créature. Supprime le pas du moteur lié aux créatures (11,5 ms par tick à 1 000).

### C — Créature légère

Trouver les ≈ 19 µs par créature et par image de `_Process` ; réduire les ≈ 9 nœuds par créature (ombre, marques, plaques créées à la demande) ; matériau partagé pour regrouper le rendu.

### D — Si C ne suffit pas pour 2 000

Créatures de la horde sans nœud : dessin direct par le `RenderingServer` dans le tri en Y, la créature n'étant plus qu'une entrée de tableau. Les créatures à capacités (élites, mini-boss, boss) restent des nœuds.

## 4. Comptes rendus

### F1 livré — 9 octobre 2026

- **Banc** : `MovementDenseBenchmark` centre le combat sur le premier point, en spirale depuis l'origine, sans décor bloquant dans 420 px (`arena` dans le JSON ; `--arena origin` pour l'ancien point). Nouveau champ `frame_split_ms` (`tools/tests/FrameSplit.cs`) : scripts physiques, pas du moteur, scripts `_Process`, rendu, ticks physiques par image.
- **`EnemyTicker`** : une seule boucle C# appelle `Enemy.PhysicsTick` pour chaque créature active. `Enemy.SetTicking` remplace `SetPhysicsProcess` ; Godot n'appelle plus rien par créature. Écart de contrat : le `ProcessMode` d'une créature ou de son conteneur n'est plus lu (la pause de l'arbre l'est toujours) ; pour figer une créature, `SetTicking(false)`. Une exception dans une créature n'arrête pas les suivantes. Tests migrés (`SetTicking`, `PhysicsTick`), y compris deux appels réflexifs à `_PhysicsProcess` dans `ObjectsRegression.Statuses` qui, sinon, auraient tourné à vide.
- **Créatures en `Floating`** (`Enemy.tscn`), **état du sprite gardé en C#** (`SpeedScale`, `SpriteFrames`).

**Mesures**, hors quota, 1080p, même banc des deux côtés (base `067844ba` avec le banc corrigé), médiane de deux passes alternées :

| Créatures | FPS base → F1 | p99 base → F1 | Scripts physiques (ms par image) | Ticks par image |
|---|---|---|---|---|
| 240 | 101 → 113 | 15,6 → 14,1 ms | 2,4 → 1,2 | 0,59 → 0,54 |
| 320 | 60 → 77 | 37,1 → 22,4 ms | 5,7 → 2,8 | 0,99 → 0,83 |
| 400 | 37 → 57 | 84,0 → 29,5 ms | 10,7 → 3,9 | 1,49 → 1,00 |

**Vérifications** : build sans avertissement ; `tools/validate.sh` 32/32 suites, sources inchangées ; run réelle de 75 s (`capture_run.sh`) : 84 créatures apparues, 565 coups portés au joueur, 7 éliminations, aucune erreur ; relecture `godot-reviewer`, remarques traitées (appels réflexifs, exception isolée, `ProcessMode` documenté).

**Pas fait** : le plafond `active_enemies_ceiling` reste à 200, la valeur dépend d'un choix de Raphaël (question au tableau de bord). Contre un mur, le coût de `MoveAndSlide` reste élevé (F2, F3). Le pas du moteur (3,5 ms à 400) et les scripts `_Process` (6 ms à 400) sont intacts.

### A livré — 9 octobre 2026

- **`CrowdIndex`** (`scripts/Combat/`) : registre C# des cibles hostiles vivantes (`Enemy`, Indicible, interface `ICrowdMember`), grille de hachage de 64 px relue au plus une fois par tick physique et par image, ou après une inscription. `CrowdIndex.Near(centre, rayon)` prête une liste de candidats (rayon + 48 px de marge de déplacement) rendue par `using` : une requête faite pendant le parcours d'une autre reçoit sa propre liste. `CrowdIndex.All()` copie toutes les cibles. `MarkMoved()` après un déplacement hors tick (bancs, tests).
- **Les 30 parcours** de `GroupCache.GetEnemies()` passent par l'index ; chaque site garde son test exact (distance au sol, écran, cône, rectangle). `GetEnemies()` et les champs `_groupCache` devenus inutiles sont supprimés, ainsi que les paramètres correspondants (`ZoneEchoes`, `ControlPropagation`, `GroundFire`, `LevelUpFx.Play`, `RunEventContext`). Le groupe `enemies` reste (outils d'observation, comptages ponctuels).
- **Écart** : l'index voit une créature dès son inscription et l'oublie dès sa mort, là où l'ancien groupe était figé pour l'image.

**Mesure** : sans effet visible sur ce banc. Base F1 contre A, hors quota, 1080p : 400 créatures 57,9 → 57,8 FPS avec Boussole, Chaîne des noms, Cloche et Gants d'écho ; 600 créatures 33 → 34 FPS, dans le bruit. Le banc reste dominé par la physique ; le lot A retire les coûts armes × créatures et projectiles × créatures qui pèseront à plusieurs milliers, et donne à F2 et B leur grille. Le bloc mesuré sans build est écarté : un autre jeu (Graffwall) tournait à 211 % CPU pendant la mesure.

**Vérifications** : build sans avertissement ; `tools/validate.sh` 32/32 (une première passe à 31/32 : `ObjectsRegression` forçait par réflexion le cache de groupe supprimé, remplacé par `CrowdIndex.MarkMoved()`) ; relecture `godot-reviewer` : rayons des 30 sites vérifiés un par un, remarques de propreté traitées (un type public par fichier, code mort, commentaires).

### F2 livré — 9 octobre 2026

- **Séparation** : chaque créature (hors mini-boss et boss, qui poussent sans être poussés, et hors créature figée) s'écarte des cibles à moins de 26 px, d'une poussée calculée par `CrowdIndex.SeparationPush` sur les positions de la grille, sans interop. La poussée, plafonnée, pèse jusqu'à 90 % de sa vitesse.
- **Arrêt au contact** : une créature de mêlée s'arrête à 22 px du joueur au lieu de viser son centre ; elle reste tournée vers lui et garde son pas. La portée de ses coups (38 px) ne change pas.
- **Réglages** en données : `data/scaling/crowd.json` (`separation_radius_px`, `separation_strength`, `separation_max_push`, `contact_distance_px`), lus par `CrowdDataLoader`.
- **Planche** : [29-f2-separation.png](planches/29-f2-separation.png), banc dense à 240 et 400 créatures. Avant, les 333 Ombres s'empilent en un point sous le joueur et sont invisibles ; après, elles forment un disque autour de lui, les Cracheurs un anneau à distance.
- **En run** (`measure_run.sh`, 5 min, bot invincible, graines 42 et 1002, avant → après) : coups reçus par minute de 0 à 4 min 1 375 → 1 437 et 781 → 1 118, de 4 à 5 min 1 822 → 1 511 et 1 747 → 2 066 ; éliminations 344 → 402 et 319 → 377 (+17 %, la foule étalée offre plus de cibles aux tirs et aux zones).
- **Coût** non mesuré : la machine était chargée (Unity, Graffwall) pendant cette session. À mesurer avec le lot B.

**Vérifications** : build sans avertissement ; validation complète 31/32 puis les dix suites de combat 10/10 après correction d'un test qui téléportait une créature sans prévenir l'index (`CrowdIndex.MarkMoved()`).

### B livré — 9 octobre 2026

- **`Enemy` est un `Node2D`** : plus de `CharacterBody2D`, de forme de collision ni de `MoveAndSlide`. Propriété `Velocity` propre ; `MoveBody` avance de la vitesse du tick puis sort des décors bloquants. Rayon du corps 14 px × échelle de variante (`BodyRadius`, plus grand rayon atteint dans `Enemy.LargestBodyRadius`). Enfouissement du Rampant par `IsBurrowed`, au lieu de la couche de collision.
- **`ObstacleField`** (`scripts/World/`) : les décors bloquants (`EnvironmentProp`, Mémorial, Atelier) inscrivent leur emprise ; enveloppe convexe comme `ConvexPolygonShape2D`, emprise sans surface ignorée ; passage en coordonnées monde au premier tick où le décor est dans l'arbre ; grille de 64 px ; disque contre polygone par la plus courte sortie, deux passes. Vidé au début de chaque run (`WorldSetup`).
- **Touches** : un projectile du joueur interroge l'index de la foule le long du segment parcouru pendant le tick, et frappe dans l'ordre de passage (perforation) ; il ne surveille plus de corps (`collision_mask = 0`) mais reste détectable par les zones de l'Indicible. Les orbes en orbite sont des `Node2D` dont `OrbitContacts` rend les créatures entrées dans leur disque.
- **Contrôles ajoutés** (`WeaponRegression.Crowd.cs`) : disque loin d'un décor, disque qui mord une arête, centre dans le décor, pas en biais qui glisse le long du mur, emprise plate ignorée, tir à 60 px par tick qui touche la créature sur son trajet.

**En run** (`measure_run.sh`, 5 min, bot invincible, six graines, F2 → B) : coups reçus par minute de 0 à 4 min 1 253 → 2 954 (graine 42, run divergente : niveau 10 au lieu de 8), 901 → 936, 1 030 → 863, 1 017 → 799, 1 586 → 1 724, 617 → 665 ; éliminations 393 → 373, 322 → 380, 416 → 389, 243 → 298, 267 → 290, 60 → 69. Pas de régression hors de la variance d'une run.

**Vérifications** : build sans avertissement ; validation complète 32/32 après correction de deux tests qui simulaient l'ancienne physique (`OnBodyEntered` par réflexion, orbes en `Area2D`) ; suite `weapons` avec les six nouveaux contrôles ; relecture `godot-reviewer` : décors d'une run précédente, reconstruction complète de la grille à chaque ajout, polygones dégénérés, frappe d'une créature tuée entre la collecte et le coup, constante en double ; tous corrigés.

**Pas fait** : le banc de coût (FPS, découpage) et les captures de foule contre un décor, reportés : la machine faisait tourner un jeu pendant toute la fin de session. À faire avant de relever le plafond.

### Mesure après B et séparation par lot — 9 octobre 2026

Banc hors quota, machine calme (charge 1,4 à 2,5), une passe, début de session (`067844ba`) contre le code courant :

| Créatures | FPS avant → après | p99 avant → après |
|---|---|---|
| 400 | 49 → 96 | 35 → 15 ms |
| 600 | 7 → 62 | 321 → 23 ms |
| 1 000 | 3,7 → 27 | 444 → 59 ms |
| 1 500 | — → 8 | — → 330 ms |

Le lot B a fait tomber le pas du moteur physique (à 1 000 : 74 → 12 ms par image), mais 1 000 créatures restaient à 6 FPS. Sondes temporaires : le déplacement ne coûte qu'1 µs par créature (lecture de position 0,09, obstacles 0,46, écriture 0,45) ; c'est la **séparation** qui prenait 12,8 ms par tick à 1 000, chaque créature parcourant des centaines de voisines dans 9 cases de 64 px. Le profileur .NET l'attribuait à tort au déplacement : il impute au code managé le temps natif qui précède l'échantillon suivant.

Correction : `CrowdSeparation` calcule toutes les poussées en une passe par tick, sur une grille dont la case vaut le rayon de séparation, triée par case en mémoire contiguë ; chaque paire n'est examinée qu'une fois et pousse les deux créatures. Même règle de poussée qu'avant.

Reste à 1 000, par image : scripts physiques 15,5 ms (2,2 ticks par image, 7 ms par tick), `_Process` 11,7 ms, rendu 5,5 ms, pas du moteur 3,7 ms. Suite : lot C.
