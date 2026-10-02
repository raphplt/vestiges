# Revue de la qualité de VESTIGES et de la dette technique

**Suivi :** le lot [Q0, livré le 2 octobre](q0/README.md), corrige les lanceurs et les bancs décrits en F04/F05. Le texte ci-dessous conserve les constats de l'état audité ; les autres corrections suivent le [plan 26](../../plans/26-qualite-et-dette-technique.md).

Revue du 2 octobre 2026, destinée à Raphaël. Elle couvre le code, l’architecture, les données, les tests, la persistance et la préparation d’une distribution. **Le projet a un socle solide pour poursuivre le développement, mais plusieurs défauts doivent être corrigés avant une livraison publique.** Les priorités sont la séparation des outils de développement, la protection des sauvegardes et la fiabilité des validations. La dette structurelle principale se trouve dans le combat du joueur et l’orchestration des systèmes.

Cette revue ne modifie aucun code de production et ne valide ni le plaisir de jeu, ni l’équilibrage, ni la qualité artistique finale. Elle respecte le pivot V2 : aucun retour au craft, à la construction ou au cycle jour/nuit n’est proposé.

## Périmètre et méthode

État examiné : commit `9374f05fa946b3741ea979918a1612773c9b4f43`, avec les modifications locales présentes au démarrage. La Stratégie V2, l’architecture conceptuelle, les plans actifs et plusieurs audits précédents ont été consultés. Les classes centrales et leurs dépendances ont fait l’objet d’une lecture approfondie ; l’ensemble des fichiers a été inventorié et recherché. Ce n’est pas une lecture ligne par ligne de tous les générateurs et assets.

Inventaire relevé :

| Élément | Quantité |
|---|---:|
| Fichiers C# dans `scripts/` | 310 |
| Lignes dans ces fichiers, commentaires et lignes vides compris | 55 805 |
| Fichiers C# dans `tools/tests/` | 61 |
| Fichiers JSON dans `data/` | 77 |
| Fichiers C# de production dépassant 500 lignes | 19 |
| Lignes de `Player` et de ses fichiers partiels | 2 836 |
| Fichiers C# sans leur `.cs.uid`, hors cache Godot | 0 |

Les empreintes des 389 fichiers de production inventoriés avant les régressions ont été comparées après le premier train de tests : aucune variation. Pendant la rédaction, une activité concurrente a ensuite modifié les raretés, les courbes XP et spawn, ainsi que des descriptions d’objets et leur présentation. Le tableau ci-dessous décrit l’état initial vérifié ; les contrôles ciblés de fin de revue sont distingués plus bas et dans `validation.json`. Les passages cités par les constats ont été recontrôlés ; ces changements ne résolvent pas les défauts relevés. Un conflit d’écriture entre processus de build a également interrompu la première tentative du banc du cône ; la relance a permis d’isoler son véritable défaut.

Les tests ont utilisé des profils utilisateur temporaires. Les FPS n’ont pas été mesurés pendant cette revue : les builds et une activité concurrente ne constituent pas des conditions acceptables pour certifier le débit. Les mesures historiques citées conservent leur date et leurs limites.

## Résultats des validations

| Validation | Résultat vérifié |
|---|---|
| `dotnet build --nologo` | Zéro erreur et zéro avertissement |
| Smoke réel, 600 frames | Hub démarré, aucun message d’erreur inattendu détecté |
| Contrats, acquisition et effets des spécialisations | Trois suites vertes |
| Objets et armes | Deux suites vertes |
| Capacités ennemies | Verte |
| Petits lieux et bonus de terrain | Deux suites vertes |
| Temporalité et Effacement actif | Deux suites vertes ; 20 contrôles pour l’Effacement |
| Cartographie | 17 contrôles verts |
| Écrans de choix | 31 contrôles verts |
| Musique | Verte ; contrôle logique en headless, aucune recette audible |
| Déplacements | Verte |
| Profils normal et dev | Six étapes vertes |
| Cône | Échec reproductible par exception dans le banc |
| Assets UI | Deux assertions échouent |
| Intégration de Main | Exécution directe verte ; lanceur officiel défectueux sur le Bash système |
| Modèle Python de progression | Six tests verts |
| Protection du mode dev dans les assemblies d’export | `ExportDebug` et `ExportRelease` verts |
| Syntaxe des JSON de `data/` | 77 fichiers parsés, zéro erreur de syntaxe |

Les 17 suites de scripts de régression exécutées donnent donc 15 suites vertes et deux en échec. Le contrôle de Main, le modèle Python et les deux configurations d’export ont été vérifiés en complément. Un build d’assembly d’export ne constitue pas un export jouable complet avec ses données et ses ressources.

Après les changements concurrents, les contrôles ciblés d’acquisition, d’objets et du modèle Python repassent. Le banc des armes échoue désormais sur une assertion de distribution des raretés : 207 tirages Épiques/Légendaires en zone Ancrée contre 362 en zone Effacée, sur 10 000 tirages par zone ; le banc exige plus du double. Les poids de base passent leur contrôle. Cela établit une incompatibilité entre le réglage en cours et l’exigence du test, sans trancher lequel reflète le bon équilibrage. Ce résultat supplémentaire ne remplace pas le bilan initial de 15 suites sur 17.

Les preuves locales sont conservées dans [validation.json](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/doc/audits/qualite-2026-10-02/validation.json), [manifest.json](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/doc/audits/qualite-2026-10-02/manifest.json), [manifest-final.json](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/doc/audits/qualite-2026-10-02/manifest-final.json) et [journaux.log.gz](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/doc/audits/qualite-2026-10-02/journaux.log.gz). Les manifestes décrivent les états initial et final, y compris les modifications locales ; la validation explicite les relances, les changements concurrents et les limites des lanceurs.

## Points forts à conserver

**Le travail de vérification est déjà sérieux.** Le projet teste de vrais nœuds Godot, la physique, les effets et des parcours d’interface. Les audits précédents distinguent les allocations directes, le rendu, le coût de l’observateur et les chronométrages sous charge. Cette discipline est un atout majeur.

**La performance a une place réelle dans l’implémentation.** Les pools de projectiles, créatures, chiffres et effets, le budget des VFX, le sommeil des orbes, le découpage des décors, les atlas et le préchauffage des shaders montrent une démarche construite. Le parcours des cellules actives de l’Effacement et la conservation de la dette temporelle répondent à des problèmes mesurés. Il ne serait pas justifié de proposer une réécriture générale du rendu ou des ennemis.

**Des contrats de domaine commencent à bien structurer le combat.** `AttackContext`, `DamageResult`, `EnemyKillResult`, `EnemyLife` et les résultats de soin portent la provenance et distinguent les générations d’une créature recyclée. Plusieurs spécialisations et capacités sont composées dans des modules dédiés. Ce sont les bons points d’appui pour les refactorings suivants.

**Le contenu est largement piloté par les données.** Les catalogues, la progression, les raretés, les courbes, les apparitions et les réglages visuels vivent dans des JSON. Le lecteur des spécialisations fournit même un exemple de validation stricte, de publication atomique du catalogue et de collections en lecture seule.

**Les cycles de vie sont généralement traités.** Les systèmes inspectés se désabonnent du bus, les pools et plusieurs instances statiques se réinitialisent en sortie de scène. Les `.cs.uid` sont complets. La migration V1 et la séparation des profils de développement sont présentes et testées. Les défauts ci-dessous concernent les endroits où cette discipline reste incomplète.

## Défauts à corriger en priorité

P1 signifie ici : correction prioritaire avant distribution ou exposition de progression et de classements réels. P2 : défaut ou dette importante à traiter dans un lot dédié. P3 : entretien ou préparation d’une évolution, sans urgence fonctionnelle démontrée.

### F01 P1 Les outils de triche restent accessibles en production

Sources : [GameBootstrap.cs](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/scripts/World/GameBootstrap.cs:233), [DebugActionPanel.cs](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/scripts/UI/DebugActionPanel.cs:47), [DebugOverlay.cs](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/scripts/UI/DebugOverlay.cs:124), [Main.tscn](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/scenes/Main.tscn:89).

Le bootstrap crée `DebugActionPanel` sans condition de compilation ou de profil. Son entrée F4 permet d’ajouter XP et Essence, de soigner, d’améliorer une arme et d’activer `IsGodMode`. `DebugOverlay`, présent dans Main, expose aussi une option d’invulnérabilité d’essai par F1. Aucune garde équivalente à celle de `DevelopmentMode` ne protège ces chemins.

Conséquence déduite du code : une run normale peut être modifiée tout en conservant ses écritures de progression et ses envois Steam normaux. La protection du toggle du Hub, bien testée, ne suffit donc pas. Aucun export graphique complet n’a été lancé pour reproduire ce parcours ; l’absence de garde est confirmée dans les sources.

Correction : exclure les contrôles et leurs points d’entrée des configurations de production, réserver les actions au profil dev en local et étendre le contrôle d’export à tous les outils capables de modifier une run. Vérifier aussi qu’une run altérée ne peut pas alimenter les classements normaux.

### F02 P1 La persistance peut remplacer les acquis par une sauvegarde partielle ou vide

Sources : [MetaSaveManager.cs](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/scripts/Infrastructure/MetaSaveManager.cs:126), [RunHistoryManager.cs](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/scripts/Infrastructure/RunHistoryManager.cs:307), [ScoreManager.cs](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/scripts/Score/ScoreManager.cs:106).

Les fichiers définitifs sont ouverts directement en écriture, puis sérialisés et remplis. Il n’y a pas de remplacement atomique par fichier temporaire ni de copie de récupération. Après une erreur JSON, les loaders repartent sur des données vides en mémoire ; une écriture ultérieure peut remplacer le fichier défectueux sans l’avoir conservé. Ce sont des risques établis par le chemin d’écriture, pas une perte de sauvegarde observée pendant cette revue.

La fin de run écrit successivement le record, les Vestiges, les statistiques et les déblocages. `Save()` ne renvoie pas un succès à l’appelant. Une erreur d’écriture peut donc laisser une finalisation incomplète alors que le bilan continue normalement. `SaveEndOfRun()` n’a pas non plus de garde d’idempotence à son entrée ; son état `_endSettled` sert au calcul du bilan et n’empêche pas une seconde attribution.

Correction : sérialiser avant d’ouvrir la destination, écrire dans un temporaire, vérifier et remplacer le fichier, conserver la dernière version valide et mettre en quarantaine les données illisibles. Faire remonter les échecs et rendre la finalisation de run idempotente. Tester interruption d’écriture, sauvegarde tronquée, double finalisation et récupération.

Le parsing mérite un durcissement associé : à [MetaSaveManager.cs:104](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/scripts/Infrastructure/MetaSaveManager.cs:104), une racine JSON telle que `[]` provoque une erreur de type hors du seul `catch (JsonException)` ; un numéro de version non entier peut également lever une autre exception. Les versions futures sont acceptées puis ramenées à la version courante par `NormalizeData`, ce qui ne fournit pas une protection contre une ouverture avec une version plus ancienne du jeu.

### F03 P1 Les requêtes de classement Steam se remplacent entre elles

Sources : [SteamLeaderboards.cs](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/scripts/Infrastructure/Steam/SteamLeaderboards.cs:60), [SteamLeaderboards.cs](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/scripts/Infrastructure/Steam/SteamLeaderboards.cs:142), [SteamLeaderboards.cs](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/scripts/Infrastructure/Steam/SteamLeaderboards.cs:186).

`UploadScore()` demande quatre uploads à la suite. Lorsque les handles ne sont pas en cache, chaque appel remplace `_pendingUploadBoard`, `_pendingUploadScore` et le même `_findCallback`. Il n’existe qu’un emplacement d’attente pour quatre requêtes. Même avec des handles déjà connus, le même `_uploadCallback` est réaffecté à chaque appel. Un téléchargement peut également partager l’état de recherche.

Conséquence déduite du code : les quatre opérations ne peuvent pas être suivies correctement, et les recherches initiales peuvent se perdre ou se retrouver associées à un état d’attente remplacé. Ce chemin est appelé par la fin de run lorsque Steam est actif. La machine de revue est ARM64 et Steam y est désactivé : le défaut n’a pas été reproduit avec le service réel.

Correction : sérialiser les opérations ou conserver un objet d’attente et un callback par requête, avec une association explicite entre board et score. Vérifier les quatre destinations, cache froid et chaud, erreurs réseau et chevauchement d’un téléchargement.

## Défauts fonctionnels et fiabilité des validations

### F04 P2 Les contrôles peuvent annoncer un succès sans avoir exécuté le scénario

Sources : [smoke_test.sh](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/tools/smoke_test.sh:18), [test_movement.sh](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/tools/test_movement.sh:15), [measure_run.sh](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/tools/measure_run.sh:30), [bench_ab.sh](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/tools/bench_ab.sh:35).

Le smoke ignore les codes de retour de l’import et du boot avec `|| true`, ne contrôle pas les erreurs de l’import et accepte un journal de run vide. **Reproduction effectuée : un exécutable Godot de substitution qui quitte systématiquement avec 42 donne un smoke vert, code de retour 0.** Le build utilisé dans cette preuve reste le vrai build C#.

Sur le Bash système de cette machine, `test_movement.sh --run-integration` vide `QUIT_ARGS`, puis son expansion sous `set -u` provoque `QUIT_ARGS[@]: unbound variable`. Le lanceur a renvoyé 0 sans résultat du moteur. L’intégration de Main a ensuite été exécutée directement, avec borne temporelle et vérification explicite de `RESULT failures=0` : elle passe.

Les lanceurs de mesure tolèrent aussi certaines erreurs de processus et peuvent finir sans exiger le nombre prévu de résultats valides. `bench_ab.sh` peut afficher zéro passe valide sans retourner un échec. Ces sorties doivent distinguer clairement réussite, mesure partielle et absence de mesure.

Correction : contrôler le code moteur, les deux journaux, un marqueur de fin et le nombre de résultats attendus. Centraliser ces règles dans une bibliothèque commune et vérifier le lanceur par injection d’échecs silencieux, d’import et de timeout. Corriger l’expansion des tableaux vides pour les shells supportés.

### F05 P2 Des bancs ont dérivé par rapport au modèle actuel

Sources : [ConeRegression.cs](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/tools/tests/ConeRegression.cs:133), [UiArtRegression.cs](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/tools/tests/UiArtRegression.cs:21), [WeaponRegression.cs](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/tools/tests/WeaponRegression.cs:242).

Le banc du cône lève une `NullReferenceException` dans son helper de réflexion lorsqu’il cherche `_igniteChance`, un champ qui n’existe plus. Il conserve aussi d’autres anciens champs, dont `_vampirismPercent`. Ses mesures préalables et certaines assertions s’exécutent, mais le contrat complet et le résultat final ne sont plus produits. La première tentative avait échoué avant le runtime à cause d’un conflit de build ; ce défaut du banc est celui de la relance.

Le banc UI attend 31 objets actifs et 34 entrées dans le manifeste de Collection. L’état examiné contient 32 objets actifs et 35 entrées ; les neuf spécialisations et quatorze motifs restent présents. Deux assertions échouent alors que les vérifications individuelles d’icônes passent. Ce résultat indique des attentes obsolètes, pas la preuve d’un défaut visuel.

Le changement de raretés intervenu en cours de revue fait aussi échouer le seuil de multiplication par deux des Épiques/Légendaires dans le banc des armes, alors que leur fréquence augmente bien. Avant de modifier l’assertion ou les données, rendre explicite le contrat d’équilibrage attendu ; une simple hausse ne satisfait pas la condition actuelle du test.

Correction : utiliser les contrats et effets actuels pour le scénario du cône. Pour les assets, comparer les ensembles d’identifiants attendus et les relations entre catalogues et manifestes ; garder un comptage fixe uniquement s’il constitue réellement une exigence du contenu. Les helpers de réflexion doivent échouer avec le nom du membre absent, plutôt qu’une exception générique.

### F06 P2 La seed réelle et les tirages de run ne forment pas un contrat reproductible

Sources : [WorldSetup.cs](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/scripts/World/WorldSetup.cs:126), [ScoreManager.cs](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/scripts/Score/ScoreManager.cs:170), [CrisisManager.cs](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/scripts/Events/CrisisManager.cs:56), [RunEventContext.cs](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/scripts/Events/RunEvents/RunEventContext.cs:34), [RunObservation.cs](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/tools/tests/RunObservation.cs:220).

Avec une seed de Hub vide, `GameManager.RunSeed` vaut 0. `WorldSetup` tire une vraie seed dans sa propriété locale, sans la reporter au manager. Le record lit ensuite le manager et conserve 0 ; l’écran de bilan masque cette seed. La carte d’une run aléatoire ne peut donc pas être retrouvée depuis son historique.

Même avec une seed imposée, la génération déterministe de la carte ne garantit pas les mêmes tirages de gameplay : certains systèmes appellent `Randomize()`, d’autres utilisent le RNG global. L’observateur appelle `GD.Seed(seed)`, contrairement au lancement normal, et ne contrôle pas les RNG indépendants des Résurgences et événements. Les comparaisons à seed égale restent utiles, mais ne prouvent pas une trajectoire de run identique.

Correction : publier une seed effective unique dès le lancement, puis dériver des flux dédiés pour monde, apparitions, loot, progression et événements. Garder les flux cosmétiques séparés. Définir les limites de reproductibilité liées aux entrées et au temps de simulation, puis vérifier une trace de tirages.

### F07 P2 Le ciblage des projectiles ne tient pas compte de la vie recyclée d’un ennemi

Sources : [Projectile.cs](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/scripts/Combat/Projectile.cs:152), [Projectile.cs](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/scripts/Combat/Projectile.cs:204), [Player.cs](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/scripts/Core/Player.cs:1975), [Enemy.cs](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/scripts/Combat/Enemy.cs:221).

Le projectile guidé conserve un `Node2D` et vérifie uniquement que l’objet Godot reste valide. Une créature morte ou rendue au pool reste un objet valide ; la même instance peut ensuite représenter une nouvelle créature. Le projectile continue alors de suivre sa référence. La recherche de remplacement et `FindNearestEnemies` ne filtrent pas systématiquement `IsActive` et `IsDying`, contrairement au cône.

Conséquence déduite du code : des tirs peuvent viser une cible déjà morte, inactive ou une autre vie de l’instance recyclée. Le cache de groupe peut conserver une référence le temps d’une frame. Le projet possède déjà `EnemyLife` et l’utilise pour le ciblage prioritaire ; cette protection devrait être appliquée à toutes les cibles persistantes.

Correction : conserver l’identité de vie lors du verrouillage, filtrer les cibles actives et abandonner le verrouillage quand cette vie se termine. Vérifier mort en vol, retour au pool et réutilisation immédiate.

### F08 P2 Le late game a plusieurs propriétaires qui peuvent se contredire

Sources : [EndgameManager.cs](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/scripts/Events/EndgameManager.cs:75), [EndgameManager.cs](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/scripts/Events/EndgameManager.cs:121), [CrisisManager.cs](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/scripts/Events/CrisisManager.cs:103).

Le seuil de quatre crises marque `_lateGameReached` dès `CrisisStarted`. `UpdateLateGameState()` retourne ensuite immédiatement sur ce booléen. À la fin d’une crise et pendant l’exploration, `CrisisManager` choisit la phase uniquement selon l’Effacement global. Si celui-ci reste sous 0,68, le manager peut donc annoncer `IsLateGameReached=true` pendant que la phase globale redevient `Exploration`. Le seuil de crises ne suffit pas à maintenir la phase prévue.

Ce défaut est identifié par lecture du chemin de transition ; aucune longue run spécifique au quatrième seuil n’a été rejouée pour cette revue. Il illustre une dette concrète d’orchestration, au-delà d’un simple nombre de références croisées.

Correction : donner un propriétaire unique aux transitions globales, séparer l’intensité de Résurgence de l’avancement irréversible vers le late game et tester la combinaison des seuils, du boss et de la fin d’une crise.

### F09 P2 Une exception du chargement peut laisser le jeu en pause sans issue

Sources : [GameBootstrap.cs](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/scripts/World/GameBootstrap.cs:60), [GameBootstrap.cs](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/scripts/World/GameBootstrap.cs:95), [WorldSetup.cs](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/scripts/World/WorldSetup.cs:235).

Le bootstrap lance une `Task` sans conserver ni observer son résultat, met l’arbre en pause, puis attend la génération, le rendu de préchauffage et plusieurs étapes. Il n’existe pas de traitement global d’erreur ni d’état d’annulation pour ce chargement. Une exception propagée par la génération ou un loader peut empêcher la dépause et laisser l’overlay affiché. Les contrôles de validité après les attentes sont partiels.

Le chargement normal passe dans le test direct de Main ; c’est son comportement en échec ou à la sortie de scène qui reste insuffisamment protégé.

Correction : observer la tâche, expliciter les états de chargement et son annulation, libérer ses ressources, puis afficher une erreur avec une sortie vers le Hub lorsque cela est possible. Ajouter des scénarios de fichier invalide et de fermeture pendant le chargement.

### F10 P2 Un remapping peut modifier des touches que le joueur n’a pas changées

Sources : [InputRemapManager.cs](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/scripts/Infrastructure/InputRemapManager.cs:192), [InputRemapManager.cs](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/scripts/Infrastructure/InputRemapManager.cs:216), [project.godot](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/project.godot:68).

Les déplacements ont plusieurs bindings clavier initiaux, par exemple W puis flèche haut. `SaveBindings()` écrit chaque événement dans la même clé de config : le dernier remplace les précédents. Modifier seulement la touche d’interaction sauvegarde donc les flèches comme touches de mouvement. Au redémarrage, `RemapKey()` efface tous les événements clavier de chaque action puis ne réinstalle que la touche sauvegardée : les touches WASD disparaissent.

De plus, `LoadBindings()` utilise les méthodes publiques de remapping qui sauvegardent à chaque appel : lire les réglages provoque plusieurs réécritures du fichier. Cette analyse vient des opérations de sérialisation et de l’ordre des événements, sans modification du profil personnel.

Correction : sauvegarder la liste des bindings, ou conserver explicitement la touche primaire sans écraser les secondaires ; dissocier application et persistance. Vérifier le parcours modification d’une seule action, redémarrage et conservation de toutes les autres.

### F11 P2 Les classements hebdomadaires reçoivent toutes les runs

Source : [SteamLeaderboards.cs](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/scripts/Infrastructure/Steam/SteamLeaderboards.cs:75).

La classe décrit un weekly à seed fixée, mais `UploadScore()` envoie systématiquement le score sur `BoardWeekly`, sans contexte de défi ni contrôle de seed ou de semaine. `ScoreManager` appelle cette méthode pour une fin de run normale. Une fois le problème de requêtes résolu, le classement peut mélanger les conditions de jeu.

Correction : n’envoyer que les runs portant le contexte explicite d’un défi accepté. Si ce parcours n’est pas livré, suspendre cet upload jusqu’à sa validation. Le reset et la configuration côté Steam restent à vérifier sur la plateforme réelle.

## Dette structurelle et dette de contenu

### F12 P2 Les contrats de données sont de qualité inégale

Sources : [EnemyDataLoader.cs](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/scripts/Infrastructure/EnemyDataLoader.cs:141), [WeaponDataLoader.cs](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/scripts/Infrastructure/WeaponDataLoader.cs:111), [XpCurveConfig.cs](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/scripts/Infrastructure/XpCurveConfig.cs:45), [PlayerProgression.cs](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/scripts/Progression/PlayerProgression.cs:55), [PerkSpecializationDataLoader.cs](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/scripts/Infrastructure/PerkSpecializationDataLoader.cs:91).

La syntaxe des 77 JSON est valide. Cela ne vérifie pas leurs types, bornes, identifiants et références croisées. Plusieurs lecteurs accèdent directement aux clés, acceptent des valeurs par défaut différentes ou publient progressivement leur cache. Les doublons d’armes écrasent le dictionnaire tout en restant dans la liste ; des lecteurs exposent directement des collections et définitions modifiables. Le lecteur des spécialisations est nettement plus strict et constitue un modèle interne réutilisable.

Un exemple à fort impact est la courbe XP : aucune validation n’exige un coût strictement positif. Un `base_xp` ou un plafond égal à zéro peut rendre la boucle de montée de niveau non terminante. Les valeurs actuelles sont correctes ; il s’agit de l’absence de protection d’un paramètre d’équilibrage. Certains modificateurs inconnus peuvent aussi être ignorés silencieusement par les switches d’application.

Correction : validation avant publication des catalogues, rejet explicite des doublons et effets inconnus, vérification des bornes critiques et des références. Exposer les collections de référence en lecture seule. Centraliser les règles de parsing et les diagnostics utiles sans imposer immédiatement une migration de tous les loaders.

### F13 P2 Le sommeil des orbes ne borne pas leur accumulation

Sources : [XpOrb.cs](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/scripts/Combat/XpOrb.cs:268), [CombatPools.cs](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/scripts/Combat/CombatPools.cs:102), [audit Effacement du 1er octobre](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/doc/audits/erasure-active-2026-10-01/README.md:122).

Les orbes éloignées arrêtent leur traitement individuel, mais restent des nœuds et sont réinspectées par une liste de sommeil toutes les 0,25 seconde. Elles n’ont pas de fin de vie dans le Néant. L’audit du 1er octobre relève déjà **19 782 orbes encore au sol après 30 minutes**, sans en avoir isolé le coût. Ce chiffre est une observation historique, pas une mesure renouvelée aujourd’hui ni une preuve de perte de FPS imputable aux orbes.

La mise en sommeil est une bonne optimisation locale ; elle laisse ouverte la croissance de mémoire et du parcours central. La question existe déjà dans les plans 16/22 et ne doit pas devenir une nouvelle feature parallèle.

Correction proposée : appliquer la règle de durée de vie décidée pour le butin en zone oubliée, ou définir une agrégation spatiale qui conserve l’XP selon les intentions du jeu. Mesurer la croissance sur les mêmes seeds et durées avant et après. Ne pas supprimer arbitrairement le butin uniquement pour obtenir un meilleur banc.

### F14 P2 Le combat du joueur concentre trop de responsabilités

Sources : [Player.cs](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/scripts/Core/Player.cs:1722), [Player.cs](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/scripts/Core/Player.cs:1975), [GameBootstrap.cs](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/scripts/World/GameBootstrap.cs:125), [QuestManager.cs](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/scripts/Progression/QuestManager.cs:173).

`Player.cs` compte 2 161 lignes et les partiels portent l’ensemble à 2 836. Le joueur gère mouvement, défense, arsenal, ciblage, patrons d’attaque, procs, objets, animation et une partie des interactions. `_equippedWeapon` représente aussi l’arme de travail du timer qui attaque, ce qui oblige les effets persistants à conserver séparément leur vraie source. Le domaine construit ou référence plusieurs éléments d’UI ; `QuestManager` porte à la fois des règles et une interface.

Le découpage en dossiers et partiels améliore la navigation, mais ne réduit pas à lui seul les responsabilités et l’état partagé. Les bancs qui dépendent des noms privés et le besoin de contrats explicites de provenance rendent ce coût visible dès aujourd’hui.

Les fonctions de ciblage recréent plusieurs listes et trient les candidats à chaque attaque. C’est une source concrète d’allocations dans un chemin fréquent, mais aucun budget CPU ou gain de FPS n’est établi par cette revue. Les parcours répétés des ennemis peuvent aussi amplifier le coût avec le nombre de projectiles et d’effets ; il faut profiler avant de choisir un index spatial.

Correction : extraire d’abord le ciblage et l’exécution d’une attaque, avec l’arme et le contexte en paramètres, puis l’arsenal. Conserver les contrats existants et vérifier un lot à la fois. Réutiliser les buffers si les mesures le justifient ; éviter une refonte générale de tous les systèmes en même temps.

### F15 P2 La distribution complète n’est pas reproductible depuis les seuls réglages versionnés

Sources : [Vestiges.csproj](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/Vestiges.csproj:1), [.gitignore](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/.gitignore:10), [test_dev_release.sh](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/tools/test_dev_release.sh:32).

Le dépôt examiné ne versionne pas de workflow CI, de `global.json` ou de preset d’export partagé. Le SDK Godot est correctement fixé à 4.7.2 dans le projet, mais le choix du SDK .NET et la composition d’un export complet restent dépendants de l’environnement. Les bancs C# de `tools/tests/` sont compilés dans la même assembly que le jeu, sans frontière de projet explicite.

Les tests `ExportDebug` et `ExportRelease` vérifient les gardes du mode dev dans les assemblies ; ils ne testent pas le démarrage d’un paquet distribué, la présence des JSON/manifeste/audio ou les chemins de chargement après export. L’absence de preset ne prouve pas qu’un export existant manque de données : elle empêche d’en vérifier et reproduire la sélection depuis cet état du dépôt.

Correction : une commande de validation globale qui compile et importe une fois, lance les scénarios avec résultats obligatoires et retourne un échec cohérent. Ajouter ensuite une CI adaptée, un choix de SDK reproductible et un export de test avec tous les fichiers requis. Séparer progressivement les outils et tests de l’assembly distribuée. Protéger les builds/imports d’un même checkout contre une exécution concurrente.

### F16 P2 Une provenance audio reste non établie

Sources : [CREDITS.json](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/assets/audio/CREDITS.json:50), [sounds.json](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/data/audio/sounds.json:77), [AUDIO-GUIDE.md](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/doc/AUDIO-GUIDE.md:25).

Le fichier sélectionné pour `sfx_level_up` est bien référencé dans la banque active, mais son registre indique « Auteur du fichier historique non établi » et « Provenance historique à confirmer ». La traçabilité du reste du catalogue constitue un point fort ; cette entrée est une exception explicitement connue.

Action : retrouver les éléments de provenance exigés par le guide du projet, ou remplacer l’asset avant sa distribution commerciale. Cette revue constate l’état du registre ; elle ne tranche pas les droits d’usage et ne réalise pas un audit juridique des assets.

### F17 P3 La localisation est partielle et le formatage reste fixé au français

Sources : [SettingsScreen.cs](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/scripts/UI/SettingsScreen.cs:604), [HubCollectionPanel.cs](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/scripts/UI/HubCollectionPanel.cs:213), [StatCatalog.cs](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/scripts/Infrastructure/StatCatalog.cs:30), [LocaleManager.cs](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/scripts/Infrastructure/LocaleManager.cs:23).

Le jeu annonce français et anglais, mais certaines interfaces utilisent des textes français directement, notamment les options graphiques et la Collection. Plusieurs noms et descriptions des catalogues sont également des textes de contenu. Le formatage des statistiques impose `fr-FR` quelle que soit la locale sélectionnée.

Ce n’est pas un défaut du parcours français, mais le support anglais n’est pas complet. Prévoir une recette EN sur tous les écrans, centraliser les libellés et faire dépendre le formatage de la locale. La reconstruction des interfaces au changement de langue doit aussi être vérifiée.

### F18 P3 La documentation et les restes V1 multiplient les sources de vérité

Sources : [VESTIGES-ARCHITECTURE.md](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/doc/VESTIGES-ARCHITECTURE.md:5), [roadmap V2](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/doc/VESTIGES-STRATEGIE-V2.md:761), [MetaSaveManager.cs](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/scripts/Infrastructure/MetaSaveManager.cs:323), [GroupCache.cs](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/scripts/Core/GroupCache.cs:35), [tableau de bord](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/doc/plans/TABLEAU-DE-BORD.md:7).

La hiérarchie documentaire et le tableau de bord sont utiles, mais l’architecture reste datée de février et décrit des systèmes abandonnés. La roadmap V2 comporte encore des formulations ou cases générales qui ne reflètent pas directement le boss, les quêtes, les sprites et les modules déjà présents. Leur présence dans le code ne suffit cependant pas à cocher les validations de design et de recette encore attendues.

Le code conserve des API de kits et mutateurs sans appelant, ainsi que les caches de groupes `structures` et `resources` sans consommateur dans `scripts/`. Les couches de lecture de référence sont souvent modifiables par leurs appelants. Les profils et helpers statiques ciblent un seul joueur ou une seule run ; cela convient au solo actuel mais devra être explicité avant une évolution coop.

Action : garder un document technique V2 court sur les systèmes réellement actifs, archiver clairement les références historiques et retirer les façades V1 sans consommateur après vérification des migrations. Ne pas transformer ce ménage en réintroduction des systèmes supprimés. Durcir progressivement les conventions et la nullabilité dans les modules nouveaux plutôt que d’imposer une vague massive d’avertissements à tout le dépôt.

## Ordre de remboursement recommandé

Ce découpage est une proposition de travail, pas une autorisation de mise en œuvre ni une validation de nouvelles règles de gameplay. Avant tout code, rattacher chaque lot à son plan existant, comme l’exige `AGENTS.md`.

| Ordre | Lot proposé | Critère de sortie |
|---|---|---|
| 1 | Fermer les outils de modification de run en production et corriger les opérations Steam | Contrôles de tous les points d’entrée dans les deux configurations d’export ; quatre boards suivis correctement ; weekly réservé à son contexte |
| 2 | Protéger la persistance et la finalisation | Récupération après fichier tronqué et interruption ; une run ne récompense qu’une fois ; erreur d’écriture visible |
| 3 | Rétablir la confiance dans les validations | Cône et UI verts avec les contrats actuels ; exigence du banc de raretés cohérente avec le réglage validé ; lanceurs en échec sur moteur absent, code non nul, timeout et marqueur manquant ; intégration Main portable |
| 4 | Corriger seed effective, remapping, ciblage et transitions de phase | Scénarios de régression ciblés, sans changement non intentionnel de règles ou de progression |
| 5 | Valider les données et les échecs du chargement | Catalogue publié entièrement ou rejeté ; borne XP protégée ; erreur de chargement avec sortie utilisable |
| 6 | Traiter la croissance du butin et le ciblage coûteux | Même instrument, seeds et durée avant/après ; mémoire, quantités, allocations et temps mesurés ; contrat de loot conservé |
| 7 | Réduire la concentration dans Player et stabiliser la livraison | Un module extrait et vérifié par lot ; export complet reproductible ; sources et outils séparés progressivement |
| 8 | Fermer les lacunes de contenu et documentation | Provenance audio résolue, parcours EN vérifié, référence technique V2 actualisée, recettes humaines conservées explicitement |

Les trois premiers lots apportent le meilleur bénéfice immédiat : ils protègent les acquis des joueurs et empêchent une validation verte trompeuse. L’extraction de `Player` peut ensuite avancer sans bloquer les itérations du jeu. Une réécriture générale ou l’introduction immédiate d’une architecture réseau seraient disproportionnées.

## Appréciation de la maturité

| Dimension | Appréciation |
|---|---|
| Itération sur le gameplay | Solide : outils de capture, mesures et contenu modulable |
| Propreté locale du code | Généralement bonne ; hétérogène entre anciens lecteurs et contrats récents |
| Architecture globale | Moyenne : composition réelle, mais responsabilités et dépendances encore concentrées |
| Gestion des performances | Sérieuse ; plusieurs dettes de croissance et d’allocations restent identifiées |
| Couverture de régression | Bonne sur les systèmes développés ; bancs et lanceurs à remettre en cohérence |
| Persistance | Insuffisamment robuste pour protéger des acquis durables |
| Préparation de distribution | Incomplète : outils de triche, Steam et paquet final à sécuriser |
| Documentation | Riche et traçable ; coût élevé pour retrouver la vérité active |
| Qualité artistique, audio et plaisir de jeu | Non évalués par cette revue technique |

La priorité est une consolidation ciblée. Les fondations existantes permettent de corriger les défauts sans changer la direction V2 ni arrêter tout le développement de contenu.
