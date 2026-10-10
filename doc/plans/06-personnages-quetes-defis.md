# Plan 06 — Personnages singuliers, quêtes variées et déblocages directs

Version 0.4 · Statut : **quêtes indépendantes du lore et casting diversifié décidés ; liste complète des quêtes proposée au §9 (10 octobre), à valider**.
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

**Casting validé par Raphaël le 23 septembre :** « les 6 personnages sont validés !! très bien ». Il ajoute qu'avoir des personnages un peu atypiques est une bonne idée ([fiches](06-fiches-casting.md)). Le Vagabond est le personnage initial. Planche de silhouettes et sprites 48×64 en huit directions produits par le pipeline de 08 ; l'intégration jouable des trois nouveaux (kits, armes, accès) relève du lot D.

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

L'idée directrice : un défi ouvre un nouveau jeu possible. Le catalogue et les conditions ci-dessus sont propres à Vestiges.

Build, smoke si applicable ; roadmap E et D/G après implémentation vérifiée. Le lore reste mémorable par son contenu et sa mise en scène, pas par son obligation d'accès au combat.

## 8. Personnage initial : le Vagabond — 27 septembre 2026

Décision du 23 septembre (registre, « Personnage initial »), restée sans code jusqu'ici.
- Dans `characters.json`, le Vagabond prend la condition `default` et le Traqueur reprend la sienne (tenir 12 minutes). Un profil neuf ne reçoit donc que le Vagabond, et l'accueil s'ouvre sur lui.
- `MetaSaveManager` n'impose plus le Traqueur à chaque chargement. Un profil sans aucun personnage reçoit ceux de condition `default` ; un profil existant garde tous les siens, Traqueur compris, et reçoit aussi le Vagabond au prochain contrôle des déblocages.
- Le personnage de secours de la run (aucun choix transmis) devient le Vagabond.
- **Vérifié :** accueil en profil neuf capturé (« Le Vagabond », seul personnage) ; `test_dev_mode` vert après adaptation (le Traqueur y sert désormais de personnage verrouillé en profil normal) ; smoke test.
- **Reste ouvert :** le choix de la condition du Traqueur est provisoire, les conditions du §5 n'étant pas validées.

## 9. Une seule famille de quêtes : la liste complète — 10 octobre 2026 (proposition)

Demande de Raphaël ([DECISIONS §90](DECISIONS.md)) : « il faut avoir une seule instance de quêtes : celles pour débloquer des armes, personnages, objets […] variées, de différents niveaux de difficulté, certaines imbriquées les unes dans les autres ». Les quêtes ne s'affichent pas en jeu ; une notification part **dès qu'une quête est accomplie**, sans attendre la fin de la run. Cette section **remplace les §4 et §5** : liste validée comme base le 10 octobre (« ça me va, très bonne base », DECISIONS §90), seuils à mesurer.

### 9.1 Règles

- **Une quête = un déblocage permanent** (personnage, arme ou objet). Plus de quêtes de run (Essence, XP), plus de Souvenirs comme clé, plus de Vestiges en récompense. Chaque pièce verrouillée a exactement une quête.
- **La condition fait essayer le style que la récompense renforce** (Thermomètre : tuer des créatures en feu). Elle est atteignable avec la réserve de départ ; jamais avec la pièce qu'elle débloque.
- **Trois difficultés.** ★ : une ou deux runs, en jouant normalement. ★★ : il faut le chercher (un boss, un style tenu toute une run). ★★★ : fin de run, risque ou maîtrise.
- **Des quêtes en révèlent d'autres.** La colonne « Après » dit quelle quête en révèle une autre. Une quête révélée n'apparaît qu'une fois la précédente accomplie, et ne progresse qu'à partir de là. **Rien n'indique au joueur qu'une quête dépend d'une autre** (DECISIONS §90) : avant d'être révélée, la pièce figure dans la Collection comme verrouillée, sans condition affichée.
- **Run ou cumul.** La plupart se font dans une seule run (on ne grind pas) ; quelques-unes se cumulent d'une run à l'autre.
- **Notification immédiate** : bandeau court en jeu (« Quête accomplie : Thermomètre débloqué »), mis en file s'il en part plusieurs, retenu pendant un écran de choix. La pièce n'entre dans la run en cours qu'à la run suivante.
- **Aucune quête ne s'affiche pendant la run.** Elles se consultent au Hub (Collection : chaque pièce verrouillée avec sa quête et sa progression).
- **Un personnage débloqué apporte son arme de départ** dans la réserve commune.
- **Impossible aujourd'hui, donc exclu :** POI et lore (désactivés), Reliquaire (n'existe pas), « finir une run » (une run ne finit qu'à la mort). Les trois objets « monde » (Presse-papier, Calendrier, Médaillon) attendent le Reliquaire.

### 9.2 Réserve de départ (profil neuf)

| | Disponible au départ | À débloquer |
|---|---|---|
| **Personnages** (6) | Vagabond | Traqueur, Forgeuse, Éveillée, Facteur, Scaphandrière |
| **Armes** (24 + 2 nouvelles) | 8 : Faucille, Lance-billes, Parapluie, Cloueuse, Pelle à neige, Rallonge, Assiettes, Boussole (tous les gabarits : arc, ligne, salve, cercle, guidé) | 18 : 5 avec leur personnage (Arc du gymnase, Parcmètre, Baguette de sourcier, Sacoche de lettres, Fusil-harpon), 13 par quête |
| **Objets** (33) | 15 : Ressort de sommier, Rondelle de cuivre, Mètre pliant, Pince à linge, Bouton de manteau, Bobine de fil, Lacet rouge, Aimant de frigo, Photo de classe, Jeton de fête foraine, Allumette humide, Glaçon dans un mouchoir, Dé à coudre, Semelle usée, Chewing-gum | 18 par quête |

Total : **36 quêtes** (5 personnages, 13 armes, 18 objets).

### 9.3 Personnages (5)

| Quête | Condition | Portée | Diff. | Après | Débloque |
|---|---|---|---|---|---|
| Mener la chasse | Vaincre un Souverain | run | ★ | — | **Traqueur** (+ Arc du gymnase) |
| Redresser la grille | Abattre la Barrière | run | ★★ | — | **Forgeuse** (+ Parcmètre) |
| Entendre les voix | Raviver trois Mémoriaux et survivre à deux Résurgences, dans la même run | run | ★★ | — | **Éveillée** (+ Baguette de sourcier) |
| La tournée | Parcourir 2 500 m et utiliser cinq Repères différents, dans la même run | run | ★★ | — | **Facteur** (+ Sacoche de lettres) |
| Remonter les corps | Tuer 290 créatures en zone effilochée ou effacée, dans une run (les vingt-neuf corps du lore) | run | ★★★ | Entendre les voix | **Scaphandrière** (+ Fusil-harpon) |

### 9.4 Armes (13)

| Quête | Condition | Portée | Diff. | Après | Débloque |
|---|---|---|---|---|---|
| Ratisser | Tuer 500 créatures avec des armes de mêlée | run | ★ | — | Râteau |
| Garder la côte | Tuer 500 créatures avec des armes à distance | run | ★ | — | Lentille de phare |
| Quatre airs | Avoir quatre armes au niveau 10 ou plus en même temps | run | ★ | — | Boîte à musique |
| Développer | Réussir trois micro-événements dans la même run | run | ★ | — | Polaroïd |
| Dessiner la carte | Découvrir 150 zones du brouillard | run | ★ | — | Craies |
| Toutes les portes | Ouvrir 100 coffres | cumul | ★★ | — | Trousseau |
| Compter les Résurgences | Survivre à 15 Résurgences | cumul | ★★ | — | Lampe à pétrole |
| Contre la montre | Atteindre le niveau 30 avant la 12ᵉ minute | run | ★★ | — | Chronomètre |
| Sonner la récré | Tuer 300 créatures ralenties | run | ★★ | Retenir | Cloche d'école |
| Geste précis | Tuer 10 élites d'un coup critique | run | ★★ | Lire de près | Scalpel |
| Nommer l'Indicible | Vaincre l'Indicible | run | ★★★ | Redresser la grille | Gomme |
| Au-delà du climax | Survivre à une Résurgence après la chute de l'Indicible | run | ★★★ | Nommer l'Indicible | Gants de boxe |
| Dernière émission | Porter quatre Oublis à la fois et survivre à une Résurgence ainsi | run | ★★★ | Appeler la meute | Transistor |

### 9.5 Objets (18)

| Quête | Condition | Portée | Diff. | Après | Débloque |
|---|---|---|---|---|---|
| Fièvre | Tuer 100 créatures en feu | run | ★ | — | Thermomètre |
| Retenir | Tuer 150 créatures ralenties | run | ★ | — | Épingle à nourrice |
| Lire de près | Tuer 150 créatures d'un coup critique | run | ★ | — | Lunettes de lecture |
| Encaisser | Subir 1 000 dégâts dans une run | run | ★ | — | Genouillère |
| Reprendre des forces | Récupérer 1 000 PV par des soins | run | ★ | — | Paille tordue |
| Dans la foule | Tuer 200 créatures à moins de 120 px de toi | run | ★ | — | Gilet réfléchissant |
| Grandir | Atteindre le niveau 30 | run | ★ | — | Boîte de pansements |
| Pas bouger | Tuer 300 créatures pendant que tu es immobile | run | ★★ | — | Tabouret de camping |
| Bouquet final | Tuer 25 créatures en moins de 2 secondes | run | ★★ | — | Pétard mouillé |
| De justesse | Tuer 150 créatures en étant sous 35 % de tes PV | run | ★★ | — | Médaille cabossée |
| Bien au chaud | Atteindre la 5ᵉ minute sans être descendu sous 90 % de tes PV | run | ★★ | — | Thermos |
| Tenir le feu | Traverser une Résurgence en perdant moins de 25 % de tes PV max | run | ★★ | — | Écusson de pompier |
| Économiser | Garder 300 Essence en poche | run | ★★ | — | Porte-monnaie usé |
| Appeler la meute | Atteindre Péril 5 | run | ★★ | — | Sifflet d'arbitre |
| Le détail | Tuer 600 créatures d'un coup critique | run | ★★ | Lire de près | Loupe de philatéliste |
| Annoter | Tuer un Souverain d'un coup critique | run | ★★ | Le détail | Stylo à quatre couleurs |
| Clouer | Tuer 400 créatures avec la Cloueuse | run | ★ | — | Reflet brisé |
| Recopier | Faire ascensionner deux armes dans la même run | run | ★★★ | — | Papier carbone |

### 9.6 Quêtes qui en révèlent d'autres

- **Critique :** Lire de près → Le détail → Annoter ; Lire de près → Geste précis.
- **Froid :** Glaçon (départ) → Retenir → Sonner la récré. **Feu :** Allumette (départ) → Fièvre.
- **Boss :** Redresser la grille (Barrière) → Nommer l'Indicible → Au-delà du climax.
- **Risque :** Appeler la meute (Péril 5) → Dernière émission (quatre Oublis).
- **Effacement :** Entendre les voix (Éveillée) → Remonter les corps (Scaphandrière).

Répartition : 14 ★, 17 ★★, 5 ★★★ ; 33 en une run, 2 en cumul.

### 9.7 Ce qu'il faudra compter (pour le lot de code)

Déjà observable : éliminations (par arme, élites, Souverains), coffres, Résurgences, Mémoriaux ravivés, Failles, Péril, Oublis, Barrière et Indicible, endgame, niveau, Essence, dégâts subis, soins, distance, zones découvertes, micro-événements réussis, ascensions, état ralenti/en feu à la mort.
À ajouter : coup critique dans le résultat de dégâts ; Repères utilisés (signal) ; phase d'Effacement au point de mort ; immobilité et distance au joueur à la mort ; PV au moment de l'élimination ; fenêtre d'éliminations sur 2 s ; PV perdus pendant une Résurgence.

Tous les seuils sont des valeurs d'essai, à mesurer sur des runs (`tools/measure_run.sh`) avant de figer : un ★ doit tomber en une ou deux runs normales.

### 9.8 Profils et ancien système

- **Aucune préservation d'ancien profil** (DECISIONS §90) : le seul profil joué est le profil dev de Raphaël, qui a tout. Le chargement d'une ancienne sauvegarde ne plante pas et ne perd ni historique ni Souvenirs ; les nouveaux champs démarrent vides. Les garde-fous des lots B et A sur les droits acquis ne s'appliquent plus.
- **Persistance exigée** : déblocages, quêtes révélées et compteurs cumulés passent par l'écriture atomique existante (Q2a/Q2b) ; une quête accomplie en run est enregistrée tout de suite, pas à la mort, pour survivre à un crash ou à un retour au camp par le menu.
- Les quêtes de run (`run_*`), les récompenses Souvenir/Vestiges/Essence/XP des quêtes et `requires_souvenir` des armes disparaissent ; le Journal des Souvenirs reste, sans effet sur le combat.
- Le mode dev ouvre tout, comme aujourd'hui.

### 9.9 Backlog : quêtes d'exception

Demande de Raphaël (DECISIONS §90) : des quêtes en plus pour des **objets légendaires ou des armes « pétées »**. Elles ne font pas partie du premier lot ; leurs récompenses sont à créer (plan 05). Idées, toutes à valider :

| Exploit | Récompense envisagée |
|---|---|
| Ouvrir tous les coffres d'une carte en une run (39 placés) | Objet légendaire de butin |
| Raviver les neuf Mémoriaux d'une carte en une run | Objet légendaire de survie |
| Utiliser les treize types de Repères en une run | Arme d'exploration |
| Tuer au moins une fois chaque créature du bestiaire, boss compris (cumul) | Arme « pétée » |
| Vaincre l'Indicible à Péril 10 | Arme « pétée » |
| Ascensionner quatre armes dans la même run | Objet légendaire de build |
| Débloquer toutes les armes et tous les objets | Dernière pièce, la plus forte |

### 9.10 Lots

Ordre décidé (DECISIONS §90) : quêtes d'abord, personnages ensuite.

| Lot | Contenu | Vérification |
|---|---|---|
| **Q1 — Déblocages et données** | Fichier des 36 quêtes (récompense typée, portée run/cumul, difficulté, quête qui la révèle) ; réserve de départ en données ; sauvegarde méta : personnages, armes et objets débloqués, quêtes accomplies et révélées, compteurs cumulés. Une seule règle d'accès lue par le loot, les offres de niveau, les Ateliers, la Collection et l'accueil. Retraits : quêtes de run et leur panneau, `requires_souvenir`, conditions de personnages en dur, récompenses Souvenir/Vestiges. Mode dev : tout ouvert. | Profil neuf : seule la réserve sort en 3 runs mesurées ; profil dev inchangé ; sauvegarde rechargée identique ; build, smoke, suites existantes |
| **Q2 — Suivi en run et notification** | Suivi des 36 conditions sur l'EventBus ; compteurs manquants (§9.7) ; quête accomplie enregistrée aussitôt ; bandeau de notification en file. | Scène de régression qui rejoue les événements de chaque condition (seuil −1, seuil, après) ; capture du bandeau en vraie run |
| **Q3 — Hub et bilan** | Collection : pièce verrouillée avec sa quête et sa progression si la quête est révélée, sans condition sinon ; quêtes accomplies au bilan. | Captures du Hub (profil neuf, profil avancé) |
| **Q4 — Seuils mesurés** | `tools/measure_run.sh` sur plusieurs seeds : runs avant chaque ★, ajustement des seuils en données. | Tableau avant/après |
| **P1 à P3 — Éveillée, Facteur, Scaphandrière** | Un personnage complet par lot (fiche du casting) : branchement des sprites, stats, passif, mobilité propre (plan 01 E), arme de départ (Sacoche de lettres et Fusil-harpon à créer), sons, quête. | Captures en run, suites de mouvement et d'armes |
