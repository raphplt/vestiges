#!/usr/bin/env bash
# Exerce l'assembly de production sans moteur : le mode doit rester inactivable.
set -euo pipefail
cd "$(dirname "$0")/.."
CHECK_DIR=$(mktemp -d "${TMPDIR:-/tmp}/vestiges-release-check.XXXXXX")
trap 'rm -rf "$CHECK_DIR"' EXIT
dotnet new console --framework net10.0 --no-restore --output "$CHECK_DIR" > /dev/null
cat > "$CHECK_DIR/Program.cs" <<'CS'
using System.Reflection;
using System.Runtime.Loader;

string assemblyDirectory = Path.GetFullPath(args[0]);
AssemblyLoadContext.Default.Resolving += (context, name) =>
{
    string path = Path.Combine(assemblyDirectory, name.Name + ".dll");
    return File.Exists(path) ? context.LoadFromAssemblyPath(path) : null;
};
Assembly game = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(assemblyDirectory, "Vestiges.dll"));
Type mode = game.GetType("Vestiges.Infrastructure.DevelopmentMode", true)!;
if ((bool)mode.GetProperty("IsAvailable")!.GetValue(null)! || (bool)mode.GetProperty("IsEnabled")!.GetValue(null)!)
    throw new Exception("Le mode dev est disponible en production.");
MethodInfo activate = mode.GetMethod("SetEnabled", BindingFlags.NonPublic | BindingFlags.Static)!;
if ((bool)activate.Invoke(null, new object[] { true })! || (bool)mode.GetProperty("IsEnabled")!.GetValue(null)!)
    throw new Exception("Le mode dev peut être activé en production.");
if (mode.GetMethod("LoadPreference", BindingFlags.NonPublic | BindingFlags.Static) != null)
    throw new Exception("La lecture des préférences dev existe dans l'assembly de production.");
Type hub = game.GetType("Vestiges.UI.HubScreen", true)!;
if (hub.GetMethod("OnDevelopmentModeToggled", BindingFlags.NonPublic | BindingFlags.Instance) != null)
    throw new Exception("Le gestionnaire du toggle est compilé en production.");
Console.WriteLine($"[DevelopmentReleaseRegression] PASS : mode inactivable et toggle exclu de {new DirectoryInfo(assemblyDirectory).Name}.");
CS
for configuration in ExportDebug ExportRelease; do
    dotnet build Vestiges.csproj --nologo -c "$configuration"
    dotnet run --project "$CHECK_DIR" -- "$(pwd)/.godot/mono/temp/bin/$configuration" --dev
done
