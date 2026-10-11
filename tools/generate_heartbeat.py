#!/usr/bin/env python3
"""Double battement sourd à 72 BPM ; boucle mono sans rupture, sans source externe."""
import math
from pathlib import Path
import struct
import wave

RATE = 48000
PERIOD = 60 / 72
DEST = Path(__file__).resolve().parents[1] / 'assets/audio/sfx/gameplay/heartbeat.wav'


def thump(t, duration, frequency):
    if not 0 <= t < duration:
        return 0.0
    envelope = math.sin(math.pi * t / duration) ** 2 * math.exp(-9 * t)
    phase = 2 * math.pi * (frequency * t - 35 * t * t)
    return envelope * (math.sin(phase) + 0.25 * math.sin(2 * phase))


samples = []
for i in range(round(RATE * PERIOD)):
    t = i / RATE
    samples.append(thump(t, 0.16, 66) + 0.7 * thump(t - 0.19, 0.12, 82))
peak = max(abs(s) for s in samples)
with wave.open(str(DEST), 'wb') as out:
    out.setparams((1, 2, RATE, 0, 'NONE', 'not compressed'))
    out.writeframes(b''.join(struct.pack('<h', round(s / peak * 23000)) for s in samples))
print(DEST)
