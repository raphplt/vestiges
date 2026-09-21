# Plan 01 — Déplacements précis et naturels

Statut : **socle B/C implémenté et vérifié techniquement ; recette humaine ouverte ; mobilité avancée proposée** · Priorité : P0. Raphaël a demandé de commencer le premier plan après commit/push du dossier.
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

**Découverte complémentaire :** le chargeur de sprites connaît dash, mais Player et les inputs ne possèdent aucun dash/saut jouable. La caméra est déjà lissée dans Main.tscn ; la vitesse du joueur est appliquée directement. CharacterData n’a pas de profil de mobilité. Reprendre InputRemapManager.RemappableActions et ses axes stick ; une animation disponible ne prouve pas une mécanique.

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

**Recommandation :** même bouton de mobilité pour tous, action de base compréhensible, variantes propres au personnage. Prototyper deux variantes après le dash : une glissade directionnelle et un saut court. Leur attribution au casting est proposée en 06.

1. Créer un profil de mobilité chargé avec les données du personnage, via un module composé distinct du gros Player ; réutiliser les facteurs vitesse, les signaux et le contrat de collision du socle. Aucun nouvel Autoload requis.
2. Glissade : engagement plus long, direction ajustable mais virage moins vif, sortie toujours contrôlable ; sa vitesse ne s’accumule pas sans borne par maintien du bouton. Tester une recharge distincte et une animation pieds/ombre adaptée.
3. Saut court : dissocier hauteur visuelle et position de déplacement au sol. Ombre et cible d’atterrissage restent lisibles. Proposition initiale : franchir les petites zones de ralentissement autorisées, pas murs/ruines/eau profonde/Néant ; pas d’immunité générale aux projectiles. Toute exception de dégâts exige un tag d’attaque explicite.
4. Définir durée, arc visuel, contrôle aérien, recharge, interruptions et atterrissage invalide avant branchement. Si le lieu est bloqué, raccourcir le trajet sur le dernier point valide ; jamais téléporter de l’autre côté d’un mur. Aucun saut vertical 3D ni gravité du monde n’est présumé disponible dans cette vue 2D.
5. Ajouter une variante seulement si elle change une décision tout en restant compréhensible au même bouton. Relier objets de mobilité, animations et danger ennemi après le test isolé.

**Vérification :** nouveau personnage compris en moins d’une courte prise en main ; mêmes parcours au clavier/manette ; saut sur bordure/obstacle mobile/zone effacée, annulation et retour Hub fiables.
**Garde-fou :** ne pas livrer un saut purement décoratif présenté comme esquive ; ne pas devoir produire tout le casting avant d’évaluer ces deux prototypes.

## 5. Recette finale

- Quatre axes, quatre diagonales, pressions opposées, relâchement, remapping et stick.
- Collision de face et glissement contre un obstacle ; couloir étroit ; terrain lent ; ralentissement temporaire.
- Combat en déplacement, ouverture de coffre interrompue, pause et mort.
- Comparaison filmée avant/après par Raphaël, puis courte prise en main par 2–3 personnes si disponibles.
- Build sans warning ; smoke si scènes/projet/initialisation changent ; les critères communs du dossier s'appliquent.

**Acceptation :** Raphaël valide le contrôle, les nouveaux joueurs comprennent les axes sans consigne particulière, aucun avantage diagonal ni régression des interactions.

## 6. Limites et suite

Le socle A–C est validé en direction. D/E étendent le plan à la demande de Raphaël ; leurs règles exactes restent proposées. Le lot se termine par une mobilité agréable en situation réelle et son intégration au tempo 03, pas par la seule correction des axes.

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
- [ ] Validation du ressenti par Raphaël ; comparaison des variantes D/E seulement ensuite.

Le headless valide les invariants techniques, pas la sensation de fluidité, la qualité visuelle ou la performance d'un combat dense. La pause testée suspend réellement les ticks du Player ; le retour d'un menu complet n'est pas couvert. Le lot A n'est donc pas déclaré intégralement terminé, et l'acceptation globale du plan reste ouverte.
