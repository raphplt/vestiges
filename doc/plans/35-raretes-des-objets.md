# Plan 35 — I10 : raretés, cumul et équilibre des objets du sac

**Plan validé et implémenté le 12 octobre 2026 (DECISIONS §133).** I10a–I10e et I11 sont raccordés : 56 objets, raretés fixes, coffres payants, Chance, cumul simple, effets révisés, nouvelles mécaniques et fiches exhaustives. Les validations par lot sont consignées ci-dessous ; la recette globale et les mesures I12 sont distinctes de l’équilibrage humain encore à juger. Les [quinze nouvelles quêtes sont intégrées](35-quetes-objets-v2.md), activation vérifiée : 27 objets disponibles au profil neuf, 14 anciens à quête conservés, 15 nouveaux gardés par leurs quêtes ; 56 accessibles en dev. Aucun sprite ou animation nouveau créé.

Acquis : rareté fixe, fréquence liée au rang, coffres exclusivement à objets, aucun effet supplémentaire débloqué par un doublon. Les objets peuvent désormais porter aussi des statistiques simples, malgré leur présence sur les Écussons. Le catalogue comprend **56 objets : 22 révisés et 34 nouveaux**, détaillés dans le [catalogue V2](35-catalogue-objets-v2.md).

Les pourcentages 3 % du pansement, 20 % de la Semelle et 35 % du Thermomètre/de l’Épingle viennent de Raphaël. Les probabilités, durées, plafonds, rangs et effets ci-dessous sont validés comme premiers réglages à mesurer. Les sprites et animations nouveaux sont exclus de cette livraison.

## 0. Découverte et références à lire avant de coder

Autorité : Stratégie V2 §10 (rareté des améliorations, distincte des objets), §25 et plan 35 §11–§18 (Écussons, exemplaires, sources, coffres). La décision §133 autorise les copies sans limite, sous réserve des places disponibles dans le sac.

| Point vérifié | Source et API existante à reprendre |
|---|---|
| Catalogue, exemplaires et effets | `data/progression/passive_souvenirs.json` ; `PassiveSouvenirData` et `PassiveSouvenirDataLoader.ParseEntry` dans `scripts/Infrastructure/PassiveSouvenirDataLoader.cs`. Avant I10 : aucune rareté fixe. |
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
- **Aucune limite de copies pour les objets ordinaires**, y compris les anciennes exceptions à deux, trois, quatre ou dix copies. Seuls les légendaires et objets explicitement uniques gardent une limite. `max_copies: 0` signifie illimité ; la place du sac reste nécessaire. Les plafonds de sécurité d’un effet (probabilité ≤ 100 %, nombre d’ennemis disponibles, immunités) restent distincts d’une limite de copies.
- Les budgets de déclenchement ne se rechargent pas artificiellement en oubliant/reprenant un objet, ni en changeant d’arme. Les bonus temporaires disparaissent lorsque leur objet quitte le sac. Un soin déjà effectué ne se retire pas ; un bonus de PV max, lui, se retire sans soin gratuit au rééquipement.

## 2. Les 22 objets existants, révisés

Les mentions **choix** traduisent le retour utilisateur ; **proposition** indique une valeur ou une interprétation à régler. Rangs adoptés comme réglages initiaux ; l’équilibrage peut encore les déplacer.

| Rareté | Objet (`id`) | Effet avec n exemplaires, sans palier caché |
|---|---|---|
| Commun | Boîte de pansements (`boite_de_pansements`) | **Choix :** chaque niveau gagné soigne **3 % × n des PV max**, au maximum les PV manquants. Aucun ajout d’invulnérabilité. |
| Commun | Semelle usée (`semelle_usee`) | **Choix :** la prochaine attaque après un dash reçoit **+20 % × n dégâts**. **Proposition :** charge unique valable 3 s à la fin du dash, consommée par un seul lancement d’arme, salve entière comprise. Un nouveau dash renouvelle la charge, ne la cumule pas. |
| Commun | Chewing-gum (`chewing_gum`) | Traînée de dash : ralentissement fixe de 40 %, durée **min(2 s × n, 6 s)**, sans limite de copies. Aucun ajout de Fragilité. |
| Commun | Gilet réfléchissant (`gilet_reflechissant`) | **+2 % × n dégâts par ennemi proche**, maximum 10 ennemis. Le nombre compté ne double plus avec le doublon. |
| Commun | Sifflet d’arbitre (`sifflet_d_arbitre`) | **Choix maintenu :** +1 Péril par exemplaire pris, sans limite de copies (Péril plafonné par le système). Le Péril acquis ne se retire pas à l’oubli. |
| Inhabituel | Montre arrêtée (`montre_arretee`) | **Choix :** probabilité de ralentir un élite touché. **Proposition :** 12 % de base ; ralentissement de 35 % pendant 2 s ; budget n applications par 2 s, toutes armes confondues ; même élite réaffectable après 8 s. Aucun effet post-Souverain. |
| Inhabituel | Allumette humide (`allumette_humide`) | **Proposition :** 8 % de base pour brûler 3 s ; dégâts de brûlure actuels (25 % du coup de référence par seconde). Budget **2n applications/s** ; les copies augmentent le débit maximal, pas la chance ni les dégâts. Aucune propagation ajoutée. |
| Inhabituel | Glaçon dans un mouchoir (`glacon`) | **Proposition :** 8 % de base ; ralentissement fixe de 40 % pendant 1,5 s ; budget **n applications/s**. Ne rafraîchit pas son propre ralentissement ; 1 s de répit après sa fin sur cette cible, 2 s pour un élite. Aucun gel ajouté. |
| Inhabituel | Thermomètre (`thermometre`) | **Choix :** **+35 % × n dégâts** contre une cible en feu. Aucun ralentissement ajouté aux brûlures. |
| Inhabituel | Épingle à nourrice (`epingle_a_nourrice`) | **Choix :** **+35 % × n dégâts** contre une cible entravée. Aucun prolongement de contrôle à sa mort. |
| Inhabituel | Thermos (`thermos`) | Effet accepté : **+20 % × n dégâts** au-dessus de 90 % des PV. Le seuil ne descend plus au doublon. |
| Inhabituel | Médaille cabossée (`medaille_cabossee`) | Effet de base conservé faute de retour contraire : **+30 % × n dégâts** sous 35 % des PV. Aucun bonus de vitesse ajouté. |
| Rare | Tabouret de camping (`tabouret_de_camping`) | **Choix :** dégâts progressifs pendant l’immobilité, jusqu’à +100 %. **Proposition :** +20 points de pourcentage/s × n, plafond **+100 % total** : 5 s avec un exemplaire, 2,5 s avec deux. Bouger ou dasher remet à zéro. Remplace l’ancienne cadence ; aucune armure ajoutée. |
| Rare | Plan de métro (`plan_de_metro`) | Dash instantané, sans traverser les murs ; sans limite de copies, distance ×(1 + 0,5 × (n − 1)). Pas d’invulnérabilité ajoutée. |
| Rare | Flacon de pharmacie (`flacon_de_pharmacie`) | **Choix :** poison probabiliste, pas sur chaque coup. **Proposition :** 10 % de base, dose = 20 % du coup de référence sur 3 s ; budget **2n doses/s**, maximum **3n doses actives par cible**. Une dose neuve ne prolonge pas toutes les anciennes. |
| Rare | Porte-monnaie usé (`porte_monnaie_use`) | +1 % dégâts par 10 Essence détenue, plafond **+30 % × n**. Le plafond monte avec les copies ; aucun remboursement d’Essence ajouté. |
| Rare | Dé à coudre (`de_a_coudre`) | **Recommandation sans dépendance aux critiques :** une élimination d’arme soigne jusqu’à 1 PV ; budget de **n PV/s**, sans le second plafond à 3 % des PV max/s qui écrasait les copies. On soigne seulement le budget disponible, sans stocker un soin futur. Aucun soin d’élite supplémentaire. Rang abaissé d’Épique à Rare après cette limitation. |
| Épique | Pétard mouillé (`petard_mouille`) | Une élimination d’arme produit **une explosion**, dégâts = **100 % × n** du coup fatal. Jamais une seconde explosion obtenue au doublon, ni de cascade d’explosions d’objets. |
| Épique | Loupe de philatéliste (`loupe_de_philateliste`) | Un critique rend la cible Fragile pendant 2 s ; **+50 % × n dégâts reçus**, sans limite de copies. Pas de durée doublée. |
| Épique | Stylo à quatre couleurs (`stylo_quatre_couleurs`) | Un critique atteint **une seule autre cible** avec **60 % × n** de ses dégâts. Aucune cible supplémentaire au doublon. |
| Légendaire | Ticket de consigne (`ticket_de_consigne`) | Effet accepté : chaque copie est une résurrection consommable à 50 % des PV, 2 s d’invulnérabilité. Deux copies au plus ; pas de salut dans le Néant. |
| Légendaire | Trèfle plastifié (`trefle_plastifie`) | Effet accepté : une carte de plus à chaque level-up. Un seul exemplaire. |

Répartition révisée : **5 communs, 7 inhabituels, 5 rares, 3 épiques, 2 légendaires**. Le Dé et le Tabouret changent donc de rang par rapport au 11 octobre, et la Montre passe en Inhabituel.

## 3. Empêcher l’emballement des déclenchements

Avant I10b, Allumette et Glaçon avaient 30 % de chance de base avec un exemplaire, multipliés par le coefficient de l’arme ; les grands cumuls peuvent finir en déclenchement garanti. Le poison s’appliquait à chaque impact éligible, et aucun des trois effets n’avait de budget temporel global. I10b a remplacé ces comportements par les probabilités et budgets décrits ici.

Pour Allumette, Glaçon, Flacon et Montre :

1. Tirer sur un **impact d’arme direct**, avec `p_effectif = p_base × clamp(coefficient_arme, 0, 1)`. Pas de hausse de probabilité par copie dans cette proposition.
2. Dédupliquer par attaque et cible : les tics d’un cône continu ou les projectiles d’un même lancement ne donnent pas des tentatives illimitées sur une cible. Des attaques réellement distinctes restent éligibles.
3. Plafond **par joueur et par objet**, partagé entre toutes les armes, cibles, salves et projectiles. Fenêtre glissante de 1 s (2 s pour la Montre), sans crédit accumulé au repos. Une application à une cible consomme une unité ; aucun multiplicateur de tir ne contourne ce budget.
4. Un budget de n déclenchements n’est **pas une garantie de n déclenchements** : le jet doit réussir. Les copies améliorent la capacité du même effet, sans nouvelle mécanique.
5. Les dégâts d’objets, DOT et coups transmis ne relancent pas ces objets. Les capacités natives des armes restent distinctes ; ne pas supprimer leurs statuts ou leurs particularités en bridant les objets.
6. Le Glaçon ne doit pas maintenir une cible ralentie indéfiniment à lui seul. La durée des statuts peut prolonger son effet, mais pas supprimer le répit entre deux applications. Montre : durée modifiable mais bornée à 3 s sur élite, intervalle de 8 s entre applications.
7. Le Dé a un budget en **PV soignés**, distinct des budgets de statut et du plafond révisé de vol de vie (§3.1). Ne pas cumuler n copies et n PV par mort : cela ferait n² en foule.

Mesures obligatoires : armes mono-cible, salves, rebonds, cônes et très nombreux projectiles ; 1/3/6 copies ; même cible et foule ; durée des statuts haute ; soin effectif du Dé sur 100 éliminations simultanées. Tirages déterministes et fenêtres temporelles se vérifient au banc, pas à l’impression visuelle.

## 3.1 Soins : récompenser un build, éviter le départ autosuffisant

Constat : `data/characters/defense.json:lifesteal_max_hp_per_second = 0.03` et `Player.StepLifesteal` plafonnent **le vol de vie uniquement**, toutes armes confondues. Ce n’est pas un plafond général de régénération ; le pansement, la régénération passive et le Dé ne passent pas tous par lui.

**Révision proposée :** relever le plafond partagé de vol de vie à **8 % des PV max/s**, sans imposer ce plafond aux autres soins. Le taux prélevé reste modeste : Ampoule rouge = +0,5 point par copie, rare et à débloquer. À 100 PV max, une copie rend 0,5 PV/s pour 100 dégâts éligibles/s ; atteindre 8 PV/s réclame 1 600 dégâts/s. Avec quatre copies, il faut encore 400 dégâts/s. Calculer sur les dégâts réellement infligés, sans compter le surplus sur une cible presque morte ni des dégâts passifs qui rebouclent. La rareté ne suffit pas à elle seule à garantir l’équilibre.

Pastille = **9 PV/min par copie** (0,15 PV/s en interne), rendus progressivement, pause de 2 s après blessure conservée. Six copies donnent 54 PV/min avant interruptions et occupent six places. Dé = au plus n PV/s, sans second plafond en pourcentage, toujours limité par les éliminations d’arme et 1 PV par élimination. Pansement et Cassette gardent leurs propres conditions. Pas de rechargement des budgets par échange ni de PV gratuits avec les PV max.

Mesurer les PV réellement rendus et perdus par source en début de run (0–3 min : zéro/un objet), puis builds dédiés et cumulés à 10/20/30 min. Une hausse du plafond n’est ni un débit garanti ni une validation de l’équilibre : si un seul petit objet annule les blessures du début, corriger son gain ; conserver une récupération élevée possible avec plusieurs bons objets et assez de dégâts.

## 4. Catalogue élargi et réserve de départ

Voir le [catalogue V2](35-catalogue-objets-v2.md) : **34 nouveaux objets**, couverture des 19 statistiques globales et des paramètres d’armes encore manquants, objets de confort et mécaniques originales d’exploration/combat. Répartition cible : **14 communs / 18 inhabituels / 11 rares / 8 épiques / 5 légendaires**.

Une partie des rangs élevés est éligible dès un profil neuf : **Boucle cabossée, Diapositive en double et Tampon ANNULÉ** (épiques), **Carnet d’adresses et Cassette rembobinée** (légendaires). Ils ne sont pas donnés au départ et restent soumis aux faibles taux de leur rang. Ticket et Trèfle gardent leurs quêtes actuelles : on ouvre du contenu nouveau sans effacer les objectifs existants.

Les objets de stats et les Écussons ont une fonction distincte : quatre axes de progression continue choisis au level-up contre un sac limité d’appoints et de synergies trouvés dans le monde. Ce recouvrement est désormais voulu par Raphaël. Les petits bonus permanents occupent une place, comme les effets extraordinaires.

## 5. Obtention et affichage

Taux initiaux implémentés, distribution contrôlée au banc : filtrer les objets débloqués et cumulables, tirer le rang, puis l’objet uniformément dans ce rang. Pourcentages par récompense avec tous les rangs accessibles, pas par objet précis.

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

Le profil neuf contient des candidats des cinq rangs : **pas de renormalisation du seul fait d’une nouvelle sauvegarde**. Le taux d’un objet précis dépend du nombre de candidats éligibles de son rang. Chaque ajout de catalogue peut donc diluer une synergie ; mesurer aussi cette diversité.

Prix d’Atelier proposés : **120 / 150 / 210 / 360 / 600 Essence**, puis croissance existante. Annuler/rouvrir ne relance pas l’offre. La Chance influence les rangs tirés dans les coffres (§5.1). Aucun nouveau bonus de Péril sur le butin. Les taux des autres sources restent ceux du tableau dans ce lot ; les offres d’Atelier restent mémorisées.

Couleur, signe et nom de rareté propres à l’objet sur toutes ses présentations. Le titre du coffre garde sa couleur de coffre ; ses récompenses affichent leur propre rang. Roulette limitée aux objets éligibles de cette source. Supprimer toutes les promesses « au doublon »/« palier » dans les fiches du sac ; afficher le cumul et ses limites. Les Écussons gardent leurs paliers.

### 5.1 Chance appliquée aux coffres

**Avant I10 :** `LootRewards.PickUnlockedItem` choisissait uniformément parmi les objets accessibles, sans Chance. **Implémenté :** `ItemLootPool` tire un rang pondéré par la Chance puis un objet éligible de ce rang. `UpgradeRoller` conserve son utilisation pour les améliorations. Un objet commun ne devient jamais une version rare.

Proposition simple en données : avant normalisation, `poids_final(r) = poids_base(r) × (1 + max(0, Chance))^r`, avec r = 0/1/2/3/4 du Commun au Légendaire et Chance interne 0,03 pour +3 points. La table ci-dessus vaut donc à **0 Chance**. Filtrer les rangs vides, pondérer, normaliser, tirer le rang puis l’objet ; la Chance ne débloque rien et ne contourne pas les maxima de copies. Elle ne change ni quantité, ni prix, ni probabilité de gratuité du Pied-de-biche.

Capturer la Chance lors de l’ouverture effective, avant les récompenses (même valeur pour les deux tirages d’un épique). Ne jamais relancer le rang en sautant une animation, rouvrant un écran ou choisissant avec le Carnet. Tester 0/+3/+30/+100 points, fréquences mesurées et hausse de la probabilité cumulée « au moins Rare/Épique/Légendaire ». Afficher les chances résultantes dans le détail du coffre ; pas « +3 % de légendaires ».

### 5.2 Coffres payants et Pied-de-biche

**Choix de Raphaël :** les coffres s’ouvrent contre de l’Essence. La vitesse de l’animation n’est pas un avantage d’objet. Tous les coffres sont concernés, y compris ceux générés comme récompense ; ne pas introduire silencieusement une exception gratuite. Le butin reste exclusivement composé d’objets.

**Premiers prix proposés, à mesurer :** commun 20, rare 45, épique 90 (deux objets), ancien lore 45 Essence ; facteur temporel `1 + 0,08 × minutes_entières`, arrondi à l’entier supérieur. Figer le devis au premier affichage du prix dans la zone d’interaction ; afficher le total avant validation. Prix réglables en JSON. Comparer aux revenus réels d’un profil neuf : le premier coffre commun doit rester achetable après environ 60–90 s de jeu actif, sans dépendre d’une amélioration méta. Ces prix sont implémentés comme réglages initiaux ; mesurer aussi le nombre de coffres laissés faute de fonds et les dépenses d’Atelier/Mémorial.

**Pied-de-biche :** 15 % de gratuité par copie, sans limite de copies, probabilité plafonnée à 60 %. À la première tentative explicite d’ouverture, mémoriser un unique jet et le nombre de copies utilisé, même si le portefeuille est insuffisant. Avec zéro copie, résultat payant mémorisé sans tirage. Succès : ouvrir sans débit, même avec zéro Essence ; échec : prix normal, coffre fermé si fonds insuffisants. Le résultat persiste pour ce coffre ; acquérir une copie, annuler ou s’éloigner ne relance rien. Un coffre payant peut être retenté plus tard avec assez d’Essence.

**Transaction :** vérifier qu’une récompense accessible existe avant le jet ou tout débit ; sinon annoncer l’indisponibilité et laisser le coffre fermé. Vérification des fonds, débit éventuel, ouverture et réservation des objets forment une opération unique. Pas de paiement à la simple approche, pas de double débit, pas de récompense avant succès, pas de perte d’Essence en cas d’échec interne. Sac plein : échange/dépôt existant, pas un refus d’achat ni un remboursement pour relancer. Le paiement intervient avant calcul du bonus de dégâts du Porte-monnaie, qui suit le solde réel. Sauter la roulette ne change ni paiement ni tirage. Signal clair « Ouverture gratuite » sur succès du Pied-de-biche.

### 5.3 Statistiques et effets visibles

Le [contrat de livraison du catalogue](35-catalogue-objets-v2.md#6-contrat-de-livraison--effet-réel-statistiques-et-retour-visuel) est obligatoire pour tous les lots : effet réellement consommé en jeu, toutes les stats dans la pause même à zéro, valeurs par arme, régénération en PV/min, descriptions des conditions et budgets, retour visuel pertinent en run. `PlayerSheet` expose désormais les dix-neuf stats globales ; leurs contrôles et captures figurent en I10d/I11.

## 6. Lots d’implémentation — un à la fois

### I10a — contrats, cumul et retrait des anciens paliers

Découpage avant code : (1) modèle/validation `rarity`, `max_copies` et `unique`, suppression des courbes ; (2) copies directes et retrait symétrique, effets par copie sans paliers d’objet ; (3) filtrage des seules limites explicites et libellés ; (4) régressions de contrats/inventaire et build. Les 22 fiches conservent leur effet à une copie lors de cette migration ; leurs changements de comportement viennent en I10b.

Lire §0, `ObjectDataValidator`, `Player.Objects.ApplyPassiveEffects`, activation/retrait des milestones et `InventoryConfig`. Ajouter rareté fixe, effet par copie, maximum et paramètres de budget au modèle/contrat ; règles séparées pour `kind=item` et `badge`. Les nouveaux champs sont à créer. Remplacer la courbe des anciens niveaux d’objets ; retirer les seuls paliers d’objets et leurs branches mortes, pas ceux des Écussons. Migration des IDs et quêtes sans perte.

Vérifier par `catalogs`, `inventory`, `objects`, `saves` : 1/2/3/maximum copies, oubli et rééquipement, chiffres avant/après, aucun palier résiduel ni objet saturé offert. Les 22 objets actifs restent 22 pendant cette phase.

**Fondations livrées le 12 octobre :** rareté fixe et `max_copies` contrôlés ; 0 signifie illimité, limite positive obligatoire pour les légendaires, `unique` implique une seule copie. Les 22 objets actifs gardent leurs IDs/déblocages ; leurs anciens pas ont été convertis en valeurs par copie, sans courbe de niveaux. `ActivePassiveSouvenir.Level` reflète les copies pour les objets, tandis que les Écussons gardent leur progression. Ajout/retrait symétriques, filtres du butin et refus d’échange appliquent les seules limites explicites. Plus aucun palier dans les fiches d’objets ; collection et pause n’en promettent plus.

Validation : `tools/validate.sh /tmp/vestiges-item-foundation-final-20261012 catalogs inventory saves smoke` : 4/4 suites, 273 contrôles de contrats, 67 d’inventaire, 58 de sauvegardes, build sans warning. Les tests vérifient notamment 41 copies ordinaires, retrait exact, plafond des Tickets et paliers des Écussons inchangés. La suite `objects` historique conserve encore des assertions des anciens effets/paliers : son remplacement et le retrait physique des anciennes branches de déclencheurs sont inclus en I10b. Les nouveaux effets de statistiques doivent éviter les soins gratuits au rééquipement (`max_hp` et `shield`) en I10b/I11.

### I10b — effets révisés et garde-fous

Reprendre `ObjectTriggers.OnWeaponImpact`, `ConsumeStride`, `ObjectStances` et `ObjectUtilities`. Semelle au dash, Tabouret aux dégâts progressifs, Montre aux élites ; budgets du §3. `PlayerMobility.DashesStarted` existe ; ne pas inventer un signal de dash existant, ajouter un événement commun si nécessaire et traiter aussi le saut instantané. Reprendre le principe d’oubli réversible de `Player.Objects` et les plafonds de défense de `PlayerDefense` et le nouveau réglage de vol de vie de `Player` (§3.1), sans confondre vol de vie et Dé.

Vérifier les événements réels et les cas du §3, les deux variantes de dash, interruption du Tabouret et pause. Aucun compteur/cooldown réinitialisable par échange, aucun GetNode/recherche de groupe ou allocation par image. Listes de temps bornées, pools pour les effets fréquents.

**Livré et vérifié le 12 octobre 2026 :** les 22 objets appliquent désormais les valeurs et conditions du §2. La Semelle se charge à la fin d’un dash (saut compris), s’applique aussi aux contacts orbitaux et expire après 3 s. Le Tabouret augmente progressivement les dégâts, pas la cadence. La Montre agit sur les élites touchés. Les anciennes branches de paliers du sac ont été retirées, tandis que celles des Écussons restent inchangées.

Allumette/Glaçon/Flacon/Montre utilisent des probabilités fixes et des budgets partagés entre armes ; les tentatives sont dédupliquées par lancement et vie de cible, sans éviction temporelle permettant de relancer une attaque très longue. Les historiques sont réutilisés et libérés à la mort/recyclage de la cible. Le poison conserve des doses indépendantes, le Dé un budget en PV réellement soignés. Rééquipement et pause ne contournent aucun délai. Le nombre d’identifiants mémorisés peut croître lors d’un combat exceptionnellement long contre une cible immortelle. Quatre passes du banc dense ont depuis vérifié les effets et le recyclage en charge ([recette I12](#i12--recette-fonctionnelle-finale)), sans conclusion sur les FPS tant que la machine n’est pas calme.

Le vol de vie utilise les PV effectivement perdus par la cible, sans surplus au-delà de ses PV, avec plafond à 8 % des PV max/s. Les bonus de PV max/bouclier du sac passent par une voie d’application sans soin ni recharge gratuits. Le Plan de métro est illimité : distance de base 1, puis +0,5 par copie supplémentaire, valeurs en données. Les événements typés `PlayerDashStarted/Moved/Finished` portent le trajet réel après collisions et servent aussi aux nouveaux objets à venir.

**Preuves :** `tools/validate.sh /tmp/vestiges-items-i10b-final-20261012 objects inventory catalogs` : 3/3 suites vertes, `source_changes: []`, 273 contrôles de contrats. Régressions dédiées : probabilités/budgets interarmes, doublon d’attaque après 121 s sur cible vivante, ralentissement d’élite et répit, doses de poison décalées, Semelle orbitale, équipement du Chewing-gum en plein dash, échange réel de PV/bouclier, soin sans overkill et pause moteur réelle. Seul un formatage C# a suivi ; build final `/tmp/vestiges-i10b-reviewed-build.log` : zéro warning et zéro erreur. Aucun sprite ou animation créé. Les captures I10d/I11 et les mesures économiques I12 complètent ces suites ; l’équilibrage en partie humaine reste à juger.

### I10c — tirages et UI des raretés

Reprendre `LootRewards.PickUnlockedItem`, ajouter un contexte explicite de source (à créer), configuration JSON des taux/prix, pondération de Chance du §5.1 et validation stricte. Raccorder coffres après éventuel Oubli de rétrogradation, élites, Souverains, Ateliers et POI. Préserver les flux aléatoires séparés, `promisedItems`, le dépôt au sol et le cache de l’Atelier.

Reprendre `RarityPalette`, `RarityArt`, `ChoiceStyle` : `ChestLootScreen`, `ItemPickup`, `ItemExchange`, `WorkshopDirector`, `HudInventoryBar`, `PauseMenu`, `HubCollectionPanel` et branche d’inventaire du bilan. Rareté du coffre ≠ rareté de l’objet.

Vérifier par 100 000 tirages déterministes/source et niveaux de Chance (tolérance statistique explicite), déblocages, exclusions, plafonds et budgets ; `choice_screen`, `ui_art`, `quests`. Captures 720p/1080p : plusieurs raretés, sac plein, descriptions de cumul.

**Livré et vérifié le 12 octobre 2026.** `ItemLootConfig` et `ItemLootPool` tirent le rang puis l’objet accessible selon la source ; la Chance pondère les coffres, les autres sources conservent leurs poids. Réservations, maxima des légendaires et alternatives du Carnet partagent le même filtrage. La rareté propre figure dans les présentations et la roulette.

Preuves : `/tmp/vestiges-loot-final-142.log` — **142 contrôles verts**, dont **3,2 millions de tirages** (8 sources × 4 valeurs de Chance × 100 000), tolérance de six écarts-types plus cinq tirages, cumul de rang monotone et uniformité interne. `choice_screen`, `inventory`, `quests`, `smoke` passent aussi (`/tmp/vestiges-loot-finish-20261012/`) ; le rapport global signale uniquement la modification concurrente de `StatCatalog`, validée séparément. Capture réelle inspectée : `/tmp/vestiges-loot-paid-final-20261012/loot-revealed.png` (fenêtre effective 3840×2160 malgré la résolution demandée ; recette réellement 720p ci-dessous). L’ajout du prix et des probabilités avait rogné les cartes ; hauteur corrigée et testée. Les captures du Carnet activé et du catalogue étendu complètent ces preuves en I11.

### I10d — statistiques exhaustives et contrat de visibilité

**Livré et vérifié le 12 octobre 2026.** `PlayerSheet` expose les dix-neuf statistiques, même nulles, avec base, bonus et total ; régénération en PV/min et plafond de vol de vie distinct. La pause expose les valeurs effectives par arme et `ItemPresentation` partage rareté, conditions, budgets, copies actuelles et prochaine copie entre Collection, sac, cartes, échange et bilan. Les badges conservent leurs paliers.

**Preuves :** `tools/test_objects.sh` et `tools/test_ui_art.sh` verts ; build final `/tmp/vestiges-i10d-build-final-20261012.log`, zéro warning et zéro erreur. Captures inspectées à leurs dimensions réelles 1280×720 et 1920×1080 dans `/tmp/vestiges-i10d-pause-real720-20261012/` et `/tmp/vestiges-i10d-pause-real1080-20261012/` : stats, Pansements ×3, Glaçon, focus clavier/manette et défilement. Ces captures du binaire UI vérifié contiennent une erreur de version de configuration de faune concurrente (run sans faune) ; elle ne valide ni n’invalide le rendu des fiches. Le build final et la suite UI ont ensuite été exécutés avec les sources de faune rétablies. Ce lot fournit le socle ; chaque objet nouveau doit compléter sa propre preuve d’effet et sa présentation.

### I10e — économie des coffres et transaction

Reprendre `WorldInteraction.OpenChest`, `Chest`, `EssenceTracker`, les prompts et `ChestLootScreen` ; appliquer §5.2 avant d’activer le Pied-de-biche. Tests : juste assez/pas assez d’Essence, gratuité avec portefeuille vide, annulation/retour/doublon, deux récompenses, sac plein, aucun candidat, deux interactions simultanées et animation sautée. Paiement une fois, attribution une fois ; pas de tirage supplémentaire à la réouverture. Capturer prix, insuffisance et gratuité. Mesurer prix/revenus avant de livrer la nouvelle économie.

**Transaction livrée et vérifiée le 12 octobre 2026.** Prix en données et devis figé, jet unique de gratuité, vérification des candidats avant débit, garde contre la réentrée `EssenceChanged`, deux récompenses et attribution unique à la fermeture. Sac plein : les objets sont déposés. Les contrôles ci-dessus couvrent fonds insuffisants, zéro Essence avec gratuité, retour après acquisition du Pied-de-biche, absence de candidat, annulation, doublons et animation sautée. Capture du prix annoncé et de l’insuffisance : `/tmp/vestiges-loot-paid-final-20261012/loot-price-insufficient.png`. Les revenus de run et les achats rejoués avant/après sont désormais mesurés dans la [recette économique I12](#i12--recette-économique-du-12-octobre-2026), avec leurs limites explicites. L’accessibilité réelle des coffres et l’intérêt des dépenses restent à juger en partie humaine.

### I11 — extension du catalogue

**Implémenté et vérifié par lots.** Les 34 nouveautés du [catalogue V2](35-catalogue-objets-v2.md#5-lots-de-production) comprennent 18 objets de stats globales, six de propriétés d’armes et dix utilitaires/effets. Copies ordinaires illimitées ; trois copies pour les trois nouveaux légendaires. Les orbitales utilisent `projectile_count` et le bonus commun ; le Verre fêlé est disponible au profil neuf ; les flèches de coffres exigent l’Étiquette. Icônes existantes réutilisées, sans nouveau sprite/animation.

**Accès et preuves catalogue :** `/tmp/vestiges-i11-catalogue-validation-20261012`, quatre suites vertes (`objects`, `catalogs`, `quests`, `dev_mode`), aucune mutation de source pendant les tests. Les objets de stats ont été ajoutés jusqu’à huit copies puis retirés sans résidu ; mobilité, cadence réelle, XP acquise, régénération, armure, bouclier, vol de vie, durée et dégâts critiques vérifiés. `initial_unlock` a gardé les quinze Q fermés avant leurs quêtes ; la quête accomplie a priorité et le mode dev ouvre tout. Les 49 quêtes existantes restent inchangées. [Activation des quinze quêtes](35-quetes-objets-v2.md#6-preuves-dactivation) livrée et vérifiée ensuite (DECISIONS §138) : leurs fiches figurent dans `data/quests/quests.json`.

**Raccordements supplémentaires :** banc combat (167 contrôles lors du premier bilan, puis renforts de recette) : projectiles réellement guidés/accélérés/perforants, critique, recul, orbitales, Registre, Pelote, Tampon, Cassette et Portrait ; banc monde 201 contrôles (`/tmp/vestiges-item-loot.zMXkwP/`) : Étiquette, durée des objets au sol, Pied-de-biche, Sonnette et Carnet. Captures monde réellement 1280×720 inspectées dans `/tmp/vestiges-items-world-real720-20261012/`. Les preuves finales de recette/mesure restent regroupées en I12.

**Présentation des nouveautés :** captures réelles contrôlées 1280×720 et 1920×1080 dans `/tmp/vestiges-i11-new-items-1280x720-20261012/` et `/tmp/vestiges-i11-new-items-1920x1080-20261012/` (`pause-object.png` : Pastilles ×2, +18 PV/min ; `pause-object-status.png` : Carnet légendaire, 1/3 copies). Trois épiques dans le sac, statistiques mises à jour, navigation/défilement vérifiés ; aucune erreur de gameplay dans ces deux captures. Le test UI a été adapté à la réutilisation d’icônes : les nouveaux IDs n’exigent pas de nouveaux sprites, toutes leurs images doivent provenir du catalogue de gameplay, distinct du contenu visuel futur. `tools/test_ui_art.sh` vert après cette correction, `/tmp/vestiges-i11-ui-art-final-20261012.log`.

**I11 monde livré et vérifié le 12 octobre 2026 :** Pied-de-biche, Étiquette, Ticket de vestiaire, Sonnette et Carnet d’adresses utilisent les fiches réelles du catalogue. `tools/test_item_loot.sh` : **201 contrôles, zéro échec**, logs `/tmp/vestiges-item-loot.zMXkwP/`. Acquisition, huit copies ordinaires et retrait réels ; Carnet limité à trois copies ; disparition des seules flèches de coffres en oubliant l’Étiquette ; expiration à 120 + 45n secondes, retrait et Effacement des objets au sol ; attraction effective des orbes sans créer d’XP, uniquement après clôture des récompenses du coffre ; paiement/choix/attribution du Carnet par les écrans de jeu.

Capture en **1280×720 effectifs**, dimensions des PNG vérifiées et images inspectées : `/tmp/vestiges-items-world-real720-20261012/`. `loot-choices-1.png` montre les alternatives du Carnet ; `loot-revealed.png` montre les deux récompenses du coffre épique et l’ouverture gratuite ; `loot-pointer-without-label.png` / `loot-pointer-with-label.png` montrent la condition de l’Étiquette. Reproduction : `CAPTURE_EXTRA_ARGS="--capture-loot --capture-loot-epic --capture-loot-choices --capture-loot-free --capture-loot-pointers --capture-size 1280x720" tools/capture_run.sh <dossier> 10 10 1280x720 42`. Aucun nouveau sprite ni animation.

### I12 — mesures et recette

Prendre une référence **avant I10a**, après les coffres exclusivement à objets et avec l’état exact de défense du jour (régénération récemment modifiée, DECISIONS §126). Comparer chaque lot à sa référence immédiate, même outil `tools/measure_run.sh`, graines **42,1002,7,123**, 1 800 s, mêmes personnages et paramètres. Profils neuf et catalogue ouvert comparés chacun à lui-même. Pas de mesures de performances sur machine chargée.

Compléter les rapports avec acquisitions/refus par ID, rang et source, budgets de déclenchement, soin effectif, temps du premier rang élevé et objets inopérants faute d’arme compatible. Les tests d’achat/échange et une partie humaine complètent le bot, qui ne juge pas tous ces choix. Mesurer notamment survie sans Dé, builds de statut à salves, Tabouret au plafond et boucles de soin.

Recette : `dotnet build` sans warning ; `tools/validate.sh <dossier neuf> smoke inventory objects catalogs quests saves choice_screen ui_art small_places` ; captures des effets et vraie run ; banc dense pour nouveaux effets fréquents. Roadmap cochée uniquement après livraison et vérification, jamais sur cette préparation.


#### I12 — recette économique du 12 octobre 2026

Les tarifs restent **20 / 45 / 90 / 45 Essence**, croissance de 8 % du prix initial par minute entière, devis figé à la première consultation. Les revenus des quatre profils neufs de référence atteignent 20 Essence à **96, 83, 76 et 65 s** (graines 42, 1002, 7, 123), médiane **79,5 s** : trois graines dans la cible 60–90 s, la quatrième six secondes au-delà. Ces observations ne justifient pas de réduire le tarif initial.

`tools/tests/ChestEconomyReplay.tscn` exécute les **vrais coffres, tirages, réservations, acquisitions et débits** sur les snapshots avant et après. Les gains proviennent des mêmes CSV mesurés, sans ajout d’Essence : premier coffre commun à 30 s, puis un coffre toutes les 90 s, vingt au total ; cycle commun/commun/rare/commun/lore/commun/rare/commun/épique/rare ; retour aux coffres fermés toutes les dix secondes. Sac normal de dix places conservé, surplus compté comme objet au sol. L’absence d’écran de choix dans ce banc sélectionne automatiquement la première alternative ; la navigation du Carnet est couverte séparément par les régressions et captures.

| Graine | Revenus rejoués | Avant : coffres ouverts / 20 | Après : ouverts / 20 | Essence dépensée après | Solde final après | Premier achat après | Récompenses après, sac + sol |
|---|---:|---:|---:|---:|---:|---:|---:|
| 42 | 879 | 20 | 14 | 818 | 61 | 100 s | 10 + 5 |
| 1002 | 2 749 | 20 | 20 | 1 678 | 1 071 | 90 s | 10 + 12 |
| 7 | 1 478 | 20 | 19 | 1 393 | 85 | 80 s | 10 + 10 |
| 123 | 898 | 20 | 15 | 880 | 18 | 70 s | 10 + 6 |

Avant : 80 coffres ouverts, zéro dépense, 88 récompenses (40 entrées dans le sac, 48 au sol). Après : **68 coffres ouverts sur 80**, 4 769 Essence dépensée sur 6 004 gagnée, **73 récompenses** (40 dans le sac, 33 au sol), douze coffres encore fermés faute de fonds. Les tentatives refusées répétées ne constituent pas des coffres distincts. Le calendrier de revisite explique l’écart entre fonds suffisants et achat effectif.

Les 73 récompenses après comprennent **21 communs, 22 inhabituels, 20 rares, 8 épiques et 2 légendaires**, soit 27 IDs distincts ; les trois épiques et les deux légendaires de départ sont effectivement apparus sur profil neuf. Premiers rangs épique/légendaire à 100 / 210 / 210 / 850 s. Les rangs des anciens objets n’existaient pas dans la référence et sont tracés `unrated` : ne pas comparer une distribution de raretés qui n’existait pas. Les probabilités sont vérifiées sur 3,2 millions de tirages, pas calibrées à partir de ces 73 récompenses.

Un second passage utilise les revenus réellement mesurés **après** pour éprouver les graines moins rémunératrices : **16 / 20 / 16 / 12 coffres** ouverts, pour 990 / 1 678 / 1 049 / 545 Essence dépensée (graines 42/1002/7/123). Les traces sont dans `/tmp/vestiges-economy-current-income-20261012/`. Le calendrier et les achats sont identiques ; seuls les revenus changent. Ce scénario confirme une pression budgétaire sur les runs faibles, sans démontrer une boucle complète achats → puissance → revenus. Tarif initial conservé à 20 Essence.

**Limites du replay :** les revenus restent figés après chaque acquisition ; il ne mesure donc pas comment les nouveaux objets changent les gains suivants. Ni les trajets, ni l’Effacement des coffres, ni les dépenses d’Atelier/Mémorial, ni la récupération des objets au sol ne sont simulés. Il valide les transactions et l’ordre de grandeur des tarifs, pas le plaisir humain ni une survie en partie. Les graines peu rémunératrices demandent des choix à long terme ; une partie humaine reste nécessaire pour apprécier ces arbitrages.

Preuves : `/tmp/vestiges-economy-before-20261012/seed-*/` et `/tmp/vestiges-economy-after-20261012/seed-*/` contiennent `attempts.csv` (coffre, coût, fonds avant/après, ouverture) et `items.csv` (seconde, source, ID, rang, destination). Journaux `/tmp/vestiges-economy-{before,after}-{42,1002,7,123}.log` ; builds des deux copies sans warning. Reproduction dans chaque snapshot préparé : `godot-mono --headless --path . --fixed-fps 60 res://tools/tests/ChestEconomyReplay.tscn -- --seed 42 --income <CSV-density-42.csv> --output <dossier>`.

#### I12 — mesures de run avant/après

Références terminées : profil neuf `/tmp/vestiges-items-baseline-isolated-20261012/`, profil développé graine 42 `/tmp/vestiges-items-baseline-dev-20261012/` et graines 1002/7/123 `/tmp/vestiges-items-baseline-dev-resume-20261012/`. Chaque profil utilise les graines 42/1002/7/123, 1 800 s, bot invincible, mêmes paramètres de déplacement, sans `--nomad` ni `--visit`. Le profil neuf de référence n’ouvre aucun coffre : ses compteurs « lieux visités » indiquent la proximité, pas un achat.

Snapshot après isolé et figé : `/tmp/vestiges-items-after-source-20261012/`, manifeste SHA256 `snapshot-manifest.json`. Les huit runs après sont terminées et **valides** (1 799 échantillons par run de 1 800 s), avec ces mêmes paramètres et deux processus au maximum ; résultats dans `/tmp/vestiges-items-after-normal-20261012/` et `/tmp/vestiges-items-after-dev-20261012/`.

Les quatre runs après sur profil neuf sont terminées et valides. Le seuil de 20 Essence est atteint à **101 / 77 / 77 / 61 s**, médiane **77 s**, ce qui confirme l’ordre de grandeur du premier prix malgré la variabilité entre graines.

| Profil neuf, graine | Éliminations avant → après | Niveau final avant → après | Objets reçus avant → après | Essence gagnée avant → après |
|---|---:|---:|---:|---:|
| 42 | 715 → 866 | 23 → 26 | 1 → 0 | 879 → 1 056 |
| 1002 | 1 988 → 1 964 | 32 → 32 | 2 → 3 | 2 749 → 2 483 |
| 7 | 1 254 → 949 | 26 → 25 | 4 → 1 | 1 480 → 1 116 |
| 123 | 712 → 503 | 23 → 19 | 0 → 0 | 898 → 585 |

Les huit runs de cette comparaison de profil neuf ne dépensent pas d’Essence dans des coffres. Les variations ne peuvent donc pas être présentées comme l’effet des achats : la graine 123 évolue sans recevoir aucun objet des deux côtés. Les tables d’amélioration des armes et les changements concurrents font également partie des snapshots. Les effets des objets et les transactions disposent de leurs propres régressions contrôlées.

Le profil développé termine également ses quatre runs valides ; aucun coffre acheté dans les huit runs avant/après de ce profil.

| Profil développé, graine | Éliminations avant → après | Niveau final avant → après | Objets reçus avant → après | Essence gagnée avant → après |
|---|---:|---:|---:|---:|
| 42 | 6 075 → 4 483 | 56 → 49 | 2 → 2 | 8 703 → 5 988 |
| 1002 | 10 242 → 12 500 | 77 → 83 | 5 → 4 | 13 723 → 16 982 |
| 7 | 19 948 → 21 686 | 106 → 111 | 7 → 3 | 27 699 → 29 504 |
| 123 | 15 665 → 10 869 | 102 → 81 | 4 → 4 | 21 625 → 14 978 |

Les validations JSON des deux dossiers après déclarent chacune quatre résultats valides ; les huit références sont aussi terminées. Synthèse des seize runs : `/tmp/vestiges-items-comparison-20261012.json`, produite par `/tmp/vestiges-summarize-item-measures.py`. Les acquisitions naturelles sont peu nombreuses (zéro à quatre objets après), ce qui limite la couverture des synergies par ce bot. Les augmentations sur certaines graines et diminutions sur d'autres n'isolent pas un effet propre au catalogue ; les suites contrôlées et le replay économique complètent donc cette observation.

**Comparaison descriptive, pas attribution causale exclusive aux objets :** le snapshot après inclut aussi des changements concurrents de faune et de présentation absents de la référence. Ces mesures ne produisent aucune conclusion de FPS et le bot invincible ne valide pas l’équilibrage de survie. Les soins effectifs, plafonds et builds spécialisés sont éprouvés par les régressions dédiées ; les objets acquis en run libre sont relevés sans forcer leur apparition.

#### I12 — recette fonctionnelle finale

La validation intégrée `/tmp/vestiges-items-integrated-final-20261012/` termine **15 suites sur 16** ; l'unique échec initial est une assertion UI exigeant encore une icône distincte par objet. Elle a été adaptée à la réutilisation des sprites existants puis `ui_art` repasse avec zéro échec (`/tmp/vestiges-i11-ui-art-final-20261012.log`). Les manifestes signalent des changements concurrents de présentation du menu et de ce test UI ; les résultats ne prétendent donc pas provenir d'un dossier demeuré intégralement inchangé. Build final partagé `dotnet build --no-restore` : **zéro warning, zéro erreur**, et `git diff --check` propre.

Les suites dédiées comprennent **172 contrôles combat**, **201 contrôles monde/loot** et **166 contrôles d'objets/statistiques** ; inventaire, armes, capacités ennemies, contrats, quêtes, sauvegardes, choix, petits lieux, profil dev et smoke sont verts. Les effets sont vérifiés avec de vraies acquisitions, attaques, soins et retraits ; les tests automatisés ne remplacent pas l'appréciation humaine du plaisir ou de la difficulté.

La capture `/tmp/vestiges-i11-item-combat-visible-20261012/` montre les nouveaux effets en vraie run à 1280×720 ; son journal confirme **15 PV effectivement soignés**, **deux projectiles annulés**, projectile hors portée préservé et ennemi effectivement détourné par le Portrait. Les captures monde et fiches 720p/1080p sont référencées en I11. Aucun nouveau sprite ni animation n'a été créé.

Le banc `/tmp/vestiges-i11-items-dense-20261012/` exécute quatre passes valides de 15 s, à 1280×720 et 1920×1080, avec et sans dash, **120 ennemis renouvelés, huit objets à trois copies et trois armes**. Il exerce les effets fréquents et leur recyclage sous charge. Les champs temporels sont conservés dans les JSON bruts, mais la machine exécutait d'autres travaux : **aucune conclusion de FPS ou comparaison de performance** n'en est tirée. Une session humaine et un banc sur machine calme restent les compléments nécessaires pour juger le fun et les performances cibles.
