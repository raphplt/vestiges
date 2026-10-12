# Plan 35 — L'inventaire du joueur, repensé une dernière fois

> **Amendement I15, 12 octobre 2026 (DECISIONS §144)** : les places comptent désormais les **types distincts**, pas les exemplaires. Capacité initiale en données (10 sans amélioration), augmentée par les Poches ; doublons empilés dans leur place existante, même à sac plein. Les descriptions historiques « un exemplaire, une place » ci-dessous sont remplacées par le [lot I15](35-raretes-des-objets.md#i15--places-par-type-dobjet-doublons-empilés-12-octobre-2026).

11 octobre 2026. Demande de Raphaël au [§109](DECISIONS.md), partie du retour sur le late game ([plan 34](34-late-game.md)) : « qu'on repense encore une dernière fois à l'inventaire du joueur et à ce qu'il a », « de manière assez prioritaire ». **Étude et proposition : rien n'est implémenté.** Questions tranchées le 11 octobre (§8) : la deuxième étagère, ce sont **les Écussons**. La progression permanente (améliorations au Hub) est traitée à part, au [plan 36](36-progression-permanente.md).

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

## 8. Décisions du 11 octobre ([§110](DECISIONS.md))

- **Nom** : les **Écussons**, cousus sur la veste, une famille d'objets concrets (Raphaël a refusé les concepts « Habitudes », « Gestes »… comme trop flous). Un Écusson par stat, au nom d'un métier ou d'une figure : Écusson du boxeur (dégâts), du coureur (vitesse), du pompier (bouclier, déjà dans le jeu), du veilleur (PV max)… La table des 17 noms est à proposer en I0.
- **Montée** : au level-up **et** par l'usage (H2). Ce qui « s'use » est l'Écusson : il gagne un peu à chaque fois que le joueur fait ce que l'Écusson représente.
- **Objets** : trouvés dans le monde seulement, sac de 10 agrandissable par le Campement (plan 36), doublons permis, oubli quand le sac est plein.
- **Ordre** : après le Péril visible (plan 34 G0) et le Campement (plan 36).

Dans tout ce plan, « Habitudes » se lit désormais « Écussons ».

## 9. Lot I0 — les Écussons, la barre du bas, le sac, la migration (11 octobre 2026)

**Document de conception, rien n'est codé.** I1 et la suite attendent la validation de Raphaël (§9.5).

### 9.1 Les 17 Écussons

Un Écusson est cousu sur la veste : une figure ou un métier, une stat, toujours la même. Il monte de deux façons (H2, §110) :
- **au level-up**, comme aujourd'hui : carte d'Écusson, gain selon la rareté ;
- **par l'usage** : chaque Écusson porté remplit un fil quand le joueur fait ce que la figure représente. Un fil plein donne un niveau.

Stats, réglages et paliers restent ceux de l'objet d'aujourd'hui. Seuls le nom, l'icône et la montée changent. Le nom de l'objet actuel peut rester comme sous-titre (« Écusson du batteur — le ressort de sommier »), pour ne pas perdre les quêtes ni la Collection.

Seuils réglés au bot en I7 : voir le §17 (ceux ci-dessous étaient 10 à 100 fois trop bas).

| Écusson | Objet d'aujourd'hui (`id`) | Stat | Monte par l'usage quand… | Fil (proposé, à mesurer) |
|---|---|---|---|---|
| **du batteur** | Ressort de sommier (`memoire_vive`) | Cadence | les armes frappent | 400 attaques |
| **du semeur** | Rondelle de cuivre (`resonance`) | Zone | un coup touche 3 créatures ou plus | 120 coups larges |
| **de l'arpenteur** | Mètre pliant (`portee_etendue`) | Portée | une créature meurt à plus de 250 px | 150 éliminations lointaines |
| **de l'horloger** | Lunettes de lecture (`oeil_critique`) | Critique (chance et dégâts) | un coup est critique | 200 critiques |
| **du veilleur** | Bouton de manteau (`ancrage`) | PV max | le joueur reste au contact (créatures à moins de 200 px) | 60 s au contact |
| **de la couturière** | Bobine de fil (`regeneration`) | Régénération | le joueur regagne des PV, toutes sources | 150 PV soignés |
| **du rugbyman** | Genouillère (`peau_dure`) | Armure | le joueur encaisse un coup | 40 coups reçus |
| **du pompier** | Écusson de pompier (`carapace`) | Bouclier | le bouclier absorbe des dégâts | 150 dégâts absorbés |
| **du coureur** | Lacet rouge (`instinct`) | Vitesse et recharge du dash | le joueur court ou se dashe | 300 m parcourus |
| **du glaneur** | Aimant de frigo (`siphon_essence`) | Attraction de l'XP | des orbes d'XP sont ramassés | 300 orbes |
| **du bon élève** | Photo de classe (`photo_de_classe`) | XP gagnée | le joueur gagne un niveau | 4 niveaux |
| **du forain** | Jeton de fête foraine (`jeton_de_fete`) | Chance | un coffre s'ouvre | 3 coffres |
| **du jongleur** | Papier carbone (`souffle_du_neant`) | Projectiles | un projectile touche | 500 touches |
| **du barbier** | Paille tordue (`paille_tordue`) | Vol de vie | le vol de vie rend des PV | 100 PV volés |
| **de l'apothicaire** | Pince à linge (`persistance`) | Durée des statuts | un statut est posé (brûlure, ralenti, figé, saignement…) | 150 statuts |
| **de l'archer** | Reflet brisé (`reflet_brise`) | Perforation | un projectile traverse une créature | 200 traversées |
| **de l'arbitre** | Sifflet d'arbitre (`sifflet_d_arbitre`) | +1 Péril par niveau | **jamais** (voir plus bas) | — |

Notes :
- **Le batteur, l'horloger, le coureur** portent deux réglages, comme leurs objets aujourd'hui (cadence seule ; chance et dégâts critiques ; vitesse et dash). Ce sont deux faces d'une même figure : on les garde ensemble.
- **L'arbitre ne doit pas monter par l'usage.** Le Péril se choisit (plan 28) : un Écusson qui l'augmenterait seul trahirait ce choix. Deux options :
  - A, recommandée : il redevient un **objet** du sac, « +1 Péril », avec doublons ;
  - B : il reste un Écusson, mais au level-up seulement.
- **Une bonne part des déclencheurs existe déjà** : `QuestTracker` suit 34 faits (`quests.json`), dont les éliminations critiques, les PV soignés, les dégâts encaissés, la distance, les coffres ouverts et les éliminations proches. La montée par l'usage peut écouter les mêmes événements, sans calcul par image. Les attaques, les touches de projectile, les traversées et les orbes ramassés sont à ajouter.
- **Réglage du fil** : l'usage doit donner environ **un quart** des niveaux d'un Écusson sur 30 min, le level-up le reste. Les seuils ci-dessus sont des ordres de grandeur, à mesurer au bot (I7) avec les compteurs de `RunObservation`.
- **Pas de plafond** (§3.2). Le palier du niveau 15 reste ; la question d'un palier tous les 15 niveaux est au §9.5.
- **Icônes** : un écusson brodé (forme commune : un blason de tissu à bord surpiqué), et dans le champ la silhouette de la figure : baguettes, faux, chaîne d'arpenteur, loupe d'horloger, lanterne, aiguille, ballon ovale, casque, chaussure, panier, cartable, ticket, quilles, rasoir, fiole, flèche, sifflet. Planche au plan 25, à faire après validation.

### 9.2 La barre du bas : 4 armes, 4 Écussons, le sac

Aujourd'hui : 4 cases d'armes (36 unités) au-dessus de 6 petites cases d'objets, centrées sous la barre d'XP. Proposition :

```
                       ┌────────── barre d'XP ──────────┐
  ┌────┬────┬────┬────┐   ╭───╮╭───╮╭───╮╭───╮     ┌─┬─┬─┬─┬─┐
  │ ⚔  │ ⚔  │ 🔒 │ 🔒 │   │ ⬢ ││ ⬢ ││   ││   │     │▪│▪│▪│ │ │  Sac 3/10
  │ 12 │  4 │    │    │   │ 7 ││ 3 ││   ││   │     ├─┼─┼─┼─┼─┤
  └────┴────┴────┴────┘   ╰─▔─╯╰─▔─╯╰───╯╰───╯     │ │ │ │ │ │
   4 armes (36 u.)         4 Écussons (30 u.)       └─┴─┴─┴─┴─┘
   cadenas = Campement     fil d'usage sous           10 cases de 12 u.,
                           chaque écusson             puis « +n » au-delà de 10
```

- **Armes** : inchangées, cadenas au-delà de la limite du Campement (plan 36 M2).
- **Écussons** : 4 cases en forme d'écusson de tissu, niveau en chiffre. Sous chacune, un **fil** fin qui se remplit avec l'usage. C'est le seul retour de H2 en run : pas de texte ni de bandeau. Un niveau gagné par l'usage fait briller l'écusson une fois.
- **Sac** : une grille de 2 × 5 mini-icônes à droite, sans niveau, et le compteur « 3/10 ». Au-delà de 10 places (Poches du Campement), la grille garde 10 cases et affiche « +n ». Le détail complet est dans la pause.
- Largeur totale : environ 4 × 39 + 4 × 33 + 70 = 360 unités, contre 160 aujourd'hui. À ×2, cela tient dans 1 280 px sans toucher la minimap.

### 9.3 L'écran d'échange quand le sac est plein

On ne fige pas le combat sans le vouloir. Le ramassage marche comme l'échange d'armes d'aujourd'hui (`WeaponPickup`) :
1. On passe sur l'objet. Le sac a de la place : il entre, son icône vole vers le sac (comme les armes).
2. Le sac est plein : l'objet reste au sol avec l'invite « Sac plein — maintenir [Interagir] pour échanger ». Une jauge se remplit pendant le maintien, comme pour l'arme.
3. L'invite tenue ouvre l'écran d'échange, et la run se met en pause :

```
  ┌──────────────────────── Sac plein ─────────────────────────┐
  │                                                             │
  │   NOUVEL OBJET              TON SAC (10/10)                 │
  │  ┌──────────────┐         ┌───┬───┬───┬───┬───┐             │
  │  │  [icône]     │         │ ▪ │ ▪ │ ▪ │ ▪ │ ▪ │             │
  │  │ Ticket de    │         ├───┼───┼───┼───┼───┤             │
  │  │ consigne     │         │ ▪ │ ▪ │ ▪ │[▪]│ ▪ │  ← focus    │
  │  │ rare         │         └───┴───┴───┴───┴───┘             │
  │  │ Une résur-   │         Allumette humide (×2)             │
  │  │ rection.     │         Les coups enflamment : 6 %.       │
  │  └──────────────┘                                           │
  │                                                             │
  │   [Valider] Oublier l'objet choisi   [Retour] Laisser au sol │
  └─────────────────────────────────────────────────────────────┘
```

- À gauche, le nouvel objet : nom, rareté, effet. À droite, le sac : le focus parcourt les 10 cases, le détail de la case suit.
- **Valider** oublie l'objet choisi : il s'efface en poussière, comme l'oubli du bannissement. Le nouvel objet prend sa place.
- **Retour** laisse le nouvel objet au sol. Il y reste jusqu'à ce que sa zone s'oublie (plan 22 §6).
- Un doublon est affiché « ×2 » ; oublier en retire un seul exemplaire.
- Manette : le stick parcourt la grille, A oublie, B ferme. Aucune action ne coûte de Péril (§110).

### 9.4 Migration des données et des quêtes

**Données.**
- `passive_souvenirs.json` est scindé en deux fichiers, avec un contrat chacun (`objects_contract.json` scindé aussi) :
  - `badges.json` : les 17 Écussons (ou 16 avec l'option A pour l'arbitre) ;
  - `items.json` : les objets.
- **Les identifiants ne changent pas** (`memoire_vive`, `carapace`…). Quêtes, sauvegardes, historique et Collection y font référence. On change le nom affiché, pas l'`id`.
- Les deux objets désactivés (`flamme_interieure`, `fragment_deternite`) restent désactivés, côté objets.
- Les 16 objets à effet perdent leurs niveaux : chacun est réglé à une valeur unique, d'abord celle du niveau 10 d'aujourd'hui, à remesurer. Leur palier du niveau 15 devient soit l'effet de base, soit l'effet d'un doublon, au cas par cas. Ce tableau est à faire en I2.
- Butin : le « niveau d'objet » des coffres (`loot_tables`) devient « objet » (I3).

**Quêtes** (`quests.json`, 18 objets débloqués).

| Quête | Débloque | Devient |
|---|---|---|
| Lire de près (`read_closely`) | Lunettes de lecture | Écusson de l'horloger |
| Encaisser (`take_hits`) | Genouillère | Écusson du rugbyman |
| Tenir le feu (`hold_the_fire`) | Écusson de pompier | Écusson du pompier |
| Recopier (`copy_out`) | Papier carbone | Écusson du jongleur |
| Reprendre des forces (`regain_strength`) | Paille tordue | Écusson du barbier |
| Clouer (`nail_down`) | Reflet brisé | Écusson de l'archer |
| Appeler la meute (`call_the_pack`) | Sifflet d'arbitre | objet (option A) ou Écusson de l'arbitre |
| Fièvre, Retenir, Bouquet final, Grandir, Le détail, Annoter, Pas bouger, Dans la foule, Bien au chaud, De justesse, Économiser | Thermomètre, Épingle, Pétard, Pansements, Loupe, Stylo, Tabouret, Gilet, Thermos, Médaille, Porte-monnaie | objets (sans changement de quête) |

- Un nouveau type de déblocage `badge` s'ajoute à `weapon`, `object` et `character` (`UnlockKind`, `QuestDataLoader`, `MetaSaveManager.IsUnlocked`). Six ou sept quêtes passent de `object` à `badge`. Rien ne change dans les sauvegardes : elles ne gardent que les quêtes accomplies, et les déblocages s'en déduisent.
- **Réserve de départ** (profil neuf, plan 06 §9) : 10 Écussons (batteur, semeur, arpenteur, veilleur, couturière, coureur, glaneur, bon élève, forain, apothicaire) et 5 objets (Allumette humide, Glaçon, Dé à coudre, Semelle usée, Chewing-gum). Il faudra plus d'objets de départ : 5, c'est peu pour un sac de 10. On pourrait en ajouter 3 à 5 parmi les nouveaux objets (I6).
- **Textes des quêtes** : les récompenses affichées (« débloque l'objet… ») suivent le type. La quête « Quatre airs » dépend maintenant du Campement (plan 36 §11).
- **Tests** : `QuestRegression` (comptes 5 / 17 / 18 et réserve de départ), `ObjectsRegression` (objets à niveaux), `catalogs`, `perk_effects`. Ils sont à réécrire avec I1 et I2.
- **Historique et bilan** : les runs passées gardent les objets sous leur `id`. Le bilan et l'historique liront le bon catalogue par l'`id`, sans migration.

### 9.5 Questions pour Raphaël

1. **Noms des Écussons** : la table du §9.1 te va-t-elle ? Les noms à revoir en priorité : du veilleur (PV max), du rugbyman (armure), du barbier (vol de vie), du bon élève (XP).
2. **L'arbitre** : redevient-il un objet du sac (A, recommandé : le Péril reste un choix), ou reste-t-il un Écusson qui ne monte qu'au level-up (B) ?
3. **Paliers sans plafond** : garder le seul palier du niveau 15, ou un palier tous les 15 niveaux (15, 30, 45…) ? Le second demande d'inventer deux effets de plus par Écusson.
4. **Part de l'usage** : environ un quart des niveaux d'un Écusson en 30 min, le reste au level-up. Plus, moins ?
5. **Le fil sous chaque Écusson au HUD** : seul retour de l'usage en run (pas de bandeau). D'accord ?
6. **Sac plein** : invite maintenue puis écran en pause (§9.3), comme l'échange d'armes ? Ou un écran qui s'ouvre tout seul au contact ?
7. **Objets de départ** : seulement 5 dans la réserve d'un profil neuf. En ajouter 3 à 5 parmi les nouveaux objets (I6), ou débloquer plus tôt ceux des quêtes ★ ?
8. **Les objets à effet sans niveaux** : partir de la valeur du niveau 10 d'aujourd'hui, et faire du palier 15 l'effet d'un doublon. Ce principe te va-t-il, avant le réglage objet par objet en I2 ?

## 10. Lots de la refonte — 11 octobre 2026 (§116)

Raphaël a accepté les propositions du §9.5 ([DECISIONS §116](DECISIONS.md)). L'arbitre redevient un objet, et l'Écusson garde un seul palier, au niveau 15.

**Choix techniques, différents de l'I0 sur un point.** `passive_souvenirs.json` n'est pas scindé : chaque fiche gagne un champ `kind` (`badge` ou `item`). Ainsi un seul chargeur et un seul contrat restent, les `id` ne bougent pas et l'historique se lit sans migration. Les réglages de l'inventaire vont dans un petit fichier à part, `data/progression/inventory.json` : 4 Écussons, sac de 10, montée des Écussons, valeur des exemplaires d'un objet.

| Lot | Contenu | Vérification |
|---|---|---|
| **I1** | **Écussons** : `kind` sur les 35 fiches (16 Écussons, 19 objets dont l'arbitre), noms « Écusson du… » avec l'ancien nom en sous-titre ; 4 emplacements à part ; offre au level-up (nouveau, niveau suivant) ; niveau maximum 99, demi-pas au-delà du niveau 30 ; type de déblocage `badge` dans les quêtes ; rangée de 4 Écussons au HUD, section à la pause | suites `objects`, `quests`, `catalogs`, `perk_effects`, nouvelle suite `inventory` ; capture du HUD |
| **I2** | **Objets en exemplaires** : plus de niveaux ; un exemplaire vaut le niveau 10 d'aujourd'hui, le deuxième atteint le palier (niveau 15), puis 19, 22, 24, 25 ; sac de 10 places (un exemplaire, une place) ; oubli d'un exemplaire, effet et palier retirés proprement. Le level-up n'offre plus d'objets dès I2 ; les coffres en donnent (choix fait en codant : pas de règle provisoire à défaire en I3) | suites `objects`, `inventory` |
| **I3** | **Le monde donne les objets** : le level-up n'en offre plus ; coffres (« objet » au lieu de « niveau d'objet »), élites (au hasard), Souverains (sûr) et Atelier (contre Essence) ; objet posé au sol, ramassé s'il y a de la place, sinon invite à maintenir et écran d'échange en pause | suites `objects`, `small_places`, `inventory` ; captures de l'échange |
| **I4** | **Surplus d'armes** : une arme au niveau 50 et ascensionnée reste offerte au level-up, avec +4 % de dégâts par carte, sans plafond | suites `weapons`, `inventory` |
| **I5** | **Interface** : pause (armes, Écussons, sac), fiche du joueur, Collection (Écussons et objets), bilan | captures Hub et run |
| **I6** | **Nouveaux objets, première fournée** : Ticket de consigne (une résurrection), Plan de métro (le dash devient un saut court), Flacon de pharmacie (poison), Trèfle plastifié (une carte de plus par level-up), Montre arrêtée (le temps ralentit à la mort d'un Souverain) ; trois d'entre eux rejoignent la réserve de départ | suites `objects`, `quests` ; captures |
| **I7** | **Montée par l'usage (H2)** : un fil par Écusson porté, déclencheurs du §9.1, seuils en données ; fil au HUD ; mesure de la part de l'usage sur 30 min | suite `inventory`, mesure au bot |
| M4 (plan 36) | **Poches** au Campement : sac +3 places par rang, 5 rangs (10 → 25) | suite `camp` |

I8 (revue des Réminiscences et des bénédictions) attend une partie jouée.

## 11. Lot I1 livré : les Écussons — 11 octobre 2026

**Données.**
- Les 35 fiches de `passive_souvenirs.json` portent maintenant `kind`. On compte 16 Écussons (« Écusson du batteur »…, l'ancien nom passé en `subtitle`) et 19 objets, dont l'arbitre et les deux fiches désactivées.
- Le contrôle au chargement refuse une étagère absente ou inconnue, et un sous-titre vide.
- `inventory.json` donne 4 emplacements d'Écussons et le rendement : au-delà du niveau 30, un niveau d'Écusson ne vaut plus qu'un demi-pas. Les Écussons montent jusqu'au niveau 99 ; les objets restent plafonnés à 30.
- Valeurs : l'Écusson du batteur vaut +240 % de cadence au niveau 30 (inchangé), puis +4 % par niveau, jusqu'à +516 % au niveau 99.

**Jeu.**
- Le joueur a deux étagères : 4 Écussons, et les objets (encore 6 emplacements à niveaux, en attendant I2).
- Le level-up offre un Écusson neuf tant qu'il reste une place, et ses niveaux suivants sans plafond pratique. L'objet suit sa propre règle, à part.
- Les cartes affichent « ÉCUSSON ». L'aperçu d'une carte suit le même rendement que la valeur obtenue.
- Le coffre (« niveau d'objet »), la Faille et la carte de survie voient les deux étagères.

**Quêtes.**
- Nouveau type de déblocage `badge`. Six quêtes débloquent maintenant un Écusson : Lire de près, Encaisser, Tenir le feu, Recopier, Reprendre des forces, Clouer.
- Un Écusson déclaré `object`, ou l'inverse, est refusé au chargement.
- La page Quêtes affiche « Écusson ». Les sauvegardes ne changent pas : les déblocages se déduisent des quêtes accomplies.

**Interface.**
- La rangée sous les armes sort de `HUD.cs` dans `HudInventoryBar` : 4 cases d'Écussons à cadre doré, un écart, puis les objets.
- La pause et la fiche du joueur ont une section Écussons, au niveau sans plafond, avec le sous-titre.
- Le bilan montre les Écussons puis les objets.

**Vérifications.**
- Build sans avertissement.
- 17 suites vertes : `smoke`, `inventory` (nouvelle, 14 contrôles), `objects`, `quests`, `catalogs`, `perk_effects`, `perk_acquisition`, `perk_contracts`, `choice_screen`, `run_trace`, `dev_mode`, `saves`, `small_places`, `weapons`, `ui_art`, `launchers`, `camp`.
- Tests mis à jour :
  - `objects` : 4 Écussons et 6 objets, rendement au-delà du niveau 30, offre par étagère ;
  - `catalogs` : refus de `kind` et de `subtitle` ;
  - `quests` : 12 objets et 6 Écussons débloqués, refus d'un mauvais type.
- Capture en run regardée : la rangée Écussons et objets sous les armes.

**Reste pour la suite :**
- Les icônes des Écussons sont celles des anciens objets. La broderie est à faire au plan 25.
- La Collection range encore Écussons et objets ensemble (I5).
- **À mesurer en fin de run longue.** Au niveau 99, certains Écussons deviennent extrêmes, même avec le demi-pas : critique au-delà de 100 % (horloger), +32 projectiles (jongleur), +32 perforations (archer). Il faut y arriver d'abord (99 niveaux sur un seul Écusson). Les bornes éventuelles sont à décider sur la mesure du late game (plan 34), pas avant.
- Relecture `godot-reviewer`. Corrigés :
  - sous-titre exigé pour un Écusson et refusé pour un objet ;
  - au bilan, les cases d'Écussons restent réservées, avec un écart avant les objets ;
  - textes de la pause traduits ;
  - commentaire périmé ;
  - comparaison ordinale dans un test ;
  - `test_inventory.sh` ajouté à AGENTS.md.

## 12. Lot I2 livré : objets en exemplaires, sac de 10, oubli — 11 octobre 2026

**La règle.**
- Un objet n'a plus de niveaux : il a des **exemplaires**, et chacun prend une place du sac (10 places).
- Le nombre d'exemplaires fixe le niveau d'effet de l'objet (`inventory.json`, `item_copy_levels`) :

| Exemplaires | 1 | 2 | 3 | 4 | 5 | 6 |
|---|---:|---:|---:|---:|---:|---:|
| Niveau d'effet | 10 | 15, palier | 19 | 22 | 24 | 25 |

  Le premier exemplaire vaut le niveau 10 d'aujourd'hui (§116). Le doublon atteint le palier, et le rendement baisse ensuite.
- Le Sifflet d'arbitre a ses propres exemplaires (`copy_levels` de 1 à 10) : +1 Péril par exemplaire, jusqu'à 10.
- Le contrôle au chargement refuse `copy_levels` sur un Écusson, au-delà du niveau maximal de l'objet, ou s'ils ne montent pas.

**L'oubli** (`Player.ForgetItemCopy`).
- Retirer un exemplaire ramène l'objet au niveau des exemplaires restants. Retirer le dernier le fait quitter le sac.
- L'écart d'effet est retiré (multiplicatif comme additif), ainsi que les paliers que l'objet n'atteint plus. Les objets de déclencheur et d'état ont pour cela un `Deactivate`.
- Le Péril d'un Sifflet oublié ne se rend pas.

**Les sources, pour l'instant.**
- Le level-up n'offre plus d'objets : seulement les armes et les Écussons.
- Les coffres ont un butin « objet » : un objet débloqué au hasard que le sac peut recevoir, sinon l'Essence de secours. Poids : commun 14, rare 22, épique 30, à côté du reste de leur table.
- Le « niveau d'objet » des coffres et les niveaux de la Faille ne visent plus que les Écussons.
- Les élites, les Souverains, l'Atelier et l'objet posé au sol (avec l'écran d'échange) viennent en I3.

**Interface.**
- La rangée du HUD montre le sac : une petite case par place, un exemplaire par case, les doublons côte à côte. La rangée se recentre si le sac grandit (Poches).
- La pause et la fiche du joueur affichent « Sac n/10 » et « ×2 ».
- Le bilan montre les objets portés avec leurs exemplaires.

**Vérifications.**
- Build sans avertissement.
- Suites vertes : `inventory` (exemplaires, palier au doublon, oubli qui rend effet et palier, objets d'état, sac plein, Poches, coffre), `objects`, `catalogs`, `quests`, `smoke`, `perk_*`, `choice_screen`, `run_trace`, `dev_mode`, `saves`, `small_places`, `weapons`, `ui_art`, `launchers`, `camp`, `field_bonuses`, `run_phase`.
- Capture regardée : le sac de 10 sous les armes.
- En 200 s de run, le bot n'a reçu aucun objet : les coffres seuls en donnent peu. I3 ajoute les autres sources, puis on mesure.
- **Entre I2 et I3**, le jeu n'a pas encore d'écran pour oublier : un sac plein refuse les objets, et le coffre donne alors son Essence de secours. L'écran d'échange arrive avec I3.
- **Le Sifflet d'arbitre** : le Péril pris ne se rend pas à l'oubli. Reprendre un Sifflet oublié en redonne, puisque chaque exemplaire pris monte le Péril. C'est voulu : le Péril reste un choix, jamais une dette.
- Relecture `godot-reviewer`. Corrigés :
  - un effet entièrement oublié gardait un reste d'arrondi (1e-8), qui suffisait à faire tourner l'horloge de la Semelle ou à poser des taches de Chewing-gum. Les valeurs sont maintenant ramenées à zéro, et un test le vérifie sur trois objets ;
  - un objet de coffre qui ne trouve plus de place à l'ouverture devient son Essence de secours ;
  - les paliers d'un objet s'affichent en exemplaires (« 2 exemplaires : … ») ;
  - le bilan n'est plus borné à 10 fiches ;
  - le sac ne se réduit jamais sous ce qu'il contient ;
  - clé de traduction devenue inutile retirée.

## 13. Lot I3 livré : le monde donne les objets, l'écran d'échange — 11 octobre 2026

**Sources** (réglages dans `inventory.json` et `landmarks.json`) :
- **Coffres** : butin « objet » (depuis I2).
- **Élites** : une chance sur quatre de lâcher un objet au sol (`elite_item_chance`).
- **Souverains** : un objet, toujours (`sovereign_items`).
- **Atelier** : chaque Atelier met en vente un objet débloqué, tiré à la première visite, pour 120 Essence (`item_cost`, avec la même hausse que ses autres services). Sac plein : l'achat passe par l'écran d'échange.
- Les objets tirés sont ceux que le profil a débloqués (réserve de départ, puis quêtes).

**Au sol** (`ItemPickup`).
- L'objet flotte sur un anneau cyan. Il entre seul dans le sac s'il reste une place.
- Sinon, l'invite « Sac plein — échanger contre X » se maintient (0,35 s) et ouvre l'**écran d'échange**, en pause.
- Laissé au sol, l'objet s'efface après 2 minutes, comme une arme.

**Écran d'échange** (`ItemExchange`, sur l'écran de choix commun).
- Le titre donne le sac (« Sac plein (10/10) »), et le sous-titre le nouvel objet et sa règle.
- Une carte par objet du sac, avec ses exemplaires : « Glaçon dans un mouchoir ×6 ». Choisir une carte oublie un exemplaire et prend le nouvel objet.
- « Laisser au sol » ne change rien.
- L'oubli ne coûte rien (§110).

**Mesure** (Vagabond en profil neuf, bot mortel qui ratisse les lieux et ramasse les objets au sol, 4 graines, 30 min) :

| Graine | Objets reçus | Sac à 30 min | Élites abattues | Souverains abattus | Coffres ouverts | Ateliers |
|---|---:|---:|---:|---:|---:|---:|
| 42 | 10 | 10/10 | 16 | 4 | 34 | 4 |
| 1002 | 10 | 10/10 | 11 | 0 | 17 | 2 |
| 7 | 6 | 6/10 | 9 | 1 | 28 | 2 |
| 123 | 10 | 10/10 | 16 | 1 | 36 | 5 |

- Le sac se remplit en une run de 30 min dans trois graines sur quatre. L'échange devient donc un choix de fin de run, comme voulu.
- Le bot n'achète pas à l'Atelier et rate souvent les Souverains (la chasse échoue s'il ne les poursuit pas) : un joueur trouvera un peu plus d'objets.
- Avant les élites et les Souverains (coffres seuls, première mesure) : 2 à 4 objets en 30 min.

**Vérifications.**
- Build sans avertissement.
- Suite `inventory` : objet lâché par un Souverain, rien par une créature ordinaire, ramassage, échange qui s'ouvre en pause, « Laisser au sol », oubli et prise, reprise de la run.
- `catalogs` vert (`item_cost`), `objects`, `small_places`, `quests`, `smoke` verts.
- Captures sous `xvfb-run` (`CAPTURE_EXTRA_ARGS="--capture-exchange"`, nouveau mode) regardées : l'objet au sol et son invite, puis l'écran d'échange avec les exemplaires.
- Relecture `godot-reviewer`. Un vrai défaut corrigé : un objet au sol **déjà à son maximum d'exemplaires** ouvrait l'échange, faisait oublier un autre objet et ne rentrait pas. Désormais :
  - les objets lâchés et vendus sont tirés parmi ceux que le joueur peut encore cumuler ;
  - l'échange ne s'ouvre pas pour un objet au maximum (il rougit au sol), et l'objet entrant n'est jamais proposé à l'oubli ;
  - si la prise échoue malgré tout, l'exemplaire oublié revient ;
  - la carte de l'Atelier est éteinte pour un objet au maximum, et l'Atelier se rouvre si l'on renonce à l'échange.
- Corrigés aussi :
  - invite composée une fois ;
  - tirages sur des flux à part (`item_drops`, `workshop_items`), pour ne décaler ni les coffres ni les Trempes ;
  - Atelier perdu retiré des offres ;
  - icônes chargées sans erreur si elles manquent.
- Pas encore : le ramassage n'a ni son ni envol d'icône vers le HUD (I5).

## 14. Lot I4 livré : surplus d'armes — 11 octobre 2026

- Une arme au niveau 50, qui a pris sa voie d'ascension (ou qui n'en a pas), n'a plus rien à gagner. Le level-up lui offre alors une carte **SURPLUS** à la place de son amélioration : +4 % de dégâts de l'arme par carte, additionnés, sans plafond (`weapon_surplus_damage` dans `level_up_offer.json`, contrôlé au chargement).
- Le gain s'applique après tout le reste, ascension comprise.
- Tant que l'arme peut encore s'ascensionner, c'est la carte d'ascension qui vient : le surplus ne la remplace pas.
- La carte dit « Dégâts de l'arme +8 % → +12 % ».
- Avec les Écussons sans plafond (I1) et le surplus, le level-up n'est plus jamais vide en fin de run.
- Vérifications : contrôle dans la suite `inventory` (pas de surplus avant l'ascension, deux cartes = ×1,08 sur les dégâts, plus d'amélioration offerte) ; refus d'un gain nul au chargement (`catalogs`) ; `weapons` vert.

## 15. Lot I5 livré : interface — 11 octobre 2026

Pause, fiche du joueur, barre du bas et bilan suivaient déjà l'inventaire depuis I1 et I2 (Écussons, sac, exemplaires). I5 complète le reste :
- **Collection** : onglet **Écussons** à part, entre Armes et Objets. Sous chaque fiche, la règle tirée des données :
  - Écusson : l'objet d'où il vient, montée au level-up jusqu'au niveau 99, demi-pas au-delà du seuil (`inventory.json`) ;
  - objet : exemplaires au plus et exemplaire du palier, ou exemplaires seuls s'il n'a pas de palier.
- **Ramassage d'un objet** : son de trouvaille, et l'icône s'envole du sol vers sa case du sac (nouveau signal `ItemPickedUp`, même envol que les armes).
- **Pause** : « Surplus +N % » à côté du niveau d'une arme qui en a reçu (I4).
- **Bilan** : la rangée s'appelle « Écussons et objets ».
- Outillage : `--collection-tab` de `HubCapture` rouvrait la Collection par une méthode disparue ; il passe désormais par `OpenPage`.

**Vérifications.**
- Build sans avertissement ; `smoke`, `ui_art`, `inventory`, `choice_screen`, `objects`, `quests` verts.
- Captures regardées sous `xvfb-run` :
  - pause (`--capture-pause`, refaite pour 4 Écussons, un sac à doublons et un surplus) : arme « Surplus +8 % », section Écussons, barre du bas avec 7 exemplaires sur 10 ;
  - Collection, onglets Écussons et Objets (`HUB_EXTRA_ARGS="--collection-tab badges"`).
- Non vérifié : l'envol de l'icône (trop bref pour la capture), à juger en jeu (A-VERIFIER).

## 16. Lot I6 livré : nouveaux objets, première fournée — 11 octobre 2026

Cinq objets à effet unique, chacun avec sa règle et son icône (modèles SDF dans `tools/sprites/items/gadgets.py`, `tools/generate_item_icons.py --lot i6`).

| Objet | Effet | Exemplaires | Accès |
|---|---|---|---|
| **Ticket de consigne** | Un coup mortel ne tue pas : relevé à 50 % des PV, 2 s d'invulnérabilité ; l'exemplaire se consume. Sans effet contre le Néant. | 2 au plus (deux résurrections) | quête « Sursis » : 300 éliminations sous 25 % des PV dans une run |
| **Plan de métro** | Le dash devient un saut instantané : toute sa longueur en une image, contre les murs et le Néant comme un dash, silhouettes laissées sur le trajet ; le Chewing-gum pose ses taches sur tout le trajet. | 2 : saut ×1, puis ×1,5 | réserve de départ |
| **Flacon de pharmacie** | Chaque coup d'arme ajoute au poison de la cible 20 % de ses dégâts de base (30 % au doublon…), rendus sur 3 s ; le poison s'accumule sans plafond, chaque coup relance la durée, il attend quand la créature est terrée. Chiffres vert-de-gris. | 6, comme les autres objets | réserve de départ |
| **Trèfle plastifié** | Une carte de plus à chaque level-up (4 au lieu de 3). | 1 | quête « Porte-bonheur » : 25 coffres ouverts dans une run |
| **Montre arrêtée** | À la mort d'un Souverain tué par le joueur, les créatures à 700 px tournent à 25 % de leur vitesse pendant 2 s (3 s au doublon…). | 6 | réserve de départ |

**Choix faits en codant, à confirmer.**
- Le Ticket ne joue pas contre l'Effacement : relevé dans le Néant, le joueur y mourrait aussitôt et perdrait le ticket pour rien. « Le Néant, lui, ne rend rien » est écrit sur la fiche.
- L'invulnérabilité du saut est celle du dash : nulle par défaut (`mobility.json`). Le saut évite les coups parce qu'il déplace d'un coup, pas parce qu'il protège.
- Le poison est un nouveau statut (`StatusKind.Poison`, `Enemy.Poison.cs`) sans marque sur le sprite : seuls ses chiffres verts le montrent. Une marque (bulles) viendra avec le plan 27 si le retour en jeu le demande.
- Montre arrêtée : Souverains seulement, comme prévu. Le bot en abat 0 à 4 en 30 min : l'objet risque d'être faible. À juger en jeu ; piste : l'élargir aux élites, à durée moindre.
- Réserve de départ : Plan de métro, Flacon et Montre (18 objets et Écussons ouverts dans un profil neuf, contre 15). Ticket et Trèfle, les plus forts, passent par une quête.
- Les cinq fiches n'ont pas de palier : la Collection dit seulement « Jusqu'à N exemplaires ».

**Vérifications.**
- Build sans avertissement.
- Suite `inventory` : deux tickets puis deux relevés, le Néant qui ne prend pas le ticket ; saut de 72 px en une image puis fin du dash, second exemplaire ×1,5, oubli qui rend le dash ; poison qui s'accumule et se vide sur la durée (20 en 1 s, 60 au total) ; Flacon (coup de 100 → 20 de poison) ; Montre (créature proche ralentie) ; Trèfle (3 → 4 cartes).
- Comptes mis à jour dans `objects` (38 fiches proposées) et `quests` (38 quêtes, 18 pièces de départ).
- Capture du level-up à quatre cartes (`--capture-levelup`, nouvelle vue `levelup-clover`) regardée : les quatre cartes tiennent en 1080p.
- Relecture `godot-reviewer`, corrigés :
  - le saut finit dans son image (rien ne le voit « durer ») ; un compte des dashs (`DashesStarted`) le montre aux lecteurs en `_Process` (Rémanence) ; le Chewing-gum pose ses taches du départ à l'arrivée ;
  - le poison se calcule sur le coup de base de l'arme, comme la Brûlure : un cône, qui frappe par fractions d'image, n'empoisonnait presque rien ;
  - vitesse remise à zéro après le saut (les tirs visés lisent la vitesse du joueur) ;
  - la longueur du saut survit à un changement de module de mobilité ;
  - le poison attend qu'une créature terrée ressorte au lieu de s'évaporer.
- Le résultat de dégâts publié reste « fatal » quand le Ticket relève le joueur, comme le prévoit `PlayerDamageResult` : aucune récupération de PV ne s'ajoute au relevé.

## 17. Lot I7 livré : les Écussons montent aussi par l'usage — 11 octobre 2026

Chaque Écusson porté a un **fil** : il se remplit quand le joueur fait ce que sa figure représente, et un fil plein donne un niveau, comme une carte commune.

**Règle.**
- Déclencheur et seuil sur chaque fiche (`usage` dans `passive_souvenirs.json`), déclencheurs listés au contrat (`usage_triggers`), contrôlés au chargement : un fil sur un objet ou un déclencheur inconnu refuse le catalogue.
- **Le fil s'allonge avec le niveau de l'Écusson** : seuil × (1 + 0,1 × (niveau − 1)), réglage `level_scale` d'`inventory.json`. Sans cela, un Écusson qui renforce ce qui remplit son fil s'emballe : à seuil fixe, l'arpenteur (portée) a pris 52 niveaux par l'usage sur une graine, l'horloger et le batteur avaient la même boucle. Une carte qui monte l'Écusson allonge aussi son fil.
- `BadgeUsage`, enfant du joueur, écoute l'EventBus ; une horloge d'un quart de seconde ne tourne que pour le contact et la distance. Un cône qui frappe sa cible à chaque image sous un même lancer ne compte qu'un coup par créature.
- HUD : un fil doré sous chaque Écusson de la barre du bas ; un niveau gagné ainsi fait briller l'Écusson une fois. Rien d'autre en run.

**Seuils réglés au bot** (au niveau 1 de l'Écusson) :

| Écusson | Monte quand… | Seuil |
|---|---|---:|
| du batteur | une arme touche (une fois par créature et par attaque) | 5 000 coups |
| du semeur | une attaque touche 3 créatures | 300 coups larges |
| de l'arpenteur | une créature meurt de ce qu'a fait une arme à plus de 250 px | 400 |
| de l'horloger | un coup est critique | 750 |
| du veilleur | une créature est à moins de 200 px | 150 s |
| de la couturière | le joueur regagne des PV, toutes sources | 2 000 PV |
| du rugbyman | le joueur encaisse un coup | 250 coups |
| du pompier | le bouclier absorbe des dégâts | 20 |
| du coureur | le joueur se déplace | 1 500 m |
| du glaneur | de l'XP est ramassée | 1 200 fois |
| du bon élève | le joueur gagne un niveau | 6 niveaux |
| du forain | un coffre s'ouvre | 3 coffres |
| du jongleur | une arme à distance touche | 750 |
| du barbier | le vol de vie rend des PV (nouveau `HealingKind.Lifesteal`) | 450 PV |
| de l'apothicaire | un statut posé par une arme s'achève | 900 |
| de l'archer | une attaque à distance touche une créature de plus (traversée, rebond, salve) | 300 |

Les seuils du §9.1 étaient 10 à 100 fois trop bas : le bot fait 50 à 90 niveaux, 40 000 à 60 000 coups et 13 km en 30 min.

**Mesure** (Vagabond en profil neuf, bot mortel qui ratisse les lieux, 4 graines, 30 min) : niveaux d'Écussons gagnés depuis le niveau 1, dont ceux du fil.

| Graine | Niveaux d'Écussons | Dont l'usage | Part |
|---|---:|---:|---:|
| 1002 | 53 | 13 | 25 % |
| 123 | 68 | 18 | 26 % |
| 42 | 55 | 18 | 33 % |
| 7 | 71 | 15 | 21 % |

Un quart des niveaux vient de l'usage, comme visé (§9.1). Le détail varie avec le build : l'apothicaire ne monte que si une arme pose des statuts, l'archer et le jongleur qu'avec une arme à distance.

**Vérifications.**
- Build sans avertissement ; `inventory` : contrat et fiches cohérents, refus d'un fil sur un objet et d'un déclencheur inconnu, bon élève (un niveau, un éclat), fil qui s'allonge au niveau 2 et après une carte, coup large sur trois créatures, dix tics de cône comptés une fois, critique, forain.
- `smoke`, `objects`, `catalogs`, `quests`, `perk_effects`, `perk_acquisition`, `choice_screen`, `ui_art`, `weapons`, `saves`, `camp` verts.
- Capture du HUD (`--capture-pause`) regardée : les quatre fils sous les Écussons, encore vides après 20 s de combat (moins d'un seizième). Le remplissage et l'éclat sont vérifiés par la suite `inventory`, pas à l'image : à juger en jeu.
- Mesure : nouveau rapport `badge_levels`, `badge_usage_levels`, `badges` et `usage_totals` (tous les déclencheurs comptés pendant la mesure, portés ou non) dans `RunObservation`.
- Relecture `godot-reviewer`, corrigés : bouclier recompté dans un appel imbriqué (l'Écusson du pompier monte et rend du bouclier), tics de cône comptés comme des coups, lancers suivis un seul à la fois, statuts et éliminations dus aux objets comptés, horloge qui tournait pour rien, éclat du HUD qui pouvait se doubler.
- Reste approximatif, assumé : le glaneur compte aussi l'XP d'un lieu ou d'un fragment de lore (quelques fois par run, contre des milliers d'orbes).

## 18. Lot I9 — coffres réservés aux objets du sac (11 octobre 2026)

Demande de Raphaël : les coffres ne donnent plus que des objets.

Lot livré :
- Les quatre tables de coffres (commun, rare, épique, ancien coffre de lore) ne tirent que des exemplaires d’objets débloqués. Nombre de tirages conservé : un, sauf deux pour l’épique.
- Supprimer le bonus de statistique et son ancienne configuration. Aucun remplacement par de l’Essence : sac plein, l’objet est posé au sol pour l’échange existant.
- La roulette ne montre que des objets accessibles ; adapter la galerie et les régressions.
- Valider par build sans avertissement, suites inventaire/objets/catalogues/interface, smoke et capture du butin en run.

Raretés des objets : aucune dans le modèle actuel, tirage uniforme parmi les objets accessibles et encore cumulables. Raphaël signale des écarts de puissance ; une répartition par rareté reste à définir, sans classement arbitraire ajouté dans ce lot.

**Validation I9 :** build à 0 avertissement ; `smoke`, `inventory` (67 contrôles), `objects`, `catalogs`, `choice_screen`, `ui_art` verts (`/tmp/vestiges-chests-items-20261011`). Après retrait des champs de bonus devenus inutiles et adaptation des fixtures UI, nouveau build et suites `inventory`/`choice_screen` verts (`/tmp/vestiges-chests-items-final-20261011`). Les tests couvrent les quatre coffres, le sac plein au tirage et après le premier objet d’un épique, l’absence de conversion en Essence, le dépôt au sol et l’ouverture/annulation de l’échange. Capture réelle seed 42 regardée : roulette et résultat uniquement objet (`/tmp/vestiges-chests-items-capture-20261011/loot-revealed.png`).

Le retrait des bonus de statistiques et l’augmentation du nombre d’objets changent l’économie de puissance : aucun nouvel équilibrage de run longue n’est revendiqué. Sans rareté propre aux objets, commun et rare donnent pour l’instant le même tirage ; l’épique donne deux objets.

## 19. I10 — rareté fixe des objets, préparation (11 octobre 2026)

Raphaël demande un rang fixe par objet, des probabilités liées au rang et un classement cohérent avec la puissance. [Proposition complète](35-raretes-des-objets.md) : 22 objets classés dans les cinq rangs existants, probabilités par source, prix d’Atelier, apparence et lots I10a–I10d. Principe acquis ; classement et taux proposés, pas encore implémentés ni mesurés. Aucun multiplicateur automatique de puissance ajouté par la rareté.

## 20. Révision du 12 octobre — cumul simple et extension du catalogue

Les retours de Raphaël (DECISIONS §128) remplacent les propositions de doublon et le classement du 11 octobre : aucun effet de palier sur les objets du sac ; effets révisés et budgets de déclenchement au [plan I10](35-raretes-des-objets.md). Extension préparée : [57 objets au total, dont 35 nouveaux](35-catalogue-objets-v2.md), statistiques globales et propriétés d’armes, utilitaires et hauts rangs accessibles dès le profil neuf. Les sections de livraison antérieures décrivent le code existant ; la nouvelle cible reste à implémenter, un lot à la fois.

## 21. Deuxième retour du 12 octobre — économie, soins et effets vérifiables

DECISIONS §132 complète §128 : régénération affichée en PV/min, noms plus courts, Chance appliquée aux coffres, relèvement du plafond de vol de vie, toutes les statistiques en pause et effets réellement appliqués/visibles. Les orbitales utilisent la quantité de projectiles commune ; Anneau retiré. Étiquette conditionnant les flèches des coffres, coffres payants en Essence et Pied-de-biche pour une chance d’ouverture gratuite. Plan cadastral abandonné, Portrait effacé proposé en remplacement.

Cible révisée : **56 objets dont 34 nouveaux** (14 communs, 18 inhabituels, 11 rares, 8 épiques, 5 légendaires). [Catalogue](35-catalogue-objets-v2.md) et [plan](35-raretes-des-objets.md) précisent les noms, soins, formules et lots I10d/I10e ajoutés. Les valeurs de Chance, soins, prix et nouveaux effets restent proposées. La section 20 décrit la première révision ; aucune mise en jeu de ces propositions n’est revendiquée.
