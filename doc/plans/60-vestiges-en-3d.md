# Plan 60 — Vestiges en 3D

Suite du [plan 59](59-verticalite.md) (l'essai de la verticalité), sur les décisions [§228](DECISIONS.md) et [§229](DECISIONS.md). Écrit le 15 novembre 2026, avant toute ligne de code.

## 1. Ce que Raphaël a décidé

- Le relief prend, en relief 2D comme en 3D : « c'est dingue le jeu change totalement de dimension » (§226).
- **Tout le jeu passe en 3D, la logique comprise**, plutôt qu'un pont de rendu : « il faut revoir tout en 3D au lieu de faire un pont meme si ca coute encore plus » (§228).
- **La caméra tourne**, et les bâtiments ont des volumes (§229).
- **Le low/mid poly remplace le pixel art**, à essayer d'abord : « je serai chaud pour changer la vue "pixel art" en une vue "low/mid poly" » (§229).
- **La caméra se valide vite** : « il faudra assez rapidement valider une caméra » (§229).

## 2. L'état des lieux

Le code C# de la run fait 92 000 lignes. Par dossier, la part qui dépend de la 2D :

| Dossier | Fichiers | Lignes | Dessinent en 2D | Physique 2D | Dessin à la main | Ce que c'est |
|---|---|---|---|---|---|---|
| World | 117 | 25 400 | 70 | 19 | 14 | Génération, décors, Effacement, Mémoriaux, gardiens, ambiance |
| Infrastructure | 113 | 18 900 | 4 | 0 | 2 | Chargeurs JSON, sauvegarde, audio, Steam, langues, entrées |
| UI | 80 | 17 200 | 10 | 0 | 38 | HUD, Hub, menus, cartes |
| Combat | 100 | 14 800 | 48 | 4 | 21 | Créatures, armes, projectiles, effets, Indicible |
| Core | 37 | 5 800 | 11 | 1 | 3 | Joueur, GameManager, EventBus |
| Progression | 35 | 5 800 | 8 | 0 | 5 | Perks, quêtes, Essence, Péril |
| Events, Spawn, Meta, Score | 21 | 4 100 | 5 | 1 | 1 | Résurgences, apparitions, Souvenirs, records |

Ce qu'on en tire :

- **La moitié du code ne bouge pas, ou presque.** Infrastructure, Progression, Meta, Score, Events et Spawn sont de la logique et des données. Sauvegardes, quêtes, Campement, classement, audio et langues restent tels quels.
- **L'interface reste en 2D.** HUD, Hub, menus, cartes de niveau et minimap se dessinent par-dessus la 3D comme aujourd'hui. Seuls les éléments posés dans le monde changent : plaques des créatures, chiffres de dégâts, invites.
- **La génération se garde en grande partie.** Terres, chemins, pièces faites main et terrain rythmé travaillent sur une grille de cellules. Seule la sortie change : `BiomeTileMapper` (940 lignes) et `RoadTileGenerator` (850) posent des tuiles ; ils seront remplacés par un sol en maillage. Les placeurs de décors posent des sprites ; ils poseront des modèles.
- **Le cœur à réécrire**, ce sont le joueur (`Player.cs`, 2 600 lignes), les créatures (`Enemy.cs`, 2 100), les projectiles, les effets (21 fichiers dessinent à la main) et les 34 shaders, tous écrits pour la 2D.
- **Les réglages sont en pixels.** 129 clés `_px` dans `data/`, et des centaines de portées, vitesses et rayons. On les convertit d'une passe, avec un facteur unique, vérifiée par les régressions.
- **Les personnages et les créatures existent déjà en volumes 3D.** Le pipeline de sprites (`tools/sprites/`) les modélise par des formes 3D (sphères, capsules, ellipsoïdes) montées sur un squelette, puis les pose et les photographie en pixel art. Les mêmes modèles peuvent devenir des maillages low poly, squelette compris, sans tout redessiner. Les décors passent par le même pipeline. C'est l'atout du portage.
- **Jolt est déjà le moteur physique 3D** du projet.
- **Le rendu est en GL Compatibility**, choisi pour la 2D. Pour une 3D éclairée, avec ombres et brouillard, Forward+ est à comparer.

## 3. La forme, défauts de l'agent réversibles

- **Une run 3D neuve, à côté de l'actuelle.** Une scène `Main3D` ; la run 2D reste jouable et testée jusqu'à ce que la 3D l'égale, puis elle part en un lot.
- **Gel des nouveautés sur la run 2D** pendant le portage : seulement des corrections. Sinon chaque nouveauté serait faite deux fois.
- **Le sol est carré et l'unité est le mètre.** Positions en `Vector3`, Y vers le haut. L'échelle exacte (taille du joueur, d'une cellule, d'un étage) se règle en 60.0.
- **La horde n'a pas de corps physiques.** Comme aujourd'hui et comme dans le bac à sable : grille de voisinage, `TickRoster`, collisions par la carte de hauteurs et les emprises des décors. Le joueur et le monde statique peuvent passer par Jolt, à mesurer.
- **Les projectiles ignorent les étages**, et le contact ne touche qu'à hauteur égale (règles du bac à sable, plan 59 §8).
- **Le style par défaut à essayer : low poly « crade »**, qui garde l'esprit de la DA (mystérieux, crade, indescriptible) : textures basse résolution, palette réduite, brouillard, tramage, ombrage par faces.
- **Les modèles naissent du pipeline actuel.** Le volume de chaque pose est maillé en low poly : un maillage par pose, comme une planche de sprites, joués à la cadence des sprites (§8, 60.0a). On garde le squelette et les poses déjà écrits. Les volumes faits main d'un artiste peuvent s'y substituer plus tard, un par un.
- **Un décor qui cache le joueur s'ouvre.** Avec une caméra qui tourne, un mur ou un immeuble peut masquer le joueur ; il devient transparent ou se coupe. `PropOcclusion` a déjà l'index spatial pour ça.

## 4. Les lots

| Lot | Contenu | Vérification | Effort |
|---|---|---|---|
| **60.0 La caméra et le style** | Bac à sable : arène 3D low poly, la horde, le facteur et deux créatures tirés du pipeline SDF, quelques décors. Caméras à comparer, deux ou trois styles, Compatibility contre Forward+. Détail au §7 | Captures caméra × style ; banc de 120 créatures, machine calme. **Raphaël choisit la caméra et le style** | Élevé |
| **60.1 Les fondations** | Scène `Main3D`, unités et conversion des réglages en mètres, le joueur 3D (marche, dash devenu bond, hauteur, collisions), la caméra choisie, les entrées | Suite de mouvement portée ; contrôle des rebords | Élevé |
| **60.2 Les créatures** | Déplacement, voisinage, `TickRoster`, `EnemyPool`, montée des falaises, capacités, statuts, gardiens | Suite des capacités ennemies portée ; banc dense | Élevé |
| **60.3 Le combat** | Armes, projectiles, touches, orbes d'XP, butin, `CombatPools`, Élan, Série, percussion. **La run 3D se joue, à plat**, sur une terre d'essai | Suite des armes portée ; une run jouée | Élevé |
| **60.4 Le monde** | Le sol en maillage par tronçons, les hauteurs nées de la génération, chemins et routes, décors en instances groupées, le vide et l'Effacement, l'ouverture des décors qui cachent. **Le relief entre dans les terres**, les bois d'abord, la carrière née en relief | `tools/measure_run.sh` porté ; captures ; banc. À redécouper par terre | Très élevé |
| **60.5 Les lieux de la run** | Mémoriaux, repaire et gardiens, coffres, Failles, Indicible et Montée, Résurgences | Régressions de la cartographie, de l'Effacement, du changement de terre | Élevé |
| **60.6 Les effets et l'interface du monde** | Effets d'impact, chiffres, plaques, invites, brouillard, shaders de l'Effacement et du vide, `ShaderWarmup` | Captures avant/après de chaque effet ; banc | Élevé |
| **60.7 L'art** | Les six personnages jouables et les habitants, les 16 créatures, les 197 décors, les sols des cinq terres en low poly, par le pipeline. En partie en parallèle des lots 60.1 à 60.6 dès que 60.0 a fixé le style | Planches de captures ; le bestiaire en vraie run | Très élevé |
| **60.8 La bascule** | Toutes les régressions, la mesure au bot et les captures sur la run 3D ; la run 2D part ; les outils (`capture_run`, bancs, `measure_run`), `AGENTS.md`, l'architecture et la charte réécrits | Validation globale `tools/validate.sh` verte | Moyen |

## 5. Ordre, risques et arrêt

**Ordre** : 60.0 d'abord, seul. Puis 60.1 à 60.3 à la suite : la run 3D devient jouable, à plat, sur une terre d'essai. Puis 60.4 et 60.5, qui portent les terres et la boucle complète. Enfin 60.6 et 60.8. L'art (60.7) avance à côté dès 60.0.

**Ce qui attend** : la Montée (étape 6), la carrière T4, les gardiens G3 à G5. Ils se feront directement en 3D.

**Les risques, du plus lourd au plus léger** :
1. **Animer seize créatures, six personnages et les habitants.** Chacun a de 16 à 40 animations en sprites. Les squelettes et les poses du pipeline existent, mais leur passage en maillages animés n'a jamais été fait. 60.0 le prouve sur trois corps.
2. **Le coût d'une horde en 3D**, surtout en Compatibility et sur macOS : 120 créatures animées, des projectiles, des milliers de décors. Mesuré en 60.0, avant tout portage.
3. **La lisibilité avec une caméra qui tourne** : où est le nord, qui est derrière un mur, que voit-on de la horde. Jugé en 60.0.
4. **Le low poly qui fait « pas cher »** au lieu de « crade et mystérieux ». C'est un risque d'art, jugé sur les captures et en jeu en 60.0.
5. **La durée.** Le jeu n'a pas de nouveauté de fond pendant tout le portage. La run 2D reste jouable pour qu'il ne soit jamais cassé.

**Arrêt** : si, à la fin de 60.0, ni la caméra ni le style ne convainquent Raphaël, ou si la horde ne tient pas 60 images par seconde, on le dit avant 60.1. Le repli est le relief en 2D pure (plan 59 §4, option A).

## 6. L'avocat du diable

- **Les survivors-likes vivent très bien en 2D.** Le relief a pris dans le bac à sable sur quinze minutes de jeu, pas sur une run entière. Réponse : c'est pour ça que la run 2D reste jouable, et que 60.0 et 60.3 sont des points de jugement.
- **On jette des mois de pixel art** : 6 000 images, 197 décors, les bestiaires animés. Réponse : on garde les modèles, qui étaient déjà en 3D, et l'essentiel du code de jeu. On perd le rendu, pas la conception.
- **Le chantier double le travail de chaque nouveauté** tant que les deux runs coexistent. Réponse : gel des nouveautés sur la run 2D.
- **Une caméra libre change l'équilibrage** : portée de vue, tirs venus de dos, densité. Réponse : la caméra se choisit en premier, et l'équilibrage se fait sur la run 3D, pas transposé.

## 7. 60.0, la caméra et le style — découpage proposé

Le bac à sable de la verticalité (`tools/relief_sandbox.sh`) sert de base : sa simulation (horde, bond, étages, contrôle des rebords) et son arène sont gardées, seul le rendu change.

| Sous-lot | Contenu | Vérification | Effort |
|---|---|---|---|
| **60.0a Les corps en low poly** | Un exporteur Python qui transforme un modèle du pipeline en maillage low poly : une pièce par forme, attachée à son os, couleurs de la palette par face. Le facteur, le rampant, le rôdeur ; un arbre, une maison, un rocher. Export glTF, animations de marche et d'attaque tirées des poses existantes | Planche des six modèles, de face et de dos ; ils s'importent dans Godot sans avertissement | Élevé |
| **60.0b L'arène low poly** | Sol et falaises de l'arène en maillage low poly, ombrage par faces, les modèles à la place des sprites ; le relief et la horde du bac à sable | Captures ; contrôle des rebords toujours vert | Moyen |
| **60.0c Les caméras** | Caméra qui tourne (Q/E et souris), sous trois réglages à comparer : isométrique haute (orthographique ou faible perspective), 3D de haut rapprochée, poursuite. Réglages dans le JSON du bac à sable | Captures de chaque caméra, au milieu de la horde et au pied d'un mur | Moyen |
| **60.0d Les styles** | Deux ou trois styles basculés d'une touche : low poly crade (textures basse résolution, tramage, brouillard), aplats nets, mid poly éclairé | Planche caméra × style | Moyen |
| **60.0e Le moteur de rendu** | Le même banc en Compatibility et en Forward+ : 120 créatures animées, projectiles, décors | Banc A/B machine calme, images par seconde et temps d'image | Faible |
| **Séance de Raphaël** | Il joue chaque caméra et chaque style | Choix de la caméra, du style et du moteur, consignés | Ton temps |

Ce que 60.0 doit trancher, à la fin :
1. **la caméra** : son angle, sa distance, orthographique ou en perspective, sa portée de vue ;
2. **le style** : crade, aplats ou mid poly ;
3. **le moteur de rendu** : Compatibility ou Forward+ ;
4. **l'échelle** : taille du joueur, d'une cellule, d'un étage.

## 8. 60.0a livré — les corps en low poly

**Ce qui est fait.**
- `tools/sprites/lowpoly.py` : le volume SDF d'une pose est échantillonné sur une grille grossière puis maillé par « surface nets » (un sommet par cellule traversée, une face par arête traversée). Faces non lissées, une couleur par face : le ton de base (troisième) de la rampe du matériau qu'elle recouvre. Les matériaux émissifs (l'œil du rôdeur) vont dans une surface à part.
- `tools/export_lowpoly.py` : sans argument, il exporte le jeu d'essai (facteur, rampant, rôdeur, arbre moyen, maison `prop_bld_house_w3_a`, rocher moussu) dans `assets/models/{characters,creatures,props}/`. Chaque modèle donne un `.glb` et un manifeste `.json` (animations, poses, cadence de 8 images par seconde, triangles). Unités : le mètre, Y vers le haut, l'avant vers +Z.
- `tools/model_sheet.sh <dossier>` (scène `tools/tests/ModelSheet.tscn`) : importe chaque `.glb`, le montre de face, de trois quarts, de profil et de dos, puis chaque animation pose par pose, puis tous côte à côte à leur vraie taille.

**Mesures.**

| Modèle | Poses | Triangles au plus | Hauteur |
|---|---|---|---|
| facteur | 27 (idle, walk, dash, hurt, death) | 3 204 | 1,58 m |
| rampant | 16 | 1 644 | 0,49 m |
| rôdeur | 16 | 2 648 | 1,88 m |
| arbre moyen | 1 | 6 400 | 5,06 m |
| maison | 1 | 10 624 | 8,40 m |
| rocher moussu | 1 | 440 | 0,92 m |

Les échelles relatives sont celles des sprites : le facteur arrive à l'épaule du rôdeur, et la maison fait cinq hommes de haut. Tout s'importe dans Godot sans avertissement.

**Écart au découpage.** Le §3 prévoyait une pièce low poly par forme, attachée à son os. Ce n'est pas faisable sans réécrire les modèles, car une forme du pipeline est une fonction de distance opaque : on ne peut pas lire qu'elle est une sphère de rayon 3 sur l'avant-bras. Le maillage par pose reprend donc les poses existantes telles quelles, sans rien redessiner. Le prix : il n'y a pas d'interpolation entre deux poses (8 images par seconde, comme les sprites), et la mémoire croît d'environ 30 Ko par pose. Si une animation fluide devient nécessaire, il faudra exposer la nature des primitives dans `sdf.py`, ce qui touche tout le pipeline.

**Couleurs.** glTF veut des couleurs de sommet linéaires, mais Godot en GL Compatibility relit `COLOR_0` comme du sRGB, et le drapeau `vertex_color_is_srgb` du matériau n'y change rien (mesuré). Les couleurs sont donc écrites en sRGB : la veste du facteur, de base (78, 106, 140), rend (84, 115, 166) sous la lumière de la planche. Ce point est à revoir si 60.0e choisit Forward+.

**Défauts connus.**
- Une pièce plus fine que le pas de grille (les lettres du sac, les griffes, l'insigne de la casquette) se maille en éclats flottants. Le pas d'un personnage est de 1,6 unité, pour que le visage reste lisible ; à 2,4 unités, les yeux disparaissent.
- La maison a 10 600 triangles, trop pour un décor répété. On décimera en 60.0b, ou on montera le pas des grands décors.
- Les six modèles pèsent 18 Mo dans le dépôt, dont la moitié pour le facteur et ses 27 poses : chaque face garde ses propres sommets, en flottants. Avant d'exporter tout le bestiaire, il faudra quantifier (couleurs sur un octet, positions sur 16 bits) ou ne générer les modèles qu'au build.


## 9. 60.0b — l'arène low poly, découpage

Le bac à sable garde sa simulation, son arène et ses trois vues ; seul le rendu change, et l'ancien reste à portée pour comparer.

- **Rendu low poly par défaut**, l'ancien (sprites et sol photographié d'un biome) derrière `--sprites`. Sans photographie, le bac à sable démarre sans générer de run.
- **Le sol** : un triangle par demi-cellule, faces non lissées, une couleur par étage (sol, gradin, butte, marches) avec un léger écart de teinte et de hauteur par sommet, nul au bord des parois pour ne pas les décoller.
- **Les falaises** : découpées en facettes d'un tiers de cellule sur un demi-étage, bossuées vers l'extérieur sans toucher leurs arêtes, en strates de roche ; le haut garde la couleur du dessus, comme une lèvre d'herbe ; dans le vide, elles s'assombrissent jusqu'au noir.
- **Les corps** : le facteur, le rampant, le rôdeur et le charognard à la place des sprites, un maillage par pose (marche, repos, bond, blessure, mort), tournés vers leur marche ; flash blanc au coup, rouge à la blessure ; ombres portées réelles en plus de la tache au sol, qui dit la hauteur du bond.
- **Les décors** : arbres et rochers semés par graine sur les cellules plates à l'écart du tour du bot, une maison posée dans le coin sud-ouest ; leurs emprises deviennent des obstacles de la simulation (la maison en chapelet de cercles).
- **L'échelle** : celle des sprites, 2,65 m par cellule (un étage fait 1,86 m, le facteur 0,6 cellule), réglée dans `data/sandbox/relief.json` ; l'échelle définitive se tranche à la fin de 60.0.
- **La vue « relief 2D »** devient, en low poly, une vue isométrique orthographique éclairée : un avant-goût de la caméra isométrique haute de 60.0c.

Vérification : captures des trois vues (`--capture`), contrôle des rebords (`--check-edges`) toujours vert, build sans avertissement.

**Livré.** `tools/relief_sandbox.sh` démarre en low poly ; `--sprites` rend l'ancien bac à sable.
- `tools/tests/ReliefLowPoly.cs` : le relief en facettes (20 400 triangles pour l'arène de 44 × 44), le chargement d'un modèle et de ses poses, les décors semés (34 : arbres, rochers, la maison).
- Réglages dans le bloc `lowpoly` de `data/sandbox/relief.json` : échelle, couleurs, écarts de teinte et de relief, décors.
- Le charognard rejoint les modèles exportés (`tools/export_lowpoly.py`), car la horde du bac à sable en compte.

**Ce que montrent les captures.**
- La scène se lit d'emblée dans les trois vues : les étages par leurs strates et leur lèvre d'herbe, la fosse de vide qui noircit, la horde par ses silhouettes, et les vraies ombres des arbres et de la maison, qui donnent l'échelle.
- La vue isométrique orthographique est la plus nette ; la poursuite montre toute l'arène et ses escaliers.
- Les bosses des falaises se voient à peine : les parois restent des pans droits. C'est à revoir avec les styles (60.0d), par exemple des parois plus hautes ou des rochers posés au pied.
- Les piliers isolés (un seul cube de deux étages) font caisses : un décor de rocher ou de ruine les habillera mieux.

**Mesures.** Contrôle des rebords vert (113 rebords, aucun collant ; la tour et le haut palier ne se prennent pas d'un bond). Images par seconde lues sur le HUD, machine chargée (charge 2,3) : 144 en isométrique orthographique, autour de 65 à 70 dans les deux vues en perspective, avec 35 créatures, ombres portées et MSAA 4×. C'est une indication, pas une mesure : le banc A/B se fait en 60.0e.

## 10. 60.0d avancé — les styles à basculer, avec des contours

Retour de Raphaël sur 60.0b (§233) : les sols en facettes ne lui plaisent pas du tout, le reste à peine mieux. Il aime le low poly, mais celui-ci est « trop low » ; il veut des bordures noires et essayer plusieurs styles. Les styles passent donc avant les caméras.

**Ce qui est fait.** Dans `tools/relief_sandbox.sh`, chaque effet se bascule seul, en jeu :

| Touche | Effet | Choix |
|---|---|---|
| F1 | Sol | facettes ; lisse (cellules découpées en 3 × 3, couleur qui ondule en larges plaques, falaises droites) ; pixel art (les tuiles du jeu, éclairées) |
| F2 | Corps et décors | low poly ; mid poly (maillage deux fois plus fin, ombrage lissé) |
| F3 | Contours noirs | oui, non |
| F4 | Pixelisation | image rendue au quart de la résolution, agrandie au pixel près |
| F5 | Ombrage | normal ; toon (lumière en aplats) |

Le style de départ se règle dans `data/sandbox/relief.json` (`lowpoly.start`). Par défaut : sol lisse, mid poly, contours.

- **Le mid poly** : `python3 tools/export_lowpoly.py --detail mid`, dans `assets/models/mid/`, non versionné (23 Mo). Le script du bac à sable le génère au premier lancement. Les modèles se lisent à l'exécution (`GltfDocument`), sans passer par l'import de Godot. Triangles par pose : facteur 14 100, rôdeur 10 800, rampant 7 000, maison 35 700.
- **Les contours** (`tools/tests/relief_outline.gdshader`) se dessinent en post-traitement, sur la profondeur : un pixel devient un trait quand la profondeur y fait un saut ou un pli vers la caméra. Ils cernent les silhouettes, les arêtes des falaises et des marches, le bord du vide. Le test porte sur 1/z en perspective et sur z en orthographique, deux grandeurs qui varient en ligne droite sur un plan : un sol vu de biais ne se raye pas. Le trait fait 2 pixels en pleine résolution, 1 une fois pixelisé.
- **Galerie de captures** : après le tour du bot, `--capture` photographie dix combinaisons (`style_*.png`).

**Ce qu'on a appris.**
- **En GL Compatibility, le MSAA prive les contours des petits corps** : la profondeur lue alors ne garde que les grands sauts (mesuré sur captures). Quand les contours sont actifs, l'anticrénelage passait donc au FXAA, qui en fait n'existe pas en GL Compatibility (corrigé au §14).
- **Les seuils** : 0,0025 relatif en perspective, 0,012 cellule en orthographique. Plus haut, les arêtes de falaise tournées vers la caméra et les créatures restent sans trait.
- **Ce que montrent les captures.** L'isométrique orthographique avec contours ressemble à une illustration à l'encre : chaque marche, chaque falaise et chaque créature est cernée. Pixelisée et en toon, elle ressemble à du pixel art. Le sol lisse sans pixelisation fait un peu flou, comme une texture étalée. Le sol en tuiles pixel art, vu en 3D, fourmille.
- **Défaut** : quelques points isolés sur le sol lisse en perspective.

**Ce qui reste pour 60.0d** : le choix de Raphaël parmi ces combinaisons, puis les réglages du style retenu (épaisseur du trait, couleurs, brouillard).

**Séance de Raphaël (§234).**
- Contours gardés, à peaufiner sur le joueur et les créatures.
- Pixelisation abandonnée.
- Vue 3D de haut abandonnée.
- Il penche pour la caméra libre en poursuite plutôt que pour l'isométrique ; il lui manque le contrôle vertical.
- Sol : ni lisse (« ça manque de texture ») ni en facettes ; on regardera comment Megabonk et Minecraft texturent le leur.
- Détail des corps non tranché.


## 11. 60.0d suite — la caméra libre et le sol texturé (16 novembre)

**La caméra libre devient la vue de départ** (§234, §235). La souris la tourne autour du joueur, de gauche à droite et, désormais, de haut en bas.
- Monter la souris abaisse la caméra et relève le regard vers l'horizon, comme dans les jeux à la troisième personne.
- L'inclinaison va de 4° (presque à hauteur du joueur) à 72° (presque à la verticale), 24° au départ.
- La molette règle le recul, de 3,5 à 14 cellules.
- Tout se règle dans `camera_chase` de `data/sandbox/relief.json`.
- **La caméra ne traverse plus le relief.** Le rayon qui va du joueur à la caméra est sondé tous les 0,2 cellule. Au premier point qui passe sous le sol (plus une marge de 0,35 cellule), la caméra s'arrête juste avant. Elle se rapproche vite et se recule en douceur.
- **Retirés** : la vue 3D de haut et la pixelisation. Tab bascule maintenant entre la caméra libre et le relief 2D. Les styles passent de F1 à F4 : sol, corps, contours, ombrage toon.

**Comment Megabonk et Minecraft font leur sol.**
- Minecraft : des textures de 16 × 16 pixels sur des blocs d'un mètre, lues au plus proche. Le pixel reste franc à toute distance ; le motif est un grain au pixel, en quelques tons proches.
- Megabonk : la même recette sur des modèles low poly. Textures pixel art grossières, contours, un ciel et un étalonnage qui unifient l'image.
- Dans Godot, cela se fait avec des coordonnées de texture tirées du monde et un filtre au plus proche avec mipmaps.

**Le sol texturé**, nouveau style F1 et style de départ :
- **Les textures** viennent de `python3 tools/generate_relief_textures.py [--sheet planche.png]` et vont dans `assets/models/textures/`. Il y en a huit de 128 px, qui se répètent sans couture et couvrent chacune 4 × 4 cellules, soit 32 px par cellule, la densité des tuiles du jeu.
  - Quatre dessus : sol, terrasse, butte, marches.
  - Deux flancs : terre en strates, roche en blocs.
  - Deux franges d'herbe.
- **Leur fabrication** : un bruit périodique et un grain au pixel ramenés à quatre tons. Les tons forment une rampe aux ombres froides et aux lumières chaudes, autour des couleurs du bloc `lowpoly.colors`. Par-dessus, on sème les motifs des sols du jeu (`tools/sprites/ground_motifs.py`) : touffes, feuilles, cailloux, fissures, mousse. Le tirage est déterministe.
- **Le relief** (`tools/tests/ReliefTexturedGround.cs`) reprend la géométrie du sol lisse, une matière par texture. Les coordonnées de texture sont celles du monde : le motif passe d'une cellule à l'autre sans couture, et les strates des flancs restent à la même hauteur d'une falaise à l'autre.
  - Une frange d'herbe découpée pend du haut des falaises herbues, comme le côté d'un bloc d'herbe de Minecraft.
  - La teinte des dessus ondule en larges plaques.
  - Les falaises du vide plongent au noir.
- **Ce que montrent les captures.** En caméra libre, le sol a enfin une matière : herbe en grain, terre battue des marches, roche de la butte, strates sous la frange. Cela se lit de près comme à l'horizon. En isométrique, le rendu rappelle une carte de jeu de stratégie en pixel art. Le contraste entre le sol texturé et les corps lisses en mid poly reste à juger.

**Vérifié** :
- build sans avertissement ;
- smoke test ;
- contrôle des rebords inchangé (`rebords=113 colles=0`, escaliers jusqu'à la tour 4,00, bond seul +0,00) ;
- captures des deux vues, aux deux bornes d'inclinaison et au sommet de la tour ;
- le mode `--sprites` tourne toujours ;
- les textures régénérées sont identiques à l'octet près.

**Ce qui reste pour 60.0d** :
- l'avis de Raphaël sur le sol texturé ;
- le détail des corps : low poly, mid poly, ou des corps texturés comme le sol ;
- les contours du joueur et des créatures ;
- les quelques points isolés des contours sur le sol en perspective.

## 12. 60.0d suite — le mouvement, la caméra fixe, le sol plus simple et l'herbe (16 novembre)

Retours de Raphaël en jouant (§236), dans son ordre de priorité.

**Le mouvement.** Raphaël le trouvait « trop rapide », « saut de puce », avec un saut qui « va beaucoup trop loin, beaucoup trop haut » et des sauts en diagonale « abusés ». La cause : la vitesse et le bond venaient du jeu 2D, où des sprites de 30 px courent à 12 m/s sans que cela se voie. Des corps à l'échelle le trahissent. Ce qui change :
- **Rythme** : la vitesse vient du personnage (`characters.json`, px/s ÷ 45 px par cellule), multipliée par `pace` (0,67). Le facteur passe de 12,2 à 8,4 m/s.
- **Inertie** : la vitesse se prend en 0,12 s et se perd en 0,09 s, au lieu d'arriver d'un coup.
- **Le saut** (Espace) est commun à tous. Il garde l'élan de la course, sans poussée : c'était le dash du jeu 2D devenu bond qui propulsait à 8,5 cellules par seconde, dans toutes les directions et dès l'arrêt. Il se règle par sa hauteur et sa durée.
- **Des étages plus bas** : un étage valait 0,7 cellule, soit 1,86 m, plus que le facteur (1,58 m). Le saut qui le franchit était donc forcément plus haut que le corps. L'étage passe à 0,5 cellule (1,33 m), et la plus haute marche franchie sans sauter à 0,19 cellule, un quart d'étage. Les créatures suivent le même rythme (×0,67).
- **La capacité de chaque personnage** (Maj ou clic droit) vient de son profil de mobilité du jeu (`data/movement/mobility.json`) :
  - dash : traqueur, vagabond, forgeuse ;
  - glissade sur patins, qu'on oriente en route : facteur ;
  - déphasage : éveillée ;
  - saut flottant, qui quitte le sol et pèse moins tant qu'il dure : scaphandrière.
- **`--character <id>`** choisit le personnage. Ses modèles sont exportés au premier lancement et ne sont pas versionnés : 9 Mo en low poly.
- La marche du modèle suit la vitesse au sol, pour que les pieds ne glissent pas.

Relevé du contrôle headless (`--check-edges`), avant → après :

| | avant | après |
|---|---|---|
| Vitesse du facteur | 12,2 m/s | 8,4 m/s |
| Sommet du saut | 2,2 m (1,4 corps) | 1,64 m (1 corps), 1,24 étage |
| Temps en l'air | 0,55 s | 0,70 s |
| Gravité | 6 g | 2,7 g |
| Saut en course | 12 m | 5,9 m |

Par personnage :

| Personnage | Vitesse | Capacité |
|---|---|---|
| Traqueur | 9,4 m/s | dash de 3,4 m |
| Vagabond | 7,8 m/s | dash de 2,8 m |
| Forgeuse | 7,1 m/s | dash de 2,5 m |
| Facteur | 8,4 m/s | glissade de 5,1 m |
| Éveillée | 8,0 m/s | déphasage de 2,9 m |
| Scaphandrière | 7,5 m/s | saut flottant de 4,5 m |

Les rebords ne collent toujours pas (113 sur 113). Les escaliers mènent à la tour, et le saut seul n'y monte pas. Deux seuils du contrôle ont été ramenés à la vitesse du personnage, et l'inertie n'y compte plus comme un collage.

**La caméra libre est à distance fixe** : « il ne faut pas qu'on puisse régler la profondeur ». La molette est retirée.

**Le sol, plus simple et en relief** : « un peu trop détaillé ; c'était peut-être mieux qu'il soit un peu plus low poly ». Raphaël a aussi remarqué que dans Megabonk, les textures sont très pauvres, mais que le jeu rend bien grâce au détail posé en volume, comme l'herbe.
- **Textures deux fois moins fines** : 16 px par cellule au lieu de 32, moins de motifs semés, moins de grain.
- **Des touffes d'herbe en relief** (`tools/tests/ReliefGrass.cs`) : 16 000 touffes de sept brins sur les cellules d'herbe, à l'écart des rebords. Elles sont teintées comme le sol, un peu plus claires.
  - Un seul `MultiMesh` les dessine toutes.
  - Elles passent après les opaques sans écrire la profondeur. Sinon, les contours, qui lisent la profondeur, les noircissaient entièrement (constaté sur capture).
  - Réglages dans `lowpoly.grass` : 12 par cellule, 0,17 cellule de haut.

**Ce qui reste** :
- l'avis de Raphaël sur le mouvement en main et sur ce sol ;
- le détail des corps ;
- les contours du joueur et des créatures.

## 13. 60.0d suite — des corps plus grands, la caméra plus proche, l'herbe en bouquets (16 novembre)

Raphaël, capture de Megabonk à l'appui (§237) : « le personnage pourrait (et devrait) être plus grand, et pareil pour les ennemis », « les herbes, il y en a beaucoup trop ». Dans Megabonk, le personnage tient environ un sixième de la hauteur de l'écran, la caméra est proche et basse, et l'herbe pousse en rares bouquets sur une texture qui porte le reste.
- **Corps ×1,35** (`lowpoly.body_scale`) : le joueur et les créatures, avec leurs ombres, leurs emprises et leurs portées de contact (`ReliefConfig.ScaleBodies`). Les décors gardent leur taille. Le rendu en sprites (`--sprites`) n'est pas touché.
- **La caméra libre se rapproche et s'abaisse** : 4 cellules de recul au lieu de 7,5, 16° d'inclinaison au départ au lieu de 24°, visée à 1,05 cellule au-dessus des pieds. Le facteur tient environ un septième de la hauteur de l'écran.
- **L'herbe en bouquets épars** : 3 touffes par cellule au cœur d'un bouquet, semées selon un bruit de grande échelle, avec de larges plaques nues entre les bouquets. Il y a 1 500 touffes au lieu de 16 000. Chaque touffe a neuf brins plus larges, couchés vers l'extérieur et moins contrastés, sur 0,2 cellule de haut.

Les rebords ne collent toujours pas avec les emprises agrandies (113 sur 113, contrôle headless). Les escaliers mènent à la tour, et le saut seul n'y monte pas.

**Ce qui reste** : le jugement de Raphaël sur cette échelle et sur le mouvement en main, puis le détail des corps et leurs contours.

## 14. 60.0d suite — un seul sol, des falaises taillées, des contours sans escaliers (16 novembre)

Retours de Raphaël sur les quatre sols (§238) :
- tuiles du jeu : « elles ne me plaisent pas du tout » ;
- facettes : « juste des carrés coupés en deux, pas esthétique du tout » ;
- lisse : « je n'aime pas du tout » ;
- texturé : « mon préféré jusqu'à présent ; à voir si on peut pas faire quelque chose de mieux avec des polys » ;
- contours : « trop d'escaliers, on voit les pixels ».

Ce qui change :
- **Le sol texturé reste seul.** Les trois autres sont retirés du bac à sable, avec leur code (`ReliefLowPoly.BuildTerrain`) et leurs réglages (`shade_jitter`, `height_jitter`, `cliff_jitter`, quatre couleurs). Le rendu en sprites (`--sprites`) garde ses tuiles.
- **F1 bascule ses falaises : taillées (au départ) ou droites.** Taillées, elles sont des facettes de roche :
  - trois par cellule de large, une rangée par quart de cellule ;
  - les sommets intérieurs ressortent jusqu'à `carved_relief` (0,14 cellule) selon un bruit qui court le long de la paroi. Le relief continue d'une cellule à l'autre et ne s'arrête qu'aux coins ;
  - le haut et le pied restent droits, pour se raccorder au sol et à la simulation ;
  - la texture reste en coordonnées du monde, sans s'étirer, et la frange d'herbe épouse les mêmes facettes ;
  - les contours marquent les plis comme de fines fissures. 28 000 triangles au lieu de 8 400.
- **Les escaliers des contours avaient deux causes**, relevées dans le code :
  - l'image 3D se calculait en 1920 × 1080, puis s'agrandissait ×2 sur l'écran 4K ;
  - le FXAA, choisi quand les contours sont actifs, n'existe pas en GL Compatibility : il n'y avait aucun anticrénelage.

  Désormais, l'image se rend à la définition réelle de la fenêtre et suit son redimensionnement. Le trait garde la même largeur à l'écran : 2 px pour 1 080 lignes. Le shader teste le pli à quatre portées et prend la part de celles qui touchent, et son seuil est adouci. Le cœur du trait reste plein et ses bords s'estompent (vu sur un rebord agrandi, avant/après).

**Reste à voir** : les arêtes de la géométrie sans contour (brins d'herbe) restent crénelées, faute de MSAA avec les contours. Un suréchantillonnage coûterait 4 fois les pixels d'une image 4K, et n'est pas fait.

## 15. 60.0d suite — des falaises hautes et des cases de montée (16 novembre)

Raphaël (§239) : « il faut que les falaises soient beaucoup plus hautes, genre la plupart, on ne peut pas les sauter. Et donc il faut des cases de montée. »

- **Les cases de montée sont des rampes** (`ReliefArena`). Dans l'arène, `>`, `<`, `v` et `^` montent vers l'est, l'ouest, le sud ou le nord. Plusieurs cases de suite font une seule pente, de la case pleine qui la précède à celle qui la suit.
  - Le sol de la simulation (`GroundAt`, `HighestUnder`) y est continu. Le joueur et la horde montent en marchant, et rien d'autre ne change dans `ReliefSim`.
  - Le rendu : un dessus en pente couvert de terre battue, des talus droits sur les côtés (`AddSlopedCliff`), pas d'herbe. Le contrôle des rebords les écarte.
  - Le rendu en sprites (`--sprites`) ne connaît pas les rampes : il les dessine à plat, à mi-hauteur.
- **L'arène est redessinée** : la plupart des falaises font deux étages (2,65 m) ou plus, au-delà des 1,64 m du saut.
  - Un haut palier de 2 étages, et la tour de 4 étages dessus, chacun par sa rampe.
  - Un gradin d'un étage, qu'on saute, et un plateau de 3 étages qui le domine, par sa rampe.
  - Un plateau de 3 étages au sud-ouest, par une longue rampe.
  - Une falaise de 2 étages au sud, avec une rampe au milieu.
  - Deux gradins sautables à l'est, le puits et trois piliers de 2 étages à contourner.
- **Les plateaux d'un à trois étages sont herbus**, avec leur frange ; seule la tour, au-delà, est en roche nue.
- **Contrôle headless** : rebords 105 sur 105 sans collage. On monte à la tour par les rampes (4,00 étages). Le saut seul ne monte sur aucune falaise de deux étages ou plus (+0,00 sur quatre parois), mais monte sur le gradin (+1,00).
- **Conséquence à jouer** : la chute fait mal. Tomber de 2 étages coûte une dizaine de PV, de la tour une trentaine (`safe_drop_levels` 1,2). Les créatures escaladent les parois à 0,4 cellule par seconde : 2,5 s pour deux étages, ce qui donne à la hauteur un vrai refuge.

## 16. Convertir les assets (60.7) — inventaire et outillage proposé

Inventaire fait le 16 novembre (§239) en lisant le code. Il n'existe plus aucune retouche Aseprite : le dossier `art/` est absent, les dernières ont été retirées (commit 65d3f0c7). **Presque tout a une source procédurale SDF**, donc une conversion depuis le SDF ne perd rien.

| Catégorie | Sources | Voie | Effort |
|---|---|---|---|
| Personnages jouables (6), candidats (5) et habitants (3) | SDF, `tools/sprites/characters/` ; 2 jouables exportés | automatique (`export_lowpoly.py`, même interface) | faible |
| Créatures (14) | SDF ; 3 exportées | automatique ; les grands gardiens (Grand Cerf, tréant) à alléger | faible, puis moyen |
| Faune (5) | SDF, `tools/sprites/fauna/` | automatique, après une ligne d'outil (`models.creature` ne lit que `creatures/`) | faible |
| Décors des biomes (285 entrées, ~240 modèles 3D une fois fusionnés) | SDF ; 7 catalogues sur 8 accessibles, exodus manque | automatique ; fusionner les variantes orientées et les canopées séparées, inutiles avec une caméra qui tourne | moyen |
| Bâtiments (~37) | SDF ; la maison fait 10 600 triangles | hybride : alléger, fenêtres en matériau émissif, intérieurs et ouverture au masquage à refaire | moyen à élevé |
| Coffres, Mémoriaux, Failles, stèles, Barrière | SDF (`props/chests.py`, `landmarks.py`, `barrier.py`), absents de l'export | automatique, à ajouter aux catalogues | faible |
| Indicible | SDF et shaders | hybride : maillages et shaders refaits | élevé |
| Autels, repaires, cercles, marqueurs | dessinés en C# | à refaire en maillages ou décalques | moyen |
| Sols (22 matières, ~79 anciennes tuiles) | motifs procéduraux (`ground_motifs.py`) | hybride : rendre les mêmes matières en textures carrées sans couture, comme `generate_relief_textures.py` ; routes à refaire | moyen |
| Projectiles (29) | SDF | billboards ou maillages minuscules | moyen |
| VFX (~40 dont 17 en C#, 33 shaders) | partiel | hybride : pickups en maillages, lueurs en lumières, shaders réécrits en `spatial` | élevé |
| Sang | 1 PNG | décalque au sol | faible |
| Interface (~400 PNG) | — | reste en 2D ; plaques, chiffres et invites en `Label3D` | moyen |

**L'outillage à écrire, dans cet ordre** (lots de 60.7, à lancer dès que le style est arrêté) :
1. **Export de masse dirigé par les catalogues** (`--all`, `--biome`) : catalogues manquants, faune, pickups, et fusion automatique des variantes orientées et des canopées. Effort moyen.
2. **Emprises et collisions dans le manifeste** : emprise au sol (déjà dans `PropModel`), boîte de collision, point d'ancrage. Effort faible.
3. **Poids et finesse** : sommets partagés et quantifiés (29 Mo pour 9 modèles aujourd'hui), décimation des grands décors, niveaux de détail, plancher de pas pour les petites pièces. Effort moyen.
4. **Planches de contrôle par catalogue** (`tools/model_sheet.sh` étendu), avec un budget de triangles. Scènes ou `MeshLibrary` par biome générées depuis les manifestes. Effort moyen.
5. **Textures de sol des 22 matières**, par le générateur du relief branché sur les `GroundMaterial` du jeu. Effort moyen.

Plus tard, transversal : un squelette glTF exporté pièce par pièce, pour une animation interpolée et moins lourde que le maillage par pose (élevé).

**Le facteur** : Raphaël le trouve « assez moche ». Son modèle se reprend dans le SDF (`tools/sprites/characters/facteur.py`) au moment du détail des corps, puis passe par la même conversion. Pas avant que le style soit arrêté.

## 17. Toutes les falaises hautes ; la conversion commence : l'export de masse (lot C1, 16 novembre)

Raphaël (§240) : plus de falaise qu'on grimpe en sautant, toutes de 3 à 5 étages, « à tester » ; et avancer, en commençant la conversion des assets.

**L'arène** (`data/sandbox/relief.json`) est redessinée sans aucun gradin : deux plateaux de 3 étages au nord, chacun avec un sommet de 5 étages ; deux plateaux de 4 étages (sud-ouest et est) ; une falaise de 3 étages au sud ; le puits ; trois piliers de 3 étages. Chaque hauteur a sa rampe, parfois longue de huit cases. Les plateaux de 3 étages sont herbus, ceux de 4 et 5 en roche nue.
- Contrôle headless : rebords 86 sur 86 sans collage, la tour atteinte par les rampes (5,00 étages), et le saut ne monte sur aucune des six parois essayées (+0,00).
- À jouer : tomber de 3 étages coûte une vingtaine de PV, de 5 près de 50. Les créatures mettent 4 à 6 s à escalader.

### Les lots de la conversion

| Lot | Outil | État | Effort |
|---|---|---|---|
| **C1** | Export de masse dirigé par les catalogues, bibliothèque et index, planches par catalogue | **livré** (ci-dessous) | moyen |
| C2 | Emprises et collisions dans le manifeste de chaque modèle : emprise au sol, boîte de collision, point d'ancrage, décor bloquant ou non (seuils de `world_gen.json`) | à faire | faible |
| C3 | Poids et finesse : sommets partagés et quantifiés, décimation des grands décors, niveaux de détail ; un pas plancher pour les pièces minces des grands décors | C3a livré (§18), C3b et C3c à faire | moyen |
| C4 | Chaîne d'import Godot : scènes ou `MeshLibrary` par biome tirées des manifestes, budget de triangles vérifié sur les planches | à faire | moyen |
| C5 | Textures de sol des 22 matières, et décalques pour les décors plats | à faire | moyen |

Ces lots n'attendent pas l'arrêt du style : l'export se relance en deux minutes quand un modèle ou un réglage change.

### C1 livré

`python3 tools/export_lowpoly.py --all --jobs 6` exporte tout ce qui a une source SDF dans `build/models/`, en 106 s.
- **Ce que couvre `--all`** : personnages, créatures, faune et les douze catalogues de décors de `generate_props.py`. Cela inclut les coffres, les monuments (Mémoriaux, Failles, stèles), la Barrière, l'Indicible et l'exode urbain.
- **Ce qui est réduit** :
  - le tronc et la canopée d'un arbre se fondent en un modèle ;
  - d'une famille de variantes orientées (`_x`/`_y`, `_h`/`_v`, `_e`/`_w`/`_n`/`_s`, et la forme sans suffixe), seule la première est gardée.
  - Il reste 312 modèles ; les 36 variantes écartées sont notées dans l'index.
- **La bibliothèque n'est pas versionnée** : elle est ignorée de git, et de Godot par un `.gdignore`.
  - Son `index.json` donne, modèle par modèle, ses sources, ses variantes écartées, ses poses, son plus gros maillage, sa hauteur en mètres, son poids et sa durée.
  - Le mode explicite (`--character`, `--prop`, sortie dans `assets/models/`) ne change pas pour le bac à sable.
- **Les échecs sont rapportés, pas levés** : un modèle en erreur n'arrête pas les autres.
  - Si un processus est tué (mémoire, machine chargée par d'autres builds), ce qui restait repasse un par un.
- **Les décors minces** (clôtures, grillage, rambarde) sortaient vides, parce que leur pas, calé sur la longueur, enjambait les lattes. Le pas est maintenant affiné jusqu'à 1,2 unité tant que le maillage n'atteint pas la hauteur de la matière. Une clôture fait 1,06 m, le grillage 1,46 m.
- **La planche de la bibliothèque** : `tools/model_sheet.sh <dossier> --library build/models`.
  - Elle lit les .glb à l'exécution, sans import.
  - Elle montre les modèles par dizaines et par catalogue, du plus petit au plus grand, nommés, le facteur à gauche pour l'échelle : 37 planches.

**Bilan** : 312 modèles, 0 échec, 275 Mo.

| | Modèles | Poids |
|---|---|---|
| Personnages | 14 | 112 Mo |
| Créatures | 14 | 100 Mo |
| Décors (12 catalogues) | 279 | 62 Mo |
| Faune | 5 | 1 Mo |

Ce que montrent les planches, pour les lots suivants :
- **Le poids est dans les animations** : un maillage complet par pose, 27 poses pour un personnage. Les deux plus lourds sont le Grand Cerf (34 Mo, 15 800 triangles) et l'épouvantail (29 Mo, 16 700). C'est le premier gain de C3, et le squelette glTF (transversal) le réglerait à la racine.
- **Bâtiments, véhicules, rochers, personnages et créatures** passent bien. Les bâtiments font 8 000 à 10 600 triangles, la gloriette 12 600.
- **Les pièces minces des grands décors cassent** : les branches de l'arbre mort, la flèche de la grue du port et l'antenne radio sortent en morceaux, et la poubelle grillagée est trouée. Leur pas suit la taille du décor. C3 leur donne un pas plancher par pièce.
- **18 décors sont plats par nature** : flaques, rails, marelle, flèches au sol, salines, pavés, nénuphars, cale. Cinq sortent sans aucun triangle. Ils deviennent des décalques au sol (C5), pas des maillages ; la planche les écarte.
- **Les couleurs sont crues** : le maillage ne porte que la couleur de base de chaque matériau, alors que le sprite la nuançait en quatre tons. Le vert des buissons, notamment, est vif. Ce réglage appartient au style (60.0), pas à l'export.

## 18. C3, le poids et la finesse — découpage proposé (16 novembre)

**Ce que dit la mesure** (bibliothèque de C1, 312 modèles) :
- un triangle coûte 102 octets par pose : trois sommets propres, chacun avec position, normale et couleur en flottants ;
- les 33 modèles animés pèsent 213 Mo pour 629 poses, et les 279 décors 62 Mo ;
- le budget dépasse ce qu'il faut à la vue : épouvantail 16 700 triangles, Grand Cerf 15 800, gloriette 12 600, maisons 10 300 à 10 600 ;
- le pas d'un décor suit sa taille (`extent / 40`) : 5,8 unités pour une maison de 8,4 m. Une pièce plus mince que deux pas (branche, flèche de grue, antenne, grillage) passe entre les points de la grille ;
- le Godot installé (4.7.2) ne connaît ni `KHR_mesh_quantization`, ni meshopt, ni Draco. Seul le glTF de base est sûr.

**Tranché par défaut par l'agent, réversible (§241) :**
- **Pas de quantification des positions** : elle demande une extension que l'import de Godot ne déclare pas. Le gain vient du partage des sommets et de types plus petits du glTF de base.
- **Les niveaux de détail ne s'exportent pas** : l'import de Godot les génère seul (C4). On les mesure là-bas, à l'import.
- **Pas de décimation par effondrement d'arêtes** : elle demanderait une bibliothèque de plus et adoucirait les facettes. Un modèle trop lourd reprend un pas plus grossier, cherché jusqu'à tenir son budget. Le low poly reste uniforme.
- **Le maillage par pose reste** : le squelette glTF demande de savoir quelle pièce suit quel os, ce que les créatures n'exposent pas. Il reste transversal, plus tard.

| Sous-lot | Contenu | Vérification | Effort |
|---|---|---|---|
| **C3a, le format compact** | Sommets partagés en low poly aussi ; un sommet ne se double qu'à la frontière de deux matériaux. Couleur sur 8 bits, indices sur 16 bits quand le maillage le permet. Plus de normale écrite : la facette se calcule dans le shader (dérivées de la position), avec un `ShaderMaterial` qui remplace le `StandardMaterial3D` de `ReliefLowPoly`, en ombrage normal ou toon (F4). Les poses identiques d'une animation ne s'écrivent qu'une fois. | Poids de la bibliothèque avant/après ; planche d'un même catalogue avant/après, à l'identique ; bac à sable en low poly, F4 compris | moyen |
| **C3b, les pièces minces** | Une pièce plus mince que deux pas se maille à part, à un pas tiré de son épaisseur (plancher 1,2 unité), puis rejoint le reste du décor. Son épaisseur se mesure sur sa boîte échantillonnée, sans rien changer aux modèles SDF. | Planches de l'arbre mort, de la grue du port, de l'antenne radio et de la poubelle grillagée, avant/après ; triangles ajoutés notés dans l'index | moyen |
| **C3c, les budgets** | Un budget de triangles par catégorie, dans un fichier de réglages de l'export : personnage 3 000, créature 4 000, grand gardien 8 000, décor 2 500, bâtiment 6 000 (à ajuster sur les planches). Un modèle au-dessus reprend un pas plus grossier, par dichotomie, après C3b pour ne pas défaire les pièces minces. L'index note le budget, le pas retenu et les dépassements. | Index : aucun dépassement non signalé ; planches des plus lourds (épouvantail, Grand Cerf, gloriette, maisons) | faible |

**Ordre** : C3a d'abord, le plus gros gain pour le moindre risque, puis C3b, puis C3c, qui s'appuie sur les deux. Chaque sous-lot relance l'export complet (`--jobs 4` à 6 selon la charge) et se juge sur les planches.

**Cible** : la bibliothèque passe de 275 Mo à moins de 40, sans perte visible sur les planches, et les quatre décors cassés sortent entiers.

### C3a livré

Le format compact. `tools/sprites/lowpoly.py` écrit moins d'octets par triangle ; les triangles restent identiques sur les 312 modèles.
- **Sommets partagés** en low poly : un sommet par couple sommet de grille × matériau, doublé seulement à la frontière de deux matériaux.
- **Plus de `NORMAL` écrite** en low poly. Le mid poly garde ses normales.
- **`COLOR_0` en octets normalisés** (VEC4, alpha opaque, alignement 4 octets du glTF), aussi en mid poly.
- **Indices sur 16 bits** si le maillage a 65 535 sommets ou moins, sinon 32 (aussi en mid poly).
- **Poses identiques réutilisées** : une pose égale à une précédente réutilise son maillage (empreinte blake2b). Le manifeste gagne `flat_normals` et `meshes` ; l'index note `meshes` par modèle.
- **Décors plats sans triangle** : ils gardent leur nœud, sans surface, comme avant. glTF de base seulement.
- **`tools/export_lowpoly.py`** : la fusion des pièces passe par `merged` de `lowpoly.py`.
- **Côté Godot** : `tools/tests/lowpoly_flat.gdshader` et `lowpoly_flat_toon.gdshader`, corps commun `lowpoly_flat.gdshaderinc`.
  - La normale de facette vient de `cross(dFdx(VERTEX), dFdy(VERTEX))`, retournée vers la caméra. `ALBEDO = COLOR.rgb`, `ROUGHNESS` 1 : même rendu que l'ancien `StandardMaterial3D`.
  - Le toon (F4) change de shader, le mode d'ombrage ne se réglant pas par paramètre.
  - `ReliefLowPoly.SurfaceMaterial(emissive, flatNormals)` choisit Glow, Flat ou Plain d'après le manifeste. `ModelSheet` utilise les mêmes matériaux.
- **Galerie de capture du bac à sable** : un cas `style_taille_low_contours_toon` ajouté (le low poly en toon n'y était pas).

**Deux pièges trouvés**
- Sans `NORMAL`, Godot n'en génère aucune au chargement (sonde : kiosque 7 347 sommets, aucune normale ; avant, 37 932 sommets avec normales). L'ombrage par faces n'en a pas besoin, mais le décalage des ombres lit la normale de sommet : le toit du kiosque s'est strié d'acné. Correctif : `ReliefLowPoly.AddSmoothNormals` reconstruit au chargement une normale lissée pondérée par l'aire (§241).
- Godot tient les faces avant en sens horaire (l'import glTF retourne l'ordre des indices). Le premier essai prenait le produit vectoriel dans le mauvais sens : normales vers l'intérieur, acné pire. Corrigé, planche identique à l'avant.

**Mesures** : export complet `--all --jobs 2` (le garde-fou mémoire refusait 6 ouvriers, 13 Gio libres), 137 s.

| | Avant | Après |
|---|---|---|
| Personnages (14) | 112,1 Mo | 16,5 Mo |
| Créatures (14) | 100,4 Mo | 14,2 Mo |
| Faune (5) | 0,8 Mo | 0,1 Mo |
| Décors (279) | 62,0 Mo | 10,0 Mo |
| **Total** | **275 Mo** | **40,8 Mo** |

- Exemples : Grand Cerf 34,0 → 4,7 Mo, épouvantail 28,8 → 4,0, facteur 8,85 → 1,34, gloriette 1,37 → 0,19.
- Poses dédoublées : 9, toutes dans la faune ; aucune chez les personnages et créatures.
- Modèles versionnés du bac à sable (`assets/models`) : les quatre modèles de personnages et créatures 18,2 → 2,8 Mo, les décors 1,9 → 0,3 Mo.
- Mémoire vive : 5 fois moins de sommets par maillage chargé (kiosque 37 932 → 7 347).
- La cible de §18 (moins de 40 Mo) est presque tenue (40,8 Mo) ; C3c finira.

**Vérifications** : `dotnet build` 0 avertissement ; `tools/smoke_test.sh` vert, aucun `.import` modifié ; planches avant/après d'un sous-ensemble de 13 modèles (facteur, éveillée, Grand Cerf, épouvantail, rampant, rôdeur, maison, kiosque, arbre moyen, réverbère, lanterne des marais, feu de camp, rocher), identiques à l'œil ; capture du bac à sable (`tools/relief_sandbox.sh -- --capture`, 14 s) en low poly normal et toon, et en mid poly : propres, sans erreur.

**Pas de banc** : le changement ne touche que le bac à sable et l'outillage, pas la run. Le coût en jeu se mesurera à l'import (C4).

**Reste** : le réverbère et la lanterne des marais étaient déjà cassés avant (mât trop mince) ; c'est C3b. Puis C3c, les budgets.
