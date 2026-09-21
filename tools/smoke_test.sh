#!/usr/bin/env bash
# Build C# + import Godot headless + boot du jeu quelques secondes.
# Échoue si le build casse ou si le runtime émet une erreur de script / exception.
# Usage : tools/smoke_test.sh [frames]   (défaut : 600 frames ≈ 10 s)
set -euo pipefail

cd "$(dirname "$0")/.."
GODOT="${GODOT_BIN:-godot-mono}"
FRAMES="${1:-600}"
LOG_DIR="${TMPDIR:-/tmp}/vestiges-smoke"
mkdir -p "$LOG_DIR"

echo "== dotnet build"
dotnet build --nologo -v q -clp:ErrorsOnly

echo "== godot import ($("$GODOT" --version))"
"$GODOT" --headless --import --path . >"$LOG_DIR/import.log" 2>&1 || true

echo "== godot run ($FRAMES frames)"
"$GODOT" --headless --path . --quit-after "$FRAMES" >"$LOG_DIR/run.log" 2>&1 || true

# Bruit attendu en headless : pas de DLL Steam, driver audio factice à 44,1 kHz, fuites à la fermeture forcée.
NOISE='steam_api|MixRate mismatch|ObjectDB instances were leaked|resources still in use at exit'
ERRORS=$(grep -E '^(ERROR|SCRIPT ERROR)|Unhandled exception|System\.[A-Za-z]+Exception' "$LOG_DIR/run.log" | grep -Ev "$NOISE" || true)

if [[ -n "$ERRORS" ]]; then
  echo "✗ Erreurs runtime (log complet : $LOG_DIR/run.log)"
  echo "$ERRORS" | sort | uniq -c | sort -rn | head -30
  exit 1
fi

echo "✓ Smoke test OK (logs : $LOG_DIR)"
