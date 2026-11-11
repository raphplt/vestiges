# Plan 58 — Les terres complètes

Étape 5 de [LE-JEU.md](../LE-JEU.md), le gros chantier voulu par Raphaël ([§201](DECISIONS.md)), passé avant les gardiens G3 à G5 le 27 octobre ([§205](DECISIONS.md)).

## 1. Ce que Raphaël a vu

Après la visite des cinq terres (`tools/play_lands.sh`) : « en effet pour les cartes ça fait très vide / linéaire pour la plupart, même s'il y a des bonnes bases ». Puis : « au global ça manque de variations / POI, ou props vraiment particulières (ça se remarquait pas trop quand les biomes étaient mixés mais là ça ressort plus) ». Et déjà au [§189](DECISIONS.md) : « la forêt fait très vide, la ville fait hyper répétitive ».

Il avait tranché la forme au §189 : des lieux de Vaulme, **3 à 5 pièces faites main par terre et des chemins procéduraux entre elles** (« on se souvient des lieux, pas des chemins »), et une génération qui gagne de la structure : clairières et fourrés, places, repères visibles de loin, alternance d'espaces ouverts et serrés.

## 2. L'état des lieux

Vue d'ensemble de chaque terre à son arrivée (graine 7, zoom 0,25) : [planches/plan58_terres_avant.png](planches/plan58_terres_avant.png).

| Terre | Ce qu'on voit | Pourquoi |
|---|---|---|
| **Les bois de la colline** | Un semis uniforme d'arbres, sans clairière ni fourré, sans rien à trouver | Aucun placeur ni lieu composé : semis de Perlin seul. Les décors rares (lampadaire, mur) n'acceptent que le béton, qui pèse 5 % |
| **Les fermes du plateau** | La terre la plus riche, mais la même ferme copiée dans chaque région | Un seul plan de ferme pour toute la terre, retourné une fois sur deux ; une ferme par région |
| **La Haute-Ville** | Une grille d'îlots identiques, sans place ni repère | Rues droites tous les 14 rangs, sans ondulation ni effondrement ; une rangée d'immeubles par îlot ; les places restent vides ; au plus deux églises de la taille d'un immeuble |
| **Le Bas-Port** | Presque vide : de l'herbe humide et des flaques. Ni quai, ni bateau, ni maison | C'est le biome du marais, sans rien du port promis par le lore (quais, bateaux couchés, marais salants, cale, digue) ; aucun petit lieu |
| **La carrière des Aubiers** | Un bruit uniforme de cailloux et de matériel épars | Tout le matériel des chantiers est aussi semé au hasard ; ni paroi, ni gouffre, ni palier ; un seul plan de chantier |

Ce qui est commun à toutes, et qu'on retrouve partout pareil :
- **Les Mémoriaux sont posés au hasard** dans une couronne, sur un sol quelconque, sans rien autour. Le repaire du gardien est toujours au même endroit, à 160 px du départ, sur un sol quelconque.
- **Le semis générique** couvre toute la carte à densité presque constante.
- **Les décors de jonction** entre biomes ne se posent plus, puisqu'une terre n'a qu'un biome.
- **Des systèmes entiers sont éteints** : les onze éléments de lore (balançoire fantôme, porte sans mur, miroir d'eau…), les anciens POI et les micro-événements.

Il n'y a presque pas de décor orphelin : les 197 décors dessinés sont tous posés quelque part, sauf un portail de ferme. **Il ne manque pas des décors à brancher : il manque des lieux.** Le seul format de lieu composé, `SitePlan` (ferme, chantier), existe en un exemplaire par biome.

## 3. La forme

Une terre, c'est **un cœur, trois à cinq pièces, et entre elles un terrain qui a du relief**.

1. **Les pièces maîtresses.** Des lieux faits main, de 15 à 40 cellules de côté, chacune unique sur sa terre : le belvédère des bois, le marché du samedi, le parvis de Saint-Aubin, les quais du Bas-Port, le front de taille de la carrière. Chacune a :
   - un plan JSON : les décors à leur place, des groupes où tirer une variante, et des éléments facultatifs, pour qu'elle ne soit jamais tout à fait la même ;
   - un sol à elle (pavés, clairière, quai) ;
   - **une place de Mémorial** : les Mémoriaux se posent dans les pièces d'abord. On ravive le Mémorial du belvédère, pas « un Mémorial ».
2. **La pièce du cœur** porte le repaire du gardien : la clairière où dort le Cerf, le champ de l'Épouvantail, le parvis du Tocsin.
3. **Entre les pièces**, les chemins procéduraux d'aujourd'hui, et un terrain rythmé par biome :
   - clairières et fourrés dans les bois ;
   - parcelles et haies dans les champs ;
   - avenues, ruelles et places dans la ville ;
   - chenaux et îlots au port ;
   - paliers et gouffres à la carrière.

   Le semis générique perd son uniformité.
4. **Des trouvailles garanties** : chaque terre pose une poignée de décors remarquables uniques (ce que les champs font déjà avec leurs cinq grands décors). Les petits lieux (fouille, embuscade) couvrent toutes les terres.

Les pièces se dessinent par le pipeline procédural des décors (`tools/generate_props.py`) ; l'ami artiste les reprend ensuite, comme les gardiens.

## 4. Les pièces, terre par terre

Proposition de l'agent, réversible. Chaque lot de terre la précise avant de coder. La grammaire est livrée (T0, §7).

| Terre | Cœur (repaire) | Pièces | Terrain entre elles |
|---|---|---|---|
| **Les bois de la colline** (« où l'on montait le dimanche ») | La clairière du Cerf : cercle de souches, pierres moussues | Le belvédère (table d'orientation, bancs, rambarde au bord du vide) ; l'aire de pique-nique du dimanche (tables, balançoire rouillée, barbecue) ; la maison engloutie (lierre, voiture devenue jardinière) ; le viaduc effondré (piles, tronçon d'autoroute, voitures) | Clairières et fourrés serrés, sous-bois |
| **Les fermes du plateau** (« le marché du samedi ») | Le champ de l'Épouvantail : sillons, corbeaux, cabane | Le marché du samedi (étals, bâches, cagettes) ; le moulin ; le hameau (lavoir, croix de chemin) ; la ferme brûlée. Trois plans de ferme au lieu d'un, moins nombreuses | Parcelles et haies, comme aujourd'hui |
| **La Haute-Ville** (« le clocher visible de loin ») | Le parvis de Saint-Aubin : l'église en grand et son clocher, repère de toute la terre | La place de la mairie (fontaine, kiosque) ; l'école (cour, préau) ; la poste ; l'arrêt de la ligne 7 | Rues qui ondulent, avenues et ruelles, îlots de tailles variées, rues effondrées |
| **Le Bas-Port** (« des quais qui ne bordent plus rien ») | La cale du chantier naval : une coque sur sa cale | Les quais (pierre, bateaux couchés, bittes d'amarrage, grue) ; les marais salants (bassins) ; la digue ; les maisons du port | Chenaux, îlots de roseaux, passerelles |
| **La carrière des Aubiers** (« d'où venait la pierre ») | Le front de taille : paroi en gradins | La grande machine figée ; les vestiaires (casiers, photo, thermos) ; la descente (tunnel vers le noir) ; le gouffre (un trou de vide dans la terre). Plusieurs plans de chantier | Paliers et gouffres. Le matériel ne se sème plus au hasard |

## 5. Les lots

| Lot | Contenu | Effort |
|---|---|---|
| **T0** | **La grammaire des pièces.** Le format `SitePlan` devient celui des pièces : plusieurs plans par terre, chacun unique, avec des groupes de variantes, des éléments facultatifs, un sol à lui, une place de Mémorial et, pour la pièce du cœur, la place du repaire. La terre pose son cœur puis ses pièces, écartées et réparties dans la bande des Mémoriaux, reliées aux chemins. Les Mémoriaux et le repaire s'y logent ; les Mémoriaux en trop restent posés comme aujourd'hui. Les décors remarquables garantis passent des champs à toutes les terres (liste par biome en JSON). Démonstration avec des pièces faites de décors existants, une par terre. Mesure : vues d'ensemble avant/après, pièces vues au bot | Moyen à élevé |
| **T1** | **Les bois de la colline.** Clairières et fourrés (le terrain rythmé, réglé en JSON, réutilisé ensuite), la clairière du Cerf, quatre pièces, leurs décors neufs | Élevé |
| **T2** | **Le Bas-Port.** La terre la plus vide et la plus loin du lore : chenaux, la cale, les quais, les marais salants, la digue, les maisons ; ses petits lieux | Élevé |
| **T3** | **La Haute-Ville.** Une grille qui ondule et casse, des places, le parvis de Saint-Aubin et son clocher, la mairie, l'école, la poste, la ligne 7 | Élevé |
| **T4** | **La carrière des Aubiers.** Paliers et gouffres, le front de taille, la machine, les vestiaires, la descente ; plusieurs chantiers | Élevé |
| **T5** | **Les fermes du plateau.** Le champ de l'Épouvantail, le marché, le moulin, le hameau, la ferme brûlée, trois plans de ferme | Moyen |

Ensuite, les gardiens G3 à G5 du [plan 57](57-chemin-et-gardiens.md) (Tocsin, Pilleur, Carrier) prennent leur pièce du cœur.

**Tranché par défaut, réversible :**
- **L'ordre** : la grammaire d'abord, puis les bois, qui n'ont aucune structure. Ensuite le Bas-Port, le plus vide, puis la ville, la carrière, et les champs en dernier : ce sont déjà les plus riches.
- **Les Mémoriaux dans les pièces** : un objectif devient un lieu. Les règles du Mémorial ne changent pas (cercle, 20 s, vagues).
- **Les systèmes éteints** (éléments de lore, anciens POI) restent éteints. Une pièce peut reprendre une de leurs idées en décor (la balançoire, la porte sans mur), sans leur interaction.
- **Corps procéduraux**, repris ensuite par l'ami artiste.
- **Une terre se juge par sa vue d'ensemble et par ce qu'on y trouve en marchant** : les captures de chaque pièce et les pièces vues au bot. La densité de créatures ne doit pas bouger (`measure_run`, avant/après).

## 6. L'avocat du diable

- **Des pièces faites main se répètent d'une run à l'autre.** Quatre terres par run, pour quatre à six pièces par terre. On revoit le belvédère à chaque run qui passe par les bois : c'est voulu, on se souvient des lieux. Les variantes internes et le tirage des pièces (toutes ne sortent pas à chaque fois) évitent la copie exacte.
- **Le coût en sprites est élevé**, vingt à trente décors neufs par terre. Le pipeline procédural les tient, mais chaque lot de terre est un gros lot. C'est le prix du « vraiment particulier » que demande Raphaël.
- **Des pièces trop grandes bouchent la terre** quand le front l'avale. Elles restent dans la bande des Mémoriaux, laissent passer (pas de mur continu) et ne bloquent jamais le cœur.

## 7. T0, la grammaire des pièces — livré le 27 octobre

**Ce qui est fait :**
- **`data/world/land_pieces.json`** : par biome, une pièce du cœur et des pièces uniques au format des lieux composés (`SitePlan`). Chaque pièce a :
  - `id` ;
  - `memorial` : la place de son Mémorial, dans l'ellipse de son emprise ;
  - `ground` : un sol imposé dans l'ellipse inscrite dans l'emprise, jamais un rectangle ; l'eau y sèche ;
  - des éléments à `group`, dont un seul est posé par pièce (la barque ici ou là, la voiture, le wagon) ;
  - des éléments à `chance`.
  - Un décor `…_base` prend sa canopée `…_canopy`.
  - Le cœur exige un sol : le repaire ne tombe jamais dans l'eau.
- **La terre pose son cœur autour du repaire, puis trois ou quatre pièces** (`pieces_per_land`) dans la bande des Mémoriaux (`LandPieceComposer`, sur le thread de génération).
  - Les pièces sont écartées de 1 400 px au moins. Parmi les douze premiers emplacements valides, le plus éloigné des pièces déjà posées gagne.
  - Chaque pièce réserve son emprise, est reliée au chemin le plus proche s'il passe à portée, et peut être retournée.
  - Elle se pose avant tout autre lieu ; fermes, chantiers, semis et placeurs l'évitent.
- **Les Mémoriaux se logent d'abord dans les pièces** : leurs places sont réservées avant les coffres. Les Mémoriaux en trop se posent comme avant.
- **Décors remarquables garantis** (`remarkables` dans `data/props/<biome>.json`) :
  - un exemplaire de chacun par terre, à 18 colonnes les uns des autres, glissés dans le semis (`world_gen.json`, `props.remarkable_*`) ;
  - bois : voiture envahie, panneau rouillé, arbre étranglé ;
  - marais : autel englouti, ponton effondré, arbre lié, charrette noyée ;
  - carrière : godet, casier, convoyeur.
- **Démonstration, des décors existants seulement** : refaite par les lots de chaque terre.
  - Bois : la clairière du Cerf, la vieille route, le cercle de pierres.
  - Champs : le champ de l'Épouvantail, le vieux moulin.
  - Marais : l'île du gardien, le ponton des pêcheurs, l'autel noyé.
  - Carrière : le front de taille, la baraque des mineurs, le godet.
  - La Haute-Ville n'a pas de pièce : sa grille remplit la terre, T3 la casse.
- **Outil** : `CAPTURE_EXTRA_ARGS="--capture-pieces --land-order <biome>,…"` cadre chaque pièce de la première terre, sans créatures, puis la terre à 0,25.

**Vérifié :**
- `tools/validate.sh` : smoke, catalogues (797 contrôles, dont treize refus du fichier des pièces et deux des remarquables), terres (cœur au repaire, assez de pièces, chaque place de Mémorial occupée, sur chaque terre traversée), quêtes, capacités ennemies. Build sans avertissement.
- Captures des quatre terres, graine 7 : trois pièces par terre (deux aux champs, qui n'ont qu'une pièce hors du cœur), deux Mémoriaux sur cinq dans les pièces, tous les remarquables posés. La génération des pièces coûte 6 à 8 ms.

**Mesure** (`measure_run`, profil neuf, bot des terres, Traqueur, bois puis marais puis carrière, 900 s, graines 7 et 42, avant et après au même commit de base) :

| | Avant (7 / 42) | Après (7 / 42) |
|---|---|---|
| Créatures visibles, 0-60 s | 13,7 / 28,4 | 23,4 / 22,7 |
| Créatures visibles, 180-300 s | 14,1 / 24,4 | 22,7 / 19,8 |
| Éliminations | 540 / 401 | 487 / 446 |
| Lieux vus | 24 / 20 | 22 / 27 |
| Mémoriaux vus | 9 / 8 | 8 / 9 |
| Gardiens vaincus | 2 / 2 | 2 / 2 |

La densité ne bouge pas au-delà du bruit d'une graine à l'autre, et les gardiens tombent pareil. Les pièces ne se comptent pas encore à part dans la mesure : un lot de terre ajoutera « pièces vues ».

Les pièces, graine 7 : [planches/plan58_t0_pieces.png](planches/plan58_t0_pieces.png) (la clairière du Cerf et la vieille route dans les bois, le champ de l'Épouvantail, l'autel noyé, le front de taille et la baraque des mineurs).

**Ce qui reste faible, à reprendre par terre :** les pièces de démonstration sont maigres. Ce sont des décors épars sur un sol à peine distinct, et on ne les lit bien qu'en y étant. Le « vraiment particulier » viendra des décors neufs de T1 à T5.

## 8. T1, les bois de la colline — découpage proposé le 10 novembre

Proposition de l'agent, réversible ([§207](DECISIONS.md)). Le lot est élevé : il se coupe en quatre, chacun livré, capturé et mesuré avant le suivant.

**Ce qui ne va pas aujourd'hui** (vue d'ensemble, graine 7) : un semis de Perlin à densité presque constante (`PropSpawner`, fréquence 0,08, chance de 0,5 à 1,5 fois la densité), dix-neuf décors dont trois remarquables, et trois pièces de démonstration faites d'arbres, de souches et de pierres. Rien ne se voit de loin, rien ne raconte « où l'on montait le dimanche ».

| Sous-lot | Contenu | Effort |
|---|---|---|
| **T1a** | **Le terrain rythmé.** Une clé `rhythm` dans `data/props/<biome>.json` : un second bruit, basse fréquence, découpe la terre en zones, chacune avec son multiplicateur de densité, ses décors permis et son sol. Aux bois : **clairières** (herbe, fleurs et fougères, presque rien qui bloque), **fourrés** (buissons et jeunes arbres serrés, avec des trouées), **sous-bois** (le semis d'aujourd'hui). Les pièces, les chemins et le départ gardent la priorité. Réutilisé par T2 à T5 (chenaux, places, paliers) | Moyen |
| **T1b** | **La clairière du Cerf et le belvédère.** Décors neufs : pierres dressées moussues, cercle de souches, lit de mousse où dort le Cerf ; table d'orientation, bancs, rambarde, longue-vue. **Le belvédère borde un pan de vide** creusé dans la terre, comme les bandes du bac à sable : un lieu où pousser la horde (pilier 1). La rambarde a des trouées | Moyen à élevé |
| **T1c** | **L'aire de pique-nique et la maison engloutie.** Tables, balançoire rouillée (l'idée de la balançoire fantôme, sans son interaction), barbecue, poubelle ; maison mangée de lierre, voiture devenue jardinière, boîte aux lettres. Variantes par groupe | Moyen à élevé |
| **T1d** | **Le viaduc effondré et la mesure.** Piles, tronçon d'autoroute tombé, voitures dessus et dessous. La mesure compte « pièces vues » (`RunObservation`). Vue d'ensemble avant/après, captures de chaque pièce, `measure_run` avant/après au même commit de base | Moyen |

Ensuite, quatre pièces sur quatre sont neuves et la terre en pose trois ou quatre par run : on ne voit pas tout à chaque fois.

**Tranché par défaut, réversible :**
- **Le rythme est une couche de données commune** : chaque terre la règle dans son fichier de décors ; le code ne connaît pas « clairière » ni « fourré », seulement des zones pondérées.
- **Les fourrés ne font jamais de mur** : une trouée tous les quelques mètres, pour que le nomade et la horde passent. Vérifié au bot (pas de blocage) et par la densité de créatures, qui ne doit pas bouger.
- **Un pan de vide au belvédère** : le vide n'existe aujourd'hui qu'au bord de l'Effacement et dans le bac à sable. Le belvédère en ouvre un petit, borné, loin du cœur et du départ. Si l'essai du vide (étape 4) le retire, le belvédère garde sa rambarde au bord d'une pente.
- **Corps procéduraux** (`tools/sprites/props/forest.py`), repris ensuite par l'ami artiste.

## 9. T1a, le terrain rythmé — livré le 10 novembre

**Ce qui est fait :**
- **Une clé `rhythm` dans `data/props/<biome>.json`** (`TerrainRhythm`) : un bruit de valeur lent, de pas `scale_px` à l'écran, rangé dans [0 ; 1], choisit la zone de chaque cellule. Chaque zone a :
  - `up_to` : sa borne haute dans le bruit ;
  - `density` : le multiplicateur de la chance de semis ;
  - `weights` et `others` : les poids des décors nommés et des autres ;
  - `ground` : un sol imposé (herbe ou sous-bois), l'eau et le béton restent ;
  - `lanes` : des trouées sinueuses (isoligne d'un second bruit) où aucun décor bloquant ne se pose.
- **La carte des zones se calcule sur le thread de génération**, juste après le terrain : les chemins, les pièces et leurs sols passent ensuite, et gardent la priorité. Le semis la relit pour sa densité et ses poids.
- **Les bois** : clairières (herbe, fleurs, souches et pierres, un bouleau isolé, densité ×0,3), sous-bois (le semis d'avant) et fourrés (sous-bois imposé, grands arbres, buissons, fougères et troncs couchés, densité ×1,8, trouées tous les quelques mètres). Graine 7 : 21 % de clairières, 56 % de sous-bois, 21 % de fourrés.
- Les autres biomes n'ont pas de rythme : leur semis est le même tirage qu'avant.

**Vérifié :**
- `tools/validate.sh` smoke, catalogues, terres : 3/3. Le contrôle du catalogue refuse douze fichiers de rythme mal écrits (clé inconnue, pas hors borne, zone seule ou en double, bornes décroissantes, dernière zone courte, sol inconnu, décor absent, poids négatif, trouées mal réglées). `tools/test_movement.sh` : aucun échec. Build sans avertissement. Relecture `godot-reviewer` : rien de bloquant, trois retouches faites.
- Vues d'ensemble et vues à hauteur du joueur : [planches/plan58_t1a_rythme.png](planches/plan58_t1a_rythme.png). De haut, les clairières se lisent comme de grandes ouvertures ; au sol, le fourré est serré mais se traverse.
- Coût : 6 266 décors sur la terre des bois à la graine 7 au lieu de 5 666 (+11 %) ; le calcul des zones prend 43 ms sur le thread de génération. Pas de banc de FPS : la machine était chargée, et le banc dense ne se joue pas dans les bois.

**Mesure** (`measure_run`, profil neuf, bot des terres, Vagabond, bois puis marais puis carrière, 600 s, graines 7 et 42, avant et après au même commit de base) :

| | Avant (7 / 42) | Après (7 / 42) |
|---|---|---|
| Créatures visibles, 0-60 s | 15,6 / 14,1 | 17,3 / 14,7 |
| Créatures visibles, 180-300 s | 19,1 / 23,0 | 16,8 / 21,4 |
| Éliminations | 408 / 535 | 546 / 546 |
| Mémoriaux vus | 4 / 9 | 7 / 6 |
| Gardien des bois vaincu | non / oui | oui / oui |

La densité de créatures reste dans le bruit d'une graine à l'autre. Les fourrés n'arrêtent pas le bot : il vainc le Cerf aux deux graines.

**Ce qui reste faible :** les clairières sont des pelouses presque nues. Elles attendent les pièces de T1b à T1d, qui s'y logeront volontiers ; le compte des « pièces vues » arrive avec T1d.

## 10. T1b, la clairière du Cerf et le belvédère — livré le 10 novembre

**Ce qui est fait :**
- **Dix décors neufs** (`tools/sprites/props/forest.py`) :
  - deux pierres dressées moussues ;
  - un lit de mousse où dort le Cerf ;
  - une table d'orientation en bronze, inclinée vers la caméra pour se lire ;
  - deux bancs de jardin public (deux orientations) ;
  - trois segments de rambarde en fonte, dont un cassé ;
  - une longue-vue à pièces.
- **La clairière du Cerf**, cœur des bois : le lit de mousse sous le Cerf, sept pierres dressées en anneau ouvert à l'ouest (d'où arrive le joueur), six souches plus loin, puis l'anneau d'arbres d'avant.
- **Le belvédère**, nouvelle pièce des bois. Il borde un **pan de vide** en ellipse de 720 × 420 px au nord de son emprise. Une rambarde à trouées longe le bord, avec devant elle une table d'orientation, des longues-vues, des bancs, et le Mémorial au sud-ouest.
- **Une clé `void` dans une pièce** (`land_pieces.json`, `{at, radii}`). À l'arrivée sur la terre, `ErasureManager` creuse les cellules du front dont le centre tombe dans l'ellipse : elles sont dans le Néant dès le départ, comme le bord de l'Effacement. Le vide se dessine comme le Néant du front, en blanc qui grésille, et la minimap le hachure. Les créatures le longent et y tombent, le joueur s'y brûle et le saute au dash, comme partout ailleurs.
- **Le lecteur refuse** :
  - un vide de moins d'une cellule d'Effacement (128 px) ;
  - un vide qui déborde de l'emprise ;
  - un Mémorial à moins de 240 px de son bord ;
  - un vide au cœur, dont le combat du gardien garde son sol.
- **Aucun chemin ne mène au vide** : une pièce à vide refuse un emplacement où le réseau de chemins traverse l'ellipse élargie de 96 px, retournée ou non. Le raccord de la pièce part au sud, loin du vide.
- **La terre restante ne compte pas les cellules creusées** (`LandFront`) : la part debout et la part du cœur restent à 1 à l'arrivée, et la minimap aussi.

**Vérifié :**
- `tools/validate.sh` smoke, catalogues, terres, mouvement : 4/4, et terres seules après la règle des chemins. Un passage des terres pendant une mesure a donné trois échecs de minutage sur la carrière, sans pièce à vide ; relancée machine moins chargée, la suite passe. Build sans avertissement. Le contrôle du catalogue exige le belvédère avec son vide et refuse cinq vides mal écrits.
- **Placement** (`--capture-pieces`, bois en première terre, dix graines : 1, 2, 3, 5, 7, 11, 13, 17, 42, 99) : quatre pièces posées dix fois sur dix, belvédère compris, de 14 à 16 cellules creusées, pièces posées en 14 à 21 ms.
- Captures : [planches/plan58_t1b_belvedere.png](planches/plan58_t1b_belvedere.png). Le Cerf dort au milieu des pierres dressées. Au belvédère, le Néant s'ouvre au nord, la rambarde le borde, et le vide est hachuré sur la minimap.
- Relecture `godot-reviewer` : cinq remarques, toutes traitées (terre restante sans les cellules creusées, chemins hors du vide, deux commentaires).

**Mesure** (`measure_run`, profil dev, bot des terres, Forgeuse, bois puis marais puis carrière, 600 s, graines 7 et 42, avant au commit de T1a, après au code final) :

| | Avant (7 / 42) | Après (7 / 42) |
|---|---|---|
| Créatures visibles, 0-60 s | 13,7 / 12,2 | 12,0 / 15,1 |
| Créatures visibles, 180-300 s | 17,7 / 17,6 | 18,8 / 26,0 |
| Éliminations | 751 / 850 | 1 028 / 864 |
| Distance parcourue (m) | 1 735 / 6 744 | 5 738 / 6 056 |
| Gardiens vaincus | 0 / 2 | 2 / 2 |
| Morts | 0 / 0 | 0 / 0 |

Une première mesure, avant la règle des chemins, laissait le bot coincé 380 s à la graine 42 : il avait abandonné un Mémorial, n'en avait que deux sur trois et attendait au départ. Le bot va en ligne droite, sans contourner le vide ni la rambarde. Avec le code final, il n'y a plus de blocage. Aucune créature n'est comptée tombée dans le vide : le bot ne pousse pas la horde, seul un joueur le fera.

**Ce qui reste :** la rambarde déborde parfois d'une demi-cellule sur le Néant, qui se creuse par cellules de 128 px (visuel seulement). Les deux pièces de démonstration attendent T1c et T1d.

## 11. T1c, l'aire de pique-nique et la maison engloutie — livré le 10 novembre

**Ce qui est fait :**
- **Neuf décors neufs** (`tools/sprites/props/forest.py`) :
  - deux tables de pique-nique à bancs pris dans les pieds (deux orientations), l'un des bancs tombé ;
  - une balançoire rouillée dont une planchette traîne au sol ;
  - un barbecue maçonné, une poubelle de tôle sur poteau ;
  - une boîte aux lettres à porte pendante ;
  - deux maisons englouties (deux sens) ;
  - une seconde voiture-jardinière, de l'autre orientation.
- **La maison engloutie** reprend le pavillon de la ferme (`building`), de plain-pied. Le lierre couvre les murs presque jusqu'au toit et coule de l'égout, la mousse prend le versant, un frêne monte contre le pignon et des buissons atteignent les appuis de fenêtre. Le générateur de bâtiments accepte désormais un seul niveau ; les bâtiments existants ne changent pas.
- **L'aire de pique-nique**, pièce des bois : deux rangées de tables sur une pelouse, des barbecues au fond, des poubelles entre les tables, une balançoire et un banc à l'écart, un panneau à l'entrée. Le Mémorial se tient à l'ouest de la pelouse.
- **La maison engloutie**, pièce des bois : le pavillon au milieu d'un jardin redevenu sous-bois, la voiture dans l'allée (deux places possibles), la boîte aux lettres au portail, des pans du mur de clôture au sud, des haies folles sur les côtés. Le Mémorial se tient dans le jardin, au sud-est.
- **Les deux pièces de démonstration des bois sont retirées** : la vieille route et le cercle de pierres. Les bois ont trois pièces neuves (belvédère, pique-nique, maison), toutes posées à chaque run jusqu'au viaduc de T1d.

**Vérifié :**
- `tools/validate.sh` : démarrage et catalogues, puis catalogues, terres et mouvement (3/3) avec le JSON définitif. Pas de C# touché, donc pas de relecture `godot-reviewer`.
- **Placement** (`--capture-pieces`, bois en première terre, graines 1, 2, 3, 5, 7, 11, 13, 17, 42, 99) : le cœur et les trois pièces sont posés dix fois sur dix, en 15 à 30 ms.
- Captures : [planches/plan58_t1c_pique_nique_maison.png](planches/plan58_t1c_pique_nique_maison.png). Une première version éparpillait quatre tables sur une grande pelouse et alignait le mur en moignons ; les tables sont passées en deux rangées et le mur en quelques pans espacés.

**Mesure** (`measure_run`, profil dev, bot des terres, Facteur, bois puis marais puis carrière, 600 s, graines 7 et 42, avant au commit de T1b, après au code final) :

| | Avant (7 / 42) | Après (7 / 42) |
|---|---|---|
| Créatures visibles, 0-60 s | 13,9 / 31,6 | 13,9 / 17,7 |
| Créatures visibles, 180-300 s | 18,2 / 21,7 | 19,1 / 24,4 |
| Éliminations | 2 071 / 1 310 | 1 762 / 689 |
| Distance parcourue (m) | 5 729 / 5 549 | 5 416 / 10 168 |
| Gardiens vaincus | 2 / 2 | 2 / 3 |
| Morts | 0 / 0 | 0 / 0 |

Le bot ne se coince pas dans les nouvelles pièces : il vainc au moins les deux premiers gardiens aux deux graines, et le troisième à la graine 42, où la run va plus loin (distance doublée) avec moins d'éliminations. La densité de créatures reste dans le bruit.

**Ce qui reste :** le mur de clôture se lit encore comme une suite de pans isolés, faute d'un décor de mur long. Le compte des pièces vues arrive avec T1d.

## 12. T1d, le viaduc effondré et la mesure — livré le 10 novembre

**Ce qui est fait :**
- **Cinq décors neufs** (`tools/sprites/props/forest.py`) :
  - deux piles de béton, l'une entière sous son chevêtre, l'autre cassée à mi-hauteur, fers à nu ;
  - deux tronçons de tablier tombés, inclinés, l'asphalte et ses lignes blanches, la glissière tordue : l'un porte une voiture rouge écrasée, l'autre est fendu avec un jeune arbre dans la fente ;
  - un panneau d'autoroute bleu tombé au sol.
- **Le viaduc effondré**, quatrième pièce des bois : quatre piles en ligne, les deux tronçons de tablier tombés entre elles, une voiture-jardinière dessous, le panneau au sud, une seconde voiture une fois sur deux, des rochers, des arbres et des fougères qui reprennent le béton. Le Mémorial se tient au sud, entre les tronçons.
- **Les bois posent trois ou quatre pièces parmi quatre** : on ne voit plus tout à chaque run.
- **La mesure compte les pièces vues** (`RunObservation`) : `pieces_seen`, `pieces_placed` et `pieces_seen_ids` au résultat. Une pièce est vue quand le cadre de la caméra recouvre la moitié centrale de son emprise. Le cœur ne compte pas. La terre d'après la victoire recompose la carrière : ses pièces comptent deux fois.
- **Le bot des terres contourne enfin** : son détour, quand il reste bloqué loin de sa cible, ne se déclenchait jamais. Le contrôle général du blocage remettait le chrono à zéro juste avant lui. À la graine 42, le bot restait coincé 450 s entre deux arbres de la clairière du Cerf, à 320 px du repaire. Le constat de blocage est désormais partagé entre les deux.

**Vérifié :**
- `tools/validate.sh` démarrage, catalogues, terres et mouvement : 3/4, puis le mouvement seul, relancé : 1/1. L'échec (« dash défaut : invulnérable pendant tout le dash ») est un minutage sous charge : ce lot ne touche ni au joueur ni au dash. Build sans avertissement.
- **Placement** (`--capture-pieces`, bois en première terre) : graines 1 et 42, le viaduc posé et lisible.
- **Captures** : [planches/plan58_t1d_viaduc_bois.png](planches/plan58_t1d_viaduc_bois.png), qui montre :
  - la vue d'ensemble des bois avant T1 (T0, graine 7) : un semis uniforme ;
  - la même vue après T1 : clairières et fourrés, quatre pièces neuves ;
  - le viaduc de près (graine 42).
- Pas de relecture `godot-reviewer` : le C# touché est l'outil de mesure (une quarantaine de lignes).

**Mesure** (`measure_run`, profil dev, bot des terres corrigé des deux côtés, Éveillée, bois puis marais puis carrière, 600 s, graines 7, 42 et 11, avant au commit de T0, après au code final) :

| | Avant, T0 (7 / 42 / 11) | Après, T1 (7 / 42 / 11) |
|---|---|---|
| Pièces des bois posées (hors cœur) | 2 / 2 / 2 | 4 / 4 / 3 |
| Pièces des bois vues | 1 / 1 / 1 | 3 / 2 / 1 |
| Pièces vues, trois terres | 4 / 5 / 3 | 6 / 6 / 3 |
| Créatures visibles, 0-60 s | 13,8 / 20,2 / 19,9 | 12,4 / 25,1 / 19,9 |
| Créatures visibles, 180-300 s | 19,6 / 24,4 / 23,1 | 23,9 / 20,7 / 24,5 |
| Éliminations | 1 682 / 1 064 / 1 048 | 1 299 / 1 576 / 871 |
| Distance parcourue (m) | 8 136 / 8 683 / 8 920 | 8 593 / 9 204 / 8 950 |
| Gardiens vaincus | 3 / 3 / 3 | 3 / 3 / 3 |
| Morts | 0 / 0 / 0 | 0 / 0 / 0 |

Le bot voit une à trois pièces des bois sur trois ou quatre. Il va droit aux Mémoriaux, puis au cœur ; un joueur qui flâne en verra plus. Le belvédère n'est vu à aucune graine : son vide le pose loin des chemins. La densité de créatures reste dans le bruit, et les trois gardiens tombent à chaque graine, avant comme après.

**Bilan de T1** : les bois ont un terrain rythmé (clairières, sous-bois, fourrés), un cœur fait main (la clairière du Cerf) et quatre pièces neuves, faites de vingt-quatre décors neufs. Trois ou quatre de ces pièces sont posées à chaque run.

**Ce qui reste :** un tablier debout, sous lequel on passe, est en réserve ([§211](DECISIONS.md)). Le mur long de la maison engloutie manque toujours. Le Bas-Port (T2) suit, dans l'ordre du §5.

## 13. T2, le Bas-Port — découpage proposé le 11 novembre

Proposition de l'agent, réversible ([§212](DECISIONS.md)). Le lot est élevé : il se coupe en quatre, comme T1, chacun livré, capturé et mesuré avant le suivant.

**Ce qui ne va pas aujourd'hui** ([planches/plan58_t2_bas_port_avant.png](planches/plan58_t2_bas_port_avant.png), graines 7 et 42) :
- La terre est un marécage générique : des taches d'eau éparses, un semis uniforme d'arbres morts, de racines et d'ossements. Rien ne dit « port », ni de loin ni à hauteur du joueur.
- Le cœur est un anneau d'arbres morts autour du Cerf, gardien provisoire jusqu'au Pilleur (plan 57 G4).
- Deux pièces de démonstration : le ponton des pêcheurs et l'autel noyé. Le marais a vingt-six décors, aucun du port.
- Le lore en dit beaucoup : la digue rompue à 22 h 40, la cale où Marc Delmas est allé fermer le chantier, les quais d'une mer que la ville a oubliée, le courrier que le Facteur portait encore à des adresses rayées.

| Sous-lot | Contenu | Effort |
|---|---|---|
| **T2a** | **Le terrain du Bas-Port.** Des **chenaux** : rubans d'eau sinueux, tirés d'une isoligne de bruit comme les trouées des fourrés, interrompus par des gués. Quand un chemin croise un chenal, la cellule redevient du sol et porte une **passerelle**. Puis les zones du rythme de T1a : **roselières** (roseaux hauts, presque rien qui bloque), **vasières** (sol nu, pieux, épaves), **prés salés** (le semis d'aujourd'hui, allégé de ses arbres morts). Décors neufs : passerelle (deux sens), pieux d'amarrage, roseaux hauts, casiers, filet qui sèche sur des perches | Moyen à élevé |
| **T2b** | **La cale du chantier naval et les quais.** Le **cœur** : une coque de chalutier sur sa cale, étayée, le treuil et sa chaîne, des tas de planches, la cabane du chantier ; le repaire au pied de la coque. **Les quais**, pièce qui borde un **pan de vide** (la mer oubliée), comme le belvédère : un quai de pierre, des bittes d'amarrage, une échelle qui descend dans le Néant, une grue de port, des barques couchées. Le ponton des pêcheurs est retiré | Élevé |
| **T2c** | **Les marais salants et la digue.** Les **marais salants** : des bassins d'eau rectangulaires séparés de diguettes où l'on marche, des tas de sel devenu gris, la cabane du saunier, un las et une brouette. **La digue** : un long talus de pierres sèches, avec **la brèche** par où l'eau est entrée, des blocs effondrés et des sacs de sable. Une pièce peut désormais poser de l'eau (`water`, comme `void`). L'autel noyé est retiré | Moyen à élevé |
| **T2d** | **Les maisons du port et la mesure.** Une rangée de maisons de pêcheurs basses (`building`, de plain-pied, crépi blanc, volets peints écaillés), des filets, des casiers, une boîte aux lettres qui déborde de courrier « retour à l'envoyeur ». Décors remarquables garantis du port. Vue d'ensemble avant/après, captures de chaque pièce, `measure_run` avant/après au même commit de base, pièces vues | Moyen |

Ensuite, le Bas-Port a un cœur et quatre pièces neuves, et en pose trois ou quatre par run.

**Tranché par défaut, réversible :**
- **La mer oubliée est le vide** : les quais bordent un pan de Néant, comme le belvédère des bois. Le lore le dit (« pour oublier le Bas-Port, elle oublie la mer ») ; c'est aussi un lieu où pousser la horde (pilier 1).
- **Les chenaux ralentissent, ils n'arrêtent pas** : l'eau ralentit déjà le joueur sans le bloquer. Un chenal a des gués tous les quelques mètres, et aucun chemin ne s'y arrête.
- **Le Cerf reste le gardien provisoire** du Bas-Port jusqu'au Pilleur (plan 57 G4). La cale garde la place du repaire, au pied de la coque.
- **Les deux pièces de démonstration partent** au fil des sous-lots : le ponton avec les quais (T2b), l'autel noyé avec les marais salants (T2c).
- **Le marais reste le biome** (`swamp`) : son identifiant ne change pas, seuls ses décors, son rythme et ses pièces deviennent ceux du port.
- **Corps procéduraux** (`tools/sprites/props/swamp.py`), repris ensuite par l'ami artiste.

## 14. T2a, le terrain du Bas-Port — livré le 11 novembre

**Ce qui est fait :**
- **Des chenaux** (`rhythm.channels` dans `data/props/swamp.json`, `TerrainRhythm`) : l'isoligne d'un bruit lent (pas de 1 500 px) creuse des rubans d'eau sinueux. Un second bruit les coupe de **gués**, sur un peu moins d'un tiers de leur longueur. Ils évitent les abords du départ. Ils couvrent 4 700 à 6 500 cellules selon la graine.
- **Les chemins franchissent les chenaux** : le routeur traverse l'eau d'un chenal à un coût de 6 (`paths.costs.channel`, `world_gen.json`). Les autres eaux restent infranchissables. Les cellules d'eau sous un chemin redeviennent du sol, et chaque cellule que l'axe du chemin traverse porte une **passerelle** (`ChannelBridgePlacer`) :
  - un caillebotis non bloquant, plaqué au sol, qu'on foule sans passer derrière (option `flat` d'`EnvironmentProp.Initialize`) ;
  - quatre sens : le long de x, le long de y, et les deux diagonales. Toute une traversée prend le sens de son entrée à sa sortie, sinon un chemin qui tourne sur l'eau alterne les modules en escalier ;
  - 17 à 44 passerelles par terre.
- **Trois zones** (le rythme de T1a) :
  - **roselières** : roseaux hauts, presque rien qui bloque, densité ×1,4 ;
  - **prés salés** : le semis d'avant, allégé de ses arbres morts, avec des casiers et des filets qui sèchent ;
  - **vasières** : sol nu, pieux, épaves, barques, densité ×0,6.

  Selon la graine, chaque zone couvre 26 à 40 % de la terre.
- **Les décors neufs** (`tools/sprites/props/swamp.py`) :
  - quatre modules de passerelle ;
  - des roseaux hauts à plumets ;
  - des casiers à homards, avec leur cordage et leur flotteur rouge ;
  - un filet qui sèche sur deux perches, effrangé, avec ses lièges ;
  - une rangée de pieux d'amarrage, deux cassés.
- **Le marais perd ses taches d'eau** : le poids de l'eau du biome passe de 0,20 à 0,10, sinon les flaques naturelles noient les chenaux.
- **`SwampPropPlacer` est retiré.** Ses 870 lignes posaient une centaine de décors en dur, à des cellules réservées. Le rythme JSON fait ce travail désormais.
- **La capture des pièces** (`--capture-pieces`) prend aussi les deux traversées de chenal les plus proches du départ (`bridge-00`, `bridge-01`).

**Vérifié :**
- `tools/validate.sh` démarrage, catalogues, terres et mouvement : 4/4. Build sans avertissement.
- Le contrat des catalogues lit le rythme du Bas-Port (trois zones, chenaux, quatre passerelles). Il refuse sept chenaux mal formés : clé inconnue, chenal trop large, gués absents, gués partout, passerelle manquante, passerelle inconnue, image absente.
- **Placement** (`--capture-pieces`, Bas-Port en première terre, graines 1, 2, 3, 5, 7, 11, 13, 17, 42 et 99) : les trois pièces actuelles sont posées à chaque graine, et deux traversées sont capturées à chaque graine.
- **Captures** : [planches/plan58_t2a_terrain_bas_port.png](planches/plan58_t2a_terrain_bas_port.png), qui montre :
  - la vue d'ensemble avant et après (graine 7) ;
  - une passerelle en roselière (graine 7) ;
  - une traversée en diagonale (graine 42).
- Relecture `godot-reviewer` : aucun défaut bloquant. Trois remarques ont été traitées :
  - les modules diagonaux (28 à 30 px) dépassaient le seuil des décalques : ils sont désormais plaqués au sol ;
  - le biome d'une passerelle se lit par la même cellule que le relevé ;
  - une traversée en épingle prend le sens du tracé en son milieu.

**Mesure** (`measure_run`, profil dev, Scaphandrière, Bas-Port puis bois puis carrière, 600 s, graines 7, 42 et 11, avant au commit c8a13cb8, après au code final, chacun dans sa copie de travail) :

| | Avant (7 / 42 / 11) | Après (7 / 42 / 11) |
|---|---|---|
| Troisième Mémorial du Bas-Port ravivé (s) | 118 / 97 / 98 | 135 / 112 / 110 |
| Bas-Port quitté (s) | 185 / 165 / 193 | 168 / 186 / 208 |
| Pièces du Bas-Port vues (hors cœur) | 1 / 1 / 1 | 1 / 1 / 2 |
| Créatures visibles, 0-60 s | 11,1 / 17,3 / 18,1 | 13,3 / 15,9 / 15,3 |
| Créatures visibles, 180-300 s | 19,7 / 19,5 / 20,1 | 18,4 / 18,8 / 19,2 |
| Éliminations | 1 972 / 1 597 / 788 | 2 471 / 920 / 749 |
| Distance parcourue (m) | 8 068 / 8 063 / 8 567 | 8 412 / 9 174 / 8 307 |
| Gardiens et échos vaincus | 4 / 3 / 2 | 4 / 3 / 2 |
| Morts | 0 / 0 / 0 | 0 / 0 / 0 |

Le bot met 12 à 17 s de plus à raviver les trois Mémoriaux du Bas-Port. Il ne suit pas les chemins : il coupe à travers les chenaux, dans l’eau qui le ralentit. Il quitte la terre entre 17 s plus tôt et 21 s plus tard. La densité de créatures et le compte des gardiens ne bougent pas.

**Ce qui reste :** au sol, les trois zones se distinguent par leurs décors plus que par leur couleur ; la teinte propre de chaque zone attendra les maisons et le sel (T2c, T2d). Les deux pièces de démonstration (ponton, autel noyé) sont encore là. Suite : T2b, la cale du chantier naval et les quais.

## 15. T2b, la cale du chantier naval et les quais — livré le 11 novembre

**Ce qui est fait :**
- **Onze décors neufs** (`tools/sprites/props/swamp.py`) :
  - le **chalutier sur sa cale** : coque bleue délavée, antifouling rouille, timonerie, potence de chalut, mât de charge, posé sur ses tins et tenu par trois paires d'étais, coulures de rouille et bois nu ;
  - la **cale de halage** : longrines goudronnées, traverses, rails ;
  - le **treuil**, avec sa chaîne enroulée qui court au sol ;
  - un **tas de planches** ;
  - la **cabane du chantier** : planches peintes, tôle ondulée rouillée, porte entrouverte, carreau cassé, enseigne ;
  - pour les quais : la **bordure de granit**, les **pavés**, la **bitte d'amarrage** et la **grue du port**, dont la flèche en treillis passe au-dessus du vide ;
  - une **barque retournée** sur ses chevalets ;
  - des **caisses à poisson** en plastique.
- **Le cœur devient la cale du chantier** (`cale_du_chantier`, à la place de l'île du gardien) :
  - le chalutier au nord, sa cale dessous, le treuil en tête de cale, la cabane à l'est ;
  - des planches, des casiers et des barils, une barque retournée ;
  - le Cerf dort au pied de la coque ;
  - l'ouest reste ouvert.
- **Les quais** (`quais`, à la place du ponton des pêcheurs) bordent la mer oubliée, un pan de vide au nord :
  - la bordure de granit le longe en ligne droite, avec le pavé derrière, quatre bittes et la grue ;
  - des barques, des caisses et des filets ;
  - le Mémorial se tient au sud-ouest ;
  - on y pousse la horde dans le Néant.
- **Le pan de vide se cale sur la grille d'Effacement** (`LandPieceComposer.AlignVoid`).
  - Le vide se creuse par cellules de 128 px, alignées sur le monde, alors que les pièces s'ancrent sur la grille des tuiles. Le bord du pan tombait donc n'importe où dans la pièce, à une cellule près.
  - L'ancre glisse désormais pour que le centre du pan tombe pile au centre d'une cellule. Pour cela, le décalage du pan (`void.at`) doit tomber sur la grille des tuiles : x multiple de 32, y de 16, x/32 + y/16 pair. Sinon le catalogue est refusé. Les mêmes dix-sept cellules se creusent alors à chaque run.
  - Si l'ancre alignée ne tient pas (place prise, trop près d'une autre pièce), elle ne bouge pas, et un avertissement le dit.
  - Le belvédère des bois en profite : son bord tombe droit, juste au-dessus de la rambarde. Son pan passe de (0, −150) à (0, −160), sur la grille.
- **Les éléments de lieu ont une clé `flat`** : un plancher, une cale ou des pavés se plaquent au sol quelle que soit leur hauteur à l'écran. On marche dessus sans passer derrière. Un élément `flat` ne peut pas bloquer.

**Vérifié :**
- `tools/validate.sh` démarrage, catalogues, terres et mouvement : 4/4. Après les retours de relecture, catalogues et terres repassent. Build sans avertissement.
- Le contrat des catalogues lit le cœur et les quais. Il refuse quatre nouveaux cas : `flat` non booléen, `flat` et bloquant, vide entre deux pixels, vide entre deux rangées de tuiles.
- **Placement** (`--capture-pieces`, Bas-Port en première terre, graines 1, 2, 3, 5, 7, 11, 13, 17, 42 et 99) : trois pièces à chaque graine, dix-sept cellules de vide à chaque graine, sans avertissement. Avant le calage, la graine 2 en creusait douze.
- **Captures** : [planches/plan58_t2b_cale_quais.png](planches/plan58_t2b_cale_quais.png), qui montre :
  - le cœur (graine 7) ;
  - les quais, avec la bordure contre le bord du vide (graine 42) ;
  - les onze décors ;
  - le belvédère calé (graine 1 des bois).
- Relecture `godot-reviewer`. Elle relève que l'ancre retombe sur la grille des tuiles : le centre du pan restait jusqu'à 32 px du centre de cellule, et les colonnes du bord pouvaient basculer (16 ou 17 cellules selon la graine). L'ancre prend désormais la cellule de tuile la plus proche du point visé, et les rayons des quais (430 × 190 px) gardent la marge. Une seconde relecture relève que le glissement ignorait l'écart minimal entre pièces et se taisait s'il était refusé : il respecte désormais l'écart et avertit. Le test des quais ne dépend plus de l'ordre des pièces.

**Mesure** (`measure_run`, profil dev, Traqueur, Bas-Port puis bois puis carrière, 600 s, graines 7, 42 et 11, avant au commit 4c3c7c43, après au code d'avant la seconde relecture (qui ne touche que des cas de refus et le belvédère), chacun dans sa copie de travail) :

| | Avant (7 / 42 / 11) | Après (7 / 42 / 11) |
|---|---|---|
| Troisième Mémorial du Bas-Port ravivé (s) | 123 / 106 / 101 | 106 / 112 / 88 |
| Bas-Port quitté (s) | 176 / 170 / 175 | 166 / 187 / 148 |
| Pièces du Bas-Port vues (hors cœur) | 1 / 1 / 2 | 1 / 1 / 1 |
| Créatures visibles, 0-60 s | 19,4 / 19,1 / 23,4 | 14,6 / 16,8 / 19,4 |
| Créatures visibles, 180-300 s | 18,3 / 19,9 / 21,0 | 17,5 / 18,9 / 18,2 |
| Éliminations | 1 650 / 1 289 / 1 017 | 2 096 / 1 359 / 1 354 |
| Distance parcourue (m) | 9 300 / 9 762 / 10 038 | 8 999 / 10 321 / 11 519 |
| Gardiens et échos vaincus | 4 / 3 / 3 | 4 / 3 / 3 |
| Morts | 0 / 0 / 0 | 0 / 0 / 0 |

Le bot ravive les trois Mémoriaux du Bas-Port 13 à 17 s plus tôt aux graines 7 et 11, et 6 s plus tard à la graine 42. Il voit les quais aux graines 42 et 11. Sur la première minute, il voit 3 à 5 créatures de moins, mais il en tue davantage (+70 à +450 sur la run). Le cœur est plus ouvert que l'île du gardien, et le bot y nettoie plus vite. Entre 180 et 300 s, la densité ne bouge pas. Le compte des gardiens non plus.

**Ce qui reste :**
- Le pavé des quais se lit un peu répétitif : trois rangées du même module.
- En capture, le pan de vide n'est pas encore dessiné quand la caméra saute sur une pièce juste après le cœur. La minimap le montre. C'est un artefact du téléport de la capture, pas du jeu.
- La première minute moins dense est à surveiller dans la mesure de T2d.

Suite : T2c, les marais salants et la digue.

## 16. T2c, les marais salants et la digue rompue — livré le 11 novembre

**Ce qui est fait :**
- **Neuf décors neufs** (`tools/sprites/props/swamp.py`) :
  - la **saline**, plaquée au sol : une diguette d'argile autour d'une eau laiteuse, une croûte de sel gris sur les bords et en plaques ;
  - le **tas de sel** devenu gris, sali à la base, raviné ;
  - le **las** du saunier, couché ;
  - la **brouette** pleine de sel pris en bloc ;
  - la **cabane du saunier** : moellons passés à la chaux qui s'écaille, toit de tuiles creuses où il en manque, banc de pierre ;
  - la **vanne** entre deux bassins, planche relevée et manivelle rouillée ;
  - le **talus de la digue**, en pierres sèches enherbées, qui bloque ;
  - les **blocs de la brèche**, roulés par l'eau ;
  - les **sacs de sable** posés trop tard, un crevé.
- **Une pièce peut poser de l'eau** (`water`, `LandPieceConfig`) : des bassins rectangulaires en coordonnées de pièce, retournés avec elle.
  - Ils se remplissent une fois toutes les pièces et leurs embranchements posés. Un chemin qui en traverse un reste à sec.
  - L'eau l'emporte sur le sol imposé.
  - Le lecteur refuse un bassin plus petit qu'une tuile, hors de l'emprise, ou à moins de 64 px du Mémorial.
- **Les marais salants** (`marais_salants`, à la place de l'autel noyé) :
  - six salines en deux rangs, séparées de diguettes où l'on marche, des vannes entre elles ;
  - au sud, les tas de sel de la dernière récolte, le las, la brouette ;
  - la cabane du saunier à l'est ;
  - le Mémorial au sud-ouest.
- **La digue rompue** (`digue_rompue`, pièce neuve) :
  - un long talus d'ouest en est, la mer revenue au nord ;
  - au milieu, la brèche : des blocs roulés, une langue d'eau qui passe et s'étale au sud, des sacs de sable de part et d'autre ;
  - le Mémorial au sud-est.
- **Le Bas-Port pose désormais ses trois pièces à chaque run** : les quais, les marais salants, la digue.

**Écarts au plan :**
- **La forme d'une saline vient du décor, pas des tuiles.** Posés seuls, six bassins d'eau se fondaient en une mare : la bordure de vase des tuiles d'eau noie les diguettes. Le décor plaqué donne la forme, l'eau dessous ralentit.
  - L'eau est un peu plus petite que le décor, et remontée de 24 px : une tuile d'eau se dessine plus bas que sa cellule.
  - Il reste un halo de vase autour de chaque saline, qui se lit comme de la terre mouillée.
- **Le calage du vide (T2b) se fait dès le choix de l'emplacement.** Sur les dix graines de placement, la graine 99 refusait le glissement des quais (trop près d'une autre pièce) : 16 cellules et un avertissement.
  - Le sens de la pièce est désormais tiré avant les candidats. Chaque candidat est calé avant qu'on éprouve sa place et son écart aux autres pièces : il n'y a plus de glissement à refuser.
  - L'ordre du tirage change, donc les placements de toutes les terres aussi.

**Vérifié :**
- `tools/validate.sh` démarrage, catalogues, terres et mouvement : démarrage et mouvement verts, puis catalogues et terres verts au code final. Build sans avertissement.
- Le contrat des catalogues lit les six bassins des marais salants. Il refuse cinq nouveaux cas : liste de bassins vide, clé inconnue, bassin plus petit qu'une tuile, bassin hors de l'emprise, Mémorial au bord d'un bassin.
- **Placement** (`--capture-pieces`, Bas-Port en première terre, graines 1, 2, 3, 5, 7, 11, 13, 17, 42 et 99) : quatre pièces à chaque graine (le cœur et les trois pièces), dix-sept cellules de vide aux quais à chaque graine, aucun avertissement.
- **Captures** : [planches/plan58_t2c_salants_digue.png](planches/plan58_t2c_salants_digue.png), qui montre :
  - les marais salants (graine 42) ;
  - la digue rompue (graine 7) ;
  - les neuf décors ;
  - la brèche de près.

  Les salines ont demandé quatre essais. Six bassins d'eau seuls se fondaient en une mare. Écartés, ils restaient reliés par la vase. Avec le décor plaqué, un liseré d'eau dépassait. L'eau remontée de 24 px le fait disparaître.
- Relecture `godot-reviewer`. Trois remarques ont été traitées :
  - la marge du Mémorial est inclusive : le cas de test tombait pile sur le bord exclu de `Rect2.HasPoint` ;
  - les bassins se remplissent une fois toutes les pièces posées, pour qu'un embranchement tracé plus tard n'en traverse pas un plein d'eau ;
  - l'écriture du terrain passe par une seule aide (`SetTerrain`).

**Mesure** (`measure_run`, profil dev, Vagabond, Bas-Port puis bois puis carrière, 600 s, graines 7, 42 et 11, avant au commit ccb6754f, après au code final, chacun dans sa copie de travail) :

| | Avant (7 / 42 / 11) | Après (7 / 42 / 11) |
|---|---|---|
| Pièces du Bas-Port posées (hors cœur) | 2 / 2 / 2 | 3 / 3 / 3 |
| Pièces du Bas-Port vues (hors cœur) | 1 / 1 / 1 | 2 / 1 / 2 |
| Troisième Mémorial du Bas-Port ravivé (s) | 114 / 122 / 93 | 104 / 125 / 160 |
| Bas-Port quitté (s) | 190 / 208 / 160 | 168 / 194 / 223 |
| Créatures visibles, 0-60 s | 19,7 / 16,6 / 15,7 | 18,2 / 16,2 / 14,4 |
| Créatures visibles, 180-300 s | 20,3 / 17,3 / 21,8 | 20,3 / 21,4 / 19,1 |
| Éliminations | 1 076 / 820 / 1 091 | 1 561 / 1 052 / 743 |
| Distance parcourue (m) | 8 305 / 8 684 / 9 415 | 7 946 / 8 677 / 8 525 |
| Gardiens et échos vaincus | 3 / 2 / 3 | 4 / 3 / 2 |
| Morts | 0 / 0 / 0 | 0 / 0 / 0 |

Le bot voit une à deux pièces du Bas-Port au lieu d'une. Aux graines 7 et 42, il ravive les trois Mémoriaux 10 s plus tôt et 3 s plus tard. À la graine 11, le nouveau tirage éloigne les pièces : premier Mémorial à 59 s au lieu de 33 s, troisième à 160 s au lieu de 93 s. Le bot quitte alors la terre 63 s plus tard et vainc un écho de moins. Il ne se bloque nulle part ; c'est le placement qui change. La densité ne bouge pas.

Une première mesure, au code d'avant le calage au choix de l'emplacement, donnait 3 / 2 / 2 pièces vues et des Mémoriaux ravivés en 116 / 115 / 109 s. La différence entre les deux mesures donne l'ordre de l'écart d'un tirage à l'autre.

**Ce qui reste :**
- Un halo de vase entoure chaque saline. Il se lit comme de la terre mouillée, mais un bord d'eau propre à la saline demanderait une tuile de bassin.
- La première minute moins dense de T2b ne revient pas ici. À regarder dans la mesure d'ensemble de T2d, avec plus de graines.
- À la graine 11, les pièces éloignées allongent le Bas-Port d'une minute. La mesure d'ensemble de T2d dira si c'est fréquent.
- La suite des terres a échoué deux fois sur la carrière, par instabilité : une fois le gardien (le repaire ne s'ouvre pas), une fois un chronomètre de 16 ms sous une charge de 6. Elle passe à chaque relance. Ces deux tests ne touchent pas le Bas-Port.

Suite : T2d, les maisons du port et la mesure d'ensemble.

## 17. T2d, les maisons du port et la mesure d'ensemble — livré le 11 novembre

**Ce qui est fait :**
- **Cinq décors neufs** (`tools/sprites/props/swamp.py`) :
  - **trois maisons de pêcheurs** de plain-pied, reprises du générateur d'immeubles (`building`, style `house`) : crépi blanc sali, tuiles moussues, volets qui s'écaillent, deux largeurs, trois états ;
  - la **boîte aux lettres** du port : tôle jaune passé, la fente bourrée, un tas d'enveloppes à ses pieds, tamponnées de rouge ;
  - le **feu de port** : socle de béton, tourelle en treillis rouge délavé, lanterne éteinte.
- **Les maisons du port** (`maisons_du_port`, pièce neuve) :
  - trois maisons en rangée au nord, la boîte aux lettres devant celle du milieu ;
  - des filets qui sèchent, des casiers, des caisses, des barques retournées, deux lanternes ;
  - le sud reste ouvert pour le combat ;
  - le Mémorial au sud-ouest.
- **Le Bas-Port a quatre pièces** (les quais, les maisons, les marais salants, la digue) autour de la cale, et en pose trois ou quatre par run.
- **Ses décors remarquables sont ceux du port** : le feu de port, le ponton effondré, la barque submergée, la charrette noyée, posés une fois chacun sur la terre (4 sur 4 à chaque graine mesurée). L'autel noyé et l'arbre ligaturé restent au semis.

**Écart au plan :** une bande de pavés devant les maisons devait faire la rue. Elle se lisait comme un muret : elle est retirée.

**Vérifié :**
- `tools/validate.sh` démarrage, catalogues, terres et mouvement : 3/3 puis 3/3 au code final. Build sans avertissement.
- Le contrat des catalogues compte quatre pièces au Bas-Port et lit les maisons du port.
- **Placement** (graines 7, 42 et 11) : 4 à 5 pièces avec le cœur, quatre remarquables sur quatre, aucun avertissement.
- **Captures** : [planches/plan58_t2d_maisons_port.png](planches/plan58_t2d_maisons_port.png), qui montre :
  - le Bas-Port avant T2, puis après, à la graine 7 ;
  - les maisons du port ;
  - les cinq décors.
- Pas de relecture `godot-reviewer` : le seul C# touché est le contrat des catalogues (deux index et deux comptes).

**Mesure d'ensemble de T2** (`measure_run`, profil dev, Forgeuse, Bas-Port puis bois puis carrière, 600 s, graines 7, 42, 11, 3 et 99, avant au commit c8a13cb8, avant T2a, après au code final, chacun dans sa copie de travail) :

| | Avant (7 / 42 / 11 / 3 / 99) | Après (7 / 42 / 11 / 3 / 99) |
|---|---|---|
| Pièces du Bas-Port posées (hors cœur) | 2 / 2 / 2 / 2 / 2 | 4 / 4 / 3 / 4 / 3 |
| Pièces du Bas-Port vues (hors cœur) | 1 / 1 / 1 / 1 / 1 | 3 / 2 / 1 / 2 / 2 |
| Troisième Mémorial du Bas-Port ravivé (s) | 121 / 99 / 100 / 102 / 100 | 129 / 130 / 139 / 119 / 120 |
| Bas-Port quitté (s) | 202 / 172 / 179 / 156 / 183 | 189 / 218 / 210 / 165 / 188 |
| Créatures visibles, 0-60 s | 10,6 / 15,4 / 16,4 / 18,0 / 17,6 | 11,4 / 13,3 / 17,3 / 12,7 / 14,2 |
| Créatures visibles, 180-300 s | 17,9 / 20,2 / 19,0 / 19,7 / 18,2 | 22,8 / 18,5 / 20,5 / 22,7 / 21,0 |
| Éliminations | 1 338 / 907 / 858 / 1 683 / 1 349 | 1 141 / 968 / 800 / 1 489 / 1 495 |
| Distance parcourue (m) | 7 717 / 8 196 / 8 050 / 7 960 / 7 740 | 7 700 / 7 524 / 8 306 / 7 464 / 7 688 |
| Gardiens et échos vaincus | 3 / 2 / 2 / 3 / 3 | 3 / 3 / 3 / 3 / 3 |
| Morts | 0 / 0 / 0 / 0 / 0 | 0 / 0 / 0 / 0 / 0 |

Le bot voit en moyenne deux pièces du Bas-Port au lieu d'une : il traverse des lieux, plus seulement un semis. Les Mémoriaux se logent dans les pièces, plus éloignées les unes des autres : le troisième est ravivé 23 s plus tard en moyenne (104 → 127 s), et le Bas-Port est quitté 16 s plus tard (178 → 194 s). La première minute est un peu moins dense (15,6 → 13,8 créatures visibles), la suite un peu plus (19,0 → 21,1). Le bot vainc deux échos de plus sur cinq runs et ne meurt jamais.

**Ce qui reste :**
- Le Bas-Port dure un peu plus longtemps au bot (+16 s). Il ne suit pas les chemins : un joueur qui les suit, passerelles comprises, traverse plus vite.
- Les maisons sont posées en rangée sans rue entre elles. Une rue demanderait un module de pavés qui se lise à plat.
- Un halo de vase entoure les salines (T2c).

**Le Bas-Port est fait.** Il a un cœur, quatre pièces neuves, des chenaux, des gués et des passerelles, et des décors remarquables de port. Suite : la Haute-Ville (T3), à découper comme T1 et T2.
