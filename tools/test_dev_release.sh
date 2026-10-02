#!/usr/bin/env bash
# Exerce l'assembly de production sans moteur : le mode doit rester inactivable.
set -euo pipefail
cd "$(dirname "$0")/.."
source tools/lib/validation.sh
validation_entry "$0" "$@"
CHECK_DIR=$(mktemp -d "${TMPDIR:-/tmp}/vestiges-release-check.XXXXXX")
PRESETS_INSTALLED=0
cleanup_release() {
    local result=$?
    if [[ $PRESETS_INSTALLED -eq 1 ]]; then
        if ! cmp -s export_presets.cfg "$CHECK_DIR/resources-presets.cfg"; then
            echo "Réglages d'export modifiés pendant le test ; sauvegarde antérieure conservée : $CHECK_DIR" >&2
            result=1
        elif [[ -f "$CHECK_DIR/original-presets.cfg" ]]; then
            cp -p "$CHECK_DIR/original-presets.cfg" export_presets.cfg
        else
            rm -f export_presets.cfg
        fi
    fi
    if [[ $result -ne 0 || ${VALIDATION_KEEP_LOGS:-0} == 1 ]]; then
        echo "Journaux conservés : $CHECK_DIR" >&2
    else
        rm -rf "$CHECK_DIR"
    fi
    exit "$result"
}
trap cleanup_release EXIT
isolate_godot_profile "$CHECK_DIR/profile"
validation_prepare "$CHECK_DIR"
dotnet new console --framework net10.0 --no-restore --output "$CHECK_DIR" > /dev/null
cat > "$CHECK_DIR/Program.cs" <<'CS'
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.Loader;

string assemblyDirectory = Path.GetFullPath(args[0]);
AssemblyLoadContext.Default.Resolving += (context, name) =>
{
    string path = Path.Combine(assemblyDirectory, name.Name + ".dll");
    return File.Exists(path) ? context.LoadFromAssemblyPath(path) : null;
};
Assembly game = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(assemblyDirectory, "Vestiges.dll"));
using PEReader pe = new(File.OpenRead(Path.Combine(assemblyDirectory, "Vestiges.dll")));
MetadataReader metadata = pe.GetMetadataReader();
string[] forbidden = metadata.TypeDefinitions.Select(handle => metadata.GetTypeDefinition(handle))
    .Select(type => metadata.GetString(type.Namespace) + "." + metadata.GetString(type.Name))
    .Where(name => name.StartsWith("Vestiges.Tests.") || name is "Vestiges.UI.DebugActionPanel" or "Vestiges.UI.DebugOverlay").ToArray();
if (forbidden.Length != 0)
    throw new Exception("Types de développement distribués : " + string.Join(", ", forbidden));
void RequireAbsentMember(string typeName, params string[] members)
{
    Type type = game.GetType(typeName, true)!;
    foreach (string member in members)
        if (type.GetMember(member, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static).Length != 0)
            throw new Exception($"Hook distribué : {typeName}.{member}");
}
RequireAbsentMember("Vestiges.Core.Player", "IsGodMode", "IsAIControlled", "AIInputOverride", "AITriggerInteract", "SurviveFatalHitsForTests", "DisableDefenseForTests");
RequireAbsentMember("Vestiges.Core.PlayerMobility", "UseInvulnerabilityTrial");
RequireAbsentMember("Vestiges.Core.PlayerDefense", "Disable");
RequireAbsentMember("Vestiges.World.ErasureManager", "OverrideMemory", "RefreshGroundMemory");
RequireAbsentMember("Vestiges.Spawn.SpawnManager", "ForceSpawnEnemy");
RequireAbsentMember("Vestiges.World.GameBootstrap", "AddDevelopmentTools");
RequireAbsentMember("Vestiges.Infrastructure.DevelopmentMode", "IsTestLaunch", "RequireTestAccess");
Type mode = game.GetType("Vestiges.Infrastructure.DevelopmentMode", true)!;
if ((bool)mode.GetProperty("IsAvailable")!.GetValue(null)! || (bool)mode.GetProperty("IsEnabled")!.GetValue(null)!)
    throw new Exception("Le mode dev est disponible en production.");
if ((bool)mode.GetProperty("IsTestSession")!.GetValue(null)! || !(bool)mode.GetProperty("CanSubmitResults")!.GetValue(null)! || mode.GetProperty("CurrentProvenance")!.GetValue(null)!.ToString() != "Normal")
    throw new Exception("Les arguments de test affectent la provenance de l'export.");
MethodInfo activate = mode.GetMethod("SetEnabled", BindingFlags.NonPublic | BindingFlags.Static)!;
if ((bool)activate.Invoke(null, new object[] { true })! || (bool)mode.GetProperty("IsEnabled")!.GetValue(null)!)
    throw new Exception("Le mode dev peut être activé en production.");
if (mode.GetMethod("LoadPreference", BindingFlags.NonPublic | BindingFlags.Static) != null)
    throw new Exception("La lecture des préférences dev existe dans l'assembly de production.");
Type hub = game.GetType("Vestiges.UI.HubScreen", true)!;
if (hub.GetMethod("OnDevelopmentModeToggled", BindingFlags.NonPublic | BindingFlags.Instance) != null)
    throw new Exception("Le gestionnaire du toggle est compilé en production.");
Console.WriteLine($"[DevelopmentReleaseRegression] PASS : outils, bancs et hooks exclus ; arguments dev/test sans effet de {new DirectoryInfo(assemblyDirectory).Name}.");
CS
for configuration in ExportDebug ExportRelease; do
    validation_run 180 "$CHECK_DIR/build-$configuration.log" '0 (?:Warning|Avertissement)\(s\)' dotnet build Vestiges.csproj --nologo -warnaserror -c "$configuration"
    validation_run 120 "$CHECK_DIR/run-$configuration.log" '^\[DevelopmentReleaseRegression\] PASS : .+$' dotnet run --project "$CHECK_DIR" -- "$(pwd)/.godot/mono/temp/bin/$configuration" --dev --mortal --peril 5 --force-level-up --capture-perks
    cat "$CHECK_DIR/run-$configuration.log"
done
python3 - <<'PYSCENES'
from pathlib import Path
import re
for scene in Path('scenes').rglob('*.tscn'):
    if re.search(r'res://(?:tools/|scripts/UI/Debug(?:ActionPanel|Overlay)\.)',scene.read_text()):
        raise SystemExit(f'Ressource de développement référencée : {scene}')
print('✓ Scènes de jeu sans référence aux contrôles ni aux bancs.')
PYSCENES
# Preset temporaire sans exclusions : .gdignore doit suffire à la sélection native des ressources.
# Les éventuels réglages locaux sont sauvegardés puis restaurés à l'identique.
if [[ -f export_presets.cfg ]]; then cp -p export_presets.cfg "$CHECK_DIR/original-presets.cfg"; fi
ARCH=x86_64
if [[ $(uname -m) == arm64 || $(uname -m) == aarch64 ]]; then ARCH=arm64; fi
cat > "$CHECK_DIR/resources-presets.cfg" <<PRESETS
[preset.0]
name="Q1Resources"
platform="Linux"
runnable=false
advanced_options=false
dedicated_server=false
custom_features=""
export_filter="all_resources"
include_filter=""
exclude_filter=""
export_path=""
script_export_mode=2

[preset.0.options]
binary_format/architecture="$ARCH"
dotnet/include_scripts_content=false
dotnet/include_debug_symbols=false
dotnet/embed_build_outputs=false
PRESETS
cp "$CHECK_DIR/resources-presets.cfg" export_presets.cfg
PRESETS_INSTALLED=1
validation_run 240 "$CHECK_DIR/resources.log" '^\[ DONE \] savezip$' \
    "${GODOT_BIN:-godot-mono}" --headless --editor --path . --export-pack Q1Resources "$CHECK_DIR/resources.zip"
python3 - "$CHECK_DIR" <<'PYRESOURCES'
from pathlib import Path
import json,sys,zipfile
root=Path(sys.argv[1])
with zipfile.ZipFile(root/'resources.zip') as archive:
    names=sorted(archive.namelist())
for name in names:
    if name.startswith('tools/') or 'DebugActionPanel' in name or 'DebugOverlay' in name:
        raise SystemExit('Ressource de développement distribuée : '+name)
if not all(any(name.startswith('scenes/'+scene+'.') for name in names) for scene in ['Main','Hub']):
    raise SystemExit('Scènes de jeu absentes de l’archive.')
(root/'resources.files.txt').write_text('\n'.join(names)+'\n')
(root/'resources.json').write_text(json.dumps({'resource_count':len(names),'development_resources':0},indent=2)+'\n')
print(f'[DevelopmentExportRegression] PASS files={len(names)} development_resources=0')
PYRESOURCES
