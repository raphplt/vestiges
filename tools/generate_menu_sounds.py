#!/usr/bin/env python3
"""Sons de menu faits des matières du jeu (plan 15 U1, DECISIONS §178) : survol en papier (quatre variantes), clic de
pierre, confirmation à la petite cloche de bronze, refus en corde étouffée.

Synthèses originales retenues par Raphaël sur la planche du 14 octobre. Graines et niveaux de la planche : regénérer
donne les mêmes octets que ceux écoutés. La cloche sonne fa, tonalité du thème du Hub (fa mineur), et ses échos
s'assourdissent et se trouent, signature de l'Effacement.
Usage : python3 tools/generate_menu_sounds.py
"""
import hashlib
import math
import wave
from pathlib import Path

import numpy as np

from audio_bank_audit import momentary_max

RATE = 48000
OUT = Path(__file__).resolve().parents[1] / "assets/audio/sfx/ui"
F5, C3 = 698.46, 130.81
# Sonie momentanée maximale des sons Kenney remplacés : les nouveaux restent au niveau du mix.
LEVEL_HOVER, LEVEL_CLICK, LEVEL_CONFIRM, LEVEL_REFUSE = -32.033, -34.006, -24.023, -29.518


def t_axis(duration):
    return np.arange(round(RATE * duration)) / RATE


def noise(n, rng):
    return rng.uniform(-1, 1, n)


def biquad(x, kind, freq, q=0.707):
    w = 2 * math.pi * freq / RATE
    alpha = math.sin(w) / (2 * q)
    cw = math.cos(w)
    if kind == "lp":
        b = [(1 - cw) / 2, 1 - cw, (1 - cw) / 2]
    elif kind == "hp":
        b = [(1 + cw) / 2, -(1 + cw), (1 + cw) / 2]
    else:  # passe-bande à gain de crête unitaire
        b = [alpha, 0, -alpha]
    a = [1 + alpha, -2 * cw, 1 - alpha]
    b = [v / a[0] for v in b]
    a1, a2 = a[1] / a[0], a[2] / a[0]
    y = np.zeros_like(x)
    x1 = x2 = y1 = y2 = 0.0
    for i, v in enumerate(x):
        out = b[0] * v + b[1] * x1 + b[2] * x2 - a1 * y1 - a2 * y2
        x2, x1, y2, y1 = x1, v, y1, out
        y[i] = out
    return y


def env(t, attack, decay):
    """Attaque linéaire courte (pas de claquement au premier échantillon), puis décroissance exponentielle."""
    rise = np.clip(t / attack, 0, 1) if attack > 0 else 1
    return rise * np.exp(-np.maximum(t - attack, 0) * decay)


def modes(t, freqs, decays, amps, rng):
    out = np.zeros_like(t)
    for f, d, a in zip(freqs, decays, amps):
        out += a * np.sin(2 * math.pi * f * t + rng.uniform(0, 2 * math.pi)) * np.exp(-t * d)
    return out


def mix_at(base, x, at):
    i = round(at * RATE)
    end = min(len(base), i + len(x))
    base[i:end] += x[:end - i]
    return base


def fade_tail(x, ms=8):
    n = min(len(x), round(RATE * ms / 1000))
    x[-n:] *= np.linspace(1, 0, n)
    return x


def pluck(freq, duration, rng, damping=0.996, bright=0.5):
    """Karplus-Strong : corde pincée, comme les cordes du thème du Hub."""
    n = round(RATE * duration)
    period = int(RATE / freq)
    buf = rng.uniform(-1, 1, period)
    buf = biquad(buf, "lp", 1200 + 3000 * bright)
    buf -= buf.mean()  # le filtre moyenneur garde la composante continue : l'ôter dès le départ
    out = np.zeros(n)
    prev = 0.0
    for i in range(n):
        v = buf[i % period]
        out[i] = v
        buf[i % period] = damping * (0.5 * v + 0.5 * prev)
        prev = v
    return fade_tail(out * env(t_axis(duration), 0.002, 0), 40)


def bell(prime, duration, rng, decay=5.0):
    """Petite cloche : bourdon, fondamentale, tierce mineure, quinte, nominal (accord de cloche classique)."""
    t = t_axis(duration)
    ratios = [0.5, 1.0, 1.19, 1.5, 2.0, 2.52, 3.0]
    amps = [0.5, 1.0, 0.55, 0.35, 0.45, 0.2, 0.12]
    out = sum(a * np.sin(2 * math.pi * prime * r * t) * np.exp(-t * decay * (0.6 + r * 0.5)) for r, a in zip(ratios, amps))
    strike = biquad(noise(len(t), rng), "bp", 3200, 1.2) * np.exp(-t * 300) * 0.4
    return fade_tail((out + strike) * env(t, 0.002, 0), 40)


def fragmented_echo(x, rng, taps, delay, gain):
    """Échos qui s'effritent : chaque reprise plus sourde, un peu plus basse, trouée de silences."""
    out = np.zeros(len(x) + round(RATE * delay * taps) + 1)
    out[:len(x)] += x
    src = x
    for k in range(1, taps + 1):
        src = biquad(src, "lp", 3600 / k)
        drift = 1 - 0.006 * k
        echo = np.interp(np.arange(len(src)) * drift, np.arange(len(src)), src)
        grain = round(RATE * rng.uniform(0.018, 0.032))
        gate = np.ones(len(echo))
        for g in range(0, len(echo), grain):
            if rng.random() < 0.18 * k:
                gate[g:g + grain] = 0.0
        # Bords des trous adoucis sur 8 ms : un trou franc claquerait.
        gate = np.convolve(gate, np.ones(384) / 384, mode="same")
        mix_at(out, echo * gate * gain ** k, delay * k)
    return out


def tape(x, rng, cutoff, wow=0.0015):
    """Patine de bande : léger pleurage et haut du spectre adouci."""
    t = np.arange(len(x)) / RATE
    warp = t + wow * np.sin(2 * math.pi * 3.1 * t + rng.uniform(0, 6)) / (2 * math.pi * 3.1)
    return biquad(np.interp(warp * RATE, np.arange(len(x)), x), "lp", cutoff)


def hover_paper(rng, variant):
    t = t_axis(0.06)
    base = biquad(biquad(noise(len(t), rng), "hp", 650), "lp", 3000 + 120 * variant)
    crackle = np.zeros(len(t))
    for at in sorted(rng.uniform(0.004, 0.035, 3)):
        crackle = mix_at(crackle, biquad(noise(200, rng), "bp", 2200, 2) * np.exp(-np.arange(200) / 40), at)
    return fade_tail(biquad(biquad(base * 0.6 + crackle * 1.4, "lp", 2800), "lp", 2800) * env(t, 0.005, 60))


def click_stone(rng):
    t = t_axis(0.14)
    thump = np.sin(2 * math.pi * 150 * t) * np.exp(-t * 45)
    ring = modes(t, [1240, 2050, 3310], [70, 95, 140], [0.5, 0.35, 0.18], rng)
    tick = biquad(noise(len(t), rng), "lp", 3000) * np.exp(-t * 600)
    return fade_tail((thump * 0.8 + ring + tick * 0.7) * env(t, 0.001, 0))


def confirm_bell(rng):
    dry = bell(F5, 0.7, rng, decay=4.2)
    return fade_tail(tape(fragmented_echo(dry, rng, taps=2, delay=0.2, gain=0.36), rng, cutoff=5200), 30)


def refuse_pluck(rng):
    p = pluck(C3, 0.22, rng, damping=0.94, bright=0.15)
    return fade_tail(biquad(p, "lp", 1100) * np.exp(-t_axis(0.22) * 12))


def write(path, x):
    with wave.open(str(path), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes((np.clip(x, -1, 1) * 32767).astype("<i2").tobytes())


def leveled(path, x, target):
    """Normalise la crête puis ajuste au niveau momentané visé ; deux passes, la mesure n'étant pas linéaire au bord."""
    x = biquad(x - x.mean(), "hp", 45)
    x = x / np.max(np.abs(x)) * 0.5
    for _ in range(2):
        write(path, x)
        x = x * 10 ** ((target - momentary_max(path)) / 20)
    if np.max(np.abs(x)) > 0.98:
        raise SystemExit(f"{path.name} : crête trop haute au niveau visé")
    write(path, x)


def rng_for(label):
    """Graine tirée du libellé du candidat sur la planche (« survolB0 »…), pour retrouver les octets écoutés."""
    return np.random.default_rng(int(hashlib.sha256(label.encode()).hexdigest()[:8], 16))


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    jobs = [(f"menu_hover_{v + 1}.wav", hover_paper(rng_for(f"survolB{v}"), v), LEVEL_HOVER) for v in range(4)]
    jobs += [("menu_click.wav", click_stone(rng_for("clicA0")), LEVEL_CLICK),
             ("menu_confirm.wav", confirm_bell(rng_for("confirmerC0")), LEVEL_CONFIRM),
             ("menu_refuse.wav", refuse_pluck(rng_for("refusC0")), LEVEL_REFUSE)]
    for name, samples, level in jobs:
        path = OUT / name
        leveled(path, samples, level)
        print(f"{path} {hashlib.sha256(path.read_bytes()).hexdigest()}")


if __name__ == "__main__":
    main()
