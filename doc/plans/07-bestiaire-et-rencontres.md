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

**La Barrière, boss intermédiaire gardé le 4 octobre** ([DECISIONS §62](DECISIONS.md), [script du lore](../VESTIGES-LORE.md) §6) : la barrière de la Montée devenue chose, chaînes, grilles, poings qui frappent de l'autre côté. Sa résistance et ce qu'elle rend en tombant montent avec le nombre de Mémoriaux ravivés. À concevoir (fiche, motifs d'attaque, place dans le calendrier) avant toute production.

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

**Fermé le 4 octobre 2026** : Raphaël juge le sujet plus d'actualité ([DECISIONS §62](DECISIONS.md)). Les deux pistes ci-dessous restent pour mémoire, sans lot.

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

## L'Indicible mesuré — 4 octobre 2026

Mesure demandée au §64 ([compte rendu et journaux](../audits/indicible-2026-10-04/README.md), mode `--measure-indicible` de `RunObservation`). Sur deux seeds et trois postures de bot (en cercle, immobile au centre, posté hors de l'anneau), 20 s chacune :
- **Le boss ne perd aucun PV** : aucun tir n'atteint un bord.
  - Ses zones touchables sont les quatre bords, à 310–390 px de son centre. Les armes du build portent à 120–180 px.
  - Le ciblage vise son centre, où il n'y a rien à toucher. Hors de l'anneau, le centre est hors de portée et le joueur cesse de tirer.
  - Mêlée, orbite, cône et chaîne ne le voient pas du tout : ils ne retiennent que les `Enemy`.
- **Ses tentacules touchent un joueur immobile** (environ un coup par vague), **jamais un joueur qui bouge sans cesse** (annonce de 0,8 s, couloir de 30 px).

L'Indicible n'est donc pas combattable en l'état : c'est un défaut de conception (géométrie et ciblage), pas un réglage. Pistes pour la reprise, à concevoir avec la Barrière (§64) :
- **A. Bords ciblables :** chaque bord devient une cible que les armes voient (projectiles, mêlée, orbite, cône), et l'arène se resserre à la portée des armes, ou la suit.
- **B. Un cœur :** une partie centrale touchable, les bords restent un décor menaçant.
- **C. Refonte complète** avec la fiche de la Barrière : silhouette, phases, et attaques qui punissent aussi le joueur qui bouge.

## Fiche commune : la Barrière et l'Indicible — 4 octobre 2026 (proposition)

Demandée aux §64 et §65 : l'Indicible est **à refaire entièrement**, conçu avec la Barrière. **Rien n'est codé avant validation.** Sources : la [mesure](../audits/indicible-2026-10-04/README.md), le [script du lore](../VESTIGES-LORE.md) (§4, §7, §9), la recette commune des boss de famille (§4 ci-dessus).

### 1. Ce que la mesure impose aux deux boss

| Constat du 4 octobre | Règle de conception |
|---|---|
| Les zones touchables étaient à 310–390 px ; les armes portent à 120–180 px | **Tout ce qui se frappe arrive à portée d'arme** : 60–150 px du joueur au moment où il doit frapper, ou le joueur peut s'en approcher sans traverser une zone mortelle |
| Le ciblage visait le centre du nœud, vide | **Chaque partie touchable est une cible comme une créature** : mêlée, orbite, cône, chaîne, guidage et projectiles la voient par le même chemin que les `Enemy`, et le ciblage vise la partie, pas le centre du boss |
| Les tentacules ne touchaient jamais un joueur qui bouge | **Chaque phase mêle deux menaces** : une qui punit l'immobilité (frappe sur la position, annoncée) et une qui punit le mouvement mécanique (balayage, ligne qui avance, frappe sur la position anticipée) |
| L'Indicible ne perdait aucun PV, sans que rien ne le montre | **Mesure d'acceptation chiffrée** avec le mode `--measure-indicible` étendu aux deux boss (§5) |

Système commun, construit une fois : une **partie de boss** (cible, PV propres ou part d'une réserve commune, annonce, état vulnérable), les annonces existantes du lot B (cercle rouille, couloir de visée) et une barre de vie de boss. Les réglages vivent en données, contrôlés au chargement comme ceux de Q6b.

### 2. La Barrière — boss intermédiaire

**Ce qu'elle est (script §7) :** la barrière de la Montée devenue chose, chaînes, grilles, poings qui frappent de l'autre côté. Le joueur est du côté de ceux qui montaient. Plus on a ravivé de Mémoriaux, plus elle résiste, et plus elle rend en tombant.

**Forme :** une grille en travers du chemin, perpendiculaire à la direction où avance le joueur, sur environ 700 px. Elle ne bouge pas : c'est le joueur qui vient à elle, et l'Effacement le pousse par derrière. Elle se compose de **battants cadenassés** : **un, plus un par Mémorial ravivé dans la run, jusqu'à cinq**. Chaque battant est une partie touchable (environ 100 px de large, à hauteur de la grille), avec ses PV.

**Attaques**, chacune annoncée :
- **Les poings** (punit l'immobilité) : des poings passent entre les barreaux et frappent la position du joueur, 0,7 s après l'annonce, à moins de 200 px de la grille.
- **La chaîne** (punit le mouvement mécanique) : une chaîne balaie un arc devant un battant, de gauche à droite ou l'inverse. Il faut sortir de l'arc vers l'arrière ou franchir la chaîne au dash.
- **Le verrou** : quand un battant tombe, les autres se resserrent. Les poings frappent alors deux fois, sur la position présente puis sur la position anticipée.

**Récompense :** chaque battant brisé rend une récompense (forme à fixer avec la politique de butin, plan 13, non arbitrée). Elle grandit donc avec les Mémoriaux, comme le veut le script. La grille ouverte laisse passer : la run continue derrière.

**Fin personnelle de la Forgeuse :** briser la chaîne de la barrière de ses mains (script §5). La Barrière en est le lieu naturel (lot L4 du plan 19) : la scène se déclenche quand la Forgeuse porte le coup final, si son fil est complet.

### 3. L'Indicible — boss de fin de la partie classique

**Ce qu'il est (script §7) :** la nuit du 14 elle-même, ce qu'on ne peut pas dire. Trop grand pour l'écran : une tempête, une mer, des mains. Pas de corps à viser. Il se combat **par ses mains**, et il **se découvre** quand on a tenu.

**Trois phases**, dans l'ordre de la nuit (script §4) :
1. **La tempête.** L'écran s'assombrit, le vent pousse légèrement le joueur dans une direction qui change. Des éclairs frappent sa position (punit l'immobilité). Des **mains** sortent du sol à 80–150 px du joueur : ce sont les parties touchables. Chacune vit quelques secondes, puis agrippe la position du joueur.
2. **La marée.** L'eau monte depuis un bord de l'écran, par bandes. L'eau profonde ralentit et blesse. Les mains sortent de l'eau, plus nombreuses. Le joueur doit garder du sec sans fuir les mains (punit le mouvement mécanique : la bande avance).
3. **La seconde vague.** Une vague traverse tout l'écran, avec une ou deux brèches. Il faut y être (punit l'immobilité et la fuite au hasard). Après chaque vague, l'Indicible **se découvre** quelques secondes : une grande cible, au plus près du joueur, qui prend des dégâts accrus. C'est là qu'on le fait vraiment reculer.

**PV :** une réserve commune. Les dégâts aux mains l'entament ; la fenêtre découverte l'entame davantage. Les seuils de phase sont des fractions de la réserve.

**Ce que cette forme garde pour la suite du lore :** la vraie fin (L7) ouvre une dernière Faille face à lui. Si le joueur refuse, la Veilleuse lève sa lampe et l'Indicible se laisse voir en entier : la Montée, de nuit, sous la pluie. La phase 3 prépare cette image (une vague, puis la Montée découverte un instant).

**Déclenchement, inchangé :** cinquième Résurgence ou 22 minutes (`EndgameManager`). Après sa mort, l'endgame commence comme aujourd'hui.

### 4. Questions pour Raphaël

| # | Question | Options | Recommandation |
|---|---|---|---|
| 1 | **Quand vient la Barrière ?** | A. À un moment de la run (après la troisième Résurgence, vers 12 min), elle se lève devant le joueur, en travers de sa route. B. C'est un lieu de la carte, au bout de la Montée (plan 22) ; le joueur décide quand il l'affronte. C. Elle remplace la troisième Résurgence. | **A** : elle tombe au milieu de la run, quand les Mémoriaux comptent. Le joueur ne l'évite pas, mais il a pu s'y préparer en ravivant des Mémoriaux. B reviendra naturellement quand la Montée existera. |
| 2 | **Que changent les Mémoriaux ravivés ?** | A. Le nombre de battants (PV et récompenses), comme ci-dessus. B. Aussi une attaque de plus par paire de Mémoriaux. | **A** : la règle du script, lisible d'un coup d'œil ; la difficulté monte par la longueur du combat, pas par la complexité. |
| 3 | **Comment se frappe l'Indicible ?** | A. Les mains près du joueur, puis la fenêtre découverte après chaque vague. B. Un cœur unique qui se déplace dans l'écran. C. Des bords ciblables qui se resserrent (piste A de la mesure). | **A** : toutes les armes servent, la mêlée comprise, et la fenêtre récompense celui qui a tenu. B ressemble à un boss ordinaire ; C garde le défaut de portée. |
| 4 | **Combien de phases ?** | A. Trois (tempête, marée, vague). B. Une seule, de plus en plus dense. | **A** : chaque phase apprend une menace, et l'ensemble raconte la nuit sans un mot. |
| 5 | **Durée visée avec un build de référence** | A. Barrière 60–90 s, Indicible 2–3 min. B. Plus court. C. Plus long. | **A**, réglé par la mesure (§5), puis en jouant. |

### 5. Lots proposés, après validation

| Lot | Contenu | Vérification |
|---|---|---|
| **B1 — Parties de boss** | Cible commune que toutes les armes voient, réserve de PV, annonces, barre de boss, réglages en données contrôlés au chargement. Mannequin de test. | Banc : chacune des 24 armes touche un mannequin à partie unique (mêlée, orbite, cône, chaîne, guidage, ligne) ; fixture négative de réglages |
| **B2 — La Barrière** | Grille, battants selon les Mémoriaux, poings, chaîne, verrou, récompense par battant (selon le plan 13), apparition selon la question 1. Sprites au pipeline (planche d'abord). | Mesure en trois postures : PV perdus dans chacune, coups reçus immobile **et** en mouvement, durée de combat ; captures |
| **B3 — L'Indicible** | Remplace l'actuel : trois phases, mains, marée, vague, fenêtre découverte. Visuel sans corps (assombrissement, eau, mains). | Même mesure ; captures de chaque phase ; coût au banc si l'eau et la vague coûtent |
| **B4 — Réglage** | Durées et dégâts sur les deux mesures, puis partie de Raphaël. | Durées dans la fenêtre de la question 5 |

L'ancien `Indicible.cs` et ses réglages (`data/scaling/indicible.json`, plan 26 Q6b) sont remplacés en B3, pas conservés à côté.

## Reprise de la Barrière — 9 octobre 2026

Ordre fixé au [§84](DECISIONS.md) : la Barrière vient **à 10 min, à l'heure**, avant les pouvoirs des personnages, juste après les lots T1–T2 du [plan 30](30-temps-forts.md). Questions de reprise tranchées au [§85](DECISIONS.md). La fiche (§2 ci-dessus) change sur ces points :

| Point | Fiche du 4 octobre | Désormais |
|---|---|---|
| Déclenchement | Après la 3e Résurgence (~12 min, en fait 14–15 min) | `barrier_at_sec` 600 ; si une Résurgence, son annonce ou l'accalmie est en cours, à leur fin (même règle que les rendez-vous de Souverains, plan 30 T2) |
| Contournement | Non traité (700 px se contournent) | **Ailes de chaînes** tendues entre des poteaux, intouchables, au-delà de l'écran de part et d'autre ; on ne passe qu'en brisant un battant |
| Orientation | « Perpendiculaire à la direction du joueur » | **Selon le cap** : horizontale à l'écran si le joueur va vers le haut ou le bas, verticale sinon ; les deux trois-quarts des décors (`AXIS_X_YAW` 62°, `AXIS_Y_YAW` 22°) |
| Récompense | Selon le plan 13 | **Un coffre par battant**, le dernier rare |
| Barre de vie | « Barre de boss » | **En haut de l'écran**, nom et PV, un cran par battant ; sert aussi à l'Indicible et aux deux Souverains |
| Créatures | Non traité | Densité visée **×0,5** pendant le combat, en données |

### Lot B1 — Parties de boss : découpage proposé

Constat du code (9 octobre) : toutes les armes et tous les objets trouvent leurs cibles par `CrowdIndex`, puis gardent `node is Enemy { IsActive: true, IsDying: false }` (19 fichiers, une trentaine de chemins : projectiles, orbite, mêlée, cône, chaîne, guidage, échos, feu au sol, déclencheurs d'objets, propagation des contrôles). L'Indicible actuel n'est vu que par les projectiles et le guidage, par trois exceptions écrites à la main (`Projectile` l. 361, `Player` l. 2144, `TargetLock`).

**Choix d'architecture : une partie de boss est une `Enemy`.** Configurée par un comportement `boss_part`, immobile, sans attaque de contact, sans XP, sans recul ni gel ni séparation (règles déjà vraies pour le rang `boss`), elle est vue par les trente chemins sans en toucher un seul, statuts compris (brûlure, saignement, Fragile comptent sur un battant). L'autre voie, une interface de cible commune à `Enemy` et à une classe de partie, réécrirait les trente chemins pour le même résultat ; elle reste possible plus tard si les parties s'écartent trop des créatures. Le visuel d'une partie appartient au boss (la grille dessine ses battants) : la partie ne porte qu'une silhouette de touche et son flash.

| Sous-lot | Contenu | Vérification |
|---|---|---|
| **B1a — La partie** | Comportement `boss_part` dans `EnemyGrammar` et une fiche `data/enemies/boss_part.json` (sprite vide, PV et dégâts nuls, rayon de touche) ; `Enemy` : immobile, pas de contact, pas d'orbe, mort sans `EnemyKilled` (la mort du boss entier l'émet une fois, pour le score, les quêtes et les succès) ; signal `BossPartBroken`. Rayon de touche jusqu'à 50 px (`LargestBodyRadius` : la marge des requêtes de tous les tirs grandit, à mesurer). **Réserve commune** `BossHealth` : une partie garde ses PV (battant) ou reporte ses dégâts sur la réserve (mains de l'Indicible), avec des seuils (phases). Réglages communs en données (`data/scaling/boss_parts.json`), contrôlés au chargement comme Q6b : boss absent avec un message clair si invalides. | Contrôles unitaires dans un banc (réserve, seuils, report, mort unique) ; fixture négative de réglages ; `test_enemy_abilities.sh` et `test_weapons.sh` verts |
| **B1b — Barre de boss** | Barre en haut de l'écran : nom, PV, crans (un par partie à PV propres), apparition et disparition. Signaux `BossEncounterStarted`, `BossHealthChanged`, `BossEncounterEnded` par l'`EventBus`. Branchée sur les deux Souverains de la chasse (§85). | Captures `--event souverain` à 1080p (barre lisible, ne masque ni le HUD ni l'annonce) ; écran de pause et de niveau par-dessus |
| **B1c — Mannequin et banc** | Mode de mesure `--measure-boss-dummy` (extension de `RunObservation.IndicibleMeasure`) : un mannequin à une partie à 120 px du joueur, puis à trois parties en réserve commune ; chacune des 24 armes équipée seule pendant 10 s. | Chacune des 24 armes fait perdre des PV au mannequin (mêlée, orbite, cône, chaîne, guidage, ligne, zones) ; banc dense A/B (`/bench`) avec une partie de 50 px en jeu : pas de perte mesurable |

Ce que B1 ne fait pas : rien de visible en run hors de la barre des Souverains. Les exceptions de l'Indicible actuel restent jusqu'à B3, qui le remplace.

### Lot B2 — La Barrière : découpage proposé

| Sous-lot | Contenu | Vérification |
|---|---|---|
| **B2a — Planche des sprites** | Au pipeline des décors (`tools/sprites/props/`), deux orientations : travée de grille, battant cadenassé (intact, entamé, brisé ouvert), poteau et aile de chaîne répétable, poing (annonce, frappe), chaîne qui balaie. Planche sur sol réel avec un personnage de référence. | **Validation de Raphaël avant toute intégration** |
| **B2b — La grille en jeu** | Pose à 600 s devant le joueur selon son cap, à ~350 px ; battants = 1 + Mémoriaux ravivés, au plus 5 ; ailes infranchissables ; densité ×0,5 ; un coffre par battant, le dernier rare ; la grille ouverte laisse passer. | Captures dans les deux orientations ; le joueur ne passe pas sans briser ; chronologie : un pic net à 10–11 min |
| **B2c — Attaques** | Poings (immobilité, 0,7 s d'annonce, < 200 px), chaîne (mouvement mécanique, arc devant un battant), verrou (double frappe après chaque battant tombé). | Mesure en trois postures : PV perdus dans chacune, coups reçus immobile **et** en mouvement, durée 60–90 s avec le build de référence |

### B2a — planche proposée — 9 octobre 2026

[Planche](planches/07-b2a-barriere.png), modèle `tools/sprites/props/barrier.py` (pas encore branché à `generate_props.py`, rien d'écrit dans `assets/`). Grille municipale en fer forgé entre des piliers de pierre, chaîne de la Forgeuse, cadenas de laiton dont le trou de serrure porte la lumière vert-acide des créatures : c'est le point à frapper. Un battant fait 104 × 66 px (2,2 fois la hauteur d'un personnage), un pilier 20 × 87 px.
- **Horizontale** (lacet 0, face à la caméra) : se lit bien ; le battant brisé pend ouvert vers le joueur, chaîne rompue et cadenas au sol.
- **Verticale** (lacet 72°) : la grille part en diagonale (≈ 35° de la verticale), comme les grillages des rues verticales ; de profil exact (90°), les barreaux se fondaient en un trait. À même longueur au sol, il faut environ deux fois plus de travées qu'à l'horizontale. Cadenas plus petits de profil.
- **Poing**, deux variantes à choisir : gantelet de fer riveté, ou main grise de noyé ; chaîne au poignet, phalanges vers le joueur. Le cercle d'annonce est dessiné par le jeu.
- **Ailes** : bornes de fer et chaîne lourde, module répétable ; **maillons** à plat et de chant pour la chaîne qui balaie.

À valider par Raphaël avant B2b : silhouette générale, hauteur, gantelet ou main, orientation verticale en diagonale.

## Reprise des boss — 10 octobre 2026

Raphaël : « avance en autonomie sur les deux boss (l'Indicible et celui à 10 min) ». Lots enchaînés dans l'ordre du découpage : B1a, B1b, B1c, puis B2.

### B1a livré — 10 octobre 2026

- **Fiche commune** `data/enemies/boss_part.json` : comportement `boss_part`, rang `boss`, sans vitesse, dégâts, XP ni poids de tirage. Une partie est une `Enemy` : les trente chemins de ciblage la voient sans changement, statuts compris.
- **`Combat/BossHealth`** : réserve d'un boss. `AddPart(partie, rayon, PV propres)` : PV propres > 0, la partie tombe seule (battant) ; sinon elle reflète la réserve commune et la vide avec les autres (mains). Seuils de phase en parts de la réserve totale, franchis une fois et dans l'ordre. Événements `PartHit` (pour le flash du boss), `PartBroken`, `PhaseReached`, `Depleted` (levé à la mort de la partie qui vide la réserve, après la récompense de son battant).
- **`Enemy`** : une partie ne bouge ni ne frappe (sortie du tick après les statuts), n'a ni corps dessiné ni ombre ; ses PV perdus (coup, brûlure, saignement) vont au boss, sans l'excès d'un coup fatal. Sa mort n'émet pas `EnemyKilled` et ne laisse ni XP ni coffre : le boss le fera une fois. Le recul, déjà ignoré au rang `boss`, ne la pousse pas.
- **Rayon de touche** fixé par le boss, 50 px au plus (`data/scaling/boss_parts.json`, contrôlé au chargement par `Infrastructure/BossPartsConfig` avec la présence de la fiche). Il relève `Enemy.LargestBodyRadius`, donc la marge de recherche de tous les tirs, pour la suite de la run : coût à mesurer en B1c.
- **Vérifié** : `tools/test_boss_parts.sh` (nouvelle suite `boss_parts` de `tools/validate.sh`), 28 contrôles : réglages et 5 fichiers invalides refusés, deux battants (excès ignoré, ordre des événements, pas d'élimination émise), trois mains en réserve commune (coup et brûlure reportés, seuils dans l'ordre, défaite unique), partie collée au joueur et poussée qui ne bouge ni ne frappe, détachement au retour au pool. Suites smoke, enemy_abilities, weapons, catalogs, indicible, objects, launchers : 8/8.

### B1b livré — 10 octobre 2026

- **Barre de boss** (`UI/BossHealthBar`, enfant du HUD) : en haut au centre, sous le temps, le nom du biome et la ligne d'aide des événements (y = 62 en unités 960×540), 300 de large. Nom, remplissage rouille, traînée claire du dernier coup, un cran par partie à PV propres ; fondu à l'entrée et à la sortie, éclair blanc quand le boss tombe.
- **Signaux** `BossEncounterStarted`, `BossHealthChanged`, `BossEncounterEnded` par l'`EventBus`, portés par `Combat/BossBarFeed` : chaque rencontre a un numéro, si bien qu'une fin tardive ne ferme pas la barre du boss suivant ; la mise à jour ne part que si les PV ont changé. `BossHealth.ShowBar(bus)` ouvre la barre d'un boss à parties ; elle se ferme quand la réserve est vide ou par `EndEncounter()`.
- **Souverains** : la chasse ouvre la barre au nom du Souverain, la tient à jour à chaque tick et la ferme à sa mort, à l'expiration ou au nettoyage.
- **Vérifié** : 7 contrôles de plus dans `tools/test_boss_parts.sh` (35 en tout : crans, barre qui suit un battant brisé, rencontre suivante, fin unique, fondu) ; capture `--event hunt` à 1080p ([capture](../audits/boss-2026-10-10/b1b/souverain-024s.jpg)) : barre lisible, ne recouvre ni les PV ni le temps ni le score ; le HUD est en couche 10, sous les écrans de choix (20) et de pause (50). Suites smoke, boss_parts, movement-integration, run_trace : 4/4.

### B1c livré — 10 octobre 2026

- **Mesure du mannequin** `--measure-boss-dummy [--weapons …] [--seconds 10] [--distance 120] [--radius 50]` (`RunObservation.BossDummy`) : chaque arme seule, joueur immobile tourné vers le mannequin, sans autre créature ; une partie à PV propres, puis trois parties en réserve commune sur un arc.
- **Premier passage à 120 px** ([journal](../audits/boss-2026-10-10/b1c/mannequin-120px-avant.txt)) : les 14 armes à distance touchent ; **les 10 armes de mêlée (portée 55–70) jamais**, car la mêlée, l'estoc, le premier maillon de la chaîne et le cône mesuraient la distance au **centre** de la cible. Face à un battant de 100 px de large, on frappait le bord sans le toucher.
- **Correction** : une partie de boss se frappe à son **bord** (`Enemy.StrikeMargin`, égal à son rayon, nul pour une créature : l'allonge des armes contre les créatures ne change pas). Appliquée à `FindEnemiesInArc` (mêlée, estoc, chaîne) et au cône continu, dont la recherche s'élargit de `Enemy.LargestPartRadius` (0 tant qu'aucune partie n'a été posée).
- **Après, à 100 px** (bord à 50 px, à portée de toutes les armes ; [journal](../audits/boss-2026-10-10/b1c/mannequin-100px-apres.txt)) : **24 armes sur 24** entament le mannequin, à une partie comme en réserve commune (62 à 745 PV en 10 s). Le cône continu (`last_broadcast`) part où regarde le joueur, sans viser : la mesure tourne le joueur vers le mannequin.
- **Coût** : banc dense avec et sans partie de 50 px (`--boss-part 50`, nouvelle option de `MovementDenseBenchmark`), quatre passes alternées : aucun écart au-delà de la dispersion (scripts physiques 0,28 contre 0,28 ms en 720p, 0,41 contre 0,40 ms en 1080p ; [tableau](../audits/boss-2026-10-10/b1c/banc-dense-partie-50px.md)). Machine chargée : les FPS ne valent qu'en ordre de grandeur.

### Lot B3 — L'Indicible : découpage proposé (10 octobre 2026)

Fiche du 4 octobre (§3 ci-dessus), validée au §70 ; déclenchement inchangé (5e Résurgence ou 22 min, `EndgameManager`). Il remplace `Combat/Indicible.cs` et `data/scaling/indicible.json`, sans les garder à côté. Construit sur B1 : les mains sont des parties de boss en **réserve commune** ; la barre de boss et la mort unique (`EnemyKilled` « indicible » pour le score, les succès et l'entrée en endgame) viennent de `BossHealth`.

| Sous-lot | Contenu | Vérification |
|---|---|---|
| **B3a — Socle et tempête** | Nouveau `Indicible` : réserve commune, seuils de phase 2/3 et 1/3 ; réglages refaits et contrôlés au chargement (Q6b). **Tempête** : écran assombri (`CanvasModulate` de Main), vent qui fait dériver le joueur et change de sens, éclairs annoncés sur sa position (immobilité), **mains** qui sortent du sol à 80–150 px du joueur, vivent quelques secondes puis agrippent sa position (annonce). Sprite : la main grise de noyé de la planche B2a. | `IndicibleRegression` refait (réglages, réserve, phases, mort unique et récompense) ; mesure en postures (`--measure-indicible` refait) : PV perdus, coups reçus immobile et en mouvement ; captures |
| **B3b — Marée** | L'eau monte d'un bord par bandes : eau profonde qui ralentit et blesse ; plus de mains, qui sortent de l'eau. | Mesure : temps passé dans l'eau, coups reçus selon la posture ; captures |
| **B3c — Seconde vague et fenêtre** | Une vague traverse l'écran avec une ou deux brèches ; après chaque vague, l'Indicible **se découvre** quelques secondes au plus près du joueur : grande cible, dégâts accrus sur la réserve. | Mesure : vagues évitées par la brèche, part des PV pris dans la fenêtre ; captures de chaque phase ; coût au banc si l'eau et la vague coûtent |
| **B4 — Réglage** | Durées et dégâts des deux boss sur leurs mesures (Barrière 60–90 s, Indicible 2–3 min avec le build de référence), puis partie de Raphaël. | Durées dans les fenêtres du §70 |

### B2 livré — 10 octobre 2026 (B2a intégré tel que proposé, B2b, B2c)

Raphaël n'avait pas validé la planche B2a ; il a demandé d'avancer en autonomie. Elle est intégrée telle que proposée, poing en **gantelet** ; tout reste révisable (DECISIONS §89).

- **Sprites** : `tools/generate_props.py barrier` écrit `assets/bosses/barrier/` (18 sprites, manifeste) et `barrier_layout.json`, les pas écran entre piliers et entre bornes d'aile dans les deux orientations (113 px en horizontale ; 64 px en diagonale verticale, d'où deux fois plus de travées pour la même longueur au sol).
- **Levée** (`Events/BarrierDirector`, créé par `GameBootstrap`) : à `appear_at_sec` 600, ou à la fin d'une Résurgence, de son annonce ou de son accalmie. Elle se lève **devant le joueur selon son cap des 3 dernières secondes**. Le cap ignore les sauts, Failles comprises. La grille est horizontale si le joueur va vers le haut ou le bas, diagonale sinon. Elle se pose à **150 px** : à 350, elle naissait hors de l'écran, la vue faisant 540 px de monde en hauteur. Si elle sortirait de la carte (marge de 400 px) ou tomberait dans l'eau, une autre direction est essayée. Battants : 1 + Mémoriaux ravivés, 5 au plus. Pendant le combat, la foule visée tombe à ×0,5 (`SpawnManager.EncounterDensityMultiplier`) et aucun micro-événement ne part.
- **Grille** (`Combat/Barrier`) : travées fixes, piliers et ailes de chaînes de 3 000 px de chaque côté. Un mur (`StaticBody2D`, couche des décors) arrête le joueur, dash compris ; les créatures passent entre les barreaux. Chaque battant est une partie de boss à PV propres : il clignote quand il est frappé, passe « entamé » sous la moitié, puis s'ouvre en tombant. Son mur disparaît alors, et un coffre tombe du côté du joueur (le dernier rare). La grille entière compte une fois comme élimination de boss. Les décors sur la ligne sont retirés à la levée : invisibles, sans collision, hors du champ d'obstacles des créatures (`EnvironmentProp.Withdraw`, `ObstacleField.Remove`).
- **Fin du combat** : tous les battants brisés, ou joueur parti à 900 px **de l'autre côté**. Du côté d'où il arrive, seulement au-delà de 2 500 px (Faille) : reculer n'évite pas la Barrière.
- **Attaques** (`Combat/BarrierAttacks`), distances au sol :
  - **poings** : sur la position du joueur à moins de 260 px de la grille, annonce de 0,7 s ;
  - **chaîne** : arc de 150° et de 200 px devant le battant le plus proche, annonce de 0,9 s puis balayage de 0,9 s ; le dash la franchit sans dégâts ;
  - **verrou** : dès qu'un battant est tombé, chaque poing est suivi, 0,35 s plus tard, d'un second sur la position anticipée à 1 s.

  La mort par la Barrière s'affiche « La Barrière » au bilan (`boss:` + clé du nom).
- **Réglages** `data/events/barrier.json` (`Infrastructure/BarrierConfig`), contrôlés au début de la run, sons et coffres compris. Sons provisoires de la banque.

**Vérifié :**
- `tools/test_boss_parts.sh` : réglages et 11 fichiers invalides refusés, battants selon les Mémoriaux, réserve, disposition des deux orientations.
- Capture `--capture-barrier` dans les deux orientations ([haut](../audits/boss-2026-10-10/b2/barriere-up-1-devant.jpg), [battant entamé et brisé](../audits/boss-2026-10-10/b2/barriere-up-2-entame-brise.jpg), [verticale](../audits/boss-2026-10-10/b2/barriere-right-1-devant.jpg), [verticale brisée](../audits/boss-2026-10-10/b2/barriere-right-2-entame-brise.jpg)) : le joueur bute contre un battant fermé à pied (arrêté à 20 px de la grille) et au dash, puis passe par le battant brisé.
- Mesure par postures `--measure-barrier`, 30 s chacune, trois battants ([journal](../audits/boss-2026-10-10/b2/postures.txt)) :

| Posture | Poings portés / lancés | Chaînes portées / lancées |
|---|---|---|
| Immobile devant un battant | 13 / 13 | 4 / 5 |
| Va-et-vient le long de la grille | 1 / 13 | 2 / 2 |
| Aller-retour vers la grille | 2 / 7 | 0 / 0 |
| Va-et-vient avec un battant brisé (verrou) | 9 / 26 | 2 / 2 |

L'immobilité est punie par les poings, le va-et-vient régulier par la chaîne et le verrou ; qui varie ses déplacements passe.

- **Vraie run** (build de référence du plan 30, `--timeline --nomad --visit --mortal --prefer …`, 4 graines jusqu'à 13 min ; [journal](../audits/boss-2026-10-10/b2/vraie-run-75000pv.txt)) : la Barrière se lève entre 610 et 682 s, toujours à un battant (le bot ne ravive pas de Mémorial). Le bot va au battant et le frappe de près, immobile. Avec 6 000 PV, le battant tombait en 3 à 7 s. Avec 75 000 PV, il tombe en 64 et 109 s dans deux graines (28 et 48 poings, 10 et 15 chaînes reçus) et tient plus de 98 et 170 s dans les deux autres. Les builds diffèrent beaucoup sur une cible unique : 350 à 3 400 dégâts par seconde au total, foule comprise. Réserve retenue : **30 000 + 15 000 par battant** (45 000 PV pour un battant, 75 000 pour trois, 105 000 pour cinq). Réglage fin en B4, avec des Mémoriaux ravivés.
- Deux défauts trouvés par cette mesure et corrigés : la grille pouvait se lever **au bord de la carte**, battant hors d'atteinte (une autre direction est désormais essayée) ; un joueur qui **reculait** de 900 px mettait fin au combat (seul le passage de l'autre côté le fait).
- Relecture `godot-reviewer` : un coup ignoré (invulnérabilité, palier d'objet) n'est plus compté ni ne consomme la chaîne ; retrait d'un obstacle indexé par décor (il parcourait les 10 000 obstacles à chaque décor masqué) ; décors masqués seulement une fois la grille posée.
- Suites : smoke, boss_parts, enemy_abilities, weapons, objects, movement-integration, run_trace, run_phase, indicible, catalogs, launchers : 11/11.

### B3a livré — 10 octobre 2026 (socle et tempête)

- **Nouvel Indicible** (`Combat/Indicible`, `Infrastructure/IndicibleConfig`, `data/scaling/indicible.json` réécrits ; l'ancien et ses bords ciblables sont retirés, avec `Projectile.TryAbsorb`). Déclenchement inchangé (`EndgameManager`). Réserve commune de `BossHealth` : PV de la fiche × `boss_hp_scale`, seuils de phase à 2/3 et 1/3, barre de boss « L'Indicible ». La mort, unique, émet `EnemyKilled` « indicible » : score, succès et endgame inchangés.
- **La nuit tombe** : le `CanvasModulate` de Main s'assombrit en 2,5 s (le HUD n'est pas touché) et revient à la mort, ou aussitôt si le boss sort sans être vaincu.
- **Vent** : une dérive de 30 px/s qui pousse le joueur (`Player.ExternalDrift`), dont le sens change toutes les 4 à 7 s.
- **Éclairs** : toutes les 3 s, annonce de 0,9 s là où le vent aura porté un joueur passif. Ils punissent l'immobilité.
- **Mains** (sprites `tools/generate_props.py indicible` : main grise de noyé, paume ouverte puis poing qui agrippe) : parties de boss en réserve commune, 3 à la fois (+1 par seuil franchi). Elles sortent du sol à 80–150 px du joueur, vivent 4,5 s, puis agrippent, après 0,8 s d'annonce, la position où il sera s'il garde son pas (vent compris) : elles punissent qui file droit. Frapper une main entame la réserve.
- **Phases 2 et 3** : pour l'instant la même tempête avec plus de mains ; la marée (B3b) et la seconde vague (B3c) les remplaceront.
- **Défaut trouvé à la mesure et corrigé** : le vent portait le joueur immobile hors de l'éclair pendant l'annonce (1 éclair sur 5, 0 prise sur 6 ; [journal](../audits/boss-2026-10-10/b3a/postures-avant-correction-du-vent.txt)). Éclairs et prises visent désormais la position dérivée.
- **Réserve** : la fiche passe de 2 000 à 90 000 PV (288 000 à 22 min, ×3,2). L'ancienne valeur fondait en quelques secondes devant des builds qui font 350 à 3 400 dégâts par seconde dès 11 min. Réglage à mesurer en vraie run longue (B4).

**Vérifié :**
- `tools/test_indicible.sh` (25 contrôles) dans la vraie scène de run : réglages et 10 fichiers invalides refusés, nuit, dérive de 106 px en 14 s, 3 éclairs sur 3 et 3 prises sur 4 sur un joueur immobile, réserve entamée par une main, seuils dans l'ordre, mort unique et +5 500 points, vent retombé.
- Mesure `--measure-indicible` en postures, 20 s chacune ([journal](../audits/boss-2026-10-10/b3a/postures.txt)) :

| Posture | Éclairs portés | Prises portées |
|---|---|---|
| Immobile | 4 / 5 | 6 / 6 |
| Va-et-vient en ligne droite | 0 / 5 | 3 / 6 |
| Cercle | 0 / 5 | 0 / 6 |

- Capture `--capture-endgame` ([tempête](../audits/boss-2026-10-10/b3a/tempete.jpg)) : nuit, main, annonce d'éclair, barre.
- Relecture `godot-reviewer`, corrigée :
  - le vent passait par la vitesse du joueur, si bien qu'un joueur immobile marchait, faisait des pas et se tournait ; il pousse désormais le corps à part (`MoveAndCollide`), en run, hors dash, sans quitter le sol ;
  - libération différée par un tween lié au nœud ;
  - garde commune avant de toucher une main ;
  - le boss attend hors de l'état de run ;
  - un seul tween pour la nuit ;
  - comportement `indicible` retiré de la grammaire (plus aucun consommateur) ;
  - la capture de fin attend qu'une main existe pour finir le boss.
- Suites : smoke, indicible, boss_parts, weapons, enemy_abilities, catalogs, run_phase, movement, movement-integration, run_trace, launchers : 11/11 ; après la relecture, indicible, movement, movement-integration, catalogs et enemy_abilities : 5/5, mesure par postures inchangée.

### B3b livré — 10 octobre 2026 (marée)

- **`Combat/IndicibleTide`**, sous le premier seuil (2/3) : le vent et les éclairs tombent ; l'eau monte depuis un bord de l'écran, **ancrée dans le monde**, par bandes de 45 px toutes les 2,4 s, chacune annoncée 0,8 s par une bande claire qui palpite. Le front part à 270 px du joueur et va jusqu'à 200 px au-delà ; puis l'eau se retire et revient d'un autre bord 2,5 s plus tard. Sur 60 px, l'eau est peu profonde : le joueur y va à ×0,8. Plus loin, elle est profonde : ×0,55, et une blessure par seconde (dégâts de la fiche × 0,5). Les mains sortent de l'eau quand elle est à portée, deux de plus pendant la marée. Rendu : deux polygones au sol (couche −5), recalculés seulement quand une bande avance.
- **Mesure** `--measure-indicible --phase 2`, 30 s par posture ([journal](../audits/boss-2026-10-10/b3b/postures-maree.txt) ; l'eau allait alors jusqu'à 120 px au-delà du joueur, portée ensuite à 200) :

| Posture | Prises portées | Eau profonde | Coups de l'eau |
|---|---|---|---|
| Immobile | 11 / 12 | 2,4 s | 3 |
| Cercle | 0 / 12 | 20,4 s | 22 |
| Va-et-vient | 5 / 12 | 2,4 s | 3 |

  L'immobile est pris par les mains, celui qui tourne sans regarder entre dans l'eau.
- Capture `--capture-endgame --phase 2` ([marée](../audits/boss-2026-10-10/b3b/maree.jpg)) : eau profonde, bande peu profonde, main qui sort de l'eau.

### B3c livré — 10 octobre 2026 (seconde vague et fenêtre découverte)

- **`Combat/IndicibleWave`**, sous le second seuil (1/3) : la mer se retire. Toutes les 9 s, une vague traverse l'écran d'un bord à l'autre (700 px de part et d'autre du joueur, 420 px/s), après 1,6 s d'annonce. Ses une ou deux **brèches** de 80 px, à moins de 220 px du joueur, sont des couloirs de lumière chaude. Elle blesse ce qu'elle franchit hors brèche (dégâts de la fiche × 3). Rendu : écume et masse d'eau construites une fois par vague, seul le nœud avance.
- **Fenêtre découverte** : après chaque vague, l'Indicible se découvre 4,5 s à 110 px du joueur. C'est une grande main violacée de 46 px de rayon, une partie de la réserve commune rendue **Fragile** (+100 % de dégâts, statut existant), entourée d'un anneau.
- **Mesure** `--measure-indicible --phase 3`, 40 s par posture ([journal](../audits/boss-2026-10-10/b3c/postures-vague.txt)). La nouvelle posture « brèche » fait aller le bot dans la brèche la plus proche dès l'annonce.

| Posture | Vagues portées | Prises portées | PV pris pendant la fenêtre |
|---|---|---|---|
| Immobile | 3 / 4 | 16 / 16 | 1 483 |
| Cercle | 2 / 4 | 0 / 16 | 480 |
| Va-et-vient | 4 / 4 | 8 / 16 | 1 440 |
| Brèche | **0 / 4** | 14 / 16 | 182 |

  La vague s'évite par la brèche ; mais rester planté dans la brèche livre aux mains. La phase demande les deux gestes.
- Captures (image toutes les 0,5 s) : [vague](../audits/boss-2026-10-10/b3c/vague.jpg), [brèches annoncées](../audits/boss-2026-10-10/b3c/breches.jpg), [découvert](../audits/boss-2026-10-10/b3c/decouvert.jpg). Une première version (écume de 30 px, brèches à 35 % d'opacité) se lisait mal la nuit : masse d'eau ajoutée derrière l'écume, brèches à 60 %.
- **Banc** : `tools/test_indicible.sh`, 31 contrôles (limite portée à 6 000 images) : marée sous le premier seuil sans vent ni éclair, seconde vague sous le second seuil, fenêtre découverte qui prend double, mort unique.
- **Suites** : smoke, indicible, boss_parts, weapons, enemy_abilities, objects, catalogs, run_phase, movement, movement-integration, run_trace, audio, music, launchers : 14/14.

**Ce qui reste (B4)** : la réserve (90 000 × 3,2) et les dégâts se règlent sur une vraie run longue (22 min et plus) avec le build de référence ; la Barrière avec des Mémoriaux ravivés. Sons provisoires dans les deux boss.

### B4, premier passage — 10 octobre 2026 (mesure en vraie run longue)

Build de référence du plan 30, bot invincible (pour atteindre le boss), 4 graines jusqu'à 26 min ([journaux](../audits/boss-2026-10-10/b4/)). Le bot va au battant de la Barrière ; il ne cherche pas les mains de l'Indicible et ne joue pas la brèche.
- **Indicible** (levé à 22 min) :
  - réserve **288 000 PV** (fiche 90 000 × 3,2) : vaincu en **22, 56 et 141 s** ; une graine le laisse en vie après 240 s ;
  - réserve **432 000 PV** : 88 s, puis plus de 280 s dans les trois autres graines.

  Les runs divergent dès la Barrière et l'écart entre builds est énorme. **288 000 PV retenus** (médiane proche de 100 s, sous les 2 à 3 minutes visées), réglage fin à la partie de Raphaël. Les trois phases s'enchaînent dans les trois combats gagnés (tempête, marée, vague).
- **Barrière, un battant** (le bot ne ravive pas de Mémorial) :
  - à **45 000 PV** : 16, 23, 35, 113, 203 et 208 s, deux combats encore ouverts après 118 et 190 s ;
  - médiane au-delà de 110 s, d'où la base ramenée à **20 000** : 35 000 PV pour un battant, 65 000 pour trois, 95 000 pour cinq.
- **Défaut trouvé et corrigé :** à 10 min, le bot rôde près des bords déjà effacés. La pose ne vérifiait que les limites de la carte : dans une graine, le battant est tombé dans le Néant, hors d'atteinte pendant 750 s. Elle vérifie désormais sept points d'appui le long de la grille (sol de la carte, pas d'eau, pas de Néant de l'Effacement). Si aucune direction n'est parfaite, elle garde celle qui a le plus d'appuis.
- Suites après ces réglages : smoke, boss_parts, indicible, catalogs, run_trace, launchers : 6/6.

### Barrière : finition — 10 octobre 2026, fin d'après-midi

Demande de Raphaël, en attendant de tester : « fait les points 2, 4 (essaie de créer ou trouver de nouveaux sons uniques et appropriés). 5 : fait une vraie animation, prends le temps ».

**Animation de levée** (`Combat/BarrierRise`, `Combat/BarrierCrack`, section `rise` de `data/events/barrier.json`) :
1. **Présage** (0,9 s) : le sol se fend le long de la future grille, du centre vers les bords (760 px/s). L'entaille a des lèvres de terre et un cœur vert-acide, la lumière des cadenas. Elle s'élargit sous la grille et lance quelques rameaux ; la terre gicle à son front tant qu'il est dans le champ.
2. **Surgissement** : chaque pièce sort de terre par son bas en 0,7 s (seule la part émergée du sprite est dessinée, sans shader), en vague du centre vers les ailes (380 px/s). Elle dépasse sa hauteur de 16 % puis se pose, en tremblant au pixel ; pierre et terre jaillissent à son pied, et un pilier secoue l'écran.
3. **Verrouillage**, quand le cœur est debout (~2,6 s pour trois battants) : les cadenas s'allument d'un éclair vert, les chaînes des ailes se tendent d'un rebond, le verrou claque. Les murs ne bloquent qu'à cet instant ; un joueur resté sur la ligne est rendu à son côté. Les attaques ne commencent qu'après.

Pendant la levée, les battants sont enfouis : ni cibles ni vulnérables, les tirs ne les visent pas. Une fin de combat pendant la levée l'interrompt. La fissure reste ensuite comme une cicatrice à la lueur éteinte. Relecture `godot-reviewer`, corrigée :
- taille et pivot des pièces en cache (plus de chaînes créées à chaque image) ;
- fissure redessinée par crans de 14 px et seulement à 1 100 px du centre, sa lueur variant par l'opacité d'un nœud enfant ;
- flash de verrou porté par le flash du battant, qu'un coup interrompt ;
- pas de débris pour les ailes hors champ. Captures image par image : [horizontale](../audits/boss-2026-10-10/b2-finition/levee-up.jpg), [diagonale](../audits/boss-2026-10-10/b2-finition/levee-right.jpg), [détail](../audits/boss-2026-10-10/b2-finition/levee-detail.jpg). Le joueur bute toujours sur la grille fermée, dash compris, et passe par le battant brisé.

**Sons propres à la Barrière** (8, dans `assets/audio/sfx/selected/barrier_*.wav`, banque `sfx_barrier_*`, crédits dans `assets/audio/CREDITS.json`) :
- **Matière** : fer forgé, chaînes de geôle, pierre qui se fend. Montages de prises libres (Kenney CC0 ; BigSoundBank de Joseph SARDIN, licence de type CC0 : chaînes 0359, 0361 et 0358, grilles 0303 et 0683, porte de fer 0616), baissées d'environ une sixte pour alourdir le métal, et synthèses originales (grondement de terre, coup sourd, souffle à bande glissante).
- **Les huit** : levée, verrou, battant brisé (le cadenas éclate, la chaîne se rompt, le vantail pivote), chute de la grille, annonce et coup des poings, annonce et balayage de la chaîne.
- **Production** : script reproductible à l'octet près (graine fixe), `~/.local/share/vestiges-audio/2026-10-10/barriere/prepare.py`, avec sources, licences et page d'écoute (`index.html`).
- **Vérifié par mesure** (niveaux, absence d'écrêtage, spectrogrammes), **pas à l'oreille**. Un premier souffle bourdonnait à 50 Hz (découpage par blocs) : refait en filtrage glissant, voir le [spectre](../audits/boss-2026-10-10/b2-finition/spectres-balayage-levee.jpg).

**Réglage avec des Mémoriaux** (3 et 5 battants forcés par `--barrier-memorials N`, 4 graines ; [mesures](../audits/boss-2026-10-10/b2-finition/mesures.txt)) :
- **Constat** : sans spécialisation Convergence, chaque arme vise la créature la plus proche. Devant la grille, la foule passait avant le battant. Résultat : de 24 s à « jamais en 200 s » selon que la foule collait ou non au bot.
- **Préférence de ciblage des parties de boss** (`target_bias`, 100 px, dans `data/scaling/boss_parts.json`) : une partie à portée compte 100 px plus près qu'elle n'est. Les armes qui visent la frappent donc avant la foule ; zones, orbites et arcs continuent de frapper la foule autour. Les tirs comptent aussi le bord de la partie dans leur portée, comme la mêlée.
- **Relevé du combat toutes les 5 s** : collé au battant, un build y fait de 310 à 4 300 dégâts par seconde selon la graine (médiane vers 700) ; encerclé par plus de 50 créatures, le bot reste à 150 px et n'en fait que 220.
- **Réserve retenue, base 5 000 + 15 000 par battant** : 20 000 PV pour un battant, 50 000 pour trois, 80 000 pour cinq. Il suffit d'un battant pour passer, les autres sont la récompense ; au rythme médian, trois battants tombent en ~70 s.
- **Bug trouvé et corrigé** : la somme flottante des PV perdus ne retombait pas toujours exactement à zéro, si bien qu'une grille aux trois battants brisés n'était pas déclarée vaincue. Elle l'est désormais dès que toutes ses parties à PV propres sont tombées.

Suites : smoke, boss_parts, indicible, audio, weapons, enemy_abilities, movement-integration, catalogs, launchers : 9/9.
