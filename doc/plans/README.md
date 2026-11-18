# VESTIGES — Dossier de plans à valider

**18 novembre — les décors 3D bloquent par ce qui touche le sol ([plan 60 §21](60-vestiges-en-3d.md), §244) :**
- Chaque modèle de décor porte dans son manifeste son emprise au sol, sa boîte de collision et la règle de blocage du jeu (lot C2a). Un arbre bloque par son tronc.
- Même verdict de blocage que les sprites 2D pour 90 décors sur 96. Le bac à sable bloque avec ces boîtes ; les décors tournent librement.
- Les 22 matières du sol 2D sont rendues en textures du relief, et `--biome` habille le sol et les plateaux de la terre choisie ([§22](60-vestiges-en-3d.md), §245, C5a). Les décors plats (flaques, nénuphars) se posent en décalques (C5b).

**17 novembre — les modèles 3D se convertissent et se simplifient de loin ([plan 60 §19](60-vestiges-en-3d.md), §242) :**
- `tools/convert_models.sh` écrit un `.scn` par modèle (poses, normales lissées, niveaux de détail de Godot) : 312 modèles en 4,4 s. Chargement 0,16 s contre 0,42 s pour le .glb. Planches identiques à l'œil.
- Les niveaux de détail s'appliquent en Compatibility : arbre moyen 10 014 triangles de près, 5 006 à 60 m, 2 500 à 30 m avec un seuil de 4 px. Le seuil se réglera en C4c.
- Le bac à sable lit les `.scn` et les reconvertit au lancement. Les `.scn` ne sont pas versionnés (55,8 Mo contre 47,9 Mo de .glb).
- Les planches de la bibliothèque écrivent triangles et budget sous chaque modèle, en rouge au-delà (C4b) : 48 hors budget.
- `tools/relief_sandbox.sh -- --biome <terre>` sème les décors d'une terre (`data/props`), tirés de la bibliothèque (C4c). Forêt : 122 décors ; niveaux de détail à 4 px : 618 k triangles contre 1 120 k, environ +5 % d'images par seconde.
- Retours de Raphaël : les trous venaient de faces retournées (1,4 à 5,4 % des faces), dont l'orientation se lit maintenant dans la grille ; bibliothèque réexportée. Les décors qui cachent le joueur s'effacent en trame. La direction du rendu des couleurs est en attente de Raphaël.
- Couleurs des modèles 3D, au choix de Raphaël « aplats et palette terne » ([§20](60-vestiges-en-3d.md), §243) : l'usure peinte ne colore plus les décors, palette ternie par sorte de modèle (`tools/sprites/lowpoly_style.json`) ; bibliothèque réexportée.
- Suite : C2 (emprises) ou C5 (décalques).

**16 novembre — les budgets de triangles des modèles 3D ([plan 60 §18](60-vestiges-en-3d.md), §241) :**
- Un fichier de réglages fixe un budget par sorte de modèle (personnage 5 000, créature 4 000, grand gardien 16 000, décor 2 500 à 10 000 selon la taille). L'index de la bibliothèque note les dépassements.
- Seuls 2 modèles grossissent (épouvantail, brute du vide), planches regardées. Les décors gardent leur pas : le grossir défaisait les pièces minces. 48 décors dépassent leur budget, signalés. Bibliothèque de 48,3 à 47,9 Mo ; la cible de 40 Mo n'est pas tenue, le poids tient aux animations.
- Suite : C2 (emprises et collisions) ou C4 (import Godot, niveaux de détail), au choix de Raphaël.

**16 novembre — les pièces minces des décors 3D ne disparaissent plus ([plan 60 §18](60-vestiges-en-3d.md), §241) :**
- Le réverbère, la grue, l'arbre mort, les grillages et le mât radio sortent entiers : la grille se raffine là où une pièce plus mince que son pas passait au travers. Décors de 568 212 à 1 009 446 triangles (×1,78), bibliothèque de 40,8 à 48,3 Mo.
- Réserves notées au plan : câble de la grue et pointe du mât manquent encore, quelques épaississements et petits trous.
- Suite : C3c (budgets de triangles), sur demande de Raphaël.

**16 novembre — des modèles 3D près de sept fois plus légers ([plan 60 §18](60-vestiges-en-3d.md), §241) :**
- La bibliothèque de modèles passe de 275 à 41 Mo, sans changement visible sur les planches : sommets partagés, couleurs et indices plus petits, facettes calculées au shader. Les triangles ne bougent pas.
- Suite : C3b (les pièces minces : arbre mort, grue, antenne, grillage, réverbère), puis C3c (les budgets de triangles).

**16 novembre — toutes les falaises hautes, l'export de masse des assets ([plan 60 §17](60-vestiges-en-3d.md), §240) :**
- Dans le bac à sable, plus aucune falaise ne se saute : toutes font 3 à 5 étages, chacune avec sa rampe. À jouer : la chute coûte 20 à 50 PV.
- La conversion commence (lot C1) : `python3 tools/export_lowpoly.py --all` tire en deux minutes 312 modèles 3D de tout ce qui a une source SDF. La bibliothèque est dans `build/models/`, non versionnée.
- Planches : `tools/model_sheet.sh <dossier> --library build/models`. Restent les emprises, le poids des animations, les pièces minces, l'import Godot et les sols (C2 à C5).

**16 novembre — des falaises qu'on ne saute pas, des rampes, la conversion des assets ([plan 60 §15 et §16](60-vestiges-en-3d.md), §239) :**
- **Falaises** : la plupart font deux étages ou plus, au-delà du saut. On y monte par des rampes. Quelques gradins d'un étage restent sautables.
- **Mécanique forte** : pas de nouvelle, nouer les ingrédients. L'Effacement crée le vide, la hauteur est le refuge, on jette la horde en bas, et la Série le récompense.
- **Assets** : inventaire fait. Presque tout se convertit depuis le SDF, et cinq outils sont à écrire.

**16 novembre — un seul sol, des falaises taillées, des contours lissés ([plan 60 §14](60-vestiges-en-3d.md), §238) :**
- **Sol** : seul le sol texturé reste. F1 bascule ses falaises, taillées en facettes de roche ou droites.
- **Contours** : l'image se rend à la définition de l'écran (elle était en 1080p agrandie), et le trait est anticrénelé.
- **Megabonk** : une conduite pour s'en tenir à distance.

**16 novembre — des corps plus grands, l'herbe en bouquets ([plan 60 §13](60-vestiges-en-3d.md), §237) :**
- **Corps** : le joueur et les créatures sont 1,35 fois plus grands.
- **Caméra libre** : plus proche (4 cellules) et plus basse, comme dans Megabonk.
- **Herbe** : en rares bouquets, avec des plaques nues entre eux.

**16 novembre — le mouvement repris, l'herbe en relief ([plan 60 §12](60-vestiges-en-3d.md), §236) :**
- **Mouvement** : le facteur court à 8,4 m/s au lieu de 12, avec un peu d'inertie.
- **Saut** (Espace) : commun à tous, à hauteur d'homme (1,64 m), il garde l'élan sans pousser (5,9 m en course au lieu de 12). Les étages descendent à 1,33 m pour rester franchissables d'un saut.
- **Capacité** : chaque personnage a la sienne, tirée du jeu (Maj ou clic droit). Le facteur a sa glissade sur patins ; on change de personnage avec `--character <id>`.
- **Caméra libre** : distance fixe.
- **Sol** : textures deux fois plus grossières, et des touffes d'herbe en relief à la Megabonk.

À jouer : le mouvement d'abord.

**16 novembre — la caméra libre et le sol texturé ([plan 60 §11](60-vestiges-en-3d.md), §235) :** dans `tools/relief_sandbox.sh`, la caméra libre est la vue de départ. La souris l'incline aussi de haut en bas (de 4° à 72°), la molette règle le recul, et la caméra ne traverse plus les falaises. La 3D de haut et la pixelisation sont retirées. Le sol suit Minecraft et Megabonk : des textures pixel art (herbe, terre battue, roche, strates) lues au plus proche, avec une frange d'herbe qui pend au haut des falaises. À toi de juger ce sol, et son accord avec les corps (F2).

**15 novembre — tes choix de style ([plan 60 §10](60-vestiges-en-3d.md), §234) :** les contours restent ; la pixelisation et la vue 3D de haut partent ; la caméra libre en poursuite te plaît davantage que l'isométrique, mais il lui manque le contrôle vertical. À la reprise : la caméra libre avec contrôle vertical, puis les contours du joueur et des créatures. Le sol lisse manque de texture : on regardera comment Megabonk et Minecraft font le leur. Le détail des corps reste ouvert.

**15 novembre — les styles à basculer, avec des contours noirs ([plan 60 §10](60-vestiges-en-3d.md), §233) :** retour de Raphaël, le low poly est « trop low » et les sols en facettes ne vont pas. Dans `tools/relief_sandbox.sh`, chaque effet se bascule seul : F1 le sol (facettes, lisse, pixel art), F2 les corps (low ou mid poly), F3 les contours noirs, F4 la pixelisation, F5 l'ombrage toon. À toi de choisir la combinaison.

**15 novembre — l'arène low poly ([plan 60 §9](60-vestiges-en-3d.md), §232) :** lot 60.0b. Le bac à sable de la verticalité passe en facettes : relief en strates, le facteur et la horde en modèles animés, arbres, rochers et une maison, avec de vraies ombres. À jouer avec `tools/relief_sandbox.sh` (Tab pour les trois vues) ; `--sprites` rend l'ancien pour comparer. Prochain sous-lot : 60.0c, les caméras.

**15 novembre — les corps en low poly ([plan 60 §8](60-vestiges-en-3d.md), §231) :** lot 60.0a. Le facteur, le rampant, le rôdeur, un arbre, une maison et un rocher sont tirés en low poly des volumes des sprites : un maillage par pose, une couleur par face. Ils s'importent dans Godot sans avertissement (`python3 tools/export_lowpoly.py`, planche par `tools/model_sheet.sh <dossier>`). Prochain sous-lot : 60.0b, l'arène low poly.

**15 novembre — le plan 60, Vestiges en 3D ([plan 60](60-vestiges-en-3d.md), §230) :** neuf lots, de la caméra et du style (60.0) à la bascule (60.8). La moitié du code (sauvegardes, quêtes, Campement, audio, interface) ne bouge presque pas ; le joueur, les créatures, les effets et la sortie de la génération sont réécrits. Les personnages et créatures existent déjà en volumes 3D dans le pipeline de sprites, et deviennent les modèles low poly. Premier lot : 60.0, à jouer avant tout portage.

**15 novembre — caméra et style ([plan 59 §10](59-verticalite.md), §229) :** la caméra tourne, la vue au pixel près tombe, et Raphaël est partant pour passer du pixel art au low/mid poly, à essayer d'abord. Le plan 60 commence par un bac à sable de caméra et de style, pour valider la caméra avant tout portage.

**15 novembre — tout le jeu passe en 3D ([plan 59 §10](59-verticalite.md), §228) :** Raphaël préfère un vrai passage en 3D, logique comprise, au pont de rendu. La caméra reste un choix libre. La run 3D se bâtit à côté de l'actuelle, qui reste jouable jusqu'à ce que la 3D l'égale. Le plan 60 découpera le portage.

**15 novembre — la suite du relief ([plan 59 §9](59-verticalite.md), §227) :** la logique du jeu reste en 2D et seul le rendu passe en 3D, par un pont. D'abord R0 : la vraie run, à plat, jouée en relief 2D et en poursuite, mesurée, avec la liste de ce qui reste à porter. Puis les étages dans les bois (R1), les effets (R2), et le relief dans la génération (R3).

**15 novembre — le relief prend ([plan 59 §8](59-verticalite.md), §226) :** Raphaël trouve que le jeu « change totalement de dimension », en relief 2D comme en 3D, sans pouvoir choisir. Par défaut, le relief se bâtit sur un monde 3D rendu d'abord en relief 2D, ce qui garde la poursuite ouverte. Dans le bac à sable, on ne reste plus collé aux rebords (105 sur 107 avant, 0 après), et un haut palier et une tour ne se prennent que par leurs escaliers.

**15 novembre — le bac à sable de la verticalité, relief 2D et 3D ([plan 59 §8](59-verticalite.md), §225) :** à jouer avec `tools/relief_sandbox.sh`. Une arène en étages (gradin, butte, mesa sans escalier, arête, fosse et bord de vide), la horde qui grimpe, un bond sur Espace qui monte d'un étage et une retombée qui repousse. Une seule simulation et trois vues, à basculer avec Tab : relief 2D au pixel près, 3D de haut qu'on tourne, 3D en poursuite à la souris comme dans Megabonk. La vue 2D est rendue par le moteur 3D, ce qui règle le tri des falaises. C'est la voie à suivre si l'aspect 2D est gardé. Après un premier essai de Raphaël : la horde passe à 35, et le décor est un vrai biome photographié (forêt par défaut, `--land-order urban_ruins` pour la ville), sol et décors compris.

**15 novembre — les terres font vide, essai de la verticalité ([plan 59](59-verticalite.md), §224) :** retour de Raphaël après avoir joué les terres d'un seul biome : des écrans entiers vides, et des décors en plus n'y changeront rien. Il propose des étages et un saut, comme dans Megabonk. L'agent recommande un relief en 2D isométrique, d'abord en bac à sable, avec un critère d'arrêt (écran plein, envie de bondir, lisibilité dans la horde). Le dash devient le bond. Faits le jour même : raviver un Mémorial passe de 20 à 12 s, et l'objectif de la terre revient en deux cases à cocher sous les PV.

**12 novembre — Saint-Aubin et son parvis ([plan 58 T3b1](58-terres-completes.md), §221, §222) :** la Haute-Ville a son cœur. L'église en grand, intacte ou ruinée, avec un clocher-porche 2,4 fois plus haut qu'un immeuble, une horloge arrêtée sur 23 h 10, une flèche d'ardoise, une nef et une abside. Sur le parvis dallé : le monument aux morts, la corde de cloche coupée au pied du clocher, des tilleuls et des bancs. Le Cerf y dort, et le bot le vainc aux trois graines. Les églises de quartier du semis sont retirées. Retour de Raphaël : les rues sont trop linéaires. T3e (diagonales, parcs, places en étoile) passe avant la mairie.

**12 novembre — le parvis et la mairie découpés ([plan 58 §20](58-terres-completes.md), §220) :** deux sous-lots. T3b1 : Saint-Aubin en grand, avec un clocher deux fois et demie plus haut qu'un immeuble, et son parvis comme cœur de la ville (monument aux morts, corde de cloche coupée, repaire au pied du clocher). Les deux églises du semis partent. T3b2 : la place de la mairie, avec l'horloge arrêtée sur 23 h 03, une fontaine sèche, un kiosque et un panneau de faire-part d'oubli.

**12 novembre — la Montée avant la carrière et les champs ([LE-JEU](../LE-JEU.md), §219) :** demandé par Raphaël. Une fois la Haute-Ville finie (T3b à T3d), on fait la Montée et l'Indicible en finale (étape 6), pour qu'une run aille de bout en bout avant les testeurs. La carrière (T4), les champs (T5) et les gardiens G3 à G5 viennent après.

**11 novembre — la grille de la Haute-Ville qui ondule et casse ([plan 58 T3a](58-terres-completes.md), §218) :** la grille se règle en JSON. Les rues sont plus ou moins espacées ; certaines décrochent, fusionnent deux îlots, finissent en impasse ou s'effondrent en cratère. Des avenues plantées de tilleuls et des places en quinconce s'y ajoutent, et les pièces trouent la grille. Le square est la première pièce de la ville. Mesure (Facteur, trois graines) : densité inchangée. À la graine 11, le bot ne reste plus pris dans la ville.

**11 novembre — la Haute-Ville découpée ([plan 58 §18](58-terres-completes.md), §217) :** quatre sous-lots. T3a : la grille qui ondule et casse. T3b : le parvis de Saint-Aubin et la place de la mairie. T3c : l'école et la poste. T3d : l'arrêt de la ligne 7 et la mesure.

**11 novembre — les maisons du port, le Bas-Port est fait ([plan 58 T2d](58-terres-completes.md), §216) :** trois maisons de pêcheurs basses en rangée, des filets, des casiers, une boîte aux lettres qui déborde de courrier tamponné. Le Bas-Port a quatre pièces autour de la cale et en pose trois ou quatre par run. Ses décors remarquables sont ceux du port, dont un feu de port neuf. Mesure d'ensemble (cinq graines) : le bot voit deux pièces du Bas-Port au lieu d'une ; il ravive ses Mémoriaux 23 s plus tard et quitte la terre 16 s plus tard ; aucune mort. Suite : la Haute-Ville (T3), à découper.

**11 novembre — les marais salants et la digue rompue ([plan 58 T2c](58-terres-completes.md), §215) :** une pièce peut désormais poser de l'eau en bassins rectangulaires (`water`). Les marais salants alignent six salines plaquées au sol, avec vannes, tas de sel gris, las, brouette et cabane du saunier. La digue rompue barre la terre d'un talus de pierres sèches, avec la mer revenue au nord et une brèche par où l'eau passe encore. L'autel noyé part. Neuf décors neufs. Le calage du vide des quais se fait dès le choix de l'emplacement : 17 cellules à chacune des dix graines. Le Bas-Port pose ses trois pièces à chaque run. Suite : T2d, les maisons du port et la mesure d'ensemble.

**11 novembre — la cale du chantier naval et les quais ([plan 58 T2b](58-terres-completes.md), §214) :** le cœur du Bas-Port devient la cale : un chalutier bleu délavé, étayé sur sa cale de halage, le treuil, la cabane du chantier, des planches. Le Cerf dort au pied de la coque. Les quais remplacent le ponton : une bordure de granit longe la mer oubliée, un pan de vide, avec des pavés, des bittes et une grue dont la flèche passe au-dessus du Néant. Onze décors neufs. Un pan de vide se cale désormais sur la grille d'Effacement, et son bord tombe au même endroit à chaque run (le belvédère des bois aussi). Les éléments de lieu gagnent la clé `flat`. Au bot, la densité ne bouge pas après la première minute. Suite : T2c, les marais salants et la digue.

**11 novembre — le terrain du Bas-Port ([plan 58 T2a](58-terres-completes.md), §213) :** des chenaux sinueux traversent la terre, coupés de gués ; là où un chemin les croise, une passerelle de planches dans l'un des quatre sens. Trois zones : roselières, prés salés, vasières. Cinq décors neufs : passerelle, roseaux hauts, casiers, filet qui sèche, pieux. `SwampPropPlacer` et ses placements en dur sont retirés. Au bot, raviver les trois Mémoriaux prend 12 à 17 s de plus, et la densité de créatures ne bouge pas. Suite : T2b, la cale et les quais.

**11 novembre — le Bas-Port découpé ([plan 58 §13](58-terres-completes.md), §212) :** quatre sous-lots, chacun capturé et mesuré. T2a, le terrain (chenaux, passerelles, roselières, vasières). T2b, la cale du chantier naval au cœur, et les quais qui bordent la mer oubliée, un pan de vide. T2c, les marais salants et la digue rompue. T2d, les maisons du port et la mesure. Suite : T2a.

**10 novembre — le viaduc effondré, les bois sont faits ([plan 58 T1d](58-terres-completes.md), §211) :** cinq décors neufs (piles, tronçons de tablier tombés, panneau d'autoroute) composent la quatrième pièce des bois, qui en pose trois ou quatre par run. La mesure compte les pièces vues : le bot en voit une à trois aux bois, contre une avant T1. Le bot des terres contourne enfin un obstacle près de sa cible. Suite : T2, le Bas-Port.

**10 novembre — l'aire de pique-nique et la maison engloutie ([plan 58 T1c](58-terres-completes.md), §210) :** neuf décors neufs aux bois (tables, balançoire, barbecue, poubelle, boîte aux lettres, maison mangée de lierre, voiture-jardinière). Deux pièces les composent et remplacent les démonstrations de T0 : les bois posent trois pièces faites main par run. Suite : T1d, le viaduc effondré et la mesure des pièces vues.

**10 novembre — la clairière du Cerf et le belvédère ([plan 58 T1b](58-terres-completes.md), §209) :** dix décors neufs aux bois (pierres dressées, lit de mousse, table d'orientation, bancs, rambarde, longue-vue). Le Cerf dort dans un cercle de pierres ouvert à l'ouest. Le belvédère borde un pan de vide creusé dans la terre (clé `void` d'une pièce), où pousser la horde ; aucun chemin n'y mène. Suite : T1c, l'aire de pique-nique et la maison engloutie.

**10 novembre — les bois prennent du rythme ([plan 58 T1a](58-terres-completes.md), §207, §208) :** un bruit lent découpe la terre des bois en clairières, sous-bois et fourrés, réglés en JSON (`rhythm` dans le fichier de décors du biome) et réutilisables par les autres terres. Les clairières ouvrent le bois, les fourrés le serrent avec des trouées. Les décors augmentent de 11 %, la densité de créatures ne bouge pas, et le bot vainc le Cerf aux deux graines. Suite : T1b, la clairière du Cerf et le belvédère.

**27 octobre — la grammaire des pièces ([plan 58 T0](58-terres-completes.md), §206) :** chaque terre pose désormais un cœur autour du repaire et trois ou quatre pièces faites main (`data/world/land_pieces.json`). Chaque pièce a ses variantes, son sol et la place d'un Mémorial ; deux Mémoriaux sur cinq s'y logent. Des décors remarquables sont garantis sur les bois, le marais et la carrière. Les pièces de démonstration reprennent des décors existants et restent maigres ; la densité de créatures ne bouge pas. Suite : T1, les bois de la colline.

**27 octobre — le chantier des terres ([plan 58](58-terres-completes.md), §205) :** les cinq terres vues d'en haut, une par une : la forêt est un semis uniforme, les champs répètent une ferme, la ville est une grille, le Bas-Port est presque vide, la carrière un bruit de cailloux. Chaque terre aura un cœur, trois à cinq pièces faites main où se logent les Mémoriaux, et un terrain rythmé. Premier lot : T0, la grammaire des pièces.

**27 octobre — l'Épouvantail garde les champs ([plan 57 G2](57-chemin-et-gardiens.md), §204) :** deuxième gardien à part. Un épouvantail planté sur son pieu, faux rouillée et corbeaux. Son fauchage couvre un disque autour de lui, sauf une brèche à 120° du joueur, d'un côté puis de l'autre. Au bot, ses combats durent 90 et 130 s. Suite : G3, le Tocsin.

**27 octobre — raviver tient en haleine ([plan 57 C4](57-chemin-et-gardiens.md), §203) :** le cercle d'un Mémorial a son horloge (anneau qui se remplit, « Tiens le cercle · 12 s », rouge hors du cercle), deux vagues annoncées au sol, un resserrement au dernier tiers et une onde qui balaie la foule quand il se rallume. Au bot, le ravivage n'est pas plus lent mais trois fois plus disputé à la graine 7. Suite : G2, l'Épouvantail.

**27 octobre — le HUD se tait ([plan 57 C3](57-chemin-et-gardiens.md), §203) :** plus de ligne « Terre 1 · Mémoriaux ravivés 0/3 » en permanence ni de flèche d'objectif ; le compteur tient en quatre icônes sur la minimap, le haut de l'écran ne parle qu'aux changements et dans la dernière minute avant la rage. Suite : C4, raviver un Mémorial qui tient en haleine.

**27 octobre — visiter les cinq terres ([DEV-MODE](../DEV-MODE.md), §202) :** `tools/play_lands.sh` ouvre une run dont les terres se suivent dans un ordre fixe (bois, fermes, Haute-Ville, Bas-Port, carrière) ; F4 › Terres passe à la suivante ou refait celle-ci dans un autre biome. Pour que Raphaël voie ce qui existe avant le découpage de l'étape 5.

**27 octobre — ce qui restait flou sur le chemin du gardien ([plan 57 C2](57-chemin-et-gardiens.md), §201) :** Mémoriaux et repaire sur la minimap dès l'arrivée, épinglés au bord du radar ; dans la dernière minute avant la rage, l'objectif rouge compte à rebours ; au réveil, l'arène s'ouvre au plus à 40 créatures. Au bot, la 3e terre est enfin vaincue (trois fois sur cinq à la graine 7, de 40 à 256 s). Suite : G2 à G5, puis l'étape 5, les terres complètes.

**27 octobre — le Grand Cerf fini ([plan 57 G1](57-chemin-et-gardiens.md), §200) :** corps procédural à sa taille (écorce, crâne blanc, ramure morte), cinq animations dont l'élan, charges en chaîne en deuxième phase (trois charges annoncées, la dernière ouvre le vide), mort mise en scène, et le Cerf couché au repaire au lieu de la stèle. Au bot, combats des terres 1 et 2 de 74 à 233 s ; la 3e terre reste hors de portée du bot, à revoir avec C2. Suite : C2.

**25 octobre — le gardien devient une créature à part ([plan 57 G0](57-chemin-et-gardiens.md), §199) :** rang `guardian`, nommé par chaque biome, refusé dans les foules ; coups choisis dans sa fiche, PV de sa fiche par rang, plus de Souverain grossi. Le Grand Cerf garde toutes les terres, sur un corps provisoire (le Tréant ×2) jusqu'à G1. Au bot, combats des terres 1 et 2 de 72 à 150 s, comme au B1. Suite : G1, le corps et les charges en chaîne du Cerf.

**25 octobre — le chemin du gardien et un gardien par terre ([plan 57](57-chemin-et-gardiens.md), §198) :** Raphaël n'a pas vu les gardiens : le chemin (trois Mémoriaux, le repaire) n'était dit que par une petite ligne près de la minimap. Lots C1 et C2 : objectif en haut au centre, flèche d'objectif, cartons, minimap. Lots G0 à G5 : un gardien à part par terre (le Grand Cerf, l'Épouvantail, le Tocsin, le Pilleur, le Carrier), plus de Souverain grossi.

**23 octobre — une seule horloge ([plan 56 B2](56-boucle-de-run.md), §196) :** Résurgences, micro-événements, chasses de Souverains, Ateliers, Puits et stèles éteints en run par interrupteur de données (`--reserve` les rallume dans les outils) ; cinq quêtes qui en dépendaient passent en réserve, leurs pièces restent verrouillées. Suite : B3, la run se lit.

**23 octobre — l'arène du gardien ([plan 56 B1](56-boucle-de-run.md), §195) :** pendant le combat d'un gardien, la difficulté s'arrête, rien d'autre ne se déclenche, la foule est plafonnée à 40 et le gardien passe devant elle dans le choix des cibles ; PV revus, sans affixe. Au bot en profil neuf, 7 combats sur 10 en 73 à 112 s (6 à 30 min avant), deux runs gagnées vers 19 min. Suite : B2, une seule horloge.

**20 octobre — revue de la boucle de run ([plan 56](56-boucle-de-run.md), §194) :** au bot en profil neuf, le 2e gardien est un mur (6 à 30 min de combat, ou jamais vaincu) : la foule grossit pendant le combat et les Résurgences s'y ajoutent. Proposé : forme gardée, combat en arène fermée, une seule horloge (systèmes en réserve éteints), run lisible (lieux de Vaulme, piste de run), la Montée définie avec la fin au choix. Lots B1 à B3 avant la séance de jeu.

**19 octobre — le rythme du départ mesuré ([plan 55](55-rythme-du-depart.md), §193) :** l'ouverture remplit l'écran dès 3 s (12 créatures visibles la première minute au lieu de 3) ; le premier niveau tombe à la première élimination, niveau 10 vers 5 min ; aimant à 100 px. Au bot, ×10 vers 30 à 55 s au lieu de 2 à 4 min. Aucun cran de réglage. À jouer.

**18 octobre — la Série finie et allumée en run ([plan 52 S5](52-elan.md), §192) :** la Série et le vide sont actifs en run normale ; fenêtre de 5 s au rang 0 ; meilleure Série au bilan et record par personnage ; mesure au bot (premier rang après 3 min, la densité du départ freine). À jouer.

**18 octobre — les retours de la Série ([plan 52 S4](52-elan.md)) :** aura au sol par rang, onde, secousse et cloche plus aiguë au rang gagné, verre et chiffre fendu au coup reçu, gerbe d'Essence à l'encaissement, tic de la jauge presque vide ; contour à la couleur du rang dès FURIE, images rémanentes à MONSTRE, musique de combat dès CARNAGE.

**18 octobre — la Série au HUD ([plan 52 S3](52-elan.md)) :** gros « ×N » et nom du rang en haut au centre, jauge de la fenêtre, animations de gain, de rang, de coup reçu (÷2) et d'encaissement (+N ESSENCE) ; le compteur de tête, la fenêtre sous les pieds et « EN RAFALE » sont retirés. Capture : `tools/elan_sandbox.sh --screen 1 -- --capture <dossier> --scenario streak --enemies 0`.

**18 octobre — la Série en trois règles ([plan 52 S2](52-elan.md), §192) :** six rangs de ×10 (ÉVEIL) à ×320 (MONSTRE), fenêtre de 4 s à 2 s ; un coup coupe la Série en deux ; une jauge vide l'encaisse en Essence (Série × taux du rang) ; un coup au gardien relance la fenêtre. Toujours éteinte en run normale jusqu'à S5.

**18 octobre — la Série essayée, version finie proposée ([plan 52 §15](52-elan.md), §191) :** retours de Raphaël (compteur au HUD, plus gros, plus de retours, doute sur « touché, on perd tout ») ; proposition en trois règles (six rangs, coup qui coupe en deux, jauge vide qui encaisse en Essence), retours, lots S2 à S5. Nouvelle règle : boucler une mécanique de bout en bout avant le sujet suivant. Ensuite, revue de la boucle de run.

**18 octobre — la Série ([plan 52 S1](52-elan.md)) :** combo d'éliminations (« ×N » au-dessus de la tête), fenêtre de 2,5 s, palier perdu au coup, chute dans le vide qui vaut 3 ; paliers à 20, 60 et 150 (vitesse, dégâts, Chance). À essayer avec le vide dans `tools/elan_sandbox.sh` ; toujours éteinte en run normale.

**18 octobre — forme du jeu verrouillée, aucun point en suspens ([LE-JEU.md](../LE-JEU.md), §190) :** survivors-like, trois terres tirées puis la Montée et l'Indicible en finale ; la Série (combo d'éliminations) remplace la jauge d'Élan ; ordre de travail en effort ; le §2 du tableau de bord est trié et reste vide.

**18 octobre — percussion et vide comme arme ([plan 52 E4](52-elan.md)) :** le dash projette les créatures traversées (dès le palier 0, plus fort par palier ; `--percussion-tier 2` pour la version d'origine) ; Élan allumé, une créature ordinaire poussée dans le Néant y tombe, éliminée, et les créatures longent le bord en marchant. Bac à sable prêt pour la semaine d'essai (critère d'arrêt au [LE-JEU.md](../LE-JEU.md)).

**18 octobre — saut du vide ([plan 52 E0 et E3](52-elan.md)) :** le banc dense, invalide depuis le 17 octobre (arène hors de la terre), et le bac à sable sont reposés sur la terre ; dès le palier I d'Élan, le dash est porté au-dessus du Néant (64, 128, 192 px), +0,15 à la retombée sur le sol. Élan toujours éteint en run normale ; à essayer dans `tools/elan_sandbox.sh` (bandes de vide à l'est du départ).

**18 octobre — revue de direction ([LE-JEU.md](../LE-JEU.md), §189), à lire avant tout :** gel des nouveautés jusqu'à fin novembre et tranche verticale (le Vagabond, deux terres de Vaulme, un gardien, environ 8 armes et 20 objets). Premier chantier : le vide comme arme en bac à sable, jauge d'Élan gardée, une semaine avec critère d'arrêt. Structure de run en exploration sur papier jusqu'au 25 octobre. Les terres deviennent des lieux de Vaulme, avec des pièces faites main et une génération structurée.

**18 octobre, nuit — réglages de combat et d'interface lus strictement ([plan 26 Q7g-3, Q7g-4](26-qualite-et-dette-technique.md), §68) :** défense, pistage des créatures, budget d'effets, puis bilan de run, palettes de la carte et des raretés, visuels d'armes, musique, catalogue des stats et mobilité ; un fichier cassé ne ramène plus en silence l'armure d'avant le plan 47. Mêmes valeurs effectives. Validation complète de la branche : 40/40 suites. Puis la banque de sons (Q7h-1) : un son au fichier absent ou au bus mal écrit arrête l'accueil en le nommant. Puis les décors par biome (Q7h-2), lus une seule fois au lieu de chaque génération.

**17 octobre, nuit — tables de butin et lieux lus strictement ([plan 26 Q7g-1, Q7g-2](26-qualite-et-dette-technique.md), §68) :** mêmes valeurs ; un lieu à la forme, à la table ou au gardien inconnus, une sorte de butin inconnue ou un montant mal écrit arrêtent le chargement. **Question ajoutée au tableau de bord** : trois tirages dépassent leur borne haute d'une unité (butin des lieux, nombre de lieux, grappes de créatures).

**17 octobre, nuit — personnages, signatures, Souvenirs et montées d'armes lus strictement ([plan 26 Q7f](26-qualite-et-dette-technique.md), §68) :** `characters.json` est contrôlé (arme, signature, mobilité, sprites) ; les « perks exclusifs » hérités du craft, jamais lus, sont retirés du fichier. Mêmes valeurs effectives. Puis `perks.json` (Q7f-2) : signatures contrôlées en entier, cinq champs hérités des anciens Dons retirés. Puis Souvenirs et montées d'armes (Q7f-3) : un Souvenir en double ou sans constellation, un pas de montée absent arrêtent le chargement au lieu de disparaître en silence.

**17 octobre, nuit — apparition, foule et variantes lues strictement ([plan 26 Q7e-1, Q7e-2](26-qualite-et-dette-technique.md), §68) :** `spawn_flow.json` a un lecteur strict ; le jeu ne retombe plus sur d'anciens réglages oubliés si le fichier est cassé, et une créature de secours inconnue refuse le fichier au lieu d'être retirée en silence. Mêmes valeurs effectives. Puis `crowd.json` et `_variants.json` (Q7e-2) : un fichier de variantes cassé ne fait plus disparaître en silence élites et Souverains, il arrête le chargement en nommant le champ.

**17 octobre, nuit — Résurgences et Effacement lus strictement ([plan 26 Q7d-1, Q7d-2](26-qualite-et-dette-technique.md), §68) :** `crises.json` et `erasure.json` ont chacun un lecteur unique et strict au lieu de sept lectures indépendantes qui ignoraient un fichier invalide ; mêmes valeurs effectives. Puis `run_events.json` (Q7d-3) : contrat par sorte d'événement. Q7d terminé ; une vingtaine de lecteurs restent à durcir (Q7e à Q7g proposés).

**17 octobre, nuit — fermes et chantiers lus strictement ([plan 26 Q7c-4c](26-qualite-et-dette-technique.md), §68) :** `farms.json` et `quarry_sites.json` sont contrôlés (décors présents, positions, rangées, scènes) ; un fichier absent ou invalide arrête le chargement au lieu de désactiver le lieu en silence ; mêmes valeurs effectives.

**17 octobre, nuit — réglages du monde lus strictement ([plan 26 Q7c-4b](26-qualite-et-dette-technique.md), lot sans décision de design, §68) :** `world_gen.json` est contrôlé champ par champ (biomes, décors, zones, terres, chemins) et un fichier refusé arrête le chargement au lieu de générer en silence une carte de 30 cellules ; mêmes valeurs effectives. Corrigé dans la foulée : une panne aux catalogues ne lance plus de génération orpheline derrière l'écran d'erreur (`test_loading.sh` de nouveau vert).

**17 octobre, nuit — zones au sol rondes mais travaillées ([plan 25 S12](25-sprites-et-design.md), §164, §172) :** toutes les zones redeviennent des cercles au bord rugueux d'un texel, avec un rendu beaucoup plus fouillé : annonces à tirets qui tournent, sol fissuré et ondes qui convergent ; couloirs à chevrons ; flaques à ondes ; feu à couronne de braises ; éclats à onde de choc et débris ; auras d'affixe tournantes. Coût relatif −29 % par rapport aux anciennes zones (gardes exactes hors du bord) ; [planche](planches/25-s12-zones-avant-apres.png). À juger en combat.

**17 octobre — sprites vivants ([plan 54](54-sprites-vivants-et-animations.md), §188) :** le duo pilote (Vagabond, Rôdeur) est validé en jeu, échelle gardée. Lot S2 livré : les cinq autres jouables passent au nouveau générateur (marche asymétrique, dash en quatre temps, tissus en inertie, Forgeuse qui tient son marteau) ; retouches de la Scaphandrière refaites à neuf. Lot G3 livré ensuite : traînées du dash à la silhouette du personnage et à sa couleur, créatures qui tremblent avant d'attaquer et s'étirent à la détente, écrasement selon le sens du coup. Lot S3 livré le soir même : les onze autres créatures du bestiaire au même moteur (matières, fusions courtes, marches à contretemps, attaques avec anticipation), exportées en jeu sans nouveau fichier ; [planche](planches/plan54_lot_S3_bestiaire.png).

**16 octobre, soir — la boucle arrêtée ([plan 51 §11](51-objectifs-de-run.md), §186) :** trois terres d'environ 10 min, tirées et plus petites, qui tombent des bords vers le cœur, puis une 4e carte de late game sans fin où la difficulté monte toujours. Sur chaque terre, on ravive des Mémoriaux en tenant leur cercle 20 s (clés, ancres, puissance) ; dès trois lumières, on réveille le gardien au cœur, sur le sol qu'on a sauvé ; la terre suivante se recoud autour du joueur. Formats plus audacieux (île qui dérive, archipel, Montée en finale) mis en réserve. Suite : Élan E2–E3, puis prototype des terres.

**16 octobre — refonte de la boucle et Élan ([plan 51](51-objectifs-de-run.md), [plan 52](52-elan.md), §184–185) :** après le premier testeur, la run passera par trois terres d'environ 10 min, plus petites, qui tombent dans le Néant des bords vers le cœur, chacune avec un boss déclenché par le joueur. Rien n'est codé côté boucle ; le point de reprise est en tête du plan 51. L'Élan devient la mécanique centrale (gestes maîtrisés et vitesse qui rendent plus fort) : le lot E1 est livré (jauge, bac à sable `tools/elan_sandbox.sh`, éteint en run normale), E2 (esquive parfaite) vient ensuite.

**13 octobre — oublier au Puits ([plan 44](44-oublier-a-la-faille.md), §157–158) :** une arme, un Écusson ou une pile d'objets s'oublie à un Puits, lieu dédié (6 par carte), contre de l'Essence ; d'abord placé à la Faille, l'oubli en est sorti le soir même et les puits à boire ont disparu (100, +50 % par oubli). La prochaine arme ou le prochain Écusson repart de la moitié des niveaux. L'échange d'arme au sol et l'écran d'échange du sac disparaissent : un objet qui n'entre pas attend au sol, sans expirer tant que le sac est plein. Les Souverains ne disparaissent plus avant d'être abattus. Lot F1 livré, à juger en jeu.

**12 octobre — tirs à la chaîne ([plan 21 G6h](21-systeme-de-jeu.md), §130) :** les projectiles d'une attaque partent l'un après l'autre (0,08 s, la chaîne tenant dans 60 % de la recharge), comme dans Megabonk, au lieu de partir tous dans la même image ; les salves gardent leur éventail d'un coup ; un son par tir, plus doux après le premier. Suite d'armes verte. Questions de reprise (§131) : pas de critique de base, mais des objets de critique dès le départ et plus d'armes qui en gagnent au niveau.

**12 octobre — direction « crade » ([plan 40](40-direction-crade.md), §122) :** Raphaël cherche l'esthétique des BD de son enfance (Métal Hurlant, Beksiński, Rain World). Maquette hors jeu en trois dosages, dosage léger retenu. **G1 livré** : palette salie et grain au pixel du monde dans le shader commun, sans toucher aux assets ; joueur et créatures en palette seule. **G2 livré** : taches d'eau et de moisissure au sol et sur les décors, tramées au pixel. Suite : hachures dans les ombres (G3).

**12 octobre — objets implémentés ([plan 35 I10–I11](35-raretes-des-objets.md), §133) :** [56 objets](35-catalogue-objets-v2.md), dont 34 nouveaux ; rareté fixe, copies ordinaires illimitées, Chance sur les coffres payants en Essence, effets réels et stats exhaustives en pause, projectiles et satellites unifiés. 27 objets accessibles au profil neuf, 14 anciens gardés par leurs quêtes ; [quinze nouvelles quêtes intégrées](35-quetes-objets-v2.md), activation vérifiée (§138), Pied-de-biche à 50 coffres cumulés. Aucun nouveau sprite/animation. Raccordements vérifiés par lots ; économie et builds restent à éprouver humainement en I12.

**11 octobre — atmosphère et détail du monde ([plan 39](39-atmosphere.md), §119) :** constat (lumière plate, seule la forêt a une ambiance sonore, aucune faune ni vent commun, lore et POI coupés) et recommandations en trois phases. Questions tranchées (§119 bis), lumière tirée par run retirée. **A1 livré** : étalonnage des couleurs par biome en uniformes globaux (sol, routes, décors, canopées, moitié pour les entités), fondu de 1,5 s, réglages en JSON ; captures avant/après des cinq biomes regardées, 213 contrôles de catalogues. Le soir même, étalonnage dosé à 65 % et fondu de 2,5 s (retour de Raphaël, §119 ter). **A2 livré** : ombres de nuages tramées qui défilent sur le sol, les décors et à moitié sur les entités, couverture et noirceur par biome, aucune au marais. **12 octobre, A3 livré** : ombres portées des arbres et grands décors vers l'arrière-droite, effacées sous les nuages et dans l'oubli ; +0,07 ms par image machine calme, à recouper. **A4a livré** : ambiance par biome en données (fond, sons ponctuels, réverbération du monde), forêt inchangée, réverbération proposée en carrière et en ville ; A4b (planche d'écoute) ensuite. **A5 livré** : météo à part des micro-événements (brouillard épais qui referme la vue, pluie de cendres, vent de l'oubli, orage avec éclairs), tirée par biome, suspendue pendant un boss. **A4c** : ambiances choisies par Raphaël intégrées (marais, carrière, champs, Hub, sons ponctuels de ville), fondu enchaîné entre fonds ; voile du marais réaffiché (jamais visible jusque-là). Carrière à deux fonds tirés l'un ou l'autre, vent fort pour la ville (§125). **B1 livré** : vent commun (herbes, buissons et canopées ploient en vagues, rafales), particules par biome ; ombres portées pixelisées avec un réglage de qualité (§126). **B2 livré** : faune qui fuit (corbeaux, pigeons, rats, papillons, grenouilles), née hors de la vue, absente des zones qui s'effacent, qui s'envole à l'approche, aux morts proches et devant l'Effacement. **B3 livré** : l'exode dans les rues (§137), une trentaine de scènes rares par carte, embouteillages portières ouvertes et barrages « ÉVACUATION », tous tournés vers l'extérieur de la carte ; autant de décors en ville qu'avant (−1,8 %). **C1 livré** : radios et téléphones abandonnés (§143), en son seul : 5 à 6 radios par carte sur une voiture d'embouteillage diffusent un message d'évacuation synthétisé qui perd ses mots quand la zone s'oublie ; une cabine sur deux sonne et se tait quand on arrive. À écouter. **B4a livré** : fausses lumières (§145), une lampe sur quatre des décors qui en portent une brille encore (feux tricolores clignotants, lampes de carrière, lanternes du marais), discrète de jour et plus forte dans le sombre, qui vacille puis meurt avec l'oubli. **B4b livré** : une fenêtre allumée pour 15 % des immeubles, lumière chaude ou télévision restée allumée, repérée dans les sprites. **B4c livré** : feux de camp éteints qui fument, cinquième scène-récit des champs à densité égale, 4 par carte en moyenne. B4 complet.

**11 octobre — retours de Raphaël sur trois jours de livraisons ([plan 37](37-retours-du-11-octobre.md), §117) :** Hub (pages séparées, sélection, Quêtes en liste unique, Collection sans bouton, Campement expliqué, Réminiscences par quêtes, curseur), interface de run (jeu flouté derrière les menus, HUD sans encadrés, objets à gauche, minimap boussole, chargement fluide), monde (cercles en pixel art, transitions de biomes, bords de routes en iso, ennemis). Lot H1 engagé.

**11 octobre — l'inventaire, lots I1 à I7 (plan 35 §11–§17) et les Poches (plan 36 M4) :** 4 Écussons qui montent au level-up et par l'usage (un fil sous chaque Écusson ; un quart des niveaux par l'usage, mesuré au bot), sac d'objets en exemplaires (10 places, 25 avec les Poches), objets lâchés par les coffres, élites, Souverains et vendus aux Ateliers, écran d'échange quand le sac est plein, surplus d'arme au niveau 50, cinq nouveaux objets (Ticket de consigne, Plan de métro, Flacon de pharmacie, Trèfle plastifié, Montre arrêtée). Points à juger dans `A-VERIFIER.md`.

**11 octobre — l'inventaire, lot I0 (plan 35 §9) :** les 17 Écussons nommés (du batteur, du coureur, du pompier…) avec leur déclencheur de montée par l'usage, maquettes de la barre du bas (4 armes, 4 Écussons, sac compact) et de l'écran d'échange, migration des données et des 18 quêtes d'objets. Rien n'est codé : huit questions au plan 35 §9.5 avant I1.

**11 octobre — le Campement (plan 36, M0–M3) :** progression permanente au Hub, payée en Vestiges. 13 améliorations en trois rayons (emplacements d'armes de 2 à 4, relances, bannissements, neuf stats, Essence de départ), remboursement libre, cadenas sur les cases d'armes fermées. Prix du §110 gardés : 41 runs moyennes pour tout acheter. Profil neuf à deux armes : 78 % des éliminations d'un départ à quatre armes à 10 min. Questions au plan 36 §13 ; points à vérifier dans `A-VERIFIER.md`.

**11 octobre — refonte des menus achevée ([plan 04 §9](04-interfaces-et-hub.md#9-achèvement-de-tous-les-menus--11-octobre-2026), P4–P7) :** montée de niveau, butin, journal, bilan et rencontres dans la direction Pierre et Effacement, sur `codex/refonte-menus`. Build zéro warning, 15/15 suites puis contrôle final 5/5 ; captures réelles 720p/1080p, texte agrandi, profils neuf/dev, mort et relance avec un autre héros. [Rapport et captures](../audits/menus-2026-10-11/README.md). Aucune fusion vers main.

**10 octobre — refonte des menus ([plan 04 §8](04-interfaces-et-hub.md#8-production-autorisée--10-octobre-2026), P1–P3) :** livrée et vérifiée sur `codex/refonte-menus`, sans fusion vers main. Accueil conservé, personnage choisi après Partir/Recommencer, pierre et Effacement, Quêtes/Collection reliées, pause compacte et paramètres communs. Build sans warning, suites 7/7 puis 2/2, captures 720p/1080p et nouvelle run depuis le bilan. [Personnages](planches/04-menus-personnages-integre.png) · [Pause](planches/04-menus-pause-integree.png). Recette artistique en jeu ouverte.

**10 octobre — quêtes de déblocage (plan 06 §10, lots Q1–Q4) :** une seule famille de 36 quêtes qui débloquent personnages, armes et objets pour de bon ; profil neuf réduit à la réserve de départ (Vagabond, 8 armes, 15 objets). Suivi en run sans affichage, bandeau dès qu'une quête est accomplie, pages Quêtes et Personnages au Hub, seuils mesurés sur 7 runs (9 ajustés). À trancher : quêtes de critique et Recopier.

**10 octobre — les deux boss en autonomie (plan 07, B1–B4, §89) :** parties de boss vues par toutes les armes (la mêlée frappe au bord), barre de boss en haut de l'écran (Souverains compris). La Barrière se lève à 10 min devant le joueur, l'arrête jusqu'à ce qu'il brise un battant, frappe l'immobile (poings) et le va-et-vient (chaîne, verrou), un coffre par battant. L'Indicible est refait en trois phases : tempête, marée, seconde vague à brèches, puis il se découvre. Premier réglage en vraie run longue. Planche B2a intégrée sans validation ; reste à jouer.

**10 octobre — reprise audio (plan 15, M1–M3) :** musique de combat réservée aux pics (Souverain, boss, foule ≥ 85 créatures), 22 à 33 % du temps d'exploration au lieu de 83 % ; annonce et Résurgence entrent aux points V2. Pistes séparées synchrones d'une run enregistrée (`--audio-stems`, `tools/audio_stems.py`) pour isoler le frottement ; planche de 57 candidats pour 19 sons manquants, dont les impacts par matière. Écoutes hors dépôt, choix de Raphaël attendus.

**10 octobre — sprites des héros à venir (plan 32) :** les sept héros du lore qui n'avaient pas de sprites jouables les ont, plus Élie enfant, au gabarit du Vagabond, du Traqueur et de la Forgeuse ([casting complet](planches/32-casting.png)). Éveillée en standardiste (cornet, écouteur, cordons du standard et cheveux qui flottent), Facteur à moustache et sacoche débordante sur ses patins, Scaphandrière au casque de cuivre à hublot et vert-de-gris ; Sonneur à la corde coupée et cloche de bronze sur le dos, Écolière (la fillette de l'écho) au cartable rouge et à la craie rose, Photographe au réflecteur de flash ; Veilleuse voûtée sous son châle vert, lampe allumée, et l'Enfant du Bas-Port en ciré jaune, le bonnet de sa sœur à la main. Planche orpheline des champs supprimée. Rien n'est branché : planches à valider.

**10 octobre — carrière (plan 31 Q1) :** roches de la mine striées de strates, fissurées, poussiéreuses ; cristaux d'Essence en prismes facettés dont seules les pointes luisent, avec un reflet bleu-vert sur la roche ; panneau « danger » sur deux piquets, vestiaire au bleu de travail pendu, pelleteuse à vérins et peinture écaillée, gyrophare rayé, caisse d'explosifs au pochoir, baraques et wagonnets rouillés en coulures, pieds du convoyeur rattachés. Les cinq biomes du plan 31 sont repris.

**10 octobre — champs et ferme (plan 31 U4, C1) :** trois murs urbains orphelins supprimés (accord de Raphaël). Maisons de ferme régénérées avec les façades urbaines ; granges en planches jointes à la peinture écaillée, l'endommagée vraiment abîmée (toit crevé sur ses chevrons, planches arrachées, battant tombé) ; haies en grappes avec aubépine, verger en fleurs, arbre isolé sans « œil » ; herbes hautes en brins, coquelicots et bleuets, murets de moellons, balles de foin, épouvantail, puits, menhir, charrue à trois socs, tracteurs, éolienne et silos rouillés en coulures ; plus de boules vertes au pied des objets. Question : supprimer la planche orpheline des champs. Suite : carrière (Q1).

**10 octobre — Barrière : reprise, découpage et planche (plan 07, B1–B2a) :** questions de reprise tranchées (DECISIONS §85) : ailes de chaînes infranchissables, grille orientée selon le cap, un coffre par battant, barre de boss en haut de l'écran (Souverains compris), créatures ×0,5 pendant le combat. Découpage proposé au plan 07 : B1a partie de boss (une `Enemy` au comportement `boss_part`, vue par les trente chemins des armes et objets sans les toucher ; réserve de PV commune), B1b barre de boss, B1c mannequin et banc des 24 armes ; B2a planche, B2b grille en jeu à 600 s, B2c attaques. Planche des sprites proposée ([07-b2a-barriere.png](planches/07-b2a-barriere.png), modèle `tools/sprites/props/barrier.py`, rien dans `assets/`) : grille de fer forgé, piliers de pierre, cadenas à l'œil vert-acide, battant intact, entamé, brisé, ailes, poing en deux variantes. **À valider par Raphaël :** gantelet ou main, verticale en diagonale (~35°), hauteur. Suite : B1a, qui ne dépend pas de la planche.

**9 octobre, nuit — décors de la forêt (plan 31 F1) :** les 18 modèles repris. Arbres à fourche, contreforts et écorce rainurée, feuillage en grappes qui prennent chacune leur lumière (fini les chou-fleurs), bouleau barré de noir ; arbre étranglé par un vrai lierre ; buissons, fougère en frondes, fleurs à corolles, cèpes et girolles, souche sciée, rochers moussus, tronc couché ; mur de briques, candélabre de fonte, panneau de danger et voiture envahis de lierre et de mousse peinte. Kit végétal commun (`_flora.py`) ; l'arbre isolé des champs et le verger en profitent. Suite : champs et ferme (C1), carrière (Q1).

**9 octobre, nuit — Souverains à heure fixe (plan 30, lot T2) :** deux chasses au Souverain à 2:30 et 7:00, hors tirage, qui attendent la fin d'une Résurgence ou d'une accalmie sans jamais la chevaucher. Souverain à PV ×4 et sans Tenace : combat de 5 à 58 s avec le build de référence (avant : 4 à 7 s, et un Souverain sur quatre runs avant 10 min). Le bot de mesure contourne désormais un obstacle quand il suit une cible. Les chasses ratées viennent du bot encerclé, qui n'esquive pas. Suite : B1–B2, la Barrière à 10 min.

**9 octobre, soir — décors urbains (plan 31 U1–U3) :** immeubles repris (enduit taché, coulures, briques à nu, fissures, soubassement ; vitres et reflets, volets, balcons ; toits gravillonnés avec édicule, cheminées, mousse et flaques ; lierre en tiges), ruines éventrées (coupe rongée, papiers peints, plancher qui pend, gravats en blocs), gravats en trois variantes, bureau renversé procédural, voitures et poutres rouillées. Deux défauts du pipeline corrigés en option : lignes internes qui noircissaient les toits plats, détails plus fins qu'un pixel qui faussaient l'éclairage. Question : supprimer trois murs orphelins (U4).

**9 octobre, soir — Résurgence qui déborde (plan 30, lot T1) :** pendant une Résurgence, le plafond de run est multiplié par 1,4 (`crisis_cap_multiplier`), sans retrait à la fin (§84). La 2e crise passe de +2 % à +45–64 % de foule proche selon le bot ; dégâts reçus pendant la crise ×1,6 à ×1,9 pour les bots qui avancent ; 300 créatures au plus, 133–139 FPS au pic en 1080p. Elle se mesure mais se voit encore peu (arrivée dispersée, seuls les bords ternis). Suite : T2, Souverains à heure fixe.

**9 octobre — décors du marais (plan 31, §86) :** audit des 186 sprites de décors validé par Raphaël, marais puis urbain. Les 25 modèles du marais sont repris un par un : bois blanchi rainuré au pied mouillé, barbes de lichen effilochées, objets noyés cernés d'un clapot argenté, petits décors recomposés pour leurs 20 px (massettes, champignons, nénuphars, fûts). Kit commun de détails de surface et générateur parallèle ; galerie en jeu par décor (`--capture-prop-gallery`). Suite : immeubles urbains (U1).

**9 octobre — temps forts des 15 premières minutes (plan 30, étude) :** chronologie mesurée sur 4 graines et trois bots (sonde `--timeline`, lecture seule). En 10 min, 6 à 7 moments (2 Résurgences, 4–5 micro-événements), autant que Megabonk, mais peu marqués : la population colle au plafond de run dès 5 min, si bien que la 2e Résurgence n'ajoute presque rien (+2 % de foule, +64 % avec le plafond relevé en mesure) et que l'après-crise est plus dangereux que la crise. Les micro-événements sont les seuls pics (×2–3 de foule rapprochée) ; le Souverain meurt en 4–7 s ; la Barrière arriverait vers 15 min. Cinq pistes chiffrées et comparaison sourcée avec Vampire Survivors et Megabonk : [30-temps-forts.md](30-temps-forts.md). Tranché (§84) : Résurgence qui déborde le plafond sans reflux, deux Souverains à heure fixe, Barrière à 10 min en priorité ; nuées et vagues plus tard. Suite : lot T1.

**9 octobre — à-coups en run (plan 29) :** trois causes hors foule. Ressources chargées à leur première apparition (habitants, flèche, effet de mort) : chargées pendant l'écran de chargement. Shaders des particules de Résurgence compilés au premier affichage (60 ms) : matériaux partagés préchauffés. Pauses du GC .NET de 23 à 65 ms : collecte de la jeune génération tous les 4 Mo (`GcPacer`), pire pause 15 ms. Pire image hors démarrage en vraie run 60–68 → 16–22 ms. Brief de l'étude des temps forts : [30-temps-forts-brief.md](30-temps-forts-brief.md).

**9 octobre — performance des foules (plan 29, §83) :** le banc qui avait fixé le plafond à 200 créatures posait la foule dans un immeuble depuis la carte agrandie du 30 septembre. Diagnostic : une créature coûte surtout par ce qui l'entoure (rappel moteur → C# de 9 µs par tick, corps physique, pas de séparation), pas par sa logique. F1 : une seule boucle C# avance toutes les créatures, mode `Floating`, banc corrigé ; 320 créatures 60 → 77 FPS, 400 → 37 → 57, 32/32 suites. À décider : nouveau plafond, séparation (F2).

**7 octobre, soir — Péril et difficulté (plan 28, §78) :** mesure de référence : avec le build de Raphaël, une créature meurt six fois plus vite après 10 min qu'au début. Le Péril n'a plus de plafond et ne paie plus que par plus d'ennemis (bonus d'XP et de rareté retirés, score gardé) ; il s'affiche au HUD. Dix stèles par carte le montent d'un point ; le Sifflet d'arbitre, nouvel objet, d'un point par niveau. Contrôles des catalogues, objets et armes verts, captures regardées. Suite : mesures à Péril 10 et 20, plafond de créatures au banc, puis montée naturelle et Résurgences (P4).

**7 octobre — tout se voit, V1a (plan 27) :** les statuts se lisent sur le sprite des créatures : figé (pose tenue, givre), ralenti (teinte froide, pas ralentis), brûlure (bords bas chauds), Fragile (fêlures). Réglages en données, 13 contrôles, banc A/B sans coût après avoir ramené les boucles du shader à 4 pixels (−5 % en 1080p avant). **V1b :** étoiles qui tournent (désorienté), larmes (saignement), braises (brûlure) autour du sprite, à sa hauteur réelle, jamais coupées par les réglages d'effets. **V1c :** vérifié sur forêt, ville et carrière, en foule de soixante et au banc avec Cloche, Lampe, Berceuse et Polaroïd ; les stries du ralenti, un appel de dessin par créature, sont retirées. **V2a :** brûlure et saignement ont leur chiffre, regroupé toutes les 0,5 s, orange ou rouge, jamais fondu avec ceux des coups ; reliquat affiché à la mort. **V2b :** les Craies dessinent leur forme (étoile, cercle, maison, soleil) à la taille réelle de la zone, l'écho des Gants a son vrai rayon, coups secondaires et objets ont leur gerbe colorée ; banc sans coût. **V2c :** champ du Chronomètre tramé à sa taille réelle, premier maillon du Trousseau depuis le joueur, petit éclat d'un projectile en bout de course. **V2d :** icônes des paliers d'objets quand ils agissent, trait de la brûlure transmise, poussière du recul (plafonnée pour laisser le budget aux impacts). V2 terminé. **V3a :** le joueur touché s'éclaire en rouge, un liseré rouge borde l'écran du côté du coup, « −12 » s'affiche au-dessus de lui, la caméra secoue enfin de façon lisible. **V3b :** la toile de la Tisseuse ne colle plus à un coup annulé (bouclier, invulnérabilité, dash) et se voit (fils sur le personnage, icône près de la jauge) ; le joueur que l'Effacement ralentit pâlit. **V3c :** le bouclier qui casse vole en éclats, revient plein avec un éclat ; l'armure qui pare se voit ; les soins éclairent la jauge ; plus d'icône de vol de vie à vie pleine. **V3d :** le Néant ne rejoue plus le paquet d'une blessure deux fois par seconde : liseré pâle qui pulse, pas de secousse ni de son de coup, dash libre. V3 terminé, à essayer en jeu. **V4 :** un Instable blessé montre au sol le rayon de sa détonation ; l'écran de mort nomme l'explosion ou l'événement qui a tué ; les couloirs de tir atteignent le joueur. Plan 27 terminé côté code, à jouer ; suite : difficulté et Péril (§74).

**6 octobre, nuit — tout se voit, V0 (plan 27) :** le shader des créatures et du joueur applique de nouveau Modulate (lu dans `vertex()` : en fragment, `COLOR` contient déjà la texture). Reviennent le flash des coups, les teintes d'annonce, l'estompage du Rampant, l'éclair et le clignotement d'invulnérabilité du joueur. Même défaut réparé sur les projectiles du joueur : liseré et halo du §53 enfin visibles (§73). Deux modes de capture (`--capture-statuses`, `--capture-player-hit`), planches avant/après regardées, banc A/B sans coût mesurable. Suite : V1, les statuts sur les créatures.

**6 octobre, soir — synergies de règles (planche 05, lot S1, §70) :** les trois planches en attente sont validées. S1a : écho, forme, saignement et feu comptent comme l'arme pour les objets ; ce qu'un objet produit ne déclenche jamais d'objet. S1b : le feu de la Lampe brûle (mêmes dégâts à un PV près), Fragile vaut pour les tics, l'Épingle lit les entravés, une créature figée n'attaque plus (mini-boss exceptés), cumul des Brûlures sans emprunt d'intensité ni de durée. S1c : offre de la Propagation, cap des désorientés, compteur du Scalpel. 20 contrôles ajoutés, validations vertes à chaque sous-lot, trois relectures. Suite : pouvoirs des personnages (plan 06), puis Barrière et Indicible (B1–B4).

**6 octobre — biomes (plan 26 Q7c-4a) :** les cinq fiches sont contrôlées ensemble avant publication, leurs définitions sont en lecture seule ; un refus affiche le diagnostic et empêche la génération. 590 valeurs avant/après identiques, 190 contrôles de catalogues, validations 9/9 puis 5/5 et fiche réellement invalide dans une copie isolée vérifiés. [Preuves](../audits/catalogues-biomes-2026-10-06/README.md). Suite : paramètres du monde, fermes et carrières (Q7c-4b/c), puis événements et Résurgences (Q7d). Planches de lore, synergies et boss toujours en attente de relecture.

**5 octobre — corrections de run et catalogues (plan 26), sans décision de design (§68) :** Q8a tirs guidés verrouillés sur une seule vie de créature ; Q8b remapping sans perte de WASD au redémarrage ; Q8c seed effective publiée et tirages de gameplay dans des flux dérivés de la seed (même seed, mêmes apparitions) ; Q8d un seul propriétaire de la phase de run (late game gardé après la quatrième Résurgence) ; Q7a courbe d'XP, score et Péril contrôlés ; Q7b objets, raretés et offre ; Q7c-1 à Q7c-3 lieux, bénédictions, Oublis, coffres, bonus lâchés, petits lieux et Repères (135 fixtures, valeurs identiques avant/après). Chaque lot : contre-épreuve, valeurs identiques avant/après quand elles devaient l'être, relecture, suites vertes. Galerie de captures d'armes corrigée (plus d'image prise pendant le fondu).

**4 octobre, nuit — chargement récupérable (plan 26 Q4) :** une panne pendant le chargement affiche l'erreur et un retour au camp au lieu de laisser le jeu en pause ; une scène quittée en route ne laisse ni pause ni décors orphelins. 7 scénarios, contre-épreuves par mutation, 7/7 suites. Planches proposées : synergies de règles ([05-planche-synergies.md](05-planche-synergies.md)), Barrière et Indicible ([plan 07](07-bestiaire-et-rencontres.md)), lore L1 ([19-planche-L1.md](19-planche-L1.md)).

**4 octobre, nuit — nettoyage (plan 26 N1) :** meute sans bonus de dégâts, arme en main abandonnée, réglage « Taille du texte » retiré, `score_points` des lieux et coffres retiré (score aux seules éliminations) ; mêmes valeurs, 7/7 suites. Marche à suivre Steam écrite ([STEAM-MISE-EN-PLACE.md](../STEAM-MISE-EN-PLACE.md)). Réponses de reprise : [DECISIONS §67](DECISIONS.md).

**4 octobre — sauvegardes protégées (plan 26 Q2a) :** méta, historique, record et analytics écrits par temporaire vérifié avec copie de secours ; fichier illisible mis de côté et repris sur la copie ; version future jamais réécrite. 39 contrôles, 6/6 suites. **Q2b :** fin de run réglée une seule fois, méta en une écriture, historique réparé au camp, échec affiché au bilan et au camp ; 58 contrôles, 6/6 suites. **Q6a :** contrat des armes en données, effets typés, arme fautive refusée avec un message précis ; valeurs effectives identiques, 13/13 suites. **Q6b :** réglages de l'Indicible en données, même combat ; constat : le boss ne se combat pas vraiment (question au tableau de bord). **Q3 :** classements Steam par une file unique (une opération à la fois, délais, relances, réponses tardives ignorées), rappels pompés pendant la pause, Weekly par semaine ISO ; 56 contrôles avec un faux service, 8/8 suites ; Steam réel non vérifié. **Q6c :** créatures typées et contrôlées par un contrat (stats, capacités, réglages, sons, renforts), meute par famille, groupes d'apparition contrôlés au chargement ; mêmes valeurs, 163 contrôles, 10/10 suites ; question : `pack_bonus_damage` jamais appliqué. **Indicible mesuré** ([plan 07](07-bestiaire-et-rencontres.md#lindicible-mesuré--4-octobre-2026)) : il ne peut pas être blessé (bords hors de portée, ciblage au centre) ; reprise à concevoir avec la Barrière. Questions de reprise consignées ([DECISIONS §62](DECISIONS.md)) : lore v1.1 validé comme base, personnages, G6g, Ateliers, Mémorial, mort et bilan validés. [Plan 26](26-qualite-et-dette-technique.md).

**2 octobre — qualité du code et dette technique :** le [plan 26](26-qualite-et-dette-technique.md) couvre les 18 constats et les noms/règles en dur. **Q0 livré et vérifié** : lanceurs stricts, bancs réparés, validation globale 21/21 ; [preuves](../audits/qualite-2026-10-02/q0/README.md) et [commande commune](../VALIDATION.md). Prochain lot recommandé : Q1 ; nettoyage du combat aux Q5–Q7. [DECISIONS §54–55](DECISIONS.md).

**1er octobre — Effacement, cellules actives :** les zones au Néant gardent leur état sans être recalculées ; stabilisations et ordre des signaux préservés. Rejeu de 30 min identique, recalculs −23,8 %, 72 contrôles, captures et run Main de 30 min vérifiés. [Lot 10 6C et preuves](../audits/erasure-active-2026-10-01/README.md).

**1er octobre (soir) — réveil des lieux dans le monde :** au dernier éclat, le monde se fige, se pixelise, les éclats tournent autour de la stèle et s'y fondent, puis l'oubli recule jusqu'aux bords avant l'écran ; même traitement, plus court, pour la Faille. Passable d'un appui ; 7 contrôles et captures ViewSonic vérifiés. [Plan 24 L6b](24-retours-du-1er-octobre.md#l6b--mémorial-et-faille-mis-en-scène-dans-le-monde-livré-le-1er-octobre-soir).

**1er octobre — recette fiabilisée :** l’intégration Main atteint ses 26 assertions, au lieu d’expirer à 1 500 frames ; captures rejetées sur erreur moteur inattendue. [Vérifications](../audits/verification-tools-2026-10-01/README.md).

**1er octobre — préchauffage rendu :** le chargement soumet réellement 16 shaders et les particules d’XP au GPU, puis libère son viewport ; 17 échantillons vérifiés et captures Main, [plan 10 5B1](10-terrain-et-tiles.md#5b1--soumettre-réellement-les-shaders-au-rendu--découpage-du-1er-octobre). Coût à froid/chaud encore à mesurer sur machine calme.

**1er octobre — robustesse des choix :** relances et entrées interrompues corrigées, 24 contrôles (12 échecs → zéro), captures Main vérifiées ; [plan 04 R7](04-interfaces-et-hub.md#r7--entrées-des-écrans-de-choix--lot-engagé-le-1er-octobre).

**1er octobre (soir) — audio A0/A1 livrés :** enregistrement audible de référence (Movie Maker de Godot, mixage du moteur, trace et rapport), puis pilotage de la musique stabilisé : intention séparée de la phase, annonce tenue jusqu'au début, accalmie, endgame, première run enfin musicale, fondus à puissance constante ; 22 contrôles verts. Avant/après et planche A2 (points d'entrée des morceaux d'annonce et de crise) hors dépôt, **à écouter par Raphaël** : quatre questions au plan 15. Le rôle exploration/combat reste à choisir (foule permanente). [Plan 15](15-audio.md#a0-et-a1--livrés-le-1er-octobre-soir).

**1er octobre — retours visuels et combat :** R1/R2 (pause, survols et icônes de butin), R3b (cadence du Hurleur), R5a–c (trois personnages, 456 PNG) et R6 (carte par biome) livrés et vérifiés. Rotation des coffres préservée. Restent le ressenti humain, l’approbation artistique et le banc FPS de la carte sur machine calme. [État courant au plan 24 §12](24-retours-du-1er-octobre.md#12-retours-de-recette--1er-octobre-2026). Cette continuation exclut l’audio (DECISIONS §47) ; son suivi indépendant reste au plan 15.

**1er octobre — correctif du fond des coffres :** rotation continue des rayons à 8°/s, calculée à chaque rendu, avec scintillement des poussières ; ouverture réelle capturée et inspectée (plan 25 I7, remplace le fond fixe I6 refusé par Raphaël).

**1er octobre — [plan 25](25-sprites-et-design.md) intégré sur `main` :** à la demande de Raphaël ([DECISIONS §41](DECISIONS.md)), les huit lots sont raccordés aux écrans et systèmes du plan 24. Raretés et Chance, 34 objets, 14 motifs de Réminiscences, projectiles, cinq bonus, XP, sceaux, minimap, menus et chargement : captures inspectées, build sans avertissement, smoke vert. Les trois objets du monde et les Réminiscences sans effet actif restent « À venir » dans la Collection ; leurs images sont prêtes sans activer de nouvelles règles.

**1er octobre :** retours de la partie du matin, diagnostic et lots au [plan 24](24-retours-du-1er-octobre.md) ([DECISIONS §39](DECISIONS.md)).

**Où en est chaque chose : [tableau de bord](TABLEAU-DE-BORD.md)** (une page, tenue à jour à chaque lot et à chaque décision). Priorité du 30 septembre : le [système de jeu, référence unique](21-systeme-de-jeu.md) (4 armes, 6 objets à niveaux, 4 Réminiscences après les Résurgences) et la [carte à explorer](22-carte-a-explorer.md). Les paragraphes ci-dessous gardent l'historique ; là où ils parlent d'objets sans plafond, de traits ou de quatre passifs conservés, ils sont périmés ([DECISIONS §32 à §34](DECISIONS.md)).

**Priorité actuelle — 28 septembre : objets et perks.** Raphaël demande leur refonte conjointe : objets cumulables sans plafond, quatre perks équipés au maximum. Le [catalogue de neuf spécialisations](05-perks-specialisations.md) est validé pour une V1 extensible ; B0 à B2 et la partie sans objets de B3 sont livrés et vérifiés : sept perks sur neuf sont proposés en run. Délestage et Habitude attendent les objets (B4), dont le catalogue, la récompense à choix et l'inventaire restent à décider par Raphaël. Le [catalogue commun historique](05-catalogue-objets-perks.md) garde l’audit et la proposition d’objets à réviser. Ce chantier coordonne les plans 05/13/17/20, sans ajouter un nouveau plan numéroté. Les autres priorités ci-dessous constituent l'historique et les dépendances des chantiers concernés.

**Catalogue B V1 validé :** quatre perks qualitatifs sans niveaux parmi neuf, avec ajouts futurs possibles. Le [catalogue V1 de neuf perks](05-perks-specialisations.md) précise leurs règles, acquisition et interactions, et compare les plafonds d'armes 70/99 à la courbe d'XP. Le contenu V1 est validé ; coefficients à mesurer, nouveau plafond non arbitré. L'étude A/B/C est conservée au §12 du catalogue historique.

Date : 25 septembre 2026 · Version : 0.7 · Statut : **socle de déplacement validé par Raphaël ; dash commun validé ; priorité au casting et à la refonte des sprites avant les mobilités spécifiques**.

Ce dossier transforme les retours de Raphaël en lots réalisables. Il couvre le contrôle, les sensations, la boucle, les interfaces, les builds, la progression, les ennemis, l'identité et la compétition. Les décisions acquises et les propositions restantes sont consignées dans le [registre de décisions](DECISIONS.md). Le 22 septembre, Raphaël valide explicitement les déplacements de base refaits et demande de poursuivre le plan 01. Son [compte rendu](01-deplacements.md#7-première-implémentation--21-septembre-2026) distingue mesures automatisées, validation du socle et recette complète restante. Le [compte rendu du lot D](01-deplacements.md#8-prototype-de-mobilité--22-septembre-2026) décrit le dash livré, ses vérifications et les variantes à comparer ; Raphaël valide ensuite le dash livré. Il demande de redessiner les personnages, de valider un catalogue d’au moins cinq à six identités avec de nouveaux sprites, puis seulement de reprendre le lot E. Le [mode dev](../DEV-MODE.md) permet de tester le contenu disponible sans progression préalable.

## 1. Direction commune

**Promesse : avancer dans un monde qui s'oublie, construire une puissance singulière et revenir avec une raison précise de repartir.**

Trois échelles de plaisir :
- À la seconde : mouvement précis, attaque lisible, impact, élimination et collecte satisfaisants.
- À la minute : choix de trajet et de build, risque assumé, récompense, tension puis respiration.
- D'une run à l'autre : maîtrise, nouveaux styles de jeu, fragments de lore, records.

La [Stratégie V2](../VESTIGES-STRATEGIE-V2.md) reste l'autorité gameplay/roadmap. La [Charte](../CHARTE-GRAPHIQUE.md) reste l'autorité visuelle. Les décisions explicites de Raphaël consignées dans le registre précisent ces références ; un amendement de la V2 les signale. Les nouvelles variantes proposées restent à valider. Aucun craft, cycle jour/nuit, matériau ou Foyer in-run ne revient.

## 2. Documents et décisions

| Plan | Priorité | Décision principale à valider | Dépendances |
|---|---|---|---|
| [00 — État des lieux et sources](00-etat-des-lieux.md) | Référence | Constats et limites de l'audit | Aucune |
| [01 — Déplacements](01-deplacements.md) | P0 | Socle B/C et dash D validés ; E après nouveau casting et sprites (06/08) | 03 pour calibrer la mobilité active |
| [02 — Juiciness, score et récompenses](02-juiciness-score.md) | P0 | Direction validée ; score en run, record au bilan seulement ; refonte de la mort : M1 à M4 livrés, recette en jeu attendue | 01 pour le ressenti ; cadrage 08 |
| [03 — Boucle et rythme](03-boucle-et-rythme.md) | P0 | Direction validée ; début plus menaçant et XP moins rapide | Première passe 01/02 |
| [04 — Interfaces et Hub](04-interfaces-et-hub.md) | P1 | Navigation, sélection, typographie et affichage adaptatif | Cadrage 08 ; contrats 05/06 |
| [05 — Armes, objets et builds](05-armes-objets-builds.md) | **P0, priorité actuelle** | [Neuf perks V1](05-perks-specialisations.md) validés et extensibles ; B0 à B3 sans objets livrés (sept perks actifs) ; [catalogue d'objets V1](05-objets-catalogue-v1.md) proposé, à valider | 13/17, XP 20 |
| [06 — Personnages, quêtes et défis](06-personnages-quetes-defis.md) | P0 casting ; P2 progression | Casting initial, récompenses, progression et défis | 05 ; présentation 04 |
| [07 — Bestiaire et rencontres](07-bestiaire-et-rencontres.md) | P1 début de run ; P2 reste | Menaces à distance originales, rôles, compositions | 01/03 ; cadrage 08 |
| [08 — Direction artistique, sprites et lore](08-direction-artistique.md) | Transversal | Référence visuelle, personnages mémorables, pipeline | Cadrage immédiat, production après validation gameplay |
| [09 — Classement hebdomadaire](09-classement-hebdomadaire.md) | Plus tard | Périodes, règles comparables et intégrité des scores | Score 02 ; équilibre 03/05/06 |
| [10 — Terrain et tiles](10-terrain-et-tiles.md) | P1 transversal | Cohérence, transitions, circulation et coût du terrain | Cadrage 08 ; déplacements 01 |
| [11 — Mécaniques originales](11-mecaniques-originales.md) | Prototypes | Sélection et critères d'abandon des innovations | 01/03/05/10 selon proposition |
| [12 — Micro-événements et variantes](12-micro-evenements.md) | P0 | Cadence, cinq événements, élites et Souverains (v1 livrée) | 03/07 |
| [13 — Butin](13-butin.md) | P0 après recette 12 | Trois formes de butin (Vestige figé, Triptyque, Pacte d'oubli), raretés, sources lisibles | Objets 05, idée B 11 | **Absorbé par le plan 22.**
| [14 — Anomalies du monde](14-anomalies-du-monde.md) | P1 | Anomalies rares liées au joueur et à l'oubli, jamais mortelles | 12, 13 |
| [15 — Audio](15-audio.md) | Prochain gros chantier (1er octobre) | 50 choix branchés ; nouvelle passe musique, ambiances, effets et mixage en run, avec identité des Résurgences | 24 §12, DECISIONS §44 ; inventaire restant à réauditer |
| [16 — L'oubli rendu sensible](16-oubli-sensible.md) | P1 | Lots O1–O6 : sol qui oublie, choses qui se défont, frontière visible, coût et récompense de l'oubli | 10 (shader du sol), 02, 13, 15 |
| [17 — Armes, coffres et modificateurs](17-armes-coffres-modificateurs.md) | P0 | Vagues 0–5 ; rareté sur les améliorations ; noms d'armes ; Mémoriaux et Failles | 05, 08, 13, 16, 18 |
| [18 — Inventaire des restes V1](18-inventaire-restes-v1.md) | P0 | Éléments de lore et POI vectoriels, Colosses, identifiants Steam « nuits » | 17 (lots 0B/0C) |
| [19 — Lore](19-lore.md) | Transversal | Étape A : incohérences, textes trop directs, 14 questions à trancher, pistes de révélation | 06 (casting), 16 (échos), 17 (noms d'armes) |
| [20 — Récompense et puissance](20-recompense-et-puissance.md) | P0 (R1 capital) | Retours du 28 septembre ; modèle de progression révisé pour 300 à 400 niveaux en 45 min (§6.6), cibles à valider ; paliers validés ; lots D1, R1-0 et R1-T livrés ; R1-F proposé | 03, 05, 06, 13, 16, 17 |
| [21 — Système de jeu](21-systeme-de-jeu.md) | **P0, référence unique** | Fixe le build : 4 armes (niveau 50, ascension), 6 objets à 50 niveaux, 4 Réminiscences après les Résurgences, personnages à affinités. [Historique](21-historique.md) ; lot G1 livré, G2a en cours | 05, 06, 17, 20, 22 |
| [22 — Carte à explorer](22-carte-a-explorer.md) | **P0** | Décors qui deviennent des lieux, Atelier et Trempe, Reliquaire, Repères, Atlas ; absorbe 13, 11 B et les POI de 18 ; à valider | 21, 12, 16, 17 |

Les numéros servent à identifier les plans, pas à imposer leur exécution intégrale dans cet ordre. Le [registre](DECISIONS.md) fait foi pour leur statut de validation.

## 3. Séquence recommandée

1. Utiliser le mode dev pour les essais. Prioriser ensemble 06 (refonte et validation d’au moins cinq à six personnages) et 08 (tous leurs sprites à refaire, échelle et style communs). Valider les identités et les nouveaux sprites en jeu avant les mobilités spécifiques du lot 01 E ; le dash commun reste la référence validée.
2. Construire une première référence avec le contenu existant et les lots A–C de 03 : contrôle, menace dès le départ, cadence XP, exploration et Résurgence. Réévaluer la difficulté après introduction de mobilité active.
3. Définir les contrats de contenu de 05 et 06, sélectionner les rôles de 07 et produire les prototypes nécessaires. Réaliser la lisibilité prioritaire de 04, puis assembler l'expérience complète décrite ci-dessous. Les écrans finaux dépendent de ces catégories.
4. Approfondir les objets cumulables, livrer les déblocages directs, la Collection et le Hub épuré. Ajouter les créatures de 07, les assets de 08 et les améliorations terrain retenues de 10. Un prototype de 11 à la fois, selon ses dépendances.
5. Tester plusieurs runs complètes, finaliser le late game/endgame et les critères de sortie.
6. Traiter 09 seulement après stabilisation des règles de score.

Un seul lot d'implémentation ouvert à la fois. Un échec de lisibilité, de performance ou de fun ramène au lot concerné avant expansion.

**Mise à jour du 29 septembre, nuit — plan 21 version 2 et lot G1 livré :** Raphaël tranche le plan 21 ([DECISIONS §32](DECISIONS.md)) :
- armes au niveau 50 par stats aléatoires, avec ascension à deux voies au niveau max ;
- objets en 6 emplacements jusqu'au niveau 50 ;
- fragments après les Résurgences ;
- bannissements gratuits puis payés en Péril ;
- courbe de 300 à 400 niveaux conservée.

Le [lot G1](21-historique.md) est livré. Question ouverte : les traits, dont l'agent recommande la fusion dans les objets.

**Mise à jour du 29 septembre, soir — refonte du build depuis les principes :** Raphaël fixe [douze principes de conception des builds](../PRINCIPES-BUILD.md) et demande une reconception neutre. Le [plan 21](21-systeme-de-jeu.md) propose cinq familles aux rôles nets et trois canaux d'acquisition :
- armes (voies au niveau 5) et traits sur propriétés communes, par le level-up ;
- objets à déclencheurs communs, par le monde ;
- fragments après chaque Résurgence.

Il propose aussi une courbe de 80 à 110 niveaux au lieu de 300 à 400. Les catalogues d'objets et de perks précédents deviennent des matériaux pour ce plan. À valider avant tout code.

**Mise à jour du 29 septembre, suite — proposition 2 des objets et des icônes, à valider :** le [catalogue d'objets](05-objets-catalogue-v1.md) compte 30 objets, dont 19 à condition, contrepartie ou effet sur le monde et 11 multiplicateurs. Il ajoute les déblocages : 13 objets au départ, les autres par quêtes ou achats en Vestiges au Hub. Les [icônes de perks](planches/perks-icones-proposition-2.png) deviennent des fragments teintés par famille, aux pictogrammes repris des retours visuels du jeu ([plan 05 §15](05-perks-specialisations.md)).

**Mise à jour du 29 septembre — catalogue d'objets révisé et planche d'icônes de perks, à valider :** le [catalogue d'objets V1](05-objets-catalogue-v1.md) propose 23 objets. Il retire les cinq qui copiaient une signature d'arme, donne une destination aux treize passifs et une source à part pour la récompense à choix (le Reliquaire, Triptyque du plan 13). La [planche d'icônes de perks](planches/perks-icones-proposition-1.png) propose des pin's émaillés, en deux variantes d'émail. Rien n'est créé en jeu avant validation.

**Mise à jour du 29 septembre — B3 sans objets livré :** [plan 05 §14](05-perks-specialisations.md#14-compte-rendu-b3-partie-sans-objets--sillage-et-seconde-lecture-29-septembre-2026). Sillage et Seconde lecture sont actifs. Propagation, mesurée sur les trois armes de contrôle, reste inchangée : seule la Cloche échoue, c'est un réglage d'arme. Délestage et Habitude sont bloqués : il n'existe ni inventaire d'objets ni récompense à choix. Banc d'effets de 66 assertions.

**Mise à jour du 29 septembre — lot B2 livré (effets de combat et de survie) :** [plan 05 §13](05-perks-specialisations.md#13-compte-rendu-b2--effets-de-combat-et-de-survie-livrés-le-29-septembre-2026). Prévoyance, Reprise, Débordement, Convergence et Propagation sont actifs en run, avec leurs retours visuels. S'y ajoutent l'état inactif, la pause chiffrée et l'avertissement avant un échange d'arme. Banc d'effets de 50 assertions, captures regardées, banc dense A/B sans écart hors bruit (sans perk actif dans le banc). Réglage à trancher : Propagation ne se déclenche presque pas avec la Cloche seule. Suite proposée : B3.

**Mise à jour du 29 septembre — lot B1 livré (acquisition des perks) :** [plan 05 §12](05-perks-specialisations.md#12-compte-rendu-b1--acquisition-livrée-et-vérifiée-le-29-septembre-2026). Quatre droits aux paliers 2/6/12/20 dans la file de niveaux, report, cascade, relance, bannissement protégé, première offre composée, éligibilité selon l'arsenal, carte et pause. En sommeil en run normale tant qu'aucun effet n'est branché ; vérifié par un banc de 21 assertions et une capture en aperçu. Suite proposée : B2.

**Mise à jour du 28 septembre — mort refondue, lot M1 livré :** [plan 02 lot D, seconde passe](02-juiciness-score.md). La mort se joue dans le monde : impact et ralenti, le joueur se défait, l'Effacement part de sa place et décolore l'écran, le tueur reste en couleur. Le bilan suit sans titre explicite. Direction validée ([DECISIONS §22](DECISIONS.md)) : une seule page de bilan dense, distance mise en avant. Suite proposée : M2 (données de run).

**Mise à jour du 28 septembre — lot M2 livré :** [plan 02 lot D](02-juiciness-score.md). La run relève sa distance (au plus loin et parcourue), une frise, les éliminations par arme, les élites, Souverains et boss. L'historique passe en version 3 sans perdre les runs V2 ; une montée de version les aurait effacées, c'est corrigé. Suite proposée : M3 (bilan dense).

**Mise à jour du 28 septembre — lot M3 livré :** [plan 02 lot D](02-juiciness-score.md). Le bilan tient en une page dense. En tête : distance, score et durée. Au centre : le tableau des armes (part, DPS, éliminations), le tueur et les faits. Dessous : la frise de la run. Suite proposée : M4 (gains animés).

**Mise à jour du 28 septembre — lot M4 livré, refonte de la mort terminée :** [plan 02 lot D](02-juiciness-score.md). Les cartes de gain se retournent une à une et les Vestiges montent dans un compteur. Passer la révélation donne le même état final. Reste à juger en jeu : durée de la séquence, densité de la page, échelle des distances.

**Mise à jour du 28 septembre — lot 6B livré :** le feedback du cône du Transistor est cadencé, les dégâts et procs restent à chaque tick. À 50 cibles sur quatre secondes : allocations directes **2,66 → 1,41 Mo (−47 %)** et feedback **12 000 → 2 000 (−83 %)**, mêmes dégâts et attribution. 38 assertions et régressions armes/capacités vertes, smoke et captures avec chaîne/homing vérifiés. [Preuves, scripts et limites](../audits/performance-2026-09-28/lot-6b/README.md). Aucun gain FPS revendiqué. Suite proposée : 5B (préchauffage).

**Mise à jour du 28 septembre — lot 6A livré après autorisation :** les statuts expirent au loin et l’Effacement conserve ses pas manqués (rattrapage limité à quatre par image). Même banc : 13 échecs avant, zéro après sur 26 assertions ; à 1 Hz, 30,80 % d’oubli en dix minutes au lieu de 15,40 %. Build sans avertissement, capacités/déplacement, smoke et capture de run vérifiés. L’intégration Main headless expire avant comme après et reste signalée. [Preuves et limites](../audits/performance-2026-09-28/lot-6a/README.md). Prochain lot : 6B (cône).

**Mise à jour du 28 septembre — seconde passe de performances et robustesse (diagnostic uniquement) :** [rapport](../AUDIT-PERFORMANCES-2026-09-28.md), [preuves et scripts](../audits/performance-2026-09-28/README.md), [lots proposés au plan 10 §11](10-terrain-et-tiles.md#11-audit-de-performances-du-27-septembre--lots).
- Cône : 12 000 impacts et 3,05–3,18 Mo alloués dans quatre secondes d’émission sur 50 cibles, après chauffe.
- Deux défauts temporels reproduits : statuts lointains qui n’expirent pas ; temps excédentaire perdu par l’Effacement.
- Préchauffage : zéro dessin hors champ contre huit dans le champ. Physique : 10 694 corps de décor, mais seulement 2 318 formes ; potentiel mémoire estimé des corps inutiles ≈5,88 Mio.
- Vingt cycles techniques Hub/run/Hub : 148 nœuds et zéro orphelin à chaque retour. Profils managés de foule archivés. A/B final au calme : 2,28–2,49 ms/image à 60 ennemis, 7,42–7,58 ms à 240 ; inspection du banc trop faible pour expliquer cette pente. Pic initial de 108–137 ms à isoler de la mesure du combat. Aucune conclusion de FPS sous charge.
- Suites proposées : 6A horloges, 6B impacts continus, 5B préchauffage, 3B attribution foule, 6C cellules actives, 6D corps optionnels. **Aucun correctif de production, aucune case de roadmap cochée ; travail de feu préexistant préservé.**

**Mise à jour du 28 septembre, nuit :** [plan 20 §6.7–§6.8](20-recompense-et-puissance.md). Paliers validés ; réserve de niveaux automatique ; montée plus forte en endgame.
- Lot R1-0 livré : courbe d'XP en données (`data/scaling/progression.json`), plafonnée à 3 000 XP par niveau.
- Lot R1-T livré : temps pour tuer mesuré. Sur 45 min, il baisse de moitié même pour le bot ; 2 300 à 2 600 orbes au sol à 45 min.
- L'endgame n'est jamais atteint, car l'Indicible n'est pas combattable : R1-F proposé, avec des PV ×1,07 par minute après 22 min.

**Mise à jour du 28 septembre, soir :** [plan 20](20-recompense-et-puissance.md) après les réponses de Raphaël.
- §6.6 : une excellente run vise 300 à 400 niveaux en 45 min. Coût de niveau plafonné à 3 000 XP au-delà du niveau 41 (simulé : 358 niveaux à 45 min) ; réserve de niveaux pour enchaîner les choix ; niveaux de surplus au-delà d'un build complet (≈ 219 montées).
- Lot D1 livré (§7.2) : Présage moins fréquent et absent avant 3 min, dégâts à distance à ×1,02 par minute, tir du Hurleur annoncé. Zones et tirs : 46,6 → 34,2 % des dégâts filtrés.

**Mise à jour du 28 septembre, après-midi :** [plan 20 §6–§9](20-recompense-et-puissance.md#6-modèle-de-progression--28-septembre-2026-proposition-cibles-à-valider). Mesure de 30 min (bot nomade, deux seeds) et `tools/progression_model.py`.
- Niveaux cibles par archétype à valider, leviers d'XP chiffrés et simulés (temps, oubli, Résurgences, Péril, boss intermédiaire à rang choisi, Indicible, build).
- Constats : l'XP par PV à abattre est divisée par 4 à 30 min ; les orbes laissées pèsent au plus 15 % de l'XP (750 à 920 au sol), alors que 73 % des créatures apparues ne meurent jamais.
- Dégâts à distance (`tools/damage_sources.py`) : c'est le Présage qui pèse (24 % des coups, présent dans tous les biomes), pas les tireurs, qui blessent en proportion de leur présence.
- Densité vers 1 000 créatures : environ trois fois moins de coût par créature à trouver.
- Rien n'est implémenté côté jeu.

**Mise à jour du 27 septembre, nuit (session locale, sans Raphaël) :** machine enfin calme, [audit de performances](../AUDIT-PERFORMANCES-2026-09-27.md) repris en lots ([10 §11](10-terrain-et-tiles.md)).
- Lot 1 : le pool d'ennemis ne laisse plus d'ennemis orphelins en fin de run (20 par run avant), abonnement de la montée de niveau désabonné. Le brouillard, invisible depuis mars, est retiré ; **à arbitrer** : l'Oubli du regard n'avait donc aucun effet visible.
- [17 3D](17-armes-coffres-modificateurs.md) : un coffre posé après un Oubli des repères naît avec sa colonne raccourcie.
- Lot 2 : le sol et les routes se dessinent depuis un seul atlas, à l'image identique. À 1 080p, 156 → 255 FPS et 1 894 → 331 appels de dessin par image sur le banc de combat dense.
- Banc des chemins T3 fait : pas de coût mesurable.
- [02 lot D](02-juiciness-score.md) et [04 C2](04-interfaces-et-hub.md) : une arme gagnée pendant la run a sa carte au bilan, avec « Voir dans la Collection », qui ouvre la Collection sur elle.
- [04 lot C](04-interfaces-et-hub.md) : les colonnes de la pause défilent au stick droit et à Page haut/bas.
- Crash à la fermeture du jeu lancé depuis Godot (préexistant) corrigé par une sortie propre ([10](10-terrain-et-tiles.md)).
- Audit, lot 4 : les orbes d'XP laissées loin s'endorment. Un nomade en laisse environ 300 en 10 minutes ; avec 400 orbes, 720p passe de 244 à 289 FPS.
- [08](08-direction-artistique.md) : auras d'affixe en pixel art, sous les pieds.
- [03 lot E](03-boucle-et-rythme.md) : **l'Indicible n'est pas combattable** (aucune arme ne le vise, 0 PV perdu en 40 s de combat). Sa mort et le passage en endgame marchent. Refonte à arbitrer avec le lore.
- [17](17-armes-coffres-modificateurs.md) et [02 J3](02-juiciness-score.md) : arme au sol refaite (anneau pixel, invite commune « Échanger », plus de stats ni de texte au sol), icône qui vole vers le HUD.
- Audit, lot 5 : les animations des créatures se chargent sous l'écran de chargement. Avant, chaque nouvelle espèce faisait sauter une image en pleine partie.
- [10 lot E](10-terrain-et-tiles.md) : les herbes plient aussi au passage des huit créatures les plus proches.
- Explosion des créatures instables en effets pixel recyclés, qui montrent la zone réelle des dégâts, mesurée au sol ([10](10-terrain-et-tiles.md)).
- [06](06-personnages-quetes-defis.md) : un profil neuf commence avec le Vagabond (décision du 23 septembre) ; les profils existants gardent leurs personnages.

**Mise à jour du 27 septembre, soir (session locale) :** deux bugs signalés par Raphaël.
- [Son des coffres](15-audio.md) : la mélodie de révélation ne part plus pendant le défilement et s'éteint avec l'écran.
- [Chargement](10-terrain-et-tiles.md) : plus de frame de carte vide au départ, et chargement ramené de 6,0 à 3,5 s (génération sur un thread, décors construits hors de l'arbre).
- [07 lot B](07-bestiaire-et-rencontres.md), fin : tirs du Cracheur, de la Sentinelle et de la Tisseuse annoncés par un couloir de visée ; portée de la Sentinelle dessinée au sol.
- [04 lot C](04-interfaces-et-hub.md) : paramètres entièrement navigables au clavier et à la manette (ils étaient à la souris seule), pause ouverte sur « Reprendre », cadre doré sur le focus.
- [03 lot C](03-boucle-et-rythme.md) : présage des Résurgences, les bords de l'écran se ternissent pendant l'avertissement et les créatures s'agitent.
- [02 lot D](02-juiciness-score.md) : le bilan montre les dégâts de chaque arme, la principale en or.
- [17 lot 3B](17-armes-coffres-modificateurs.md) : au Mémorial ravivé, relancer les bénédictions contre de l'Essence.
- [10 lot D](10-terrain-et-tiles.md) : contrôle automatique de connectivité ; sur 13 seeds, les 31 coffres, Mémoriaux et Failles de chaque carte sont accessibles à pied depuis le départ.
- [10 lot E](10-terrain-et-tiles.md) : herbes, fleurs et roseaux plient au passage du joueur (shader, aucune boucle sur les décors).
- [16 O6](16-oubli-sensible.md) : les échos de l'oubli montrent de vrais habitants (écolière, vieil homme, ouvrière) au lieu des personnages jouables.
- [08, ennemis](08-direction-artistique.md) : les huit ennemis de mars (Brute du Vide, Ombre, Hurleur, Sentinelle, Cracheur Pâli, Rampant, Rampant d'Ombre, Tréant) refaits dans le pipeline, à l'échelle du joueur. Toutes les créatures en jeu sont désormais procédurales.

**Mise à jour du 27 septembre, suite (session locale, sans Raphaël) :**
- Bancs de A-VERIFIER toujours pas faits : charge entre 4 et 7,7 toute la session (seuil fixé à 2).
- [07 lot B](07-bestiaire-et-rencontres.md), étapes 1 et 3 : la charge de la Brute, le surgissement du Rampant et le cri du Hurleur s'annoncent au sol et laissent une fenêtre ; le Rampant ne blesse plus enfoui ; tuer un Hurleur qui crie coupe l'appel. Sons et animations branchés.
- [04 lot B](04-interfaces-et-hub.md), étapes 1 à 4 : échelle typographique commune (rien sous 14 px en base 1080p, lisible en 720p), réglage « Taille du texte » 100/115/130 %, colonnes de la pause et onglets des paramètres qui défilent.
- [08 P6b](08-direction-artistique.md) : un chantier par région de la carrière, sur le modèle des fermes (entrée de galerie étayée, voie et wagonnets, baraque, machines groupées). Le placement des fermes et des chantiers passe par un même `SiteComposer`.

**Mise à jour du 27 septembre (session locale, sans Raphaël) :**
- Passage de vérification sur le Mac de ce que la session cloud avait livré sans GPU ([A-VERIFIER](A-VERIFIER.md)) : bilan, level-up, chemins, fermes, repères et échos regardés en capture. Corrigés : murmure des échos flou, brèche du toit de l'église trop régulière.
- [17 2B](17-armes-coffres-modificateurs.md) : chaque arme reprend la couleur de son icône dans ses effets d'attaque.
- [17 2C](17-armes-coffres-modificateurs.md) : arme en main en essai, derrière une option désactivée par défaut.
- [07 lot B](07-bestiaire-et-rencontres.md) : **deux mécaniques qui n'avaient jamais marché** réparées. La charge de la Brute du Vide avançait à 35 px/s (vitesse écrasée par la poursuite), et le recul des armes ne repoussait rien. Réglages des comportements passés en données ; recyclage du pool vérifié par un test.
- [04 lot B](04-interfaces-et-hub.md) : une quarantaine de textes français sans accents corrigés.
- [05](05-armes-objets-builds.md) : les six synergies de perks s'annoncent sans aucun effet ; à arbitrer.
- [08 P5 et P6](08-direction-artistique.md) : décors du marais refaits à l'échelle du personnage, décors propres à la carrière (roche, cristaux d'Essence, machines figées, traces des ouvriers). P3 à P6 sont livrés.
- Non fait : bancs de performance (machine chargée toute la session).

**Mise à jour des 26 et 27 septembre (nuit, session cloud sans Raphaël) :**
- [10 T3](10-terrain-et-tiles.md#lot-t3-livré--chemins-et-routes-26-septembre-2026) : chemins de terre entre les régions, raccordés aux rues, avec un style par biome. Les rues verticales ne sont plus coupées en deux bandes, et les trottoirs ont des bordures usées.
- [16 O6](16-oubli-sensible.md) : échos de l'oubli. Dans les zones Fragiles, la silhouette pâle d'un habitant apparaît parfois, puis se dissout à l'approche en laissant un murmure.
- [08 P4b-1/2](08-direction-artistique.md) : une ferme par région des champs, reliée au réseau de chemins par un embranchement (maison, grange, silo, hangar, enclos, haies).
- [02 J0](02-juiciness-score.md) terminé : les effets de mort et la collecte d'XP sont recyclés, et le panneau des quêtes ne recrée plus ses lignes quatre fois par seconde. Banc de combat dense : 284 → 8 nœuds créés en 15 s.
- [07 §7](07-bestiaire-et-rencontres.md) : perception et laisse propres à cinq créatures (valeurs provisoires, mesure à refaire).
- [10 T2](10-terrain-et-tiles.md) : décors de transition mêlés le long des frontières de biomes. [08 I3](08-direction-artistique.md) : hauteur de vol unique des projectiles.
- [16 O2](16-oubli-sensible.md) : en zone Effacée, des éclats du Néant s'élèvent des décors proches.
- [08 P4b-3](08-direction-artistique.md) : haies, murets et clôtures aux bords des parcelles, vergers en rangs.
- [16 O5](16-oubli-sensible.md) : un Autel qui sert rappelle sa zone à l'existence. Depuis la fusion de main, c'est le réveil d'un Mémorial qui le fait (les Autels n'existent plus).
- [08 P4b-4](08-direction-artistique.md) : scènes-récits dans les champs (pique-nique abandonné, linge étendu, épouvantail aux corbeaux, tracteur embourbé).
- [02 J1](02-juiciness-score.md) : retour de coup sans tween (recul sur le visuel seul), chiffres de dégâts en Saira, cumulés par cible, critique distinct par la forme.
- [08 P2](08-direction-artistique.md) : l'église et le pylône de télécommunication reviennent comme repères rares des Ruines Urbaines, refaits dans le pipeline.
- [02 J2](02-juiciness-score.md) : morts orientées par le dernier coup (dissolution, éclats, nuage), orbes d'XP qui jaillissent du corps, onde et éclair à la mort des élites.
- [02 J3](02-juiciness-score.md) : orbes qui s'étirent et laissent une traînée, son de ramassage qui monte le long d'une chaîne, barre d'XP qui pulse.
- [02 J4](02-juiciness-score.md) : montée de niveau avec onde dorée, colonne de lumière, créatures proches repoussées (en apparence), barre d'XP qui éclate, écran de choix qui entre avec du punch.
- [02 J5](02-juiciness-score.md) : compteur de morts en rafale (« ×24 »), léger recul de caméra quand l'écran se remplit, voix sonores coupées de la plus ancienne à la plus récente.
- [02 J6](02-juiciness-score.md) : traces de pas selon le sol (ronds dans l'eau, poussière), coffres qui frémissent à l'approche.
- [02 lot A](02-juiciness-score.md) : score exact. Horloge de jeu qui exclut les pauses, score qui monte au HUD entre deux kills, « nouveau record » enfin affiché au bilan, barème en JSON.
- [02 lot D](02-juiciness-score.md) : bilan de fin de run refait en trois zones (score et record, personnage et build complet, gains), révélé en trois temps après que le monde a pâli.
- [04 C2](04-interfaces-et-hub.md) : Collection dans le menu de l'accueil. Armes et souvenirs de run en grille, détail et condition de déblocage à la demande, même règle de disponibilité que le loot.
- [07 lot A](07-bestiaire-et-rencontres.md) : audit des données du bestiaire (`tools/audit_bestiary.py`). La Tisseuse ne sort qu'en crise dans les Marécages ; propositions à arbitrer.
- [07 lot C](07-bestiaire-et-rencontres.md) : un Colosse par crise, livré puis **retiré** à la fusion de main, qui a supprimé les Colosses (plan 17 lot 0C).
- [03 lot C](03-boucle-et-rythme.md) : pendant une Résurgence l'oubli s'accélère ; à l'accalmie, Essence doublée 30 s et coffre rare posé à portée devant le joueur (V2 §8).
- **À arbitrer en priorité** : la Tisseuse hors des Marécages ; le barème des points d'intérêt (valeurs de `pois.json` inutilisées) ; la recette du bilan de fin de run et de la Collection (premières passes sans maquette validée).
- 27 septembre : main (plans 17 et 18) fusionnée dans la branche de la session. Les apports de la nuit sont reportés sur les coffres, le level-up et les Mémoriaux de main ; ce qui dépendait des Autels, des Colosses ou des raretés d'armes a été retiré.
- Travail fait dans un conteneur sans GPU : les points à revérifier sur la machine de Raphaël sont listés dans [A-VERIFIER.md](A-VERIFIER.md).

**Mise à jour du 26 septembre (plan 17, vague 0 livrée) :** coffres réintégrés et visibles ([17](17-armes-coffres-modificateurs.md#lot-0a-livré--26-septembre)).
- 23 coffres sur toute la carte au lieu de 10 à 15 autour du départ, dégagés des décors.
- Sprites à l'échelle des personnages, colonne de lumière à la couleur de la rareté, invite « Ouvrir », flèches de bord d'écran.
- Palette de rareté unique ; butin montré tel qu'obtenu, sans perks V1 ni malédictions.
- Mesure de densité désormais headless en temps accéléré (`tools/measure_run.sh`) : 5 seeds × 3 min en ≈ 1 min.
- Enchaînés le même jour, à la demande de Raphaël : 0B (restes V1 visibles retirés, POI et éléments de lore désactivés), 0C (code, données et 306 PNG morts, Colosses), 0D (références au jeu du genre ramenées à une section de garde-fous).
- Deux correctifs trouvés en route : blocage de partie au level-up, durée de run comptée en temps réel.
- Vague 1, sans attendre la décision 4.3 : les quatre bugs de build corrigés (effets de l'arme qui frappe, notes de la Boîte à musique, niveau unique, bannissement), banc `tools/test_weapons.sh` ; pilote de trois icônes d'armes 32×32 à juger.
- Soir : décision 4.3 prise (rareté seulement sur les améliorations) ; coffres et icônes v2 ; vague 1 livrée : croissance des armes en données, level-up à raretés (Chance et oubli), cartes « avant → après », pause avec équipement et fiche. Recette groupée attendue.
- Nuit : vague 3 livrée ([17](17-armes-coffres-modificateurs.md#vague-3-détaillée--26-septembre-soir)). Péril à la place de l'Appel du Vide et des malédictions (3A) ; Mémoriaux à raviver à la place des Autels (3B) ; Failles et Oublis (3C). Écran de choix commun et lieux interactifs partagés avec les coffres. Question ouverte : ces écrans figent la run (V2 §11 voulait l'Autel sans pause). Vague 2 en attente des noms d'armes et du style d'icônes ; vague 4 dépend du plan 13.
- Nuit (suite), après les retours de Raphaël (pause gardée, noms et icônes validés, Oublis de carte validés) : lots 2A (noms, histoire des armes dans la pause), 2B (24 icônes 32×32, HUD agrandi) et 3D (neuf Oublis de carte) livrés. Restent : Craies et Chronomètre à confirmer ; 2C (arme en main) non commencé ; vague 4 dépend du plan 13.

**Mise à jour du 26 septembre (armes, coffres, modificateurs) :** nouvelle priorité de Raphaël, auditée puis planifiée, rien d'implémenté.
- [Plan 17](17-armes-coffres-modificateurs.md) : armes (sprites, noms, présentation, level-up à raretés, pause), coffres, modificateurs de run (Chance, Péril, Mémoriaux et Failles), objets ensuite, north star.
- Coffres : ils apparaissent toujours (13 et 14 dans les logs du jour) mais ne sont jamais ouverts. Sprites restés à 16×12 face aux décors agrandis, placement confiné à 7,6 % de la carte depuis le passage au rayon 200, aucun signal.
- [Plan 18](18-inventaire-restes-v1.md) : inventaire des restes V1. Les visuels « lisses » viennent du rendu procédural (polygones, dégradés, lumières texturées par le logo Godot), pas des PNG.
- Décisions attendues : plan 17 §6 et plan 18 §5.

**Mise à jour du 26 septembre (soir) :**
- Raphaël valide les tuiles de la forêt (« mille fois mieux »).
- Le lot T1 couvre quatre biomes sur cinq (forêt, carrière, champs, marais) en tuiles de Wang ; la ville reste à faire ([10](10-terrain-et-tiles.md)).
- Vue isométrique respectée pour les sorts et effets, lots I1–I4 : projectiles orientés, annonces au sol, ombres de contact, finitions ([08](08-direction-artistique.md#respect-de-la-vue-isométrique--chantier-du-26-septembre-2026)).
- Bord du monde infranchissable.
- Créatures distancées qui perdent la trace ([07 §7](07-bestiaire-et-rencontres.md)).
- Suite : la ville en T1, O3 (frontière de l'oubli), T3 (chemins).

**Mise à jour du 26 septembre (après-midi) :** Raphaël valide l'accueil et les effets d'attaque, trouve l'ouverture « un tout petit trop peu adoucie », valide l'ordre des chantiers et délègue les autres arbitrages ([DECISIONS §7](DECISIONS.md#7-arbitrages-délégués-du-26-septembre), provisoires).
- Nouvelle machine, un Mac Apple Silicon. Steam y levait une exception à chaque frame ; les outils de test écrivaient dans le vrai profil. Les deux sont corrigés.
- Ouverture adoucie d'un cran ([03 §7](03-boucle-et-rythme.md#7-ouverture-de-run--26-septembre-2026)).
- Jonctions tramées entre biomes ([10 T2](10-terrain-et-tiles.md#lot-t2-livré--26-septembre-2026)) et sol qui oublie ([16 O1](16-oubli-sensible.md#6-arbitrages-et-lot-o1--26-septembre-2026)), par un même shader du sol.
- Suite prévue : T1 (sol refait), O3 (frontière de l'oubli), 07 §7 (perception des créatures).

**Mise à jour du 26 septembre (retours de jeu) :** début de run adouci (6 s de répit, montée sur une minute, [03 §7](03-boucle-et-rythme.md#7-ouverture-de-run--26-septembre-2026)) ; sons d'attaque ennemis branchés, provisoires ([15](15-audio.md)) ; décors découpés en tronçons, ≈ ×2 FPS en combat dense ([10 §10](10-terrain-et-tiles.md#10-investigation-performance--26-septembre-2026)). Propositions à arbitrer : composition des champs (08 P4b), tiles et jonctions (10 §9), [plan 16](16-oubli-sensible.md) sur l'oubli, perception des créatures (07 §7).

**Point d'arrêt du 26 septembre (retours de jeu) :** livrés et vérifiés (build, smoke, régressions, bancs, captures) : ouverture de run, sons d'attaque provisoires, tronçons de décors. Non fait : relecture `godot-reviewer` du diff lancée mais pas intégrée ; recette en jeu de ces trois points ; lot suivant de l'investigation perf (physique interne du moteur, 10 §10) ; aucun des lots proposés (08 P4b, 10 §9, 16, 07 §7) n'est commencé, en attente d'arbitrage.

**Mise à jour du 26 septembre :** l'écran d'accueil est refait sur demande de Raphaël (« trop classique », pas de stats mais le sprite, « surprends-moi »). Le Hub devient le camp du Foyer, vivant ; les personnages veillent autour du feu ; le menu est textuel. Compte rendu et limites : [plan 04](04-interfaces-et-hub.md#accueil-refait--26-septembre-2026). Recette attendue.

**Mise à jour du 25 septembre :**
- Audio : [50 choix branchés, nettoyage et suite](15-audio.md) ; [archives hors dépôt](../audio/README.md).
- Recette de Raphaël : cadence des micro-événements validée en l'état, élites bien dosées, micro-événements appréciés, HUD « bien mieux ». Le soin, peut-être trop rare, est noté pour les plans 13 et 03.
- Nouveau chantier prioritaire : **visuel et juiciness de tout le jeu**.
  - Régression des biomes : cause trouvée dans l'historique (14 mars), mesurée, corrigée ([10 §6](10-terrain-et-tiles.md#6-régression-un-seul-biome-autour-du-départ--25-septembre-2026)).
  - Audit des décors ([10 §7](10-terrain-et-tiles.md#7-audit-des-décors--25-septembre-2026)) : pas de sprite manquant, mais des petits décors camouflés qui bloquent, des collisions décalées devant les décors, des immeubles traversables et aucun tri en profondeur. Lots D1–D3.
  - Refonte procédurale des décors, urbain d'abord : lots P0–P6 du [plan 08](08-direction-artistique.md#décors-procéduraux--chantier-du-25-septembre-2026).
  - Juiciness : liste priorisée J0–J6 du [plan 02 §7](02-juiciness-score.md#7-chantier-prioritaire-du-25-septembre--juiciness-de-tout-le-jeu).
  - Effets d'attaque du joueur et des ennemis refaits en pixel art dans les couleurs de la charte, avec un onglet de réglages (activation, opacité, secousses) : lots V0–V3 du [plan 08](08-direction-artistique.md#effets-dattaque--chantier-du-25-septembre-2026), recette attendue.
- Les plans 13 et 14 restent non arbitrés.

**Mise à jour du 24 septembre :**
- Raphaël valide le design des nouveaux ennemis. Le personnage joué passe à environ 30 px avec un idle animé (08). Son redessin reste reporté.
- HUD refait : plaques contrastées, jauge de PV sous le héros, boussole retirée. Nouvelle police Saira Semi Condensed (04).
- Nouveau [plan 12 — micro-événements et variantes renforcées](12-micro-evenements.md) : cinq événements toutes les 2 à 3 minutes autour des Résurgences, élites naturelles et Souverains. v1 implémentée, recette humaine attendue.
- Demandes suivantes consignées sans implémentation : [plan 13 — butin](13-butin.md) (objets aléatoires à raretés, trois formes de présentation, sources visibles de loin) et [plan 14 — anomalies du monde](14-anomalies-du-monde.md) (événements rares liés au joueur et à l'oubli). Le socle d'objets du plan 05 est leur prérequis.

**Mise à jour du 23 septembre :**
- Les saccades périodiques en combat dense sont corrigées (01 §10). Raphaël trouve le jeu « beaucoup plus fluide » ; la cible 1080p reste ouverte.
- Le début de run reste trop facile. Son hypothèse porte sur le bestiaire : trop passif, trop de corps à corps, pas assez d'attaques à distance originales. Elle est intégrée en 03 A2 et 07, où des menaces à distance originales sont proposées.
- Six [fiches de casting](06-fiches-casting.md) et l'audit des sprites (08 §2) attendent sa validation.

**Suite immédiate du plan 01 (historique du 22 septembre) :** la [mesure en combat dense](01-deplacements.md#9-mesure-de-mobilité-en-combat-dense--22-septembre-2026) est réalisée avec 120 ennemis en 720p/1080p ; elle révèle que les 60 FPS constants ne sont pas tenus. Le diagnostic de ces saccades reste un travail technique à mener avant de conclure la recette. Les variantes clavier/manette et le tempo de début de run restent à évaluer ; aucun nouveau mouvement spécifique ne précède le casting. Le prochain lot de conception prioritaire reste 06 A / 08 A : six fiches et une planche commune à présenter, puis une référence visuelle jouable. Les identités et les choix de dimensions/orientations restent à valider ; le numéro 02 n’impose pas de passer avant cette priorité.

## 4. Expérience de référence proposée

Périmètre de validation, pas réduction du catalogue définitif :
- Un personnage abouti, dans un biome, avec contrôle fluide et une mobilité active retenue après essai.
- Trois armes déjà présentes : Lame Ébréchée, Arc de Fortune et Marteau Lourd, sous réserve de leurs noms/données vérifiés dans le plan 05.
- Trois objets prototypes cumulables, leurs raretés et deux combinaisons aux effets visibles.
- Quatre rôles ennemis, un ennemi fort et une Résurgence.
- Un coffre, un Autel, un fragment de lore rapide.
- Score animé sans record pendant la partie, HUD lisible, bilan de mort refondu et un déblocage direct présenté dans la Collection.

Première session : 8 à 12 minutes pour isoler les sensations ; ensuite runs complètes aux durées de la V2. Ce test court n'impose pas de fin artificielle au jeu final.

**Passage au lot suivant :** commandes comprises immédiatement ; au moins deux styles de combat perçus ; joueur capable d'expliquer un détour, une amélioration et sa mort ; prochaine tentative choisie pour une raison précise.

## 5. Protocole de validation

Pour chaque plan, Raphaël peut répondre : **validé**, **validé avec modifications**, **à retravailler** ou **reporté**, en citant son numéro et les décisions concernées. La validation porte sur le périmètre et les choix explicités, pas sur toutes les extensions futures mentionnées.

Chaque implémentation devra fournir :
- Le lot réalisé et les différences par rapport au plan approuvé.
- Les captures ou séquences avant/après adaptées au changement.
- Les résultats techniques et les observations de playtest, distingués.
- Les décisions encore ouvertes et l'impact sur les lots suivants.

Les réglages numériques proposés sont des points de départ expérimentaux, jamais des résultats mesurés. Il n'y a pas d'estimation de durée ferme avant diagnostic du premier lot.

## 6. Vérifications communes

- C# : `dotnet build` avec zéro warning.
- Scènes, shaders, projet ou initialisation : `tools/smoke_test.sh`, puis essai visuel du parcours touché. Un démarrage headless du Hub ne prouve pas une run correcte.
- Performance : même machine, résolution et scène avant/après ; combat dense avec au moins 100 ennemis ; cible 60 FPS. Noter modèle matériel, moyenne, pics, mémoire et coût des effets.
- État : nouvelle sauvegarde et ancienne sauvegarde de test ; sortie/retour Hub ; événements non dupliqués ; aucune perte de déblocage.
- Accessibilité : texte lisible à 1280×720 et 1920×1080, grand texte, navigation clavier/manette, effets réduits, information indépendante de la seule couleur.
- Architecture : valeurs d'équilibrage JSON, ressources/cache/pools, pas d'allocation ni recherche de nœuds par frame ajoutée, signaux/EventBus entre systèmes.
- Après validation réelle d'un lot : cocher uniquement les items concernés dans la [roadmap V2 §25](../VESTIGES-STRATEGIE-V2.md#25-phases-de-développement). La création de ces plans ne valide aucune fonctionnalité.

## 7. Traçabilité des retours

| Retour | Plans responsables |
|---|---|
| Accueil, menus, onglet Exploration, police | 04 + 08 |
| Vrai choix, davantage de personnages, meilleurs sprites | 06 + 04 + 08 |
| Armes par défaut et à débloquer, tri du catalogue | 05 + 06 |
| Objets variés, effets intéressants, inspiration Megabonk | 05 + 06 |
| HUD intuitif et bien dimensionné | 04 + 02 |
| Quêtes/défis utiles et complets | 06 |
| Bestiaire augmenté | 07 |
| Boucle plaisante, rejouabilité, score/lore/quêtes | 03 + 02 + 05 + 06 + 08 |
| Score permanent, animations, sentiment de récompense | 02 |
| Déplacements en diagonale | 01 |
| Âme, DA, armes/personnages/lore mémorables | 08 et critères de contenu 05/06/07 |
| Classement mondial avec remise à zéro hebdomadaire | 09 |

## 8. Nouveaux retours intégrés

Dash commun validé ; casting d’au moins cinq à six personnages et refonte de tous les sprites avant 01 E : 06/08 ; mode dev tout débloqué : [guide](../DEV-MODE.md) ; mobilité expressive clavier/manette : 01/06 ; record uniquement au bilan et bilan majeur : 02/04 ; XP et menace initiale : 03 ; Hub peu textuel et Collection directe : 04 ; objets illimités et quêtes indépendantes du lore : 05/06 ; casting décalé cohérent : 06/08 ; nouveaux mobs et boss de famille : 07 ; terrain : 10 ; innovations : 11 ; fabrication du pixel art homogène : 08.


**1er octobre, continuation hors audio — R3b :** cadence du Hurleur ×2,5, contrôles et captures vérifiés ; [rapport du banc fixe et des runs](../audits/projectile-cadence-2026-10-01/README.md). R6 carte et R5 personnages suivent, conformément à DECISIONS §47.

**1er octobre, préchargement :** le boot Main charge désormais le shader actuel du fond de coffre ; référence supprimée retirée, smoke et ouverture réelle vérifiés (plan 25).

**1er octobre, R6 livré :** radar/carte par biome, routes/eau, danger et légende ;
17 contrôles, build/smoke et captures ViewSonic vérifiés ([plan 04](04-interfaces-et-hub.md#r6-livré--1er-octobre-2026)). R5 personnages suit hors audio (DECISIONS §47).

**1er octobre, R5a livré :** Traqueur repris, 152 PNG déterministes, planches et animations Main vérifiées sur ViewSonic. Vagabond puis Forgeuse suivent (plan 08).

**1er octobre, R5b livré :** Vagabond repris, 152 PNG déterministes, planches et Main vérifiées. Forgeuse suit (plan 08).

**1er octobre, R5c livré :** Forgeuse reprise et vérifiée. Les trois personnages sont produits (456 PNG déterministes), captures Main sur ViewSonic et build/smoke verts ; recette artistique humaine ouverte (plan 08).
