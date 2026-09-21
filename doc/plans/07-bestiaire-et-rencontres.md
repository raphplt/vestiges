# Plan 07 — Bestiaire distinctif et rencontres variées

Statut : **ajout de créatures demandé ; familles et boss proposés** · Priorité : P2 · Dépendances : contrôle 01, tempo 03, référence 08.
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

