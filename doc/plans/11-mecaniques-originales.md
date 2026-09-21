# Plan 11 — Mécaniques originales à prototyper

Version 0.2 · Statut : **propositions à examiner, aucune mécanique approuvée**.
Références : V2 §2/8/9/13, plans 01/03/05/10. L'originalité recherchée est propre à l'expérience Vestiges ; aucune revendication de nouveauté mondiale.

## 1. Règle de sélection

Une bonne idée doit rendre déplacement, exploration ou build plus intéressant avec une règle compréhensible et peu de texte. Prototyper une seule idée à la fois, sur une séquence courte, avant assets définitifs. Comparer à la même situation sans mécanique ; rejeter si le joueur ne perçoit pas un choix nouveau.

Le monde continue de s'effacer : aucune proposition n'arrête indéfiniment ce processus et aucune ne réintroduit base, craft ou farm stationnaire nécessaire.

## 2. Phase 0 — Points d'appui

- Player et module de mobilité à construire dans 01 : positions, attaque et fin d'action ; le signal « mobilité terminée » est une extension proposée, pas présent.
- EventBus : EnemyKilled, ZonePhaseChanged, ChestOpened ; ErasureManager et états de mémoire existants.
- VfxFactory : échos/traînées existants ; utiliser leurs patterns après audit des allocations.
- BiomeTileMapper, WorldGenerator et routes : masques/positions et terrain déterminé par seed.
- Objets de 05 : pile agrégée et attribution de procs à créer avant une version cumulable.
- Récompenses de 06 : accès directs ; pas de clé narrative imposée.

## 3. Idée A — Rémanence offensive du mouvement

**Pitch :** le mouvement laisse une courte mémoire que le prochain coup fait résonner. La trajectoire de déplacement devient une partie du build.

**Proposition :** un objet rare, ou le passif d'un seul personnage à choisir, enregistre le trajet de la mobilité. Le prochain coup direct dans les deux secondes provoque une réplique locale le long de ce trajet. Le joueur peut traverser latéralement un groupe puis tirer pour exploiter sa trace.

Contrat candidat :
- Fin de mobilité → trajet borné en longueur réelle et nombre d'échantillons (huit maximum pour l'essai).
- Prochain coup dans la fenêtre → une zone d'écho brève ; une cible ne prend l'effet qu'une fois par activation.
- Proposition initiale : 30 % des dégâts directs de référence ; si objet cumulable, chaque exemplaire renforce cette puissance, sans créer davantage de chemins.
- L'écho ne déclenche pas un autre écho, ne relance pas les chaînes de procs et ne modifie ni position ni collisions du joueur.
- La trace s'efface ensuite ; aucun empilement de pièges permanents.

### Lot prototype A

1. Reprendre mobilité 01 et attribution de coup 05 ; enregistrer un petit buffer de positions préalloué.
2. Dessiner la trace avec les effets d'écho existants et un signal bref « prête/consommée ».
3. Tester mêlée/distance, cible déjà morte, trajet contre mur, fin de run et piles importantes.
4. Comparer les trajectoires prises avec/sans effet ; documenter si l'objet incite seulement à spammer le bouton.

**Garder si :** le joueur prépare volontairement un placement et reconnaît le résultat sans tutoriel long.
**Abandonner/modifier si :** trace illisible, doublon des Gantelets d'Écho, avantage dominant, dash obligatoire à chaque recharge.
**Dépendances :** 01/02/05 ; coût relatif moyen.
**Recommandation :** premier candidat, car il relie directement les demandes de mobilité et de juiciness.

## 4. Idée B — Un butin à sauver de l'Effacement

**Pitch :** une récompense du monde est encore récupérable, mais sa zone se défait. Le détour doit être choisi et exécuté, pas subi.

**Proposition :** certains coffres/porteurs apparaissent avec une valeur visible et une mémoire locale déjà fragile. Le joueur peut continuer vers une route sûre ou tenter la récupération avant disparition.

Contrat candidat :
- Occasion optionnelle annoncée visuellement avant son accès, jamais indispensable à une quête tirée au hasard.
- Position générée sur trajet praticable selon 10 ; temps/mémoire restants suffisants pour un joueur sans mobilité avancée.
- Récompense prise = acquise normalement ; seul un butin encore dans le monde peut disparaître. Aucun objet possédé n'est volé.
- Un seul événement de ce type visible à la fois au prototype ; cadence contrôlée par données.
- Récompense choisie dans le pool disponible 05, rareté/valeur à comparer au risque. Pas de ralentissement d'ouverture qui transforme le défi en attente immobile.

### Lot prototype B

1. Reprendre état de zone et placement coffre, puis définir entrée/expiration/récompense unique.
2. Montrer l'urgence via matière/lumière/son du monde, avec indicateur compact si nécessaire.
3. Comparer au coffre normal : détours, risques acceptés/refusés, compréhension de la perte.
4. Tester arrivée à expiration, deux événements simultanés, pause et fin de run.

**Garder si :** le joueur anticipe le risque et accepte parfois de renoncer ; la récupération est satisfaisante.
**Abandonner/modifier si :** course obligatoire, perte incompréhensible, frustration répétée ou retour dangereux forcé.
**Dépendances :** 03/05/10 ; coût relatif faible à moyen.
**Recommandation :** second candidat, avant un gros nouveau système global.

## 5. Idée C — Chemins rémanents

**Pitch :** les pas rappellent brièvement un ancien chemin, une rue ou un passage. Le monde réagit et devient lisible sans panneau de texte.

**Prototype initial purement visuel :** en traversant certains tronçons, le joueur révèle pendant quelques secondes leur ancienne continuité. Cela suggère un détour ou un lieu ; aucune route jouable ne surgit hors de la géométrie validée.

Contrat candidat :
- Tracés prévus par seed et cohérents avec collisions ; révélation locale, brève et limitée.
- Palette distincte du danger et des projectiles ; indication observable en effets réduits.
- La mémoire réelle d'une zone continue de diminuer : ce rappel n'annule pas l'Effacement.
- Pas de bonus automatique de vitesse ni de récompense dans le premier essai ; on mesure d'abord orientation et plaisir du passage.

### Lot prototype C

1. Reprendre routes/masques de 10 et un overlay mis en cache.
2. Déclencher à proximité du joueur avec traitement local et durée en données.
3. Tester si le joueur s'oriente davantage par le monde et moins par le HUD.
4. Si le résultat est bon, proposer ensuite une extension d'exploration ; elle nécessite une validation distincte.

**Garder si :** détail mémorable, direction mieux comprise, monde cohérent.
**Abandonner/modifier si :** concurrence avec l'Effacement, faux passage, surcharge lumineuse.
**Dépendances :** 08/10 ; coût relatif faible pour l'essai visuel.

## 6. Recette et sélection

Pour chaque candidat : build/smoke si applicable, séquence filmée, trois situations et au moins une combinaison extrême, puis retour de Raphaël. Noter intérêt, compréhension, fatigue, performance et coût de généralisation.

Livrable final : « garder », « retravailler » ou « abandonner », avec preuve et changement précis dans le plan propriétaire. Ne pas ajouter ces trois mécaniques à la roadmap comme engagements avant sélection. Une seule peut suffire à renforcer l'identité du jeu.

