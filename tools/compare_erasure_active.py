#!/usr/bin/env python3
"""Compare la trace de mémoire et les signaux ordonnés du lot 6C, au même instrument."""
import argparse
import gzip
import json
from pathlib import Path


def compare(reference: Path, current: Path) -> dict:
    before, after = (json.loads(gzip.decompress(path.read_bytes()) if path.suffix == '.gz' else path.read_bytes())
                     for path in (reference, current))
    if before['failures'] or after['failures']:
        raise ValueError('Une recette contient des assertions en échec')
    if before['seed'] != after['seed']:
        raise ValueError('Les seeds diffèrent')
    if before['events'] != after['events']:
        raise ValueError('Les transitions ordonnées diffèrent')
    old = next(row for row in before['results'] if row['scenario'] == 'roaming')
    new = next(row for row in after['results'] if row['scenario'] == 'roaming')
    if len(old['trace']) != len(new['trace']):
        raise ValueError('Les traces ne couvrent pas la même durée')
    for a, b in zip(old['trace'], new['trace'], strict=True):
        for key in ('step', 'tracked', 'active', 'state_hash'):
            if a[key] != b[key]:
                raise ValueError(f"Pas {a['step']}, {key} différent : {a[key]} != {b[key]}")
    zeros = [row for row in after['results'] if row['scenario'] == 'zero_cells']
    if any(row['visits_per_update'] != 0 for row in zeros):
        raise ValueError('Les cellules stables sont encore recalculées')
    if new['visits'] >= old['visits']:
        raise ValueError('Le nombre de calculs ne baisse pas')
    return {
        'valid': True,
        'seed': before['seed'],
        'steps_identical': len(new['trace']),
        'ordered_events_identical': len(after['events']),
        'visits_before': old['visits'],
        'visits_after': new['visits'],
        'reduction_percent': round(100 * (1 - new['visits'] / old['visits']), 3),
        'last_step_before': old['trace'][-1],
        'last_step_after': new['trace'][-1],
        'zero_cells_before': [row for row in before['results'] if row['scenario'] == 'zero_cells'],
        'zero_cells_after': zeros,
    }


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('reference', type=Path)
    parser.add_argument('current', type=Path)
    parser.add_argument('--output', type=Path)
    args = parser.parse_args()
    report = json.dumps(compare(args.reference, args.current), indent=2, ensure_ascii=False) + '\n'
    if args.output:
        args.output.write_text(report)
    print(report, end='')
