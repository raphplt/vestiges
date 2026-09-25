# Plan 13 — Le butin : objets aléatoires, raretés et sources lisibles

Statut : **proposition à valider, rien d'implémenté** · Priorité : P0 après la recette du plan 12 · Dépendances : modèle d'objets du [plan 05](05-armes-objets-builds.md#3-trois-piliers-de-build-et-contrat-dobjets), idée B du [plan 11](11-mecaniques-originales.md#4-idée-b--un-butin-à-sauver-de-leffacement), repères du [plan 12](12-micro-evenements.md).

## 1. Le retour

Raphaël, le 24 septembre : « il faut que le jeu ait du loot (trouver une ou deux manières de présenter le loot — Megabonk en a trois je crois de mémoire), différent de Megabonk bien sûr, mais le loot doit être aléatoire, de raretés différentes, et ça doit être une composante du jeu. Les trucs qui donnent du loot doivent être identifiables facilement sur la map. »

**Constat dans le code :**
- Le butin actuel se résume à des armes (4 slots vite pleins), des perks et de l'Essence.
- Les coffres (commun, rare, épique, lore) et les POI existent, mais ils se repèrent mal et leur contenu surprend peu.
- Le **système d'objets cumulables** décidé au plan 05 (rareté fixe par objet, piles sans limite) **n'existe pas encore**. C'est le prérequis de tout ce plan : sans objets, on ne peut distribuer que plus d'armes ou plus de chiffres.

## 2. Principes

1. **Le butin est une décision, pas une collecte.** Chaque forme de présentation pose une question différente : *est-ce que je fais le détour ?*, *lequel je garde ?*, *qu'est-ce que j'accepte de perdre ?*
2. **Lié au thème de l'oubli.** Les objets sont des *vestiges* : des fragments du monde d'avant, que l'Effacement menace et que le joueur arrache à l'oubli. Choisir un objet, c'est en laisser d'autres être oubliés.
3. **Rareté lisible de loin.** Chaque source porte une colonne de lumière à la couleur de sa rareté, visible avant l'objet lui-même (même langage que les repères du plan 12). La forme de la source dit *comment* on obtient le butin, la couleur dit *combien il vaut*.
4. **Différent de Megabonk.** Pas de coffre payant à prix croissant, pas de monnaie dédiée au loot. L'Essence reste la monnaie des Autels. Le coût d'un butin, c'est le **risque** (détour, Effacement, gardiens) et le **renoncement** (ce qu'on laisse oublier).
5. **Aléatoire encadré.** Le tirage est pondéré par rareté et par biome, et il garantit une progression : les raretés montent avec la distance parcourue et l'Effacement, pas avec une table fixe.

## 3. Les raretés

Cinq raretés, avec couleur et forme d'icône (lisibles sans la couleur, cf. accessibilité) :

| Rareté | Couleur | Fréquence cible | Rôle |
|---|---|---|---|
| Commun | Blanc cassé | Majorité | Statistiques simples cumulables |
| Inhabituel | Vert | Fréquent | Statistique + petit effet conditionnel |
| Rare | Bleu | Régulier | Règle de combat (proc, déclencheur) |
| Épique | Violet | Rare | Transforme un style (ex. projectiles qui rebondissent) |
| Légendaire | Or | Exceptionnel | Change la façon de jouer ; rare en run, souvent débloqué par quête |

Le premier lot vise une vingtaine d'objets : 8 communs, 6 inhabituels, 4 rares, 2 épiques. Les légendaires viendront après la recette.

## 4. Trois manières de présenter le butin

### A. Le Vestige figé — trouver
Un objet du monde d'avant, figé dans un halo, flotte au-dessus du sol. Une colonne de lumière de sa rareté le signale de loin, mais **sa forme reste floue** (silhouette) : on sait ce qu'il vaut, pas ce qu'il est. En le touchant, le joueur le « reconnaît » : courte révélation (arrêt sur image léger, nom, effet), puis ajout à l'inventaire.
- Ramassage direct, sans menu : le butin courant ne casse pas le rythme.
- Sources : posé dans le monde, lâché par les élites et les Souverains, contenu des coffres (un coffre libère 1 à 3 vestiges au lieu d'ouvrir un écran).
- Rareté majoritairement commune à rare.

### B. Le Triptyque — choisir
Un petit autel de mémoire à trois alcôves. En approchant, les trois objets se révèlent ; le joueur en prend **un**, et **les deux autres s'effacent** sous ses yeux (dissolution, son de perte). C'est le moment « build » : on choisit en connaissance de cause.
- 2 à 4 par run, placés sur des POI ou gardés par une élite.
- Rareté garantie : au moins un rare, parfois un épique.
- Variante du Triptyque « scellé » : un seul objet visible, les deux autres cachés.

### C. Le Pacte d'oubli — échanger
Une stèle fissurée propose d'**oublier** un objet possédé (une pile entière ou un exemplaire) contre un objet tiré **une rareté au-dessus**. Le seul endroit où le joueur perd volontairement quelque chose : c'est le thème du jeu mis en mécanique.
- 1 à 2 par run, en zone d'Effacement avancé (risque pour y accéder).
- Aperçu clair : ce qu'on perd, la rareté visée ; l'objet obtenu reste une surprise.
- Ne remplace pas les Autels (armes, Essence) : il agit seulement sur les objets.

### Transversal : le butin menacé
Idée B du plan 11, appliquée aux trois formes. Plus une source est proche du front d'Effacement, plus sa rareté monte, **et elle disparaît quand sa zone s'efface**. Le halo se désagrège progressivement : c'est l'urgence, lisible sans chiffre. On ne perd jamais un objet déjà ramassé.

## 5. Lisibilité sur la carte

- **Colonne de lumière** à la couleur de la rareté, hauteur selon la rareté. Elle est visible au-delà du cadre grâce à sa hauteur.
- **Forme de la source** : halo flottant (Vestige), autel à trois alcôves (Triptyque), stèle fendue (Pacte), coffre. Une silhouette par forme, pas de confusion avec le décor.
- **Repères de bord d'écran** : petits losanges colorés au bord de l'écran pour les sources proches hors champ (portée limitée pour ne pas saturer), comme la flèche des micro-événements, en plus discret.
- **Son** : un tintement dont le timbre dépend de la rareté quand une source entre dans le cadre (Légendaire : son unique).
- La minimap, désactivée pour les performances, pourrait revenir en version simple (points de butin seulement).

## 6. Économie et équilibrage

- Densité cible de départ : une source de butin visible toutes les 20 à 30 s de marche, soit environ 25 à 40 par run de 15 min, toutes formes confondues. À mesurer avec le banc de captures.
- Les objets ne remplacent pas les armes, les passifs ni les montées de niveau : ils s'ajoutent (plan 05). Surveiller la montée en puissance (plan 03) : si le build explose trop tôt, on baisse la densité de communs avant de toucher aux raretés.
- La chance (stat) et certains personnages influent sur le tirage des raretés : un levier de build supplémentaire.
- Le bilan de fin de run (plan 02) montre les objets trouvés, par rareté.

## 7. Lots proposés

| Lot | Contenu | Vérification |
|---|---|---|
| **A — Socle objets** | Modèle du plan 05 : JSON par objet, inventaire agrégé par ID, effets compilés, 20 objets. Pas de nœud par exemplaire. | Build, test de cumul de 1 000 exemplaires, perf |
| **B — Vestige figé** | Source, colonne de lumière, révélation, drops d'élites et de Souverains, coffres qui libèrent des vestiges | Captures, lisibilité à 720p |
| **C — Raretés et tirage** | Tables pondérées par biome, distance et Effacement ; stat de chance | Distribution mesurée sur 50 runs simulées |
| **D — Triptyque** | Autel à trois choix, dissolution des deux autres | Recette : le choix est-il intéressant ? |
| **E — Pacte d'oubli** | Échange contre la rareté supérieure | Recette : le sacrifice est-il tentant ? |
| **F — Butin menacé** | Rareté selon l'Effacement, disparition avec la zone | Recette : détours pris ou refusés, compréhension |
| **G — Présentation** | Inventaire en pause, bilan de fin par rareté, Collection du Hub | Plan 04 |

Ordre recommandé : A → B → C, puis une recette (le butin se ressent-il ?), puis D/E/F. Un lot à la fois.

## 8. Retour du 25 septembre : le soin

Raphaël : « le soin est peut-être trop difficile à obtenir aujourd'hui ». À traiter avec ce plan ou le plan 03, pas maintenant.

Sources de soin vérifiées dans le code le 25 septembre :
- perks de vampirisme et de régénération (`Player`) ;
- soin payant en Essence aux Autels (`AltarManager.TryHeal`, `data/scaling/altars.json`) ;
- Veille (30 %) et Vestige tombé, seuls micro-événements qui soignent (`data/events/run_events.json`).

Aucune créature ni aucun coffre ne lâche de soin. Il n'existe donc pas de correctif trivial : ajouter une source est un choix d'économie. Pistes à arbitrer avec le butin : un objet de soin dans les tables de coffres, une rare « braise de mémoire » lâchée par les élites, un soin partiel à la fin d'une Résurgence.

## 9. Décisions demandées à Raphaël

1. Valider les trois formes (Vestige figé, Triptyque, Pacte d'oubli) ou en écarter une.
2. Rareté fixe par objet (recommandé, plan 05) ou raretés multiples d'un même objet.
3. Les coffres actuels : les transformer en conteneurs de vestiges (recommandé), ou garder leur écran de butin.
4. Priorité : lancer le lot A dès la fin de la recette du plan 12 ?
