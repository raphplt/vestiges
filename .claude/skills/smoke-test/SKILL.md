---
name: smoke-test
description: Valide que Vestiges compile et démarre (dotnet build + import Godot headless + boot du Hub). À utiliser après toute modification de C#, scènes, shaders, project.godot ou assets importés, et avant de déclarer une tâche terminée.
allowed-tools: Bash(tools/smoke_test.sh *) Bash(tools/smoke_test.sh) Read Grep
---

# Smoke test Vestiges

1. Lancer `tools/smoke_test.sh $ARGUMENTS` (argument optionnel : nombre de frames, 600 par défaut).
2. Si vert : le dire en une ligne.
3. Si rouge :
   - Build cassé : lire les erreurs `dotnet`, corriger, relancer.
   - Erreur runtime : ouvrir `${TMPDIR:-/tmp}/vestiges-smoke/run.log` autour de l'erreur ; la backtrace C# donne le fichier et la ligne (`res://scripts/...`). Les erreurs de shader affichent la ligne fautive précédée de `E`.
   - Corriger la cause, pas le filtre : n'ajouter un motif à `NOISE` dans le script que pour du bruit propre au mode headless, avec justification.
4. Ne jamais déclarer une tâche terminée avec un smoke test rouge sans le signaler explicitement.

Limite : le smoke test ne boote que le Hub. Il ne lance pas de run et ne remplace pas un playtest.
