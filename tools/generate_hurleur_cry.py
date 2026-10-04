#!/usr/bin/env python3
"""Cri du Hurleur synthétisé (DECISIONS §66) : trois variantes à écouter, rien n'est branché dans le jeu.

Une voix par synthèse additive (harmoniques pondérées par des formants de voyelle), une courbe de hauteur qui monte
puis retombe, un vibrato lent, un souffle filtré, une réverbération par convolution et un passe-bas : une plainte
lointaine plutôt qu'un cri strident. Sortie : WAV 48 kHz stéréo 16 bits, crête à -6 dBFS.

Usage : python3 tools/generate_hurleur_cry.py <dossier>
"""

import sys
import wave
from pathlib import Path

import numpy as np

SR = 48_000
RNG = np.random.default_rng(14)

# Formants (fréquence Hz, largeur Hz, gain) : « ou » sombre et « o » un peu plus ouvert.
VOWEL_OU = [(330, 90, 1.0), (750, 120, 0.45), (2300, 200, 0.08)]
VOWEL_O = [(450, 100, 1.0), (850, 130, 0.55), (2500, 220, 0.10)]


def envelope(n, attack, release):
    env = np.ones(n)
    a, r = int(attack * SR), int(release * SR)
    env[:a] = np.sin(np.linspace(0, np.pi / 2, a)) ** 2
    env[n - r:] *= np.cos(np.linspace(0, np.pi / 2, r)) ** 2
    return env


def contour(times, points):
    """Courbe de hauteur lissée passant par (temps, Hz)."""
    t, f = zip(*points)
    return np.exp(np.interp(times, t, np.log(f)))


def voice(duration, pitch_points, vowel_from, vowel_to, vibrato_hz=5.0, vibrato_depth=0.018, harmonics=36):
    n = int(duration * SR)
    times = np.arange(n) / SR
    vib = 1.0 + vibrato_depth * np.sin(2 * np.pi * vibrato_hz * times) * np.clip(times / 0.35, 0, 1)
    f0 = contour(times, pitch_points) * vib
    phase = 2 * np.pi * np.cumsum(f0) / SR
    morph = np.clip(times / duration, 0, 1)
    out = np.zeros(n)
    for k in range(1, harmonics + 1):
        freq = k * f0
        gain = np.zeros(n)
        for (fa, wa, ga), (fb, wb, gb) in zip(vowel_from, vowel_to):
            fc = fa + (fb - fa) * morph
            bw = wa + (wb - wa) * morph
            g = ga + (gb - ga) * morph
            gain += g * np.exp(-0.5 * ((freq - fc) / bw) ** 2)
        gain *= (freq < 4000) / k ** 0.6
        out += gain * np.sin(k * phase + RNG.uniform(0, 2 * np.pi))
    return out * envelope(n, 0.18, 0.45)


def breath(n, level):
    noise = RNG.standard_normal(n)
    spectrum = np.fft.rfft(noise)
    freqs = np.fft.rfftfreq(n, 1 / SR)
    spectrum *= np.exp(-0.5 * ((freqs - 1200) / 700) ** 2)
    return np.fft.irfft(spectrum, n) * level * envelope(n, 0.25, 0.5)


def lowpass(signal, cutoff):
    spectrum = np.fft.rfft(signal)
    freqs = np.fft.rfftfreq(len(signal), 1 / SR)
    spectrum *= 1 / np.sqrt(1 + (freqs / cutoff) ** 8)
    return np.fft.irfft(spectrum, len(signal))


def reverb(signal, seconds, wet, seed):
    rng = np.random.default_rng(seed)
    n = int(seconds * SR)
    ir = rng.standard_normal(n) * np.exp(-np.arange(n) / SR * 6.5 / seconds)
    ir = lowpass(ir, 2500)
    ir /= np.max(np.abs(ir))
    total = len(signal) + n
    size = 1 << (total - 1).bit_length()
    tail = np.fft.irfft(np.fft.rfft(signal, size) * np.fft.rfft(ir, size), size)[:total]
    tail /= np.max(np.abs(tail)) + 1e-9
    dry = np.pad(signal, (0, n))
    dry /= np.max(np.abs(dry)) + 1e-9
    return dry * (1 - wet) + tail * wet


def stereo(left, right):
    peak = max(np.max(np.abs(left)), np.max(np.abs(right)))
    scale = 10 ** (-6 / 20) / peak
    return np.stack([left * scale, right * scale], axis=1)


def write(path, frames):
    data = (np.clip(frames, -1, 1) * 32767).astype(np.int16)
    with wave.open(str(path), "wb") as out:
        out.setnchannels(2)
        out.setsampwidth(2)
        out.setframerate(SR)
        out.writeframes(data.tobytes())


def variant_a():
    """Plainte : une voix, « ou » qui s'ouvre en « o », monte puis retombe sous son départ."""
    d = 1.4
    v = voice(d, [(0, 190), (0.35, 290), (0.8, 270), (1.4, 165)], VOWEL_OU, VOWEL_O)
    v += breath(len(v), 0.08)
    v = lowpass(v, 3500)
    return stereo(reverb(v, 1.1, 0.35, 1), reverb(v, 1.1, 0.35, 2))


def variant_b():
    """Appel : deux élans courts, le second plus haut, comme un signal lancé à la meute."""
    first = voice(0.55, [(0, 200), (0.2, 280), (0.55, 230)], VOWEL_OU, VOWEL_OU, vibrato_depth=0.01)
    second = voice(0.8, [(0, 230), (0.25, 330), (0.8, 210)], VOWEL_OU, VOWEL_O, vibrato_depth=0.02)
    gap = np.zeros(int(0.12 * SR))
    v = np.concatenate([first, gap, second])
    v += breath(len(v), 0.06)
    v = lowpass(v, 3200)
    return stereo(reverb(v, 1.0, 0.32, 3), reverb(v, 1.0, 0.32, 4))


def variant_c():
    """Chœur : trois voix graves légèrement désaccordées (fondamentale, quinte, octave), lointaines."""
    d = 1.5
    base = [(0, 140), (0.4, 205), (0.9, 190), (1.5, 120)]
    voices = []
    for ratio, detune, gain in [(1.0, 1.0, 1.0), (1.5, 1.006, 0.55), (2.0, 0.994, 0.35)]:
        points = [(t, f * ratio * detune) for t, f in base]
        voices.append(gain * voice(d, points, VOWEL_OU, VOWEL_O, vibrato_hz=4.2 + ratio, vibrato_depth=0.015))
    v = sum(voices) + breath(int(d * SR), 0.07)
    v = lowpass(v, 3000)
    return stereo(reverb(v, 1.4, 0.45, 5), reverb(v, 1.4, 0.45, 6))


def main():
    if len(sys.argv) != 2:
        sys.exit("Usage : python3 tools/generate_hurleur_cry.py <dossier>")
    target = Path(sys.argv[1])
    target.mkdir(parents=True, exist_ok=True)
    for name, build in [("A-plainte", variant_a), ("B-appel", variant_b), ("C-choeur", variant_c)]:
        path = target / f"hurleur-cri-{name}.wav"
        frames = build()
        write(path, frames)
        print(f"{path} : {len(frames) / SR:.2f} s")


if __name__ == "__main__":
    main()
