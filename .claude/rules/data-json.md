---
paths:
  - "data/**/*.json"
  - "scripts/Infrastructure/*DataLoader.cs"
---

# Données de gameplay (JSON)

- Chaque fichier de `data/` est lu par un lecteur de `scripts/Infrastructure/` (ou du système concerné), strictement (plan 26 Q7) : `JsonConfigReader` (System.Text.Json), clés connues seulement (`AllowOnly`, les clés `_…` sont des commentaires), règle par valeur (`Positive`, `Chance`, `Integer`…), références vérifiées contre les autres catalogues. Un nouveau champ JSON n'existe que si le lecteur le déclare ; sinon le fichier est refusé. Champ optionnel → `JsonConfigReader.Has` + secours.
- Un lecteur expose `TryLoad` (exigé par `GameBootstrap` à l'étape « Catalogues ») et ne publie qu'un fichier accepté. Toute modification d'un lecteur se vérifie par `tools/tests/CatalogValuesProbe.tscn` avant/après (valeurs identiques) et des cas de refus dans `CatalogContractRegression`.
- Clés en `snake_case` côté JSON, propriétés PascalCase côté C#.
- Chaque entrée a un `id` unique, stable, en `snake_case`. Ne jamais renommer un `id` existant : sauvegardes méta, quêtes et succès Steam y font référence.
- Textes affichés : clés de traduction (`UI_...`) ajoutées dans `assets/translations/translations.csv` (colonnes fr/en), pas de texte en dur.
- Fichiers `_template.json` : documentation de schéma, non chargés.
- Dossiers hérités du pivot V2 (`recipes/`, `resources/`, `events/`, `simulation/`) : ne pas les enrichir sans vérifier qu'un loader les utilise encore.
