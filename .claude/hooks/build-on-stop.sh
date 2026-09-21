#!/usr/bin/env bash
# Stop : si du C# a changé depuis le dernier commit, vérifier que ça compile avant de rendre la main.
# Exit 2 renvoie les erreurs à Claude, qui continue pour les corriger.
set -uo pipefail

input=$(cat)
# Évite une boucle infinie si Claude n'arrive pas à corriger : on ne bloque qu'une fois.
[[ "$(jq -r '.stop_hook_active // false' <<<"$input")" == "true" ]] && exit 0

cd "${CLAUDE_PROJECT_DIR:-.}" || exit 0
[[ -n "$(git status --porcelain -- '*.cs' '*.csproj')" ]] || exit 0

if ! out=$(dotnet build --nologo -v q -clp:NoSummary 2>&1); then
  echo "dotnet build échoue :" >&2
  grep -E 'error|warning' <<<"$out" | sort -u | head -20 >&2
  exit 2
fi

if grep -q 'warning CS' <<<"$out"; then
  echo "dotnet build passe mais avec des warnings (objectif : 0) :" >&2
  grep 'warning CS' <<<"$out" | sort -u | head -20 >&2
  exit 2
fi
