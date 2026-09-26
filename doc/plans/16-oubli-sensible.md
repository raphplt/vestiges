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
