#!/usr/bin/env python3
"""Résumé des processus indépendants ; les mesures brutes restent disponibles."""
import json
from pathlib import Path
import statistics
import sys

root = Path(sys.argv[1])
results = [json.loads(p.read_text()) for p in sorted(root.glob('*-baseline.json')) + sorted(root.glob('*-dash.json'))]
if not results or any(not row['valid'] for row in results):
    raise SystemExit('Mesures absentes ou invalides')
lines = ['# Benchmark mobilité — rendu Main', '',
         'Comparaison du code courant sans activation puis avec dash répété, pas d’un ancien commit.',
         'Médianes entre processus indépendants ; maxima conservés. FPS libres, VSync désactivée, audio Dummy.', '',
         f"CPU : {results[0]['cpu']} ({results[0]['cpu_threads']} threads)",
         f"GPU : {results[0]['gpu']}", f"Moteur : {results[0]['engine']}", '',
         '| Résolution | Variante | Essais | Moyenne ms | FPS | p95 ms | p99 ms | Pic ms | >16,67 ms % | Dashs | RSS max Mio | Nœuds créés/s |',
         '|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|']
def nodes(rows):
    # Champ ajouté le 25 septembre 2026 : absent des mesures plus anciennes.
    values = [r['nodes_added_per_second'] for r in rows if 'nodes_added_per_second' in r]
    return f"{statistics.median(values):.0f}" if values else "n/m"


for resolution in ([1280, 720], [1920, 1080]):
    for dash in (False, True):
        rows = [r for r in results if r['resolution'] == resolution and r['dash'] == dash]
        if not rows:
            continue
        metric = lambda key: statistics.median(r['frames'][key] for r in rows)
        lines.append(f"| {'×'.join(map(str, resolution))} | {'Dash' if dash else 'Sans dash'} | {len(rows)} | {metric('mean_ms'):.2f} | {metric('fps'):.1f} | {metric('p95_ms'):.2f} | {metric('p99_ms'):.2f} | {max(r['frames']['max_ms'] for r in rows):.2f} | {metric('over_16_67_percent'):.2f} | {sum(r['activations'] for r in rows)} | {max(r['rss_process_peak_bytes'] for r in rows)/2**20:.1f} | {nodes(rows)} |")
lines.extend(['', results[0]['fixture'], '',
              'Les stats dash_window_frames des JSON couvrent les frames pendant le dash et la frame suivante ; elles ne sont pas une mesure GPU isolée de l’effet.',
              'Le timer mural entre _Process inclut rendu, physique, attente et ordonnanceur. Les champs *_cpu_mean_ms sont des moniteurs Godot complémentaires, pas une attribution CPU ou GPU isolée.',
              'Le viewport logique et l’image physique ont les dimensions demandées : le cadrage varie entre résolutions. La comparaison marche/dash conserve le même cadrage pour chaque résolution.',
              'Les captures PNG et écritures de résultats sont après la fenêtre mesurée. Le RSS maximal inclut le chargement.',
              f"Population vivante minimale : {min(r['living_min'] for r in results)} ; dans le rayon de traitement complet de 600 px : {min(r['full_ai_range_min'] for r in results)}.",
              'Ce banc ne prouve ni le ressenti manette/clavier ni la performance de tous les builds et biomes.'])
text = '\n'.join(lines) + '\n'
(root / 'SUMMARY.md').write_text(text)
print(text)
