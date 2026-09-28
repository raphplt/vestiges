# VESTIGES — Seconde passe : performances et robustesse

28 septembre 2026. Diagnostic uniquement, aucun correctif de production. Suite de l’[audit du 27 septembre](AUDIT-PERFORMANCES-2026-09-27.md) et du [suivi des lots](plans/10-terrain-et-tiles.md#11-audit-de-performances-du-27-septembre--lots).

## Diagnostic

Les problèmes encore ouverts ne justifient pas une réécriture du moteur du jeu. Cette passe apporte quatre éléments nouveaux :

- **Le cône est un multiplicateur d’impacts, même après mutualisation des effets.** Reproduction avec 50 cibles : **12 000 signaux de dégâts en quatre secondes d’émission**, et **3,05–3,18 Mo alloués dans les appels au cône** après chauffe. Aucun ennemi ne meurt dans cette fixture. Ce coût existe donc indépendamment du renouvellement de la foule et des explosions.
- **Les optimisations de distance ont déjà un défaut de temps.** À 1 500 px, ralentissement et désorientation de deux secondes ont encore deux secondes restantes après dix secondes simulées. L’Effacement perd aussi le temps excédant son intervalle : à une mise à jour par seconde, sa progression à dix minutes est **15,40 % au lieu de 30,80 %**. Ce sont des reproductions de robustesse, pas une affirmation que le jeu tourne à 1 FPS.
- **Le préchauffage ne soumet pas ses sprites au rendu.** Dans un viewport GL réellement rendu : **0 objet/0 dessin hors champ, 8 objets/8 dessins dans le champ**, avec les huit mêmes shaders. Dix shaders utilisés en run manquent à sa liste. Le coût des compilations à cache froid reste à mesurer.
- **Le soupçon de 10 000 obstacles physiques était trop large.** La carte de référence contient **10 694 corps, 2 318 formes et 8 376 corps sans forme**. Remplacer les derniers par des nœuds visuels représente environ **5,88 Mio de mémoire native potentielle**, par extrapolation d’une reproduction isolée ; aucun gain de FPS n’est établi.

La validation de durée de vie manquante est faite : **20 cycles techniques Hub → Main → Hub, 148 nœuds et zéro orphelin à chaque retour**. Le profil managé de la foule est maintenant disponible par méthode. Une fenêtre calme a ensuite permis un A/B de densité : **2,28–2,49 ms/image à 60 ennemis, 7,42–7,58 ms à 240**, observateur décimé. L’inspection par le banc coûte seulement quelques dizaines de microsecondes : elle ne suffit pas à expliquer la pente.

## 1. Méthode et portée des mesures

### État analysé et environnement

Base `0383e05379cc148496c24c4ef01ea7a3e7769fb6`, plus les modifications présentes au départ, copiées dans un worktree détaché `/tmp/vestiges-audit-20260928`. Les fichiers de production y sont identiques à ceux trouvés au début. Les flaques de feu en cours n’ont pas été modifiées ni évaluées comme un nouveau chantier. Systèmes, cycles, profils et A/B d’observateur utilisent l’assembly SHA-256 `ce4f62be3faca9cced319ce0d0ebcc3590b44f3dad55e5788d2046f9e71fb658`. Le dernier A/B de densité utilise l’assembly déjà construit dans l’arbre principal (`6a47a72a424a43417aee0f6175a4e0d4a3a5c968ab54324e868eae4c58763838`), sans rebuild entre ses quatre passes. **Toutes les empreintes de production sont identiques entre ces séries**, vérifiées par les manifestes ; seules les comparaisons internes au même build sont exploitées.

Godot **4.7.2 Mono**, .NET 10, build Debug, GL Compatibility ; Linux, Ryzen 7 5700X, Radeon RX 6950 XT, Mesa 26.2.3. Seed **221092026**. Sauvegardes et paramètres isolés dans des profils temporaires. Les empreintes, commandes, sorties et relevés `uptime` sont dans le [dossier de preuves](audits/performance-2026-09-28/README.md).

**Attention : `project.godot` sélectionne Jolt pour `physics/3d/physics_engine`. Les `CharacterBody2D` et `StaticBody2D` de cet audit relèvent de la physique 2D. Un chantier sur Jolt 3D ne traiterait pas leur coût.**

### Niveaux de preuve

| Niveau | Ce qu’il signifie ici |
|---|---|
| **Mesuré** | Compteurs sur la vraie scène, cycles de vie, soumissions au rendu, ou trace explicitement instrumentée. Les limites de chaque compteur restent indiquées. |
| **Reproduit isolément** | Méthodes réelles appelées dans une fixture contrôlée, sans réimplémenter leurs algorithmes. Géométrie et cadence imposées ; ce n’est pas une run jouée. |
| **Déduit du code** | Chemin d’exécution, conséquence algorithmique ou risque dont le coût réel n’est pas mesuré. |

Deux processus indépendants reproduisent les essais des systèmes. Ordres **A/B/B/A** pour 0/50 cibles, 60/240 ennemis et 841/6 000 cellules ; le test mémoire conserve aussi sa première séquence de chauffe. Les profils managés suivent 60/240/240/60 ennemis. Les intervalles bruts sont conservés, sans fusion des passes.

### Charge et limites importantes du banc

Le seuil de `bench_ab.sh` est **4** sur les 16 processeurs logiques de cette machine. Une tentative de comparaison du banc avec observateur complet/décimé a été **refusée à 4,65**. Les séries headless et les profils ont rencontré une charge supérieure au seuil. Une fenêtre calme en fin de session a permis huit passes A/B de l’observateur (**charge 1,61–3,89**) puis quatre passes de densité 60/240/240/60 (**2,20–2,99**). Les relevés `uptime` avant/après et la charge chaque seconde sont conservés. **Seules ces douze passes non profilées sont utilisées pour le débit.** Les 4,7 → 11 ms du premier audit ne sont pas un avant/après valable : des lots ont été livrés entre-temps.

Les chronométrages par appel restent dans les JSON comme diagnostics bruts. Sous charge, les conclusions reposent sur les allocations, nombres d’impacts, états, corps, formes et objets rendus. Les pourcentages EventPipe sont exploratoires, non utilisés comme estimation du gain d’une optimisation.

Autres limites :

- Les appels isolés à l’ennemi ne constituent pas un profil exclusif d’une vraie frame ; ils ne contiennent ni le pas global du serveur physique ni le rendu. Les positions sont remises en place avant chaque échantillon. Les différentes sous-méthodes ne doivent pas être additionnées au coût total.
- `ProcessBehaviorAbilities` couvre meute/régénération, **pas toute l’IA**. La fixture isolée utilise des Ombres ; le profil rendu conserve le mélange Ombres/Cracheurs du banc.
- Le cône touche 50 ennemis immobiles parmi 240. Les 240 restent parcourus dans le témoin à zéro cible. Les quatre secondes mesurées sont du **temps d’émission actif**, pas quatre secondes d’une rotation complète avec cooldown.
- Les cycles headless testent la destruction des scènes après une seconde simulée de run ; ils ne rejouent pas mort, choix de récompense, Steam, vingt longues runs, ni la mémoire GPU.
- Le parcours d’Effacement est synthétique et fini, sans combat ; il ne prouve pas la distribution des zones d’un vrai joueur nomade.
- Les gains possibles ne s’additionnent pas : une réduction des impacts change aussi les effets, l’audio et les signaux.

Une première fixture avait désactivé les corps physiques avec leur scène, invalidant `MoveAndSlide`. Elle a été rejetée à la lecture des erreurs, puis remplacée par une fixture conservant les corps dans l’espace physique (`KeepActive`). Ses résultats ne figurent pas dans les séries officielles. Le premier import du worktree a également fini avec une erreur native à la fermeture ; le build, les diagnostics officiels et la validation finale sont distingués de cet essai préliminaire.

## 2. Rendu du sol : conserver les gains acquis

Le lot atlas est livré et mesuré ; les tronçons de décors existent déjà dans `PropChunks`. Cette passe ne propose ni de refaire l’atlas, ni de réimplémenter le découpage des décors, ni de remettre le brouillard retiré.

**Déduit du code :** les tronçons masquent le rendu, pas les collisions. De plus, `PropContainer` contient maintenant des tronçons : un diagnostic qui compte seulement ses enfants directs trouve **zéro `EnvironmentProp`**. Le recensement du présent audit descend dans tous les conteneurs, y compris les décalques. C’est aussi une précaution à conserver dans les outils de capture et de contrôle du monde.

**Validation qui décide :** comparer les futurs changements de rendu avec l’atlas et les tronçons actuels, sur les mêmes positions et zooms. Ne pas reprendre comme référence les chiffres antérieurs à leur livraison.

## 3. Durée de vie : vingt cycles sans accumulation de nœuds

**Mesuré — `cycles.json`, 21 points incluant le Hub initial.**

| Compteur au Hub | Initial | Après 1 run | Après 20 runs |
|---|---:|---:|---:|
| Nœuds | 148 | 148 | 148 |
| Nœuds orphelins | 0 | 0 | 0 |
| Ressources | 584 | 2 884 | 2 889 |
| Mémoire managée après GC | 1,30 Mo | 8,24 Mo | 8,23 Mo |
| Mémoire native suivie par Godot | 101,72 Mo | 177,91 Mo | 189,43 Mo |
| RSS du processus | 246,82 Mo | 546,04 Mo | 554,83 Mo |

Après chauffe, cycles 5–20 : mémoire managée **8,225–8,227 Mo**, RSS **554,8–563,7 Mo**, ressources **2 884–2 892**. La minuscule hausse managée inclut les lignes de résultat conservées par le banc. La mémoire native varie encore ; ce test ne démontre pas un plateau universel de tous les caches.

La fuite de nœuds du premier audit ne réapparaît pas. Ne pas interpréter les ~300 Mo supplémentaires de RSS après le premier chargement comme 300 Mo perdus par run : les caches et les allocateurs restent résidents.

**Suite utile :** après un lot touchant callbacks/pools, refaire les cycles avec une run chargée en morts, variantes, cônes, level-ups et événements avant destruction. Aucun nouveau chantier général de gestion mémoire n’est justifié par cette série seule.

## 4. XP, explosions et feu : périmètre fermé pour cette passe

L’endormissement des orbes et les explosions recyclées sont livrés. Les flaques sont un travail préexistant dans l’arbre et restent intactes. Le présent audit ne leur attribue aucun nouveau gain.

La conservation de l’XP et la recette des feux restent dans leurs lots. Une longue run devra les inclure, mais **la croissance des appels du cône mesurée ici ne vient pas de ces systèmes** : il n’y a aucune mort dans sa fixture.

## 5. Combat : le coût résiduel du cône est maintenant reproduit

**Reproduit isolément — `systems-a.json` et `systems-b.json`, appels réels à `Player.ProcessSustainedCone`, `Enemy.TakeDamage` et aux abonnés du signal.**

| Deux émissions de 2 s après chauffe | A : 0 cible touchée | B : 50 cibles | B répété | A répété |
|---|---:|---:|---:|---:|
| Ticks mesurés | 240 | 240 | 240 | 240 |
| Signaux `EntityDamaged` | 0 | 12 000 | 12 000 | 0 |
| Dégâts cumulés observés | 0 | 4 245,118 | 4 245,118 | 0 |
| Octets alloués dans les appels | 63 360 | 3 048 624 | 3 179 672 | 63 360 |

Ces compteurs se reproduisent dans les deux processus. Surcoût par rapport au témoin : **2,985–3,116 Mo pour 12 000 impacts**, soit environ **249–260 octets par impact**. Cela correspond à **0,75–0,78 Mo/s d’émission active** dans cette fixture. Ce sont des allocations sur le thread appelant ; le travail des effets entre les appels et les allocations natives ne sont pas inclus. Pas de mesure de pauses GC attribuables au cône dans une longue run.

**Déduit du code :** chaque impact provoque aussi une demande audio, même si le throttle la refuse ensuite ; un appel à `PlayHit`, même si son budget l’écarte ; le feedback, le chiffre, les effets de l’arme, les statistiques et les procs. Le plafonnement du résultat visuel n’empêche pas toute la préparation en amont.

**Piste :** séparer intégration des dégâts, cadence des effets/procs et feedback. Un budget de feedback par cible et par temps peut réduire les demandes répétées. Pour les dégâts/statistiques, agréger seulement si le contrat de gameplay le permet.

**Pièges :** passer naïvement de 60 à 10 impacts/s conserve éventuellement le DPS mais change les probabilités de proc, l’exécution, les événements « tous les N coups » et l’ordre des morts. Le vampirisme proportionnel aux dégâts n’a pas le même contrat qu’un proc par impact. Les procs et les statistiques doivent rester attribués à la bonne arme. Les cibles regroupées du test ne sont pas une densité garantie en jeu.

**Validation qui décide :** même seed, 0/1/10/50/100 cibles ; DPS cumulé et dégâts par arme identiques, distributions de procs explicitement validées, absence d’impacts après mort/recyclage ; allocations directes, allocations totales et appels de feedback. Ensuite cône + chaîne + homing dans la vraie scène, avec et sans renouvellement des ennemis.

La grille spatiale/registre typé proposée le 27 reste une **hypothèse**, pas un lot automatiquement prioritaire. Le cache de groupe est toujours lié à l’image rendue, pas à la génération d’activation d’un ennemi : la validité des références lors de morts et réutilisations imbriquées doit précéder toute mutualisation supplémentaire des buffers.

## 6. Foule : profil obtenu, contrat temporel à réparer avant tout LOD

### A/B au calme : la pente subsiste sans inspection à chaque image

**Mesuré — 720p, passes 60/240/240/60, même build et seed, inspection une image sur 60, 10 s par passe après 3 s de chauffe.**

| Population / ordre | Temps moyen brut par image | p99 | Population observée |
|---|---:|---:|---:|
| 60, A1 | 2,279 ms | 4,236 ms | 60–60 |
| 240, B1 | 7,576 ms | 14,148 ms | 240–240 |
| 240, B2 | 7,424 ms | 12,715 ms | 240–240 |
| 60, A2 | 2,493 ms | 5,012 ms | 60–60 |

Le temps moyen est multiplié par environ **3,14** entre les moyennes des deux populations. La pente subsiste après les lots livrés et avec l’observateur décimé ; aucune optimisation de production n’a été appliquée dans cet A/B.

L’A/B distinct de l’observateur (complet/décimé, puis décimé/complet à chaque population) mesure directement **8,2–8,3 µs par inspection complète à 60 ennemis**, **31,9–36,5 µs à 240**. Son coût total sur dix secondes passe d’environ **36 à moins de 1 ms** à 60 ennemis, et **42–44 à moins de 1 ms** à 240. La suppression de ces inspections ne produit pas de gain de débit dans ces deux paires : les passes décimées sont même plus lentes. Cette différence dépasse le travail retiré et ne permet pas de conclure que moins observer ralentit le jeu ; trajectoires, cadence des effets et variabilité restent présentes. Elle exclut surtout l’inspection linéaire comme explication principale des plusieurs millisecondes de pente.

**Biais supplémentaire trouvé :** le maximum des **douze passes** est la deuxième image mesurée (index 1), **108–137 ms**. Le code initialise alors son inventaire `SeedSeenNodes`, ses ensembles et ses compteurs mémoire après le départ du chronomètre ; son travail est payé dans l’intervalle suivant. La coïncidence avec ce chemin est établie, sa durée exclusive n’est pas instrumentée séparément. Ne pas attribuer ces maxima au combat sans isoler cette préparation. Les intervalles restent bruts, aucune image n’a été retirée des moyennes ci-dessus. Déplacer la préparation avant la fenêtre mesurée fait partie du prochain lot de banc.

Deux captures, 60 et 240 ennemis, ont été ouvertes : elles montrent les créatures massées au contact et les Cracheurs autour, pas 240 silhouettes bien séparées. Le banc ne couvre donc pas tous les coûts d’une foule répartie à l’écran. Autre limite de répétabilité : `QuestManager` appelle son propre `Randomize()` ; la seed de carte n’impose pas les mêmes quêtes/HUD. Figer aussi ces tirages dans la fixture lors du prochain lot, sans changer le gameplay de production.

### Attribution exploratoire, sans gain de FPS annoncé

**Mesuré sous instrumentation et sous charge — quatre traces EventPipe**, 60/240/240/60 ennemis, dix secondes par trace après chauffe, même assembly. Les piles brutes au format Speedscope et la synthèse par méthode sont archivées. L’observateur du banc inspecte la foule une fois toutes les 60 images pendant ces profils.

Les méthodes désormais identifiées dans les piles incluent `Enemy._PhysicsProcess`, `MoveWithKnockback` → `MoveAndSlide`, `UpdateSpriteAnimation`, `ProcessMelee`, `ProcessAbilities`, `Player._PhysicsProcess` et `GrassTrample._Process`. Les appels C#/natif sont visibles dans leurs appelants. **La broadphase native, le solveur et le rendu ne sont pas séparables par ce profil managé.** Les attentes des autres threads ne doivent pas être mélangées au thread principal ; le script de synthèse isole celui qui exécute l’ennemi.

Les pourcentages inclusifs se recouvrent, mesurent du temps mural échantillonné et restent sensibles à la charge. Ils ne constituent donc pas une preuve que remplacer telle méthode ferait gagner un pourcentage donné de FPS. Cette passe localise des méthodes candidates, mais ne décompose pas encore les quelque cinq millisecondes supplémentaires de la foule mesurées au calme.

**Reproduit isolément :** les boucles d’observation, d’animation et de mouvement mesurées séparément n’allouent aucun octet managé après préparation dans ces séries. Cela exclut un besoin de réécrire ces boucles pour une fuite d’allocations supposée ; cela ne les rend pas gratuites en CPU/natif. Les durées brutes sont conservées mais non utilisées sous charge pour classer les gains.

L’instrumentation facultative `--audit-observer-period` permet maintenant une vraie comparaison de l’observateur à chaque image contre une inspection décimée. Par défaut, son comportement antérieur est conservé. Les minima de population du mode décimé portent seulement sur les points inspectés ; on ne les présente pas comme un contrôle exhaustif de chaque frame.

### Défaut nouveau : les états temporaires n’expirent plus au loin

**Reproduit isolément dans les deux processus.** Sur une Ombre, appliquer un ralentissement ×0,5 de 2 s et une désorientation de 2 s, puis exécuter 600 ticks de 1/60 s en conservant la distance imposée :

| Distance du joueur | Ralentissement restant après 10 s | Facteur de vitesse | Désorientation restante |
|---|---:|---:|---:|
| 100 px | 0 s | 1 | expirée |
| 1 500 px | **2 s** | **0,5** | **2 s** |

**Cause vérifiée :** le retour anticipé au-delà de 600 px exécute ignite/bleed, mais saute `ProcessSlowDecay` et `ProcessDisorient`. La décroissance du recul et la régénération des modificateurs sont également situées dans le chemin proche ; leur effet exact à distance n’a pas été reproduit séparément.

Ce défaut peut augmenter le temps de résidence des ennemis ralentis derrière le joueur et réintroduire un état ancien à leur retour. **Son effet sur la population et les performances d’une run nomade n’est pas mesuré.**

**Piste :** rendre explicite le contrat des horloges qui continuent à distance, celles qui se suspendent et celles qui s’annulent. Ensuite seulement étaler les décisions coûteuses, sans ralentir les états temporels.

**Validation qui décide :** comparer un même état en continu puis avec éloignement/retour ; tester slow, désorientation, recul, DOT, régénération et annonces. Ajouter pause, hitstop, mort et recyclage. Le futur A/B de foule doit garder populations, trajet, dégâts et zoom comparables.

## 7. Premières apparitions : préchauffage visuellement nul, couverture incomplète

**Mesuré — GL Compatibility, viewport 256×256 mis à jour en continu.** Les huit chemins sont extraits de `GameBootstrap.WarmupShaders` ; les mêmes matériaux et `icon.svg` sont réutilisés dans les deux placements. Sur cinq images par étape, ordre hors champ/dans le champ/dans le champ/hors champ : **0/8/8/0 objets et appels de dessin** à chaque étape, sans exception de shader.

**Déduit du code et du contrat moteur :** le placement `(-9999, -9999)` ne fait pas le rendu annoncé par le commentaire. La [documentation officielle Godot](https://docs.godotengine.org/en/stable/tutorials/performance/pipeline_compilations.html) demande, pour Compatibility, d’afficher matériaux et particules au moins une image dans le champ. L’expérience établit l’absence de soumission de ces sprites ; elle ne prouve pas qu’aucune partie de compilation n’a lieu au chargement ni qu’un autre objet n’a pas déjà chauffé un shader.

La liste comporte **8 des 19 fichiers `.gdshader`**. Parmi les 11 absents, un concerne le Hub ; les **10 absents utilisés en run** sont `ground`, `path`, `pixel_fx`, `light_column`, `prop_forget`, `prop_trample`, `erasure_veil`, `echo`, `crisis_omen` et `iridescent_fluid`. Les références exactes figurent dans `shader-coverage.json`. Un fichier shader n’équivaut pas à une seule variante : sprites, particules et configurations de matériau doivent être représentés. Certains absents sont naturellement rendus au chargement du monde ; la liste manquante n’est pas une preuve de dix saccades futures.

**Piste :** une petite scène réellement rendue sous l’overlay, avec objets représentatifs et particules en émission ; attendre un signal de fin de rendu, pas seulement un timer de 0,1 s. Tester qu’un overlay couvrant la scène ou un viewport non consommé n’annule pas le travail.

**Validation qui décide :** cache pilote isolé froid/chaud, variantes A/B alternées, première attaque/mort/colonne/crise/écho/Effacement enregistrée. Mesurer les compilations et les images correspondantes. **Cette attribution à cache froid n’est pas faite ici** : le test rendu prouve la visibilité, pas le nombre de millisecondes perdues au premier effet.

Le préchargement des animations déjà livré reste acquis. Le renouvellement des `ShaderMaterial` à la réinitialisation des ennemis est toujours visible dans le code ; sans attribution des allocations sur le banc de renouvellement, il reste derrière le cône dans l’ordre proposé.

## 8. Effacement : coût borné par les zones suivies, erreur de rattrapage indépendante

### Zones à zéro toujours revisitées

**Reproduit isolément**, parcours sinusoïdal de 30 minutes à l’intérieur d’une emprise finie (x ±5 000, y ±2 200), vitesse maximale d’environ 103 px/s, update de 0,5 s. Ce trajet ignore les obstacles et ne remplace pas un bot nomade.

| Temps simulé | Cellules mémorisées | Cellules à zéro |
|---|---:|---:|
| 5 min | 4 693 | 0 |
| 10 min | 6 522 | 0 |
| 20 min | 6 555 | 0 |
| 25 min | 6 593 | 0 |
| 30 min | 6 753 | 6 744 |

Au total, **21 195 630 passages de cellules** dans 3 600 mises à jour. À la fin, **99,87 % des cellules** sont à zéro mais restent revisitées deux fois par seconde. Le compteur `zero_visits_after_update` mesure les cellules nulles **après** traitement : il inclut leur première arrivée à zéro et ne doit pas être présenté comme un nombre exact d’itérations évitables.

À buffers chauds, les séries de 100 appels avec **841 puis 6 000 cellules déjà à zéro** allouent chacune **68 000 octets** dans les appels. La taille de la collection ne multiplie donc pas les allocations dans ce test ; elle multiplie surtout le travail parcouru. Les durées par update restent dans les JSON, sans conclusion de débit sous charge. La texture de mémoire demeure minuscule et ne justifie pas un chantier de compression.

**Piste :** ensemble des cellules encore susceptibles d’évoluer, avec réactivation lors de la stabilisation d’un lieu ; ou évaluation paresseuse par horodatage. La deuxième option est plus risquée : distance au joueur, crises et changements de multiplicateur modifient l’intégrale du déclin. Ne pas prétendre qu’une simple date remplace exactement toute l’histoire.

**Validation :** mêmes phases, mémoire, événements de Failles et effets de Mémoriaux sur un enregistrement de trajet ; compter cellules visitées et uploads réellement changés. À ce stade, réduire les cellules à zéro est une amélioration de late game, pas le premier chantier pour le début de run.

### Temps excédentaire perdu

**Reproduit isolément**, 600 s au même point, configuration courante :

| Fréquence d’appel imposée | Updates exécutées | Effacement global | Mémoire de la cellule du joueur |
|---|---:|---:|---:|
| 60 Hz | 1 200 | 30,803 % | 87,998 % |
| 30 Hz | 1 200 | 30,804 % | 87,998 % |
| 144 Hz | 1 200 | 30,806 % | 87,998 % |
| 1 Hz | **600** | **15,404 %** | **93,999 %** |

**Cause :** `_updateTimer` est remis à zéro et le déclin reste calculé avec `_updateIntervalSec`, même si le `delta` accumulé est plus long. Les dégâts du Néant utilisent aussi cet intervalle fixe ; la perte de dégâts est déduite du code, le joueur invincible de la fixture ne la mesure pas.

**Piste :** conserver le reste temporel et définir le rattrapage des longues images. Une boucle de rattrapage illimitée peut créer une nouvelle saccade et émettre des cascades de signaux ; une intégration agrégée peut sauter une phase et donc un événement. Le contrat doit être choisi avant l’optimisation du parcours.

**Validation :** trajectoire enregistrée avec delta régulier, jitter, pics de 100/500/1 000 ms, pause et hitstop. Comparer intégrale de l’oubli, dégâts et événements, avec une limite explicite au travail de rattrapage.

## 9. Monde et physique : distinguer nœud, corps et forme

**Mesuré dans Main, parcours récursif de la carte seed 221092026 :**

- **10 694 `EnvironmentProp`**, donc autant de corps `StaticBody2D`.
- **2 318 formes de collision**, lues via `PhysicsServer2D.BodyGetShapeCount`.
- **8 376 corps sans forme**, tous en couche zéro ; **zéro corps en couche zéro avec une forme**.

Ces corps sans forme ont un coût de création/résidence, mais pas 8 376 obstacles géométriques à tester. Il faut mesurer la broadphase des **formes réellement enregistrées**, pas assimiler le nombre de nœuds au nombre de proxies de collision. `active_objects` ne donne pas non plus le nombre total de corps statiques.

**Reproduit isolément :** huit lots de 10 000 nœuds dans un processus headless, ordre Node2D/StaticBody2D/StaticBody2D/Node2D répété. Après la première séquence de chauffe, les deltas natifs sont stables : **19 782 568 octets pour Node2D**, **27 142 568 pour StaticBody2D sans forme**. Écart : **736 octets par corps**. L’extrapolation aux 8 376 décors non bloquants donne **6 164 736 octets, soit 5,88 Mio**. Cela ne mesure ni le RSS récupérable, ni la différence des wrappers C# d’une future implémentation, ni les caches de l’allocateur.

**Piste raisonnable :** composer le décor depuis un nœud visuel, avec un corps seulement quand il bloque. Gain mémoire borné, complexité modérée mais réelle : les placeurs, l’occlusion, le tri et les outils typent `EnvironmentProp`.

**Piste conditionnelle :** activer seulement les collisions des tronçons utiles. Elle exige une mesure préalable du coût natif des 2 318 formes. Le joueur, les projectiles et les ennemis lointains ont des besoins différents ; masquer un tronçon à la caméra ne suffit pas à décider qu’il ne doit plus bloquer.

**Validation qui décide :** dans le banc, détacher temporairement les seules formes très loin de toutes les interactions, comparer A/B alterné à trajectoire identique, profil natif du pas physique, mémoire et contacts. Puis tests aux limites de tronçons et sur projectiles rapides. **Le coût isolé de broadphase reste non mesuré.**

## 10. Ce qui change réellement par rapport au premier audit

| Question du 27 | Apport de cette passe | Ce qui reste ouvert |
|---|---|---|
| Foule : coût sans méthodes | Profils managés, A/B de densité au calme ; coût de l’inspection et pic initial du banc identifiés | Profil natif, inventaire avant mesure, aléatoire des quêtes fixé, bestiaire plus varié |
| Préchauffage probablement hors champ | 0 contre 8 objets rendus, couverture documentée | Compilation froide et saccades associées |
| Cône jamais mesuré | 12 000 impacts, 3,05–3,18 Mo/4 s actives | Procs, churn et longue run réelle |
| Zones d’Effacement revisitées | Parcours fini 30 min, 6 753 cellules ; erreur temporelle reproduite | Bot réel, rattrapage exact des transitions |
| ~10 000 corps de décors | 2 318 formes ; coût sans forme ≈736 octets/corps | Temps du serveur physique natif |
| Vingt retours au Hub absents | 20 cycles, zéro orphelin, pas d’accumulation de nœuds | Runs longues, bilan, récompenses et GPU |
| Traitement lointain à ralentir | Défaut de durée des statuts reproduit | Coût démographique et contrat de tous les états |

## 11. Effets : contrôler les cadences avant d’ajouter des pools

**Déduit du code :** `FxBudget` et le plafond d’impacts de `PlayerAttackFx` se réinitialisent sur `Engine.GetProcessFrames`. Si plusieurs ticks physiques rattrapent une seule image, ils partagent le même budget ; à débit élevé, le plafond nominal par seconde augmente. Les plafonds par image ne sont pas des plafonds d’objets vivants ni des quotas par seconde.

Ce choix peut être volontaire pour protéger chaque image, mais il ne doit pas servir de contrat de proc ou de cadence audio. Le diagnostic du cône montre qu’on peut respecter un budget de dessin tout en répétant plusieurs milliers de demandes en amont.

**Validation :** même enregistrement d’impacts à 30/60/144 images/s, avec rattrapage physique ; compter demandes, effets accordés/refusés, effets vivants et sons effectivement joués. Préserver les annonces de danger et les critiques. Aucun chantier de remplacement des VFX déjà recyclés n’est proposé ici.

## 12. Ce qui ne mérite pas un chantier prioritaire

- **ECS, C++, autre renderer :** aucune preuve nouvelle ne les justifie.
- **Carrés de distance partout :** pas de mesure d’un coût significatif ; plusieurs distances servent au déclin ou à l’interpolation.
- **Refaire atlas, orbes dormantes, explosions, animations préchargées ou feu :** déjà traité ou travail en cours, hors périmètre.
- **Supprimer 10 000 collisions supposées :** il y a 2 318 formes dans cette carte ; commencer par la bonne population.
- **Optimiser la copie d’une texture de mémoire de 1 Kio :** regarder d’abord les parcours et leur cadence.
- **Réécrire les pools pour le seul RSS après chargement :** zéro orphelin sur vingt cycles ; distinguer caches et croissance.
- **Grille spatiale généralisée immédiate :** justifier par les builds à multiples recherches, puis traiter invalidation et ordre des effets.
- **Baisser la physique globale à 30 Hz ou dormir davantage l’IA sans contrat de temps :** le défaut de statut à distance montre le risque concret.

## 13. Ordre d’exécution proposé

Lots proposés, **non implémentés et non cochés dans la roadmap V2**. Un seul lot de correction à la fois ; les suites déjà engagées conservent leur périmètre.

| Lot | Objet | Pourquoi / sortie attendue |
|---|---|---|
| **6A — Horloges et distance** | Expiration des statuts lointains, contrat d’Effacement et rattrapage borné | Robustesse préalable à tout LOD ; scénarios temporels identiques avec/sans éloignement ou hitch |
| **6B — Impacts continus** | Dissocier dégâts, procs et feedback du cône | Réduire appels/allocations à DPS et attribution conservés ; budget et sémantique des procs explicités |
| **5B — Préchauffage réellement rendu** | Scène de variantes sous l’overlay | Compteurs de soumission non nuls ; validation cache froid/chaud sur premiers effets |
| **3B — Foule sans biais d’observation** | Inventaire avant mesure, quêtes figées dans le banc, profil natif, familles et CrowdZoom réel | A/B observateur déjà fait ; attribution native avant choix entre interopération, mouvement, animation et requêtes spatiales |
| **6C — Cellules actives de l’Effacement** | Retirer du travail les cellules stables sans perdre leur état | Même histoire de phases/Failles/Mémoriaux ; visites proportionnelles aux cellules utiles |
| **6D — Corps des décors non bloquants** | Corps optionnel et décision séparée sur collisions lointaines | Vérifier le gain mémoire attendu ~5,88 Mio ; n’engager le streaming physique que si mesuré |

6A regroupe deux contrats temporels, à réaliser et valider en deux sous-étapes distinctes. 3B est une suite de **mesure**, pas une autorisation de refonte. Le gain possible de 6D est modeste par rapport à la résidence totale : il peut rester différé.

## 14. Recette de performance à conserver

Les [scripts et commandes exactes](audits/performance-2026-09-28/README.md) font partie du livrable. Les références à conserver pour décider des futurs correctifs :

1. **Cône** : A/B/B/A, mêmes cibles/seed/build ; allocations et appels, DPS/procs, puis vraie run avec renouvellement.
2. **Foule** : 60/120/240 ennemis, observateur complet/décimé ; intervalles d’images bruts, charge suivie pendant chaque passe ; profiler séparément afin de ne pas appeler « FPS de production » ceux du profil.
3. **Horloges** : distance fixe puis aller-retour ; deltas réguliers, irréguliers et gros hitches ; comparer états et événements plutôt qu’une capture finale seule.
4. **Effacement** : vingt à trente minutes avec bot nomade, crises et stabilisations, et seed identique des deux côtés ; compter zones suivies/actives/nulles et transitions.
5. **Shaders** : cache froid isolé, puis chaud, plusieurs ordres ; première apparition de chaque famille, tracée avec rendu effectif.
6. **Physique** : nombre de corps **et** de formes ; ablation des seules formes lointaines ; profil natif avant architecture nouvelle.
7. **Durée de vie** : vingt cycles après scènes riches, mémoire après GC/stabilisation, ressources, orphelins et RSS ; ne pas exiger que le RSS revienne au Hub initial.

**Validation de cet audit :** build C# sans warning ; diagnostics officiels sans erreur inattendue ; JSON bruts et piles conservés ; les changements de production présents au début sont exclus du commit. Le détail du smoke test et de la vérification finale se trouve dans le dossier de preuves.

**Ce que cette machine ne permet pas encore de conclure dans cette session :** un gain de FPS d’un correctif de production, la part native exacte de la broadphase/du solveur/rendu, le coût des compilations pilote à froid et la tenue à 60 FPS sur une configuration mid-range. Le GPU de cette machine est plus puissant que la cible minimale visée ; un bon résultat ici ne suffirait pas à valider celle-ci.
