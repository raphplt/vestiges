# Plan 06 — Personnages singuliers, quêtes variées et déblocages directs

Version 0.3 · Statut : **quêtes indépendantes du lore et casting diversifié décidés ; contenu proposé**.
Priorité : P0 pour le casting · Dépendances : 08 pour la refonte de tous les sprites ; 05/04/03 pour progression et intégration. Le casting validé précède désormais 01 E.
Références : V2 §17/18/25 sous amendement ; [décisions](DECISIONS.md).

## 1. Direction acquise

Les quêtes observent des actions variées et débloquent directement personnages, armes et objets. Elles ne donnent plus un Souvenir comme clé intermédiaire. Les fragments narratifs gardent leur intérêt propre : histoires, constellations et évolution visuelle du Hub, sans progression de combat obligatoire.

Les personnages peuvent être décalés. Leur étrangeté vient de leur ancien métier, obsession ou souvenir : silhouette, mobilité, arme et mécanique racontent la même chose. L'humour visuel n'impose ni parodie permanente ni rupture avec le monde qui s'oublie.

## 2. Phase 0 — Existant et points sensibles

Sources : [characters.json](../../data/characters/characters.json), [quests.json](../../data/quests/quests.json), [QuestManager](../../scripts/Progression/QuestManager.cs), [MetaSaveManager](../../scripts/Infrastructure/MetaSaveManager.cs), [RunHistoryManager](../../scripts/Infrastructure/RunHistoryManager.cs), [SouvenirManager](../../scripts/Meta/SouvenirManager.cs).

Trois personnages, sept quêtes de run et cinq permanentes. Les récompenses de quatre quêtes sont des Souvenirs filtrant effectivement quatre armes. Méta : pas de listes d'armes/objets débloqués, ni récompenses weapon_unlock/item_unlock. Le maximum de crises est calculable via GetMaxCrises dans l'historique limité à 50 runs ; un record pérenne doit être ajouté pour les objectifs qui le demandent.

APIs existantes : QuestManager.GetProgressionSnapshots/ResolvePendingProgressionQuests, MetaSaveManager.UnlockCharacter/CompleteQuest/UpdateStats, SouvenirManager.DiscoverSouvenir. Étendre ces contrats après lecture ; ne pas inventer un service d'accès déjà disponible.

**Migrations à traiter :** Load de MetaSaveManager utilise actuellement la migration legacy pour les versions inférieures ; elle ne constitue pas une migration V2 complète. RunHistoryManager peut archiver/vider un historique jugé ancien. Une simple hausse de version perdrait des informations visibles : écrire des migrations spécifiques et testées.

## 3. Casting proposé

**Correction de priorité du 22 septembre :** Raphaël demande une refonte des personnages et une liste d’au moins cinq à six personnages validés avec les bons sprites, tous à refaire, avant les mobilités spécifiques. Les six concepts ci-dessous constituent une matière à retravailler, pas un casting approuvé. Aucun personnage existant ni son sprite ne constitue une identité définitive à préserver par défaut.

La règle de personnage initial n'a pas encore été arbitrée explicitement : V2 prévoit Vagabond, code actuel Traqueur. Recommandation maintenue : Vagabond pour les nouveaux profils, sans retirer les personnages déjà obtenus. Les conditions ci-dessous sont des propositions de quêtes, sans Souvenir requis.

| Personnage | Singularité cohérente | Mobilité candidate | Accès proposé |
|---|---|---|---|
| Vagabond | Répare sa tenue avec les traces de ses voyages ; endurance mobile | Dash court réactif | Initial selon arbitrage |
| Traqueur | Poursuit des empreintes dont l'auteur s'est effacé ; précision fragile | Pas latéral vif, même action de mobilité | 200 éliminations dans une run |
| Forgeuse | Transporte un marteau trop lourd, insiste pour « redresser » le monde | Bond court lourd, impact à l'atterrissage à prototyper | 15 minutes actives dans une run |
| Éveillée | Entend plusieurs versions d'un même lieu ; manipulation d'Essence | Déplacement bref avec silhouette rémanente, sans traversée de mur | Survivre à deux Résurgences et utiliser deux Autels sur une run |
| Le Facteur sans destination | Sac débordant de lettres à des adresses disparues ; obstination tendre | Glissade sur patins bricolés, virage moins vif | Ouvrir plusieurs caches de deux biomes sur une run |
| La Scaphandrière sans mer | Casque et bottes de plongée dans un monde où sa mer manque | Saut flottant court, contrôle aérien limité | Vaincre un boss de famille et terminer un défi de terrain |

Cible de travail : six identités entièrement revues, pour satisfaire la demande d’au moins cinq à six personnages. Valider le catalogue ensemble (rôles, silhouettes et complémentarité), puis refaire les sprites de chaque identité. Les noms/concepts du tableau restent à valider ; les trois personnages existants ne sont pas considérés comme déjà aboutis. Colosse et Ombre de V2 restent des alternatives à arbitrer, pas des ajouts automatiques au-dessus des six.

**Fiches complètes proposées le 23 septembre :** [06-fiches-casting.md](06-fiches-casting.md). Elles attendent la validation de Raphaël avant la planche de silhouettes et les sprites de 08.

Fiche obligatoire par personnage : silhouette à taille de jeu, motif sonore, phrase de personnalité, arme initiale, passif chiffré, mobilité, contrepartie, deux synergies et une faiblesse. Au moins une différence mécanique observable au-delà des statistiques. Les mouvements spéciaux se branchent sur le module de 01 avec contrôles communs.

## 4. Quêtes : variété et récompense directe

| Famille | Exemples proposés | Ce qu'elle fait découvrir |
|---|---|---|
| Combat | Éliminations d'une famille ; combattre avec une arme ; interrompre un soutien | Priorité et maîtrise d'arme |
| Build | Cumuler dix exemplaires d'un objet ; réussir une fusion ; combiner deux effets | Expérimentation et piles |
| Mobilité | Éviter des charges avec une mobilité ; traverser une rencontre sans arrêt prolongé | Usage intentionnel du mouvement |
| Exploration | Deux biomes ; détour vers un Autel ; récupérer un coffre en zone fragile | Choix de trajet |
| Maîtrise | Résurgence sans dégât ; boss avec une seule arme | Exploit lisible |
| Défi | Run avec règle explicite, validée au départ | Variante de jeu |
| Jalons | Première crise ; premier boss ; entrée en endgame | Progression naturelle |

Les quêtes permanentes progressent automatiquement, sans activation obligatoire. Une intention peut être épinglée. Les quêtes de run restent au nombre de trois dans le premier essai, choisies dans des objectifs accessibles et différents. Pas d'injonction à lire le lore pour obtenir du matériel.

Chaque fiche précise : événement réel, périmètre run/crise/cumul, attribution des éliminations/procs, seuil, reset, disponibilité, reward_type/reward_id, état déjà acquis et texte court. Les nouveaux objectifs de mobilité attendent 01 ; ceux de boss attendent 07. Une quête ne peut exiger un objet qu'elle seule débloque.

## 5. Premier parcours concret proposé

| Quête / reprise | Condition candidate | Récompense directe | État |
|---|---|---|---|
| Première trace | Ouvrir deux coffres dans une run | Essence/XP | Quête de run existante |
| Lire le terrain | Explorer trois POI | Essence/XP | Existant, densité à mesurer |
| La flamme se souvient | 12 minutes actives | Bâton d'Essence | Même ID de quête, nouvelle récompense typée |
| Fendre le vide | 200 éliminations dans une run | Traqueur selon casting + récompense monnaie à harmoniser | Condition existante |
| Masse vivante | 15 minutes actives | Forgeuse selon casting | Nouvelle fiche de déblocage |
| Double passage | Deux Résurgences et deux Autels dans une run | Éveillée | Proposition indépendante du lore |
| Compter les Résurgences | Cumul de crises, seuil à tester (25 actuel, essai 8) | Lanterne Mémorielle | Migration d'accès existant |
| Nommer l'Indicible | Vaincre le boss | Tranchant du Vide | Même ID, accès direct |
| Au-delà du climax | Entrer en endgame | Gantelets d'Écho | Même ID, accès direct |
| À la lisière | Éliminations en zone effilochée | Photographie Fendue | Nouvel objet de 05 |
| Faire des réserves | Atteindre dix exemplaires d'un objet initial | Nouvel objet orienté cumul | Nouveau, rythme à tester |
| Tenir sa ligne | Résurgence avec une seule arme équipée | Objet de maîtrise | Objectif puis défi spécifique |

Ajouter trois défis pilotes après la chaîne de déblocage fiable : une seule arme, mobilité à recharge modifiée, départ avec objet maudit. Définir pour chacun offres autorisées, gains, abandon et classement séparé. Ne pas affirmer qu'un mutateur existe : le système courant est incomplet.

Les chiffres sont des hypothèses. Mesurer le nombre de runs avant récompense, équilibrer l'effort et éviter l'accumulation de quêtes qui ne changent que le nombre de kills.

## 6. Lots d'action

### Lot A — Graphe des accès, conditions et casting

1. Cartographier accès actuels et futurs depuis un profil vierge ; conserver les IDs stables.
2. Séparer quêtes/défis de Journal/Souvenirs dans données et présentation.
3. Présenter six fiches complètes et une planche commune de silhouettes ; choisir le personnage initial et valider le casting d’au moins cinq à six personnages avec Raphaël. Puis refaire et valider tous leurs sprites avec 08 avant 01 E.
4. Définir chaque condition/récompense et les compteurs réellement manquants ; reprendre EventBus et RunRecord.
5. Vérifier sources de loot et absence de boucles de déblocage avec 05.

**Vérification :** armes/objets atteignables sans aucun Souvenir ; graphe réalisable ; tous les IDs et prérequis résolus.
**Garde-fou :** pas de verrouillage de contenu accessible auparavant à un profil existant.

### Lot B — Droits acquis et sauvegarde atomique

1. Ajouter disponibilités méta d'armes/objets et récompenses typées, indépendantes des possessions de run.
2. Migrer chaque ancien Souvenir-clé vers son droit : memory_flame→essence_staff, lantern_memory→memory_lantern, void_knowledge→void_edge, echo_fragment→echo_gauntlets.
3. Pour les anciens profils, préserver également les armes obtenables sans clé auparavant, même si elles reçoivent une quête dans le nouveau modèle ; ne pas leur appliquer rétroactivement les restrictions d'un profil neuf.
4. Si une quête était déjà terminée, conserver cet acquis ; si un droit est déjà possédé, la notification ne redonne pas une récompense différente implicitement.
5. Conserver Souvenirs, monnaie, personnages, quêtes et historique par migration V2 spécifique. Ne pas rejouer l'import legacy V1 ; écrire fixture ancienne/nouvelle et stratégie de récupération.
6. Traiter attribution et complétion de façon idempotente ; un crash/rechargement ne double pas monnaie ou notifications. Couper les nouveaux accès gameplay depuis SouvenirManager après migration, sans retirer le journal.

**Vérification :** profil vierge sans lore, profil V2 avancé, quête terminée sans clé, clé sans quête, crash avant/après attribution ; historique intact.
**Garde-fou :** ne pas marquer des quêtes nouvelles terminées arbitrairement pour simuler la préservation des droits.

### Lot C — Progression visible et Collection

1. Reprendre les snapshots et ajouter les compteurs nécessaires aux conditions approuvées.
2. Tirer des objectifs éligibles et variés ; masquer les variantes dont les mécaniques ne sont pas livrées.
3. Brancher Collection, menu quêtes, suivi HUD compact et bilan 02 ; une carte montre une récompense concrète.
4. Grouper les complétions ; distinguer avancement, objectif terminé et contenu disponible.
5. Mesurer sur plusieurs runs le temps avant une récompense désirable.

**Vérification :** mêmes droits affichés par Collection et appliqués par loot ; une phrase suffit à comprendre l'objectif ; aucune récompense requérant de lire les Souvenirs.
**Garde-fou :** plus de quêtes ne signifie pas plus de remplissage textuel ou de grind.

### Lot D — Casting élargi et défis

1. Une fois le casting et ses nouveaux sprites validés, comparer les mobilités de 01 avec les passifs retenus. Les mobilités suivent les personnages ; elles ne déterminent pas leur casting à l’avance.
2. Intégrer les personnages du catalogue approuvé, un personnage complet à la fois : mécanique, accès, animation, arme, son et menu. L’Éveillée, le Facteur et la Scaphandrière restent des candidats tant que non validés.
3. Vérifier début plus menaçant pour chaque personnage ; les nouveaux déplacements ne dispensent pas de jouer.
4. Implémenter un défi pilote puis les suivants, avec offres, score et sauvegarde cohérents.
5. Ajouter boss de famille et quêtes associées lorsque 07 les rend effectivement disponibles.

**Vérification :** silhouettes et mouvements distincts, contraintes apprises rapidement, récompense obtenue de bout en bout ; aucune migration ni UI factice à la place d'un vrai accès.
**Garde-fou :** personnages décalés cohérents avec l'Effacement ; pas de craft Forgeuse ni pouvoir de nuit réintroduit.

## 7. Recette finale

Tester seuil avant/au/après, simultanéité, cause des kills, pause, mort, reprise, migration et disponibilité de tous les pools. Comparer début/fin d'une série de runs pour juger variété et satisfaction, pas seulement nombre d'objectifs cochés.

Référence générale : [quêtes Megabonk, wiki communautaire](https://megabonk.wiki/wiki/Quests). L'inspiration est le défi qui ouvre un nouveau jeu possible ; le catalogue et les conditions ci-dessus sont des propositions Vestiges.

Build, smoke si applicable ; roadmap E et D/G après implémentation vérifiée. Le lore reste mémorable par son contenu et sa mise en scène, pas par son obligation d'accès au combat.
