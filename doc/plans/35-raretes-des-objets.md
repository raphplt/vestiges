# Plan 35 — I10 : rareté fixe des objets du sac

11 octobre 2026. Suite de [I9 : coffres exclusivement à objets](35-inventaire-du-joueur.md#18-lot-i9--coffres-réservés-aux-objets-du-sac-11-octobre-2026). Demande de Raphaël : rareté fixe par objet, fréquence liée à cette rareté, classement cohérent avec la puissance.

**Statut : préparation, aucun changement de gameplay livré par ce document.** Le principe est acquis ; le classement et les nombres ci-dessous sont la proposition de départ à éprouver. Les 22 objets actifs sont couverts. Les deux fiches désactivées ne sont pas réactivées.

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

## 1. Règle de jeu

- Un objet possède **une seule rareté de catalogue**, identique dans toutes les runs et pour tous ses exemplaires. Un Dé à coudre épique reste épique, même sorti d’un coffre commun.
- Cinq rangs existants : **Commun** (gris), **Inhabituel** (vert), **Rare** (bleu), **Épique** (violet), **Légendaire** (or). `lore` est la couleur d’un lieu, jamais une rareté d’objet.
- La rareté reflète l’effet existant, son utilité réelle, ses conditions et son potentiel avec deux exemplaires. Elle n’ajoute **aucun multiplicateur automatique** aux dégâts, soins ou autres valeurs : les effets puissants sont classés plus haut.
- Commun : effet modeste, fortement conditionné ou outil de prise de risque. Inhabituel : brique de synergie ou bonus conditionnel fiable. Rare : apport polyvalent ou changement de mobilité. Épique : moteur de build, multiplication des dégâts ou survie soutenue. Légendaire : règle exceptionnelle de survie ou de construction du build.
- Une bonne synergie commune peut battre un objet épique mal adapté. La rareté ne promet pas un classement absolu de toutes les situations.
- Exemplaires, plafonds, paliers du doublon, quêtes et sac de 10–25 places conservés. Les améliorations d’armes et d’Écussons gardent leur propre système de rareté.

## 2. Classement proposé des 22 objets

Valeurs pour un exemplaire, hors autres bonus ; aucun rééquilibrage numérique implicite. Les raisons prennent aussi en compte le doublon actuel.

| Rareté | Objet (`id`) | Effet déterminant et raison du classement |
|---|---|---|
| Commun | Boîte de pansements (`boite_de_pansements`) | Soin de 3 % des PV max au niveau gagné ; ponctuel. L’invulnérabilité du doublon exige une cascade de trois niveaux. |
| Commun | Semelle usée (`semelle_usee`) | +60 % sur une attaque après 2 s de marche ; pas +60 % de DPS permanent. Deux attaques au doublon. |
| Commun | Chewing-gum (`chewing_gum`) | Contrôle local sur le trajet du dash ; demande de placer la traînée. Fragilité au doublon, limitée à cette zone. |
| Commun | Gilet réfléchissant (`gilet_reflechissant`) | +2 % de dégâts par ennemi très proche, maximum +20 % avec un exemplaire ; exige l’exposition au danger. Surveiller le doublon (20 ennemis). |
| Commun | Montre arrêtée (`montre_arretee`) | Ralentissement de 75 % pendant 2 s après la mort d’un Souverain. Rarement utile, et après la menace principale ; sa mise en scène ne justifie pas un rang élevé. |
| Commun | Sifflet d’arbitre (`sifflet_d_arbitre`) | +1 Péril par exemplaire : outil volontaire de difficulté/score, pas une hausse directe de puissance. Commun ne signifie pas bénéfique dans tous les builds. |
| Inhabituel | Allumette humide (`allumette_humide`) | Apporte la Brûlure et ouvre des synergies ; propagation au doublon. Proc dépendant de l’arme. |
| Inhabituel | Glaçon dans un mouchoir (`glacon`) | Apporte le ralentissement de 40 %, puis le gel au doublon. Bon soutien, sans dégâts propres. |
| Inhabituel | Thermomètre (`thermometre`) | +40 % contre les cibles brûlées ; exige une source de Brûlure. |
| Inhabituel | Épingle à nourrice (`epingle_a_nourrice`) | +40 % contre les cibles entravées ; dépend du contrôle disponible. |
| Inhabituel | Tabouret de camping (`tabouret_de_camping`) | +30 % de cadence après 1 s immobile ; rester en place est une contrainte forte. Les +10 d’armure au doublon sont un point de vigilance. |
| Inhabituel | Thermos (`thermos`) | +20 % de dégâts au-dessus de 90 % des PV ; perd son intérêt quand le joueur encaisse. Seuil plus permissif au doublon. |
| Inhabituel | Médaille cabossée (`medaille_cabossee`) | +30 % de dégâts sous 35 % des PV ; puissance contre risque réel. |
| Rare | Plan de métro (`plan_de_metro`) | Change le dash en déplacement instantané ; esquive et repositionnement polyvalents, sans traversée des murs ni invulnérabilité ajoutée. |
| Rare | Flacon de pharmacie (`flacon_de_pharmacie`) | Poison cumulable sur 3 s ; apport offensif général, mais dégâts retardés et coefficient de l’arme. |
| Rare | Porte-monnaie usé (`porte_monnaie_use`) | Jusqu’à +30 % de dégâts avec l’Essence conservée ; remboursement de 20 % au doublon, moteur économique en plus du combat. |
| Épique | Dé à coudre (`de_a_coudre`) | 1 PV par élimination d’arme, sans délai propre : le soin croît avec la foule abattue. Bien plus fort en run dense que son apparence ne le suggère. |
| Épique | Pétard mouillé (`petard_mouille`) | Explosion à chaque élimination d’arme, à 100 % du coup fatal ; au doublon, coefficient accru et deux explosions. Très fort pour nettoyer une foule. |
| Épique | Loupe de philatéliste (`loupe_de_philateliste`) | Les critiques rendent la cible Fragile : +50 % de dégâts de toutes les armes pendant 2 s. Peut devenir quasi permanent dans un build critique. |
| Épique | Stylo à quatre couleurs (`stylo_quatre_couleurs`) | Un critique inflige 60 % de ses dégâts à une autre cible ; au doublon, deux cibles et coefficient accru. Forte multiplication avec les critiques. |
| Légendaire | Ticket de consigne (`ticket_de_consigne`) | Annule une mort, rend 50 % des PV et protège 2 s ; consommé, deux exemplaires au plus. Ne sauve pas du Néant. |
| Légendaire | Trèfle plastifié (`trefle_plastifie`) | Une quatrième carte à chaque level-up : améliore durablement tous les choix suivants. Un seul exemplaire. |

**Répartition : 6 communs, 7 inhabituels, 3 rares, 4 épiques, 2 légendaires.**

Réserve de départ inchangée : Semelle, Chewing-gum et Montre communs ; Allumette et Glaçon inhabituels ; Plan et Flacon rares ; Dé épique. Les légendaires restent derrière leurs quêtes. « Disponible au départ » signifie éligible, pas fréquent ni présent dans le sac au lancement.

Classements à surveiller après mesure : Dé (soutien vital d’un profil neuf), Tabouret et Gilet à deux exemplaires, Flacon sur les armes à nombreux impacts. La Montre faible devra être retravaillée séparément si elle devient un objet systématiquement refusé ; une rareté ne corrige pas un effet sans intérêt.

## 3. Obtention : table de départ proposée

Pour chaque récompense d’objet : **filtrer les objets accessibles et cumulables → tirer une rareté parmi les rangs disponibles → tirer uniformément un objet de cette rareté**. Le sac plein ne filtre pas l’objet : il déclenche le dépôt au sol et l’échange.

Pourcentages par **récompense d’objet**, avec tous les rangs disponibles. Ce ne sont pas des pourcentages par objet individuel.

| Source | Commun | Inhabituel | Rare | Épique | Légendaire | Quantité |
|---|---:|---:|---:|---:|---:|---|
| Coffre commun / source générique historique | 60 % | 25 % | 11 % | 3,5 % | 0,5 % | 1 par tirage déclaré |
| Coffre rare | 0 % | 55 % | 32 % | 11 % | 2 % | 1 |
| Coffre épique | 0 % | 0 % | 70 % | 25 % | 5 % | 2 tirages |
| Ancien coffre de lore | 0 % | 55 % | 32 % | 11 % | 2 % | 1 ; même table que le rare |
| Objet d’élite | 35 % | 35 % | 22 % | 7 % | 1 % | Chance de lâcher un objet conservée : 25 % |
| Souverain | 0 % | 0 % | 65 % | 30 % | 5 % | 1 garanti |
| Atelier | 30 % | 40 % | 20 % | 9 % | 1 % | 1 offre, mémorisée à la première visite |

Ainsi, un épique donne au moins deux objets rares tant qu’il en existe d’éligibles ; il ne garantit pas un objet épique. Deux tirages à 5 % donnent **9,75 %** de chance d’au moins un légendaire si le groupe reste éligible aux deux tirages. Une élite donne un légendaire avec **0,25 %** de probabilité avant exclusions (25 % × 1 %).

**Exclusions sans impasse :** supprimer les rangs vides et renormaliser leurs poids restants. Si aucun rang de poids positif n’est accessible, descendre au rang éligible le plus élevé sous le minimum normalement garanti. Si aucun objet n’est éligible, aucun tirage possible : ne pas inventer d’Essence de remplacement. Valider que les profils normaux ne rencontrent pas cet état avec leurs limites de sac. Recalculer l’éligibilité et les exemplaires promis à chaque récompense d’un coffre multiple.

Exemple de profil neuf dans un coffre commun : aucun légendaire accessible, donc 60/99,5, 25/99,5, 11/99,5 et 3,5/99,5, soit environ **60,30 / 25,13 / 11,06 / 3,52 %**. Le Dé est le seul épique initial : il ne doit jamais ouvrir artificiellement l’accès aux légendaires.

La fréquence d’un objet précis vaut la fréquence effective de son rang divisée par le nombre d’objets éligibles de ce rang. Elle évolue avec les déblocages et les plafonds ; le taux de la catégorie reste stable quand son catalogue s’agrandit.

**Chance et Péril :** aucun multiplicateur supplémentaire dans cette V1. Le code actuel ne fait intervenir Chance ni dans le tirage d’objets ni dans les groupes de coffres. Une future influence de Chance devra modifier les chances de tirer un rang, jamais transformer l’objet après tirage. Ne pas détourner les règles de rareté du level-up.

**Atelier :** prix de base proposé par rareté : **120 / 150 / 210 / 360 / 600 Essence**, puis croissance existante de l’Atelier. Prix et objet figés selon les règles actuelles d’offre ; rouvrir ou annuler un échange ne permet pas de relancer le tirage. Ces prix restent à mesurer avec l’économie actuelle.

## 4. Affichage attendu

La rareté de l’objet doit être identique dans la Collection, le sac/HUD, la pause, le bilan, au sol, à l’Atelier et dans l’échange. Nom traduit et signe de rareté en plus de la couleur ; ne pas teinter le dessin entier au point de perdre son identité.

Dans un coffre : **le titre et l’image représentent le coffre ; chaque carte représente la rareté propre de l’objet**. La roulette utilise les candidats réellement accessibles à cette source, avec leurs vraies couleurs ; aucun légendaire impossible affiché comme promesse. Le survol/détail explique l’effet et le palier en exemplaires.

## 5. Lots d’implémentation, un à la fois

### I10a — données et contrat

- Lire les références du §0. Ajouter un champ `rarity` aux objets actifs du catalogue selon §2, et sa représentation dans `PassiveSouvenirData`/`ParseEntry`. Ne pas ajouter de rareté fixe aux Écussons.
- Étendre la liste fermée et la validation de `ObjectDataValidator` : champ exigé pour les objets actifs, cinq valeurs seules ; `lore`, faute de frappe et rareté sur un Écusson refusées. Les objets désactivés restent exclus.
- Créer une configuration dédiée aux poids par source et prix d’Atelier, avec le pattern de chargement strict de `InventoryConfig`/`JsonConfigReader` : nombres finis, poids non négatifs, somme positive, sources et rangs connus, prix positifs. Ne pas modifier `upgrade_rarities.json`.
- **Vérifier :** catalogue 22/16 inchangé, classement complet, quêtes et anciennes sauvegardes compatibles ; tests `catalogs`, `inventory`, `quests`, `saves`. Pas de réinitialisation des déblocages, la rareté se lit depuis l’ID du catalogue.

### I10b — tirage commun et sources

- Reprendre `PickUnlockedItem` et son filtrage ; ajouter un contexte explicite de source (nouvelle API), puis le tirage en deux étapes du §3. Tous les nombres viennent de la configuration.
- Raccorder coffres, élites, Souverains, Ateliers et POI qui réutilisent une table de coffre. Transmettre la rareté effective du coffre après un éventuel Oubli de rétrogradation. Préserver les flux `RunRandom.Loot`, `item_drops` et celui de l’Atelier ; aucun `GD.Rand*` pour le gameplay.
- Conserver `promisedItems`, les limites par objet, le dépôt au sol et l’offre mémorisée. Appliquer le prix proposé via les règles d’achat existantes.
- **Vérifier :** probabilités par source sur échantillon déterministe (100 000 tirages, sans cumul pour le test statistique), exclusions de quêtes, rang vide, rang minimum indisponible, sac plein, plafond d’un objet à une copie, deux récompenses et annulation d’achat. Contrôler les proportions avec une tolérance statistique explicite, pas l’apparition fortuite d’un légendaire en vraie run.

### I10c — présentation cohérente

- Réutiliser `RarityPalette`, `RarityArt`, `RarityIcon` et les cadres existants, selon §4. Points d’entrée : `ChestLootScreen.CreateSlot/RevealSlot`, `ItemPickup`, `ItemExchange.Open`, `WorkshopDirector.AddItemOffer`, `HudInventoryBar.FillItem`, `PauseMenu`, `HubCollectionPanel`. Vérifier la branche de sac de `RunSummaryPanels` avant édition.
- La couleur et le signe des cartes ne doivent plus reprendre aveuglément la rareté du coffre. Ne pas confondre les trois notions : objet fixe, coffre/source, amélioration de level-up.
- **Vérifier :** suites `choice_screen`, `ui_art`, `inventory` ; captures 720p et 1080p de la Collection, du sac/échange et d’un coffre donnant des objets de rangs différents. Contrôle à la manette.

### I10d — mesures et recette finale

- Avant I10b, enregistrer la référence **après I9**, avec `tools/measure_run.sh` ; pas les anciennes mesures de coffres à Essence/XP. Même outil, mêmes graines **42, 1002, 7, 123**, mêmes builds/personnages et mêmes paramètres avant/après, 1 800 s par run.
- Profils : neuf (`MEASURE_PROFILE=normal`) et catalogue ouvert (profil dev). Nommer explicitement leurs différences de Campement/armes ; comparer chaque profil à lui-même, pas leurs scores entre eux.
- Ajouter si nécessaire aux rapports les objets proposés/acquis/refusés par source, rareté et ID, temps du premier rare/épique/légendaire, doublons et sac plein. La mesure actuelle ne renseigne pas encore tous ces champs. Le bot ne juge pas la valeur d’un échange ni celle d’un achat : les tests déterministes et une partie jouée complètent les runs.
- Mesurer dégâts des objets, soin effectif du Dé, survie et effet des doublons sur builds dédiés. Ne pas rendre un objet très puissant « équilibré » uniquement parce qu’il est rare ; ajuster ses valeurs en données si la mesure le demande, avec avant/après. Toute promotion/dégradation de rang se consigne.
- **Recette :** build sans warning ; `tools/validate.sh <dossier neuf> smoke inventory objects catalogs quests saves choice_screen ui_art small_places` ; captures du §I10c et vraie run. Cocher la roadmap uniquement pour les lots effectivement implémentés et vérifiés.

## 6. Critères d’acceptation

- Les 22 objets ont une rareté fixe visible et persistante par leur identité ; tous leurs exemplaires sont identiques à nombre de copies égal.
- Aucun objet verrouillé, désactivé ou au maximum ne sort ; aucun Écusson ne se glisse dans le sac.
- Les sources utilisent les poids documentés, leur rang minimum et les mêmes règles de secours ; aucune perte d’objet ni conversion en autre butin quand le sac est plein.
- Aucun multiplicateur de puissance ajouté par l’étiquette de rareté ; les écarts de force justifient le classement, les valeurs discutables sont mesurées.
- La rareté d’un coffre ne recolore plus artificiellement un objet. Une icône et un nom rendent le rang lisible sans dépendre de la couleur seule.
