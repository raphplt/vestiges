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

## R5 — Particules plus riches (≈ 1 j)

Constat : « un peu trop simples » ; en ville, « juste des petits carrés qui bougent ».

- Petites planches animées : feuille qui virevolte (4 poses), papier qui se retourne, flocon de cendre. Pas de rotation libre, qui casserait la grille des pixels.
- Ville : remplacer les papiers par ce que la Bible décrit (§7.3, cendres et poussière qui flottent dans les rayons de lumière), ou retirer. À trancher.
- Vérification : captures en mouvement (séquence courte), réglage des particules respecté.

## R6 — Personnage par défaut (≈ 0,2 j)

« Peut-être que je ferai en sorte que ce soit le Traqueur le personnage par défaut… à voir. » Si confirmé : Traqueur sélectionné par défaut sur un profil neuf, et premier de la liste.
