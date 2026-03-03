# VESTIGES — Plan de production des tiles (Aseprite)

> **Objectif :** produire directement les tiles dans Aseprite, en pixel art isométrique, sans génération par IA.
> **Format de base :** tile losange 64×32 (ratio 2:1), sans anti-aliasing, palette limitée selon biome.

---

## Règles de production

- Travailler avec la palette biome correspondante (voir `assets/palettes/`).
- Garder des bords nets (pas de dégradé lissé, pas d'anti-aliasing).
- Limiter le nombre de teintes par asset pour préserver la lisibilité en jeu.
- Favoriser les variations de silhouette et de matière (pas seulement un shift de teinte).
- Pour les animations, commencer par 2 frames lisibles avant d'ajouter du détail.

### Pourquoi il y a des `v1`, `v2`, `v3` ?

Les versions `v1/v2/v3` sont des **variantes visuelles du même type de tile**.

Concrètement, elles servent à :

- **Casser la répétition** : si une map place 50 fois le même sol, l'œil voit un motif artificiel.
- **Rendre le terrain vivant** : micro-changements de fissures, mousse, cailloux, traces.
- **Améliorer la lecture gameplay** sans changer la fonction : `v1`, `v2`, `v3` restent le même "sol" côté design.
- **Faciliter l'autotiling/placement aléatoire** : le jeu peut piocher une variante au hasard et éviter l'effet damier.

### Comment créer de bonnes variantes (méthode simple)

1. Faire une `v1` propre et lisible (version de référence).
2. Dupliquer en `v2`, puis modifier **20 à 30 %** des détails :
	- déplacer 2–3 éléments (cailloux, feuilles, taches),
	- changer l'orientation d'une fissure/racine,
	- varier légèrement la répartition clair/foncé.
3. Dupliquer en `v3`, puis changer encore la distribution des masses secondaires.
4. Garder **la même silhouette globale** du tile (même catégorie visuelle), sinon on lit un autre matériau.

### Ce qu'il faut éviter

- Faire `v2/v3` uniquement en recolorant `v1`.
- Changer trop fort le contraste au point d'avoir une variante qui saute aux yeux.
- Ajouter un détail "unique" énorme sur une seule variante (ça crée un motif repérable en boucle).

### Règle rapide de validation

Place `v1/v2/v3` en damier sur une zone 6×6 dans Godot :

- si tu vois un motif répétitif immédiat → pas assez de variation,
- si une case attire trop l'œil → variation trop forte,
- si tout se lit comme le même matériau naturel → c'est bon.

---

## PRIORITÉ 0 — MVP (à faire en premier)

### 1. Sol forêt — herbe + mousse (3 variants)
**Fichier :** `assets/tiles/foret/tile_foret_sol_base.png` ✅ FAIT  
**Fichier :** `assets/tiles/foret/tile_foret_sol_v2.png`  
**Fichier :** `assets/tiles/foret/tile_foret_sol_v3.png`  
**Direction visuelle :** sol forestier sombre, mousse verte sur terre brune, petites feuilles éparses.

### 2. Sol forêt — terre + racines (2 variants)
**Fichier :** `assets/tiles/foret/tile_foret_terre_v1.png`  
**Fichier :** `assets/tiles/foret/tile_foret_terre_v2.png`  
**Direction visuelle :** terre compacte brun foncé, racines apparentes qui traversent le losange, quelques cailloux.

### 3. Sol béton fissuré — ruines (3 variants)
**Fichier :** `assets/tiles/ruines/tile_ruines_sol_v1.png`  
**Fichier :** `assets/tiles/ruines/tile_ruines_sol_v2.png`  
**Fichier :** `assets/tiles/ruines/tile_ruines_sol_v3.png`  
**Direction visuelle :** dalle grise fissurée, végétation qui reprend, ambiance urbaine abandonnée.

### 4. Sol boueux — marécages (3 variants)
**Fichier :** `assets/tiles/marecages/tile_marecages_sol_v1.png`  
**Fichier :** `assets/tiles/marecages/tile_marecages_sol_v2.png`  
**Fichier :** `assets/tiles/marecages/tile_marecages_sol_v3.png`  
**Direction visuelle :** boue humide sombre, petites flaques, végétation en décomposition.

### 5. Sol roche brute — carrière (3 variants)
**Fichier :** `assets/tiles/carriere/tile_carriere_sol_v1.png`  
**Fichier :** `assets/tiles/carriere/tile_carriere_sol_v2.png`  
**Fichier :** `assets/tiles/carriere/tile_carriere_sol_v3.png`  
**Direction visuelle :** roche gris-brun, veines minérales, gravats industriels, touches de terre rouge ferrugineuse.

### 6. Sol prairie herbeuse — champs (3 variants)
**Fichier :** `assets/tiles/champs/tile_champs_sol_v1.png`  
**Fichier :** `assets/tiles/champs/tile_champs_sol_v2.png`  
**Fichier :** `assets/tiles/champs/tile_champs_sol_v3.png`  
**Direction visuelle :** herbe verte et dorée, prairie ouverte, micro-fleurs discrètes.

### 7. Chemin/route (4 variants : droit, courbe, T, croix)
**Fichier :** `assets/tiles/commun/tile_chemin_droit.png`  
**Fichier :** `assets/tiles/commun/tile_chemin_courbe.png`  
**Fichier :** `assets/tiles/commun/tile_chemin_T.png`  
**Fichier :** `assets/tiles/commun/tile_chemin_croix.png`  
**Direction visuelle :** bande de terre tassée plus claire, herbe latérale, usure visible au centre.

### 8. Arbre géant — base + canopée
**Fichier :** `assets/sprites/foyer/arbre_geant_base.png` (32×48)  
**Fichier :** `assets/sprites/foyer/arbre_geant_canopee.png` (64×48)  
**Direction visuelle :** tronc massif ancien, écorce sombre moussue, racines épaisses ; canopée dense lisible en vue iso.

### 9. Arbre moyen (3 variants)
**Fichier :** `assets/tiles/foret/arbre_moyen_v1.png` (16×32)  
**Fichier :** `assets/tiles/foret/arbre_moyen_v2.png`  
**Fichier :** `assets/tiles/foret/arbre_moyen_v3.png`  
**Direction visuelle :** petit tronc + houppier rond, silhouette simple et claire, teintes forêt assombries.

### 10. Mur béton debout — ruines (3 variants)
**Fichier :** `assets/tiles/ruines/mur_beton_intact.png` (32×32)  
**Fichier :** `assets/tiles/ruines/mur_beton_fissure.png`  
**Fichier :** `assets/tiles/ruines/mur_beton_effondre.png`  
**Direction visuelle :** segment de mur en béton, fissures progressives, armatures apparentes sur les versions dégradées.

### 11. Paroi rocheuse — carrière (3 variants)
**Fichier :** `assets/tiles/carriere/paroi_v1.png` (32×48)  
**Fichier :** `assets/tiles/carriere/paroi_v2.png`  
**Fichier :** `assets/tiles/carriere/paroi_v3.png`  
**Direction visuelle :** falaise stratifiée, minéraux rouges, éventuelle veine de cristal bleutée.

---

## PRIORITÉ 1 — Core loop complet

### 12. Eau peu profonde (2 variants + 2 frames anim)
**Fichier :** `assets/tiles/commun/eau_shallow_v1_f1.png`  
**Fichier :** `assets/tiles/commun/eau_shallow_v1_f2.png`  
**Direction visuelle :** eau translucide laissant voir le fond, petites rides, reflet discret.

### 13. Eau profonde (2 variants + 2 frames anim)
**Fichier :** `assets/tiles/commun/eau_deep_v1_f1.png`  
**Fichier :** `assets/tiles/commun/eau_deep_v1_f2.png`  
**Direction visuelle :** eau opaque sombre, lecture immédiate de profondeur et de danger.

### 14. Sol transition biome (2 par combo)
**Fichier :** `assets/tiles/commun/transition_foret_ruines.png`  
**Fichier :** `assets/tiles/commun/transition_foret_marecages.png`  
**Direction visuelle :** fondu matière/couleur entre biomes, frontière lisible mais naturelle.

### 15. Autoroute fissurée envahie (4 variants)
**Fichier :** `assets/tiles/foret/autoroute_v1.png`  
**Direction visuelle :** asphalte craquelé, marquages effacés, herbes et mousse invasives.

### 16. Sous-bois dense (2 variants)
**Fichier :** `assets/tiles/foret/sous_bois_v1.png`  
**Direction visuelle :** litière dense (feuilles, mousse, champignons), ambiance ombrée.

### 17. Mur ruiné + lierre (3 variants, 32×32)
**Fichier :** `assets/tiles/foret/mur_lierre_v1.png`  
**Direction visuelle :** mur cassé repris par le végétal, contraste pierre grise / lierre vert.

### 18. Buisson (3 variants)
**Fichier :** `assets/tiles/foret/buisson_v1.png` (16×12)  
**Direction visuelle :** buisson compact, silhouette ronde, 2–3 niveaux de verts.

### 19. Rocher mousse (2 variants)
**Fichier :** `assets/tiles/foret/rocher_mousse_v1.png` (16×12)  
**Direction visuelle :** petit bloc rocheux gris avec plaques de mousse.

### 20. Sol intérieur carrelage cassé (2 variants)
**Fichier :** `assets/tiles/ruines/sol_carrelage_v1.png`  
**Direction visuelle :** carrelage alterné usé, fissures et carreaux manquants, poussière.

### 21. Trottoir défoncé (2 variants)
**Fichier :** `assets/tiles/ruines/trottoir_v1.png`  
**Direction visuelle :** bordure urbaine cassée, dalles disjointes, terre et mauvaises herbes.

### 22. Poutrelle acier (2 variants)
**Fichier :** `assets/tiles/ruines/poutrelle_h.png` (32×8)  
**Fichier :** `assets/tiles/ruines/poutrelle_diag.png`  
**Direction visuelle :** poutrelle métallique rouillée au sol, volume lisible en iso.

### 23. Débris béton (3 variants)
**Fichier :** `assets/tiles/ruines/debris_v1.png` (16×8)  
**Direction visuelle :** amas de gravats, morceaux irréguliers, tiges métalliques visibles.

### 24. Sol mousse humide marécages (2 variants)
**Fichier :** `assets/tiles/marecages/sol_mousse_v1.png`  
**Direction visuelle :** sol spongieux sombre, mousse vive toxique, suintements d'eau.

### 25. Eau marécageuse laiteuse (2 variants + anim)
**Fichier :** `assets/tiles/marecages/eau_marecage_v1_f1.png`  
**Direction visuelle :** surface trouble vert-brun, débris flottants, stagnation inquiétante.

### 26. Arbre mort blanchi (3 variants, 16×32)
**Fichier :** `assets/tiles/marecages/arbre_mort_v1.png`  
**Direction visuelle :** tronc pâle sans feuilles, branches sèches, présence fantomatique.

### 27. Racine aérienne (2 variants, 16×16)
**Fichier :** `assets/tiles/marecages/racine_v1.png`  
**Direction visuelle :** racines torsadées émergentes, style mangrove.

### 28. Brume au sol marécages (3 frames anim)
**Fichier :** `assets/tiles/marecages/brume_f1.png` (32×8)  
**Direction visuelle :** liseré de brume basse semi-transparente, animation très douce.

### 29. Sol terre rouge carrière (2 variants)
**Fichier :** `assets/tiles/carriere/sol_terre_rouge_v1.png`  
**Direction visuelle :** terre sèche rouge-brun, dépôts minéraux, petits éclats de pierre.

### 30. Rails de mine (2 variants)
**Fichier :** `assets/tiles/carriere/rails_droit.png`  
**Fichier :** `assets/tiles/carriere/rails_courbe.png`  
**Direction visuelle :** rails rouillés sur traverses bois, intégrés au sol rocheux.

### 31. Étai bois soutènement (2 variants, 16×24)
**Fichier :** `assets/tiles/carriere/etai_v1.png`  
**Direction visuelle :** structure de support en bois, usée mais lisible, contraste avec paroi.

### 32. Veine de cristal Essence (3 tailles, 16×16)
**Fichier :** `assets/tiles/carriere/cristal_petit.png`  
**Fichier :** `assets/tiles/carriere/cristal_moyen.png`  
**Fichier :** `assets/tiles/carriere/cristal_gros.png`  
**Direction visuelle :** cristaux cyan lumineux incrustés dans roche sombre.

### 33. Éboulis pierres (3 variants, 16×12)
**Fichier :** `assets/tiles/carriere/eboulis_v1.png`  
**Direction visuelle :** petit amas de pierres tombées, silhouette irrégulière.

### 34. Sol terre sèche champs (2 variants)
**Fichier :** `assets/tiles/champs/sol_terre_seche_v1.png`  
**Direction visuelle :** terre craquelée brune, aspect agricole abandonné.

### 35. Herbes hautes (3 variants + 2 frames vent)
**Fichier :** `assets/tiles/champs/herbe_haute_v1_f1.png` (16×16)  
**Direction visuelle :** touffes hautes dorées, animation latérale légère (effet vent).

### 36. Muret pierre bas (2 variants, 32×12)
**Fichier :** `assets/tiles/champs/muret_intact.png`  
**Fichier :** `assets/tiles/champs/muret_ecroule.png`  
**Direction visuelle :** muret rural en pierres sèches, version intacte puis affaissée.

### 37. Arbre solitaire champs (2 variants, 16×32)
**Fichier :** `assets/tiles/champs/arbre_solitaire_v1.png`  
**Direction visuelle :** arbre isolé à silhouette marquée, couronne sculptée par le vent.

### 38. Dissolution bord de map (3 niveaux)
**Fichier :** `assets/tiles/commun/dissolution_25.png`  
**Fichier :** `assets/tiles/commun/dissolution_50.png`  
**Fichier :** `assets/tiles/commun/dissolution_75.png`  
**Direction visuelle :** disparition progressive par dithering vers le vide.

### 39. Brouillard de guerre (3 frames anim)
**Fichier :** `assets/tiles/commun/fog_f1.png`  
**Fichier :** `assets/tiles/commun/fog_f2.png`  
**Fichier :** `assets/tiles/commun/fog_f3.png`  
**Direction visuelle :** voile brumeux clair, semi-transparent, lecture nette des zones inexplorées.

---

## PRIORITÉ 2 — Polish (plus tard)

### Forêt
| # | Asset | Direction visuelle |
|---|-------|--------------------|
| 40 | Clairière fleurie | Tache lumineuse avec herbes claires et fleurs sauvages. |
| 41 | Voiture envahie | Carcasse rouillée recouverte de lierre et mousse. |
| 42 | Maison effondrée | Fondations + pans de mur brisés repris par la nature. |
| 43 | Panneau routier | Signal tordu, rouille, texte illisible. |
| 44 | Lampadaire cassé | Mât incliné, tête brisée, végétation grimpante. |
| 45 | Pont fissuré | Segment de pont cassé, pierre ou béton éclaté. |
| 46 | Souche | Souche ancienne avec mousse et champignons. |

### Ruines
| # | Asset | Direction visuelle |
|---|-------|--------------------|
| 47 | Mur brique exposé | Enduit tombé laissant briques apparentes. |
| 48 | Escalier effondré | Marches cassées, gravats en pied d'escalier. |
| 49 | Bureau renversé | Mobilier retourné, papiers épars, poussière. |
| 50 | Poubelle conteneur | Bac métallique cabossé et rouillé. |
| 51 | Voiture urbaine | Véhicule abandonné, pneus à plat, vitres ternes. |
| 52 | Feu signalisation | Bloc de feu au sol, verre brisé. |
| 53 | Clôture grillagée | Grillage tordu avec sections arrachées. |

### Marécages
| # | Asset | Direction visuelle |
|---|-------|--------------------|
| 54 | Nénuphar | Feuille plate sombre flottante sur eau trouble. |
| 55 | Souche dans l'eau | Souche pourrie partiellement immergée. |
| 56 | Champignon toxique | Grappe de champignons aux teintes d'alerte. |
| 57 | Pierre affleurante | Pierre plate émergente servant de pas. |

### Carrière
| # | Asset | Direction visuelle |
|---|-------|--------------------|
| 58 | Wagon mine renversé | Wagon rouillé couché, roches déversées. |
| 59 | Machine rouillée | Équipement industriel abandonné, masses mécaniques lisibles. |
| 60 | Gouffre sombre | Ouverture noire profonde avec bord irrégulier. |

### Champs
| # | Asset | Direction visuelle |
|---|-------|--------------------|
| 61 | Coquelicots | Petites touches rouges contrastées dans l'herbe. |
| 62 | Clôture bois | Poteaux + traverses en bois usé. |
| 63 | Meule de foin | Masse de paille dégradée, silhouette tassée. |
| 64 | Flaque d'eau | Petite flaque réfléchissante sur sol herbeux. |

---

## Workflow Aseprite (recommandé)

1. Créer le canvas à la bonne taille (64×32 par défaut, sinon taille indiquée).
2. Charger la palette du biome concerné.
3. Bloquer les masses principales (sol, volume, silhouette).
4. Ajouter les détails lisibles en jeu (fissures, mousse, débris, racines, etc.).
5. Vérifier la lecture en zoom 100 % et en contexte iso.
6. Sauvegarder le source `.aseprite` (dossier source) puis exporter le `.png` dans le repo.

Pour les variants : faire la v1, dupliquer, puis modifier matière/silhouette/détails (pas juste la couleur). Pour les anims : décaler légèrement la forme ou les reflets entre frames pour garder un mouvement discret.

### Mini workflow spécial `v1 → v2 → v3`

1. `v1` = version "propre" (la référence).
2. `v2` = même tile, détails déplacés (fissures/mousse/cailloux).
3. `v3` = même tile, autre rythme de détails (zones plus calmes vs plus chargées).
4. Test rapide en grille dans Godot (ou mockup Aseprite) avant export final.

Objectif : qu'un joueur ne remarque jamais le "copier-coller" des sols pendant l'exploration.
