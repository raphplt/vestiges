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

## 5. Lots de correction de la run

Ces lots sont indépendants entre eux après Q0 ; chacun est livré séparément. Ils peuvent passer avant Q5 si un défaut gêne directement la recette. Leur ordre recommandé est ciblage, remapping, seed puis phases.

| Lot | Action | Critère de sortie |
|---|---|---|
| **Q8a — Identité des cibles** (F07, plans 05/17) | Étendre `EnemyLife` aux verrouillages persistants et filtrer les vies actives dans les recherches. | Mort en vol, retour au pool et réutilisation immédiate : le tir n'attaque pas une vie différente par erreur ; régression armes et capture du homing. |
| **Q8b — Remapping** (F10, plan 04) | Sauvegarder les bindings sans perdre les secondaires ; séparer application et écriture des préférences. | Changer seulement une action, redémarrer, conserver WASD/flèches/manette ; reset cohérent ; pas de réécriture lors de la lecture. |
| **Q8c — Seed effective** (F06, plans 02/09/10) | Publier une seed réelle dans le contexte et l'historique ; dériver des RNG distincts pour les systèmes de gameplay et les effets cosmétiques. | Run aléatoire enregistrée avec sa seed ; lancement normal et observateur partagent l'initialisation. Trace de tirages reproductible avec les mêmes entrées et temps simulé ; limites de reproductibilité explicites. |
| **Q8d — Transitions globales** (F08, plans 03 et late game) | Un seul propriétaire des transitions, avec contrats de seuils irréversibles et de crise. | Quatrième Résurgence avec oubli sous 0,68, fin de crise, seuil d'oubli et boss : phase et état restent cohérents ; endgame conservé. Vérifier les consommateurs musique, HUD, spawn et historique. |

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
- [ ] Q4 — Chargement observé et récupérable.
- [x] Q5 — Motifs d'attaque typés.
- [x] Q6a — Effets et paramètres des armes explicites.
- [x] Q6b — Réglages du boss dans les données.
- [x] Q6c — Relations et capacités ennemies validées.
- [ ] Q7 — Autres catalogues validés, compte rendu par famille.
- [ ] Q8a — Verrouillage sur une vie d'ennemi.
- [ ] Q8b — Remapping sans pertes.
- [ ] Q8c — Seed effective et tirages contrôlés.
- [ ] Q8d — Transitions globales cohérentes.
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
