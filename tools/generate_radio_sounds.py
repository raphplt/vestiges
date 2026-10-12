#!/usr/bin/env python3
"""
Sons des radios et des cabines abandonnées (plan 39 C1), générés sans source extérieure.

- Message d'évacuation : un fichier par mot ou groupe de mots, synthétisé par espeak-ng (voix française), puis passé
  dans un poste de radio (bande étroite, saturation douce, léger flottement). Les groupes servent la diction : un mot
  qui se lie au suivant (« grands axes ») reste avec lui. `AbandonedVoicesDirector` les enchaîne et en perd de plus en
  plus quand la zone s'oublie. L'ordre de lecture est dans `data/world/abandoned_voices.json` ; ces identifiants
  doivent y correspondre.
- Grésillement de fond en boucle, bip d'alerte avant le message, sonnerie de téléphone à cloches (une sonnerie de
  1,5 s puis 3,5 s de silence, en boucle).

Le résultat est déterministe : régénérer donne les mêmes fichiers. Une voix enregistrée peut remplacer la synthèse
plus tard sous les mêmes noms, sans toucher au code.

    python3 tools/generate_radio_sounds.py
"""
from __future__ import annotations

import subprocess
import tempfile
import wave
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parent.parent
OUTPUT = ROOT / "assets/audio/sfx/radio"
RATE = 22050

# Deux messages, phrase par phrase ; une virgule garde l'intonation suspendue d'un mot qui n'achève pas la phrase.
MESSAGES = [
    [
        [("avis", "Avis,"), ("a_la", "à la"), ("population", "population.")],
        [("ordre", "Ordre,"), ("evacuation", "d'évacuation,"), ("immediate", "immédiate.")],
        [("quittez", "Quittez,"), ("la_ville", "la ville,"), ("par_les", "par les"), ("grands_axes", "grands axes.")],
        [("ne_revenez_pas", "Ne revenez pas,"), ("en_arriere", "en arrière.")],
    ],
    [
        [("ceci", "Ceci,"), ("n_est_pas", "n'est pas"), ("un_exercice", "un exercice.")],
        [("rejoignez", "Rejoignez,"), ("les_points", "les points"), ("de_rassemblement", "de rassemblement.")],
        [("n_emportez", "N'emportez,"), ("que", "que"), ("le_necessaire", "le nécessaire.")],
        [("ne_vous", "Ne vous"), ("retournez_pas", "retournez pas.")],
    ],
]


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


def fade(signal: np.ndarray, seconds: float = 0.008) -> np.ndarray:
    n = min(len(signal) // 2, int(seconds * RATE))
    ramp = np.linspace(0.0, 1.0, n)
    signal = signal.copy()
    signal[:n] *= ramp
    signal[-n:] *= ramp[::-1]
    return signal


def write(path: Path, signal: np.ndarray) -> None:
    pcm = np.clip(np.round(signal * 32767.0), -32768, 32767).astype("<i2")
    with wave.open(str(path), "wb") as out:
        out.setnchannels(1)
        out.setsampwidth(2)
        out.setframerate(RATE)
        out.writeframes(pcm.tobytes())


def radio(signal: np.ndarray, rng: np.random.Generator) -> np.ndarray:
    """Poste de radio : bande téléphonique, saturation douce du petit haut-parleur, flottement de la réception."""
    padded = np.concatenate([np.zeros(RATE // 10), signal, np.zeros(RATE // 10)])
    narrow = band(padded, 380.0, 2900.0)
    narrow /= np.max(np.abs(narrow)) + 1e-9
    driven = np.tanh(2.6 * narrow) / np.tanh(2.6)
    t = np.arange(len(driven)) / RATE
    flutter = 1.0 + 0.12 * np.sin(2 * np.pi * rng.uniform(4.0, 7.0) * t + rng.uniform(0, 2 * np.pi))
    return band(driven * flutter, 320.0, 3300.0)


def trim(signal: np.ndarray, threshold: float = 0.02) -> np.ndarray:
    loud = np.flatnonzero(np.abs(signal) > threshold * np.max(np.abs(signal)))
    return signal[loud[0]:loud[-1] + 1]


def speak(text: str) -> np.ndarray:
    with tempfile.TemporaryDirectory() as folder:
        path = Path(folder) / "word.wav"
        subprocess.run(["espeak-ng", "-v", "fr", "-s", "128", "-p", "30", "-a", "150", "-w", str(path), text], check=True)
        with wave.open(str(path), "rb") as source:
            if source.getframerate() != RATE:
                raise SystemExit(f"espeak-ng rend à {source.getframerate()} Hz, {RATE} attendus")
            samples = np.frombuffer(source.readframes(source.getnframes()), dtype="<i2")
    return samples.astype(np.float64) / 32768.0


def words() -> None:
    for message in MESSAGES:
        for sentence in message:
            for clip_id, text in sentence:
                rng = np.random.default_rng(sum(map(ord, clip_id)))
                voice = trim(radio(trim(speak(text)), rng), 0.03)
                write(OUTPUT / f"radio_{clip_id}.wav", fade(level(voice, -17.0)))


def static_loop(seconds: float = 6.0) -> None:
    """Souffle de la porteuse, qui respire lentement, et crépitements épars ; boucle sans raccord."""
    rng = np.random.default_rng(3901)
    n = int(seconds * RATE)
    t = np.arange(n) / RATE
    hiss = band(rng.normal(size=n), 500.0, 5200.0, order=2)
    hiss /= np.sqrt(np.mean(hiss ** 2))
    # Fréquences entières sur la durée de la boucle : la respiration se referme sur elle-même.
    breath = 1.0 + 0.25 * np.sin(2 * np.pi * 2 / seconds * t) + 0.15 * np.sin(2 * np.pi * 7 / seconds * t + 1.3)
    clicks = np.zeros(n)
    for index in rng.integers(0, n, size=int(seconds * 9)):
        clicks[index] = rng.choice([-1.0, 1.0]) * rng.uniform(2.0, 9.0)
    kernel = np.zeros(n)
    kernel[:60] = np.exp(-np.arange(60) / 12.0)
    crackle = np.fft.irfft(np.fft.rfft(clicks) * np.fft.rfft(kernel), n)
    write(OUTPUT / "radio_static.wav", level(band(hiss * breath + crackle, 300.0, 6000.0, order=2), -22.0, -6.0))


def chime() -> None:
    """Trois bips d'alerte avant le message, passés dans le poste."""
    beep, gap = int(0.17 * RATE), int(0.11 * RATE)
    t = np.arange(beep) / RATE
    tone = (np.sin(2 * np.pi * 880.0 * t) + 0.3 * np.sin(2 * np.pi * 1760.0 * t)) * fade(np.ones(beep), 0.012)
    signal = np.concatenate([np.concatenate([tone, np.zeros(gap)]) for _ in range(3)])
    write(OUTPUT / "radio_chime.wav", fade(level(radio(signal, np.random.default_rng(3902)), -18.0)))


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
    words()
    static_loop()
    chime()
    phone_ring()
    print(f"[generate_radio_sounds] {sum(len(s) for m in MESSAGES for s in m)} mots, grésillement, bip, sonnerie → {OUTPUT.relative_to(ROOT)}")


if __name__ == "__main__":
    main()
