# Plan 10 — Terrain lisible, tiles cohérentes et monde réactif

Version 0.2 · Statut : **amélioration à examiner demandée ; lots proposés**.
Priorité : P1 transversal · Dépendances : 08 contrat graphique, 01 mobilité, 03 exploration.
Références : V2 §9, Charte §1–4, [décisions](DECISIONS.md).

## 1. But

Rendre le monde plus beau, reconnaissable et agréable à parcourir : matières crédibles, transitions cohérentes, chemins lisibles, détails réactifs et obstacles compréhensibles. Toute révision de technique doit servir un défaut observé ou une amélioration testable, pas un remplacement global par principe.

## 2. Phase 0 — Sources et faits vérifiés

- [WorldGenerator.cs](../../scripts/World/WorldGenerator.cs), AssignBiomes vers 246 et CreateBiomeSeeds vers 295 : une affectation biome par cellule et biome central aléatoire. La V2 prévoit un début Forêt/Champs et des transitions graduelles.
- [BiomeTileMapper.cs](../../scripts/World/BiomeTileMapper.cs), GetSourceId vers 176, LoadTileGroup vers 251 : textures en 64×32, variantes par hash de cellule ; HashCell ne prend que x/y, pas la seed.
- [iso_tileset.tres](../../assets/tiles/iso_tileset.tres) : taille 64×32, déjà plus détaillée que le 32×16 de base de la Charte.
- Berges directionnelles du mapper vers 585 ; [RoadTileGenerator.cs](../../scripts/World/RoadTileGenerator.cs), masques de connexion/cache : patterns à reprendre pour une transition pilote.
- [WorldSetup.cs](../../scripts/World/WorldSetup.cs), ApplyTerrainAsync vers 331 : traitement par lots de 600, overlays routes ; conserver ce contrôle de charge.
- IsWalkable de WorldGenerator teste principalement l'eau ; EnsureWaterConnectivity nettoie l'eau isolée, sans garantir un trajet jouable entre spawn et récompenses après placement des props.
- [EnvironmentProp.cs](../../scripts/World/EnvironmentProp.cs) et [PropSpawner.cs](../../scripts/World/PropSpawner.cs) : collisions/occlusion à contrôler ; l'occlusion parcourt les props solides, donc densifier peut coûter.
- [generate_tiles.py](../../tools/generate_tiles.py) emploie du bruit par pixel : l'augmentation de détail doit organiser des formes et matières, pas augmenter cette granularité.

Confiance élevée sur le code ; fréquence visible des répétitions, blocages et coût réel encore non mesurés.

## 3. Cible proposée

| Aspect | Cible |
|---|---|
| Échelle | Contrat 64×32 du plan 08, même densité de pixel apparente entre sol et entités |
| Matières | Grandes formes calmes + variations locales + quelques accents |
| Frontières | Une bande visuelle de transition entre biomes compatibles |
| Navigation | Routes, clairières et obstacles cohérents avec les empreintes physiques |
| Départ | Forêt/Champs conformément V2, avec menace initiale de 03 |
| Variété | Variantes par famille et seed, pas répétition liée aux seules coordonnées |
| Vie du monde | Herbes, poussière, eau et traces brèves réagissent au mouvement |
| Performance | Complexité locale bornée et données mises en cache |

La transition visuelle ne doit pas rendre ambigu le biome de gameplay : définir un biome autoritaire par cellule, une présentation mélangée et des zones dangereuses lisibles.

## 4. Lots d'action

### Lot A — Atlas de défauts et référence

1. Capturer trois seeds : départ, deux frontières, route, berge, décor dense, zone effilochée.
2. Reprendre une vue debug des pieds, empreintes et collisions ; la créer localement si absente.
3. Mesurer génération, FPS, temps d'occlusion et nombre de props/tiles.
4. Distinguer défaut de texture, placement, raccord, collision, caméra et bruit VFX.
5. Valider les corrections prioritaires avec le pilote artistique 08.

**Livrable :** atlas annoté et mesures sur même matériel.
**Vérification :** chaque tâche B–E répond à un constat ou une hypothèse explicite.
**Garde-fou :** ne pas annoncer tous les chemins cassés sur la seule absence d'un validateur.

### Lot B — Contrat et variété organisée

1. Reprendre le TileSet et les listes de variantes chargées ; identifier celles jamais utilisées.
2. Définir dans les données familles calme/détail/accent, poids et contraintes de voisinage.
3. Introduire un hash stable salé par seed pour les variations cosmétiques, sans modifier la topologie de gameplay involontairement.
4. Revoir les textures les plus bruyantes dans la palette/éclairage de 08.
5. Maintenir un sol assez calme derrière les silhouettes, projeter correctement pieds et ombres.

**Vérification :** même seed = mêmes variantes ; autre seed = variation identifiable ; pas de damier ou accent répété systématiquement.
**Garde-fou :** pas de tirage aléatoire par frame, pas d'asset au scale individuel incohérent.

### Lot C — Transition de biome pilote

1. Tester Forêt→Marécage sur une scène et des seeds contrôlées.
2. Reprendre les masques de voisins/berges existants pour raccorder sols, humidité et végétation.
3. Créer une bande graduée, de largeur définie en données ; répartir quelques props de transition.
4. Maintenir collision/passabilité/scaling autoritaires indépendants du mélange purement visuel.
5. Comparer avant/après puis généraliser uniquement aux paires adjacentes retenues.

**Vérification :** raccord sans couture dominante, biome reconnaissable, transition lisible avec Effacement.
**Garde-fou :** ne pas créer un système de météo, de ressources ou de terrain destructible non demandé.

### Lot D — Circulation et placement

1. Reprendre IsWalkable, placement props et objectifs ; définir l'empreinte praticable du joueur.
2. Valider la connectivité réelle après décoration : spawn → objectifs importants, largeur des dégagements et possibilités de fuite.
3. Si un placement bloque, corriger localement ou déplacer l'objectif ; journaliser les cas et conserver la reproductibilité.
4. Tester dash, glissade et saut sur leurs règles réelles, jamais supposer qu'ils traversent un obstacle pour sauver une génération invalide.
5. Rendre routes/clairières identifiables sans flèches partout ; maintenir les détours risqués de 03.

**Vérification :** parcours sur panel de seeds avec validation automatique de connectivité, puis essais manuels sur segments étroits.
**Garde-fou :** pas d'élargissement uniforme qui supprime toute variété ni de récompense garantie dans un endroit inaccessible.

**Lot D, étape 2 — connectivité vérifiée, 27 septembre 2026 (session locale) :**
- Nouveau contrôle `--check-connectivity` de la scène d'observation. Commande : `MEASURE_EXTRA_ARGS="--check-connectivity" tools/measure_run.sh <dossier> 5 "<seeds>"`.
  - Il pose une grille de 12 px sur toute la carte, une fois les décors posés.
  - Une case est bloquée hors des limites, sur le bord effacé, ou si un corps de la couche des obstacles (décors bloquants, Mémoriaux) passe à moins d'un rayon de joueur (12 px). L'eau reste praticable, comme en jeu.
  - Parcours en largeur depuis le joueur, sans passer en diagonale entre deux obstacles qui se touchent. Un lieu (coffre, Mémorial, Faille) compte comme atteint si une case atteinte est à sa portée d'interaction.
  - Sortie : une ligne `RESULT connectivity` et `connectivity.png` (gris bloqué, vert atteint, rouge lieu inaccessible).
- **Résultat sur 13 seeds** (221092026, 1002, 7, 42, 20260926, 1 à 6) : 31 lieux par carte, **aucun inaccessible**. 0,3 s de calcul par carte.
- L'étape 3 (déplacer un lieu inaccessible) n'a donc rien à corriger aujourd'hui. Le contrôle reste disponible après chaque changement de placement.
- **Observé en passant :** une exécution headless sur treize s'est terminée par un plantage du moteur à la fermeture (`mutex lock failed`), après avoir écrit son résultat. Il ne s'est pas reproduit sur huit relances de la même seed. Cause non établie. Seul changement récent qui touche aux threads : la génération du monde sur un thread (`ad2e8d6`). À surveiller.

### Lot E — Réactivité du décor

1. Définir matériau sous le joueur et réutiliser audio/VFX de 02 pour pas, éclaboussures et poussière.
2. Réagir au passage/mobilité par herbe déplacée visuellement, particules brèves et traces temporaires.
3. Limiter la zone d'activation ; mutualiser/pooler les effets ; optimiser l'occlusion si les mesures A le justifient.
4. Comparer rendu en marche lente, dash, saut et combat dense.
5. Le chemin rémanent de 11 est un prototype optionnel séparé, pas un prérequis de ce lot.

**Vérification :** monde réactif sans gêne de lecture ni coût proportionnel à toute la map ; effets réduits cohérents.
**Garde-fou :** les shaders ne déplacent pas des collisions réelles à l'insu du joueur.

**Lot E, étape 2 — herbes qui plient, 27 septembre 2026 (session locale) :**
- Les herbes hautes, coquelicots, fleurs, fougères, roseaux et buissons bas s'écartent du joueur. Leur haut se cisaille à l'opposé de lui, la base reste plantée (5 px au plus, sur 30 px autour).
- **Sans index spatial ni boucle sur les décors** :
  - `GrassTrample` écrit une fois par frame la position du joueur dans l'uniforme global `trample_origin` ;
  - le shader `prop_trample` (le shader de l'oubli des décors, plus un cisaillement dans `vertex()`, via `prop_forget.gdshaderinc` partagé) fait le reste sur le GPU.
- Choix des décors : non bloquants, sans canopée, 40 px de haut au plus, et dont le nom commence par un préfixe de végétation (`trample_prefixes` et `trample_max_height` dans `world_gen.json`). La hauteur seule prenait aussi des tas de gravats (capture).
- Capture `--capture-trample` (nouveau mode) regardée : une touffe d'herbe haute penche d'un côté puis de l'autre selon la position du joueur.
- **Non fait :** traces temporaires (étape 2, suite) ; seules les herbes réagissent au joueur, pas aux créatures (fait le 27 septembre au soir, ci-dessous).

**Lot E, étape 2 (suite) — herbes qui plient aussi pour les créatures, 27-28 septembre 2026 :**
- `GrassTrample` choisit, toutes les 50 ms, les huit créatures les plus proches du joueur dans un rayon de 600 px, par insertion dans un tableau fixe, sans allocation. Il les écrit dans quatre uniformes globaux (`trample_creatures_0` à `_3`, deux positions par vecteur). Dans `vertex()`, le shader retient le passage le plus proche, joueur ou créature.
- Relecture à 20 Hz plutôt qu'à chaque image. Le premier banc, à chaque image, coûtait 0,19 ms par image à 720p (291 → 276 FPS), à cause du parcours des créatures. À 20 Hz, une créature bouge de quelques pixels entre deux lectures, ce qui ne se voit pas sur une flexion de 5 px.
- **Mesure** : trois bancs A/B contre `HEAD` à 20 Hz. Les FPS sont inexploitables : la charge est montée jusqu'à 4,8 et les passes vont de 162 à 273 FPS. Le temps GPU, insensible à la charge, est identique (720p 0,40 → 0,39 ms, 1 080p 0,68 → 0,66 ms). Le CPU restant vaut au plus le tiers des 0,19 ms.
- **Vérifié** : `--capture-trample` complétée : une Ombre posée à gauche d'une touffe, le joueur hors de portée, et la touffe se couche vers la droite. Image regardée ; smoke test.

## 5. Recette et décision finale

Même scènes/seeds/trajectoires avant/après, clavier/manette, biomes/effacement et densité normale/forte. Garantir 60 FPS cible, génération raisonnable et chemins lisibles ; build et smoke pour scènes/shaders/initialisation.

Valider les gains en lisibilité, cohérence et déplacement. Le streaming complet ou une nouvelle architecture de terrain n'entre dans le plan que si le profilage établit son besoin et qu'un sous-plan est ensuite approuvé. Roadmap A/F/G.


## 6. Régression « un seul biome autour du départ » — 25 septembre 2026

**Retour de Raphaël :** « j'ai l'impression d'apparaître sur un seul terrain qui couvre toute la carte, alors qu'avant les biomes étaient mélangés, et c'était mieux ».

**Cause, dans l'historique git :** la génération n'a pas changé en septembre. Le changement date du commit `8340950` (14 mars 2026, « Refactor World Generation ») :
- Avant : cinq secteurs angulaires bruités qui partaient tous du point d'apparition. Tous les biomes se touchaient au départ.
- Après : régions de Voronoï, avec un biome central posé sur le spawn et les quatre autres sur un anneau, à au moins 60 cellules les uns des autres. Le rayon de la carte passe en même temps de 100 à 200 cellules.
- Conséquence : le joueur démarre au milieu d'une seule région d'environ 60 cellules de large. Un écran à 1080p couvre une douzaine de cellules autour du joueur.

**Mesure** (`--capture-map` du banc `RunObservation`, 40 seeds consécutives à partir de 1000, grille de biomes réelle) :

| Indicateur | Avant correction | Après correction |
|---|---|---|
| Part du biome majoritaire, rayon 10 cellules | 100 % | 94 % |
| Part du biome majoritaire, rayon 20 cellules (≈ 1,5 écran) | 100 % | 73 % |
| Biomes présents dans un rayon de 20 cellules | 1,0 | 2,9 |
| Biomes présents dans un rayon de 40 cellules | 1,6 | 4,2 |
| Distance du spawn à la première frontière (moyenne / max) | 31 / 60 cellules | 7,6 / 12 cellules |
| Temps de génération d'un monde (20 seeds, même machine) | 868 ms | 253 ms |

**Correction :** une mosaïque de régions (`WorldGenerator.CreateBiomeRegions`).
- Des centres de région sont tirés sur tout le disque avec un espacement minimal (tirage de Poisson), soit une centaine de régions.
- Chaque région reçoit un biome : jamais le même que ses voisines, le biome le moins représenté en priorité, `map_weight` respecté.
- La première région est décalée de 40 % d'un espacement par rapport au spawn, pour que le départ soit proche d'une frontière.
- Le bruit de déformation des frontières est conservé.
- Recherche par grille de cases : pas de coût supplémentaire au chargement.
- Réglages dans `data/world/world_gen.json`, bloc `biome_layout` : `region_spacing` (28 cellules), `warp_strength` (9), `spawn_offset_factor` (0,4).

Tous les consommateurs (tiles, layouts urbain, marais et champs, props, POI, spawns, nom du biome dans le HUD) interrogent le biome par cellule : ils fonctionnent tels quels avec plusieurs régions par biome.

**Vérifications :** build sans avertissement, smoke test vert, `MovementRegression` et `EnemyAbilityRegression` sans échec. Vues d'ensemble dézoomées avant/après sur les seeds 1000 et 1002 : au premier écran de la seed 1002, champs et ruines urbaines sont côte à côte.

Reproduire : `CAPTURE_EXTRA_ARGS="--capture-map" tools/capture_run.sh <dossier> 10 0 1920x1080 1000`. Le banc écrit `biomes-<seed>.png` (un pixel par cellule, anneaux de 20 et 40 cellules) et `overview-zoom*.png`.

**Points ouverts pour Raphaël :**
- Taille des régions : 28 cellules, soit environ deux écrans par région. Plus grand donne plus d'identité à chaque biome ; plus petit, plus de mélange. Réglage en une ligne.
- Les frontières restent franches (changement de tile net) : c'est le lot C (transition pilote) de ce plan.
- La V2 prévoit un début en Forêt ou en Champs ; ce n'est pas appliqué, le biome de départ reste aléatoire.

## 7. Audit des décors — 25 septembre 2026

**Retour de Raphaël :** sur toutes les cartes, et surtout en zone urbaine, « beaucoup de sprites invisibles et de hitbox de décor très frustrantes ». Les tiles elles-mêmes sont acceptables.

**Méthode :**
- Audit statique de 127 entrées de décor : les 5 JSON de `data/props/` (placés par `PropSpawner`) et les tables codées en dur d'`UrbanPropPlacer` et de `SwampPropPlacer`. Pour chaque sprite : existence, `.import`, pixels opaques, luminance, largeur de la base visible (cinquième inférieur de la silhouette), puis comparaison avec la collision.
- Captures en jeu (`--capture-props`, seed 1002, 1080p) de la zone la plus chargée de chaque biome, formes de collision affichées.

**Constats :**

1. **Aucun sprite manquant ni transparent.** Les 127 fichiers existent et ont leur `.import`. Les décors « invisibles » sont des sprites minuscules à l'échelle actuelle, et camouflés : même valeur et même palette que des sols très bruités.

   | Biome | Décors bloquants quasi invisibles en jeu |
   |---|---|
   | Carrière | rocher moussu 14×10, souche 10×8, poutrelles 32×8 (collision de 6 px sur une barre plate) |
   | Urbain | bennes 12×12, feu tricolore 8×16, cabine 10×20, voiture 32×16 posée sur l'asphalte de même teinte |
   | Champs | balles de foin 12×10, muret 32×10, clôtures 32×12, charrue 20×10, épouvantail, puits, menhir |
   | Forêt | souche, rocher moussu, tronc couché 24×8, lampadaire 8×28 |
   | Marais | tonneau 12×14, souche pourrie, tronc couché |

   Au total, 30 décors de moins de 16 px de haut ou de moins de 120 pixels opaques bloquent le joueur.

2. **Toutes les collisions sont des cercles centrés sur le bas du sprite.** Le point d'ancrage est le pied du sprite, donc la moitié basse du cercle déborde devant le décor visible : de 4 à 14 px pour les arbres, voitures, épaves et murets (27 décors), de 14 à 25 px pour les sept immeubles urbains.

3. **Les immeubles sont traversables sur la moitié de leur emprise.** Un immeuble de 118 à 140 px de large a un cercle de 40 à 56 px posé devant sa façade. On bute sur du vide devant, puis on traverse les murs. L'antenne radio (64 px de base) n'a qu'un cercle de 16 px.

4. **Pas de tri en profondeur.** `PropContainer` est dessiné sous le joueur et les ennemis, et `EnemyContainer` au-dessus du joueur (ordre des nœuds, seul le sol a `y_sort_enabled`). Passer derrière un immeuble dessine le joueur sur son toit. La transparence d'occlusion de `PropSpawner` ne concerne que ses propres décors : ni les immeubles ni les décors du marais n'y sont inscrits.

5. **Coût par frame.** L'occlusion parcourt à chaque tick physique tous les décors hauts de `PropSpawner` (plusieurs milliers sur une carte de rayon 200 ; 10 576 décors au total sur la seed 1002) et réécrit leur `SelfModulate`, même loin de l'écran.

**Lots proposés :**

| Lot | Contenu | Vérification |
|---|---|---|
| **D1 — Collisions calées sur le visible** (maintenant) | L'emprise est calculée une fois par texture à partir de ses pixels (largeur et bas de la base visible) : losange iso aplati sous la base, pas de cercle décalé. Pas de collision pour les petits décors (seuil de taille). Les JSON et tables passent de `collision_radius`/`collision_offset_y` à `blocking` (+ `footprint_scale` facultatif). | Re-audit : aucun débordement > 2 px, aucun petit décor bloquant ; captures collisions avant/après par biome ; `MovementRegression` |
| **D2 — Profondeur iso** (maintenant, avec D1) | Tri en Y commun pour décors, joueur, ennemis, coffres et POI ; sol, brouillard et overlays hors tri par `z_index`. Occlusion limitée aux décors proches (index spatial construit une fois), étendue à tous les placeurs. | Captures derrière et devant un immeuble, un arbre, un coffre ; FPS combat dense avant/après |
| **D3 — Lisibilité provisoire** | Ombre de contact sous chaque décor, pour l'ancrer au sol en attendant la refonte 08 | Captures carrière et ville |

La refonte visuelle des décors eux-mêmes (dessin, échelle, contraste) relève du plan 08, lot « décors procéduraux ».

### D1 et D2 livrés — 25 septembre 2026

- `PropFootprint` mesure une fois par texture la base visible (largeur, centre, bas de silhouette). La collision est un losange iso 2:1 couvrant 85 % de cette base, posé dessous. Plus de cercle décalé.
- Un décor ne bloque que s'il est marqué `blocking` **et** assez grand : 18 px de haut et 150 pixels opaques au moins. Les JSON de `data/props/` et les tables des placeurs urbain et marais passent de `collision_radius`/`collision_offset_y` à `blocking` (+ `footprint_scale` facultatif). Seuils dans `world_gen.json`, bloc `props`.
- Tri en Y sur la racine de `Main` et sur les conteneurs de décors, d'ennemis et de POI. Le point de tri d'un décor est le centre de son emprise, pas le pied du sprite. Le sol (z −10), les routes (−9) et l'overlay d'Effacement (−5) passent dessous. Les décors plats non bloquants (≤ 12 px) et les flaques sont des décalques au sol (−1). Les chiffres de dégâts restent au-dessus (30).
- `PropOcclusion` indexe une fois les décors hauts (≥ 24 px ou canopée) de tous les placeurs, soit 1 586 sur la seed 1002, dans une grille de cases de 256 px. À chaque tick, seuls ceux de la case du joueur sont testés.
- Effet de bord : l'overlay d'Effacement, dessiné jusqu'ici sous le sol (z −1 contre 0), redevient visible.
- Vérifié : build sans avertissement, smoke test, `MovementRegression`, captures `--capture-props` avant/après dans les cinq biomes, dont le passage derrière un immeuble.
- Décalques au sol sortis du tri : 5 720 des 10 576 décors de la seed 1002 passent dans un conteneur `GroundDecals` non trié (z −1). Le tri en Y ne porte plus que sur les décors qui ont une hauteur.
- **Performance** (`tools/benchmark_movement.sh`, 120 ennemis, 15 s, Ryzen 7 5700X + RX 6950 XT, machine redevenue calme ; les premiers essais sous charge étaient invalides) :

  | Version | 720p FPS moyen / p99 | 1080p FPS moyen / p99 |
  |---|---|---|
  | `ecbe5f4` (avant la session), second passage | 74 / 19 ms | 60 / 21 ms |
  | D1 + D2 + décalques hors tri | 123 / 13 ms | 118 / 13 ms |

  Le gain vient très probablement de la suppression de l'ancienne boucle d'occlusion, qui réécrivait la transparence de milliers de décors à chaque tick. Réserve : la carte de la seed du banc diffère entre les deux versions (mosaïque de biomes).

### D3 livré — 25 septembre 2026

Chaque décor qui a une hauteur reçoit une ombre de contact : ellipse iso 2:1 à bord net, 115 % de la base visible, deux paliers d'opacité, sous les entités (z −1). La texture est générée une fois par largeur (arrondie à 4 px) pour garder des pixels de taille unique. Les décalques au sol n'en ont pas.


## 8. Stries diagonales du sol de la forêt — 25 septembre 2026

**Retour de Raphaël (capture en jeu) :** le sol de la forêt « ne fait pas naturel », il forme des stries.

**Mesure :** `RunObservation --capture-map` sort maintenant une ligne `RESULT terrain`. Elle donne la part de chaque terrain par biome, et la proportion de voisins diagonaux ↘ et ↙ de même terrain.
- Avant le correctif, sur 40 seeds : 87,3 % dans les deux diagonales. Le terrain de base est donc isotrope : les plaques de terrain ne sont pas en cause.
- Forêt : 79 % de terrain `forest`, 20 % d'herbe.

**Cause :** `BiomeTileMapper.HashCell` calculait `(x·73856093) ^ (y·19349663)`. Ses bits de poids faible ne dépendent que de x et y modulo 4 : `hash % 4` choisissait donc les quatre tuiles de terrain `forest` (deux de terre brune, deux de sous-bois vert) selon un motif périodique, qui dessine des diagonales régulières.

**Correction :**
- `HashCell` mélange les bits (finaliseur à avalanche). Le choix des variantes n'est plus périodique, dans tous les biomes.
- Forêt : terre et sous-bois ne sont plus tirés case par case. Un bruit continu (`ForestFloorNoise`), échantillonné au point au sol de la cellule (grille « stacked » dépliée en 2:1, donc isotrope), dessine des clairières et des sentiers de terre dans le sous-bois ; la variante vient ensuite du hash. L'ordre de `tile_sources.forest` fait foi : la première moitié est la terre, la seconde le sous-bois.
- Vérifié par capture en vraie run (seed 1002) : plus de stries. Les bords des plaques restent en marches de tuiles, faute de tuiles de transition (piste déjà ouverte dans ce plan).

## 9. Retours du 26 septembre : tiles et jonctions

**Raphaël :** « tu n'as pas touché aux tiles, en vrai je pense que là aussi tu aurais de la marge pour les améliorer » ; « ce serait bien d'avoir un effet de jonction entre les environnements, que ça ne fasse pas brut ».

**Constat :**
- Les tiles (`tools/generate_tiles.py`) sont des losanges de 64×32 remplis de bruit par pixel. De près, c'est granuleux ; de loin, cela forme un damier de valeurs. Les décors refaits sont désormais plus lisibles que le sol qui les porte.
- La frontière entre deux biomes est une arête de losanges nette. Avec la mosaïque de régions (§6), il y en a bien plus qu'avant.

**Lots proposés, qui remplacent l'ordre de priorité des lots B et C du §4 :**

| Lot | Contenu | Vérification |
|---|---|---|
| **T1 — Sol procédural** | Réécrire les tiles dans le pipeline commun (`tools/sprites/`) : grandes formes calmes (plaques d'herbe, dalles, vase), 3 à 4 tons par matière, détails rares, palette accordée aux décors du biome. Variantes raccordables entre elles, sans motif de pixel qui se répète | Planche des tiles côte à côte, captures des cinq biomes à zoom normal et dézoomé |
| **T2 — Jonctions** | Mélange au sol le long des frontières : un shader du sol lit une petite texture « biome par cellule » générée au chargement, et mêle les deux matières sur une bande de 1 à 2 cellules, par tramage (pixels nets, pas de fondu flou). Quelques décors de transition (herbes entre forêt et champs, gravats entre ville et carrière). Le biome de gameplay reste celui de la cellule | Captures aux frontières des cinq paires fréquentes, avant/après ; banc de performance (le coût du shader doit rester négligeable) |
| **T3 — Routes et chemins** | Chemins de terre des champs et pistes de la forêt raccordés aux rues, bordures de trottoir usées | Captures |

Ordre recommandé : T2 d'abord, qui a l'effet le plus visible sur la carte en mosaïque, puis T1, en validant biome par biome comme pour les décors.

### Lot T2 livré — 26 septembre 2026

Ordre T2 → T1 → T3 validé par Raphaël.

- **Shader du sol** `assets/shaders/ground.gdshader`, posé par `World/GroundMaterial` sur `Ground` après la pose du terrain. Une texture de 401×401 décrit chaque cellule (biome, tuile posée, drapeau « frontière à moins de deux cases ») ; un atlas rassemble les 73 tuiles utilisées. Près d'une frontière, une partie des pixels prend la matière du biome voisin, par amas de 2×2 pixels, avec une lisière qui ondule selon un bruit lent. Pas de fondu flou, et les marches de losanges ont disparu.
- Le biome de gameplay reste celui de la cellule. L'eau et le bord dissous ne participent pas. Les routes (`RoadOverlay`) reçoivent le même shader sans mélange.
- **Réglages** dans `ground_blend` de `data/world/world_gen.json` : `enabled`, `band_px` (34), `edge_noise` (0,35), `noise_scale` (0,045).
- **Coût :** aucun par frame côté CPU ; construction au chargement. Seules les cellules marquées frontière cherchent le voisin (8 à 24 lectures), les autres sortent après une seule. Banc de combat dense à 720p sur le Mac chargé (charge ≈ 8) : 91 FPS sans mélange, 85 avec. L'écart est dans le bruit de la machine, et le temps GPU n'est pas mesurable en GL Compatibility sous macOS. À remesurer machine calme avec `tools/bench_ab.sh`.
- **Vérification :** `CAPTURE_EXTRA_ARGS="--capture-junctions" tools/capture_run.sh <dossier>` capture, pour chacune des dix paires de biomes voisins, la frontière la plus proche du départ, avec les décors puis sol seul au zoom ×2. Pour l'avant, passer `enabled` à `false`. Captures regardées : les dix paires présentent une lisière organique à la place de l'escalier de losanges.
- **Non fait :** les décors de transition (herbes entre forêt et champs, gravats entre ville et carrière) ; le sol lui-même (T1) reste granuleux, et la carrière montre toujours des losanges de cristal cyan réguliers.

**Décors de transition, suite de T2 (26 septembre, session cloud) :** `World/JunctionPropPlacer`.
- Sur les cellules à moins de deux cases d'un autre biome, 7 % reçoivent un décor tiré dans la liste de la paire de biomes (`junction_props.pairs` dans `world_gen.json`, les dix paires) : buissons, fougères et herbes hautes entre forêt et champs ; gravats et poutrelles entre ville et carrière ; roseaux entre marais et champs ; pierres moussues autour de la carrière…
- Ils sont posés avant les décors génériques, jamais bloquants, et décalés dans leur case pour ne pas s'aligner le long de la frontière.
- Seed de capture : 2 292 décors ajoutés. Un premier réglage à 14 % en posait 4 653, et les flaques entre marais et champs formaient des rangées : elles sont retirées de la liste.
- Vérification : captures `--capture-junctions` des dix paires, regardées. Build sans avertissement, smoke test.

### Lot T1, premier biome : la Forêt Reconquise — 26 septembre 2026

**Constat** (`python3 tools/tile_preview.py <biome> [groupe]` pave une zone comme en jeu) : les cinq sols ont trois défauts communs. Le bruit est tiré pixel par pixel ; un motif revient au même endroit de chaque tuile et dessine une trame diagonale ; des variantes de tons différents font réapparaître les losanges.

**Méthode, les tuiles de Wang :**
- Chaque arête de la grille porte une couleur (0 ou 1), tirée par hachage de l'arête elle-même (`World/WangTiles.cs`). Une matière existe donc en 16 tuiles, une par combinaison d'arêtes.
- Près d'une arête, le motif ne dépend que de la couleur de cette arête : les voisines se raccordent sans couture et aucun motif ne se répète à l'échelle de la grille.
- Le motif est un bruit lent ramené à 4 tons, en grandes plaques, avec de rares détails au centre : feuilles, racines, fougères, fleurs.
- Générateur : `python3 tools/generate_ground.py <matière|all> [--sheet planche.png]` (module `tools/sprites/ground.py`). Il est déterministe : régénérer donne des fichiers identiques octet pour octet.

**Intégration :**
- Un biome déclare ses groupes concernés (`wang_tile_groups`). Un groupe peut contenir plusieurs matières de 16 tuiles, rangées l'une après l'autre ; la forêt tire la terre ou le sous-bois par son bruit de plaques habituel.
- `blend_terrains: true` étend les jonctions tramées de T2 aux frontières entre matières du même biome.
- La forêt utilise trois matières : `foret_sol`, `foret_terre` et `foret_sousbois`, 48 tuiles au total. Les anciennes tuiles restent sur le disque, inutilisées.

**Vérification :** captures `--capture-props --hide-collisions` avant et après, regardées. Le sol forestier devient un sous-bois continu, parcouru de sentiers de terre aux bords organiques, sans losange visible. Régression de déplacement et smoke test verts.

**Suite :** le même traitement pour les champs, la ville, le marais et la carrière. La forêt sert de référence de style : il vaut mieux que Raphaël la valide en jeu avant de généraliser (densité des détails, contraste, taille des plaques).

### Lot T1, deuxième biome : la Carrière Effondrée — 26 septembre 2026

Raphaël valide la forêt « à 100 %, mille fois mieux » : elle sert de référence. La carrière passe en quatre matières de Wang :
- `carriere_sol` : gravats, éclats clairs, rouille, rares éclats de cristal ;
- `carriere_roche` : dalles grises ;
- `carriere_industriel` : plaques rouillées ;
- `carriere_cristal` : fosses sombres semées de cristaux.

Les palettes sont resserrées par rapport à un premier essai trop bleu et trop saturé. Quand un groupe contient plusieurs matières, le jeu choisit la matière par un bruit lent : des plaques cohérentes plutôt qu'un damier de cellules. Capture `--capture-props` regardée : la trame de losanges de cristal cyan a disparu. Le marais, avec ses berges directionnelles, les champs, avec leurs parcelles, et la ville, avec ses dallages et trottoirs, demandent chacun un traitement propre.

### Lot T1, troisième biome : les Champs Sauvages — 26 septembre 2026

Huit matières de Wang : herbe, prairie fleurie, sol sec, blé, blé dense, chaume, chemin et bosquet. Le blé et le chaume utilisent un bruit étiré (`stretch`) qui dessine des rangs. Le plan de parcelles (`WildFieldsLayout`) garde la main : chaque type de parcelle choisit sa matière, et le blé dense comme la prairie fleurie forment des plaques de bruit lent au lieu d'alterner cellule par cellule. Capture `--capture-props` regardée : rangs de blé, chaume et prairies se fondent sans losanges. Les matières déjà livrées (forêt, carrière) sont inchangées.

### Lot T1, quatrième biome : les Marécages — 26 septembre 2026

Trois matières de Wang : sol moussu, vase et eau (motif étiré en rides). Les berges directionnelles dessinées à la main ne servent plus : dans les biomes qui fondent leurs matières (`blend_terrains`), l'eau participe désormais au mélange tramé, avec son propre identifiant, ce qui dessine des rives organiques. Le ralentissement dans l'eau reste calculé par cellule ; seul le rendu de la rive est tramé, sur une demi-cellule de part et d'autre. La vase borde l'eau par endroits et forme de rares plaques ; un premier essai, trop étendu et trop contrasté, a été resserré. Il reste la ville, dont les dallages et trottoirs demandent un motif régulier, pas un bruit.

### Lot T1, cinquième biome : les Ruines Urbaines — 26 septembre 2026

Quatre matières :
- `ruines_sol` : béton fissuré, sous les routes et aux abords des immeubles ;
- `ruines_trottoir` : dalles de 16 px ;
- `ruines_carrelage` : carrelage de 8 px, pour les intérieurs et une partie des places ;
- `ruines_place` : grandes dalles de 32 px en quinconce.

Le générateur sait désormais dessiner un dallage (`slab_px`). Les origines des tuiles tombent sur des multiples de 32 au sol, donc les joints se raccordent d'une tuile à l'autre. Un bruit lent efface une partie des joints (`joint_wear`) : sans cette usure, la ville entière ressemblait à du papier millimétré. Les groupes spéciaux de la ville (trottoir, place, intérieur, bord d'immeuble) passent aussi en Wang.

`blend_terrains` est activé : les losanges de béton isolés disparaissent dans le dallage. Les routes, sur leur propre couche, restent nettes. Capture `--capture-props` regardée.

**T1 couvre les cinq biomes.** Les anciennes tuiles restent sur le disque, inutilisées, car leurs `.import` sont référencés.

### Lot T3 livré — chemins et routes, 26 septembre 2026

**Chemins de terre entre les régions** (`World/PathNetworkGenerator`) :
- Un arbre couvrant relie les centres des régions voisines de la mosaïque. S'y ajoutent des boucles (30 % des liaisons voisines courtes) et deux liaisons depuis le départ : le joueur apparaît sur un chemin.
- Chaque liaison est tracée par A* sur la grille, avec un coût par cellule :
  - l'eau et les immeubles sont infranchissables ;
  - les rues et les chemins déjà tracés sont bon marché : un chemin qui entre en ville emprunte la chaussée et sort par un bout de rue, et deux chemins proches se rejoignent au lieu de courir côte à côte ;
  - un bruit lent fait serpenter le tracé.
- Les tronçons qui passent sur une rue ou sur un chemin déjà tracé ne sont pas redessinés.
- Le tracé est lissé (Chaikin), ondule légèrement, puis devient un ruban maillé (`PathMeshes`) de largeur constante au sol : deux fois moins épais à l'écran quand il file vers la profondeur. Un chemin qui ne mène nulle part s'amincit sur ses derniers 72 px.
- **Style par biome** (`path_style` dans `data/biomes/*.json` : ton, ornières, largeur) :
  - champs : chemin de terre à deux ornières, l'herbe visible au milieu ;
  - forêt : piste pleine et étroite ;
  - marais : piste de vase ;
  - carrière : piste de gravier grise, à demi ornièrée.

  Le ton se fond sur une dizaine de points au passage d'un biome à l'autre.
- **Shader** `path.gdshader` : bord tramé par amas de 2×2 pixels, lisière qui ondule, quatre tons par plaques, gravillons épars. L'oubli s'y applique comme au sol, car les fonctions d'oubli du sol sont extraites dans `ground_forget.gdshaderinc`, partagé par les deux shaders.
- Aucun décor n'est posé sur les cellules d'un chemin, ni dans le marais.
- **Réglages** : bloc `paths` de `data/world/world_gen.json` (voisinage, boucles, coûts, ondulation, marge au bord du monde, plafond d'exploration).
- **Coût** (seed 221092026, conteneur cloud sans GPU) : 87 régions, 96 liaisons, 82 tronçons, 3 790 cellules. Au chargement, 257 ms de calcul CPU pour 207 000 cellules explorées. Aucun coût par frame côté CPU. Le rendu ajoute un maillage par tronçon, écarté hors écran par le moteur.

**Rues verticales et bordures** (`RoadTileGenerator`) :
- Défaut corrigé : en grille « stacked », une colonne de cellules zigzague de ±16 px d'un rang à l'autre, et chaque tile centrait sa bande verticale sur sa cellule. Les rues verticales apparaissaient donc en deux bandes séparées par un joint de trottoir.
- Chaque bitmask existe désormais en deux variantes, une par parité de rang. La bande verticale est décalée au milieu de la colonne, et chaque tile ne dessine que sa tranche de 16 px de haut, le pas entre deux rangs. On obtient une seule chaussée droite, sans recouvrement. Les voitures et gravats des rues verticales sont recentrés d'autant.
- Bordure de trottoir d'un pixel autour de la chaussée : claire au nord et à l'ouest, ombrée au sud et à l'est, absente par tronçons de 3 px (usure).

**Vérification** :
- Nouveau mode `CAPTURE_EXTRA_ARGS="--capture-paths" tools/capture_run.sh <dossier>` : pour chaque biome, le chemin le plus proche du départ, avec décors puis sol seul au zoom ×2 ; un raccord à une rue ; une vue dézoomée. Captures regardées.
- `--capture-props --hide-collisions` avant/après pour la ville : la rue verticale est continue.
- Build sans avertissement, smoke test, `MovementRegression` vert.

**Non fait, points ouverts** :
- Les allées de parcelles des champs (`WildFieldsLayout`) restent de larges bandes de terre. Les chemins les suivent de préférence (coût réduit). Leur refonte en haies et limites de parcelles relève du plan 08 P4b-3.
- Les bouts de chemin sans issue pourraient accueillir les points d'intérêt : les POI sont placés après les chemins et n'en tiennent pas compte.
- Pas de passerelles ni de gués : l'eau reste infranchissable pour le tracé.
- Aucun banc de rendu sur GPU réel. Mesures et recette à faire sur le Mac.

## 10. Investigation performance — 26 septembre 2026

**Demande de Raphaël :** « j'ai beaucoup aimé les initiatives pour améliorer les performances […] peut-être que ça vaut le coup de faire une investigation plus poussée ».

**Instrumentation ajoutée au banc de combat dense** (`tools/benchmark_movement.sh`) :
- nombre d'ennemis réglable (`BENCH_ENEMIES`) ;
- temps de rendu CPU et GPU du viewport, draw calls, objets rendus, paires de collision, corps actifs, nœuds dans l'arbre ;
- expériences d'attribution par `BENCH_EXTRA_ARGS` (`--hide-props`).

**Constats** (Ryzen 7 5700X + RX 6950 XT, machine calme, charge ≈ 2) :

| Mesure | 120 ennemis, 720p | 300 ennemis, 720p |
|---|---|---|
| FPS / p99 | 118 / 13,0 ms | 56 / 28,1 ms |
| Rendu CPU / GPU | 5,1 / 0,8 ms | 6,2 / 1,1 ms |
| Physique (moniteur Godot) | 4,2 ms | 10,0 ms |
| Draw calls / nœuds | 1 043 / 29 800 | 1 271 / 30 850 |

1. **Rendu :** avec les décors masqués (`--hide-props`), on passe de 118 à 252 FPS et de 5,1 à 1,4 ms de rendu CPU, sans que les draw calls changent. Le coût venait du tri en Y : Godot rassemble et trie à chaque frame tous les enfants visibles d'un conteneur trié, hors écran compris (~4 900 décors).
   **Correctif `PropChunks` :** les décors et les décalques sont répartis en tronçons de 768 px (244 sur la seed du banc), et ceux qui sont loin de la caméra sont masqués (marge de 360 px pour les grands décors). Les tronçons héritent du tri de leur conteneur. `PropOcclusion` est construit avant le découpage.

   | Ennemis | Résolution | FPS avant → après | p99 avant → après | Rendu CPU avant → après |
   |---|---|---|---|---|
   | 120 | 720p | 118 → 247 | 13,0 → 7,5 ms | 5,1 → 1,5 ms |
   | 120 | 1080p | 110 → 223 | 13,7 → 7,8 ms | 5,5 → 1,9 ms |
   | 300 | 720p | 56 → 104 | 28,1 → 15,8 ms | 6,2 → 2,0 ms |
   | 300 | 1080p | 56 → 94 | 27,1 → 16,1 ms | 6,4 → 2,4 ms |

   Le nombre d'objets rendus est inchangé : on voit la même chose. Les captures `--capture-props` des cinq biomes montrent des décors jusqu'aux bords de l'écran.
2. **Physique, désormais le goulot à forte densité** (≈ 30 µs par ennemi et par tick d'après le moniteur). Des sondes temporaires, non committées, en attribuent peu au code du jeu :
   - `Enemy._PhysicsProcess` : 4,4 µs par ennemi, dont 2,0 µs dans `MoveAndSlide`, soit ≈ 0,5 ms pour 120 ennemis.
   - `Player._PhysicsProcess` : 29 µs par tick.
   - Avec les décors supprimés, la physique ne baisse pas (5,8 ms à 120 ennemis) : les `StaticBody2D` des décors ne sont pas en cause.

   Le reste est interne au moteur (synchronisation des `CharacterBody2D`, zones de détection, pas du serveur physique 2D).
   **Suite proposée :** profiler une session avec le profileur de Godot (moniteurs de serveur) ; si la synchronisation des corps domine, essayer pour les créatures simples un déplacement allégé (séparation par grille et requêtes de collision statiques par lots) à la place d'un `CharacterBody2D` chacune. Critère : 300 ennemis à 60 FPS p99 inclus.

### Chargement d'une run — 27 septembre 2026

**Retour de Raphaël :** « quand la map se charge au début, il y a des frames où on voit la map pas chargée », et le chargement peut-il être optimisé ?

**Constat**, enregistré image par image (`tools/capture_hub.sh <dossier> ui_accept` avec `HUB_DEV="--dev --record 520"`, nouvelle option `--record` de `HubCapture`) : la toute première frame de la run montrait le HUD et un monde noir. L'écran de chargement n'entrait dans l'arbre qu'après une frame d'attente.

**Mesure** : nouveau chronomètre `LoadProfiler`, une ligne `[Chargement]` par étape dans le journal. Même seed, avant → après :

| Étape | Avant | Après |
|---|---|---|
| Génération (terrain, plans, chemins, fermes et chantiers, dans `_Ready`) | 1 060 ms | 1 120 ms |
| Tuiles du sol et des routes | **3 008 ms** | **420 ms** |
| Décors | 746 ms | 819 ms |
| Index d'occlusion et tronçons | 400 ms | 395 ms |
| **Total jusqu'à la run** | **6,0 s** | **3,6 s** |

Les tuiles coûtaient cher par l'attente, pas par le calcul. La boucle rendait la main au moteur toutes les 600 cellules, soit plus de 200 frames d'attente pour 126 000 cellules. Elle la rend désormais quand sa tranche de 12 ms est épuisée, et l'écran de chargement reste animé.

**Correctifs :**
- l'écran de chargement est ajouté (en différé) dès le `_Ready` du bootstrap : la première frame de la run est déjà noire, avec l'écran de chargement (enregistrement regardé) ;
- les tuiles et les routes sont posées par tranches de temps ;
- une frame de respiration sépare les placeurs de décors.

**Suite, le même jour :**
- **Génération sur un thread.** Terrain, biomes, plans, chemins, fermes et chantiers sont du calcul pur. Ils partent sur un thread dès le `_Ready` de `WorldSetup` et sont attendus au début d'`InitializeWorldAsync`. Le `TileSet` et les tuiles de route restent sur le thread principal (`PrepareTiles`). Aucun nœud ne lit le générateur avant : les enfants de la scène sont prêts avant leur parent, et l'arbre est en pause pendant le chargement.
  - Effet : 1,1 s d'écran noir figé devient un écran de chargement animé.
  - Le total ne baisse pas sur une machine chargée (le thread met 1,4 s, charge 6 à 7).
- **Décors construits hors de l'arbre.** Les placeurs posent les 10 700 décors dans un conteneur détaché. Les décors sont rangés en tronçons hors de l'arbre, puis les 248 tronçons y entrent d'un bloc. Avant, chaque décor entrait dans la scène, puis en sortait et y rentrait au découpage.
  - `EnvironmentProp.VisibleWorldRect` se calcule par les transformations locales (conteneurs à l'origine) ;
  - la position des scènes-récits se lit en local.
- Mesure, même seed (charge 7) :

  | Étape | Avant | Après |
  |---|---|---|
  | Décors des biomes | 635 ms | 425 ms |
  | Décors de la ville | 221 ms | 119 ms |
  | Décors du marais | 63 ms | 34 ms |
  | Tronçons (entrée des décors dans la scène) | 360 ms | 325 ms |
  | **Total** (6,0 s au départ) | 3,8 s | **3,5 s** |
- **Vérifié :** `test_movement` (intégration dans une vraie `Main`) et smoke test verts. Captures `--capture-props` de la ville et de la forêt regardées : décors, collisions et transparence derrière les immeubles et les canopées intacts.
- **Constat en passant :** la séparation des « décalques au sol » (décors plats hors du tri en Y, `SeparateGroundDecals`) ne trouve plus aucun décor : 0 sur 10 694, déjà avant ces changements. Depuis la refonte des décors à l'échelle du personnage, aucun ne passe sous la hauteur de `ground_decal_max_height`. À revoir si le tri en Y redevient coûteux.
- **Reste :** l'entrée des décors dans la scène (0,3 s d'un bloc) pourrait s'étaler sur plusieurs frames, et le calcul des chemins (0,4 s) explore 186 000 cellules.

## 11. Audit de performances du 27 septembre — lots

L'[audit](../AUDIT-PERFORMANCES-2026-09-27.md) propose six lots (§13). Les deux premiers répondent à des constats directs.

### Lot 1 livré — durée de vie des pools et abonnements, 27 septembre 2026

- **Réserve du pool d'ennemis** : `EnemyPool` libère à sa sortie de l'arbre les ennemis rendus, détachés de la scène, que la destruction de la run n'atteignait pas. Diagnostic de l'audit rejoué (trois pools créés puis détruits) : 20, 40 puis 60 ennemis survivants avant, **0** après, 0 nœud orphelin.
- **Abonnement de la montée de niveau** : `GameBootstrap` s'abonnait à `EventBus.LevelUp` par une lambda qui capturait des variables locales, jamais désabonnée : un rappel de plus par run. Il passe par une méthode désabonnée en sortie de run.
- **Brouillard** : son shader le rendait transparent depuis mars (le masque n'était jamais fourni). La couche de 160 000 tuiles ne dessinait donc rien. Elle est retirée avec son shader et ses 200 frames d'initialisation. Le suivi des cellules découvertes (`ZoneDiscovered`, sons et succès) reste, et il marche désormais dès la première frame : pendant l'initialisation, rien n'était découvert.
  - Banc A/B contre `HEAD` (2 passes, charge 2,7 à 3,8) : draw calls inchangés (1 895 à 1 080p), FPS dans le bruit (149 → 154). Aucun gain de rendu : la couche ne dessinait déjà rien.
  - **À arbitrer** : l'Oubli du regard (« le brouillard se lève 25 % moins loin ») n'avait donc aucun effet visible. Soit on dessine enfin le voile de la Stratégie V2 (§13, « voile blanc-bleuté animé »), soit on remplace cet Oubli.
- **Flash de coup** : `HitFeedback` n'écrit plus `flash_amount`, que le shader d'entité ne lit plus ; le flash visible passe par `SelfModulate`.
- **Correctif du plan 17 (3D, « Limite »)** : un coffre posé après un Oubli des repères (butin d'événement, d'élite) naît avec sa colonne raccourcie. Vérifié par `--capture-oublis` (`late_chest_signal=0.50`).
- **Vérifié :** build 0 warning, smoke test, `test_movement`, `test_enemy_abilities`, `test_weapons` verts ; capture en vraie run regardée ; relecture `godot-reviewer` sans bug.
- **Non fait :** les 20 allers-retours Hub → run → Hub proposés par l'audit (RSS, instances natives) ; le diagnostic isolé du pool suffit à établir la fuite et sa correction.

**Banc reporté de T3, fait machine calme** (`bench_ab.sh 2e50f59^`, worktree à `2e50f59`, 2 passes) : 1 080p 155,9 → 154,9 FPS, 720p 162,0 → 176,9 FPS. Les chemins n'ont pas de coût mesurable. Les p99 (22,5 → 17,1 ms) sont trop bruités pour conclure, et la carte diffère entre les deux versions.

### Lot 2 livré — atlas du sol et des routes, 27 septembre 2026

Chaque variante de tuile était une source du `TileSet` avec sa propre texture : 493 textures entremêlées. Le rendu changeait de texture presque à chaque tuile, donc un appel de dessin par tuile ou presque.

- `BiomeTileMapper` rassemble désormais toutes les tuiles, routes générées comprises, dans un seul atlas de 2 048 × 512 px, une seule source du `TileSet`. La logique de choix des tuiles (Wang, routes directionnelles, eau, rives, dissolution, graines) est intacte : elle manipule toujours des identifiants de tuile, que `SetCell` traduit en coordonnées d'atlas.
- Les jonctions (`GroundMaterial`) lisent la matière voisine dans ce même atlas. Elles ne construisent plus le leur, et le shader du sol est inchangé.
- **Image identique** : 20 captures de jonctions et 7 phases de l'oubli (même seed) comparées au pixel près. Les écarts avant/après sont du même ordre que ceux de deux runs identiques (HUD, particules) ; le sol ne diffère nulle part.
- **Banc A/B contre `HEAD`** (2 passes, charge 2,6 à 3,4) :

  | Résolution | FPS | p99 | Draw calls | Rendu CPU | GPU | Temps moyen par image |
  |---|---|---|---|---|---|---|
  | 720p | 183,7 → **281,1** | 9,8 → 7,1 ms | 1 332 → 279 | 2,35 → 0,96 ms | 1,43 → 0,38 ms | 5,44 → 3,56 ms |
  | 1 080p | 155,9 → **254,9** | 11,1 → 7,9 ms | 1 894 → 331 | 3,11 → 1,19 ms | 2,07 → 0,68 ms | 6,42 → 3,93 ms |

- Chargement : tuiles et matériau du sol 458 → 350 à 370 ms (deux runs).
- **Vérifié :** build 0 warning, smoke test, `test_movement`, `test_dev_mode`.

### Crash à la fermeture du jeu — 27 septembre 2026

Une capture sur deux environ finissait par `FATAL: Condition "csharp_lang && !csharp_lang->script_bindings.is_empty()"` et un core dump. Le crash était déjà présent avant cette session : reproduit au commit `c78e1dd6`.
- **Cause** : quitter d'un coup laissait vivantes des milliers d'enveloppes C# (1 817 formes de collision de décors, des tweens, des styles), faute de passage du ramasse-miettes. Libérées après l'arrêt du runtime .NET, elles déclenchent ce contrôle des builds de debug, c'est-à-dire du jeu lancé depuis `godot-mono`.
- **Correctif** : `GameExit.QuitAsync` libère la scène, fait passer le ramasse-miettes, puis quitte. Le bouton Quitter du camp et de la pause l'utilisent, ainsi que la fermeture de la fenêtre, désormais interceptée par `GameManager`.
- **Vérifié** : avant, 1 crash sur 1 run de `--capture-junctions` ; après, aucune ligne de fuite et aucun crash sur 5 runs, dont 3 fermés par la vraie demande de fermeture de la fenêtre (`--close-window`).

### Lot 3, première mesure — morts en masse, 27 septembre 2026

Le banc dense ne tuait rien (PV ×10 000). Nouveau mode `--churn` de `MovementDenseBenchmark` : PV normaux, chaque créature morte est remplacée sur un anneau autour du joueur, les montées de niveau sont choisies aussitôt. `--weapons a,b,c` ajoute des armes. Le banc compte aussi les **nœuds réellement créés** (`nodes_created`, première apparition de l'instance) ; l'ancien compteur (`nodes_added`) comptait aussi un ennemi recyclé qui revient dans l'arbre.

`BENCH_REPEATS=1 BENCH_SECONDS=15 BENCH_EXTRA_ARGS="--churn --weapons heavy_hammer,chain_of_names,music_box" tools/benchmark_movement.sh <dossier>`, machine calme (charge 1,4), au commit du correctif de fermeture :

| Résolution | Mode | FPS | p99 | Morts/s | Nœuds créés (15 s) | Alloué (15 s) |
|---|---|---|---|---|---|---|
| 720p | sans dash | 192 | 10,1 ms | 27 | 558 | 23 Mo |
| 720p | dash | 203 | 9,0 ms | 25 | 370 | 23 Mo |
| 1 080p | sans dash | 203 | 9,2 ms | 24 | 354 | 22 Mo |
| 1 080p | dash | 191 | 10,3 ms | 26 | 427 | 22 Mo |

- **Créations** : l'écran de level-up construit ses cartes à chaque niveau (Label, conteneurs, boutons), c'est attendu. Les pools grandissent jusqu'à leur régime : un `DeathFx` vit 5 s, soit environ 130 en vol à 25 morts/s. Les annonces au sol (`GroundTelegraph`) apparaissent une seule fois par ennemi et par espèce, dans le cache de capacités. Aucune création par coup ni par mort au-delà de ces régimes transitoires.
- **Allocations** : 1,5 Mo/s avec des morts, contre 0,14 Mo/s sans. Elles restent en génération 0, sans pic visible au p99. Pas un chantier prioritaire.
- **Reste du lot 3** : XP laissée derrière soi en run nomade longue, Effacement tardif, builds à cône continu.

### Lot 4, première partie — orbes d'XP endormies loin du joueur, 27 septembre 2026

**Mesure** (lot 3) : en run nomade de 10 minutes (`MEASURE_EXTRA_ARGS="--nomad" tools/measure_run.sh`, quatre seeds), 94 à 323 orbes restent au sol à la fin, jusqu'à 370. Chacune garde son rappel de physique, son sprite animé et sa lueur de particules GPU. Le banc (`--orbs 400`, orbes semées entre 1 200 et 3 000 px) chiffre leur coût à 0,7 ms par image à 720p : 294 → 244 FPS.

**Correctif** : au-delà de 750 px (hors de l'écran, et au-delà de l'attraction même avec un gros aimant), une orbe s'endort. Elle n'a alors plus de physique, d'animation ni de lueur. `CombatPools` fait une ronde toutes les 0,25 s et la réveille quand le joueur revient à portée, avec 100 px d'hystérésis. Sa zone reste active : marcher dessus la ramasse. L'XP est conservée, rien ne disparaît. Un jeton de sommeil invalide l'entrée d'une orbe ramassée puis réutilisée.

- **Vérifié** : `--check-orb-sleep` (orbe endormie à 1 400 px, réveillée et ramassée au retour du joueur, XP exacte) ; smoke test.
- **Banc A/B contre `HEAD`**, 400 orbes lointaines, 2 passes :

  | Résolution | FPS | p99 |
  |---|---|---|
  | 720p | 244,0 → **289,0** | 8,9 → 6,8 ms |
  | 1 080p | 229,0 → **256,9** | 9,2 → 7,8 ms |

- Le banc dense quitte désormais proprement, sinon le crash de fermeture interrompait la série. Il compte les orbes par parcours, pour compiler aussi dans les worktrees de base.

### Lot 5, première partie — animations des créatures préchargées, 27 septembre 2026

`EnemySpriteLoader` chargeait les 16 à 32 animations d'une espèce à sa première apparition, en 16 à 25 ms, soit au moins une image sautée à 60 FPS. Sur une run nomade de 5 minutes, sept espèces se chargeaient juste après l'écran de chargement (environ 140 ms de saccades au démarrage), et les autres en pleine partie. Le chronomètre est désormais dans le journal.

Les animations de toutes les espèces se chargent sous l'écran de chargement, une par image pour qu'il reste animé : nouvelle étape « animations des créatures », 222 ms. Le chargement total passe de 3,15 à 3,25 s. Plus aucun chargement d'animations pendant la partie (journal d'une run nomade de 300 s vérifié).

### Lot 4, suite — explosion des créatures instables recyclée, 28 septembre 2026

L'audit (§11) relevait que chaque explosion d'une créature à l'affixe Instable recréait ses `SpriteFrames`, son sprite, ses particules GPU, son matériau et un minuteur, avec des PNG de mars (plan 18, n° 22).
- `CombatPools.ShowExplosion` la remplace par des effets du pool commun : une zone au sol tramée du **rayon réel des dégâts** (ellipse 2:1), une onde, un éclair et une gerbe de braises qui retombent.
- Les dégâts de l'explosion sont désormais mesurés au sol, comme toutes les zones depuis les lots I du plan 08. Elle touche exactement ce qui est dessiné : plus loin à l'horizontale, moitié moins à la verticale de l'écran.
- `VfxFactory.CreateExplosionVfx`, les cinq images `vfx_explosion_f*` (non supprimées, voir plan 08) et la texture d'étincelle en losange ne sont plus utilisés, et le code correspondant est retiré.
- **Vérifié** : `--capture-bestiary --affix explosive --kill` (nouvelle option `--kill`), images regardées ; smoke test.

### Lot 4, suite — flaques de feu sans nœud et mesurées au sol, 28 septembre 2026

Chaque flaque de feu de la Lanterne Mémorielle créait un nœud et parcourait toutes les créatures à chaque tick de dégâts (audit §11). Ses dégâts étaient mesurés en cercle à l'écran, alors que la zone dessinée est une ellipse au sol.
- `GroundFire` devient une simple liste de données tenue par `CombatPools`, sans nœud par flaque, avec un seul traitement pour toutes les flaques. La zone tramée passe par `CombatPools.AddGroundFire`.
- Les dégâts sont mesurés au sol : ils touchent exactement l'ellipse montrée.
- **Test** : `test_weapons` a un nouveau contrôle. Une créature à 0,8 rayon à l'horizontale brûle ; une autre à 0,8 rayon à la verticale de l'écran (1,6 rayon au sol) reste hors de la flaque.


### Seconde passe d’audit — 28 septembre 2026 : lots proposés, aucun correctif

Le [rapport du 28 septembre](../AUDIT-PERFORMANCES-2026-09-28.md) complète les zones non mesurées du premier audit. [Preuves et reproduction](../audits/performance-2026-09-28/README.md). Les lots déjà livrés ne sont pas rouverts ; les flaques de feu trouvées en cours restent intactes.

- Cône : 50 cibles, **12 000 impacts et 3,05–3,18 Mo alloués dans les appels sur 4 s d’émission active**, après chauffe ; deux processus reproduisent les compteurs.
- Robustesse : à 1 500 px, deux statuts de 2 s ont encore 2 s après 10 s simulées. L’Effacement perd le temps excédentaire : appels à 1 Hz → 15,40 % au lieu de 30,80 % à 10 min (fixture de hitch, pas FPS observés).
- Préchauffage : les huit sprites soumis au même viewport produisent **0 dessin hors champ, 8 dans le champ** ; dix shaders de run absents de la liste. Compilations froides non chronométrées.
- Physique : **10 694 corps, mais 2 318 formes** ; 8 376 décors sans forme. Surcoût natif isolé de 736 octets/corps, soit un potentiel estimé de **5,88 Mio** ; aucun gain FPS établi.
- Durée de vie : **20 cycles techniques Hub → Main → Hub, 148 nœuds et 0 orphelin à chaque retour** ; pas de preuve nouvelle de fuite de nœuds dans ce scénario court.
- Foule : observateur décimable ajouté au banc, profils managés 60/240/240/60 archivés ; sous charge, leurs pourcentages ne décident pas d’un gain. Un A/B final au calme confirme 2,28–2,49 ms/image à 60 ennemis contre 7,42–7,58 ms à 240, observateur décimé. Le profil natif reste à établir ; le pic initial du banc (108–137 ms, index 1) doit être exclu par préparation avant chronométrage, pas supprimé des données brutes.

Ordre proposé, **non implémenté** ; un seul lot de correction ouvert à la fois :

| Lot | Périmètre | Validation qui décide |
|---|---|---|
| **6A — Horloges et distance** | D’abord expiration des statuts lointains ; puis reste temporel/rattrapage borné de l’Effacement | Même expiration avec éloignement/retour ; même intégrale, dégâts et transitions sous jitter, hitch, pause et hitstop |
| **6B — Impacts continus** | Séparer dégâts, procs et retours visuels/audio du cône | Baisser appels/allocations à dégâts et attribution d’arme conservés ; contrat des procs validé |
| **5B — Préchauffage rendu** | Variantes réelles rendues sous l’overlay, attente de fin de rendu | Soumission non nulle, comparaison cache pilote froid/chaud, premiers effets sans compilation tardive attribuable |
| **3B — Attribution de la foule** | A/B observateur fait ; déplacer l’inventaire avant mesure, fixer les quêtes dans la fixture, profiler le natif puis varier bestiaire/zoom | Même seed/build, charge ≤ seuil de `bench_ab.sh`, intervalles bruts ; aucune refonte avant attribution |
| **6C — Cellules actives de l’Effacement** | Éviter les visites des cellules à zéro, sans supprimer leur mémoire | Même historique de phases, Failles et stabilisations ; compteur de visites réduit en late game |
| **6D — Corps optionnels des décors** | Corps réservé aux décors bloquants ; streaming physique conditionnel séparé | Vérifier le potentiel mémoire ~5,88 Mio ; ne streamer les formes que si leur coût natif est établi |

Aucune case de la roadmap V2 n’est cochée par cet audit. Les pistes ECS, C++, changement de renderer, carrés de distance généralisés et reprise des optimisations déjà livrées restent écartées sans mesure nouvelle.


### Lot 6A — horloges : exécution autorisée le 28 septembre 2026

Raphaël autorise le démarrage des corrections par les problèmes critiques après le TLDR de l’audit. Ce lot est réalisé avant le cône et le préchauffage.

1. **6A.1 — Statuts des ennemis.** Expiration du ralentissement et de la désorientation, régénération et décroissance du recul indépendantes de la distance ; aucune décision aléatoire de direction hors de la zone d’IA complète. DOT inchangés en cadence, mais arrêt du tick si un DOT tue la créature. Les annonces restent annulées au loin ; aucun déplacement physique coûteux ajouté hors champ.
2. **6A.2 — Effacement.** Conserver le reste de temps et rejouer les pas logiques de 0,5 s, au plus quatre par image. Conserver la dette au-delà, publier la texture une fois par image ; ne pas sauter les transitions de phase ni regrouper les dégâts du Néant. Temps de simulation Godot : pause/hitstop ne deviennent pas du temps mural. Les pas rattrapés utilisent la position et les multiplicateurs disponibles, sans inventer un historique du joueur pendant une image non simulée.
3. **Validation avant livraison.** Banc ciblé avant/après (statuts proches/lointains/retour, DOT/mort/recyclage, horloges régulières/jitter/hitch, dette bornée, transitions et dégâts du Néant), régressions capacités/déplacement, smoke et courte capture de run. Pas de promesse de FPS : c’est un correctif de robustesse préalable aux optimisations.

**Livré et vérifié le 28 septembre.** [Banc, résultats et limites](../audits/performance-2026-09-28/lot-6a/README.md) : 26 assertions, 13 échecs avant → zéro après. À 1 500 px, les deux statuts de 2 s expirent, recul et régénération restent cohérents avec la proximité. À 1 Hz, dix minutes simulées donnent 30,8037 % d’Effacement au lieu de 15,4037 %, comme à 30/60/144 Hz. Dette conservée sous jitter et blocage de 10 s, au plus quatre pas par image ; phases et dégâts du Néant restent séquentiels. Build sans avertissement, capacités/déplacement, smoke et capture de run passent. Le test optionnel d’intégration Main headless expire à 1 500 frames avant comme après : limite conservée dans les preuves. La roadmap coche seulement ce correctif vérifié ; 6B et les autres lots restent à réaliser.

### Lot 6B — impacts continus : exécution autorisée le 28 septembre 2026

Raphaël demande de poursuivre sur le cône. Premier périmètre : cadence des dégâts, signaux et procs conservée à chaque tick ; budget de feedback par cible (10 impulsions par seconde d’exposition), premier coup et coup fatal visibles ; chiffres cumulés exacts, texte mis à jour seulement si sa valeur affichée change. Pas d’agrégation des dégâts ni de modification des probabilités des perks. État visuel réinitialisé au recyclage.

Validation : même fixture et seed, avant/après 0/1/10/50/100 cibles, dégâts/signaux/attribution et état des procs, allocations dans les appels, demandes de feedback ; sortie du cône, mort et réutilisation. Régressions armes/capacités, build sans avertissement et capture. Pas de conclusion FPS sous charge. Le cône s’appelle actuellement **Transistor** (`last_broadcast`).

**Attribution pendant l’exécution :** les demandes de feedback baissent de 12 000 à 2 000 sur 50 cibles, mais cela ne réduit pas à soi seul les allocations directes du banc. Le signal Godot `EntityDamaged` construit un tableau `params` par impact : utiliser sa surcharge `ReadOnlySpan<Variant>` garde les abonnés synchrones et supprime ce tableau, sans tampon partagé réentrant. Les entiers des chiffres sont déjà souvent internés par .NET ; ne pas leur attribuer un gain mémoire non mesuré.

**Lot 6B livré le 28 septembre.** [Mesures et recette](../audits/performance-2026-09-28/lot-6b/README.md) : quatre passes alternées, 0/1/10/50/100 cibles. À 50 cibles sur 4 s : 12 000 impacts et mêmes dégâts, feedback 12 000 → 2 000 (−83 %), allocations directes 2 655 360 → 1 407 360 octets (−47 %, 104 octets évités par impact). 38 assertions vertes, régressions armes/capacités, smoke et captures du cône avec chaîne/homing vérifiés. La cadence des procs est conservée ; leur séquence exacte est comparée avec RNG cosmétique neutralisé. Pas de conclusion FPS ni de baisse mesurée des pauses GC ; la longue run avec renouvellement soutenu reste à observer. Prochain lot proposé : 5B, préchauffage rendu.
