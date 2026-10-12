# Plan 40 — Direction « crade » : palette salie, grain, taches, hachures

Demande de Raphaël, 12 octobre 2026 ([DECISIONS §122](DECISIONS.md)). Il cherche « une certaine esthétique qui va au-delà du pixel art » : celle des BD qu'il lisait plus jeune, « un truc un peu mystérieux, crade, indescriptible ». Après une liste de pistes, il retient tout le Métal Hurlant (Bilal, Moebius, Caza, Druillet, Hermann, Schuiten), Beksiński et, en jeu vidéo, Rain World. Il veut une DA « un peu crade » par quatre moyens : palette salie, grain, hachures dans les ombres, taches. Deux mises en garde : « ça peut vite devenir grossier ou forcé dans le trait », et « le jeu a déjà beaucoup d'assets ».

Unité d'estimation : **1 j = une journée de travail d'agent**, vérification par capture comprise.

## 1. Constat

- Les tuiles redessinées au plan 37 plaisent d'autant plus que la matière est ambiguë : marais > forêt > carrière. Leur vocabulaire (petits objets nets, éclairés en haut à gauche) est propre et lisible, l'inverse du crade.
- Les références de la Bible §4 (Hyper Light Drifter, Eastward, Dead Cells, Gris) sont des jeux nets et colorés.
- Le sol, les routes, les décors, les canopées et les entités passent déjà tous par `world_grade.gdshaderinc` (plan 39 A1–A3). Un traitement ajouté là vaut pour tout le jeu **sans redessiner un seul asset**.

## 2. Maquette hors jeu (12 octobre)

Captures `--capture-props --hide-collisions`, graine 1002, traitées en Python au pixel du monde (1 px monde = 2 px écran), trois dosages : [vue d'ensemble](planches/40-maquette-crade.jpg), [gros plans](planches/40-maquette-gros-plans.jpg), [ombre de nuage hachurée](planches/40-maquette-nuage-hachure.jpg). Script : hors dépôt (bloc-notes de la session).

| Moyen | Résultat |
|---|---|
| Palette salie (désaturée, jaunie comme un vieux papier, noirs relevés vers un brun-olive, ombres vert-gris) | Gain le plus net, surtout ville et champs. Le dosage marqué rend tout brun et boueux. |
| Grain au pixel du monde (fin, plus une fibre lente) | Lisible comme une texture, reste du pixel art. |
| Taches (auréoles d'eau, moisissure) | Très bien au sol et sur les façades ; sur une canopée ou une créature, on croit à une saleté de l'écran. |
| Hachures selon la luminosité | Échec : trame régulière de points (premier jet), puis invisibles (traits rompus). |
| Hachures dans une ombre connue (nuage) | Traits à la main lisibles de près, discrets de loin. |

**Retenu par Raphaël : dosage léger.**

## 3. Garde-fous

- **Discret d'abord.** Le dosage léger de la maquette est un plafond, pas un point de départ.
- **Au pixel du monde, accroché au sol.** Grain, taches et hachures se calculent sur la position monde arrondie au pixel : ils ne glissent pas quand la caméra bouge.
- **Rien qui glisse sur une entité.** Le joueur et les créatures ne reçoivent que la palette salie, à la part d'entité de l'étalonnage ; ni grain, ni taches, ni hachures. Les yeux vert-acide restent intacts.
- **Avant l'oubli.** Le traitement s'applique avant l'Effacement : le Néant reste d'un blanc pur. Le monde dont on se souvient est taché ; ce que l'oubli prend devient net et vide.
- **En données.** Recette commune dans `data/world/atmosphere.json`, dosage par biome dans le bloc `atmosphere` de `data/biomes/*.json`, fondu avec l'étalonnage.
- **Aucun nouvel asset, aucun nouveau shader** : `ShaderWarmup` inchangé.
- **Coût :** quelques lectures de bruit par pixel sur des shaders existants. Banc A/B à la fin de G2, machine calme.

## 4. Lots

### G1 — Palette salie et grain (0,5 à 1 j)

- `world_grime` dans `world_grade.gdshaderinc` : désaturation, jaunissement, ombres vert-gris, noirs relevés ; grain au pixel du monde.
- Appliqué au sol, aux routes, aux décors, aux herbes et aux canopées ; palette seule aux entités.
- Section `grime` de `atmosphere.json` (recette et dosage commun), `grime` par biome ; uniformes globaux publiés par `BiomeAtmosphere` avec l'étalonnage.
- Vérification : captures des cinq biomes, avant/après à la même graine ; contrôles de catalogues (refus des valeurs hors bornes).

### G2 — Taches (0,5 j)

- Auréoles d'eau et moisissure en coordonnées monde, au sol, sur les routes et sur les décors ; jamais sur les canopées ni les entités.
- Vérification : captures ; banc A/B de G1 + G2.

### G3 — Hachures dans les ombres (1 j)

- Traits courts à la pente 2:1, rompus et d'écart irrégulier, dans les ombres de nuages (A2) et les ombres portées (A3) ; contre-hachure au cœur de l'ombre.
- Vérification : captures, gros plans au pixel.

### Plus tard, à discuter

- Mettre à jour les références de la Bible §4 (ajouter Métal Hurlant, Beksiński, Rain World) : décision de Raphaël.
- Les assets eux-mêmes (tuiles, décors, créatures) dans cette direction : seulement si le traitement global ne suffit pas.

## 5. G1 livré — 12 octobre 2026

**Fait :**

- `world_grade.gdshaderinc` : `grime_palette` (désaturation, jaunissement du papier, ombres tirées vers le vert-gris, noir relevé vers un brun-olive) et `world_grime_at` (palette, puis grain fin au pixel monde et fibre lente). Appliqués après l'étalonnage et les nuages, avant l'oubli.
- Sol, routes, décors et herbes : palette et grain au pixel monde. Canopées : grain lu au point de repos du pixel (`grime_anchor`), il suit la feuille quand elle se balance. Joueur et créatures : palette seule, à la part `entity_share` ; yeux intacts.
- Trois uniformes globaux (`world_grime`, `world_grime_paper`, `world_grime_ink`), neutres dans `project.godot` : le Hub n'en voit rien. `BiomeAtmosphere` publie la recette au chargement et le dosage du biome avec l'étalonnage, fondu compris ; remis à zéro en quittant la run.
- Recette dans la section `grime` de `data/world/atmosphere.json` (valeurs du dosage léger de la maquette), dosage `grime` dans chaque bloc `atmosphere` : 1 partout, 0,8 en forêt (elle perdait sa fraîcheur dans la maquette).
- Contrôles : `grime` hors borne par biome ; section absente, papier trop sombre, noir trop relevé, grain trop fort, saturation hors borne.

**Vérifié :**

- Captures `--capture-props --hide-collisions`, graine 1002, avant/après au même commit de base, regardées : [planche](planches/40-G1-avant-apres.jpg), [gros plans](planches/40-G1-gros-plans.jpg). Rendu conforme à la maquette légère : ville poisseuse, champs moins acides, marais vert-de-gris, carrière plus terne ; joueur lisible, grain discret au pixel.
- `tools/test_catalogs.sh` : 231 contrôles. `dotnet build` 0 warning, `tools/smoke_test.sh` vert, aucune erreur de shader en run.
- Premier essai : le shader du sol ne compilait plus (variable locale `grain` homonyme de son uniforme) ; renommée.

**Non mesuré :** coût GPU (un hachage et un bruit de valeur par pixel sur le sol, les décors et les canopées). Banc A/B prévu à la fin de G2.

## 6. G2 livré — 12 octobre 2026

**Fait :**

- `world_stains_at` dans `world_grade.gdshaderinc` : auréoles d'eau ou de thé (intérieur à peine plus sombre, bord marqué, le bleu baisse plus que le rouge) et plaques de moisissure, en coordonnées monde. Sol, routes et décors seulement ; ni canopées ni entités.
- Paliers nets tramés au pixel (Bayer 2×2, comme les nuages), auréole rompue par un bruit. Premier jet à bord lissé : un trait brun continu, net de près, qui se lisait comme un cheveu sur l'écran.
- Réglages `stains` (0,8) et `stain_scale_px` (110) dans la section `grime` ; force multipliée par le dosage du biome. Contrôles : force hors borne, taches minuscules.

**Vérifié :**

- Captures à la graine 1002, avant / G1 / G2 : [planche](planches/40-G2-avant-apres.jpg), [gros plans](planches/40-G2-gros-plans.jpg). De loin, les taches restent discrètes ; de près, marques d'eau séchée sur le marais et le pavé, taches de thé dans le blé.
- `tools/test_catalogs.sh` : 233 contrôles. `dotnet build` 0 warning, `tools/smoke_test.sh` vert, aucune erreur de shader en run.

**Banc A/B (G1 + G2 contre la base `42901716`), non concluant :** deux passes alternées de 15 s, machine calme pour la première (charge 1,3 à 2,6), chargée pour la seconde (13 à 22, une compilation en parallèle), écartée. Seule mesure propre : 1080p sans dash, 2,58 ms avant, 2,62 ms après. En 720p, +0,9 ms sans dash et +0,3 ms avec, mais un coût GPU grandirait avec la résolution, ce qui n'est pas le cas : bruit probable. À recouper machine calme.
