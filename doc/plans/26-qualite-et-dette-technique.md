# Plan 26 — Qualité du code et remboursement de la dette technique

2 octobre 2026 · Plan préparé à la demande de Raphaël ([DECISIONS §54](DECISIONS.md)). **Q0 et Q1 livrés et vérifiés (§55, §56, §58) ; Q5 engagé (§58), autres lots non commencés.** Référence de diagnostic : [revue du 2 octobre](../audits/qualite-2026-10-02/README.md), ses manifestes et ses journaux.

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

### Q2b — Finaliser une run une seule fois

Constat : F02. Dépend de Q2a. Plan associé : 02, score et bilan.

Donner une identité à la finalisation, calculer ses récompenses une fois et mémoriser son état validé dans la persistance. Séparer calcul, engagement des acquis et affichage du bilan. Éviter les sauvegardes méta intermédiaires au milieu d'une même attribution. Définir la reprise entre méta et historique : un historique manquant se répare sans réattribuer les Vestiges, et un échec reste visible.

**Sortie :** double appel et reprise après interruption ne doublent ni monnaie, ni statistiques, ni déblocages ; une fin de run ne s'annonce pas sauvegardée après un échec. Vérifier l'interruption entre chaque étape persistante, sans prétendre qu'un remplacement atomique d'un seul fichier rend plusieurs fichiers transactionnels.

### Q3 — Coordonner les opérations Steam

Constats : F03, F11. Plan associé : 09.

Associer chaque recherche, upload et download à son contexte ; sérialiser les opérations ou maintenir un état indépendant par requête. Réserver le weekly à un contexte explicite de défi, s'il existe ; suspendre cet upload tant que son parcours n'est pas livré. Préserver les clés externes Steam existantes.

**Sortie :** quatre destinations suivies correctement à cache froid/chaud, chevauchement d'un download, erreurs et retries maîtrisés. Vérification avec un substitut du service, puis une session Steam sur une plateforme compatible ; l'absence de cette session reste signalée.

## 4. Lots de propreté et de données

### Q4 — Rendre le chargement récupérable

Constat : F09. Plan associé : 10, chargement.

Observer la tâche de chargement, formaliser réussite/échec/annulation, protéger les reprises après `await` et libérer les ressources temporaires. En cas d'erreur de catalogue ou génération, sortir de la pause et proposer un retour au Hub avec un diagnostic utile. Ce lot précède le durcissement des lecteurs, qui rendra explicites davantage d'erreurs.

**Sortie :** fichier invalide, exception injectée et fermeture pendant génération ne laissent ni arbre bloqué en pause, ni tâche utilisant une scène détruite. Chargement normal et Hub passent ; capture du parcours d'erreur si l'UI change.

### Q5 — Typer la grammaire des attaques

Constats : retour §54, F12, F14. Plans associés : 05, 17 et 21.

Commencer par le motif d'attaque et la catégorie mêlée/distance : types fermés, conversion dans les lecteurs et ascensions, puis migration cohérente de `WeaponInstance`, `Player`, `WeaponProperties`, `WeaponTraits`, objets et UI. Conserver les clés JSON existantes et leur compatibilité. Supprimer les normalisations et comparaisons répétées dans le domaine. Traiter ensuite les effets à l'impact et spéciaux dans Q6a, plutôt que migrer toutes les chaînes du dépôt dans le même lot.

**Sortie :** aucune sélection du motif d'attaque par string dans les consommateurs migrés ; motif inconnu rejeté précisément. Toutes les armes et ascensions se chargent. Les dégâts, cibles, cadences, nombres et portée restent identiques aux scénarios de référence ; armes, objets, spécialisations et choix passent. Captures pour orbite, chaîne, homing, cône et mêlée. Mesurer les chemins fréquents si leur coût est modifié.

### Q6a — Expliciter les contrats et paramètres des effets d'armes

Constats : retour §54, F12. Dépend de Q5. Plans associés : 05, 17 et 21.

Définir les mécanismes d'effets et leurs paramètres utiles avec types, unités et bornes ; résoudre les dictionnaires lors du chargement. Rendre explicites les réglages de secours actuels dans les données ou profils de données partagés. Valider les stats de base/croissance et leurs clés avec le catalogue de statistiques existant. Contrôler les références audio, projectile et VFX ; les algorithmes de dessin restent dans leurs modules.

**Sortie :** typo de mécanisme/stat, paramètre requis absent, valeur non finie ou référence absente produit un diagnostic précis avant la run. Les réglages migrés reproduisent exactement les valeurs effectives antérieures, ascensions comprises. Une arme supplémentaire utilisant un mécanisme existant fonctionne sans ajout de branche par son ID dans `Player`.

### Q6b — Sortir les réglages du boss du code

Constat : retour §54. Plans associés : 07 et late game V2.

Migrer les cadences, dégâts, dimensions de zones dangereuses, seuils de phase et récompense de l'Indicible dans sa configuration. Distinguer les dimensions qui changent la hitbox des détails purement décoratifs. Conserver les algorithmes et la mise en scène dans le module du boss.

**Sortie :** la configuration initiale donne exactement les valeurs actuelles ; réglages invalides rejetés ; séquences, dégâts et récompense vérifiés dans un scénario de boss, puis capture en run. Le lot ne redessine pas le boss ni ne change ses phases.

### Q6c — Déclarer les relations de contenu et les capacités ennemies

Constats : retour §54, F12. Plan associé : 07.

Rendre explicite l'appartenance à une meute dans les données ; conserver l'ensemble actuel des Charognards. Résoudre les capacités depuis un registre de mécanismes validé, avec erreur pour une clé inconnue. Relever les autres règles qui dépendent d'un ID concret et décider pour chacune si elle représente une relation de contenu ou une référence légitime à une entité unique.

**Sortie :** la meute ne dépend plus d'un nom recopié dans son calcul ; une créature configurée avec une capacité existante n'exige pas une nouvelle branche par son ID. Les capacités et le bonus de meute actuels passent leur régression. Les références uniques conservées sont localisées et validées.

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
- [ ] Q2a — Écriture et récupération des sauvegardes.
- [ ] Q2b — Finalisation persistante sans double attribution.
- [ ] Q3 — Opérations Steam et contexte weekly.
- [ ] Q4 — Chargement observé et récupérable.
- [ ] Q5 — Motifs d'attaque typés.
- [ ] Q6a — Effets et paramètres des armes explicites.
- [ ] Q6b — Réglages du boss dans les données.
- [ ] Q6c — Relations et capacités ennemies validées.
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

**Lot en cours : Q5**, choisi par Raphaël le 3 octobre (§58) avant Q2–Q4 : le nettoyage demandé des noms et règles de combat commence par les motifs d'attaque.
