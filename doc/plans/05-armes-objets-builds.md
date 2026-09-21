# Plan 05 — Armes, objets et construction des builds

Statut : **4 armes / 4 passifs et objets illimités validés ; spécifications proposées** · Priorité : P1 · Dépendances : tempo initial 03 ; rendu 02/08.
Références : V2 §10–13/17 ; [PROGRESSION-SYSTEM](../PROGRESSION-SYSTEM.md) sous réserve de ses conflits V2 ; [dossier](README.md).

## 1. Objectif et arbitrage préalable

Chaque arme doit avoir une raison d'être choisie. Chaque récompense doit améliorer ou transformer une intention de build. Les déblocages doivent élargir les possibilités sans rendre les premiers équipements inutiles.

**Décisions acquises :** quatre armes, quatre passifs, et **un système d’objets indépendant, sans limite de slots ni d’exemplaires cumulés fixée par le design**. Les objets ont des raretés et certains doivent être débloqués. Les quêtes donnent directement accès au contenu, sans passage obligatoire par les Souvenirs.

Le circuit armes/passifs au level-up reste la recommandation, à articuler avec l’XP plus lente de 03. La validation des huit slots ne tranche pas à elle seule toutes les sources de récompenses ; documenter le choix retenu dans V2 §13.

## 2. Phase 0 — Existant vérifié

Sources : [weapons.json](../../data/weapons/weapons.json), [perks.json](../../data/perks/perks.json), [passifs](../../data/progression/passive_souvenirs.json), [fusions](../../data/progression/fusions.json), [FragmentManager.cs](../../scripts/Progression/FragmentManager.cs), [Player.cs](../../scripts/Core/Player.cs), [PerkManager.cs](../../scripts/Progression/PerkManager.cs), [CursedItemManager.cs](../../scripts/Progression/CursedItemManager.cs).

- 24 armes ; 64 perks réels, dont huit synergies et trois passifs personnages ; 13 passifs de run ; cinq fusions ; trois malédictions.
- Quatre armes sont déjà conditionnées par `RequiresSouvenir`. Des quêtes récompensent ces Souvenirs : les déblocages indirects existent.
- `Player.ResolveWeaponLoot` filtre les Souvenirs requis mais pas source/tier/drop_condition ; `FragmentManager` filtre tier/Souvenir mais pas source. Les restrictions déclarées ne sont donc pas homogènes.
- `ResolvePerkLoot` tire dans tous les perks, tandis que `PerkManager.PickRandomPerks` applique des filtres. À unifier.
- Fusions : `ApplyFusion` existe, mais aucun appel trouvé ; l'ouverture du coffre journalise seulement la proposition. `cable_whip` est une référence invalide et `kill_restore_day` est obsolète V2.
- Plusieurs effets de fusion ne sont pas raccordés aux branches d'exécution trouvées ; certains bénéfices de malédictions n'ont pas de consommateur trouvé. Auditer avant d'afficher leurs promesses.
- `WeaponInstance.GetStat` retourne la valeur de base au niveau 1 avant d'appliquer la rareté : les multiplicateurs sont ignorés sur une arme neuve. Le modèle `WeaponRarityData` porte seulement des multiplicateurs ; les effets spécifiques à chaque rareté promis par V2 §10 restent à concevoir.

APIs à reprendre : `WeaponDataLoader.Get/GetAll/GetDefaultForCharacter`, `WeaponInstance(WeaponData, string rarity = "common")`, `GetStat(string, float)`, `CloneWithRarity(string)`, `Player.AddWeapon`, `AddOrUpgradePassive`, `FragmentManager.SelectFragment(string, string)`, `FusionDataLoader.FindFusion(string, string)`, `LootResolver.Roll(string, int)`.

## 3. Trois piliers de build et contrat d’objets

| Catégorie visible | Fonction | Capacité / acquisition |
|---|---|---|
| Armes | Manière d’attaquer | Quatre slots, départ/niveau/loot selon disponibilité |
| Passifs | Axes de spécialisation et combinaisons | Quatre slots, niveaux bornés comme système actuel |
| Objets | Statistiques et règles qui enrichissent le build | Aucun plafond de slots ou d’exemplaires ; trouvés dans le monde |
| Objets maudits | Sous-famille d’objets avec risque/avantage | Prise volontaire, cumul et contrepartie annoncés |
| Souvenirs | Histoire et évolution narrative du Hub | Collection distincte, aucun prérequis des déblocages de combat |
| Fusions | Transformation d’une combinaison | Remplacent les éléments prévus par leur recette |

Les anciens « perks » sont à trier dans les trois piliers, les passifs de personnage et les synergies. Ne pas imposer un quatrième vocabulaire concurrent dans l’interface. Un effet migré ne reste pas dans deux pools avec double application.

### Modèle proposé, non encore disponible

- Une définition JSON par objet : ID stable, nom, icône, rareté, tags, sources pondérées, disponible initialement ou récompense de quête, déclencheur, formule de cumul, paramètres, description et feedback.
- Rareté **fixe par définition** proposée (Commun/Inhabituel/Rare/Épique/Légendaire), avec formes/icônes en plus des couleurs. Pas de même objet tiré dans cinq raretés au premier lot ; cette variante compliquerait inutilement la lecture des piles.
- Inventaire de run agrégé par ID → nombre d’exemplaires. Ramasser un doublon augmente la pile, sans remplacer une arme, un passif ou un autre objet. Tout le butin de run disparaît à la fin ; l’accès au loot reste débloqué en méta.
- Chaque exemplaire ajoute une valeur réelle documentée : quantité, puissance, durée ou autre effet. Les probabilités restent mathématiquement cohérentes ; si une chance atteint 100 %, choisir explicitement une conversion d’excédent en puissance/quantité plutôt qu’un doublon inutile. Les trois prototypes initiaux évitent cette difficulté en renforçant la puissance.
- Pas de node, timer ou abonnement par exemplaire : effets compilés à acquisition, déclencheurs indexés par type d’événement, calcul agrégé et VFX mutualisés. Une pile de 1 000 objets ne crée pas 1 000 objets Godot.
- Les limites de présentation et de fréquence d’une mécanique ne plafonnent pas le nombre d’exemplaires. Une cadence fixe peut rester fixe si chaque exemplaire améliore la puissance du déclenchement ; l’effet total est indiqué à la sélection.
- Définir origine des dégâts et identifiant de chaîne pour empêcher les procs récursifs sans fin. Ne pas tronquer silencieusement les dégâts calculés au budget de particules. Tester les grandes valeurs numériques ; « illimité » exprime l’absence de limite de design, pas une mémoire physique infinie.
- Collection depuis le menu principal : icône/rareté/état ; au focus, effet de base et règle de cumul. En pause/bilan, montrer nombre possédé et effet total, liste défilante/groupée.

Distinguer tier, rareté d’arme, niveau et déblocage ; les raretés d’objets constituent un contrat séparé. Réutiliser palettes et loaders, pas le modèle d’arme mutable sans adaptation.

**Raretés d’armes, décision encore ouverte :** proposition en deux sous-lots, multiplicateurs fiables puis effets mineurs/notables/majeurs/uniques et reforge prévus par V2. La roadmap reste ouverte si le second sous-lot est reporté.

## 4. Inventaire de départ et tri proposé

Sources et effets ci-dessous : déclarations actuelles, pas verdict d'équilibrage. Aucun retrait définitif proposé sans comparaison.

| ID | Source actuelle | Rôle déclaré | Action proposée |
|---|---|---|---|
| chipped_blade | default | Arc rapide de proximité | Référence polyvalente |
| heavy_hammer | default | Lent, recul fort | Référence impact/contrôle |
| makeshift_bow | default | Tir linéaire | Référence distance |
| sling | world | Trois projectiles dispersés | Comparer aux haches |
| sharpened_pipe | world | Estoc court rapide | Valoriser placement précis |
| crossbow | world | Tir perforant lent | Distinguer de l'arc |
| cleaver | world | Arc large | Distinguer de lame/marteau |
| whip | world | Couverture circulaire | Réparer référence fusion |
| throwing_axes | world | Deux projectiles en éventail | Distinguer de la fronde |
| nail_mace | world | Dégâts périodiques | Valider effet et lisibilité |
| teachers_bell | loot_epic | Onde, recul, ralentissement | Arme identitaire prioritaire |
| surgeons_scalpel | loot_epic | Soin tous les cinq impacts | Valider rythme/soin |
| lighthouse_shard | loot_epic | Rayon fortement perforant | Maîtriser couverture |
| music_box | loot_epic | Notes orbitales | Identité de positionnement |
| chain_of_names | loot_epic | Sauts entre cibles | Tester densité et plafonds |
| compass_needle | loot_epic | Projectile guidé | Donner une contrepartie claire |
| photographers_flash | loot_epic | Désorientation | Vérifier effet réel |
| essence_staff | world + Souvenir | Tir guidé consommant Essence | Clarifier coût d'usage |
| void_edge | world + Souvenir | Désintégration | Vérifier règles élites/boss |
| memory_lantern | world + Souvenir | Feu au sol | Réécrire description nuit |
| echo_gauntlets | world + Souvenir | Écho retardé | Distinguer arme et objet d'écho |
| childs_drawing | loot_legendary | Formes/zone variables | Préserver singularité et lecture |
| last_broadcast | loot_legendary | Cône maintenu | Remplacer condition boss/nuit legacy |
| clock_hand | loot_legendary | Ralentissement local | Limiter cumul sur boss |

Fiche à produire pour chacune : rôle, faiblesse, portée/couverture, cadence, cibles, effet, coût, synergies, accès méta, sources en run, signal visuel/son, comportement contre boss. Verdict autorisé : conserver, différencier, corriger ou réserver ; fusion/suppression seulement après décision.

## 5. Six objets proposés, trois pour le premier prototype

Noms, raretés et valeurs sont des propositions. n est le nombre d’exemplaires ; aucune ligne ne comporte de plafond de pile. Tester n = 1, 2, 10, 100 et 1 000 sans supposer ces quantités fréquentes en jeu normal.

| Objet | Rareté proposée | Déclenchement / effet à n exemplaires | Accès proposé |
|---|---|---|---|
| Éclat de Rémanence | Rare | Tous les six impacts directs, écho pour 30 % × n du coup ; l’écho ne redéclenche pas de proc | Initial |
| Semelle du Nomade | Commun | Après 3 s de déplacement continu, prochain coup direct +15 % × n ; recharge par déplacement ; arrêt >0,5 s annule | Initial |
| Graine d’Ancrage | Inhabituel | Trois éliminations directes en 2 s donnent bouclier 5 × n PV pendant 3 s, déclenchement au plus toutes les 8 s | Initial |
| Photographie Fendue | Rare | Élimination en zone effilochée : +n Essence | Quête de risque, directement |
| Fil des Noms | Épique | Mort d’une cible marquée : onde de puissance 20 % × n du dégât de référence ; pas de chaîne récursive | Quête de maîtrise ; marquage à valider |
| Sceau de l’Oubli | Légendaire maudit | Reprendre un objet maudit existant avec gain et contrepartie monotones par pile ; coefficients à mesurer | Quête de défi ; pas avant raccordement réel des effets |

Le bouclier temporaire ne s’empile pas sur lui-même à chaque déclenchement, mais sa valeur augmente avec tous les exemplaires. Les Gantelets d’Écho et l’Éclat doivent rester distincts (arme autonome contre modulation d’impacts). Les objets de mobilité spécifiques ne supposent pas un dash déjà implémenté : dépendance 01.

Définir attribution, ordre des effets, cible morte, dégâts secondaires, pause/mort et interactions boss. Chaque objet doit tenir en une phrase courte en jeu ; sa formule détaillée est accessible au focus.

## 6. Lots d'action

### Lot A — Contrat des catégories et de disponibilité

1. Valider modèle de level-up et lexique, puis aligner V2 et note de progression.
2. Reprendre le cache des loaders et les tables pondérées de `LootResolver`.
3. Définir une politique commune d'éligibilité : type de source, statut méta, contexte de run, slots, doublons, bans et rang.
4. Remplir la fiche des 24 armes et auditer les 64 perks : effet vivant, legacy, personnage, cumul, synergie.
5. Migrer les quatre anciens accès par Souvenir vers des déblocages explicites selon 06. Préserver tous les droits acquis et tester un profil neuf sans fragment de lore.

**Vérification :** même contenu bloqué dans tous les chemins génériques ; source garantie respectée ; aucun perk interdit proposé.
**Garde-fou :** ne pas déduire les règles de loot d'un simple nom « épique » ; pas de perte d'accès sur anciennes sauvegardes.

### Lot B — Trois armes et trois objets de référence

1. Reprendre `WeaponInstance` et les patterns existants pour lame, arc, marteau.
2. Créer définitions, inventaire agrégé et effets prototypes distincts des armes/passifs, avec compteurs sans limite de design, raretés et attribution explicite. Reprendre les loaders et modificateurs disponibles ; les nouveaux contrats d’objet restent à créer.
3. Relier chaque effet à un feedback 02 et à une description 04.
4. Tester deux builds contrastés et une combinaison atypique ; comparer à absence d'objet.
5. Mesurer fréquence réelle des déclenchements et coût sous forte densité ; comparer piles 1/10/100/1 000 et nombreux types distincts. Vérifier que chaque doublon améliore l’effet et que le budget visuel ne réduit pas le calcul de récompense.

**Vérification :** effet observé = description ; contribution perceptible ; pas de boucle de procs ni d'effet persistant après run.
**Garde-fou :** ne pas ajouter tout le catalogue d'objets avant validation de ces trois-là.

### Lot C — Fusions, raretés et malédictions fiables

1. Réparer les IDs et remplacer les effets V1 par des effets V2 approuvés.
2. Relier `FusionAvailable` à une offre effective au coffre ; revalider les ingrédients au moment du choix.
3. Appliquer consommation/remplacement/récompense atomiquement ; refuser proprement un état invalide.
4. Corriger le chemin niveau 1, puis vérifier la matrice niveau 1/amélioré × Commun/Rare/Légendaire et le passage par l'Autel ; ne pas confondre rareté et tier.
5. Raccorder et tester les bénéfices/contreparties des trois malédictions avant de les proposer.
6. Dans le second sous-lot approuvé, définir les familles d'effets de rareté, leur puissance, leurs armes éligibles et exclusions ; reprendre les déclenchements bornés du prototype d'objets. Fixer dans les données quel effet est ajouté ou renforcé à chaque palier.
7. Conserver les effets tirés dans l'instance lors d'un drop, échange ou changement de rareté ; la reforge est le seul reroll volontaire prévu ici. Relier coût, aperçu et résultat effectif à l'Autel et vérifier l'absence de paiement sans résultat.

**Vérification :** une fusion réalisable de bout en bout ; annulation sans perte ; effet final réel ; bonus de loot/vitesse vérifiés ; rareté active dès niveau 1 ; effets/renforcements/reforge conformes aux paliers approuvés, sans double cumul après échange.
**Garde-fou :** pas de consommation d'arme avant validation complète ; pas de bonus affiché mais inactif.

### Lot D — Extension contrôlée

1. Valider une famille d'armes/objets à la fois selon les fiches et retours.
2. Raccorder son objectif d'accès de 06, sa fiche Collection de 04 et ses assets de 08.
3. Régler pondérations et coûts à partir de plusieurs runs et seeds.
4. Réviser les candidats trop similaires ; proposer regroupement/retrait seulement avec justification observable.

**Vérification :** nouvelles options utiles sans rendre le départ médiocre ; offre compréhensible même slots pleins.
**Garde-fou :** le nombre de définitions n'est pas le critère de réussite.

## 7. Recette et référence Megabonk

Tester profil neuf/avancé, quatre slots armes/passifs pleins mais nouveaux objets toujours acquis, grandes piles, reroll, bannissement, offres vides, fusion, changements de scène, sauvegarde et interactions de procs. Build, smoke si applicable et combat dense.

Megabonk met officiellement en avant armes, personnages et objets à synergies : [page Steam](https://store.steampowered.com/app/3405340/Megabonk/). Les mécanismes de conditions d'accès et d'effets combinables sont décrits par son [wiki communautaire sur les objets](https://megabonk.wiki/wiki/Items) ; source indicative consultée via résultats indexés, accès direct refusé pendant cet audit. Aucun nombre de contenus ni réglage du jeu de référence n'est une cible pour Vestiges.

Roadmap B/E/G et validation de puissance de A/C. Acceptation : chaque choix du lot a une utilité explicable, les synergies se ressentent, les récompenses promises fonctionnent et les accès sont cohérents.
