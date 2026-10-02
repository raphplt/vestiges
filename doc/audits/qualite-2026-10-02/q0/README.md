# Q0 — Validations fiables

2 octobre 2026 · [Plan 26](../../../plans/26-qualite-et-dette-technique.md) · [Guide d'utilisation](../../../VALIDATION.md)

Q0 corrige les faux succès des lanceurs et la dérive des bancs cône/UI. Le contrôle global termine avec **21 suites réussies sur 21**, zéro warning au build et aucun changement de source pendant l'exécution. Aucun réglage de gameplay ou de rareté n'est modifié.

## État de référence et périmètre

Référence : `1a071991a57339fdf7f04a09085bcd0fcb514896`, après les lots 21 F. Les changements Q0 ont été validés dans un checkout isolé lorsque des mesures longues ont occupé le checkout principal. Les modifications concurrentes de gameplay, données et bancs sont préservées et exclues de ce snapshot ; leurs résultats ne sont pas certifiés ici. [Périmètre final et hashes](delivery-context.json). Le formatage préexistant des inputs dans `project.godot` est conservé.

[État initial](before.json), [fichiers Q0 transférés et SHA-256](isolated-checkout.json), [manifestes avant](global/manifest-before.json) et [après](global/manifest-after.json). Les journaux sont compressés pour conserver les diagnostics sans encombrer le dépôt.

## Avant/après observé

| Contrôle | Avant Q0 | Après Q0 |
|---|---|---|
| Smoke avec moteur quittant avec 42 | Code 0 et « OK » | Échec explicite, même avec le vrai build C# |
| Mouvement `--run-integration` | Tableau vide sous Bash macOS, aucun résultat moteur, code 0 | Main exécutée et `RESULT failures=0` exigé |
| Cône | Exception sur `_igniteChance` supprimé, pas de résultat final | Contrats actuels d'Allumette et de vol de vie ; résultat final à zéro échec |
| UI art | Deux échecs sur anciens comptages 31/34 | Ensembles d'IDs comparés aux JSON et manifestes ; résultat final à zéro échec |
| Mesures/A-B | Résultats incomplets ou zéro passe parfois acceptés | Nombre demandé, retour processus, résultat unique, validité et artefacts obligatoires |

Les validations partagent les contrôles de build, import terminé, timeout, code moteur et journal. Un `RESULT` ne neutralise jamais un code de sortie non nul, une assertion échouée ou une erreur runtime. Les erreurs ne sont plus filtrées par mots présents dans la ligne ; seule la ligne précise de ressources encore utilisées à la fermeture Godot est tolérée. Les profils temporaires sont normalisés pour éviter le double slash de `TMPDIR` macOS, qui provoquait une erreur de création du répertoire ObjectDB.

Un verrou de checkout empêche les lanceurs concernés de se superposer. Le lanceur global construit et importe une fois, continue après l'échec d'une suite et conserve son résultat individuel. Les tests des lanceurs prouvent également le rejet d'un changement de source pendant une validation.

Le smoke instance le vrai Hub, attend les images demandées et vérifie un menu contenant un bouton visible et utilisable, l'état Hub et l'absence de pause. Le cône exerce les déclencheurs d'objets actuels et le soin cadencé/plafonné ; sa réflexion fournit désormais le nom du membre manquant. Les ensembles UI distinguent les contenus actifs des contenus futurs, sans fixer leur nombre.

## Vérifications

| Vérification | Résultat | Preuve |
|---|---|---|
| Global, build/import partagés | 21/21 ; sources modifiées : 0 ; build 0 warning | [Rapport](global/validation.json), [lanceur](global/launcher.log.gz), [build](global/build.log.gz) |
| Pannes injectées et orchestration | 8 tests réussis : sortie 42 après résultat, absence de binaire, silence, timeout, import invalide/incomplet, résultat répété, erreur cachée derrière `steam_api`, CSV absent/vide/partiel, A/B incomplet, verrou et mutation de source | [Tests des lanceurs](global/launchers.log.gz) |
| Raretés, contrat 21 F2 déjà corrigé | Sur 10 000 tirages : 195 Épiques/Légendaires sans bonus, 347 en zone Effacée, 2 198 avec 1 de Chance ; assertions vertes | [Armes](global/weapons.log.gz) |
| Cône | 120 ticks de dégâts, 5 brûlures ; cadence/plafond du vol de vie, provenance et procs vérifiés | [Cône](global/cone.log.gz) |
| Densité headless réelle | Seeds 7 et 42, 10 s chacune, 9 échantillons par seed ; 2/2 valides | [Mesures](measurement-final/validation.json) |
| Densité GL réelle | Seed 42, 10 s, 9 échantillons ; 1/1 valide | [Mesure rendue](density-final/validation.json) |
| Préchauffage GL | 18 shaders et lueur d'XP : chacun soumis au GPU, viewport libéré, `valid=true` | [Rapport GPU](shaders-final/shaders.json) |
| ExportDebug et ExportRelease | Build sans warning, mode dev inactivable, toggle exclu | [Exports](dev-release-delivery.log.gz) |

Le contrôle strict a exposé quatre diagnostics `custom_samplers` par chargement du shader des projectiles dans trois suites. La première validation globale a correctement échoué avec 18/21 suites. Le correctif conserve les mêmes échantillons de texture et seuils, en les calculant dans `fragment()` sans transmettre le sampler intégré à une fonction auxiliaire. Les suites headless passent ensuite sans masquer ces erreurs. La [comparaison GL](projectile-comparison.log.gz) montre deux images non vides identiques pixel par pixel pour le sprite de notes : [image avant/après](projectile-comparison.png), [instrument](compare-projectile.gd), [shader de référence](player_projectile-before.gdshader).

Une vraie run GL de 20 s, seed 42, est capturée à 5, 10 et 15 s, puis son journal est contrôlé par le validateur commun : [résultat](capture-final-launcher.log.gz). Les images ont été inspectées dans `/tmp/vestiges-q0-r2cryh57/capture-final/` ; aucun résultat de performance n'en est déduit.

## Limites conservées

Sur le checkout neuf sans cache, le premier import Godot a signalé une police du thème non encore importée, puis a produit ses imports. Le lanceur l'a rejeté. La relance conservant ces imports a terminé sans erreur ; aucune modification manuelle de `.godot/` ou des `.import` n'a été faite. Le guide décrit cette situation ; un import qui reste en erreur ne doit pas être contourné.

Les captures historiques et les builds manuels de l'éditeur ne sont pas coordonnés par le nouveau verrou. Les vrais bancs FPS/A-B n'ont pas été rejoués sur cette machine occupée : leurs contrôles de sorties et d'agrégation sont testés par fixtures, sans affirmation de gain de performance. Le fonctionnement réel de Steam, la reproductibilité des seeds (Q8c), les noms/règles de combat en dur (Q5–Q7) et les autres lots restent ouverts.
