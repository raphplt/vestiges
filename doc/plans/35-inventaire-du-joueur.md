# Plan 35 — L'inventaire du joueur, repensé une dernière fois

11 octobre 2026. Demande de Raphaël au [§109](DECISIONS.md), partie du retour sur le late game ([plan 34](34-late-game.md)) : « qu'on repense encore une dernière fois à l'inventaire du joueur et à ce qu'il a », « de manière assez prioritaire ». **Étude et proposition : rien n'est implémenté.** La progression permanente (améliorations au Hub) est traitée à part, au [plan 36](36-progression-permanente.md).

## 0. En bref

- **Le problème.** En fin de run, tout plafonne : 4 armes au niveau 50 et 6 objets au niveau 30. Le level-up n'a alors plus rien à offrir (le pool est vide, le surplus du plan 21 §9 n'est pas codé). Le late game n'a donc plus de butin.
- **Ce que veut Raphaël**, trois étagères :
  1. **4 armes** ;
  2. **4 améliorations répétables**, une par stat, faciles à reconnaître (« tome de dégâts, tome de bouclier »), mais **pas des tomes**, pour ne pas copier Megabonk ;
  3. **des objets à effet unique**, nombreux (10, 20, 25), pas forcément infinis, qu'on ne cherche pas à empiler par dizaines.
- **Ce qu'il craint** : « avec 4 armes et 4 tomes, ça soit pareil que Megabonk ». Les objets illimités ne sont pas propres à Megabonk (Risk of Rain 2), donc ce point-là ne le gêne pas.
- **Proposition** :
  - les **17 objets « stat pure »** actuels deviennent la deuxième étagère, sous le nom provisoire d'**Habitudes** : ce que le corps retient quand la mémoire s'efface ;
  - les **16 objets à effet unique** restent des objets, **sans niveaux**, rangés dans un **sac** de 10 places, agrandi par la progression permanente ;
  - les objets viennent **du monde** (coffres, élites, Souverains, Ateliers) et le level-up sert aux armes et aux Habitudes ;
  - un sac plein se gère en **oubliant** un objet, comme le bannissement.
- **Ce qui nous distingue de Megabonk** :
  - les Habitudes peuvent aussi **s'entraîner** par la façon de jouer (option H2 : courir fait monter l'Habitude de courir) ;
  - les objets se **trouvent** dans le monde au lieu de s'offrir au level-up, ce qui récompense l'exploration voulue au §33 ;
  - le sac est **limité** : il faut choisir et oublier.

## 1. Aujourd'hui

| Étagère | Nombre | Montée | Sources | Fin de run |
|---|---|---|---|---|
| Armes (25 au catalogue, 8 en réserve de départ) | 4 (`Player.MaxWeaponSlots`, en dur) | Niveaux 1 à 50, puis deux ascensions | Arme de départ, carte de level-up, Wagonnet, échange au sol ; Atelier (niveau d'arme, Retrempe) | Au niveau 50, plus rien |
| Objets (35 dans `passive_souvenirs.json`, 33 actifs) | 6 (`Player.MaxPassiveSlots`, en dur) | Niveaux 1 à 30, palier au 15 | Cartes de level-up ; butin « niveau d'objet » des coffres | Au niveau 30, plus rien |
| Réminiscences (9, 7 branchées) | 4 | Choix après une Résurgence | Résurgence | — |
| Bénédictions (10) | Sans limite | Gain de stat au Mémorial | Mémorial | — |
| Bonus de coffre | Sans limite | Une stat au hasard | Coffres | — |

Les 33 objets actifs, triés :
- **Stat pure (17)** : Ressort de sommier (cadence), Rondelle de cuivre (zone), Mètre pliant (portée), Lunettes de lecture (critique), Bouton de manteau (PV max), Bobine de fil (régénération), Genouillère (armure), Écusson de pompier (bouclier), Lacet rouge (vitesse et dash), Aimant de frigo (attraction), Photo de classe (XP), Jeton de fête foraine (Chance), Papier carbone (projectiles), Paille tordue (vol de vie), Pince à linge (durée des statuts), Reflet brisé (perforation), Sifflet d'arbitre (+1 Péril par niveau).
- **Effet unique (16)** : Allumette humide (enflamme), Glaçon (ralentit), Thermomètre (dégâts aux brûlés), Épingle à nourrice (dégâts aux entravés), Pétard mouillé (explosion à l'élimination), Dé à coudre (soin à l'élimination), Semelle usée (bonus en marchant), Boîte de pansements (soin au niveau), Loupe de philatéliste (critique qui fragilise), Stylo à quatre couleurs (critique qui rebondit), Tabouret de camping (cadence immobile), Chewing-gum (traînée du dash), Gilet réfléchissant (dégâts selon la foule), Thermos (dégâts au-dessus d'un seuil de PV), Médaille cabossée (dégâts sous un seuil de PV), Porte-monnaie usé (dégâts selon l'Essence).

## 2. L'historique à revoir

Cette demande revient sur des décisions acquises ; le §109 les remplace explicitement.
- **§20–§21** (28 septembre) : « objets stackables à l'infini », puis le schéma 4 armes, 4 tomes et objets illimités jugé trop proche de Megabonk.
- **§30** : objets peut-être limités à 4, 8 ou 12 ; bannir, c'est oublier.
- **§33–§34** (30 septembre) : traits supprimés, **6 objets au plus**, montant en niveau ; « l'intérêt à long terme doit venir d'autre chose que l'accumulation d'objets ».
- **§68, §71, §73** : pas de tomes ; XP et Chance par des objets dédiés.

Ce qui change : les objets « stat » de §33 étaient des tomes déguisés. Les séparer franchement, en nom et en règle, rend le jeu plus lisible (c'est le souhait de Raphaël) et rouvre la place pour de vrais objets.

## 3. Proposition

### 3.1 Armes : 4, sans changement de fond

- Le nombre d'emplacements passe en données, pour que la progression permanente puisse y toucher (plan 36).
- **Surplus** (plan 21 §9, jamais codé) : quand toutes les armes sont au niveau 50 et ascensionnées, la carte d'arme devient un gain lisible. Par exemple, une petite hausse de dégâts de l'arme, sans plafond, ou une onde tous les 10 niveaux (§74). Il faut au moins que le level-up ne soit jamais vide.

### 3.2 Les Habitudes : 4 emplacements, une stat chacune

**Le concept.** « Le jardin a disparu. Le geste, non. » (Faucille). Le monde s'oublie, mais le corps garde ses gestes. Une Habitude est un geste que le personnage répète jusqu'à ce qu'il tienne seul : frapper plus fort, courir plus vite, encaisser.

**La règle.**
- 4 emplacements ; une Habitude = une stat, toujours la même.
- Le nom dit l'effet : Habitude de frapper (dégâts), de courir (vitesse), d'encaisser (armure)…
- Gain franc à chaque niveau, avec la rareté de l'amélioration.
- **Pas de plafond de niveau**, ou un plafond très haut avec rendement décroissant : c'est l'étagère qui absorbe les niveaux du late game.

**Le catalogue de départ.** Les 17 objets « stat pure » deviennent des Habitudes : mêmes stats, mêmes réglages, mêmes paliers. Leurs icônes d'objets (bouton de manteau, lacet rouge…) peuvent rester, comme « la chose qui rappelle le geste ». Sinon, il faut une famille d'icônes de gestes (plan 25).

**Options de différenciation** (à trancher) :

| Option | Règle | Pour | Contre |
|---|---|---|---|
| **H1** | Seulement au level-up, comme les autres cartes | Simple, lisible | Plus proche des tomes |
| **H2** (recommandé) | Au level-up, **et** une petite montée par l'usage : chaque Habitude équipée gagne de l'expérience quand le joueur fait le geste (courir, être touché, tuer au critique…) | Propre à Vestiges et au lore ; récompense un style de jeu | Plus de réglages ; à garder discret pour ne pas devenir une corvée |
| H3 | Seulement par l'usage, jamais au level-up | Très original | Lent, peu de contrôle, le level-up perd une catégorie |

**Le nom** (à trancher) : **Habitudes** (recommandé : clair, lié au lore du geste), Gestes, Ancrages, Cicatrices.

### 3.3 Les objets : un sac, des effets uniques

**La règle.**
- **Pas de niveaux.** Un objet fait une chose, et la fait tout de suite.
- **Un sac de 10 places au départ**, agrandi jusqu'à 20 ou 25 par la progression permanente (plan 36). Option : une poche en plus, rare, trouvée en run.
- **Doublons permis** : un deuxième exemplaire renforce l'effet, avec rendement décroissant, et prend une place. On peut empiler, mais ce n'est pas le but : le sac pousse à la variété.
- **Sac plein** : en ramassant, un écran propose d'**oublier** un objet du sac ou de laisser le nouveau au sol. L'oubli est gratuit, sans Péril : c'est un choix de build, pas un bannissement.
- **Sources : le monde.**
  - coffres, qui donnent un objet au lieu du « niveau d'objet » actuel ;
  - élites (rare) et Souverains (sûr) ;
  - Ateliers, qui en vendent pour de l'Essence ;
  - Marées et Échos-Souverains du late game.
  
  Le level-up n'offre plus d'objets : il sert aux armes et aux Habitudes. La décision §39/§40 (pas d'armes dans les coffres) tient toujours.
- **Rareté de l'objet** : commun, rare, épique, légendaire, au sens d'effet plus fort ou plus singulier. Elle remplace la rareté d'amélioration pour cette étagère.

**Le catalogue.**
- Les 16 objets à effet unique, réglés pour un seul niveau.
- De **nouveaux objets uniques**, sur les exemples de Raphaël : meilleures améliorations (rareté des cartes), court déplacement instantané, une résurrection par run, poison.
- Cible : **40 à 60 objets** à la sortie. Catalogue à écrire au plan 05, ou dans un plan dédié.

**Pistes d'objets à effet** (à affiner) :
- Ticket de consigne : une résurrection, l'objet se consume.
- Plan de métro : le dash devient un court saut instantané.
- Flacon de pharmacie : les coups empoisonnent.
- Trèfle plastifié : une carte de level-up de plus.
- Clé sans porte : les coffres s'ouvrent à distance.
- Montre arrêtée : à la mort d'un Souverain, le temps ralentit 3 s.

### 3.4 Réminiscences et bénédictions

Ces couches se recouvrent avec les deux nouvelles étagères (stats et effets) :
- les **bénédictions** de Mémorial sont des stats plates ; elles pourraient devenir un niveau d'Habitude offert ;
- les **Réminiscences** (4, après une Résurgence) changent le comportement des armes ; elles pourraient devenir des objets rares de Résurgence.

**Recommandation** : ne rien changer au premier passage, et revoir après le prototype (I1–I3) si l'écran de pause devient trop chargé.

## 4. Ce que ça règle pour le late game

- Les **Habitudes sans plafond** absorbent les niveaux : il y a toujours une carte utile.
- Le **sac** se remplit et se trie jusqu'à tard ; les objets du late game peuvent être les plus forts (légendaires des Marées).
- Le **surplus d'armes** empêche le level-up vide.

## 5. Ce que ça touche

- **Données** :
  - `passive_souvenirs.json` scindé : Habitudes d'un côté, objets de l'autre ;
  - `objects_contract.json` à refaire ;
  - tables de butin (« niveau d'objet » remplacé par « objet ») ;
  - emplacements en données.
- **Code** :
  - `Player.Objects` (sac, doublons, oubli) ;
  - `FragmentManager` (offre : armes, Habitudes, surplus) ;
  - `LootRewards` ;
  - Atelier ;
  - effets des objets réglés sans niveau.
- **Interface** :
  - barre du bas : 4 armes et 4 Habitudes ;
  - sac compact (grille ou compteur), lisible sans pause ;
  - écran d'échange quand le sac est plein ;
  - pause et Collection.
- **Méta** : 18 objets sont débloqués par des quêtes (`quests.json`). Ceux qui deviennent des Habitudes changent de type de déblocage ; il faut les relire.
- **Équilibrage** : remesurer la puissance au bot sur 30 min (plan 33). Les objets n'ont plus de niveaux, et les Habitudes n'ont pas de plafond.

## 6. Lots proposés

| Lot | Contenu |
|---|---|
| **I0** | Planche : barre du bas, sac, écran d'échange ; tableau des 17 Habitudes et de leurs déclencheurs d'usage (si H2) ; noms à trancher |
| **I1** | Habitudes : données, 4 emplacements, offre au level-up, sans plafond ; les 17 objets « stat » migrés |
| **I2** | Objets sans niveaux, sac de 10, doublons, oubli ; les 16 objets à effet réglés pour un niveau |
| **I3** | Sources : coffres, élites, Souverains, Atelier ; le level-up n'offre plus d'objets |
| **I4** | Surplus d'armes |
| **I5** | Interface : HUD, pause, Collection, échange |
| **I6** | Nouveaux objets uniques (catalogue v2), en lots de 8 à 10 |
| I7 | H2 (montée par l'usage), si retenue |
| I8 | Revue des Réminiscences et bénédictions |

## 7. Questions pour Raphaël

1. **Le nom** de la deuxième étagère : Habitudes, Gestes, Ancrages, Cicatrices, autre ?
2. **Montée** des Habitudes : H1 (level-up seul), H2 (level-up et usage), H3 (usage seul) ?
3. **Objets hors du level-up**, trouvés seulement dans le monde : d'accord ?
4. **Sac** : 10 places au départ, jusqu'à 20 ou 25 par la progression permanente ; doublons permis ; oubli libre quand il est plein ?
