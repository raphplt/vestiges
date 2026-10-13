#!/usr/bin/env python3
"""Tic-tac du compte à rebours (éclats d'un Mémorial, DECISIONS §154) : clic boisé court et sa variante pressante,
plus aiguë et plus sèche. Bruit et résonances tirés d'une graine fixe : régénérer donne les mêmes octets."""
import math
from pathlib import Path
import random
import struct
import wave

RATE = 48000
ROOT = Path(__file__).resolve().parents[1] / 'assets/audio/sfx/gameplay'


def tick(duration, pitch, decay, seed):
    rng = random.Random(seed)
    samples = []
    for i in range(round(RATE * duration)):
        t = i / RATE
        click = rng.uniform(-1, 1) * math.exp(-t * 900)
        body = math.sin(2 * math.pi * pitch * t) + 0.45 * math.sin(2 * math.pi * pitch * 2.71 * t)
        samples.append(0.55 * click + body * math.exp(-t * decay))
    # Attaque de 1 ms : sans elle, le premier échantillon claque.
    for i in range(48):
        samples[i] *= i / 48
    return samples


def write(name, samples, level):
    peak = max(abs(s) for s in samples)
    path = ROOT / name
    with wave.open(str(path), 'wb') as out:
        out.setparams((1, 2, RATE, 0, 'NONE', 'not compressed'))
        out.writeframes(b''.join(struct.pack('<h', round(s / peak * level)) for s in samples))
    print(path)


write('countdown_tick.wav', tick(0.09, 1850, 70, 1), 16000)
write('countdown_tick_urgent.wav', tick(0.07, 2600, 95, 2), 20000)
