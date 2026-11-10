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
