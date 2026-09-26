# Plan 17 — Armes, coffres et modificateurs de run

Statut : **proposé le 26 septembre 2026, à arbitrer** · Priorité : P0 (demande de Raphaël) · Dépendances : [05](05-armes-objets-builds.md) (contrat armes/objets), [08](08-direction-artistique.md) (pipeline de sprites), [13](13-butin.md) (butin), [16](16-oubli-sensible.md) (oubli), [18](18-inventaire-restes-v1.md) (restes V1).

## 1. Le retour

Raphaël, le 26 septembre :
- **Sprites des armes** : ils datent de la première version, ils ne vont pas avec la nouvelle DA, plus fine. Tous sont à revoir.
- **Présentation** : on montre portée et dégâts d'une manière que personne ne lit. Plus de stats au choix d'une arme, au plus une description courte. Au level-up, montrer **la ou les stats augmentées**, avec plusieurs raretés d'amélioration (Commun, Rare, Épique…) : une meilleure rareté renforce davantage ou plusieurs stats à la fois. En pause, afficher les armes et leurs stats. Question ouverte : des stats à virgule ?
- **Coffres** : « ils ont disparu des runs, ou en tout cas je n'en vois plus ». Les réintégrer et les rendre visibles, pour qu'on ne les prenne pas pour du décor.
- **Modificateurs** : chance, difficulté, et des mécaniques propres au jeu. Une mécanique liée à l'Effacement, des sanctuaires de mémoire qui donnent des bonus, et pourquoi pas une mécanique miroir qui donne un malus. Sans devenir lourd ni prendre l'oubli au sens littéral.
- **Identité** : armes peu identifiables (sprites, **noms trop clichés**), amélioration incomprise, raretés les plus rares trop absentes. Il faut qu'on puisse « faire la run de sa vie » avec les bons choix.
- **Objets** : importants, peut-être dans un second temps, par crainte de copier une référence du genre.
- **North star** : des mécaniques propres qui donnent envie de découvrir le jeu et d'y revenir.
- **Doc** : le jeu de référence du genre y est trop cité.
- **Restes V1** : inventaire complet demandé, voir le [plan 18](18-inventaire-restes-v1.md).

## 2. État des lieux (26 septembre, audit du code)

### Armes

- **24 armes** : 12 de mêlée, 10 à distance, 2 spéciales. Toutes les stats sont déjà des flottants en interne (`Dictionary<string,float>`).
- **Montée de niveau** : une table unique pour toutes les armes (`weapon_upgrades.json` : dégâts +15 %, cadence +10 %…). Tout plafonne au niveau 6 : **les niveaux 7 et 8 ne donnent rien**. Arcs, rebonds, orbes et cônes n'évoluent jamais.
- **Carte de level-up** (`LevelUpScreen.FormatWeaponStats`) :
  - elle affiche `Arc | 10 dég | 1.0/s | 60m` ;
  - ce sont les valeurs de base du JSON, sans niveau, ni rareté, ni personnage ;
  - le texte est **identique pour « Nouveau » et pour une amélioration**, sans aucun écart affiché ;
  - « m » désigne des pixels, « /s » un multiplicateur ; la Boîte à musique affiche « 0.0/s ».
- **Commandes** : le level-up se joue **à la souris uniquement**.
- **Raretés d'arme** :
  - ce sont de simples multiplicateurs (×1,00 à ×1,36) ;
  - **ignorées au niveau 1**, puis appliquées d'un coup au niveau 2 ;
  - le multiplicateur global fausse certaines stats : une Chaîne légendaire gagne des dégâts à chaque rebond ;
  - **trois palettes se contredisent** (données, level-up, coffre).
  - Une arme obtenue au level-up ou lâchée par une créature est toujours commune.
- **Pause** : elle ne montre que le nom et la rareté de l'arme du slot 0. Elle présente les stats du personnage comme si c'étaient celles de l'arme.
- **Bugs de build** :
  - les effets au contact et le recul des projectiles utilisent **la dernière arme qui a tiré**, donc les effets se mélangent entre armes ;
  - la Boîte à musique met 20 s à faire apparaître ses notes ;
  - deux compteurs de niveau se désynchronisent (l'Autel ne monte pas le badge) ;
  - bannir une amélioration consomme le bannissement sans effet.
- **Sprites** :
  - 37 icônes 64×64 de mars, issues d'un ancien générateur (`scripts/generate_weapons.py`) ;
  - pixels agrandis par des facteurs non entiers, puis affichées à des échelles elles aussi non entières (HUD 22 px, cartes 48 px, sol ×0,5) ;
  - plusieurs icônes ne correspondent pas au nom : le Tuyau est une lance en bois, la Lame un bâton brun ; l'Éclat de phare est transparent à 88 %.
  - Aucune arme n'est visible sur le personnage.
- **Noms** : les armes de départ et du monde ont des noms génériques (Lame Ébréchée, Arc de Fortune, Marteau Lourd, Fronde Rouillée, Tranchant du Vide, Bâton d'Essence). Les armes de coffre épique sont déjà plus singulières (Cloche de l'Institutrice, Boîte à musique, Dessin d'enfant).

### Coffres

**Ils n'ont pas disparu, on ne les voit plus.** Preuves :
- les logs des deux parties du 26 septembre indiquent 13 et 14 coffres générés, et **aucun ouvert** ;
- `chests_opened` vaut 0 dans les 11 parties de l'historique.

Deux causes s'additionnent :
1. **Échelle.** Les coffres sont restés à 16×12 px (mars), avec un halo à 10 % d'opacité. Les 23–25 septembre, personnages et décors sont passés à 2 ou 3 fois leur taille : un buisson fait 34×29 px, un immeuble 187 px de haut. Le coffre a la taille d'une touffe d'herbe. Il passe derrière les décors triés en Y et sous les canopées, et rien ne dégage d'espace autour de lui.
2. **Placement.** `ChestSpawner` a été écrit pour une carte de rayon 80. La carte fait 200 depuis le 14 mars. Tous les coffres sont confinés à 55 cellules du départ, soit 7,6 % de la carte. En allant vers le nord ou le sud, on en sort en moins de 4 s.

Autres constats :
- **Signalement** : pas de colonne de lumière, pas d'invite « [E] », pas de minicarte.
- **Contenu** : il tire des perks V1 (Architecte, Maître du Temps…), des malédictions imposées, et affiche « Souvenir: random_souvenir » ou « Arme inconnue ».

### Modificateurs de run

- **Chance** : elle existe (`_luckBonus`) mais ne sert qu'au tier des offres de level-up. Elle ne touche ni les raretés, ni les coffres.
- **Appel du Vide** : seul curseur de difficulté, obtenu par un perk aléatoire. C'est un malus pur (le bonus d'XP n'est pas lu), et l'activer **efface les malédictions**.
- **Malédictions** : imposées par les coffres, sans leur contrepartie.
- **Code mort** : fusions, synergies, marchand, Mémorial de perks, mutateurs.
- **Autels** : placeholders en losange ; ils n'agissent que sur le slot 0, avec des touches codées en dur.
- **Seul vrai choix risque/récompense aujourd'hui** : combattre près de l'oubli (score et Essence majorés, plan 16 O5).

### Jeu de référence dans la doc

24 mentions dans 10 fichiers de `doc/`, aucune dans le code. Six passages vont au-delà de la référence neutre, dont un qui en décrit la structure comme modèle (`PROGRESSION-SYSTEM.md`, « on s'en inspire »).

## 3. Principes

1. **Tout a appartenu à quelqu'un.** Une arme est un objet du monde d'avant, détourné. Son nom dit l'objet, sa description dit ce qu'il fait, sa fiche dit à qui il était. Le thème vit dans les choses, pas dans le vocabulaire.
2. **Montrer ce qui change, et seulement ça.** Pas de chiffre sur un choix s'il ne bouge pas. Une amélioration montre « avant → après ». Une arme nouvelle montre ce qu'elle fait, en une phrase.
3. **La rareté se ressent.** Elle se voit (couleur **et** forme), s'entend, et change vraiment l'ampleur du gain. Les raretés hautes existent dans chaque run, rarement mais sûrement, et plus souvent si l'on prend des risques.
4. **L'oubli est la monnaie du risque.** Ce qui vaut le plus se trouve près de ce qui disparaît. Mécanique d'abord, jamais de texte lourd ni de métaphore à rallonge.
5. **Pas de mécanique reprise telle quelle.** Les conventions du genre (level-up à trois choix, raretés colorées, stats à virgule) sont communes à tous ; les mécaniques signatures d'un jeu précis ne le sont pas. Chaque système proposé ici part de l'Effacement ou de la mémoire.

## 4. Propositions de fond

### 4.1 Level-up à raretés

Chaque carte d'**amélioration** tire une rareté. La rareté fixe l'ampleur et le nombre de stats touchées. Valeurs de départ, à calibrer :

| Rareté | Poids de base | Amélioration d'arme | Amélioration de passif |
|---|---|---|---|
| Commun | 60 | 1 stat, gain ×1 | gain ×1 |
| Inhabituel | 25 | 1 stat, gain ×1,5 | gain ×1,5 |
| Rare | 11 | 2 stats, gain ×1,25 chacune | gain ×2 |
| Épique | 3,5 | 2 stats ×1,75, ou 1 stat + 1 palier | gain ×2,5 |
| Légendaire | 0,5 | 3 stats ×2 + 1 palier | gain ×3 + effet |

- **Stats par arme.** Chaque arme déclare dans son JSON les stats qui peuvent monter, avec un poids. La Cloueuse monte en dégâts, cadence, perçage et vitesse ; la Rallonge en dégâts, rayon et recul. Cela remplace la table unique : chaque arme grandit à sa façon.
- **Paliers.** Les stats entières (projectiles, perçage, rebonds, notes en orbite) ne montent que par palier, réservé à l'Épique et au Légendaire. Ce sont les moments forts d'un build.
- **Tirage de la rareté** : poids de base, modifiés par la **Chance**, puis par **l'oubli**. Un level-up pris en zone Fragile, Effilochée ou Effacée décale les poids vers le haut (voir 4.4).
- **Carte d'arme nouvelle** : sans rareté, cadre neutre, badge « Nouvelle ».
- **Niveaux** : plus de palier vide. Chaque niveau correspond à une carte réellement appliquée. Le plafond passe en données ; proposition : aucun plafond de design, rendement contrôlé par les poids.
- **Commandes** : clavier et manette, focus visible, relance et bannissement qui fonctionnent sur les améliorations, bouton « passer ».

Maquettes (texte indicatif) :

```
┌──────────── ◆ RARE ─────────────┐   ┌────────── NOUVELLE ───────────┐
│ [icône]  Cloueuse      Niv 3→4  │   │ [icône]  Lance-billes         │
│ Dégâts      14,2 → 16,8   +18 % │   │ Projette trois billes         │
│ Cadence     1,35 → 1,52 /s      │   │ en éventail.                  │
│                                 │   │                  ▸ Distance   │
└─────────────────────────────────┘   └───────────────────────────────┘
```

### 4.2 Stats à virgule : oui

- **En interne** : c'est déjà le cas.
- **À l'affichage**, pour que les petits gains existent et qu'un Légendaire se lise :
  - dégâts et cadence avec une décimale ;
  - bonus en pourcentage entier ;
  - compteurs (projectiles, perçage, rebonds) en entiers ;
  - virgule française partout.
- **Valeurs effectives** : l'affichage montre ce que le joueur inflige vraiment (arme × niveau × personnage × bonus), jamais la valeur de base du JSON.
- **Unités** : cadence en attaques par seconde réelles. La portée n'est pas affichée en chiffres sur les cartes ; en pause, elle apparaît en pourcentage du bonus.

### 4.3 La rareté appartient aux améliorations, pas à l'arme

**Décision demandée, amendement de la V2 §10.**

Aujourd'hui, deux raretés coexisteraient : celle de l'instance d'arme (multiplicateurs, V2 §10) et celle des améliorations (4.1). Pour le joueur, « Cloueuse Rare niveau 3 » et « amélioration Rare » se confondent.

**Recommandation.** Une arme n'a qu'un niveau et des stats accumulées. La rareté vit sur les améliorations et, plus tard, sur les objets (plans 05 et 13). Conséquences :
- le bug du niveau 1 et le multiplicateur global disparaissent avec le système ;
- l'Autel ne « monte plus la rareté » : il **ravive** l'arme choisie, c'est-à-dire une amélioration garantie Rare ou mieux contre de l'Essence (4.5) ;
- une arme lâchée par un Souverain arrive avec une ou deux améliorations déjà tirées.

*Alternative* : garder les deux raretés, comme dans la V2. Deux lectures à enseigner, et une interface plus chargée.

### 4.4 Ce qu'on arrache à l'oubli

Règle unique, lisible en une ligne dans le voile de zone : **plus la zone est oubliée, plus les raretés montent.** Elle s'applique aux level-ups, aux coffres et aux Mémoriaux.

- Proposition : Fragile décale les poids d'un cran, Effilochée de deux, Effacée de trois. La Chance s'ajoute.
- Le joueur peut en jouer : garder ses orbes d'XP pour les ramasser près du front, ouvrir un coffre en lisière plutôt qu'au centre.
- La règle prolonge le plan 16 O5 (score et Essence majorés), sans système nouveau à apprendre.

### 4.5 Mémoriaux et Failles — le souvenir et son miroir

Deux lieux du monde, symétriques. Ils remplacent cinq concepts qui se recouvrent aujourd'hui : Autels, POI « sanctuaire », POI « anomalie », malédictions imposées et Appel du Vide.

**Le Mémorial (bonus).** C'est l'Autel refondu : la V2 le décrit déjà comme un « point de mémoire concentrée ».
- **Activation** : le Mémorial libère trois **éclats** dans un rayon d'un écran, visibles et animés. Les ramasser, en combattant, l'active. C'est court (10 à 20 s), en mouvement, sans cercle où stationner, ce qui colle au jeu nomade.
- **Récompense** : un choix entre trois **bénédictions** de run, avec rareté : stats du joueur, Chance, soin, ou **lever un Oubli** (voir la Faille).
- **La tension propre à Vestiges** : la rareté de la bénédiction monte avec l'oubli de sa zone (4.4). Activer tôt, c'est sûr. Attendre que le front approche rapporte davantage, mais un Mémorial englouti par le Néant est perdu.
- **À l'activation**, la zone est ravivée : sa mémoire remonte autour du Mémorial, comme l'exploration de POI le fait déjà (`ErasureManager`, stabilisation à 0,72).
- **Essence** : une fois activé, le Mémorial accepte l'Essence pour **raviver une arme** (Rare ou mieux garanti, arme au choix, fini le slot 0), soigner ou relancer la bénédiction.

**La Faille (malus choisi).** C'est la trace de l'Effacement dans le monde.
- **Proposition** : en la touchant, le joueur voit une récompense forte immédiate (une amélioration Épique ou Légendaire à choisir parmi trois, plus tard un objet) **et son prix** : un **Oubli**.
- **L'Oubli** est un malus durable, nommé sobrement, avec un effet en une ligne. Exemples :

  | Oubli | Effet |
  |---|---|
  | Oubli des gestes | Cadence −8 % |
  | Oubli de la peur | +1 élite en vie |
  | Oubli du chemin | Tes zones s'effacent 15 % plus vite |

- **Péril** : chaque Oubli ajoute +1 de **Péril**. Le Péril augmente le nombre et la vigueur des créatures, et en échange le score, l'XP et la rareté des tirages.
- **Le miroir** :
  - le Mémorial se vide avec l'oubli, la Faille se multiplie avec lui (plus de Failles en zones Effacées) ;
  - le Mémorial rend (bénédiction ou Oubli levé), la Faille prend (Oubli contre puissance immédiate).
  - Une run peut enchaîner les Failles pour une puissance folle, puis chercher un Mémorial pour effacer sa dette.
- **Refus** : on peut toujours refuser, et rien n'est imposé. C'est la différence avec les malédictions actuelles.
- **Lien avec le plan 13** : le Pacte d'oubli (échanger un objet contre plus rare) devient une option de la Faille le jour où les objets existent.

**Stats de run nouvelles, en clair.** Deux seulement, affichées en pause :
- **Chance** : tirages de rareté partout ;
- **Péril** : difficulté contre récompense ; il remplace l'Appel du Vide et les malédictions.

### 4.6 Noms et descriptions des armes

**Règles :**
- nom court (1 à 3 mots) : un objet concret du monde d'avant, détourné ;
- pas d'adjectif générique (Ébréché, Rouillé, Lourd, de Fortune, Artisanal) ;
- pas de jargon (Vide, Essence, Mémoriel) ;
- une description de 3 à 8 mots, verbe d'action, sans chiffre ;
- une ligne sur l'ancien propriétaire (le champ `lore_flavor`, qui existe et n'est jamais affiché), montrée en pause et dans la Collection.

**Proposition de catalogue, à valider ou amender.** Rôles et équilibrage seront revus en vague 2.

| id | Nom actuel | Proposé | Description courte |
|---|---|---|---|
| chipped_blade | Lame Ébréchée | Faucille | Fauche en arc devant toi. |
| heavy_hammer | Marteau Lourd | Parcmètre | Frappe lourde qui repousse tout. |
| makeshift_bow | Arc de Fortune | Arc du gymnase | Tire une flèche droit devant. |
| sling | Fronde Rouillée | Lance-billes | Projette trois billes en éventail. |
| sharpened_pipe | Tuyau Affûté | Parapluie | Estoc rapide à courte portée. |
| crossbow | Arbalète Artisanale | Cloueuse | Des clous qui traversent les rangs. |
| cleaver | Couperet du Boucher | Pelle à neige | Balaie large autour de toi. |
| whip | Fouet de Câbles | Rallonge | Fouette tout autour de toi. |
| throwing_axes | Haches de Jet | Assiettes | Lance deux assiettes en éventail. |
| nail_mace | Masse Cloutée | Râteau | Griffe et fait saigner. |
| teachers_bell | La Cloche de l'Institutrice | Cloche d'école | Une onde qui repousse et ralentit. |
| surgeons_scalpel | Le Scalpel du Chirurgien | Scalpel | Coups rapides qui te soignent. |
| lighthouse_shard | Éclat de Phare | Lentille de phare | Un rayon qui traverse tout. |
| music_box | La Boîte à Musique | Boîte à musique | Des notes tournent autour de toi. |
| chain_of_names | La Chaîne des Noms | Trousseau | Des clés qui sautent de cible en cible. |
| compass_needle | L'Aiguille de Boussole | Boussole | Un tir qui trouve sa cible. |
| photographers_flash | Le Flash du Photographe | Polaroïd | Éblouit et désoriente. |
| essence_staff | Bâton d'Essence | Baguette de sourcier | Des tirs qui cherchent leur proie. |
| void_edge | Tranchant du Vide | Gomme | Efface ce qu'elle touche. |
| memory_lantern | Lanterne Mémorielle | Lampe à pétrole | Laisse le sol en feu. |
| echo_gauntlets | Gantelets d'Écho | Gants de boxe | Chaque coup frappe deux fois. |
| childs_drawing | Le Dessin d'Enfant | Dessin d'enfant | Des formes imprévisibles. |
| last_broadcast | La Dernière Émission | Transistor | Un cône d'ondes qui s'élargit. |
| clock_hand | L'Aiguille de l'Horloge | Aiguille d'horloge | Frappe et fige le temps autour. |

### 4.7 Sprites des armes

- **Pipeline** : les icônes sortent du pipeline SDF (`tools/sprites/`) qui fait déjà personnages, créatures, décors et projectiles. Même lumière, mêmes contours, palettes de la charte. Nouveau module `tools/sprites/weapons/`. L'ancien `scripts/generate_weapons.py` est retiré.
- **Format** : 32×32, affiché **à échelle entière** partout (HUD, cartes, pause, arme au sol), jamais réduit par un facteur non entier. Les cadres du HUD et des cartes s'adaptent à l'icône, pas l'inverse. La charte fixe 16×16 pour les icônes UI : **amendement de la charte demandé**, 16 px ne suffisant pas à rendre une Cloueuse reconnaissable.
- **Lecture** : silhouette identifiable en noir, fond neutre, orientation commune (diagonale bas-gauche → haut-droit), une couleur signature par arme, reprise dans ses effets d'attaque (`fx`).
- **Pilote de style** : trois icônes d'abord (Faucille, Cloueuse, Boîte à musique), sur une planche à taille réelle et en jeu, avant les 21 autres.
- **Arme en main** : aujourd'hui aucune. L'ajouter sur 8 directions et plusieurs personnages est un chantier lourd. **Décision demandée** (4.9).

### 4.8 Pause : armes, passifs et fiche du personnage

- **Armes** : les quatre armes, chacune avec icône, nom, niveau, stats effectives utiles à son pattern et **dégâts infligés pendant la run** (repère simple pour juger une arme).
- **Passifs** : les quatre passifs, avec niveau et effet total.
- **Fiche du personnage** : toutes les stats du joueur (PV max, régénération, armure, esquive, vitesse, dégâts, cadence, critique, portée, **zone**, aimant, Chance, Péril), les Oublis actifs, et plus tard les objets.
- **Portée et zone séparées** : `aoe_radius` multiplie aujourd'hui la portée, ce qui fait deux libellés pour un seul effet. Le contrat du plan 05 §4 les sépare.

### 4.9 Coffres réintégrés et visibles

- **Placement** : sur toute la carte, par bandes de distance **relatives au rayon de la carte**. Les quantités et bandes passent en données (`chests.json`). Il faut un dégagement réservé autour de chaque coffre (pas de décor, pas d'îlot d'immeuble), une préférence pour les chemins praticables, et la cible V2 : un coffre toutes les 60 à 90 s d'exploration active.
- **Sprite** : refait dans le pipeline SDF à l'échelle des personnages (≈ 28 à 36 px de large), avec ombre de contact. Quatre silhouettes distinctes : bois, métal, cristal, ancien.
- **Signal** :
  - une **colonne de lumière** à la couleur de la rareté, sur la grille des texels, visible au-delà du cadre ;
  - une invite « [E] Ouvrir » ;
  - un petit repère au bord de l'écran pour les coffres proches hors champ ;
  - un son quand un coffre entre dans le cadre (plan 15).
- **Palette de rareté unique** : une seule source de données pour les coffres, le level-up, le HUD et les armes au sol. Aujourd'hui, trois palettes se contredisent.
- **Contenu, en attendant le plan 13** : la roulette actuelle est gardée, mais :
  - perks V1 filtrés ;
  - malédictions retirées ;
  - libellés corrigés ;
  - la carte de coffre montre l'arme obtenue.

  La transformation en conteneurs de vestiges reste une décision du plan 13.

### 4.10 North star

Proposition d'énoncé, à valider avant de l'inscrire dans la Stratégie V2 : **« Avancer dans un monde qui s'oublie, arracher ce qui compte à l'oubli, et devenir quelqu'un d'unique avec ce qu'on a sauvé. »**

Quatre piliers signatures, dont deux sont déjà en place :
1. **Le monde s'efface derrière toi** : la carte est une ressource qu'on consomme. En place.
2. **L'oubli est la monnaie du risque** : score, Essence, raretés, Mémoriaux et Failles (4.4, 4.5). Partiellement en place (plan 16 O5).
3. **Tout a appartenu à quelqu'un** : armes et objets portent une vie. Leur **Éveil** raconte cette vie (vague 5).
4. **Le monde se souvient de toi** : l'Écho de ta dernière run (plan 14 B), ce que tu as sauvé ou laissé. À prototyper (vague 5).

## 5. Vagues

Un lot à la fois, chacun vérifié et recetté avant le suivant. Les vagues 0 et 1 ne demandent pas d'arbitrage de fond, en dehors des questions du §6.

### Vague 0 — Remise en ordre (courte)

| Lot | Contenu | Vérification |
|---|---|---|
| **0A — Coffres** | Placement sur toute la carte en données, dégagement, nouveaux sprites, colonne de lumière, invite, repère de bord, palette de rareté unique, contenu nettoyé | Nouveau mode `--capture-chests` (chaque coffre cadré, avec et sans décors) ; coffres entrés à l'écran en 3 min sur 5 seeds, avant/après ; captures 720p et 1080p regardées |
| **0B — Restes V1 visibles** | Corrections sans arbitrage du [plan 18](18-inventaire-restes-v1.md) : perks V1 hors du butin, Tisseuse avec son sprite, lumières texturées par le logo Godot remplacées, faux Souvenirs et buff mort retirés, marchand inerte retiré | Build, smoke, captures ; test de tirage de butin sur 1 000 coffres simulés |
| **0C — Nettoyage invisible** | Code et données morts du plan 18 (ressources, craft, Mémorial de perks, minicarte, fusions si 4.3 retenu, noms « nuit »), PNG orphelins | Build sans warning, smoke, régressions |
| **0D — Doc** | Mentions du jeu de référence ramenées aux citations de Raphaël et aux garde-fous ; une section « Références et garde-fous » unique dans la Stratégie V2 ; `PROGRESSION-SYSTEM.md` réécrit sans modèle externe | Relecture |

### Vague 1 — Armes : présentation et montée en puissance

| Lot | Contenu | Vérification |
|---|---|---|
| **1A — Socle** | Bloc de stats commun (joueur et armes), stats montables par arme en JSON, niveau unique, portée et zone séparées. Corrections : effets par arme source, Boîte à musique, paliers vides, bannissement | Tests d'intégration (deux armes à effets différents, notes dès l'équipement) ; build ; régressions |
| **1B — Level-up à raretés** | Tirage (Chance, oubli), cartes « avant → après », carte d'arme nouvelle avec description, clavier et manette, relance, bannissement, passer | Distribution des raretés sur 10 000 tirages ; captures des cinq raretés ; recette de Raphaël |
| **1C — Pause** | Armes, passifs, fiche du personnage, dégâts par arme | Captures 720p et 1080p ; grand texte |

Pilote de style pendant la vague 1 : trois icônes (4.7), pour que les cartes et la pause se construisent au bon format.

### Vague 2 — Armes : identité

| Lot | Contenu | Vérification |
|---|---|---|
| **2A — Catalogue** | Fiche de chaque arme (rôle, faiblesse, stats montables), noms et descriptions validés, part d'armes à distance relevée, allonge de la mêlée (acquis du 22 septembre) | Relecture de Raphaël ; mesures de portée (plan 05 §4) |
| **2B — Sprites** | 24 icônes dans le pipeline, arme au sol à échelle entière, couleur signature dans les effets | Planche à taille réelle ; captures en jeu |
| **2C — Arme en main** | Seulement si décidé (4.7) | Captures 8 directions |

### Vague 3 — Modificateurs de run

| Lot | Contenu | Vérification |
|---|---|---|
| **3A — Chance et Péril** | Chance branchée sur tous les tirages, Péril remplaçant l'Appel du Vide et les malédictions, bonus de rareté près de l'oubli | Distributions mesurées ; test d'intégration |
| **3B — Mémorial** | Autel refondu : éclats, bénédictions à rareté, zone ravivée, Essence (raviver l'arme choisie, soin, relance), sprite | Captures ; recette : attendre ou activer ? |
| **3C — Faille** | Oublis, récompense forte, Péril, lever un Oubli au Mémorial, sprite | Recette : tentant ou punitif ? |

### Vague 4 — Objets

Socle d'objets du plan 05, premier lot du plan 13 (lot A), coffres en conteneurs si retenu. Les objets reprennent le bloc de stats de 1A et les raretés de 1B. Ce sont eux aussi des objets du monde d'avant, avec un propriétaire. La crainte de copier se traite par le fond : effets liés au mouvement, à l'oubli et aux lieux, présentation propre au plan 13.

### Vague 5 — Signatures

Un prototype à la fois (plan 11) :
- **Éveil des armes** : à un certain niveau, une condition liée à l'histoire de l'objet (activer un Mémorial avec la Boîte à musique, abattre un Souverain avec la Cloche…) transforme l'arme en une version unique, avec son visuel. L'Éveil remplace les fusions, jamais branchées.
- **Écho de ta dernière run** (plan 14 B).
- **Rémanence du mouvement** (plan 11 A).

## 6. Décisions demandées à Raphaël

1. **L'ordre des vagues** : 0 → 1 → 2 → 3 → 4 → 5. *Alternative* : identité (2) avant présentation (1), mais les sprites seraient produits avant de connaître leur format d'affichage.
2. **La rareté** : sur les améliorations et les objets seulement (recommandé, 4.3), ou aussi sur l'arme (V2 §10) ?
3. **Les noms** : la direction « objet du quotidien détourné » et le catalogue du 4.6 (validé, amendé ou à refaire).
4. **Les icônes en 32×32** (amendement de la charte) et **l'arme en main** : oui, non ou plus tard ?
5. **Mémoriaux et Failles** (4.5) : la direction, et la fusion Autel → Mémorial.
6. **Le plafond de niveau d'arme** : aucun (recommandé) ou borné ?
7. **Le plan 18** : les décisions listées en fin de plan (éléments de lore vectoriels, POI, identifiants Steam « nuits »).

Tous les chiffres de ce plan sont des points de départ, pas des réglages mesurés.

### Réponses de Raphaël — 26 septembre 2026

| # | Réponse | Application |
|---|---|---|
| 1 | « Fais selon ce qui te semble le plus logique » | Ordre retenu : 0 → 1 → 2 → 3 → 4 → 5, un lot à la fois (0A, 0B, 0C, 0D, 1A…) |
| 2 | Pas de réponse | Recommandation 4.3 **provisoire** : ne pas la trancher seul. La confirmer avec Raphaël au début de la vague 1, avant le lot 1A |
| 3 | « La direction des noms me paraît beaucoup mieux » | Direction validée ; le catalogue du 4.6 sert de base au lot 2A, où chaque nom sera confirmé |
| 4 | Icônes 32×32 : oui. Arme en main : « à tester », plutôt **désactivée par défaut**, car le joueur peut en avoir quatre | Amendement de la charte à faire en 2B ; 2C devient un essai derrière une option, désactivée par défaut |
| 5 | Mémoriaux et Failles : « oui, ça me va » | Direction validée pour la vague 3, Autel refondu en Mémorial compris |
| 6 | Plafond de niveau d'arme « à déterminer (niveau 50, 100 ?) » | Plafond en données ; valeur à proposer au lot 1B à partir de la courbe d'XP et de la durée de run, mesurée |
| 7 | Voir le [plan 18 §5](18-inventaire-restes-v1.md#5-décisions-demandées-à-raphaël) | — |
