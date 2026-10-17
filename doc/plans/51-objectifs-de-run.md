# Plan 51 — La run en terres

> **Point de reprise du 16 octobre, à lire en premier.** Il couvre la boucle revue (ce plan) et l'Élan ([plan 52](52-elan.md)). Les citations et décisions détaillées sont au [registre §184–186](DECISIONS.md).
>
> **Pourquoi.** Le premier testeur n'avait pas d'objectif dans la run et se sentait perdu vers 15 à 20 min ; Raphaël trouve la grande carte unique lassante. La boucle avait été construite d'après le lore : on repart du jeu, et le lore suivra, retouché avec cohérence.
>
> **Boucle arrêtée le 16 octobre au soir : voir le §11, qui fait foi** ([registre §186](DECISIONS.md)). En bref :
> - **la run** : trois terres d'environ 10 min, tirées parmi 5, sur des cartes plus petites, sans choix de la suivante ; la difficulté monte sans arrêt ; après le troisième gardien, **une 4e carte dédiée au late game**, sans fin ;
> - **une terre** : raviver des Mémoriaux en tenant leur cercle 20 s (clés, ancres et puissance), réveiller le gardien au cœur dès trois lumières, ou il se lève seul quand la terre est réduite ; l'anneau de terre restante autour de la minimap sert de jauge ;
> - **l'arène, c'est ce qu'on a sauvé** : au réveil, le sol non ancré s'effondre ; pendant le combat, le front se fige et le gardien brise le sol ;
> - **le vide** se saute à l'Élan, ou se traverse en payant l'Essence puis les PV ;
> - **le passage** : la terre suivante se recoud autour du joueur (un portail en repli) ;
> - **la charte des gardiens** est validée telle quelle.
>
> **Mis en réserve** (§11.5) : l'île qui dérive, l'archipel, les terres de plus en plus courtes, la Montée en finale fixe.
>
> **Encore ouvert** (§11.6) : la 4e carte, les questions de l'Élan, la place des Résurgences, les durées ; Raphaël doit jouer la Barrière et l'Indicible avant de décider de leur sort.
>
> **Ordre changé le 17 octobre** (registre §186) : le prototype des terres (P0 à P3, recadrés au §11.7, 3 à 4 semaines) passe avant la suite de l'Élan. E2 (l'esquive parfaite) est livré et jugé « un peu difficile, mais c'est une bonne chose ».
>
> **Élan acté** : c'est la mécanique centrale, en version « maîtrise ». La vitesse et les gestes bien faits (esquive parfaite, saut du vide, percussion, glissade) rendent plus fort. La décision du 13 octobre est annulée.
>
> **Élan livré : lot E1** (`71e67a0c`).
> - Jauge à trois paliers, nourrie par la course jusqu'au palier I, perdue à l'arrêt et d'un palier par coup.
> - Bonus de +8 %/+10 % à +24 %/+36 % de vitesse et de dégâts ; barre sous la recharge du dash.
> - Bac à sable `tools/elan_sandbox.sh`, à essayer.
> - L'Élan est éteint en run normale : `enabled_in_runs` vaut faux dans `data/movement/elan.json`.
> - Avec E1 seul, on ne dépasse pas le palier I.
>
> **Élan, la suite** : E2 l'esquive parfaite, E3 le saut du vide, E4 la percussion, E5 la glissade, E6 les retours et réglages.
>
> **Élan ouvert** : jauge ou seulement une traînée ; l'Élan gardé ou non d'une terre à l'autre ; une créature tombée dans le Néant comptée comme éliminée (proposé : oui).
>
> **Prototype, état au 17 octobre** (branche `terres`, worktree `../vestiges-terres`, non fusionnée ; terres allumées par `--lands`) :
> - P0 horloge de run unique (§12, sur `main`) ;
> - P1a à P1d : construire une terre, la vider, remettre les systèmes à zéro, passage jouable par la commande `land next` (§13) ;
> - P2a le front de chute, P2b les Mémoriaux à tenir, P2d le vide qui prend l'Essence (§14).
>
> **Reste** : P2c (anneau de terre restante, minimap du front), P1e (recousure), puis P3 (gardien réveillé par trois lumières, effondrement au réveil, gardien qui se lève seul, multiplicateur de rang). À la fusion vers `main`, reporter la correction du banc du butin (oubli de P0). **Captures impossibles tant que macOS ne dessine pas la fenêtre de capture** (fenêtre masquée).
>
> **Sur un autre poste** (Mac) : `git pull`, puis `tools/elan_sandbox.sh`. `GODOT_BIN` désigne le binaire Godot .NET 4.7.2 s'il ne s'appelle pas `godot-mono`.

15 octobre 2026. Ouvert sur le manque relevé par le premier testeur ([§184](DECISIONS.md)) : la run n'a pas d'objectif, et le joueur se sent perdu vers 15 à 20 min. **Direction retenue par Raphaël le jour même ([§185](DECISIONS.md)) : une run traverse trois terres, chacune une carte plus petite qui s'efface de ses bords vers son cœur, avec un boss à la fin.** Rien n'est implémenté. Les propositions ci-dessous sont à trancher (§9).

**Mise à jour du 16 octobre ([§185](DECISIONS.md)) :**
- **L'Élan devient la mécanique centrale** (plan 52), à valider en bac à sable avant la structure.
- **Structure : la terre qui tombe est préférée** (§3). La Traversée et le Mangeur sont écartés. Les détails restent à étudier « plus librement, sans trop se rattacher à l'existant » (§10).
- **Boss** : agressifs, avec de vrais designs. La Barrière n'est probablement pas à garder comme boss.

La première version de ce plan, « la Descente » (une seule carte en deux paliers, collée au lore), est remplacée. Raphaël a demandé de partir du jeu, pas du lore : « la boucle de gameplay s'était plus adaptée en fonction du lore, alors que normalement ça aurait dû être l'inverse ».

## 0. En bref

> Première version du 15 octobre, **remplacée par le §11** là où elle diverge (Mémoriaux clés, pas de choix de terre, recousure au lieu des ponts, 4e carte de late game).

- **Une run, c'est trois terres** tirées parmi une sélection : 5 au départ (forêt, champs, ville, marais, carrière), d'autres plus tard.
  - Environ 10 min par terre, et un gardien à battre à la fin de chacune.
  - Une run réussie dure environ 30 min.
  - Ensuite, la boucle recommence plus dure, pour les runs d'une heure et plus.
- **La signature : la terre se défait.** Elle tombe dans le Néant de ses bords vers son cœur, avec préavis, selon un motif propre à chaque terre. Le joueur la ralentit là où il se tient. Les Mémoriaux ravivés deviennent des ancres qui tiennent leur zone.
  - Le vide n'est pas un mur : on peut le traverser en payant.
  - Ce qui pousse à se dépêcher : le butin des bords va tomber, et la terre rétrécit.
- **Les Mémoriaux sont les clés.** Il en faut trois pour appeler le gardien au cœur, quand on veut. Chaque Mémorial de plus rend le gardien plus dur et plus généreux.
  - Si on n'appelle jamais le gardien, il se lève seul quand il ne reste que le cœur, sur un îlot, au milieu de la foule.
- **Le passage : deux ponts.** La mort du gardien fait naître deux chemins à travers le Néant, vers deux terres qu'on voit à l'horizon. On choisit sa route.
  - La traversée est un temps calme pendant lequel la terre suivante se prépare : elle cache le chargement.
- **L'identité de chaque terre** vient d'abord de sa façon de s'effacer, qui ne coûte presque rien à faire varier. S'y ajoutent une règle propre, une famille de créatures et un gardien.
- **Faisabilité** : changer de carte en cours de run est le socle. Il faut le construire, pour 2 à 3 semaines, parce que tout le code suppose une carte unique.
  - Un prototype jouable, pour juger le plaisir avant de produire du contenu : 3 à 4 semaines.
  - La production complète demandera ensuite les terres travaillées, les gardiens et le lore.

## 1. Ce que font les autres jeux (recherche du 15 octobre)

| Jeu | Ce qui fait passer à la suite | Ce qui pousse à partir |
|---|---|---|
| Risk of Rain 2 | Trouver le téléporteur, le charger 90 s dans sa zone et tuer le boss. 5 étapes par boucle, chaque rang tiré parmi 2 à 9 variantes | Difficulté = (1 + 0,0506 × minutes) × 1,15^étapes, et le butin de chaque carte est fini. Conseil courant : activer avant 3 min. L'autel de la Montagne double le boss contre le double de butin |
| Megabonk | Porte du boss à trouver, chrono de 10 min par étape, 1 à 3 étapes selon le palier | À la fin du chrono vient une nuée infinie de fantômes. Elle a été détournée pour farmer, puis corrigée par patch |
| DRG: Survivor | Barre d'objectif qui avance seule, élite, puis capsule de départ. 5 étapes | 30 s pour rejoindre la capsule, sinon la run est finie |
| Halls of Torment, HoloCure, 20 Minutes Till Dawn | Boss à l'heure, survivre au chrono | Chrono fixe, notre situation actuelle |
| Jeux de type « Spleef » (mini-jeux Minecraft) | Le sol de l'arène disparaît | Survivre sur ce qui reste |
| Battle royales | Zone qui rétrécit en cercle | Dégâts hors de la zone |

Sources : [wiki RoR2, difficulté](https://riskofrain2.wiki.gg/wiki/Difficulty), [téléporteur](https://riskofrain2.wiki.gg/wiki/Teleporter), [autel de la Montagne](https://riskofrain2.wiki.gg/wiki/Shrine_of_the_Mountain), [environnements](https://riskofrain2.wiki.gg/wiki/Environments), [PC Gamer sur Megabonk](https://www.pcgamer.com/games/roguelike/megabonk-review/), [Destructoid](https://www.destructoid.com/how-to-go-to-the-next-stage-in-megabonk/), [wiki DRG: Survivor](https://deeprockgalactic.wiki.gg/wiki/Survivor:Elimination).

**Leçons :**
- **La pression la plus saine** vient d'une ressource finie face à une menace qui monte avec le temps. Un temps illimité pousse à farmer.
- **Chercher l'objectif lasse** sur une grande carte : c'est une plainte récurrente sur RoR2. Une carte plus petite, avec un cœur visible, règle ça.
- **Les étapes lassent s'il y a peu de cartes** : IGN le reproche à Megabonk, Ars Technica à Halls of Torment. Il faut donc des terres qui se jouent autrement, pas seulement qui se voient autrement.
- **Personne ne fait du sol qui disparaît le chrono d'un survivors-like.** La zone des battle royales est un cercle uniforme, subi, avec des dégâts hors zone. Le Spleef est une arène sans combat de foule. C'est notre ouverture.

## 2. La run

```
TERRE 1 (rang 1)          PONTS        TERRE 2 (rang 2)          PONTS        TERRE 3 (rang 3)
~10 min, gardien    →   choix A/B   →  ~10 min, gardien    →   choix A/B   →  ~10 min, gardien final
                                                                                    ↓
                                                     victoire (bilan) ou on continue : la boucle,
                                                     terres de rang 4, 5…, difficulté multipliée
```

- **La durée.** Une run réussie dure environ 30 min, 10 min par terre, durée à confirmer en jouant. Une très grosse performance passe par la boucle et dépasse l'heure.
- **La difficulté ne redescend jamais.**
  - Une horloge de run continue fait monter PV, dégâts et densité, comme aujourd'hui.
  - Chaque terre franchie ajoute un multiplicateur de rang, l'équivalent du ×1,15 par étape de RoR2.
  - Le Péril, déjà affiché, porte cette montée et la rend visible.
  - La tension monte donc par paliers à chaque terre et en continu pendant chacune, comme Raphaël le demande : progressive, pas constante.
- **N'importe quelle terre peut tomber à n'importe quel rang.** Le danger vient du rang, pas du biome. Une forêt de rang 3 est dure. C'est plus de variété avec le même contenu.
- **Après la troisième terre**, deux choix proposés :
  - s'arrêter sur une victoire, avec le bilan ;
  - continuer dans la boucle, une suite de terres de rang 4, 5…, sans fin. Elle remplace le late game sur place des plans 34 et 47 et sert la course au classement.

## 3. La signature : la terre se défait

Critères visés :
- **urgence lisible** : on voit ce qui va tomber, et quand ;
- **un choix par minute** ;
- **jamais de mort injuste** : le sol ne disparaît jamais sous les pieds sans préavis ;
- **elle joue avec le combat** ;
- **chaque terre s'efface à sa façon** ;
- **ce n'est pas le cercle d'une battle royale.**

### Le front

- **La date de chute de chaque morceau de sol est calculée dès la génération.** Elle dépend de :
  - la distance au cœur ;
  - un bruit, qui dessine des langues et des baies, donc des îlots naturels ;
  - le motif propre à la terre (§5).

  Le front est donc prévisible et lisible. La minimap peut montrer ce qui tombe dans la minute.
- **Trois états avant le vide**, chacun visible et audible, de 20 à 30 s chacun : **Fragile** (fissures, couleur qui pâlit), **Effiloché** (sol qui clignote, créatures plus rapides), puis **Néant**. Ces états existent déjà dans le code ; ils passent du temps global à la date de chute.
- **Le cœur ne tombe jamais.** C'est là qu'on appelle le gardien.
- **Le rythme** : une terre qu'on laisse filer se réduit à son cœur vers 12 à 13 min. Avec 10 min visées, il reste une marge.
- **Le premier morceau tombe vers 1 min**, après un répit d'arrivée.

### Ce que le joueur peut faire contre le front

- **Tenir là où il se tient.** Le front ralentit près du joueur : c'est déjà le cas, −72 % de déclin dans un rayon de 2,5 cellules. Ce qu'on regarde tient. Défendre un coin riche devient un choix, mais l'horloge, elle, continue.
- **Ancrer un morceau avec un Mémorial ravivé.** Sa zone ne tombera plus, c'est un îlot sauvé avec ses coffres. Choix : raviver le Mémorial du bord, qui sauve une région mais peut devenir un îlot coupé, ou celui du centre, plus sûr ?
- **Payer en vitesse.** Une Faille donne de la puissance contre un Oubli, et les Oublis accélèrent déjà l'Effacement. Ce compromis existe dans le code, mais on ne le voit pas. Il devient lisible : la terre tombe plus vite.

### Ce qui pousse à se dépêcher

- **Le butin des bords va tomber.** Les lieux sont plus riches près du front ; c'est un principe du plan 22, déjà en place. Le coffre du bord vaut le détour, et dans 40 s il n'existe plus.
- **La menace vient du vide.** Les créatures naissent plus nombreuses près du Néant (`spawn_density_at_zero_memory` vaut 2,2) : la foule pousse depuis les bords.
- **La terre rétrécit.** Moins de place pour esquiver, foule plus serrée, le combat se resserre de lui-même.

### Le vide : on le traverse, en payant

Un îlot coupé ne doit pas devenir une perte bête. Quatre options :

| Option | Effet | Pour | Contre |
|---|---|---|---|
| **A. Mur** | Le Néant bloque | Simple, très lisible | Îlots perdus, frustrant |
| **B. Traverser en payant (recommandé)** | Le Néant draine l'Essence, puis les PV une fois l'Essence épuisée | Un petit saut est bon marché, une longue traversée coûteuse. L'Essence devient « la mémoire qui tient debout » | Le réglage du drain demande du soin |
| C. Ponts de mémoire | Maintenir une touche vers un îlot fait naître un chemin, payé en Essence à la longueur | Actif, créatif. Côté lore : on paie pour se souvenir d'un chemin. Côté code : c'est une stabilisation en ligne, qui existe déjà | Une action de plus à apprendre |
| D. Chute | Tomber fait perdre des PV, puis on revient au dernier sol | Le frisson du Spleef | Punitif sur une run de 30 min |

- **Recommandation : B d'abord, C ensuite**, comme objet, pouvoir de personnage ou service d'Atelier.
- **Le dash ne traverse rien aujourd'hui** : il fait environ 70 px. Si l'on veut des petits sauts de vide, c'est un argument pour la refonte du mouvement (plan 42 §6).
- **Les personnages pourraient y trouver une identité** : pour la Scaphandrière, le Néant est la mer, et elle le traverserait à moindre coût.

### Le gardien

- **Trois Mémoriaux ravivés l'appellent.** Il en faut moins si certains ont été perdus : on ne compte que ceux qui restent. Le joueur va au cœur quand il veut et lance le combat.
- **Chaque Mémorial au-delà de trois** rend le gardien plus fort et sa récompense plus riche. C'est le pari de l'autel de la Montagne, à notre façon.
- **Si le gardien n'est jamais appelé** et que la terre se réduit à son cœur, il se lève seul, renforcé, sur l'îlot qui reste, au milieu de la foule. Ce n'est pas un chrono arbitraire : c'est la conséquence de l'attente. Battre le gardien reste possible, mais c'est intenable si le build ne suit pas.
- **Pendant le combat**, deux options :
  - le front se fige, pour un combat juste ;
  - le front continue, et l'arène rétrécit (Spleef pendant un boss).

  À trancher en jouant, peut-être selon la terre.
- **Il faut un bestiaire de gardiens dédiés** : un par terre, plus le gardien final. Raphaël préfère de « gros personnages forts » aux entités abstraites. Le prototype utilise les Souverains existants.

### Le passage : deux ponts

- **La mort du gardien fait naître deux chemins à travers le Néant**, vers deux terres visibles à l'horizon : silhouette, nom et ce qui les rend particulières (« marais, foule rapide », « ville, élites en plus »).
- **On marche sur le pont choisi, 10 à 20 s de calme.** La terre quittée tombe entièrement derrière soi. Ça sert à trois choses :
  - respirer ;
  - le choix de route, qui rend la run moins linéaire ;
  - cacher la préparation de la terre suivante, 1,5 à 2,5 s de chargement.
- **Un portail reste possible.** Les ponts sont plus à nous et cohérents avec le vide.

## 4. Où vont les systèmes actuels

| Système | Devient |
|---|---|
| Mémoriaux | Clés du gardien et ancres contre le front. Flux de réveil gardé, 4 à 5 par terre |
| Coffres et lieux (Ateliers, Puits, Failles, stèles) | Moins nombreux par terre, plus serrés, plus riches près du front |
| Effacement | Date de chute par cellule et motif par terre au lieu d'un déclin global. La présence du joueur, l'ancrage et les Oublis sont gardés |
| Résurgences | Une par terre, comme pic à mi-terre, ou fondues dans le front. À trancher au prototype |
| Micro-événements | Par terre, à thème |
| Souverains | Gardiens provisoires du prototype ; ils restent des rencontres de chasse |
| Barrière, Indicible | **Raphaël doit les jouer avant de trancher** (dev : `tools/run_dev.sh`). L'Indicible pourrait devenir le gardien final, la Barrière un gardien de la ville, ou être retirés |
| Péril, niveaux de difficulté, Campement | Inchangés. Le Péril porte la montée |
| Score et classement | Éliminations, inchangés ; les terres atteintes s'ajoutent au bilan |
| Minimap | Centrale : elle montre le front et ce qui va tomber |
| Météo, atmosphère, musique | Par terre |
| Lore | Repris après le jeu (§7) |

## 5. L'identité de chaque terre

Le levier le moins cher et le plus fort, c'est **le motif d'effacement**. Changer la fonction de la date de chute change toute la façon de jouer la terre. Pistes à choisir, pas des décisions :

| Terre | Elle s'efface… | Sa règle | Ce que ça fait jouer |
|---|---|---|---|
| Forêt | Par **clairières** qui s'ouvrent aussi à l'intérieur, pas seulement aux bords | Canopée qui cache, vue courte | Lire la carte, se méfier du centre aussi |
| Champs | Par **sillons**, des bandes parallèles comme une moisson | Grand ouvert, vent qui pousse projectiles et foule | Longues lignes, foules rapides, course |
| Ville | Par **pâtés entiers** qui tombent d'un coup au préavis | Rues-couloirs, élites qui sortent des bâtiments | Goulets, choisir sa rue |
| Marais | Comme une **marée** qui monte puis redescend en partie : du sol revient | Eau qui ralentit, eau profonde | Rythme en vagues, attendre le reflux |
| Carrière | Par **paliers**, un niveau entier s'effondre | Hauteurs, cristaux d'Essence abondants | Le vide moins cher à traverser, verticalité |

Chaque terre a aussi sa famille de créatures (en partie déjà répartie par biome), son gardien, ses événements et son ambiance. **Les variantes viennent plus tard** pour la rejouabilité (ville inondée, forêt de nuit), comme les versions de cartes de Megabonk.

## 6. Pourquoi c'est à nous

- **Le chrono est le monde lui-même, pas un nombre.** On voit le temps manger la carte.
- **Le joueur agit sur le chrono** : sa présence, les ancres, les Oublis, la traversée payée. Le cercle des battle royales est subi.
- **Le motif d'effacement donne l'identité des terres** : la même mécanique se joue cinq fois différemment.
- **Le choix de route se fait à pied, sur le vide**, pas dans un menu.
- **Le thème** (un monde qu'on oublie, qu'on fuit, qu'on retient) n'est plus un décor mais la règle du jeu.

## 7. Le lore, après le jeu

Le script ([VESTIGES-LORE.md](../VESTIGES-LORE.md)) se prête mieux qu'il n'y paraît :
- **§2** : les biomes sont déjà « les morceaux de la vallée qui tiennent encore, recousus au hasard ». Chaque terre est un de ces morceaux, et celui qu'on quitte se décroche.
- **§1** : « Ce qu'on n'entretient plus s'efface » donne la terre qui tombe. « On se souvient des lieux, pas des chemins » : les ponts sont ce qu'il en coûte de retenir un chemin.
- **Mémoriaux** : des gens qui ont refusé d'oublier. Ils tiennent leur zone, ce sont les ancres.

**À reprendre** :
- la présence de la Montée « dans chaque carte » (§7 bis) ;
- la règle « ce qu'il a vu reste à sa place » ;
- l'Indicible et la Barrière, selon le sort des boss ;
- l'au-delà (§11), qui devient le chemin entre les morceaux.

Ce travail se fait en lot dédié, une fois la boucle jouée, pour adapter le lore au jeu. Pas avant.

## 8. Faisabilité et lots

### Le socle technique (études du 15 octobre)

- **Pas d'horloge unique** : sept compteurs de temps indépendants pilotent la run (`SpawnManager`, `CrisisManager`, `RunEventDirector`, `EndgameManager`, `ErasureManager`, `RunTracker`, `QuestTracker`). Un service `RunClock` les remplace, en gardant le temps réel pour le bilan.
- **Une seule carte par run.**
  - La génération et la pose (`WorldSetup`, ~3,6 s, jusqu'à 7 s sous charge) sont faites une fois.
  - Des index statiques (`ObstacleField`, `Memorial.All`, `Chest.Closed`, `SmallPlace.Revealed`) et une vingtaine de systèmes gardent des références à la carte : Effacement, minimap, brouillard, lumières, voix, pools, directeurs.
  - Tout l'état du joueur vit dans des nœuds de `Main`, sans sauvegarde instantanée.
- **Recommandé : régénérer en place**, pour 2 à 3 semaines.
  - Isoler ce que la carte produit.
  - Ajouter un `ClearWorld` et une construction de terre.
  - Remettre à zéro chaque système sur un signal `LandChanged`.
  - Ajouter un test qui enchaîne trois terres sous le bot invincible.
  - Avec des terres environ trois fois plus petites, la génération pure peut tourner pendant la marche sur le pont.
- **Recharger la scène** serait plus risqué : 3 à 5 semaines pour transporter une quinzaine de systèmes.
- **Graines** : une par terre, dérivée de la graine de run et du rang. À corriger : `WorldSetup._EnterTree` relance `RunRandom` à chaque chargement.

### Lots du prototype (juger le plaisir avant de produire)

| Lot | Contenu | Coût estimé |
|---|---|---|
| **P0 Horloge** | `RunClock` unique ; mesure avant/après identique | 2–3 j |
| **P1 Changer de terre** | Régénération en place, terre réduite et mono-biome sur les générateurs actuels, pont de passage simple, graine par terre, test de trois terres | 2–3 sem. |
| **P2 La terre se défait** | Date de chute par cellule, trois états avec préavis, cœur sûr, présence du joueur, ancres des Mémoriaux, minimap du front, vide traversable (Essence puis PV) | 1–1,5 sem. |
| **P3 Gardien et route** | Trois Mémoriaux appellent le gardien (Souverain provisoire) au cœur, gardien qui se lève seul si la terre est réduite, deux ponts au choix, multiplicateur de rang, Péril visible | 1 sem. |
| — | **Raphaël joue** ; réglages | — |

En parallèle, ou avant P3 : **Raphaël joue la Barrière et l'Indicible actuels** pour décider de leur avenir.

### Production ensuite (à découper après le prototype)

- Motif et règle propres à chaque terre (§5), terres plus travaillées, avec des pièces posées à la main.
- Bestiaire des gardiens et du gardien final : conception, sprites, combats.
- Boucle après la troisième terre, victoire et bilan.
- Lore (§7), Stratégie V2 (§6 à §14 à réécrire), première run guidée (plan 50).
- Plans touchés : 22 (carte), 30 (temps forts), 33 §8, 34 et 47 (calendriers sur 30 min), 07 (boss), 28 (Péril).
- Classement : terres atteintes au bilan ; règles du weekly inchangées.

**Risques :**
- **Frustration** (îlots, chutes) : préavis, cœur sûr, vide traversable.
- **Monotonie** avec 5 terres : motifs d'effacement, puis variantes.
- **Caches oubliés** lors du changement de terre : test d'enchaînement obligatoire.
- **Ampleur** : c'est un tournant du jeu. Les autres gros chantiers attendent le verdict du prototype.

## 9. Questions à Raphaël

> **Répondu le 16 octobre (§11, registre §186)** : vide traversable en payant, avec le saut d'Élan ; gardien qui se lève seul ; recousure (portail en repli) ; 4e carte de late game sans fin ; front figé et sol brisé par le gardien. Le feu vert du prototype vient après les lots E2 et E3 de l'Élan.

1. **Le vide** : mur, traversée payée en Essence puis en PV (recommandé), ponts de mémoire à créer, ou chute ?
2. **Si on n'appelle pas le gardien** : il se lève seul, renforcé, sur l'îlot qui reste (recommandé), ou rien ne se passe et la foule finit le travail ?
3. **Le passage** : deux ponts vers deux terres visibles (recommandé), ou un portail ?
4. **Après la troisième terre** : victoire ou boucle au choix (recommandé), ou late game sur place ?
5. **Pendant le combat contre le gardien** : front figé, ou arène qui rétrécit ? À essayer au prototype, ou décidé maintenant ?
6. **Feu vert pour le prototype P0 à P3** (3 à 4 semaines, Souverains en gardiens provisoires), avant toute production de terres et de boss ?

## 10. Suite du 16 octobre : ce qui reste à étudier

> **Tranché au §11** : anneau de terre restante, cercle de Mémorial à tenir 20 s, gardien réveillé par trois lumières, Mémoriaux clés, ancres et puissance, charte validée.

Retenu : la terre qui tombe. Écartés : la Traversée (front derrière le joueur, trop linéaire) et le Mangeur (un gardien errant qui mange la terre, coûteux et contraire à un boss agressif).

**À étudier**, en pensant plus librement que l'existant :
- **Une jauge ou un chrono visible.** Raphaël en veut un : difficulté, effacement, à voir. Une piste : la part de terre restante, montrée comme une jauge, plutôt qu'une horloge.
- **Une zone à tenir.** Idée de Raphaël : « rester dans une zone délimitée ». Par exemple, raviver un Mémorial en tenant sa zone pendant que la terre tombe autour. Attention, c'est le principe du téléporteur de Risk of Rain 2 : à rendre propre au jeu par le lien avec la chute du sol et avec l'Élan.
- **Le réveil du gardien** : un repaire visible au cœur, réveillé quand on veut, ou seul quand il ne reste que le cœur. Pas de clés à rassembler.
- **Mémoriaux** : des ancres, qui sauvent leur zone, et de la puissance.

**Charte des gardiens** (proposée, à valider) :
- agressif : il traque le joueur ;
- des attaques annoncées et lisibles, qu'on apprend ;
- pensé pour l'Élan : charges à sauter, ondes à traverser en esquive parfaite, coups qui brisent le sol en vide ;
- l'arène tombe pendant le combat ;
- deux ou trois phases, un combat de 1 à 2 min ;
- un vrai corps, un gros personnage, différent pour chaque terre.

Avant les terres : **un gardien de référence** conçu avec cette charte. La Barrière, un mur passif par nature, sort des boss ; l'Indicible est à jouer avant de décider.


## 11. La boucle arrêtée — 16 octobre 2026 (soir)

Raphaël revient sur trois points du §0 : les Mémoriaux « pas des clés », le choix entre deux terres, et le format « trois terres de 10 min », jugé peu original. Il demande de raisonner en game designer : un jeu que n'importe qui comprend, aux objectifs clairs, auquel on veut rejouer. Après trois formats plus audacieux (§11.5), il choisit de partir au plus simple. **Cette section fait foi : elle remplace le §0 et les §2 à §5, et répond aux §9 et §10.** Décisions au [registre §186](DECISIONS.md).

### 11.1 Le test d'une bonne boucle

Les critères auxquels on mesure chaque choix, maintenant et au prototype :

1. **Lisible en cinq secondes, sans texte.** À tout moment, le joueur sait ce qui le menace (la foule, le sol qui tombe), où aller (une colonne de lumière, le cœur) et ce qu'il construit (un compteur qui avance).
2. **Un objectif à chaque échelle.** La seconde : bouger, esquiver, entretenir l'Élan. La minute : quel Mémorial, quel coffre avant qu'il tombe. La terre : réveiller le gardien quand on se sent prêt. La run : passer les trois gardiens, puis tenir le plus longtemps possible.
3. **Une décision qui compte par terre** : réveiller le gardien maintenant, ou chercher encore un Mémorial pendant que le sol s'en va.
4. **Des pics et des creux** : tenir un Mémorial (pic), explorer (creux), le gardien (sommet), le passage (souffle).
5. **Le temps et le butin sont finis**, ce qui empêche de farmer sans chrono arbitraire.
6. **Chaque mort dit « la prochaine fois, je… ».**
7. **La maîtrise se voit** : le bon joueur va plus vite, plus loin, avec plus de style (l'Élan).
8. **Une seule idée au centre** : le sol qui s'efface et le mouvement qui le défie. Mémoriaux, gardien et passage passent tous par le sol.

### 11.2 La run

```
TERRE 1        TERRE 2        TERRE 3        4e TERRE : LE LATE GAME
~10 min    →   ~10 min    →   ~10 min    →   carte dédiée, sans fin
gardien        gardien        gardien        la difficulté monte toujours
```

- **Trois terres d'environ 10 min**, tirées parmi 5 (forêt, champs, ville, marais, carrière), sur des cartes plus petites qu'aujourd'hui. Pas de choix de la terre suivante.
- **La difficulté monte sans arrêt** : horloge de run continue et multiplicateur de rang à chaque terre, portés par le Péril (§2).
- **Après le troisième gardien : une 4e carte dédiée au late game**, où l'on reste à l'infini pendant que la difficulté continue de monter, pour les runs d'une heure et le classement. Elle remplace le late game sur place des plans 34 et 47. Sa conception est ouverte (§11.6).
- **Durée** : environ 30 min pour atteindre la 4e terre, une heure ou plus pour une grosse performance.

### 11.3 Une terre : raviver, réveiller, recoudre

1. **Arriver.** La terre se forme autour du joueur, avec un répit d'environ 30 s. De loin, on voit des colonnes de lumière : ce sont les Mémoriaux éteints (4 ou 5 par terre). Au cœur, le gardien dort, un gros corps immobile.
2. **Raviver.** Entrer dans le cercle d'un Mémorial et le **tenir environ 20 s** pendant que la foule converge. Le cercle est assez large pour y courir : on le tient en mouvement, donc l'Élan se garde. Un Mémorial ravivé :
   - **allume une lumière sur le gardien** : c'est une clé, il en faut trois ;
   - **ancre sa zone**, qui ne tombera plus ;
   - **donne une puissance au choix**, comme aujourd'hui.

   Un Mémorial du bord rapporte plus, mais son sol tombe tôt. Si le front l'atteint avant qu'on l'ait ravivé, il est perdu.
3. **La terre tombe**, des bords vers le cœur, avec préavis (§3, inchangé : date de chute par cellule, trois états, cœur sûr, présence du joueur qui ralentit le front).
   - **La jauge** est un anneau autour de la minimap, qui se vide avec la terre. Un repère y marque le moment où le gardien se lève seul. Aucun chiffre.
   - **Le vide** : les petits trous se franchissent d'un dash en l'air (Élan, E3). Marcher dans le vide draine l'Essence, puis les PV.
4. **Réveiller.** Avec trois lumières, on touche le gardien quand on veut. Chaque Mémorial de plus le rend plus fort et sa récompense plus riche. Si on attend jusqu'au repère, il se lève seul, enragé, au milieu de la foule.
5. **L'arène, c'est ce qu'on a sauvé.**
   - Au réveil, tout le sol non ancré s'effondre d'un coup. Il reste le cœur et les zones des Mémoriaux ravivés, si bien que les choix d'exploration dessinent le combat.
   - Pendant le combat, le front se fige, mais les coups du gardien ouvrent des trous dans le sol.
6. **Recoudre.** Le gardien vaincu, l'îlot reste seul dans le vide et le butin vient au joueur. Puis la terre suivante se recoud autour de lui : les morceaux montent du Néant, du plus proche au plus lointain, comme la chute jouée à l'envers. La génération de la terre se cache dans ce moment. **Repli : un portail**, si la recousure se révèle trop dure à faire.

Le premier tutoriel s'écrit en trois étapes : raviver un Mémorial, réveiller le gardien, passer à la terre suivante.

### 11.4 La charte des gardiens (validée telle quelle)

- **Agressif** : il traque le joueur.
- **Attaques annoncées et lisibles**, qu'on apprend.
- **Pensé pour l'Élan** : charges à sauter, ondes à traverser en esquive parfaite, coups qui ouvrent le vide.
- **Deux ou trois phases**, un combat de 1 à 2 min.
- **Un vrai corps**, un gros personnage, différent pour chaque terre.

**Un gardien de référence** est conçu avant les terres. La Barrière sort des boss ; le sort de l'Indicible attend que Raphaël l'ait joué.

### 11.5 Mis en réserve (formats plus audacieux, non retenus pour l'instant)

Proposés le même soir pour s'éloigner du squelette de Megabonk, écartés par Raphaël au profit du plus simple. À rouvrir si le prototype manque d'identité.
- **L'île qui dérive** : une seule terre vivante. Le sol le plus ancien tombe, et chaque Mémorial ravivé fait monter du vide un morceau neuf de son côté, souvent d'un autre biome. L'île avance là où l'on se souvient. Trois Mémoriaux font monter le gardien, trois gardiens la finale. Le plus original, et le plus dur à coder (socle d'environ 4 à 5 semaines).
- **L'archipel** : 10 à 12 petites îles de 2 à 3 min, chacune avec son Mémorial, qu'on enchaîne par des sauts d'Élan, avec un gardien toutes les trois ou quatre. Le rythme est fort, mais haché, et on y perd l'exploration.
- **Terres de plus en plus courtes** (environ 9, 8 puis 6 min) et **la Montée comme finale fixe**, la même à chaque run, avec le gardien final en haut. La Montée pourrait devenir la 4e carte du late game.

### 11.6 Encore ouvert

- **La 4e carte du late game** : sa forme (arène fixe, qui tombe ou non), ce qui la relance (vagues, gardiens en boucle), si l'arriver compte comme une victoire au bilan, et son lien éventuel avec la Montée.
- **L'Élan** (plan 52 §6) : jauge ou traînée seule, l'Élan gardé ou non d'une terre à l'autre, une créature tombée dans le vide comptée comme éliminée.
- **Les Résurgences** : une par terre, à mi-chemin, ou fondues dans le front. À trancher au prototype.
- **Les durées** (10 min, 20 s de cercle, préavis) : à régler en jouant.
- **Raphaël joue la Barrière et l'Indicible** avant de décider de leur sort.

### 11.7 Ordre de travail

1. ~~L'Élan d'abord~~ : **changé le 17 octobre**, le prototype passe en premier. E2 est livré ; E3 à E6 viennent après le prototype. En attendant E3, le vide se traverse à pied, en payant.
2. **Le prototype des terres** : lots P0 à P3 du §8, recadrés ainsi :
   - **P1** construit la recousure au lieu du pont ;
   - **P2** ajoute le cercle à tenir, les ancres, l'anneau et le vide traversable ;
   - **P3** fait le réveil par trois lumières, l'effondrement au réveil, le gardien qui se lève seul et le multiplicateur de rang.
   La 4e carte et les gardiens dédiés viennent en production, après le prototype.

## 12. Compte rendu P0, l'horloge de run unique — 17 octobre 2026

**Livré :**
- **`RunClock`** (`scripts/Core/RunClock.cs`) : un nœud de `Main`, frère des systèmes de la run. Il compte le temps de jeu depuis le début de la run, pauses exclues, seulement dans l'état Run, donc il s'arrête à la mort. Il est traité avant les autres systèmes à chaque image (priorité -100), pour que tous lisent la même date. La somme se fait en double, pour ne pas dériver sur une run d'une heure.
- **Sept horloges retirées**. Apparitions, Résurgences, micro-événements, fin de partie, Effacement, bilan et quêtes lisent `../RunClock`. Leurs minuteurs propres (cadence des apparitions, durée d'une crise, pas de l'Effacement) restent locaux. La Barrière et les micro-événements, qui lisaient déjà le temps des apparitions, suivent.
- **L'outil de dev qui avance la difficulté** garde son sens : il décale seulement les tables et courbes de spawn (`_developmentTimeOffset`), sans toucher au reste de la run.
- **Bancs** : ceux qui créent ces systèmes seuls posent une horloge à côté et l'avancent avec le pas (temps, Effacement actif, quêtes, phases de run, mouvement, présages).

**Vérifié :**
- build sans avertissement ;
- suites vertes : démarrage, temps, phases de run, quêtes, mouvement seul et intégré, trace de run, Effacement actif ;
- **même calendrier avant et après**, sur deux graines et 10 min de jeu (`tools/measure_run.sh`, un worktree pour la référence) : micro-événements à 74, 168, 240 et 470 s, première élite à 150 s, Résurgence au même moment.
- **Le reste varie autant qu'entre deux passes de la référence**. La mesure n'est pas déterministe au-delà de 30 s : sur la graine 42, les deux passes de référence donnent 665 et 688 éliminations, la passe après P0 601. L'XP varie jusqu'à 25 % d'une passe à l'autre.

**Relecture (`godot-reviewer`)** : rien de bloquant. Corrigé : code mort retiré, commentaire du bilan, date de l'Effacement jamais négative pendant le chargement, contrôle de la pause renforcé (l'horloge elle-même, état Run, se fige). Changement voulu : le temps ne part qu'à l'état Run, à la fin du chargement ; avant, les apparitions et l'Effacement comptaient déjà pendant le chargement.

**Ce que P0 prépare** : les terres pourront pousser une horloge de terre, arrêter le temps pendant la recousure et caler leurs calendriers sur une seule source (P1).

## 13. Découpage de P1, changer de terre — 17 octobre 2026

Étude du 17 octobre (inventaire complet du code) : **la racine de `Main` est elle-même le monde** (`WorldSetup`). Il n'y a pas de nœud « carte » à libérer d'un bloc : changer de terre veut dire libérer une liste précise d'enfants de `Main`, vider les index statiques, et remettre à zéro une quinzaine de systèmes. Le joueur et les systèmes qui portent l'état de run (niveau, armes, objets, Essence, Péril, quêtes, score, horloge) ne bougent pas. Travail sur la branche `terres`, dans un worktree séparé (un autre agent travaille sur `main`).

**Un interrupteur, comme l'Élan** : les terres restent éteintes dans les runs normales (`lands.enabled` à faux dans `data/world/world_gen.json`) tant que le prototype n'est pas jouable de bout en bout. La run actuelle ne change pas pendant P1 à P3.

| Lot | Contenu | Vérification |
|---|---|---|
| **P1a Construire une terre** | `BuildLandAsync(LandSpec)` extrait de l'initialisation : rang, graine, rayons, biome, coefficient sur les comptes de lieux et de coffres. Génération dans un `WorldGenerator` local, échangé sur le fil principal (aujourd'hui affecté depuis le thread, lu en continu par le joueur et les apparitions). TileSet d'origine gardé. Graine par terre : la terre 0 garde la graine de run, les suivantes `RunRandom.SeedFor("land/n")`, sans jamais relancer `RunRandom.Begin`. Une terre avec un seul biome est acceptée (seuil de deux biomes aujourd'hui). Section `lands` dans `world_gen.json` : rayons environ ×0,58 (surface ÷3), comptes ×1/3, biomes tirés sans remise | Terres éteintes : même carte qu'avant (mêmes lignes de génération, même nombre de décors et de coffres), chargement vert. Une petite terre à un biome mesurée (carte, durée de construction) |
| **P1b Vider la carte** | `ClearWorld` : libère par tranches les nœuds de carte (fond, bord, chemins, décalques, occlusion, tronçons de décors, placeurs, lore), vide le sol et les routes, `ObstacleField`, les lieux et les coffres (`PoiContainer`), les créatures (nouveau `EnemyPool.ReturnAll`), les orbes et les bonus au sol. Signaux `LandChanging` et `LandChanged` dans l'EventBus | Après le vidage : tous les index statiques vides (Mémoriaux, coffres, Failles, Ateliers, Puits, stèles, petits lieux, interactifs, obstacles), aucun nœud orphelin |
| **P1c Remettre les systèmes à zéro** | Sur `LandChanged` : Effacement (dont un `Clear` de son index), minimap, brouillard (double abonnement corrigé), faune ; petits lieux, cabines, fausses lumières et occlusion recréés (`ErasureMotes` rafraîchi) ; directeurs des Mémoriaux, Ateliers et Failles ; micro-événement et Barrière interrompus. Joueur ramené au départ (caméra, harpon, mouvement) ; horloge arrêtée pendant la construction | Nouveau banc qui enchaîne trois terres sous le bot invincible : niveau, armes, objets, PV, Essence, Péril, quêtes et score identiques avant et après chaque passage, aucune erreur, lieux de la nouvelle terre comptés |
| **P1d Passage jouable** | Commande de dev « terre suivante » ; écran de chargement par terre ; génération lancée sur le thread pendant le passage ; mesure de durée par terre | Capture d'un passage, durée par terre, banc dense sans régression |
| **P1e La recousure** | La terre suivante monte du Néant autour du joueur, du plus proche au plus lointain, et cache la fin de la construction. Repli : un portail | Capture, à juger par Raphaël |

**Choix pris pour le prototype**, à revoir en jouant :
- **L'Effacement repart de zéro à chaque terre.** P2 le refait de toute façon (date de chute par cellule).
- **Le premier biome** est tiré comme les suivants ; la terre 0 n'est plus la carte en mosaïque d'aujourd'hui, dès que les terres sont allumées.
- **Les comptes de lieux sont divisés par trois** avec la surface : environ 13 coffres, 3 Mémoriaux, 2 Failles, 3 Ateliers, 2 stèles et 3 Puits par terre, à régler en jouant. Le cercle à tenir des Mémoriaux vient en P2.

**Défaut existant relevé en passant** : les chemins de terre n'apparaissent jamais sur la minimap (`WorldSetup.PathCells` est libéré avant que la minimap ne les échantillonne). À corriger avec P1c, qui reprend la minimap.

### Compte rendu P1a — 17 octobre 2026

**Livré (branche `terres`) :**
- **`LandSpec`** : une terre à construire, avec son rang, sa graine, sa taille, son biome et la part de lieux gardée. **`LandsConfig`** : section `lands` de `world_gen.json`, éteinte, allumée pour une run de dev par `--lands`.
- **`WorldSetup` construit la terre en cours.** La graine de carte (`LandSeed`) est celle de la run sur la terre 0, puis `RunRandom.SeedFor("land/n")`. La graine de run (`Seed`) reste celle du bilan et des records. Les petits lieux suivent la graine de terre.
- **Générateur local**, échangé sur le fil principal après l'attente : plus jamais de carte à moitié construite lue par le joueur ou les apparitions.
- **TileSet d'origine gardé** pour les terres suivantes ; une terre d'un seul biome est acceptée.
- **Comptes réduits** par `SitePlacer.Scaled`, arrondis sans perte d'un groupe à l'autre : 13 coffres, 5 Mémoriaux, 2 Failles, 3 Ateliers, 3 stèles et 2 Puits par terre.
- **Biomes tirés sans remise** sur la run (ordre mélangé par la graine de run).

**Vérifié :**
- **terres éteintes : la génération est identique à la ligne près** (40 lignes de génération, coffres, lieux et décors) sur les graines 7 et 42, comparée à la mesure d'après P0 ;
- **terres allumées** : 116 × 232 cellules, un seul biome (champs sur la graine 7, marais sur la 42), construction du monde en 1,8 à 2,1 s contre 5 s pour la grande carte, sous la même charge ; 9 301 décors aux champs, 3 667 au marais ;
- capture de la carte d'une terre de champs, regardée ;
- suites vertes : démarrage, chargement, catalogues, cartographie, petits lieux, mouvement intégré.

**Relevé pour la production** : une terre de champs seule répète beaucoup ses fermes (une par région). Les terres d'un seul biome demanderont des pièces plus variées (§5).

### Compte rendu P1b et P1c — 17 octobre 2026

Livrés ensemble : le vidage seul fait planter les systèmes qui gardent la carte (petits lieux, minimap), il n'a de sens qu'avec leur remise à zéro.

**Livré (branche `terres`) :**
- **`LandTransition`** (`scripts/World/LandTransition.cs`) orchestre le passage, jeu en pause comme au chargement :
  1. signal `LandChanging` ;
  2. vidage de la carte, puis construction de la terre suivante par le même chemin que le premier chargement (`WorldSetup.BuildLandAsync`) ;
  3. petits lieux, cabines et fausses lumières refaits ;
  4. joueur posé au départ (`Player.ArriveOnLand`) ;
  5. signal `LandChanged`.

  Il pose aussi ces détails au premier chargement, à la place du bootstrap.
- **`WorldSetup.ClearWorldAsync`** :
  - libère les nœuds de carte, qu'il tient en liste ;
  - vide les décors, les lieux et les objets au sol, le sol et les obstacles ;
  - retire les créatures sur-le-champ (`Enemy.Withdraw`, sans butin ni élimination) ;
  - ramène au joueur les orbes d'XP restées au sol (`XpOrb.CollectAll`).
- **Réactions au départ** : l'Effacement repart de zéro ; la minimap oublie sa carte ; une collecte d'éclats en cours s'arrête ; les objets en vente des Ateliers disparaissent ; un micro-événement en cours finit échoué ; une Barrière levée reste derrière ; les bonus au sol disparaissent (ceux déjà pris continuent) ; la faune s'efface.
- **Ce qui suit la run sans rien perdre** :
  - le brouillard garde l'Oubli du regard et son abonnement ;
  - les bonus de vitesse des abribus courent jusqu'au bout ;
  - les particules d'oubli relisent l'index d'occlusion refait.
- **Défauts corrigés en route :**
  - une terre d'un seul biome laissait des trous où un terrain n'avait pas de tuile (32 cellules de béton au marais) : ce terrain prend maintenant l'herbe du biome ;
  - les chemins de terre n'apparaissaient jamais sur la minimap (défaut existant) : leur emprise est gardée après la construction. C'est le seul changement visible en run normale.
- **Banc `tools/test_lands.sh`** (suite `lands`) : trois terres enchaînées par le vrai passage sur la vraie scène, foule et orbe posées avant chacun.

**Vérifié :**
- build sans avertissement ; banc des terres vert et sans erreur au journal, sur carrière, marais puis champs ;
- à chaque passage :
  - rien ne reste de la terre quittée (5 Mémoriaux et 13 coffres comptés, pas 10 ni 26) ;
  - aucune créature ancienne ; l'orbe revient au joueur ;
  - sol sans trou ; joueur au départ ;
  - niveau, PV, armes, Essence, Péril, éliminations et horloge identiques ;
  - puis 4 s de jeu sans erreur, l'horloge repart ;
- un passage dure 1,4 à 1,9 s, machine calme ;
- validation large lancée au moment du commit, à la demande de Raphaël ; son résultat est consigné au lot suivant.

**Suite : P1d**, une commande de dev « terre suivante » et un écran pendant le passage, pour essayer en jouant.

**Relecture de P1b et P1c (`godot-reviewer`), corrigé le même jour :**
- les orbes ramassées au passage pouvaient faire monter de niveau pendant le passage, et sa fin levait la pause sous l'écran de choix. Elles sont maintenant ramassées après la reprise, et le banc couvre le cas (5 000 XP au sol) ;
- le saut au départ de la terre suivante comptait dans le trajet parcouru (bilan, quête de distance) et dans les fils de marche des Écussons. Les deux se recalent sur `LandChanged`, et le cap de la Barrière oublie le trajet d'avant ;
- passage protégé par un `try/finally` (pause et état rendus même sur erreur) ; deux désabonnements hors de leur garde ; brouillard qui repart d'un relevé complet ; restes de la collecte d'éclats remis à zéro.

**Validation large** (18 suites) : 16 vertes au premier passage. **Butin d'objets** : son banc créait l'Effacement seul, sans horloge à côté. C'est un oubli du lot P0, qui est sur `main`, corrigé ici et à reporter sur `main` à la fusion. **Mouvement** : 5 contrôles de vitesse ×1,08 juste après le scénario du Facteur, sous une forte charge machine. Non reproduit seul, deux fois vert depuis.

### Compte rendu P1d — 17 octobre 2026

**Livré (branche `terres`) :**
- **Commande de dev `land next`** (ou `land N`), et bouton « Terre suivante » dans l'atelier de test (F4), section « Terres ». Documentée dans `doc/DEV-MODE.md`. Pour jouer en petites terres d'un biome : `tools/run_dev.sh -- --lands`.
- **Écran pendant le passage** : l'écran de chargement du jeu couvre le passage, avec sa marche, ses phrases et sa progression (étapes de la construction), puis son fondu de sortie. Il tient la place de la recousure (P1e).
- **Outil de mesure** : `--land-every S` change de terre toutes les S secondes de jeu, et garde une image de chaque terre neuve (`land-N.png`) pendant une capture. La sonde de rythme ne compte plus le saut au départ comme un trajet.

**Vérifié :**
- banc des terres vert, dernier passage lancé par la commande de dev ; suites du mode dev, des outils de dev et de l'export de release vertes (les outils restent absents des exports) ;
- capture d'une run qui change de terre toutes les 25 s : marais en 1,6 s, carrière en 1,5 s, sans erreur au journal.

**Pas vérifié à l'image** : toutes les images de la capture, y compris celle prise à 30 s avant tout passage, montrent la même vue figée de l'écran de chargement initial. macOS ne dessinait plus la fenêtre de capture, masquée pendant le travail sur la machine. La capture de l'esquive parfaite (lot E2), prise sur `main` avant ce chantier, avait le même défaut. C'est à refaire fenêtre visible, ou à voir en jouant.

**Banc dense** : non relancé, aucune boucle par image n'a changé (le passage n'existe qu'à la demande, et les nouvelles réactions ne tournent qu'au changement de terre).

## 14. Découpage de P2, la terre se défait — 17 octobre 2026

P1e (la recousure) passe après P2. Elle est purement visuelle, avec un repli prévu (le portail), et ne peut pas être jugée à l'image tant que la fenêtre de capture n'est pas dessinée. P2 est le cœur du prototype.

**Choix pris pour le prototype, à revoir en jouant :**
- **Le cœur est le centre de la terre, là où le joueur arrive.** On part du lieu sûr, on va chercher Mémoriaux et butin vers les bords qui tombent, et on revient au cœur réveiller le gardien. Le gardien endormi s'y voit dès l'arrivée (P3).
- **Le front ne vaut que sur les terres allumées.** La grande carte garde son Effacement actuel tant que les terres sont éteintes.
- **Rythme de départ** : premier sol qui tombe vers 1 min ; la terre réduite à son cœur vers 12 min si on la laisse filer ; chaque état de préavis (fragile, effiloché, effacé) dure 25 s. Tout est réglé en données.

| Lot | Contenu | Vérification |
|---|---|---|
| **P2a Le front** | Date de chute par cellule, calculée à la construction de la terre : distance au cœur, bruit en langues et en baies, cœur qui ne tombe jamais. La mémoire d'une cellule devient le temps qui lui reste avant sa chute, donc les mêmes états et le même rendu qu'aujourd'hui. La présence du joueur retarde la chute autour de lui. Toutes les cellules de la terre suivies (quelques milliers) | Banc : états dans l'ordre et à l'heure, cœur intact, présence qui retarde ; mesure de la terre restante par minute ; capture du front |
| **P2b Les Mémoriaux à tenir** | Sur les terres, raviver un Mémorial, c'est tenir son cercle environ 20 s (au lieu de rassembler ses éclats) ; une fois ravivé, sa zone est ancrée et ne tombe plus ; chaque Mémorial ravivé allume une lumière comptée pour le gardien (P3) ; un Mémorial atteint par le front avant d'être ravivé est perdu | Banc : cercle tenu, quitté, ravivé ; zone ancrée qui ne tombe pas ; compteur de lumières |
| **P2c Lire la terre** | Anneau de terre restante autour de la minimap, avec le repère du réveil forcé ; la minimap montre le front et ce qui tombe dans la minute | Capture, à juger par Raphaël |
| **P2d Le vide** | Marcher dans le Néant draine l'Essence, puis les PV quand elle est épuisée (au lieu des seuls PV aujourd'hui) | Banc : drain d'Essence puis de PV |

### Compte rendu P2a, le front — 17 octobre 2026

**Livré (branche `terres`, terres allumées seulement) :**
- **`LandFront`** (`scripts/World/LandFront.cs`) :
  - chaque cellule d'Effacement de la terre reçoit à la construction sa date de chute, tirée de sa distance au cœur et d'un bruit (langues et baies) ;
  - sa mémoire est le temps qui lui reste, rapporté au préavis : les mêmes états, le même rendu du sol et les mêmes signaux qu'aujourd'hui ;
  - le cœur ne tombe jamais.
- **Une part égale par minute.** La date suit le carré de la distance : la terre perd une surface régulière, au lieu de fondre vite au début puis de moins en moins. La tension monte donc jusqu'au bout, comme voulu au §185. C'est un choix pris en mesurant, à revoir en jouant.
- **L'Effacement suit le front** sur une terre :
  - toutes ses cellules sont suivies, environ 5 100 ;
  - l'horloge du front avance avec le jeu, plus vite sous les Oublis, aux niveaux durs et pendant les Résurgences ;
  - la présence du joueur retarde la chute autour de lui ;
  - un lieu ravivé ou un coffre ouvert repousse la chute alentour, sans jamais l'avancer.

  Aucun Effacement global : le late game ne vient pas de la chute des terres.
- **Réglages** : section `land_front` de `data/scaling/erasure.json`, contrôlée en entier. Première chute à 60 s, cœur atteint à 720 s, préavis de 100 s (25 s par état), cœur de 12 % du rayon, ±90 s de bruit.

**Vérifié :**
- banc des terres, 39 contrôles verts :
  - règles du front : états dans l'ordre et à l'heure, présence qui repousse, chute dans le Néant, stabilisation qui n'avance jamais, ancre et cœur intacts, terre restante ;
  - sur la vraie terre : 5 103 cellules, cœur intact, 182 cellules du bord déjà fragiles à l'arrivée ;
  - front neuf à chaque terre ;
- Effacement actif et petits lieux verts ;
- **terre restante mesurée** (bot nomade invincible, 13 min, graines 7 et 42, courbes quasi identiques) :

  | min | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | 11 | 12 |
  |---|---|---|---|---|---|---|---|---|---|---|---|---|
  | debout | 99 % | 94 % | 85 % | 76 % | 67 % | 57 % | 43 % | 33 % | 24 % | 15 % | 6 % | 1,5 % (cœur) |

  Avec une date linéaire en distance, la moitié de la terre était tombée dès 5 min et 10 % restaient à 9 min.

**Pas vérifié à l'image** : le front se dessine avec le rendu existant des états d'Effacement, mais aucune capture n'a pu être prise (fenêtre de capture non dessinée, voir P1d).

### Compte rendu P2b, les Mémoriaux à tenir — 17 octobre 2026

**Livré (branche `terres`, terres allumées seulement) :**
- **Le cercle à tenir.** Activer un Mémorial fait apparaître un cercle de 220 px au sol (`MemorialCircle`), qui se remplit du centre vers le bord tant que le joueur s'y trouve. Il est assez large pour y courir.
  - Hors du cercle, le temps tenu redescend à moitié de la vitesse de montée.
  - Au-delà de 560 px, la tentative s'arrête.
  - Pendant le maintien, la foule est 1,6 fois plus dense.
  - 20 s tenues ravivent le Mémorial par le réveil actuel (animation, bénédictions).
  - Un Mémorial que le Néant atteint pendant le maintien est perdu, comme avant.
- **L'ancre** : sur une terre, un Mémorial ravivé fixe les cellules à 4 cellules d'Effacement de lui (environ 512 px). Elles ne tomberont plus ; le sol déjà tombé reste tombé.
- **Les lumières** : `MemorialDirector.LitThisLand` compte les Mémoriaux ravivés sur la terre en cours, remis à zéro à chaque terre. Le gardien les lira (P3).
- **Réglages** : `memorial.circle` dans `data/world/landmarks.json`, `land_front.anchor_radius_cells` dans `data/scaling/erasure.json`, tous contrôlés.
- **Défaut évité en route** : un passage de terre pendant l'animation de réveil d'un lieu libérait ce lieu en pleine animation. L'animation s'arrête maintenant au passage, sans rappel, et la caméra revient sur le joueur.

**Vérifié :**
- banc des terres vert : le cercle apparaît et resserre la foule ; s'éloigner arrête la tentative et rend la foule normale ; 20 s tenues ravivent le Mémorial, allument une lumière, et la bénédiction se choisit ; quand tout le reste est tombé, sa zone tient (terre restante : 2 %, cœur compris) ;
- suites de démarrage, catalogues, petits lieux, Effacement actif et écran de choix vertes.

**Pas encore** : aucune indication à l'écran du temps tenu, à part le remplissage du cercle, ni de son et de tic-tac du maintien. À juger en jouant. Le compte à rebours des éclats reste propre à la grande carte.

### Compte rendu P2d, le vide — 17 octobre 2026

**Livré (branche `terres`, terres allumées seulement)** : le Néant d'une terre prend d'abord l'Essence, 6 par seconde par tranches entières, puis les PV pour la part qu'elle ne couvre plus. Traverser un trou court reste bon marché ; une longue traversée, ou une traversée sans Essence, coûte cher. Réglage `land_front.void_essence_per_second`. La grande carte garde le seul coût en PV.

**Vérifié** : banc des terres vert deux fois de suite. Sur une cellule tombée, l'Essence passe de 40 à 28 en 2 s et les PV ne bougent pas ; sans Essence, les PV passent de 70 à 63,7 en 2 s.

**Banc rendu robuste** en route : le bot choisit les cartes d'un écran de niveau ou de choix comme un joueur pressé ; une créature déjà mourante au passage n'est plus comptée comme restée (elle finit sa mort puis revient au pool) ; les orbes des créatures tuées juste avant le passage reviennent aussi au joueur. Le niveau peut donc monter au passage, et c'est voulu.

### Compte rendu P3, le gardien — 17 octobre 2026

**Livré (branche `terres`, terres allumées seulement) :**
- **Le repaire** (`GuardianLair`), au cœur, un peu au nord du départ : une masse endormie sous une colonne de lumière, avec une rangée de lumières qui s'allument une à une avec les Mémoriaux ravivés de la terre. Une fois les trois allumées, on le touche (« Réveiller le gardien ») pour le réveiller. Visuel provisoire.
- **Le gardien** (`GuardianDirector`) : pour le prototype, un Souverain de la terre (variante champion), PV ×3, escorté de 6 créatures, avec sa barre de boss. Chaque lumière en plus ajoute 25 % de PV et un coffre rare à sa récompense ; il laisse aussi un coffre épique, en plus de celui de tout Souverain.
- **Le gardien qui se lève seul** : si la terre est réduite à son cœur (horloge du front au cœur atteint), il se lève sans qu'on l'ait réveillé, enragé (PV ×1,5, dégâts ×1,3), au milieu de la foule.
- **Le réveil** fait tomber d'un coup tout le sol qui n'est ni le cœur ni ancré par un Mémorial ravivé (`ErasureManager.CollapseUnanchored`), et fige le front jusqu'à la mort du gardien.
- **Le passage** : 4 s après la mort du gardien, la terre suivante se recoud (`LandTransition`). Chaque terre franchie ajoute 3 points de Péril, la montée par rang, visible au HUD.
- **Sur les terres**, la Barrière n'existe plus et l'Indicible ne vient plus à l'heure : le gardien de chaque terre les remplace.
- **Réglages** : `data/world/guardian.json`, contrôlé en entier.

**Défauts corrigés en route :**
- **Une terre toute urbaine plaçait 0 Mémorial et 1 coffre** : le dégagement exigé autour des immeubles (176 px) est impossible dans des rues étroites. Un second essai, seulement quand le premier échoue, n'évite que les immeubles qui toucheraient le lieu. Il reste un coffre commun sans place sur 13 en ville. La grande carte reste identique à la ligne près (graines 7 et 42).
- **Fuite intermittente à la fermeture** (965 formes physiques) : les créatures rendues au pool attendaient une libération différée, qui ne s'exécute plus quand le jeu quitte. Les passages de terre remplissent ce pool. Elles sont maintenant libérées tout de suite.

**Vérifié :**
- banc des terres vert trois fois d'affilée :
  - repaire fermé avec 1 lumière, ouvert avec toutes ;
  - réveil : terre debout 6 % → 2 %, front figé ;
  - gardien tué : au moins deux coffres ;
  - terre suivante recousue : Péril 0 → 3 ;
  - sur cette terre laissée filer, le gardien se lève seul, enragé ;
- suites de démarrage, chargement, catalogues, petits lieux, Effacement actif, phases de run, Indicible, parties de boss, capacités des créatures et mouvement intégré vertes.

**Mesure au bot, sans conclusion sur le gardien** : run de 40 min, terres allumées, graines 7 et 42. Le gardien de la première terre se lève seul, enragé (16 060 PV et 3 267 PV), et le bot ne le tue jamais. Le bot ne joue pas la boucle des terres : il ne ravive pas de Mémorial, ne revient pas au cœur, et quand la terre se réduit à son cœur, il erre dans le Néant (6 334 Essence drainées sur la graine 7). **Pour mesurer le rythme d'une run en terres, il faut un bot qui joue la boucle** : aller aux Mémoriaux, tenir le cercle, revenir au cœur, réveiller et combattre le gardien.

**Relecture (`godot-reviewer`)** : rien de bloquant. Corrigé :
- l'effondrement ne traite plus que les cellules qui tombent. Pire cas mesuré, une terre entière : 5 087 cellules en 7,3 ms dans la même image, sous charge moyenne ;
- le passage après le gardien est lancé une seule fois, journalisé s'il échoue, et jamais si la run est finie ;
- pendant le combat, un coffre ouvert ne relève plus le sol tombé (arène vraiment figée) ;
- le repaire est posé dans la zone dégagée du départ (160 px à l'est), jamais dans un immeuble.

Relevé : les quêtes « battre la Barrière » et « battre l'Indicible » ne sont pas atteignables sur les terres. Ce sera à revoir avec le catalogue des quêtes quand les terres seront allumées pour tous.

**Reste du prototype** : P2c (anneau de terre restante, minimap du front), P1e (recousure), le bot des terres, puis la séance de jeu de Raphaël. La charte des gardiens (attaques qui brisent le sol, un vrai corps par terre) attend la production.
