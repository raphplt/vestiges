"""
Modèle de progression (plan 20) : niveaux attendus par palier de temps selon l'archétype de run,
XP que cela suppose, et comparaison avec des runs mesurées.

Usage :
    python3 tools/progression_model.py                         # courbes et archétypes cibles
    python3 tools/progression_model.py <dossier de mesure>...  # mesures seules, provenance inconnue
    python3 tools/progression_model.py --xp-provenance pre-r1-a <dossier>...  # anciens relevés, sources à simuler
    python3 tools/progression_model.py --xp-provenance post-r1-a <dossier>... # sources déjà dans xp_gained

L’option de provenance s’applique à tous les dossiers du même appel ; ne pas mélanger les versions.
Les anciennes commandes restent valides pour afficher les mesures, mais ne déclenchent plus de projection
sans provenance explicite. L’API simulate exige elle aussi xp_provenance. Aucune provenance n’est déduite
du nom/date du dossier. En post-R1-A, les leviers temps/oubli/Résurgence sont déjà inclus dans la mesure :
on ne les réapplique pas et on ne prétend pas les retirer pour simuler un autre réglage. Les scénarios
harvest/build/Péril et niveaux bonus de boss restent des hypothèses, pas une reproduction de cette run.
Ne pas utiliser les bonus de boss proposés si le relevé contient déjà ces mêmes récompenses garanties.

La simulation part de l'XP ramassée minute par minute par le bot de mesure (run passive : il prend la première
carte, ne cherche ni combat ni orbe, ne meurt pas), puis applique à chaque archétype sa façon de jouer (PLAY)
et les leviers proposés (LEVERS), avec la courbe du jeu. Ce sont des hypothèses de travail,
pas des mesures : elles servent à vérifier qu'un jeu de leviers donne l'écart voulu entre archétypes avant d'en
les comparer en jeu.

La courbe est lue dans data/scaling/progression.json, comme le jeu (XpCurveConfig).
"""
from __future__ import annotations

import argparse
import csv
import json
from dataclasses import dataclass, field
from pathlib import Path

PROGRESSION_JSON = Path("data/scaling/progression.json")
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
    """XP pour passer du niveau n au suivant : base × n^exponent, majorée aux premiers niveaux et plafonnée
    à `cap` si donné, comme XpCurveConfig.CostOf."""
    base: float
    exponent: float
    cap: float | None = None
    early_levels: int = 5
    early_start: float = 1.65
    early_end: float = 1.2
    _cumulative: list[float] = field(default_factory=lambda: [0.0, 0.0], repr=False)

    def cost(self, level: int) -> float:
        xp = self.base * level ** self.exponent
        if level <= self.early_levels:
            t = (level - 1) / (self.early_levels - 1) if self.early_levels > 1 else 0
            xp *= self.early_start + (self.early_end - self.early_start) * t
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
    data = json.loads(PROGRESSION_JSON.read_text())
    return Curve(data["base_xp"], data["exponent"], data.get("max_xp_per_level"), data.get("early_levels", 5),
                 data.get("early_multiplier_start", 1.65), data.get("early_multiplier_end", 1.2))


def uncapped(curve: Curve) -> Curve:
    """La même courbe sans plafond : celle du jeu avant le 28 septembre (plan 20 §6.6)."""
    return Curve(curve.base, curve.exponent, None, curve.early_levels, curve.early_start, curve.early_end)


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
    """XP réellement ramassée (avant multiplicateur de Péril), morts, orbes au sol et temps pour tuer, par palier."""
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
                cell = f"{xp:.0f} XP · {kills:.0f} morts · {rows[b]['xp_orbs']} orbes"
                if "damage_dealt" in rows[b]:
                    cell += f" · {time_to_kill(rows[a], rows[b], b - a):.2f} s pour tuer"
                cells.append(cell)
            else:
                cells.append("—")
        print(f"| {name} | " + " | ".join(cells) + " |")


def time_to_kill(start: dict[str, str], end: dict[str, str], minutes: int) -> float:
    """PV moyen d'une créature apparue sur le palier, divisé par les dégâts infligés par seconde (plan 20, R1-T).
    Il baisse quand le joueur gagne en puissance plus vite que les créatures ; il doit rester stable ou monter."""
    spawned = int(end["spawned"]) - int(start["spawned"])
    dps = (float(end["damage_dealt"]) - float(start["damage_dealt"])) / (60 * minutes)
    if spawned <= 0 or dps <= 0:
        return float("nan")
    return (float(end["spawned_hp"]) - float(start["spawned_hp"])) / spawned / dps


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


def simulate(bot_xp: list[float], bot_oblivion: list[float], curve: Curve, levers: dict,
             *, xp_provenance: str) -> dict[str, dict[int, int]]:
    """Niveau de chaque archétype aux paliers, avec les leviers donnés. Au-delà de la mesure, la dernière
    minute mesurée se prolonge avec la pente moyenne des cinq dernières.
    post-r1-a conserve le revenu de source observé ; ces agrégats ne permettent pas de le désentrelacer."""
    if xp_provenance not in ("pre-r1-a", "post-r1-a"):
        raise ValueError("La simulation exige une provenance XP pre-r1-a ou post-r1-a.")
    if not bot_xp or len(bot_xp) != len(bot_oblivion):
        raise ValueError("Les relevés XP/oubli doivent être non vides et de même longueur.")
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
            source_multiplier = 1.0
            if xp_provenance == "pre-r1-a":
                source_multiplier = ((1 + levers["time_growth"] * minute)
                                     * (1 + levers["oblivion_xp"] * min(1, oblivion * play["oblivion"]))
                                     * (1 + crisis_share * play["crisis"] * (levers["crisis_xp"] - 1)))
            xp = (flow * source_multiplier * play["harvest"]
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
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("folders", nargs="*", type=Path)
    parser.add_argument("--xp-provenance", choices=("pre-r1-a", "post-r1-a"),
                        help="Version commune des sources XP des dossiers ; nécessaire pour simuler.")
    args = parser.parse_args()
    folders = args.folders
    game = read_curve()
    without_cap = uncapped(game)
    print(f"Courbe sans plafond : {without_cap.describe()} ; courbe du jeu : {game.describe()}\n")
    curve_table({"sans plafond": without_cap, "du jeu": game})

    runs = measured_runs(folders)
    rows = dict(ARCHETYPES)
    rows.update(measured_levels(runs))
    for label, curve in (("sans plafond", without_cap), ("du jeu", game)):
        print(f"\nCibles et mesures, courbe {label} : niveau atteint (XP cumulée, en milliers)\n")
        table(rows, curve)
        print(f"\nXP à ramasser et niveaux gagnés par minute, courbe {label} :\n")
        flow(rows, curve)
    measured_flow(runs)

    bot_xp, bot_oblivion = bot_minutes(folders)
    if bot_xp and args.xp_provenance is None:
        print("\nProvenance XP inconnue : mesures affichées, simulation désactivée. "
              "Préciser --xp-provenance pre-r1-a ou post-r1-a pour des dossiers homogènes.")
    elif bot_xp:
        print(f"\nProvenance déclarée : {args.xp_provenance} (hypothèses PLAY, pas une nouvelle mesure).")
        status_quo = dict(LEVERS, time_growth=0.0, oblivion_xp=0.0, crisis_xp=1.0, peril_xp=0.08, mid_boss_levels={},
                          final_boss_levels=0)
        if args.xp_provenance == "pre-r1-a":
            print("\nScénario historique sans plafond, sources sans R1-A (PLAY et Péril à 8 %) :\n")
        else:
            print("\nScénario sans plafond conservant les sources post-R1-A observées (PLAY et Péril à 8 %) :\n")
        table(simulate(bot_xp, bot_oblivion, without_cap, status_quo,
                       xp_provenance=args.xp_provenance), without_cap)
        if args.xp_provenance == "post-r1-a":
            print("\nCourbe du jeu : sources observées conservées, leviers temps/oubli/Résurgence non réappliqués. "
                  "PLAY/Péril et boss bonus proposés restent hypothétiques.\n")
        else:
            print(f"\nSimulation, courbe du jeu et leviers {LEVERS} :\n")
        simulated = simulate(bot_xp, bot_oblivion, game, LEVERS, xp_provenance=args.xp_provenance)
        table(simulated, game)
        print()
        flow(simulated, game)


if __name__ == "__main__":
    main()
