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
| **U1** | Immeubles urbains : matières des façades (enduit, brique, salissures, coulures, fissures), toits lisibles (gravier, édicules, bacs), lierre et mousse refaits en vrai feuillage | Livré, voir §5 |
| **U2** | Ruines : effondrements remplis de gravats et de planchers éventrés, variantes endommagées vraiment différentes | Livré, voir §5 |
| **U3** | Petits décors urbains : murs, débris, bureau renversé, poutres ; taille minimale et contraste sur le sol des ruines | Livré, voir §5 |
| **U4** | Orphelins urbains : les trois murs supprimés avec leurs `.import`, accord de Raphaël | Livré |
| **F1** | Forêt : les 18 modèles repris (23 PNG), arbres partagés avec les champs et la ferme ; galerie en jeu | Livré, voir §6 |
| **C1** | Champs et ferme : maisons de ferme (façades du kit urbain), grange endommagée vraiment différente, haies, verger, petits décors des champs | Livré, voir §7 |
| **Q1** | Carrière : roches, cristaux, panneau de danger, vestiaire, machines, petits décors lisibles | Livré, voir §8 |

Forêt, champs et carrière ont été demandés après les lots urbains ([DECISIONS §84](DECISIONS.md)) : « je crois que sur les autres il y a moins de choses à revoir », d'où une reprise plus légère, un lot par biome.

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

## 5. Compte rendu U1–U3 — urbain

**Deux défauts du pipeline commun, trouvés en regardant les toits.**

- *Toits noirs.* Sur un plan horizontal vu à 30°, deux pixels voisins diffèrent de 2,8 unités de profondeur, plus que le seuil de ligne interne (2,6) : chaque pixel d'un toit plat ou d'un plateau était dessiné comme une ligne interne sombre. C'est ce qui faisait les « toits sombres » de l'audit. `PropModel(smooth_slopes=True)` ne trace plus une ligne que si l'écart rompt la pente du pixel opposé. Désactivé par défaut : les sprites validés des autres biomes ne changent pas tant qu'ils ne sont pas repris ; sur le marais, l'écart est négligeable (vérifié), il n'est donc pas régénéré.
- *Détails plus fins qu'un pixel.* Les petites marches d'un gravier peint faussaient les normales : `Part(relief=False)` colore sans compter dans l'éclairage.
- Le parapet des toits plats avait un fond plein qui cachait la membrane : gravier, mousse et flaques étaient peints dessous.

**Immeubles (U1).** Façades : enduit taché en grandes plages, coulures sous la corniche, pied sali par l'humidité, briques à nu là où l'enduit est tombé (plus il y a de dégâts, plus il y en a), fissures, soubassement de pierre appareillée. Fenêtres : vitres sombres avec reflet en diagonale, vitres brisées, fenêtres condamnées, encadrements, linteaux, volets à lames sur les maisons et quelques immeubles, balcons à garde-corps sur les étages. Toits plats : membrane gravillonnée, mousse et flaques, édicule d'escalier avec sa porte, souches de cheminée en brique, ventilations, antenne ; toits de tuiles en rangs, cheminée. Descente d'eau en zinc, seuil et encadrement de porte. Boutiques : vitrine vitrée, rideau de fer à moitié baissé, enseigne aux lettres illisibles. Le lierre grimpe en deux ou trois tiges en zigzag chargées de petites grappes, au lieu de bâtons verts ; l'arbuste du toit pousse en touffe. Église : appareil de pierre de taille, pied sali, ardoises, même lierre.

**Ruines (U2).** L'effondrement est rongé par un bruit qui ne fait qu'agrandir la coupe : murs et planchers s'arrêtent en dents irrégulières, l'intérieur montre ses papiers peints rayés, un pan de plancher pend à son bord avec ses fers à béton, un tas de gravats en blocs comble le bas. Les gravats au pied des façades sont des tas bosselés couverts de moellons, plus des cubes isolés. Corniches et toiture reculent un peu plus que les murs, pour ne pas laisser d'éclats suspendus. Coût : mémoire de la coupe et bornes rapides du lierre et des gravats, une ruine passe d'environ 17 à 2–3 minutes de rendu.

**Petits décors (U3).** Gravats en trois variantes franches (dalle cassée et fers tordus, tas de démolition en briques, tronçon de poteau et son armature), grain de gravillons, poussière au sol. Bureau renversé désormais procédural (il était hors manifeste). Voitures : rouille en coulures, mousse posée sur le toit et le capot au lieu de boules, joints de portières, reflet sur les vitres, enjoliveurs. Poutres : rouille peinte le long de l'âme.

**Vérifications U1–U3.** Planches de génération ×2 sur le sol des ruines, rendus rapides ×3–×5 de chaque famille, galerie en jeu (`--capture-prop-gallery urban_ruins`, seed 1002, 38 sprites cadrés) regardée : façades, toits et ruines se lisent à l'échelle réelle, la transparence derrière les immeubles (`PropOcclusion`) est intacte. Hauteurs des modules inchangées à ±2 px (172–190 px, sous la limite d'un îlot) ; manifeste régénéré, le bureau renversé y entre. Régénération complète (immeubles et mobilier) comparée octet par octet : identique ; smoke test vert.

**U4 — question à Raphaël.** `prop_brick_wall`, `prop_concrete_wall` et `prop_concrete_wall_v3` ne sont référencés par aucun JSON ni placeur, et n'ont pas de modèle procédural. Proposition : les supprimer avec leurs `.import`. Accord de Raphaël le 10 octobre (« oui supprime les ») : supprimés, après avoir revérifié qu'aucune scène, aucun JSON ni aucun code ne cite leur nom ou leur uid.

## 6. Compte rendu F1 — forêt

**Kit commun.** Les formes végétales et le bois du marais (branches torses, rainures, polypores) passent dans `tools/sprites/props/_flora.py`, avec les touffes de la forêt : marais, forêt, champs, ferme, carrière et immeubles les importent de là au lieu de s'importer entre biomes (rendus inchangés, vérifié octet par octet). Trois ajouts :

- *Feuillage en grappes* (`_leaf_mass`) : chaque touffe est un cœur hérissé de petites boules ; chaque grappe prend sa lumière et se détache de ses voisines par une ligne interne, un bruit fin les froisse pour qu'aucune ne se lise en bulle. Il remplace les bosses sinusoïdales régulières, qui faisaient des chou-fleurs.
- *Peau du dessus* (`_top_skin`) : masque des faces tournées vers le ciel, pour poser la mousse sur le dessus d'un mur ou d'une carrosserie sans la placer à la main.
- *Borne rapide* (`_bounded`) : la règle du lierre urbain (distance à l'enveloppe loin d'elle, vraie forme près du bord), réutilisable.

**Arbres (tronc + canopée).** Le fût se tord un peu, s'évase en cinq contreforts et se divise en trois maîtresses branches et une flèche ; des rameaux vont chercher les touffes de la couronne. Les branches se voient sous la canopée et par ses trouées quand elle devient transparente. Écorce rainurée, mousse côté ombre au pied des grands chênes. Le bouleau garde sa flèche, son écorce blanche est barrée de lenticelles noires et son pied noirci ; sa couronne est haute et légère. Les arbres sont composés de face (`FRONT`) : la mousse et l'ombre tombent du bon côté. L'arbre isolé des champs et les deux arbres du verger viennent du même code : ils gagnent fourche et branches (défaut « troncs en poteaux » de l'audit) et sont régénérés avec ce lot ; leurs couleurs restent à revoir au lot C1.

**Décor par décor :**

| Décor | Avant | Après |
|---|---|---|
| Chênes (×2), jeune arbre, bouleau | poteau planté dans un chou-fleur | fourche, branches, contreforts, écorce rainurée ; feuillage en grappes en trois étages (ombre, cœur, lumière) |
| Arbre étranglé | poteau gris, boules vertes en spirale | fût étêté en échardes, branches mortes, trois tiges de lierre en hélice chargées de feuilles plaquées, pans qui pendent des branches |
| Buissons (×2) | boules vertes | pied de tiges sombres, grappes ombrées par-dessous ; la variante fleurie porte des grappes violettes à pointe pâle |
| Fougère | étoile plate | onze frondes en arc, peignes de folioles qui s'affinent, frondes du fond à l'ombre, deux crosses |
| Fleurs (×2) | taches | rosette de feuilles, tiges inégales, corolles tournées vers la caméra avec leur cœur, boutons |
| Champignons | une boule ocre | trois cèpes et deux girolles de tailles franches sur un lit de mousse et de feuilles mortes |
| Souche | cylindre brun | plateau scié à cernes et gerce, contreforts, écorce rainurée, mousse côté ombre, polypores, une pousse |
| Rochers moussus (×2) | cubes gris, galette verte | blocs bosselés et fendus, lichen, calotte de mousse qui coule sur les flancs, pied humide, herbe |
| Tronc couché | capsule brune, boules vertes | écorce rainurée, bois nu vers le bout cassé, bout scié à cernes, mousse épaisse, moignons, polypores |
| Mur en ruine | dalle brune, taches vert sombre | redans de briques à joints, enduit resté par plaques, soubassement de pierre, mousse sur les redans, lierre en tiges, briques tombées, herbe |
| Réverbère | perche et une boule | candélabre de fonte vert sombre, socle mouluré, crosse, lanterne à vitres dont une brisée, coulures de rouille, lierre jusqu'à mi-fût |
| Panneau rouillé | losange jaune de profil | triangle de danger face à la caméra : bord rouge, fond blanc passé, pictogramme, rouille en plaques et coulures, herbe au pied |
| Voiture envahie | boules vertes, sucette | mousse peinte sur le toit et le capot, lierre en chapelets le long des portières, herbes aux roues, jeune arbre en grappes qui perce le toit |

**Vérifications F1.** Aperçus ×2 sur les trois sols de la forêt (sol, sous-bois, terre) et ×5 isolés, quatre passes ; galerie en jeu (`--capture-prop-gallery forest_reclaimed`, seed 1002, 32 sprites cadrés) regardée : arbres à fourche et bouleaux se détachent du sous-bois, petits décors lisibles à côté du personnage. Réverbère, panneau, mur et voiture ne se posent que sur le béton de la forêt, absent des seeds 1002, 7 et 42 : vus en planche seulement. Tailles gardées dans leurs catégories (fleurs et champignons au-dessus du seuil de décalque comme avant, buisson et fougère sous le seuil de piétinement, souche toujours bloquante). Régénération de la forêt comparée octet par octet : identique ; marais régénéré après le passage de ses aides dans `_flora.py` : aucun fichier modifié.

## 7. Compte rendu C1 — champs et ferme

**Kit.** Les touffes d'herbe en brins (`_grass`) passent dans `_flora.py` et remplacent partout les boules vertes posées au pied des objets (tracteurs, silos, abreuvoir, pique-nique, tracteur embourbé). `_top_skin` ne prend plus les faces verticales qu'on longe en montant : la mousse du dessus restait accrochée aux flancs (mur en ruine et voiture de la forêt régénérés, plus nets). `tree` gagne deux réglages : `cluster` plafonne la taille des grappes d'une grande couronne (au-delà de ~6 px, une grappe à peine sortie de son cœur se lisait en œil sur l'arbre isolé), `blossom` sème la floraison sur toute la couronne au lieu d'un chapeau rose.

**Maisons de ferme.** Elles sortent du même `building` que les immeubles : leurs PNG n'avaient pas été régénérés depuis U1. Ils le sont : enduit taché, coulures, briques à nu, volets, toit de tuiles moussu, lierre.

**Décor par décor :**

| Décor | Avant | Après |
|---|---|---|
| Arbre isolé | grappes en « œil » | grappes plafonnées, ombre plus franche |
| Pommier en fleurs | chapeau rose posé sur le vert | fleurs semées sur toute la couronne |
| Herbes hautes (×3) | pavés de bâtons | quinze brins fins en éventail, pied sombre, pointe claire ; épis pour le blé sauvage |
| Fleurs des champs (×3) | taches | rosette, tiges, corolles tournées vers la caméra : coquelicots à cœur noir, bleuets, marguerites |
| Murets (×3) | blocs lisses gris et gris foncé | moellons bosselés sur deux assises et couvertine de chant, lichen, mousse sur le dessus, herbe au pied ; pierres roulées sous la trouée |
| Clôtures (×4) | bâtons lisses | bois veiné dans le sens du fil, lichen, herbe au pied des piquets |
| Balles de foin | cylindre doré, boule verte | brins clairs, filet de liage, pied humide ; la pourrie brunit, s'affaisse, se couvre de mousse et d'herbe |
| Épouvantail | de profil | de face : bras qui a lâché, veste rapiécée, paille aux manches et au col, chapeau mou |
| Puits | margelle lisse | moellons appareillés, mousse, montants veinés, toit de planches moussu, corde et seau |
| Menhir | œuf gris à points jaunes | pierre bosselée et fendue, lichens jaune pâle et gris, mousse au pied |
| Charrue | barre brune illisible | poutre, trois versoirs d'acier décalés au bord usé clair, roue de jauge, rouille, herbe |
| Tracteurs (×2) | blocs | calandre, garde-boue, toit de cabine, crampons, rouille en coulures |
| Éolienne | pales en boules | lames minces vrillées, rouille par plaques, herbe aux pieds |
| Silos (×2) | cylindres lisses, coulures en bâtons | tôle ondulée, coulures sous les cerclages ; le silo effondré a un sommet arraché en dents de scie et une déchirure |
| Granges (×3) | rayures de sucre d'orge, toit à taches | planches jointes et veinées, peinture écaillée sur le bois gris, pied sali, rouille en coulures, mousse au bas des pans ; l'endommagée a le toit crevé sur ses chevrons, des planches arrachées, un battant tombé devant et la paille répandue |
| Hangars (×2) | toit à taches | rouille dans le sens de la pente, poteaux veinés, paille piquée de brins clairs |
| Haies (×4) | boudins vert sombre | grappes sombres au pied, plus claires au sommet ; aubépine blanche semée sur les fleuries |

**Vérifications C1.** Aperçus ×2 sur trois sols des champs (herbe, blé, chaume) et ×5 isolés, plusieurs passes par famille ; planches de génération regardées ; galerie en jeu (`--capture-prop-gallery wild_fields`, seed 1002, 63 sprites cadrés, dont les fermes entières) : granges, maisons et hangars se lisent comme une ferme, la grange endommagée se distingue au premier coup d'œil, haies et petits décors se détachent de l'herbe. Décors inchangés (flaques, remorque, portails, poteau, linge, épouvantail aux corbeaux, arbres de verger en feuilles) : rendus identiques. Forêt : seuls le mur en ruine et la voiture changent (`_top_skin`), les arbres restent identiques. Régénération des champs et de la ferme comparée octet par octet : identique.

**Question à Raphaël.** `assets/props/wild_fields/_wild_fields_hero_props.png` est la planche d'un ancien générateur (`scripts/generate_wild_fields_props.py`), importée par Godot mais chargée nulle part. La supprimer avec son `.import` ? Rien n'est supprimé sans son accord. **Réponse le 10 octobre :** « Oui supprime la planche orpheline stp » ; supprimée. L'ancien générateur reste : relancé, il réécrirait sept décors de C1 (question au tableau de bord).

## 8. Compte rendu Q1 — carrière

**Roche.** Une même peau pour toutes les roches de la carrière (`_rock_skin`) : strates horizontales rompues, fissures d'un pixel, poussière sur les faces tournées vers le ciel ; affleurements, bloc éboulé, veine, pioche et front des galeries la reçoivent. Les blocs restent anguleux : ce sont des pierres taillées par la mine. Éclats et cailloux au pied deviennent des blocs à facettes au lieu de boules.

**Cristaux d'Essence.** Ils se lisaient comme une flamme bleue : des fuseaux émissifs, sans ombre, fondus ensemble. Ce sont maintenant des prismes hexagonaux à pointe, en éventail (le plus grand presque droit au centre), ombrés par leurs facettes ; seules les pointes luisent, et la roche autour prend un reflet bleu-vert.

**Décor par décor :**

| Décor | Avant | Après |
|---|---|---|
| Affleurements (×2), bloc éboulé | boîtes lisses | strates, fissures, poussière, cailloux à facettes |
| Tas de déblais et de minerai | cône à ondulations régulières | grain bosselé, gravillons sombres et clairs, pierres roulées |
| Veine et grappe de cristaux | flamme bleue | prismes facettés en éventail, pointes lumineuses, reflet sur la roche |
| Godet de pelleteuse | jouet jaune à rotules bleues | axes d'acier sombre, deux vérins à tige brillante, peinture écaillée, coulures de rouille, boue séchée dans le godet |
| Convoyeur | deux pieds flottaient sous le tapis | pieds rattachés au bâti incliné |
| Gyrophare | boule orange sur un fil | poteau rayé jaune et noir au pied, lanterne grillagée, boîtier rouillé |
| Vestiaire | boîte verte illisible | armoire debout, porte ouverte à aérations, bleu de travail pendu, photo, casque et thermos au pied |
| Caisse d'explosifs | caisse lisse | planches jointes, losange d'avertissement au pochoir, mèches qui dépassent |
| Panneau de danger | losange jaune de 8 px | panneau « danger » sur deux piquets : bandeau rouge, texte illisible, pictogramme d'éboulement, rouille aux bords, pierres au pied |
| Wagonnets | rouille en boules | rouille en coulures |
| Baraques | toit uni, rouille en bâtons | coulures sous le toit, pied sali, ondes du toit, reflet sur la vitre |

**Vérifications Q1.** Aperçus ×2 sur trois sols de la carrière (sol, roche, industriel) et ×5 isolés, trois passes ; planche de génération ; galerie en jeu (`--capture-prop-gallery collapsed_quarry`, seed 1002, 41 sprites cadrés) regardée : roches texturées sans virer au camouflage, cristaux lisibles et lumineux sur le sol sombre, panneau et vestiaire identifiables. Voies et rails, chariot renversé, touret et étais inchangés. Régénération comparée octet par octet : identique.

**Bilan du plan 31.** Les cinq biomes sont repris : marais (M0–M2), urbain (U1–U4), forêt (F1), champs et ferme (C1), carrière (Q1). Restent hors plan : coffres et lieux (notés 7/10 à l'audit, non demandés).
