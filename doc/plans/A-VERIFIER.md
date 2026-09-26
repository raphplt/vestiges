# À revérifier sur la machine de Raphaël

Fichier temporaire. Il liste ce qu'une session cloud a livré sans pouvoir le vérifier complètement. Le conteneur avait Godot 4.7.2 et .NET 10, mais pas de GPU : rendu logiciel à environ 5 FPS, et aucune mesure de FPS n'y a de sens. Le build, le smoke test, les régressions et les captures ont bien été faits. Chaque point ci-dessous est à cocher par le Claude local, ou par Raphaël en jeu, puis à retirer. Supprimer le fichier quand il est vide.

## Plan 10 T3 — chemins et routes

- [ ] **Coût GPU des chemins.** Environ 80 rubans maillés avec un shader par pixel (bruit, tramage, oubli). Lancer `tools/bench_ab.sh <commit d'avant T3> <dossier>` machine calme ; le banc se déroule dans les champs et la carrière, donc sur des chemins.
- [ ] **Temps de génération** : 257 ms de calcul CPU au chargement dans le conteneur (`[PathNetwork]` dans le log). Vérifier sur le Mac que l'écran de chargement ne s'allonge pas de façon sensible.
- [ ] **Recette visuelle en jeu** : largeur des chemins (champs 46 px au sol, forêt et marais 26 px, carrière 36 px), contraste des ornières, fréquence des chemins (un arbre couvrant plus 30 % de boucles). Les captures sont dézoomées ou au zoom ×2 ; le rendu à la taille réelle de l'écran de Raphaël peut différer.
- [ ] **Rues verticales** : une seule chaussée désormais (une tile par parité de rang). Vérifier les carrefours en T et en croix, et la position des voitures garées sur les rues verticales (recentrées de ±16 px).
- [ ] **Bordures de trottoir** : lisibles ou trop discrètes ?

## Plan 16 O6 — échos

- [ ] **Fréquence et lisibilité en vraie partie** : premier écho à 60 s, puis toutes les 45 à 90 s. Il n'apparaît qu'en zone Fragile ou Effilochée, donc pas avant que l'Effacement ait progressé. Trop rare, trop discret ?
- [ ] **Taille du murmure** : police par défaut, 9 px en monde (18 px à l'écran au zoom 2). Vérifier la lisibilité et la netteté à 1080p et en grand texte.
- [ ] **Langue** : les captures du conteneur affichent l'anglais (locale par défaut). Vérifier le français.

## Plan 08 P4b — fermes

- [ ] **Recette visuelle** : 16 fermes sur la seed de capture, toutes sur le même plan retourné. La répétition se voit-elle en jeu ? Faut-il des plans variés ou moins de fermes ?
- [ ] **Collisions** : maison, grange, silo, hangar, puits, tracteur et remorque bloquent (emprise du manifeste) ; clôtures, haies et portail ne bloquent pas. Vérifier avec `--capture-props` et en jeu qu'on ne reste pas coincé dans la cour ou derrière le hangar.
- [ ] **Coût** : environ 30 décors de plus par ferme, dont 3 à 4 grands sprites. Mesure `--measure-props` dans les champs à refaire machine calme. Placement : 273 ms au chargement dans le conteneur, à vérifier sur le Mac.
