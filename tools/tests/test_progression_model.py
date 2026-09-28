"""Régressions du modèle : la provenance interdit de réappliquer les sources R1-A."""
import importlib.util
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

MODEL_PATH = Path(__file__).resolve().parents[1] / "progression_model.py"
spec = importlib.util.spec_from_file_location("progression_model", MODEL_PATH)
model = importlib.util.module_from_spec(spec)
sys.modules[spec.name] = model
spec.loader.exec_module(model)


class ProgressionProvenanceTests(unittest.TestCase):
    def setUp(self):
        self.curve = model.Curve(100, 0, early_levels=0)
        self.levers = dict(model.LEVERS, mid_boss_levels={}, final_boss_levels=0)
        self.without_sources = dict(self.levers, time_growth=0, oblivion_xp=0, crisis_xp=1)
        self.xp = [1000.0] * 45
        self.oblivion = [.5] * 45

    def simulate(self, levers, provenance):
        return model.simulate(self.xp, self.oblivion, self.curve, levers, xp_provenance=provenance)

    def test_post_r1a_does_not_apply_source_bonuses_again(self):
        self.assertEqual(self.simulate(self.levers, "post-r1-a"),
                         self.simulate(self.without_sources, "post-r1-a"))

    def test_pre_r1a_still_models_new_source_bonuses(self):
        with_sources = self.simulate(self.levers, "pre-r1-a")
        without_sources = self.simulate(self.without_sources, "pre-r1-a")
        self.assertGreater(with_sources["excellente"][45], without_sources["excellente"][45])
        self.assertEqual(without_sources, self.simulate(self.levers, "post-r1-a"))

    def test_unknown_provenance_cannot_guess_projection(self):
        with self.assertRaisesRegex(ValueError, "provenance"):
            self.simulate(self.levers, "unknown")
        with self.assertRaises(TypeError):
            model.simulate(self.xp, self.oblivion, self.curve, self.levers)

    def test_empty_or_misaligned_samples_rejected(self):
        for xp, oblivion in (([], []), ([1], []), ([1, 2], [0])):
            with self.assertRaises(ValueError):
                model.simulate(xp, oblivion, self.curve, self.levers, xp_provenance="post-r1-a")

    def test_legacy_cli_shows_measurements_without_assuming_provenance(self):
        with tempfile.TemporaryDirectory() as temp:
            folder = Path(temp)
            seed = folder / "seed-1"
            seed.mkdir()
            (seed / "density-1.csv").write_text("t,level,xp_gained,memory\n60,3,100,.8\n119,4,200,.8\n")
            result = subprocess.run([sys.executable, str(MODEL_PATH), str(folder)], cwd=MODEL_PATH.parent.parent,
                                    capture_output=True, text=True, check=True)
        self.assertIn("mesure ", result.stdout)
        self.assertIn("simulation désactivée", result.stdout)
        self.assertNotIn("Provenance déclarée", result.stdout)

    def test_weapon_budgets_follow_current_curve(self):
        curve = model.read_curve()
        for weapon_max, full_level in ((50, 204), (70, 284), (99, 400)):
            self.assertEqual(1 + 3 + 4 * (weapon_max - 1) + 4, full_level)
            level, carry = model.level_after(1, 0, curve.cumulative(full_level), curve)
            self.assertAlmostEqual(level + carry / curve.cost(level), full_level, places=8)


if __name__ == "__main__":
    unittest.main()
