"""
Modèle de progression (plan 20) : niveaux attendus par palier de temps selon l'archétype de run,
XP que cela suppose, et comparaison avec des runs mesurées.

Usage :
    python3 tools/progression_model.py                         # courbes et archétypes cibles
    python3 tools/progression_model.py <dossier de mesure>...  # + niveaux mesurés (density-*.csv de measure_run.sh)
                                                               #   et simulation des leviers (LEVERS, PLAY)

La simulation part de l'XP ramassée minute par minute par le bot de mesure (run passive : il prend la première
carte, ne cherche ni combat ni orbe, ne meurt pas), puis applique à chaque archétype sa façon de jouer (PLAY)
et les leviers proposés (LEVERS), avec la courbe proposée (PROPOSED_CURVE). Ce sont des hypothèses de travail,
pas des mesures : elles servent à vérifier qu'un jeu de leviers donne l'écart voulu entre archétypes avant d'en
coder un seul.

La courbe actuelle est lue dans scripts/Progression/PlayerProgression.cs (constantes BaseXpToLevel et
XpScalingExponent, majoration des niveaux 1 à 5), pour rester alignée sur le jeu.
"""
from __future__ import annotations

import csv
import re
import sys
from dataclasses import dataclass, field
from pathlib import Path

PROGRESSION_CS = Path("scripts/Progression/PlayerProgression.cs")
PALIERS_MIN = (2, 5, 10, 15, 20, 30, 40, 45)

# Cibles proposées (à valider par Raphaël) : niveau atteint à chaque palier, en minutes.
# Raphaël, 28 septembre : une excellente run atteint 300 à 400 niveaux en 45 min, par vagues (boss, Résurgences).
ARCHETYPES: dict[str, dict[int, int]] = {
    "médiocre": {2: 4, 5: 10, 10: 18, 15: 25},
    "moyenne": {2: 4, 5: 11, 10: 21, 15: 32, 20: 40},
    "bonne": {2: 5, 5: 12, 10: 25, 15: 42, 20: 55, 30: 105, 40: 150},
    "excellente": {2: 5, 5: 13, 10: 30, 15: 60, 20: 90, 30: 190, 40: 300, 45: 380},
}

# Leviers proposés (plan 20 §6.5), destinés à data/ s'ils sont retenus. Valeurs à calibrer.
LEVERS = {
    "time_growth": 0.02,      # XP par créature × (1 + time_growth × minute) ; ×1,4 à 20 min
    "oblivion_xp": 0.5,       # XP × (1 + oblivion_xp × oubli sous le joueur), oubli = 1 − mémoire
    "crisis_xp": 2.0,         # multiplicateur d'XP pendant une Résurgence (70 s toutes les ~240 s dès 4 min)
    "peril_xp": 0.12,         # par point de Péril (0,08 aujourd'hui dans data/scaling/peril.json)
    "mid_boss_minute": 13,    # boss intermédiaire, après la 3e Résurgence
    "mid_boss_levels": {1: 2, 2: 4, 3: 7, 4: 10, 5: 15},  # niveaux gagnés selon le rang choisi
    "final_boss_minute": 22,  # l'Indicible (data/scaling/crises.json, boss_time_threshold_sec)
    "final_boss_levels": 8,
}

# Façon de jouer de chaque archétype, relative au bot de mesure. Hypothèses, à discuter.
#   harvest : XP tuée et ramassée par rapport au bot (build, gestion de la masse, ramassage des orbes) ;
#   oblivion : temps passé en zone oubliée par rapport au bot ; crisis : part d'une Résurgence jouée au cœur ;
#   peril : Péril atteint à 20 min ; build_xp : bonus d'XP de build atteint à 20 min ; boss_rank : rang choisi ;
#   death : minute de fin de run (V2 §5 : régulier 15–25 min, bon joueur 25–40 min).
PLAY = {
    "médiocre": {"harvest": 0.8, "oblivion": 0.6, "crisis": 0.3, "peril": 0, "build_xp": 0.0, "boss_rank": 1, "death": 17},
    "moyenne": {"harvest": 1.0, "oblivion": 1.0, "crisis": 0.5, "peril": 1, "build_xp": 0.0, "boss_rank": 2, "death": 25},
    "bonne": {"harvest": 1.25, "oblivion": 1.3, "crisis": 0.8, "peril": 3, "build_xp": 0.2, "boss_rank": 3, "death": 40},
    "excellente": {"harvest": 1.5, "oblivion": 1.6, "crisis": 1.0, "peril": 6, "build_xp": 0.6, "boss_rank": 5, "death": 45},
}


@dataclass
class Curve:
    """XP pour passer du niveau n au suivant : base × n^exponent, majorée aux niveaux 1 à 5 comme
    PlayerProgression.CalculateXpForLevel, et plafonnée à `cap` si donné."""
    base: float
    exponent: float
    cap: float | None = None
    _cumulative: list[float] = field(default_factory=lambda: [0.0, 0.0], repr=False)

    def cost(self, level: int) -> float:
        xp = self.base * level ** self.exponent
        if level <= 5:
            xp *= 1.65 + (1.20 - 1.65) * (level - 1) / 4
        return min(xp, self.cap) if self.cap else xp

    def cumulative(self, level: int) -> float:
        """XP totale pour atteindre `level` depuis le niveau 1."""
        while len(self._cumulative) <= level:
            self._cumulative.append(self._cumulative[-1] + self.cost(len(self._cumulative) - 1))
        return self._cumulative[level]

    def describe(self) -> str:
        text = f"{self.base:g} × niveau^{self.exponent:g}"
        return text + (f", plafonnée à {self.cap:g} XP par niveau" if self.cap else "")


def read_curve() -> Curve:
    text = PROGRESSION_CS.read_text()
    base = float(re.search(r"BaseXpToLevel\s*=\s*([\d.]+)f", text).group(1))
    exponent = float(re.search(r"XpScalingExponent\s*=\s*([\d.]+)f", text).group(1))
    return Curve(base, exponent)


def proposed_curve(current: Curve) -> Curve:
    """Courbe proposée (plan 20 §6.6) : identique jusqu'au niveau 41, puis 3 000 XP par niveau."""
    return Curve(current.base, current.exponent, cap=3000)


def measured_runs(folders: list[Path]) -> dict[str, dict[int, dict[str, str]]]:
    """Ligne du relevé de densité à chaque palier, par run mesurée."""
    runs = {}
    for folder in folders:
        for path in sorted(folder.glob("seed-*/density-*.csv")):
            with path.open() as handle:
                rows = {int(float(r["t"])): r for r in csv.DictReader(handle)}
            name = f"mesure {folder.name}/{path.stem.split('-')[-1]}"
            runs[name] = {p: row_at(rows, p * 60) for p in PALIERS_MIN if row_at(rows, p * 60)}
    return runs


def row_at(rows: dict[int, dict[str, str]], second: int) -> dict[str, str] | None:
    """Ligne de la seconde voulue ; le relevé d'une run de N s s'arrête à N − 1."""
    return rows.get(second) or rows.get(second - 1)


def measured_levels(runs: dict[str, dict[int, dict[str, str]]]) -> dict[str, dict[int, int]]:
    return {name: {p: int(row["level"]) for p, row in rows.items()} for name, rows in runs.items()}


def measured_flow(runs: dict[str, dict[int, dict[str, str]]]) -> None:
    """XP réellement ramassée (avant multiplicateur de Péril), morts et orbes au sol, par palier."""
    if not any("xp_gained" in row for rows in runs.values() for row in rows.values()):
        return
    print("\nMesure : XP ramassée par minute sur le palier, morts par minute, orbes au sol en fin de palier :\n")
    print("| Run | " + " | ".join(f"{a}→{b} min" for a, b in zip(PALIERS_MIN, PALIERS_MIN[1:])) + " |")
    print("|" + "---|" * len(PALIERS_MIN))
    for name, rows in runs.items():
        cells = []
        for a, b in zip(PALIERS_MIN, PALIERS_MIN[1:]):
            if a in rows and b in rows and "xp_gained" in rows[b]:
                xp = (float(rows[b]["xp_gained"]) - float(rows[a]["xp_gained"])) / (b - a)
                kills = (int(rows[b]["killed"]) - int(rows[a]["killed"])) / (b - a)
                cells.append(f"{xp:.0f} XP · {kills:.0f} morts · {rows[b]['xp_orbs']} orbes")
            else:
                cells.append("—")
        print(f"| {name} | " + " | ".join(cells) + " |")


def bot_minutes(folders: list[Path]) -> tuple[list[float], list[float]]:
    """XP ramassée et oubli sous le joueur, minute par minute, moyennés sur les runs mesurées."""
    xp_runs, oblivion_runs = [], []
    for folder in folders:
        for path in sorted(folder.glob("seed-*/density-*.csv")):
            with path.open() as handle:
                rows = {int(float(r["t"])): r for r in csv.DictReader(handle)}
            if "xp_gained" not in rows.get(60, {}):
                continue
            minutes = (max(rows) + 1) // 60
            xp = [0.0] + [float(row_at(rows, m * 60)["xp_gained"]) for m in range(1, minutes + 1)]
            xp_runs.append([xp[m] - xp[m - 1] for m in range(1, minutes + 1)])
            oblivion_runs.append([1 - float(row_at(rows, m * 60)["memory"]) for m in range(1, minutes + 1)])
    if not xp_runs:
        return [], []
    length = min(map(len, xp_runs))
    average = lambda runs: [sum(run[m] for run in runs) / len(runs) for m in range(length)]
    return average(xp_runs), average(oblivion_runs)


def level_after(level: int, carry: float, xp: float, curve: Curve) -> tuple[int, float]:
    carry += xp
    while carry >= curve.cost(level):
        carry -= curve.cost(level)
        level += 1
    return level, carry


def simulate(bot_xp: list[float], bot_oblivion: list[float], curve: Curve, levers: dict) -> dict[str, dict[int, int]]:
    """Niveau de chaque archétype aux paliers, avec les leviers donnés. Au-delà de la mesure, la dernière
    minute mesurée se prolonge avec la pente moyenne des cinq dernières."""
    slope = (bot_xp[-1] - bot_xp[-6]) / 5 if len(bot_xp) > 6 else 0
    result = {}
    for name, play in PLAY.items():
        level, carry, levels = 1, 0.0, {}
        for minute in range(1, max(PALIERS_MIN) + 1):
            if minute > play["death"]:
                break
            index = minute - 1
            flow = bot_xp[index] if index < len(bot_xp) else bot_xp[-1] + slope * (index - len(bot_xp) + 1)
            oblivion = bot_oblivion[min(index, len(bot_oblivion) - 1)]
            ramp = min(minute / 20, 1)
            crisis_share = 70 / 240 if minute > 4 else 0
            xp = (flow * play["harvest"]
                  * (1 + levers["time_growth"] * minute)
                  * (1 + levers["oblivion_xp"] * min(1, oblivion * play["oblivion"]))
                  * (1 + crisis_share * play["crisis"] * (levers["crisis_xp"] - 1))
                  * (1 + levers["peril_xp"] * play["peril"] * ramp)
                  * (1 + play["build_xp"] * ramp))
            level, carry = level_after(level, carry, xp, curve)
            for at, gained in ((levers["mid_boss_minute"], levers["mid_boss_levels"].get(play["boss_rank"], 0)),
                               (levers["final_boss_minute"], levers["final_boss_levels"])):
                if minute == at and gained:
                    bonus = curve.cumulative(level + gained) - curve.cumulative(level)
                    level, carry = level_after(level, carry, bonus, curve)
            if minute in PALIERS_MIN:
                levels[minute] = level
        result[name] = levels
    return result


def table(rows: dict[str, dict[int, int]], curve: Curve) -> None:
    print("| Run | " + " | ".join(f"{p} min" for p in PALIERS_MIN) + " |")
    print("|" + "---|" * (len(PALIERS_MIN) + 1))
    for name, levels in rows.items():
        cells = []
        for palier in PALIERS_MIN:
            level = levels.get(palier)
            cells.append("—" if level is None else f"{level} ({curve.cumulative(level) / 1000:.1f} k)")
        print(f"| {name} | " + " | ".join(cells) + " |")


def flow(rows: dict[str, dict[int, int]], curve: Curve) -> None:
    """XP par minute qu'il faut ramasser entre deux paliers pour tenir la cible, et niveaux gagnés par minute."""
    print("| Run | " + " | ".join(f"{a}→{b} min" for a, b in zip(PALIERS_MIN, PALIERS_MIN[1:])) + " |")
    print("|" + "---|" * len(PALIERS_MIN))
    for name, levels in rows.items():
        cells = []
        for a, b in zip(PALIERS_MIN, PALIERS_MIN[1:]):
            if a in levels and b in levels:
                gained = curve.cumulative(levels[b]) - curve.cumulative(levels[a])
                cells.append(f"{gained / (b - a):.0f} XP · {(levels[b] - levels[a]) / (b - a):.1f} niv.")
            else:
                cells.append("—")
        print(f"| {name} | " + " | ".join(cells) + " |")


def curve_table(curves: dict[str, Curve]) -> None:
    print("| Niveau | " + " | ".join(f"{name} : pour le suivant | {name} : cumulée" for name in curves) + " |")
    print("|---|" + "---|---|" * len(curves))
    for level in (5, 10, 20, 30, 40, 50, 100, 150, 200, 300, 400, 1000):
        cells = [f"{curve.cost(level):.0f} | {curve.cumulative(level):.0f}" for curve in curves.values()]
        print(f"| {level} | " + " | ".join(cells) + " |")


def main() -> None:
    folders = [Path(arg) for arg in sys.argv[1:]]
    current = read_curve()
    proposed = proposed_curve(current)
    print(f"Courbe actuelle : {current.describe()} ; proposée : {proposed.describe()}\n")
    curve_table({"actuelle": current, "proposée": proposed})

    runs = measured_runs(folders)
    rows = dict(ARCHETYPES)
    rows.update(measured_levels(runs))
    for label, curve in (("actuelle", current), ("proposée", proposed)):
        print(f"\nCibles et mesures, courbe {label} : niveau atteint (XP cumulée, en milliers)\n")
        table(rows, curve)
        print(f"\nXP à ramasser et niveaux gagnés par minute, courbe {label} :\n")
        flow(rows, curve)
    measured_flow(runs)

    bot_xp, bot_oblivion = bot_minutes(folders)
    if bot_xp:
        status_quo = dict(LEVERS, time_growth=0.0, oblivion_xp=0.0, crisis_xp=1.0, peril_xp=0.08, mid_boss_levels={},
                          final_boss_levels=0)
        print("\nSimulation, jeu actuel (courbe actuelle ; seuls la façon de jouer et le Péril à 8 % changent) :\n")
        table(simulate(bot_xp, bot_oblivion, current, status_quo), current)
        print(f"\nSimulation, courbe proposée et leviers {LEVERS} :\n")
        simulated = simulate(bot_xp, bot_oblivion, proposed, LEVERS)
        table(simulated, proposed)
        print()
        flow(simulated, proposed)


if __name__ == "__main__":
    main()
