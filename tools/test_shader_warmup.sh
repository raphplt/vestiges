#!/usr/bin/env bash
# Dessins soumis au GPU, hors champ et dans le champ ; ne mesure pas les FPS.
set -euo pipefail
cd "$(dirname "$0")/.."
source tools/lib/portable.sh
SCREEN_ARGS=$(godot_screen_args)
GODOT="${GODOT_BIN:-godot-mono}"
OUTPUT=$(abs_path "${1:?dossier de sortie requis}")
mkdir -p "$OUTPUT"
TEST_DIR=$(mktemp -d "${TMPDIR:-/tmp}/vestiges-shaders.XXXXXX")
trap 'rm -rf "$TEST_DIR"' EXIT
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
dotnet build --nologo >"$OUTPUT/build.log"
"$GODOT" --headless --editor --import --path . >"$OUTPUT/import.log" 2>&1
run_timeout 120 "$GODOT" --path . --windowed $SCREEN_ARGS --resolution 1280x720 --audio-driver Dummy \
    --rendering-method gl_compatibility res://tools/tests/ShaderWarmupAudit.tscn \
    -- --dev --output "$OUTPUT/shaders.json" ${SHADER_AUDIT_ARGS:-} >"$OUTPUT/run.log" 2>&1
rg '\[ShaderWarmupAudit\] RESULT' "$OUTPUT/run.log"
ERRORS=$(rg '^(ERROR|SCRIPT ERROR)|Unhandled exception|System\.[A-Za-z]+Exception' "$OUTPUT/run.log" | rg -v 'steam_api|MixRate mismatch|ObjectDB instances were leaked|resources still in use at exit' || true)
if [[ -n "$ERRORS" ]]; then
    echo "$ERRORS"
    exit 1
fi
