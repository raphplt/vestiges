# Plan 16 — L'oubli rendu sensible

Statut : **proposé le 26 septembre 2026, à arbitrer** · Priorité : P1 (identité) · Dépendances : 10 (sol, jonctions), 02 (effets), 13 (butin menacé), 15 (audio).
Références : [Stratégie V2 §8](../VESTIGES-STRATEGIE-V2.md#8-leffacement--mécanique-centrale), Bible (désagrégation, blanc-bleuté du Néant).

## 1. Le retour

Raphaël, le 26 septembre : « la mécanique de l'oubli se voit finalement assez peu dans le jeu. Elle pourrait être plus exploitée, que ce soit sur la map visuellement ou autrement. »

## 2. Écart entre la V2 et le code

| Phase (mémoire) | Ce que prévoit la V2 §8 | Ce qui existe (`ErasureManager`, `ErasureOverlay`) |
|---|---|---|
| Ancrée (100–75 %) | Couleurs vives | Rien de particulier |
| Fragile (75–50 %) | Couleurs qui se délavent, brume au sol, détails flous, sons étouffés | Teinte froide translucide par case de 128 px (visible depuis le 25 septembre, dessinée sous le sol auparavant) |
| Effilochée (50–25 %) | Structures transparentes, brume, sol fissuré, sons distordus ; −10 % vitesse et dégâts du joueur | Teinte plus opaque ; plus de créatures (jusqu'à ×2,2 à mémoire nulle), plus rapides (+30 %) |
| Effacée (25–1 %) | Quasi monochrome, structures qui se désagrègent en particules, sol instable ; −25 %, dégâts lents | Même teinte, plus claire |
| Néant (0 %) | Vide blanc animé, dégâts continus, ennemis exclusifs | Bord de carte effacé à la génération, sans lien avec la mémoire des zones |

Hors pourcentage dans le HUD, le joueur ne voit presque pas où en est le monde, ni ce que l'oubli lui coûte ou lui offre.

## 3. Principes

1. **Lisible avant d'être joli :** on doit savoir d'un coup d'œil qu'une zone s'efface et à quel point, sans lire le HUD.
2. **Beau et mélancolique, pas gris :** la Bible veut un monde « douloureusement beau ». L'oubli retire la couleur et le détail ; il ne salit pas.
3. **L'oubli touche aussi le joueur**, pour que fuir soit une décision et pas un décor, sans jamais tuer une run sur un détail invisible.
4. **Coût borné :** les effets se calculent par case de mémoire (128 px), pas par pixel ni par décor et par frame. Carte de mémoire en texture basse résolution, lue par les shaders.

## 4. Lots proposés

| Lot | Contenu | Vérification |
|---|---|---|
| **O1 — Le sol oublie** | Une texture « mémoire par case », mise à jour à 2 Hz, lue par le shader du sol : désaturation progressive, puis passage au blanc-bleuté, puis fissures lumineuses tramées. Remplace la teinte plate actuelle | Captures d'une même zone aux cinq phases (mode de capture forçant la mémoire) ; banc de performance |
| **O2 — Les choses se défont** | Décors et immeubles en zone Effilochée : contour qui s'effrite (shader de dissolution tramée déjà utilisé pour les morts), transparence partielle ; en zone Effacée, particules qui s'élèvent des décors proches du joueur (pool, plafonné) | Captures ; nœuds créés par seconde stables |
| **O3 — La frontière qui avance** | Le front de l'Effacement devient visible : lisière blanche animée là où la mémoire passe sous 25 %, son dédié à l'approche (plan 15) ; le Néant (0 %) réel, avec dégâts continus | Recette : le joueur sait-il où fuir sans regarder le HUD ? |
| **O4 — Ce que l'oubli coûte** | Débuffs de la V2 appliqués au joueur en zone Effilochée et Effacée, avec un signal clair (bord d'écran délavé, icône) ; réglages en JSON | Recette : punitif ou juste ? Mesures de dégâts reçus avec `tools/measure_density.sh` |
| **O5 — Ce que l'oubli offre** | Risque et récompense : Essence et score majorés en zone fragile, butin qui disparaît avec la zone ([plan 13](13-butin.md) lot F), Autels qui « rappellent » brièvement une zone (V2 §11) | Recette : détours pris ou refusés |
| **O6 — Échos** | Rares silhouettes-souvenirs d'habitants dans les zones Fragiles (sans collision), lien avec le lore ([plan 14](14-anomalies-du-monde.md)) | Recette : mémorable, pas bruyant |

**Ordre recommandé :** O1 puis O3 pour la lisibilité (voir l'oubli), puis O4/O5 ensemble (le ressentir, avec un équilibre coût/récompense), puis O2 et O6 pour l'ambiance. O1 dépend du shader de sol du [plan 10 T2](10-terrain-et-tiles.md#9-retours-du-26-septembre--tiles-et-jonctions), qui peut porter les deux effets.

## 5. Décisions demandées à Raphaël

1. L'ordre des lots, ou ceux à écarter.
2. Les débuffs de la V2 (−10 %/−25 %) : à appliquer tels quels, adoucis, ou remplacés par un seul coût lisible (par exemple une régénération bloquée) ?
3. Le Néant à 0 % : traversable avec dégâts (V2), ou mur infranchissable ?

## 6. Arbitrages et lot O1 — 26 septembre 2026

Ordre O1 → O3 → O4/O5 → O2/O6 validé par Raphaël. Arbitrages délégués ([DECISIONS §7](DECISIONS.md#7-arbitrages-délégués-du-26-septembre)) : débuffs de la V2 adoucis et en JSON au lot O4 ; Néant traversable avec dégâts continus.

**O1 livré — le sol oublie :**
- `ErasureManager` publie deux fois par seconde la mémoire des zones autour du joueur : une fenêtre de 32×32 zones de 128 px, publiée en *global shader uniforms* déclarés dans `project.godot` (`erasure_memory`, `erasure_window`, `erasure_far_memory`). Aucune référence du gestionnaire vers le sol.
- Le shader du sol (le même que les jonctions du [plan 10 T2](10-terrain-et-tiles.md#lot-t2-livré--26-septembre-2026)) lit cette mémoire aux sommets de chaque tuile :
  - Fragile : les couleurs se délavent vers un gris pâle et froid ;
  - Effilochée : des plaques de pixels passent au blanc effacement par tramage, et les premières veines de fissure apparaissent ;
  - Effacée : sol quasi blanc, réseau de fissures lumineuses qui respirent ;
  - Néant : blanc qui grésille.
- La teinte plate de `ErasureOverlay` (rectangles alignés sur les axes) est supprimée.
- **Vérification :** `CAPTURE_EXTRA_ARGS="--capture-erasure" tools/capture_run.sh <dossier>` impose la mémoire autour du joueur et capture chaque phase, puis un dégradé d'ouest en est. Captures regardées. Un premier passage n'avait rien montré, sans cause identifiée ; les quatre suivants sont conformes. À surveiller.
- **Coût :** la mémoire est lue aux sommets (4 lectures par sommet, pas par pixel). Les fissures, calculées par un réseau de Voronoï, ne sont évaluées que sous 50 % de mémoire. Aucun banc n'a été fait en zone effacée ; à mesurer lors de la recette.
- **Limites :** les décors et les créatures gardent leurs couleurs dans les zones effacées (lot O2). La frontière de l'oubli n'est pas encore animée (O3).

**O3 livré — la frontière qui avance (26 septembre) :**
- **Lisière** : là où la mémoire passe sous 25 %, le shader du sol dessine une frange blanche et bleutée qui scintille. On voit la limite de l'Effacé sans regarder le HUD. Capture `--capture-erasure` (dégradé recentré sur la lisière) regardée.
- **Néant réel** : à mémoire nulle, le joueur perd 6 % de ses PV max par seconde (`void_damage_ratio_per_second` dans `data/scaling/erasure.json`), par tranches à chaque mise à jour de l'Effacement (0,5 s). Il reste traversable (arbitrage délégué). Test d'intégration : 70 → 66 PV en 80 ticks.
- **Non fait** : le son d'approche du front (plan 15, traité par un autre agent) ; les débuffs d'Effilochée et d'Effacée (O4).

**O4 livré — ce que l'oubli coûte (26 septembre) :**
- **Pénalités adoucies** par rapport à la V2 (arbitrage délégué), dans `zone_effects` de `data/scaling/erasure.json` :
  - Effilochée : vitesse ×0,95 et dégâts ×0,95 (V2 : ×0,90) ;
  - Effacée et Néant : ×0,88 (V2 : ×0,75).

  Elles s'appliquent à part des bonus (`ErasureEffects`), donc elles se lèvent exactement en sortant de la zone.
- **Signal** : `ErasureManager` émet `PlayerErasurePhaseChanged` quand la zone du joueur change de phase. Le joueur ajuste ses facteurs, et un voile d'écran (`UI/ErasureVeil`, shader `erasure_veil`) couvre les bords d'une trame blanc-bleuté (intensité 0,35, 0,7 puis 1). Le voile passe sous le HUD et sous le panneau des quêtes, remonté à la couche du HUD.
- **Vérification** : test d'intégration (pénalités appliquées dans le Néant, levées au retour) ; captures des phases.

**O5 livré en partie — ce que l'oubli offre (26 septembre) :**
- Chaque créature abattue rapporte plus de score selon la phase de la zone où elle tombe : +25 % en Fragile, +50 % en Effilochée, +100 % en Effacée et au Néant.
- Elle a aussi une chance de donner 1 Essence de plus : 15 %, 35 %, puis 60 %.
- Réglages : `score_bonus` et `essence_chance` dans `zone_effects`. Le détour vers la frontière devient un vrai choix de risque et de récompense.
- **Test d'intégration** : le même kill vaut 33 points au Néant, contre 16 en zone ancrée.
- **Non fait** : le butin qui disparaît avec la zone (dépend du plan 13, non implémenté) et le « rappel » d'une zone par les Autels (V2 §11).

**O2 livré — les choses se défont (26 septembre) :**
- **Décors** (`prop_forget.gdshader`, un seul matériau partagé) **et canopées** (`sway.gdshader`) lisent la mémoire à leur point au sol :
  - Fragile : ils se délavent comme le sol ;
  - Effilochée : leurs bords s'effritent en pixels perdus, et une partie blanchit ;
  - Effacée : ils se réduisent à des silhouettes grises qui s'émiettent ;
  - Néant : il n'en reste que des fragments.
- La lecture de la mémoire est commune au sol, aux décors et aux canopées (`erasure_memory.gdshaderinc`).
- **Coût** (banc de combat dense, même moment, machine chargée) : 1 303 appels de dessin contre 1 305, FPS équivalents (58 contre 61 en 720p, 46 contre 46 en 1080p). Le rendu par lots est préservé.
- **Non fait** : les particules qui s'élèvent des décors proches en zone Effacée (prévues au plan initial). Elles demandent un pool et un plafond à mesurer.

