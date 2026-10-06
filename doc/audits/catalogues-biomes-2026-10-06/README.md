# Biomes — plan 26 Q7c-4a, 6 octobre 2026

Base : `9a787909`, checkout initial propre. Godot 4.7.2 mono, .NET 10.0.112, Linux. Le lot ne modifie aucun JSON de gameplay ni aucune image.

## Résultat

Les cinq fiches sont contrôlées ensemble avant publication. Le catalogue est conservé intégralement si un candidat est refusé. Les définitions et leurs collections imbriquées sont en lecture seule ; `GetAll` rend une copie de la liste. Les groupes d'apparition conservent leurs répétitions et leur ordre. Les références de textures sont contrôlées par `ResourceLoader.Exists`, qui reconnaît aussi les ressources importées d'un export.

Les champs obligatoires sont `id`, `terrain_weights`, les deux groupes d'apparition et `tile_sources`. Les autres gardent les secours actuels : nom vide, son absent, danger 1, poids de carte 1, lieux 3–5 et table absente vide, aucun groupe de Wang, fusion fausse, style absent hérité du monde ; un style présent peut hériter de ses trois valeurs par défaut. Un groupe déclaré de Wang doit compter un multiple de seize tuiles. Un groupe spécial de tuiles doit être consommé par `BiomeTileMapper`. Les commentaires `_…` restent admis ; les doublons et les champs inconnus sont refusés.

Le lecteur commun refuse également un nombre JSON fini qui devient infini lors de sa conversion en `float`.

## Vérifications

- `valeurs-avant.txt` et `valeurs-apres.txt` : sonde `CatalogValuesProbe` étendue avant le changement du lecteur. **590 valeurs identiques**, dont 101 lignes de biomes (ordre du catalogue, groupes, poids, tuiles, Wang et styles). Les 489 autres valeurs déjà relevées sont inchangées. Les deux journaux sont contrôlés par `check_validation_log.py`.
- `tools/validate.sh /tmp/vestiges-q7c4a-final smoke catalogs loading run_trace cartography movement-integration enemy_abilities objects small_places` : **9/9**, sources inchangées. La suite catalogues comporte **190 contrôles**, soit 55 ajoutés : champs, types, bornes, sommes de poids, références, groupes de Wang, doublons, JSON illisible, secours, répétitions, lecture seule et absence de publication.
- Après relecture (ressources importées, recherche du diagnostic indépendante du mot « panne »), `tools/validate.sh /tmp/vestiges-q7c4a-verified smoke catalogs loading run_trace dev_release` : **5/5**, sources inchangées. Build sans avertissement. Les exports Debug/Release gardent les bancs et outils hors des assemblies et de l'archive de ressources ; cela ne constitue pas un lancement du jeu exporté (Q12 reste ouvert).
- `run_trace` : à la seed 221092026, les dix apparitions gardent l'empreinte `BBC84B0B25A77BE0` sur deux lancements ; seed 7 : `1F6DA76689948860`.
- **Vrai fichier invalide**, dans `/tmp/vestiges-q7c4a-invalid-biome`, copie du checkout sans `.git`, `.godot` ni `doc` : `map_weight` de la forêt mis à 0. Build puis import par Godot, profil temporaire, `LoadingRecoveryRegression --scenario invalid-biome`. **6 contrôles verts** : arbre en pause derrière l'erreur, bouton focalisé, diagnostic fichier/biome/champ, aucune tâche de génération lancée, retour au Hub dépausé, absence de décors orphelins. Les seuls diagnostics d'erreur admis sont ceux de ce fichier et de ce champ. Le dépôt et le profil personnel ne sont pas modifiés.

`journaux.log.gz` conserve les builds/imports, les deux validations et le scénario réel. `validation-initiale.json` et `validation-finale.json` conservent les synthèses. Le premier import de la copie sans cache a rencontré la police du thème avant son scan (cas documenté dans `doc/VALIDATION.md`) ; son journal est conservé comme échec, le second import est vert. Le premier essai du scénario réel a révélé que le banc ne cherchait que les messages contenant « panne » ; le banc est corrigé et le scénario rejoué avec succès. Aucun de ces premiers essais n'est présenté comme vert.

## Limites et suite

Les paramètres de `world_gen.json`, les fermes et les carrières restent à Q7c-4b/c ; les événements, Résurgences et réglages d'Effacement restent à Q7d. Le catalogue V1 des lieux, actuellement désactivés, fournit les identifiants de référence ; son propre lecteur n'est pas durci ici. Aucun changement visuel, équilibrage ou gain de FPS n'est revendiqué. Les tests headless ne prouvent pas le rendu GPU ni Steam réel.
