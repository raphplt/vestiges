# Plan 02 — Juiciness, score vivant et récompenses

Statut : **direction validée avec corrections ; bilan détaillé proposé ; chantier juiciness prioritaire du 25 septembre (§7)** · Priorité : P0 · Dépendances : premier lot 01, cadrage visuel 08.
Références : V2 §10/15/16/21 ; Bible §7/8/9 ; [dossier](README.md).

## 1. Diagnostic vérifié

- Le score est déjà affiché : `HUD.BuildScoreArea()` utilise une police taille 12 et une zone de texte étroite ; `OnScoreChanged(int newScore)` remplace le texte sans animation. Voir [HUD.cs](../../scripts/UI/HUD.cs), vers 576/878.
- `ScoreManager.CurrentScore` inclut un score de survie calculé à la lecture. Les notifications existantes ne garantissent pas que le HUD suive le temps écoulé entre deux événements. Voir [ScoreManager.cs](../../scripts/Score/ScoreManager.cs), propriété `SurvivalScore` et émission de `ScoreChanged`.
- `RunTracker.RunDurationSeconds` utilise le temps mural (`Time.GetTicksMsec`) : définir une durée de jeu actif commune au score, aux quêtes et au bilan, excluant pauses et figée à la mort. Voir [RunTracker.cs](../../scripts/Infrastructure/RunTracker.cs), vers 61.
- Les effets existent : impacts, traînées, collecte XP, level-up, dissolution, secousses. Le problème n'est donc pas seulement la quantité d'effets.
- `Player.PlayAttackFeedback(bool isMelee, Vector2 direction)` anime `_visual`, un Polygon2D masqué lorsque le sprite joueur est chargé : il faut vérifier le résultat visible dans les deux chemins.
- `ScreenShake.Hitstop(float duration = 0.04f)` est explicitement neutralisé dans le code. Ne pas le réactiver sans comprendre et retester le ressenti.

Ces constats sont statiques ; l'intensité réellement ressentie reste à filmer et à évaluer.

## 2. Décisions de Raphaël intégrées

- Score visible pendant le gameplay actif, nombre lisible et gains groupés. **Aucune progression vers un record, aucun seuil à battre ni annonce de dépassement pendant la run, pause comprise. Comparaison et célébration uniquement au bilan final.**
- Trois intensités : courant, important, exceptionnel. La rareté doit se reconnaître par animation/forme/son autant que par couleur.
- Réglage des secousses et effets réduits ; pas de clignotement indispensable à la compréhension.
- Premier lot sans nouveau combo ni multiplicateur : améliorer la perception et la fiabilité du score existant avant d'en changer l'économie.
- Aucun gel systématique du jeu sur chaque impact. Un éventuel arrêt très bref d'animation locale sera une expérience séparée.

## 3. Grammaire des récompenses proposée

| Événement | Réponse visuelle | Réponse sonore | Limite |
|---|---|---|---|
| Coup courant | Réaction brève de cible + impact local | Son de matériau/arme | Regroupement sous forte densité |
| Coup lourd ou critique | Silhouette/impact plus marqué | Accent distinct | Ne cache pas la télégraphie ennemie |
| Élimination | Dissolution orientée + départ du loot | Ponctuation courte | Pas de secousse à chaque mort |
| XP / Essence | Attraction, trajet, arrivée visible | Petite variation de hauteur | Voix simultanées plafonnées |
| Level-up | Signature autour du joueur + choix clair | Montée courte | Pas de long délai avant choix |
| Coffre rare | Anticipation puis révélation identifiable | Signature de rareté | Durée compatible avec le flow |
| Fusion / déblocage | Mise en valeur de ce qui change | Motif mémoriel | Une notification par gain |
| Record personnel au bilan final uniquement | Score final comparé à l’ancien record | Signature unique au bilan | Jamais dans le HUD ni la pause |
| Résurgence terminée | Relâchement visuel + récompense localisable | Résolution du motif | Ne couvre pas un danger restant |

## 4. Phase 0 — Contrats à reprendre

Sources : [EventBus.cs](../../scripts/Core/EventBus.cs), [VfxFactory.cs](../../scripts/Combat/VfxFactory.cs), [ScreenShake.cs](../../scripts/Combat/ScreenShake.cs), [Player.cs](../../scripts/Core/Player.cs), [ScoreManager.cs](../../scripts/Score/ScoreManager.cs), [HUD.cs](../../scripts/UI/HUD.cs).

APIs existantes à relire et réutiliser :
- `ScoreChangedEventHandler(int newScore)`, `EnemyKilledEventHandler(string enemyId, Vector2 position)`, `LevelUpEventHandler(int newLevel)`.
- `VfxFactory.CreateProjectileImpact(Vector2 position, Color color)`, `CreateXpCollectBurst(Vector2 position)`, `CreateLevelUpBurst(Vector2 position)`.
- `ScreenShake.AddTrauma(float amount)` et niveaux de particules existants.
- `ScoreManager.SaveEndOfRun()` et `BuildRunRecord()` pour garder le même score au HUD, au bilan et dans la sauvegarde.

Un événement de gain avec cause, un profil de feedback JSON et un pool supplémentaire seraient des **extensions à concevoir**, pas des APIs supposées disponibles.

## 5. Lots d'action

### Lot A — Score exact et lisible

1. Capturer le score au repos, après kill, coffre, POI, Résurgence et passage endgame.
   Reprendre le suivi de temps dans RunTracker pour exclure pauses/écrans qui suspendent la simulation et figer la durée finale à la mort ; vérifier tous ses consommateurs avant modification.
2. Centraliser la notification depuis le score autoritaire ; notifier le score de temps à cadence bornée quand sa valeur change, sans recherche de nœud par frame.
3. Distinguer valeur réelle et valeur affichée interpolée : l'animation ne doit jamais calculer la progression.
4. Regrouper les gains rapprochés ; point de départ proposé : fenêtre de 0,25 seconde, à ajuster visuellement.
5. Prévoir chiffres longs et multiplicateur expliqué ; conserver l’ancien record dans le snapshot final, sans le présenter avant la fin de run.
6. Reprendre le format du détail de score au bilan. Un arrêt de partie force l'affichage de la valeur finale exacte.
7. Placer les coefficients actuels dans des données selon le pattern des loaders du projet ; conserver les valeurs dans cette première passe.

**Vérification :** score évolue pendant 10 secondes sans kill ; HUD/bilan/historique concordent exactement ; pause et mort n'ajoutent pas de temps ; gros nombre sans débordement.
**Garde-fou :** pas de formule indépendante dans le HUD, pas de double comptage de Résurgence ou boss, pas de record comparé à une valeur déjà écrasée.

### Lot B — Combat perceptible

1. Reprendre le feedback existant et l'appliquer à la représentation réellement visible, avec un point d'ancrage visuel commun si nécessaire.
2. Définir un profil pour lame, arc et marteau : anticipation, mouvement, impact, réaction, son.
3. Préserver hitbox et timing de dégâts ; distinguer toute modification mécanique d'un ajustement d'animation.
4. Réserver les accents forts aux événements rares ; appliquer plafonds de densité et options existantes.
5. Examiner création/libération des nœuds VFX et mettre en pool les effets fréquents avant multiplication.

**Vérification :** les trois armes sont reconnaissables sans consulter leur nom ; un coup reçu reste plus identifiable qu'un effet cosmétique ; test 100+ ennemis.
**Garde-fou :** pas de freeze global par coup ; pas de lumière/particule illimitée ; pas de vibration permanente.

### Lot C — Collecte et récompenses

1. Reprendre `XpOrb`, les loot pickups et les signaux de progression ; documenter chaque trajet visuel source → joueur → compteur.
2. Synchroniser la récompense réelle, son effet et sa notification ; le gain reste acquis si l'animation est interrompue.
3. Soigner coffre, Autel, choix de niveau, fusion et déblocage selon la grammaire.
4. Regrouper les notifications ; priorité danger > choix obligatoire > récompense > information secondaire.
5. Reporter les détails longs dans la pause/Chroniques ; une découverte de lore ne bloque pas la fuite.

**Vérification :** on sait ce qui a été gagné et pourquoi ; rafale de gains compréhensible ; inventaire plein géré ; transitions de scène sans gain perdu/dupliqué.
**Garde-fou :** la récompense ne dépend pas du callback de fin d'un Tween.

### Lot D — Refonte majeure de la fin de run

**Existant :** GameOverScreen construit un panneau fixe 360×420 très textuel ; le bilan enregistre une seule arme finale. SaveEndOfRun met à jour le meilleur score avant que ShowGameOver lise IsNewRecord : capturer ancien record et comparaison avant sauvegarde. Sources : [GameOverScreen.cs](../../scripts/UI/GameOverScreen.cs), [RunHistoryManager.cs](../../scripts/Infrastructure/RunHistoryManager.cs), [ScoreManager.cs](../../scripts/Score/ScoreManager.cs).

**Composition proposée :** un vrai écran de résultat aéré à trois zones. En tête, score final dominant et éventuel nouveau record. Au centre, personnage et build visuel (quatre armes, quatre passifs, objets regroupés avec compteurs), puis quelques faits : durée, éliminations, Résurgences, cause de mort. En bas, gains et déblocages sous forme de cartes, puis « Rejouer » et « Hub ». Détails du score, statistiques par source et progression secondaire restent accessibles à la demande.

1. Produire deux maquettes avec run courte/perdue et run longue/riche en objets. Réduire les informations simultanées ; garder l’action suivante évidente à la manette et au clavier.
2. Construire un snapshot final autonome : résultat comptable, ancien record, nouveau record booléen, cause, build complet avec niveaux/raretés/compteurs, objectifs avancés et récompenses attribuées. Geler ce snapshot avant de libérer la run.
3. Révéler en trois temps brefs : fin du combat et respiration, score/bilan, nouveaux contenus. L’animation peut être accélérée ou passée sans perte de gain ; les boutons ne changent pas de position et aucun clic de combat ne relance accidentellement.
4. À collecte de données constante, ne montrer que les statistiques fiables. Les dégâts par arme/objet nécessitent une attribution à ajouter ; anciennes runs = « non mesuré », jamais zéro inventé.
5. Préserver historique et progression : une hausse de version ne doit pas envoyer les saves V2 dans la migration V1 ni vider leur historique. Écrire une migration V2→nouveau format et une attribution idempotente.
6. Relier une carte déblocage à la Collection du menu ; quêtes et Souvenirs ont des sections distinctes. Montrer le lore découvert sans en faire la monnaie d’accès aux armes.
7. Comparer clarté, satisfaction et envie de relance sur des séquences filmées ; la référence Megabonk guide la valorisation du build et du gain, pas un écran copié sans adaptation. Une capture précise de son écran final reste à documenter avant maquette comparative ; aucune disposition spécifique n’est prétendue vérifiée ici.

**Vérification :** zéro record en jeu/pause ; record correct au bilan même après sauvegarde ; total exact ; quatre armes/passifs et grandes piles d’objets lisibles ; récompenses identiques après passage des animations ou réouverture ; ancienne sauvegarde et historique conservés.
**Garde-fou :** aucune récompense dans un callback d’animation ; aucune longue liste de statistiques forcée avant relance.

### Ambition de sensation

Raphaël valide une juiciness très poussée : mobilité, impacts, collectes, raretés et transformations doivent se sentir fortement. La cible est une réponse riche et synchronisée — poses/anticipations, courbes d’animation, timbres, trajectoires, éclats et réactions — avec montée spectaculaire du build. Les budgets de particules et les options réduites servent cette cible à 60 FPS ; ils ne justifient pas une présentation timide. Valider une séquence « départ modeste → build puissant → bilan gratifiant » avec le même vocabulaire visuel.

## 6. Recette finale et sortie

Même monde et même build, séquence courte avant/après ; puis run dense réelle. La seed du monde ne fixe pas tous les tirages de spawn et de crises : utiliser spawns/calendrier contrôlés pour une comparaison stricte, sinon répéter les essais et noter leur variabilité. Tester effets réduits, son coupé et absence de secousse. Effectuer build, smoke si applicable, profilage et vérifications du [dossier](README.md).

Acceptation par Raphaël : score suivi sans effort, armes expressives, récompenses distinctes, danger toujours lisible, fatigue visuelle acceptable et cible 60 FPS conservée.

Roadmap : C (score), D (lisibilité/son/bilan), F (effets/audio), G (performance/accessibilité). Les extensions de score doivent aussi préciser leur incidence sur 09.

## 7. Chantier prioritaire du 25 septembre : juiciness de tout le jeu

**Demande de Raphaël :** le jeu « a le potentiel d'être dix fois plus joli et plus juicy ». Impacts, morts, ramassage d'XP et de loot, montées de niveau, sensation de « rouler sur la game » quand le build monte en puissance, micro-interactions. D'abord une liste priorisée, puis des petits lots mesurés : 60 FPS en combat dense, pools, niveaux de particules existants.

### État vérifié dans le code (25 septembre)

- `VfxFactory` fournit une trentaine d'effets : impacts, slashs par arme, explosion, dissolution, flaque irisée, burst d'XP, level-up, traînées. Les trois niveaux `ParticleLevel` (Full, Reduced, Off) sont respectés.
- **Aucun effet n'est recyclé.** Chaque coup instancie une scène `DamageNumber` et un flash ; chaque mort, une dissolution et une flaque ; chaque projectile, un `GpuParticles2D` de traînée et un impact. Tout est libéré ensuite. En combat dense, c'est des centaines de créations de nœuds par seconde : le premier verrou avant d'ajouter des effets.
- `ScreenShake` : secousses légère, moyenne et forte, et un `Hitstop` que les morts de boss et les coups critiques appellent (voir §1 : pas de gel systématique).
- Level-up : burst et secousse moyenne dans `GameBootstrap`. Ramassage d'XP : burst par orbe, sans progression sonore ni lien visuel avec la barre.

### Liste priorisée

| Ordre | Lot | Ce que le joueur ressent | Contenu | Mesure |
|---|---|---|---|---|
| 1 | **J0 — Socle recyclé** | Rien de visible, mais tout le reste devient possible | Pool générique d'effets (chiffres de dégâts, flash, impact, dissolution, flaque, burst XP) ; budget d'effets par frame qui dégrade proprement selon `ParticleLevel` ; banc « combat dense » avant/après | FPS moyen et 1 % bas avec 120 ennemis, nœuds créés par seconde, mémoire |
| 2 | **J1 — Impact** | Chaque coup « claque » | Flash blanc du sprite touché (shader, pas de nœud), écrasement bref et recul visuel de la cible, étincelles orientées dans le sens du coup, chiffres de dégâts lisibles (Saira, regroupés par cible, critique distinct par la forme), son de matière | Trois armes reconnaissables sans lire leur nom ; coût par coup |
| 3 | **J2 — Morts** | Éliminer est gratifiant, et une rafale de morts est spectaculaire | Dissolution orientée selon le dernier coup, éclats projetés, pop et saut du butin, signature propre aux élites et Souverains (anneau d'onde, flash, secousse) | Lisibilité à 100+ ennemis ; aucune mort sans retour visuel |
| 4 | **J3 — Collecte** | Ramasser est un plaisir continu | Orbes aspirées avec accélération et traînée, hauteur du son qui monte sur une chaîne de ramassages, barre d'XP qui pulse à chaque arrivée, Essence et butin avec trajectoire vers le HUD | Chaîne de 50 orbes sans saturation sonore |
| 5 | **J4 — Montée de niveau** | Le passage de niveau est un temps fort | Onde au sol et colonne de lumière, repoussée visuelle des créatures proches (sans effet de jeu), barre d'XP qui flashe, écran de choix qui entre avec punch | Délai jusqu'au choix inchangé |
| 6 | **J5 — Rouler sur la game** | Quand le build scale, on le voit et on le sent | Effets qui grandissent avec les dégâts par seconde et la taille des vagues fauchées, compteur de morts en rafale (« ×24 »), cascade de dissolutions, sons superposés plafonnés, léger recul de caméra quand l'écran se remplit | Séquence début modeste → build puissant, même vocabulaire visuel |
| 7 | **J6 — Micro-interactions** | Le monde répond | Poussière de pas selon le sol, herbes qui plient, éclaboussures, coffres qui frémissent à l'approche, reflets sur les POI, retours d'interface | Coût borné à la zone visible |

Chaque lot : captures avant/après dans une vraie run (`tools/capture_run.sh`), mesure en combat dense, `ParticleLevel` Reduced et Off vérifiés, puis ton retour avant le lot suivant. J0 précède tout le reste. L'ordre de J1 à J6 suit le ressenti « à la seconde » du §1 du dossier.
