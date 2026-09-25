#!/usr/bin/env bash
# PostToolUse (Edit|Write) : un JSON invalide dans data/ (ou un manifeste de décors dans assets/)
# casse silencieusement un loader au runtime.
# Exit 2 renvoie l'erreur à Claude pour correction immédiate.
set -uo pipefail

file=$(jq -r '.tool_input.file_path // empty')
[[ ( "$file" == */data/*.json || "$file" == */assets/*/props_manifest.json ) && -f "$file" ]] || exit 0

if ! err=$(jq empty "$file" 2>&1); then
  echo "JSON invalide dans $file : $err" >&2
  exit 2
fi
