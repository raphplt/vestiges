# Plan 35 — I10 : raretés, cumul et équilibre des objets du sac

**Révision du 12 octobre 2026, après les retours de Raphaël. Préparation uniquement.** Cette version remplace le classement et les règles de doublon proposés le 11 octobre. Le gameplay actuel conserve encore ses anciens effets tant que les lots ci-dessous ne sont pas livrés.

Acquis : rareté fixe, fréquence liée au rang, coffres exclusivement à objets, aucun effet supplémentaire débloqué par un doublon. Les objets peuvent désormais porter aussi des statistiques simples, malgré leur présence sur les Écussons. Le catalogue cible comprend **57 objets : 22 révisés et 35 propositions nouvelles**, détaillées dans le [catalogue V2](35-catalogue-objets-v2.md).

Les pourcentages 3 % du pansement, 20 % de la Semelle et 35 % du Thermomètre/de l’Épingle viennent de Raphaël. Les probabilités, durées, plafonds, nouveaux rangs et effets nouveaux ci-dessous sont des **réglages proposés**, à mesurer ; pas une validation implicite.

## 0. Découverte et références à lire avant de coder

Autorité : Stratégie V2 §10 (rareté des améliorations, distincte des objets), §25 et plan 35 §11–§18 (Écussons, exemplaires, sources, coffres). Le catalogue et le code actuel priment sur les anciennes propositions d’objets infinis.

| Point vérifié | Source et API existante à reprendre |
|---|---|
| Catalogue, exemplaires et effets | `data/progression/passive_souvenirs.json` ; `PassiveSouvenirData` et `PassiveSouvenirDataLoader.ParseEntry` dans `scripts/Infrastructure/PassiveSouvenirDataLoader.cs`. Pas de rareté actuelle. |
| Validation des fiches | `scripts/Infrastructure/ObjectDataValidator.cs`, `ObjectKeys`, `ValidateObject` : liste fermée des clés et distinction `kind=item` / `badge`. |
| Tirage commun | `scripts/World/LootRewards.cs`, `PickUnlockedItem(Player, RandomNumberGenerator, Dictionary<string,int> promised = null)` : déblocages, maximum d’exemplaires et exemplaires promis filtrés avant tirage uniforme. |
| Coffres et multi-récompenses | `LootRewards.Resolve(List<LootResolver.LootResult>, Player)` et `WorldInteraction.OpenChest` ; quatre tables `data/loot_tables/chest_*.json`. Le dictionnaire `promisedItems` évite de dépasser le maximum lors d’un même coffre. |
| Autres sources | `scripts/World/ItemDropDirector.cs`, `OnEnemyKill` et `Drop` ; `scripts/World/WorkshopDirector.cs`, `AddItemOffer` et cache `_itemOffers`. Le POI historique appelle aussi `Resolve` (`scripts/Core/Player.cs`, `ApplyPoiLoot`). |
| Sac plein | `LootRewards.Apply(in ResolvedLoot, Player, EventBus, Vector2? dropPosition = null)` → signal `ItemDropRequested`, dépôt et échange existants. |
| Puissance réelle | `scripts/Progression/ObjectTriggers.cs` : `OnWeaponImpact`, `AgainstTarget`, `RewardWeaponKill` ; `ObjectStances.cs` et `ObjectUtilities.cs`. Le poison et les chances de statut tiennent compte du coefficient de déclenchement de l’arme. |
| Apparence | `data/ui/rarities.json` ; `RarityPalette.Main/Colors/DisplayName(string)` ; `RarityArt.Rank(string)` ; exemple `ChoiceStyle.StyleCard` dans `ChestLootScreen`. Lire aussi la Charte graphique : pixels nets, signes lisibles sans couleur seule. |

**Garde-fous d’API :** `OfferWeight` concerne les offres de niveau, pas les objets du sac. `UpgradeRoller.BumpSteps` concerne les améliorations, pas la rareté fixe d’un objet. Les nouveaux champs et le paramètre de source du tirage décrits plus bas sont **à créer**, pas des API existantes.

## 1. Règles de cumul et de puissance

- Une seule rareté par ID d’objet : Commun / Inhabituel / Rare / Épique / Légendaire. La rareté indique la puissance et la fréquence attendues, sans multiplicateur automatique des effets.
- `n` = nombre d’exemplaires présents dans le sac. Chaque exemplaire prend une place. Un bonus de +20 % par exemplaire donne +20 / +40 / +60 %, pas ×1,2 à chaque ajout.
- **Aucun palier d’objet.** Retirer les bonus auparavant déclenchés au deuxième exemplaire : propagation, gel, seconde explosion, armure du Tabouret, remboursement du Porte-monnaie, etc. Les paliers des Écussons restent distincts et ne sont pas concernés.
- Cumul quantitatif du même effet : puissance, durée, débit maximal ou charges. Choisir explicitement l’axe par objet ; éviter de faire monter simultanément chance, dégâts et fréquence et de créer un gain quadratique caché.
- Ne plus faire passer les objets par les anciens niveaux d’effet `[10,15,19,22,24,25]`. Décrire les effets par exemplaire et leur plafond en données. La suppression d’un exemplaire retire exactement son apport ; la fiche affiche 1 exemplaire, quantité actuelle et quantité suivante.
- Limite proposée : 6 exemplaires par défaut ; exceptions explicites dans les tableaux. Un objet saturé n’est plus offert. Trèfle : un exemplaire car une carte supplémentaire est une capacité entière, pas une raison d’inventer un effet secondaire au doublon.
- Les budgets de déclenchement ne se rechargent pas artificiellement en oubliant/reprenant un objet, ni en changeant d’arme. Les bonus temporaires disparaissent lorsque leur objet quitte le sac. Un soin déjà effectué ne se retire pas ; un bonus de PV max, lui, se retire sans soin gratuit au rééquipement.

## 2. Les 22 objets existants, révisés

Les mentions **choix** traduisent le retour utilisateur ; **proposition** indique une valeur ou une interprétation à régler. Tous les rangs restent proposés.

| Rareté | Objet (`id`) | Effet avec n exemplaires, sans palier caché |
|---|---|---|
| Commun | Boîte de pansements (`boite_de_pansements`) | **Choix :** chaque niveau gagné soigne **3 % × n des PV max**, au maximum les PV manquants. Aucun ajout d’invulnérabilité. |
| Commun | Semelle usée (`semelle_usee`) | **Choix :** la prochaine attaque après un dash reçoit **+20 % × n dégâts**. **Proposition :** charge unique valable 3 s à la fin du dash, consommée par un seul lancement d’arme, salve entière comprise. Un nouveau dash renouvelle la charge, ne la cumule pas. |
| Commun | Chewing-gum (`chewing_gum`) | Traînée de dash : ralentissement fixe de 40 %, durée **2 s × n**, au plus 6 s (3 copies). Aucun ajout de Fragilité. |
| Commun | Gilet réfléchissant (`gilet_reflechissant`) | **+2 % × n dégâts par ennemi proche**, maximum 10 ennemis. Le nombre compté ne double plus avec le doublon. |
| Commun | Sifflet d’arbitre (`sifflet_d_arbitre`) | **Choix maintenu :** +1 Péril par exemplaire pris, maximum 10 exemplaires. Le Péril acquis ne se retire pas à l’oubli. |
| Inhabituel | Montre arrêtée (`montre_arretee`) | **Choix :** probabilité de ralentir un élite touché. **Proposition :** 12 % de base ; ralentissement de 35 % pendant 2 s ; budget n applications par 2 s, toutes armes confondues ; même élite réaffectable après 8 s. Aucun effet post-Souverain. |
| Inhabituel | Allumette humide (`allumette_humide`) | **Proposition :** 8 % de base pour brûler 3 s ; dégâts de brûlure actuels (25 % du coup de référence par seconde). Budget **2n applications/s** ; les copies augmentent le débit maximal, pas la chance ni les dégâts. Aucune propagation ajoutée. |
| Inhabituel | Glaçon dans un mouchoir (`glacon`) | **Proposition :** 8 % de base ; ralentissement fixe de 40 % pendant 1,5 s ; budget **n applications/s**. Ne rafraîchit pas son propre ralentissement ; 1 s de répit après sa fin sur cette cible, 2 s pour un élite. Aucun gel ajouté. |
| Inhabituel | Thermomètre (`thermometre`) | **Choix :** **+35 % × n dégâts** contre une cible en feu. Aucun ralentissement ajouté aux brûlures. |
| Inhabituel | Épingle à nourrice (`epingle_a_nourrice`) | **Choix :** **+35 % × n dégâts** contre une cible entravée. Aucun prolongement de contrôle à sa mort. |
| Inhabituel | Thermos (`thermos`) | Effet accepté : **+20 % × n dégâts** au-dessus de 90 % des PV. Le seuil ne descend plus au doublon. |
| Inhabituel | Médaille cabossée (`medaille_cabossee`) | Effet de base conservé faute de retour contraire : **+30 % × n dégâts** sous 35 % des PV. Aucun bonus de vitesse ajouté. |
| Rare | Tabouret de camping (`tabouret_de_camping`) | **Choix :** dégâts progressifs pendant l’immobilité, jusqu’à +100 %. **Proposition :** +20 points de pourcentage/s × n, plafond **+100 % total** : 5 s avec un exemplaire, 2,5 s avec deux. Bouger ou dasher remet à zéro. Remplace la cadence actuelle ; aucune armure ajoutée. |
| Rare | Plan de métro (`plan_de_metro`) | Dash instantané, sans traverser les murs ; deux copies au plus, distance ×1 puis ×1,5. Pas d’invulnérabilité ajoutée. |
| Rare | Flacon de pharmacie (`flacon_de_pharmacie`) | **Choix :** poison probabiliste, pas sur chaque coup. **Proposition :** 10 % de base, dose = 20 % du coup de référence sur 3 s ; budget **2n doses/s**, maximum **3n doses actives par cible**. Une dose neuve ne prolonge pas toutes les anciennes. |
| Rare | Porte-monnaie usé (`porte_monnaie_use`) | +1 % dégâts par 10 Essence détenue, plafond **+30 % × n**. Le plafond monte avec les copies ; aucun remboursement d’Essence ajouté. |
| Rare | Dé à coudre (`de_a_coudre`) | **Recommandation sans dépendance aux critiques :** une élimination d’arme soigne jusqu’à 1 PV ; budget de **n PV/s**, plafonné aussi à 3 % des PV max/s. On soigne seulement le budget disponible, sans stocker un soin futur. Aucun soin d’élite supplémentaire. Rang abaissé d’Épique à Rare après cette limitation. |
| Épique | Pétard mouillé (`petard_mouille`) | Une élimination d’arme produit **une explosion**, dégâts = **100 % × n** du coup fatal. Jamais une seconde explosion obtenue au doublon, ni de cascade d’explosions d’objets. |
| Épique | Loupe de philatéliste (`loupe_de_philateliste`) | Un critique rend la cible Fragile pendant 2 s ; **+50 % × n dégâts reçus**, 3 copies au plus (+150 %). Pas de durée doublée. |
| Épique | Stylo à quatre couleurs (`stylo_quatre_couleurs`) | Un critique atteint **une seule autre cible** avec **60 % × n** de ses dégâts. Aucune cible supplémentaire au doublon. |
| Légendaire | Ticket de consigne (`ticket_de_consigne`) | Effet accepté : chaque copie est une résurrection consommable à 50 % des PV, 2 s d’invulnérabilité. Deux copies au plus ; pas de salut dans le Néant. |
| Légendaire | Trèfle plastifié (`trefle_plastifie`) | Effet accepté : une carte de plus à chaque level-up. Un seul exemplaire. |

Répartition révisée : **5 communs, 7 inhabituels, 5 rares, 3 épiques, 2 légendaires**. Le Dé et le Tabouret changent donc de rang par rapport au 11 octobre, et la Montre passe en Inhabituel.

## 3. Empêcher l’emballement des déclenchements

Dans le code actuel, Allumette et Glaçon ont 30 % de chance de base avec un exemplaire, multipliés par le coefficient de l’arme ; les grands cumuls peuvent finir en déclenchement garanti. Le poison, lui, s’applique actuellement à chaque impact éligible. Aucun de ces trois effets n’a de budget temporel global. Ces constats ne décrivent pas la cible proposée.

Pour Allumette, Glaçon, Flacon et Montre :

1. Tirer sur un **impact d’arme direct**, avec `p_effectif = p_base × clamp(coefficient_arme, 0, 1)`. Pas de hausse de probabilité par copie dans cette proposition.
2. Dédupliquer par attaque et cible : les tics d’un cône continu ou les projectiles d’un même lancement ne donnent pas des tentatives illimitées sur une cible. Des attaques réellement distinctes restent éligibles.
3. Plafond **par joueur et par objet**, partagé entre toutes les armes, cibles, salves et projectiles. Fenêtre glissante de 1 s (2 s pour la Montre), sans crédit accumulé au repos. Une application à une cible consomme une unité ; aucun multiplicateur de tir ne contourne ce budget.
4. Un budget de n déclenchements n’est **pas une garantie de n déclenchements** : le jet doit réussir. Les copies améliorent la capacité du même effet, sans nouvelle mécanique.
5. Les dégâts d’objets, DOT et coups transmis ne relancent pas ces objets. Les capacités natives des armes restent distinctes ; ne pas supprimer leurs statuts ou leurs particularités en bridant les objets.
6. Le Glaçon ne doit pas maintenir une cible ralentie indéfiniment à lui seul. La durée des statuts peut prolonger son effet, mais pas supprimer le répit entre deux applications. Montre : durée modifiable mais bornée à 3 s sur élite, intervalle de 8 s entre applications.
7. Le Dé a un budget en **PV soignés**, distinct des budgets de statut et du plafond existant de vol de vie. Ne pas cumuler n copies et n PV par mort : cela ferait n² en foule.

Mesures obligatoires : armes mono-cible, salves, rebonds, cônes et très nombreux projectiles ; 1/3/6 copies ; même cible et foule ; durée des statuts haute ; soin effectif du Dé sur 100 éliminations simultanées. Tirages déterministes et fenêtres temporelles se vérifient au banc, pas à l’impression visuelle.

## 4. Catalogue élargi et réserve de départ

Voir le [catalogue V2](35-catalogue-objets-v2.md) : **35 nouveaux objets**, couverture des 19 statistiques globales et des paramètres d’armes encore manquants, objets de confort et mécaniques originales d’exploration/combat. Répartition cible : **14 communs / 18 inhabituels / 11 rares / 9 épiques / 5 légendaires**.

Une partie des rangs élevés est éligible dès un profil neuf : proposition **Boucle de ceinture, Diapositive en double et Tampon ANNULÉ** (épiques), **Carnet d’adresses et Cassette rembobinée** (légendaires). Ils ne sont pas donnés au départ et restent soumis aux faibles taux de leur rang. Ticket et Trèfle gardent leurs quêtes actuelles : on ouvre du contenu nouveau sans effacer les objectifs existants.

Les objets de stats et les Écussons ont une fonction distincte : quatre axes de progression continue choisis au level-up contre un sac limité d’appoints et de synergies trouvés dans le monde. Ce recouvrement est désormais voulu par Raphaël. Les petits bonus permanents occupent une place, comme les effets extraordinaires.

## 5. Obtention et affichage

Proposition de taux conservée, non encore mesurée : filtrer les objets débloqués et cumulables, tirer le rang, puis l’objet uniformément dans ce rang. Pourcentages par récompense avec tous les rangs accessibles, pas par objet précis.

| Source | Commun | Inhabituel | Rare | Épique | Légendaire | Quantité |
|---|---:|---:|---:|---:|---:|---|
| Coffre commun / source historique générique | 60 % | 25 % | 11 % | 3,5 % | 0,5 % | 1 par tirage déclaré |
| Coffre rare | 0 % | 55 % | 32 % | 11 % | 2 % | 1 |
| Coffre épique | 0 % | 0 % | 70 % | 25 % | 5 % | 2 tirages |
| Ancien coffre de lore | 0 % | 55 % | 32 % | 11 % | 2 % | 1 |
| Objet d’élite | 35 % | 35 % | 22 % | 7 % | 1 % | chance de lâcher un objet : 25 % |
| Souverain | 0 % | 0 % | 65 % | 30 % | 5 % | 1 garanti |
| Atelier | 30 % | 40 % | 20 % | 9 % | 1 % | 1 offre mémorisée |

Retirer les rangs vides et renormaliser. Si aucun rang de poids positif ne contient de candidat, descendre au meilleur rang éligible sous le minimum normal ; aucun remplacement en Essence. Recalculer les exemplaires promis entre les deux récompenses d’un épique. Sac plein : dépôt au sol et échange.

Le profil neuf cible contient des candidats des cinq rangs : **pas de renormalisation du seul fait d’une nouvelle sauvegarde**. Le taux d’un objet précis dépend du nombre de candidats éligibles de son rang. Chaque ajout de catalogue peut donc diluer une synergie ; mesurer aussi cette diversité.

Prix d’Atelier proposés : **120 / 150 / 210 / 360 / 600 Essence**, puis croissance existante. Annuler/rouvrir ne relance pas l’offre. Pas de nouveau multiplicateur de Chance ou de Péril sur les objets dans ce lot ; la Chance continue à agir là où elle est déjà branchée, notamment les améliorations. La Capsule porte-bonheur reste donc utile même sans agir sur les coffres.

Couleur, signe et nom de rareté propres à l’objet sur toutes ses présentations. Le titre du coffre garde sa couleur de coffre ; ses récompenses affichent leur propre rang. Roulette limitée aux objets éligibles de cette source. Supprimer toutes les promesses « au doublon »/« palier » dans les fiches du sac ; afficher le cumul et ses limites. Les Écussons gardent leurs paliers.

## 6. Lots d’implémentation — un à la fois

### I10a — contrats, cumul et retrait des anciens paliers

Lire §0, `ObjectDataValidator`, `Player.Objects.ApplyPassiveEffects`, activation/retrait des milestones et `InventoryConfig`. Ajouter rareté fixe, effet par copie, maximum et paramètres de budget au modèle/contrat ; règles séparées pour `kind=item` et `badge`. Les nouveaux champs sont à créer. Remplacer la courbe des anciens niveaux d’objets ; retirer les seuls paliers d’objets et leurs branches mortes, pas ceux des Écussons. Migration des IDs et quêtes sans perte.

Vérifier par `catalogs`, `inventory`, `objects`, `saves` : 1/2/3/maximum copies, oubli et rééquipement, chiffres avant/après, aucun palier résiduel ni objet saturé offert. Les 22 objets actifs restent 22 pendant cette phase.

### I10b — effets révisés et garde-fous

Reprendre `ObjectTriggers.OnWeaponImpact`, `ConsumeStride`, `ObjectStances` et `ObjectUtilities`. Semelle au dash, Tabouret aux dégâts progressifs, Montre aux élites ; budgets du §3. `PlayerMobility.DashesStarted` existe ; ne pas inventer un signal de dash existant, ajouter un événement commun si nécessaire et traiter aussi le saut instantané. Reprendre le principe d’oubli réversible de `Player.Objects` et les plafonds de défense de `PlayerDefense` et de vol de vie de `Player`, sans confondre vol de vie et Dé.

Vérifier les événements réels et les cas du §3, les deux variantes de dash, interruption du Tabouret et pause. Aucun compteur/cooldown réinitialisable par échange, aucun GetNode/recherche de groupe ou allocation par image. Listes de temps bornées, pools pour les effets fréquents.

### I10c — tirages et UI des raretés

Reprendre `LootRewards.PickUnlockedItem`, ajouter un contexte explicite de source (à créer), configuration JSON des taux/prix et validation stricte. Raccorder coffres après éventuel Oubli de rétrogradation, élites, Souverains, Ateliers et POI. Préserver les flux aléatoires séparés, `promisedItems`, le dépôt au sol et le cache de l’Atelier.

Reprendre `RarityPalette`, `RarityArt`, `ChoiceStyle` : `ChestLootScreen`, `ItemPickup`, `ItemExchange`, `WorkshopDirector`, `HudInventoryBar`, `PauseMenu`, `HubCollectionPanel` et branche d’inventaire du bilan. Rareté du coffre ≠ rareté de l’objet.

Vérifier par 100 000 tirages déterministes/source (tolérance statistique explicite), déblocages, exclusions, plafonds et budgets ; `choice_screen`, `ui_art`, `quests`. Captures 720p/1080p : plusieurs raretés, sac plein, descriptions de cumul.

### I11 — extension du catalogue

Le [catalogue V2](35-catalogue-objets-v2.md#5-lots-de-production) découpe les 35 nouveautés en quatre lots : stats globales, propriétés d’armes, utilitaires/épiques, légendaires. Un objet n’entre dans le tirage que si son effet, son cumul, son icône, sa description et son accès sont terminés. Les quêtes des nouvelles fiches non disponibles au départ restent à écrire avant leur activation.

### I12 — mesures et recette

Prendre une référence **avant I10a**, après les coffres exclusivement à objets et avec l’état exact de défense du jour (régénération récemment modifiée, DECISIONS §126). Comparer chaque lot à sa référence immédiate, même outil `tools/measure_run.sh`, graines **42,1002,7,123**, 1 800 s, mêmes personnages et paramètres. Profils neuf et catalogue ouvert comparés chacun à lui-même. Pas de mesures de performances sur machine chargée.

Compléter les rapports avec acquisitions/refus par ID, rang et source, budgets de déclenchement, soin effectif, temps du premier rang élevé et objets inopérants faute d’arme compatible. Les tests d’achat/échange et une partie humaine complètent le bot, qui ne juge pas tous ces choix. Mesurer notamment survie sans Dé, builds de statut à salves, Tabouret au plafond et boucles de soin.

Recette : `dotnet build` sans warning ; `tools/validate.sh <dossier neuf> smoke inventory objects catalogs quests saves choice_screen ui_art small_places` ; captures des effets et vraie run ; banc dense pour nouveaux effets fréquents. Roadmap cochée uniquement après livraison et vérification, jamais sur cette préparation.
