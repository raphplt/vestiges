# Plan 59 — La verticalité : des étages et un saut

15 novembre 2026 · [registre §224 et §225](DECISIONS.md). Statut : **bac à sable des deux voies livré (§8), à jouer par Raphaël**. Rien n'entre dans le jeu avant sa séance.

## 1. Le retour

Raphaël, le 15 novembre, après avoir joué la nouvelle version avec ses terres d'un seul biome :

- « comparé à avant où tous les biomes se mélangeaient, ça fait très vide. Il y a parfois des écrans entiers sans rien » ;
- « je ne pense pas que rajouter des props à l'infini règle le problème » ;
- « j'ai une solution, et elle est radicale : rajouter de la verticalité au jeu. Comme dans Megabonk, avoir des étages, et donc aussi pouvoir sauter. Je sais que ça implique énormément de choses dans le jeu mais je crois que c'est à essayer au moins. Et on va se concentrer sur ça. »

## 2. Lecture de l'agent

**Le diagnostic est juste.** Le §205 le notait déjà : les terres d'un seul biome montrent ce que le mélange cachait. Une capture de la carrière à 50 s (graine 221092026) le montre : un sol plat, les mêmes blocs posés partout, et rien qui oblige à choisir un chemin. Ajouter des décors remplit l'image, pas le jeu. Un écran paraît plein quand il pose une question au joueur : par où passer, où se réfugier, d'où sauter.

**Ce que la verticalité apporte vraiment dans Megabonk** :
1. **un paysage qui se lit** : des hauteurs vues de loin servent de repères ;
2. **la hauteur comme ressource** : on monte pour souffler, la horde s'entasse au pied des pentes, on saute dedans pour la faucher ;
3. **un geste à maîtriser** : sauter et enchaîner se prend en main en une seconde, et se maîtrise en des dizaines d'heures. C'est le principe du §191 ;
4. **l'exploration récompensée** : le coffre posé sur la butte, qu'il faut trouver comment atteindre.

**Ça rejoint deux piliers.** La maîtrise (le saut) et le vide : la falaise est un vide à hauteur d'homme. Pousser la horde du haut d'un gradin, c'est le geste du vide sans le Néant. Et le vide reste l'étage le plus bas.

**Ce qui existe déjà et sert** : le saut du vide ([plan 52 E3](52-elan.md)) a déjà une hauteur visible, une ombre, une retombée, et des règles de bord. La percussion pousse déjà la foule. Les emprises au sol des décors et `PropOcclusion` savent déjà trier et masquer.

## 3. L'avocat du diable

- **Megabonk est en 3D.** Sa verticalité vient de la caméra libre et de la physique. En 2D isométrique, on la simule : elle peut sembler un gadget, une falaise dessinée plutôt qu'un vrai relief.
- **La lisibilité au milieu de 120 créatures.** Qui est en haut, qui est en bas ? Si on ne le lit pas d'un coup d'œil, le relief devient de la confusion. C'est le premier risque, et le premier critère d'arrêt.
- **Le refuge tue la pression.** Si la hauteur protège trop, le joueur s'y installe et la horde attend en bas. Il faut que les créatures montent, plus lentement.
- **Tout ce qui est fait est plat.** Les pièces des bois, du Bas-Port et de la Haute-Ville (T1 à T3) n'ont pas de relief. Si l'essai prend, il faudra les reprendre. La carrière (T4), pas encore faite, est le terrain naturel pour commencer : gradins, fosses, rampes de chargement.
- **La forme a été verrouillée le 18 octobre.** On la rouvre quatre semaines après. C'est légitime si l'essai a un critère d'arrêt, et risqué s'il n'en a pas.
- **Une autre cause possible du vide** : des terres trop grandes pour ce qu'elles contiennent. Les rétrécir coûte bien moins cher. C'est le plan de repli si l'essai ne prend pas (§6).

## 4. Deux options

| | A. Relief en 2D isométrique (**recommandée**) | B. Passer en 3D |
|---|---|---|
| Principe | Des étages discrets (sol, gradin, butte), des falaises et des rampes. Chaque entité a un étage, et on dessine le décalage en hauteur. | Monde en 3D sous caméra isométrique, sprites en panneaux ou modèles simples, vraie physique de saut. |
| Ce qu'on garde | Le rendu, les 10 000 décors, la génération, les shaders, les bancs | Les données de jeu, les règles, l'audio |
| Ce qu'on refait | Le mouvement, le tri en Y, la génération du sol (une carte de hauteurs), les falaises | Le rendu, la génération, les décors, les collisions, les shaders, les captures |
| Effort | Élevé | Très élevé, une refonte |
| Risque | Que le faux relief ne donne pas le frisson de Megabonk | Des mois sans jeu jouable |

**Recommandation : A, d'abord en bac à sable.** Si l'essai prend mais que la 2D bride l'idée, la question de la 3D se posera avec un vrai argument. Pas avant.

## 5. Les règles proposées (option A), défauts réversibles

- **Étages** : une carte de hauteurs par cellule de terrain, de 0 à 2 (sol, gradin, butte). Le vide reste en dessous de tout. Un étage décale l'image d'environ 32 px vers le haut, à régler.
- **Le saut** : **le dash devient un bond**, sur la même touche (Espace). Il passe en l'air, il monte d'un étage au bord d'une falaise, et il franchit une cellule de trou. La Série continue d'allonger le bond au-dessus du vide (E3). On garde ainsi un seul geste à apprendre ; une touche à part reste possible d'un mot.
- **Descendre** : on marche hors du bord, ou on bondit. Descendre d'un étage ne coûte rien ; de deux, un peu de PV, pour que la butte ne soit pas qu'un raccourci.
- **Les rampes et escaliers** montent sans sauter.
- **Les créatures** : elles foncent vers le joueur comme aujourd'hui, sans recherche de chemin. Au pied d'une falaise, elles **grimpent lentement** (ralenties, en le montrant) et prennent une rampe quand elles sont dessus. Elles sautent en bas sans frein. Les volantes ignorent les étages.
- **Le contact ne touche qu'au même étage.** La hauteur protège du corps à corps, pas des tirs. Les projectiles du joueur et des créatures ignorent les étages, pour que le tir automatique reste simple.
- **La falaise comme vide** : une créature poussée du haut d'un gradin (percussion, onde du Mémorial) tombe, prend des dégâts de chute et compte à la Série comme une chute dans le vide, en moindre poids.
- **Lisibilité** : ombre portée au sol, étages bas plus sombres, liseré clair au bord des falaises, un son à chaque bond et à chaque retombée.

## 6. Critère d'arrêt de l'essai

Raphaël joue 15 min le bac à sable avec la horde :
1. l'écran paraît-il plein, sans un décor de plus ?
2. bondir, monter et sauter dans la horde donne-t-il envie de recommencer ?
3. lit-on qui est en haut et qui est en bas au milieu de 120 créatures ?

Deux « non » sur trois : on retire le relief, et le repli est **des terres plus petites et plus denses** (rayon de carte, distance des Mémoriaux), à mesurer au bot.

## 7. Découpage proposé

| Lot | Contenu | Vérification | Effort |
|---|---|---|---|
| **V0 Les règles du relief** | Carte de hauteurs (`HeightField`), étage de chaque entité, falaise qui bloque en montant, chute en descendant, rampe. Composant à part, `Player.cs` ne grossit pas. | Suite de mouvement : monter par une rampe, buter contre une falaise, descendre, chute de deux étages | Moyen |
| **V1 Le bac à sable en relief** | Arène faite main (gradins, butte, rampes, une fosse de vide), falaises dessinées, décalage et tri des entités, ombre ; le dash devient un bond | Captures du joueur sur chaque étage et en l'air ; banc de coût sur le tri | Élevé |
| **V2 La horde en relief** | Créatures qui grimpent et sautent en bas, contact au même étage, chute du haut d'un gradin, sons | Suite des capacités ennemies ; banc dense (le niveau par entité coûte une lecture de grille) | Moyen |
| **Séance de Raphaël** | Critère d'arrêt du §6 | — | Ton temps |
| **V3 et suite, si gardé** | Le relief entre dans la génération : la carrière d'abord (elle remplace T4), puis les autres terres ; minimap des hauteurs ; bot qui bondit | Mesure au bot, captures | Élevé |

**Ce qu'on met de côté pendant l'essai** : la Montée (étape 6) et la carrière T4 attendent. La séance de la Série et du vide (étape 4) peut se faire dans le même bac à sable.

## 8. Le bac à sable des deux voies — 15 novembre 2026

Raphaël, sur la question du §4 : « étudier les deux dans un sandbox, relief 2D et 3D ».

**À jouer** :

```bash
tools/relief_sandbox.sh
```

Options après `--` : `--land-order <biome>` (`forest_reclaimed` par défaut ; aussi `urban_ruins`, `wild_fields`, `collapsed_quarry`, `swamp`), `--tiles` (tuiles simples, sans biome), `--view 2d|3d|chase`, `--enemies N` (35 par défaut), `--seed S`. Réglages et arène dans `data/sandbox/relief.json` : on les change sans recompiler.

**Commandes** : déplacement et Espace comme en jeu (Espace fait le bond). Tab passe d'une vue à l'autre, sur la même partie, sans couper. Q/E tournent la caméra et la molette zoome (3D de haut). La souris tourne la caméra en poursuite, Échap la libère. R ramène au départ, +/- change la taille de la foule par 10.

**Trois vues d'une seule simulation.** Le bond, les étages, la horde et les tirs sont les mêmes, seul le rendu change. On compare donc le rendu et la caméra, pas deux jeux :

| Vue | Ce qu'elle montre |
|---|---|
| **Relief 2D** | Caméra isométrique orthographique sans lumière, réglée pour rendre les tuiles 64×32 du jeu pixel pour pixel (45,25 px par unité, inclinaison 30°). C'est l'image qu'aurait le jeu actuel avec des étages. |
| **3D de haut** | La même vue en perspective, avec soleil, ombres portées (les sprites en font aussi) et brume. Elle tourne et zoome. |
| **3D en poursuite** | Caméra basse derrière le joueur, tournée à la souris : la lecture de Megabonk. |

**Le décor est un vrai biome.** Au lancement, le bac à sable génère la vraie run sur la terre demandée, la photographie et la libère. Le sol est rendu tel que le jeu le dessine (tuiles et transitions, chemins, routes, décalques, ombres portées des décors, teinte d'ambiance), et devient le dessus des étages à l'endroit exact où le jeu le pose. Les falaises prennent sa teinte. Les décors et lieux debout (arbres, pierres, immeubles, mobilier) sont replacés à leur point au sol, bloquent selon leur emprise, et sautent les cellules en pente ou en bord de marche. Le relief, lui, reste celui de l'arène : les étages ne viennent pas du biome.

**L'arène** (44 × 44 cellules, bordée de vide) :
- un gradin en L dont le bord tombe droit dans le vide (le belvédère où pousser la horde) ;
- une butte sur le gradin ;
- trois escaliers ;
- une mesa sans escalier, qu'on n'atteint qu'en bondissant (le refuge) ;
- une arête d'une cellule, des piliers ;
- une fosse de vide.

**Règles posées** (§5) :
- le bond monte d'un étage et pas de deux ;
- une retombée d'au moins 0,6 étage sous le point de départ blesse et repousse la foule au même niveau ;
- descendre de plus de 1,2 étage coûte des PV ;
- les créatures grimpent les falaises à 0,55 unité par seconde, environ 1,3 s par étage ;
- elles longent le vide sans y entrer, et n'y tombent que poussées ;
- une chute ne les blesse que si une poussée l'a causée (tir, retombée) : un premier essai les blessait aussi quand elles descendaient d'elles-mêmes, et 32 éliminations sur 54 venaient de là ;
- le contact ne touche qu'à hauteur égale, les tirs ignorent les étages ;
- la horde compte 35 créatures : à 90, Raphaël l'a trouvée beaucoup trop grande pour juger le relief.

**Vérifié** : captures du bot (`--capture <dossier>`) dans les trois vues, en l'air, la caméra tournée et au bord du gradin. Le relief se lit dans les deux rendus. Sur la forêt et sur la ville en ruine, le sol photographié, les arbres et les immeubles se posent juste, sans rayure dans les feuillages (le tronc et le feuillage d'un arbre, confondus en 3D, sont écartés vers la caméra). La horde s'amasse au pied des falaises pendant que quelques créatures grimpent, et le refuge se voit. 144 images par seconde en relief 2D, 100 à 144 en 3D, machine chargée (charge 3), à ne pas prendre pour une mesure.

**Ce que l'essai a appris en le construisant** :
- **la vue relief 2D est rendue par le moteur 3D**, en orthographique et sans lumière. L'image est celle d'une 2D isométrique, mais le tri et l'occlusion (le sprite derrière une falaise, devant une autre) sont réglés par la profondeur, sans un seul cas à la main. En 2D pure (le rendu actuel du jeu), c'est le plus gros chantier du relief : le tri en Y de `Main` ne sait pas ce qu'est une falaise. S'il garde l'aspect 2D, le relief devrait donc passer par ce rendu 3D orthographique, ce qui ouvre une voie hybride : on garde le pixel art et les sprites, on change le moteur de rendu du monde. Son coût est de reprendre la pose des 10 000 décors (sprites en 3D, à regrouper) et le sol, pas le jeu ;
- **le sol est carré dans la simulation**, comme dans un monde en 3D. Une marche vers le haut de l'écran couvre donc deux fois moins de pixels qu'une marche de côté. Le jeu actuel bouge en pixels d'écran, et la différence se sentira peut-être. Elle tient en une ligne si elle gêne ;
- **en 3D, un décor reste une carte plate.** Ça passe pour un arbre ou une pierre, pas pour un immeuble : en poursuite, la ville en ruine montre des façades en carton. La vraie 3D demanderait des volumes pour les bâtiments, quand le relief 2D garde les sprites tels quels ;
- **les créatures n'ont aucune recherche de chemin** : elles foncent, grimpent et longent le vide. Ça suffit pour l'essai, à revoir si le relief est gardé.

**Ta séance**, avec les critères du §6 joués dans les trois vues :
1. l'écran paraît-il plein ?
2. bondir, monter et sauter dans la horde donne-t-il envie de recommencer ?
3. lit-on les étages dans la horde ?

Et une question de plus : **laquelle des trois vues veux-tu jouer ?** La suite en dépend (§4) : relief 2D par le rendu 3D orthographique, ou vraie 3D.

### Première séance de Raphaël — 15 novembre 2026

> « c'est dingue le jeu change totalement de dimension (que ce soit avec la 2D relief ou la 3D). c'est formidable. par contre j'arrive pas à me décider, les deux me plaisent énormément. »

Les critères 1 et 2 du §6 sont pris : le relief reste. Deux retours de jeu, traités le même jour :

- **On restait collé aux rebords**, en montant comme en descendant, et il fallait bondir pour s'en extraire. Cause : le joueur tombait dès que son centre quittait un rebord. Il retombait donc à cheval sur la paroi, et chaque pas qui gardait un bout du corps sur la marche haute était refusé. Correction : le joueur tient sur le plus haut sol sous son corps, et ne tombe qu'une fois sorti du rebord tout entier. Mesuré par `--check-edges` (sans rendu, sur chaque marche et chaque falaise de l'arène) : **105 rebords collants sur 107 avant, 0 sur 113 après**.
- **« des paliers plus hauts qu'on ne peut pas sauter »** : l'arène gagne au nord-ouest un **haut palier** à 2 étages au-dessus du sol, et sur lui une **tour** 2 étages plus haut (4 au-dessus du sol), chacun avec un escalier de 7 marches. Le bond monte 1,17 étage au plus : ni le palier ni la tour ne se prennent d'un bond. Le contrôle le vérifie (escaliers jusqu'à 4 étages, 0 étage gagné au bond seul), et le bot y passe à la fin de son tour. Les hauteurs s'écrivent au-delà de 9 en lettres (a = 10 quarts, g = 16).

**Le choix de la vue n'a pas à se faire maintenant** (§226). Les trois vues sont un même monde en 3D sous trois caméras. Bâtir le relief du jeu sur ce monde 3D, rendu d'abord en relief 2D, garde la poursuite ouverte : ce serait alors un changement de caméra et des volumes pour les bâtiments, pas une refonte.
