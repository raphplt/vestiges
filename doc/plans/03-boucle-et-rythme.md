# Plan 03 — Boucle de jeu, rythme et envie de relancer

Statut : **direction validée ; priorité à la difficulté initiale et cadence XP** · Priorité : P0 · Dépendances : première passe 01/02.
Références : V2 §5–14/16/25 ; [dossier](README.md) ; [diagnostic](00-etat-des-lieux.md).

## 1. Objectif

Faire de chaque run une succession de décisions et de transformations perceptibles : choisir où avancer, engager un combat, risquer un détour, orienter son build, traverser une Résurgence, découvrir quelque chose et comprendre pourquoi repartir.

**Retour utilisateur prioritaire : le début est trop facile, les niveaux arrivent trop vite et la menace ne se ressent pas.** Revoir les mécaniques existantes est autorisé lorsqu’une comparaison montre un progrès. L’objectif est une menace lisible et des décisions fréquentes, avec des niveaux plus espacés, sans rallonger artificiellement les combats.

« Ne jamais se lasser » est une ambition, pas un critère testable. On mesurera plutôt les périodes sans choix utile, la diversité des builds essayés et les raisons spontanées de relancer.

## 2. Phase 0 — État existant et références

Lire [CrisisManager.cs](../../scripts/Events/CrisisManager.cs), [EndgameManager.cs](../../scripts/Events/EndgameManager.cs), [ErasureManager.cs](../../scripts/World/ErasureManager.cs), [SpawnManager.cs](../../scripts/Spawn/SpawnManager.cs), [RunTracker.cs](../../scripts/Infrastructure/RunTracker.cs) et les JSON de [scaling](../../data/scaling/crises.json).

Constats :
- Première Résurgence à 240 s ; intervalle configuré 240 ±45 s, durée 70 s, avertissement 20 s. L'intervalle est reprogrammé à la fin de la crise : le cycle nominal début→début est 310 s.
- La V2 attend des crises toutes les 3–5 min, un signal 30 s avant et une accalmie récompensée.
- Aucun branchement trouvé de la crise dans le calcul d'Effacement, ni de récompense post-crise garantie sur `CrisisEnded`. À confirmer lors du parcours en jeu.
- Le boss Indicible et une transition endgame existent. Des multiplicateurs de spawn/scaling existent aussi ; leur présence ne prouve pas leur intérêt ni leur stabilité en run longue.
- `RunTracker` fournit déjà pression, DPS glissant, ennemis, kills et dégâts par minute.

APIs à reprendre : `CrisisManager.EnableEndgameTempo()`, propriétés `TimeUntilNextCrisis`/`CrisisTimeRemaining`, signaux `CrisisWarning`/`CrisisStarted`/`CrisisEnded`, `GameManager.SetRunPhase`. Un orchestrateur de récompense post-crise serait une extension, pas une API existante.

## 3. Décisions proposées

| Sujet | Proposition pour le premier essai |
|---|---|
| Boucle | Conserver exploration → combat → puissance → fuite de l'Effacement |
| Début | Combat dans les 10 premières secondes, conforme V2 |
| Premier choix | Cible d’essai 45–75 s pour le premier niveau ; les décisions de combat commencent avant. Ajuster après mesures, sans avalanche de niveaux |
| Exploration | Montrer des opportunités alternatives et leur risque, sans itinéraire imposé |
| Résurgence | Définir les intervalles début→début, première à environ 4 min |
| Accalmie | Fenêtre de 30 s de récompense et baisse relative de pression, conformément V2 |
| Boss | Tester d'abord l'Indicible déjà implémenté comme candidat au climax |
| Durée | V2 : débutant 5–10 min, régulier 15–25 min, expert au-delà ; aucune mort forcée |
| Relance | Choix explicite : record, essai de build, quête ou lore |

Choisir l'Indicible comme cible finale ferme les alternatives Convergence/Choix de V2 §14 : décision à valider, pas actée ici.

## 4. Lots d'action

### Lot A — Établir la référence jouable

1. Reprendre le suivi `RunTracker` et l'historique pour enregistrer temps actif, choix de niveau, coffre, Autel, découverte, Résurgence et mort.
2. Ajouter seulement les marqueurs manquants : délai avant première récompense, périodes sans opportunité, choix refusés, raisons des détours.
3. Jouer trois seeds fixes avec un même personnage, puis une seed découverte librement.
4. Noter séparément incidents techniques, incompréhensions et ennui ; filmer les passages concernés.

**Livrable :** frise de run annotée avec moments forts/faibles.
**Vérification :** temps actif cohérent avec 02 ; monde comparable sur même seed. Les crises et spawns utilisent aussi des tirages non fixés par cette seed : contrôler leur calendrier pour une comparaison stricte, sinon répéter les essais et consigner leur variabilité.
**Garde-fou :** une simulation ou un compteur de DPS ne mesure pas le plaisir humain.

### Lot A2 — Menace initiale et XP : diagnostic puis essais

**Sources complémentaires :** PlayerProgression.CalculateXpForLevel utilise en C# une courbe 20 × niveau^1,35 avec correction initiale ; le premier seuil est 33 XP. Un Rampant d’Ombre donne 10 XP, un Charognard 8 : quatre ou cinq éliminations peuvent suffire, sans autre gain/modificateur. Ce calcul ne mesure pas le temps réel d’un niveau.

SpawnManager possède une maintenance de densité toutes les 0,25 s en plus de l’intervalle de spawn. Ne pas déduire le débit réel des seuls 1,9 s initiaux. Le Traqueur a une vitesse 240 contre 60 pour un Rampant d’Ombre avant multiplicateurs : hypothèse de poursuite trop facile à tester, pas diagnostic unique.

**Retour de Raphaël du 23 septembre (après le correctif de la flèche) :** du niveau 1 au niveau 5 en moins de 30 secondes, sans aucune difficulté. Son hypothèse : un bestiaire trop passif, trop de corps à corps, pas assez d'attaques à distance, et pas seulement des projectiles simples. Constats et propositions dans le [plan 07](07-bestiaire-et-rencontres.md#retour-de-raphaël-du-23-septembre-et-constats).

Éléments chiffrés :
- Il faut **407 XP** cumulés pour atteindre le niveau 5 (33 + 78 + 126 + 171).
- Tous les ennemis sont plus lents que le joueur.
- La session du 23 septembre (Carrière Effondrée, Traqueur) a atteint le niveau 7 d'une traite, face à des ennemis tous au corps à corps.
- Le log ne contenait pas d'horodatage. `RunTracker` journalise désormais `[RunTracker] Niveau N à X s (K éliminations)`. Cette durée est murale et inclut les écrans de choix ; elle sert de repère, pas de mesure du temps actif.

**Essai menace lancé le 23 septembre :** Présage et bond du Charognard, dans les pools de début de tous les biomes ([compte rendu](07-bestiaire-et-rencontres.md#premier-essai-menace--23-septembre-2026)). À jouer avant tout changement XP.

**Ordre des essais révisé :** l'essai menace (étape 4) passe avant l'essai XP (étape 3), conformément à l'hypothèse de Raphaël. On garde un seul groupe de paramètres à la fois. Si la menace revue ne suffit pas à espacer les premiers niveaux, on applique ensuite +25 % sur les seuils XP.

1. Mesurer sur les cinq premières minutes : temps par niveau, XP/ennemi et sources annexes, fréquence des écrans de choix, dégâts reçus, temps d’élimination, distance des menaces et durée passée simplement à reculer.
2. Externaliser la courbe XP et les réglages initiaux dans les données selon les loaders du projet. Garder un preset référence pour comparer.
3. Essai isolé XP : augmenter les seuils initiaux (premier essai +25 %, puis +50 % si justifié), sans encore changer les ennemis. Mesurer si chaque niveau redevient gratifiant et si le build se construit assez tôt ; ajuster aussi récompenses de quête/XP pour éviter de contourner la courbe.
4. Essai isolé menace : rapprochement lisible, attaques anticipables et interception/rôles, vitesse relative, couverture d’espace. Comparer avant d’augmenter les PV : plus de vie seule peut rendre le combat laborieux sans danger.
5. Combiner le meilleur réglage XP et menace, puis retester avec le dash/les mobilités de 01 et avec piles d’objets de 05. Ajuster le burst de puissance et le loot si une arme annule toute menace immédiatement.
6. Ne pas lisser tous les risques : une erreur volontaire doit coûter, une bonne esquive doit réussir. Vérifier les premières secondes avec nouveau profil et personnage lent, sans tuer sans avertissement.

**Livrable :** trois courbes comparées (référence, XP revue, menace revue), puis preset combiné avec observations.
**Vérification :** premières décisions d’évitement dans la première minute ; joueur inattentif menacé, joueur actif capable d’éviter ; moins de pauses de niveau et gains plus significatifs. Pas de taux de mort cible imposé sans panel ; documenter la dispersion entre débutants et habitués.
**Garde-fou :** n’ajuster qu’un groupe de paramètres à la fois ; aucun début inoffensif conservé pour préserver une ancienne prescription « level-up rapide ». Si la répétition des choix ralentit le jeu, revoir leur structure au lieu de seulement retarder les récompenses.

### Lot B — Départ et décisions d'exploration

1. Reprendre les placements coffres/Autels/POI du monde existant ; contrôler accès et visibilité du premier objectif.
2. Concevoir trois situations : choix sûr, détour risqué, poursuite d'un objectif de build.
3. Vérifier que la minimap et les signaux du monde donnent assez d'information pour choisir.
4. Régler densité, XP et récompenses dans les JSON par petites variations isolées.
5. Faire comprendre l'Effacement par conséquence visible, sans tutoriel long.

**Vérification :** le joueur peut expliquer son détour et le danger de rester ; aucune traversée prolongée sans choix ou combat utile.
**Garde-fou :** ni parcours automatique vers tous les coffres, ni obligation de visiter chaque POI.

### Lot C — Résurgence puis respiration

1. Écrire la sémantique du calendrier et reprendre `CrisisManager` pour la respecter, sans chevauchement accidentel.
2. Relier l'avertissement à un signal sonore et visuel lisible dans toutes les phases concernées.
3. Tester l'accélération temporaire d'Effacement promise en V2 avec des paramètres dédiés ; revenir au rythme normal en sortie et à l'arrêt de run.
4. À la fin, proposer le coffre rare garanti dans une zone atteignable et la fenêtre Essence prévue par la V2.
5. Faire persister une possibilité de fuite : la respiration réduit la pression sans créer de base sûre.

**Vérification :** chronologie mesurée, une récompense par crise, emplacement accessible, expiration correcte des bonus, sortie mort/pause sans état bloqué.
**Garde-fou :** pas de cadeau dans le Néant inaccessible, pas de cumul permanent de multiplicateurs.

### Lot D — Montée en puissance et diversité

1. Reprendre trois armes et quelques objets de 05 ; établir deux builds aux comportements visiblement différents.
2. Mesurer les occasions de choix et les écarts de puissance, pas seulement les valeurs de DPS.
3. Contrôler l'économie des Autels : achat utile, alternative réelle, pas de soin obligatoire à chaque passage.
4. Introduire les rôles ennemis de 07 de façon à mettre les builds à l'épreuve.
5. Ajuster après plusieurs essais, en conservant une version des paramètres comparés.

**Vérification :** le joueur identifie au moins deux tournants de son build ; pas de choix systématiquement supérieur dans les essais.
**Garde-fou :** ne pas accroître la taille du catalogue pour masquer des offres sans intérêt.

### Lot E — Climax et endgame

1. Tester arrivée, télégraphie, esquive et mort de l'Indicible dans le mouvement nomade.
2. Vérifier la transition unique au post-boss, les récompenses et le score.
3. Introduire variation des compositions/événements avant d'augmenter uniquement les PV.
4. Régler la valeur supplémentaire de l'endgame avec le barème de 02 et versionner les règles.
5. Tester charge et nombres extrêmes sur un accès forcé, puis confirmer l'accessibilité en vraie run.

**Vérification :** boss compréhensible, endgame distinct, sortie de run fiable, performance stable.
**Garde-fou :** l'accès debug n'est pas une preuve d'équilibrage ; les runs debug ne pourront pas être classées en 09.

## 5. Playtest et acceptation

Petit panel proposé : Raphaël et 2–3 testeurs, trois tentatives par personne si possible. Ce panel est qualitatif, pas une preuve statistique de rétention.

Questions après la run : « Pourquoi ce trajet ? Quel choix a changé ton build ? Pourquoi es-tu mort ? Que veux-tu essayer ensuite ? » Consigner verbatim et instants d'hésitation.

Accepter le lot lorsque les commandes ne gênent plus, le début menace réellement et les niveaux sont moins précipités, la tension alterne avec des récompenses, la mort paraît explicable et plusieurs raisons de rejouer émergent. Si seul le déblocage motive malgré un combat ennuyeux, revenir au lot 02.

Vérifications techniques du dossier ; roadmap A/B/C/D. Reporter les validations de fun explicitement dans §25 après essais, pas à la seule présence des systèmes.

## 6. Améliorations structurelles autorisées

Toute mécanique existante peut être remise à l’épreuve : loot, densité, chronologie des crises, interaction Autel, progression, génération. Pour chaque refonte : problème observé, solution candidate, coût, comparaison contrôlée, critères garder/abandonner et migration si nécessaire. Terrain et navigation sont traités en 10 ; les innovations de 11 doivent prouver leur intérêt dans cette boucle. La V2 reste nomade et sans craft/base/jour-nuit.
