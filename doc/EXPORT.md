# Exporter Vestiges

9 octobre 2026. Rien de ce guide n'a encore été exécuté sur ce dépôt : il n'y a ni modèles d'export installés ni `export_presets.cfg`. Les étapes marquées **à vérifier** le seront au premier export.

## Pourquoi c'est utile tout de suite

Les mesures de performance sont prises avec l'exécutable de l'éditeur et une assembly C# en configuration Debug : JIT non optimisé (≈ +25 % sur le code C# d'après le plan 29), mode dev, vérifications de l'éditeur. Le jeu que les joueurs lanceront est un export Release. C'est lui qu'il faut mesurer, surtout pour la cible d'entrée de gamme.

## 1. Installer les modèles d'export (une fois)

La version des modèles doit être exactement celle de l'éditeur : **4.7.2, édition .NET (mono)**.

- **Par l'éditeur** : *Éditeur → Gérer les modèles d'export → Télécharger et installer*. Environ 1 Go.
- **À la main** : télécharger `Godot_v4.7.2-stable_mono_export_templates.tpz` depuis la page de la version 4.7.2 sur `github.com/godotengine/godot/releases`, puis l'installer par *Gérer les modèles d'export → Installer depuis un fichier*. Sous Linux, ils arrivent dans `~/.local/share/godot/export_templates/4.7.2.stable.mono/` (**à vérifier**).

## 2. Créer les presets (une fois, dans l'éditeur)

*Projet → Exporter… → Ajouter* :

- **Linux** (x86_64) et **Windows Desktop** (x86_64).
- Ressources : « Exporter toutes les ressources du projet » convient.
- .NET : rien à régler, l'export compile l'assembly en configuration `ExportRelease` (optimisée, sans `TOOLS`).
- Steam : copier `libsteam_api.so` (Linux) ou `steam_api64.dll` (Windows) à côté du binaire exporté (voir [STEAM-MISE-EN-PLACE](STEAM-MISE-EN-PLACE.md)) ; sans elle, Steam est désactivé et le jeu tourne quand même.

L'éditeur écrit `export_presets.cfg` à la racine : le versionner. Il ne contient pas de secret tant qu'aucune clé de chiffrement n'y est mise.

## 3. Exporter en ligne de commande

Après un `dotnet build` sans avertissement :

```bash
mkdir -p build/linux build/windows
godot-mono --headless --path . --export-release "Linux" build/linux/Vestiges.x86_64
godot-mono --headless --path . --export-release "Windows Desktop" build/windows/Vestiges.exe
```

Le nom entre guillemets est celui du preset. `--export-debug` produit une version avec les vérifications de débogage, utile pour lire des erreurs mais pas pour mesurer. Ne pas versionner `build/`.

## 4. Mesurer les performances en export

Ce qui ne marche pas tel quel :

- Les scènes de banc de `tools/tests/` ne sont pas compilées dans un export (`Compile Remove="tools/**/*.cs"` hors Debug dans `Vestiges.csproj`).
- Les commandes du bot (joueur piloté, invincible) sont réservées à l'éditeur (`#if TOOLS` dans `Player.cs`).
- Le mode dev ne peut pas s'activer dans un export de production (`DevelopmentMode.cs`, [DEV-MODE](DEV-MODE.md)).

Ce qu'on peut faire dès le premier export :

- **Mesure à la main** : lancer `build/linux/Vestiges.x86_64 --print-fps` (affiche les FPS dans la console, **à vérifier** dans un export), jouer une run et noter les FPS en foule dense. Grossier, mais c'est le vrai jeu.
- **Comparer avec l'éditeur** : jouer la même situation dans l'éditeur, pour chiffrer l'écart éditeur → export.

Ce qu'il faudra ajouter pour une mesure reproductible (petit lot à part) : un preset « Linux Bench » qui inclut `tools/tests/` et les commandes du bot sous un symbole propre (par exemple `VESTIGES_BENCH`, au lieu de `TOOLS`), jamais livré aux joueurs. Le banc dense et `--frame-stats` tourneraient alors dans un vrai export Release, avec les mêmes chiffres que ceux du plan 29.

## 5. Entrée de gamme

Une mesure sur une machine plus modeste vaut mieux que toute estimation. Faute de mieux, la règle du plan 29 : ≤ 8 ms par image sur le Ryzen 7 5700X de Raphaël, pour ≈ 16 ms (60 FPS) sur un processeur deux fois plus lent par cœur. Le jeu n'utilise qu'un cœur : c'est la vitesse d'un cœur qui compte, pas leur nombre.
