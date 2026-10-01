# Cartographie R6 — 1er octobre 2026

Grille de découverte et d’Effacement conservée (128 px monde). Le fond cartographique
passe de 1 à 4 × 4 pixels par cellule, échantillonnés à la découverte : biomes,
eau, rues et sentiers. Cache du terrain, teinte de phase indépendante, Néant connu
hachuré. Légende sur fond commun, repères cernés, texte 100/115/130 % pris en charge.

## Vérifications

- Build : zéro warning/erreur. Smoke 600 frames vert.
- `tools/test_cartography.sh` : **17 contrôles**, découverte, bords, coordonnées
  négatives, phases, transparence, aller-retour de couleur et 1 000 revisites sans
  allocation gérée. Logs compressés conservés ici.
- Main réelle, seed **221092026**, même parcours de découverte : référence
  `/tmp/vestiges-map-before`, résultat intermédiaire `/tmp/vestiges-map-after`,
  final `/tmp/vestiges-map-final`. Radar, carte entière, danger, Néant et bord du
  monde inspectés. La phase de danger est injectée pour éprouver l’affichage ;
  elle ne constitue pas une observation du timing naturel de l’Effacement.
- Fenêtre X11 **écran 1, (3840, 0)** : ViewSonic. Sortie native 3840 × 2160
  malgré la demande 1920 × 1080. Grand texte 130 % inspecté, plus réduction de
  lecture à 1280 × 720 ; pas de validation d’une fenêtre native 720p.

## Coût et limites

Micro-banc CPU identique : carte 202 × 102, disque de 441 cellules, 40 passages
après trois échauffements. Ancien aplat : **0,0191 ms** ; nouveau raster avec
publication Image : **1,2404 ms**, soit 7 056 échantillons au premier relevé.
Le sampler du banc est constant : ces temps excluent les requêtes au monde réel
et le transfert GPU. Buffers gérés : **2 657 916 octets** ; image RGBA :
**1 318 656 octets**, à publier au plus quatre fois/s et seulement si modifiée.
Une revisite ne rééchantillonne pas ; une phase relit le cache de 16 pixels.

La charge machine était de 10,39 pour 16 cœurs logiques, avec un autre jeu actif.
Banc FPS reporté : aucune affirmation de gain ou de 60 FPS constants. Le terrain
rendu du monde, les règles d’Effacement, le rayon de découverte et la portée du
radar ne changent pas. La qualité artistique finale reste une recette humaine.
