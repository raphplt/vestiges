# Plan 35 — L'inventaire du joueur, repensé une dernière fois

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
