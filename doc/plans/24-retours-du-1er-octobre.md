# Plan 24 — Retours de la partie du 1er octobre : alléger, rendre vivant, tenir debout

1er octobre 2026 · Retours de Raphaël après une partie jouée jusqu'un peu après la première Résurgence ([DECISIONS §39](DECISIONS.md)). Le diagnostic initial et ses propositions sont conservés ci-dessous ; les lots livrés sont consignés au §11. **Nouvelle recette après intégration des sprites : §12, à traiter.** Les chiffres des propositions sont des valeurs de départ. Ce plan ne redéfinit pas le système de build : ce qui touche aux armes, aux objets ou à la montée de niveau se reporte au [plan 21](21-systeme-de-jeu.md) une fois validé.

## 0. Ce que Raphaël a dit, en bref

**Ce qui va :** les armes sont mieux présentées au level-up ; la mécanique du Mémorial (trois éclats en 20 s) est bonne ; l'ouverture de coffre est plus animée que le reste.

**Ce qui ne va pas :**
1. **Écran de chargement** : un écran noir avec du texte, jamais refait.
2. **Trop de choses à l'écran :**
   - les quêtes de run sont toujours affichées, et « ça ne fait pas jeu » ;
   - la grande barre du haut (« Exploration », biome, temps, pourcentage) : garder le temps, pas besoin d'une barre de progression ;
   - les annonces de Résurgence, de horde et de micro-événement sont de trop.
3. **Score** : il monte sans tuer, on ne comprend pas à quoi il est lié. Il manque le nombre de kills.
4. **Trop dur à la première Résurgence :** une seule arme, régénération trop lente, monstres très forts, pas l'occasion de se préparer.
5. **Pas assez de nouvelles armes au level-up**, et des améliorations de l'arme déjà possédée qui reviennent.
6. **Écran du Mémorial** pas assez « juicy » : il apparaît d'un coup, sans animation ; « La zone se souvient » est peut-être à retirer.
7. **Direction artistique des écrans :** le fond qui tourne derrière le coffre n'est pas pixelisé ; tout doit suivre la DA (pixel, « mode oubli »).
8. **Raretés** : des caractères génériques, pas de vraies icônes.
9. **Les coffres ne doivent pas donner d'armes.**
10. **Flèches des coffres** visibles de trop loin, à plusieurs écrans.
11. **Minimap** : des carrés de couleurs qui ne veulent rien dire ; elle montre toute la carte au lieu d'un zoom autour du joueur.
12. **Repères** peu intéressants (+1 % de Chance).
13. **Chance** : on ne voit pas si elle agit.
14. **Stats au level-up** : un tiret pour vitesse, dégâts, zone et durée.
15. **Armes de mêlée** : certaines portent trop court.
16. **Survie :** pas assez d'outils (bouclier temporaire, régénération). Idée : des **bonus lâchés** par les ennemis puissants ou de petits butins (aimant d'XP, soin, etc.).
17. **Objets pas redessinés**, contrairement aux armes.
18. Les objets ne doivent pas dire d'eux-mêmes avec quoi ils font synergie.

**Question posée :** où en est la construction des mécaniques principales (§9).

## 1. Diagnostic

Chaque constat a été vérifié dans le code au commit `13fc505`.

| # | Retour | Ce que fait le jeu aujourd'hui | Cause |
|---|---|---|---|
| 1 | Chargement | `GameLoadingOverlay` : fond noir, particules qui montent, une phrase de lore parmi 8, écrite en dur et non traduite, toutes les 3 s ; étape technique en bas (« Terrain… 42 % ») | Jamais repris depuis la V1 |
| 2a | Quêtes | `QuestManager` : panneau en haut à droite, titre, deux lignes de texte par quête (`[ ]` + nom, puis description, progression, récompense). Taille en pixels écran, hors échelle du HUD. Aucune option pour le masquer | Pensé comme un panneau de debug lisible, jamais habillé |
| 2b | Barre du haut | `HUD.BuildRunProgress` : phase (« EXPLORATION »), biome, temps, et une barre avec pourcentage. **Le pourcentage est l'Effacement global du monde**, pas une exploration : il monte seul avec le temps, et la run bascule en fin de partie à 68 % | Aucune légende : Raphaël l'a lu comme une progression d'exploration, ce qui montre qu'elle ne se comprend pas |
| 2c | Annonces | Résurgence : texte d'alerte (« Résurgence dans 12 s », « Résurgence 1 », « ACCALMIE — ESSENCE ×2 ») et phase « RÉSURGENCE » dans la barre. Micro-événements (`RunEventHud`) : bandeau titre + objectif + deux jauges, bilan « Réussi / Échoué » 3,5 s, toast « Vaincu ». « La Harde » est le micro-événement `stampede` | Choix d'origine : tout expliciter |
| 3 | Score | `ScoreManager` : **3 points par seconde survécue**, + 10 par élimination (bonus en zone qui s'efface), + 175 par Résurgence, + 50 par lieu, + coffres selon rareté, le tout × personnage × mutateurs × Péril. Les éliminations sont comptées (`TotalKills`) mais affichées seulement au bilan | Le temps est le terme qui fait monter le score à l'arrêt |
| 4 | Difficulté | Première Résurgence à **4:00**. À ce moment, PV des créatures ×1,73 (×1,43 avant le lot R5 du plan 23, soit +20 %) et dégâts ×1,29. Régénération de base 0,3 à 0,6 PV/s pour 70 à 120 PV. Aucun soin au sol | R5 a été calé sur un temps pour tuer **mesuré avec un bot invincible**, qui ne dit rien des dégâts subis ; et sur un joueur qui a plusieurs armes. Avec une seule arme, le joueur est en dessous de la courbe |
| 5 | Armes au level-up | Une carte « nouveauté » est garantie, mais **armes et objets partagent ce tirage**. Avant le niveau 5 : 4 armes de palier 1 contre 31 objets, donc une nouvelle arme dans **~11 %** des offres. La 3ᵉ carte est souvent prise par l'objet de survie garanti. La carte « amélioration » garantie, avec une seule arme et sans objet, est toujours une amélioration de cette arme. Poids écrits en dur dans `FragmentManager` | Le tirage dilue les armes dans 31 objets |
| 6 | Mémorial | `ChoiceScreen.Open` met `Visible = true` et la pause. Fond : un voile noir à 75 % et le cadre. Ni tween, ni rayons, ni son d'ouverture. Seule la stèle s'illumine 0,6 s en jeu | Écran générique partagé avec les Failles |
| 7 | Fond du coffre | `LightRaysControl` : 16 triangles pleins dessinés en vectoriel (`DrawColoredPolygon`), à bords lisses, tournant à 0,25 rad/s. Même contrôle derrière le level-up | Dessiné à la résolution de l'écran, pas à celle du pixel art |
| 8 | Raretés | `ChoiceStyle.RarityGlyph` : rien (commune), ◆, ◆◆, ★, ★★. **La police Saira ne contient ni ◆ ni ★** : ils passent par la police de secours du système, donc leur rendu change selon l'OS et peut donner des carrés vides | Glyphes Unicode au lieu d'icônes dessinées |
| 9 | Armes dans les coffres | Tables `chest_common/rare/epic` : arme à 20, 22 et 35 % des tirages. **Défaut en plus :** `PickWeapon` appelle `PickRandomWeapon()` sans le joueur, donc un coffre peut tirer une arme déjà portée, qui tombe au sol et ne se ramasse pas | Tables de butin, et un oubli dans `LootRewards.cs:163` |
| 10 | Flèches des coffres | `edge_pointer.range_px = 1200` (`data/chests/chest_placement.json`). Avec le zoom ×2, l'écran couvre 960 × 540 px de monde : un coffre à 1 200 px est à plus de deux écrans en hauteur | Portée réglée avant l'agrandissement de la carte |
| 11 | Minimap | `Minimap` : **toute la carte** sur 180 unités, un pixel par cellule de 128 px. Carrés 3×3 = petits lieux (couleur de chacun : puits et cabines en bleu, cristal en cyan…), 4×4 = coffres (couleur de rareté), 5×5 = Mémoriaux (or) et Failles (violet). Aucune légende | Pensée comme carte globale, pas comme radar |
| 12 | Repères | +0,01 de Chance au premier usage de chacun des 12 types de lieu | Effet trop petit (voir 13) |
| 13 | Chance | 1 point de Chance = 10 crans ; chaque cran donne 30 % de monter la rareté d'un rang. +1 % de Chance = 0,1 cran = **+3 points de chance de monter d'un rang** : imperceptible. Rien n'explique la Chance, rien ne montre quand elle a joué | Valeur faible, et aucun retour visuel |
| 14 | Tirets dans les stats | `PlayerSheet.Bonus` affiche « — » quand un multiplicateur global vaut ×1. **Dégâts** reste presque toujours à « — » : les cartes d'arme montent les dégâts de chaque arme, pas un multiplicateur global, et aucun objet actif ne donne de dégâts globaux. Vitesse, Zone, Durée ne bougent qu'avec un objet précis | Une ligne qui montre un bonus global, lue comme une valeur absente |
| 15 | Portée de mêlée | 8 armes de mêlée sur 12 portent à 60 ou moins : Scalpel 35, Parapluie 45, Gants de boxe 45, Râteau 50, Gomme 50, Parcmètre 55 (**~46** chez la Forgeuse, dont la portée personnelle vaut ×0,83), Trousseau 55, Faucille 60. L'Arc porte à 150 | Valeurs d'origine, jamais revues après le passage à la carte agrandie et aux foules denses |
| 16 | Outils de survie | Soins : objets (Bobine, Dé à coudre, Boîte de pansements), Scalpel, une bénédiction, puits, voiture, pique-nique, Mémorial payant, deux micro-événements. **Aucun ramassable de soin, aucun aimant ramassable, aucun bonus temporaire lâché par les élites** (elles donnent XP ×4, +3 Essence, 8 % arme, 15 % coffre). Seul bonus temporaire : l'Abribus (vitesse +20 % pendant 20 s) | Le combat ne rend presque rien d'immédiat |
| 17 | Objets pas redessinés | **Aucun des 31 objets n'a d'icône propre.** Le JSON n'a qu'une couleur ; `PerkIconResolver` choisit une ancienne icône de perk d'après la stat. Plusieurs objets partagent la même (bouclier et armure, XP et aimant, cible ralentie et perforation, immobile et recharge), et les autres retombent sur « dégâts » | Le travail visuel s'est arrêté aux armes |
| 18 | Synergies | Plus aucun texte « synergie avec » ni « Pour : ». **Reste un indice :** au survol d'une carte d'objet au level-up, les armes concernées s'allument dans l'inventaire (`LevelUpScreen.cs:472-484`) | Choix du lot R2 du plan 23 |

## 2. Chantier A — Un écran de jeu allégé

Principe : en combat, l'écran montre **la vie, l'XP, le temps, le score et les kills, la minimap**. Le reste vit dans la pause, ou dans le monde.

### A1. Barre du haut → le temps seul

- **Retirer** la barre et le pourcentage d'Effacement global. L'Effacement se voit déjà dans le monde (sol qui blanchit, overlay) et dans la minimap.
- **Garder** le temps, en grand, au centre en haut. Il change de couleur pendant une Résurgence (violet) : c'est la seule trace de phase.
- **Biome :** plus de nom permanent. Quand le joueur entre dans un nouveau biome, son nom apparaît 2 s sous le temps, en fondu, puis disparaît.
- Le seuil de fin de partie (68 %) ne change pas : il est seulement caché.

### A2. Quêtes de run → une forme de jeu, repliée par défaut

- **Par défaut :** trois petits **sceaux** en pixel art (16 × 16) sous la minimap, un par quête. Chaque sceau a un anneau qui se remplit avec la progression. Pas de texte.
- **Quand une quête avance :** le sceau pulse une fois. **Quand elle est remplie :** il se brise en éclats dorés et un toast court (« Quête : Sans s'arrêter ✓ ») passe 2 s.
- **Le détail** (nom, condition, progression, récompense) est dans la pause, et en maintenant une touche (`show_quests`, Tab par défaut, à déclarer dans `[input]`).
- **Option** dans les paramètres : Quêtes **repliées** (défaut) / **détaillées** (l'ancien panneau, habillé) / **masquées**.
- Le panneau passe à l'échelle du HUD (il est aujourd'hui en pixels écran).

### A3. Annonces → dans le monde, pas en texte

| Annonce | Aujourd'hui | Proposition |
|---|---|---|
| « Résurgence dans 12 s » | Texte d'alerte | Retiré. Le `CrisisOmen` (bords désaturés) commence 20 s avant, avec un son grave qui monte. Le temps passe au violet au début |
| « Résurgence 1 » | Texte | Retiré |
| « ACCALMIE — ESSENCE ×2 » | Texte | Retiré. Le compteur d'Essence du HUD brille et affiche « ×2 » pendant l'accalmie |
| Micro-événement | Bandeau titre + objectif + 2 jauges, bilan 3,5 s | Bandeau retiré. L'objectif se lit **dans le monde** : cercle au sol qui se remplit (Veille), cible marquée et flèche au bord de l'écran (chasse au Souverain, relique tombée). Réussite : son + éclats ; échec : la cible se dissout. Le toast « Vaincu » est retiré |

**Risque :** la Veille et la relique tombée demandent de comprendre l'objectif. La première fois de chaque type dans le profil, une ligne courte peut apparaître 3 s (« Tiens la lumière »). Ensuite, plus rien. **À trancher par Raphaël** (Q3).

### A4. Score et kills

**Proposition : le score mesure ce qu'on fait, pas le temps qui passe.** Remplacer les 3 points par seconde par des **points de distance** : chaque nouvelle cellule de carte atteinte (au-delà du plus loin qu'on soit allé) rapporte des points. C'est la question du jeu, « jusqu'où », et le score cesse de monter quand on tourne en rond.

- Le reste ne change pas : éliminations, Résurgences, lieux, coffres, multiplicateurs.
- Le barème se règle dans `data/scaling/score.json` pour que le score d'une run de 15 min reste du même ordre (même seed, même outil, avant/après).
- **Kills :** un compteur avec une petite tête de mort en pixel art, sous le score, en haut à droite.
- **Explication :** dans la pause, une ligne par source (« Éliminations 4 210 · Distance 3 100 · Lieux 650… »). Le bilan le fait déjà en partie.

Le score est du ressort du [plan 02](02-juiciness-score.md). Si Raphaël préfère garder le temps, l'alternative est de le garder et de l'expliquer dans la pause (Q4).

### A5. Flèches des coffres

- `range_px` : 1 200 → **600** (le coffre est au plus à un demi-écran au-delà du bord). Au plus **2** flèches au lieu de 3.
- Les flèches des lieux révélés par une cabine téléphonique n'ont aucune limite de distance : c'est voulu (la cabine sert à ça), on les garde.

### A6. Minimap → un radar

- **Vue zoomée** autour du joueur, qui le suit : environ 2 400 px de monde de côté (2,5 écrans), au lieu des 12 800 px de la carte.
- **Carte entière** dans la pause, ou en maintenant une touche (`show_map`, M), avec une **légende**.
- **Des formes, pas des carrés de couleur** : petits pictogrammes 5 × 5 en pixel art — coffre, stèle du Mémorial, faille, point pour les petits lieux. Les petits lieux utilisés disparaissent au lieu de devenir gris.
- Moins de catégories : les petits lieux n'apparaissent qu'une fois vus à l'écran (aujourd'hui, dès que le brouillard est levé).

## 3. Chantier B — Des écrans qui ont l'air de Vestiges

### B1. Un fond animé commun, en vrai pixel art

Un seul composant de fond pour les écrans de choix (level-up, coffre, Mémorial, Faille), dessiné **à la résolution du pixel art** puis agrandi sans lissage :
- rendu dans un `SubViewport` au quart de l'écran (480 × 270), filtre *nearest* ;
- palette limitée à celle de la [charte](../CHARTE-GRAPHIQUE.md), tramage (*dithering*) pour les dégradés ;
- motifs : rayons **en marches d'escalier** qui tournent lentement, poussière d'oubli qui monte, bords qui s'effilochent comme une zone effacée ;
- une teinte par écran : or pour le level-up et le coffre, cyan pâle pour le Mémorial, violet pour la Faille.

Cela remplace `LightRaysControl` (triangles vectoriels lisses). Coût à vérifier au banc : un `SubViewport` de 480 × 270 pendant un écran en pause doit être négligeable.

### B2. Le Mémorial ravivé, mis en scène

Séquence d'environ 1,2 s, passable d'un appui :
1. Le dernier éclat ramassé : arrêt sur image 0,1 s, l'écran se **pixelise** puis se désature vers les bords (l'oubli recule).
2. La stèle envoie une colonne de lumière ; les trois éclats tournent autour et s'y fondent.
3. Le fond B1 (cyan) monte en fondu ; le titre « Mémorial ravivé » s'écrit lettre à lettre.
4. Les trois cartes **tombent une à une** (0,08 s d'écart), avec un son par carte et un éclat à l'atterrissage. Une carte rare ou mieux a un liseré qui scintille.

- **« La zone se souvient » est retiré.** Le titre suffit.
- Même traitement, plus court, pour l'écran de la Faille.

### B3. Coffre

Garder la roulette, qui plaît. Remplacer le fond par B1 (or). Les cases qui défilent passent au pixel art (icônes au lieu de textes « Essence ×6 », « ? »).

### B4. Icônes de rareté

Cinq icônes dessinées, en pixel art 12 × 12, qui remplacent ◆ et ★ partout (level-up, Mémorial, Faille, coffre) :
- **un éclat de verre** qui gagne une facette par rang : commune (éclat terne, une facette), inhabituelle (vert, deux), rare (bleu, trois), épique (violet, quatre), légendaire (or, cinq, avec un reflet animé) ;
- même famille que les « fragments teintés » des Réminiscences, déjà jugés meilleurs ([DECISIONS §30](DECISIONS.md)).

Corrige aussi le défaut de police : ◆, ★ et « ᵉ » ne sont pas dans Saira.

### B5. Écran de chargement

**Proposition : une scène plutôt qu'un texte.** Le personnage choisi marche sur une bande de sol en pixel art ; devant lui, le sol se dessine au rythme du chargement ; derrière, il s'efface. C'est la barre de progression, sans barre.
- Une phrase de lore au-dessus, réécrite selon le [plan 19](19-lore.md) (les textes actuels sont jugés trop directs, [tableau de bord §3](TABLEAU-DE-BORD.md)) et traduite.
- Les étapes techniques (« Terrain… 42 % ») disparaissent de l'écran ; elles restent dans le log.
- Raccord avec la `VoidTransition` du Hub : le noir de fin de transition devient le noir du fond.

## 4. Chantier C — Tenir jusqu'à la première Résurgence

Ordre voulu : **corriger l'accès aux armes d'abord, mesurer, puis seulement retoucher les créatures.** Le lot R5 a monté les PV parce que le joueur était plus fort ; avec une seule arme, il ne l'est pas.

### C1. Plus de nouvelles armes au level-up

- **Tirage séparé** : tant que le joueur a moins de 3 armes, chaque offre contient **une carte « nouvelle arme »** garantie (si une arme est disponible), en plus de la nouveauté et de l'amélioration. Au-delà de 3 armes, une chance sur deux.
- Ensuite, le partage actuel reprend.
- Les **poids de tirage** passent du code à `data/progression/level_up_offer.json` (règle « data-driven » d'`AGENTS.md`).
- Effet attendu : 2 armes vers le niveau 3, 3 armes vers le niveau 6, au lieu d'une seule à la première Résurgence. À mesurer avec `tools/measure_run.sh` (niveau du joueur et nombre d'armes à 4:00).

### C2. Portée des armes de mêlée

Plancher de portée de base à **65**, valeurs de départ :

| Arme | Avant | Après |
|---|---|---|
| Scalpel | 35 | 55 (estoc : reste court, c'est son jeu) |
| Parapluie | 45 | 65 |
| Gants de boxe | 45 | 60 |
| Râteau | 50 | 70 |
| Gomme | 50 | 65 |
| Parcmètre | 55 | 70 |
| Trousseau | 55 | 65 |
| Faucille | 60 | 70 |

Et la portée personnelle de la Forgeuse ne réduit plus la mêlée (×0,83 aujourd'hui) : elle ne s'appliquerait qu'aux armes à distance, ou passerait à ×1. À vérifier contre le rayon de contact des créatures et au banc `tools/test_weapons.sh`.

### C3. Régénération et première Résurgence

- **Régénération de base ×2** : Traqueur 0,6, Vagabond 1,0, Forgeuse 1,2 PV/s. Le Vagabond garde son +0,2.
- **Bobine de fil** : +0,4 → +0,6 PV/s par niveau.
- **Première Résurgence** : PV des créatures ramenés à leur niveau d'avant R5 jusqu'à la fin de la première Résurgence (multiplicateur fixe 1,25 → 1,0 avant 5:10), puis la courbe actuelle. À décider **après** la mesure de C1 et C4.
- **Outil :** ajouter à `measure_run.sh` les **PV perdus par minute** (le bot est invincible, mais les dégâts qu'il aurait subis se comptent). Sans cette mesure, on ne sait pas chiffrer « on se sent faible ».

### C4. Bonus lâchés : l'idée de Raphaël

Des **ramassables temporaires**, à effet immédiat, en objets du quotidien comme le reste du jeu :

| Bonus | Effet | Forme |
|---|---|---|
| Gourde | Soin 20 % des PV max | Gourde cabossée |
| Aimant à boussole | Toute l'XP de l'écran vient au joueur | Fer à cheval aimanté |
| Couverture de survie | Bouclier temporaire 30 % des PV max, 15 s | Couverture dorée pliée |
| Café froid | Cadence +30 %, 10 s | Gobelet |
| Pétard | Onde qui repousse et blesse autour | Pétard rouge |

**Sources :**
- élites : 35 % de lâcher un bonus ;
- Souverain : un bonus garanti, plus son coffre ;
- fin d'une Résurgence survécue : deux bonus, dont un soin ;
- éliminations ordinaires : 0,4 % (soin et aimant surtout).

**Règles :** au plus 4 bonus au sol en même temps ; ils clignotent puis s'effacent au bout de 20 s ; ils suivent le butin qui disparaît avec sa zone ([plan 16](16-oubli-sensible.md)). Tables et durées dans `data/loot_tables/field_bonuses.json`. Sprites 16 × 16 par le pipeline `tools/sprites`. Pool `NodePool<T>` comme les orbes d'XP.

C'est un nouveau système de butin : le [plan 13](13-butin.md) n'a jamais été arbitré et il est absorbé par le plan 22. **À valider par Raphaël avant codage** (Q1).

## 5. Chantier D — Lisibilité du build

### D1. Stats sans tirets

- **Valeurs, pas multiplicateurs**, là où elles ont un sens : Vitesse 200, PV 100, Régénération 0,5 /s, Armure 0, Bouclier 0, Portée ×1,00.
- Les **bonus globaux** (Dégâts, Zone, Durée, Cadence) n'apparaissent que s'ils valent autre chose que 0, en « +12 % ».
- Les **dégâts par arme** sont dans l'inventaire : au survol d'une arme, ses stats du moment (dégâts, cadence, projectiles).
- Même fiche pour la pause et le level-up (`PlayerSheet` est déjà commune).

### D2. Voir la Chance agir

- Quand la Chance (ou l'oubli de la zone, ou le Péril) a **monté la rareté** d'une carte, la carte l'affiche : son icône de rareté (B4) arrive d'abord au rang tiré, puis **saute** au rang supérieur avec un trèfle qui s'allume et un son. Le joueur voit le bonus se produire.
- Une ligne dans la pause, au survol de « Chance » : « Chaque point de Chance fait monter la rareté des améliorations, du butin et des bénédictions. »

### D3. Des Repères qui comptent

Aujourd'hui +1 % de Chance par type : imperceptible. **Proposition : un Repère donne un petit gain permanent lié au lieu**, de la même taille qu'une carte commune d'objet, et le dit en une ligne :

| Lieu | Gain |
|---|---|
| Puits | PV max +10 |
| Veine de cristal | Chance +5 % |
| Épouvantail | Armure +2 |
| Boîte aux lettres | XP +5 % |
| Wagonnet | Aimant +15 % |
| Voiture | Bouclier +5 |
| Cabine téléphonique | Portée +5 % |
| Abribus | Vitesse +4 % |
| Pique-nique | Régénération +0,3 PV/s |
| Coffre | Chance +5 % |
| Mémorial | 1 relance |
| Faille | 1 bannissement gratuit |

Douze types, douze petits gains : explorer tout le monde rapporte un vrai build de départ. Données dans `data/world/waymarks.json` (le code les applique déjà par `ApplyPerkModifier`). Alternative minimale : Chance +5 % par type au lieu de +1 % (Q5).

### D4. Synergies

- Aucun texte de synergie ne reste dans les objets. **Proposition :** retirer aussi la surbrillance des armes concernées au survol d'une carte d'objet, puisque Raphaël ne veut pas que le jeu explicite les synergies (Q6).
- Nettoyer le commentaire périmé de `scripts/Combat/WeaponProperties.cs:5` (cartes « Pour : … »).

### D5. Coffres sans armes

- Retirer la ligne `weapon` de `chest_common`, `chest_rare` et `chest_epic`. Son poids passe aux **niveaux d'objet** (moitié) et à l'**Essence** (moitié). Le bonus de stat reste.
- Corriger au passage `LootRewards.PickWeapon`, qui ne passe pas le joueur à `PickRandomWeapon` : il reste utilisé par les lieux (cache, sanctuaire, fouille) et le Wagonnet.
- **Question :** les élites (8 %) et le Souverain (25 %) lâchent aussi des armes au sol. Les garder ? (Q7) Recommandation : oui pour le Souverain, retirer pour les élites, remplacées par les bonus C4.

## 6. Chantier E — Les objets ont droit au même soin que les armes

- **31 icônes d'objet**, 32 × 32, au format des icônes d'armes validées, par le pipeline `tools/sprites` (un générateur `tools/generate_item_icons.py`, objets du quotidien abîmés : ressort, papier carbone, rondelle, mètre pliant…).
- **Planche de proposition d'abord** (`doc/plans/planches/`), validée par Raphaël, puis intégration : champ `icon` dans `passive_souvenirs.json`, `PerkIconResolver` retiré.
- **Les trois objets « monde »** (Presse-papier, Calendrier, Médaillon) reçoivent leur icône avec la planche, même s'ils attendent le Reliquaire.
- Dans le même passage : les **icônes des Réminiscences** (fragments teintés par famille, motifs à valider, [tableau de bord §2](TABLEAU-DE-BORD.md)).

Si « redessinés » voulait dire aussi **revoir le contenu** (effets, noms, liste), le catalogue est au [plan 21 §4](21-systeme-de-jeu.md) et attend toujours sa relecture (Q8).

## 7. Lots proposés

Un lot à la fois, chacun clos par `/close-lot`. Les lots marqués « libre » ne posent pas de question de design et peuvent partir dès l'accord sur l'ordre.

| Lot | Contenu | Statut | Vérification |
|---|---|---|---|
| **L1** Corrections nettes | D5 (coffres sans armes, `PickWeapon`), A5 (flèches 600 px, 2 au plus), D1 (stats sans tirets), « La zone se souvient » retiré, D4 nettoyage du commentaire | Libre | `measure_run.sh` (coffres), capture du level-up |
| **L2** Écran allégé | A1 (temps seul, nom de biome furtif), A3 (annonces retirées), A4 (compteur de kills ; score de distance si Q4 = oui) | Libre sauf Q3, Q4 | Captures avant/après en Résurgence et en micro-événement |
| **L3** Armes et survie | C1 (offre d'armes), C2 (portées de mêlée), C3 (régénération ; mesure des PV perdus), puis réglage de la première Résurgence d'après la mesure | Libre | `measure_run.sh` avant/après, même seed : armes et niveau à 4:00, PV perdus par minute ; `test_weapons.sh` |
| **L4** Quêtes repliées | A2 (sceaux, toast, touche, option) | Libre | Capture, test de la touche |
| **L5** Raretés et fond | B4 (icônes de rareté, planche), B1 (fond pixel commun) | Planche à valider | Planche, puis captures des 4 écrans ; `/bench` du `SubViewport` |
| **L6** Mémorial et coffre | B2, B3, D2 (la Chance qui saute) | Après L5 | Capture du Mémorial en run (`--capture-memorial`) |
| **L7** Minimap radar | A6 | Libre | Capture |
| **L8** Bonus lâchés | C4 | **Q1** | `measure_run.sh` (bonus vus, PV perdus), captures |
| **L9** Repères | D3 | **Q5** | Capture, mesure de la Chance en fin de run |
| **L10** Icônes d'objets | E | Planche à valider | Planche, captures du level-up et de la pause |
| **L11** Chargement | B5 | Maquette à valider | Capture du chargement |

**Ordre recommandé :** L1 → L3 → L2 → L5 → L6 → L4 → L7, avec les planches de L5 et L10 préparées tôt pour que Raphaël les voie pendant ce temps. L3 passe avant L2 parce que la difficulté empêche de juger le reste.

## 8. Questions à Raphaël

| # | Question | Recommandation |
|---|---|---|
| Q1 | Bonus lâchés (C4) : liste, sources et taux | Les cinq bonus, élites à 35 % |
| Q2 | Ordre des lots | Celui du §7 |
| Q3 | Micro-événements sans texte : une ligne d'aide la première fois de chaque type ? | Oui, une fois par profil |
| Q4 | Score : remplacer les points de temps par des points de distance ? | Oui, c'est « jusqu'où » |
| Q5 | Repères : gain lié au lieu (tableau D3) ou Chance +5 % ? | Gain lié au lieu |
| Q6 | Retirer la surbrillance des armes concernées au survol d'un objet ? | Oui |
| Q7 | Armes lâchées par les élites et le Souverain : garder ? | Souverain oui, élites non |
| Q8 | « Objets pas redessinés » : les icônes seulement, ou aussi le contenu (effets, noms) ? | Icônes d'abord ; contenu à relire au plan 21 §4 |
| Q9 | Quêtes : sceaux repliés par défaut, ou masquées par défaut ? | Repliées |

## 9. Où en sont les mécaniques principales (1er octobre)

**En résumé :** la **structure** du système de jeu est en place et jouable (4 armes, 6 objets, 4 Réminiscences, cartes à raretés). Le **contenu** est inégal : les armes sont complètes jusqu'au niveau 50 mais seules 4 sur 24 ont leur ascension ; les objets fonctionnent tous mais n'ont ni icônes ni déblocages ; la moitié des Réminiscences reste à faire.

| Système | Fait | Reste | Bloqué par |
|---|---|---|---|
| **Armes** : 24, 4 emplacements, niveau 1 à 50, stats tirées au hasard avec rareté, stats entières fractionnaires | Tout, sprites et icônes validés | Ascensions : **4 armes sur 24** (Arc, Faucille, Cloche, Boîte à musique) ; portées de mêlée (C2) ; équilibrage mesuré avec un joueur qui encaisse | Voies des 20 autres armes, proposées au [21-historique §28](21-historique.md), **à valider** |
| **Objets** : 6 emplacements, niveau 1 à 30, palier au niveau 15 | **31 objets** en jeu (15 de propriété, 16 de déclencheur), effets et paliers codés | **Icônes : 0 sur 31** ; 3 objets « monde » ; accès D/Q/V (tout est ouvert aujourd'hui) ; chiffres à régler en jeu | Reliquaire (plan 22 C3) pour les objets « monde » ; relecture du catalogue (plan 21 §4) |
| **Réminiscences** : 4 emplacements, après chaque Résurgence | **7 sur 14** | 7 à coder ; icônes | Délestage et Habitude attendaient les objets : débloqués |
| **Montée de niveau** : cartes à la Megabonk, relances, bannissements payés en Péril | En jeu | Offre d'armes (C1) ; surplus de fin de progression ; réserve de niveaux | — |
| **Personnages** | Vagabond, Traqueur, Forgeuse jouables, signatures | Affinités (G4) ; 3 personnages à intégrer ; mobilités propres | Plans 01 E, 06, 08 |
| **Butin** | Coffres (Essence, XP, niveaux d'objet, bonus de stat), Mémoriaux, Failles, 9 petits lieux | Coffres sans armes (D5) ; bonus lâchés (C4) ; Reliquaire ; Atelier ; Atlas | Plan 22 §11 : Atelier, Trempe, Atlas à confirmer |
| **Déblocages** | — | Par quête, par Vestiges, par l'Atlas | Plans 21 §10, 22 |

Ce qui manque pour dire « le système de jeu est fini » :
1. les voies des 20 autres armes (validation puis codage, le plus long) ;
2. le Reliquaire et les 3 objets « monde » ;
3. les 7 Réminiscences restantes ;
4. les icônes d'objets et de Réminiscences ;
5. les déblocages ;
6. l'équilibrage de bout en bout, une fois tout cela en place.

## 10. Réponses de Raphaël — 1er octobre ([DECISIONS §40](DECISIONS.md))

Toutes les recommandations du §8 et toutes les propositions sont validées, avec ces changements :

- **Score (Q4, A4) :** ni temps, ni distance. Le score compte **les éliminations seulement** ; les points de lieux, de coffres et de Résurgences disparaissent aussi.
- **Repères (Q5) :** laissés à l'agent. Gain lié au lieu (D3).
- **Armes des ennemis (Q7) :** supprimées pour les élites **et** le Souverain.
- **Ascensions :** les 20 autres armes reçoivent leurs voies (proposition du [21-historique §28](21-historique.md)), en plus de ce plan. Lot **L12**.
- **Barre d'XP :** sur toute la largeur de l'écran, en bas, beaucoup plus jolie. Ajoutée au lot L2.
- **Sprites et design :** confiés au [plan 25](25-sprites-et-design.md). Ce plan code les écrans avec des replis dessinés ; les images du plan 25 s'y branchent ensuite.

**Ordre de travail retenu :** L1 → L3 → L2 (avec la barre d'XP et le score aux éliminations) → L12 → L4 → L7 → L9 → L8 → L5/L6 (code) → L11.

## 11. Comptes rendus

### L1 — corrections nettes (1er octobre)

- **Plus d'armes hors du level-up et du Wagonnet :**
  - la ligne « arme » quitte les tables des coffres (commun, rare, épique) et des lieux (`poi_*`) ; son poids passe moitié aux niveaux d'objet, moitié à l'Essence ;
  - les créatures ne lâchent plus d'armes : `TryDropWeapon` (1 % des ordinaires, élites, Souverain, mini-boss) et `weapon_drop_chance` retirés ;
  - le Wagonnet ne pose une arme que si un emplacement est libre, sinon il donne de l'Essence : même raison (DECISIONS §40) ;
  - la branche « arme » de `LootRewards` est retirée, ce qui fait disparaître le défaut de l'arme déjà portée tirée par un coffre.
- **Flèches des coffres :** 700 px et 2 au plus (au lieu de 1 200 px et 3). 700 plutôt que 600 : l'écran couvre 480 px de part et d'autre du centre en largeur, à 600 la flèche n'aurait presque jamais servi sur les côtés.
- **Stats :** Vitesse et Portée en valeur (« 240 », « 300 (+18 %) ») ; Dégâts, Cadence, Zone, Durée et Aimant n'apparaissent que s'ils ont un bonus. Plus de tiret.
- **Mémorial :** sous-titre « Choisis une bénédiction. », sans « La zone se souvient ».
- **Synergies :** la surbrillance des armes concernées au survol d'un objet est retirée (Q6), avec `UpgradeText.ConcernedWeapons`. `WeaponProperties` reste pour les affinités ; son commentaire « Pour : … » est corrigé.

**Vérifications :** `dotnet build` sans avertissement ; `test_weapons`, `test_small_places` et `test_objects` à zéro échec ; smoke vert. `test_objects` doit tourner avec `LANG=fr_FR.UTF-8` : dans un conteneur en anglais, deux contrôles de texte de carte échouent déjà sur `main` (« and » au lieu de « et »).

### L3 — armes et survie (1er octobre)

- **Offre de level-up :** tant que le joueur porte moins de 3 armes, chaque offre contient une carte « nouvelle arme » (si une arme est disponible) ; au-delà, une chance sur deux tant qu'un emplacement est libre. Elle compte comme la nouveauté garantie. Les poids de tirage, les paliers d'armes par niveau et la chance d'un palier de plus passent du code à `data/progression/level_up_offer.json` (`LevelUpOfferConfig`).
- **Portées de mêlée :** Scalpel 35 → 55, Parapluie 45 → 65, Gants de boxe 45 → 60, Râteau 50 → 70, Gomme 50 → 65, Parcmètre 55 → 70, Trousseau 55 → 65, Faucille 60 → 70. La portée propre au personnage allonge la mêlée mais ne la raccourcit plus (Forgeuse ×0,83 auparavant).
- **Régénération :** de base ×2 (Traqueur 0,6, Vagabond 1,0, Forgeuse 1,2 PV/s) ; Bobine de fil +0,4 → +0,6 PV/s par niveau ; bonus de coffre et bénédiction suivent (+0,6 et +0,3).
- **Première Résurgence :** PV des créatures **inchangés**. La mesure ci-dessous montre que l'accès aux armes suffit ; à revoir en jeu.
- **Outil :** `measure_run.sh` donne désormais les dégâts reçus par minute avant 4:00 et pendant la première Résurgence, et le nombre d'armes et le niveau à 4:00.

**Mesure** (`measure_run.sh`, `--nomad`, 330 s, seeds 221092026, 1002, 7 ; avant = `870092a` avec le même outil). Le bot prend toujours la première carte : il prend donc toutes les armes offertes, ce qui majore le nombre d'armes par rapport à un joueur.

| Seed | Armes à 4:00 | Niveau à 4:00 | Éliminations | Dégâts reçus /min avant 4:00 | Pendant la 1ʳᵉ Résurgence |
|---|---|---|---|---|---|
| 221092026 | 3 → 4 | 6 → 10 | 298 → 347 | 420 → 873 | 931 → 481 |
| 1002 | 2 → 4 | 6 → 10 | 130 → 368 | 867 → 600 | 1 146 → 719 |
| 7 | 1 → 4 | 3 → 11 | 69 → 389 | 4 922 → 770 | 4 963 → 590 |

Les dégâts reçus sont bruts (le bot est invincible, la régénération n'y est pas déduite). Pendant la première Résurgence, ils baissent de 37 à 88 %.

**Relecture de L1 (sous-agent) traitée ici :** test du Wagonnet à quatre armes portées (de l'Essence, aucune arme au sol) ; commentaire de `WeaponPickup` ; roadmap V2 annotée (armes des coffres retirées), parcours du premier coffre et perk « Pilleur » de la Stratégie V2 corrigés ; la ligne Portée de la fiche montre le bonus de portée du personnage (la portée de chaque arme est dans sa fiche). `WeaponProperties` reste sans appelant en jeu : il servira aux affinités des personnages (plan 21 G4).

### L2 — écran allégé, score aux éliminations, barre d'XP (1er octobre)

- **Haut au centre :** plus de plaque, de phase écrite, de barre ni de pourcentage d'Effacement. Le temps seul, plus grand ; il passe au violet pendant une Résurgence, à l'orange en fin de partie, à l'or au-delà. Le nom du biome apparaît sous le temps quand on y entre, deux secondes, puis s'efface.
- **Annonces :** plus de « Résurgence dans 12 s », « Résurgence 1 », « ACCALMIE — ESSENCE ×2 ». Le présage (bords désaturés) et la couleur du temps suffisent ; pendant l'accalmie, le compteur d'Essence affiche « ×2 » et pulse. Micro-événements : plus de bandeau, de bilan ni de toast « Vaincu » ; le repère au sol (colonne, anneau qui se remplit) et la flèche de bord d'écran restent. La première fois qu'un profil croise un type d'événement, son objectif s'écrit 3,5 s sous le temps (`seen_hints` dans la sauvegarde méta). La réussite se fait entendre.
- **Score :** les éliminations seulement, chacune selon la créature (`kill_points`, bonus en zone qui s'oublie), × personnage × mutateurs × Péril. Plus de points de temps, de Résurgences, de lieux, de coffres, de boss ni d'endgame ; le score n'est plus notifié à la seconde, seulement à une élimination ou à un changement de Péril. Le bilan affiche « N éliminations ». Les champs de détail du score disparaissent de l'historique (les anciennes runs se relisent, les clés en trop sont ignorées).
- **Éliminations :** une tête de mort en pixel art et le compte, sous le score ; l'Essence à droite.
- **Barre d'XP :** sur toute la largeur, au bas de l'écran (`XpBar`), dessinée en unités du HUD (deux pixels à 1080p) : liseré et embouts dorés, remplissage biseauté en trois tons dont le bas est tramé, crans aux dixièmes et repères aux quarts, reflet qui la parcourt, tête lumineuse et étincelles. Une orbe la fait briller, un niveau l'éclaire en blanc avant de repartir de zéro. La plaque de vie perd sa petite barre d'XP ; armes et objets remontent d'autant.

**Vérifications :** `dotnet build` sans avertissement ; `test_weapons`, `test_objects`, `test_small_places`, `test_perk_acquisition`, `test_perk_effects`, `test_perk_contracts`, `test_movement` (score immobile sans élimination, une élimination rapporte), `test_enemy_abilities`, `test_dev_mode` à zéro échec ; smoke vert. Captures en vraie run (1080p, 20 et 40 s) regardées : temps seul, plaque score-éliminations-Essence, barre d'XP lisible et tramée.

**Reste du chantier A :** quêtes repliées (L4) et minimap radar (L7).

**Relecture de L3 et L2 (sous-agent) traitée :** le record passe dans un nouveau fichier (`highscore_kills.save`), un ancien record fait avec les points de temps ne serait plus jamais battu ; ligne Portée de la fiche réduite au bonus gagné en run (la portée propre au personnage s'affichait comme un bonus) ; `seen_hints` normalisé au chargement ; tween de couleur du temps tué avant d'être relancé ; nom de biome qui ne clignote plus en franchissant deux frontières ; journal du level-up qui ne retire plus le palier ; le test du Wagonnet à quatre armes, sauté sans bruit faute de second wagonnet, rejoue maintenant la pose directement.

### L12 — ascensions des 20 autres armes (1er octobre)

Validées par la demande de Raphaël (DECISIONS §40), d'après la proposition du [21-historique §28](21-historique.md). Les 24 armes ont leurs deux voies.

| Arme | Voie A | Voie B |
|---|---|---|
| Parcmètre | Séisme : cercle complet, cadence × 0,7 | Contravention : coup droit, dégâts × 1,8, recul × 2 |
| Lance-billes | Grêle : billes × 2, dégâts × 0,7 | Bille d'acier : une bille, perforation 5, dégâts × 2,5 |
| Parapluie | Rafale : cadence × 1,6, portée × 0,8 | Ouvert : arc de 120°, recul × 3 |
| Cloueuse | Agrafeuse : salve de 3 (20°), dégâts × 0,5 | Clou de charpente : dégâts × 2, perforation illimitée, cadence × 0,7 |
| Pelle à neige | Congère : ralentit 2 s (×0,5) | Déblayer : cercle complet, recul × 2 |
| Rallonge | Court-circuit : désoriente 0,8 s | Enrouleur : portée × 1,5, cadence × 0,8 |
| Assiettes | Service complet : assiettes × 2 | Vaisselle cassée : perforation 3 |
| Râteau | Herse : Saignement de 8 s au lieu de 4 | Ratisser : cercle complet |
| Scalpel | Suture : soin tous les 3 coups au lieu de 5 | Incision : fait saigner 3 s |
| Lentille de phare | Balayage : trois rayons (30°), dégâts × 0,6 | Foyer : dégâts × 2, cadence × 0,6 |
| Trousseau | Passe-partout : sauts × 2 | Clé unique : un saut, dégâts × 2,2 |
| Boussole | Rose des vents : aiguilles × 3, dégâts × 0,5 | Nord : guidage maximal, perforation 3 |
| Polaroïd | Rafale de flashs : cadence × 1,5 | Surexposition : désorientation de 3 s au lieu de 1,5 |
| Baguette de sourcier | Fourche : tirs × 2 | Source : dégâts × 1,8 |
| Gomme | Mie de pain : cercle complet | Encre : dégâts × 1,6, portée × 1,3 |
| Lampe à pétrole | Nappe : feu au sol de 4 s, rayon 45 (2 s, 30) | Mèche courte : tirs × 2, feux de rayon 20 |
| Gants de boxe | Enchaînement : deux échos | Crochet : dégâts × 1,7, recul × 2 |
| Craies | Marelle : formes de rayon 80 (50) | Dessin appliqué : formes × 2 |
| Transistor | Grandes ondes : cône de 25° à 100° (15° à 60°) | Fréquence pirate : le cône désoriente 0,6 s |
| Chronomètre | Arrêt sur image : le champ fige 0,5 s au lieu de ralentir | Compte à rebours : cadence × 1,5 |

**Levier ajouté :** `special_overrides`, réglages de l'effet spécial de l'arme remplacés par la voie (Suture, Nappe, Mèche courte, Enchaînement, Marelle, Arrêt sur image). L'effet est copié une fois au choix de la voie, l'arme de base reste intacte. L'écho des Gants lit `echo_count`, le champ du Chronomètre `freeze_seconds` ; la cible frappée subit le champ même si elle manque au cache des ennemis de la frame.

**Vérifications :** `test_weapons` à zéro échec, dont trois contrôles nouveaux : les 24 armes ont deux voies dans un motif que leur famille sait jouer ; Suture soigne tous les 3 coups et Enchaînement fait deux échos sans toucher l'arme de base ; Arrêt sur image fige. `test_small_places`, `test_movement`, `test_enemy_abilities` à zéro échec. Galerie en vraie run (`--capture-weapons --ascensions`) regardée pour Séisme, Agrafeuse, Balayage et Ouvert : les coups touchent tout autour, en salve, en éventail et en arc.

**Reste :** comme la Moisson de la Faucille, une voie en cercle (Séisme, Déblayer, Ratisser, Mie de pain) dessine encore l'effet de son arme d'origine ; l'effet visuel circulaire est à reprendre avec le plan 25. Chiffres de départ, à régler en jeu.

### L9 — Repères (1er octobre)

Chaque type de lieu donne, à son premier usage dans la run, le gain du tableau D3, de la taille d'une carte commune d'objet : puits PV max +10, veine de cristal Chance +5 %, épouvantail armure +2, boîte aux lettres XP +5 %, wagonnet aimant +15 %, voiture bouclier +5, cabine portée +5 %, abribus vitesse +4 %, pique-nique régénération +0,3 PV/s, coffre Chance +5 %, Mémorial une relance, Faille un bannissement gratuit. Le message flottant dit le gain (« Repère : Puits · PV max +10 »). Gains en données (`waymarks.json`) ; relances et bannissements passent par un signal (`ChoiceTokensGranted`), sans lien direct entre les Repères et le level-up.

**Vérifications :** `test_small_places` à zéro échec avec un contrôle des douze gains ; trois contrôles anciens adaptés (le puits et l'abribus donnent maintenant aussi leur Repère, le bouclier de la voiture encaisse un coup entier). `test_objects`, `test_perk_acquisition` à zéro échec ; smoke vert.

### L4 — quêtes repliées (1er octobre)

- **Par défaut, des sceaux :** un sceau de cire en pixel art par quête, sous la plaque du score (`RunQuestSeals`). Un anneau de huit crans se remplit avec la progression ; chaque cran allumé fait pulser le sceau ; une quête remplie le brise en éclats dorés, il reste doré et coché, et « Quête accomplie : … » passe 2,6 s dessous. Les sceaux sont dessinés en code en attendant ceux du plan 25 (S6).
- **Le détail** (nom, condition, progression, récompense) : en maintenant **Tab** (manette : LB), action `show_quests` déclarée et remappable. L'ancien panneau, resserré, passe à l'échelle du HUD.
- **Réglage** Paramètres › Graphismes › « Quêtes de run » : Sceaux (défaut), Détaillées (le panneau en permanence), Masquées.
- Une action `show_map` (M, manette : RB) est déclarée pour la carte entière du lot L7.
- Les sceaux suivent les quêtes par un signal (`RunQuestUpdated`), sans lien direct entre le HUD et le gestionnaire de quêtes.

**Vérifications :** build sans avertissement, smoke vert, `test_objects` et `test_dev_mode` à zéro échec. Capture en vraie run à 60 s regardée : trois sceaux colorés sous le score, anneaux en partie allumés.

**Correctif :** `test_dev_mode` cherchait le record sous son ancien nom de fichier (`highscore.save`), renommé au lot L12 ; le banc et son script suivent le nouveau nom.

### L7 — minimap radar (1er octobre)

- **Radar :** 104 unités de côté, 26 cellules de 128 px (environ trois écrans), quatre unités par cellule ; la fenêtre suit le joueur, calée sur la grille d'une unité pour que les cellules glissent pixel par pixel. Le joueur au centre.
- **Carte entière** en maintenant **M** (manette : RB), centrée à l'écran, un nombre entier d'unités par cellule, avec une **légende** à droite (toi, coffre à la couleur de sa rareté, Mémorial, Faille, petit lieu à visiter).
- **Pictogrammes en pixels** au lieu des carrés : coffre, stèle, faille, point ; contour sombre d'un pixel. Un petit lieu servi disparaît de la carte au lieu de passer au gris.
- Banc : `--hold-map` maintient la touche pendant une capture.

**Vérifications :** build sans avertissement, smoke vert ; captures en vraie run regardées : radar à 60 s (coffre, Mémorial, lieux, bord du brouillard) et carte entière à 90 s avec sa légende.

**Non fait :** n'afficher un petit lieu qu'une fois vu à l'écran (aujourd'hui : dès que le brouillard est levé autour) ; le retrait des lieux servis suffit à désencombrer.

### L8 — bonus lâchés (1er octobre)

- **Cinq bonus** (`data/world/field_bonuses.json`) : gourde (soin de 20 % des PV max), fer à cheval aimanté (aimant d'XP ×25 pendant 3 s : toute l'XP de l'écran arrive), couverture de survie (bouclier de 30 % des PV max pendant 15 s), café froid (cadence +30 % pendant 10 s), pétard (onde de 160 px qui ôte 60 % de leurs PV aux créatures, le quart aux élites et Souverains, et les repousse).
- **Sources :** élite 35 %, Souverain toujours, deux bonus près du joueur à la fin d'une Résurgence survécue (le premier est une gourde), 0,4 % sur toute autre élimination (gourde ou aimant).
- **Règles :** au plus 4 au sol ; 20 s de vie, clignotement les 4 dernières ; ramassé à 26 px. Le nom du bonus s'affiche au ramassage, avec un son et des éclats. Pool de nœuds ; aucun parcours de décor.
- **Dessin :** chaque bonus est généré en pixels depuis un motif (deux pixels par trait, contour sombre), au-dessus d'une lueur au sol de sa couleur ; il flotte. À remplacer par les sprites du plan 25 (S5).
- Le butin qui disparaît avec sa zone (plan 16) ne s'applique pas encore aux bonus : leur durée de 20 s suffit.

**Vérifications :** nouveau banc `tools/test_field_bonuses.sh` (6 contrôles, zéro échec) : données, limite au sol, soin au contact, effets à durée appliqués puis retirés, pétard qui touche la créature proche et pas la lointaine, effacement en fin de vie. Smoke vert. Capture en vraie run avec `--show-bonuses` regardée : gourde, aimant et café lisibles sur la route, couverture ramassée par le bot.

### L5 et L6, partie code — fond pixel, Mémorial mis en scène, Chance visible (1er octobre)

- **Fond commun en vrai pixel art (B1) :** `assets/shaders/choice_backdrop.gdshader` et `PixelBackdrop`, derrière le level-up, l'écran du coffre, le Mémorial et la Faille. Dessiné en gros pixels sur la grille de l'écran (4 px à 1080p), trois tons tramés (Bayer 4 × 4) : rayons en bandes qui tournent, halo, poussière d'oubli en pixels, bords qui s'effilochent en violet. Une teinte par écran (or, cyan pour le Mémorial, violet pour la Faille) ; il monte en fondu à l'ouverture. Remplace `LightRaysControl` (triangles vectoriels à bords lisses), supprimé.
- **Mémorial et Faille mis en scène (B2) :** éclair bref, le fond monte, le titre s'écrit lettre à lettre, puis les cartes tombent une à une avec un petit son chacune (environ 1 s). Un appui pendant l'entrée la termine sans choisir de carte. Les services du Mémorial, rouverts après chaque achat, ne rejouent pas l'entrée. « La zone se souvient » était déjà retiré (L1).
- **Coffre (B3) :** même fond, en or ; la roulette est gardée. Ses cases textuelles attendent les icônes du plan 25.
- **Chance visible (D2) :** quand la Chance, l'oubli de la zone ou le Péril montent la rareté d'une carte, elle montre d'abord la rareté tirée, puis saute au rang gagné avec un trèfle en pixels et un son ; les cartes d'une offre sautent l'une après l'autre. Dans la pause, une ligne sous « Chance » dit ce qu'elle fait.

**Vérifications :** build sans avertissement, smoke vert, `test_objects`, `test_perk_acquisition`, `test_weapons` à zéro échec. Captures en vraie run regardées : bénédictions du Mémorial (fond cyan tramé, poussière, liseré violet allégé après une première capture trop chargée), level-up épique (fond or). Le saut de rareté n'a pas été capturé : il demande de la Chance au moment de l'offre.

**Reste :** la mise en scène dans le monde avant l'écran (l'écran qui se pixelise, les éclats qui tournent autour de la stèle) ; les icônes de rareté, qui restent des glyphes absents de la police jusqu'au lot S1 du plan 25.

### L11 — écran de chargement (1er octobre)

- Le personnage choisi marche (ses vraies animations, ×3) sur une bande de sol en gros pixels (`LoadingWalkStrip`) : devant lui, le sol se dessine au rythme du chargement, sa dernière colonne scintille ; derrière, il s'efface en pixels violets. C'est la barre de progression, sans barre.
- La progression vient des étapes réelles (`GameBootstrap`, `WorldSetup`) : chaque étape a sa part, et un « N % » dans son texte la place à l'intérieur. Les textes techniques ne s'affichent plus ; ils vont au journal (`[Chargement] …`).
- Huit phrases réécrites, moins directes, traduites (`LOADING_LINE_1` à `8`) : « Quelqu'un a laissé la lumière allumée. », « Personne n'a fermé la barrière. »… À relire avec le plan 19.
- Au fondu, la marche va au bout, puis la phrase et la bande s'effacent. Le noir de départ reste raccord avec la transition du Hub.
- Banc : `tools/tests/LoadingCapture.tscn` joue l'overlay seul avec les vraies étapes et le photographie trois fois.

**Vérifications :** build sans avertissement, smoke vert, capture regardée (bande à mi-chemin, personnage qui marche, lisière effacée, phrase au-dessus).

**Relecture des lots L4 à L11 (sous-agent) traitée :** shader du fond préchauffé au chargement (pas d'à-coup à la première ouverture) ; la fin d'une couverture de survie rabat le bouclier au nouveau maximum sans entamer le bouclier de base ; la carte entière et le détail des quêtes, tenus au moment d'une pause, se replient au lieu de rester figés sous l'écran de choix ; une relance des bénédictions ne rejoue plus l'entrée ; restes de commentaires retirés. Bancs `test_field_bonuses`, `test_objects`, `test_small_places`, `test_movement` à zéro échec ; smoke vert.

### L6b — Mémorial et Faille mis en scène dans le monde (livré le 1er octobre, soir)

Reste de B2, validé en [DECISIONS §40](DECISIONS.md). Découpage, un seul lot :

1. **Couche commune `LandmarkReveal`** (sous le HUD, au-dessus du monde et du voile d'Effacement) : le monde se fige
   (pause, sons du monde assourdis), l'image se pixelise par paliers de pixels du monde puis revient, les bords se
   désaturent par une trame autour du lieu, puis la trame recule jusqu'aux bords (l'oubli recule) avec un front à la
   couleur du lieu. Temps réel ; un appui (validation, clic, bouton de manette) passe toute la séquence et ouvre l'écran
   sans son entrée. Caméra amenée sur le lieu pendant la séquence, rendue au joueur à la fermeture de l'écran.
2. **Mémorial (≈ 0,8 s) :** les éclats ramassés réapparaissent autour de la stèle, tournent en spirale, montent et se
   fondent dans une colonne de lumière élargie ; à la fusion, la stèle passe à l'état ravivé avec son éclair, gerbe
   d'étincelles. Puis l'écran des bénédictions (entrée existante).
3. **Faille (≈ 0,45 s), à chaque ouverture de son offre :** même voile, front violet, sa colonne s'élargit et palpite.
4. **Vérification :** captures de la séquence (`--capture-memorial`, `--capture-rift`) regardées sur ViewSonic ; build sans
   avertissement, smoke vert ; coût nul hors séquence (couche invisible, aucun nœud par frame).

Son : un seul repère existant à la fusion (`sfx_souvenir_trouve`), provisoire, à juger à l'écoute avec le plan 15.

**Compte rendu.** `LandmarkReveal` (`scripts/UI/`, shader `landmark_reveal.gdshader`) est créé une fois par run, invisible
hors séquence. Mémorial : 0,8 s, pixelisation à 2 puis 3 pixels du monde, trame grise autour d'un cercle de 72 px gardé
en couleur, éclats en spirale (1,6 tour) fondus dans une colonne élargie, stèle ravivée à la fusion, puis le cercle
s'ouvre jusqu'aux coins derrière un liseré cyan clairsemé. Faille : 0,45 s, colonne violette et fente qui bat par poses.
Les directions ne passent pas la séquence (le joueur marchait en ramassant le dernier éclat). L'invite d'interaction
est masquée avant toute activation immédiate. Shader ajouté au préchauffage.

**Vérifications :** build sans avertissement, smoke vert, `test_movement` à zéro échec, `test_choice_screen`
31 contrôles à zéro échec, dont 7 nouveaux sur le réveil (pause, direction ignorée, avancement, une seule fin,
caméra sur le lieu puis rendue au joueur, passage d'un appui). Ce banc compte désormais en secondes, pas en images :
le headless en enchaîne des milliers par seconde. `test_shader_warmup` valide. Captures `--capture-memorial` et
`--capture-rift` sur ViewSonic, regardées : pixelisation, éclats en orbite, cercle qui s'ouvre, enchaînement sur
l'écran, stèle ravivée et caméra revenue. Une première passe avait un front trop épais et trop vif ; il est
maintenant un liseré.

**Reste :** ressenti en partie réelle (durée totale avec l'entrée de l'écran, environ 2 s, passable dès le premier
appui) et choix du son à la fusion, à l'écoute.

## 12. Retours de recette — 1er octobre 2026

Source : [DECISIONS §44](DECISIONS.md), après intégration du plan 25 et correction du fond des coffres. **Reprise autorisée en DECISIONS §45 ; état des lots ci-dessous.** Le travail sur les sprites est globalement apprécié ; les nouveaux retours portent aussi sur des systèmes antérieurs.

### Retours mis au propre

| Sujet | Retour et résultat recherché | Plan responsable |
|---|---|---|
| Personnages | Le Traqueur déplaît particulièrement ; le Vagabond et la Forgeuse méritent également de meilleurs sprites. Reprendre les trois personnages actuellement jouables. | [08](08-direction-artistique.md), casting de référence au [06](06-personnages-quetes-defis.md) |
| Projectiles ennemis | Trop de tirs traversent l'écran et obligent à esquiver continuellement, au détriment du combat contre la foule. Revoir la place de ces menaces en conservant les esquives intéressantes déjà présentes. | [07](07-bestiaire-et-rencontres.md) |
| Pause | Retirer les descriptions des armes dans l'équipement, « Le monde se fige, mais ta mémoire reste éveillée. », « HALTE DANS LE VIDE » et l'aide sous Chance. Améliorer aussi la composition et le style du menu. | [04](04-interfaces-et-hub.md) |
| Boutons | L'effet de survol par superposition de lignes et de contours ne plaît pas. Préparer un état plus simple, avec sélection clavier/manette toujours lisible. | 04, [25](25-sprites-et-design.md) |
| Coffres | Essence, dégâts critiques, PV supplémentaires et autres gains doivent être reconnaissables par leur propre icône. Le badge de rareté ne suffit pas à identifier la récompense. | 04, 25 |
| Résurgences | On ne perçoit pas assez clairement qu'une Résurgence est en cours. Renforcer les signes visuels et surtout explorer la musique et les effets sonores. | [15](15-audio.md), coordination 08 et 24 A3 |
| Audio général | Musiques et effets sonores demandent une vraie passe d'ensemble : c'est le prochain gros chantier souhaité. | 15 |
| Carte | Rendu jugé trop pixellisé, trop gris et peu soigné. Examiner la minimap et la carte agrandie ; identifier si le retour vise aussi le terrain avant d'étendre le chantier au [plan 10](10-terrain-et-tiles.md). | 04, 24 A6/L7, appui 08 |

### Lots proposés pour la reprise

**Découpage initial confirmé en DECISIONS §45, puis continuation hors audio en §47.** Un lot terminé et vérifié avant le suivant ; les comptes rendus ci-dessous distinguent implémentation et recette humaine.

| Lot | Action bornée | Vérification attendue |
|---|---|---|
| **R1 — Pause et boutons** | Supprimer les quatre familles de textes citées, conserver les chiffres utiles à l'équipement, puis soigner hiérarchie, espaces et survol/focus. | Captures avant/après de la pause et des boutons concernés ; navigation clavier/manette. |
| **R2 — Icônes du butin** | Inventorier les types de gains réellement proposés et leur associer des icônes de ressource/statistique. Garder une indication de rareté distincte. Réutiliser les sprites adaptés avant d'en produire. | Coffre montrant plusieurs gains différents ; icônes lisibles à leur taille réelle. |
| **R3 — Pression des tirs** | Identifier les ennemis et compositions qui accumulent les tirs à longue portée. Mesurer portée, durée de vie, cadence et simultanéité, puis essayer un ajustement ciblé. | Comparaisons sur mêmes seeds, durée et build ; densité ennemie, tirs simultanés et dégâts, complétées par une partie jouée. Ne pas conclure au plaisir sur un bot invincible. |
| **R4 — Audio et Résurgences** | Auditer à l'oreille une run, puis traiter un cycle complet : annonce sensorielle, début, phase active, retour au calme. En faire le premier lot du chantier audio détaillé au plan 15. | Séquence enregistrée avec son réel ; événement perceptible sans lire une annonce, combat encore lisible. |
| **R5 — Trois personnages** | Améliorer d'abord le Traqueur, puis appliquer le niveau de qualité retenu au Vagabond et à la Forgeuse. Pipeline procédural existant et graines fixes. | Planches comparatives à taille réelle sur plusieurs sols, puis captures en mouvement ; silhouettes et directions lisibles. |
| **R6 — Carte** | Comparer radar et carte agrandie, puis travailler finesse du dessin, palette et hiérarchie des repères. Le caractère gris et grossier doit disparaître sans sacrifier la lecture. | Captures à taille d'usage, repères et zones explorées distinguables. Si le terrain est concerné, borner un lot séparé au plan 10. |

**Contraintes conservées :** pas de suppression générale de l'esquive ; les gains de statistiques des coffres ne sont pas les Réminiscences qualitatives ; ne pas réintroduire automatiquement les bandeaux d'annonce retirés ; garder le fond des coffres en rotation fluide (plan 25 I7). Aucun réglage de cadence, vitesse ou portée n'est validé par avance. Les choix audio déjà retenus restent la base de l'écoute, pas une liste à remplacer en bloc.

### Prompt court de continuation

> Travaille seul sur Vestiges. Lis AGENTS.md, CLAUDE.md, DECISIONS §44/47 et le plan 24 §12, puis les plans concernés. R1/R2 (pause, survols, butin), R3b (cadence du Hurleur), R5a–c (trois personnages) et R6 (carte) sont implémentés et vérifiés. L’audio est hors du périmètre de cette continuation ; son suivi indépendant reste au plan 15. Les suites ouvertes sont la recette humaine du combat et du visuel, le banc de coût de la carte sur machine calme et un éventuel retour visant le terrain du monde. Ne pas les présenter comme déjà validés. Préserver la rotation des coffres à 8°/s, capturer sur ViewSonic uniquement et actualiser les plans après chaque lot.


### R1 — pause et survols livrés

Pause recomposée, textes demandés retirés, états de boutons simplifiés et
navigation clavier/manette vérifiée par événements dans une vraie Main.
Compte rendu et captures au [plan 04, R1](04-interfaces-et-hub.md#r1--lot-engagé-le-1er-octobre).
Mappings A/B de validation/retour ajoutés après constat de leur absence ; build
sans avertissement et smoke verts. La rotation des coffres n'est pas modifiée.


### R2 — icônes de butin livrées

Essence, XP, niveaux d'objet, Souvenirs et treize statistiques identifiés par
leurs icônes ; éclat de rareté séparé. Ouverture réelle et galerie complète
capturées et regardées à 100/130 %, build sans avertissement, smoke et 169
contrôles UI verts. [Détail au plan 04](04-interfaces-et-hub.md#r2--lot-engagé--icônes-de-nature-du-butin).

### R3 — diagnostic puis correction ciblée, ressenti encore ouvert

Quatre tireurs identifiés ; le Présage attaque au sol. Mesures et essai isolé de
4 → 2 s sur quatre runs de 320 s : les Hurleurs dominent les tirs des Résurgences,
mais les variations de composition empêchent de valider la réduction de durée.
[Rapport, protocole et chiffres](../audits/projectile-pressure-2026-10-01/README.md).
R3b livré ensuite : recharge du tir du Hurleur ×2,5, cri conservé. Banc fixe
à 6 Hurleurs/2 Cracheurs : projectiles actifs moyens −38,7 % (agressivité 1)
et −34,9 % (1,6), 60 cris dans chaque cas ; 61 régressions, quatre runs et
captures vérifiés. [Rapport R3b](../audits/projectile-cadence-2026-10-01/README.md).
La recette de plaisir et de difficulté en partie humaine reste ouverte.

### R4 — préparation audio livrée ; R5/R6 maintenus

Le [plan 15](15-audio.md#r4--préparation-livrée-écoute-à-entreprendre) donne le
diagnostic des transitions, le protocole d'écoute de 360 s et les lots A0–A3.
Priorités techniques repérées : annonce musicale écrasée par le rafraîchissement
adaptatif, compteur d'ennemis non corrigé par les retraits lointains, absence
d'accalmie musicale explicite. Ces constats de code restent à écouter en contexte.
Aucun son ni gain changé ; **A0, l'enregistrement et l'écoute réels, est la
prochaine action**, uniquement sur ViewSonic pour la fenêtre.

**Mise à jour du soir :** A0 enregistré (mixage du moteur), A1 livré (pilotage de
la musique par intention, annonce tenue, accalmie) ; avant/après à écouter par
Raphaël. [Plan 15](15-audio.md#a0-et-a1--livrés-le-1er-octobre-soir).

R5 (Traqueur → Vagabond → Forgeuse) et R6 (radar/carte agrandie) ont depuis
été livrés dans la continuation hors audio (§47), avec leurs vérifications
aux plans 08 et 04. Tous ces lots préservent la rotation du coffre à 8°/s.

### Continuation hors audio et R6 livré — 1er octobre 2026

DECISIONS §47 exclut l’audio de cette continuation autonome ; le prompt
ci-dessus est actualisé pour tenir compte des lots livrés. R3b est livré,
puis R6 : terrain de la carte détaillé par biome, routes/eau, danger et légende.
17 contrôles, build/smoke, captures Main sur ViewSonic avec texte 130 % vérifiés
([plan 04](04-interfaces-et-hub.md#r6-livré--1er-octobre-2026)). R5a–c sont également livrés (ci-dessous). La rotation
des coffres reste à 8°/s ; correction séparée du préchargement de son shader.

### R5a–c livrés et état courant hors audio — 1er octobre 2026

Traqueur (col, poignets, volumes), Vagabond (écharpe, sac), Forgeuse (lunettes,
gants, tablier) repris dans le pipeline procédural. **456 PNG déterministes,
120 animations**, cadres/pivots conservés et aucun débordement. Planches sur
forêt/ruines/carrière, puis marche/dash/hurt/mort dans Main inspectés sur
ViewSonic ; build sans warning et smoke verts. [Rapport R5](../audits/characters-2026-10-01/README.md).

R1/R2, R3b, R5 et R6 sont livrés techniquement. Restent la recette humaine du
ressenti de combat et du visuel, la mesure FPS de la carte sur machine calme,
et la clarification du retour « carte » s’il visait aussi le terrain rendu.
Le préchargement du shader actuel a aussi été réparé (plan 25) ; la rotation
fluide à 8°/s est conservée. L’audio n’a pas été traité dans ces lots.

**Intégration vérifiée :** travail audio indépendant conservé ; conflits de
commentaires/numérotation résolus, continuation hors audio numérotée §47.
Build 0 warning, 17 contrôles carte et 61 capacités, smoke 600 frames et Main
headless 20 s verts. [Journal d’intégration](../audits/integration-hors-audio-2026-10-01/README.md).
