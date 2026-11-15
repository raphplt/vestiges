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
- **Les modèles naissent du pipeline actuel.** Chaque forme d'un personnage devient une pièce low poly attachée à son os : une sphère devient un icosaèdre, une capsule un cylindre à six ou huit pans. On garde le squelette et les poses déjà écrits. Les volumes faits main d'un artiste peuvent s'y substituer plus tard, un par un.
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
