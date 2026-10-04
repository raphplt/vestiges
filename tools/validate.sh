#!/usr/bin/env bash
# Validation headless globale : un build/import, scènes séquentielles et résultat par suite.
# Usage : tools/validate.sh [dossier neuf] [noms de suites...]
set -euo pipefail
cd "$(dirname "$0")/.."
source tools/lib/validation.sh
validation_entry "$0" "$@"
OUTPUT=$(abs_path "${1:-/tmp/vestiges-validation-$(date +%Y%m%d-%H%M%S)}")
if [[ $# -gt 0 ]]; then shift; fi
tests=(smoke perk_contracts perk_acquisition perk_effects objects weapons enemy_abilities small_places
    field_bonuses cone temporal erasure_active cartography choice_screen ui_art music movement
    movement-integration dev_mode development_tools dev_release saves progression-model launchers)
if [[ $# -gt 0 ]]; then tests=("$@"); fi
for name in "${tests[@]}"; do
    case "$name" in
        smoke|perk_contracts|perk_acquisition|perk_effects|objects|weapons|enemy_abilities|small_places|field_bonuses|cone|temporal|erasure_active|cartography|choice_screen|ui_art|music|movement|movement-integration|dev_mode|development_tools|dev_release|saves|progression-model|launchers) ;;
        *) echo "Suite inconnue : $name" >&2; exit 1 ;;
    esac
done
python3 - "$OUTPUT" "${tests[@]}" <<'PYCHECK'
from pathlib import Path
import sys
root=Path(sys.argv[1])
if len(set(sys.argv[2:]))!=len(sys.argv[2:]): raise SystemExit('Suites dupliquées.')
if root.exists() and any(root.iterdir()): raise SystemExit('Choisir un dossier de validation neuf.')
root.mkdir(parents=True,exist_ok=True)
PYCHECK
TEST_DIR=$(mktemp -d "${TMPDIR:-/tmp}/vestiges-validation-profile.XXXXXX")
trap 'validation_cleanup "$TEST_DIR"' EXIT
isolate_godot_profile "$TEST_DIR"

snapshot() {
    python3 - "$1" <<'PY'
import hashlib,json
from pathlib import Path
import sys
root=Path.cwd()
files=set()
for folder,patterns in {'scripts':['*.cs','*.gd'], 'tools':['*.cs','*.gd','*.tscn','*.sh','*.py'],
        'addons':['*.gd','plugin.cfg'],'data':['*.json'],'scenes':['*.tscn'],'assets':['*.gdshader','*manifest.json']}.items():
    for pattern in patterns: files.update((root/folder).rglob(pattern))
files.update(p for p in (root/'Vestiges.csproj',root/'project.godot',root/'tools/.gdignore') if p.exists())
hashes={str(p.relative_to(root)):hashlib.sha256(p.read_bytes()).hexdigest() for p in sorted(files)}
Path(sys.argv[1]).write_text(json.dumps(hashes,indent=2)+'\n')
PY
}
snapshot "$OUTPUT/manifest-before.json"
validation_prepare "$OUTPUT"
export VESTIGES_VALIDATION_PREPARED_ROOT="$(pwd -P)"
: > "$OUTPUT/results.tsv"
failed=0
for name in "${tests[@]}"; do
    echo "== $name"
    command_args=()
    case "$name" in
        smoke) command_args=(tools/smoke_test.sh) ;;
        movement-integration) command_args=(tools/test_movement.sh --run-integration) ;;
        development_tools) command_args=(tools/test_development_tools.sh) ;;
        progression-model) command_args=(python3 tools/tests/test_progression_model.py) ;;
        launchers) command_args=(python3 tools/tests/test_validation_launchers.py) ;;
        cone|temporal|erasure_active|field_bonuses|music) command_args=("tools/test_$name.sh" "$OUTPUT/$name") ;;
        *) command_args=("tools/test_$name.sh") ;;
    esac
    exit_code=0
    "${command_args[@]}" >"$OUTPUT/$name.log" 2>&1 || exit_code=$?
    printf '%s\t%s\n' "$name" "$exit_code" >> "$OUTPUT/results.tsv"
    if [[ $exit_code -ne 0 ]]; then
        failed=1
        echo "ÉCHEC : $name (code $exit_code)"
        tail -25 "$OUTPUT/$name.log"
    else
        echo "OK : $name"
    fi
done
snapshot "$OUTPUT/manifest-after.json"
python3 - "$OUTPUT" <<'PY' || failed=1
import json
from pathlib import Path
import sys
root=Path(sys.argv[1])
before=json.loads((root/'manifest-before.json').read_text()); after=json.loads((root/'manifest-after.json').read_text())
changed=sorted(key for key in before.keys()|after.keys() if before.get(key)!=after.get(key))
results=[{'name':name,'exit_code':int(code)} for name,code in
    (line.split('\t') for line in (root/'results.tsv').read_text().splitlines())]
report={'results':results,'passed':sum(row['exit_code']==0 for row in results),'expected':len(results),'source_changes':changed}
(root/'validation.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
print(f"Validation : {report['passed']}/{report['expected']} suites ; sources modifiées : {len(changed)}.")
if changed: print('État source changé pendant les tests : '+', '.join(changed),file=sys.stderr)
sys.exit(1 if changed or any(row['exit_code'] for row in results) else 0)
PY
echo "Preuves : $OUTPUT"
exit "$failed"
