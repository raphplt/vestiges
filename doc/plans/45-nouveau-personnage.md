# Plan 45 — Préparer le prochain personnage jouable

Version 0.3 · 13 octobre 2026 · **Implémentation autorisée : Scaphandrière ; N0–N3 livrés techniquement ; catalogue N4 et validation en run N5 à poursuivre.**

Demande initiale de Raphaël : « préparer l'implémentation d'un nouveau personnage en jeu », avec tous les sprites nécessaires. Après proposition de la Scaphandrière et de l'adaptation de sa réserve d'air : **« okok go alors »**. Cet accord autorise l'implémentation des lots N0–N5 ; il n'est pas une validation artistique finale ni une mesure de l'équilibrage. Le kit complet avait déjà été demandé dans **DECISIONS §90**.

Les choix chiffrés ci-dessous sont ceux de l'agent pour le premier essai, pas des chiffres dictés par Raphaël.

Rattachement : [plan 06, intégrations P2/P3](06-personnages-quetes-defis.md#11-lot-p2--le-facteur-personnage-complet--10-octobre-2026), [plan 32, sprites](32-heros.md), [Stratégie V2 §25](../VESTIGES-STRATEGIE-V2.md#25-phases-de-développement). Les lots ci-dessous sont séquentiels : terminer et vérifier un lot avant le suivant.

## 0. Découverte documentaire — état initial avant implémentation

### Sources à relire au démarrage de l'implémentation

| Source | Ce qu'elle fixe |
|---|---|
| Stratégie V2, amendement et §§2–3, 25 | Boucle nomade, exclusions V1, suivi de livraison |
| `doc/CHARTE-GRAPHIQUE.md`, §§1, 4–7 | Lisibilité, palette, contours, animation et nommage |
| `doc/VESTIGES-LORE.md:130` | Scaphandrière, Solange Delmas, vingt-neuf corps remontés, recherche d'Anne et Marc |
| `doc/plans/06-fiches-casting.md`, fiche 6 | Identité, harpon, réserve d'air, saut flottant ; chiffres d'essai |
| `doc/plans/32-heros.md`, §§6 et 9 | Modèle produit, gabarit, pivot, 216 images ; validation artistique ouverte |
| `doc/plans/06-personnages-quetes-defis.md`, §§11–12 | Exemples complets du Facteur et de l'Éveillée, limites encore ouvertes |
| `doc/plans/DECISIONS.md`, §§129 ter, 155, 159 | Traqueur initial, retrait du score, projectiles petits et finis, contour léger |
| `doc/VALIDATION.md` | Profils temporaires, build/import communs, suites et captures |

### Audit initial avant N0 — ne pas utiliser comme contrat d’intégration courant

- `data/characters/characters.json` contient **cinq** jouables : Traqueur, Vagabond, Forgeuse, Facteur, Éveillée. L'état initial du plan 32, qui parle encore de trois jouables, est historique.
- `tools/sprites/characters/scaphandriere.py` contient déjà le modèle complet : cadre **42×44**, pivot **(21, 40)** ; décalage des pieds au branchement **18 px**.
- `assets/characters/scaphandriere/` contient **216 PNG et leurs 216 imports**. Audit de préparation : RGBA, dimensions uniformes, alpha binaire, aucune image vide. Leur présence ne prouve pas le rendu en run ni l'approbation artistique.
- Les animations comprennent 8 directions × (6 repos + 8 marche + 4 dash + 3 blessure + 6 mort). Il ne faut donc pas annoncer 216 images entièrement manquantes pour cette candidate.
- Le dossier ne contient **pas de portrait**. Les cinq jouables utilisent `portrait.webp`, actuellement en 864×864.
- Le **Fusil-harpon n'existe pas** dans le catalogue d'armes ni dans les modèles d'icônes/projectiles. Il demande un vrai lot de combat et d'assets.
- La quête `raise_the_bodies`, « Remonter les corps », existe : **290 éliminations en zone effilochée ou effacée dans une run** ; sa récompense nomme déjà `scaphandriere`. Ne pas créer une deuxième quête ni reprendre l'ancienne proposition « boss + terrain ».

**Écart de documentation visuelle constaté avant N0 (corrigé depuis) :** la Charte décrivait encore 4 directions et moins d'images, alors que le runtime et les héros livrés utilisent 8 directions et 27 images par direction. Ses petites dimensions sont annoncées comme des bounding boxes, pas nécessairement des cadres d'export. En N0, consigner précisément l'amendement des plans 25/32 dans la Charte et mesurer l'emprise visible ; ne pas réduire arbitrairement les sprites ni prétendre que les textes concordent déjà.

### APIs et modèles réellement disponibles

| Point d'entrée existant | Usage prévu / exemple à reprendre |
|---|---|
| `CharacterDataLoader.Load()`, `CharacterData` (`scripts/Infrastructure/CharacterDataLoader.cs`) | Copier la structure de la fiche du Facteur : `sprite_folder`, `sprite_feet_offset`, `mobility`, stats, arme et passif |
| `CharacterSpriteLoader.LoadOrGet(string charId, string folder)` (`scripts/Combat/CharacterSpriteLoader.cs:36`) | Charge les séquences `char_<id>_<DIR>_<action>_<NN>.png`, numérotées à partir de 01 |
| `CharacterPortrait.Artwork(string characterId)` / `Idle(string characterId, int frame = 1)` (`scripts/UI/CharacterPortrait.cs`) | Illustration WebP et animation des menus ; ne pas ajouter un second chargeur |
| `RoundMomentum.Initialize(Player player, PerkMomentum config)` et `Advance(float dt)` (`scripts/Progression/RoundMomentum.cs`) | Exemple de signature composée, réglages JSON, progression temporelle et retrait des abonnements |
| `RemanenceEcho.Initialize(Player player, PerkEcho config)` (`scripts/Progression/RemanenceEcho.cs`) | Autre exemple de signature et de feedback dédié ; branchement dans `PerkManager` |
| `PlayerMobility.Step(float delta, Vector2 input, float speed, float terrainFactor, bool allowed)` (`scripts/Core/PlayerMobility.cs:94`) | Machine de mobilité commune ; profils dans `data/movement/mobility.json` |
| `ErasureEffects.For(ErasureManager.ErasureZonePhase phase)` (`scripts/World/ErasureEffects.cs:21`) | Effets de zone ; consommation dans `Player.OnErasurePhaseChanged`, vitesse et calcul de dégâts |
| `Player.TakeErasureDamage(float damage)` (`scripts/Core/Player.cs:1442`) | Chemin spécial des dégâts du Néant : ne pas détourner la défense ordinaire |
| `MetaSaveManager.IsUnlocked(UnlockKind kind, string id)` (`scripts/Infrastructure/MetaSaveManager.cs:488`) | Droits calculés à partir des quêtes accomplies ; une ancienne complétion doit ouvrir les récompenses ajoutées à sa quête |
| `generate(character_id, output, sheet, scale, editable=False, actions=...)` (`tools/generate_character.py:36`) | Production procédurale et sources de retouche ; modèle de Scaphandrière déjà enregistré |

Les futurs noms comme `AirReserve`, une configuration d'air ou un comportement de traction sont **à créer**. Ils ne constituent pas des APIs déjà disponibles. Pas de champ JSON inventé sans DTO, lecture et validation correspondants.

## 1. Brief proposé pour la Scaphandrière

**Promesse :** une exploratrice lourde qui avance dans un monde qu'elle prend encore pour la mer. Le casque rond en cuivre et le filin la distinguent immédiatement ; le harpon ouvre un passage et l'air lui accorde une fenêtre près du front d'Effacement.

- Reprendre le lore et le modèle existants : toile usée, cuivre/laiton, vert-de-gris discret, hublot, bouteille et tuyau, bottes lestées. Ne pas ajouter de vert-acide réservé aux créatures.
- Montrer de la masse dans la marche et une suspension brève dans la mobilité. Le tuyau et le filin suivent le mouvement ; aucun harpon tenu à la main n'est requis par le système actuel d'auto-attaque.
- Portrait : fonction et tenue immédiatement identifiables, casque entier dans le cadrage, lisible dans les découpes des menus. Source existante à localiser, **sans création de remplacement** (correction Raphaël en N1) ; ne pas supposer que l'ancien dossier de portraits fourni en octobre est disponible sur cette machine.
- Contrepartie proposée : lenteur et faible cadence, compensées par la portée et une résistance supérieure à celle du Traqueur. La fiche mentionne une vitesse d'essai de 190 ; PV, dégâts, portée et régénération sont établis ci-dessous puis seront mesurés, sans toucher aux cinq héros existants.
- Synergies à éprouver : perforation/allonge du harpon ; trajet près du front pour gagner de l'Essence. L'air ne doit pas encourager à rester indéfiniment immobile.

### Réserve d'air : adapter une fiche devenue incohérente

La fiche de septembre propose 6 s de protection contre les dégâts des zones effilochées/effacées, recharge en 10 s hors danger. **Ces zones ne causent actuellement aucun dégât environnemental.** `data/scaling/erasure.json`, `zone_effects`, leur donne des multiplicateurs de vitesse/dégâts de 0,95 puis 0,88 et un voile de 0,35 puis 0,7. Le Néant inflige, lui, des dégâts par un chemin spécifique.

**Adaptation retenue pour le prototype :** pendant 6 s cumulées en zone effilochée ou effacée, l'air neutralise les malus de vitesse et de dégâts du personnage. Recharge complète en 10 s en zone ancrée/fragile. Le voile du monde et le bonus d'Essence restent ceux de la zone, pour conserver sa lecture et éviter de modifier les autres systèmes. À zéro, les malus reviennent immédiatement. Aucun renouvellement en passant d'une cellule dangereuse à une autre ; pause et écrans de choix ne rechargent pas.

Le Néant conserve ses règles et n'offre pas de recharge. Étendre la réserve à ses dégâts serait une autre proposition de design, avec contrôle du soin et du stationnement ; ne pas le faire implicitement. La version sans dégâts à absorber peut sembler trop discrète : vérifier son intérêt réel en N2 avant de produire tous ses effets définitifs.

### Harpon et mobilité : deux contrats à expliciter

- **Harpon :** projectile lent, perforant, longue portée ; la fiche demande de ramener le premier ennemi léger, ou d'attirer le personnage vers un lourd. Les déplacements forcés imposent une définition de « léger/lourd » en données, des plafonds de distance/durée et une politique pour les boss. Proposition : boss et parties de boss ancrés ; jamais de téléportation ni de traversée de mur, eau infranchissable ou Néant. Le rappel automatique du joueur doit être évalué en prototype : il peut le projeter dans une attaque sans intention du joueur. Une éventuelle simplification doit être décrite comme telle.
- **Saut flottant :** la fiche propose 0,5 s et un contrôle limité. Un profil de dash plus long ne suffit pas à implémenter un saut. Prototype avec trajectoire physique au sol, silhouette décalée verticalement et ombre au sol ; collisions actives, aucune immunité générale. Traiter explicitement le ralentissement en eau peu profonde ; ne pas annoncer un franchissement de terrain que le code ne réalise pas.

## 2. Inventaire de production visuelle

| Livraison | Quantité / contrat | État pour la Scaphandrière |
|---|---|---|
| Repos | 6 images × 8 directions = **48** | Présentes ; contrôler la respiration, le hublot et le tuyau |
| Marche | 8 × 8 = **64** | Présentes ; contrôler le poids, les pieds et le filin |
| Mobilité | 4 × 8 = **32** | Dash présent ; poses propres au saut flottant à produire si retenu |
| Blessure | 3 × 8 = **24** | Présentes ; vérifier la lecture avec le flash runtime |
| Mort | 6 × 8 = **48** | Présentes ; contrôler le dernier état et la dissolution |
| Portrait | WebP si la source existante est retrouvée | **Ne pas recréer.** Fallback sprite runtime en attendant la source correcte (retour Raphaël pendant N1) |
| Icône du Fusil-harpon | **1 PNG 32×32** | À créer dans le pipeline d'icônes d'armes |
| Harpon en vol | **1 atlas directionnel + entrée du manifeste** | À créer ; contrat du générateur : 16 directions, nombre de frames défini par son modèle |
| Traction | Filin, accrochage et relâchement | À prototyper ; privilégier les primitives/effets existants, texture seulement si nécessaire |
| Réserve d'air | États plein, consommation, vide, recharge | À concevoir près du personnage ; jauge lisible, particules limitées, pas quatre copies des 216 images |
| Saut | Départ, suspension, atterrissage ; ombre au sol | Ombre et effets existants à réutiliser si suffisants, compléments identifiés en prototype |
| Présentation du passif et de la mobilité | Libellés et éventuel pictogramme | Reprendre le contrat actuel du Hub ; ne pas créer une icône sans consommateur |
| Ascensions du harpon | Deux voies et leurs différences visibles | À définir avec l'arme ; pas d'icônes supplémentaires supposées obligatoires |

**Bilan :** 216 images de corps existent ; portrait et arme manquent ; mobilité et signature demandent une production ciblée. Pour une identité inédite, les 216 images et le modèle sont à produire intégralement. Ni animation d'attaque du corps ni arme en main ne doivent être ajoutées par habitude : elles ne sont pas chargées par le contrat actuel.

Les projectiles doivent suivre DECISIONS §159 : dessin à taille finale, contour léger, lisibilité sur les cinq biomes, pas de harpon surdimensionné ressemblant à une arme entière. Le gabarit exact et la longueur visible seront comparés à la flèche actuelle, pas décidés par agrandissement après export.

## 3. Lots d'implémentation

### N0 — Fiche finale et contrat des assets — livré le 13 octobre

**Identité retenue :** Scaphandrière, Solange Delmas ; lore conservé. Stats d'essai : **115 PV, vitesse 190, dégâts 10, cadence 0,9, portée 340, régénération 1,035, bouclier 0, interaction 65**. La régénération correspond à −10 % du 1,15 courant de référence ; aucun héros existant modifié.

**Contrats retenus :** air de 6 s, recharge de 10 s en Ancré/Fragile, neutralisation de vitesse/dégâts en Effiloché/Effacé, ni protection ni recharge dans le Néant. Feedback : réserve près du personnage, souffle discret aux transitions ; conserver voile du monde et bonus d'Essence. Harpon : premier léger ramené vers le joueur, premier lourd attirant le joueur, boss/parties de boss ancrés ; un rappel par lancement, trajets bornés, collision conservée, sous-pas de mouvement pour éviter tout passage à travers murs/eau/Néant. La traction s’arrête à l’entrée de l’eau de carte ; les dangers et ralentissements de marée de l’Indicible gardent leurs règles. Mobilité : saut flottant de 0,5 s, contrôle limité, déplacement physique au sol, arc visuel séparé et ombre au sol ; collisions et dangers actifs. Le terrain courant n'a qu'un type `Water` : le saut ignore son ralentissement pendant la mobilité, sans créer de profondeur ni promettre un terrain inexistant.

**Fichiers et consommateurs :** `assets/characters/scaphandriere/char_scaphandriere_<DIR>_<action>_<NN>.png` : 216 présents, 32 `dash` à retoucher, lus par `CharacterSpriteLoader` ; `portrait.webp` : conserver uniquement la source détaillée existante si retrouvée, sinon fallback `CharacterPortrait.Idle` ; ne pas créer un remplacement ; icône et atlas du harpon : à créer en N3 via les générateurs d'armes/projectiles, lus par les mêmes consommateurs que les autres armes. Aucun sprite supplémentaire obligatoire pour l'air, le filin et l'ombre avant le prototype des primitives existantes.

**Charte amendée :** contrat de 8 directions × 27 frames, cadres d'export distingués de l'emprise visible mesurée. Revue des 216 PNG et de la référence Scaphandrière effectuée ; cadre initial 42×44, pivot (21, 40). L’audit N1 montre des semelles coupées de 1–2 pixels sur des poses existantes ; cadre étendu à **42×48**, pivot (21, 40) conservé et décalage des pieds **16** (= 40 − 48/2). Aucun changement de taille physique ni de proportions. Aucun item de roadmap validé sur le seul design.

**Références :** sections 0–2 de ce plan, `scaphandriere.py`, `CharacterSpriteLoader`, fiches du casting, plan 32 H1. **Preuve :** fiche sans contradiction avec l'Effacement actuel, planche existante revue, aucune ligne de roadmap cochée sur cette seule base. **Garde-fous :** pas de craft, défense fixe, score réintroduit ou déblocage initial modifié ; aucune proposition présentée comme décision de Raphaël.

### N1 — Animations prêtes à intégrer ; design détaillé existant conservé

**À faire :** partir du modèle existant pour cette candidate ; reprendre les animations des héros joués dans `tools/sprites/poses.py`. Produire les retouches utiles et les poses de mobilité. **Correction de Raphaël en cours de N1 : ne pas recréer le sprite détaillé / portrait du personnage.** Conserver le design existant ; le portrait IA créé par l’agent a été retiré du jeu et son prompt de production du dépôt. Utiliser le fallback sprite déjà prévu par le runtime tant que la source correcte du portrait n’est pas localisée. La recherche ciblée n’a retrouvé que `doc/illustrations/references/ref_scaphandriere.png` (planche de sprites), pas un portrait détaillé réutilisable. Exporter les planches de directions/actions et sources éditables ; conserver le pivot. Garder une copie des sprites avant régénération pour la comparaison.

**Références :** `tools/generate_character.py`, `tools/review_character.py`, `tools/sprites/characters/scaphandriere.py`, `doc/RETOUCHES-ASEPRITE.md`, plan 32 §§6/9.

**Preuves :** 216 images cohérentes si le contrat de 27 frames est conservé ; séquences sans trou, RGBA et alpha binaire, aucun corps coupé ; vues sur sols clair/sombre et en aplat noir ; déterminisme des 32 poses modifiées vérifié par deux rendus indépendants ; autres poses comparées à la baseline, hors marge de cadre restituée. Le nombre de frames ne doit pas empêcher une amélioration justifiée, mais tout changement actualise le manifeste du lot et sa recette.

**Garde-fous :** ne pas régénérer tout le casting ; respecter les verrous Aseprite ; ne pas éditer les imports. Le générateur supprime les anciennes frames surnuméraires : contrôler le diff. `review_character.py` inspecte tous les PNG du dossier, donc un `portrait.png` y ferait échouer le contrôle ; utiliser le WebP attendu par le runtime.

### N2 — Kit jouable en prototype

Découper ce lot en **N2a réserve d'air**, **N2b arme**, puis **N2c mobilité**, chacun testé avant de commencer le suivant. Utiliser les bancs existants pour isoler les comportements avant la fiche jouable complète.

**N2a livré techniquement le 13 octobre 2026.** La signature `scaphandriere_air` contient les durées d'essai de 6 s et 10 s. `PerkAir` et son lecteur refusent champs absents/inconnus, types non numériques, zéro, valeurs négatives ou non finies ; le catalogue n'est publié qu'après lecture complète. `PerkManager` crée `AirReserve`, sans ajouter prématurément la Scaphandrière au catalogue des personnages.

Le joueur conserve la phase réelle et recalcule sa pénalité à changement de phase ou de protection ; les bonus de statistiques restent séparés. Le module relit le monde à son branchement, même si le signal de phase a déjà été émis. L'air ne neutralise que vitesse et dégâts offensifs en zone effilochée/effacée ; voile et chance d'Essence sont inchangés. À zéro, les malus reviennent immédiatement ; recharge en zone ancrée/fragile. Dans le Néant, la quantité est conservée sans recharge ni protection. Les blessures ordinaires ne vident pas la bouteille ; pause et état hors run suspendent son temps. Mort ou retrait du module suppriment la protection.

Jauge de prototype : un seul `Node2D` réutilisé près du casque, remplissage au pixel et quatre états lisibles (plein, consommation, vide, recharge), plus un état gris de réserve gelée dans le Néant. Aucun nœud, recherche dans l'arbre ou allocation de collection par frame. Sa visibilité en foule et son placement seront jugés avec les captures du kit complet, pas sur le seul smoke du Hub.

**Preuves N2a :** `tools/validate.sh /tmp/vestiges-45-air-n2a-20261013 smoke perk_effects perk_contracts movement-integration erasure_active` : **5/5**, aucune source modifiée pendant les suites, build **0 warning / 0 erreur**. `perk_effects` comporte **40 assertions Air** : lecteur strict, réserve initiale, demi-charge, épuisement, recharge plafonnée, transitions, vrais dégâts calculés, bonus conservés, blessure, Néant, pause Godot réelle, hors run, retrait, branchement dans une phase déjà effacée, mort et nouveau joueur. UID de `AirReserve.cs` produit par l'import Godot. Journaux et manifeste dans ce dossier. Aucune case globale de roadmap cochée ; N2b/N2c et la capture en run restent à réaliser.

**N2c — Découpage avant code, 13 octobre.** 1) Profil `float_jump` et réglages validés par le lecteur ; 2) hauteur visuelle calculée par la mobilité, application au sprite sans déplacer ombre ni collision, et eau séparée des ralentissements de combat ; 3) recette module puis vrai joueur et suites communes. Valeurs d'essai : durée **0,5 s**, vitesse **×1,2** (114 px à vitesse 190), recharge **3 s**, virage **90°/s**, arc sinusoïdal haut de **10 px**, aucune invulnérabilité, arrêt aux murs. Le saut ignore le seul facteur d'eau de carte pendant sa durée, dès son premier tick ; `_slowFactor` et les malus d'Effacement restent effectifs. Position du sprite seule élevée, décalage des pieds 16 conservé (42×48, pivot 21/40), ombre fixe. Hurt, mort, pause, menu et obstacle reposent aussitôt le sprite. Plan de métro prioritaire : saut instantané sans hauteur persistante ; mobilité prioritaire sur la traction du harpon. Tests à 30/60/120 Hz, eau/contrôles, arc/virage, collision/Néant, interruptions, blink et traction ; fiche personnage toujours reportée à N4.

**N2c livré techniquement le 13 octobre.** Le profil `float_jump` et les deux réglages nouveaux (`visual_height_px`, `ignore_water_while_active`) passent par `MobilityConfig` ; le lecteur exige types numériques/booléens et nombres finis bornés. `PlayerMobility` calcule l'arc sans nouveau nœud ni état physique. `Player` applique la hauteur à la position du sprite seulement et la remet à zéro sur interruption ; pause et sortie de run remettent aussi immédiatement une pose au sol, en conservant la priorité blessure/mort. Le facteur d'eau de carte est ignoré dès le premier tick, tandis que les ralentissements de combat/marée et l'Effacement restent actifs. Traction du harpon annulée par la mobilité ; Plan de métro sans hauteur persistante. Aucun personnage supplémentaire ajouté au catalogue.

**Preuves N2c :** revue indépendante statique PASS ; `tools/validate.sh /tmp/vestiges-45-floatjump-n2c-20261013-v2 smoke movement movement-integration weapons perk_effects` : **5/5**, aucune source modifiée pendant les tests, build **0 warning / 0 erreur**. La suite mouvement vérifie **30 assertions Saut** : 114 px en 0,5 s aux cadences 30/60/120 Hz, sommet 10 px (9,95 px échantillonnés à 30 Hz), eau et restauration du ralentissement à l'atterrissage, contrôle/Effacement conservés, ombre immobile et pieds à −6, virage borné, pause/hurt/mort, pose au sol après pause, mur fin/Néant, Plan de métro et priorité harpon. La première validation conservée dans `/tmp/vestiges-45-floatjump-n2c-20261013` a révélé deux problèmes de fixtures : le scénario coffre attendait une ouverture sans financer le nouveau prix en Essence ; la suite perks quittait avant libération complète de ses corps temporaires (fuite RID). Corrigés dans les tests par un vrai portefeuille et un drainage de deux frames après libération du joueur, sans modifier les coffres ni assouplir le contrôle des journaux. Les captures du personnage complet et le jugement du saut à taille de jeu restent en N5 ; aucun item global de roadmap coché.


**À faire :** reprendre les signatures `momentum` / `echo` de `data/perks/perks.json`, leurs DTO dans `PerkDataLoader` et leur création par `PerkManager` pour une réserve d'air composée. Ajouter ses durées en JSON, un état de run explicite et une interface minimale pour les malus effectifs. Ne pas multiplier puis diviser les statistiques à chaque frame ; recalculer à changement de phase ou d'état.

Pour le harpon, reprendre une arme perforante du catalogue et les chemins projectile existants ; n'ajouter un comportement de traction qu'après définition du contrat. Couvrir collision, mort de cible, réutilisation du pool, tir multiple, rémanence de l'Éveillée et ownership des dégâts. Définir ses niveaux et ses deux ascensions avec le contrat d'arme courant.

Pour la mobilité, reprendre le profil `glide` comme exemple de réglages spécifiques et `PlayerMobility` comme machine commune ; ajouter uniquement les capacités manquantes du saut retenu, sans nouvel input. Introduire la fiche du personnage quand toutes ses références minimales résolvent, pour le tester avec un profil dev temporaire.

**Références :** APIs §0 ; `scripts/Core/Player.cs`, `scripts/Core/PlayerMobility.cs`, `scripts/Progression/PerkManager.cs`, `scripts/Infrastructure/WeaponContract.cs`, `data/weapons/weapon_contract.json`, exemples P2/P3 du plan 06.

**Preuves :** air à mi-charge, épuisement, recharge, transitions, pause, mort et nouvelle run ; harpon sur léger/lourd/boss, cible détruite ou recyclée, multitir et pool ; mobilité à 30/60/120 Hz, murs, coins, eau, Néant, hurt et menus. Comparer parcours dangereux avec/sans air et apprécier si le kit change réellement les trajets.

**Garde-fous :** pas de stats en dur, de `GetNodesInGroup` ou d'allocation par frame ; ne pas déplacer directement `GlobalPosition` pour traverser une collision ; ne pas recycler la cible d'un harpon sur la nouvelle vie d'un ennemi poolé. Les nouveaux `.cs` auront chacun leur `.cs.uid`.

### N3 — Arme, feedback et sons définitifs

**Découpage avant code — 13 octobre (N2c validé 5/5).** 1) Ajouter le modèle et l’icône 32×32 du fusil-harpon, puis le projectile métallique à barbes en 16 directions, à une taille proche de la flèche ; génération ciblée avec sources reproductibles, sans toucher les autres assets. 2) Relier icône, atlas et son de tir au contrat existant ; conserver le filin et les impacts mutualisés. 3) Composer un tir bref et deux ponctuations discrètes de vanne/air depuis la banque CC0 locale avec crédits et empreintes : entrée en consommation et épuisement, sans boucle ni prétention de respiration humaine. Paramètres audio validés dans la réserve, anti-répétition commun aux transitions, silence à l’initialisation/pause/mort/hors run. Conserver l’atterrissage existant. 4) Revue indépendante, planches native/agrandie et auditions au gain de jeu hors dépôt ; validation audio, armes, mouvement intégré et effets des perks avec build sans warning. Captures et écoute dans la vraie run restent en N5 après catalogue N4. Aucun portrait produit ou modifié.

**Contrat initial :** reprendre `tools/sprites/weapons/icons.py` pour le harpon 32×32, `tools/sprites/projectiles.py` pour son atlas et `ProjectileSprites` pour sa lecture. Produire les effets utiles retenus en N2, en passant par les pools existants pour les effets fréquents. Finaliser les poses si le prototype change le geste, sans recréer le portrait existant. Définir tir, impact, souffle/épuisement et atterrissage ; auditionner les sons retenus avec la banque actuelle, sans boucles respiratoires envahissantes.

**Références :** `tools/generate_weapon_icons.py`, `tools/generate_projectiles.py`, `scripts/Combat/CombatPools.cs`, `doc/AUDIO-GUIDE.md`, DECISIONS §159.

**Preuves :** planches à taille native, captures en combat réel, différenciation des états de réserve, lecture du filin et du projectile au milieu des impacts ; sons réellement écoutés. **Garde-fous :** pas de halo massif ni de création/destruction d'un nœud à chaque bulle ; le générateur d'icônes réécrit tout le catalogue, contrôler que seules les différences attendues subsistent.

**N3 livré techniquement le 13 octobre.** Icône `weapon_icon_harpoon_rifle.png` 32×32 et atlas `proj_harpoon.png` (16 directions, cadres 32×24, une frame) propres au harpon ; pointe/barbillons lisibles après correction de leur couverture à taille native. Gabarit opaque 15×8 à l’est, maximum 15 px de largeur et 9 px de hauteur selon l’orientation ; courte traînée de 3 px (alpha 50/100/150), corps opaque et aucun contact avec les bords. Icône à alpha binaire avec marge. Source atlas Aseprite avec trois calques et sidecar, pixels identiques au PNG ; régénérations indépendantes du code icône/atlas identiques. Les références d’arme pointent sur cette icône, ce projectile et `sfx_weapon_harpoon_rifle`, en conservant le filin N2b et `sfx_impact_blade`.

Le tir est une composition CC0 de `crossbow_release` ralenti et `chain_jump` discret, 220 ms. `air_consume` (192 ms) et `air_empty` (240 ms) composent `dash_start`/`lantern_fire` en ponctuations matérielles de vanne et de souffle, sans boucle ni revendication de respiration humaine. Cibles mesurées : tir **−40,05**, ponctuations **−43,04** (sonie momentanée effective, gain de banque inclus). Le DTO d’air valide structure, références audio et délai commun **1,5 s** ; `AirReserve` utilise `AudioManager.PlayAt` et son pool, sans allocation ni lookup de nœud par frame. Initialisation, recharge, pause/reprise, hors run, mort et ajout tardif en danger restent silencieux ; vingt allers-retours de frontière n’ajoutent aucune demande pendant le délai. Le son d’atterrissage existant reste utilisé.

**Preuves N3 :** revue indépendante **PASS** (dont les trois WAV reproduits octet pour octet) ; `tools/validate.sh /tmp/vestiges-45-n3-20261013-v1 smoke audio weapons movement-integration perk_effects` : **5/5**, build **0 warning / 0 erreur**, **0 changement source** pendant les suites. Les effets de perks ajoutent **30 assertions audio** : 19 de contrat et 11 de transitions. L’audit audio mesure **144 fichiers, 0 échec**, avec seulement les deux avertissements préexistants sur `sfx_orage_proche` et `sfx_chest_reveal`. Sur **11 848 fichiers d’assets préexistants**, seuls les crédits et le manifeste des projectiles changent ; tous les PNG/WAV et autres fichiers restent identiques, y compris les sprites du personnage. Aucun portrait produit ou modifié.

Planches inspectées : [directions native/×3 sur deux fonds](planches/45-harpon-directions.png), [cinq biomes native/×4](planches/45-harpon-biomes.png), [icône native/×6](planches/45-harpon-icone.png). Rapport compact : [validation N3](planches/45-scaphandriere-n3-validation.json). Auditions au gain de jeu et répétées : `/tmp/vestiges-scaph-n3-20261013/audition/`, manifeste avec empreintes et gains. **Limite explicite : ces mesures et fichiers ne constituent pas une écoute humaine ; timbre, mix perçu, lecture du filin et captures du kit complet restent à vérifier en N5 après N4.**

Reproduction ciblée : `python3 tools/generate_weapon_icons.py harpoon_rifle`, `python3 tools/generate_projectiles.py harpoon --editable` (respecte la retouche existante ; son export passe par `tools/export_retouches.py`), `python3 tools/generate_scaphandriere_audio.py /tmp/vestiges-scaph-audition`. Le dernier outil ne remplace que les trois sorties N3 et leurs entrées banque/crédits ; les sources CC0 sont vérifiées par empreinte avant composition. Le générateur d’icônes accepte désormais des identifiants d’armes pour éviter toute réécriture du catalogue complet.

### N4 — Déblocage, menus et sauvegardes

**Découpage avant code — 13 octobre :** 1) Ajouter la fiche Scaphandrière avec les stats N0, le pivot N1 (`sprite_feet_offset: 16`), le passif `scaphandriere_air`, le profil `float_jump` et le Fusil-harpon ; 2) rattacher personnage et arme à `raise_the_bodies`, conserver sa condition et ouvrir les droits depuis les sauvegardes déjà accomplies ; 3) adapter les fixtures de quête, vérifier profil neuf, accomplissement/rechargement, ancienne sauvegarde et mode dev ; 4) recette `smoke quests catalogs saves menu_navigation ui_art inventory dev_mode`. Les menus consomment les libellés JSON existants et le portrait utilise le repli animé actuel, sans nouvel artwork. Captures en run et recette visuelle finale réservées à N5.

**À faire :** suivre P2a/P3a : fiche et passif, profil de mobilité, portrait, arme dans catalogue/contrat/visuels, affichages du Hub, sélection avant départ, pause et bilan. Auditer les textes FR/EN et compléter `assets/translations/translations.csv` pour les textes réellement localisés : les chargeurs de fiche lisent aussi des libellés français bruts, ne pas inventer de clés sans consommateur. Pour la Scaphandrière, conserver `raise_the_bodies` et son seuil ; y rattacher le harpon si retenu comme récompense partagée. **Retirer le champ `name` de sa récompense de personnage dès son entrée au catalogue** : `QuestDataLoader` le réserve aux personnages à venir et rejette un personnage connu qui le conserve. Vérifier les règles d'accès depuis les quêtes et la disponibilité des contenus à venir.

**Références :** `data/quests/quests.json:103`, `scripts/Progression/QuestBook.cs`, `scripts/Infrastructure/QuestDataLoader.cs`, `scripts/Infrastructure/MetaSaveManager.cs`, `scripts/UI/CharacterPortrait.cs`, `tools/tests/QuestRegression.cs`.

**Preuves :** profil neuf verrouillé (Traqueur initial), quête réalisable par les anciens héros, quête accomplie qui donne les bons droits, sauvegarde/rechargement, profil ayant déjà accompli cette quête avant l'ajout de l'arme, profil dev. Tester les menus à 720p/1080p au clavier et à la manette. Mettre à jour la fixture qui emploie la Scaphandrière comme personnage inconnu, en conservant le test de rejet avec un identifiant synthétique absent.

**Garde-fous :** ne pas réinitialiser les droits ni exiger de refaire une quête achevée ; pas de condition circulaire nécessitant le nouveau personnage pour l'obtenir. Ne pas forcer les listes ou compteurs à cinq/six si le catalogue peut les fournir.

**N4 livré et vérifié — 13 octobre :** Scaphandrière inscrite au catalogue avec les stats N0, le décalage de pieds **16**, `float_jump`, `scaphandriere_air` et `harpoon_rifle`. Description : « Endurante, elle garde son souffle dans l'Effacement et ramène ses proies au harpon. » Fusil-harpon déclaré arme de départ (`source: default`, `default_for: scaphandriere`). `raise_the_bodies` garde ses **290 éliminations en zone effilochée/effacée** et donne personnage et arme ; nom de récompense retiré au profit du catalogue. Les cinq anciennes fiches et les autres quêtes sont inchangées. Aucun nouveau portrait : les six poses de repos **42×48**, filtre nearest, servent de repli dans la fiche.

Les menus consomment directement les noms/descriptions français des fiches, du passif, de l'arme et de la mobilité ; aucune clé de traduction sans consommateur ajoutée. `MetaSaveManager` dérive déjà les droits des quêtes accomplies : aucun code de migration ni réaccomplissement requis. Fixture d'ancien profil contenant uniquement la quête et son ancien marqueur personnage : personnage et arme accessibles sans réécriture forcée, acquis conservés, sauvegarde/rechargement suivants vérifiés. Profil neuf fermé, accomplissement commun, rechargement et retour normal après mode dev vérifiés. Le parcours réel du Hub teste la fiche verrouillée, le repli animé, les textes du kit, le lien quête → Collection du harpon, puis le départ après déblocage. Fixture de personnage inconnu remplacée par `unknown_character`, sans perdre le rejet.

**Preuves :** `tools/validate.sh /tmp/vestiges-scaph-n4-20261013 smoke quests catalogs saves menu_navigation ui_art inventory dev_mode` : **8/8 suites vertes**, build **0 warning / 0 erreur**, import réussi, **0 source modifiée pendant la validation**. Quêtes : **70 + 325 assertions** ; navigation : **55 assertions**. Revue indépendante `discovery_integration` : PASS. Aucun changement du runtime ; captures 720p/1080p et combat complet restent à N5. Aucun commit/push.

### N5 — Validation finale et livraison

**Découpage avant code — 13 octobre, N4 validé 8/8.** Ajouter un mode de banc `--capture-scaph-kit` : vraies phases via `ErasureManager.OverrideMemory/RefreshGroundMemory`, temps de jeu et assertions aux quatre captures air (plein, consommation, vide, recharge) ; cinq cas de harpon avec vrais projectiles du catalogue (base léger/lourd/partie de boss ancrée, puis chaque ascension sur léger), une capture de liaison par cas et mesures avant/après. Réutiliser la galerie personnage corrigée (directions, apex/atterrissage, vraie blessure, mort), une run nomade de 45–60 s seed 1002, deux captures du Hub 720p/1080p en profil ayant accompli `raise_the_bodies`. Revue indépendante du banc avant moteur, build/smoke sans warning, contrôle strict des logs, inspection des images et enregistrement moteur court si disponible. Aucune mesure de performance ni validation subjective du timbre déduite de ces preuves.

**À faire :** exécuter les contrôles ci-dessous, inspecter les captures, corriger les défauts du lot. Archiver preuves, version, seeds et profils. Comparer les cinq héros existants sur leurs chemins touchés. Toute affirmation de performance exige un avant/après sur le même banc et une machine calme ; toute conclusion d'équilibrage exige plusieurs seeds et un profil identique.

**Preuves :** build zéro warning, suites vertes, corps et effets visibles en run, arme complète, droits persistants, aucun asset manquant. Recette humaine du fun et validation artistique distinguées de la conformité technique. **Garde-fous :** un smoke du Hub ne prouve pas le combat ; une capture de mobilité ne prouve pas la mort ; le bot invincible ne prouve pas l'équilibrage de la survie.

Cocher dans la Stratégie V2 §25 uniquement l'item précis implémenté et vérifié. Ne pas clore les cases globales « personnages équilibrés » ou « sprites validés » sur une livraison technique. Reporter le résultat dans le plan 06 (lot P4 si Scaphandrière), le plan 32 et le tableau de bord.

## 4. Commandes de recette

À lancer pendant l'implémentation, avec des dossiers de résultats neufs. Les noms de suites ci-dessous existent dans `tools/validate.sh`.

```sh
# Prévisualisation, sans écrire dans assets/
python3 tools/generate_character.py scaphandriere --dry-run --sheet /tmp/vestiges-scaph-preview.png

# Après copie préalable des anciennes frames dans le dossier baseline
python3 tools/review_character.py scaphandriere --baseline /tmp/vestiges-scaph-baseline --output /tmp/vestiges-scaph-review

# Build + import + suites séquentielles sur profil temporaire
tools/validate.sh /tmp/vestiges-scaph-validation smoke movement movement-integration weapons perk_contracts perk_effects erasure_active quests catalogs saves menu_navigation ui_art inventory dev_mode audio

# Après inscription au catalogue : directions, mobilité, blessure
CAPTURE_EXTRA_ARGS="--capture-character --character scaphandriere" tools/capture_run.sh /tmp/vestiges-scaph-character 12 2 1920x1080 1002

# Combat réel et effets, en conservant la même seed pour les comparaisons
CAPTURE_EXTRA_ARGS="--character scaphandriere --nomad" tools/capture_run.sh /tmp/vestiges-scaph-run 60 5 1920x1080 1002
```

Compléter par la mort, le passif actif/épuisé, les trois types de cible du harpon et les ascensions : ces scénarios ne sont pas garantis par les deux captures ci-dessus. Prévoir un scénario dédié si les outils actuels ne savent pas les forcer. Capturer le Hub via `tools/capture_hub.sh` avec la séquence réelle de navigation du menu courant. Ne pas employer une sauvegarde personnelle pour simuler un déblocage.

## 5. Historique — préparation livrée le 13 octobre, avant accord

- Audit du casting, du modèle, des exports, des chargeurs, de la quête et de l'Effacement ; planche H1 existante regardée.
- Inventaire de production et lots N0–N5 rédigés ; identité et adaptation du kit restent des propositions.
- `dotnet build` : **0 warning, 0 erreur**. Pas de smoke ni de capture relancés : préparation documentaire, aucune scène, initialisation ou image du jeu modifiée.
- Aucune case de livraison gameplay cochée et aucun nouveau personnage déclaré jouable.


## 6. N0–N1 livrés techniquement — 13 octobre 2026

- Identité, stats d'essai et contrats du kit fixés dans N0 ; accord et correction de Raphaël consignés dans DECISIONS §161.
- **216 PNG 42×48 RGBA**, alpha binaire, séquences complètes ; aucun pixel au bord, aucune coupe. **Pivot (21, 40), décalage des pieds 16**, échelle 0,53 et proportions inchangées.
- **32 poses de mobilité adaptées au saut flottant** : impulsion, deux suspensions jambes repliées, réception. Deux rendus indépendants donnent les mêmes octets/SHA-256 sur les 32 PNG.
- Les **184 autres poses gardent exactement leurs pixels sur les 42 premières lignes** ; 183 conservent aussi les 44 lignes du cadre initial, la dernière restitue le contour de la semelle coupée. Les lignes ajoutées rendent les semelles complètes, sans changement de design. Les fichiers des cinq autres héros sont identiques à la baseline.
- **40 sources Aseprite et 40 JSON** dans `art/retouches/assets/characters/scaphandriere/` ; les 216 frames décodées sont pixel-identiques aux PNG. Les verrous de retouche existants sont respectés ; aucune retouche préexistante n'a été écrasée.
- Planches inspectées : [toutes actions et directions](planches/45-scaphandriere-animations.png), [saut sur fond sombre/clair et silhouette noire](planches/45-scaphandriere-saut.png). [Synthèse de recette](planches/45-scaphandriere-validation.json) et [audit/SHA-256 des 216 fichiers](planches/45-scaphandriere-audit.json).
- **Aucun nouveau portrait livré.** Le portrait IA ajouté par erreur a été retiré avec son import et son prompt du dépôt, après la correction de Raphaël. Le runtime conserve son fallback de sprite si le portrait détaillé existant n'est pas disponible.

Recette : `python3 tools/generate_character.py scaphandriere --editable --sheet /tmp/vestiges-scaph-n1-20261013/all-actions-final.png`, puis second rendu indépendant des seules poses `dash` dans un dossier temporaire, décodage Aseprite et `tools/review_character.py`. Baseline, comparaison et script de vérification : `/tmp/vestiges-scaph-n1-20261013/`. `git diff --check` vert. Aucune modification C# ni scène dans N1 : build/import/smoke et captures en run sont repris avec N2 et N5. Cette conformité des assets ne constitue ni un équilibrage du personnage ni une validation artistique de Raphaël.


## 7. N2b — Fusil-harpon : contrat d'implémentation (avant code)

Après validation de N2a, ajouter `harpoon_rifle` au catalogue avec le pattern de tir perforant existant, ses poids de croissance et deux ascensions. Premier survivant touché par une salve : léger ramené vers la position courante du joueur (seule la rémanence garde son point de départ figé) ; lourd attire le joueur ; boss, mini-boss et parties ancrés. `traction_weight` explicite en données (0 léger, 1 lourd ; Brute lourde), indépendant du visuel. Une liaison active par joueur et au plus une traction sur chaque créature ; les tirs supplémentaires/perforations n'ajoutent pas de rappels au même lancement.

Composant `HarpoonTether`, origine et provenance rémanence conservées dans `AttackContext` ; la rémanence peut ramener un léger vers son propre point, jamais attirer le joueur. Registre préalloué borné des lancements, références libérées avec les projectiles ; aucun historique infini, aucun nœud créé par impact. Filin via une ligne persistante. Mort/recyclage de cible contrôlé par `TargetLock` ; pause, hors run, blessure et mobilité annulent la liaison. Mobilité prioritaire avant tout déplacement forcé.

Trajets limités par distance, durée, vitesse et distance d'arrêt en JSON. Léger : sous-pas de 2 px et résolution des obstacles, sans modifier le déplacement normal de l'IA. Lourd : vitesse du joueur puis `MoveAndSlide`, clamp sol/Néant/eau, aucun téléport. L'eau de carte bloque la traction ; les dangers de la marée gardent leurs effets. Images et sons existants temporaires en N2b ; assets définitifs réservés à N3, sans portrait IA.

Recette N2b : armes (survivants, salve, perforation, écho, pooling, obstacles fins à 30/60/120 Hz, eau/Néant, invalides de contrat), puis `weapons catalogs enemy_abilities perk_effects movement-integration`, build zéro warning. N2c et fiche personnage restent hors de ce lot.


### N2b livré et vérifié — 13 octobre

`harpoon_rifle` ajouté au catalogue, **tier 1**, stats d'essai : dégâts 24, cadence 0,65, portée 340, vitesse du projectile 260, une munition et deux perforations. Croissance sur dégâts/cadence/portée/vitesse/perforation/nombre. Ascensions : **Treuil de sauvetage** (portée/cadence et rappel plus long/rapide) et **Lance des abysses** (puissance/perforation, rappel court). Image Cloueuse, projectile `bolt` et son Cloueuse sont des références temporaires valides ; N3 remplace ces assets, N4 rattache l'arme de départ et sa quête.

`HarpoonTether` fournit une ligne persistante et une liaison par propriétaire. Registre de **256 salves simultanées**, préalloué, référencé par les projectiles jusqu'à leur retour au pool ; en saturation seule la traction est refusée, les dégâts/perforations continuent. Salve libérée au dernier projectile, sans historique infini. Provenance `AttackContext.IsEcho` persistante ; origine réelle de chaque projectile figée au départ effectif, y compris départ retardé. Le léger normal suit le joueur courant ; un léger touché par une rémanence suit son point figé. Un lourd touché par une rémanence ne rappelle jamais le joueur.

Traction d'essai : **96 px au plus, 320 px/s, 0,35 s**, arrêt à **12 px + rayons des deux corps**. Le Treuil monte à 160 px/440 px/s/0,45 s ; la Lance limite à 56 px/0,2 s. L'arrêt avant portée d'attaque du lourd reste à apprécier en capture N5. La Brute lit `traction_weight: 1`, les autres fiches héritent de 0 ; boss, mini-boss, parties et Souverains restent ancrés. Mouvement léger sous-pas de 2 px contre `ObstacleField` ; lourd via `MoveAndSlide`, clamp commun du sol/Néant/eau et ralentissements existants conservés. Priorité au dash et à la blessure ; pause, hors run, mort/recyclage et mort du propriétaire libèrent la liaison.

La revue a corrigé trois défauts avant validation : le déplacement simplifié de l'IA hors 600 px contourne désormais les tractions sans arrêter les minuteries ; aucune nouvelle accroche pendant `Hurt` ; les soustractions de positions lointaines utilisent une tolérance absolue de **0,05 px**, évitant d'annuler une liaison normale sur un arrondi float.

**Recette verte :**

- `/tmp/vestiges-scaph-n2b-v1` : `catalogs`, `enemy_abilities`, `perk_effects`, `movement-integration` verts ; entrée et départ dans l'eau réelle vérifiés. Sources inchangées pendant exécution.
- `/tmp/vestiges-scaph-n2b-v3` : `weapons` vert, **0 erreur de régression** ; build/import partagés, **0 warning et 0 erreur C#**, sources inchangées. Les essais précédents ont corrigé uniquement les fixtures : mur sur la couche du masque joueur (4), attente du `QueueFree`, victime de mort différée séparée des cibles vivantes ; assertions conservées.
- Cas vérifiés : léger/lourd/boss, déplacement du tireur, perforation et multi-tir avec une seule accroche, premier impact fatal puis survivant, rémanence et départ retardé, provenance d'un autre propriétaire refusée, projectile ordinaire réutilisant le pool, vie recyclée, cible morte, propriétaire mort, vraie pause, hors run, mobilité/hurt prioritaires ; **1024 salves séquentielles et 256 simultanées** ; murs de 2 px à **30/60/120 Hz**, arrêt du joueur à **67 px** devant le mur placé à 80 px ; cible hors 600 px, coordonnées (64000,64000), Néant, eau générée, deux ascensions et paramètres obligatoires/bornés du contrat.

Code/runtime stabilisé après v1 ; v2/v3 ne changent que les fixtures et assertions de `WeaponRegression.Harpoon`. Pas de revendication de performance ni d'équilibrage. Aucun portrait modifié, aucun commit/push. N2c peut reprendre `Player.Harpoon` (`Cancel`, `IsActive`, `PullsPlayer`) ; `Advance` est appelé après `Mobility.Step`, donc le saut garde priorité sur le rappel dès son premier tick.
