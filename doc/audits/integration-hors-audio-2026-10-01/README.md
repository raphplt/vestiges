# Intégration des retours hors audio — 1er octobre 2026

Lots : R3b (cadence), préchargement du shader actuel, R6 (carte), R5a–c
(Traqueur, Vagabond, Forgeuse). Chacun a son commit, son plan et ses preuves.
Les changements audio indépendants du commit `8fd758f6` sont conservés ; aucun
fichier du moteur ou des données audio n’est modifié par les lots présents.
Conflits résolus : commentaires d’options dans RunObservation et deux décisions
numérotées 46. Audio garde §46 ; continuation hors audio devient §47, liens ajustés.

Après réunion : build **0 warning / 0 erreur**, **17 contrôles cartographiques**,
**61 contrôles de capacités ennemies**, smoke **600 frames** et boot de **Main
pendant 20 secondes de jeu** (seed 221092026, headless) verts. Les **456 SHA-256**
des personnages correspondent encore aux rapports de production. Journaux ici.
Les captures précédentes sont inspectées dans les rapports de chaque lot ;
fenêtres exclusivement ViewSonic, aucune écoute ni intervention audio dans ce
travail. Les artefacts visuels restent hors Git.

Limites conservées : ressenti humain de la pression des tirs et appréciation
artistique ouverts ; benchmark FPS de la carte différé sur machine chargée ;
les réductions 720p d’images 4K ne remplacent pas un essai natif 720p.
