# Plan 01 — Déplacements précis et naturels

Statut au 22 septembre 2026 : **socle B/C et dash D validés par Raphaël ; lot E après refonte du casting et des sprites** · Priorité : P0. Raphaël demande de poursuivre le premier plan et valide explicitement les déplacements de base refaits.
Références : [dossier](README.md), V2 §2/8/10, [état des lieux](00-etat-des-lieux.md).

## 1. Problème et résultat attendu

Raphaël signale que les déplacements vont en diagonale et ne conviennent pas. Avant cette implémentation, `Player._PhysicsProcess(double delta)` appliquait `CartesianToIsometric(Vector2 cartesian)` à la direction d'entrée.

La transformation calculait `(x-y, (x+y)*0.5)` puis normalisait : droite produisait une trajectoire bas-droite. Une entrée analogique faible était également normalisée. Source historique : [Player.cs](../../scripts/Core/Player.cs) au commit `e6f5d2c`. Cette conversion a été retirée du déplacement du joueur ; les projections du monde restent en place.

**Cible validée :** les directions de commande correspondent aux axes visibles de l'écran ; la vue et les décors restent isométriques. Le personnage se contrôle sans conversion mentale.

## 2. Décisions acquises et précision du périmètre

| Décision | Proposition | Conséquence |
|---|---|---|
| Repère de commande | Écran : haut = haut, droite = droite | Changement assumé du contrôle actuel |
| Diagonales | Combinaison de directions, vitesse maximale identique | Pas de bonus de vitesse diagonal |
| Analogique | Conserver la magnitude après zone morte | Marche lente possible au stick |
| Inertie | Réponse directe dans le premier lot | Accélération/glisse éventuelles seulement après comparaison |
| Orientation | Animation suit le déplacement ; attaque peut viser ailleurs | Séparer les deux intentions visuelles si nécessaire |
| Mobilité active | Demande ajoutée : dash/saut et mouvements propres aux personnages | Lots D/E ci-dessous ; variantes à choisir |

La cible et les décisions du socle sont validées par Raphaël. Il demande désormais une mobilité très fluide, expressive et aussi réussie à la manette qu’au clavier. Les timings et variantes ci-dessous sont des propositions nouvelles, pas des validations implicites.

## 3. Phase 0 — Lecture obligatoire et contrats existants

Lire `Player.cs` : entrée, vitesse, `_facingDirection`, `UpdateSpriteAnimation`, collisions, `IsAIControlled` et `AIInputOverride`. Relire `project.godot` [input], `InputRemapManager` et les chargeurs de sprites joueur.

Réutiliser le chemin d'entrée `Input.GetVector("move_left", "move_right", "move_up", "move_down")`, `Velocity` et `MoveAndSlide()` déjà en place. Les noms d'actions existent : ne pas introduire une autre famille de touches.

Cartographier les consommateurs d'`AIInputOverride` avant toute modification : le contrat de cette entrée peut être cartésien ou déjà projeté. Une donnée de simulation ne suffit pas à prouver qu'un contrôleur de simulation est encore disponible.

## 4. Lots d'action

### Lot A — Reproduction et référence

1. Filmer quatre directions et quatre diagonales sur terrain libre, caméra stable.
2. Mesurer le déplacement sur 2 secondes dans chaque direction avec la même vitesse.
3. Répéter au stick à faible et pleine amplitude, puis avec remapping.
4. Identifier séparément trajectoire, orientation du sprite, camera smoothing et collision ; conserver les valeurs avant modification.

**Livrable :** tableau entrée → vecteur → vitesse → animation, avec reproduction de l'anomalie.
**Vérification :** droite produit actuellement un déplacement diagonal bas-droite, avec une composante verticale non nulle ; noter le résultat réel.
**Garde-fou :** ne pas compenser le problème en tournant la caméra ou les décors.

### Lot B — Contrôle dans le repère approuvé

1. Reprendre la lecture d'entrée et l'application des facteurs terrain/ralentissement de `_PhysicsProcess`.
2. Appliquer la direction dans le repère approuvé sans normalisation supplémentaire qui écrase le stick.
3. Maintenir une vitesse bornée pour une combinaison de touches et documenter le repère de l'entrée IA.
4. Reprendre les protections mort/état de run et les annulations d'interaction existantes.
5. Rechercher les usages de `CartesianToIsometric` ; retirer uniquement la conversion devenue inutile au mouvement, pas les projections de génération du monde.

**Livrable :** correctif local et contrat d'entrée documenté.
**Vérification :** pour des axes purs, composante perpendiculaire négligeable ; distance diagonale et cardinale à pleine amplitude dans une tolérance de 2 % sur sol libre ; stick faible plus lent.
**Garde-fou :** ne pas modifier vitesse des personnages, ralentissement de l'eau ou danger de l'Effacement pour masquer un problème de contrôle.

### Lot C — Orientation, animation et caméra

1. Reprendre les animations disponibles et établir leur correspondance aux huit directions de déplacement.
2. Garder des transitions stables près des frontières de secteurs ; limiter les inversions à l'arrêt.
3. Vérifier attaque automatique pendant une fuite dans le sens opposé ; décider si une direction d'attaque séparée est nécessaire.
4. Vérifier absence de glissement visuel, tremblement des pieds et décalage des ombres.
5. Ajuster la caméra seulement si la référence filmée démontre une gêne résiduelle.

**Livrable :** matrice déplacement/visée/animation et séquence de combat.
**Vérification :** orientation compréhensible sans déplacement modifié par le ciblage.
**Garde-fou :** ne pas exiger huit sprites originaux avant le correctif ; les nouvelles animations relèvent de 08.

### Lot D — Fluidité mesurable et mobilité active

**Constat avant le lot D :** le chargeur de sprites connaît dash, mais Player et les inputs ne possèdent aucun dash/saut jouable. La caméra est déjà lissée dans Main.tscn ; la vitesse du joueur est appliquée directement. CharacterData n’a pas de profil de mobilité. Reprendre InputRemapManager.RemappableActions et ses axes stick ; une animation disponible ne prouve pas une mécanique.

1. Comparer la réponse directe validée à une accélération très brève (40–90 ms) et un freinage de 30–70 ms, valeurs d’essai. Préserver le demi-tour volontaire, l’arrêt précis et la faible amplitude du stick ; noter input→premier mouvement et input→vitesse cible.
2. Traiter séparément physique, pose visuelle et caméra. Ne pas arrondir la position physique à une grille pour obtenir des pixels nets. Tester le rendu à basse vitesse, près des obstacles et après collision ; animer selon le mouvement effectif.
3. Prototyper une action de mobilité commune, appelée « mobilité » dans la configuration proposée. Binding initial à examiner : Espace au clavier et bouton principal de face à la manette, entièrement remappable. Pas de double appui directionnel obligatoire.
4. Prototype recommandé : dash court dans la direction d’entrée, ou dernière direction de déplacement si aucune entrée. Le stick indique sa direction mais une activation réussie parcourt une distance stable ; aucune visée souris indispensable.
5. Paramètres JSON d’essai : durée 0,15 s, vitesse 2,4×, recharge 2,5 s, buffer d’entrée 80 ms. Tester séparément sans invulnérabilité puis avec une fenêtre courte ; aucun choix final avant recette avec la difficulté du début de run.
6. Définir états locomotion/mobilité/hurt/death ; annuler proprement sur mort, vider le buffer à l’ouverture des menus, figer recharge en pause. Respecter murs/collisions et assurer une sortie non coincée. Définir explicitement l’interaction avec eau, ralentissements et interruption de coffre.
7. Reprendre traînées, ombres, animation dash et audio existants ; l’effet annonce départ et fin, le cooldown est lisible sans barre de texte. Un buffer améliore la tolérance, il ne doit pas déclencher une action imprévue au retour d’un menu.

**Livrable :** deux variantes de fluidité et deux variantes de dash comparées, avec contrôles identiques sur les deux périphériques.
**Vérification :** pas de drift à neutre, mouvement progressif à 25/50/100 % de stick ; huit directions clavier, angles intermédiaires manette, demi-tour, mur, coin, ralentissement, mort et pause. Aucun frame hitch perceptible à l’activation.
**Garde-fou :** fluidité ne signifie pas glissade incontrôlable ; le dash ne traverse ni murs ni Néant sans règle approuvée. Préserver la menace en retestant 03, plutôt qu’ajouter automatiquement une invulnérabilité généreuse.

### Lot E — Mobilités de personnages, saut et glissade

**Prérequis explicite de Raphaël (22 septembre) :** refondre les personnages et valider au moins cinq à six identités avec leurs nouveaux sprites (06/08). Tous les sprites actuels sont à refaire. Le dash commun est validé ; cette validation ne lance pas E avant ce préalable.

**Recommandation :** même bouton de mobilité pour tous, action de base compréhensible, variantes propres au personnage. Prototyper deux variantes après le dash : une glissade directionnelle et un saut court. Leur attribution au casting est proposée en 06.

1. Créer un profil de mobilité chargé avec les données du personnage, via un module composé distinct du gros Player ; réutiliser les facteurs vitesse, les signaux et le contrat de collision du socle. Aucun nouvel Autoload requis.
2. Glissade : engagement plus long, direction ajustable mais virage moins vif, sortie toujours contrôlable ; sa vitesse ne s’accumule pas sans borne par maintien du bouton. Tester une recharge distincte et une animation pieds/ombre adaptée.
3. Saut court : dissocier hauteur visuelle et position de déplacement au sol. Ombre et cible d’atterrissage restent lisibles. Proposition initiale : franchir les petites zones de ralentissement autorisées, pas murs/ruines/eau profonde/Néant ; pas d’immunité générale aux projectiles. Toute exception de dégâts exige un tag d’attaque explicite.
4. Définir durée, arc visuel, contrôle aérien, recharge, interruptions et atterrissage invalide avant branchement. Si le lieu est bloqué, raccourcir le trajet sur le dernier point valide ; jamais téléporter de l’autre côté d’un mur. Aucun saut vertical 3D ni gravité du monde n’est présumé disponible dans cette vue 2D.
5. Ajouter une variante seulement si elle change une décision tout en restant compréhensible au même bouton. Relier objets de mobilité, animations et danger ennemi après le test isolé.

**Vérification :** nouveau personnage compris en moins d’une courte prise en main ; mêmes parcours au clavier/manette ; saut sur bordure/obstacle mobile/zone effacée, annulation et retour Hub fiables.
**Garde-fou :** ne pas livrer un saut purement décoratif présenté comme esquive ; ne pas lancer ces prototypes avant le casting et les nouveaux sprites validés, conformément à la nouvelle priorité de Raphaël.

## 5. Recette finale

- Quatre axes, quatre diagonales, pressions opposées, relâchement, remapping et stick.
- Collision de face et glissement contre un obstacle ; couloir étroit ; terrain lent ; ralentissement temporaire.
- Combat en déplacement, ouverture de coffre interrompue, pause et mort.
- Comparaison filmée avant/après par Raphaël, puis courte prise en main par 2–3 personnes si disponibles.
- Build sans warning ; smoke si scènes/projet/initialisation changent ; les critères communs du dossier s'appliquent.

**Acceptation :** Raphaël valide le contrôle, les nouveaux joueurs comprennent les axes sans consigne particulière, aucun avantage diagonal ni régression des interactions.

## 6. Limites et suite

Les déplacements de base livrés en B/C sont validés par Raphaël le 22 septembre 2026. Cette validation autorise la poursuite du prototype D ; ses variantes et son équilibrage restent à comparer en jeu. Le saut et la glissade E viennent après validation du nouveau casting et de ses sprites (06/08). Les captures du lot A et la recette complète ne sont pas déduites de cette validation. Le plan se termine par une mobilité agréable en situation réelle et son intégration au tempo 03, pas par la seule correction des axes.

Roadmap : contribue à A (flow/playtest), D (onboarding) et G (accessibilité). Cocher un item global seulement si son périmètre complet est effectivement validé.

## 7. Première implémentation — 21 septembre 2026

**Périmètre livré :** socle technique des lots B/C et mesures du lot A. La recette filmée et le ressenti restent ouverts. Aucun dash, saut, profil de personnage, inertie ou réglage caméra n'est ajouté dans ce lot.

- Commandes dans les axes écran, même vitesse maximale sur les huit directions ; conservation de l'amplitude issue de la zone morte Godot.
- Entrée IA documentée dans le même repère, bornée à 1 et bloquée hors run comme le clavier/manette. Aucun consommateur externe d'`AIInputOverride` trouvé dans le dépôt.
- Pose, cadence de marche et pas déterminés après `MoveAndSlide()`, depuis `GetRealVelocity()`. Le joueur bloqué contre un mur revient au repos ; les pas et la cadence de marche suivent la vitesse réelle, y compris au stick partiel.
- Quatre poses existantes conservées. Les diagonales utilisent NE/NW/SE/SW ; les axes purs conservent le côté orthogonal de la dernière pose. Une bande de stabilité de 0,1 sur les composantes unitaires évite le changement incessant de pose autour d'un axe. L'arrêt conserve l'orientation ; la visée des armes reste indépendante.
- Noms d'animations mis en cache sous forme de `StringName`, sans interpolation de chaîne par tick. Hurt/death/idle gardent une cadence normale. Les facteurs de vitesse, eau et ralentissement et l'annulation d'interaction restent dans leur chemin existant.

### Mesures reproductibles

Commande : [`tools/test_movement.sh`](../../tools/test_movement.sh). Le banc instancie la vraie scène Player avec le Traqueur (240 px/s), les sprites existants, les autoloads et un mur `StaticBody2D`. Il injecte les événements clavier/stick dans Godot et appelle le contrôleur une fois par tick physique. Mesure de 120 ticks à 60 Hz, soit 2 secondes simulées, sans génération du monde.

Les répertoires XDG de données/config/cache sont temporaires ; aucun remapping ou fichier du joueur n'est écrit. Les bindings du processus de test utilisent le périphérique synthétique 31 pour éviter qu'une manette branchée sur la machine contamine les mesures. Les premières mesures non isolées ont été écartées. La référence ci-dessous a été rejouée avec ce banc isolé sur le Player du commit `e6f5d2c`, puis le correctif a été rétabli.

| Entrée | Déplacement avant `(x ; y)` en px | Déplacement après `(x ; y)` en px |
|---|---|---|
| Droite | `(429,325 ; 214,662)` | `(480 ; 0)` |
| Bas | `(-429,325 ; 214,662)` | `(0 ; 480)` |
| Gauche | `(-429,325 ; -214,662)` | `(-480 ; 0)` |
| Haut | `(429,325 ; -214,662)` | `(0 ; -480)` |
| Bas-droite | `(0 ; 480)` | `(339,412 ; 339,412)` |
| Bas-gauche | `(-480 ; 0)` | `(-339,412 ; 339,412)` |
| Haut-gauche | `(0 ; -480)` | `(-339,412 ; -339,412)` |
| Haut-droite | `(480 ; 0)` | `(339,412 ; -339,412)` |
| Stick droite : 25 % utiles | `(429,325 ; 214,662)` | `(120 ; 0)` |
| Stick droite : 50 % utiles | `(429,325 ; 214,662)` | `(240 ; 0)` |
| Stick droite : 100 % utiles | `(429,325 ; 214,662)` | `(480 ; 0)` |

« Utile » désigne l'amplitude **après** zone morte : pour la zone morte 0,2 du projet, les trois entrées physiques simulées sont 0,4 / 0,6 / 1. La distance diagonale après correction diffère de la distance cardinale de moins de 0,001 px sur ces deux secondes. Les cadences de marche mesurées sont respectivement 0,25 / 0,5 / 1.

Le banc vérifie également : absence de drift dans la zone morte, remapping sur L, relâchement, touches opposées, ralentissement ×0,5, bonus vitesse ×1,25, IA bornée, blocage hors run, suspension du déplacement par la pause de l'arbre, pose de fuite indépendante du feedback d'attaque, stabilité autour d'un axe, collision et glissement contre mur, arrêt de la marche/des pas contre mur et immobilité/animation de mort.

### Vérifications et limites

- [x] Banc de régression headless : aucune assertion en échec.
- [x] Build C# : zéro warning, zéro erreur.
- [x] Smoke du Hub après ajout de la scène de test : `tools/smoke_test.sh`, 600 frames, données utilisateur isolées.
- [ ] Recette réelle clavier et manette, capture avant/après, caméra et rendu à faible vitesse, couloirs et coins.
- [ ] Run complète : terrain eau, interruptions POI/coffre, transitions Hub/run, combat avec ennemis réels. Le banc vérifie le feedback d'attaque appelé directement, pas une rencontre complète.
- [x] Validation des déplacements de base par Raphaël le 22 septembre 2026 : « les déplacements de base ont été refaits pour aller dans le sens du plan, je valide ces changements ».
- [ ] Comparaison des variantes du prototype D en situation réelle, puis essai E (saut/glissade).

Le headless valide les invariants techniques, pas la sensation de fluidité, la qualité visuelle ou la performance d'un combat dense. La pause testée le 21 septembre suspend réellement les ticks du Player ; le retour d'un menu complet n'était pas couvert par cette première passe. La validation explicite du socle par Raphaël le 22 septembre ne documente ni une recette manette exhaustive, ni des captures, ni l'avis d'autres joueurs. Le lot A n'est donc pas déclaré intégralement terminé, et l'acceptation globale du plan reste ouverte.

## 8. Prototype de mobilité — 22 septembre 2026

**Périmètre livré :** prototype technique du lot D, sur le socle B/C validé. Les deux réponses de locomotion et les deux fenêtres de protection sont disponibles pour comparaison. Le dash par défaut est désormais validé par Raphaël. Les variantes alternatives F1 restent ouvertes ; la mesure de performance en combat dense a été complétée en §9. Le lot E attend le nouveau casting et ses sprites validés (06/08).

### Commandes et comportement

- **Mobilité : Espace au clavier, X (Carré sur une manette PlayStation) à la manette**, modifiables dans les paramètres. Ce bouton de face évite le conflit avec A/Croix déjà affecté à l’interaction. Une ancienne configuration sans action de mobilité conserve ses remappings et reçoit cette nouvelle commande par défaut.
- Le dash suit la direction d’entrée au déclenchement, ou la dernière direction de déplacement au neutre. Le stick détermine l’angle ; son amplitude ne réduit pas la distance du dash. La marche conserve sa réponse analogique.
- Réglages dans [`data/movement/mobility.json`](../../data/movement/mobility.json) : durée 150 ms, multiplicateur de vitesse ×2,4, recharge 2,5 s à partir du déclenchement et buffer de 80 ms avant disponibilité. Le maintien ou la répétition clavier ne relance pas automatiquement le dash.
- La réponse directe validée reste active par défaut. Dans le panneau **F1**, choisir « Réponse brève (JSON) » pour comparer une accélération de 60 ms et un freinage de 40 ms. Le demi-tour volontaire annule la poussée dans l’ancien sens.
- Le dash n’accorde **aucune invulnérabilité par défaut**. La case « Fenêtre d’invulnérabilité d’essai » du panneau F1 active 60 ms de protection. Fermer le panneau avec F1 après le choix pour reprendre l’essai avec les commandes de jeu ; ces options servent à comparer, sans modifier le JSON.
- [`PlayerMobility`](../../scripts/Core/PlayerMobility.cs) compose les états locomotion/mobilité/hurt/death ; `Player` conserve les entrées et les collisions. Un dégât reçu interrompt le dash et refuse une nouvelle mobilité pendant la réaction hurt (200 ms, en JSON), sans immobiliser la marche. La mort annule l’action et son buffer.
- L’eau conserve son facteur ×0,5 ; les ralentissements et bonus de vitesse existants s’appliquent aussi au dash. Les murs arrêtent l’action au contact ; les coins permettent de repartir. Le déplacement de mobilité est contrôlé par segments pour interdire le Néant, le terrain effacé et les limites du monde, sans franchissement par un grand pas physique.
- Le départ du dash annule une ouverture de coffre ou une interaction POI en cours, y compris au neutre. L’ouverture d’un menu annule l’action et vide le buffer ; la pause fige la recharge. Une touche maintenue au retour doit être relâchée avant une nouvelle activation.
- L’animation dash existante, quatre traînées recyclées et une jauge de recharge accompagnent le mouvement. Les intensités réduite/désactivée diminuent ou suppriment les traînées ; l’état de recharge reste lisible. Les sons de pas existants `sfx_pas_gravier` et `sfx_pas_beton` sont réutilisés au départ et à la fin, en attendant un éventuel asset dédié.

La vérification dans la vraie scène de run a aussi conduit à corriger la liaison au gestionnaire d’Effacement : le cache est résolu lors de l’initialisation du personnage et de l’entrée en run, après la création du monde. L’ordre d’initialisation du bootstrap attend désormais une frame avant d’ajouter l’overlay et le préchauffage, pour éviter les ajouts d’enfants pendant la construction de l’arbre.

### Mesures et régressions

Le banc [`tools/test_movement.sh`](../../tools/test_movement.sh) conserve les sauvegardes et remappings isolés du joueur. Les tests du contrôleur utilisent les vrais événements Godot clavier/stick synthétiques ; ceux des frontières temporelles appellent directement le module de mobilité. Les mesures ci-dessous utilisent le Traqueur à 240 px/s, avec ticks de 60 Hz sauf les frontières vérifiées à la milliseconde.

| Cas | Résultat technique |
|---|---|
| Dash sur sol libre, huit directions | 86,4 px en 150 ms, sans bonus diagonal |
| Dash au stick, amplitudes utiles 25 / 50 / 100 % | Même distance de 86,4 px ; angle conservé |
| Dash avec ralentissement ×0,5 | 43,2 px |
| Recharge sans pause | 150 intervalles de 1/60 s, soit 2,5 s (151 ticks en comptant celui du départ) |
| Réponse directe | Vitesse cible dès le premier tick de traitement de l’entrée |
| Réponse brève, plein stick | 66,667 px/s au premier tick ; cible 240 px/s au quatrième tick, soit 66,7 ms à 60 Hz |
| Réponse brève, arrêt / demi-tour | Arrêt en trois ticks (50 ms) ; inversion dès le premier tick |
| Module de dash à 30 / 60 / 120 Hz | 86,4 px à chaque cadence |
| Buffer, temps restant avant recharge | Appuis à 79 et 80 ms acceptés ; à 81 et 90 ms expirés |
| Protection courte d’essai | Active à 59 ms ; terminée à 61 ms |

Le banc couvre aussi le remapping clavier/manette, la compatibilité d’anciens bindings, les angles intermédiaires, le neutre, les appuis opposés, le freinage/demi-tour, les ralentissements, les murs/coins et leur sortie, la mort, les dégâts et la protection d’essai. Il ouvre le vrai menu de pause, contrôle le gel de recharge et le relâchement obligatoire, et interrompt les interactions de vrais nœuds coffre/POI. Il ne remplace pas une prise en main sur un périphérique physique.

### Vérifications automatisées finales

- [x] `tools/test_movement.sh` : **137 assertions réussies**, `RESULT failures=0`, sortie 0 ; build zéro warning/zéro erreur. [Log du banc](/home/raphael/.codex/visualizations/2026/09/22/01a0c86f-e595-7c52-9e53-47f3b92f0e1b/plan01/movement-regression.log).
- [x] `tools/test_movement.sh --run-integration` : **12 assertions réussies**, `RESULT failures=0`, sortie 0 ; build zéro warning/zéro erreur. Le banc charge la vraie `Main.tscn` et sa génération de monde (seed 221092026). [Log d’intégration](/home/raphael/.codex/visualizations/2026/09/22/01a0c86f-e595-7c52-9e53-47f3b92f0e1b/plan01/movement-integration.log).

L’intégration confirme la liaison automatique du joueur à l’Effacement après initialisation, puis après réinitialisation du personnage alors que l’état est déjà Run, sans injecter cette référence dans le joueur. Une région de Néant fixée par le test dans le vrai gestionnaire arrête le dash à x=127,96 en trois ticks sans collision physique. Une nappe d’eau naturellement générée à (1376 ; 368) donne 120 px/s en marche et 43,20044 px de dash. Cette région de Néant est une fixture de test ; elle ne prouve pas une rencontre spontanée du front lors d’une run complète.

Le banc libère explicitement les 20 ennemis préchauffés hors arbre lors de son nettoyage. La fuite historique d’`EnemyPool` en gameplay n’est pas corrigée par ce lot ; le résultat du banc n’atteste donc pas d’une absence de fuite sur les transitions de vraies runs.

### Vérification du rendu et preuves

- [x] `tools/smoke_test.sh 600` : build sans warning ni erreur, import et démarrage du Hub réussis, données XDG isolées.
- [x] Parcours automatisé GL Compatibility à **1280×720**, Godot 4.7.2, AMD Radeon RX 6950 XT / Mesa 26.2.3 : Hub → vraie `Main.tscn` → déplacement et Espace → recharge → variantes F1 → pause/paramètres/contrôles → retour Hub, arbre non suspendu.
- [x] Jauge pleine avant le dash puis partiellement remplie pendant la recharge ; anneau et petite traînée cyan visibles pendant l’action après correction de leur ordre de dessin. Les contrôles montrent Mobilité / Space / X ; les variantes F1 sont accessibles.

Les [notes de vérification](/home/raphael/.codex/visualizations/2026/09/22/01a0c86f-e595-7c52-9e53-47f3b92f0e1b/plan01/verification-notes.txt), le [log GL](/home/raphael/.codex/visualizations/2026/09/22/01a0c86f-e595-7c52-9e53-47f3b92f0e1b/plan01/final-run.log) et le [résultat smoke](/home/raphael/.codex/visualizations/2026/09/22/01a0c86f-e595-7c52-9e53-47f3b92f0e1b/plan01/smoke.log) sont conservés localement hors du dépôt. Le parcours de run ne produit plus les erreurs d’ajout d’enfants et de cible d’overlay du bootstrap ; des messages de fuite de ressources/textures/RID subsistent à la fermeture GL.

| Capture locale | Preuve apportée |
|---|---|
| [Référence avant compilation du prototype](/home/raphael/.codex/visualizations/2026/09/22/01a0c86f-e595-7c52-9e53-47f3b92f0e1b/plan01/baseline-run-ready.png) | État de run avant la passe de mobilité ; pas un film de référence des anciens axes |
| [Avant le dash](/home/raphael/.codex/visualizations/2026/09/22/01a0c86f-e595-7c52-9e53-47f3b92f0e1b/plan01/final-run-ready.png) | Jauge disponible sous les pieds |
| [Pendant le dash](/home/raphael/.codex/visualizations/2026/09/22/01a0c86f-e595-7c52-9e53-47f3b92f0e1b/plan01/final-dash-middle.png) | Pose, anneau et traînée |
| [Recharge](/home/raphael/.codex/visualizations/2026/09/22/01a0c86f-e595-7c52-9e53-47f3b92f0e1b/plan01/final-run-cooldown.png) | Jauge partielle après l’action |
| [Contrôles](/home/raphael/.codex/visualizations/2026/09/22/01a0c86f-e595-7c52-9e53-47f3b92f0e1b/plan01/final-settings-controls.png) | Mobilité remappable au clavier et à la manette |
| [Variantes F1](/home/raphael/.codex/visualizations/2026/09/22/01a0c86f-e595-7c52-9e53-47f3b92f0e1b/plan01/final-debug-trial-controls.png) | Sélection réponse directe/brève et protection d’essai |
| [Retour Hub](/home/raphael/.codex/visualizations/2026/09/22/01a0c86f-e595-7c52-9e53-47f3b92f0e1b/plan01/final-hub-return.png) | Parcours achevé sans pause résiduelle |

Les effets restent discrets face au terrain dense ; leur appréciation en mouvement reste à faire. La jauge mesure environ 32×4 pixels à 720p et les polices existantes des menus/HUD sont petites : les captures attestent du rendu, pas d’une validation humaine exhaustive de la lisibilité. À cette étape, aucun playtest humain, essai manette physique ou benchmark de combat dense n’était revendiqué ; la mesure technique dense ajoutée ensuite figure en §9.

### Recette restant à faire

- [ ] Comparer les quatre combinaisons réponse directe/brève et protection nulle/courte, au clavier et sur une manette physique ; retenir les sensations et timings avec Raphaël.
- [ ] Évaluer caméra, basse vitesse, trajectoires étroites et lisibilité des effets dans une vraie session de combat ; compléter la comparaison filmée du lot A et la recette humaine A–C.
- [ ] Rejouer le début de run du plan 03 : difficulté, fréquence d’échappatoire et intérêt des détours après introduction du dash.
- [x] Mesurer sur la même machine le code courant sans puis avec activation du dash : 120 ennemis, GL à 1280×720 et 1920×1080, dix cas valides (§9). Il ne s’agit pas d’un avant/après historique ; la cible 60 FPS constants n’est pas atteinte et les saccades restent à traiter.
- [ ] Après refonte et validation du casting (au moins cinq à six personnages) et de tous ses sprites, reprendre les mobilités spécifiques du lot E.

Raphaël valide le dash livré : « Ok top je valide ». Il demande ensuite de prioriser les personnages et les sprites avant E. Cette validation du résultat ne constitue pas une mesure de performance en combat dense ni un choix de toutes les variantes d’essai F1.

## 9. Mesure de mobilité en combat dense — 22 septembre 2026

### Protocole reproductible

Le banc [`tools/benchmark_movement.sh`](../../tools/benchmark_movement.sh) charge la vraie `Main.tscn` avec rendu GL Compatibility. Il compare **le code courant sans activation de dash et le même code avec dash répété**. Ce n’est pas une comparaison avec un ancien commit ni une mesure isolée du coût du module ou de ses effets.

```bash
tools/benchmark_movement.sh /tmp/vestiges-movement-dense-results
```

Choisir un nouveau répertoire de résultats : le script refuse un dossier contenant déjà des mesures. Il effectue le build et l’import avant de lancer les cas. `GODOT_BIN` permet de sélectionner le binaire ; `BENCH_SECONDS`, `BENCH_WARMUP` et `BENCH_REPEATS` règlent respectivement la fenêtre de mesure, la chauffe et le nombre de répétitions. Les valeurs par défaut sont **20 secondes mesurées après 5 secondes de chauffe, trois processus indépendants par variante et résolution**, soit douze cas à 1280×720 et 1920×1080. L’ordre sans dash/avec dash alterne entre répétitions. Réduire la durée peut rendre le cas dash invalide si moins de cinq activations sont observées.

Fixture de [`MovementDenseBenchmark`](../../tools/tests/MovementDenseBenchmark.cs) :

- Seed 221092026 ; Traqueur invincible avec son arme initiale active, sans Souvenir équipé. Profil dev et répertoires XDG temporaires, Steam désactivé. Le banc attend la fin de l’initialisation différée du brouillard avant la chauffe.
- Population fixée à 120 ennemis : 100 `shade` et 20 `fading_spitter`, points de vie multipliés par 10 000 pour conserver les IA, attaques, collisions et impacts pendant la mesure. Les gardes initiaux sont retirés ; spawn naturel, progression de l’Effacement et crises sont figés.
- Même cible orbitale pour les deux variantes ; demandes de dash espacées de 2,65 secondes. Le dash change réellement la trajectoire et les contacts. Le banc vérifie déplacement effectif et, pour la variante dash, distance parcourue pendant l’action.
- Au moins 100 ennemis vivants et dans un rayon de 600 pixels du joueur pendant toute la mesure : au-delà, leur IA utiliserait le traitement simplifié. Le nombre créé dans le pool ne suffit pas à valider la charge.
- Rendu fenêtré vérifié aux dimensions demandées, VSync désactivée, aucun plafond de FPS, audio `Dummy`. Les captures et écritures de fichiers interviennent après la fenêtre mesurée.

Les intervalles muraux entre appels `_Process` donnent moyenne, FPS, p95, p99, maximum et part des images au-delà de 16,67 ms. Les moniteurs Godot `TimeProcess`/`TimePhysicsProcess`, mémoire managée/native, allocations et RSS complètent les JSON ; les intervalles bruts sont conservés en CSV. Malgré le nom des champs `process_cpu_mean_ms`/`physics_cpu_mean_ms`, ces moniteurs ne constituent pas une attribution exacte du temps CPU. Le RSS maximal du processus inclut le chargement. Les statistiques `dash_window_frames` couvrent le dash et l’image suivante, sans isoler son coût GPU. Le script [`summarize_movement_benchmark.py`](../../tools/summarize_movement_benchmark.py) produit `SUMMARY.md` avec les médianes des essais et les maxima conservés ; chaque cas garde son log, son JSON, son CSV et sa capture PNG.

### Résultats de la série finale

**Dix mesures valides conservées : trois paires sans/avec dash à 720p, deux paires à 1080p**, chacune après cinq secondes de chauffe et sur vingt secondes mesurées. Matériel : AMD Ryzen 7 5700X, 16 threads ; AMD Radeon RX 6950 XT ; Godot 4.7.2 stable, GL Compatibility. Chaque cas maintient les 120 ennemis vivants dans le rayon de traitement complet ; chacun des cinq cas dash compte huit activations, soit quarante au total.

La [synthèse locale](/home/raphael/.codex/visualizations/2026/09/22/01a0c86f-e595-7c52-9e53-47f3b92f0e1b/plan01/dense-benchmark/SUMMARY.md) reprend les médianes des moyennes/percentiles et les maxima des pics/RSS :

| Résolution | Variante | Essais valides | Moyenne ms | FPS | p95 ms | p99 ms | Pic ms | Images >16,67 ms | RSS max Mio |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
| 1280×720 | Sans dash | 3 | 16,58 | 60,3 | 23,20 | 48,99 | 107,25 | 24,25 % | 732,3 |
| 1280×720 | Dash | 3 | 15,97 | 62,6 | 21,29 | 46,90 | 93,54 | 18,75 % | 684,0 |
| 1920×1080 | Sans dash | 2 | 20,09 | 49,8 | 28,29 | 53,72 | 101,65 | 83,87 % | 734,9 |
| 1920×1080 | Dash | 2 | 19,46 | 51,4 | 26,93 | 51,52 | 103,73 | 77,92 % | 679,9 |

**Conclusion technique : la cible de 60 FPS constants n’est pas atteinte dans cette fixture.** À 720p, la moyenne avoisine 60 FPS mais les percentiles et pics révèlent des saccades ; à 1080p, la moyenne est proche de 50 FPS. Les cas dash ne présentent pas de dégradation globale dans cette série ; les trajectoires et contacts différents interdisent d’en conclure que le dash améliore la performance ou d’isoler le coût de ses effets. Aucun correctif de gameplay ou de performance de production n’a été entrepris dans ce lot de mesure.

Les [notes d’exécution](/home/raphael/.codex/visualizations/2026/09/22/01a0c86f-e595-7c52-9e53-47f3b92f0e1b/plan01/dense-benchmark/RUN-NOTES.md) détaillent les reprises : trois tentatives ont été interrompues ou ont échoué pendant la préparation du rendu, avant tout échantillon, sans JSON inclus dans la synthèse. Le deuxième cas sans dash à 1080p a été repris avec succès ; la troisième paire 1080p n’a pas été obtenue. L’attente d’une nouvelle image rendue est désormais bornée dans le banc. La cause exacte de ces incidents n’est pas établie. Pour reproduire, conserver une fenêtre de jeu active et vérifier qu’elle rend effectivement ; le headless ne remplace pas cette mesure.

Les dix JSON, CSV et captures sont conservés avec leurs logs dans le même dossier, sans remplacer les données brutes. Captures de contrôle : [720p avec dash](/home/raphael/.codex/visualizations/2026/09/22/01a0c86f-e595-7c52-9e53-47f3b92f0e1b/plan01/dense-benchmark/1280x720-1-dash.png), [1080p avec dash](/home/raphael/.codex/visualizations/2026/09/22/01a0c86f-e595-7c52-9e53-47f3b92f0e1b/plan01/dense-benchmark/1920x1080-1-dash.png). Les ennemis de mêlée se regroupent sous leur IA réelle : une silhouette visible ne correspond pas nécessairement à un seul ennemi ; le décompte actif/proche provient du banc.

### Portée et limites

La comparaison porte sur une fixture dense à population constante, pas sur une run complète : elle ne mesure pas les éliminations/loots en chaîne, les Résurgences, tous les biomes ou les builds avancés. Le temps mural inclut rendu, physique, attentes et ordonnanceur ; ce n’est pas un profil GPU. La trajectoire et les contacts diffèrent avec le dash : un écart ne peut pas être attribué entièrement à ses traînées. Le pilote audio factice ne mesure pas le coût de sortie sonore réel.

Le banc fixe `ContentScaleSize` aux dimensions demandées : le cadrage est identique entre les deux variantes d’une résolution, mais diffère entre 720p et 1080p. L’écart entre résolutions ne représente donc pas un coût GPU par pixel à champ de vue constant.

Une moyenne supérieure à 60 FPS ne démontre ni l’absence de hitch, ni une cible tenue sur matériel intermédiaire. Garder pics et percentiles visibles. Cette mesure ne clôt pas la recette humaine clavier/manette, le choix des variantes, le recalage du début de run en 03, ni la case générale de performance en endgame. Le lot E attend toujours le casting et les nouveaux sprites validés en 06/08.
