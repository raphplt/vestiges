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

Proposition de l'agent, réversible. Chaque lot de terre la précise avant de coder.

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
