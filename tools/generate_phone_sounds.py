#!/usr/bin/env python3
"""
Sonnerie des cabines abandonnées (plan 39 C1), générée sans source extérieure : deux cloches frappées en alternance,
1,5 s de sonnerie puis 3,5 s de silence, en boucle. Les radios et leur message d'évacuation ont été retirés
(DECISIONS §150). Le résultat est déterministe : régénérer donne le même fichier.

    python3 tools/generate_phone_sounds.py
"""
from __future__ import annotations

import wave
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parent.parent
OUTPUT = ROOT / "assets/audio/sfx/radio"
RATE = 22050


def db(value: float) -> float:
    return 10.0 ** (value / 20.0)



def band(signal: np.ndarray, low: float, high: float, order: int = 4) -> np.ndarray:
    """Passe-bande sans déphasage, dans le domaine des fréquences ; circulaire, donc sans raccord sur une boucle."""
    spectrum = np.fft.rfft(signal)
    freqs = np.maximum(np.fft.rfftfreq(len(signal), 1.0 / RATE), 1e-3)
    gain = 1.0 / np.sqrt(1.0 + (low / freqs) ** (2 * order)) / np.sqrt(1.0 + (freqs / high) ** (2 * order))
    return np.fft.irfft(spectrum * gain, len(signal))



def level(signal: np.ndarray, rms_db: float, peak_db: float = -3.0) -> np.ndarray:
    rms = np.sqrt(np.mean(signal ** 2)) + 1e-9
    signal = signal * (db(rms_db) / rms)
    peak = np.max(np.abs(signal)) + 1e-9
    return signal * min(1.0, db(peak_db) / peak)



def write(path: Path, signal: np.ndarray) -> None:
    pcm = np.clip(np.round(signal * 32767.0), -32768, 32767).astype("<i2")
    with wave.open(str(path), "wb") as out:
        out.setnchannels(1)
        out.setsampwidth(2)
        out.setframerate(RATE)
        out.writeframes(pcm.tobytes())



def phone_ring(ring: float = 1.5, period: float = 5.0) -> None:
    """Sonnerie à deux cloches frappées en alternance par le marteau, 1,5 s puis silence, en boucle de 5 s."""
    n = int(period * RATE)
    t = np.arange(n) / RATE
    signal = np.zeros(n)
    partials = ((1.0, 1.0), (2.06, 0.5), (2.74, 0.35), (3.93, 0.2))
    strike_period = 1.0 / 40.0
    strikes = np.arange(0.0, ring, strike_period)
    for k, start in enumerate(strikes):
        bell = 1180.0 if k % 2 == 0 else 1310.0
        since = t - start
        alive = since >= 0
        envelope = np.exp(-since[alive] / 0.12)
        for ratio, amplitude in partials:
            signal[alive] += amplitude * envelope * np.sin(2 * np.pi * bell * ratio * since[alive])
    write(OUTPUT / "phone_ring.wav", level(band(signal, 450.0, 7500.0), -16.0, -4.0))



def main() -> None:
    OUTPUT.mkdir(parents=True, exist_ok=True)
    phone_ring()
    print(f"[generate_phone_sounds] sonnerie → {OUTPUT.relative_to(ROOT)}")


if __name__ == "__main__":
    main()
