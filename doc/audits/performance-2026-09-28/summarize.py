#!/usr/bin/env python3
"""Synthèse reproductible ; aucune durée sous charge n'est convertie en FPS."""
import collections
import csv
import io
import gzip
import json
import pathlib
import statistics
import sys

root = pathlib.Path(sys.argv[1]) if len(sys.argv) > 1 else pathlib.Path(__file__).parent
summary = {}
for path in sorted(root.glob('systems-*.json')):
    rows = []
    for row in json.loads(path.read_text())['results']:
        row = dict(row)
        samples = row.pop('intervals_ms', None)
        if samples:
            ordered = sorted(samples)
            row['timing_diagnostic_only'] = {'samples': len(samples), 'median_ms': statistics.median(samples),
                                           'p99_ms': ordered[min(len(ordered)-1, int(.99*len(ordered)))], 'max_ms': max(samples)}
        rows.append(row)
    summary[path.stem] = rows
for name in ['physics', 'shaders', 'cycles']:
    path = root / (name + '.json')
    if path.exists():
        summary[name] = json.loads(path.read_text())
profile_rows = []
for path in sorted((root / 'profile').glob('*.speedscope.json*')):
    data = json.loads(gzip.decompress(path.read_bytes()) if path.suffix == '.gz' else path.read_bytes())
    frames = data['shared']['frames']
    # Le thread principal est celui qui contient les callbacks Enemy (pas les threads .NET en attente).
    candidates = []
    for profile in data['profiles']:
        events = profile.get('events', [])
        if any('Vestiges.Combat.Enemy._PhysicsProcess' in frames[e['frame']]['name'] for e in events):
            candidates.append(profile)
    if len(candidates) != 1:
        raise ValueError(f'{path}: thread de jeu ambigu ({len(candidates)})')
    profile = candidates[0]
    stack = []
    previous = profile['startValue']
    inclusive = collections.Counter()
    exclusive = collections.Counter()
    for event in profile['events']:
        duration = event['at'] - previous
        for frame in set(stack):
            inclusive[frame] += duration
        if stack:
            exclusive[stack[-1]] += duration
        if event['type'] == 'O':
            stack.append(event['frame'])
        else:
            if not stack or stack.pop() != event['frame']:
                raise ValueError('pile incohérente')
        previous = event['at']
    total = profile['endValue'] - profile['startValue']
    selected = []
    for index, value in inclusive.most_common():
        name = frames[index]['name']
        if any(x in name for x in ['Vestiges.Combat.Enemy.', 'Vestiges.Core.Player._', 'Vestiges.Tests.MovementDenseBenchmark._',
                                   'Vestiges.World.GrassTrample._', 'Godot.CharacterBody2D.MoveAndSlide', 'Vestiges.Core.GroupCache.GetEnemies']):
            selected.append({'method': name, 'inclusive_sampled_ms': value, 'exclusive_sampled_ms': exclusive[index],
                             'inclusive_percent_main_thread': value / total * 100})
    profile_rows.append({'file': path.name, 'thread': profile['name'], 'sampled_span_ms': total,
                         'warning': 'Temps mural échantillonné, appels natifs inclus dans les appelants ; les pourcentages inclusifs se recouvrent.', 'methods': selected})
summary['rendered_benchmarks'] = {}
for directory in ['crowd', 'density']:
    rows = []
    for path in sorted((root / directory).glob('*.json')):
        if path.name.startswith('manifest'): continue
        row = json.loads(path.read_text())
        raw = path.with_suffix('.frames.csv.gz')
        if raw.exists():
            samples = [float(r['wall_ms']) for r in csv.DictReader(io.StringIO(gzip.decompress(raw.read_bytes()).decode()))]
            rows.append({'file': path.name, 'enemies': row['enemies'], 'observer_period': row['observer_period_frames'],
                         'frames': row['frames'], 'max_frame_index': samples.index(max(samples)),
                         'first_five_ms': samples[:5], 'observer_mean_usec': row['observer_total_usec'] / row['observer_samples'],
                         'observer_total_usec': row['observer_total_usec'], 'observer_samples': row['observer_samples'],
                         'valid': row['valid']})
    summary['rendered_benchmarks'][directory] = rows
summary['managed_profiles'] = profile_rows
(root / 'summary.json').write_text(json.dumps(summary, indent=2, ensure_ascii=False) + '\n')
print(root / 'summary.json')
