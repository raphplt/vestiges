# À revérifier sur la machine de Raphaël

Fichier temporaire. Il liste ce qu'une session cloud a livré sans pouvoir le vérifier complètement. Le conteneur avait Godot 4.7.2 et .NET 10, mais pas de GPU : rendu logiciel à environ 5 FPS, et aucune mesure de FPS n'y a de sens. Le build, le smoke test, les régressions et les captures ont bien été faits. Chaque point ci-dessous est à cocher par le Claude local, ou par Raphaël en jeu, puis à retirer. Supprimer le fichier quand il est vide.

## Plan 10 T3 — chemins et routes

- [ ] **Coût GPU des chemins.** Environ 80 rubans maillés avec un shader par pixel (bruit, tramage, oubli). Lancer `tools/bench_ab.sh <commit d'avant T3> <dossier>` machine calme ; le banc se déroule dans les champs et la carrière, donc sur des chemins.
- [ ] **Temps de génération** : 257 ms de calcul CPU au chargement dans le conteneur (`[PathNetwork]` dans le log). Vérifier sur le Mac que l'écran de chargement ne s'allonge pas de façon sensible.
- [ ] **Recette visuelle en jeu** : largeur des chemins (champs 46 px au sol, forêt et marais 26 px, carrière 36 px), contraste des ornières, fréquence des chemins (un arbre couvrant plus 30 % de boucles). Les captures sont dézoomées ou au zoom ×2 ; le rendu à la taille réelle de l'écran de Raphaël peut différer.
- [ ] **Rues verticales** : une seule chaussée désormais (une tile par parité de rang). Vérifier les carrefours en T et en croix, et la position des voitures garées sur les rues verticales (recentrées de ±16 px).
- [ ] **Bordures de trottoir** : lisibles ou trop discrètes ?
