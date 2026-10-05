# Plan 26 — Qualité du code et remboursement de la dette technique

2 octobre 2026 · Plan préparé à la demande de Raphaël ([DECISIONS §54](DECISIONS.md)). **Q0 à Q3, Q5 et Q6a à Q6c livrés et vérifiés ; autres lots non commencés.** Référence de diagnostic : [revue du 2 octobre](../audits/qualite-2026-10-02/README.md), ses manifestes et ses journaux.

Objectif : protéger les acquis des joueurs, rendre les validations fiables et permettre de faire évoluer une arme, une créature ou un réglage sans chercher des règles dupliquées dans plusieurs classes. Le travail conserve la direction V2 et les décisions de gameplay actuelles ; il procède par modules et vérifications ciblées.

## 1. Cadre et priorités

La [Stratégie V2](../VESTIGES-STRATEGIE-V2.md) reste l'autorité gameplay ; les décisions récentes et le [plan 21](21-systeme-de-jeu.md) précisent le système actif. Les réglages des lots 21 F1–F6 évoluent en parallèle de cette préparation. Au début de chaque lot, relire le delta depuis l'audit et reproduire le défaut sur l'état courant. Les résultats du 2 octobre ne constituent pas une certification de toutes les versions suivantes.

L'ordre recommandé est : **fiabiliser les contrôles → protéger la progression et la distribution → nettoyer les contrats de combat et les données → corriger les défauts de run → extraire les responsabilités et mesurer les coûts → terminer la préparation de livraison.**

- Un seul lot d'implémentation à la fois ; achever le lot de gameplay actuellement ouvert avant de commencer cette consolidation.
- Les premiers lots sont correctifs. Les migrations de données et extractions conservent les comportements et coefficients du début de leur lot ; un changement d'équilibrage relève du plan de gameplay concerné.
- Reprendre les systèmes, pools et contrats déjà présents. Les nouvelles classes doivent avoir une responsabilité concrète ; éviter une infrastructure générique sans consommateur.
- Chaque lot clôturé conserve son état avant/après, ses vérifications et ses limites dans un compte rendu. Cocher la roadmap V2 seulement si son item est effectivement implémenté et vérifié ; la préparation de ce plan ne coche aucune case.
- Les nouveaux contrats peuvent activer progressivement la nullabilité et les diagnostics ciblés. Les fichiers historiques sont durcis au fil de leur reprise, sans générer une vague d'avertissements à ignorer.

## 2. Le problème des noms et valeurs en dur

Le retour de Raphaël porte sur les noms d'attaques et autres règles codées en dur. Les noms publics des armes sont déjà majoritairement déclarés dans `weapons.json`. Le code manipule cependant beaucoup de **chaînes techniques non vérifiées**, des **identifiants de contenu employés comme conditions** et des **réglages de gameplay locaux**.

Exemples confirmés lors de la préparation :

| Exemple | Source actuelle | Problème et destination proposée |
|---|---|---|
| `"orbital"`, `"chain"`, `"homing"`, `"burst"`, `"circular"` | [Player.cs](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/scripts/Core/Player.cs:1730), [WeaponProperties.cs](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/scripts/Combat/WeaponProperties.cs:16), [WeaponTraits.cs](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/scripts/Combat/WeaponTraits.cs:15) | La même grammaire circule sous forme de strings et est normalisée à plusieurs endroits. Parser une fois vers un type fermé et utiliser ce type dans le domaine. |
| `"slow"`, `"dot"`, `"local_time_slow"`, `"heal_every_n_hits"` | [Player.cs](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/scripts/Core/Player.cs:812), [Player.cs](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/scripts/Core/Player.cs:863) | Effets connus dispersés et paramètres lus par clés à chaque exécution. Contrats d'effets et paramètres validés au chargement. |
| Effet inconnu transformé en `null` | [EnemyAbilityFactory.cs](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/scripts/Combat/Abilities/EnemyAbilityFactory.cs:6) | Une faute de clé peut supprimer silencieusement une capacité. Résolution contrôlée et diagnostic avant publication du catalogue. |
| Meute limitée à `other._enemyId == "charognard"` | [Enemy.cs](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/scripts/Combat/Enemy.cs:714) | La règle dépend du nom d'un contenu. Déclarer la famille de meute ou son ensemble de membres dans les données, en conservant exactement les membres actuels. |
| Intervalle 2,5 s, dégâts 15, largeur 30 et récompense 5 000 du boss | [Indicible.cs](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/scripts/Combat/Indicible.cs:15) | Ces paramètres affectent l'équilibrage et le score. Les charger depuis une configuration de boss avec bornes et unités explicites. |
| Valeurs de secours `n = 5`, soin 4, écho 0,3 s / 60 % / rayon 40 | [Player.cs](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/scripts/Core/Player.cs:871) | Réglages cachés si les paramètres manquent. Les expliciter dans les données ou dans un profil de valeurs par défaut documenté et validé. |
| Croissance d'arme de secours : poids dégâts 3, cadence 2 | [WeaponDataLoader.cs](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/scripts/Infrastructure/WeaponDataLoader.cs:223) | Un champ absent déclenche une autre table de progression. Rendre le contrat obligatoire ou désigner explicitement un profil partagé de données. |
| Styles VFX tels que `"cleave"`, `"bell"` et `"clock"` | [PlayerAttackFx.cs](/Users/raph/Documents/Travail/Personnel/Repositories/vestiges/scripts/Combat/PlayerAttackFx.cs:57) | La sélection du style appartient aux données ; les formes dessinées sont des algorithmes C#. Vérifier les styles et leurs paramètres à la frontière du catalogue. |

Le catalogue examiné contient 24 armes, sept motifs d'attaque de base et sept familles d'effets spéciaux. Le nettoyage doit couvrir les ascensions, objets, spécialisations, aperçu des cartes et affichage des statistiques qui consomment ces mêmes contrats.

### Règle de séparation proposée

| Nature | Où elle doit vivre | Règle de qualité |
|---|---|---|
| Nom, description, règle montrée au joueur | Clé de traduction référencée par le catalogue | Le texte affiché n'identifie jamais une mécanique. Les chiffres viennent de la définition réellement utilisée. |
| Identifiant d'arme, créature, objet ou asset | Catalogue JSON et références validées | Identifiant stable conservé pour les sauvegardes ; résolution au chargement. Le nom affiché peut changer indépendamment. |
| Famille fermée de mécanismes : motif, type d'effet, stat, capacité | Type C# ou registre central de mécanismes | Les clés JSON sont traduites une fois à l'entrée. Une valeur inconnue produit une erreur précise. Les types ne recopient pas la liste entière du contenu. |
| Coefficients, dégâts, délais, rayons, plafonds et poids | JSON du système concerné | Valeurs finies, unités et bornes contrôlées ; une source de vérité. Réglages lus une fois et conservés dans les définitions. |
| Algorithme d'attaque, formule, géométrie et invariant technique | C# | Nom explicite et responsabilité localisée. Une formule n'est pas transformée en langage de scripts JSON. |
| Nom de nœud, propriété Godot, clé de protocole externe | Frontière avec Godot ou le service | Centraliser les contrats partagés quand cela évite une divergence ; ne pas déplacer mécaniquement ces chaînes dans des fichiers de gameplay. |

Exemple de cible : `"attack_pattern": "homing"` reste lisible dans le JSON ; le lecteur le convertit en `AttackPatternKind.Homing`. Le combat, les propriétés et l'UI consomment ce type. Une clé inconnue indique le fichier, l'arme et le champ fautif au chargement. Les dégâts, la portée et la force de guidage restent des paramètres du catalogue.

**Critère d'entretien :** ajouter une arme utilisant un mécanisme existant doit se faire par ses données, assets et traductions. Un mécanisme réellement nouveau peut nécessiter du C#, localisé dans un module et enregistré une fois. Déplacer toutes les chaînes dans une classe `Constants` ne suffit pas à atteindre ce résultat.

Pour éviter le retour de cette dette, les modules migrés appliquent trois contrôles : sélection des mécanismes par types ou registre résolu, absence de branche de gameplay fondée sur un nom affiché, et validation automatique des catalogues ajoutée au lanceur Q0. Chaque référence à un ID concret qui reste nécessaire est localisée à la frontière de contenu et justifiée. Les scénarios vérifient un comportement observable — dégâts, cibles, acquisition — plutôt que le nom d'un champ privé ou une copie de l'algorithme testé.

## 3. Lots correctifs prioritaires

### Q0 — Rendre les validations dignes de confiance

Constats : F04, F05 et la partie lanceurs de F15. Plans associés : 10 et 21 pour les bancs et leurs contrats.

- Contrôler les retours de l'import, du moteur et des timeouts ; exiger les marqueurs de fin et les résultats attendus. Un log vide et zéro passe valide sont des échecs.
- Corriger les tableaux vides sous le Bash supporté, les lanceurs de mesure et l'intégration Main. Partager une règle commune de réussite et isoler les profils utilisateur.
- Remettre le cône en accord avec les contrats actuels ; vérifier les ensembles d'assets plutôt que les anciens comptages. Réviser le banc des raretés selon la décision §53 et le plan 21 F2 : raretés hautes rares sans Chance, fréquence accrue avec la Chance. Le seuil historique « plus du double en zone Effacée » doit être confronté à ce contrat, sans rééquilibrer le jeu pour satisfaire un test périmé.
- Prévoir un lanceur global qui construit et importe une fois puis exécute séquentiellement les scènes, avec résultat individuel et échec global. Empêcher deux builds/imports de ce lanceur d'écrire simultanément dans le même checkout.

**Sortie :** le moteur de substitution qui quitte avec 42 échoue ; import invalide, timeout et marqueur absent échouent ; les suites cône, UI et armes terminent avec leurs vrais résultats ; Main fonctionne via son lanceur. Les erreurs attendues des scénarios négatifs sont identifiées précisément.

**Découpage d'exécution Q0, 2 octobre :** reproduire et conserver les sorties actuelles ; ajouter une bibliothèque de validation pour build/import, retours moteur, logs et résultats ; l'appliquer aux lanceurs de régression et de mesure ; réparer les contrats du cône et des catalogues UI ; ajouter le lanceur global séquentiel et son verrou de checkout ; vérifier les pannes injectées puis les vraies scènes. Le test de raretés a déjà été révisé dans les lots 21 F : le revalider sans remplacer ses nouvelles règles. Aucun correctif des autres lots Q1–Q14 n'est inclus.

**Diagnostic pendant Q0 :** le contrôle strict expose quatre erreurs du compilateur de shaders headless lors du chargement de `player_projectile.gdshader` dans trois suites. Les assertions du banc d'armes passent également en GL. Ajouter à ce lot un correctif équivalent du shader, sans passage du sampler intégré à une fonction auxiliaire ; vérifier les deux renderers et comparer les images avant/après. Ne pas masquer ce diagnostic dans les journaux.

**Livré et vérifié le 2 octobre :** bibliothèque commune, profils normalisés/isolés, verrou de checkout, lanceurs de régression et de mesure stricts, résultats A/B complets exigés, contrats cône/UI réparés et smoke du vrai Hub avec bouton utilisable. `tools/validate.sh` partage son build/import : **21/21 suites, zéro warning, aucune source modifiée**. Huit tests de lanceurs couvrent les pannes et les mutations de source ; mesures réelles sur deux seeds, exports Debug/Release et préchauffage GL vérifiés. Shader des projectiles compatible headless, image GL identique avant/après, run capturée. Les changements de gameplay concurrents ont été exclus du checkout de validation ; aucun gain FPS n'est affirmé. [Compte rendu et preuves](../audits/qualite-2026-10-02/q0/README.md), [guide](../VALIDATION.md).

### Q1 — Fermer les outils de modification de run en production

Constat : F01. Plans associés : 04, mode dev et 09 pour l'intégrité du score.

Inventorier tous les points d'entrée de debug, capturer/tester leur usage local, puis exclure leurs contrôles et créations des assemblies et scènes distribuées. Réserver les actions au profil dev protégé et conserver une provenance explicite lorsqu'un scénario de test altère une run. Élargir le contrôle d'export au panneau F4, à l'overlay F1 et aux arguments de test qui modifient la progression.

**Sortie :** ces actions restent utilisables dans le profil dev ; elles sont inaccessibles dans les deux configurations d'export. Le profil normal conserve ses acquis, et aucune run d'essai altérée n'envoie de score normal à Steam. Vérifier en assembly puis dans le paquet jouable de Q12.

**Découpage d’exécution Q1, 2 octobre :** relever les accès F1/F4 et les commandes des bancs ; retirer les panneaux de la scène partagée et compiler leurs créations/contrôles seulement en local, avec accès réservé au profil dev ; exclure les sources de `tools/` des assemblies distribuées et leurs ressources par l’exclusion native du répertoire `tools/` (`.gdignore`), avec les panneaux déplacés dans ce répertoire ; retirer les hooks d’invincibilité, d’essai et de simulation des exports ; identifier les sessions de banc avant l’initialisation Steam et enregistrer la provenance dans l’historique ; tester les profils, les vrais panneaux et les deux assemblies. Validation sur un instantané isolé pour préserver les mesures d’équilibrage en cours ; aucune valeur de gameplay modifiée. Le paquet jouable multi-OS reste la recette Q12.

**Q1 livré et vérifié, 3 octobre.** Le code est entré avec le commit c5fac34e (panneaux F1/F4 déplacés dans `tools/development/`, exclus par `tools/.gdignore`, provenance Normal/Test/Development résolue avant les autoloads, hooks de banc refusés hors profil dev ou banc, envois Steam coupés hors run normale). Vérification sur `main` au commit 70f523c6, sans modification locale : `tools/validate.sh` sur les suites smoke, dev_mode, development_tools, dev_release et launchers, **5/5**, sources inchangées. Outils dev : 16 contrôles en lancement normal, 11 en essai, 19 en profil dev ; sauvegardes normales (méta, historique, record, analytics) identiques après les essais. Exports Debug et Release : outils, bancs et hooks absents des assemblies, arguments dev/test sans effet ; archive de ressources sans aucune ressource de développement (7 246 fichiers). [Preuves](../audits/qualite-2026-10-02/q1/). **Reste à Q12 :** le paquet jouable multi-OS ; le fonctionnement réel de Steam n'est pas prouvé en headless.

### Q2a — Protéger les fichiers de sauvegarde

Constat : F02. Plans associés : 02 et infrastructure méta.

Sérialiser avant toute ouverture de la destination ; écrire et vérifier un temporaire, remplacer la destination et conserver la dernière version valide. Vérifier les formes et versions JSON avant désérialisation. Récupérer depuis la copie valide ; préserver un fichier illisible pour diagnostic. Refuser sans réécriture une version future non prise en charge. Faire remonter un résultat de sauvegarde explicite.

**Sortie :** interruption aux étapes d'écriture/remplacement, racine `[]`, fichier tronqué, version invalide ou future et erreur d'accès ne détruisent pas la dernière sauvegarde valide. Les migrations existantes et la séparation normal/dev passent avec des profils temporaires. Qualifier les limites de remplacement de fichier selon les plateformes livrées.

**Découpage d'exécution Q2a, 4 octobre ([DECISIONS §62](DECISIONS.md)) :** un utilitaire commun `SaveFile` pour les quatre fichiers d'acquis (méta, historique, record, agrégat analytics) ; les réglages `.cfg` restent hors du lot (préférences, pas des acquis).
- **Écriture :** contenu sérialisé en mémoire d'abord ; écrit dans `<fichier>.tmp`, vidé sur disque, relu et comparé ; remplacement de la destination par renommage, la version précédente devient `<fichier>.bak`. Tout échec renvoie un `SaveWriteResult` explicite et laisse la destination intacte.
- **Lecture :** chaque fichier a son contrôle de forme (racine, type et valeur de version, désérialisation complète). Un `.tmp` resté d'une écriture interrompue est ignoré. Un fichier illisible est déplacé en `<nom>.corrupt-<date>` puis la lecture reprend sur `.bak` ; une destination absente avec un `.bak` valide est restaurée.
- **Version future ou fichier inaccessible :** lecture au mieux, écritures refusées pour la session, fichier jamais réécrit.
- **Vérification :** scène `SaveFileRegression` et `tools/test_saves.sh` (suite `saves` de `tools/validate.sh`) : interruption avant et pendant le remplacement, racine `[]`, fichier tronqué, version invalide et future, erreur d'écriture, migration V1 inchangée, profils normal/dev (`test_dev_mode.sh`).

### Q2b — Finaliser une run une seule fois

Constat : F02. Dépend de Q2a. Plan associé : 02, score et bilan.

Donner une identité à la finalisation, calculer ses récompenses une fois et mémoriser son état validé dans la persistance. Séparer calcul, engagement des acquis et affichage du bilan. Éviter les sauvegardes méta intermédiaires au milieu d'une même attribution. Définir la reprise entre méta et historique : un historique manquant se répare sans réattribuer les Vestiges, et un échec reste visible.

**Sortie :** double appel et reprise après interruption ne doublent ni monnaie, ni statistiques, ni déblocages ; une fin de run ne s'annonce pas sauvegardée après un échec. Vérifier l'interruption entre chaque étape persistante, sans prétendre qu'un remplacement atomique d'un seul fichier rend plusieurs fichiers transactionnels.

**Q2a livré et vérifié, 4 octobre.** `SaveFile` écrit méta, historique, record et agrégat analytics par temporaire relu puis renommage, la version précédente en `.bak`. Chaque lecture contrôle la forme : racine, version entière, désérialisation complète. Un fichier illisible part en `.corrupt-<date>` et la lecture reprend sur `.bak` ; une destination absente se restaure depuis `.bak`, ou depuis un `.tmp` sain si le remplacement s'est arrêté entre ses deux renommages. Version future, fichier inaccessible ou archivage V1 impossible : lecture au mieux, écritures refusées pour la session, fichier intact. `Save()` et `SaveRun()` renvoient un `WriteResult`.
- **Vérifié :** `tools/test_saves.sh`, 39 contrôles ; `tools/validate.sh` sur smoke, dev_mode, development_tools, saves, movement-integration et launchers, **6/6**, sources inchangées. Build sans avertissement.
- **Relecture `godot-reviewer` :** aucun écrasement d'une sauvegarde valide. Corrigés : `.tmp` sain promu quand la destination manque, journal « Saved run » seulement après une écriture réussie, statut de chargement remis à zéro au changement de profil, `var` retiré.
- **Outil :** `tools/check_validation_log.py` admet des erreurs attendues par `VALIDATION_EXPECTED_ERRORS` (expression régulière), réservé aux scénarios qui corrompent exprès une sauvegarde.
- **Reste pour Q2b :** prévenir le joueur (aucun écran n'affiche encore un profil illisible, futur ou une écriture refusée) ; résultats d'écriture encore ignorés par la fin de run. **Limites :** réglages `.cfg` hors lot ; sous Windows, `File.Replace` est atomique, sous Linux et macOS il passe par un lien puis un renommage (cas couvert par la promotion du `.tmp`) ; non vérifié sur Windows ni macOS. Une corruption juste après une migration V1 reprend sur le V1 et refait la migration.

**Découpage d'exécution Q2b, 4 octobre :**
- **Identité :** chaque fin de run reçoit un `run_id`, porté par son relevé dans l'historique.
- **Une seule attribution :** `SaveEndOfRun` ne fait rien la seconde fois ; la méta retient les dernières runs réglées et refuse d'en régler une deux fois.
- **Un seul engagement méta :** Vestiges, statistiques, déblocages, identité de la run et relevé en attente d'historique partent dans **une** écriture (sauvegardes regroupées), au lieu de trois. Les quêtes de progression appliquent récompense et validation dans une même écriture.
- **Ordre et reprise :** méta d'abord, historique ensuite, puis le relevé en attente est retiré. Un historique manquant se répare au lancement suivant (Hub) depuis ce relevé, sans réattribuer les Vestiges.
- **Échec visible :** le bilan dit que la progression n'a pas été enregistrée quand l'engagement méta ou l'historique échoue.
- **Vérification :** double appel, échec de la méta, échec de l'historique puis réparation, interruption entre méta et historique, quêtes ; suites `saves`, `dev_mode`, `development_tools`, `movement-integration`.

**Q2b livré et vérifié, 4 octobre.** Chaque fin de run porte un `run_id`. `SaveEndOfRun` ne fait rien au second appel. `MetaSaveManager.SettleRun` engage en **une** écriture (au lieu de trois) les Vestiges, les statistiques, les déblocages, l'identité de la run (20 dernières retenues) et son relevé en attente d'historique. Une run déjà réglée ne rapporte rien. `RunSettlement` écrit la méta, puis l'historique, puis retire le relevé ; le camp complète au lancement les relevés restés en attente, sans réattribuer. Les quêtes de progression écrivent récompense et validation ensemble. Les lots d'écritures se ferment toujours, même sur exception.
- **Échec visible :** au bilan, « Progression non enregistrée » si la méta échoue, « la run rejoindra les Chroniques au camp » si seul l'historique est en attente. Au camp, sous les Vestiges : sauvegarde illisible, reprise sur la copie, version plus récente, fichier inaccessible ou dernière écriture en échec. Avis du camp capturés (profil illisible, version future) et regardés ; la ligne du bilan n'apparaît qu'en cas d'échec, non capturée.
- **Vérifié :** `tools/test_saves.sh`, 58 contrôles (19 de plus) : lot sans écriture intermédiaire, double règlement, double appel de la vraie fin de run, interruption entre méta et historique puis redémarrage, historique en échec sur deux runs de suite puis réparation, méta en échec (disque inchangé, historique non touché). `tools/validate.sh` sur smoke, dev_mode, development_tools, saves, movement-integration et launchers, **6/6**. Build sans avertissement.
- **Relecture `godot-reviewer` :** pas de double attribution. Corrigés : lots fermés par `try/finally` (une exception aurait sinon bloqué toutes les écritures suivantes en silence), plusieurs relevés en attente au lieu d'un seul emplacement, bilan qui distingue acquis perdus et historique en attente. Le test a révélé en plus qu'un ajout d'historique en échec restait en mémoire et trompait la réparation : la mémoire est maintenant remise à l'état du disque.
- **Limites :** après un échec de la méta, les acquis restent en mémoire et partent à la prochaine écriture réussie de la session, sans doublon (la run est déjà marquée réglée). Le record (`highscore_kills.save`) reste un fichier à part, sans risque de doublon (maximum). Envois Steam et analytics protégés par le seul garde du second appel ; leur coordination reste à Q3.

### Q3 — Coordonner les opérations Steam

Constats : F03, F11. Plan associé : 09.

Associer chaque recherche, upload et download à son contexte ; sérialiser les opérations ou maintenir un état indépendant par requête. Réserver le weekly à un contexte explicite de défi, s'il existe ; suspendre cet upload tant que son parcours n'est pas livré. Préserver les clés externes Steam existantes.

**Sortie :** quatre destinations suivies correctement à cache froid/chaud, chevauchement d'un download, erreurs et retries maîtrisés. Vérification avec un substitut du service, puis une session Steam sur une plateforme compatible ; l'absence de cette session reste signalée.

**Découpage d'exécution Q3, 4 octobre ([DECISIONS §64](DECISIONS.md)).** Relevé dans `SteamLeaderboards` (nœud ajouté à `Main` par `GameBootstrap`) :
- **F03 :** un seul emplacement d'attente (`_pendingUploadBoard`, `_pendingUploadScore`) et un seul `CallResult` par sorte d'appel. Les quatre envois de fin de run le réaffectent à la suite ; dans Steamworks.NET, `CallResult.Set` abandonne l'appel précédent. À cache froid, seule la dernière recherche (Weekly) aboutit, avec le dernier score en attente ; à cache chaud, les quatre envois partent mais un seul résultat revient. Une lecture lancée pendant ce temps partage la même recherche.
- **Pause :** à la mort, `SaveEndOfRun` envoie les scores, puis `DeathSequence` met l'arbre en pause jusqu'au retour au camp. `SteamManager` hérite de la pause et ne pompe plus les rappels Steam : les réponses n'arrivent qu'au camp, quand le nœud de `Main` qui les attendait est détruit.
- **F11 :** `Vestiges_Weekly` reçoit toutes les runs et rien ne prouve que Steam le remette à zéro (le code le suppose). Décision §64 : toutes les runs, un tableau par semaine.
- **Hors lot :** aucun écran ne lit les classements (la lecture n'a pas d'appelant) ; les succès gardent des restes V1 (`ACH_PLACE_50_STRUCTURES`, à Q14).

1. **Destinations :** `RunLeaderboards` (C# pur) calcule les envois d'une run : Global et Weekly reçoivent le score, Crises le nombre de crises, un tableau par personnage le score (aucun si l'identifiant est vide). Weekly = `Vestiges_Weekly_<année ISO>-W<semaine>`, à partir de la date UTC de la fin de run. Les noms Steam restent dans le code (clés de protocole externe, §2).
2. **File d'opérations :** `LeaderboardQueue` (C# pur, sans type Steamworks, donc chargeable hors x86/x64) traite **une opération à la fois** : recherche du tableau si son identifiant n'est pas en cache, puis envoi ou lecture. Chaque opération a son jeton ; un rappel tardif ou d'une autre opération est ignoré. Délai de réponse de 15 s, puis nouvelle tentative ; 3 tentatives au plus, puis échec signalé et opération suivante. File bornée (64), refus explicite au-delà. Résultat par opération (tableau, score, réussite, rang), journalisé.
3. **Service Steam :** `ILeaderboardBackend` sépare la file de Steamworks ; `SteamLeaderboardBackend` fait les trois appels (`FindOrCreateLeaderboard`, `UploadLeaderboardScore` en gardant le meilleur, `DownloadLeaderboardEntries`). Une seule opération en vol, donc un `CallResult` par sorte d'appel suffit.
4. **Propriétaire unique :** `SteamManager` (autoload) possède la file, qui survit au changement de scène ; il pompe les rappels et avance les délais **même en pause** (`ProcessMode` toujours). `SteamLeaderboards` et sa création dans `GameBootstrap` disparaissent ; `ScoreManager` appelle `SteamManager.SubmitRunScores`. La file est vidée à l'arrêt de Steam et quand une session devient une session d'essai.
5. **Vérification :** scène `LeaderboardQueueRegression` avec un faux service piloté à la main, `tools/test_steam_leaderboards.sh`, suite `steam_leaderboards` de `tools/validate.sh`. Cas : cache froid (une recherche par tableau, chaque score sur son tableau), cache chaud (aucune recherche), lecture en même temps que les envois, recherche et envoi en échec puis réussite, trois échecs puis passage à la suite, délai dépassé puis réponse tardive ignorée (pas de double envoi), arrêt avec opérations en attente, file pleine, semaines ISO aux changements d'année, personnage absent. **Non prouvé :** le service Steam réel (bibliothèque native absente, App ID 480) ; à vérifier avec l'App ID du jeu, au plus tard à Q12.

**Q3 livré et vérifié, 4 octobre.**
- **Code :** `RunLeaderboards` calcule les envois d'une run (Global, personnage, Crises, `Vestiges_Weekly_<année ISO>-W<semaine>`). `LeaderboardQueue` traite les opérations une à une, avec un jeton par tentative, un délai de 15 s, 3 tentatives espacées de 2 s, une file de 64 au plus et un résultat par opération. `ILeaderboardBackend` la sépare de Steamworks ; `SteamLeaderboardBackend` fait les trois appels. `SteamManager` possède la file, pompe les rappels en pause (`ProcessMode` toujours) et avance les délais en temps réel. `SteamLeaderboards.cs` et sa création dans `GameBootstrap` sont supprimés ; `ScoreManager` appelle `SteamManager.SubmitRunScores`.
- **Écarts au découpage, issus de la relecture :**
  - L'opération suivante ne démarre jamais depuis un rappel du service, mais à l'image suivante. Selon le dispatcher de Steamworks.NET, réarmer un `CallResult` pendant son propre rappel pourrait faire perdre la réponse suivante.
  - Une exception du service compte comme une tentative en échec ; celle d'un abonné est journalisée sans bloquer la file.
  - Les délais se comptent en temps réel : la séquence de mort ralentit `Engine.TimeScale` à 0,03 juste après l'envoi.
- **Vérifié :** `tools/test_steam_leaderboards.sh` (suite `steam_leaderboards`), 56 contrôles avec un faux service :
  - semaines ISO aux changements d'année, envois d'une run ;
  - cache froid (une recherche par tableau, chaque score sur son tableau) et cache chaud (aucune recherche) ;
  - lecture mêlée aux envois ;
  - échecs relancés puis réussis, abandon après 3 tentatives, lecture en échec, refus immédiat sans boucle, exception du service ;
  - délai dépassé : réponse tardive ignorée pendant l'attente et pendant la relance, ancienne recherche tardive ignorée ;
  - suite démarrée à l'image suivante, abonné qui ajoute un envoi, arrêt avec opérations en cours ou en attente, file pleine.

  Trois erreurs injectées dans la file (jeton ignoré, deux opérations en vol, suite lancée dans le rappel) font chacune échouer le test. Le jeton ignoré n'était d'abord pas détecté : le cas « réponse tardive pendant la relance » a été ajouté. `tools/validate.sh` sur smoke, steam_leaderboards, saves, dev_mode, development_tools, dev_release, movement-integration et launchers : **8/8**. Build sans avertissement.
- **Relecture `godot-reviewer` :** aucun bloquant, aucun score sur le mauvais tableau, aucun type Steamworks nommé hors des chemins atteints quand Steam est actif. Corrigés : délais en temps réel, suite hors du rappel, exceptions, cas de test manquants, liens des plans 00 et 09. Justifié : le test de `tools/tests` n'a pas de `.cs.uid`, car `tools/` est exclu de l'import (comme `IndicibleRegression`).
- **Non prouvé :** le service Steam réel (bibliothèque native absente du dépôt, App ID 480). En particulier, le comportement réel de `CallResult` ; la création des tableaux par `FindOrCreateLeaderboard` ; les noms des joueurs hors amis (`GetFriendPersonaName` sans `RequestUserInformation`), à traiter le jour où un écran lira les classements. À vérifier avec l'App ID du jeu, au plus tard à Q12.
- **Limites :**
  - Pire cas d'une panne réseau : environ 30 s par tentative, donc plusieurs minutes de file bloquée pour quatre tableaux, puis abandon. Un score abandonné n'est pas rejoué à la session suivante (pas de persistance).
  - Les succès Steam gardent des restes V1 (`ACH_PLACE_50_STRUCTURES`), à Q14.

## 4. Lots de propreté et de données

### Q4 — Rendre le chargement récupérable

Constat : F09. Plan associé : 10, chargement.

Observer la tâche de chargement, formaliser réussite/échec/annulation, protéger les reprises après `await` et libérer les ressources temporaires. En cas d'erreur de catalogue ou génération, sortir de la pause et proposer un retour au Hub avec un diagnostic utile. Ce lot précède le durcissement des lecteurs, qui rendra explicites davantage d'erreurs.

**Sortie :** fichier invalide, exception injectée et fermeture pendant génération ne laissent ni arbre bloqué en pause, ni tâche utilisant une scène détruite. Chargement normal et Hub passent ; capture du parcours d'erreur si l'UI change.

**Découpage d'exécution Q4, 4 octobre** (validé en cinquième position, §67). Relevé : `GameBootstrap._Ready` lance `SetupNormalGameAsync` sans l'observer (`_ = …`), après avoir lu sept catalogues hors de toute protection ; l'arbre est mis en pause dès la première ligne et n'est rendu qu'à la dernière. Une seule reprise après `await` vérifie que la scène existe encore (après les shaders). `WorldSetup.InitializeWorldAsync` attend la génération lancée sur un thread (`Task.Run`), puis cède la main une dizaine de fois sans contrôle ; une sortie de scène pendant la pose des décors laisse `PropStaging`, des milliers de nœuds hors de l'arbre, sans propriétaire. `EnemyPool.PrewarmAsync` crée des créatures après une attente sans vérifier son pool.
1. **États du chargement :** `GameBootstrap` exécute tout le chargement, lecture des catalogues comprise, dans une méthode observée. Trois issues explicites : réussite (dépause, fondu), **échec** (journal avec l'étape en cours, le type et le message de l'exception, pile complète ; écran d'erreur ; l'arbre reste en pause derrière lui, pour que la run à moitié construite ne s'anime pas, et le bouton le libère), **abandon** (la scène a été quittée : aucune suite, aucune erreur). Chaque reprise après `await` vérifie que la scène est encore là, dans le bootstrap, `WorldSetup` et le préchauffage du pool.
2. **Ressources :** à la sortie de scène pendant le chargement, l'arbre est dépausé, `PropStaging` est libéré, et une génération encore en cours est observée (son exception éventuelle va au journal au lieu de rester sans témoin).
3. **Écran d'erreur :** l'écran de chargement s'arrête et affiche une phrase, l'étape et le message, et un bouton « Retour au camp » focalisé (manette et clavier), qui ramène au Hub. Textes FR et EN.
4. **Pannes injectées, réservées aux bancs :** une étape nommée peut lever une exception, sur le fil principal ou dans le thread de génération. Le crochet n'existe que dans les assemblies de développement et n'agit qu'en session de banc (même garde que Q1).
5. **Vérification :** scène `LoadingRecoveryRegression`, `tools/test_loading.sh`, suite `loading` de `tools/validate.sh`. Cas : chargement normal ; exception à l'étape des décors ; exception dans le thread de génération ; scène quittée pendant la génération ; scène quittée pendant la pose des décors (nœuds orphelins revenus au niveau d'avant) ; fermeture du jeu pendant le chargement (sortie propre). Puis suites smoke, movement-integration, dev_mode, dev_release ; capture de l'écran d'erreur, regardée.

**Limite écrite :** `tools/validate.sh` refuse qu'un banc modifie les sources. Le cas « fichier invalide » est donc représenté par une exception à l'étape des catalogues ; les lecteurs qui refusent un fichier faux au lieu de continuer viennent avec Q7.

**Q4 livré et vérifié, 4 octobre.**
- **Code :** `GameBootstrap.RunLoadAsync` est la seule entrée du chargement ; aucune exception n'en sort. L'étape en cours est nommée (`Step`) pour le journal et l'écran. `LoadGuard.EnsureAlive` suit chaque attente dans `GameBootstrap` et `WorldSetup` (dont `YieldFrame`, devenu une tâche qui vérifie la scène) ; `EnemyPool.PrewarmAsync` s'arrête si son pool a quitté l'arbre. À la sortie de scène en plein chargement : dépause (`GameBootstrap._ExitTree`), `PropStaging` libéré, génération en cours observée (`WorldSetup._ExitTree`) ; `decalStaging` est libéré même si la construction échoue. Les lecteurs de catalogues restent exécutés dans `_Ready`, dans le même ordre, mais sous protection.
- **Visible :** en cas d'échec, l'écran de chargement s'arrête sur « Le chargement a échoué. », l'étape et le message, et un bouton « Retour au camp » focalisé ; l'arbre reste en pause derrière lui, le bouton le libère et ramène au Hub. Si l'écran ne peut pas se construire, retour direct au camp. Textes FR et EN.
- **Pannes injectées :** `LoadGuard.InjectFault` (catalogues, génération dans son thread, décors), `[Conditional("TOOLS")]`, champ de réglage sous `#if TOOLS`, actif seulement en session de banc.
- **Vérification :** build sans avertissement ; `tools/test_loading.sh`, 7 scénarios en lancements séparés (normal ; panne aux catalogues, dans le thread de génération, aux décors ; scène quittée pendant la génération, pendant la pose des décors ; fermeture pendant le chargement), 20 contrôles. Contre-épreuves par mutation, code restauré ensuite : sans libération de `PropStaging`, sans dépause à la sortie, sans écran d'erreur, le banc échoue chaque fois sur le contrôle attendu. `tools/validate.sh` 7/7 (smoke, loading, movement-integration, dev_mode, development_tools, dev_release, saves), puis 4/4 après les corrections de relecture. Capture de l'écran d'erreur en 1080p, regardée. Relecture `godot-reviewer` : aucun bug bloquant ; trois constats corrigés (`decalStaging`, repli si l'écran d'erreur échoue, `using` sous `#if TOOLS`). Le quatrième (le fondu est lancé avant le marqueur de réussite) est laissé : `FadeOut` ne fait que créer une interpolation.
- **Reste :** le cas « fichier invalide » est représenté par une panne aux catalogues ; les lecteurs qui refusent un fichier faux viennent avec Q7. Le nom d'étape affiché est en français dans les deux langues (libellé technique, aussi écrit au journal).

### Q5 — Typer la grammaire des attaques

Constats : retour §54, F12, F14. Plans associés : 05, 17 et 21.

Commencer par le motif d'attaque et la catégorie mêlée/distance : types fermés, conversion dans les lecteurs et ascensions, puis migration cohérente de `WeaponInstance`, `Player`, `WeaponProperties`, `WeaponTraits`, objets et UI. Conserver les clés JSON existantes et leur compatibilité. Supprimer les normalisations et comparaisons répétées dans le domaine. Traiter ensuite les effets à l'impact et spéciaux dans Q6a, plutôt que migrer toutes les chaînes du dépôt dans le même lot.

**Sortie :** aucune sélection du motif d'attaque par string dans les consommateurs migrés ; motif inconnu rejeté précisément. Toutes les armes et ascensions se chargent. Les dégâts, cibles, cadences, nombres et portée restent identiques aux scénarios de référence ; armes, objets, spécialisations et choix passent. Captures pour orbite, chaîne, homing, cône et mêlée. Mesurer les chemins fréquents si leur coût est modifié.

**Découpage d'exécution Q5, 3 octobre** (choisi par Raphaël, §58). Relevé : 24 armes, 7 motifs (`linear`, `arc`, `burst`, `chain`, `circular`, `homing`, `orbital`), 3 catégories (`melee`, `ranged`, `special`), 11 voies d'ascension qui changent de motif. Les chaînes circulent dans `Player` (choix de l'attaque, orbite, mêlée, salve, guidage, portée affichée, allonge du personnage), `WeaponProperties`, `WeaponTraits`, `UpgradeText`, `HubCollectionPanel` et trois bancs, avec des `ToLower` répétés.

1. **Types et lecture :** `AttackPatternKind` et `WeaponCategory` dans `Infrastructure/WeaponGrammar.cs`, avec la table clé JSON ↔ type et les clés de libellé. `WeaponDataLoader` convertit `type` et `attack_pattern` de l'arme et de ses voies une seule fois ; valeurs par défaut inchangées (`ranged`, `linear`). Une clé inconnue écarte l'arme avec un diagnostic qui nomme l'arme, la voie et le champ, et la liste des valeurs admises.
2. **Consommateurs :** `WeaponData`, `WeaponInstance`, `WeaponAscensionData`, `Player`, `WeaponProperties`, `WeaponTraits`, `UpgradeText`, `HubCollectionPanel` lisent le type ; plus aucune comparaison de motif ni de catégorie par chaîne. Les libellés de la Collection passent par des clés de traduction (le motif `burst` n'y avait pas de nom).
3. **Vérification :** fixture négative (motif, catégorie et motif de voie inconnus rejetés, message précis, sans erreur moteur), bancs armes, objets, perks, choix, cône et mouvement verts ; captures orbite, chaîne, guidage, cône et mêlée. Aucun chemin fréquent n'alloue : le coût ne peut que baisser (comparaison d'énumérations au lieu de `ToLower`), pas de banc FPS.

**Hors Q5 :** effets à l'impact et spéciaux (`slow`, `sustained_cone`…) au Q6a ; type des créatures (`Enemy`, `SpawnManager`) au Q6c/Q7.

**Q5 livré et vérifié, 3 octobre.**
- **Code :** `WeaponGrammar` (types, table des clés, clés de libellé) ; `WeaponDataLoader.TryParseWeapon` lit `type` et `attack_pattern` de l'arme et de ses voies, et renvoie un diagnostic (`arme X, voie Y, attack_pattern « Z » inconnu (liste)`) ; l'arme fautive est écartée avec une erreur au chargement. Une voie sans motif, ou à `null`, garde celui de l'arme. `WeaponData.Category` remplace `Type`. Plus aucune comparaison de motif ou de famille d'arme par chaîne, ni de `ToLower`, dans `Player`, `WeaponProperties`, `WeaponTraits`, l'interface et les bancs.
- **Visible :** la Collection lit ses libellés par traduction (FR/EN) et nomme enfin la salve (« Salve ») ; la carte « Nouvelle » des deux armes `special` (Boîte à musique, Craies) dit « Spéciale » au lieu de « Distance ».
- **Vérification :** build sans avertissement ; fixture négative (motif, famille et motif de voie inconnus refusés, messages précis, aucune erreur moteur) ; worktree 12/12 suites (smoke, armes, objets, trois suites de perks, choix, cône, mouvement, intégration Main, UI, mode dev), puis `main` fusionné 8/8 (avec petits lieux et carte). Relecture `godot-reviewer` : branches équivalentes, aucun consommateur oublié ; ses deux remarques de code corrigées (doc de banc déplacée, motif `null`). Captures avant/après sur 8 armes (orbite, chaîne, guidage, cône, arc, onde, salve, ligne) : mêmes attaques, mêmes dégâts. Pas de banc FPS : les chemins chauds comparent des énumérations au lieu d'appeler `ToLower`. [Preuves](../audits/qualite-2026-10-02/q5/).
- **Piège évité :** `WeaponRegression` appelait `PerformMeleeAttack("arc")` par réflexion, sans erreur de compilation ; corrigé.

### Q6a — Expliciter les contrats et paramètres des effets d'armes

Constats : retour §54, F12. Dépend de Q5. Plans associés : 05, 17 et 21.

Définir les mécanismes d'effets et leurs paramètres utiles avec types, unités et bornes ; résoudre les dictionnaires lors du chargement. Rendre explicites les réglages de secours actuels dans les données ou profils de données partagés. Valider les stats de base/croissance et leurs clés avec le catalogue de statistiques existant. Contrôler les références audio, projectile et VFX ; les algorithmes de dessin restent dans leurs modules.

**Sortie :** typo de mécanisme/stat, paramètre requis absent, valeur non finie ou référence absente produit un diagnostic précis avant la run. Les réglages migrés reproduisent exactement les valeurs effectives antérieures, ascensions comprises. Une arme supplémentaire utilisant un mécanisme existant fonctionne sans ajout de branche par son ID dans `Player`.

**Découpage d'exécution Q6a, 4 octobre.** Relevé : 24 armes, 4 effets à l'impact (`dot`, `slow`, `disorient`, `freeze`) dont 9 remplacés par des voies, 7 effets spéciaux (`heal_every_n_hits`, `instant_disintegrate`, `delayed_echo`, `ground_fire`, `local_time_slow`, `random_shape`, `sustained_cone`), 6 voies à `special_overrides`, une à `flags`/`params` (`orbit_pulse`). 18 stats d'armes dans les données ; `Player` et `WeaponInstance` lisent chaque stat avec un secours écrit à l'appel (14 valeurs, divergentes pour `damage`, `range`, `attack_speed`, présentes dans les 24 armes donc jamais utilisées). Les effets spéciaux lisent leurs réglages avec 17 secours dans `Player`, dont trois absents des données (`echo_radius` 40, `echo_count` 1, `freeze_seconds` 0) et un en dur (l'onde du Dessin d'enfant fait 0,5 × les dégâts). `essence_cost_per_attack` (3 armes) n'est lu nulle part.

1. **Contrat en données :** `data/weapons/weapon_contract.json` déclare chaque stat d'arme (secours actuel, bornes, obligatoire ou non), chaque effet à l'impact et spécial (réglages, bornes, entier ou non, secours pour les seuls réglages facultatifs), les drapeaux de voie et leurs réglages, les styles et projectiles d'effet admis. `WeaponContract` le lit et refuse un contrat qui ne couvre pas exactement les types du code.
2. **Types :** `OnHitEffectKind` et `SpecialEffectKind` remplacent les chaînes ; `Player` et `WeaponTraits` comparent des types. Les réglages d'un effet sont résolus au chargement, secours compris : plus aucun `TryGetValue(…) ? … : secours` dans `Player`. L'onde du Dessin d'enfant reçoit un réglage `shape_damage_ratio` (0,5).
3. **Stats :** `WeaponInstance.GetStat(clé)` prend le secours du contrat ; les secours écrits à l'appel disparaissent.
4. **Diagnostics au chargement :** clé de stat, de croissance, de multiplicateur ou de remplacement inconnue ; croissance sans réglage d'amélioration ; valeur non finie ou hors bornes ; type d'effet inconnu ; réglage requis absent, inconnu ou hors bornes ; `special_overrides` sans effet spécial ou hors de ses réglages ; drapeau inconnu ou réglage manquant ; son, style, famille ou projectile introuvable. L'arme fautive est écartée avec un message qui nomme l'arme, la voie et le champ, comme en Q5.
5. **Vérification :** relevé des valeurs effectives (stats, effet, réglages) de chaque arme et de chaque voie au commit de base et après, identiques ; fixture négative par catégorie de diagnostic ; suites armes, objets, perks, choix, cône, mouvement, intégration Main. `essence_cost_per_attack` reste déclaré « non lu », question pour Raphaël.

**Q6a livré et vérifié, 4 octobre.**
- **Code :** `data/weapons/weapon_contract.json` et `WeaponContract` déclarent 18 stats d'armes, 4 effets à l'impact, 7 effets spéciaux et leurs réglages, le drapeau `orbit_pulse`, les styles, familles et alias de projectiles. `OnHitEffectKind` et `SpecialEffectKind` remplacent les chaînes dans `Player`, `WeaponTraits` et `WeaponProperties`. Les réglages des effets sont résolus au chargement : `Player` lit `se.Get(SpecialEffectParam.…)`, sans aucun secours écrit à l'appel. `WeaponInstance.GetStat(clé)` prend le secours du contrat : plus aucun secours écrit à l'appel dans `Player`, `WeaponInstance`, `WeaponTraits`, la pause et les bancs. L'onde du Dessin d'enfant lit `shape_damage_ratio` (0,5) au lieu d'une constante. `WeaponDataValidator` refuse une arme fautive avant toute conversion ; la propriété `Shapes`, jamais lue, est retirée.
- **Diagnostics :** 27 cas refusés par la fixture négative du banc des armes, chacun avec un message qui nomme l'arme, la voie et le champ (stat inconnue, non numérique, non finie, hors bornes, obligatoire absente ; croissance inconnue ou sans réglage d'amélioration ; effet inconnu ; réglage absent, inconnu, non entier ; champ d'arme ou de voie inconnu ; multiplicateur nul ; remplacement hors effet ou sans effet ; drapeau inconnu ou réglage manquant ; son, image, style, famille ou projectile introuvable).
- **Mêmes valeurs :** relevé de 1 259 valeurs effectives (chaque arme sans voie puis avec ses deux voies), identique au commit de base ([preuves](../audits/qualite-2026-10-02/q6a/)). Le contrôle a d'abord refusé deux armes réelles : mes bornes étaient fausses (cadence 0 de la Boîte à musique, vitesse 0 du cône), corrigées dans le contrat.
- **Fuite à la fermeture :** une première version du validateur parcourait les dictionnaires Godot. Le banc de mouvement échouait alors 14 fois sur 15, toutes tentatives de correction comprises : une forme physique ou un `ConfigFile` signalé en fuite à la fermeture du moteur. Le commit de base passait 8 fois sur 8. Mesures : sans validation, 3 sur 3 propres ; avec un passage forcé du ramasse-miettes, 3 sur 3 propres. Les centaines d'enveloppes natives jetées retardaient la finalisation d'autres objets. Le validateur lit désormais l'arme en `System.Text.Json` : 4 sur 4 propres, puis vert dans la validation.
- **Vérifié :** `tools/validate.sh` sur smoke, weapons, objects, les trois suites de perks, choice_screen, cone, movement, movement-integration, ui_art, dev_mode et saves, **13/13**. Build sans avertissement. Relecture `godot-reviewer` : aucun comportement de combat changé, aucune arme réelle refusée. Corrigés : contrat incomplet signalé au lieu d'échouer en silence, familles déclarées dans le contrat (plus de dépendance vers `Combat`), bornes « strictement positif » nommées, dictionnaire vide partagé.
- **Pas de banc FPS :** sur les chemins chauds, une recherche de dictionnaire remplace une autre, et la variable `out` disparaît.
- **Question pour Raphaël :** `essence_cost_per_attack` (Bâton d'essence 0,5, Lanterne 0,3, Gants de boxe 0,2) n'est lu par aucun mécanisme. Il reste déclaré dans le contrat comme « non lu ». À brancher sur un coût en Essence, ou à retirer ?

### Q6b — Sortir les réglages du boss du code

Constat : retour §54. Plans associés : 07 et late game V2.

Migrer les cadences, dégâts, dimensions de zones dangereuses, seuils de phase et récompense de l'Indicible dans sa configuration. Distinguer les dimensions qui changent la hitbox des détails purement décoratifs. Conserver les algorithmes et la mise en scène dans le module du boss.

**Sortie :** la configuration initiale donne exactement les valeurs actuelles ; réglages invalides rejetés ; séquences, dégâts et récompense vérifiés dans un scénario de boss, puis capture en run. Le lot ne redessine pas le boss ni ne change ses phases.

**Découpage d'exécution Q6b, 4 octobre.** Relevé dans `Combat/Indicible.cs` :
- **Constantes de combat :** cadence des tentacules 2,5 s, première attaque à 30 % de la cadence, cadence enragée à 60 % ; 2 tentacules par attaque, 3 enragé ; annonce 0,8 s ; largeur 30 et longueur 120 du couloir, qui servent aussi au test de touche ; cible tirée à ±60 autour du joueur ; enragé à 50 % des PV ; dégâts 15.
- **Constantes de position :** les bords (demi-longueur 200, demi-épaisseur 40, à 350 du centre) sont à la fois le décor et les zones qui reçoivent les projectiles.
- **Constantes décoratives :** yeux (5, à 330 du centre, ±150, déplacés toutes les 3 s en 1,2 s), excroissances, couleurs, durée du couloir qui frappe (0,4 s), fondus de mort.
- **Doublons :** `data/enemies/indicible.json` déclare déjà 15 de dégâts sans que personne ne les lise ; les PV en ont un secours de 2 000 écrit dans le code ; `ScoreReward` (5 000) ne sert qu'au message de journal, la vraie récompense étant dans `score.json`.

1. **Configuration :** `data/scaling/indicible.json` porte le rythme, les nombres, le seuil d'enrage, le couloir (dimensions de touche), les bords (dimensions des zones touchables) et, à part, les réglages décoratifs. `IndicibleConfig` la lit et la contrôle (bornes, entiers, sections), et refuse une configuration invalide avec un message précis, dès le début de la run (`EndgameManager._Ready`).
2. **Une source par valeur :** les PV et les dégâts viennent de la fiche `indicible.json`, sans secours ; la récompense reste dans `score.json`, et le journal la lit là.
3. **Ni algorithme, ni mise en scène, ni phases ne changent.**
4. **Vérification :** scène `IndicibleRegression` avec configuration lue égale aux constantes actuelles, configurations invalides refusées, scénario de boss (première attaque, rythme normal puis enragé, nombre de tentacules, dégâts reçus, récompense de 5 000 points à la mort), puis capture `--capture-endgame` regardée.

**Q6b livré et vérifié, 4 octobre.**
- **Code :** `data/scaling/indicible.json` et `IndicibleConfig` portent le rythme, les nombres de tentacules, l'enrage, le couloir et les bords (ce qui touche) et, à part, le décor. `Indicible.cs` n'a plus de constante de combat. Les PV et les dégâts viennent de la fiche `indicible.json`, sans secours ; `ScoreReward`, qui ne servait qu'au journal, est retiré (la récompense reste dans `score.json`). Algorithmes, mise en scène et phases inchangés.
- **Configuration invalide :** refusée avec le champ en cause (section ou réglage absent, valeur nulle, négative, non finie, non entière, part hors de ]0 ; 1], nombre au-delà de 32). Elle est détectée au début de la run par `EndgameManager`, qui le dit et retire seulement le boss ; le reste de la run se joue.
- **Vérifié :** `tools/test_indicible.sh` (suite `indicible`), 47 contrôles. Configuration lue égale aux 20 constantes d'avant ; 8 configurations invalides refusées ; combat scripté : première attaque à 0,75 s, puis 2,5 s, enragé 1,5 s ; 2 puis 3 tentacules par attaque ; dégâts 15 × multiplicateur ; +5 000 points à la mort. `tools/validate.sh` sur smoke, indicible, movement-integration, music, choice_screen et launchers, **6/6**. Capture `--capture-endgame` avant/après au même seed : même résultat (6 400 PV, boss vaincu, endgame), même image au cadrage près ([avant/après](../audits/qualite-2026-10-02/q6b/avant-apres-endgame.png)).
- **Relecture `godot-reviewer` :** valeurs identiques à l'ancien code, ligne à ligne. Corrigés : l'exception au chargement rendait la fin de partie inerte en silence, remplacée par un refus explicite ; état « boss apparu » faux si la fiche manque ; plafond des nombres ; chargement du barème retiré du journal de mort.
- **Constat hors lot, à trancher :** dans la capture, en 40 s de combat, l'Indicible ne perd aucun PV et ne touche jamais le joueur. Cause probable, non mesurée : les tirs visent son centre, là où il n'y a rien à toucher, puisque ses zones touchables sont les quatre bords à 350 px. Son apparence se résume à des rectangles sombres. Q6b ne le redessine pas ; question ouverte au tableau de bord.

### Q6c — Déclarer les relations de contenu et les capacités ennemies

Constats : retour §54, F12. Plan associé : 07.

Rendre explicite l'appartenance à une meute dans les données ; conserver l'ensemble actuel des Charognards. Résoudre les capacités depuis un registre de mécanismes validé, avec erreur pour une clé inconnue. Relever les autres règles qui dépendent d'un ID concret et décider pour chacune si elle représente une relation de contenu ou une référence légitime à une entité unique.

**Sortie :** la meute ne dépend plus d'un nom recopié dans son calcul ; une créature configurée avec une capacité existante n'exige pas une nouvelle branche par son ID. Les capacités et le bonus de meute actuels passent leur régression. Les références uniques conservées sont localisées et validées.

**Découpage d'exécution Q6c, 4 octobre.** Relevé sur 13 créatures (`data/enemies/*.json`) :
- **Meute :** `Enemy.ProcessPackBonus` compte les voisins dont l'identifiant vaut `"charognard"`. Seul le Charognard a le comportement `pack`. `pack_bonus_damage` (0,15) est lu mais jamais appliqué : seule la vitesse change. Les secours sont écrits à l'appel (0,15, 0,10, 120).
- **Capacités :** `EnemyAbilityFactory` rend `null` pour une clé inconnue, et l'ennemi la perd avec un simple avertissement. Six clés (`omen_strike`, `pounce`, `charge`, `burrow`, `cry`, `aimed_shot`) lisent 59 réglages, chacun avec un secours écrit à l'appel. Le Hurleur appelle des renforts `shade` par défaut. `fx_family` inconnu retombe en silence sur une famille par défaut. Les sons ne sont pas contrôlés.
- **Grammaire :** `type` (`melee`, `ranged`, `boss`), `behavior` (`default`, `pack`, `sentinel`, `weaver`, `indicible`) et `tier` (`normal`, `elite`, `miniboss`, `boss`) circulent en chaînes dans `Enemy`, `EssenceTracker`, `QuestManager` et `RunTracker`. Une faute de frappe passe sans erreur. Le lecteur parcourt les dictionnaires Godot et n'a aucun contrôle de clé ni de borne.
- **Identifiants en dur :**
  - `"indicible"` dans six fichiers : boss final unique, référence légitime.
  - `"colosse_"` dans `AudioManager` : reste V1, aucune créature ne porte ce préfixe.
  - Deux listes de secours d'apparition dans `SpawnManager` : six identifiants en dur, quand un biome n'a pas de liste.
  - Les variantes (`elite`, `champion`, `aberration`) sont nommées dans `Enemy`, `SpawnManager` et `RunTracker` : relève du catalogue des variantes, donc de Q7.

1. **Types :** `EnemyGrammar` (Infrastructure) déclare `EnemyCombatType`, `EnemyBehavior`, `EnemyTier` et `EnemyAbilityKind`, avec la table clé JSON ↔ type. Les consommateurs comparent des types, plus aucune chaîne. Valeurs par défaut inchangées (`default`, `normal`).
2. **Contrat en données :** `data/enemies/_contract.json` (le préfixe `_` l'exclut des fiches). Il déclare :
   - les stats (obligatoires ou avec secours, bornes) ;
   - chaque capacité, avec ses réglages numériques (secours actuel, bornes, entier ou non ; `first_delay` facultatif sans secours, calculé par le code) et ses réglages textuels (famille, son, créature) ;
   - les valeurs admises de `impact_shake`.

   `EnemyContract` le lit en `System.Text.Json` et refuse un contrat qui ne couvre pas exactement les capacités du code.
3. **Réglages résolus au chargement :** `EnemyAbilityData` reçoit chaque réglage, secours compris ; les capacités lisent `Number(clé)` sans secours écrit à l'appel. `reinforcement_id` devient obligatoire (le Hurleur déclare déjà `shade`).
4. **Meute en données :** `"pack_family": "charognard"` sur le Charognard ; `Enemy` compte les voisins de la même famille. Un comportement `pack` sans famille est refusé. Les réglages de meute passent par le contrat ; `pack_bonus_damage` reste déclaré « non lu », question pour Raphaël.
5. **Diagnostics au chargement :** `EnemyDataValidator` lit chaque fiche en `System.Text.Json`, avant toute conversion. Cas refusés :
   - champ, stat ou visuel inconnu ;
   - stat obligatoire absente, non numérique, non finie ou hors bornes ;
   - type, comportement ou rang inconnu ;
   - capacité inconnue, réglage inconnu, hors bornes ou non entier ;
   - famille, son ou créature de renfort introuvable ;
   - meute sans famille ;
   - identifiant en double.

   La fiche fautive est écartée avec un message qui nomme le fichier, la créature et le champ. Les renforts sont contrôlés une fois toutes les fiches lues.
6. **Identifiants :**
   - `EnemyGrammar.FinalBossId` remplace les six `"indicible"`. `AudioManager` perd le préfixe `colosse_` (aucune créature).
   - Les listes de secours d'apparition passent dans `spawn_flow.json` ; leurs créatures sont contrôlées au chargement.
   - Variantes : à Q7.
7. **Vérification :**
   - relevé des valeurs effectives de chaque créature et de ses capacités, secours compris, au commit de base et après : identique ;
   - fixture négative par diagnostic ;
   - régression de meute (voisins de même famille comptés, autres ignorés) ;
   - suites enemy_abilities, small_places, indicible, movement, movement-integration, smoke, music et launchers.

**Q6c livré et vérifié, 4 octobre.**
- **Code :**
  - `EnemyGrammar` type le combat (`melee`, `ranged`, `boss`), le comportement, le rang et les six capacités. `Enemy`, `SpawnManager`, `RunTracker`, `EssenceTracker` et `QuestManager` comparent des types, plus aucune chaîne.
  - `data/enemies/_contract.json` et `EnemyContract` déclarent les stats, formes, secousses et familles, et pour chaque capacité ses réglages (secours, bornes, entiers, facultatifs) et ses textes (famille, son, créature, secousse).
  - `EnemyDataValidator` contrôle chaque fiche en `System.Text.Json` ; `EnemyDataLoader` est réécrit sans dictionnaire Godot. Fiche fautive, identifiant en double, renfort absent ou renforts en boucle : la fiche est écartée, avec un message qui nomme la créature et le champ.
  - Les capacités lisent leurs réglages résolus (`Number`, `Text`), sans secours écrit à l'appel. Les planchers du code sont devenus les minimums du contrat. `EnemyAbilityFactory` crée une capacité par sorte typée.
- **Relations de contenu :**
  - La meute compte les voisins de la même `pack_family` (le Charognard déclare `charognard`) ; un comportement `pack` sans famille est refusé.
  - Les renforts du Hurleur sont obligatoires et contrôlés une fois toutes les fiches lues.
  - Les groupes d'apparition des biomes et les deux groupes de secours, sortis de `SpawnManager` vers `spawn_flow.json`, sont contrôlés au chargement (`EnemyPools`) : une créature absente est retirée et signalée une fois, au lieu d'une erreur à chaque tirage.
  - `EnemyGrammar.FinalBossId` remplace les `"indicible"` de six fichiers. `AudioManager` perd le préfixe V1 `colosse_`.
  - Le ralentissement de la Tisseuse (0,4 pendant 2 s, en dur) passe au contrat.
  - Les variantes (`elite`, `champion`, `aberration`) restent à Q7.
- **Mêmes valeurs :** relevé des valeurs effectives de chaque créature et de ses capacités, secours compris, au commit de base et après ([preuves](../audits/qualite-2026-10-02/q6c/)). 263 lignes identiques, groupes de secours compris. Les seuls écarts : `_packFamily` ajouté, `_packBonusDamage` retiré (champ jamais appliqué). Une consommation de hasard reste identique : `aimed_shot` tirait son premier délai même quand la fiche le fixait. Aucune fiche ne le fixe, donc rien ne change ; à savoir si l'on en ajoute un.
- **Vérifié :** `tools/test_enemy_abilities.sh`, 163 contrôles. Ils couvrent :
  - le catalogue complet ;
  - 47 fiches fautives refusées avec leur message (dont racine non objet et JSON illisible), plus un son vide explicite accepté ;
  - 7 contrats candidats refusés ;
  - groupes de biome et de secours ;
  - renfort absent en chaîne, doublon, fichier illisible, renforts en boucle (soi, A↔B) ;
  - familles égales à celles du rendu ;
  - ordre des capacités ;
  - meute : un Rôdeur d'une autre famille ne compte pas, un membre d'une autre fiche de la même famille compte.

  Les capacités existantes passent toujours. Build sans avertissement. `tools/validate.sh` sur smoke, enemy_abilities, weapons, small_places, indicible, cartography, movement, movement-integration, music et launchers : **10/10**, sources inchangées.
- **Relecture `godot-reviewer` :** aucun bloquant. Elle a comparé un à un les 59 secours et planchers, et vérifié les chemins de chargement et les bancs par réflexion. Corrigés :
  - fixtures manquantes, dont le contrat contrôlé à part (`EnemyContract.Check`) ;
  - groupes de biome contrôlés au chargement ;
  - champ mort `_packBonusDamage` retiré ;
  - garde enum ↔ table des capacités ;
  - renforts en boucle refusés ;
  - test des familles et de l'ordre des capacités ;
  - nombre de fiches lu sur le disque.

  Restent : le réglage numérique obligatoire d'une capacité est une garde sans cas aujourd'hui (tout réglage a un secours ou est facultatif).
- **Piège évité :** trois bancs (`EnemyAbilityRegression`, `ProjectileCadenceBenchmark`, `RunObservation`) lisaient le cache des capacités par réflexion avec des clés texte : migrés vers les clés typées. Sans cela, ils auraient cassé sans erreur de compilation.
- **Partagé avec les armes :** `DataValueRule` (règle de valeur) et `DataKeySets` (banque audio, manifeste) servent aux deux validateurs ; les messages de bornes écrivent les nombres comme dans le JSON, quelle que soit la langue du système.
- **Question pour Raphaël :** `pack_bonus_damage` (0,15 au Charognard) n'a jamais été appliqué : la meute n'accélère que. À brancher (dégâts +15 % par voisin), ou à retirer ?
- **Hors lot, relevé :** l'Essence par rang (1, 4, 8) est écrite deux fois, dans `EssenceTracker` et `QuestManager` (à Q7).

### N1 — Nettoyage des décisions du soir (§65, §67)

Quatre retraits décidés par Raphaël, sans valeur de jeu changée. Les ornières du tracteur restent telles quelles.

**Découpage, 4 octobre :**
1. **Bonus de dégâts de la meute :** retirer `pack_bonus_damage` de `charognard.json` et du contrat des créatures. La meute garde son bonus de vitesse. Fixture négative : une créature qui déclare encore `pack_bonus_damage` est refusée (stat inconnue du contrat).
2. **Arme en main (plan 17 lot 2C) :** retirer `HeldWeapon` et son appel dans `Player`, l'option de Paramètres › Graphismes et sa clé `held_weapon`, le champ `held_sprite` (24 armes, lecteur, validateur), les 24 sprites `assets/weapons/held/` que rien d'autre ne référence, leur rendu dans `tools/generate_weapon_icons.py` et `tools/sprites/weapons/icons.py`, et la capture `--capture-held`. Fixture négative : une arme qui déclare encore `held_sprite` est refusée (champ inconnu).
3. **Taille du texte (plan 04 lot B3) :** retirer le réglage et `TextSettings`. L'échelle typographique par rôle reste ; disparaissent l'agrandissement, la taille retenue en méta pour le réappliquer, `UITheme.RefreshTextScale` et l'option `--text-step` des captures. La légende de la minicarte garde sa taille de base (8 px). Les mises en page souples du lot B4 (défilements, retours à la ligne) restent.
4. **Barème des lieux et des coffres :** le score ne compte que les éliminations (§40) et aucun code ne lisait plus `score_points`. Retirer le champ de `pois.json` et de `chests.json`, ainsi que les propriétés `ScorePoints` des lecteurs, de `Chest` et de `PointOfInterest`.
5. **Fichiers de suivi :** retirer de `A-VERIFIER.md` les points tranchés (ornières, barème, arme en main, synergies, taille du texte), mettre à jour les plans 02, 04 et 17.

**Vérification :** aucun lecteur restant (recherche dans `scripts/` et dans tout `tools/tests`, réflexion comprise) ; relevé avant/après au même commit de base des valeurs effectives des armes et des créatures (suites armes et capacités ennemies) ; `tools/validate.sh` sur smoke, weapons, enemy_abilities, choice_screen, small_places, ui_art et dev_mode ; capture des Paramètres et du Hub (le réglage a disparu, rien ne déborde). Relecture `godot-reviewer`.

**N1 livré et vérifié, 4 octobre.**
- **Retiré :** `pack_bonus_damage` (contrat et Charognard) ; l'arme en main (`HeldWeapon`, option, clé `held_weapon`, `held_sprite` des 24 armes, 24 sprites et leurs `.import`, `render_held`, `--capture-held`) ; le réglage « Taille du texte » (`TextSettings`, `RefreshTextScale`, `Scaled`, `SetTextSize`, `SetFixedTextSize`, `--text-step`) ; `score_points` des 6 lieux et des 4 coffres, et les propriétés `ScorePoints`. `UITheme.SetTextRole` pose désormais la taille du rôle directement.
- **Valeurs inchangées :** au palier 100 %, l'ancien calcul rendait la taille de base ; la légende de la minicarte reste à 8 px et le score du bilan à sa taille. Les journaux des suites armes et capacités ennemies sont identiques au relevé du commit de base e5488a5c, à deux lignes près : les deux nouvelles fixtures négatives (`held_sprite` refusé comme champ d'arme inconnu, `pack_bonus_damage` refusé comme stat inconnue du contrat).
- **Vérification :** build sans avertissement ; `tools/validate.sh` 7/7 (smoke, weapons, enemy_abilities, choice_screen, small_places, ui_art, dev_mode), sources inchangées pendant les tests ; captures des onglets Graphismes et Effets regardées (les deux lignes ont disparu, rien ne déborde) ; générateur d'icônes relancé, icônes identiques. Relecture `godot-reviewer` : aucun lecteur oublié ni taille changée ; ses deux remarques corrigées (commentaire orphelin dans `WeaponDataLoader`, paramètre `size` inutile dans `write_sheet`).
- **Relevé hors lot :** `tools/generate_weapon_icons.py` ne reproduit pas exactement l'icône des Craies versionnée, déjà au commit de base (retouche à la main probable). Non touché.

### Q7 — Valider et publier les autres catalogues

Constat : F12. Dépend de Q4. Plans associés : ceux de chaque catalogue.

Reprendre une famille à la fois : XP/scaling, progression/objets, ennemis, puis monde/événements. S'appuyer sur la validation stricte des spécialisations : unicité, types, bornes critiques, références croisées et publication du catalogue entier après réussite. Protéger les définitions partagées en lecture seule et garder l'état mutable dans les instances de run. Les règles XP exigent notamment un coût strictement positif.

**Sortie par sous-lot :** fixtures de champs absents, mauvais types, doublons, bornes et références ; aucune publication partielle. Les JSON actuels passent le validateur et les régressions du système. Les conventions de compatibilité et valeurs par défaut sont documentées, avec diagnostic fichier/entrée/champ.

**Découpage d'exécution Q7, 5 octobre** (lot sans décision de design, §68). Q4 étant livré, un catalogue refusé au chargement de la run arrête proprement le chargement : écran d'erreur, message qui nomme le fichier et le champ, retour au camp. Sous-lots par famille, chacun livré à part : **Q7a** courbe d'XP, barème du score et Péril ; **Q7b** objets et raretés d'amélioration ; **Q7c** monde (biomes, lieux, coffres, bonus lâchés) ; **Q7d** événements et Résurgences. Armes (Q6a), créatures (Q6c) et réglages de l'Indicible (Q6b) sont déjà faits.

**Q7a — XP, score, Péril.** Relevé : `XpCurveConfig` lit `progression.json` avec des secours inventés et aucune borne (un `base_xp` nul rend la montée de niveau sans fin) ; `ScoreConfig` lit chaque entrée par `AsDouble` sans contrôle de type ni de créature connue ; `PerilDataLoader` lit `max`, `banish` et `per_point` par accès direct et garde ses valeurs initiales si le fichier manque.
1. **Lecteur commun :** le lecteur strict de l'Indicible (Q6b) devient `JsonConfigReader`, partagé : section, nombre positif, positif ou nul, part, entier borné, clé inconnue refusée (hors commentaires `_…`), clé en double refusée, première erreur retenue avec le chemin du champ. L'Indicible l'utilise sans changer de comportement.
2. **Trois contrats :** courbe d'XP (tous champs obligatoires, coûts strictement positifs, `early_levels` entier), score (`default` obligatoire, points entiers positifs ou nuls, chaque clé est une créature connue), Péril (`max` entier, bannissements entiers, `peril_divisor` ≥ 1, six valeurs par point, positives ou nulles). Rien n'est publié tant que tout le fichier n'est pas valide.
3. **Au chargement de la run :** l'étape « Catalogues » de `GameBootstrap` exige les trois ; un refus devient l'écran d'erreur de Q4.
4. **Vérification :** relevé avant/après au même commit de base des valeurs effectives (coût des niveaux 1 à 250, points de chaque créature, multiplicateurs du Péril de 0 à 10, bannissements) : identiques. Fixtures négatives par règle (champ absent, mauvais type, borne, clé inconnue, doublon, créature inconnue), message précis, aucune publication. Suites loading, choice_screen, progression-model, perk_acquisition ; un catalogue faux injecté arrête le chargement avec son message.

**Q7a livré et vérifié, 5 octobre.** `JsonConfigReader` (Infrastructure) remplace le lecteur privé de l'Indicible, mêmes messages, nombres en culture invariante ; il refuse aussi une clé inconnue et une clé en double. `XpCurveConfig`, `ScoreConfig` et `PerilDataLoader` exposent `TryLoad`/`TryParse`, sans secours inventé et sans publication partielle. L'étape « Catalogues » de `GameBootstrap` exige les trois : un refus devient l'écran d'erreur de Q4, avec le fichier et le champ. `PlayerProgression` et `ScoreManager` tolèrent un refus (la run est arrêtée de toute façon).
- **Vérification :** sonde `CatalogValuesProbe` (coût des niveaux 1 à 250, points des 13 créatures et d'une inconnue, multiplicateurs du Péril de 0 à 10, bannissements) : 276 valeurs identiques avant/après au même commit de base. `tools/test_catalogs.sh` : 24 contrôles (fichiers du dépôt acceptés ; champ absent, coût ou plafond nul, type, entier, clé inconnue, clé en double, table ou défaut absent, créature inconnue, points négatifs ou fractionnaires, bloc absent, diviseur nul, valeur négative ; aucun refus ne publie, même partiellement). `tools/validate.sh` 11/11 (smoke, catalogs, indicible, loading, choice_screen, progression-model, perk_acquisition, perk_effects, objects, movement-integration, dev_mode). Relecture `godot-reviewer` : rien de bloquant ; les trois fichiers du dépôt portent déjà tous les champs.
- **Limite écrite :** le refus au chargement de la run n'est pas rejoué avec un vrai fichier faux, que `tools/validate.sh` interdit d'écrire ; il passe par le chemin d'échec de Q4, déjà prouvé par la panne injectée aux catalogues.

**Q7b — Objets, raretés d'amélioration, offre de niveau.** Relevé : `PassiveSouvenirDataLoader` lit par accès direct, avec des secours (nom = identifiant, 30 niveaux, modificateur multiplicatif) ; il ne vérifie ni le nom des statistiques, ni celui des effets de palier, ni les réglages qu'ils lisent : un nom inconnu serait ignoré en silence par les `switch` d'application, un réglage absent rendrait 0. Un doublon d'identifiant écrase l'objet. `UpgradeRoller` et `LevelUpOfferConfig` lisent sans type ni borne. Le relevé croisé montre que les 36 statistiques et les 32 effets de palier du catalogue ont tous un lecteur dans le code, sauf `cooldown_reduction`, porté par un objet désactivé.
1. **Contrat des objets** (`data/progression/objects_contract.json`) : statistiques admises, avec leurs modificateurs et les réglages qu'elles exigent ; effets de palier et leurs réglages ; statistiques admises seulement sur un objet désactivé. `ObjectDataValidator` (System.Text.Json) contrôle tout le fichier : champs connus et typés, identifiant unique, 1 à 8 effets, statistique et modificateur admis, pas non nul (sauf objet désactivé), réglages exigés présents et seuls ceux connus, chaque `{réglage}` de la description fourni, paliers (niveau de 2 au maximum, effet connu, réglages exacts, texte), icônes présentes. Rien n'est publié tant que tout n'est pas valide.
2. **Raretés et offre :** lecture stricte par `JsonConfigReader` (identifiants uniques, poids et gains strictement positifs, chance de montée dans [0 ; 1], nombre de statistiques entier, crans de zone entiers ; poids d'offre positifs).
3. **Au chargement de la run :** l'étape « Catalogues » exige les trois.
4. **Vérification :** même sonde que Q7a, étendue aux objets (désactivés compris), raretés, crans et offre : valeurs identiques avant/après. Fixtures négatives par règle. Suites catalogs, objects, perk_acquisition, perk_effects, choice_screen, weapons, loading.

**Q7b livré et vérifié, 5 octobre.** `objects_contract.json` (35 statistiques, 32 effets de palier, une statistique inactive) et `ObjectDataValidator` ; `PassiveSouvenirDataLoader.TryLoad` ne publie aucun objet si le catalogue est refusé, et son lecteur ne garde que les secours des champs facultatifs (les contrôles sont au validateur, dont celui du facteur multiplicatif qui s'annulerait avant le niveau maximum). `UpgradeRoller` et `LevelUpOfferConfig` stricts ; `UpgradeRoller.Apply` et `PerilDataLoader.Apply` (renommés : ils publient un texte valide). L'étape « Catalogues » exige objets, raretés et offre.
- **Vérification :** sonde étendue, 408 valeurs (dont 121 lignes d'objets) identiques avant/après au même commit de base. `tools/test_catalogs.sh` : 60 contrôles (36 nouveaux : 20 sur les objets, 8 sur les raretés dont la non-publication, 4 sur l'offre, plus les fichiers du dépôt). Le validateur a d'abord refusé les deux objets désactivés, sans description : règle ajustée (un objet désactivé n'est jamais montré). `tools/validate.sh` 11/11, puis 7/8 après les corrections de relecture : la suite `loading` a planté à la fermeture du moteur (erreur de segmentation au ménage .NET, après un scénario réussi) ; les bancs qui chargent Main quittent désormais par `GameExit.QuitAsync`, comme le jeu ; `loading` vert trois fois de suite, `run_trace` vert. Relecture `godot-reviewer` : un défaut réel corrigé (le contrôle du facteur restait au lecteur, qui sautait l'objet : publication partielle), réglages exigés pour toutes les statistiques d'un objet et non plus la première, `params` d'un autre type refusé.
- **Limite écrite :** si le catalogue des objets est refusé, la Collection du Hub n'affiche aucun objet et seule la console le dit ; la run, elle, s'arrête sur l'écran d'erreur.

**Q7c — Monde**, en quatre livraisons : **Q7c-1** Mémoriaux, Ateliers, Failles, bénédictions et Oublis ; **Q7c-2** coffres et bonus lâchés ; **Q7c-3** petits lieux et repères ; **Q7c-4** génération (paramètres du monde, biomes, fermes, carrières).

**Q7c-1.** Relevé : `LandmarkDataLoader` lit par accès direct avec des secours en initialiseurs de champ (tous inutilisés : le fichier porte chaque clé) ; les raretés minimales (`blessing_min_rarity`, `offer_min_rarity`, `weapon_min_rarity`) ne sont pas contrôlées ; `StatEffectReader` (bénédictions) et `OubliDataLoader` lisent sans type, sans contrôle de la statistique ni de l'effet. Les neuf effets d'Oubli et les dix statistiques des bénédictions ont tous un lecteur dans le code.
1. **Lecteur commun étendu :** texte obligatoire, valeur prise dans une liste, ressource existante, booléen, intervalle croissant, liste non vide.
2. **Contrats :** lieux (tous champs, couronnes de placement dans [0 ; 1], intervalles croissants, prix entiers, raretés minimales connues, lues dans le fichier des raretés sans dépendre de `Progression`, images présentes) ; bénédictions (statistique que le joueur applique lui-même, liste `player_stats` ajoutée au contrat des objets, calquée sur `Player.ApplyPerkModifier` ; modificateur admis ; clé traduite ; valeur non nulle) ; Oublis (effet connu, valeur strictement positive, clés traduites). Rien n'est publié tant que le fichier n'est pas valide ; l'étape « Catalogues » les exige.
3. **Vérification :** sonde étendue (430 valeurs identiques avant/après) ; fixtures négatives ; suites catalogs, loading, small_places, choice_screen, objects, perk_effects, movement-integration, erasure_active, cartography.

**Q7c-1 livré et vérifié, 5 octobre.** `LandmarkDataLoader`, `BlessingDataLoader` (avec `StatEffectReader`, séparé de `StatEffectData`) et `OubliDataLoader` stricts, `Apply` et `TryLoad` ; `DataKeySets.ListIds` lit les raretés ; `player_stats` au contrat des objets, dont la cohérence avec `stats` est contrôlée à la lecture du contrat. Vérification : 430 valeurs identiques avant/après ; `tools/test_catalogs.sh` 88 contrôles (28 nouveaux : 13 sur les lieux dont la non-publication, 8 sur les bénédictions, 6 sur les Oublis, plus les fichiers du dépôt) ; `tools/validate.sh` 10/10. Relecture `godot-reviewer` : rien de bloquant ; `player_stats` correspond exactement aux 19 cas de `Player.ApplyPerkModifier` ; trois retouches appliquées.

**Q7c-2 livré et vérifié, 5 octobre.** Relevé : `ChestDataLoader` lisait ses trois fichiers avec des secours (rareté commune, colonne de 96 px, famille « silk »…) et sans contrôler les raretés, les familles d'effets (une inconnue retombe en silence sur un repli, `PixelPalette.ParseFamily`), les tables de butin ni les statistiques du bonus ; un coffre sans multiplicateur de bonus prenait 1. `FieldBonusDataLoader` prenait comme réglage toute valeur numérique d'un bonus, et `FieldBonusDirector` gardait des replis d'équilibrage en dur.
- **Code :** `ChestDataLoader.Apply` contrôle ensemble coffres, placement et bonus de stat, et ne publie qu'après les trois (raretés de la palette, familles du contrat des créatures, `LootTableLoader.Exists`, statistiques du joueur et modificateurs admis, images, rétrogradation, chaque rareté de coffre a son multiplicateur). `FieldBonusDataLoader.Apply` : effets connus avec exactement leurs réglages, images du manifeste des ramassables, variantes connues, noms traduits, chances dans [0 ; 1] (`JsonConfigReader.Chance`, aussi pour la Faille). `FieldBonusData.Param` sans repli ; les replis de `FieldBonusDirector` sont retirés. `DataKeySets.StringList` et `SectionKeys`.
- **Vérification :** 465 valeurs identiques avant/après ; `tools/test_catalogs.sh` 116 contrôles (28 nouveaux, dont « aucun refus n'a publié ») ; `tools/validate.sh` 10/10, puis catalogues et bonus lâchés verts après les retouches. Relecture `godot-reviewer` : rien de bloquant ; les 16 familles du contrat des créatures sont exactement celles de `ParseFamily` ; un élément de coffre qui n'est pas un objet levait une exception au lieu d'un message (corrigé, fixture ajoutée).

## 5. Lots de correction de la run

Ces lots sont indépendants entre eux après Q0 ; chacun est livré séparément. Ils peuvent passer avant Q5 si un défaut gêne directement la recette. Leur ordre recommandé est ciblage, remapping, seed puis phases.

| Lot | Action | Critère de sortie |
|---|---|---|
| **Q8a — Identité des cibles** (F07, plans 05/17) | Étendre `EnemyLife` aux verrouillages persistants et filtrer les vies actives dans les recherches. | Mort en vol, retour au pool et réutilisation immédiate : le tir n'attaque pas une vie différente par erreur ; régression armes et capture du homing. |
| **Q8b — Remapping** (F10, plan 04) | Sauvegarder les bindings sans perdre les secondaires ; séparer application et écriture des préférences. | Changer seulement une action, redémarrer, conserver WASD/flèches/manette ; reset cohérent ; pas de réécriture lors de la lecture. |
| **Q8c — Seed effective** (F06, plans 02/09/10) | Publier une seed réelle dans le contexte et l'historique ; dériver des RNG distincts pour les systèmes de gameplay et les effets cosmétiques. | Run aléatoire enregistrée avec sa seed ; lancement normal et observateur partagent l'initialisation. Trace de tirages reproductible avec les mêmes entrées et temps simulé ; limites de reproductibilité explicites. |
| **Q8d — Transitions globales** (F08, plans 03 et late game) | Un seul propriétaire des transitions, avec contrats de seuils irréversibles et de crise. | Quatrième Résurgence avec oubli sous 0,68, fin de crise, seuil d'oubli et boss : phase et état restent cohérents ; endgame conservé. Vérifier les consommateurs musique, HUD, spawn et historique. |

**Découpage d'exécution Q8a, 5 octobre** (lot sans décision de design, §68). Relevé : le tir guidé (`Projectile._homingTarget`) et le tir de rafale en attente (`_departureTarget`) gardent un `Node2D` et ne vérifient que la validité de l'objet Godot ; une créature rendue au pool puis réutilisée reste valide, avec une nouvelle vie (`EnemyLife`). Le groupe `enemies` est mis en cache pour l'image : une créature morte peut y figurer encore. Sur 31 recherches dans ce groupe, 9 ne filtrent pas les créatures inactives ou mourantes (recherche du tir guidé, tirs groupés, saut de chaîne, arc de mêlée, écho des Gants, formes des Craies, champ du Chronomètre, feu au sol, voisins de meute et explosion de créature).
1. **Verrou par vie :** `TargetLock` retient la créature et sa vie ; il ne rend la cible que si c'est la même vie, active et pas mourante. Une cible du groupe qui n'est pas une créature (l'Indicible) reste suivie tant que son nœud vit. Le tir guidé et le tir de rafale l'utilisent ; une vie terminée fait chercher une autre cible, comme un tir neuf.
2. **Recherches filtrées :** les 9 recherches ignorent les créatures inactives ou mourantes, comme les 22 autres.
3. **Vérification :** dans le banc des armes, un tir guidé verrouillé sur une créature qui meurt puis est réutilisée ailleurs ne la suit plus et se tourne vers la créature restante ; même contrôle pour le tir de rafale en attente ; contre-épreuve sans le verrou. Suites armes, cône, objets et mouvement ; capture du tir guidé (Boussole).

**Q8a livré et vérifié, 5 octobre.** `TargetLock` (nœud + `EnemyLife`) pour le tir guidé et le tir de rafale en attente ; une vie terminée fait chercher la cible la plus proche, comme un tir neuf ; l'Indicible, qui n'est pas une créature, reste suivi tant que son nœud vit. Les 9 recherches ignorent les créatures inactives ou mourantes. Vérification : 3 contrôles dans le banc des armes (verrou tenu tant que la cible vit ; cible morte puis revenue du pool à 5 000 px abandonnée au profit de la créature restante, pour le tir guidé et pour le tir de rafale) ; contre-épreuve sans la comparaison de vie : les deux derniers échouent (le tir de rafale partait vers la créature revenue, direction (−0,71 ; 0,71)). `tools/validate.sh` 8/8 (smoke, weapons, cone, objects, perk_effects, enemy_abilities, movement, movement-integration). Capture du tir guidé (Boussole, Baguette, cibles qui meurent au premier coup), regardée. Relecture `godot-reviewer` : rien de sérieux ; les créatures du banc sont libérées par `QueueFree` comme dans les autres contrôles du même banc, hors pool. Pas de banc FPS : `TryGet` compare deux entiers, sans allocation.
- **Relevé hors lot :** dans `--capture-weapons`, la première arme de la galerie est capturée pendant le fondu de l'écran de chargement (images presque noires). Déjà le cas avant Q4 (mêmes luminosités au commit d8a342ae). **Corrigé le 5 octobre :** la galerie attend que l'écran de chargement ait disparu. Luminosité moyenne des images : 10 à 58 avant (la deuxième arme sortait encore pendant le fondu), 92 partout après.

**Découpage d'exécution Q8b, 5 octobre** (lot sans décision de design, §68). Relevé (`InputRemapManager`) : les déplacements ont deux touches par défaut (ZQSD/WASD physiques et flèches). La sauvegarde écrit chaque touche d'une action dans la même clé : la dernière, la flèche, écrase la première. Au démarrage, `LoadBindings` passe par `RemapKey`, qui efface toutes les touches de l'action puis n'en remet qu'une, **et réécrit le fichier à chaque action lue**. Changer seulement la touche d'interaction fait donc perdre WASD au redémarrage suivant. `ResetToDefaults` ne remet qu'une touche par action et perd aussi les flèches.
1. **Touche principale et secondaires :** les liaisons par défaut sont relevées au démarrage, avant toute lecture. Remapper remplace la seule touche principale (la première par défaut) ou le seul bouton principal ; les flèches, les axes du stick et les autres boutons restent.
2. **Appliquer sans écrire :** la lecture des préférences applique sans sauvegarder ; seule une action du joueur écrit. Le fichier ne garde que ce que le joueur a changé, avec une version.
3. **Ancien fichier :** sans version, une touche enregistrée qui est une touche secondaire par défaut (la flèche écrite par erreur) est ignorée ; toute autre est un vrai choix du joueur et devient la touche principale. Le fichier n'est pas réécrit à la lecture.
4. **Réinitialisation :** toutes les liaisons par défaut reviennent, secondaires comprises, et le fichier est supprimé.
5. **Vérification :** scène `InputRemapRegression` en plusieurs lancements (un redémarrage réel entre deux), `tools/test_input_remap.sh`, suite `input_remap`. Cas : changer seulement l'interaction, redémarrer, retrouver WASD, flèches, croix et stick ; fichier inchangé par la lecture ; ancien fichier écrit par le défaut ; ancien fichier avec un vrai choix (déjà couvert par le banc de mouvement) ; réinitialisation complète. Contre-épreuve sur l'ancien enregistrement. Suites movement et dev_mode.

**Q8b livré et vérifié, 5 octobre.** `InputRemapManager` relève les liaisons par défaut au démarrage (après les boutons et le stick ajoutés pour la manette, avant toute lecture). Remapper remplace la première touche ou le premier bouton, à sa place ; flèches, stick et autres boutons restent. La lecture applique sans écrire ; la sauvegarde ne garde, en format version 2, que ce qui diffère des défauts, et signale un échec d'écriture. Un ancien fichier (sans version) dont la touche est une secondaire par défaut est lu comme non modifié ; tout autre choix devient la touche principale. La réinitialisation remet toutes les liaisons par défaut et supprime le fichier.
- **Vérification :** `tools/test_input_remap.sh`, 5 lancements dans un profil temporaire avec un vrai redémarrage entre eux, 22 contrôles ; le lanceur vérifie que la lecture laisse le fichier intact (empreinte avant/après). Contre-épreuve avec l'ancien code : l'ancien fichier donne `move_up : Up` seulement, WASD perdu. `tools/validate.sh` 5/5 (smoke, input_remap, movement, dev_mode, ui_art), puis la suite rejouée après les retouches de relecture. Relecture `godot-reviewer` : aucun bug ; retouches de forme appliquées. Son constat « `.cs.uid` du banc absent » ne s'applique pas : `tools/.gdignore` exclut les bancs de l'import.
- **Limite écrite :** un joueur qui, avant ce lot, avait volontairement remappé un déplacement sur sa propre flèche voit ce choix ignoré une fois ; l'ancien fichier ne permet pas de le distinguer du défaut.

**Découpage d'exécution Q8c, 5 octobre** (lot sans décision de design, §68), en deux livraisons. Relevé : avec une seed de Hub vide, `GameManager.RunSeed` vaut 0 ; `WorldSetup` tire une vraie seed sans la publier ; le record garde 0 et le bilan n'affiche rien. Le jeu appelle 118 fois le générateur global (`GD.Rand*`) dans 40 fichiers, cosmétique et gameplay mêlés ; l'observateur appelle `GD.Seed`, le lancement normal non.
- **Q8c-1 — Seed effective :** `GameManager.EffectiveSeed`, publiée par `WorldSetup` dès qu'elle est connue (jamais 0) ; le record et l'historique la gardent ; le bilan l'affiche pour toute run. `RunSeed` reste la demande du Hub (0 = aléatoire), pour que « Rejouer » garde son comportement. Vérification : scénario de chargement à seed aléatoire (seed publiée, égale à celle du monde, reprise par le record), suites loading, dev_mode, saves.
- **Q8c-2 — Flux de tirage :** un service de run, initialisé par la seed effective au même endroit pour le lancement normal et l'observateur, fournit des flux séparés (apparitions, butin, progression, événements, combat) ; les tirages cosmétiques restent sur le générateur global. Vérification : trace des premiers tirages de chaque flux identique sur deux lancements à même seed et même pas de temps, différente avec une autre seed ; limites écrites (entrées du joueur, temps réel).

**Q8c-1 livré et vérifié, 5 octobre.** `GameManager.EffectiveSeed` publiée par `WorldSetup` (jamais 0), lue par le record (donc l'historique) et par le bilan, qui affiche désormais la seed de toute run, aléatoire comprise. `RunSeed` reste la demande du Hub : « Rejouer » ne change pas. Vérification : scénario `random-seed` du banc de chargement (seed publiée et égale à celle du monde, demande du Hub laissée à 0, record identique) ; `tools/validate.sh` 5/5 (smoke, loading, dev_mode, development_tools, saves). Diff de dix lignes de code : pas de relecture par sous-agent. Reste Q8c-2.

**Q8c-2 livré et vérifié, 5 octobre.** `RunRandom` (Core) : flux partagés `Spawn`, `Loot`, `Combat`, `Behavior`, et `SeedFor(nom)` pour les systèmes qui gardent leur générateur (Résurgences, micro-événements, Failles, Mémoriaux, Ateliers, petits lieux, déclencheurs et paliers d'objets, quêtes, coffres d'après-Résurgence, offres de niveau, placement des lieux et des éléments de lore). Seeds dérivées par FNV-1a du nom et SplitMix64. `WorldSetup` fixe la seed dans `_EnterTree`, avant le `_Ready` du joueur, appelle `Begin`, et `End` à la sortie. L'observateur ne fait plus `GD.Seed` : même initialisation que le lancement normal. Distributions conservées (`GD.RandRange` entier → `RandiRange`, inclusif aux deux bornes ; réel → `RandfRange`). Restent sur le générateur global : les tirages cosmétiques (traînées, flottements, jaillissement des orbes, Hub, échos, fond de chargement) et les visuels de l'Indicible, refait en B3.
- **Vérification :** `tools/test_run_trace.sh` (vraie Main, joueur immobile, pas fixe à 60 images/s) : deux lancements à la seed 221092026 donnent les mêmes 10 apparitions (empreinte BBC84B0B25A77BE0), la seed 7 une autre (1F6DA76689948860). Contre-épreuve au commit précédent : deux lancements à même seed divergent (055FCF90… / 8FE61243…). `tools/validate.sh` complet 29/29, puis 13/13 après les corrections de relecture. Relecture `godot-reviewer` : six tirages de jeu oubliés (lieux, Essence de zone, choix pondéré de l'offre, projectiles d'objets, errance des créatures perdues, premiers délais des capacités) migrés ; `End` ajouté ; les bancs du cône et des effets temporels initialisent les flux au lieu de compter sur `GD.Seed` ; le jaillissement des orbes rendu au générateur global (cosmétique).
- **Limites écrites :** la reproductibilité suppose les mêmes entrées et le même pas de temps (`--fixed-fps`) ; une partie jouée à la main ne se rejoue pas. Les comparaisons A/B contre un commit antérieur à celui-ci ne consomment plus les tirages dans le même ordre : comparer des distributions, pas des trajectoires.
- **Défaut relevé, non corrigé (changerait l'équilibrage) :** deux tirages entiers écrits `(min, max + 1)` sont inclusifs aux deux bornes : un groupe d'apparition peut compter `max + 1` créatures (`SpawnManager`, taille des groupes de même espèce), un butin `MaxAmount + 1` (`LootResolver`). À trancher avec le butin (plan 13).

**Découpage d'exécution Q8d, 5 octobre** (lot sans décision de design, §68). Relevé : trois propriétaires écrivent la phase de run (`GameManager.SetRunPhase`) : `CrisisManager` (début et fin de crise, et à chaque image hors crise selon l'Effacement seul), `EndgameManager` (seuil d'Effacement, quatrième crise, boss, endgame) et `GameManager` (début de run, mort). Le seuil de 0,68 est lu deux fois. Défaut F08 confirmé : la quatrième Résurgence marque le late game, mais à sa fin `CrisisManager` repasse en Exploration tant que l'Effacement reste sous 0,68, et le remet à chaque image.
1. **Un propriétaire :** `GameManager` résout seul la phase à partir de trois faits de run : une crise en cours, le late game atteint (irréversible), l'endgame atteint. Règle : mort > endgame > crise > late game > exploration. Les faits sont remis à zéro au début de chaque run.
2. **Les autres rapportent :** `CrisisManager` signale le début et la fin d'une crise (la phase change avant le signal de début, comme aujourd'hui) et ne lit plus l'Effacement. `EndgameManager` signale le late game (Effacement, quatrième crise, boss) et l'endgame. `SetRunPhase` n'est plus public.
3. **Vérification :** scène `RunPhaseRegression` avec les vrais `CrisisManager` et `EndgameManager` : quatrième Résurgence sous 0,68 puis fin de crise (late game gardé, aussi aux images suivantes), crise pendant le late game, seuil d'Effacement, boss puis endgame (une crise garde l'endgame), mort ; contre-épreuve sur l'ancien code. Suites movement-integration, erasure_active, music, indicible, dev_mode ; consommateurs de `RunPhaseChanged` (musique, HUD, apparitions, historique) relus.

**Q8d livré et vérifié, 5 octobre.** `GameManager` résout seul la phase (`ApplyRunPhase`) à partir de `ReportCrisis`, `ReportLateGame` et `ReportEndgame` ; faits remis à zéro au passage à `Run` ; `SetRunPhase` privé. `CrisisManager` ne lit plus l'Effacement (son double du seuil 0,68 est retiré ; `EndgameManager` le lit). La phase change toujours avant `CrisisStarted` et après `CrisisEnded`.
- **Vérification :** `tools/test_run_phase.sh`, vrais `CrisisManager` et `EndgameManager` pilotés image par image, 19 contrôles (quatrième Résurgence sous le seuil : late game gardé à la fin, et 30 images plus tard ; crise en late game ; endgame gardé pendant et après une crise ; mort non écrasée ; nouvelle run remise à zéro). Contre-épreuve : en remettant le seul marquage local de `EndgameManager` (le chemin du constat F08), le banc échoue sur « late game atteint ». `MusicRegression` adaptée aux signalements, verte. `tools/validate.sh` 9/9 (smoke, run_phase, music, movement-integration, erasure_active, indicible, dev_mode, loading, run_trace). Relecture `godot-reviewer` : aucun consommateur de `RunPhaseChanged` changé (ils lisent la nouvelle phase ; la musique la résout en différé) ; une mort n'est plus écrasée par une fin de crise ou la mort du boss. L'Effacement global ne baisse jamais (`ErasureManager`) : rendre le late game irréversible ne change rien en jeu, hors le cas F08.

## 6. Lots d'architecture et de performance

### Q9 — Extraire progressivement le combat de Player

Constat : F14. Dépend de Q5 et Q6a. Plans associés : 05, 17 et 21.

Trois sous-lots, chacun avec son propre compte rendu :

1. **Q9a — Ciblage :** un service de sélection, filtres et buffers maîtrisés. Reprendre les contrats de Q8a ; mesurer les allocations et le temps avant/après avec le même scénario dense avant de choisir un index spatial.
2. **Q9b — Exécution :** un contexte explicite contenant arme source, propriétaire et tirages ; les patrons d'attaque et effets persistants consomment ce contexte. Le timer ne détourne plus l'arme équipée comme variable de travail.
3. **Q9c — Arsenal :** isoler slots, améliorations et ascensions. `Player` orchestre les commandes, la mobilité et la représentation ; l'UI reçoit un état de présentation sans porter les règles. Extraire ensuite la construction d'UI de `QuestManager` dans un sous-lot distinct **Q9d**, lié aux plans 04/06.

**Sortie de chaque sous-lot :** contrats de provenance, dégâts, procs, vol de vie, acquisition et ascensions verts ; captures des parcours concernés. Même build et même contenu de référence, aucun changement d'équilibrage. Toute affirmation de gain utilise des mesures avant/après ; les responsabilités et dépendances comptent davantage qu'une limite arbitraire de lignes par fichier.

### Q10 — Borner la croissance des orbes

Constat : F13. Plans associés : 16 et 22, qui gardent la décision de devenir du butin.

Mesurer d'abord quantités, mémoire et coût du parcours de sommeil sur les mêmes seeds et durées, jusqu'aux runs longues. Appliquer ensuite une règle existante de récupération, durée de vie ou agrégation compatible avec le design. Si aucune règle n'est acquise, préparer la comparaison des solutions dans les plans 16/22 avant de modifier l'XP récupérable.

**Sortie :** progression et XP conservées selon le contrat retenu ; mesures de croissance et de coût avant/après, collecte à distance et retour vers les orbes vérifiés. Cette décision de gameplay reste ouverte ; le plan ne valide pas une suppression automatique du butin.

## 7. Lots de préparation de livraison

| Lot | Action | Critère de sortie |
|---|---|---|
| **Q11 — Contenu et localisation** (F17, plan 04) | Reprendre écran par écran les libellés et les noms/descriptions de contenu, puis le formatage selon la locale. S'appuyer sur les contrats de Q6a pour montrer les valeurs effectives. | Parcours FR/EN Hub, Collection, choix, pause, paramètres et bilan capturés ; clés manquantes détectées ; nom affiché indépendant des IDs. |
| **Q12 — Build et export reproductibles** (F15, préparation Early Access) | Fixer le SDK de travail, versionner les réglages d'export partageables sans secrets, séparer progressivement tests/outils de la livraison et brancher Q0 sur une CI adaptée. | Un checkout propre produit le paquet avec JSON, manifestes, shaders et audio ; lancement Hub → run → bilan ; absence des outils Q1 dans ExportDebug/ExportRelease. Qualifier chaque OS effectivement testé. |
| **Q13 — Traçabilité audio** (F16, plan 15) | Établir la provenance du son de level-up ou le remplacer dans le chantier audio. | Registre complet, références cohérentes, test musique et écoute du nouveau son si remplacement ; aucune conclusion juridique déduite d'un simple test de chargement. |
| **Q14 — Référence technique et restes V1** (F18, plan 18) | Décrire les modules actifs et leurs flux ; retirer les API/groupes V1 sans consommateur après contrôle des migrations. Actualiser les comptes rendus des plans. | Recherche sans appelants avant suppression, fixtures de migration vertes, architecture V2 exploitable, références historiques clairement marquées. Les recettes humaines encore ouvertes restent visibles. |

## 8. Vérifications et progression

Avant chaque lot, noter commit, modifications locales, versions moteur/SDK et état des scénarios concernés. Ne pas réutiliser un profil personnel pour une injection d'échec. Les comptes rendus distinguent preuve par lecture, scénario automatisé, capture inspectée, mesure et recette humaine.

Pour clore un lot :

- `dotnet build` avec zéro erreur et zéro warning.
- Régressions pertinentes, retour moteur et marqueur final vérifiés via Q0 ; fixtures négatives pour les contrats qui doivent refuser des données.
- Smoke si scènes, shaders, `project.godot` ou initialisation touchés ; vérifier Main en complément pour les systèmes de run.
- Capture inspectée si le comportement visible change ; écoute pour l'audio.
- Avant/après avec même banc, seeds et durée si coût ou optimisation ; FPS uniquement sur machine calme.
- Compte rendu au présent plan ou au plan du système, lien vers les preuves, état mis à jour au tableau de bord. `.cs.uid` versionné avec chaque nouveau script. Documenter tout contrôle impossible.

### Couverture de la revue

| Constats | Lots |
|---|---|
| F01 | Q1, Q12 |
| F02 | Q2a, Q2b |
| F03, F11 | Q3 |
| F04, F05 | Q0 |
| F06 | Q8c |
| F07 | Q8a, Q9a |
| F08 | Q8d |
| F09 | Q4 |
| F10 | Q8b |
| F12 | Q5, Q6a–c, Q7 |
| F13 | Q10 |
| F14 | Q5, Q9a–d |
| F15 | Q0, Q12 |
| F16 | Q13 |
| F17 | Q11 |
| F18 | Q14 |
| Retour de Raphaël §54 : noms et règles en dur | Q5, Q6a–c, Q7, Q11 |

### Checklist de réalisation

- [x] Q0 — Lanceurs et bancs fiables.
- [x] Q1 — Outils de modification exclus des runs normales distribuées.
- [x] Q2a — Écriture et récupération des sauvegardes.
- [x] Q2b — Finalisation persistante sans double attribution.
- [x] Q3 — Opérations Steam et contexte weekly.
- [x] Q4 — Chargement observé et récupérable.
- [x] Q5 — Motifs d'attaque typés.
- [x] Q6a — Effets et paramètres des armes explicites.
- [x] Q6b — Réglages du boss dans les données.
- [x] Q6c — Relations et capacités ennemies validées.
- [ ] Q7 — Autres catalogues validés, compte rendu par famille.
- [x] Q8a — Verrouillage sur une vie d'ennemi.
- [x] Q8b — Remapping sans pertes.
- [x] Q8c — Seed effective et tirages contrôlés.
- [x] Q8d — Transitions globales cohérentes.
- [ ] Q9a — Ciblage extrait et mesuré.
- [ ] Q9b — Exécution par contexte d'attaque.
- [ ] Q9c — Arsenal isolé.
- [ ] Q9d — Présentation des quêtes séparée.
- [ ] Q10 — Croissance des orbes traitée selon une règle de butin décidée.
- [ ] Q11 — Contenu et parcours FR/EN cohérents.
- [ ] Q12 — Paquet distribué reproductible et vérifié.
- [ ] Q13 — Provenance du son de level-up résolue.
- [ ] Q14 — Documentation active et restes V1 repris.
- [x] N1 — Nettoyage des décisions du soir (§65, §67).

**Suite (§64) :** mesurer l'Indicible, puis proposer les lots de production du lore (plan 19). Prochain lot qualité à choisir avec Raphaël : Q4 (chargement récupérable) recommandé, puisqu'il précède Q7. Q2a, Q2b, Q3, Q6a, Q6b et Q6c sont livrés le 4 octobre.
