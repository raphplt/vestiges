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

#### Lot 0A détaillé — 26 septembre

Périmètre : §4.9 et les lignes 0A du plan 18 (9 à 13, 18, 21). Hors lot : le son d'entrée dans le cadre (candidats à choisir par Raphaël, plan 15), le tirage de butin sur 1 000 coffres et la purge de `perks.json` (0B), la refonte en conteneurs de vestiges (plan 13).

| # | Étape | Détail |
|---|---|---|
| 1 | Mesure avant | Banc `--density` : coffres entrés dans le cadre en 3 min (`chests_seen`, `first_chest_s`), 5 seeds, sur le code d'avant le lot |
| 2 | Placement en données | `data/chests/chest_placement.json` : groupes (coffre, nombre, bande de distance en fraction du rayon de carte), un coffre commun garanti près du départ, écart minimal entre coffres en pixels. Tirage uniforme en surface dans la bande, graine du monde. Refus : eau, oubli, îlots d'immeubles (et leur voisinage au sud, qui masquerait le coffre). Préférence pour les rues, places et allées des champs |
| 3 | Dégagement | Cellules réservées autour du coffre, en pixels écran (côtés, nord, et surtout sud : un arbre de 118 px posé devant le couvrirait). Les placeurs de décors les évitent déjà (`usedCells`) |
| 4 | Sprites | `tools/sprites/props/chests.py`, rendus par `generate_props.py chests` : bois, métal, cristal, ancien, fermé et ouvert, ≈ 30 px de large, pivot au manifeste, ombre de contact commune aux décors |
| 5 | Palette de rareté unique | `data/ui/rarities.json` (clé de nom traduite, couleur, tons clair et sombre) lue par `RarityPalette` ; les coffres, le level-up, l'écran de butin, le HUD et les armes au sol s'y alimentent. `weapon_rarity.json` ne garde que les poids et multiplicateurs |
| 6 | Colonne de lumière | `LightColumn` : shader sur la grille des texels (trois tons de la rareté, tramage, poussières qui montent), commune aux coffres et au repère des micro-événements (`RunEventMarker`). Remplace l'aura en losange des coffres |
| 7 | Invite et jauge | Invite « [touche] Ouvrir » au-dessus du coffre le plus proche à portée (clavier ou manette selon la dernière entrée) ; jauge d'ouverture en pixels à la place de la barre arrondie « harvest » |
| 8 | Repère de bord | Flèches au bord de l'écran vers les coffres fermés proches hors du cadre, couleur de rareté, trois au plus |
| 9 | Contenu | Perks tirés avec le filtre du level-up (plus de perks V1) ; malédictions retirées des tables ; butin résolu **avant** la roulette, qui montre l'arme, le perk ou le Souvenir obtenus ; libellés traduits et accentués ; popups de butin posées dans le monde supprimées (la roulette les remplace) |
| 10 | Vérification | Build, smoke ; `--capture-chests` (chaque coffre cadré, avec et sans décors) en 720p et 1080p, regardé ; mesure après sur les 5 mêmes seeds |

#### Lot 0A livré — 26 septembre

**Fait :**
- **Placement** (`ChestSpawner`, `data/chests/chest_placement.json`) : 23 coffres par carte (12 communs, 6 rares, 3 anciens, 2 épiques), bandes de 0,06 à 0,95 du rayon, un commun garanti entre 0,05 et 0,09 (toujours à portée des flèches au départ), 720 px au moins entre deux coffres. Tirage par la graine du monde. Refus dans l'eau, l'oubli, les îlots d'immeubles et leur abord sud ; bonus aux rues, places et allées des champs. Avant : 10 à 15 coffres selon la seed, tous à moins de 55 cellules du départ.
- **Dégagement** : 72 px de côté, 32 au nord, 124 au sud, réservés avant les décors.
- **Sprites** (`tools/sprites/props/chests.py`, `generate_props.py chests`) : malle de bois, cantine de métal, caisse de fer envahie de cristaux, châsse de pierre à voûte dorée ; fermés 28×29 à 32×35 px (avant 16×12), ouverts avec couvercle levé et intérieur sombre ; pivot et emprise au manifeste, ombre de contact des décors.
- **Palette de rareté unique** (`data/ui/rarities.json`, `RarityPalette`) : couleurs et noms traduits ; lue par les coffres, le level-up, l'écran de butin, le HUD et les armes au sol. `weapon_rarity.json` ne garde que poids et multiplicateurs. Le « rare » du level-up passe de l'orange au bleu, « Peu commun » devient « Inhabituel ».
- **Colonne de lumière** (`LightColumn`, `light_column.gdshader`) : grille des texels, trois tons de la rareté, pleine sur les deux tiers puis tramée, poussières qui montent, tache au sol. Hauteur et largeur par coffre (commun 110 px, épique 160 px). Le repère des micro-événements l'utilise aussi. L'aura en losange ne sert plus aux coffres.
- **Invite et jauge** : « [E] Ouvrir » en police pixel au-dessus du coffre le plus proche, touche clavier ou manette selon la dernière entrée ; jauge pixel 24×3 à la couleur de la rareté, qui remplace la barre arrondie « harvest » (POI compris).
- **Repères de bord** (`ChestPointers`) : trois flèches au plus vers les coffres fermés à moins de 1 200 px, hors du cadre, au-dessus de la barre d'armes.
- **Contenu** : perks tirés par le filtre du level-up (`PerkManager.PickLootPerk`, perks V1 exclus) ; malédictions retirées des tables ; butin résolu avant la roulette (`LootRewards`), qui affiche « Arme : … », « Don : … », « Souvenir : … » ; un Souvenir quand tous sont retrouvés devient 8 d'Essence ; plus de popups de butin posées dans le monde pour les coffres.
- **Code** : l'ouverture quitte `Player.cs` pour `ChestInteraction` (`Player.cs` passe de 2 686 à 2 362 lignes) ; registre `Chest.Closed` au lieu des recherches par groupe.
- **Banc** : `--capture-chests` ; la mesure de densité compte les coffres entrés dans le cadre, non masqués par un décor, et signalés (coffre, haut de colonne ou flèche visibles) ; `--nomad` fait garder un cap au bot.
- **Mesure headless** (`tools/measure_run.sh`, demande de Raphaël en cours de lot) : la mesure de densité suit le temps de jeu, sans rendu, en `--fixed-fps 60` ; compilation et import une seule fois, seeds en parallèle (`MEASURE_JOBS`). 5 seeds × 180 s de jeu en 67 s au lieu d'environ 17 min en fenêtre. Les deux côtés d'un avant/après se mesurent avec le même mode : sur une même seed, fenêtre et headless ne donnent pas les mêmes éliminations (run chaotique, machine chargée).

**Mesures** (headless, bot nomade invincible, seeds 221092026, 1002, 7, 42, 20260926 ; moyennes) :

| | Coffres générés | Vus en 3 min | Signalés en 3 min | Vus en 8 min | Non masqués en 8 min | Signalés en 8 min |
|---|---|---|---|---|---|---|
| Avant | 10 à 15 | 4,6 | 4,6 | 8,8 | 7,8 | 8,8 |
| Après | 23 (+ coffres d'événement) | 5,0 | 9,6 | 10,6 | 10,6 | 17,2 |

- La série de 8 min « après » précède le dernier réglage du premier coffre (bande 0,06 à 0,12 au lieu de 0,05 à 0,09) ; celle de 3 min le suit.
- « Vu » : le coffre entre dans le cadre. Avant le lot, un coffre vu faisait 16×12 px sans signal ; il n'était pas pour autant remarqué (aucun ouvert dans l'historique). La mesure ne dit pas si le joueur le remarque : c'est la recette qui le dira.
- Après : un coffre vu toutes les 45 s environ pour ce bot, plus que la cible V2 (60 à 90 s). Le bot nomade couvre peut-être plus de terrain qu'un joueur ; le nombre de coffres se règle dans `chest_placement.json`.
- Mesure en fenêtre avant le passage au headless (avant, nomade, 3 min) : 1, 4, 8, 9 et 11 coffres vus selon la seed, du même ordre.
- Captures regardées : chaque coffre de deux seeds (221092026 champs et carrière, 1002 ville), avec et sans décors ; aucun masqué. Départ de trois seeds : le premier coffre est à l'écran ou montré par une flèche. Coffre hors cadre : haut de colonne au bord bas et flèche. Ouverture : jauge bleue, puis roulette « COFFRE RARE » avec « Don : Embrasement » et « Essence ×14 », appliqués ensuite (score, Essence).
- 720p : sur ce Mac, `capture_run.sh` ignore `--resolution` (fenêtre plein écran, image en 3840×2160). Le 720p est jugé sur l'image réduite : colonnes, invite et flèches restent lisibles ; les flèches y sont petites.
- Vérifié aussi : build sans avertissement, smoke, `test_movement` (dont les deux cas coffre, adaptés au composant), `test_enemy_abilities`, `test_dev_mode`. Relecture `godot-reviewer` : deux plantages possibles (sprites de coffre absents du JSON ou du disque) et deux points mineurs, corrigés.
- Coût : non mesuré au banc de combat (machine chargée, charge 9). Une colonne est un quad de 30×120 px environ, écarté du rendu hors écran ; les flèches parcourent 23 coffres par frame, sans allocation.
- Constat hors lot : `RunTracker.RunDurationSeconds` suit l'horloge murale, pauses comprises. Le score de survie et la quête de durée comptent donc le temps passé en pause.

**Écarts au plan :**
- Pas de son d'entrée dans le cadre : il attend un choix de Raphaël parmi des candidats (plan 15).
- Les POI et Autels gardent leur aura en losange jusqu'à leur refonte (0B, vague 3).
- L'écran de roulette garde sa mise en page ; seuls couleurs, titres et contenu changent.

**Recette attendue** : les coffres se voient-ils sans les chercher ? Les colonnes gênent-elles ? Fréquence (un coffre vu toutes les 45 s pour le bot) ; sprites des quatre coffres ; invite et flèches ; « Don » pour nommer un perk reçu.

#### Lot 0B détaillé — 26 septembre

Périmètre : lignes 0B du plan 18 (1, 3 à 8, 20) et ses réponses §5. Raphaël enchaîne les lots et fera la recette à la fin (DECISIONS, 26 septembre).

| # | Étape | Détail |
|---|---|---|
| 1 | Flash au logo Godot | Supprimer la `PointLight2D` texturée par `icon.svg` du level-up et des explosions d'élites (`VfxFactory.CreateFlashLight`) ; particules et sprite d'explosion restent |
| 2 | POI et éléments de lore | Désactivés par données (`pois_enabled`, `lore_elements_enabled` dans `world_gen.json`), code gardé jusqu'à la refonte (vague 3, plan 08). Les coffres restent : ils ne dépendent plus des POI |
| 3 | Quête « Explorer 3 points d'intérêt » | Champ `enabled` lu par `QuestDataLoader` ; la quête passe à `false` jusqu'au retour des POI (reste 6 quêtes de run pour 3 tirées) |
| 4 | Effacement sans POI | Les POI et les éléments de lore stabilisaient la mémoire autour d'eux. L'ouverture d'un coffre prend le relais (`ChestOpened` → `StabilizeZone`), réglable dans `erasure.json` ; vérifié par capture de l'oubli |
| 5 | Faux Souvenirs, buff `warmth` | Émissions retirées des trois éléments de lore et de la porte (aucun effet réel, un succès débloqué à tort) |
| 6 | Marchand | Retiré de `pois.json` et des cinq tirages de POI |
| 7 | Tisseuse | `sprite_folder` au lieu de `sprite`, pieds calés par capture du bestiaire |
| 8 | Perks V1 dans le butin | Filtre livré en 0A ; test de tirage sur 1 000 coffres par type (`--loot-draws`), aucun perk exclu ni malédiction |
| 9 | Passif `fragment_deternite` | Proposé au level-up sans aucun effet : `enabled: false`, retiré des tirages, gardé pour les sauvegardes |

#### Lot 0B livré — 26 septembre

**Fait :**
- Flash de level-up et d'explosion d'élite : la `PointLight2D` au logo Godot est supprimée (`VfxFactory.CreateFlashLight` retirée) ; particules et sprite d'explosion restent.
- POI et éléments de lore désactivés par `world_gen.json` (`pois_enabled`, `lore_elements_enabled`) ; leur code reste pour la refonte. Le mutateur `PoisDisabled` ne retire plus les coffres.
- Quête « Lire les traces » (explorer 3 POI) : `enabled: false`, lu par `QuestDataLoader` ; il reste 6 quêtes de run.
- Effacement : ouvrir un coffre ravive la mémoire alentour comme l'exploration d'un POI (`stabilize_on_chest_open` dans `erasure.json`). Sans cela, seuls les vrais Souvenirs stabilisaient encore.
- Faux Souvenirs des éléments de lore et buff `warmth` retirés ; signal `PlayerBuffApplied` supprimé (plus aucun émetteur).
- Marchand retiré de `pois.json` et des cinq tirages de biome.
- Tisseuse : `sprite_folder` et `sprite_feet_offset: 19` ; ses 112 images servent enfin.
- Passif `fragment_deternite` retiré des tirages.
- Test `--loot-draws 1000` : 4 000 coffres tirés sans perk exclu ni malédiction, 27 perks distincts ; les coffres anciens ne donnent que de l'Essence en profil dev (tous les Souvenirs y sont déjà retrouvés), repli prévu.

**Trouvé en vérifiant (commits à part) :**
- Blocage de partie : une arme ramassée pendant l'écran de niveau rendait le choix « nouvelle arme » impossible ; l'écran se fermait et le jeu restait en pause. Le banc headless s'y est bloqué (seed 20260926). Un choix devenu impossible relance maintenant une offre à jour (`a90c36e`).
- Durée de run à l'horloge murale, pauses comprises (score de survie, quête de durée, historique) : elle suit le temps de jeu (`6613cb4`).

**Vérifié :** build sans avertissement, smoke, `test_movement`, `test_enemy_abilities`, `test_dev_mode` ; captures de la Tisseuse (sprite d'araignée, ombre sous les pattes) et des phases de l'oubli (inchangées) ; journal : aucun POI ni élément de lore généré, 23 coffres.

**Point ouvert :** les Souvenirs ne viennent plus que des coffres anciens (3 par carte) ; les sanctuaires et bâtiments à fouiller en donnaient aussi. La progression méta ralentit jusqu'au retour des Mémoriaux (vague 3).

Mesure après 0B (headless, nomade, 8 min, 5 seeds) : les cinq parties vont au bout, dont la seed qui restait bloquée ; 11,4 coffres vus et 15,2 signalés en moyenne.

#### Lot 0C livré — 26 septembre

Nettoyage de ce que le joueur ne voit pas (plan 18 §4). Aucun comportement de jeu ne change, sauf la disparition des Colosses, qui n'apparaissaient plus.

**Retiré :**
- Chargeurs et données sans lecteur : `ResourceDataLoader` et `resources.json`, gabarit de recettes, lot de simulation, champs de biome `resource_bias` et `ambient_color_day/dusk` ; gabarits de biome et de butin réécrits au format V2.
- Classes mortes : `Minimap`, `EventSpriteFactory` ; 12 `.cs.uid` orphelins (Base, craft, Foyer, recettes).
- Mémorial de perks : écouteur de `PerkManager` et mode « MÉMORIAL » de `LevelUpScreen` ; effet `bonus_resource`.
- 13 perks V1 de `perks.json` (structures, jour et nuit, Foyer, récolte, lumière) et les deux synergies qui en dépendaient ; stat `harvest_speed`.
- Visibilité factice (`IsPositionVisible`, toujours vraie, appelée dans cinq boucles de ciblage) et pénalité d'Essence inerte des armes.
- Signaux jamais émis : `XpMultiplierChanged`, `FogRevealBurst`.
- Colosses : données, 257 sprites, générateurs, comportement de charge et de slam, barème de score, succès Steam.
- Nuits : champs du score et de l'historique (`nights_survived`, `death_night`, structures, ressources), `CurrentNight`, alias `GetMaxNights`, `GetTopByNights`, moyennes de nuits.
- 306 PNG jamais affichés, avec leur `.import` : ressources, structures, outils, objets V1, Foyer, icônes de HUD V1, fonds et aperçus, brouillard, sanctuaire, `ombre`, aperçus d'ennemis, 13 variantes d'armes, icônes d'état, planches ; générateurs `generate_resources.py` et `generate_structures.py`. Vérification préalable : ni chemin, ni nom, ni uid cités, chargeurs par convention compris.
- Traductions `CRAFT_*`.

**Renommé :** pools d'ennemis `exploration_enemy_pool` et `resurgence_enemy_pool` ; condition `survive_12_minutes` (Vagabond) ; succès `ACH_SURVIVE_CRISIS_*`, stat `STAT_MAX_CRISES`, classement `Vestiges_Crises` (jeu absent de Steam, renommage libre) ; succès impossibles retirés ; déblocage de Souvenir `weapon` au lieu de `recipe` ; textes « nuit » de deux armes.

**Gardé, à dessein :**
- Signaux émis mais pas encore écoutés (`WeaponDropped`, fusions, `RandomEventEnded`) et `MemorialActivated` : le catalogue audio d'une autre session (plan 15) s'en sert comme points d'accroche des sons.
- Ambiance sonore des Colosses dans `AudioManager` et sons jamais joués : fichiers du plan 15, en cours dans une autre session.
- Fusions (dépendent de la décision 4.3), synergies restantes, squelette des mutateurs (V2 §18), migration de `max_nights_survived`.
- Quatre décors urbains jamais placés (`prop_supermarket_shelves`, `prop_collapsed_stairs`, `prop_graffiti_wall`, `prop_concrete_wall_v2`) : à Raphaël de dire s'ils servent à de futurs intérieurs.
- `scripts/generate_weapons.py` : il sera retiré avec les nouvelles icônes (2B) ; relancé, il recréerait les variantes supprimées.

**Aussi :** les flèches de bord d'écran évitent désormais la bande haute du HUD (score, progression).

**Vérifié :** build sans avertissement, smoke, `test_movement`, `test_enemy_abilities`, `test_dev_mode` ; trois runs headless de 5 min sans erreur de ressource manquante ; capture en fenêtre (ville, HUD complet).

#### Lot 0D livré — 26 septembre

- **Stratégie V2** : section unique « Références et garde-fous » (§2) : conventions du genre assumées, citations de Raphaël, garde-fous du principe 5, références d'atmosphère. Les deux passages qui comparaient la V2 à d'autres jeux sont reformulés.
- **GDD** : le tableau des références renvoie à cette section et ne garde que l'atmosphère ; six « comme … » / « inspiré de … » retirés.
- **Plans 02, 05, 06** : les liens vers les wikis et la page Steam du jeu de référence sont remplacés par un renvoi aux garde-fous.
- **`PROGRESSION-SYSTEM.md`** réécrit (v2.0) : sans modèle externe, et aligné sur la V2 (plus de Foyer, de nuits ni de POI ; coffres, blocage de niveau corrigé, évolutions du plan 17).
- **Restent, à dessein** : citations de Raphaël (registre, plan 13, README), ordres de grandeur de densité du plan 03, roadmap V1 historique, fichiers audio d'une autre session.

### Vague 1 — Armes : présentation et montée en puissance

| Lot | Contenu | Vérification |
|---|---|---|
| **1A — Socle** | Bloc de stats commun (joueur et armes), stats montables par arme en JSON, niveau unique, portée et zone séparées. Corrections : effets par arme source, Boîte à musique, paliers vides, bannissement | Tests d'intégration (deux armes à effets différents, notes dès l'équipement) ; build ; régressions |
| **1B — Level-up à raretés** | Tirage (Chance, oubli), cartes « avant → après », carte d'arme nouvelle avec description, clavier et manette, relance, bannissement, passer | Distribution des raretés sur 10 000 tirages ; captures des cinq raretés ; recette de Raphaël |
| **1C — Pause** | Armes, passifs, fiche du personnage, dégâts par arme | Captures 720p et 1080p ; grand texte |

Pilote de style pendant la vague 1 : trois icônes (4.7), pour que les cartes et la pause se construisent au bon format.

#### Lot 1A, corrections livrées — 26 septembre

La décision 4.3 (rareté sur l'arme ou seulement sur les améliorations) n'est pas prise : le socle de 1A (bloc de stats commun, stats montables par arme, portée et zone séparées) l'attend. Les quatre bugs de build, eux, n'en dépendent pas.

- **Effets de l'arme qui frappe** : les projectiles portent leur arme (`Projectile.SourceInstance`), les orbitales et le cône la leur ; effet au contact, recul et effet spécial viennent de cette arme, plus de la dernière qui a tiré. Les perks (vampirisme, embrasement…) restent globaux.
- **Boîte à musique** : ses notes apparaissent dès qu'elle est portée (avant : 20 s, son minuteur à `attack_speed` 0) ; leurs dégâts suivent le niveau de l'arme à chaque impact, leur nombre est recalculé à chaque montée (level-up, Autel, reforge) ; retirer l'arme retire ses notes.
- **Niveau unique** : il n'existe plus que sur l'instance d'arme ; badge du HUD, carte de level-up, Autel et fusions lisent la même valeur. Plafond lu dans `weapon_upgrades.json` (8, inchangé).
- **Bannissement** : bannir écarte l'arme ou le passif de la run, améliorations comprises ; un bannissement qui viderait l'offre est refusé et non consommé (l'écran restait ouvert sans carte).
- **Banc** `tools/test_weapons.sh` (`WeaponRegression`) : 5 vérifications, toutes vertes.
- **Mesure** (headless, nomade, 5 min, 3 seeds) : éliminations 575 et 547 contre 527 et 498 avant ; 139 contre 528 sur la seed 1002, où les offres ont divergé (le bot, qui prend la première offre, n'a eu que des passifs jusqu'au niveau 5). Les projectiles n'empruntent plus le recul ni les effets des armes de mêlée : l'équilibrage de 2A en tiendra compte.

#### Vague 1 détaillée — 26 septembre (soir)

Décision 4.3 prise : la rareté vit sur les améliorations, plus sur l'arme. Choix de mise en œuvre tranchés ici, provisoires jusqu'à la recette :

| Lot | Contenu | Choix |
|---|---|---|
| **1A — Socle** | Rareté d'arme retirée (instances, butin, armes au sol, HUD, Autel : reforge supprimé, amélioration conservée). Chaque arme déclare ses **stats montables** dans `weapons.json` (stat, poids, pas, mode) et ses **paliers** (stats entières). Bonus accumulés sur l'arme, plus de table commune par niveau. Portée et zone séparées selon le contrat du plan 05 §4. Catalogue des stats (libellé, format, unité) commun au joueur et aux armes | Pas de base : dégâts +12 %, cadence +8 %, portée +6 %, zone +8 %, recul +15 %, vitesse de projectile +10 % ; paliers +1. Portée : allonge des coups, distance des tirs, rayon d'orbite. Zone : arcs et cônes (angle), ondes, feux au sol, explosions |
| **1B — Level-up à raretés** | Tirage de la rareté (poids, Chance, oubli), carte « avant → après » calculée sur les valeurs effectives, carte « Nouvelle » avec description courte (colonne du §4.6), clavier et manette, relance, bannissement, passer. Plafond de niveau en données | Poids 60 / 25 / 11 / 3,5 / 0,5. Chaque cran d'oubli (Fragile 1, Effilochée 2, Effacée 3) et la Chance donnent une chance de monter d'une rareté. Plafond proposé après mesure du nombre de level-ups sur une run de 20 min |
| **1C — Pause** | Armes (icône, nom, niveau, stats effectives utiles au motif, dégâts infligés dans la run), passifs (niveau, effet total), fiche du personnage | Dégâts par arme comptés à l'impact, l'arme source étant désormais connue |

#### Vague 1 livrée — 26 septembre (soir)

**1A — Socle**
- Rareté d'arme retirée : `WeaponInstance` n'a qu'un niveau et les gains accumulés de ses améliorations ; `WeaponRarityDataLoader`, `weapon_rarity.json` et le reforgeage de l'Autel disparaissent. L'Autel donne une amélioration Rare au moins.
- Croissance en données : chaque arme de `weapons.json` déclare `growth` (stat → poids) et `milestones` (stats entières) ; les pas vivent une fois dans `weapon_upgrades.json`. Plafond de niveau d'arme : 50, en données. Mesure : en 20 min de jeu, le bot atteint le niveau 45 ; les passifs plafonnent à 5, chaque arme reçoit donc 8 à 10 améliorations par run. Le plafond ne mord pas : c'est un garde-fou, le rendement vient des poids (proposition du §4.1).
- Fusions retirées (jamais appliquées, rendues inatteignables par le plafond ; l'Éveil de la vague 5 les remplace).
- Portée et zone séparées (plan 05 §4). Portée : allonge des coups, distance des tirs, rayon d'orbite. Zone : ouverture des arcs et des cônes, rayon des ondes circulaires (Fouet, Cloche), des échos, des ralentissements, des formes, des feux au sol et des notes. Les passifs et perks de zone n'allongent plus les tirs.
- Catalogue des stats affichées (`data/ui/stats.json`, `StatCatalog`) : libellé traduit, forme (valeur, entier, pourcentage), nombres à la française.

**1B — Level-up à raretés**
- Tirage (`UpgradeRoller`, `data/progression/upgrade_rarities.json`) : poids 60 / 25 / 11 / 3,5 / 0,5, puis une chance de 30 % de monter d'un rang par cran (Chance, oubli : Fragile 1, Effilochée 2, Effacée 3).
- Mesure sur 10 000 tirages : zone intacte 60,7 / 24,3 / 10,9 / 3,5 / 0,7 % ; zone Effacée 20,6 / 35,3 / 25,5 / 12,8 / 5,9 %.
- Arme : 1 à 3 stats selon la rareté, au gain de la rareté ; palier (+1 projectile, perçage, rebond ou note) aux raretés Épique et Légendaire si l'arme en a. Passif : écart entre deux niveaux de la table × 1 à 3 ; les passifs entiers sautent un niveau de plus aux grandes raretés.
- Cartes : rareté en couleur et en symbole (rien, ◆, ◆◆, ★, ★★), cadre plus épais aux grandes raretés ; « Dégâts 29,0 → 36,0 +24 % » sur les valeurs effectives ; portée, zone et vitesses en pourcentage seul ; carte « Nouvelle arme » avec la phrase du §4.6 et la famille (mêlée ou distance).
- Commandes : haut et bas entre les cartes et les actions, validation pour choisir ; relance, bannissement et « Passer ».

**1C — Pause**
- Trois panneaux : équipement (armes : niveau, dégâts, cadence, compteurs, dégâts infligés dans la run ; passifs : niveau et effet total), boutons, fiche du personnage (PV, régénération, armure, esquive, vitesse, dégâts, cadence, critique, portée, zone, aimant, Chance, et les bonus présents).
- Péril et Oublis viendront avec la vague 3.

**Vérifié :**
- Build sans avertissement, smoke, `test_movement`, `test_enemy_abilities`, `test_dev_mode`.
- `test_weapons` (11 vérifications) : distribution des raretés, gains d'un Légendaire (3 stats et un palier), zone +20 % sans allonger la portée, notes qui touchent.
- Captures du level-up aux cinq raretés et de la pause, regardées.
- Headless 8 min (3 seeds) : aucune erreur. Éliminations 842, 1 104 et 215. La seed 42 est basse parce que le bot prend toujours la première carte et n'a reçu que des passifs jusqu'au niveau 5 (une seule arme jusqu'à 394 s). Ce n'est pas un défaut de combat.

**Points ouverts :**
- Les passifs gagnent peu par niveau dans leurs tables actuelles (Flamme intérieure : +10 % puis +2 % par niveau) : à revoir avec l'équilibrage de 2A.
- Les icônes restent celles de mars jusqu'au lot 2B.

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

**3A livré (26 septembre, nuit).** `PerilManager` (nœud de run) tient le Péril et reste le seul émetteur de `DifficultyModifierChanged` ; `PerilChanged` prévient score, level-up et fiche. Chaque point, d'après `data/scaling/peril.json` : créatures +6 % en nombre, +10 % PV, +6 % dégâts ; XP +8 %, score +12 %, un demi-cran de montée de rareté. Plafond 10. Retirés : perk Appel du Vide (données, bouton de pause, icône, tableaux `per_stack`), `CursedItemManager` et `cursed_items.json` (jamais branchés). La fiche de pause montre le Péril et ses effets ; l'écran de fin le compte dans le multiplicateur. Le Péril n'a pas encore de source : les Failles (3C) la donnent.

Vérifié : build sans avertissement, smoke test, `tools/test_weapons.sh` (12 contrôles, dont « 4 points de Péril = 2 crans »). Mesure headless (`MEASURE_EXTRA_ARGS="--peril 5"`, 3 graines × 180 s, temps de jeu) :

| | Créatures apparues | Dégâts reçus à 30 s | Niveau à 180 s |
|---|---|---|---|
| Péril 0 | 330 / 451 / 278 | 15 / 0 / 57 | 4 / 6 / 5 |
| Péril 5 | 399 / 454 / 460 | 115 / 84 / 77 | 3 / 11 / 15 |

La pression monte comme prévu. Le niveau atteint varie trop avec les armes que le bot prend (première carte) pour juger le gain d'XP : à regarder en jeu, les valeurs sont un point de départ.

**3B livré (26 septembre, nuit).** Cinq Mémoriaux par carte (un près du départ, quatre au loin), placés comme les coffres et après eux par un placeur commun (`SitePlacer`) : ils s'écartent des coffres et réservent leur dégagement avant les décors. Endormi, un Mémorial porte une colonne de lumière turquoise (teinte `memorial` de la palette). « Raviver » libère trois éclats à 200–380 px, à ramasser en 20 s ; sinon ils s'éteignent et on peut réessayer. Ravivé : la zone est stabilisée (`MemorialAwakened` → Effacement), puis trois bénédictions à rareté (Inhabituel au moins ; Chance, oubli de la zone et Péril la font monter). Ensuite « Honorer » ouvre les services : raviver une arme au choix (amélioration Rare au moins, 30 Essence) ou se soigner (40 % des PV, 20 Essence) ; chaque usage au même Mémorial augmente le prix de moitié. Un Mémorial englouti par le Néant est perdu. Réglages : `data/world/landmarks.json`, `data/progression/blessings.json` (dix bénédictions).

Socle commun posé pour la suite : `IInteractable` et son registre (coffres, Mémoriaux, puis Failles passent par la même touche, la même invite et la même jauge ; `ChestInteraction` devient `WorldInteraction`) ; `ChoiceScreen`, l'écran de choix commun, avec la grammaire du level-up extraite dans `ChoiceStyle` ; `StatModifier`, modificateur de stat réversible (bénédictions, Oublis). L'Autel (`AltarManager`, `altars.json`) est retiré.

Vérifié : build sans avertissement, smoke test, `tools/test_weapons.sh` (« arme ravivée Rare au moins »), `tools/test_movement.sh`, capture `CAPTURE_EXTRA_ARGS="--capture-memorial"` regardée (invite, éclats, bénédictions, services avant et après achat, stèle ravivée). Deux défauts vus et corrigés : la colonne du Mémorial traversait le compteur d'éclats, et l'invite des services portait le même nom que le soin.

**3C livré (26 septembre, nuit).** Trois Failles au départ (couronne 0,25–0,9), violettes, au ras du sol. « Sonder » ouvre trois offres : une amélioration Épique ou Légendaire d'une arme ou d'un passif possédés, chacune avec son **Oubli** (malus durable, huit dans `data/progression/oublis.json`) et +1 Péril ; « Refuser » laisse la Faille ouverte. Une offre acceptée referme la Faille. Avec peu de cibles (une arme en début de run), la même arme revient avec d'autres gains et un autre Oubli, pour garder un choix. Quand une zone devient Effacée entre 500 et 1 400 px du joueur, une Faille s'y ouvre avec 8 % de chance, au plus une toutes les 45 s et six ouvertes à la fois. Au Mémorial, « Se rappeler » lève un Oubli (40 Essence, +50 % par usage) ; le Péril reste. La fiche de pause liste les Oublis sous le Péril.

Extraits en route : `FragmentOption.ApplyTo` (appliquer une amélioration), `UpgradeRoller.RollGains` (gains à une rareté donnée), `UpgradeText` (lignes « avant → après »), partagés par le level-up et la Faille.

Vérifié : build sans avertissement, smoke test, `tools/test_weapons.sh`, `tools/test_movement.sh`, capture `--capture-rift` regardée (Faille ouverte et refermée, offre, fiche de pause avec Péril 1 et l'Oubli, levée au Mémorial : Oublis 0 → 1 → 0). Mesure headless `--nomad`, 2 graines × 15 min : 1 et 3 Failles ouvertes dans les zones Effacées, aucune erreur. Relecture `godot-reviewer` : deux défauts corrigés. Un Oubli de PV max pris à 1 PV rendait plus de PV qu'il n'en avait pris une fois levé (lever un Oubli rend la stat, jamais de PV ; contrôle ajouté à `tools/test_weapons.sh`). Une Faille pouvait s'ouvrir sur une autre (écart minimal appliqué aussi aux ouvertures).

**Écarts et questions pour la recette :**
- **Pause** : Mémorial et Faille ouvrent un écran de trois cartes qui fige la run, comme le level-up. La V2 (§11) voulait un Autel sans pause ; un écran à lire (gains, Oublis) sous les coups semblait injouable. À trancher.
- **Faille** : activée par la touche d'interaction (maintien 0,8 s), pas au simple contact, pour éviter de l'ouvrir en courant.
- **Lever un Oubli** est un service payant du Mémorial, pas une bénédiction ; « relancer la bénédiction » (4.5) n'est pas fait.
- Oublis limités aux stats du joueur : « +1 élite en vie » et « tes zones s'effacent plus vite » (exemples de 4.5) demandent des crochets dans l'apparition et l'Effacement, à ajouter si la direction plaît.
- Toutes les valeurs (coûts, prix des services, taux d'ouverture, poids du Péril) sont des points de départ.

#### Vague 3 détaillée — 26 septembre (soir)

La vague 2 attend deux validations de Raphaël (noms, un par un ; style des icônes v2) ; la vague 4 dépend du plan 13, non arbitré. La vague 3 a sa direction validée : elle passe devant. Choix provisoires :

| Lot | Contenu | Choix |
|---|---|---|
| **3A — Chance et Péril** | Péril, stat de run : chaque point renforce les créatures (nombre, vigueur) et majore score, XP et rareté des tirages. L'Appel du Vide (perk et bouton de pause) et les malédictions (`CursedItemManager`) sont retirés ; la Chance pèse sur tous les tirages de rareté (level-up, Mémorial, Faille) | Valeurs en `data/scaling/peril.json` ; Péril et Oublis dans la fiche de la pause |
| **Écran de choix commun** | Un écran de trois cartes réutilisable (titre, rareté, lignes, prix), au clavier, à la manette et à la souris : bénédictions, services du Mémorial, offres de la Faille | Même grammaire visuelle que le level-up |
| **3B — Mémorial** | Remplace l'Autel. Interagir libère trois éclats à moins d'un écran ; les ramasser en 20 s l'active (sinon ils reviennent). Récompense : trois bénédictions à rareté (monte avec l'oubli de la zone et la Chance) ; la zone est ravivée. Ensuite, contre de l'Essence : raviver l'arme choisie (amélioration Rare au moins) ou se soigner. Un Mémorial englouti par le Néant est perdu | Bénédictions en `data/progression/blessings.json` ; sprite procédural |
| **3C — Faille** | Trace de l'Effacement : quelques-unes au départ, d'autres s'ouvrent dans les zones oubliées. Toucher une Faille propose trois améliorations Épiques ou Légendaires, chacune avec son **Oubli** (malus durable) ; en prendre une ajoute l'Oubli et +1 Péril. Refus toujours possible. Un Mémorial peut lever un Oubli | Oublis en `data/progression/oublis.json` ; sprite procédural |

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

### Réponses de Raphaël — 26 septembre (soir)

| Sujet | Réponse | Application |
|---|---|---|
| Rareté (question 2, §4.3) | « Seulement la rareté sur les améliorations » | Recommandation 4.3 retenue : une arme n'a qu'un niveau ; la rareté vit sur les améliorations, puis sur les objets. Débloque la vague 1 (1A socle, 1B, 1C) |
| Nom d'un perk reçu | « Le mot don me va » | « Don : … » gardé à l'écran de butin |
| Décors urbains jamais placés | « Tu peux les virer » | Quatre décors supprimés, et leurs générateurs |
| Style des icônes, fréquence des coffres | Questions de méthode | Jugés en recette : planche des icônes, et nombre de coffres croisés en jeu (réglage dans `chest_placement.json`) |
| Recette des planches | « Je pense que tu peux faire mieux pour les armes, et les coffres pourraient être un peu plus détaillés / avoir des motifs ; sinon pour le reste c'est parfait. Go » | Coffres v2 : face avant tournée vers la caméra, planches et cerclages cloutés, nervures, fermoirs et étiquette, veines lumineuses et cristaux des deux côtés, frise, colonnettes et médaillon. Icônes v2 : trois-quarts, ligatures et virole de la faucille, cloueuse en pistolet (carter, chargeur de clous, poignée), boîte à coins de laiton, pieds et danseuse. Vague 1 lancée |
