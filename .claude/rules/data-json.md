---
paths:
  - "data/**/*.json"
  - "scripts/Infrastructure/*DataLoader.cs"
---

# Données de gameplay (JSON)

- Chaque dossier de `data/` est lu par un loader statique de `scripts/Infrastructure/` (`EnemyDataLoader`, `WeaponRarityDataLoader`, ...). Parsing via `Godot.Json` puis mapping manuel (`dict["cle"]`) : un nouveau champ JSON n'existe pour le jeu que si le loader le lit explicitement. Champ optionnel → `ContainsKey` + valeur par défaut.
- Clés en `snake_case` côté JSON, propriétés PascalCase côté C#.
- Chaque entrée a un `id` unique, stable, en `snake_case`. Ne jamais renommer un `id` existant : sauvegardes méta, quêtes et succès Steam y font référence.
- Textes affichés : clés de traduction (`UI_...`) ajoutées dans `assets/translations/translations.csv` (colonnes fr/en), pas de texte en dur.
- Fichiers `_template.json` : documentation de schéma, non chargés.
- Dossiers hérités du pivot V2 (`recipes/`, `resources/`, `events/`, `simulation/`) : ne pas les enrichir sans vérifier qu'un loader les utilise encore.
