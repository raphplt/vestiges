# Écrans de choix : relance et entrée interrompue — 1er octobre 2026

Lot R7 du plan 04. Base `63492145`, même banc avant/après, Godot 4.7.2/.NET 10.

- `tools/test_choice_screen.sh` : **24 contrôles, 12 échecs avant → 0 après**.
  Les échecs de refus/sortie après validation directe sont les conséquences de
  la sélection prématurée du même scénario, pas douze causes indépendantes.
- `tools/test_ui_art.sh` : 169 contrôles, zéro échec ; rotation toujours 8°/s.
- Build : zéro avertissement/erreur. Smoke : 600 frames, vert.
- Captures Main : `VESTIGES_SCREEN=1 CAPTURE_EXTRA_ARGS="--capture-choices"
  tools/capture_run.sh /tmp/vestiges-choice-after 10 0 1920x1080 221092026`.
  Deux images avant et deux après ouvertes et inspectées, empreintes jointes.
  Le titre, le sous-titre et Partir étaient invisibles après réouverture ;
  l'éclair restait figé. Après correction, tous sont visibles, fond cyan final.
  Passer l'entrée conserve les cartes et termine aussi le fondu du fond.
- Le scénario force une relance à 0,1 s puis ralentit les tweens pour saisir
  cet instant : reproduction d'un état d'interface, pas mesure de temps réel.
  Données de bénédictions réelles, callback compté sans gain attribué. Le
  résultat dans les deux versions est deux choix, écran fermé et pause levée.
- Les parcours de recette passent désormais l’entrée avant de prendre la
  bénédiction ou de consommer l’intention d’achat du bot.
- Souris, Entrée et bouton A sont injectés ; cela ne vaut pas recette sur une
  manette physique. Achat puis réouverture, refus de Faille et monde arrêté/
  repris vérifiés par le banc. Aucun fichier audio ni équilibrage modifié.

Les contrôles sont headless ; les fenêtres sont sur ViewSonic uniquement.
Pas de revendication de gain de FPS : modifications aux ouvertures/fermetures.
L'avertissement Steam, les MixRate du pilote factice et les ressources à la
fermeture sont conservés dans les logs ; ils ne sont pas des erreurs de scénario.
