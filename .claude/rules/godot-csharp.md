---
paths:
  - "scripts/**/*.cs"
  - "scenes/**/*.tscn"
---

# C# / Godot

- Namespace = `Vestiges.<Dossier>` (ex. `Vestiges.Combat`). Un fichier par type public.
- Nouveau script : Godot génère le `.cs.uid` au prochain import (`tools/smoke_test.sh` le fait). Le committer avec le `.cs`.
- Suppression d'un script : supprimer aussi son `.cs.uid` et ses références dans les `.tscn` et `project.godot`.
- Boucles chaudes (`_Process`, `_PhysicsProcess`, IA ennemie, projectiles) : pas de LINQ, pas de `new` de collections, pas de concaténation de strings, pas de `GetNode` ni de lookup par groupe. Mettre en cache dans `_Ready` ou utiliser `GroupCache`.
- Entités fréquentes : passer par les pools existants (`Spawn/EnemyPool.cs`), jamais `QueueFree` + `Instantiate` en rafale.
- Communication inter-systèmes : déclarer un `[Signal]` dans `Core/EventBus.cs` et l'émettre, plutôt que d'appeler un autre manager directement.
- Toute valeur d'équilibrage (dégâts, cooldowns, taux) vient d'un JSON de `data/` via un loader de `Infrastructure/`, jamais d'une constante.
- Inputs : utiliser `IsActionPressed("<action>")` avec une action déclarée dans `project.godot`, jamais un `Key.X` en dur.
- `GD.Print` préfixé `[NomDuSystème]`, `GD.PushError`/`PushWarning` pour les anomalies.
- Gros fichiers (`Core/Player.cs` ~2 800 lignes, `Combat/Enemy.cs`, `Combat/VfxFactory.cs`) : ne pas les faire grossir. Extraire la nouvelle logique en composant dédié.
