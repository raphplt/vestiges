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

