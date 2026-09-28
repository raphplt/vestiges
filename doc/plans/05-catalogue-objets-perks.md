# Plan 05 — Catalogue commun des objets et des perks

28 septembre 2026 · **Proposition de contenu à valider avant implémentation.** Annexe du [plan 05](05-armes-objets-builds.md), point de coordination avec les plans [13](13-butin.md), [17](17-armes-coffres-modificateurs.md) et [20](20-recompense-et-puissance.md). Aucun changement de gameplay livré par ce document.

> **Décision actuelle : B validée.** Raphaël choisit les quatre perks qualitatifs sans niveaux et demande un catalogue abouti. La proposition courante se trouve dans [Perks de spécialisation](05-perks-specialisations.md) : neuf fiches, acquisition, interactions et comparaison des plafonds d'armes 50/70/99. Le catalogue V1 de neuf perks est désormais validé et extensible ; B0 est livré et vérifié dans ce plan courant, sans activation des nouveaux effets. Coefficients à éprouver, objets historiques non automatiquement adoptés.

> **Historique : §11–12, réexamen et étude des trois directions.** Les §3–10 conservent la première proposition et son audit ; leurs nombres, retraits et lots ne s'appliquent pas automatiquement à B. Les constats d'absence de choix dans ces sections décrivent leur date de rédaction, avant la décision ci-dessus.

## 1. Périmètre et décisions acquises

Raphaël donne la priorité à la **refonte conjointe des objets et des perks** : objets cumulables sans plafond de quantité ni de slots ; perks limités à **quatre types équipés**. Il valide la préparation de la répartition concrète des effets et du catalogue, à lui présenter avant de coder. Le précédent périmètre « trois objets pour rendre les coffres utiles » est remplacé par celui-ci.

- Quatre armes et quatre perks ; les objets constituent un inventaire indépendant.
- XP, chance et oubli doivent pouvoir orienter un build. Les bonus des perks et des objets se cumulent. Aucun tome.
- Acquisition, effets réels, descriptions et inventaire appartiennent à la même verticale.
- Les pouvoirs innés des personnages restent hors des quatre emplacements ; leur refonte relève du plan 06.
- La rareté des améliorations reste distincte du niveau d'une arme ; B retire les niveaux et raretés des perks. Rareté fixe par objet retenue provisoirement dans DECISIONS §7.
- Le chantier XP R1-A/R1-G du plan 20 est déjà en cours dans l'arbre de travail : intégrer ses contrats, sans refaire ni écraser son travail.

**Première proposition conservée pour audit :** 16 perks, dix niveaux par perk, 24 objets, noms, répartition, effets, accès et règles de calcul. Les seize perks à niveaux sont remplacés par la direction B ; le catalogue d'objets doit encore être révisé conjointement. Tous les coefficients historiques restent des valeurs de départ, pas un équilibrage validé.

Cette annexe remplace, pour cette verticale, le trio pilote et les anciens lots objets du plan 05 §5–6. Les formes de butin du plan 13 restent un chantier de présentation ultérieur ; le catalogue pourra être distribué par les sources existantes.

## 2. Phase 0 — Ce que l'audit établit

Lecture statique du code et des données le 28 septembre ; pas de recette en jeu. Les fichiers XP évoluent pendant cette préparation : les méthodes sont des repères plus durables que leurs numéros de ligne.

| Constat | Source vérifiée | Conséquence pour la refonte |
|---|---|---|
| Deux circuits : anciens perks des coffres sans limite de slots, mais avec `MaxStacks` ; passifs du level-up à quatre slots | [PerkManager](../../scripts/Progression/PerkManager.cs), `OnLootReceived`/`SelectPerk` ; [Player](../../scripts/Core/Player.cs), `AddOrUpgradePassive` | Chaque effet rejoint une catégorie explicite ; fermer le circuit parallèle des « Dons » |
| 50 définitions dans l'ancien fichier : 44 perks dont cinq exclus et trois innés, plus six synergies | [perks.json](../../data/perks/perks.json) ; exclusions de `PerkManager` | Les anciennes mentions de « 64 perks » ne décrivent pas 64 choix actifs |
| 14 définitions de passifs, 13 actives, cinq niveaux chacune | [passive_souvenirs.json](../../data/progression/passive_souvenirs.json) ; [loader](../../scripts/Infrastructure/PassiveSouvenirDataLoader.cs) | 50 + 14 = 64 définitions réparties entre les deux catalogues |
| `per_level` contient un total ; l'amélioration applique la différence entre deux totaux | `ActivePassiveSouvenir.PreviewModifier` dans Player | Souffle du Néant n'ajoute rien aux niveaux 2–4 ; Reflet brisé aux niveaux 2 et 4. Exiger un gain effectif à chaque choix |
| Siphon annonce du drop d'Essence, mais augmente `xp_magnet_radius` | `siphon_essence` dans le JSON | Séparer attraction, valeur d'XP et quantité d'Essence |
| Six synergies annoncent une activation mais n'appliquent aucun effet | `PerkManager.CheckSynergies` ; [PerkDataLoader](../../scripts/Infrastructure/PerkDataLoader.cs) | Retirer ces annonces en fermant l'ancien circuit ; les combinaisons du nouveau catalogue fonctionnent par leurs effets propres |
| Les stats multiplicatives des anciens perks se multiplient à chaque exemplaire | `Player.ApplyPerkModifier` | Ne pas transformer une table plafonnée en `1,15^n` sans plafond |
| Plusieurs chances et seuils satureraient à 100 % | `Player.AddIgnite`/`AddRicochet`/`AddExecution` ; `GetCombinedProcChance` | Les piles d'objets de proc augmentent la puissance, pas une probabilité sans règle d'excédent |
| Le bouclier, l'armure et les soins ont des règles distinctes | [PlayerDefense](../../scripts/Core/PlayerDefense.cs), [defense.json](../../data/characters/defense.json) | Préserver le bouclier anti-coup fatal ; l'armure est plafonnée à 75 %, donc éviter un objet dont les copies ne feraient plus rien après ce plafond |
| Aucun type `item` ni inventaire d'objets dans le chemin de butin actuel | [LootRewards](../../scripts/World/LootRewards.cs), `Resolve`/`Apply` | Ajouter le domaine d'objets et son contrat de présentation, pas seulement des JSON |

### APIs et exemples réellement disponibles

- `PassiveSouvenirDataLoader.Load()`, `Get(string id)`, `GetAll()` : exemple de lecture `Godot.Json`, cache et filtre `enabled` à reprendre pour les définitions. `GetAll()` retourne une copie : utilisation au chargement/choix, pas par image.
- `Player.AddOrUpgradePassive(string passiveId, float gain, int levels)`, `PassiveSlots`, `MaxPassiveSlots = 4` : garde de capacité et signaux d'inventaire à conserver.
- `ActivePassiveSouvenir.PreviewModifier(float gain, int levels)` et `Upgrade(float gain, int levels)` : exemple d'aperçu avant application. Le modèle actuel mono-stat ne suffit pas aux perks à plusieurs effets.
- [FragmentManager](../../scripts/Progression/FragmentManager.cs), `SelectFragment(FragmentOption option)` et `FragmentOption.ApplyTo(Player player)` : choix résolu, revalidation à l'application et file des niveaux. L'ancienne signature `SelectFragment(string, string)` du plan 05 est périmée.
- [UpgradeRoller](../../scripts/Progression/UpgradeRoller.cs), `BumpSteps(float luck, ErasureManager.ErasureZonePhase phase, int peril)`, `RollRarity(float bumpSteps, RandomNumberGenerator rng)`, `RollGains(FragmentOption, Player, UpgradeRarity, RandomNumberGenerator)` : contrat actuel des raretés. Les nouvelles formules proposées en §6 ne sont pas déjà implémentées.
- `LootRewards.Resolve(List<LootResolver.LootResult>, PerkManager)` puis `Apply(in ResolvedLoot, Player, EventBus, Vector2)` : exemple de tirage concret avant affichage. `ResolvedLoot` doit être enrichi pour exposer description, rareté, nombre possédé et avant/après.
- [StatModifier](../../scripts/Progression/StatModifier.cs) : adaptateur vers `Player.ApplyPerkModifier`, **pas** un stockage des contributions par source.
- [PauseMenu](../../scripts/UI/PauseMenu.cs), `AddPassiveRow`, et [HubCollectionPanel](../../scripts/UI/HubCollectionPanel.cs), `PassiveEntries` : points de présentation à étendre. Le deuxième onglet Collection s'appelle encore « Souvenirs de run ».

Architecture : reprendre la séparation domaine/présentation/infrastructure et l'état par joueur de [VESTIGES-ARCHITECTURE](../VESTIGES-ARCHITECTURE.md) §2–5, 7–11. Les inventaires et le calcul des effets appartiennent à la progression du joueur, pas à un nouvel Autoload. Toute nouvelle API sera identifiée comme telle lors de l'implémentation.

## 3. Les perks — quatre choix de spécialisation

**Proposition : 16 types disponibles, quatre équipés, dix niveaux chacun.** Un nouveau type prend un slot. Reprendre le même type l'améliore. Au niveau maximum il sort des offres. Lorsque les quatre slots sont pleins, seuls les perks déjà équipés restent améliorables ; aucune cinquième acquisition cachée par coffre ou Mémorial.

Le niveau 1 est une acquisition neutre comme aujourd'hui. Les niveaux suivants tirent une rareté et conservent leur gain réel. Les valeurs ci-dessous sont le gain de base par niveau et le **total pour dix niveaux communs** ; elles ne décrivent pas dix multiplications successives. Les raretés amplifient les gains continus selon la table existante (×1 / 1,5 / 2 / 2,5 / 3), à recaler par mesure.

| ID proposé | Nom | Rôle et gain de base par niveau | Total au niveau 10, commun |
|---|---|---|---|
| `steady_force` | Force tranquille | +5 % dégâts de toutes les armes | +50 % dégâts |
| `repeated_gesture` | Geste répété | +4 % cadence | +40 % cadence |
| `weak_point` | Point faible | +2 points de chance de critique | +20 points |
| `vitality` | Tenir debout | +8 PV maximum, avec le même gain de PV courants | +80 PV |
| `protection` | Protection | +4 bouclier maximum, avec le même gain de charge | +40 bouclier |
| `recovery` | Reprise | +0,2 PV/s de régénération | +2 PV/s |
| `nomad` | Nomade | +2 % vitesse de déplacement | +20 % vitesse |
| `reach` | Allonge | +4 % portée des attaques compatibles | +40 % portée |
| `resonance` | Résonance | +4 % dimensions des zones d'attaque | +40 % rayon/largeur, pas +40 % surface |
| `multiplicity` | Démultiplication | +3 % dégâts ; +1 copie d'attaque aux niveaux 2, 4, 6, 8 et 10 | +30 % dégâts et +5 copies, sens par famille en §6 |
| `caliber` | Calibre | +4 % taille physique des projectiles | +40 % diamètre et hitbox |
| `collector` | Ramasseur | +8 % rayon d'attraction de l'XP | +80 % rayon, valeur d'XP inchangée |
| `learning` | Apprentissage | +4 % XP reçue | +40 % XP |
| `lucky_star` | Bonne étoile | +0,05 indice de Chance | +0,50 Chance |
| `edge_walker` | À la lisière | +4 % dégâts et +4 % XP, pondérés par l'oubli du lieu | Jusqu'à +40 % dégâts/XP ; nul en zone ancrée |
| `essence_gatherer` | Siphon | +5 % Essence gagnée | +50 % Essence |

Pourquoi dix niveaux : les perks continuent à progresser après les premiers choix, sans multiplier les emplacements. Le plafond de cinq niveaux actuel n'est pas une contrainte validée. Cette proposition fait passer le budget théorique d'un build complet d'environ **219 à 239 acquisitions/améliorations** à armes inchangées : `4 × 50 + 4 × 10 − 1 arme initiale`. Les acquisitions et améliorations obtenues dans le monde réduisent le nombre de level-ups nécessaires. Le calcul des niveaux de surplus R1-H doit suivre l'état réel du build, et ne jamais coder 219 ou 239 en constante.

**Démultiplication :** chaque niveau gagne des dégâts, même sans palier de copie. La rareté amplifie ce gain de dégâts ; les copies sont liées aux niveaux pairs et ne sont ni fractionnaires ni multipliées par la rareté. Le choix montre séparément les deux résultats. C'est une proposition qui remplace le saut de deux niveaux actuel des passifs entiers.

Les identités ont volontairement un effet principal lisible. L'intérêt vient des quatre places : prendre Apprentissage, Bonne étoile et À la lisière laisse une seule place pour défense, mobilité ou dégâts immédiats.

## 4. Les objets — 24 effets cumulables

Une définition possède une rareté fixe, un objet concret et une règle de cumul. Les mêmes objets peuvent revenir ; `n` est le nombre possédé. Les propositions de noms suivent le registre des objets ordinaires du plan 17 ; les textes de lore seront courts et factuels, sans résoudre les questions encore ouvertes du plan 19.

Un doublon ajoute sa contribution à l'effet total. Fréquences et délais indiqués ci-dessous sont communs à la pile ; les copies augmentent la puissance. Les dégâts secondaires ne redéclenchent aucun objet offensif. Les détails de ciblage et d'attribution sont en §6.

### Communs — dix renforts simples

| ID proposé | Objet | Effet avec n exemplaires |
|---|---|---|
| `wooden_wedge` | Cale en bois | +3 % × n dégâts |
| `alarm_spring` | Ressort de réveil | +3 % × n cadence |
| `coat_button` | Bouton de manteau | +6 × n PV maximum, gain de PV courants à l'acquisition |
| `thread_spool` | Bobine de fil | +0,1 × n PV/s |
| `red_shoelace` | Lacet rouge | +1,5 % × n vitesse de déplacement |
| `folding_ruler` | Mètre pliant | +4 % × n portée compatible |
| `copper_washer` | Rondelle de cuivre | +4 % × n rayon/largeur des zones |
| `speaker_magnet` | Aimant de haut-parleur | +8 % × n rayon d'attraction de l'XP |
| `class_photo` | Photo de classe | +5 % × n XP reçue |
| `eyeglass_lens` | Verre de lunette | +0,10 × n au multiplicateur des coups critiques |

### Inhabituels — six orientations

| ID proposé | Objet | Effet avec n exemplaires |
|---|---|---|
| `fair_token` | Jeton de fête foraine | +0,05 × n indice de Chance |
| `cracked_magnifier` | Loupe fendue | +5 % × n diamètre et hitbox des projectiles |
| `firefighter_badge` | Écusson de pompier | +5 × n bouclier maximum, gain de charge à l'acquisition |
| `worn_sole` | Semelle usée | Après 3 s de déplacement réel continu, prochaine attaque directe +15 % × n dégâts ; un seul bonus armé, pause ignorée, arrêt de plus de 0,5 s annule |
| `stopped_stopwatch` | Chronomètre arrêté | Une élimination directe donne +10 % × n cadence pendant 3 s ; les suivantes rafraîchissent la durée sans empiler le buff |
| `dented_medal` | Médaille cabossée | Sous 30 % des PV, +15 % × n dégâts ; un seuil commun, indépendant de l'ordre de ramassage |

### Rares — six règles de combat ou de risque

| ID proposé | Objet | Effet avec n exemplaires |
|---|---|---|
| `sewing_thimble` | Dé à coudre | Chaque élimination directe rend 0,5 × n PV ; remplace le vampirisme proportionnel aux dégâts dans le pool d'objets |
| `damp_match` | Allumette humide | 15 % de chance par impact direct d'enflammer : 3 × n dégâts/s pendant 3 s ; nouvelle application rafraîchit, ne multiplie pas les feux |
| `mirror_shard` | Morceau de miroir | Tous les six impacts directs, écho sur la cible du sixième pour 20 % × n des dégâts de référence ; aucun nouvel écho déclenché par l'écho |
| `cracked_marble` | Bille fêlée | 20 % de chance par impact direct de rebondir une fois sur une autre cible à moins de 120 px, pour 30 % × n des dégâts de référence |
| `workshop_badge` | Badge d'atelier | Quand un coup d'ennemi est accepté, riposte sur l'ennemi vivant le plus proche à moins de 60 px : 20 % × n des dégâts après armure, même si le bouclier les absorbe |
| `faded_id_photo` | Photo d'identité effacée | Jusqu'à +10 % × n XP et Essence sur les créatures abattues en zone oubliée, pondérés par l'oubli du lieu de mort |

### Épiques — deux effets structurants

| ID proposé | Objet | Effet avec n exemplaires |
|---|---|---|
| `unlabelled_tape` | Cassette sans étiquette | Toutes les dix éliminations directes, onde de rayon 120 px depuis le joueur : 50 % × n des dégâts de référence de l'attaque qui a terminé le compteur |
| `handless_watch` | Montre sans aiguilles | Premier impact direct après un dash : une seule onde d'écho, 0,3 s plus tard, de rayon 40 px, pour 50 % × n des dégâts de cet impact ; une charge par dash |

Ces deux effets bornent leur fréquence, pas les exemplaires ni leur puissance. Leurs effets visuels sont mutualisés et peuvent être regroupés sous forte densité sans supprimer les dégâts.

**Accès proposé :** tous les perks, communs et inhabituels accessibles initialement, ainsi qu'Allumette humide et Morceau de miroir. Les six autres objets rares/épiques se débloquent directement par des objectifs. Proposition de conditions : Dé à coudre, survivre 8 min ; Bille fêlée, 200 éliminations en une run ; Badge d'atelier, survivre à deux Résurgences dans une run ; Photo d'identité, 100 éliminations en zone oubliée cumulées ; Cassette, 1 000 éliminations cumulées ; Montre, toucher après 50 dashs cumulés. Ces conditions et leurs nouveaux compteurs restent à valider et à implémenter. Le mode dev expose tout. Aucun accès ne dépend d'un Souvenir de lore et les droits acquis du profil sont conservés.

## 5. Acquisition et présentation

| Situation | Comportement proposé |
|---|---|
| Level-up | Armes et perks ; slot libre nécessaire pour un nouveau type ; un type possédé s'améliore ; aucune acquisition d'objet dans cet écran |
| Coffre ordinaire de combat | Au moins un objet éligible garanti, accompagné des ressources/récompenses de sa table ; retrait des anciens « Dons » cachés ; détail du gain avant confirmation |
| Coffre de lore | Progression narrative conservée ; ne pas remplacer un fragment narratif par le nouveau catalogue de combat |
| Arme proposée avec quatre slots pleins | Échange volontaire clairement présenté ; l'objet garanti conserve l'intérêt du coffre même si l'arme est refusée |
| Élite/Souverain | Table d'objets configurable ; un objet garanti pour un Souverain, chance à calibrer pour une élite ; réutilisation des événements et sources existants |
| Doublon d'objet | Ajout immédiat à sa pile : nom, quantité avant/après, effet total avant/après |
| Pause | Quatre perks avec niveaux et gains cumulés ; inventaire d'objets défilant, quantité par ID et effet total ; distinction avec bénédictions/Oublis |
| Bilan et Collection | Objets trouvés, quantités ; fiche de cumul et condition de déblocage ; onglets nommés « Perks » et « Objets » |

La nouvelle garantie d'objet modifie l'économie : mesurer le nombre de coffres rencontrés et ouverts, les exemplaires par minute, les raretés et l'apport de puissance. La cible d'une source toutes les 20–30 s du plan 13 n'est pas présumée atteinte. L'inventaire ne dépend pas de la future mise en scène Vestige figé/Triptyque/Pacte.

Les cartes indiquent les armes compatibles et la valeur réelle : « Allonge : Pelle à neige, Lance-billes… ». Aucun bonus inapplicable ne doit être annoncé comme efficace sur tout l'équipement. Pour un perk spécialisé comme Calibre, proposer un nouveau type seulement si au moins une arme portée est compatible ; afficher les incompatibilités d'un objet trouvé sans le détruire, car un échange d'arme peut le rendre utile.

Icônes : suivre la [Charte](../CHARTE-GRAPHIQUE.md), taille actuelle items/perks 16×16, couleurs et silhouettes distinctes, zoom entier. L'amendement 32×32 des **armes** ne vaut pas autorisation implicite pour tous les objets. La rareté se lit par couleur et forme du cadre. Aucune planche de sprites produite à cette étape.

## 6. Contrat des effets et du cumul

### Contributions et ordre des calculs

Pour une même stat relative, les contributions de perks et objets **s'additionnent** : `multiplicateur = 1 + somme des bonus`. Les statistiques propres des armes, le personnage et les modificateurs de difficulté restent identifiés séparément. Exemple : Force tranquille +25 %, dix Cales +30 % donnent +55 %, donc ×1,55 ; pas `1,25 × 1,03^10`.

Le domaine conserve les contributions par source (type, ID, niveau/gains ou quantité), et calcule le résultat à l'acquisition, à l'amélioration et au changement de condition. Cela permet des aperçus identiques au calcul et évite de tenter d'annuler des produits opaques. Pas de recalcul du catalogue entier par image.

Pour les procs : `n = 1 / 2 / 10 / 100 / 1 000` augmente les dégâts ou le soin, jamais le nombre de timers/abonnements. Aucune limite de pile de design. Définir et tester la représentation numérique des très grandes quantités ; « infini » ne signifie pas ignorer les débordements machine.

### Portée, zone, calibre et copies : quatre axes distincts

| Famille | Allonge | Résonance | Calibre | +1 copie de Démultiplication |
|---|---|---|---|---|
| Mêlée en arc/estoc | Allonge réelle de frappe et ciblage | Largeur/angle de la zone frappée | Sans effet | Une frappe supplémentaire selon le motif, comme la logique actuelle |
| Projectile | Distance de trajet, durée de vie et ciblage cohérents | Zone d'explosion uniquement si l'arme en a une | Sprite et hitbox du projectile | Un projectile supplémentaire |
| Orbite | Rayon d'orbite | Zone d'impact si applicable | Taille de l'élément orbital et sa collision | Un élément orbital supplémentaire |
| Chaîne | Portée d'acquisition et de saut | Zone d'impact seulement si réellement présente | Sans effet | Un saut supplémentaire, sans revenir sur une cible déjà touchée dans cette chaîne |
| Cône continu | Longueur du cône | Largeur/angle | Sans effet | Une répétition logique des dégâts par cycle, agrégée ; pas une nouvelle recherche de cibles pour chaque copie |
| Onde circulaire | Sans effet d'allonge supplémentaire | Rayon de l'onde | Sans effet | Une répétition logique de la frappe, agrégée |

Ce tableau est un **contrat proposé**, pas un constat de fonctionnement actuel. Il impose de vérifier chaque arme JSON, les limites d'angles et les motifs atypiques. Les dimensions sont linéaires : +40 % de rayon donne +96 % de surface circulaire. Pour une dimension angulaire bornée, proposition explicite de conversion : la part du bonus qui dépasse la limite propre à l'arme devient un bonus additif de dégâts sur cette arme, à raison de +1 % de dégâts pour +1 % de dimension inutilisable. Exemple : bonus de zone +80 %, limite géométrique atteinte à +50 % → +50 % de dimension et +30 % de dégâts sur cette arme. La carte et la pause montrent cette conversion ; les copies suivantes de Rondelle restent utiles. Les autres armes conservent leur croissance de zone normale. Les copies d'attaque se calculent même si les VFX sont regroupés. Les gros nombres de projectiles restent un sujet de banc ; aucun gain de performance n'est présumé.

### Déclencheurs offensifs

- Un impact direct est un coup d'arme ; un effet d'objet, brûlure, écho, rebond, onde ou riposte est secondaire. Un événement conserve le joueur, l'arme source et l'origine. Les dégâts secondaires restent attribués au build et au bilan, sans récursion de procs.
- Une attaque touchant plusieurs cibles produit un impact par cible. Les copies agrégées transportent leur nombre d'impacts logiques ; elles ne déclenchent pas le même lot une deuxième fois dans la couche VFX.
- Les dégâts de référence sont les dégâts du coup d'arme après ses bonus et critique, avant les dégâts d'objet ; on ne multiplie pas à nouveau ces derniers par les stats du joueur. Une mort n'incrémente qu'une fois un compteur. Seuls les compteurs d'objets exigeant une élimination directe filtrent la source fatale : les éliminations secondaires du joueur restent valables pour XP, score, quêtes, statistiques et attribution.
- Un effet sur la cible d'un impact ne la ressuscite pas. Si elle est déjà morte, l'écho du Morceau de miroir sur cette cible est perdu ; rebond et onde peuvent encore chercher leurs propres cibles vivantes. La Montre mémorise la position du premier impact et y produit une seule onde, que la cible initiale soit encore vivante ou non ; interruption à la mort du joueur/fin de run, temps suspendu en pause. Un nouveau dash ne cumule pas de charges ; il réarme la charge unique si elle a été consommée.
- La Semelle porte sur la prochaine attaque d'une arme, identifiée par un ID d'attaque : le gain est fixé pour toutes ses frappes/projectiles, sans bénéficier aussi à une autre arme tirant dans la même image. Pour le cône, une attaque correspond à un cycle de dégâts, pas à toute la durée d'émission. La Montre ne se répète pas pour chacun des projectiles de cette attaque : seul le premier impact direct consomme sa charge.
- L'Allumette garde une chance fixe ; les piles augmentent ses dégâts. Les applications successives rafraîchissent un effet attribué à sa source ; les brûlures d'armes et d'objets ne doivent pas s'écraser par ordre arbitraire.
- La riposte exclut le Néant, l'environnement et les coups ignorés pendant l'invulnérabilité. Son texte annonce bien la cible proche, pas forcément l'attaquant.

### XP, Essence et oubli

Les bonus de build ont un propriétaire unique. Proposition : appliquer le bonus général Apprentissage/Photo de classe au moment du gain d'XP dans la progression, une fois ; les orbes transportent l'XP de source sans ces bonus. Le Péril doit rester appliqué une seule fois dans sa couche existante. Même règle pour le bonus général Siphon sur le gain d'Essence.

La Photo d'identité emploie le lieu de **mort de la créature** ; son bonus conditionnel est fixé sur sa récompense à la création et ne change plus en déplaçant l'orbe. À la lisière évalue les dégâts au lieu du joueur au moment de l'attaque ; pour l'XP, il emploie le contexte de mort comme la Photo. Les gains d'XP sans créature n'ont pas de bonus de lieu. Ces contributions de build conditionnelles s'additionnent aux contributions générales, sans se multiplier entre elles ; le payload de récompense doit conserver ce contexte pour les appliquer une seule fois.

Pondération proposée : `risque = clamp((0,75 - mémoire) / 0,50, 0, 1)` : 0 à 75 % de mémoire, 0,5 à 50 %, 1 à 25 % et moins. La référence est la mémoire locale, pas le nombre d'Oublis de Failles. Le Néant reste dangereux et traversable selon ses règles ; aucun de ces objets ne le neutralise.

Les multiplicateurs de source R1-A (temps, oubli au spawn, Résurgence) restent ceux du plan 20 §6.9 et sont calculés séparément du build. Ils ne sont pas rejoués au ramassage. Les récompenses de boss exprimées en **niveaux garantis** gardent leur nombre de niveaux ; ne pas les convertir en XP puis multiplier par le build.

Exemple de build, hors risque et difficulté : Apprentissage niveau 10 commun (+40 %) et quatre Photos (+20 %) donnent **×1,60 XP**. Avec À la lisière niveau 10 commun au risque maximal (+40 %), le total devient **×2**, pas ×2,24. Les 300–400 niveaux d'une excellente run restent une cible du plan 20, pas une conséquence démontrée de ce catalogue.

### Chance et seuils

La Chance agit sur la qualité des améliorations et du butin ; elle n'augmente ni la chance de critique, ni l'esquive, ni les procs des objets. Les objets gardent leur rareté fixe : la Chance aide à trouver un objet d'une catégorie plus rare.

Proposition pour supporter une Chance non plafonnée sans boucle par exemplaire : poids d'un rang `r = poids_de_base × (1 + L)^r`, avec `L >= 0` l'indice total de Chance. Réutiliser ensuite les promotions dues à l'oubli/Péril et les minima garantis, une seule fois. Calcul stable normalisé sur les seuls rangs éligibles ; aucun poids négatif comme pourrait en produire l'interpolation historique avec `luck > 1`. À grande Chance, le rendement sur la qualité décroît mais reste croissant mathématiquement ; vérifier la précision numérique aux piles de stress.

**Calcul théorique** avec les poids actuels d'amélioration 60 / 25 / 11 / 3,5 / 0,5, avant oubli/Péril :

| Indice L | Rare ou mieux | Légendaire |
|---:|---:|---:|
| 0 | 15,00 % | 0,50 % |
| 0,25 | 21,67 % | 1,05 % |
| 0,50 | 28,62 % | 1,85 % |
| 1,00 | 42,11 % | 4,21 % |
| 2,00 | 63,41 % | 10,98 % |

Ces résultats proviennent de la formule proposée, pas de runs mesurées. Les sources d'objets n'ont pas encore de légendaire : normaliser sur leurs rangs disponibles, sans tirage vide. Le modèle existant de promotions par Chance sera remplacé sur ces chemins, et non ajouté au nouveau calcul.

Point faible est borné par les niveaux, mais d'autres sources pourraient porter le critique au-delà de 100 %. Proposition : chaque tranche complète garantit un degré de critique, la fraction donne la chance d'un degré supplémentaire ; un degré ajoute le même bonus `(multiplicateur_critique - 1)` aux dégâts, sans exponentiation. Afficher cette règle si le seuil est atteint. Aucun objet de ce premier catalogue n'ajoute de chance d'esquive, de seuil d'exécution, de résurrection ni d'armure plafonnée.

## 7. Correspondance avec les effets actuels

Toutes les lignes sont des **destinations proposées**, pas des migrations déjà effectuées. Un ancien ID peut avoir un alias pour les données historiques sans rester dans les tirages. Préserver les passifs innés et les données méta ; ne pas convertir silencieusement une sauvegarde historique en effets de la run courante.

### Anciens perks : les 44 IDs

| IDs actuels | Destination proposée |
|---|---|
| `damage_up` | Cale en bois ; spécialisation par Force tranquille |
| `speed_up`, `traqueur_swiftness` | Lacet rouge ; Nomade |
| `hp_up` | Bouton de manteau ; Tenir debout |
| `attack_speed_up`, `forgeuse_quick_craft` | Ressort de réveil ; Geste répété |
| `extra_projectile` | Démultiplication ; fermeture de l'ancien bonus parallèle |
| `aoe_up` | Rondelle de cuivre ; Résonance |
| `armor_up` | Retiré du butin ; Protection reprend le rôle défensif avec le bouclier, armure des personnages/bénédictions conservée |
| `regen_up` | Bobine de fil ; Reprise |
| `range_up` | Mètre pliant ; Allonge |
| `xp_magnet` | Aimant ; Ramasseur |
| `lucky` | Jeton ; Bonne étoile |
| `vampirism`, `forgeuse_recycler` | Dé à coudre : soin à l'élimination proposé en remplacement du soin proportionnel aux dégâts |
| `berserker`, `forgeuse_last_wall` | Médaille, seuil commun à 30 % |
| `piercing_shot`, `traqueur_piercing` | Retirer du pool global ; perforation conservée dans les armes qui en disposent et leurs améliorations |
| `ricochet` | Bille, chance fixe et puissance par pile |
| `crit_chance` | Point faible |
| `crit_damage` | Verre de lunette |
| `ignite` | Allumette, chance fixe et puissance par pile |
| `thorns` | Badge d'atelier, cible réellement annoncée |
| `execution` | Retiré du pool global ; pas de seuil d'exécution infini ; effets propres d'armes conservés |
| `kill_speed` | Chronomètre, buff unique rafraîchissable |
| `channeling`, `siphon`, `instability` | Définitions déjà inactives à retirer ; le nouveau Siphon implémente réellement le gain d'Essence |
| `second_wind` | Retiré du pool commun proposé ; résurrection éventuelle réservée à une future identité de personnage, pas un objet à vies infinies |
| `glass_cannon` | Retiré du pool commun ; risque volontaire porté par Failles/Oublis et À la lisière |
| `vagabond_harvest`, `forgeuse_fortify`, `traqueur_precision` | Innés inchangés et hors slots, isolés du pool de loot |
| `vagabond_adaptability`, `vagabond_jack_of_all`, `forgeuse_overcharge` | Combinaisons des objets dégâts/cadence/vitesse ; retirer les doublons réservés à un personnage |
| `vagabond_survivalist`, `forgeuse_reinforce` | Bouton + Bobine ; retirer les doublons de pool |
| `vagabond_nomad` | Lacet + Aimant |
| `vagabond_scrounger` | Jeton + Aimant |
| `traqueur_ambush`, `traqueur_marked` | Déjà inactifs, à retirer ; Semelle propose un effet de mouvement distinct, sans exiger d'attendre immobile |
| `traqueur_evasion` | Retiré du pool commun ; conserver les caractéristiques innées éventuelles, pas d'objet menant à une esquive garantie |

### Six synergies

`synergy_blood_rage`, `synergy_crit_master`, `synergy_essence_storm`, `synergy_glass_berserker`, `synergy_ricochet_crit`, `synergy_executioner` : retrait des annonces et des définitions du pool. Aucun effet caché n'est attribué à leurs noms. Les combos à essayer en §8 résultent des effets documentés des composants.

### Quatorze passifs

| ID actuel | Destination proposée |
|---|---|
| `flamme_interieure` | Force tranquille |
| `memoire_vive` | Geste répété ; ne pas réutiliser cet ID pour l'XP |
| `ancrage` | Tenir debout |
| `instinct` | Nomade |
| `resonance` | Résonance |
| `siphon_essence` | Ramasseur ; nouveau Siphon sous un ID distinct |
| `peau_dure` | Protection, changement d'armure vers bouclier à valider |
| `oeil_critique` | Point faible |
| `regeneration` | Reprise |
| `portee_etendue` | Allonge |
| `souffle_du_neant` | Démultiplication |
| `fragment_deternite` | Désactivé, ne pas réactiver ; cadence couverte par Geste répété |
| `reflet_brise` | Retirer du pool global ; perforation par les armes |
| `carapace` | Protection |

Les retraits de résurrection, exécution, esquive, perforation globale et Canon de verre sont de **vrais choix de contenu à valider**, pas un nettoyage technique implicite. Ils réduisent les cas de saturation et donnent de la place aux nouveaux axes ; Raphaël peut en retenir certains et demander une autre règle de cumul.

## 8. Builds de référence à éprouver

| Intention | Les quatre perks | Objets recherchés | Coût du choix |
|---|---|---|---|
| Apprendre au bord de l'oubli | Apprentissage, Bonne étoile, À la lisière, Nomade | Photos de classe/identité, Jetons, Lacets | Très peu de défense ; puissance immédiate sacrifiée à la croissance |
| Tenir une foule à distance | Résonance, Démultiplication, Geste répété, Protection | Rondelles, Ressorts, Cassette | Moins de portée/XP/chance ; coût du combat dense à mesurer |
| Toucher fort et précisément | Point faible, Force tranquille, Allonge, Calibre | Verres, Mètres, Loupe, Bille | Dépend d'armes compatibles ; moins de marge de survie |
| Traverser et durer | Tenir debout, Reprise, Nomade, Ramasseur | Boutons, Bobines, Semelles, Dé à coudre | Survie et récolte au détriment du DPS ; ne doit pas rendre le Néant habitable |

Essais croisés : mêmes armes, personnage, seeds et parcours ; comparer sans objets, puis quantités identiques et composition différente. Mesurer les niveaux, dégâts effectifs, temps pour tuer, soins utiles, morts, exemplaires acquis et contribution des procs. Aucun de ces builds n'est annoncé équilibré avant mesure.

## 9. Lots d'implémentation après validation du contenu

Un lot à la fois. Les premiers lots établissent des contrats vérifiables ; la verticale est livrée quand tout le catalogue validé passe par acquisition → inventaire → effet → bilan. Pas de limitation de produit à trois objets.

| Lot | Travail | Références à lire et motifs à reprendre | Vérification / garde-fous |
|---|---|---|---|
| **OP0 — Contrats et données** | Définir catalogues, contributions par source, inventaire agrégé, origines de dégâts et compatibilités ; faire approuver les choix §10 ; tables de migration | Loader passifs pour parsing/cache ; `ActivePassiveSouvenir` pour aperçu ; §6–7 ; Architecture §5/7/10 | Validation des IDs, raretés, gains positifs par niveau, migrations et références ; aucun nom d'API future présenté comme existant ; aucune table ancienne supprimée tant qu'un consommateur subsiste |
| **OP1 — Perks et calcul commun** | Implémenter les 16 perks validés et leurs niveaux, dont XP/Chance/oubli ; appliquer contributions communes, Démultiplication et Calibre avec matrice des armes | `AddOrUpgradePassive`, `FragmentOption.ApplyTo`, `UpgradeRoller`, `StatModifier`, plan 20 §6.9 | Refus du cinquième type, niveau max filtré, aperçu = effet, pause/retour Hub, aucun niveau vide ; tests de limites de portée/hitbox ; préserver la réserve de niveaux en cours |
| **OP2 — Inventaire et 24 effets objets** | Brancher les objets validés au calcul commun, puis les déclencheurs ; conserver attribution et effet réel malgré VFX regroupés | `Player.OnAttackHit`, `CombatPools`, `Enemy.ApplyIgnite`, `PlayerDefense` ; §4/6 | Piles 1/2/10/100/1 000, ordre d'acquisition, absence de récursion, mort/recyclage/fin de run ; nœuds et abonnements indépendants du nombre d'exemplaires ; aucun effet laissé en simple texte |
| **OP3 — Acquisition et déblocages** | Tables cohérentes coffres/élites/Souverains ; récompenses résolues avant affichage ; accès directs des objets ; fermer anciens Dons et synergies | `LootRewards.Resolve/Apply`, `LootResolver`, `QuestManager`, `MetaSaveManager`, plan 06 | Profil neuf/ancien/dev, droits acquis, tables sans offre vide, doublons utiles, coffres avec quatre slots pleins ; un perk ne peut pas contourner les quatre slots ; aucune nouvelle source de monde nécessaire |
| **OP4 — Présentation et identité** | Icônes, level-up, révélation, pause, bilan et Collection cohérents ; traductions et valeurs totales depuis le domaine | `PauseMenu.AddPassiveRow`, `HubCollectionPanel.PassiveEntries`, `ChestLootScreen`, Charte et plan 04 | Captures à 720p/1080p, souris/clavier/manette, très grandes piles, libellés avant/après et compatibilités ; aucune formule gameplay dupliquée dans l'UI |
| **OP5 — Équilibre et recette** | Simuler la Chance et l'XP, mesurer la densité de butin et les quatre builds ; recaler les valeurs en JSON ; retirer le code réellement devenu mort | `tools/progression_model.py`, `tools/measure_run.sh`, banc dense, régressions armes/mouvement/dev ; §8 | Avant/après mêmes seeds/bancs ; build zéro avertissement, smoke, captures de run et Hub ; pas de promesse de 60 FPS sans mesure au calme ; cocher uniquement les items implémentés et vérifiés dans V2 §25 |

Le socle doit rester intégrable à chaque lot. Les contenus incomplets restent désactivés plutôt que proposés au joueur. Aucune action de ce document n'autorise à remplacer les changements de feu, XP, réserve ou performance déjà présents dans l'arbre de travail.

### Recette finale minimale

- Acquisition de quatre perks puis tentative de cinquième, évolution jusqu'au niveau 10, ban/reroll, refus d'une offre devenue invalide.
- Les 24 effets validés vérifiés contre leur description ; gain d'une pile de 1 à 2 et à 1 000 ; acquisition dans des ordres différents donnant le même état final.
- Chaque famille d'attaque contrôlée aux limites de ciblage et de collision ; copies/procs agrégés conservant dégâts et attribution.
- XP de créature/quête/coffre/boss, Péril et bonus de zone comptés une seule fois ; orbes laissées puis ramassées ailleurs ; niveaux garantis inchangés.
- Tirages à Chance nulle, forte et très forte, minima et catalogues partiellement débloqués ; résultats calculés puis simulation à graine fixe.
- Aucun effet d'objet présent après retour Hub ; innés du personnage réappliqués exactement une fois ; historique et droits méta conservés.
- Grep des anciennes sources de `LootReceived("perk", ...)`, annonces de synergies sans effet et accès à `MaxStacks` sur les objets ; chaque occurrence restante justifiée.
- Validation humaine : comprendre ses quatre choix, identifier une pile utile, percevoir sa puissance, vouloir une nouvelle combinaison.

## 10. Arbitrages pour Raphaël

1. **Catalogue et rôles :** 16 perks de spécialisation et les 24 objets proposés. Noms et effets peuvent être discutés par groupe ; aucune production d'icônes avant leur validation.
2. **Progression :** dix niveaux par perk, rareté sur les gains, quatre places fixes, copies de Démultiplication aux niveaux pairs. Recommandation : retenir cette base pour prolonger leur progression.
3. **Effets retirés/revus :** soin à l'élimination remplaçant le vampirisme, suppression de la résurrection/exécution/esquive/perforation globales et de Canon de verre dans ce catalogue ; armure des passifs remplacée par bouclier. Recommandation : examiner particulièrement ces changements avant implémentation.

Les coefficients, tables de butin et conditions de déblocage servent de points de départ ajustables par les mesures. La priorité objets/perks et la règle quatre slots/objets illimités sont déjà acquises : il n'est pas demandé de les revalider.

## 11. Réexamen de l'identité et de la structure — 28 septembre

### Retour de Raphaël

Après lecture, Raphaël trouve certains perks/objets trop proches de l'identité des armes et questionne la proximité avec Megabonk : quatre armes, quatre tomes renommés perks, objets illimités. Il demande si cette formule a été inventée par Megabonk, si sa reprise est appropriée et s'il faut envisager une autre structure. Cette question rouvre la discussion sur la fonction des catégories ; elle ne valide ni leur suppression ni une variante précise. Le §10 décrit les arbitrages demandés avant ce nouveau retour.

### Chevauchements identifiés

- Chronomètre arrêté / Montre sans aiguilles face à l'arme Chronomètre : même famille d'objet et vocabulaire du temps.
- Cassette et onde offensive face au Transistor et à la Cloche : parenté de support sonore et d'effet de combat.
- Bille fêlée face au Lance-billes pour l'identité visuelle, et au Trousseau pour la propagation entre cibles.
- Morceau de miroir qui répète les coups face aux Gants de boxe ; Allumette qui enflamme face à la Lampe à pétrole. Une interaction peut être intéressante, mais rendre le même effet accessible à tout l'arsenal affaiblit la raison de choisir l'arme qui en faisait sa signature.
- Les perks de dégâts/cadence/portée et les objets correspondants alimentent les mêmes statistiques par deux interfaces. La seule différence de provenance ou de limite de slots ne suffit pas à justifier chaque doublon.

Un objet courant peut parfaitement être une arme ou un objet passif dans cet univers. Le critère proposé est la distinction de silhouette, de fonction et de décision pour le joueur. Une affinité thématique est acceptable ; reproduire simultanément le motif offensif, le nom et l'iconographie est un signal de révision.

### Antériorités et limite de la recherche

Les briques sont antérieures à Megabonk (sorti le 18 septembre 2025, [fiche de l'auteur sur Steam](https://store.steampowered.com/app/3405340/Megabonk/)) :

- [Risk of Rain, 2013](https://store.steampowered.com/app/248820/Risk_of_Rain_2013/) : collecte d'objets et puissance croissante ; [Risk of Rain 2](https://store.steampowered.com/app/632360/Risk_of_Rain_2/), disponible en accès anticipé en 2019, met explicitement en avant l'accumulation et les combinaisons d'effets.
- [Vampire Survivors](https://store.steampowered.com/app/1794680/Vampire_Survivors/), accès anticipé en décembre 2021 : armes et améliorations passives. Le système standard de six armes/six passifs est décrit dans [cette synthèse du jeu](https://en.wikipedia.org/wiki/Vampire_Survivors) ; il comporte des exceptions, ce n'est pas un plafond absolu dans toutes les situations.
- [Brotato](https://store.steampowered.com/app/1942280/Brotato/), accès anticipé en septembre 2022 : jusqu'à six armes, traits et objets, avec sa propre économie de vagues/boutique.

Ces antériorités établissent que Megabonk n'a inventé ni les armes multiples, ni les bonus passifs, ni l'accumulation d'objets. Elles ne permettent pas d'attribuer la première occurrence exacte de l'assemblage « 4 / 4 / illimité ». La conclusion de conception proposée est qu'une convention de genre peut être pertinente, mais que multiplier les correspondances de structure, d'effets et d'acquisition rend la comparaison plus forte. Ce n'est pas une conclusion juridique.

### Trois directions à comparer, sans choix acté

| Direction | Changements | Intérêt | Limite |
|---|---|---|---|
| A — Conserver le catalogue de stats | Quatre perks de stats à niveaux et objets cumulables ; revoir noms et doublons | Familiarité et continuité avec le code | La parenté avec le rôle des tomes reste forte ; le simple renommage ne change pas les décisions |
| B — Perks de comportement | Garder jusqu'à quatre perks, mais chaque perk modifie une règle de jeu ; les gains chiffrés ordinaires viennent des armes et des objets | Trois rôles plus distincts : attaque, règle de build, accumulation ; permet des choix propres au déplacement et à l'Effacement | Reconcevoir acquisition/progression des perks ; ne pas ajouter seulement une condition à un +5 % |
| C — Retirer l'inventaire de perks | Armes et objets cumulables, améliorations au level-up ; quelques bifurcations qualitatives à des étapes de run | Supprime une couche redondante et permet d'autres moments de spécialisation | Changement plus large ; vérifier que les objets aléatoires ne remplacent pas trop de choix volontaires |

**Recommandation de l'agent, à discuter : explorer B d'abord**, car elle préserve le souhait de quatre spécialisations choisies et l'accumulation illimitée tout en changeant la fonction des perks. Les armes portent les attaques et leurs signatures ; les objets portent la puissance cumulée et des interactions complémentaires ; les perks changent la manière de tirer parti de ce build. Une portion du catalogue peut rester simple (+PV, +XP) ; tout n'a pas besoin d'une mécanique complexe ou d'une référence à l'oubli.

Exemple de direction pour un perk, non spécifié : conserver brièvement les avantages d'une zone oubliée après l'avoir quittée. Cela change le trajet et les fenêtres de combat, avec des conditions à définir, au lieu de n'augmenter que le coefficient d'un bonus de zone. Les choix qualitatifs peuvent aussi concerner les priorités de ciblage ou l'utilisation de l'Essence ; ne pas réserver tous les builds à la lisière.

Avant de reprendre les chiffres et les noms, décider quel choix exclusif apporte chaque catégorie. Changer quatre emplacements en trois ou cinq ne résout pas à lui seul la proximité de structure. Aucun inventaire, effet, plafond ni droit acquis n'est modifié à ce stade.

## 12. Trois boucles complètes de progression — étude du 28 septembre

### 12.1 Ce qui doit rester plaisant dans les trois variantes

La demande est d'aller au bout des trois réflexions, avant de choisir. Cette section décrit leurs règles et conséquences ; aucune variante n'est implémentée ni validée par Raphaël.

Invariants pour les comparer : quatre armes au maximum ; objets cumulables sans limite de slots/exemplaires ; combat automatique, mouvement et exploration ; risques payants ; XP, Chance et oubli exploitables ; cascades de niveaux et réserve automatique ; puissance très élevée possible ; aucune perte imposée d'objets déjà ramassés. La suppression des quatre perks n'est étudiée que dans C et demanderait un nouvel arbitrage explicite.

Une catégorie mérite d'exister si elle propose une décision identifiable. Dans A, on choisit **où investir ses niveaux**. Dans B, on choisit **quelles règles appliquer à son build**. Dans C, on choisit **comment faire évoluer ses armes et orienter ses trouvailles**.

L'originalité ne se mesure pas au nombre de systèmes inconnus. Les critères utiles sont : trajectoires différentes, choix de cibles, sacrifices de récompenses, préparation d'un affrontement et combinaisons de puissance que le joueur comprend. Une idée qui paraît singulière à l'écrit mais ne change aucun de ces comportements mérite d'être retirée.

### 12.2 Frontières d'identité communes

| Couche | Ce qu'elle porte | Exemples de frontière |
|---|---|---|
| Personnage | Pouvoir personnel, mobilité/signature, contraintes de départ | La transmission de marque du Traqueur, le double de l'Éveillée ou la réserve d'air de la Scaphandrière restent leurs identités proposées |
| Arme | Geste d'attaque, forme, rythme, portée, interactions propres | Cloche : onde et recul ; Transistor : cône continu ; Gants : double coup ; Trousseau : chaîne |
| Objet | Puissance cumulée, ressources du build, interactions complémentaires | Vitesse, XP, Chance, soins, portée ; renforcement d'un état déjà produit par une arme |
| Perk de B | Règle qui réorganise cette puissance ou son acquisition | Priorité de ciblage, report de dégâts perdus, ramassage sur un trajet, arbitrage objet/XP |

Le partage d'une statistique ou d'un thème n'est pas interdit. Une arme qui soigne peut coexister avec de la régénération ou un objet de soin. Le signal d'alerte est qu'un objet donne à toutes les armes un motif offensif qui rend la signature de l'une d'elles superflue. Le test est : « ai-je encore une raison de choisir cette arme après avoir obtenu cet objet ? »

Conséquences sur le premier catalogue : revoir Chronomètre arrêté, Montre, Cassette, Bille, Miroir et Allumette ensemble avec les armes concernées. Des accessoires portés, marques d'usage, petits objets personnels ou éléments de vêtement donnent des silhouettes différentes des outils brandis par le joueur. Noms et icônes viennent après le rôle. Il n'est pas nécessaire que tous les objets parlent de mémoire ou déclenchent un effet spectaculaire.

**Révision de la proposition de retrait :** résurrection, exécution, esquive, perforation globale et vampirisme ne sont pas rejetés automatiquement parce que les piles sont illimitées. Il faut décider pour chacun si l'on conserve sa règle, renforce sa puissance plutôt que sa probabilité, ou le réserve à une arme/un personnage. Les suppressions du §7 restent des hypothèses de la première proposition, pas une condition de B ou C.

### 12.3 Direction A — Spécialisations chiffrées et butin cumulable

**Contrat.** Quatre armes, quatre perks de stats améliorables, objets sans plafond. Le level-up offre nouvelles armes/perks ou améliorations ; les coffres et créatures apportent les objets. Les perks sont un investissement volontaire, les objets une puissance saisie dans le monde. Dix niveaux par perk restent l'hypothèse de comparaison de A.

Pour renforcer A, on donne aux perks des axes suffisamment décisifs : croissance XP, qualité des trouvailles, vitesse d'attaque, résistance, couverture… Les objets peuvent partager une stat, mais leur provenance crée une autre décision. Prendre un perk d'XP coûte des niveaux qui auraient pu améliorer une arme ; ramasser un objet d'XP coûte surtout le détour et le risque. Cette différence est réelle et peut justifier la coexistence.

**Une run.** Je choisis tôt XP et Chance, puis je complète avec cadence et protection. J'accepte un début plus faible pour développer mon équipement. En cours de run, je décide à chaque niveau entre améliorer une arme et renforcer mon investissement. Les objets trouvés consolident cette direction ou me donnent une raison de changer d'arme. Aux quatre perks pleins, mes choix restent dans ces quatre axes.

**XP/Chance/oubli.** Les perks garantissent un moyen de poursuivre ces axes même si les objets tardent. Le risque et les Résurgences multiplient les récompenses de source ; les objets et perks ajoutent la contribution du build. Les Mémoriaux et Failles servent à accélérer cette progression selon les règles existantes.

**Puissance et endgame.** Les gains sont fréquents et immédiatement lisibles. Le catalogue de stats apporte beaucoup de choix aux cascades. Une fois armes/perks complets, les objets continuent de croître et les niveaux passent au surplus prévu au plan 20. Les bonnes runs se distinguent par les investissements précoces, les prises de risque et la qualité du butin.

**Ce qu'A réussit.** Lecture rapide, investissement contrôlable, calcul d'équilibrage assez direct, continuité avec les quatre slots actuels. Ce serait une vraie option de production si le plaisir de croissance quantitative est prioritaire.

**Son coût de design.** La parenté avec les tomes reste assumée. Plusieurs cartes peuvent se résumer au même « +% » et demander surtout un calcul de rendement. L'identité de Vestiges doit alors venir fortement de la navigation, de l'Effacement et des lieux. Remplacer les icônes ou modifier le nombre de slots ne change pas ce diagnostic.

**Condition de réussite.** Le joueur décrit un choix d'investissement (« j'ai retardé mes dégâts pour accélérer ma progression ») et le ressent. Si la sélection devient une routine identique dans toutes les runs, A a peu de marge pour donner des spécialisations mémorables.

### 12.4 Direction B — Quatre règles de build, sans niveaux de perk

**Contrat proposé.** Quatre armes ; jusqu'à quatre perks qualitatifs acquis une seule fois ; objets cumulables. Un perk ne possède ni dix niveaux, ni gain de rareté, ni doubles à collectionner. L'arme progresse en puissance et conserve ses améliorations à rareté ; l'objet augmente sa contribution avec ses exemplaires ; le perk modifie la manière de les utiliser ou de les obtenir.

Une description de perk contient un changement de règle observable. « +10 % de dégâts quand je marche » est trop proche d'un objet conditionnel pour justifier un emplacement de spécialisation à lui seul. Les règles peuvent avoir des coefficients pour l'équilibrage ; c'est leur conséquence sur les décisions qui justifie la catégorie.

#### Acquisition et rythme

- Hypothèse d'essai : un choix parmi trois perks aux niveaux **2, 6, 12 et 20**, en remplacement du choix ordinaire de ce niveau, dans le même écran. Quatre moments lisibles ; leur calendrier devra suivre les nouvelles mesures d'XP.
- Premier choix : au moins une option de survie applicable, une de combat et une de progression/exploration. Les choix suivants évitent les doublons et les règles incompatibles avec l'équipement. Préserver ainsi la fonction de `EnsureSurvivalChoice`, au lieu de la perdre avec les passifs chiffrés.
- Les trois offres doivent avoir un usage dans la run en cours. Les perks dépendant des relances, des armes ciblées ou d'un service du monde annoncent leur condition. Exclure un candidat réellement inutilisable ; ne pas interdire un build seulement parce qu'il n'est pas optimal.
- Les relances/bannissements restent ceux du système commun ; aucune monnaie nouvelle. Si le joueur passe, l'occasion de remplir ce slot reste disponible dans une offre ultérieure, sans lui rouvrir immédiatement l'écran. Les quatre paliers sont quatre droits d'acquisition, pas quatre obligations.
- Les choix sont conservés jusqu'à la fin de la run. Pas de service de respécialisation dans l'essai initial : profiter d'une conversion d'économie puis échanger gratuitement son perk contre un perk de combat brouillerait son coût.
- Les autres niveaux améliorent les armes. Les perks ne reviennent pas encombrer chaque cascade. Un saut de quinze niveaux déroule normalement les améliorations et les paliers de perk effectivement franchis, sans créer quinze nouvelles règles.

Ce rythme change le rôle des perks XP/Chance précédemment envisagés : l'axe est conservé, mais son action devient qualitative. Il demande une validation nouvelle, tout comme l'abandon des dix niveaux proposés. Les chiffres 2/6/12/20 sont un point de départ, pas une cadence déjà mesurée.

Le quatrième choix n'est pas nécessairement un choix de début de run : les trajectoires visées au plan 20 placent le niveau 20 à des moments très différents selon la réussite. Vérifier aussi que remplacer le premier gain offensif par une règle au niveau 2 n'affaiblit pas le départ. Des paliers fixes rendent l'acquisition prévisible ; ils ne garantissent pas un rythme identique entre runs.

#### Sept exemples suffisamment précis pour être éprouvés

Ce sont des fiches de conception, **pas un catalogue de lancement fixé à sept perks**. Elles couvrent combat, défense, collecte, économie, Chance et oubli sans multiplier les systèmes du monde. Les valeurs ci-dessous servent seulement à rendre les conséquences discutables.

| Perk provisoire | Règle proposée | Décision qui change | Contrepartie / limite |
|---|---|---|---|
| **Convergence** | Les armes à ciblage automatique privilégient une élite ou un Souverain à portée avant les créatures ordinaires | Construire un arsenal qui abat une menace prioritaire ; se placer pour la garder à portée | La foule continue d'approcher ; aucun effet sur un motif sans ciblage. Ne change ni la marque du Traqueur ni les règles de rebond propres à une arme |
| **Débordement** | Une fraction des dégâts excédant les PV restants est réservée au prochain coup direct de la même arme | Valoriser une arme lourde au milieu de petites cibles, préparer un gros coup avant une cible résistante | Une réserve bornée par arme, durée courte, pas de nouveau projectile ; le report précédent ne peut pas produire un nouveau report |
| **Prévoyance** | Les soins excédentaires remplissent une petite réserve de PV, rendue après un coup non fatal qui a réellement entamé les PV | Entrer dans un combat préparé ; donner une utilité aux soins trouvés ou produits à pleine vie | Pas de résurrection, pas de remplissage du bouclier, réserve finie ; consommer la réserve ne la remplit pas à nouveau |
| **Sillage** | Les orbes d'XP laissées sur la portion récente du trajet sont encore ramassables automatiquement après le passage du joueur | Tuer en avançant et dessiner un trajet de collecte, plutôt que revenir chercher les orbes derrière soi | Couloir local et temporaire, aucune attraction globale ; ne crée aucune XP et n'active pas coffres/lieux à distance |
| **Délestage** | Une récompense d'objet révélée peut être refusée avant acquisition contre une somme d'XP affichée | Sacrifier de la puissance cumulative future pour accélérer ses armes maintenant | Le butin est consommé une seule fois ; aucun objet déjà possédé n'est pris ; tous les doublons ne doivent pas devenir automatiquement moins intéressants que leur conversion |
| **Seconde lecture** | Lors d'une relance de level-up, la carte d'amélioration la plus rare est conservée et les autres sont renouvelées | Chercher un meilleur choix sans abandonner une bonne carte ; exploiter un build Chance | Consomme la relance normale ; ne crée aucune relance et n'augmente pas directement la rareté ; un choix passé n'est pas conservé entre niveaux |
| **Traversée** | Après un passage réel en zone oubliée, certains avantages de récompense de cette zone persistent brièvement une fois sorti | Préparer un détour puis combattre/choisir sa récompense dans une autre zone | Ne transporte aucune protection, aucun soin ni terrain ; ne supprime aucun malus ni dégât du Néant ; séjour préalable nécessaire, aucune réserve cumulable |

**Précisions pour empêcher des interprétations qui changeraient le jeu :**

- **Convergence** respecte portée, obstacles et capacités réelles de ciblage. Entre plusieurs élites éligibles : la plus proche ; cible maintenue tant qu'elle reste valable pour éviter un changement permanent. Les autres règles de l'arme s'appliquent ensuite. Elle n'oblige pas une onde ou une orbite à devenir un tir guidé.
- **Débordement**, valeur d'essai : reporter 50 % de l'excédent issu du coup normal, au plus une attaque normale de cette arme, pendant 3 s. Le bonus est consommé par le premier prochain impact direct. Multi-cibles : agréger les excédents d'une même attaque dans cette réserve, jamais par ennemi dans des nœuds distincts. Le report ne recrée ni critique ni proc indépendant. Il faut connaître PV avant coup, dégâts réellement appliqués et origine ; ces informations ne figurent pas aujourd'hui dans `EnemyKilled`.
- **Prévoyance**, essai : capacité de 20 % des PV maximum. À l'acquisition, une charge initiale permet un effet de survie immédiat ; ensuite seuls les soins effectivement excédentaires la remplissent. Restitution limitée aux PV perdus dans le coup et à la réserve restante, seulement si le joueur a survécu. Afficher stock et consommation. Tester particulièrement le soin continu, le Néant et les très grosses piles ; ne pas ouvrir une nouvelle voie de réactivation permanente du bouclier anti-coup fatal.
- **Sillage**, essai : les six dernières secondes de trajet, largeur liée au rayon de collecte courant. Une orbe déjà prise ne peut être créditée une deuxième fois ; une orbe hors du trajet reste dans le monde. Pas de nouvelle XP, de gel de l'Effacement ni de récolte d'objets. Le reflux de fin de Résurgence R1-B reste un événement distinct et plus large.
- **Délestage** montre le résultat en XP, sans promettre un nombre garanti de niveaux. Sa valeur dépend de la rareté et du coût du niveau au moment où l'offre est résolue ; elle est ensuite figée. Le bonus général d'XP s'applique une fois. Choisir cette option remplace le gain d'objet ; aucune boucle « recevoir → revendre → recevoir ». Boss et niveaux garantis restent sur leur contrat propre.
- **Seconde lecture** n'agit que sur les cartes d'amélioration qui ont une rareté ; à égalité, conserver celle de gauche. La carte gardée conserve aussi ses gains concrets, pas seulement sa couleur. Revalider son éligibilité ; la libérer si elle devient invalide. Aucune conservation sur un choix de perk sans rareté. Si toutes les cartes sont des nouveautés neutres, la relance fonctionne normalement et l'UI l'indique.
- **Traversée**, essai : séjour continu de 3 s au moins en zone Effilochée/Effacée, puis maintien de son contexte de récompense pendant 6 s après sortie. Le contexte transporté ne remplace pas les multiplicateurs R1-A fixés au spawn : il peut alimenter la contribution locale d'XP du build et les promotions de rareté locales, appliquées une seule fois. Pas d'addition avec le même bonus de zone réel. Le bonus d'Essence actuel reste évalué au lieu de mort de l'ennemi ; il n'est pas transporté dans cet essai. Un simple pas de part et d'autre d'une frontière ne recharge pas instantanément l'effet. Les timers suivent le temps de jeu et la pause existante ; la rareté des cartes reste fixée à leur tirage.

La puissance numérique reste dans les objets et les armes. Un objet d'XP renforce Délestage ; un objet de Chance rend Seconde lecture plus intéressant ; des dégâts élevés alimentent Débordement ; la régénération alimente Prévoyance ; l'attraction étend Sillage. Ces interactions ont lieu sans faire apparaître un nouveau système de « synergies activées » ou un bonus secret.

#### Deux builds opposés et une spécialisation de risque

| Intention | Quatre règles possibles | Ce que le joueur fait réellement | Faiblesse recherchée |
|---|---|---|---|
| Chasseur d'élites | Convergence, Débordement, Prévoyance, Sillage | Prépare ses armes lourdes dans la foule puis tient l'élite à portée ; avance pour continuer à récolter | Une masse de créatures normales peut le déborder ; dépend de l'arsenal et du placement |
| Croissance opportuniste | Délestage, Seconde lecture, Sillage, Prévoyance | Choisit quels objets abandonner, tire parti des relances, finance une avance de niveaux en continuant sa route | Moins de règles offensives ; convertir trop de butin affaiblit la croissance future |
| Détours à la lisière | Traversée, Délestage, Convergence, Sillage | Prépare un détour, abat une menace pour accéder au butin et ressort avec une fenêtre de récompense | Exposition à l'oubli et peu de défense ; le Néant reste dangereux |

Ces builds sont des hypothèses de comportements, pas des performances établies. Un joueur peut préférer un build simple : il n'a pas à surveiller sept timers, puisqu'il ne porte que quatre règles et que plusieurs sont automatiques. À l'écran, montrer uniquement les états utiles (réserve pleine, report prêt, fenêtre de Traversée) et la conséquence au moment où elle se produit.

**Ce que B réussit potentiellement.** Des spécialisations décrites par des verbes : concentrer, récupérer, convertir, conserver, traverser. Les mêmes objets peuvent avoir des usages différents selon les règles choisies, tout en conservant la satisfaction des piles. Les armes gardent leurs motifs offensifs.

**Ses risques.** Compréhension, effets trop discrets, dépendances à des sources rares, combinaisons difficiles à équilibrer. Seconde lecture peut être trop faible si les relances manquent ; Sillage peut être inutile si le ramassage est déjà presque complet ; Débordement peut doubler une arme lourde sans changer la manière de jouer. Ces problèmes seraient des raisons de revoir les perks, pas d'ajouter plus de texte pour les défendre.

**Critère d'abandon d'un candidat.** À butin et puissance comparable, le joueur ne peut montrer aucun cas où le perk a changé une décision ou sauvé une ressource. Le candidat devient alors un objet/stat simple, ou est retiré. Le principe qualitatif ne justifie pas artificiellement tous les effets.

### 12.5 Direction C — Armes et objets, spécialisation dans l'arsenal

**Contrat.** Retirer les quatre emplacements de perks. Les niveaux font acquérir/améliorer les armes ; les objets portent les statistiques globales et les interactions. Quelques décisions qualitatives font bifurquer une arme. Elles sont attachées à l'arme et remplacent certaines de ses améliorations ordinaires ; on ne recrée pas quatre perks globaux dans un autre onglet.

**Exemple de bifurcation.** Une Cloueuse arrivée à un palier propose une variante qui traverse davantage de rangs, ou une variante plus lente qui repousse fortement sa cible. Dans les deux cas, elle reste une Cloueuse. La branche dirige ses améliorations et son rôle ; elle ne lui ajoute pas le cône du Transistor. Une seule bifurcation par arme pour l'essai, palier indicatif niveau d'arme 15. La branche conserve les gains déjà acquis. Aucune fusion de recettes ni craft.

**Acquisition.** Le choix de branche entre dans la file existante et compte comme une amélioration d'arme. Présenter les deux possibilités dans la même offre, avec une autre amélioration possible, pour que le joueur puisse reporter ce choix. Une arme sans nouvelle branche validée reste jouable avec sa progression existante : ne pas inventer 48 variantes d'attaque avant d'avoir testé ce principe.

**Une run.** Je trouve une arme de contrôle, je choisis une branche qui la spécialise, puis je cherche les objets qui en tirent parti. Le niveau fait progresser mon arsenal ; le détour vers un coffre change mes statistiques globales. Mon identité vient de la composition des quatre armes, de leurs branches et des objets accumulés. L'écran de pause présente deux ensembles d'équipement, plus la fiche du personnage.

**Le problème de l'XP et de la Chance.** La suppression des perks retire leur accès volontaire au level-up. Si rien ne le remplace, une run XP/Chance devient trop dépendante du hasard. C doit donc proposer davantage de choix de butin : sur les coffres rares et les récompenses de Souverain, un choix parmi trois objets, avec une offre de croissance (XP ou Chance) lorsque le catalogue débloqué le permet. Un premier coffre accessible doit aussi proposer une option de survie. Ce sont des garanties de catégories à tester, pas la promesse de trouver toutes les piles voulues.

Ces garanties demandent de vérifier le placement et la fréquence des rencontres ; un choix théoriquement disponible dans un coffre jamais vu ne donne aucun contrôle. Les Mémoriaux peuvent compléter la progression, mais ne doivent pas devenir l'unique accès à un build XP : Raphaël a déjà signalé ne pas en rencontrer en dix minutes. C dépend plus fortement que B de la réussite du plan 13 et de la lisibilité des lieux.

**Puissance et endgame.** Les objets n'ont toujours pas de plafond. Les armes plafonnées passent au surplus quand elles sont toutes complètes, sans réintroduire des améliorations statistiques globales infinies au level-up. La puissance continue alors surtout par le butin et la bonne exploitation des armes. Le jeu doit continuer à distribuer des objets via événements/élites au-delà des coffres fixes de la carte ; cette économie doit être mesurée dans les trois directions, particulièrement ici.

**Ce que C réussit.** Catégories plus simples ; armes plus personnelles ; acquisition d'objets plus importante. Les choix de branches ont des conséquences visibles, particulièrement utiles si les perks de B restent trop abstraits.

**Ses coûts.** Production et validation de variantes par arme, accès XP/Chance plus incertain, moins de choix de progression après saturation de l'arsenal. Choisir la bonne combinaison d'objets demande du contrôle sur les offres, qui peut rallonger l'ouverture des coffres. Le catalogue global est plus simple à expliquer, mais son contenu n'est pas forcément moins cher à produire.

**Condition de réussite.** Le joueur peut poursuivre un build volontaire malgré des trouvailles imparfaites, et raconter pourquoi ses deux branches d'armes se complètent. Si toutes les décisions reviennent à prendre l'objet le plus rare, C a perdu de l'agence en supprimant les perks.

### 12.6 Même situation, trois décisions différentes

Situation de conception : trois armes dont une lourde ; un coffre rare dans une zone Effilochée, gardé par une élite ; un autre trajet permet de continuer en zone plus sûre. Le joueur a déjà des objets offensifs et de croissance. Il s'agit d'un exemple, pas d'une capture ni d'un résultat de test.

| Variante | Décision de route et de combat | Au coffre | À la prochaine cascade |
|---|---|---|---|
| A | Accepter le détour parce que l'investissement XP/Chance valorise la récompense ; juger si les statistiques permettent de battre l'élite | Prendre la puissance cumulable disponible | Répartir les niveaux entre ses quatre axes et les armes |
| B | Avec Convergence, attaquer l'élite en priorité ; avec Traversée, préparer la sortie ; avec Sillage, avancer sans perdre le ramassage récent | Délestage permet de choisir entre une pile et de l'XP ; le sacrifice dépend de l'état du build | Améliorer les armes, exploiter Seconde lecture si équipé ; une nouvelle règle seulement à un palier prévu |
| C | Exploiter le rôle particulier donné à l'arme par sa branche pour ouvrir le passage | Choisir un des trois objets, dont une option de croissance ; renforcer l'arsenal ou chercher davantage d'XP/Chance | Améliorer les armes et éventuellement choisir une branche disponible |

Ce tableau ne présume pas que B est plus amusant. Il rend observable ce qu'on doit vérifier : investissement de niveaux pour A, changement de conduite pour B, spécialisation des armes et choix de butin pour C.

### 12.7 Compatibilité avec les centaines de niveaux

Les capacités actuelles sont vérifiables dans `data/weapons/weapon_upgrades.json` (`weapon_max_level: 50`) et `Player.MaxPassiveSlots` (4). Un personnage commence avec une arme au niveau 1. Les budgets suivants comptent des **acquisitions/améliorations**, pas le niveau affiché :

| Configuration | Budget théorique hors gains du monde | Niveau joueur théorique de saturation, départ niveau 1 |
|---|---:|---:|
| Actuelle, quatre passifs de cinq niveaux | `4×50 − 1 + 4×5 = 219` | 220 |
| A, quatre perks de dix niveaux | `199 + 40 = 239` | 240 |
| B, quatre perks acquis une fois | `199 + 4 = 203` | 204 |
| C, branches intégrées aux niveaux des armes | `199` | 200 |

Ce sont des comptes de contenu, pas des prédictions de run. Butin/Mémoriaux réduisent les level-ups nécessaires ; offres passées, armes échangées et branches reportées les modifient. Si C consomme des choix de branche supplémentaires hors niveaux des armes, il faut les ajouter explicitement. Un slot encore vide ou une arme échangée peut rouvrir des choix ; le surplus se déduit de l'éligibilité réelle.

**Conséquence assumée :** B et C atteignent plus tôt le surplus que A. Cela peut convenir au retour de Raphaël (« après le build complet, quelque chose de trivial »), mais les niveaux 300–400 ne représentent alors pas 300–400 nouveaux choix de build. Les cascades d'améliorations restent importantes tant que l'arsenal progresse ; après, souffle/onde des lots R1-H prennent le relais selon leur calibrage. Si l'objectif devient de continuer à choisir longtemps après le niveau 200, il faudra revoir le plafond des armes ou la notion de surplus, et non remplir les niveaux de récompenses artificielles.

**Point à corriger dans les hypothèses du premier catalogue :** aucun nombre fixe 219/239/203/199 ne doit piloter le surplus. Le passage y est réversible si un nouveau contenu améliorable est acquis, selon une règle explicite à tester ; aucune récompense de niveau déjà consommée n'est rejouée.

Les paliers XP du plan 20 restent des cibles de trajectoire. B remplace le perk +40 % XP par des règles et des objets : on doit recalculer le modèle, pas considérer que le même nombre de niveaux suivra automatiquement. Les taux d'objets, la conversion de Délestage, les fenêtres de Traversée et l'efficacité réelle du ramassage doivent entrer dans les hypothèses. Ne pas vendre la cible de 380 niveaux comme un résultat de cette étude.

### 12.8 Où se situe l'originalité, et ce qu'il faut comparer

| Critère | A — Stats choisies | B — Règles de build | C — Arsenal et butin |
|---|---|---|---|
| Rôle des niveaux | Investir entre stats globales et armes | Faire croître les armes ; quatre choix de règle | Faire croître et orienter les armes |
| Rôle des objets | Compléter les investissements | Alimenter et exploiter les règles | Porter toute la croissance globale |
| Choix volontaire XP/Chance | Direct au level-up | Conversion/qualité/trajectoire, complétées par objets | Offres de butin à mieux maîtriser |
| Identité perceptible en combat | Surtout ampleur et efficacité | Ciblage, séquences et gestion des ressources | Variantes locales des armes |
| Lien au monde | Commun aux trois : détour, oubli, lieux, risque | Certains perks l'exploitent ; d'autres restent centrés sur le combat | Fort besoin de rencontrer les bonnes sources |
| Charge de lecture | Faible par choix, beaucoup de choix répétés | Plus forte à l'acquisition, quatre règles à retenir | Faible en catégories, branches à apprendre par arme |
| Risque principal | Redondance des statistiques et ressemblance de structure | Systèmes invisibles ou trop conditionnels | Aléatoire subi et coût du contenu d'armes |
| Coût spécifique | Refonte des passifs et calibration | Contexte de combat/récompenses et UI d'état des perks | Branches d'armes et contrôle du butin |

La formule B conserve visuellement quatre armes/quatre perks. Sa distinction ne doit donc pas être promise sur une capture d'inventaire ; elle doit se constater en jouant. C change davantage la structure, mais n'est pas automatiquement plus originale ou plus amusante. A peut être un bon jeu si le monde et l'économie fournissent les décisions distinctives. Ce sont des appréciations de conception à éprouver, pas une mesure de nouveauté mondiale.

### 12.9 Points techniques réels et frontières à préserver

Phase 0 complémentaire au §2, lecture du code courant pendant que d'autres lots évoluent :

- `FragmentManager.SelectFragment(FragmentOption)`, file de niveaux et `EnsureSurvivalChoice` : points existants pour A/B/C. La garantie de survie actuelle repose sur les passifs ; sa suppression doit avoir un remplacement explicite. La réserve automatique R1-G en cours est un contrat commun, pas une fonctionnalité à refaire dans chaque variante.
- `RiftDirector` propose actuellement des améliorations d'armes et de passifs. B/C doivent retirer les améliorations de perks de cette offre. Si les armes sont déjà complètes, prévoir une récompense d'objet selon les contrats des plans 13/17, ou rendre l'offre indisponible sans coût ; ne jamais appliquer un Oubli contre une amélioration impossible. Même audit pour chaque source d'amélioration du monde. Les bénédictions de Mémoriaux restent des récompenses distinctes ; leur existence ne recrée pas un inventaire de perks.
- `UpgradeRoller` et les gains concrets de `FragmentOption` permettent d'identifier une carte existante pour Seconde lecture. L'option doit conserver ses valeurs et être revalidée, pas être retirée par un nouveau tirage déguisé.
- `LootRewards.Resolve` puis `Apply` : frontière de Délestage et du choix d'objets de C. `LootReceived` applique immédiatement certains gains ; ne pas émettre ce signal avant le choix.
- `Player.Heal` borne aujourd'hui les PV ; `PlayerDamaged` sert aussi à mettre à jour l'interface après des soins/modifications de PV. Prévoyance nécessite un résultat de soin et un fait de coup réellement reçu. Ne pas déclencher une riposte/réserve sur une notification générale de PV.
- `Enemy.TakeDamage` retourne actuellement `void` ; `EventBus.EnemyKilled(string enemyId, Vector2 position)` ne fournit pas la source, les PV pré-impact ou l'excédent. Débordement demande un résultat de combat explicite. Coordonner ce contrat avec le suivi des dégâts en cours ; ne pas déduire l'excédent du seul compteur du bilan.
- `EventBus.XpGained(float amount)` ne transporte ni position ni provenance. Sillage s'applique aux orbes avant leur collecte. Traversée/Délestage doivent conserver un contexte de récompense explicite ; une couche unique applique les bonus de build.
- Convergence doit suivre les compatibilités des armes. Les mécanismes projetés du Traqueur et du Facteur ont déjà leurs priorités propres ; un perk global ne les écrase pas silencieusement. Une cible conservée doit aussi rester la même vie d'ennemi après réutilisation par le pool.
- Les six fiches de personnages sont des identités validées, mais les données actuelles de `characters.json` ne contiennent que trois personnages. Protéger les signatures prévues sans les annoncer implémentées. Traversée ne remplace pas la réserve d'air de la Scaphandrière ; la proposition de double offensif reste à l'Éveillée.
- Aucune de ces règles n'autorise un nœud, timer, abonnement ou projectile par exemplaire d'objet. Les contributions sont agrégées. Débordement réserve au plus quatre états d'arme ; Sillage conserve un trajet borné ; l'inventaire ne doit pas être reparcouru par impact.

### 12.10 Recommandation et prochaine preuve à obtenir

**Recommandation : B, avec quatre perks qualitatifs sans niveaux ; C est l'alternative à garder si ces règles ne sont pas perceptibles.** B répond au souhait de choisir quatre spécialisations et à la crainte de simplement renommer des tomes. Elle conserve la croissance des armes et les piles d'objets, en donnant une fonction différente à chaque catégorie. A reste le témoin utile de simplicité et de croissance quantitative ; aucune raison de le caricaturer ou de le rendre volontairement moins généreux dans les essais.

Cette recommandation implique d'abandonner le catalogue figé « 16 perks × 10 niveaux » comme cible commune. On ne remplace pas ces 16 entrées par 16 règles complexes par principe. D'abord retenir des familles de décisions, ensuite vérifier qu'il existe assez de choix intéressants pour quatre emplacements. Les sept fiches ci-dessus servent cette vérification ; le nombre final reste ouvert.

**Séquence proposée après choix de direction, pas implémentée ici :**

| Étape | Travail circonscrit | Sources et motifs à reprendre | Preuve et garde-fou |
|---|---|---|---|
| P0 — Contrats communs | Répartition armes/personnages/objets/perks ; attribution, éligibilité, récompenses ; recalcul du modèle XP | Audit §2, §12.9, plans 06/17/20 ; loaders et données existants | Tous les anciens effets ont une destination justifiée ; formule des centaines de niveaux recalculée ; aucun retrait silencieux ni API supposée |
| P1 — Essai comparatif court | Même arsenal, monde, ressources et enveloppe de puissance ; A sert de témoin, B éprouve deux règles perceptibles, C éprouve une branche et un choix de butin | `FragmentManager`, `LootRewards`, progression d'armes ; règles de prototypes du plan 11 | Comparaison filmée et jouée des décisions ; pas trois modes de jeu à maintenir ni trois refontes complètes de production |
| P2 — Catalogue de la direction retenue | Finaliser toutes les identités/effets, acquisition et présentation ; un lot fini à la fois | §12.2, Charte, plans 04/05/13 | Chaque effet décrit et réellement observable ; le prototype réduit n'est pas présenté comme la livraison finale objets/perks |
| P3 — Validation complète | Runs normales et puissantes, grandes piles, late game, sources d'objets et coûts | `tools/measure_run.sh`, `tools/progression_model.py`, captures et banc dense | Avant/après mêmes seeds, build zéro warning, smoke si code concerné, régressions et captures ; roadmap cochée seulement après implémentation vérifiée |

**Mesures utiles :** niveaux et XP par palier, objets et raretés par minute, temps pour tuer, soins réellement utiles, XP laissée au sol, nombre/durée des interruptions, activation effective de chaque règle. Pour Délestage, compter aussi les objets sacrifiés et la puissance future abandonnée ; pour C, le temps nécessaire pour réunir un axe XP/Chance. Un bot peut mesurer une économie mais ne prouve pas qu'une décision est intéressante.

**Questions de recette humaine, sur une même situation :** quel choix as-tu fait différemment grâce à ce perk/objet/embranchement ? Quel résultat as-tu reconnu à l'écran ? Aurais-tu choisi autre chose dans cette run ? Si B exige de consulter la pause pour savoir si un perk a servi, il faut améliorer son effet/feedback ou préférer C. Si C empêche de poursuivre un build faute de bonnes trouvailles, il faut améliorer les offres ou revenir à B/A. Aucun score d'originalité arbitraire ne remplace cette observation.

À cette étape, l'étude est terminée ; le choix de formule et les effets précis restent ouverts. Aucun code de gameplay, nouveau prototype ni suppression d'inventaire n'a été engagé pour cette étude.
