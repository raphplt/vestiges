# Audit de performances — VESTIGES, 27 septembre 2026

Analyse du commit `c78e1dd6`, sans modification du gameplay. Direction de référence : Stratégie V2. Build C# validé avec **0 avertissement et 0 erreur**.

## Diagnostic

Les priorités sont **le rendu du sol, la maîtrise du travail de combat et la durée de vie des objets**. Le code possède déjà des pools, des caches, un index spatial pour l'occlusion et un découpage des décors : les remettre en place ne résoudrait pas les problèmes restants.

Trois résultats ressortent :

- Le rendu du sol et des routes représente une part majeure du coût du banc à 1080p. Le masquer retire environ **1 565 appels de dessin par image**, tout en conservant les ennemis, les décors et les collisions. C'est une attribution de coût, pas le gain promis d'une future optimisation.
- Passer de **60 à 240 ennemis** fait passer le temps moyen de **4,71 à 11,08 ms**, et le p99 de **7,18 à 16,09 ms**. Le coût de foule reste significatif malgré le recyclage des effets.
- La destruction du pool laisse ses ennemis disponibles hors de l'arbre : **20, puis 40, puis 60 ennemis encore vivants** après trois cycles de création/destruction. Le diagnostic compte respectivement **80, 160 et 240 nœuds orphelins**.

En revanche, les allocations ne sont pas la première cause démontrée dans le combat de référence : environ **2,1 Mo alloués en 15 s**, et presque aucun nouveau nœud après chauffe. Les situations avec morts, XP, armes à effets de zone et renouvellement des ennemis demandent une mesure distincte.

## 1. Méthode et portée des mesures

### Environnement

Linux, Ryzen 7 5700X (16 threads), Radeon RX 6950 XT, Godot Mono 4.7.2, GL Compatibility, build Debug, fenêtre X11, VSync coupée, FPS libres, audio Dummy. Profil temporaire isolé ; sauvegardes personnelles inchangées. Les essais ont été exécutés **séquentiellement**, sans autre banc en parallèle. Les relevés de charge consultés sont restés sous le seuil de 4 utilisé par `bench_ab.sh`, avec les applications de bureau habituelles encore ouvertes.

Ce GPU ne représente pas une cible intermédiaire. Les chiffres caractérisent cette machine et ces scénarios ; ils ne certifient pas l'endgame sur une configuration minimale. Aucun pourcentage de gain historique n'est déduit des anciens rapports du projet.

### Scénarios

1. Banc existant `MovementDenseBenchmark` : 120 ennemis, 5 Ombres pour 1 Cracheur, seed `221092026`, chauffe de 5 s puis mesure de 15 s, 720p/1080p, sans/avec dash.
2. Attribution des décors à 1080p : référence, décors masqués, décors masqués, référence ; même banc et même seed. La physique des décors reste présente.
3. Charge de foule : mêmes réglages, 60 puis 240 ennemis ; un essai exploratoire par population.
4. Attribution du sol à 1080p : même banc chargé par un petit script externe ; ordre référence, sol/routes masqués deux fois, référence, puis mélange des biomes désactivé. Le script intervient avant la fenêtre mesurée. Aucun fichier de production n'est modifié.
5. Banc existant `--measure-props` : 8 s par biome au point chargé choisi par l'outil, sans spawn naturel ; contrôle complémentaire, pas une mesure pure des canopées.
6. Diagnostic headless du cycle de vie d'`EnemyPool` : trois pools successifs, chacun préchauffé à 20 ennemis, rendus au pool avant sa destruction ; vérification des identifiants d'instances après deux frames. Les objets survivants sont libérés explicitement à la fin du diagnostic.

### Résultats du combat de référence

| Résolution | Dash | Moyenne | FPS | p99 | Maximum | Images > 16,67 ms |
|---|---|---:|---:|---:|---:|---:|
| 1280 × 720 | Non | 5,93 ms | 168,7 | 9,72 ms | 26,83 ms | 0,039 % |
| 1280 × 720 | Oui | 5,93 ms | 168,6 | 9,67 ms | 26,50 ms | 0,040 % |
| 1920 × 1080 | Non | 6,73 ms | 148,6 | 10,29 ms | 24,80 ms | 0,045 % |
| 1920 × 1080 | Oui | 6,90 ms | 145,0 | 10,54 ms | 22,84 ms | 0,046 % |

120 ennemis vivants et dans le rayon de traitement complet durant ces quatre essais. Le p99 reste sous le budget de 16,67 ms ; il subsiste des pics isolés. Leur cause n'a pas été identifiée par une trace CPU/GC.

### Attribution des couches et montée en charge

| Série / scénario | Essais | Moyenne ms | FPS | p99 ms | CPU rendu ms | GPU ms | Dessins/image |
|---|---:|---:|---:|---:|---:|---:|---:|
| Décors : référence | 2 | 6,83 | 146,5 | 10,49 | 3,28 | 2,04 | 1895 |
| Décors masqués | 2 | 6,39 | 156,4 | 10,18 | 2,96 | 1,93 | 1806 |
| 60 ennemis | 1 | 4,71 | 212,5 | 7,18 | 2,84 | 1,87 | 1801 |
| 240 ennemis | 1 | 11,08 | 90,2 | 16,09 | 3,59 | 2,20 | 2080 |
| Sol : référence | 2 | 6,19 | 161,6 | 9,59 | 3,02 | 1,93 | 1895 |
| Sol + RoadOverlay masqués | 2 | 3,55 | 281,5 | 6,91 | 0,98 | 0,28 | 330 |
| Mélange des biomes coupé | 1 | 6,25 | 160,1 | 9,74 | 3,01 | 1,95 | 1895 |

Médianes des essais pour les scénarios répétés. Comparer les variantes **à l’intérieur de leur série** : les références varient entre séries.

Le sol/routes masqués réduit le temps mural moyen de **6,19 à 3,55 ms** (−42,6 %). Les chemins de terre en maillage restent visibles. Masquer les décors fait passer la moyenne de **6,83 à 6,39 ms** (−6,3 %). Le coût des décors est donc réel, mais nettement inférieur à celui des couches de tuiles dans ce cadrage.

Désactiver uniquement le mélange ne donne pas de gain net : 6,25 ms contre 6,19 ms pour la référence de cette série, avec pratiquement le même nombre de dessins. Un seul essai ne permet pas de conclure que ce shader est gratuit, particulièrement aux frontières ou en phase d’Effacement.

Le contrôle sans ennemis donne les résultats suivants. Le zoom et le protocole diffèrent du banc dense ; ces chiffres ne s’additionnent pas à ses résultats.

| Biome | FPS | p99 ms |
|---|---:|---:|
| collapsed_quarry | 327,6 | 5,11 |
| forest_reclaimed | 329,8 | 4,36 |
| swamp | 341,1 | 4,31 |
| urban_ruins | 325,7 | 5,03 |
| wild_fields | 354,9 | 3,99 |

Les suppressions de couches sont des **expériences d'attribution**. Elles changent volontairement l'image ; elles ne sont pas des solutions proposées pour le jeu. Les temps CPU de rendu et GPU se recouvrent partiellement : il ne faut pas les additionner au temps mural. Les moniteurs `TimeProcess` et `TimePhysicsProcess`, échantillonnés par frame rendue, ne constituent pas un profil par méthode.

### Limites importantes du banc

- Effacement, crises, spawn naturel et recul de caméra en foule sont désactivés. Les HP ×10 000 empêchent les morts : ni débit réel d'XP, ni renouvellement des ennemis, ni explosions de mort ne sont représentés.
- Le Traqueur utilise son arme initiale. Les builds à cône continu, chaînes, homing, feu et ricochets ne sont pas couverts.
- Le joueur tourne près du départ. Ce n'est pas une longue traversée nomade.
- Le champ de vue change entre 720p et 1080p : la différence ne mesure pas uniquement le remplissage GPU.
- Le banc inspecte lui-même chaque ennemi à chaque image pour vérifier sa validité. Cette surcharge contribue aux mesures de foule ; leur pente ne peut pas être attribuée entièrement à `Enemy._PhysicsProcess`.
- Une seule seed, des fenêtres courtes et peu de répétitions : suffisants pour orienter l'audit, insuffisants pour certifier un gain de production. Aucun profil de piles CPU, aucune trace de pauses GC et aucune mesure d'export Release n'ont été réalisés.

## 2. Rendu du sol : premier chantier de débit d'images

**Preuve : attribution mesurée ; cause exacte à départager par un prototype d'atlas.**

[BiomeTileMapper.cs](../scripts/World/BiomeTileMapper.cs), `LoadTileGroup` (vers ligne 292), crée un `TileSetAtlasSource` et une texture distincte pour chaque variante de tuile. Les routes générées suivent aussi ce principe dans `RegisterRuntimeTileGroup`. La couche `Ground` peut donc dessiner beaucoup de tuiles voisines avec des textures différentes.

[GroundMaterial.cs](../scripts/World/GroundMaterial.cs), `Apply` et `BuildAtlas`, construit bien un atlas, mais **uniquement pour que le shader prélève la matière du biome voisin**. Cet atlas ne remplace pas les textures utilisées par les sources du TileSet. Le jeu paie ainsi la création d'un atlas sans bénéficier de son regroupement pour le dessin normal du sol.

La mesure sol/routes masqués retire environ 1 565 appels de dessin sur environ 1 895. Cela désigne ces couches comme un coût majeur. Elle ne permet pas de répartir précisément le gain entre préparation CPU, soumission des dessins et calcul du shader.

**Piste recommandée :** produire les atlas de tuiles au build des assets, utiliser un petit nombre de textures et adresser les variantes par coordonnées d'atlas. Commencer par le terrain et les routes, dont l'ordre de profondeur est simple. Garder les décors hauts triés avec le joueur.

**Piège architectural :** plusieurs méthodes déduisent aujourd'hui la matière d'un `sourceId`. Avec un atlas, une même source contient plusieurs tuiles : adapter explicitement le contrat à `(sourceId, atlasCoords)` ou à une table de métadonnées. Préserver les Wang tiles, les routes directionnelles, l'eau, les transitions et les seeds.

**Validation :** même parcours et mêmes captures, appels de dessin, CPU rendu, GPU et p99 avant/après ; contrôles visuels des frontières et routes. La disparition totale du sol donne une borne expérimentale, pas un objectif de gain intégralement récupérable.

## 3. Pool d'ennemis : fuite de nœuds reproduite

**Preuve : reproduite isolément avec le code de production. Priorité élevée, correction localisée.**

[EnemyPool.cs](../scripts/Spawn/EnemyPool.cs), `Return` (ligne 55), retire l'ennemi de son parent et le conserve dans `_available`. Les ennemis préchauffés ne sont pas non plus attachés à l'arbre. `_ExitTree` (ligne 27) ne fait que remettre `Instance` à null ; aucune libération de la réserve n'est prévue.

Supprimer le parent détruit ses enfants, mais les ennemis disponibles ne sont justement plus ses enfants. Le diagnostic confirme qu'après destruction du pool leurs identifiants restent valides. Les 4 nœuds par ennemi mesurés sont ceux de la scène minimale ; un ennemi utilisé peut posséder aussi une ombre, des marqueurs ou des capacités.

**Piste :** définir le propriétaire de toute la réserve jusqu'à sa destruction et libérer explicitement les instances détachées en fin de run, ou les conserver sous un parent de réserve désactivé dont le cycle de vie est maîtrisé. Vérifier les références et l'annulation des effets avant réutilisation. Il ne suffit pas de vider la collection C#.

**Validation :** 20 passages Hub → run → Hub dans le même processus ; nombre d'orphelins, instances natives, mémoire managée après GC et RSS après stabilisation. Attendre un plateau après la montée initiale des caches, pas un retour artificiel du RSS à son niveau de démarrage.

Autre défaut de durée de vie : [GameBootstrap.cs](../scripts/World/GameBootstrap.cs), ligne 169, connecte à l'EventBus global une lambda `LevelUp` qui capture `levelUpPlayer` et `groupCache`, sans désabonnement. La garde `IsInstanceValid` protège l'appel mais ne retire pas la connexion. Le risque de rétention et d'accumulation des rappels est établi par le contrat des [signaux C# de Godot](https://docs.godotengine.org/en/4.4/tutorials/scripting/c_sharp/c_sharp_signals.html). Son volume mémoire réel n'a pas été mesuré ici. Remplacer par un abonnement dont le délégué est conservé et déconnecté en sortie de run.

## 4. XP au sol : le pool ne borne pas le travail actif

**Preuve : chemin d'exécution vérifié ; coût d'une longue run non mesuré.**

[XpOrb.cs](../scripts/Combat/XpOrb.cs), `_PhysicsProcess` (ligne 120), continue le flottement et la recherche de distance même très loin du joueur. Chaque orbe possède une `Area2D`, une forme, un sprite animé et un émetteur de particules. Le retour au pool ne se produit qu'à la collecte ; aucun endormissement spatial, regroupement ou expiration n'est prévu. [Enemy.cs](../scripts/Combat/Enemy.cs), `SpawnXpOrbs`, crée 1 à 3 orbes par mort.

Conséquence propre à la boucle nomade : avancer peut laisser derrière soi une quantité croissante d'objets qui continuent à travailler. À titre d'échelle algorithmique, 1 000 orbes encore actives correspondent à 60 000 rappels de physique par seconde à 60 Hz, avant les animations et la physique des zones. Ce n'est pas une population constatée pendant cet audit.

**Piste :** conserver la quantité d'XP en données, endormir les orbes hors de la zone d'attraction et de visibilité, fusionner les tas proches en conservant la somme exacte, et ne matérialiser que les orbes utiles. Une gestion centrale des positions et du magnétisme peut remplacer les callbacks individuels pour les orbes dormantes. Garder le saut de loot et les retours de collecte près du joueur.

Ne pas supprimer arbitrairement l'XP ancienne : ce serait un changement d'économie. Une disparition liée à l'Effacement demande une décision de gameplay distincte.

**Validation :** 0/250/1 000/3 000 orbes, à l'écran puis hors écran, collecte de masse et conservation exacte de l'XP ; pente du temps physique et du nombre de nœuds pendant 20–30 minutes de run nomade.

## 5. Combat : réduire le travail par impact et les parcours globaux

### Le cône continu déclenche toute la chaîne d'impact à chaque tick

[Player.cs](../scripts/Core/Player.cs), `ProcessSustainedCone` (ligne 1247), parcourt les ennemis puis appelle `TakeDamage` et `OnAttackHit` à chaque tick sur chaque cible dans le cône. [Enemy.cs](../scripts/Combat/Enemy.cs), `TakeDamage` (ligne 826), déclenche signal, feedback, flash, chiffre et demande audio. `OnAttackHit` ajoute les effets de l'arme, perks, vampirisme, recul et statistiques.

Avec 50 cibles effectivement touchées à 60 Hz, cela fait **3 000 chaînes d'impact par seconde**. Les dégâts sont multipliés par `delta`, mais le reste de la chaîne reste appelé 3 000 fois. Le regroupement des chiffres et le budget des étincelles réduisent déjà une partie du coût visuel ; ils n'annulent pas les appels en amont.

**Piste :** dissocier intégration des dégâts, cadence des procs et cadence du feedback. Accumuler les dégâts continus et publier les statistiques par lots lorsque leur sémantique le permet ; limiter le feedback répétitif par cible, sans diminuer le DPS ni les chances de proc. Une réduction brute du nombre d'appels changerait notamment le vampirisme et les effets « tous les N coups » : leur contrat doit être explicite avant toute modification.

### Le cache de groupe évite une requête, pas les recherches O(N)

[GroupCache.cs](../scripts/Core/GroupCache.cs), `GetEnemies` (ligne 24), fabrique une `Godot.Collections.Array<Node>` au premier accès de chaque frame rendue. Chaque consommateur la reparcourt : ciblage, ricochet, chaînes, homing, meute, feu, explosion et densité locale. Une chaîne de K rebonds peut parcourir N ennemis K fois ; H projectiles sans cible peuvent chacun recommencer leur recherche chaque tick.

Les collections Godot impliquent aussi un passage C#/natif pour leurs opérations ; la [documentation officielle des collections C#](https://docs.godotengine.org/en/4.4/tutorials/scripting/c_sharp/c_sharp_collections.html) recommande les collections .NET hors nécessité d'interopération.

**Piste :** un registre typé des ennemis actifs, mis à jour aux apparitions et retours au pool, accompagné d'une grille spatiale. Capturer une fois par tick les positions utiles ; interroger les cellules couvrant le rayon de l'attaque. Réutiliser les buffers. Pour un projectile homing sans cible, espacer et décaler les nouvelles recherches.

Les tris de `FindNearestEnemies` et `FindEnemiesInArc` (vers lignes 2098 et 2123) allouent plusieurs listes. Pour une unique cible, une recherche linéaire du minimum suffit ; pour quelques cibles, utiliser une sélection partielle. Préserver l'ordre de frappe si celui-ci affecte les chaînes, l'aléatoire ou les morts en cascade.

**Risque à traiter :** invalidation lors des morts et réutilisations dans le même tick ; une référence à un ennemi recyclé doit identifier aussi sa génération d'activation. Réutiliser une liste globale unique serait dangereux en cas d'explosion imbriquée.

## 6. Foule : le traitement simplifié existe, mais reste exécuté à 60 Hz

**Preuve : montée en charge mesurée ; répartition par méthode non profilée.**

[Enemy.cs](../scripts/Combat/Enemy.cs), `_PhysicsProcess` (ligne 476), réduit déjà le travail au-delà de 600 px. Cependant, chaque ennemi reçoit toujours son callback, consulte le joueur et sa position, met à jour certains états puis déplace son nœud. Près du joueur, le cycle complet et `MoveAndSlide` restent actifs.

La scène [Enemy.tscn](../scenes/enemies/Enemy.tscn) ne collisionne qu'avec les décors (`mask = 4`), pas avec les autres ennemis. Il serait donc incorrect de présenter la suppression des collisions ennemi/ennemi comme un gain disponible.

**Piste :** mesurer séparément mouvement natif, IA, animation et interopération ; centraliser les données communes par tick ; étaler les décisions des ennemis lointains à une cadence plus basse tout en conservant leur trajectoire et leurs effets temporels. Garder les annonces et attaques proches à pleine précision. Ne pas réduire globalement la fréquence physique du jeu.

[CrowdZoom.cs](../scripts/Combat/CrowdZoom.cs) recule jusqu'à 8 % quand la foule augmente : la surface visible peut alors augmenter d'environ **18 %** (`1 / 0,92²`). Le banc le désactive. Une recette finale doit donc tester la foule avec le vrai zoom : plus d'ennemis peut aussi signifier plus de sol, décors et shaders visibles.

## 7. Premières apparitions : préchauffage incomplet et allocations résiduelles

### Le préchauffage des shaders est hors du champ de la caméra

[GameBootstrap.cs](../scripts/World/GameBootstrap.cs), `WarmupShaders` (ligne 211), place les sprites à `(-9999, -9999)` puis les détruit après 0,1 s. En GL Compatibility, la [documentation Godot](https://docs.godotengine.org/en/stable/tutorials/performance/pipeline_compilations.html) indique qu'il faut afficher les matériaux/particules au moins une frame dans le champ pour éviter les compilations au premier usage. La présence d'un nœud hors champ ne prouve donc pas le préchauffage annoncé.

La liste ne couvre pas non plus plusieurs effets actuels : `ground`, `path`, `pixel_fx`, `light_column`, les variantes d'oubli et les matériaux de particules. Ce défaut de couverture est vérifié ; les pics du banc ne lui sont pas attribués sans trace spécifique.

**Piste :** scène de préchauffage réellement rendue dans un petit viewport, sous l'écran de chargement, avec les variantes et particules utilisées. Attendre une fin de rendu effective. Tester avec un cache de pilote froid isolé, puis chaud, sans effacer les caches personnels.

### Le pool recycle les nœuds mais recrée certains objets

[Enemy.cs](../scripts/Combat/Enemy.cs), `Reset` et `ConfigureVisual` (lignes 400 et 1280), enlève le matériau puis crée un nouveau `ShaderMaterial` à chaque initialisation. Les capacités sont déjà conservées dans `_abilityCache` : elles ne sont pas à recréer ni à « optimiser » une deuxième fois. En revanche, plaques et auras de variantes sont détruites puis recréées.

[EnemySpriteLoader.cs](../scripts/Combat/EnemySpriteLoader.cs), `LoadOrGet` (ligne 45), charge les séquences au premier usage de l'espèce. Le fallback miroir fait `GetImage` → `FlipX` → `ImageTexture.CreateFromImage`. Le préchauffage générique de 20 scènes Enemy n'appelle pas `Initialize` et ne chauffe donc pas toutes ces ressources.

**Piste :** conserver le matériau propre à chaque ennemi et réinitialiser intégralement ses paramètres ; préparer progressivement les assets des espèces susceptibles d'arriver ; stocker les animations/miroirs dans le pipeline d'assets lorsque possible. Ne pas partager un matériau mutable unique entre ennemis dont la dissolution diffère.

**Validation :** première apparition de chaque espèce, première élite, puis 100 cycles d'initialisation/retour au pool ; nombre de matériaux et allocations, mais aussi absence de dissolution ou de teinte héritée de l'usage précédent.

## 8. Effacement : coût croissant avec le territoire parcouru

**Preuve : comportement algorithmique vérifié ; coût tardif à mesurer.**

[ErasureManager.cs](../scripts/World/ErasureManager.cs), `_Process` (ligne 101), copie les clés de `_zoneMemory` puis revisite toutes les zones chaque 0,5 s. `SeedAroundPlayer` inscrit un carré de 29 × 29 zones et ajoute les nouvelles zones rencontrées. Les zones à mémoire nulle restent dans la boucle. Le coût dépend donc du territoire mémorisé, pas seulement de ce qui entoure le joueur. La carte actuelle est finie : il ne faut pas confondre cette croissance avec une carte infinie.

**Piste progressive :** sortir les zones stabilisées à zéro de l'ensemble actif tout en conservant leur état ; les réactiver explicitement si un Mémorial les restaure. Mettre la position du joueur en cache hors boucle et répartir les mises à jour si elles deviennent coûteuses. Une décroissance analytique par date est envisageable, mais doit reproduire présence du joueur, distance, accélération, crises et stabilisation : elle est plus risquée.

La texture envoyée au GPU fait seulement **32 × 32 octets** deux fois par seconde. Elle ne constitue pas une priorité crédible avant le parcours des zones et le rendu des effets.

Côté GPU, [ground_forget.gdshaderinc](../assets/shaders/ground_forget.gdshaderinc) ajoute du bruit et un calcul de fissures à 9 voisins quand la mémoire baisse ; [prop_forget.gdshaderinc](../assets/shaders/prop_forget.gdshaderinc) ajoute 4 lectures voisines par fragment dans les phases oubliées. Ces branches sont peu ou pas représentées dans le banc qui fige l'Effacement.

**Piste visuelle conditionnelle :** précalculer les champs spatiaux statiques de bruit/fissures, conserver leurs paramètres animés et les transitions ; mesurer avant d'introduire une nouvelle texture. Réduire le calcul en Néant complet si le résultat final remplace de toute façon les étapes précédentes.

**Validation :** mêmes cadrages aux mémoires 1 / 0,6 / 0,3 / 0,1 / 0, nombre de zones actives après 5/15/30 minutes, p99 autour du tick de 0,5 s. Préserver le rôle de pression de l'Effacement.

## 9. Monde : mémoire résidente et longues tranches de chargement

La seed du banc génère **10 694 décors répartis en 248 tronçons**, et la scène conserve environ **37 000 nœuds**. Le RSS maximal observé sur la première série est d'environ **713–777 Mio**, chargement compris. Cela ne signifie ni que chaque nœud travaille à chaque frame, ni que cette mémoire est entièrement imputable aux décors.

[PropChunks.cs](../scripts/World/PropChunks.cs) masque déjà les tronçons hors caméra : utile pour le rendu et le tri en Y. Il ne retire pas leurs objets de la mémoire ni leurs collisions du monde physique. Les props non bloquants sont eux-mêmes des `StaticBody2D` avec couche zéro dans [EnvironmentProp.cs](../scripts/World/EnvironmentProp.cs).

Les logs de référence attribuent environ **523–535 ms** à la phase de décors des biomes et **440–468 ms** à celle des tronçons ; la mise en place de la run atteint environ **3,3–3,5 s**. Ces intervalles de phases ne sont pas tous du CPU pur. Mais [WorldSetup.cs](../scripts/World/WorldSetup.cs), `InitializeWorldAsync`, appelle bien les placeurs et la construction des tronçons de façon synchrone entre deux `await` : l'overlay ne peut pas s'animer pendant tout ce travail.

**Piste courte :** borner les lots d'instanciation et d'entrée dans l'arbre en millisecondes ; créer directement la représentation finale plutôt que multiplier les passages ; précalculer les métriques de sprites actuellement obtenues par lecture d'image. Une partie de la génération est déjà en `Task.Run` : la recommandation n'est pas de tout déplacer sur un thread en touchant des nœuds Godot.

**Piste plus coûteuse :** séparer les données persistantes des décors de leurs instances proches et matérialiser les tronçons à l'approche. Ne l'engager qu'après mesure de la RAM et du chargement sur la cible. Garder les collisions nécessaires aux ennemis actifs, les emprises, l'occlusion et le tri du joueur. Les petites surfaces au sol et les ombres se prêtent mieux à un regroupement que les grands décors qui doivent se trier individuellement.

La séparation des décalques existe, mais le log indique **0 décalque extrait sur 10 694 décors** pour cette seed. Vérifier les métriques/manifeste et le seuil de 12 px avant d'affirmer que cette optimisation est effective. Ne pas modifier les silhouettes pour satisfaire le compteur.

## 10. Brouillard : gros travail différé et matériau probablement inopérant

[FogOfWar.cs](../scripts/World/FogOfWar.cs), `ProcessDeferredInit` (ligne 163), balaie le carré de la carte : `401² = 160 801` positions, par lots de 800, donc **202 frames minimum**. Ce remplissage a lieu après la dépause ; pendant `_initPhase`, le code ne traite pas la révélation autour du joueur. Le banc attend explicitement la fin de ce travail, ce qu'une vraie ouverture de run ne fait pas.

Le matériau est aussi suspect : [fog_of_war.gdshader](../assets/shaders/fog_of_war.gdshader) lit `mask_texture` avec `hint_default_black`, et sort transparent si le masque est noir. Aucun code du dépôt n'affecte ce paramètre. Le système utilise pourtant la suppression de cellules comme mécanisme de révélation. La configuration suggère donc un TileMap construit et dessiné pour un effet visuel transparent ; l'aspect attendu doit être confirmé par une capture d'une zone non révélée avant correction.

**Piste :** clarifier le contrat visuel du brouillard V2, puis choisir une représentation cohérente : masque de révélation avec mise à jour locale, ou tuiles dont la présence suffit réellement à produire le brouillard. Initialiser d'abord le voisinage visible et permettre la révélation pendant la préparation distante. Ne pas simplement retirer le système, dont la progression utilise les cellules découvertes.

**Validation :** déplacement immédiat après chargement, dash, frontières de révélation, Oubli du regard, découverte et quêtes ; coût d'initialisation et des mises à jour du TileMap.

## 11. Effets : terminer le travail de mutualisation sans effacer les dangers

Les effets courants sont déjà largement recyclés : projectiles, chiffres, formes, morts et XP ; `PixelSparks` regroupe les particules dans un seul nœud avec une capacité de 1 024. La recommandation générique « mettre les VFX en pool » serait dépassée.

Les exceptions à vérifier en charge sont précises :

- [GroundFire.cs](../scripts/Combat/GroundFire.cs), `Spawn`, alloue un nœud par flaque puis parcourt tous les ennemis à chaque tick de dégâts. Un gestionnaire de flaques avec requêtes spatiales et buffers réutilisés réduirait ces deux coûts.
- [VfxFactory.cs](../scripts/Combat/VfxFactory.cs), `CreateExplosionVfx` (ligne 115), recrée `SpriteFrames`, sprite, particules, matériau et Timer pour chaque explosion. Le chemin est utilisé par les morts explosives dans `Enemy.Die`. Préparer les ressources partagées et recycler la représentation.
- [FxBudget.cs](../scripts/Combat/FxBudget.cs) limite les **créations par frame**, pas le nombre total d'effets actifs ni leur surface à dessiner. Tous les chemins de `CombatPools.PlayFx` n'utilisent pas ce budget ; les dangers ennemis sont volontairement prioritaires. Un plafond par frame varie aussi avec le nombre de frames rendues.
- Le feedback de hit écrit encore `flash_amount` dans [HitFeedback.cs](../scripts/Combat/HitFeedback.cs), alors que [entity.gdshader](../assets/shaders/entity.gdshader) déclare mais ne lit plus ce paramètre. Le flash visible passe déjà par `SelfModulate`. Supprimer cette communication devenue inutile est un petit correctif, pas une promesse de plusieurs millisecondes.

**Piste :** budgets d'effets décoratifs simultanés et de surface visible, compteurs de saturation, mutualisation des ressources restantes. Réduire les doublons d'habillage avant de toucher aux annonces d'attaques ; ne jamais rendre un danger invisible pour satisfaire un budget.

## 12. Ce qui ne mérite pas un chantier prioritaire

- Le HUD met déjà à jour plusieurs textes seulement quand leur valeur change ; l'overlay debug sort immédiatement lorsqu'il est fermé.
- L'index d'occlusion des props est spatial et réutilise ses listes ; le remplacer par un nouveau système n'est pas justifié.
- Les capacités d'ennemis sont déjà mises en cache, comme les SpriteFrames après leur premier chargement.
- La génération des données du monde utilise déjà un thread ; les objets Godot et le rendu restent les étapes à traiter avec précaution.
- Les capacités du physique **3D** Jolt ne sont pas le levier pour ce jeu en physique **2D**.
- Remplacer systématiquement les distances par des carrés, supprimer toutes les chaînes ou passer en ECS/C++ avant profilage détournerait l'effort des coûts démontrés.
- Les matériaux partagés des props sont un acquis. Les gros effets de migration de renderer, de réduction de résolution ou de suppression de canopées ne sont pas justifiés par les mesures de cet audit.

## 13. Ordre d'exécution proposé

Chaque lot doit être détaillé dans le plan concerné avant de coder ; aucun n'est implémenté par cet audit et aucune case de roadmap n'est cochée.

| Ordre | Lot | Résultat attendu | Validation qui décide |
|---|---|---|---|
| 1 | Durée de vie des pools et abonnements | Pas de croissance des orphelins/abonnements entre runs | 20 cycles dans un processus, puis destruction propre |
| 2 | Atlas réel du sol et des routes | Moins de changements de texture et d'appels de dessin à image identique | A/B alterné, rendu CPU/GPU, p99, captures des tuiles et frontières |
| 3 | Mesure des scénarios manquants | Quantifier XP, morts en masse, builds et Effacement tardif | Courbes de population, allocations, pics et profil par méthode |
| 4 | Travail du combat et XP | Coût borné par les entités proches et les effets utiles | XP conservée, DPS/procs identiques, bancs 60/120/240/400 ennemis |
| 5 | Premiers usages et chargement | Moins de saccades à l'arrivée d'un contenu ; overlay fluide | Cache froid/chaud, premières espèces/effets, tranches de chargement |
| 6 | Monde résident, Effacement et rendu tardif | Limiter le coût de longue run si les mesures du lot 3 le confirment | Runs nomades longues et machine intermédiaire |

Les lots 1 et 2 répondent à des constats directs. Les suivants doivent être hiérarchisés à partir des nouveaux scénarios plutôt qu'en attribuant aujourd'hui un gain fictif à chaque méthode.

## 14. Recette de performance à conserver

Pour chaque changement, utiliser le même instrument, la même seed, le même build et le même matériel des deux côtés ; alterner les passes A/B et conserver les intervalles bruts. Mesurer aussi un export Release avant de décider des exigences matérielles.

Le socle minimal devrait couvrir : combat stable, arrivée d'une vague, morts simultanées et collecte, plusieurs armes actives avec cône/chaînes, 20–30 minutes de déplacement nomade, retour au Hub répété, premier usage des assets, frontières de biomes, forêt et Néant. Tester avec le zoom de foule réel.

Conserver moyenne, p95, p99, maximum, proportion >16,67 ms et >33,3 ms ; temps CPU/GPU ; nombres d'ennemis, projectiles, orbes, flaques, zones d'Effacement actives, effets refusés, nœuds natifs et orphelins ; octets alloués/s et pauses GC. Ajouter des marqueurs autour du ciblage, des impacts, de la réinitialisation des ennemis et du tick d'Effacement. C'est ce qui permettra de distinguer une optimisation algorithmique d'une simple variation de charge du bureau.

Les résultats structurés et les scripts de diagnostic sont conservés dans [le dossier de preuves](audits/performance-2026-09-27/README.md). Les gros logs et captures restent sous `/tmp/vestiges-audit-20260927/` sur la machine d'audit.
