# Plan 51 — La run en terres

15 octobre 2026. Ouvert sur le manque relevé par le premier testeur ([§184](DECISIONS.md)) : la run n'a pas d'objectif, et le joueur se sent perdu vers 15 à 20 min. **Direction retenue par Raphaël le jour même ([§185](DECISIONS.md)) : une run traverse trois terres, chacune une carte plus petite qui s'efface de ses bords vers son cœur, avec un boss à la fin.** Rien n'est implémenté. Les propositions ci-dessous sont à trancher (§9).

La première version de ce plan, « la Descente » (une seule carte en deux paliers, collée au lore), est remplacée. Raphaël a demandé de partir du jeu, pas du lore : « la boucle de gameplay s'était plus adaptée en fonction du lore, alors que normalement ça aurait dû être l'inverse ».

## 0. En bref

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

1. **Le vide** : mur, traversée payée en Essence puis en PV (recommandé), ponts de mémoire à créer, ou chute ?
2. **Si on n'appelle pas le gardien** : il se lève seul, renforcé, sur l'îlot qui reste (recommandé), ou rien ne se passe et la foule finit le travail ?
3. **Le passage** : deux ponts vers deux terres visibles (recommandé), ou un portail ?
4. **Après la troisième terre** : victoire ou boucle au choix (recommandé), ou late game sur place ?
5. **Pendant le combat contre le gardien** : front figé, ou arène qui rétrécit ? À essayer au prototype, ou décidé maintenant ?
6. **Feu vert pour le prototype P0 à P3** (3 à 4 semaines, Souverains en gardiens provisoires), avant toute production de terres et de boss ?
