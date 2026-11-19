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
| **60.0e Le moteur de rendu** | Le même banc en Compatibility et en Forward+ : 120 créatures animées, projectiles, décors. **Livré (§23)** : Forward+ environ −7 % | Banc A/B machine calme, images par seconde et temps d'image | Faible |
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
| C2 | Emprises et collisions dans le manifeste de chaque modèle : emprise au sol, boîte de collision, point d'ancrage, décor bloquant ou non (seuils de `world_gen.json`) | C2a livré (§21) | faible |
| C3 | Poids et finesse : sommets partagés et quantifiés, décimation des grands décors, niveaux de détail ; un pas plancher pour les pièces minces des grands décors | livré (§18) : C3a, C3b et C3c | moyen |
| C4 | Chaîne d'import Godot : scènes ou `MeshLibrary` par biome tirées des manifestes, budget de triangles vérifié sur les planches | livré (§19) : C4a, C4b et C4c | moyen |
| C5 | Textures de sol des 22 matières, et décalques pour les décors plats | C5a et C5b livrés (§22) | moyen |
| C6 | Rendu des couleurs : aplats et palette terne (§20) | C6a livré (§20) | moyen |

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

### C3b livré — 16 novembre

Les pièces minces. Le pas de grille d'un décor suit la taille de son cadre (`max(2,4, étendue du cadre / 40)`). Les pièces plus minces que ce pas (fût du réverbère, flèche de la grue, branches de l'arbre mort, barreaux de grille, mât radio, voie de carrière) passaient entre les points de la grille : elles sortaient trouées ou disparaissaient.
- **Affinage local de la grille** (`adaptive_nets`, `mesh_parts_adaptive` dans `tools/sprites/lowpoly.py`, appelés par `tools/export_lowpoly.py`). Au lieu du pas tiré de l'épaisseur prévu au découpage, une cellule grossière « ratée » se maille plus fin : une pièce mince n'est souvent pas une pièce SDF à elle seule (le treillis de la grue, les barreaux d'une grille tiennent dans une même pièce), et la mailler à part l'aurait détachée du reste.
  - Une cellule dont les huit coins sont dehors, mais où un échantillonnage fin trouve de la matière, est ratée.
  - Un test de profondeur (érosion, `THICK_DEPTH` 0,5 pas) écarte la matière épaisse.
  - Les cellules ratées et leurs voisines traversées et minces se maillent au pas fin ; la surface grossière s'y efface.
  - La pièce fine gagne un demi-pas fin d'épaisseur (`THIN_INFLATE`) pour rester entière.
  - Les pièces peintes (`relief=False`) n'entrent pas dans la surface fine.
  - Le pas fin vaut la moitié du pas du décor, pas moins de 4 cm (`FINE_FLOOR`).
- **Orientation des faces** : `_facing_inward` suit le gradient du volume pris tout près de la face, au lieu de comparer les distances à un demi-pas de part et d'autre, qui se trompait sur les pièces plus minces qu'un pas. Le changement touche aussi personnages et créatures : mêmes triangles, quelques sommets de plus aux frontières de matériaux, yeux du facteur un peu plus nets.
- **Matériau de la surface fine** : il se lit plus profond (de l'épaisseur ajoutée). Sans cela, la mousse peinte qui enveloppe le bois gagnait et le bois sortait en damier vert et noir (table de pique-nique, barrière cassée, rambarde).
- **Variante écartée** : un pas fin calé sur la taille réelle de la matière (un quarantième de son étendue). Planches très bonnes, mais décors de 570 000 à 1,66 million de triangles et environ 10 à 28 Mo, pour un gain non visible sur les planches.

**Mesures** : export complet `python3 tools/export_lowpoly.py --all --jobs 2`, 312 modèles, 0 échec, 163 s, contre C3a.

| | C3a | C3b |
|---|---|---|
| Poids de la bibliothèque | 40,8 Mo | 48,3 Mo |
| Triangles des décors | 568 212 | 1 009 446 (×1,78) |
| Triangles des personnages | 43 212 | 43 212 |
| Triangles des créatures | 56 192 | 56 192 |

- 232 décors sur 274 affinés.
- Triangles par catalogue, rapport à C3a : barrier ×3,6, landmarks ×3,6, indicible ×3,1, fields ×2,6, swamp ×2,3, urban_exodus ×2,2, quarry ×1,9, forest ×1,8, chests ×1,7, urban et fields_farm ×1,6, urban_buildings ×1,2.
- Avant → après : voie de carrière 0 → 2 260, barrière en bois cassée 24 → 1 564, rambarde cassée 48 → 1 552, pile d'os 160 → 2 998.
- Après : réverbère 1 016, arbre mort 1 940, abri de bus 2 850, grillage 2 602, mât radio 3 534, grue 7 558, kiosque 17 556. Les plus lourds : vantaux de grille et œil de l'Indicible, 15 000 à 18 250.
- La cible de 40 Mo de §18 n'est plus tenue (48,3 Mo) : C3c reprend les budgets.

**Vérifications** : planches avant/après (`tools/model_sheet.sh`) regardées. La grue retrouve sa flèche, l'arbre mort ses branches, le mât ses montants, les grilles leurs barreaux, la voie de carrière apparaît. Réverbère, grue, mât, kiosque, immeuble et abri de bus vérifiés à l'œil. `tools/smoke_test.sh` vert. Bac à sable du relief capturé (`tools/relief_sandbox.sh -- --capture`), en low et mid poly, images regardées : rendu normal. Modèles versionnés de `assets/models` régénérés (export par défaut ; le `--detail mid` n'est pas versionné).

**Points ouverts**
- Le câble du crochet de la grue et la pointe du mât restent plus fins que le pas fin et manquent.
- Petit trou au bas du tronc de l'arbre mort.
- Épaississement visible sur certaines pièces : cils de l'œil de l'Indicible devenus bulbeux, bande grise du vantail abîmé, bouts de poteaux arrondis.
- Quelques triangles sombres sur le bois (rainures peintes) et petits trous noirs (grilles, plateau de la table).
- La poubelle a perdu sa grille ajourée.
- Ces réglages relèvent de C3c ou d'un retour de Raphaël.

**Suite** : C3c (budgets de triangles), seulement sur demande de Raphaël.

### C3c livré — 16 novembre

Les budgets de triangles. Un fichier de réglages, `tools/sprites/lowpoly_budgets.json`, donne par sorte de modèle des paliers `[taille minimale en mètres, budget]` (la taille est la plus grande dimension du modèle) et un drapeau `coarsen`.

| Sorte | Budget | Grossit |
|---|---|---|
| Personnage | 5 000 | oui |
| Créature | 4 000, 16 000 à partir de 4 m (grands gardiens) | oui |
| Décor | 2 500, 6 000 à partir de 2 m, 10 000 à partir de 5 m | non (dépassement noté) |

- `max_coarsening` vaut 2,5. Le mid poly a quatre fois le budget. `--step` ignore les budgets.
- `fit_budget` (`tools/export_lowpoly.py`) exporte au pas d'origine. Au-dessus du budget, si la sorte se grossit, il cherche par dichotomie le plus petit facteur de pas qui rentre : première tentative au facteur racine(triangles / budget), puis 3 bissections. Hors de portée à ×2,5, le modèle garde son pas et le dépassement est noté.
- L'index note par modèle `budget`, `coarsening`, `over_budget`, et dans le résumé le nombre de pas grossis et la liste des dépassements. La console aussi.
- Pour un décor, le pas fin des pièces minces (C3b) reste celui d'avant le budget (`fine_base`), si le grossissement des décors est rallumé.

**Écarts au découpage**
1. Personnage à 5 000 au lieu de 3 000. À 3 000, 7 personnages sur 14 auraient grossi, alors que le pas de 1,6 est réglé pour que le visage reste lisible. À 5 000 aucun ne dépasse (le plus lourd, le vagabond, 4 528) : c'est un garde-fou.
2. Grand gardien à 16 000 au lieu de 8 000. À 8 000 (pas ×1,42 et ×1,45), le Grand Cerf perdait les andouillers de ses bois, l'épouvantail son corbeau et le ruban de son chapeau. Un gardien est seul à l'écran.
3. Des paliers de décor par taille, au lieu d'un budget « décor » et d'un budget « bâtiment » : un budget unique écrasait les grands décors.
4. Les décors ne se grossissent pas. Deux essais sur la bibliothèque complète, planches avant/après regardées :
   - Essai 1, pas fin grossi avec le pas : 51 modèles grossis, aucun dépassement, 48,3 → 41,3 Mo, décors 1 009 446 → 857 166 triangles. Les pièces minces se défont : barreaux du vantail cassé fondus en panneau à ×1,78, poteaux du hangar tronqués, balustres du kiosque, fût du silo en lames.
   - Essai 2, pas fin figé : 21 grossis, 29 dépassements, décors −5 % seulement (958 070). Tout ce qui est bâti se dégrade dès ×1,2 : porte de la cabane de carrière en damier et toit fendu, façade de la ruine criblée de trous, toit de la cabine du chalutier, bassin de la fontaine, chaîne de l'aile de grille. Seuls feuillages et troncs tiennent (arbre moyen ×1,91).
   - D'où la règle : les décors gardent leur pas. La distance relèvera des niveaux de détail que l'import de Godot génère (C4, déjà tranché au §241).

**Mesures** : export complet `python3 tools/export_lowpoly.py --all --jobs 2`, 312 modèles, 0 échec, 467 s, contre C3b.

| | C3b | C3c |
|---|---|---|
| Poids de la bibliothèque | 48,3 Mo | 47,9 Mo |
| Triangles des créatures | 56 192 | 53 896 |
| Triangles des personnages | 43 212 | identiques |
| Triangles des décors | 1 009 446 | identiques |

- 2 modèles grossis : l'épouvantail ×1,03 (16 700 → 15 672 triangles) et la brute du vide ×1,15 (5 188 → 3 920). Planches regardées : ils tiennent (épines du dos de la brute un peu réduites).
- 48 décors au-dessus de leur budget, tous signalés dans l'index : 452 000 des 1,01 million de triangles de décors. Grilles et vantaux, œil de l'Indicible, hangars, kiosque, moulin, coquelicots, hautes herbes, tilleuls, arbres fruitiers, maisons. Aucun personnage ni créature en dépassement.
- Le poids tient surtout aux animations (personnages 16,5 Mo, créatures 13,8 Mo, maillées pose par pose). La cible de moins de 40 Mo n'est pas tenue (47,9 Mo). Le levier est le squelette glTF, laissé à plus tard au §241, pas un budget de triangles.

**Vérifications** : planches avant/après (`tools/model_sheet.sh`) des modèles grossis à chaque essai, regardées (dont par le sous-agent `regard`). Modèles versionnés de `assets/models` régénérés : inchangés (aucun du jeu d'essai n'est grossi). Pas de smoke test ni de capture : seul l'outil d'export (Python) change, aucun asset versionné ne bouge.

**Points ouverts**
- Les 48 décors en dépassement : à traiter par les niveaux de détail de C4, ou par un budget relevé sur décision de Raphaël.
- Les défauts de C3b restent : câble de la grue, pointe du mât, épaississements.

**Suite** : C2 (emprises et collisions dans le manifeste) ou C4 (chaîne d'import Godot, niveaux de détail), au choix de Raphaël.

## 19. C4, l'import dans Godot — découpage proposé (17 novembre)

Raphaël : continuer par C4. Aujourd'hui, la bibliothèque (`build/models/`, 312 modèles) ne sert qu'aux planches. Le bac à sable lit trois décors de `assets/models/` en analysant le .glb à l'exécution (`ReliefLowPoly.LoadModel`). C'est sans import Godot, donc sans niveaux de détail, et les normales lissées sont recalculées à chaque lancement.

**Ce que C4 doit livrer** : des modèles prêts pour le moteur, avec leurs niveaux de détail, groupés par terre, et un bac à sable qu'on traverse au milieu des vrais décors d'une terre. On pourra y juger le style et mesurer le coût.

**Défauts de l'agent, réversibles** :
- **Un convertisseur, pas l'importeur de l'éditeur.** Une scène headless lit l'index et les .glb, ajoute les normales lissées et génère les niveaux de détail (`ImporterMesh.GenerateLods`). Elle écrit un `.res` binaire par modèle, à côté du .glb, plus son entrée dans l'index. Ouvrir la bibliothèque à l'importeur produirait 312 `.import` non versionnés et un long réimport à chaque export. Le convertisseur se relance avec l'export, de la même façon.
- **Pas de `MeshLibrary`.** Elle sert le `GridMap`, un maillage par case, sans poses. Les décors seront posés en instances groupées (`MultiMeshInstance3D`, un par modèle), comme l'herbe du bac à sable. L'index par terre suffit comme catalogue.
- **Les niveaux de détail sont ceux de Godot** (§241). Ils simplifient par effondrement d'arêtes, ce qui n'est acceptable que de loin. Si le rendu Compatibility ne les applique pas, le repli est un second export au pas ×2 de notre pipeline, montré au-delà d'une distance (`visibility_range`), ce qui garde le low poly uniforme.
- **Les personnages et créatures restent maillés pose par pose.** Le squelette glTF reste transversal et plus tard (§241).

| Sous-lot | Contenu | Vérification | Effort |
|---|---|---|---|
| **C4a, le convertisseur et les niveaux de détail** | `tools/convert_models.sh` (scène headless) : pour chaque modèle de l'index, poses comprises, un `.res` avec normales lissées et niveaux de détail ; l'index note les triangles de chaque niveau. D'abord une mesure : les niveaux de détail s'appliquent-ils en Compatibility (primitives dessinées par image, `RenderingServer.GetRenderingInfo`, un décor proche puis lointain) ? Sinon, le repli par `visibility_range`. `LoadModel` lit le `.res` quand il existe. | Primitives dessinées proche/loin ; durée de conversion ; durée de chargement `.res` contre `.glb` ; planche d'un même catalogue à l'identique | moyen |
| **C4b, le budget sur les planches** | La planche de la bibliothèque écrit sous chaque modèle ses triangles, et marque d'une couleur ceux qui dépassent leur budget (C3c). | Planches des catalogues les plus lourds (barrière, Indicible, bâtiments) | faible |
| **C4c, une terre dans le bac à sable** | `tools/relief_sandbox.sh -- --biome <catalogue>` sème les décors de ce catalogue, tirés de la bibliothèque convertie. Il écarte les décors plats (décalques, C5) et pondère les décors par la densité de la terre en 2D (`data/biomes`), avec une instance groupée par modèle. Les emprises restent les boîtes des modèles, en attendant C2. | Captures de la forêt, de la ville et de la carrière ; banc FPS (machine calme) et primitives, niveaux de détail allumés puis éteints | moyen |

**Ordre** : C4a d'abord, car tout le reste lit ses `.res`. C4b est petit et indépendant. C4c est ce que Raphaël pourra jouer.

**Risques** :
- Les niveaux de détail en Compatibility, d'où la mesure en tête de C4a.
- Le poids des `.res` : sans quantification, ils pèseront plus que les .glb compacts. On le mesure, et la bibliothèque n'est pas versionnée.
- Le coût d'une terre pleine de décors en Compatibility, mesuré en C4c, avant le monde (60.4).

### C4a livré — 17 novembre

Le convertisseur et les niveaux de détail.

**Fait**
- `tools/convert_models.sh [dossier] [--force]` (scène `tools/tests/ModelConvert`, headless ; `build/models` par défaut). Pour chaque .glb accompagné de son manifeste, un `.scn` à côté : PackedScene compressée, poses en enfants. Les normales lissées sont ajoutées (`ReliefLowPoly.WithSmoothNormals`, extraite de `AddSmoothNormals`). Les niveaux de détail viennent de `ImporterMesh.GenerateLods`, angle de fusion 60° comme l'importeur.
- Seuls les .glb plus récents que leur .scn repassent.
- Rapport `lods.json` à la racine : triangles de chaque niveau du plus gros maillage, poids, durée. Fichier séparé de l'index de l'export, pour qu'un seul outil écrive chaque fichier. Un modèle à jour garde son entrée précédente.
- `ReliefLowPoly.LoadScene` charge le `.scn` s'il est à jour, sinon le .glb à l'exécution. `LoadModel` (bac à sable) et la planche de la bibliothèque (`ModelSheet`) l'utilisent.
- `tools/relief_sandbox.sh` convertit `assets/models` avant de lancer (incrémental). Les `.scn` et `lods.json` de `assets/models` sont ignorés de git. Ligne de commande ajoutée à `AGENTS.md`.
- `--probe <modèle>` (fenêtré) : primitives dessinées par image selon la distance (4, 12, 30, 60, 120 m), à trois seuils d'erreur tolérée (`Viewport.MeshLodThreshold` 1, 4, 8 px) et au plus fin (`LodBias` poussé).

**Mesures**
- Conversion de la bibliothèque : 312 modèles, 0 échec, 4,4 s (9 s avec le lancement de Godot). 55,8 Mo de .scn contre 47,9 Mo de .glb. `assets/models` : 16 modèles (mid compris) en 1,8 s.
- 3 à 8 niveaux générés selon le modèle. Arbre moyen 10 014 → 5 006 → 2 500 → 1 250 → 574 triangles. Maison 10 682 → 5 340 → 2 670 → 1 560. Facteur 3 204 → 1 602 → 800 → 400 → 294.
- **Les niveaux de détail s'appliquent en Compatibility** (opengl3, gl_compatibility). Le repli par `visibility_range` est inutile. Primitives dessinées, fenêtre 1280×720 :

| Modèle | Seuil 1 px | Seuil 4 px | Seuil 8 px |
|---|---|---|---|
| Arbre moyen | 10 014 jusqu'à 30 m, 5 006 à 60 et 120 m | 5 006 à 30 m, 2 500 à 60 m | 5 006 dès 12 m, 2 500 à 30 m |
| Maison | mêmes paliers (10 682, 5 340, 2 670) | idem | idem |
| Rôdeur (une pose) | 2 440 jusqu'à 30 m, 1 222 à 60 m, 612 à 120 m | 1 222 à 12 m, 612 à 30 m, 308 à 60 m | 612 dès 12 m |

- Au seuil par défaut (1 px), Godot ne simplifie que tard. Le seuil se réglera à l'œil en C4c.
- Chargement des 312 modèles (GDScript headless, deux passes) : `.scn` 0,16 à 0,17 s, .glb 0,42 à 0,43 s, sans même le calcul des normales que le jeu ajoute au .glb.

**Vérifications**
- Planches de 8 modèles (arbre, maison, kiosque, Grand Cerf, rôdeur, facteur, grille, grue) lues depuis les .glb puis depuis les .scn : 0 pixel différent sur 3 planches, 8 à 36 sur 3 autres, environ 1 000 sur la grille (nuance d'ombrage de deux barreaux, regardée en zoom : invisible).
- `dotnet build` : 0 avertissement. Smoke test vert.
- Bac à sable capturé (`relief_sandbox.sh -- --capture`), décors chargés depuis les .scn convertis au lancement : rendu identique à C3b, images regardées.
- Pas de banc de FPS : le coût en jeu se mesurera en C4c.

**Écarts au découpage**
1. Format `.scn` au lieu de `.res` : une PackedScene garde les poses en enfants.
2. Les triangles de chaque niveau vont dans `lods.json`, pas dans l'index de l'export.
3. `ModelSheet` lit aussi les `.scn`, en plus de `LoadModel`.
4. Le `.cs.uid` de `ModelConvert.cs` n'est créé ni par l'import headless ni par l'éditeur headless (les scripts récents du bac à sable n'en ont pas). Il a été généré par `ResourceUID.create_id()` au format de Godot.

**Points ouverts**
- Le seuil d'erreur des niveaux de détail, à régler en C4c.
- Le poids des .scn (+16 % sur les .glb), sans conséquence tant que la bibliothèque n'est pas versionnée.
- Les 48 décors en dépassement de budget (C3c) : les niveaux de détail les allègent de loin, l'effet se verra en C4c.

**Suite** : C4b (triangles et budget écrits sur les planches), puis C4c (une terre dans le bac à sable).

### C4b livré — 17 novembre

Le budget sur les planches de la bibliothèque.

**Fait**
- `ModelSheet --library` (`tools/model_sheet.sh <dossier> --library build/models`) écrit sous chaque nom la ligne « triangles / budget », lue dans l'index de l'export. Un modèle grossi pour tenir son budget porte son facteur (« x1,03 »).
- Un modèle hors budget est écrit en rouge (`ff6b5c`). La console compte les modèles hors budget.
- Les étiquettes sont un peu plus grandes, et une sur deux est décalée plus bas : les noms des petits modèles voisins (mains de l'Indicible, maillons de barrière) ne se chevauchent plus.

**Vérifications**
- 37 planches, 48 modèles hors budget : le même compte que l'index (C3c).
- Planches regardées : barrière (5 en rouge sur 9), Indicible (les trois yeux en rouge, les mains dans le budget), bâtiments de ville (les deux ruines en rouge), créatures (épouvantail « 15 672 / 16 000 x1,03 »), forêt.
- `dotnet build` : 0 avertissement. Outil de planche seul, pas de smoke test.

**Suite** : C4c, une terre dans le bac à sable.

### C4c livré — 17 novembre

Une terre dans le bac à sable.

**Fait**
- `tools/relief_sandbox.sh -- --biome <terre>` (forest_reclaimed, urban_ruins, collapsed_quarry, wild_fields, swamp). La table de semis du jeu en 2D (`data/props/<terre>.json` : densité par cellule, poids, décors bloquants) est appliquée aux modèles de la bibliothèque (`build/models`). Le script convertit d'abord la bibliothèque, de façon incrémentale. Nouvelle classe `ReliefBiomeProps`.
- Les modèles sont retrouvés par leur nom, leur sprite source ou leur variante écartée. Un décor plat (sans triangle) est écarté, et 0 décor manque dans les cinq terres.
- Les décors bloquants tournent d'un quart de tour, les autres librement. L'emprise d'un décor bloquant est sa boîte réduite à 60 %, en attendant C2. Les repères posés à la main (la maison) ne sont pas posés quand une terre est choisie.
- Erreur tolérée des niveaux de détail : `lod_threshold` dans `data/sandbox/relief.json` (4 px), `--lod-threshold` en jeu, 0 pour toujours le plus fin. Le HUD affiche le nombre de décors, les triangles dessinés et le seuil.
- En capture, la synchronisation verticale est coupée, et le tour du bot (hors des deux premières secondes) est mesuré : images par seconde, p99, triangles par image.

**Mesures** (forêt, 122 décors, rendu 3840×2160, charge 1 à 3, vsync coupée)

| Seuil | Triangles par image | i/s | p99 |
|---|---|---|---|
| 0 (le plus fin) | 1 120 k | 1 027 (deux passes) | 1,3 ms |
| 1 px (défaut de Godot) | 986 k | 1 030 | 1,4 ms |
| 4 px (retenu) | 618 k | 1 077 à 1 099 | 1,1 à 1,3 ms |
| 8 px | 465 k | 1 091 | 1,7 ms |

- À 4 px, les triangles dessinés baissent de 45 % et les images par seconde gagnent environ 5 %. Le rendu n'est pas limité par les triangles à cette échelle. Les décors sont identiques à l'œil entre 0 et 4 px.
- Ville : 29 décors, 211 à 265 k triangles. Carrière : 137 décors, 328 à 426 k triangles.
- Une première série était plafonnée à 140 i/s par la synchronisation de l'écran (144 Hz) : elle a été écartée, d'où la vsync coupée en capture.

**Vérifications**
- Captures de la forêt, de la ville et de la carrière regardées. Les décors sont posés debout sur le relief, les ombres sont portées, et les contours sont tenus.
- `dotnet build` : 0 avertissement.

**Écarts au découpage**
1. **Un `MeshInstance3D` par décor, pas un `MultiMeshInstance3D` par modèle.** Godot choisit le niveau de détail et écarte hors champ par instance de nœud. Une instance groupée sur toute l'arène se règlerait sur son point le plus proche, et garderait donc le plus fin partout. À 120 décors, le coût des nœuds est négligeable. Les instances groupées reviendront avec le monde (60.4), découpées par secteur.
2. **Les zones rythmées des terres (clairières, fourrés) ne sont pas appliquées**, seulement la densité et les poids globaux.
3. **La ville est clairsemée** (densité 0,06) : ses immeubles sont posés par `UrbanBuildingPlacer`, pas par la table de semis. Le sol reste l'herbe de l'arène dans toutes les terres.

**Points ouverts**
- Les immeubles de la ville, les fermes des champs et les sites de la carrière : avec le monde (60.4).
- Les emprises justes : C2.
- Les décors plats en décalques : C5.

**Suite** : C2 (emprises et collisions) ou C5 (décalques), au choix de Raphaël.

### Retours de Raphaël sur C4c — 17 novembre

Raphaël a joué la forêt. « Toutes les textures sans exception sont buggées », avec des trous béants. Les arbres ne passent pas en transparence quand ils cachent la vue. Les couleurs font « IA slop » : crade, mais pas comme ça.

**Les trous : des faces retournées.** L'orientation des faces était devinée d'après le gradient du volume au centre de chaque face (§241, C3b). Elle se trompait sur 1,4 % à 5,4 % des faces : rocher moussu 3,0 %, buisson 1,4 %, tronc du grand chêne 5,1 %, jeune arbre 5,4 %, panneau 3,5 %. Une face retournée est éliminée à l'affichage : on voit au travers du modèle, d'où des trous et des taches noires partout. La couleur de ces faces était aussi lue du mauvais côté de la surface.
- Correction : l'orientation se lit exactement dans la grille (`_nets`). Une face sépare un point dedans d'un point dehors, et elle regarde vers le dehors. La méthode par le gradient est retirée.
- Bibliothèque réexportée : 312 modèles, 0 échec, 452 s, 47,5 Mo. `assets/models` réexporté en low et mid poly. Planches et bac à sable regardés : plus aucun trou.

**Les décors qui cachent le joueur s'effacent.** Les décors low poly ont leur propre matériau (`ReliefLowPoly.SetSeeThrough`). Leurs fragments pris dans un cylindre de la caméra au joueur s'effacent en trame, de 80 % sur l'axe, adoucis sur le dernier tiers du rayon. Le rayon est `see_through_radius` dans `data/sandbox/relief.json` (1,1 cellule, 2,9 m). À la passe d'ombre, le décor reste plein et son ombre ne se troue pas. La capture ajoute la prise « occlusion », le joueur derrière un grand chêne, avec et sans effacement : le chêne s'efface, le joueur et la horde se voient.
- Différence avec le jeu en 2D : l'effacement touche la part du décor dans l'axe, pas tout le décor. Il ne demande ni index spatial ni état par décor, et coûte le même prix quel que soit le nombre de décors.
- Les décors en mid poly (les trois du réglage par défaut) n'ont pas cet effacement. La bibliothèque n'a pas de mid poly.

**Les couleurs : en attente de Raphaël.** Un essai de lissage, où chaque face prend le matériau le plus présent autour d'elle, efface les petites taches : mousse en confettis, éclats du panneau. Il ne touche ni aux grandes plaques de lierre, ni aux verts vifs. Planches comparatives sur la forêt et la carrière, à 0, 2 et 5 passes. L'essai n'est pas gardé dans le code. La direction du rendu des couleurs revient à Raphaël.

## 20. C6, le rendu des couleurs : aplats et palette terne — découpage (17 novembre)

Raphaël : « Aplats + palette terne ». Le crade viendra des formes cassées, de l'usure sculptée et de la lumière, pas des taches.

**Cause** : la couleur de chaque face vient du matériau du sprite 2D juste sous elle. L'usure peinte des sprites (mousse, rouille, crasse, fibres d'écorce, crépi) est faite pour quatre tons de pixel art. En 3D, elle retombe en confettis de faces sur des volumes lisses. Les couleurs de base des sprites sont aussi plus vives que la lumière du moteur ne le supporte.

**Défauts de l'agent, réversibles** :
- **Aplats** : les détails peints en bruit (masques `noise_mask`, et toutes les pièces `relief=False`) ne colorent plus les faces. Chaque face prend la couleur de la pièce qu'ils recouvrent. Les motifs nets (bandes, rayures, joints, ardoises) restent. Les pièces sculptées (lierre en feuilles, polypores, gravats) restent aussi : c'est de l'usure en volume. Pour l'instant les décors seuls : les créatures et personnages n'ont pas de détail peint de ce genre.
- **Lissage** : deux passes où chaque face prend le matériau le plus présent autour d'elle, pour fondre les taches restantes. Les faces émissives restent telles quelles.
- **Palette terne** à l'export, par sorte de modèle : saturation et valeur réduites, et un léger mélange vers une teinte de crasse commune. Les couleurs émissives n'y passent pas. Réglages dans `tools/sprites/lowpoly_style.json`. La palette vaut ainsi en low et en mid poly, sur les planches comme en jeu.
- **Les sprites 2D ne changent pas** : le marquage des détails peints ne touche pas leur rendu.

| Sous-lot | Contenu | Vérification | Effort |
|---|---|---|---|
| **C6a, aplats et palette** | Marquage des détails peints en bruit, lecture des couleurs sans eux, lissage, palette par sorte de modèle ; réexport de la bibliothèque et de `assets/models` | Planches avant/après de la forêt, de la carrière, de la ville et des créatures ; capture du bac à sable en forêt ; sprites 2D régénérés identiques | moyen |

### C6a livré — 17 novembre

**Fait**
- `tools/sprites/props/_surface.py` : les masques en bruit (`noise_mask`, `cracks`) sont marqués comme usure (`weathering`), et `painted`, `both` et `either` transmettent la marque. Les sprites 2D ne changent pas : la forêt et la ville régénérées sont identiques à l'octet près.
- `tools/sprites/lowpoly.py` : `ColorStyle` (aplats, lissage, palette) et `colouring_parts`. En aplats, une face prend sa couleur sans les pièces d'usure ni les pièces `relief=False`. Puis viennent les passes de lissage, sauf pour les faces émissives, et la palette terne dans `material_colors` (désaturation, valeur, mélange vers la crasse), sauf pour les lueurs. Le mid poly reçoit les aplats et la palette, pas le lissage.
- `tools/sprites/lowpoly_style.json` : décors en aplats, 2 passes, saturation 0,62, valeur 0,86, crasse `5a5246` à 12 %. Créatures sans aplats, saturation 0,7, valeur 0,9, crasse à 10 %. Personnages : saturation 0,85, valeur 0,95, crasse à 5 %.
- Bibliothèque réexportée : 312 modèles, 0 échec, 451 s, 46,2 Mo, contre 47,5 Mo (moins de frontières de matériaux, donc moins de sommets doublés). `assets/models` réexporté en low et mid poly.

**Vérifications**
- Planches avant/après de la forêt, de la carrière, de la ville, des créatures et des personnages, regardées.
  - Maisons englouties sans camouflage de lierre peint, voitures sans mousse en confettis, rochers sans taches, arbres et buissons moins criards. Le lierre et la végétation sculptés restent.
  - Perte acceptée : le panneau de danger de la carrière perd sa bande rouge et son texte, peints en détail fin.
  - Les créatures changent peu : leurs taches sont des pièces sculptées, pas de l'usure peinte.
- Bac à sable en forêt (`--biome forest_reclaimed --capture`), vue libre et vue 2D regardées : les décors sont calmes et tiennent avec le sol.
- Mesure du tour du bot : 544 k triangles par image, 1 072 i/s.

**Points ouverts**
- Le sol texturé est maintenant plus vif que les décors : sa palette suivra si Raphaël le juge.
- Les marquages lisibles (panneaux, bandes de sécurité) pourraient revenir en motifs nets, s'ils manquent en jeu.


## 21. C2, emprises et collisions des modèles — découpage (18 novembre)

Aujourd'hui, le bac à sable bloque avec la boîte entière du modèle réduite à 60 % : la canopée d'un arbre ou le toit d'un abri comptent comme s'ils touchaient le sol, et un arbre fin bloque un disque de deux mètres.

**Défauts de l'agent, réversibles** :
- **L'emprise se lit dans le maillage exporté**, pas dans les `footprint` des `PropModel`, que peu de décors déclarent. Coupe des sommets à moins de `min(0,5 m, 35 % de la hauteur)` du bas du modèle, enveloppe convexe en mètres, ramenée à 8 sommets au plus. Un arbre garde son tronc, un mur sa longueur, une maison ses murs.
- **Boîte de collision** : le rectangle orienté le plus serré autour de l'emprise (centre, demi-côtés, angle), sur toute la hauteur du modèle. **Point d'ancrage** : le centre de l'emprise, relatif à l'origine du modèle.
- **Bloquant ou non** d'après les règles du jeu (`data/world/world_gen.json`, `props`) : hauteur d'écran au moins `min_blocking_height` et nom hors `trample_prefixes`. Un décor sous `ground_decal_max_height` est noté `decal` (C5). Les pixels se ramènent aux mètres par l'échelle des sprites (1 m ≈ 17 px, hauteur vue sous 30°).
- Le tout va dans le manifeste `.json` de chaque modèle (bloc `ground`) et dans l'index. Le bac à sable bloque avec la boîte de collision ; la table de la terre (`blocking` de `data/props`) garde le dernier mot, comme en 2D.

| Sous-lot | Contenu | Vérification | Effort |
|---|---|---|---|
| **C2a, emprises au manifeste** | Calcul à l'export, bloc `ground` du manifeste et de l'index ; lecture dans le bac à sable | Emprises dessinées sur les planches de la forêt et de la ville ; tour du bot et capture du bac à sable en forêt | faible |

### C2a livré — 18 novembre

**Fait**
- `tools/sprites/lowpoly_ground.py` : emprise (coupe basse, enveloppe convexe, 8 sommets), boîte de collision orientée d'aire minimale, ancrage, et règle du jeu. La hauteur d'écran est l'étendue des sommets vus sous 30° (1 m ≈ 17 px), contour de 2 px compris, comme `VisibleHeight` en 2D.
- `tools/export_lowpoly.py` écrit le bloc `ground` dans le manifeste de chaque décor et dans l'index : `footprint`, `anchor`, `collision` (centre, demi-côtés, angle, hauteur), `blocking`, `trampled`, `decal`, `screen_height_px`.
- Bac à sable : `ReliefLowPoly.AddCollision` bloque avec la boîte tournée du décor, en chaîne de cercles le long du grand côté ; un modèle sans bloc `ground` garde l'ancienne boîte. `ReliefBiomeProps` : bloquant si la table le veut et si le manifeste le juge assez haut ; rotation libre pour tous.
- Planches de la bibliothèque : emprise en jaune et boîte en rouge (bloquant) ou gris, tracées au sol.
- Bibliothèque réexportée : 312 modèles, 0 échec, 439 s. `assets/models` en low et mid poly.

**Vérifications**
- 279 décors avec emprise : 238 bloquants, 12 piétinés, 9 décalques.
- Règle de blocage comparée à la hauteur des sprites 2D sur les 96 décors qui en ont un au même nom : même verdict pour 90. Les 6 autres sont à 1 à 2 px du seuil de 18 px, plus la salière, plate en 3D. Écart médian de hauteur −2,2 px.
- Demi-côtés des arbres : 0,3 à 0,5 m pour les arbres moyens, 0,8 m pour les grands, contre un disque de deux mètres avant.
- Planches de la forêt et de la ville regardées : troncs seuls, murs des maisons, longueur des clôtures et du portail. Les abris bloquent sous leur toit.
- Bac à sable en forêt, une capture : 111 décors, tour du bot complet jusqu'au 5e étage, 993 i/s, 497 k triangles par image.

**Points ouverts**
- Les décors creux (abris, préaux) pourraient bloquer par leurs seuls poteaux, si on veut passer dessous.
- Le jeu en 3D lira ces emprises à la place du manifeste 2D quand le monde passera en 3D (60.4).

## 22. C5, les sols des terres et les décors plats — découpage (18 novembre)

Le bac à sable n'a qu'un sol, celui de la forêt d'essai, quelle que soit la terre semée ; les décors plats (flaques, marelle, rails, salines) en sont absents.

**Défauts de l'agent, réversibles** :
- **Les 22 matières du sol 2D** (`tools/generate_ground.py`, `MATERIALS`) se rendent en textures carrées sans couture par le générateur du relief : mêmes tons, mêmes parts, mêmes motifs semés, dallages et joints usés, à 16 px par cellule (formes deux fois plus petites qu'en tuiles de 32 px). Une frange d'herbe par matière. Sortie dans `assets/models/textures/ground/`. Les matières restent définies à un seul endroit : le générateur du relief importe `MATERIALS`.
- **Une terre choisit ses sols** d'après `data/biomes/<terre>.json` : le sol bas prend la matière du terrain le plus pesant (`terrain_weights`, sans eau ni béton), les plateaux de 1 à 3 étages la suivante. Les marches, la roche des hauts plateaux et les flancs ne changent pas.
- **Décors plats en décalques** : le sprite 2D du décor, déplié du regard à 30° (hauteur × 2), posé à plat sur le sol, au plus proche et en découpe alpha. Le Compatibility n'a pas de nœud `Decal` ; un quadrilatère au sol suffit sur un sol plat.
- La palette du sol ne change pas (point ouvert de C6a), en attendant l'avis de Raphaël.

| Sous-lot | Contenu | Vérification | Effort |
|---|---|---|---|
| **C5a, sols des 22 matières** | Textures et franges par matière ; `--biome` du bac à sable choisit le sol et les plateaux de la terre | Planche des 22 matières pavées 2 × 2 (raccords) ; capture du bac à sable en forêt et dans une autre terre | moyen |
| **C5b, décalques des décors plats** | Les décors plats de la table de la terre semés en quadrilatères au sol | Capture d'une terre qui en a (ville, marais) | faible |

### C5a et C5b livrés — 18 novembre

**Fait**
- `tools/generate_relief_textures.py` rend les 22 matières de `generate_ground.py` (importées, définies à un seul endroit) en textures de 64 px sans couture, 16 px par cellule, et une frange par matière : 44 PNG dans `assets/models/textures/ground/`. Rendu : fond à quatre tons aux parts du jeu, formes réduites de moitié, dalles et joints usés, motifs semés et motifs centraux. `--materials-sheet` : planche des 22 matières pavées 2 × 2.
- Le générateur était cassé depuis §238 : le réglage n'avait plus les couleurs de la roche, des marches et des flancs. Elles reviennent en défauts dans le générateur ; les 8 textures du relief ressortent identiques à l'octet.
- `ReliefTexturedGround.UseLand` : avec `--biome`, le sol bas prend la matière du terrain le plus pesant de la terre, les plateaux herbus la suivante, franges comprises. Marches, hauts plateaux et flancs ne changent pas. Forêt : `foret_terre` et `foret_sol` ; champs : `champs_sol` et `champs_bosquet` ; ville : `ruines_sol` et `foret_sol` ; carrière : `carriere_cristal` et `carriere_roche` ; marais : `marecages_sol` seul.
- Décalques (C5b) : un décor plat de la table de la terre (sans triangle, ou `decal` au manifeste de C2) se pose en quadrilatère au sol, son sprite 2D déplié du regard à 30°, découpe alpha, sans ombre. Seuls les champs (deux flaques) et le marais (nénuphars) en ont dans leur table ; marelle, rails et flèches viennent d'autres placeurs, pas encore dans le bac à sable.

**Vérifications**
- Planche des 22 matières regardée : raccords sans couture, dalles du trottoir, du carrelage et de la place, blé en rangs, cristaux, eau.
- Bac à sable, une capture des champs et une de la forêt, regardées : sols et plateaux de chaque terre, flaques à plat dans les champs.
- Smoke test vert ; `dotnet build` sans warning.
- Mesure du tour du bot : champs 702 i/s, 353 k triangles ; forêt 679 i/s, 497 k triangles, contre 993 i/s à C2 avec les mêmes triangles.
- Repris en A/B machine calme (charge 0,5 au départ), le même soir, en alternant sol de la terre et ancien sol d'essai, mêmes décors : 1 079 et 1 089 i/s contre 1 091 et 1 087 i/s. Le sol de la terre ne coûte rien ; la baisse venait de l'état de la machine.

**Points ouverts**
- Le sol des champs est vif à côté des décors en palette terne (point ouvert de C6a) : sa palette suivra si Raphaël le juge.
- La forêt a maintenant un sol bas en terre brune : c'est le poids du terrain forestier de la terre (60 %).

## 23. 60.0e, le moteur de rendu : Compatibility contre Forward+ — découpage (18 novembre)

Le quatrième choix de fin de 60.0 (§7) n'a encore aucune mesure. Le bac à sable se lance tel quel sous l'un ou l'autre moteur (`tools/relief_sandbox.sh --rendering-method forward_plus -- …`).

**Défauts de l'agent, réversibles** :
- **Banc** : le tour du bot du bac à sable, en forêt (`--biome forest_reclaimed`), 120 créatures (`--enemies 120`), 3840 × 2160, synchronisation de l'écran coupée. Quatre passes alternées (Compatibility, Forward+, Compatibility, Forward+), machine calme, même graine. Images par seconde, p99 du temps d'image, triangles.
- **Images** : les mêmes vues regardées sous les deux moteurs (couleurs de sommet, ombres, contours, effacement des décors en trame).
- Rien ne bascule : le projet reste en Compatibility. Le choix revient à Raphaël, avec la caméra, le style et l'échelle.

| Sous-lot | Contenu | Vérification | Effort |
|---|---|---|---|
| **60.0e, le banc des deux moteurs** | Mesure et comparaison d'images ; correctifs seulement si un moteur ne rend pas le bac à sable | Quatre passes, chiffres et images | faible |

### 60.0e livré — 18 novembre

**Fait**
- Forward+ rend le bac à sable sans erreur ni avertissement (Vulkan 1.4, Radeon RX 6950 XT), contours d'écran et effacement des décors en trame compris.
- Ses couleurs de sommet sortaient délavées : il relit en linéaire les couleurs écrites en sRGB pour le Compatibility (§8). Le shader des modèles (`lowpoly_flat.gdshaderinc`) les ramène en linéaire sous Forward+ seulement (`CURRENT_RENDERER`). Les matériaux standard à couleur de sommet (relief, herbe, planches) prennent le drapeau sRGB, sans effet en Compatibility. Le rendu Compatibility ne change pas.

**Mesures** (tour du bot, forêt, 111 décors, 120 créatures, 3840 × 2160, synchronisation coupée, charge 1,4 à 3,4, passes alternées)

| Moteur | i/s | p99 | Triangles par image |
|---|---|---|---|
| Compatibility | 865, 896, 895 (moyenne 885) | 1,7 à 1,8 ms | 625 à 631 k |
| Forward+ | 836, 810, 820 (moyenne 822) | 1,7 ms | 649 à 654 k |

- Forward+ coûte environ 7 % d'images par seconde sur ce banc, sans pire temps d'image plus haut. Il dessine 4 % de triangles de plus : ses niveaux de détail ne retombent pas tout à fait aux mêmes distances.
- Images regardées côte à côte (poursuite, relief 2D, effacement derrière un arbre) : mêmes formes, mêmes ombres, mêmes contours. Après correction des couleurs, Forward+ rend un rien plus sombre et moins contrasté.

**Pour le choix de Raphaël**
- Sur cette machine, les deux moteurs tiennent plus de 800 i/s en 4K : le coût ne départage pas.
- Forward+ ouvrirait ce que le Compatibility n'a pas : décalques, lumières nombreuses, brouillard volumétrique, SSAO. En retour : Vulkan ou Metal exigés, et un rendu à vérifier sur Mac (plan 53).
- Non mesuré : un Mac, une carte modeste.

## 24. Retours de Raphaël sur C2a, C5 et 60.0e — 18 novembre

Raphaël a joué le bac à sable : « les collisions c'est top, le marais aussi (a part les textures qui ont toujours des trous béants parfois) ». Sur le moteur : « ok moi ca me va de passer à forward+ ».

### Les trous des pièces minces fermés — 18 novembre

**La cause.** Un décor à pièces minces se maille en deux surfaces (§18, C3b) : la grossière, privée des cellules saisies par la pièce mince, et la fine, limitée à son voisinage. Chacune est coupée par un masque. Leurs bords se suivent sans se toucher, et entre eux on voit au travers du modèle. Le marais est fait presque tout entier de pièces minces (roseaux, racines, planches, filets, cordages) : 47 modèles sur 57 avaient des bords ouverts, 4 743 arêtes en tout. Un modèle sans affinage n'en a aucune.

**Le correctif** (`_cap_holes`, `tools/sprites/lowpoly.py`). Chaque bord ouvert des deux surfaces se referme par un éventail autour de son centre, avant la coupe en triangles. Les deux surfaces, fermées, se recouvrent au lieu de laisser une fente. Un bord se lit comme une arête parcourue plus souvent dans un sens que dans l'autre, pour tenir aussi les nappes qui se touchent. Les boucles qui repassent par un même sommet se détachent une à une.

**Mesures**
- Marais : 4 743 arêtes ouvertes avant, 0 après. Triangles : +4,0 % (119 936 à 124 734).
- Bibliothèque réexportée : 312 modèles, 0 échec, 450 s, 46,5 Mo, 0 arête ouverte. `assets/models` réexporté en low et mid poly.
- Bac à sable du marais capturé sous Forward+ : racines, lanternes, roseaux et arbres morts pleins.

**Point ouvert** : un éventail sur un bord tordu peut laisser une petite facette visible là où la pièce mince rejoint le corps. Rien de tel vu sur les captures.

### Le bac à sable passe en Forward+ — 18 novembre

`tools/relief_sandbox.sh` lance Forward+ par défaut ; `--rendering-method gl_compatibility` avant `--` revient au Compatibility. Le jeu 2D (`project.godot`) reste en Compatibility jusqu'à ce que la 3D entre dans la vraie run : ses shaders, son préchauffage et ses bancs y sont réglés, et il ne gagnerait rien à changer. Les planches des modèles restent en Compatibility.

## 25. Le sol en palette terne — 18 novembre

Raphaël, sur la palette du sol : « moi je dirai plutot terne ». Le sol restait vif à côté des décors ternis (§20).

**Fait**
- `tools/generate_relief_textures.py` passe toutes ses textures (les 8 du relief et les 44 des matières) par la palette terne d'un bloc `ground` de `tools/sprites/lowpoly_style.json` : saturation 0,6, valeur 0,84, 14 % vers la crasse `5a5246`. Un peu plus terne que les décors (0,62, 0,86, 12 %) : le sol recule, les décors se détachent. La formule est commune aux modèles (`dulled`, `tools/sprites/lowpoly.py`) ; les modèles ressortent identiques.
- Les touffes d'herbe prenaient une couleur fixe (`lowpoly.colors` de `data/sandbox/relief.json`), jaune olive sur le sol bleu-vert du marais. Elles prennent désormais la teinte moyenne de la texture sous elles (`ReliefTexturedGround.Tone`), éclaircie de 20 à 45 %.

**Vérifié** : planche des 22 matières regardée (tons distincts, motifs lisibles). Bac à sable capturé en forêt et au marais sous Forward+ : sol, décors et herbe dans la même gamme. En forêt, les touffes sur la terre brune se lisent en herbe sèche.

**Point ouvert** : l'herbe reste sombre sous l'éclairage de son matériau ; à reprendre avec la lumière (Forward+).

## 26. 60.0f, ce que Forward+ apporte — 18 novembre

Raphaël a choisi Forward+ pour la 3D (§24, DECISIONS §247), et a dit « Oui, vas-y » pour en voir les effets dans le bac à sable.

**Découpage**
- 60.0f1 : ombres de contact (SSAO) et brume volumétrique, réglées dans `data/sandbox/relief.json` (bloc `atmosphere`).
- 60.0f2 : lumières, une par projectile en vol et une par lueur de décor (surface émissive du modèle : lanternes, champignons, cristaux), plus un halo (glow).
- 60.0f3 : bascule de chaque effet en jeu (F5, F6, F7), prises de capture comparables, coût mesuré effet par effet.

### 60.0f livré — 18 novembre

**Fait**
- `tools/tests/ReliefAtmosphere.cs` porte les trois effets. Sous Compatibility, il ne fait rien et l'affichage le dit.
- Les lueurs se lisent dans les modèles : centre et teinte moyenne de chaque surface émissive de la pose de repos (`ReliefLowPoly.Model.Glows`). 48 au plus sont allumées. Au marais : 14 lueurs de décor.
- 64 lumières de projectile, sans ombre, qui suivent les tirs.
- `--effects none` ou `ssao,fog,lights` choisit les effets du lancement. La capture ajoute cinq prises (`forward_sans_effets`, `_ombres_contact`, `_brume`, `_lumieres`, `_tout`), bot arrêté.
- Réglages retouchés après une première revue des images : ombres de contact plus fortes (rayon 1, intensité 4), brume plus sombre (`675f7a`, densité 0,02 par cellule), lueurs moins fortes (énergie 1,2, portée 2,2).

**Mesures** (tour du bot, forêt, 111 décors, 120 créatures, 3840 × 2160, Forward+, charge 1,6 à 2,5)

| Effets | i/s | Temps par image | p99 |
|---|---|---|---|
| aucun | 795, 786, 780 (moyenne 787) | 1,27 ms | 1,8 à 1,9 ms |
| ombres de contact | 619 | 1,62 ms (+0,35) | 2,3 ms |
| brume | 747 | 1,34 ms (+0,07) | 1,9 ms |
| lumières et halo | 564 | 1,77 ms (+0,50) | 2,3 ms |
| les trois | 452, 449 (moyenne 450) | 2,22 ms (+0,95) | 2,9 ms |

- Les trois ensemble coûtent près d'une milliseconde par image en 4K. On reste à 450 i/s sur cette machine, très au-dessus de 60.
- Les lumières coûtent le plus, sans doute par le halo plein écran plus que par les lumières elles-mêmes. Pas séparé.
- La brume ne coûte presque rien.
- Après la relecture du code, la brume ne vaut plus qu'en caméra libre et y remplace le brouillard de profondeur ; les mesures ci-dessus les avaient tous deux. Seules les 48 premières lueurs posées s'allument, pas les plus proches du joueur.

**Vérifié** : captures du marais regardées. Sans effet, la scène est plate. Avec les trois, la brume voile le ciel en mauve sombre, les lueurs vertes des champignons éclairent le sol, les troncs et un mur voisin, et les ombres de contact ancrent la horde. Aucun artefact. Smoke test vert.

**Non vérifié**
- Le bruit temporel des ombres de contact et de la brume (images fixes seulement).
- Le coût sur une carte modeste, et sur Mac.

**Pour le choix de Raphaël** : quels effets garder, et à quelle force. Tous sont allumés au lancement du bac à sable.

## 27. Le facteur refait de zéro — 19 novembre

Raphaël, après avoir joué le bac à sable : « le sprite du personnage [...] il est atroce. il faut le refaire de 0 et lui donner un vrai caractère ». Le modèle d'avant était un bonhomme en capsules, droit, sans nez, aux yeux en trous, dont les lettres trop minces s'émiettaient en confettis.

**Découpage**
- P1 : le facteur resculpté dans le SDF (`tools/sprites/characters/facteur.py`), jugé en 3D, planches et bac à sable.
- P2, après l'accord de Raphaël : ses sprites 2D régénérés depuis le même modèle (cadre à revoir), capturés en vraie run.

### P1 livré — 19 novembre

**Le personnage** : Aimé Ribot, un vieil homme sec et têtu, voûté par des années de tournée.
- Silhouette longue penchée en avant, cou tendu, genoux pliés de patineur.
- Tête : long nez au bout rond, moustache de morse qui tombe sous le menton, sourcils broussailleux blancs, yeux enfoncés, oreilles décollées, casquette des postes trop grande portée de travers.
- Capote d'uniforme jusqu'aux genoux, ouverte devant et fendue derrière, col relevé, pièce rapportée dans le dos, ceinture, deux rangées de boutons de laiton.
- Une longue écharpe rouille flotte derrière lui. Vu de dos, comme la caméra le voit, c'est elle qui dit l'élan.
- Sacoche énorme et bombée, lettres en éventail, une liasse ficelée de rouge, une lettre toujours tenue à la main droite.
- Patins bricolés : planche épaisse et deux grosses roues.
- Allure : penché (0,32), un peu de poids, longue glissade.

**Écarts au sprite 2D**
- Les lettres envolées sont retirées. Détachées du corps, elles se lisaient en débris en 3D.
- Toute pièce fine a au moins l'épaisseur d'un pas de maillage.

**Réglages communs aux personnages**
- Pas de maillage de 1,6 à 1,2 unité, budget de 5 000 à 9 000 triangles. Le héros est le modèle vu de plus près. Le facteur sort à 7 304 triangles au plus (2,9 Mo, 27 poses, 1,62 m).
- Aplats (`plain`) pour les personnages comme pour les décors. Sans lissage des matériaux, qui effacerait les yeux.
- Les autres personnages prennent ces réglages à leur prochain export.

**Vérifié**
- Planches regardées (quatre vues agrandies et cycle de marche) : nez, moustache, sourcils et yeux se lisent ; la capote bat et l'écharpe file à la marche.
- Bac à sable capturé en forêt sous Forward+ : rendu normal, aucune erreur.
- Le mid poly est régénéré (non versionné).

**Non fait** : les sprites 2D du facteur ne sont pas régénérés (P2). `generate_character.py facteur` les changerait.

### P2 livré — 19 novembre

Raphaël, sur le nouveau modèle : « Il me va ».

**Fait**
- Les 216 sprites 2D du facteur (8 directions, 5 actions) sont régénérés depuis le même `facteur.py`. Les noms de fichiers et le nombre d'images ne changent pas, les `.import` sont gardés.
- **Cadre de 42×44 à 42×48**, pivot inchangé (21, 40). Dans l'ancien cadre, 42 images sur 216 touchaient le bas : les grosses roues à la marche vers le sud, et le corps couché de la mort, qui descend jusqu'à 6 px sous le pivot. Emprise mesurée autour du pivot : de −17 à +17 px en largeur, de −29 à +6 px en hauteur. Plus aucune image au bord.
- **`sprite_feet_offset` de 18 à 16** dans `data/characters/characters.json` (pivot moins la demi-hauteur du cadre, comme la scaphandrière).
- Le modèle 3D est réexporté, parce que le cadre borne la recherche du volume : 7 292 triangles au plus (contre 7 304), 27 poses, 1,62 m.

**Vérifié**
- La planche des huit directions est regardée : la silhouette voûtée, l'écharpe rouille, la moustache et la sacoche se lisent, et la mort entre en entier dans le cadre.
- Capture en vraie run (`--capture-character --character facteur`, graine 1002) : la marche, le dash, l'atterrissage, l'impact et la mort se jouent. Les pieds sont posés au-dessus des barres de vie, comme avant. Aucune erreur au journal.
- Smoke test vert.

**Point ouvert** : le portrait du Hub est tiré des mêmes sprites, mais il n'a pas été regardé.

## 28. La fin de 60.0 : caméra, style et échelle — 19 novembre

Raphaël a tranché les quatre choix du §7 :
1. **La caméra** : la caméra libre seule, en perspective et à distance fixe (recul de 4 cellules, inclinaison de 16° au départ, réglable de 4 à 72° à la souris, champ de 62°). La vue relief 2D quitte la cible : elle reste un outil du bac à sable, pas une option du joueur.
2. **Le style** :
   - corps en mid poly ;
   - contours noirs, sans ombrage toon ;
   - sol texturé (§238) ;
   - aplats en palette terne (§243, §248) ;
   - les trois effets de Forward+ (§249, §250).
3. **Le moteur** : Forward+ (§247).
4. **L'échelle** : à essayer, « tester d'augmenter les décors et les cellules ».

### L'essai de l'échelle

Depuis le §237, les corps sont agrandis ×1,35 et les décors non. Le facteur mesure 2,2 m dans un monde où une cellule fait 2,65 m et un étage 1,33 m. Le jeu est donc à l'échelle des corps, plus petit qu'eux.

**`--world-scale <w>`** (`lowpoly.world_scale` dans `data/sandbox/relief.json`, 1 par défaut) agrandit le monde autour des corps :
- la carte, les étages et les décors gardent leur taille en cellules ;
- tout ce qui se mesure au corps rapetisse de 1/w : taille et emprises, vitesses du joueur et des créatures, escalade, saut, portée et vitesse des tirs, poussées, rayon du piétinement, distance d'apparition de la horde, recul, visée et marge de la caméra, rayon où les décors s'effacent ;
- à l'écran, les corps ne changent pas ; les décors, les cellules et les étages grandissent ×w autour d'eux ;
- le relevé de `--check-edges` compte les mètres au corps.

| | ×1 (joué) | ×1,35 (essai) |
|---|---|---|
| Une cellule | 2,65 m | 3,58 m |
| Un étage | 1,33 m | 1,79 m |
| Falaises de 3 à 5 étages | 4,0 à 6,6 m | 5,4 à 9,0 m |
| Saut (1,64 m) | 1,24 étage | 0,92 étage |
| Vitesses, saut, capacités | inchangés | inchangés |
| Rebords qui collent | 0 sur 86 | 0 sur 86 |
| Rampes jusqu'à la tour | 5 étages | 5 étages |

Le saut ne franchit plus un étage entier : aucune falaise n'était sautable (§240), cela ne change rien au relief.

À jouer :

```bash
tools/relief_sandbox.sh -- --biome forest_reclaimed --world-scale 1.35
```

Comparer sans l'option.

### L'échelle retenue — 19 novembre

Raphaël : « le x1.35 je suis plutot pour le laisser ». `world_scale` passe à 1,35 par défaut : une cellule vaut 3,58 m et un étage 1,79 m, mesurés au corps.

**60.0 est clos.** Les quatre choix du §7 sont tranchés. 60.1 sort du bac à sable : la scène `Main3D` dans le jeu, puis le joueur, la caméra et les réglages en mètres. Son découpage est à proposer avant d'y coder.


## 29. 60.1, les fondations — découpage (19 novembre)

Raphaël, sur la run 2D gardée à côté de la 3D : « non. on est encore en phase de dev donc on va pas s'embeter avec ca » (§254). Le §3 change donc :
- **Le Hub lance la run 3D dès 60.1a.** Il n'y a pas de scène 2D à garder jouable ni de régressions 2D à tenir vertes.
- **Le code 2D reste dans le dépôt, gelé.** Il compile, mais personne ne le teste ni ne le corrige. Chaque système 2D part dès que son portage 3D existe, plus en un seul lot à la fin (60.8).
- **Le jeu se joue moins entre 60.1 et 60.3.** En 60.1, on marche sur la terre d'essai ; la horde arrive en 60.2 et le combat en 60.3.

### L'unité du monde

Le bac à sable compte en cellules : une unité Godot vaut une cellule, et un étage une demi-cellule. Ses constructeurs (relief, sol texturé, herbe, décors, caméra, effets), réglés et vus sur des dizaines de captures, sont écrits dans cette unité. **La run 3D la garde.** Le mètre ne sert qu'à lire et à parler, au corps (§253) : une cellule fait 3,58 m, un étage 1,79 m, le facteur 2,2 m. La grille de la génération (60.4) est la même grille de cellules.

**Les réglages en pixels se convertissent par un seul facteur**, `Units.CellsPerPixel`, tiré du rythme déjà joué : une vitesse du jeu 2D (px/s) ÷ 45,25 px par cellule × `pace` 0,67 ÷ 1,35 (l'échelle du monde). Cela donne 91 px par cellule. Les portées, rayons et vitesses gardent ainsi entre eux les rapports de la 2D.
- Chaque clé `_px` passe en cellules (`_cells`) dans `data/` au moment où son système est porté, plus en une seule passe. Un chargeur 2D encore vivant lit, en attendant, la valeur en pixels convertie.
- **Conséquence à surveiller en 60.4** : rapporté aux portées et aux vitesses, le monde est deux fois plus grand qu'en 2D. Une cellule 2D valait 45 px de jeu, une cellule 3D en vaut 91. Les tailles de la génération se règleront en jouant.

### Les sous-lots

| Sous-lot | Contenu | Vérification | Effort |
|---|---|---|---|
| **60.1a La pile 3D dans le jeu** | La pile 3D du bac à sable (relief, sol texturé, herbe, modèles et décors d'une terre, effets, simulation) déménage dans `scripts/World3D/` (`Vestiges.World3D`). Ses réglages quittent `data/sandbox/relief.json` pour `data/world3d/` (rendu, terre d'essai, caméra) et `data/movement/player_3d.json`. Le mode sprites du bac à sable, sa cuisson d'une run 2D et son relief en tuiles partent | `--check-edges` inchangé (0 rebord collé sur 86, mêmes m/s) ; capture du bac à sable identique | Moyen |
| **60.1b Le joueur et la caméra** | `PlayerMotor3D`, logique pure tirée de la simulation : marche et inertie, saut, capacité du personnage, chutes et leurs dégâts, piétinement, vide, emprises des décors. `PlayerBody3D` : le modèle mid poly animé, tourné vers sa course. `RunCamera3D` : la caméra libre du §28, avec le recul qui évite le relief et les décors qui s'effacent en trame. Le bac à sable s'en sert : un seul moteur, une seule caméra. Vitesse du personnage par `Units` | `--check-edges` sur le moteur partagé : rebords et relevé en m/s identiques ; capture du bac à sable | Moyen |
| **60.1c La run part en 3D** | Forward+ pour tout le projet. `scenes/Main3D.tscn` : la terre d'essai et ses décors, le personnage choisi au Hub, ses PV, la mort qui ramène au Hub. Le Hub et « Recommencer » lancent `Main3D`. Souris capturée en run, libérée en pause (Échap). Nouvelle action `jump` (Espace) ; `mobility` passe sur Maj et le clic droit ; à la manette, le stick droit tourne la caméra | Smoke test ; capture de `Main3D` lancée comme depuis le Hub ; Raphaël joue | Moyen |

À la fin de 60.1, on lance le jeu, on choisit son personnage et on marche, saute et use de sa capacité sur la terre d'essai, sous la caméra libre. 60.2 y amène la horde.

### 60.1b livré — 19 novembre

- **`PlayerMotor3D`** (`scripts/World3D/`) : le mouvement du joueur, tiré de la simulation du bac à sable, sans nœud ni rendu. Il gère la marche avec inertie, le saut, la capacité du personnage, les chutes et leurs dégâts, la retombée et le vide. La simulation du bac à sable ne garde que la horde : elle lit la retombée du moteur pour frapper la foule et remet les PV à zéro à la mort.
- **`PlayerBody3D`** : le modèle animé, tourné vers sa course, avec le clignotement d'invulnérabilité, le rouge de la blessure et l'ombre (`ReliefShadow`, partagée avec la horde).
- **`RunCamera3D`** : la caméra libre, avec le recul qui évite le relief et les entrées lues dans son repère. La vue relief 2D du bac à sable garde son propre cadrage orthographique.
- **Le facteur unique** des réglages 2D vers la 3D est `ReliefConfig.CellsPerPixel`, soit 91 px du jeu 2D par cellule ; la vitesse des personnages en passe. `MetresPerCellAtBody` (3,58 m) sert à lire les mesures.
- **Le contrôle des rebords teste le moteur du jeu**, plus une copie : 0 rebord collé sur 86, rampes jusqu'à la tour à 5 étages, mêmes vitesses, mêmes sauts et mêmes capacités qu'avant pour les six personnages. Il imprime maintenant les unités.
- **Capture du bac à sable** : caméra libre au pied d'une falaise et vue relief 2D identiques à 60.1a, avec 496 images par seconde au tour du bot en 4K.

### 60.1c livré — 19 novembre

- **Le Hub lance la run 3D** : `scenes/Main3D.tscn` (`Run3D`). « Recommencer », dans la pause, aussi. `GameManager.RunScenePath` nomme la scène de la run. La run 2D n'est plus lancée.
- **La run 3D** :
  - la terre d'essai, avec les sols et les décors de la forêt (132 décors, tirés de `build/models`), ou le semis du réglage si la bibliothèque manque ;
  - le personnage choisi au Hub, avec sa vitesse et sa capacité ; son modèle s'il est exporté, sinon le facteur, avec un avertissement ;
  - la caméra libre, la souris capturée en jeu et rendue à la pause ;
  - les PV et la recharge de la capacité en haut à gauche, en attendant le HUD (60.3) ;
  - la mort, par les chutes ou le vide, ramène au Hub ;
  - le menu de pause du jeu, réutilisé tel quel : ses panneaux d'équipement et de statistiques restent vides sans joueur 2D.
- **Forward+ pour tout le projet** (`project.godot`), Hub compris.
- **Entrées** :
  - `jump` : Espace, bouton A ;
  - `mobility` : Maj, clic droit, bouton X ;
  - `interact` passe sur le bouton Y pour libérer A ;
  - `camera_left/right/up/down` : le stick droit tourne et incline la caméra.
  Le saut est remappable comme les autres actions.
- **Captures** : `tools/tests/Run3DCapture.tscn` lance la run comme le Hub, fait marcher, sauter, user de la capacité, tourner la caméra et ouvrir la pause, une image à chaque étape.
- **Vérifié** :
  - `dotnet build` : 0 avertissement ;
  - smoke test vert sous Forward+ ;
  - `tools/test_input_remap.sh` vert : 0 échec, le bouton d'interaction attendu passe de A à Y ;
  - capture de la run 3D du facteur : marche, saut, glissade (recharge affichée), caméra tournée, pause, souris capturée puis rendue ;
  - capture de la run 3D du traqueur : son modèle exporté, son dash ;
  - capture du Hub sous Forward+ : même rendu qu'avant.
- **Relecture** (`godot-reviewer`) : rien de bloquant. Une image lente se découpe en pas de 1/60 s au plus, pour ne pas traverser une emprise d'un coup. Les noms d'action sont mis en cache.
- **Points ouverts** :
  - la mort ramène au Hub sans fin de run ni sauvegarde, jusqu'à 60.3 ;
  - un profil qui avait remappé `mobility` sur Espace aura cette touche en double avec le saut ; rien n'est migré.
- **Non vérifié** :
  - la manette, faute d'en avoir une branchée ;
  - Raphaël n'a pas encore joué.
