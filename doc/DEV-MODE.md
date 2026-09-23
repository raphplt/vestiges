# Mode dev

Le mode dev rend disponible tout le contenu implémenté pour les essais, sans remplir la progression normale. Il ne crée pas les personnages encore proposés dans les plans 06/08.

## Depuis Godot : aucun script nécessaire

1. Ouvrir le projet dans Godot Mono et le lancer avec **F5**.
2. Dans le menu principal du Hub, sous **Quitter**, activer **Dev : tout débloqué**.
3. Le Hub se recharge immédiatement avec le profil dev. Le même toggle permet de revenir au profil normal.

Le choix est mémorisé localement dans `user://development.cfg` et retrouvé au prochain F5. La bascule se fait uniquement depuis le Hub, jamais pendant une run ; les caches de progression, d’historique et d’analytics sont rechargés, ainsi que la sélection du personnage. Aucun redémarrage de Godot n’est nécessaire.

## Exclusion des exports

Le contrôle du Hub, la lecture de la préférence et l’activation sont compilés uniquement avec le symbole **TOOLS**. Le SDK Godot le définit pour la configuration locale `Debug`, mais pas pour `ExportDebug` ou `ExportRelease`.

Dans les deux types d’export, le toggle est absent et le mode reste désactivé, même si `development.cfg` contient un choix actif ou si `--dev` est passé au programme. Le jeu distribué utilise donc toujours le profil normal. Aucun réglage du projet n’a besoin d’être changé avant l’export.

## Lancement en ligne de commande, facultatif

Depuis la racine du projet :

```bash
tools/run_dev.sh
```

Le script compile puis lance Godot. `GODOT_BIN` permet de choisir le binaire compatible avec le projet. Après compilation, le lancement direct équivalent est :

```bash
godot-mono --path . -- --dev
```

`--dev` force le profil dev au lancement local ; il est ignoré dans les exports. Sans argument, la préférence du toggle est utilisée. Pour revenir à une partie normale, désactiver le toggle dans le Hub.

## Contenu et séparation des profils

- Tous les personnages présents dans le JSON sont disponibles, actuellement Traqueur, Vagabond et Forgeuse. Les personnages ajoutés ultérieurement à ces données seront inclus automatiquement.
- Tous les Souvenirs du catalogue sont accessibles dans le Journal, ce qui ouvre également les quatre accès d’armes encore liés au lore dans le code actuel. Les armes et passifs déjà accessibles le restent.
- Les quêtes ne sont pas marquées artificiellement terminées ; la monnaie, les objets équipés, les slots, les tables de loot et la difficulté ne sont pas remplacés. Le mode dev ne donne pas automatiquement l’invincibilité. F4 conserve les outils de test existants.
- Méta, quêtes, historique, meilleur score, archives de migration et analytics utilisent `user://dev/`. Le profil normal conserve ses chemins habituels. Les réglages audio, affichage, langue et commandes sont partagés.
- Le SDK Steam est désactivé dans ce mode : aucun succès ou score Steam n’est soumis depuis une partie de test. Après une activation via toggle, Steam reste coupé pour cette session, y compris si l’on revient au profil normal ; un relancement normal le réinitialise.
- Le bandeau **MODE DEV · Tout débloqué** reste visible dans le Hub et pendant la run, y compris en pause.

Les kits, anciens mutateurs et systèmes retirés par V2 ne sont pas réactivés. Le futur inventaire d’objets et les futurs droits de déblocage du plan 05/06 devront reprendre cette séparation lors de leur implémentation.

## Vérification — 22 septembre 2026

`tools/test_dev_mode.sh` utilise des données XDG temporaires. Il vérifie les accès du catalogue, une vraie fin de run via `ScoreManager.SaveEndOfRun`, puis des bascules répétées via le vrai toggle du Hub. Il contrôle le rechargement des caches, la conservation des acquis normaux et le choix mémorisé au lancement suivant sans `--dev`. Après la partie dev et les bascules, les fichiers normaux de méta, historique, score et analytics sont comparés octet par octet à leur référence.

`tools/test_dev_release.sh` compile `ExportDebug` et `ExportRelease`, charge leurs assemblies et tente d’activer le mode : il doit rester indisponible et désactivé. La lecture de préférence et le gestionnaire du toggle doivent être absents de ces assemblies. Ce contrôle ne nécessite pas de template d’export ni de distribution Steam.

Résultat : tests des profils, du toggle et des deux configurations d’export verts ; builds C# zéro warning/zéro erreur ; `tools/smoke_test.sh 600` vert sur profil normal isolé. Le parcours avec rendu à 1280×720 a vérifié le toggle normal → dev → sélection des personnages débloqués → normal, ainsi que Hub → run avec la Forgeuse → pause → retour Hub et l’indicateur du mode. Les sprites actuels restent provisoires, conformément à la demande de refonte.
