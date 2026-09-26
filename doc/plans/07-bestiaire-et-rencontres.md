# Plan 07 — Bestiaire distinctif et rencontres variées

Statut : **ajout de créatures demandé ; familles et boss proposés ; menaces à distance originales demandées le 23 septembre** · Priorité : **P1 pour les rôles du début de run** (menace, 03 A2), P2 pour le reste · Dépendances : contrôle 01, tempo 03, référence 08.
Références : V2 §8/10/14 ; GDD §4.9 hors V1 ; Bible §6.2 ; [dossier](README.md).

## 1. Objectif

Augmenter les situations de combat et les réponses demandées au joueur. Une nouvelle créature doit apporter une décision reconnaissable : changer de trajectoire, prioriser une cible, attendre une fenêtre ou s'exposer pour une récompense.

Raphaël maintient la demande de nouvelles créatures : trois familles candidates sont incluses ci-dessous. Leur réalisation se fait par lots, sans que l’audit de l’existant remplace cette extension. Le nombre de fiches JSON ne constitue pas une mesure de diversité.

## 2. Phase 0 — Inventaire existant

Sources : [données ennemis](../../data/enemies), [Enemy.cs](../../scripts/Combat/Enemy.cs), [SpawnManager.cs](../../scripts/Spawn/SpawnManager.cs), [EnemyPool.cs](../../scripts/Spawn/EnemyPool.cs), biomes et `spawn_flow.json`.

| Ennemi / famille | Rôle actuel à confirmer en jeu | Travail proposé |
|---|---|---|
| shade | Masse fragile rapide | Référence de groupe faible |
| shadow_crawler | Poursuite | Distinguer de shade par trajectoire/rythme |
| rodeur | Pression de contact lente | Clarifier zone menaçante |
| treant_corrompu | Résistance lente | Donner un comportement identifiable |
| charognard | Meute | Rendre l'effet de groupe perceptible |
| fading_spitter | Harcèlement à distance | Projectile et annonce lisibles |
| wailing_sentinel | Contrôle stationnaire | Expliquer portée et fenêtre sûre |
| void_brute | Charge | Télégraphie et récupération |
| hurleur | Renforts | Cible prioritaire reconnaissable |
| rampant | Surgissement | Indice au sol puis délai de réponse |
| tisseuse | Zone/ralentissement | Éviter pièges sans issue |
| colosse_urban / forest / swamp | Trois variantes d'un comportement | Un motif distinct par biome si utile |
| indicible | Boss dédié | Parcours et climax traités avec 03 |

15 définitions, dont trois Colosses et un boss. Aucun fichier dédié Résurgent identifié : les ennemis exclusifs de Résurgence restent un contenu à définir.

APIs/patterns : `Enemy.Initialize(EnemyData data, float hpScale, float dmgScale)`, routage par comportement dans Enemy vers 439–457/878, activation/réinitialisation via EnemyPool, composition des pools dans SpawnManager vers 417. Les noms DayEnemyPool/NightEnemyPool sont legacy ; ils ne signifient pas qu'il faut restaurer le jour/nuit.

### Retour de Raphaël du 23 septembre et constats

> « Le début est encore trop simple : on passe du niveau 1 à 5 en moins de 30 secondes sans aucun souci. Peut-être que c'est le bestiaire le problème (des monstres trop passifs, trop de monstres au CAC). Il faudrait sûrement diversifier les attaques des monstres et en avoir plus à distance, en essayant de faire en sorte qu'elles soient originales, pas que des projectiles simples. »

Constats vérifiés dans les données le 23 septembre :

- **Aucun ennemi n'est plus rapide que le joueur.** Les ennemis vont de 25 à 100 px/s (Ombre 100, Charognard 85, Rôdeur 30), contre 180 à 240 px/s pour les personnages. Reculer en ligne droite neutralise donc toute la mêlée.
- **Un seul tireur mobile, le Cracheur Pâli**, avec un projectile simple. Il n'apparaît que dans les pools des Ruines urbaines et du Marais. La Sentinelle est immobile.
- **Les pools de début, hérités du nom `day_enemy_pool`, sont presque entièrement au corps à corps.** Par exemple, la Carrière Effondrée, biome joué par Raphaël le 23 septembre, n'aligne que Rôdeur, Brute et Rampant.
- **Le correctif de la flèche du 23 septembre réduit les dégâts de zone involontaires de l'arc**, mais ne change pas ce déséquilibre de rôles.

Conséquence : le lot C de ce plan (compositions) et les nouvelles menaces à distance passent **avant** la liste de familles du lot D, pour servir l'essai « menace » du plan 03 A2.

### Menaces à distance originales proposées

Critère : chaque attaque impose une **décision de trajectoire** différente d'« esquiver une balle ». Aucune n'est un projectile en ligne droite. Toutes s'annoncent au sol ou par la silhouette avant de frapper. Noms et valeurs sont à valider ; les paramètres iront en JSON.

| Proposition | Attaque | Ce qu'elle casse | Réponse attendue | Pistes de paramètres |
|---|---|---|---|---|
| **Le Présage** | Trace au sol un cercle à l'endroit où le joueur **sera** dans 1 s, extrapolé depuis sa vitesse, puis y fait tomber un effondrement | La fuite en ligne droite | Changer de direction ou ralentir au bon moment | Délai 0,9–1,2 s, rayon 40 px, extrapolation plafonnée |
| **L'Arpenteur** | Plante trois ou quatre jalons autour du joueur, puis tend un cordeau entre eux : un périmètre qui se referme | Le kiting en cercle | Sortir de l'enclos avant la fermeture, ou détruire un jalon | Délai de fermeture 1,5 s, jalons à 4 PV |
| **Le Carillon** | Émet des ondes circulaires lentes avec une brèche orientée | L'attente à distance | Passer par la brèche en se rapprochant | Vitesse d'onde, largeur de brèche, 3 ondes par cycle |
| **L'Effaceur** | Projette une tache de Néant rampante qui gomme temporairement le sol (zone infranchissable 3 s) | Les couloirs de fuite | Anticiper son trajet, contourner | Vitesse de la tache, durée, plafond de taches simultanées |
| **Le Miroitier** | Lance des éclats de verre qui ricochent sur les murs et les props | La couverture derrière les obstacles | Lire les angles, se placer hors des lignes de rebond | Deux rebonds maximum, trajectoire prévisualisée |
| **L'Avaleur de mots** | Absorbe les projectiles du joueur dans un cône frontal puis en recrache une partie | Les builds 100 % distance | Le contourner ou le frapper de dos | Cône de 60°, taux de restitution |

**Rôles de mêlée anti-fuite** (sans rendre toute la mêlée plus rapide que le joueur) :
- **Bond annoncé du Charognard :** accroupissement visible 0,4 s, puis bond de 120 px plus rapide que le joueur. Il punit la fuite trop proche et s'évite par un pas de côté.
- **Interception :** certains Rôdeurs visent la position anticipée du joueur au lieu de sa position actuelle. Ils coupent la route au lieu de suivre la file.

**Priorité de prototypage recommandée :**
1. Présage et bond du Charognard : les moins coûteux, et ils attaquent directement la fuite en ligne droite.
2. Arpenteur.
3. Effaceur, qui dépend des règles de terrain du plan 10.

Carillon, Miroitier et Avaleur viennent ensuite, selon les essais. Chaque prototype suit la fiche obligatoire ci-dessous et passe le test « je comprends, je peux réagir, j'apprends » avant d'entrer dans les pools.

**Pools du début :** introduire au moins un rôle à distance original dans **chaque** biome dès la première minute. Utiliser la table temps actif/phase → rôles admissibles, proposée plus bas pour l'introduction progressive. Elle remplace la dépendance aux noms legacy `day_enemy_pool` et `night_enemy_pool`.

### Premier essai menace — 23 septembre 2026

Raphaël choisit de commencer par le Présage et le bond du Charognard.

**Architecture :** capacités composées, décrites par un bloc `abilities` du JSON ennemi et lues par `EnemyDataLoader`. Chacune implémente `IEnemyAbility` (`scripts/Combat/Abilities/`). Une instance est créée une fois par ennemi, puis reconfigurée à chaque sortie du pool. `Enemy` n'a reçu que des points d'accroche :
- configuration à l'initialisation ;
- traitement avant le mouvement ;
- annulation à la mort, au retour au pool, à la sortie de l'arbre et au passage en traitement simplifié au-delà de 600 px.

Les annonces au sol (`GroundTelegraph`) sont réutilisées et dessinées sans allocation. Le remplissage progressif indique le délai sans dépendre de la seule couleur.

**Présage** ([presage.json](../../data/enemies/presage.json)) : nouvel ennemi à distance, visuel provisoire (losange vert).
- Il s'arrête à 300 px et, toutes les 2,8 s, marque un cercle de 42 px là où le joueur **sera** dans 1 s (extrapolation plafonnée à 240 px). L'effondrement tombe 1 s plus tard.
- Il reste immobile pendant l'incantation, ce qui donne une fenêtre pour le frapper.
- Trois marques au plus en même temps, tous Présages confondus. Sa mort annule sa marque.
- Il est ajouté une fois à chaque pool de début et de crise des cinq biomes.

**Bond du Charognard** ([charognard.json](../../data/enemies/charognard.json)) :
- Entre 50 et 150 px, il s'accroupit 0,4 s en montrant sa trajectoire au sol (direction verrouillée au début de l'annonce).
- Il bondit ensuite de 120 px en 0,18 s (667 px/s, soit bien plus vite que le joueur), avec des dégâts ×1,2.
- Il récupère 0,35 s, immobile ; recharge 3,5 s.

**Sons :** provisoires, réutilisés depuis les créatures existantes et déclarés en JSON.

**Vérifications :**
- [`tools/test_enemy_abilities.sh`](../../tools/test_enemy_abilities.sh) : **15 assertions réussies**, sur vrais Player/Enemy. Elles couvrent :
  - la marque à la position anticipée ;
  - la fuite en ligne droite touchée ;
  - l'évitement par changement de direction ;
  - le joueur immobile touché ;
  - le plafond de trois marques ;
  - l'annulation à la mort et la place libérée ;
  - l'absence de projectile classique pendant la recharge du Présage (bug trouvé en relecture et corrigé) ;
  - l'annonce du bond, sa vitesse et sa distance (122 px) ;
  - la récupération immobile ;
  - l'évitement du bond par un pas de côté.
- Bancs de déplacement et d'intégration verts ; smoke 600 frames vert ; build zéro warning.

**Retour de Raphaël (23 septembre) :** Présage « bonne idée » mais attaque jamais vue ; bond du Charognard « bonne idée ».
- Cause de l'attaque invisible : la marque était dessinée à z −2, sous la `TileMapLayer` du sol (z 0). Elle est désormais en z absolu 1. Une capture rendue de la vraie `Main` confirme la marque visible (banc `RunObservation --capture-abilities`).
- Le corps provisoire du Présage devient un losange lilas plus grand, hors palette végétale.
- Les autres décors « au sol » en z négatif (anneaux de garde des POI, contours de coffres, lueurs d'`InteractableAura`) sont probablement masqués de la même façon. C'est à vérifier.

**Point ouvert découvert par le banc :**
- Quand l'arc automatique du joueur reste actif et que l'ennemi survit aux flèches, le bond est raccourci de 122 à 56 px, avec un recul de position. Le banc coupe désormais l'arme pour isoler les capacités.
- Piste : les flèches du joueur (`Projectile.tscn`) occupent la couche physique 4, celle des obstacles, que les ennemis prennent en collision.
- La cause n'est pas établie. À vérifier en jeu, car cela pourrait aussi gêner les trajectoires ennemies en combat dense.

**Limites et suite :**
- Aucun essai en jeu n'a encore eu lieu. La recette avec Raphaël doit juger le ressenti, la lisibilité, la fréquence et la difficulté des premières minutes.
- Le Présage n'avait ni sprite ni animation dédiée : pilote procédural livré le 24 septembre ([plan 08](08-direction-artistique.md#pilote-ennemis--24-septembre-2026)), recette en jeu à faire.
- Les valeurs sont des points de départ. Si la menace reste insuffisante, l'étape suivante du plan 03 A2 est l'essai XP (+25 %).

## 3. Fiche obligatoire par rôle

Silhouette et motif ; habitat/phase ; anticipation ; attaque ; fenêtre de réponse ; récupération ; vulnérabilité ; interaction avec ralentissement/recul ; récompense ; associations autorisées ; plafond simultané ; paramètres JSON ; animations/sons nécessaires.

Pour chaque menace, montrer une séquence « je comprends → je peux réagir → j'apprends ». La transparence liée à l'Effacement ne doit pas supprimer l'indice de danger.

## 4. Propositions de rencontres

| Situation | Composition initiale | Décision attendue | Limite à tester |
|---|---|---|---|
| Apprentissage | Masse fragile puis un tireur | Quitter la trajectoire des tirs | Pas d'encerclement complet |
| Rupture | Poursuivants + une Brute | Anticiper et laisser passer la charge | Délai et voie de fuite |
| Priorité | Groupe + Hurleur | S'exposer pour supprimer les renforts | Renforts plafonnés |
| Détour | Tisseuse près d'un objectif | Contourner ou engager | Chemin alternatif accessible |
| Résurgence | Pression de masse + menace exclusive | Rester mobile sous un danger nouveau | Lisibilité avec Effacement |
| Récompense | Ennemi fort gardant un accès utile | Arbitrer risque et valeur | Gain proportionné et visible |

Les quantités exactes et délais seront testés dans le système de spawn existant ; ne pas créer un second directeur concurrent.

### Trois nouvelles familles candidates

- **Le Porte-Nom**, soutien : renforce temporairement quelques alliés reliés visuellement ; l'éliminer rompt les liens. Paramètres : rayon, maximum de liens, intensité, délai de réattribution. Il protège des ennemis déjà présents, tandis que le Hurleur appelle des renforts : cette différence doit se voir en jeu.
- **Le Rémanent**, Résurgent : annonce une ligne d'attaque, se recompose, puis la traverse ; vulnérable après son passage. Paramètres : avertissement, distance, vitesse, récupération et maximum simultané. L'annonce demeure visible même lorsque son corps s'efface.

- **Le Glaneur**, cible mobile de récompense : transporte un amas d’objets oubliés et s’éloigne vers un passage dangereux ; décider de le poursuivre expose à d’autres menaces. Son butin est créé avec lui, il ne vole pas arbitrairement les possessions du joueur. Paramètres : distance de fuite, trajet, délai, butin, annonce de départ.

Ces noms et comportements sont des propositions à valider. Prototyper successivement soutien, Résurgent et porteur mobile ; conserver des silhouettes distinctes avant l’art final.

### Boss de familles proposés

L’idée « une version boss de chaque type » est à examiner comme une gamme, pas comme quinze sprites agrandis. Recommandation : un boss par famille mécanique retenue à terme, en commençant par deux. Les Colosses et l’Indicible restent des rencontres distinctes à réconcilier avec le calendrier 03.

| Famille | Transformation candidate | Fenêtre de réponse |
|---|---|---|
| Chargeurs | Brute : deux charges annoncées avec changement d’angle, puis fatigue | Esquive et punition après la seconde |
| Tisseuses | Grand Nœud : lignes de toile successives et corridors temporaires | Lire le corridor et changer de route |
| Meutes | Meneur : coordonne un arc de poursuite, perd ses bonus isolé | Couper la meute plutôt que subir une masse |
| Tireurs/sentinelles | Gardien : salves orientées alternant secteurs sûrs | Placement puis approche |
| Hurleurs/soutiens | Chœur : renforts interrompables, points faibles exposés pendant l’appel | Prioriser et interrompre |
| Fouisseurs | Profond : plusieurs indices au sol, attaque finale identifiable | Observer le vrai signal |
| Poursuivants lourds | Masse d’oubli : portée annoncée et cycle lent de récupération | Éviter puis revenir |
| Résurgents | Rémanent majeur : passages successifs et silhouette recomposée | Lire le rythme, pas deviner une hitbox invisible |

**Recette commune :** comportement inédit, silhouette retravaillée à densité pixel égale, annonce, phases bornées, récompense garantie, succès de quête et paramètres JSON. Loot dépend de la politique d’éligibilité de 05. Tester Brute et Tisseuse avant d’étendre aux autres familles ; la généralisation reste une décision après essais.

### « Les mobs avancent successivement » : deux sujets séparés

Clarification demandée et encore en attente au moment de rédaction. Ne pas présenter une des interprétations comme déjà validée.

- **Si le problème est une arrivée en file :** ProcessMelee poursuit directement le joueur ; le masque actuel ne fait pas collision entre ennemis. Filmer les trajectoires avant d’accuser les collisions. Prototyper une interception légère sur certains rôles, une séparation locale bornée et des approches par côtés, sans encercler instantanément le spawn. Mesurer coût avec cache spatial et 100+ ennemis.
- **Si la demande est une introduction progressive des types :** PickEnemyForPosition sélectionne selon biome/phase, pas selon une chronologie fine des familles. Ajouter une table données temps actif/phase → rôles admissibles/poids/maximum simultané. Exemple à tester : poursuivant et menace simple dès le début ; charge/tir tôt ; contrôle/soutien ensuite ; Résurgent à la crise. Le début doit déjà obliger à agir, conformément à 03.

Les deux améliorations peuvent coexister, mais leur intention utilisateur n’est pas encore tranchée.

## 5. Lots d'action

### Lot A — Audit jouable des rôles existants

1. Reprendre données et dispatch de comportements pour faire apparaître chaque famille isolément.
2. Filmer un cycle complet ; remplir la fiche obligatoire.
3. Classer le contenu : rôle convaincant, rôle à différencier, effet non fonctionnel, variante visuelle.
4. Vérifier sa présence dans les pools réellement atteignables.
5. Prioriser quatre rôles pour l'expérience de référence de 03.

**Vérification :** chaque famille possède une action observable ; les annonces sont compréhensibles sans fiche technique.
**Garde-fou :** présence en JSON ne prouve pas apparition normale.

### Lot B — Lisibilité et qualité des comportements

1. Ajuster anticipation/récupération et préserver les commandes de 01.
2. Déplacer les paramètres d'équilibrage concernés vers les JSON selon les loaders existants.
3. Brancher les animations/sons 08 et les impacts 02.
4. Vérifier recul, ralentissement et contrôle sur les ennemis forts.
5. Réinitialiser complètement les états temporaires lors de réutilisation du pool.

**Vérification :** charge évitable ; surgissement annoncé ; disparition d'un ennemi retire ses effets ; respawn sans état hérité.
**Garde-fou :** pas de télégraphie purement colorée ; pas de création de nœuds coûteux à chaque frame.

### Lot C — Compositions et progression

1. Reprendre SpawnManager et les pools de biome.
2. Introduire les rôles successivement ; réserver les combinaisons complexes au joueur préparé.
3. Plafonner les associations de contrôle, notamment Tisseuse + charge + Effacement.
4. Donner une identité aux Résurgences sans augmenter seulement les PV.
5. Associer ennemis forts aux récompenses et objectifs de 05/06.

**Vérification :** menace variée sur plusieurs seeds ; sortie praticable ; récompense sans immobilisation prolongée ; densité stable.
**Garde-fou :** un build faible ne doit pas être rendu invincible, mais le joueur doit pouvoir expliquer le danger.

### Lot D — Extension ciblée

1. Prototyper les trois nouvelles familles retenues, une à la fois, après les corrections A–C ; si un concept échoue, proposer un remplaçant qui préserve l’objectif de diversité.
2. Tester seul, en duo et en crise avant ajout aux pools.
3. Produire ses assets définitifs et son fragment d'identité après validation.
4. Réaliser les deux boss pilotes, valider leur différence de comportement et leurs récompenses, puis décider de l’extension à chaque famille.

**Vérification :** le nouveau rôle change une décision de combat ; il ne remplace pas toutes les menaces précédentes.
**Garde-fou :** pas de lot de dix variantes purement statistiques.

## 6. Recette finale

Comparer builds mêlée/distance/contrôle, début/late/endgame, effets normaux/réduits, terrain ouvert/étroit. Profiler au moins 100 ennemis avec VFX ; tester pool sur cycles répétés.

Build, smoke si applicable, séquences annotées et validation de Raphaël. Roadmap A/C/F/G. Acceptation : rôles reconnaissables, attaques évitables avec leurs indices, compositions variées et performances conservées.

## 7. Question du 26 septembre : toutes les créatures doivent-elles converger ?

**Question de Raphaël :** « je me demande si tous les mobs doivent converger vers le joueur ou seulement ceux à une certaine distance ? »

**Aujourd'hui** (`Enemy._PhysicsProcess`, `SpawnManager.CullFarDayEnemies`) :
- Toute créature active converge vers le joueur.
- Dans 600 px : IA complète.
- Au-delà : déplacement simplifié en ligne droite, à travers les obstacles.
- Au-delà de 1 400 px (environ deux écrans) : retirée.
- Aucune notion de perception. Les gardiens de POI eux-mêmes chassent dès qu'ils sont actifs, et une créature distancée suit le joueur en file jusqu'à son retrait.

**Proposition (non implémentée, à arbitrer) :**

| Rôle | Comportement proposé |
|---|---|
| Chasseurs (flux d'apparition, Résurgences, événements) | Inchangé : ils viennent pour le joueur |
| Habitants (gardiens de POI, créatures placées dans le monde) | Errance ou garde autour de leur point ; poursuite si le joueur entre dans leur perception (≈ 450 px), abandon au-delà d'un rayon de laisse (≈ 1,6 × la perception) |
| Créature distancée | Perd la trace après quelques secondes hors perception : elle erre, puis disparaît au retrait habituel |

Effets attendus : des rencontres qu'on voit venir et qu'on peut contourner, moins de files d'ennemis derrière le joueur. Le flux maintient la pression, et l'errance au loin coûte moins cher. Réglages en données par créature (`perception`, `leash`).

Risque : une pression ressentie plus faible si trop de créatures errent ; à mesurer avec `tools/measure_density.sh` (ennemis visibles, temps sans ennemi) avant et après.

### Implémentation — 26 septembre 2026

Proposition retenue par arbitrage délégué ([DECISIONS §7](DECISIONS.md#7-arbitrages-délégués-du-26-septembre)).
- **Perte de piste** (`Combat/EnemyTracking`, réglages dans `data/scaling/enemy_tracking.json`). Une créature qui a approché le joueur à moins de 450 px, puis est restée à plus de 800 px pendant 4 s, perd sa trace. Elle erre au ralenti (×0,35, changement de cap toutes les 2,5 s) jusqu'à ce que le joueur revienne à portée ou qu'elle soit retirée à 1 400 px. Les créatures qui arrivent du flux, les hardes en traversée et les créatures d'événement ne sont pas concernées.
- **Gardiens de POI :** ils ne quittent plus leur poste quand le joueur est hors du traitement complet (600 px). Avant, le déplacement simplifié les faisait converger vers lui.
- **Non fait :** perception propre à chaque créature (champs `perception` et `leash` par fiche) ; le réglage reste global.

**Mesure** (`SECONDS_PER_RUN=180 tools/measure_density.sh`, seeds 221092026 et 777, bot nomade sans esquive, avant → après) :

| Indicateur | Avant | Après |
|---|---|---|
| Créatures visibles, médiane minutes 1–2 / 2–3 | 22,5 / 23,5 | 18 / 20 |
| Quartile haut des visibles, minutes 1–3 | 43–48 | 26–33 |
| Moins de 5 créatures visibles, minutes 1–3 | 10 % / 5 % | 7 % / 3 % |
| Créatures tuées (deux seeds) | 154 / 32 | 242 / 169 |
| Dégâts reçus par minute, minute 1–2 | 2 854 | 750 |

Les amas qui suivaient le joueur disparaissent : sur la seed 777, le bot finissait enseveli sous 46 à 49 créatures. La pression ne s'effondre pas, les moments creux n'augmentent pas, et le flux apparaît davantage puisque les places se libèrent. La médiane baisse d'environ 15 % : à juger en jeu.

