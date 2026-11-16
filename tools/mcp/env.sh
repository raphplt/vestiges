#!/usr/bin/env bash
# Environnement commun des serveurs MCP du projet, sur Linux comme sur macOS.
# Claude Desktop lance les serveurs sans le profil du shell : PATH minimal,
# ni nvm ni ~/.local/bin. On retrouve donc les outils sans dépendre du PATH.

PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
export PROJECT_ROOT

PATH="$HOME/.local/bin:$HOME/.dotnet:/usr/local/bin:/opt/homebrew/bin:$PATH"

# Node via nvm s'il n'est pas déjà dans le PATH (version par défaut, sinon la plus récente).
if ! command -v npx >/dev/null 2>&1; then
    nvm_dir="${NVM_DIR:-$HOME/.nvm}"
    if [ -s "$nvm_dir/nvm.sh" ]; then
        # shellcheck disable=SC1091
        . "$nvm_dir/nvm.sh" >/dev/null 2>&1
    fi
    if ! command -v npx >/dev/null 2>&1; then
        latest_node="$(ls -d "$nvm_dir"/versions/node/*/bin 2>/dev/null | sort -V | tail -n 1)"
        [ -n "$latest_node" ] && PATH="$latest_node:$PATH"
    fi
fi
export PATH

# Binaire Godot (.NET) : GODOT_BIN l'emporte, comme pour les scripts de tools/.
if [ -z "${GODOT_BIN:-}" ]; then
    case "$(uname -s)" in
        Darwin) GODOT_BIN="$HOME/Applications/Godot_mono.app/Contents/MacOS/Godot" ;;
        *) GODOT_BIN="$(command -v godot-mono || echo "$HOME/.local/bin/godot-mono")" ;;
    esac
fi
export GODOT_BIN
