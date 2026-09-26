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

### Lot E — Réactivité du décor

1. Définir matériau sous le joueur et réutiliser audio/VFX de 02 pour pas, éclaboussures et poussière.
2. Réagir au passage/mobilité par herbe déplacée visuellement, particules brèves et traces temporaires.
3. Limiter la zone d'activation ; mutualiser/pooler les effets ; optimiser l'occlusion si les mesures A le justifient.
4. Comparer rendu en marche lente, dash, saut et combat dense.
5. Le chemin rémanent de 11 est un prototype optionnel séparé, pas un prérequis de ce lot.

**Vérification :** monde réactif sans gêne de lecture ni coût proportionnel à toute la map ; effets réduits cohérents.
**Garde-fou :** les shaders ne déplacent pas des collisions réelles à l'insu du joueur.

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
