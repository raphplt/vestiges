# Préchauffage rendu — 1er octobre 2026

Plan 10, sous-lot 5B1. Godot 4.7.2, GL Compatibility, RX 6950 XT/Mesa 26.2.3.

Le bootstrap de référence est archivé (identique à `fe46a0c8` pour cette méthode).
Le même `ShaderWarmupAudit.gd` mesure ses neuf sprites avec `--legacy-source`,
puis la véritable classe de production `ShaderWarmup`.

| Échantillons | Hors champ | Dans le champ | Contrôle individuel |
|---|---:|---:|---|
| Ancien montage, 9 shaders | 0 dessin | 9 dessins | 9/9 soumis |
| Nouveau viewport, 16 shaders + particules d'orbe | 0 dessin | 17 dessins | 17/17 soumis |

La production utilisait la colonne « hors champ ». Le nouveau viewport de
256 × 256 rend trois images, puis est désactivé et libéré. Le format des
chemins est un maillage à couleurs/UV/indices ; les effets écran passent par
des rectangles, les autres par des sprites. La lueur d'XP vient de sa fabrique
réelle. Les Shader restent référencés après la destruction des échantillons.
Le catalogue couvre les références littérales de run du code courant ; les
deux shaders exclus sont propres au Hub. Les quatre shaders historiques sans
consommateur de run ne sont plus chargés pour ce warmup.

Vérifications :

- Audit GL : chaque échantillon soumis seul, puis ensemble ; viewport libéré.
- Main rendue : journal `shaders=16 draw_calls=17 frames=3`, aucun viewport de
  préchauffage encore présent à la fin de l'initialisation.
- Parcours Faille → Mémorial : offre et services affichés, sortie et reprise
  vérifiées. Le tirage donne un Oubli permanent : sa levée ne fait pas partie
  des validations de cette capture.
- Main headless : deux seeds (221092026/1002), 20 s simulées chacune, sans
  attente de `FramePostDraw`, zéro erreur inattendue.
- Déplacements : **158 assertions vertes**, build sans avertissement,
  smoke 600 frames vert. UI : 24 contrôles R7 verts.
- Chargement : 14 captures avant et 13 après ; premiers effets : 12 avant,
  12 après et 12 après correction du délai de capture. Planches ouvertes et
  inspectées : pas d'échantillon de warmup dans la fenêtre, terrain, halos,
  colonnes et projectiles présents. ViewSonic uniquement, seed 221092026.

Limites et essais écartés :

- Charge 8,95–11,21 pour 16 cœurs logiques (seuil 4) : **aucune conclusion
  FPS ni compilation à froid**. 5B2 (cache pilote froid/chaud et attribution
  des premiers à-coups) reste ouvert. Compter un dessin ne chronomètre pas
  la compilation et ne garantit pas tous les états possibles du pilote.
- `test_movement.sh --run-integration` s'arrête à sa limite de 1 500 frames
  **avant comme après** ; ce n'est pas une validation de l'intégration.
  Les deux vraies runs headless fournissent le contrôle du bootstrap.
- Deux captures utilisaient des identifiants ennemis inexacts (`cracheur`,
  puis `spitter`) : logs conservés, essais exclus. L'identifiant vérifié dans
  les données est `fading_spitter` ; le scénario final est sans cette erreur.
- L'ancien délai de 90 frames capturait encore le fondu de chargement dans
  les premières images. Le mode capacités attend désormais sa disparition ;
  les deux séries initiales conservent ce défaut de fixture dans les preuves.
- Les warnings Steam, MixRate du serveur factice et ressources à la sortie
  restent dans les journaux. Aucun fichier audio ni réglage de combat changé.

Rejouer : `VESTIGES_SCREEN=1 tools/test_shader_warmup.sh /tmp/vestiges-shaders-neuf`.
Pour la référence : ajouter `SHADER_AUDIT_ARGS="--legacy-source <chemin absolu
vers reference-bootstrap.cs.txt>"`. Les captures se rejouent avec
`--capture-loading --capture-abilities --enemies hurleur,fading_spitter`.
