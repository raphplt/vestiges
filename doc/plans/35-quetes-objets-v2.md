# Plan 35 — Quêtes des quinze objets V2

12 octobre 2026. Demande de Raphaël après validation de l’implémentation : « une fois fait tu préparera les quêtes ». **Activation demandée ensuite : les quinze quêtes sont validées, avec 50 coffres cumulés pour le Pied-de-biche. Lot activé et vérifié.** Suite du [catalogue des 56 objets](35-catalogue-objets-v2.md). Les 49 quêtes déjà livrées et leurs récompenses restent intactes.

## 1. Périmètre et état réel

Les 34 nouveaux objets sont implémentés : 19 sont disponibles au profil neuf, 15 sont liés à leur nouvelle quête. Le profil neuf dispose de 27 objets du sac, dont trois épiques et deux légendaires ; les 14 anciens objets à quête conservent leur accès. Les 56 sont accessibles en mode dev.

Le verrou `initial_unlock: false` réserve les quinze nouveaux Q. Une quête explicitement liée à un objet a priorité : son accomplissement ouvre cet objet sans modifier le booléen. La Collection affiche la quête requise et son lien ; les quinze objectifs sont suivis en run et présentés dans la page Quêtes.

**Source unique :** [catalogue des 64 fiches de quêtes](../../data/quests/quests.json) : **57 quêtes actives**, dont les quinze nouvelles, et **7 Réminiscences historiques dormantes** dont l’effet attend son implémentation. Le brouillon JSON a été supprimé après fusion. Les seuils sont validés par Raphaël ; leur équilibrage reste à juger en partie. Tous les objectifs utilisent des faits déjà suivis : aucun nouveau compteur, aucune télémétrie de dash ou de gratuité nécessaire.

## 2. Quêtes validées

Une quête par objet. « Cumul » additionne plusieurs parties après activation ; « Run » exige toutes les conditions dans la même partie, sans ordre imposé. Les conditions de niveau concernent les armes portées simultanément. Les cibles numériques et IDs exacts figurent aussi dans le JSON.

| Objet débloqué | Quête (`id`) | Objectif exact | Portée · difficulté |
|---|---|---|---|
| Capsule porte-bonheur · Commun | **Petites fortunes** (`item_small_fortunes`) | Ouvrir **5 coffres**. | Cumul · 1 |
| Tampon élastique · Commun | **Un peu de place** (`item_make_room`) | **80 éliminations à moins de 120 px** du joueur. | Cumul · 1 |
| Bocal de conserve (ex-Ficelle cirée) · Inhabituel | **Garder le fil** (`item_hold_the_thread`) | Éliminer **100 créatures ralenties**. | Cumul · 1 |
| Roulement lisse · Inhabituel | **Sur le même air** (`item_same_tune`) | **150 éliminations à la Boîte à musique**. | Cumul · 2 |
| Cordon spiralé · Inhabituel | **Garder le contact** (`item_keep_in_touch`) | Porter **2 armes niveau 6** et traverser **1 Résurgence**. | Run · 2 |
| Fer à cheval miniature · Inhabituel | **Retrouver le nord** (`item_find_north`) | **120 éliminations à la Boussole**. | Cumul · 1 |
| Ticket de vestiaire · Inhabituel | **Objets trouvés** (`item_lost_and_found`) | Découvrir **2 500 cases** et ouvrir **3 coffres**. | Run · 1 |
| Peigne cassé · Rare | **Droit devant** (`item_straight_through`) | **180 éliminations à la Cloueuse**. | Run · 2 |
| Ampoule rouge · Rare | **Reprendre couleur** (`item_returning_colour`) | Récupérer effectivement **180 PV**. | Cumul · 2 |
| Bille irisée · Rare | **Éclats nets** (`item_clear_splinters`) | **80 éliminations par critique**. | Cumul · 2 |
| Domino ébréché · Rare | **De proche en proche** (`item_chain_reaction`) | **200 éliminations à distance** et une série de **12 éliminations en 2 s**. | Run · 2 |
| Pied-de-biche · Rare | **Forcer la serrure** (`item_open_every_lock`) | Ouvrir **50 coffres**. | Cumul · 2 |
| Registre biffé · Épique | **Signatures croisées** (`item_crossed_signatures`) | **120 éliminations en mêlée**, **120 à distance**, et **2 armes niveau 8**. | Run · 3 |
| Pelote rouge · Épique | **Rester ensemble** (`item_stay_together`) | **2 armes niveau 12** et une série de **15 éliminations en 3 s**. | Run · 3 |
| Portrait effacé · Légendaire | **Une trace de moins** (`item_one_less_trace`) | Parcourir **4 000 m**, atteindre **Péril 3**, traverser **2 Résurgences en perdant moins de 25 % des PV max à chacune**. | Run · 3 |

La difficulté 1/2/3 utilise l’échelle existante des quêtes ; elle ne change pas la rareté de la récompense. Les quêtes Run à plusieurs critères acceptent n’importe quel ordre. Pour Signatures croisées, « atteindre deux armes niveau 8 » n’exige donc pas que cela se produise après les éliminations.

## 3. Dépendances, sens et absence de boucle

- **Entrée commune :** les cinq premiers coffres ouvrent la Capsule ; cinquante ouvrent le Pied-de-biche. C’est une progression distincte des anciennes récompenses à 60 coffres cumulés (Trousseau) et 25 coffres dans une run (Trèfle). Le Pied-de-biche n’est pas nécessaire pour payer les coffres qui le débloquent. Ces seuils dépendent de l’économie payante et restent prioritaires à mesurer ; aucun nombre d’heures garanti.
- **Ficelle :** le Glaçon, le Chewing-gum et la Montre sont déjà disponibles au profil neuf. La Ficelle n’est donc pas requise pour ralentir. Le cumul à 100 fournit une étape avant les anciennes cibles de 150/300 éliminations ralenties dans une run ; il ne remplace pas ces quêtes.
- **Roulement :** dépendance réelle à la **Boîte à musique**, débloquée par **Quatre airs** (`four_tunes`, quatre armes de niveau 3). Cela suppose l’extension des emplacements d’armes du Campement. Cette dépendance est volontaire pour un objet exclusivement orbital : ne pas annoncer la quête comme réalisable au tout premier départ. Le Roulement n’aide ni à acheter les emplacements ni à débloquer l’arme. Il n’y a pas de champ de prérequis à inventer dans le JSON : le libellé nomme l’arme et la fiche de Collection renvoie à son accès existant.
- **Fer à cheval / Peigne :** Boussole et Cloueuse sont dans le tirage initial. Leur acquisition reste aléatoire, mais aucune quête de ces objets n’exige leur propre bonus. La Boussole est autoguidée : ne pas la confondre avec une arme orbitale. Droit devant précède la quête existante Clouer à 250 éliminations à la Cloueuse, sans en changer la récompense.
- **Ampoule :** régénération de base, Pastilles, Dé et soins du monde peuvent suffire. Le suivi compte les PV effectivement rendus, pas le soin théorique ni le sursoin. Aucun dégât auto-infligé exigé, aucun besoin de posséder déjà du vol de vie.
- **Bille :** le Verre fêlé est accessible au profil neuf. Les 80 éliminations critiques cumulées constituent une entrée persistante avant Lire de près (150 dans une run) et Le détail (600 dans une run). Ne pas ajouter de critique de base pour faciliter la quête.
- **Cordon / Domino :** aucun rebond préalable requis ; deux armes de départ et des éliminations ordinaires suffisent. Le Cordon peut donc être débloqué avant une arme à chaîne. Sa fiche avertit si les armes équipées ne sont pas compatibles ; on ne crée pas une dépendance cachée au Trousseau ou à la Sacoche.
- **Registre / Pelote :** deux emplacements suffisent, sans imposer les quatre emplacements du Campement. Les quêtes favorisent un build mixte et la montée de deux armes. Le suivi disponible sait compter mêlée/distance et séries d’éliminations ; il ne sait pas reconnaître « deux armes touchent la même cible », qui n’est donc pas promis par le texte.
- **Portrait :** les stèles et les Failles permettent d’atteindre le Péril 3 sans Sifflet. On demande d’éviter des pertes durant des Résurgences, pas d’utiliser un leurre encore verrouillé. Carnet d’adresses et Cassette restent légendaires disponibles dès le départ, indépendamment de ce défi.

## 4. Correspondance avec le suivi existant

Sources vérifiées : `scripts/Infrastructure/QuestStat.cs`, `QuestDataLoader.ReadCondition`, `scripts/Progression/QuestTracker.cs`, `data/quests/quests.json`.

| Faits utilisés | Source effective | Précision à conserver |
|---|---|---|
| `chests_opened` | Signal `ChestOpened` après ouverture confirmée | Une seule ouverture ; animation sautée, échange et récompenses multiples n’ajoutent pas de compte. |
| `close_kills`, `slowed_kills`, `crit_kills`, `weapon_kills`, `melee_kills`, `ranged_kills` | `EnemyKillResolved` | Arme de l’impact responsable, état de ralentissement au décès, critique réellement fatal ; parties de boss exclues. |
| `kill_burst` | Fenêtre d’éliminations dans `QuestTracker` | Fenêtre de 2 ou 3 s de temps de jeu, pause exclue ; aucune exigence d’une seule attaque. |
| `hp_healed` | `PlayerHealingResolved` et `HpRestored` | Soins effectifs seulement ; toutes sources admises par le suivi existant. |
| `weapons_at_level` | Équipement et amélioration des armes | `target` = nombre d’armes, `param` = niveau minimum ; armes portées simultanément. |
| `zones_discovered` | `ZoneDiscovered`, nombre de cases révélé | 2 500 cases, ni 2 500 biomes ni 2 500 mètres. |
| `distance_m` | `RunTracker.Journey.TravelledMeters` | Distance du voyage réellement suivie, pas une estimation à partir du temps. |
| `peril` | `PerilChanged` | Niveau atteint au moins une fois dans la run, pas niveau exigé à la mort. |
| `crises_survived`, `clean_crises` | Début/fin d’une Résurgence et pertes de PV pendant celle-ci | Pour `clean_crises`, somme des PV perdus comparée à 25 % des PV max à la fin : se soigner n’efface pas ces pertes. Deux Résurgences propres ne doivent pas nécessairement être consécutives. |

Le contrat existant accepte toutes ces conditions et leurs portées. `difficulty`, `scope`, `conditions`, `unlocks` suffisent. Les quêtes sont toutes de récompense `object`, jamais `badge`. Pas de nouvelle animation, image, action d’input ou interface.

## 5. Lot d’activation livré — 12 octobre 2026

1. Fusionner les quinze fiches préparées dans `data/quests/quests.json` avec les seuils validés, Pied-de-biche porté à 50 coffres cumulés. **49 → 64 fiches (57 actives, 7 dormantes historiques)**, **14 → 29 récompenses d’objets** ; ne pas modifier les autres récompenses. Les quinze `initial_unlock: false` restent tels quels : la quête accomplie a priorité dans `MetaSaveManager.IsUnlocked`.
2. Mettre à jour les assertions des suites quêtes ; tester chaque accomplissement et un contre-exemple (79/80, faux type d’arme, critique non fatal, soin sans PV manquant, seconde condition manquante, hors fenêtre…). Exercer l’événement réel, pas seulement `ClaimQuest`.
3. Vérifier le cumul entre deux runs, sauvegarde/rechargement, flush non doublé, isolation normal/dev, et la non-fusion de critères de deux runs pour une quête Run. Les nouvelles quêtes commencent à zéro lors de l’activation : aucune progression historique non enregistrée ne sera inventée.
4. Vérifier qu’un objet nouvellement ouvert apparaît dans les coffres/Ateliers et la Collection ; pas de doublon d’attribution, aucun changement du tirage des objets D. Contrôler les 27 objets initiaux, puis les 56 après accomplissement de toutes les quêtes.
5. Suivi d’équilibrage ultérieur, hors recette de cette activation : confronter les seuils de coffres, ralentissements, soins et du Portrait aux parties humaines. Les mesures économiques [I12 existantes](35-raretes-des-objets.md#i12--recette-économique-du-12-octobre-2026) servent de référence ; aucune nouvelle campagne longue n’est requise pour raccorder ces objectifs validés.
6. Capturer la page Quêtes et sa progression à 720p/1080p, les textes longs et le lien Collection → quête, clavier/manette. Ne cocher la roadmap qu’après activation et ces vérifications.

**Contrôles de cette préparation :** quinze IDs uniques, quinze objets Q couverts exactement une fois, aucun ID de quête ni récompense déjà utilisé, clés/portées/paramètres existants et armes valides. Ce contrôle statique est complété par la recette d’activation ci-dessous.

**Ordre de livraison :** données et assertions de catalogue, raccordements existants et régressions des quinze objectifs, captures/navigation 720p/1080p sur profil normal isolé, puis preuves et roadmap. Les 49 fiches historiques sont conservées à l’identique. Le JSON préparatoire est supprimé après fusion pour garder une seule source active.

## 6. Preuves d’activation

Catalogue : **64 fiches, 57 actives et 7 dormantes historiques**. Les 49 fiches précédentes sont identiques après comparaison structurée ; les quinze ajouts couvrent exactement les quinze objets réservés. Les 27 objets initiaux, dont trois épiques et deux légendaires, restent inchangés. Aucun changement de version de sauvegarde, aucune progression historique inventée pour les nouveaux objectifs.

`tools/validate.sh /tmp/vestiges-quest-items-cases-20261012c quests` : suite verte, **62 contrôles de catalogue/accès et 324 de suivi**, build sans warning et aucune mutation de source pendant la recette. Les quinze scénarios indépendants passent par les événements suivis en jeu, avec seuil incomplet, faits incorrects, cumul entre deux runs, double flush, rechargement, conditions de run non fusionnées et notification unique. Le cinquantième coffre du Pied-de-biche est une transaction réelle : fonds insuffisants refusés, paiement puis ouverture unique. Noms, icônes, texte `49 / 50`, verrous et bandeaux Capsule/Pied-de-biche/Portrait vérifiés. Le butin des coffres et Ateliers passe de 27 objets initiaux à 56 après accomplissement des 29 quêtes d’objets.

**Validation intégrée finale :** `/tmp/vestiges-item-quests-final-20261012` : **8/8 suites vertes**, `quests`, `catalogs`, `saves`, `inventory`, `item_loot`, `dev_mode`, `menu_navigation`, `smoke` ; zéro warning au build, aucune mutation de source. Les accès et acquis normaux sont conservés après bascule dev et retour normal.

**Captures réelles inspectées :** `/tmp/vestiges-quests-pied-720-20261012/`, `/tmp/vestiges-quests-pied-1080-20261012/` et `/tmp/vestiges-quests-portrait-720-20261012/`. Dix-huit PNG aux dimensions contrôlées 1280×720 ou 1920×1080. Profils normaux isolés ; progression `49 / 50` du Pied-de-biche fournie par une fixture pour la présentation uniquement. Les actions `ui_accept` exercent les vrais liens Quêtes → Collection → Quêtes ; `ui_cancel` vérifie le retour. `hub-04-ui_accept.png` montre la quête sélectionnée ; `hub-03-ui_accept.png` du Portrait montre le défilement jusqu’au lien et l’objectif long complet. Texte et trois critères du Portrait lisibles à 720p. Aucune erreur de gameplay dans les logs, seulement deux ressources encore utilisées à la fermeture.

Reproduction avec le helper existant : `HUB_DEV=" " HUB_EXTRA_ARGS="--quest-focus item_open_every_lock" tools/capture_hub.sh <dossier> ui_accept,ui_accept,ui_accept,ui_accept,ui_cancel 1280x720` ; remplacer l’ID par `item_one_less_trace` pour le Portrait. L’option existante `--meta-fixture` permet d’injecter le cumul de présentation dans le profil isolé. Aucun sprite ni animation ajouté.
