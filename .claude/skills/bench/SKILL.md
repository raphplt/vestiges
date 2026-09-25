---
name: bench
description: Mesure le coût d'un changement en combat dense (120 ennemis, Main réelle, FPS, p99, nœuds créés par seconde, allocations), avec contrôle de la charge machine et comparaison A/B contre un commit de base. À utiliser après une optimisation, un changement de rendu, de pool, de VFX ou de génération qui peut coûter, et avant d'affirmer un gain ou l'absence de régression.
argument-hint: "[ref de base, HEAD par défaut]"
allowed-tools: Bash(tools/bench_ab.sh *) Bash(tools/benchmark_movement.sh *) Bash(python3 tools/summarize_movement_benchmark.py *) Bash(uptime) Bash(ps *) Bash(cat /proc/loadavg) Read
---

# Banc de combat dense

## 1. La machine est-elle calme ?

`uptime` et `ps -eo pcpu,comm --sort=-pcpu | head -6`. Raphaël compile souvent d'autres projets (VM, linker, node, java). Si la charge dépasse un quart des cœurs, **ne pas mesurer de FPS** : le dire, et s'appuyer sur `nodes_added_per_second` et `allocated_bytes`, insensibles à la charge.

## 2. Mesurer

- **Comparaison** (le cas normal) : `tools/bench_ab.sh <ref de base> <dossier neuf> [passes=2]`. Le script refuse de mesurer sous charge, crée un worktree de la base, y copie la scène de banc courante (même instrument), alterne base et courant, puis affiche un tableau médian.
  - Ref de base : `HEAD` si le changement n'est pas committé, sinon le commit précédent.
- **État seul** : `BENCH_REPEATS=1 BENCH_SECONDS=15 tools/benchmark_movement.sh <dossier neuf>` puis `python3 tools/summarize_movement_benchmark.py <dossier>`.

## 3. Lire

- Une passe `valid=false` ne compte pas (fenêtre non conforme, population insuffisante).
- `nodes_added_by_type` dans le JSON dit **quoi** est créé à chaque frame : c'est la liste des candidats aux pools (`Combat/CombatPools.cs`).
- Écarts de moins de ~5 % entre deux passes : du bruit.
- Rapporter : matériel, charge en début et fin, FPS et p99 médians base/courant, nœuds/s, et la réserve éventuelle (charge, carte différente entre versions si la génération a changé).

Limite : 100 Ombres et 20 Cracheurs à 10 000 PV autour d'un joueur invincible avec l'arme de départ. Ce banc ne mesure ni un build puissant, ni les morts en rafale, ni tous les biomes.
