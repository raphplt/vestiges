#!/usr/bin/env python3
"""Pannes injectées aux frontières des lanceurs, sans moteur ni profil personnel."""
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import time
import unittest

ROOT = Path(__file__).resolve().parents[2]
FAKE_ENGINE = r'''
import os,sys,time
from pathlib import Path
args=sys.argv[1:]
def arg(name,default=''):
    return args[args.index(name)+1] if name in args else default
def trace(name):
    with open(os.environ['FAKE_TRACE'],'a') as handle: handle.write(name+'\n')
if '--import' in args:
    trace('import')
    mode=os.environ.get('FAKE_IMPORT','ok')
    if mode=='exit': sys.exit(42)
    if mode=='error': print('ERROR: import invalide')
    if mode!='missing': print('[ DONE ] first_scan_filesystem')
    else: print('Godot Engine')
    sys.exit(0)
trace('run')
if os.environ.get('FAKE_MUTATE_SOURCE'): Path('tools/mutated.gd').write_text('extends Node\n')
scene=next((Path(arg).stem for arg in args if arg.endswith('.tscn')),'Unknown')
mode=os.environ.get('FAKE_RUN','ok')
if os.environ.get('FAKE_FAIL_SCENE') and scene!=os.environ['FAKE_FAIL_SCENE']: mode='ok'
if mode=='timeout': time.sleep(300)
if mode=='silent': sys.exit(0)
if mode=='missing': print('Godot Engine'); sys.exit(0)
if scene=='RunObservation':
    seed,seconds=arg('--seed'),arg('--seconds')
    output=Path(arg('--output'));output.mkdir(parents=True,exist_ok=True)
    if mode!='missing_csv':
        rows=[] if mode=='empty_csv' else list(range(1,int(seconds)+1))
        if mode=='partial_csv': rows=[0]
        (output/f'density-{seed}.csv').write_text('t,visible,near600,hit_damage\n'+''.join(f'{t},2,3,0\n' for t in rows))
    result=f'[RunObservation] RESULT seed={seed} seconds={seconds} level=1'
elif scene=='HubSmokeRegression': result=f'[HubSmokeRegression] RESULT failures=0 frames={arg("--frames")}'
elif scene=='ShaderWarmupAudit':
    import json
    Path(arg('--output')).write_text(json.dumps({'valid':mode!='invalid_json','samples':[{'draw_calls':1}]}))
    result=f'[ShaderWarmupAudit] RESULT valid=true {arg("--output")}'
else: result=f'[{scene}] RESULT failures=0'
if mode=='error': print('ERROR: erreur runtime')
if mode=='noise_error': print('ERROR: erreur runtime contenant steam_api')
if mode=='assertion': print(f'[{scene}] FAIL assertion avant la fin')
print(result)
if mode=='duplicate': print(result)
if mode=='exit' or (mode=='partial' and arg('--seed')=='7'): sys.exit(42)
'''


class Launchers(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory(prefix="vestiges-launcher-fixture-")
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)
        (self.root / "tools" / "lib").mkdir(parents=True)
        for pattern in ("*.sh", "*.py", "lib/*.sh"):
            for source in (ROOT / "tools").glob(pattern):
                shutil.copy2(source, self.root / "tools" / source.relative_to(ROOT / "tools"))
        binary = self.root / "bin"
        binary.mkdir()
        self.engine = binary / "godot"
        self.engine.write_text(f"#!{sys.executable}\n" + FAKE_ENGINE)
        self.engine.chmod(0o755)
        dotnet = binary / "dotnet"
        dotnet.write_text(f"#!{sys.executable}\nimport os\nwith open(os.environ['FAKE_TRACE'],'a') as f: f.write('build\\n')\nprint('0 Warning(s)')\n")
        dotnet.chmod(0o755)
        self.trace = self.root / "trace.txt"
        self.env = {key: value for key, value in os.environ.items() if not key.startswith("VESTIGES_VALIDATION_")}
        self.env.update(PATH=str(binary) + os.pathsep + self.env["PATH"], GODOT_BIN=str(self.engine),
                        FAKE_TRACE=str(self.trace), VALIDATION_RUN_TIMEOUT="1", VALIDATION_IMPORT_TIMEOUT="2")
        # Tester aussi le slash terminal de TMPDIR présent sur le Bash macOS.
        self.env["TMPDIR"] = str(self.root) + "/"

    def run_script(self, script, *args, **changes):
        return subprocess.run(["bash", "tools/" + script, *map(str, args)], cwd=self.root,
                              env=self.env | changes, text=True, capture_output=True, timeout=30)

    def test_smoke_rejects_engine_and_import_failures(self):
        for key, mode in (("FAKE_RUN", "exit"), ("FAKE_RUN", "silent"), ("FAKE_RUN", "missing"),
                          ("FAKE_RUN", "error"), ("FAKE_RUN", "noise_error"), ("FAKE_RUN", "duplicate"),
                          ("FAKE_RUN", "assertion"), ("FAKE_RUN", "timeout"), ("FAKE_IMPORT", "exit"),
                          ("FAKE_IMPORT", "error"), ("FAKE_IMPORT", "missing")):
            with self.subTest(key=key, mode=mode):
                result = self.run_script("smoke_test.sh", "2", **{key: mode})
                self.assertNotEqual(result.returncode, 0, result.stdout + result.stderr)
                self.assertNotIn("✓ Smoke test OK", result.stdout)
        result = self.run_script('smoke_test.sh', '2', GODOT_BIN=str(self.root/'missing-engine'))
        self.assertNotEqual(result.returncode, 0)

    def test_success_releases_lock_and_main_integration_runs(self):
        for script, args in (("smoke_test.sh", ["2"]), ("test_movement.sh", ["--run-integration"]),
                             ("smoke_test.sh", ["2"])):
            result = self.run_script(script, *args)
            self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertEqual(self.trace.read_text().splitlines().count("run"), 3)

    def test_measurements_reject_partial_and_missing_artifact(self):
        for mode in ("partial", "missing_csv", "empty_csv", "partial_csv", "duplicate", "error"):
            with self.subTest(mode=mode):
                result = self.run_script("measure_run.sh", self.root / mode, "2", "7 42",
                                         FAKE_RUN=mode, MEASURE_JOBS="2")
                self.assertNotEqual(result.returncode, 0, result.stdout + result.stderr)
                report = json.loads((self.root / mode / "validation.json").read_text())
                self.assertEqual(report["expected"], 2)
                self.assertTrue(any(not row["valid"] for row in report["results"]))

    def test_rendered_launchers_check_artifacts_and_repetitions(self):
        result = self.run_script('measure_density.sh', self.root/'rendered', '7', '42', SECONDS_PER_RUN='2')
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        result = self.run_script('measure_density.sh', self.root/'rendered-failed', '7', '42',
                                 SECONDS_PER_RUN='2', FAKE_RUN='partial')
        self.assertNotEqual(result.returncode, 0)
        for mode in ('ok', 'invalid_json'):
            result = self.run_script('test_shader_warmup.sh', self.root/f'shader-{mode}', FAKE_RUN=mode)
            self.assertEqual(result.returncode == 0, mode == 'ok', result.stdout + result.stderr)
        result = self.run_script('benchmark_movement.sh', self.root/'zero-benchmark', BENCH_REPEATS='0')
        self.assertNotEqual(result.returncode, 0)

    def test_measurements_require_new_output_and_unique_seeds(self):
        output = self.root / "measured"
        result = self.run_script("measure_run.sh", output, "2", "7 42")
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertNotEqual(self.run_script("measure_run.sh", output, "2", "7 42").returncode, 0)
        self.assertNotEqual(self.run_script("measure_run.sh", self.root / "duplicate-seed", "2", "7 7").returncode, 0)

    def test_ab_rejects_missing_invalid_and_partial_passes(self):
        output = self.root / "ab"
        output.mkdir()
        command = [sys.executable, "tools/summarize_bench_ab.py", str(output), "1"]
        def summarize():
            return subprocess.run(command, cwd=self.root, capture_output=True)
        self.assertNotEqual(summarize().returncode, 0)
        for side in ("base", "current"):
            folder = output / f"{side}-1"
            folder.mkdir()
            for resolution in ("1280x720", "1920x1080"):
                (folder / f"{resolution}-1-baseline.json").write_text(json.dumps(
                    {"valid": True, "frames": {"fps": 60, "p99_ms": 20}}))
        self.assertEqual(summarize().returncode, 0)
        path = output / "current-1" / "1920x1080-1-baseline.json"
        path.write_text('{"valid":false}')
        self.assertNotEqual(summarize().returncode, 0)
        self.assertNotEqual(self.run_script("bench_ab.sh", "HEAD", self.root / "zero-ab", "0").returncode, 0)

    def test_global_shares_build_import_and_reports_each_suite(self):
        result = self.run_script("validate.sh", self.root / "global", "smoke", "ui_art", "movement-integration")
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        trace = self.trace.read_text().splitlines()
        self.assertEqual((trace.count("build"), trace.count("import"), trace.count("run")), (1, 1, 3))
        report = json.loads((self.root / "global" / "validation.json").read_text())
        self.assertEqual((report["passed"], report["expected"], report["source_changes"]), (3, 3, []))
        result = self.run_script("validate.sh", self.root / "global-failed", "smoke", "ui_art",
                                 FAKE_RUN="exit", FAKE_FAIL_SCENE="UiArtRegression")
        self.assertNotEqual(result.returncode, 0)
        report = json.loads((self.root / "global-failed" / "validation.json").read_text())
        self.assertEqual((report["passed"], report["expected"]), (1, 2))
        result = self.run_script('validate.sh', self.root/'global-mutated', 'smoke', FAKE_MUTATE_SOURCE='1')
        self.assertNotEqual(result.returncode, 0)
        report = json.loads((self.root/'global-mutated'/'validation.json').read_text())
        self.assertEqual(report['source_changes'], ['tools/mutated.gd'])

    def test_concurrent_validation_does_not_start_another_command(self):
        ready = self.root / "ready"
        holder = subprocess.Popen([sys.executable, "tools/with_validation_lock.py", sys.executable, "-c",
                                   "from pathlib import Path; import time; Path('ready').touch(); time.sleep(1)"],
                                  cwd=self.root, env=self.env, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
        try:
            for _ in range(100):
                if ready.exists():
                    break
                time.sleep(0.01)
            self.assertTrue(ready.exists())
            result = self.run_script("smoke_test.sh", "2")
            self.assertNotEqual(result.returncode, 0)
            self.assertFalse(self.trace.exists())
        finally:
            holder.wait(timeout=5)
        self.assertEqual(self.run_script("smoke_test.sh", "2").returncode, 0)


if __name__ == "__main__":
    unittest.main(verbosity=2)
