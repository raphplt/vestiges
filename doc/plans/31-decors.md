# Plan 31 — Décors un cran au-dessus

9 octobre 2026 · Demandé par Raphaël ([DECISIONS §84](DECISIONS.md)) : « Je voudrais que tu améliore les sprites de certains décors. Fais d'abord un tour sur le repo pour lister et évaluer la qualité des décors ». Audit présenté, validé sans retour supplémentaire ; ordre choisi : **le marais d'abord, puis la partie urbaine**, « avec une attention maximale » sur chaque décor.

## 1. Audit du 9 octobre

186 sprites regardés en planche sur le vrai sol de chaque biome, à côté du Vagabond : 172 décors sur cinq biomes, 8 coffres, 6 lieux. Tous sortent du pipeline procédural (`tools/sprites/props/`), sauf trois anciens murs urbains hors manifeste.

**Ce qui tient :** même lumière, même échelle, contours sel-out, palettes de biome. Réussites : galeries et baraques de la carrière, pelleteuse, voitures, granges, église, éolienne, tracteur embourbé, scènes du linge et du pique-nique, coffres, Mémorial.

**Défauts communs, par ordre de visibilité :**

1. Façades en aplat : murs d'une couleur, sans brique, enduit, salissure ni fissure ; toits sombres semés de rectangles verts (18 immeubles urbains, 3 fermes). Les sprites les plus pauvres en couleurs (≈ 45 couleurs pour 40 000 à 50 000 pixels), et ceux qui couvrent le plus d'écran.
2. Végétation « en pilule » : lierre en bâtons verts arrondis, arbres de toit en deux boules, mousse des voitures en tache.
3. Effondrements en aplats noirs sur les ruines.
4. Variantes « endommagées » presque identiques aux intactes (`farm_barn_damaged_a`, `apartment_damaged`).
5. Petits décors noyés dans le sol : buisson et fougère en forêt, haies vert sur vert, et surtout le marais (piquet, mousse pendante, roseaux, nénuphars).
6. Décors méconnaissables ou minuscules : bureau renversé, mur de béton « frigo », oratoire en bloc gris, mousse pendante en barre, rideau de lianes en tuyau, fûts, champignons, panneau de danger, cristaux.
7. Troncs en poteaux sans branche, troncs du verger identiques.
8. Orphelins : `prop_brick_wall`, `prop_concrete_wall`, `prop_concrete_wall_v3` (référencés nulle part), `_wild_fields_hero_props.png` ; `prop_overturned_desk` utilisé mais hors manifeste.

Notes : marais 4/10, immeubles 5/10, forêt et petits décors urbains 6/10, champs, carrière, coffres et lieux 7/10.

## 2. Lots

| Lot | Contenu | État |
|---|---|---|
| **M0** | Kit commun : détails de surface (`tools/sprites/props/_surface.py` : bruit de valeur reproductible, fibres, taches, assises, liseré d'eau) ; `generate_props.py --jobs` (rendu parallèle, résultat identique) | Livré |
| **M1** | Marais : les 25 modèles repris un par un (26 PNG), planches regardées à ×2 et ×5 sur les trois sols du biome | Livré, voir §3 |
| **M2** | Marais en jeu : galerie de captures en vraie run, corrections | Livré, voir §4 |
| U1 | Immeubles urbains : matières des façades (enduit, brique, salissures, coulures, fissures), toits lisibles (gravier, édicules, bacs), lierre et mousse refaits en vrai feuillage | À faire |
| U2 | Ruines : effondrements remplis de gravats et de planchers éventrés, variantes endommagées vraiment différentes | À faire |
| U3 | Petits décors urbains : murs, débris, bureau renversé, poutres ; taille minimale et contraste sur le sol des ruines | À faire |
| U4 | Orphelins urbains : proposition à Raphaël avant toute suppression | À faire |

Forêt, champs (haies, troncs, maisons de ferme) et carrière ne sont pas commandés ; ils profiteront du kit et des façades.

## 3. Compte rendu M0–M1 — marais

**Méthode.** Chaque décor est recomposé pour la taille réelle : 1 m ≈ 17 px, un personnage ≈ 30 px. Un aperçu parallèle (hors dépôt) rend chaque décor sur les trois sols du marais (sol, vase, eau) à ×2 comme en jeu, et isolé à ×5 pour juger le détail ; plusieurs passes par décor.

**Règles qui en sont sorties, valables pour les autres biomes :**

- *Peu d'éléments, espacés.* Sous 25 px, une touffe dense devient un pavé : les massettes passent de 12 tiges et 16 lames serrées à 5 hampes et 7 lames qui s'arquent vers l'extérieur, espacées d'au moins deux pixels.
- *La valeur avant la couleur.* Le sol du marais est sombre et moucheté : un décor s'y lit par du clair (bois blanchi, os, lichen, couvercles éclairés), pas par son contour. Racines et poteaux passent du brun-noir au bois blanchi.
- *Pied mouillé.* Le bas du bois s'assombrit sur une hauteur irrégulière (`_wet_foot`) : l'objet sort de la vase au lieu d'y être posé.
- *Liseré d'eau.* Ce qui est noyé (tronc, barque, ponton, pieux, oratoire, roues) porte un anneau de clapot argenté en pointillés à la ligne d'eau.
- *Détails peints, pas sculptés.* Rainures d'écorce, coulures de rouille, joints de pierre et veinage des planches sont des matières peintes sur la surface (`_surface.painted`) : la silhouette ne bouge pas, le détail ne coûte que là où il se voit.
- *Ombrage doux des brins.* Barbes de lichen, lames et lianes ont des rampes peu contrastées : ombrées à 4 tons, des cylindres fins se lisent comme des tuyaux.
- *Composer face à l'écran.* Les décors composés (touffes, arbres, fûts, pieux) sont modélisés de face (`FRONT`) ; les objets construits gardent un trois-quarts (`THREE_QUARTER`). En trois-quarts à 62°, la loge, la niche ou le fond d'un fût se retrouvaient de profil.

**Décor par décor :**

| Décor | Avant | Après |
|---|---|---|
| Arbre mort (tronc + canopée) | fût lisse, barbes jaunes en stalactites | contreforts, écorce rainurée, loge sombre, polypores, lichen ; barbes d'usnée gris-vert effilochées, longueurs inégales |
| Arbre mort moussu | fût et gaine verte uniforme | fût étêté en échardes, gaine de mousse côté ombre, polypores, barbes basses |
| Chicot | défense pâle de 2 px | fût brisé en échardes, moignon, écorce par plaques, polypores, touffes de mousse |
| Palétuviers (×2) | racines noires invisibles sur la vase | arceaux de bois clair d'épaisseurs variées, couronne en deux tons, racines-échasses pendantes ; la variante torse penche et a perdu la moitié de sa couronne |
| Chablis | boule de racines | galette de terre dressée face au chemin, racines claires qui rayonnent, pierres, mousse au sommet, tronc couché, trou rempli d'eau |
| Arbre lié | cerceaux orange | chaînes de fer en hélice et trois haubans vers des pieux, rubans délavés, un os pendu |
| Rideau de lianes | tuyau courbé | deux chicots, branche affaissée, treize lianes feuillues, quelques fleurs mauves |
| Mousse pendante | barre grise | chicot penché drapé de barbes en deux tons, silhouette qu'on prend pour quelqu'un |
| Roseaux | touffe brune de 16 px | massettes lisibles (voir règles) |
| Nénuphars | trait vert | sept feuilles disjointes entaillées, fleur pâle au cœur doré, bouton mauve ; reste un décalque au sol |
| Champignons toxiques | amas violet | quatre chapeaux de tailles franches, rebord lilas, spores lumineuses |
| Tapis de spores | amas | mousse, vesses-de-loup, trois hampes et quelques spores en l'air qui luisent |
| Tronc tombé | cylindre blanc | bout scié creux à cernes, bout cassé en échardes, écorce par plaques, mousse sur le dessus, polypores, clapot |
| Souche pourrie | correcte | couronne déchiquetée, eau noire dedans, polypores en escalier, échardes |
| Tronc englouti | « baleine » blanche | échine moussue, branches cassées, souche et racines au bout, clapot |
| Caillebotis | sombre | planches en deux tons veinées, longerons, travée affaissée, cordes aux pieux, clapot |
| Ponton effondré | sombre | plancher basculé en deux tons, pieux inégaux, bidon peint rouillé, cordage, clapot |
| Barque | correcte | coque à clins, plat-bord clair, banc, eau noire avec reflet, aviron, clapot |
| Charrette | brouette | roues à rayons cerclées dont une enfoncée, plateau de planches, brancards levés, sac |
| Fûts rouillés | tache | un fût debout au couvercle lisible, un couché au fond éclairé, coulures de rouille, nappe irisée |
| Pieux d'amarrage | piquet de 2 px | deux pieux fendus de hauteurs inégales, corde entre eux, lichen, clapot |
| Ossements | crâne et quelques os | cage de côtes dressée, échine, crâne cornu, fémur |
| Oratoire noyé | bloc gris | socle en assises, fût, niche voûtée et sa figure pâle, toit à deux pans moussu, bougie verte, rubans et fioles |
| Lanterne de passeur | perche et 2 px de lueur | perche tordue à potence, lanterne à cage grossie d'un tiers |

## 4. Vérifications M1–M2

- **En jeu.** Nouveau mode de capture `--capture-prop-gallery <biome>` (`tools/tests/RunObservation.PropGallery.cs`) : pour chaque décor distinct du biome, l'exemplaire le plus dégagé, le joueur à côté, créatures et brouillard retirés ; un PNG par décor. Seed 1002 : 41 sprites cadrés dans le marais (dont les décors des biomes voisins aux lisières). Les arbres, palétuviers, charrette, tronc et touffes s'intègrent au sol et au brouillard du marais ; corrections faites après la galerie : boue de la charrette allégée, chaînes de l'arbre lié épaissies (les maillons dessinés un à un faisaient du bruit, l'arbre se lisait moins « lié » qu'avant), fûts passés du teal (fondu dans le sol vert-bleu) à l'ocre passé, mousse du tronc englouti et de l'arbre moussu ramenée à l'échine et au flanc.
- **Mode `--capture-props`.** Il n'écrivait pas de ligne `RESULT`, donc `capture_run.sh` sortait en échec après une capture réussie : ligne ajoutée.
- **Seuils du jeu.** Nénuphars à 12 px de haut, toujours un décalque au sol (seuil `ground_decal_max_height`) ; roseaux à 28 px, toujours plient au passage (≤ 40) ; manifeste régénéré (pivots, emprises).
- **Reproductibilité.** Régénération complète du biome comparée octet par octet à la précédente : identique (PNG et manifeste). `dotnet build` sans avertissement.
- **Planche avant/après** (hors dépôt) : ancien sprite de `HEAD` et nouveau côte à côte, sur le sol du marais.
