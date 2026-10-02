#!/usr/bin/env bash
# Dessins soumis au GPU, hors champ et dans le champ ; ne mesure pas les FPS.
set -euo pipefail
cd "$(dirname "$0")/.."
source tools/lib/validation.sh
validation_entry "$0" "$@"
SCREEN_ARGS=$(godot_screen_args)
GODOT="${GODOT_BIN:-godot-mono}"
OUTPUT=$(abs_path "${1:?dossier de sortie requis}")
if [[ -d "$OUTPUT" && -n "$(ls -A "$OUTPUT")" ]]; then echo "Choisir un dossier neuf : $OUTPUT" >&2; exit 1; fi
mkdir -p "$OUTPUT"
TEST_DIR=$(mktemp -d "${TMPDIR:-/tmp}/vestiges-shaders.XXXXXX")
trap 'validation_cleanup "$TEST_DIR"' EXIT
isolate_godot_profile "$TEST_DIR"
python3 - "$OUTPUT" <<'PYCHECK'
import hashlib, json, pathlib, re, sys
root=pathlib.Path.cwd()
pattern=re.compile(r'res://assets/shaders/[^"\s]+\.gdshader')
warmup=root/'scripts/Infrastructure/ShaderWarmup.cs'
if warmup.exists():
    catalog=set(pattern.findall(warmup.read_text()))
    sources={}
    for source in (root/'scripts').rglob('*.cs'):
        if source==warmup: continue
        for shader in pattern.findall(source.read_text()):
            sources.setdefault(shader,[]).append(str(source.relative_to(root)))
    hub_only={'res://assets/shaders/hub_forgotten.gdshader','res://assets/shaders/outline.gdshader'}
    missing=sorted(set(sources)-catalog-hub_only)
    report={'catalog':sorted(catalog),'consumers':sources,'hub_only':sorted(hub_only),'missing':missing,
        'sha256':hashlib.sha256(warmup.read_bytes()).hexdigest()}
    (pathlib.Path(sys.argv[1])/'coverage.json').write_text(json.dumps(report,indent=2)+'\n')
    if missing: raise SystemExit('Shaders de run sans préchauffage : '+', '.join(missing))
PYCHECK
validation_prepare "$OUTPUT"
validation_run "${VALIDATION_RUN_TIMEOUT:-120}" "$OUTPUT/run.log" '^\[ShaderWarmupAudit\] RESULT valid=true .+$' "$GODOT" --path . --windowed $SCREEN_ARGS --resolution 1280x720 --audio-driver Dummy \
    --rendering-method gl_compatibility res://tools/tests/ShaderWarmupAudit.tscn \
    -- --dev --output "$OUTPUT/shaders.json" ${SHADER_AUDIT_ARGS:-}
python3 - "$OUTPUT/shaders.json" <<'PYCHECK'
import json,sys
with open(sys.argv[1]) as handle: report=json.load(handle)
if report.get('valid') is not True or not report.get('samples'):
    raise SystemExit('Préchauffage invalide ou sans échantillon.')
PYCHECK
rg '^\[ShaderWarmupAudit\]' "$OUTPUT/run.log"
