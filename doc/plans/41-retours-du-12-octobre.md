# Plan 41 — Retours de partie du 12 octobre

Retours dictés par Raphaël après une partie sur les livraisons du plan 39 (atmosphère) et le HUD refait la veille ([DECISIONS §129](DECISIONS.md)). Lots proposés, un à la fois.

## R1 — Urgences (fait le 12 octobre)

- Voile du marais coupé (`swamp_veil.strength` à 0, `data/world/atmosphere.json`) : son ondulation de tout l'écran donnait la nausée. Le calque ne s'affiche plus et ne coûte rien ; l'étalonnage, les spores et la brume du marais restent.
- Ombres portées au pixel du monde par défaut (Paramètres › Ombres : Hautes). La clé enregistrée change (`cast_shadow_quality`) : une valeur « Moyennes » déjà sauvegardée ne s'applique plus.

## R2 — HUD en haut à droite (≈ 0,5 j)

Constat : mal aligné, éliminations trop petites, quatre couleurs qui se battent (score jaune, Essence bleue, Péril rouge, éliminations blanches).

- Une seule colonne alignée à droite, une valeur par ligne, même taille de chiffres ; libellés ou icônes discrets dans la couleur du texte.
- Une seule couleur d'accent (le score), le reste dans la teinte du texte ; le Péril ne prend sa couleur qu'en montant.
- Vérification : captures du HUD en run (`/capture`), avant/après.

**Livré le 12 octobre :** sous le score (seule valeur dorée, légende « SCORE » grise), une colonne de trois lignes, éliminations, Essence, Péril : icône de 10 px puis chiffre, tous à 15 px dans la teinte du texte, dans une case de 32 px alignée sur le bord droit du score. Les icônes restent en colonne quand un nombre s'allonge. Deux icônes ajoutées au générateur du HUD (`tools/sprites/ui/hud.py`, `tools/generate_hud_sprites.py`) : cristal d'Essence et stèle du Péril, sur le modèle du crâne ; les autres textures ressortent identiques à l'octet. Les mots « Essence » et « Péril » ne sont plus écrits (les clés `UI_HUD_ESSENCE` et `UI_HUD_PERIL` ne servent plus au HUD) ; le multiplicateur d'Essence (× 1,5) reste devant le chiffre. `PixelSkull` retiré (le crâne passe par la même ligne que les deux autres). Captures avant/après regardées ; build et smoke verts.

## R3 — Colonnes de lumière (≈ 1 j)

Constat : les blocs de lumière verticaux au-dessus des coffres et de certains lieux (Failles, Mémoriaux, Ateliers, fragments ; `World/LightColumn`, `light_column.gdshader`) datent d'avant les reprises visuelles et détonnent.

- Planche de trois propositions en pixel art, dans la direction crade (plan 40), avant intégration.
- Vérification : `--capture-chests` et les captures des lieux.

## R4 — Rectangle semi-transparent à la carrière (≈ 0,5 j)

Constat : autour d'un lieu à briser dans la mine, un rectangle semi-transparent au sol, hors de la direction artistique. Source non identifiée à la lecture (ni `SmallPlace`, ni le filon de cristal). À trouver par capture des lieux de la carrière, puis corriger.

**Livré le 12 octobre :** trouvé par `--capture-places`. C'est le repère d'interaction des petits lieux (`World/InteractableAura`), allumé quand le joueur est à portée : deux losanges vectoriels lisses (au sol et en couronne) qui respiraient en échelle, hors de la grille des pixels. Refait en pixel art : seuls les quatre coins d'un losange iso au sol, liserés d'un pixel comme des crochets de sélection, dans la couleur du lieu, plus un éclat de 2 px qui monte par pixels entiers ; la pulsation ne joue plus que sur l'opacité ; la couronne disparaît, l'invite « [E] Briser » suffit au-dessus. Un premier essai, losange rempli d'un damier, faisait filet : retiré. Captures du filon et de la boîte aux lettres regardées ; build et smoke verts.

## R5 — Particules plus riches (≈ 1 j)

Constat : « un peu trop simples » ; en ville, « juste des petits carrés qui bougent ».

- Petites planches animées : feuille qui virevolte (4 poses), papier qui se retourne, flocon de cendre. Pas de rotation libre, qui casserait la grille des pixels.
- Ville : remplacer les papiers par ce que la Bible décrit (§7.3, cendres et poussière qui flottent dans les rayons de lumière), ou retirer. À trancher.
- Vérification : captures en mouvement (séquence courte), réglage des particules respecté.

**Livré le 12 octobre :**

- Poses dessinées au pixel, que la particule parcourt pendant sa vie (planche horizontale, `CanvasItemMaterial` en animation de particules, pose de départ tirée au hasard) : feuille qui virevolte (à plat, en biais, de profil, en biais inverse), papier qui se retourne, flocon de cendre, pollen qui luit. Pas de rotation libre, qui casserait la grille des pixels. Teinte des données sur deux tons (plein, ombré).
- Nouvelle sorte `glint` : poussière qui flotte presque immobile et, une pose sur six, scintille en croix (Bible §7.3).
- `motes` devient une liste d'émetteurs par biome.
- **Ville, variante 1 (en place)** : 60 points de poussière qui scintillent et 8 papiers dessinés. **Variante 2** à essayer en remplaçant la liste `motes` de `data/biomes/urban_ruins.json` par `[{ "kind": "paper", "amount": 20, "color": [0.95, 0.93, 0.86], "opacity": 0.95 }]`.
- Outils : `--capture-biome-tour <s>` (arrêt immobile au cœur des décors de chaque biome, rafale de quatre images pleine résolution à 0,2 s) et `tools/record_biome_tour.sh` (même visite filmée au Movie Maker, `tour.mp4`). La vidéo réduite et compressée perd les particules d'un pixel ; les rafales servent à vérifier.
- Rafales regardées : feuilles qui changent de pose d'une image à l'autre en forêt, poussière pâle en ville (le scintillement est rare, comme voulu). `tools/test_catalogs.sh` vert (liste vide refusée), build et smoke verts.

## R6 — Personnage par défaut (≈ 0,2 j)

« Peut-être que je ferai en sorte que ce soit le Traqueur le personnage par défaut… à voir. » Si confirmé : Traqueur sélectionné par défaut sur un profil neuf, et premier de la liste.

**Livré le 12 octobre** ([DECISIONS §129 ter](DECISIONS.md)) : le Traqueur et l'Arc du gymnase sont libres dès un profil neuf ; « Mener la chasse » débloque désormais le Vagabond et sa Lame ébréchée. Le Traqueur, déjà premier de la liste, est le personnage sélectionné par défaut (premier débloqué). L'arc rejoint la réserve des armes de départ, la lame la quitte : 17 armes à gagner, comme avant. Personnage de repli (`GameBootstrap`, bande de marche du chargement) : le Traqueur. Régressions mises à jour : quêtes (profil neuf, réserve de départ, déblocage par la chasse), mode dev (le Vagabond sert d'exemple de verrou normal) ; quêtes, écran de choix et Campement verts.
