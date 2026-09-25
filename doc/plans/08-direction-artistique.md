# Plan 08 — Direction artistique, sprites, son et identité mémorable

Statut : **pixel art détaillé et homogène demandé ; contrat de production recommandé** · Priorité : cadrage immédiat, production progressive.
Références : [Charte](../CHARTE-GRAPHIQUE.md), Bible §3/4/6–11, V2 §5/19–21.

## 1. Intention

**Priorité précisée le 22 septembre :** tous les sprites de personnages sont à refaire. Produire avec 06 une référence commune pour un casting d’au moins cinq à six personnages, valider leurs identités et leurs sprites adaptés en jeu, puis reprendre les mobilités spécifiques de 01 E. Les assets actuels restent des placeholders pendant cette refonte ; aucun n’est considéré comme définitif.

Un monde reconquis par une nature lumineuse, dont les traces humaines se défont. Les objets portent une histoire ; les créatures manifestent une réalité fausse ; les personnages incarnent différentes façons de se souvenir. La puissance du combat et la mélancolie du monde doivent coexister.

Chaque contenu important doit relier **silhouette, comportement, son et fragment d'histoire**. Exemple déjà disponible : La Cloche de l'Institutrice possède un nom, une fonction et un passé implicite ; construire sa mise en scène autour de cette unité.

## 2. Phase 0 — Contraintes et écarts existants

- Charte : grille iso 2:1, palettes et contours teintés, tailles cibles, animation et nomenclature.
- Bible : monde beau et fragile, êtres asymétriques, yeux vert-acide, désagrégation ; lire avec l'amendement V2.
- Charte : jeu 480×270 ; projet actuel : 1920×1080 avec canvas_items ; HUD : référence 960×540. Les trois valeurs ne forment pas encore un contrat explicite cohérent.
- [CharacterSpriteLoader.cs](../../scripts/Combat/CharacterSpriteLoader.cs) charge les fichiers individuels par direction NE/NW/SE/SW et action idle/walk/dash/hurt/death, séquence contiguë 00 ou 01, plafond actuel 20 frames.
- Des assets Traqueur/Vagabond existent ; le nombre de PNG ne prouve pas la couverture de toutes les actions.
- [process_generated_sprite.py](../../tools/process_generated_sprite.py) vise un Traqueur 48×64 et utilise Lanczos puis palette : ce n'est pas encore un pipeline universel conforme à la charte.
- Les 19 Souvenirs narratifs et six constellations fournissent une base ; quatre récompenses « recipe » héritées doivent être réconciliées avec V2.

### Audit des sprites existants — 23 septembre 2026

Mesuré sur les PNG de frames `_01` :

| Famille | Dimensions constatées | Écart à la cible proposée |
|---|---|---|
| Traqueur | 22×22 ; NE/NW/SE/SW ; idle 4, walk 4, dash 3, hurt 2, death 4 | Environ trois fois moins haut que 48×64 ; format carré |
| Vagabond | 16×24 ; même couverture que le Traqueur | Même écart ; densité différente du Traqueur |
| Forgeuse | **Aucun sprite** (pas de dossier `assets/characters/forgeuse`) | Tout à produire |
| Ennemis courants | Ombre 12×12, Sentinelle 12×24, Hurleur 12×20, Brute 24×24, Rôdeur 42×42, Rampant 32×16/32×32, Tréant 32×32/64×64, Cracheur 32×32/64×64, Tisseuse 64×64 | Densités incohérentes : un facteur 5 entre l'Ombre et la Tisseuse, sans lien avec leur rôle |
| Colosses | 128×128 | Cohérent avec un mini-boss, densité à vérifier |

Conséquences :
- Aucun sprite actuel ne partage la densité de pixel cible. La refonte demandée par Raphaël concerne donc aussi la conversion d'échelle et le zoom de la caméra, pas seulement le dessin.
- `CharacterSpriteLoader` ne connaît que quatre directions (NE/NW/SE/SW). Le passage à huit orientations est un changement de code à planifier au lot B, après validation.
- Les perks exclusifs de la Forgeuse décrivent encore la construction V1 ; voir les [fiches du casting](06-fiches-casting.md).

**Préalables bloquants pour le lot A :**
1. Identités du casting validées (06 A).
2. Taille joueur 48×64 et nombre d'orientations (4 ou 8) confirmés.
3. Méthode de production choisie : dessin et retouche par qui, avec quels outils, place éventuelle de l'IA comme base retouchée (§4).

## 3. Décisions à valider avant production

| Décision | Proposition |
|---|---|
| Monde | Garder le pixel art isométrique, avec une référence de densité de détail validée en jeu |
| Interface | Texte net à l'échelle écran, avec exception UI explicitée dans la charte |
| Taille du joueur | Cible de travail recommandée 48×64, avec tuiles 64×32 déjà exploitées ; valider taille apparente en scène étalon |
| Directions | Viser huit orientations joueur pour le mouvement cardinal et diagonal ; prototype initial à quatre permis, conversion du chargeur à prévoir |
| Production | Références retouchées, palette/échelle communes, animation contrôlée ; automatisation de conformité/export |
| Ton | Nature lumineuse, mémoire, usure singulière ; menace par anomalie de réalité |
| Son | Impacts et récompenses précis, accalmies audibles, place au silence |
| Lore | Fragments courts, lecture au Hub et évolution narrative/visuelle ; indépendant des quêtes et accès de combat |

Le choix d'une nouvelle résolution ou de dimensions hors charte exige une modification explicite de celle-ci après validation. Aucun fournisseur, outil payant ou budget externe n'est engagé par ce plan.

## 4. Méthode de production

**Décision de Raphaël du 23 septembre :**
- Production **procédurale par scripts**, en visant la qualité maximale. Raphaël retouchera éventuellement ; le procédural doit suffire.
- Joueur en **48×64**, **huit orientations**.
- Personnage initial : le Vagabond.

Conséquences pour le pipeline :
- Les générateurs Python existants (`scripts/generate_*.py`, `tools/`) sont repris et unifiés en une bibliothèque commune : palette master, contours sel-out, lumière orientée, gabarits de proportions, poses clés, puis dérivation des frames.
- On n'écrit plus un script isolé par personnage avec ses propres conventions.
- Chaque sortie passe un contrôle automatique : dimensions, palette, pieds et pivot, frames manquantes. Elle est aussi rendue en contact sheet à taille réelle, pour relecture.
- Le pilote (§5 lot A) sert de juge. Si la qualité procédurale plafonne, les poses maîtresses peuvent être retouchées à la main et le script en dérive les frames.

La recommandation initiale ci-dessous reste utile pour ses contraintes (densité commune, scène étalon, contrôle), mais la méthode de fabrication est tranchée.

### Pilote livré — 23 septembre 2026

**Pipeline** (`tools/sprites/`) :
- Chaque personnage est un modèle 3D simplifié : capsules, ellipsoïdes et boîtes arrondies, avec soustractions (ouverture de capuche). Il est posé sur un squelette humanoïde en cinématique directe.
- Rendu par lancer de rayons en projection orthographique inclinée à 30°, soit la compression 2:1 des tuiles, à la résolution exacte de 48×64.
- Le suréchantillonnage 3×3 décide de la couverture, du matériau et du ton de chaque pixel.
- Passes pixel art :
  - quatre tons par matériau (ombres vers le froid, lumières vers l'or) ;
  - lumière fixe venant du haut-gauche ;
  - lignes internes sur rupture de profondeur ;
  - contour sel-out teinté, plus clair côté lumière ;
  - nettoyage des pixels orphelins.
- Les huit directions et toutes les animations découlent du même modèle. Environ 0,5 s par frame, 136 frames par personnage.

**Livrables :**
- Six modèles : Vagabond, Traqueur, Forgeuse, Éveillée, Facteur, Scaphandrière (cadre 32×48 après le retour d'échelle).
- Bibliothèque d'animations commune, modulée par l'allure : idle 4, walk 4, dash 3, hurt 2, death 4.
- Planche de casting (`tools/character_lineup.py`) : silhouettes noires, sol ancré et sol effacé, taille réelle.
- Intégration en jeu du Vagabond (qui n'avait jusque-là aucun `sprite_folder` et s'affichait en polygone), du Traqueur et de la Forgeuse (qui n'avait aucun sprite).

**Code :**
- `CharacterFacing` choisit parmi huit directions avec hystérésis, et revient aux quatre diagonales si un jeu de sprites ne les fournit pas.
- `CharacterSpriteLoader` charge E/SE/S/SW/W/NW/N/NE et vérifie l'existence des frames avec `ResourceLoader.Exists`. `FileAccess.FileExists` aurait échoué sur les textures remappées d'un export.
- `sprite_feet_offset` ancre les pieds sur la position au sol.
- Les planches et aperçus hérités du Traqueur, ainsi que la documentation 16×24 du Vagabond, sont retirés.

**Retour de Raphaël (23 septembre, capture en jeu) :** « le perso apparaît comme un géant, c'est un peu abusé ».
- Le modèle rendu à 1 px par unité mesurait environ 60 px de monde, contre 20 à 35 px pour les ennemis et les décors.
- Le pipeline a maintenant une échelle de rendu (`MODEL_SCALE` = 0,62) et un cadre de 32×48, avec les pieds en (16, 45). Les personnages font environ 35 px de haut, sac ou arc compris.
- Le suréchantillonnage passe à 4×4 pour conserver les détails fins à cette taille.
- La décision « 48×64 » est donc remplacée par une taille calée sur le monde. Les huit orientations restent.
- La planche réduite reste lisible : silhouettes distinctes, attributs reconnaissables.

**Retour de Raphaël (24 septembre, après une partie) :** design des nouveaux ennemis validé ; le personnage joué est « encore un peu trop grand » et « la version idle devrait être animée » (le souffle faisait moins d'un pixel, l'idle paraissait figé). Le redessin des personnages reste reporté.
- Échelle propre aux personnages : `CHARACTER_MODEL_SCALE` = 0,53, 15 % sous `MODEL_SCALE` = 0,62 que gardent les créatures. Cadre 32×40, pieds en (16, 36), `sprite_feet_offset` = 16 ; le cadre garde 3 px sous le pivot et 2 au-dessus du sac, sans rognage (l’ancien cadre coupait une ligne de contour sous les pieds). Hauteurs mesurées (vue SE, contour compris) : Vagabond 40 → 34 px avec le sac, Traqueur 38 → 33 px avec l'arc, Forgeuse 32 → 28 px ; corps seul autour de 30 px. Le Rôdeur (29 px) est désormais à la hauteur d'épaule du joueur.
- Idle des personnages (`living_idle`) : 6 frames à 5 fps (cycle de 1,2 s), sinusoïdes qui bouclent sans à-coup. Le souffle lève épaules et tête d'un à deux pixels, les bras s'ouvrent, le poids se balance latéralement, les jambes restent immobiles. Un paramètre `drape` anime en retard d'un quart de cycle un élément souple par modèle : écharpe, pointe de capuche et manche d'outil du Vagabond ; pointe de capuche et empennages du Traqueur ; marteau qui se cale sur l'épaule et braise qui palpite chez la Forgeuse. Les créatures gardent l'idle commun.
- Régénérés : Vagabond, Traqueur, Forgeuse (les seuls intégrés en jeu). Les frames idle 05-06 s'ajoutent ; le générateur retire désormais les PNG orphelins d'un ancien nombre de frames.

**Points ouverts pour Raphaël :**
- **Lisibilité du Traqueur** : vert forêt de la charte sur la forêt, il se fond dans le décor en jeu. Pistes : cape plus sombre ou plus désaturée, accent beige plus présent, contour plus contrasté.
- **Ennemis** : ils restent à l'ancienne échelle, minuscules à côté des personnages. Prochaine étape : modèles ennemis dans le même pipeline, avec créatures asymétriques et yeux vert-acide (Bible §6.2). Pilote de trois créatures livré le 24 septembre (ci-dessous).
- **Charte** : à amender après validation (joueur 48×64, huit directions, abandon de la résolution interne 480×270).
- **Double contour** : le shader d'entité ajoute un contour au sel-out déjà peint. À comparer en jeu avec et sans.

### Pilote ennemis — 24 septembre 2026

Trois créatures du début de run passent dans le pipeline, à la même densité de pixels que les personnages (`MODEL_SCALE` commun). Choix : elles figurent dans les pools de début de tous les biomes (Présage, Rôdeur) ou de deux d'entre eux (Charognard), et couvrent trois gabarits (lanceur flottant, bipède, quadrupède).

**Pipeline :**
- `tools/sprites/creatures/` : un module par créature, avec un gabarit libre (état d'animation → volumes). Le Rôdeur réutilise le squelette humanoïde ; le Charognard et le Présage ont leur propre gabarit.
- Matériaux émissifs (`make_emissive`) pour les yeux vert-acide : ils l'emportent sur un pixel dès un quart des échantillons, ne sont ni ombrés ni cernés. Un œil d'un pixel reste visible.
- `tools/sprites/models.py` unifie personnages et créatures ; `tools/generate_enemy.py <id> [--sheet]` écrit 8 directions × idle/walk/attack/death (16 frames par direction) et remplace les anciens PNG du dossier.
- Les anciens générateurs `scripts/generate_rodeur*.py` et `generate_charognard.py` sont retirés : ils auraient écrasé les nouveaux sprites.

**Créatures :**
| Créature | Cadre, pieds | Lecture recherchée |
|---|---|---|
| Rôdeur | 40×48, (20, 45) | Bipède voûté, bosse moussue, bras gauche trop long à griffes d'os, poing de pierre à droite, yeux décalés dans un visage creux. Taille proche du joueur. |
| Charognard | 32×32, (16, 24) | Corps bas, six pattes inégales aux genoux relevés, moignon de tête fendu d'une gueule, deux yeux à hauteurs différentes. Bond : corps étiré, gueule ouverte. |
| Présage | 32×48, (16, 45) | Voile lilas qui lévite, colonne de trois yeux dans une capuche vide, éclats de pierre en couronne. Un fil à plomb pend dessous ; à l'incantation, le poids se lève vers la cible et luit du vert de la marque au sol. |

**Code :**
- `EnemySpriteLoader` charge les huit directions, les anciens jeux à quatre diagonales restant acceptés. `Enemy` utilise `CharacterFacing` (hystérésis, repli sur les diagonales) et une table de `StringName` précalculée : plus de chaîne allouée à chaque frame par ennemi.
- À l'arrêt pendant une attaque (incantation, bond annoncé), l'ennemi se tourne vers le joueur.
- `visual.sprite_feet_offset` (JSON) ancre les pieds et garde l'échelle 1. Les anciens sprites gardent leur décalage centré.
- Constat : la mise à l'échelle « taille JSON » des anciens sprites ennemis n'a jamais eu d'effet, car l'échelle était remise à 1 juste après dans `Initialize`. Ce code mort est retiré ; le rendu des anciens sprites ne change pas.

**Vérifications :**
- Build sans warning ; `EnemyAbilityRegression` 15/15 ; `MovementRegression` sans échec.
- Nouveau mode `RunObservation --capture-bestiary` : gros plans des trois créatures autour du joueur dans la vraie scène de run. Les trois chargent leurs 32 animations. Rôdeur et joueur ont une taille comparable, le Charognard reste petit mais lisible, les yeux vert-acide ressortent sur la forêt.
- Le test de capacités attendait encore trois marques de Présage simultanées alors que le JSON en autorise deux depuis le 23 septembre : il lit désormais le plafond dans les données.

**Points ouverts pour Raphaël :**
- Validation en jeu des trois silhouettes, de leurs couleurs et de leurs animations (surtout l'incantation du Présage et le bond du Charognard).
- Le Présage vu de dos est plus sombre ; à surveiller sur sol effacé.
- Le « chef » de meute du Charognard (un œil de plus, Bible §6.2) n'existe pas en jeu ; non modélisé.
- Après validation : même traitement pour les autres ennemis du début (Rampant d'Ombre, Brute, Rampant, Cracheur).

### Décors procéduraux — chantier du 25 septembre 2026

**Demande de Raphaël :** refonte visuelle des décors dans le pipeline procédural (`tools/sprites`), à la même densité de pixels que les personnages et les ennemis, biome par biome, en commençant par l'urbain.

**Constat (audit du [plan 10 §7](10-terrain-et-tiles.md#7-audit-des-décors--25-septembre-2026)) :** les décors actuels sont dessinés à une densité bien plus faible. Une benne fait 12×12 px, une voiture 32×16, alors que le joueur mesure environ 30 px de haut. Ils se fondent dans des sols très bruités, sans ombre de contact. Les immeubles (≈ 120×90) sont les seuls à une échelle crédible.

**Contrat proposé :**
- Même rendu que les créatures : modèle 3D simplifié, lancer de rayons orthographique à 30°, `MODEL_SCALE` = 0,62, quatre tons par matériau, lumière haut-gauche, contour sel-out teinté. Un décor devient un modèle, pas un dessin.
- Échelle réelle : une voiture ≈ 2,5 personnages de long, une benne à hauteur d'épaule, un immeuble de deux étages ≈ 4 personnages.
- Lisibilité sur le sol : valeur plus claire ou plus sombre que le sol du biome, ombre de contact intégrée, accents de couleur rares pour les objets repères.
- Le générateur écrit aussi l'**emprise au sol** de chaque décor (losange iso). Le jeu s'en sert pour la collision ; le calcul par pixels du lot 10 D1 reste le repli.
- Variantes par seed (usure, rouille, végétation) plutôt que des fichiers copiés.

**Lots :**

| Lot | Contenu | Validation |
|---|---|---|
| **P0 — Gabarit décors** | Module `tools/sprites/props/`, manifeste (dimensions, pieds, emprise, variantes), planche de contact à taille réelle sur les sols des cinq biomes | Planche et une capture en jeu |
| **P1 — Mobilier urbain** | Voitures (2 ou 3 carrosseries), bennes, feux, cabine, lampadaire, barrières, boîte aux lettres, panneaux, débris et poutrelles | Captures en vraie run, ville dense ; ton retour |
| **P2 — Immeubles urbains** | Modules d'immeubles (façade, angle, tour, effondré, église) à l'échelle du personnage, intérieurs visibles par les brèches | Idem, collisions et profondeur (plan 10 D1/D2) |
| **P3 à P6** | Forêt, champs, marais, carrière, dans cet ordre sauf avis contraire | Un biome validé avant le suivant |

Les tiles restent en l'état (jugées acceptables) ; un ajustement de contraste reste possible si les nouveaux décors l'exigent.

**P0 et P1 livrés — 25 septembre 2026 :**
- `tools/sprites/props/` : kit commun (`_kit.py`) et mobilier urbain (`urban.py`). `tools/generate_props.py urban [--sheet]` réécrit uniquement les fichiers du catalogue ; les `.import` (et leurs uid) sont conservés.
- Même rendu que les créatures (`MODEL_SCALE`), à l'échelle réelle : 1 m ≈ 27,5 unités, un humain ≈ 30 px. Cadre ajusté automatiquement, centré sur le point au sol. Portée de rayon paramétrable dans `render()` pour les grands volumes, nouvelle primitive `cylinder`.
- Orientation : le TileSet iso est en disposition « stacked », donc les rues sont horizontales ou verticales à l'écran. Vus exactement de profil, les décors perdaient leur volume (un grillage devenait un trait). Chaque axe prend donc un trois-quarts proche : 62° pour une rue horizontale, 22° pour une rue verticale.
- Usure reproductible par graine : taches de rouille affleurantes, mousse, carrosserie affaissée, feu penché.
- 19 fichiers : voiture en trois teintes et deux axes (le placeur choisit l'axe de la route et la teinte par hash de cellule), deux bennes (fermée ; ouverte avec sacs), feu tricolore, cabine, boîte aux lettres, grillage, panneau d'affichage, trois gravats, deux poutrelles. Tailles typiques : voiture 76×37 px, benne 40×37, feu 26×54. Gravats et poutrelles sont partagés avec la carrière.
- Placement : au moins une cellule libre entre deux décors de rue ; fréquence au bord des immeubles 18 % → 9 %, aux carrefours 25 % → 20 %.
- Captures `--capture-props --hide-collisions` (seed 1002) : chaque objet se lit à l'échelle du personnage. Défaut restant : les immeubles actuels s'empilent et débordent sur la chaussée, c'est l'objet de P2.

**P2 livré — 25 septembre 2026 :**
- **P2a, emprise exacte.** Le générateur écrit `props_manifest.json` dans le dossier du biome : pour chaque décor, le point au sol dans le sprite et l'emprise projetée (polygone en pixels). `PropManifest` le lit ; `EnvironmentProp` pose alors le pivot sur le centre de la cellule et reprend ce polygone pour la collision, le tri et l'ombre. Les anciens décors dessinés gardent l'emprise déduite des pixels.
- **P2b, immeubles.** Module `tools/sprites/props/buildings.py`, dimensionné en cellules (une cellule ≈ 3,7 m, un rang ≈ 1,9 m) :
  - Styles : immeuble de rapport (largeur 4 ou 5 cellules, intact ou abîmé), commerce (3 ou 4 cellules, vitrine et store rayé), maison (toit à deux pans) et ruine (angle effondré). Tous ont 2 étages et 4 rangs de profondeur, et chacun existe en deux lacets (±12°) : 16 fichiers `prop_bld_<style>_w<largeur>_<a|b>`.
  - Hauteur bornée par la profondeur d'un îlot à l'écran (≈ 190 px). Un premier essai à 3 étages (≈ 240 px) masquait la rue au nord et les ennemis qui s'y trouvaient.
  - Vus presque de face : la façade suit la rue, un liseré de côté donne le volume, le toit reste lisible. Un lacet de 62° comme les voitures aurait donné des rues en dents de scie.
  - Fenêtres par répétition de domaine (coût constant) : une part condamnée par des planches selon l'état, appuis, bandeaux d'étage, corniche. Toit : dalle, garde-corps, mousse, flaques, édicules, arbuste sur les immeubles abîmés. Murs : crasse au pied, coulures, lierre. Ruines : effondrement déchiqueté, intérieur sombre, gravats au pied.
  - Rendu accéléré par boîte englobante (les rayons démarrent à son entrée) et suréchantillonnage 3×3 : 20 à 50 s par immeuble.
- **Placement** (`UrbanBuildingPlacer`, extrait d'`UrbanPropPlacer`) : une seule rangée de modules, le long de la rue sud de chaque îlot. Elle s'élève au-dessus de la cour, jamais d'une rue ; une seconde rangée au nord recouvrait la première. Ruelles d'une cellule (22 %). Le style suit l'intégrité de l'îlot : ruines, immeubles abîmés, commerces 30 %, immeubles 55 %, maisons 15 %. Les modules disponibles sont lus dans le manifeste. L'ancienne logique de façades et d'angles et les huit sprites d'immeubles hérités (dont l'église et l'antenne) sont retirés.
- Vérifié : build sans avertissement, smoke test, `MovementRegression`, captures en vraie run (seed 1002). Combat dense : ≈ 110 FPS, p99 ≈ 13,5 ms, en 720p comme en 1080p (≈ 120 FPS avant les immeubles ; la zone du banc n'est pas forcément urbaine).
- Points ouverts : église et antenne à refaire dans le pipeline comme repères rares ; brèches des ruines encore anguleuses ; façades nord et rues verticales moins soignées que les façades sud.

### Recommandation initiale (22 septembre), remplacée pour la méthode

**Choix recommandé : pixel art dessiné et animé à une densité commune, produit à partir d’une scène étalon, avec retouche contrôlée et pipeline automatisé.** L’IA peut aider aux recherches de silhouettes/matières ou à une base de sprite, mais chaque résultat doit être redessiné/normalisé selon les mêmes références. Des générations indépendantes « pixel art détaillé » ne constituent pas une méthode de cohérence ; générer chaque frame indépendamment n’est pas le pipeline recommandé pour les personnages.

### Contrat candidat

- Conserver l’isométrie 2:1 et partir des **tuiles 64×32 réellement utilisées**, plutôt que prétendre les introduire. Proposer joueur 48×64, ennemis de tailles adaptées au rôle et boss redessinés plus grands ; un petit ennemi et un boss partagent la même taille de pixel de dessin.
- Point de départ : un pixel source = une unité de dessin du monde avant zoom commun. Auditer les scales par asset (notamment ennemis) pour éviter un sprite de 12 px agrandi à côté d’un sprite très détaillé. Ajuster la caméra globalement après vérification des proportions et conserver les dimensions physiques cohérentes.
- Pas de résolution basse universelle forcée à 480×270. Recommander le rendu du monde à la résolution de fenêtre avec filtrage nearest des sprites, caméra/échelle maîtrisées, UI à résolution native et polices nettes. Tester 720p/1080p/1440p : si le mouvement révèle des irrégularités de pixels, comparer un viewport monde dédié avec scaling cohérent, sans arrondir la physique. Ce point technique doit être résolu sur la scène étalon avant de modifier la Charte.
- Palette master, sous-palettes par biome, contours teintés de même épaisseur apparente, lumière orientée commune, ombres au sol cohérentes. Détails organisés en formes/matières ; pas de bruit aléatoire pour simuler la richesse.
- Pieds/pivots stables, projection commune, couverture d’animation, timings compatibles mobilité. Les huit directions du joueur sont un objectif de production ; le chargeur à quatre directions doit être adapté après validation. Miroir uniquement si costume et arme restent cohérents.
- Une règle d’exception documentée pour VFX et UI : leur douceur éventuelle ne sert pas à mélanger plusieurs résolutions de sprites.

### Chaîne de production proposée

1. Dessiner/retoucher les poses maîtresses du personnage, de l’arme et de deux ennemis sur la même planche, plus coffre, trois props, deux sols.
2. Valider palette, silhouette et taille en jeu ; figer un « kit de référence » versionné (images sources, captures normales, pivots, règles d’échelle et couleurs).
3. Animer à partir des poses, avec cohérence de volumes ; exporter les frames selon le manifeste. Les outils servent à assembler, indexer et contrôler, pas à inventer une nouvelle DA par asset.
4. Réviser l’outillage existant : éviter que redimensionnement Lanczos/contraste crée des couleurs ou pixels incohérents ; choisir la méthode de conversion en fonction de la source, puis contrôle manuel à taille réelle.
5. Contrôler chaque famille dans un contact sheet et dans la même séquence de jeu, avant intégration généralisée.

**Arbitrage de capacité :** réaliser ce pilote permet d’estimer la charge de retouche/animation. Si le niveau visé ne peut pas être maintenu par le pipeline interne, préparer un brief pour assistance artistique sur ce kit ; aucun fournisseur ni dépense ne sont engagés ici. La qualité du pilote décide de la méthode finale, pas le volume généré.

## 5. Lots d'action

### Lot A — Référence artistique jouable

1. Constituer une planche à partir du lore, palettes et assets du projet : vivant/effiloché/Néant, personnage, arme, menace, UI.
2. Produire le pilote recommandé 64×32/48×64 avec une seule variante de repli si la taille apparente échoue ; comparer l’ensemble joueur/ennemis/sol, pas un portrait isolé.
3. Comparer à zoom normal, en mouvement et en combat dense ; inclure capture 720p/1080p.
4. Fixer résolution du monde, rendu de l'interface, dimensions, pivots, ombres, orientations, contraste.
5. Mettre à jour la charte approuvée, en distinguant règles du monde et UI.

**Vérification :** silhouette repérable immédiatement, style cohérent, pas de flou/aliasing involontaire ; validation explicite de Raphaël.
**Garde-fou :** ne pas lancer la production de tout le bestiaire avant ce choix.

### Lot B — Contrat et contrôle des assets

1. Reprendre les conventions réellement acceptées par `CharacterSpriteLoader.LoadOrGet` et les chargeurs ennemis.
2. Établir un manifeste par entité : dimensions, ancrage pieds, ombre, palettes, directions/actions, frames, FPS, chemins, source.
3. Adapter l'outillage de conversion et vérifier transparence, alpha, palette, dimensions et séquences manquantes.
4. Prévoir une scène de revue des animations et un aperçu à taille réelle dans chaque biome.
5. Conserver sources éditables, provenance/licence et exports reproductibles.

**Vérification :** zéro frame manquante attendue, chargement sans fallback involontaire, stabilité des pieds et ombres.
**Garde-fou :** une feuille de sprites n'est pas automatiquement consommée par le chargeur ; aucun .import édité à la main.

### Lot C — Production priorisée

| Ordre | Livrable | Recette spécifique |
|---|---|---|
| 1 | Personnage principal complet | Déplacement/attaque/dégât/mort lisibles, silhouette propre |
| 2 | Trois armes de référence et attaques | Identité de rythme, portée et matériau perçue |
| 3 | Ennemis fréquents et leurs annonces | Rôle reconnu, fenêtre d'esquive visible |
| 4 | Coffres, Autels, XP, Essence, loot | Valeur et possibilité d'interaction compréhensibles |
| 5 | Autres personnages, ennemis forts, boss | Cohérence du casting et échelle de menace |
| 6 | Décor secondaire, ambiance, Hub évolutif | Profondeur sans parasiter combat/navigation |

Valider chaque famille dans une séquence de jeu avant la suivante. Réutiliser les palettes/pivots de référence, sans réutiliser la même silhouette pour des rôles opposés.

**Vérification :** revue du manifeste, vue en mouvement, cohérence des animations à plusieurs vitesses d'attaque.
**Garde-fou :** silhouette décorative ou animation spectaculaire ne doit pas modifier silencieusement hitbox/portée.

### Lot D — Identité narrative et sonore

1. Écrire une fiche par personnage clé : désir, trace de passé, geste, couleur/motif, arme et lien à une constellation.
2. Relier un ensemble pilote : personnage, Cloche de l'Institutrice, lieu, ennemi et fragment de lore, en validant la cohérence avec la Bible.
3. Écrire des textes courts (2–5 phrases), concrets et humains ; préserver les ambiguïtés du monde.
4. Reprendre `SouvenirManager.DiscoverSouvenir`, `SouvenirDiscovered` et le journal ; retirer les récompenses gameplay intermédiaires après migration 06. Garder les découvertes, textes, constellations et gains narratifs/visuels du Hub.
5. Définir motifs audio des récompenses et des phases, en reprenant pools/throttling d'AudioManager.
6. Montrer une première évolution du Hub liée à une découverte persistante.

**Vérification :** découverte rapide et lisible pendant fuite ; fragment retrouvé au Hub ; récompense unique ; motifs reconnaissables sans saturation.
**Garde-fou :** ni longue lecture obligatoire sous pression, ni retour aux Foyers/construction/jour-nuit.

## 6. Recette et acceptation

Présenter une séquence continue : préparation personnage → exploration → combat → Effacement → récompense → mort/retour Hub. Demander quels éléments restent en mémoire et pourquoi.

Acceptation : Raphaël reconnaît une identité cohérente dans ces moments ; les joueurs distinguent personnages/armes/menaces ; les silhouettes et informations critiques restent lisibles avec effets réduits.

Build, smoke dès que scènes/shaders/initialisation sont touchés, profilage des effets ; tests visuels sur assets et palettes. Roadmap F et parties D/E/G ; ne pas confondre conformité de fichier et qualité artistique.


La cohérence du sol et des transitions relève aussi du [plan 10](10-terrain-et-tiles.md). Validation finale de la DA sur une planche commune et une séquence en mouvement : même pixel apparent, même projection, palettes compatibles, détails lisibles, aucune famille semblant importée d’un autre jeu.
