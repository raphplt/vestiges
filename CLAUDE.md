@AGENTS.md

## Spécifique Claude Code

`AGENTS.md` est la source unique des instructions projet (partagée avec les autres agents). N'ajouter ici que ce qui est propre à Claude Code.

- **Règles contextuelles** : `.claude/rules/` (chargées selon les fichiers touchés : C#, données JSON, pixel art).
- **Skills** : `/smoke-test` (validation build + boot headless), `/roadmap-sync` (réaligner la roadmap V2 sur le code).
- **Subagent** : `godot-reviewer` pour relire un diff C#/Godot (perf, découplage, conventions) avant commit.
- **Hooks** : les JSON de `data/` sont validés à chaque écriture ; si des `.cs` ont changé, `dotnet build` tourne en fin de tour et renvoie les erreurs.
