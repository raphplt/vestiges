#!/usr/bin/env bash
# Export macOS Release signé Developer ID, notarisé et agrafé (doc/EXPORT.md).
# Godot ne sait pas signer avec rcodesign une app qui embarque des bibliothèques (le runtime .NET) :
# il exporte en ad-hoc, rcodesign re-signe tout le bundle avec le runtime renforcé exigé par Apple.
set -euo pipefail
cd "$(dirname "$0")/.."
SIGN_DIR="${APPLE_SIGNING_DIR:-$HOME/apple-signing}"
RCODESIGN="${RCODESIGN:-$HOME/.cargo/bin/rcodesign}"
OUT=build/macos
WORK="$OUT/work"

for f in "$SIGN_DIR/devid.p12" "$SIGN_DIR/api-key.json" "$RCODESIGN"; do
    [[ -e "$f" ]] || { echo "Introuvable : $f" >&2; exit 1; }
done

rm -rf "$OUT"
mkdir -p "$WORK"
"${GODOT_BIN:-godot-mono}" --headless --path . --export-release "macOS" "$WORK/Vestiges.zip" > "$WORK/export.log" 2>&1 \
    || { tail -20 "$WORK/export.log" >&2; exit 1; }
(cd "$WORK" && unzip -q Vestiges.zip)

# Sans --p12-password, rcodesign demande le mot de passe du certificat.
"$RCODESIGN" sign --for-notarization --p12-file "$SIGN_DIR/devid.p12" \
    --entitlements-xml-file tools/macos_entitlements.plist "$WORK/Vestiges.app"
"$RCODESIGN" notary-submit --api-key-file "$SIGN_DIR/api-key.json" --staple "$WORK/Vestiges.app"

# -y garde les liens symboliques du bundle tels quels.
(cd "$WORK" && zip -qry ../Vestiges.zip Vestiges.app)
rm -rf "$WORK"
echo "✓ $OUT/Vestiges.zip signé, notarisé et agrafé"
