# Preuves de l’audit du 27 septembre 2026

Commit analysé : `c78e1dd6`. Aucun changement de production. Build C# : zéro avertissement, zéro erreur.

- `mesures.json` contient les 15 résultats complets des bancs rendus, les cinq contrôles de biomes et le diagnostic du pool.
- `pool-lifecycle.txt` conserve les observations de survie des instances après destruction du pool.
- `*-machine.txt` conserve les relevés de charge avant les essais d’attribution.
- Les fichiers `.txt` des scripts sont des copies exactes des diagnostics externes utilisés. Leur extension évite de les importer comme des scripts du jeu.
- Intervalles bruts CSV, captures PNG et logs complets : `/tmp/vestiges-audit-20260927/`, non versionnés. Les valeurs détaillées restent dans le JSON si ce dossier temporaire est nettoyé.

Les essais d’attribution ne sont pas des correctifs : les scripts masquent certaines couches pour en isoler le coût. Les parties utilisent un profil temporaire et n’écrivent pas dans les sauvegardes personnelles.

## Reproduction sur le même checkout

Ces scripts contiennent les chemins de la machine d’audit. Adapter le chemin du dépôt si nécessaire. Réserver une machine calme et ne pas exécuter les séries en parallèle.

1. Recréer `/tmp/vestiges-audit-20260927/` et y recopier les quatre scripts en retirant leur dernière extension `.txt`.
2. Pour la série standard, choisir un nouveau dossier :

   `BENCH_REPEATS=1 BENCH_SECONDS=15 tools/benchmark_movement.sh /tmp/vestiges-audit-20260927/reference`

3. Lancer `bash /tmp/vestiges-audit-20260927/attribution.sh` depuis ce dépôt. Le build/import de l’étape 2 doit être terminé.
4. Lancer ensuite `bash /tmp/vestiges-audit-20260927/layers.sh`.
5. Ne pas réutiliser des dossiers contenant des résultats à conserver : les scripts ponctuels d’attribution écrivent leurs noms de sortie fixes.

Les scripts de couches chargent la scène existante du banc ; les sources C# et les shaders de production ne changent pas. Le diagnostic du pool charge sa classe C# directement, utilise ses méthodes réelles, puis libère explicitement les survivants avant de quitter.

## Limites

15 bancs denses valides, chacun de 15 s après 5 s de chauffe ; deux répétitions seulement pour chaque variante principale d’attribution. La série 60/240 ennemis et le mélange désactivé n’ont qu’un essai par variante. Les biomes ont 8 s chacun, sans validation aussi complète du viewport que le banc dense.

Les logs comportent les avertissements de fermeture usuels (`ObjectDB`/ressources) ; le contrôle de biomes signale aussi des RID de corps 2D non libérés à la fermeture. Le diagnostic indépendant du pool démontre une survie d’instances avant la fermeture du processus : sa conclusion ne repose donc pas uniquement sur ces avertissements.

Aucune optimisation n’est livrée et aucun gain de production n’est revendiqué. Les coûts par méthode, les longues runs, les pauses GC et la mémoire après de multiples vraies runs restent à instrumenter comme précisé dans le rapport.
