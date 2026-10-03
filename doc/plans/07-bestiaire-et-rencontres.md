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

**Lot A, audit des données — 27 septembre 2026 (session cloud) :** étape 4 (« présence en JSON ne prouve pas apparition normale »). Outil reproductible : `python3 tools/audit_bestiary.py`. Il relève, pour chaque fiche, le rôle déclaré et tous les chemins d'apparition trouvés dans les données et le code : pools d'exploration et de crise des biomes (anciennes clés `day_enemy_pool` et `night_enemy_pool`, avec la part de la créature), gardes de points d'intérêt, micro-événements, identifiants cités dans `scripts/`.

| Créature | Type · rang · comportement | Vit. | PV | Capacités | Exploration (part du pool) | Crise et fin de run | Autres chemins |
|---|---|---|---|---|---|---|---|
| Charognard (`charognard`) | melee · normal · pack | 85 | 18 | pounce | Forêt Reconquise 40 %, Champs Sauvages 50 % | Forêt Reconquise 17 %, Champs Sauvages 17 % | micro-événements (1) |
| Colosse Sylvestre (`colosse_forest`) | melee · miniboss · colosse | 22 | 400 | — | — | — | — |
| Colosse des Profondeurs (`colosse_swamp`) | melee · miniboss · colosse | 28 | 300 | — | — | — | — |
| Colosse de Béton (`colosse_urban`) | melee · miniboss · colosse | 25 | 350 | — | — | — | — |
| Cracheur Pâli (`fading_spitter`) | ranged · normal · default | 55 | 15 | — | Marécages 25 %, Ruines Urbaines 20 % | Ruines Urbaines 12 % | code : SpawnManager.cs |
| Hurleur (`hurleur`) | ranged · normal · screamer | 25 | 35 | — | — | Carrière Effondrée 12 %, Forêt Reconquise 17 %, Marécages 12 %, Ruines Urbaines 12 %, Champs Sauvages 17 % | — |
| L'Indicible (`indicible`) | boss · boss · indicible | 0 | 2000 | — | — | — | code : EndgameManager.cs, Indicible.cs, QuestManager.cs |
| Présage (`presage`) | ranged · normal · default | 45 | 22 | omen_strike | Carrière Effondrée 20 %, Forêt Reconquise 20 %, Marécages 25 %, Ruines Urbaines 20 %, Champs Sauvages 25 % | Carrière Effondrée 12 %, Forêt Reconquise 17 %, Marécages 12 %, Ruines Urbaines 12 %, Champs Sauvages 17 % | — |
| Rampant (`rampant`) | melee · normal · burrower | 45 | 25 | — | Carrière Effondrée 20 %, Marécages 25 % | Carrière Effondrée 12 %, Ruines Urbaines 12 % | micro-événements (1) |
| Rôdeur (`rodeur`) | melee · normal · default | 30 | 60 | — | Carrière Effondrée 40 %, Forêt Reconquise 20 %, Ruines Urbaines 20 %, Champs Sauvages 25 % | — | micro-événements (1) |
| Ombre (`shade`) | melee · normal · default | 100 | 10 | — | — | Carrière Effondrée 25 %, Forêt Reconquise 33 %, Marécages 38 %, Ruines Urbaines 12 %, Champs Sauvages 33 % | code : SpawnManager.cs |
| Rampant d'Ombre (`shadow_crawler`) | melee · normal · default | 60 | 30 | — | Marécages 25 %, Ruines Urbaines 40 % | Ruines Urbaines 12 % | micro-événements (1); code : DebugActionPanel.cs, SpawnManager.cs |
| Tisseuse (`tisseuse`) | ranged · normal · weaver | 55 | 20 | — | — | Marécages 12 % | — |
| Tréant Corrompu (`treant_corrompu`) | melee · normal · default | 25 | 120 | — | Forêt Reconquise 20 % | — | — |
| Brute du Vide (`void_brute`) | melee · normal · charger | 35 | 80 | — | Carrière Effondrée 20 % | Carrière Effondrée 25 %, Forêt Reconquise 17 %, Marécages 12 %, Ruines Urbaines 12 %, Champs Sauvages 17 % | garde de 3 point(s) d'intérêt; code : EnemySpriteLoader.cs, SpawnManager.cs |
| Sentinelle Hurlante (`wailing_sentinel`) | ranged · normal · sentinel | 0 | 25 | — | — | Carrière Effondrée 12 %, Marécages 12 %, Ruines Urbaines 12 % | garde de 2 point(s) d'intérêt; code : SpawnManager.cs |

Inatteignables en jeu normal : `colosse_forest`, `colosse_swamp`, `colosse_urban`.

Constats :
- **Les trois Colosses sont inatteignables** : aucun pool, aucun garde, aucun événement, aucun code ne les fait apparaître. Leur comportement (charge et onde) et leur coffre épique garanti existent pourtant dans `Enemy`.
- **La Tisseuse n'apparaît qu'en crise dans les Marécages** (12 % du pool), le **Hurleur** qu'en crise, le **Tréant** qu'en exploration de la Forêt.
- **Menace à distance en exploration** : le Présage partout (20 à 25 %) et le Cracheur Pâli dans les Marécages et les Ruines. Le rééquilibrage du 23 septembre a pris.
- Les étapes 1 à 3 (filmer chaque famille isolément, remplir la fiche, classer les rôles) restent à faire en jeu ; `--capture-bestiary` en donne les images fixes.

Propositions :
- **Colosses : appliquée ensuite, provisoire, puis retirée** (voir ci-dessous) ;
- Tisseuse : l'ajouter aux crises de la Forêt et des Champs, pour qu'elle existe hors des Marécages. **Appliquée le 3 octobre** (DECISIONS §57, poids faible) : une entrée sur un groupe doublé, soit 1 sur 12 de poids en Résurgence dans la Forêt et les Champs (8 %), contre 13 % dans les Marécages ; les proportions des autres créatures ne changent pas. Mesure `--nomad --visit` de 15 min : 71 à 133 Tisseuses apparues par run, soit 1,8 à 2 % des créatures, toutes zones confondues.

**Lot C, Colosse de crise — 27 septembre 2026 (session cloud, choix provisoire) — retiré à la fusion de main le même jour** : le plan 17 (lot 0C) a supprimé les Colosses (données, sprites, comportement), la décision prime. Seul le correctif de la vague d'ouverture reste. Pour mémoire, ce qui avait été livré : à partir de la deuxième crise, le Colosse du biome où se trouve le joueur se lève hors écran (`SpawnManager.TrySpawnCrisisMiniboss`). Il répond à l'étape 4 du lot C : donner une identité aux Résurgences sans seulement augmenter les PV.
- Données : `crisis_miniboss_from` (2) dans `spawn_flow.json`, et `crisis_miniboss` dans chaque biome. Forêt et Champs ont le Colosse Sylvestre, Ruines et Carrière le Colosse de Béton, Marécages le Colosse des Profondeurs. Mettre `crisis_miniboss_from` à 0 désactive le tout.
- Il garde son comportement (charge et onde), sa signature de mort (plan 02 J2) et son coffre épique garanti.
- **Correctif trouvé en chemin** : la vague d'ouverture d'une crise plantait si la crise arrivait avant que `SpawnManager` ait résolu le joueur (il ne le résout qu'à son tick). La vague était perdue en entier.
- Vérifié : `MovementRegression --run-integration`, dans une vraie `Main`. Pas de Colosse à la première crise, le Colosse Sylvestre à la deuxième.

### Lot B — Lisibilité et qualité des comportements

1. Ajuster anticipation/récupération et préserver les commandes de 01.
2. Déplacer les paramètres d'équilibrage concernés vers les JSON selon les loaders existants.
3. Brancher les animations/sons 08 et les impacts 02.
4. Vérifier recul, ralentissement et contrôle sur les ennemis forts.
5. Réinitialiser complètement les états temporaires lors de réutilisation du pool.

**Vérification :** charge évitable ; surgissement annoncé ; disparition d'un ennemi retire ses effets ; respawn sans état hérité.
**Garde-fou :** pas de télégraphie purement colorée ; pas de création de nœuds coûteux à chaque frame.

**Lot B, étape 5 vérifiée — 27 septembre 2026 :** une créature rendue au pool en plein état temporaire repart neuve. Test ajouté à `tools/test_enemy_abilities.sh` (`RunPoolReuseChecks`) : une Brute du Vide Aberration avec affixe, brûlée, saignante, ralentie, désorientée, en traversée, enfouie et frappée, rendue au pool puis réutilisée en Rôdeur. Huit vérifications vertes : ni variante ni affixe, effets effacés, vitesse et PV de la nouvelle fiche, taille, opacité et collisions d'origine, aura et plaque de nom retirées, shader propre. Aucun défaut trouvé dans `Enemy.Reset` ; le test garde ce contrat pour la suite. Étapes 1 à 4 non commencées.

**Lot B, étape 2 livrée — 27 septembre 2026 :** les réglages des comportements quittent `Enemy.cs` pour les fiches (`stats`) : cri du Hurleur (`cry_cooldown`, `cry_range`, `cry_reinforcements`), phases du Rampant (`burrow_duration`, `surface_duration`), charge de la Brute du Vide (`charge_speed`, `charge_cooldown`, `charge_range`, déjà déclarés mais ignorés, plus `charge_duration`, `charge_first_delay`, `charge_retry`). Valeurs reprises à l'identique.
- **Bogue trouvé en route : la charge de la Brute n'a jamais eu d'effet.** Depuis son introduction (1er mars), la poursuite ordinaire écrasait la vitesse de charge dans la même frame : seuls l'éclair violet et le son se jouaient, la Brute avançait à 35 px/s. Elle fonce maintenant à 200 px/s pendant 0,8 s.
- Elle ne charge plus que si le joueur est à moins de 200 px (`charge_range`), sinon elle retente 0,5 s plus tard ; avant, la charge partait de n'importe quelle distance.
- **Effet de jeu à surveiller en recette :** la Brute devient nettement plus menaçante (Carrière en exploration, toutes les crises). Réglages dans `data/enemies/void_brute.json`.
- Vérifié : deux tests dans `test_enemy_abilities` (pas de charge hors de portée ; charge à 200 px/s à portée), `test_movement` vert.

**Lot B, étape 4 (recul) — 27 septembre 2026 :** **le recul des armes n'avait aucun effet.** `ApplyKnockback` ajoutait une fois 10 à 60 px/s à la vitesse de la créature, que sa poursuite réécrivait au tick suivant : mesurée, une créature frappée pour un recul de 40 avançait encore de 7,8 px vers le joueur. Le Parcmètre (« repousse tout »), la Cloche, le Râteau ou le Chronomètre ne repoussaient rien.
- Le recul devient une impulsion qui décroît en 0,2 s environ et s'ajoute au déplacement choisi par le comportement. La stat `knockback` d'une arme vaut désormais la distance de recul en pixels (Faucille 10, Parcmètre 40, Cloche 60…).
- Garde-fous : les coups rapprochés se cumulent jusqu'à 80 px au plus ; une variante agrandie recule d'autant moins qu'elle est grande ; boss et miniboss ne reculent pas.
- Vérifié : deux tests dans `test_enemy_abilities` (recul de 40 → 34 px, poursuite déduite ; six coups de 60 plafonnés à 74 px) ; `test_movement` et `test_weapons` verts.
- **À recetter :** les armes lourdes deviennent défensives, ce qui peut adoucir un début de run déjà jugé facile. Valeurs par arme dans `weapons.json`, plafond et décroissance en tête d'`Enemy`.

**Lot B, étapes 1 et 3 livrées — 27 septembre 2026 (session locale) :** anticipation, récupération, sons et animations des trois comportements qui frappaient sans prévenir.
- **Brute du Vide.** La charge partait sans annonce, sur un flash violet de 0,3 s. Elle devient une capacité `charge` du même moteur que le bond du Charognard :
  - 0,6 s d'annonce, Brute accroupie, sprite teinté de violet et couloir au sol dans la direction verrouillée ;
  - puis 160 px à 200 px/s, et 0,9 s de récupération immobile, la fenêtre pour riposter ;
  - un coup qui porte secoue l'écran (`impact_shake`) ; le son de charge choisi (`sfx_enemy_charge`) accompagne l'annonce.
  
  Portée (200 px), recharge (8 s) et premier délai (4 s) inchangés.
- **Rampant.** Enfoui, il frappait déjà au contact alors qu'il était à 35 % d'opacité et invulnérable. Capacité `burrow` :
  - enfoui, il file vers le joueur un peu plus vite (×1,3) mais ne touche plus ;
  - à la fin de l'enfouissement, il s'arrête et un cercle couleur rouille se remplit au sol pendant 0,6 s, puis il surgit : dégâts ×1,2 dans la zone annoncée, terre projetée, son de surgissement (`sfx_rampant_surgissement`, qui se jouait jusqu'ici à chaque coup au contact), puis 0,4 s de sortie immobile ;
  - un premier essai en famille « pierre » se perdait dans l'herbe (capture), d'où la rouille.
- **Hurleur.** Le cri appelait deux Ombres sans délai et sans son. Capacité `cry` :
  - 0,8 s d'annonce : le Hurleur s'arrête, crie (`sfx_hurleur_cri`), se teinte, et un cercle marque où les renforts vont surgir ;
  - **le tuer pendant l'annonce coupe l'appel** : c'est la décision « cible prioritaire » de la fiche du rôle ;
  - relecture `godot-reviewer` : les renforts apparaissaient depuis toujours avec les PV et dégâts de la minute 0 et sans signal `EnemySpawned`. Ils reprennent désormais la montée en puissance du Hurleur et sont recensés.
- **Sons des tirs** : un tir ennemi joue désormais le son de sa fiche (`attack_audio`, ce que le chargeur annonçait sans le faire), sinon le tir choisi en B6. La Sentinelle retrouve son tir ; le Cracheur et la Tisseuse gardent le tir choisi, `sfx_projectile_vol` n'est plus utilisé.
- **Code** : les trois comportements quittent `Enemy.cs` (−200 lignes) pour `Combat/Abilities/` (`BurrowAbility`, `CryAbility`, `PounceAbility` généralisé : `first_delay`, `windup_flash`, `impact_shake`). Réglages dans le bloc `abilities` des fiches. `EnemyPool.Instance` permet au cri de tirer ses renforts du pool.
- **Vérifié :**
  - `test_enemy_abilities` : 13 assertions ajoutées, 60 au total, toutes vertes. Elles couvrent :
    - charge : annonce, vitesse, touche, récupération, pas de côté qui l'évite ;
    - Rampant : enfoui sans dégâts ni contact, surgissement annoncé, touche sur place, évité en s'écartant ;
    - Hurleur : annonce, mort qui l'efface et coupe l'appel, renforts sinon ;
  - `test_movement` vert ;
  - captures `--capture-abilities --enemies void_brute,rampant,hurleur` (nouvelle option `--still`) regardées : couloir violet de la charge, cercle vert du cri puis deux Ombres, cercle rouille du surgissement.
- **Non fait (repris plus bas) :** tirs du Cracheur et de la Sentinelle sans annonce ; portée de la Sentinelle invisible.
- **À recetter :** la Brute devient évitable, donc plus juste mais peut-être moins dangereuse ; le Rampant ne blesse plus pendant l'enfouissement ; le Hurleur, immobile 0,8 s, est plus facile à tuer.

**Lot B, fin de l'étape 1 — tirs annoncés, 27 septembre 2026 (session locale) :**
- Capacité `aimed_shot` (`AimedShotAbility`) sur le Cracheur Pâli, la Sentinelle Hurlante et la Tisseuse : le tir instantané devient une visée.
  - Le tireur s'arrête et se teinte ; un couloir court, à la couleur de son projectile, montre la direction verrouillée : 0,3 s pour le Cracheur, 0,35 s pour la Tisseuse, 0,45 s pour la Sentinelle.
  - Le projectile part dans cette direction, même si le joueur a bougé : un pas de côté pendant la visée l'évite.
- **Portée de la Sentinelle** : quand le joueur s'en approche à moins de 90 px, le contour de sa portée se dessine au sol, en pointillé discret, sans remplissage. Dedans on est visé, dehors non. La portée se mesure désormais au sol, comme le cercle : la Sentinelle vise un peu moins loin vers le nord et le sud qu'avant.
- `GroundTelegraph.ShowRing` : contour seul, pour une limite à connaître plutôt qu'un coup qui arrive. Un premier essai avec l'intérieur tramé couvrait la moitié de l'écran (capture).
- Réglages dans le bloc `abilities.aimed_shot` des trois fiches. Le Hurleur garde son tir instantané, secondaire à son cri.
- **Vérifié :**
  - quatre assertions ajoutées à `test_enemy_abilities`, 64 au total, toutes vertes : visée immobile sans tir, projectile dans la direction verrouillée, Sentinelle muette hors de portée au sol, cercle et visée à portée ;
  - captures `--capture-abilities --enemies wailing_sentinel,fading_spitter,tisseuse --still` regardées.
- **À recetter :** la cadence des tireurs baisse un peu (la visée s'ajoute à la recharge).

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

**Perception par créature — 26 septembre 2026 (session cloud) :**
- Une fiche peut fixer `perception` (distance d'engagement) et `leash` (distance de perte de trace) dans ses `stats`. Sans ces champs, le réglage commun de `enemy_tracking.json` s'applique (450 et 800 px).
- **Valeurs provisoires, à juger en jeu :**

  | Créature | Perception | Laisse | Profil |
  |---|---|---|---|
  | Ombre | 560 px | 1 000 px | Rapide et tenace |
  | Charognard | 520 px | 950 px | Meute qui flaire de loin |
  | Rôdeur | 400 px | 720 px | Lent |
  | Brute du Vide | 380 px | 700 px | Lente |
  | Tréant corrompu | 360 px | 680 px | Lent, décroche vite |
- **Mesure non concluante** (`SECONDS_PER_RUN=150 tools/measure_density.sh`, seeds 221092026 et 777, conteneur cloud à environ 5 FPS). Le bot n'y est pas reproductible : sur des runs presque identiques, les morts varient de 35 à 21 et de 18 à 67.

  | Indicateur | Avant | Après |
  |---|---|---|
  | Médiane des visibles, minutes 1–2 | 12 | 9 |
  | Médiane des visibles, minutes 2–3 | 14 | 19 |

  Mesure à refaire sur une machine qui tient 60 FPS.

**Mesure** (`SECONDS_PER_RUN=180 tools/measure_density.sh`, seeds 221092026 et 777, bot nomade sans esquive, avant → après) :

| Indicateur | Avant | Après |
|---|---|---|
| Créatures visibles, médiane minutes 1–2 / 2–3 | 22,5 / 23,5 | 18 / 20 |
| Quartile haut des visibles, minutes 1–3 | 43–48 | 26–33 |
| Moins de 5 créatures visibles, minutes 1–3 | 10 % / 5 % | 7 % / 3 % |
| Créatures tuées (deux seeds) | 154 / 32 | 242 / 169 |
| Dégâts reçus par minute, minute 1–2 | 2 854 | 750 |

Les amas qui suivaient le joueur disparaissent : sur la seed 777, le bot finissait enseveli sous 46 à 49 créatures. La pression ne s'effondre pas, les moments creux n'augmentent pas, et le flux apparaît davantage puisque les places se libèrent. La médiane baisse d'environ 15 % : à juger en jeu.


**Mesure refaite machine calme, 27 septembre au soir** (Linux, RX 6950 XT, 60 FPS tenus ; `tools/measure_density.sh` dans des worktrees au parent de « perception et laisse par créature » puis à ce commit, seeds 221092026 et 777, 180 s) :

| Indicateur | Avant | Après |
|---|---|---|
| Créatures visibles, médiane minutes 0–1 / 1–2 / 2–3 | 6 / 15 / 25,5 | 4 / 24 / 28 |
| Moins de 5 créatures visibles, minutes 0–1 / 1–2 / 2–3 | 46 % / 12 % / 4 % | 55 % / 6 % / 2 % |
| Créatures tuées (deux seeds) | 198 / 34 | 28 / 181 |
| Dégâts reçus par minute, minutes 1–2 / 2–3 | 1 028 / 1 346 | 3 764 / 5 892 |

Toujours **non concluante**, pour une autre raison que dans le conteneur : même à 60 FPS, le bot n'est pas reproductible. Ses morts s'inversent d'une seed à l'autre (198 → 28, 34 → 181), et ce sont elles qui font la densité : un bot qui tue peu s'entoure. Aucune baisse de pression n'apparaît. Seul un bot au comportement stable, ou le ressenti en jeu, tranchera.

## Reprise de la pression des projectiles — 1er octobre 2026

[DECISIONS §44](DECISIONS.md), [plan 24 §12, R3](24-retours-du-1er-octobre.md#12-retours-de-recette--1er-octobre-2026) : Raphaël ressent trop de tirs qui traversent l'écran ou la carte, issus de deux ou trois types d'ennemis selon son estimation. Ils imposent une esquive continue et gênent le combat contre la foule. Le souhait antérieur de davantage de menaces à distance ne justifie donc plus d'en augmenter la présence sans mesure.

**À faire :** inventorier les attaques actives et les compositions réellement rencontrées. Distinguer projectile à longue portée, zone au sol, charge et onde locale ; ne pas déduire le comportement d'un ennemi du seul nom de son sprite. Comparer cadence, distance parcourue, durée de vie, tirs simultanés, visibilité du tireur et possibilités de réponse.

**Pistes d'essai, non arbitrées :** limiter la part simultanée de tireurs, raccourcir certains tirs ou espacer leurs salves ; si nécessaire, transformer une attaque en menace locale télégraphiée. Choisir à partir du diagnostic, sans appliquer tous les leviers à la fois ni vider la foule. La direction demandée porte sur la place des tirs, pas seulement sur leurs dégâts ou leur beauté.

Mesurer avant/après dans des conditions comparables (seeds, durée, build), puis jouer la séquence : le critère est de pouvoir combattre et maintenir une foule tout en gardant des esquives ponctuelles intéressantes. Aucun changement de gameplay n'est livré par cette note.

### R3 — diagnostic et essai engagés

1. Relever les quatre tireurs, leurs télégraphes, cadences, portées et pools de biomes ; distinguer le Présage, frappe au sol.
2. Ajouter au banc de densité une sonde optionnelle des projectiles présents/visibles, âgés de plus de deux secondes et répartis par tireur. Aucun coût ajouté aux runs normales.
3. Mesurer deux seeds pendant 320 s, première Résurgence comprise. Comparer sur le même build les 4 s actuelles à une surcharge de 2 s **limitée au banc**, sans changer vitesse, cadence ni compositions.
4. Archiver les chiffres avec densité, dégâts et limites de reproductibilité. Décider si l'essai mérite une partie jouée ; ne pas adopter un réglage de production sur le seul bot invincible.

### R3 — diagnostic livré, réglage de production conservé

Inventaire et [rapport avec données archivées](../audits/projectile-pressure-2026-10-01/README.md) : quatre tireurs, tous à 185 px/s pendant 4 s (740 px), contre 180–300 px de portée d'engagement. Le Présage est une frappe au sol. Le Hurleur figure dans tous les pools de Résurgence.

Quatre runs headless de 320 s, seeds 221092026/1002, même build, sonde à 10 Hz : en référence, 7,20–14,78 tirs visibles en moyenne pendant la première Résurgence, pics 35–60 ; présence pendant 93–94 % de la phase. Les Hurleurs représentent 63–73 % de l'occupation de projectiles. L'essai 4 → 2 s limité au banc donne des résultats opposés selon la seed et des compositions différentes : **non concluant**, aucun nerf livré.

Sonde et synthèse réutilisables (`--measure-projectiles`, `--projectile-lifetime`, `tools/summarize_projectile_pressure.py`), build sans avertissement, 12 800 échantillons cohérents. Pas de fenêtre ouverte pour ces mesures. Prochaine expérience : composition fixe, espacement du tir du Hurleur seul en conservant cri, mêlée et annonces ; retour aux seeds puis vraie partie. Le ressenti et l'essai rendu restent ouverts, aucune case de gameplay/recette cochée.

### R3b — cadence du Hurleur, lot engagé

Continuation autonome hors audio (DECISIONS §47). Isoler le levier que R3 ne
pouvait mesurer : un banc à tireurs fixes, trajectoire et premiers délais fixes,
avec tir annoncé et cri réels. Comparer le cooldown courant à un multiplicateur
propre au Hurleur, sans toucher aux autres tireurs, vitesse, durée de vie,
annonce, cri ou tables d'apparition. Vérifier aussi l'agressivité tardive et le
recyclage des capacités. Si l'essai ménage des interruptions nettes et conserve
le rôle d'appel, adopter un réglage modéré en JSON, puis vérifier les régressions,
les mêmes runs naturelles et des captures de vraie Main. Le plaisir et la
recette humaine resteront explicitement distincts des mesures automatiques.

### R3b — cadence ciblée livrée

`abilities.aimed_shot.cooldown_multiplier = 2.5` pour le Hurleur seul, défaut 1
pour les autres. Intervalle de base 1,50 → 3,15 s, annonce de 0,4 s intacte,
cri/renforts et scaling d'agressivité conservés. Aucun changement de portée,
durée de vie ou composition des pools.

[Banc fixe, quatre runs et preuves](../audits/projectile-cadence-2026-10-01/README.md) :
354 → 168 tirs sur 90 s à agressivité normale, mêmes 60 cris et 126 tirs de
Cracheurs ; 38,7 % de projectiles actifs en moins. À agressivité 1,6 : baisse de
34,9 %. Les pointes baissent sur les deux runs naturelles, sans baisse observée
de la foule ; leurs builds divergent, donc ces runs restent des observations.

Build sans avertissement, 61 régressions de capacités vertes (réemploi de la
cadence compris), captures Main avant/après inspectées sur ViewSonic. Réglage
techniquement livré, **ressenti à confirmer en partie humaine** ; les tirs
restent continus dans le cas extrême du banc et le retour de Raphaël n'est pas
considéré comme validé par un bot. Référence obsolète du préchargement repérée
pendant les captures, à corriger dans le lot court suivant.
