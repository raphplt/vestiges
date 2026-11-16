#!/usr/bin/env bash
# Serveur MCP godot (@coding-solo/godot-mcp), lancé depuis .mcp.json.
set -euo pipefail
# shellcheck source=tools/mcp/env.sh
. "$(dirname "${BASH_SOURCE[0]}")/env.sh"

export GODOT_PATH="$GODOT_BIN"
exec npx -y @coding-solo/godot-mcp@0.1.1
