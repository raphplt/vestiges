# Mode dev

Le mode dev rend disponible tout le contenu implémenté pour les essais, sans remplir la progression normale. Il ne crée pas les personnages encore proposés dans les plans 06/08.

## Depuis Godot : aucun script nécessaire

1. Ouvrir le projet dans Godot Mono et le lancer avec **F5**.
2. Dans le menu principal du Hub, sous **Quitter**, activer **Dev : tout débloqué**.
3. Le Hub se recharge immédiatement avec le profil dev. Le même toggle permet de revenir au profil normal.

Le choix est mémorisé localement dans `user://development.cfg` et retrouvé au prochain F5. La bascule se fait uniquement depuis le Hub, jamais pendant une run ; les caches de progression, d’historique et d’analytics sont rechargés, ainsi que la sélection du personnage. Aucun redémarrage de Godot n’est nécessaire.

## Exclusion des exports

Le contrôle du Hub, la lecture de la préférence, l’activation et la création des panneaux F1/F4 sont compilés uniquement avec le symbole **TOOLS**. Le SDK Godot le définit pour la configuration locale `Debug`, mais pas pour `ExportDebug` ou `ExportRelease`.

Dans les deux types d’export, le toggle est absent et le mode reste désactivé, même si `development.cfg` contient un choix actif ou si `--dev` est passé au programme. Le jeu distribué utilise donc toujours le profil normal. Les panneaux et le service de commandes vivent dans `tools/development/`. `Vestiges.csproj` exclut tous les C# de `tools/` hors configuration locale `Debug`, et `tools/.gdignore` exclut ce répertoire du scan de ressources Godot et des archives natives. Les scènes de jeu ne référencent plus de panneau. Les hooks d’invincibilité, de bot, de survie aux coups fatals, de spawn forcé et d’invulnérabilité d’essai sont également absents des assemblies distribuées. Aucun réglage du projet n’a besoin d’être changé avant l’export.

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

- Tous les personnages présents dans le JSON sont disponibles. Les personnages ajoutés ultérieurement à ces données seront inclus automatiquement.
- Tous les Souvenirs du catalogue sont accessibles dans le Journal, ce qui ouvre également les quatre accès d’armes encore liés au lore dans le code actuel. Les armes et passifs déjà accessibles le restent.
- Les quêtes ne sont pas marquées artificiellement terminées ; la monnaie, les objets équipés, les slots, les tables de loot et la difficulté ne sont pas remplacés. Le mode dev ne donne pas automatiquement l’invincibilité. **F4** (ou le bouton **Outils dev** en bas à gauche) ouvre l’atelier de test : invincibilité, niveau exact, XP/Essence, composition des armes, temps, téléportation et créatures. **F1** conserve les statistiques et les essais de mobilité. Ces deux panneaux sont créés uniquement pour le profil dev ; leurs callbacks vérifient également ce profil. Une run normale locale ne reçoit aucun de ces contrôles.
- Méta, quêtes, historique, meilleur score, archives de migration et analytics utilisent `user://dev/`. Le profil normal conserve ses chemins habituels. Les réglages audio, affichage, langue et commandes sont partagés.
- Le SDK Steam est désactivé dans ce mode : aucun succès ou score Steam n’est soumis depuis une partie de test. Après une activation via toggle, Steam reste coupé pour cette session, y compris si l’on revient au profil normal ; un relancement normal le réinitialise.
- Le bandeau **MODE DEV · Tout débloqué** reste visible dans le Hub et pendant la run, y compris en pause.

## Atelier F4 — 14 octobre 2026

L’atelier suspend la simulation à l’ouverture. **F4**, **Échap** ou **Fermer** rétablit la pause précédente, sauf si elle a été explicitement changée dans l’atelier ou si un choix de progression attend une réponse. Les contrôles fonctionnent aussi pendant une pause existante ; les listes défilent.

- **Joueur** : invincibilité contre les coups et le Néant, soin complet, niveau exact de 1 à 9 999, quantité d’XP ou d’Essence. Fixer le niveau remet l’XP partielle à zéro, sans distribuer de cartes ni retirer les bonus du build. Ajouter de l’XP suit la courbe et les bonus habituels, avec les choix habituels.
- **Armes** : choisir un emplacement et une arme du catalogue, son niveau et la rareté de ses améliorations. **Équiper / reconstruire** fabrique une arme neuve à partir de la recette, y compris pour baisser son niveau ; les autres emplacements gardent leurs instances et leurs recharges. Les ajouts utilisent le premier emplacement libre et ouvrent au besoin les slots de test, jusqu’à quatre. Les doublons sont refusés ; retirer une arme décale les suivantes. La rareté appartient aux améliorations, pas à l’arme. Même seed et recette donnent les mêmes gains, sans consommer les flux aléatoires de la run.
- **Temps** : pause, vitesse de simulation de ×0,25 à ×8 dans le sélecteur (×0,1 à ×8 par commande), et **horloge de difficulté** en secondes. Cette horloge change les tables et les courbes des futurs spawns, ainsi que l’éligibilité des systèmes qui la lisent ; les ennemis existants gardent leurs stats. Elle ne vieillit pas le monde et ne rejoue pas les événements passés. Pour faire avancer l’ensemble de la run, utiliser l’accélération. La vitesse revient à sa valeur antérieure à la mort ou à la sortie de la scène.
- **Monde** : téléportation au clic droit dans le monde, créature du catalogue créée près du joueur.
- **Console** : mêmes commandes que les lancements et les bancs, résultat ou refus affiché au pied du panneau.

## Commandes pour les agents

Arguments répétés après le séparateur `--`, appliqués à chaque entrée en run locale dev :

```bash
tools/run_dev.sh res://scenes/Main.tscn -- \
  --dev-command "god on" \
  --dev-command "level 25" \
  --dev-command "weapon makeshift_bow 1 10 rare" \
  --dev-command "time 1200" \
  --dev-command "speed 2"
```

`godot-mono --path . res://scenes/Main.tscn -- --dev …` accepte les mêmes arguments. Les bancs `tools/` peuvent également accéder au service avec leur profil isolé, sans activer l’interface dev. Les exports ne contiennent ni ce service ni les nouveaux hooks. Chaque commande valide est journalisée avec le préfixe `[DevelopmentRunTools]` ; une commande de lancement invalide produit une erreur explicite. Les réglages d’essai ne sont pas mémorisés entre les runs.

| Commande | Effet |
|---|---|
| `god on` / `god off` | Invincibilité |
| `heal` | Santé complète |
| `level 25` | Niveau exact, sans récompenses |
| `xp 1000` | XP brute, bonus du build appliqué |
| `essence 100` | Gain d’Essence |
| `weapon makeshift_bow 1 10 rare` | ID, emplacement 1–4, niveau, rareté des améliorations |
| `remove-weapon 2` | Retire l’arme du deuxième emplacement |
| `time 1200` | Horloge de difficulté à 1 200 secondes (borne : 86 400) |
| `speed 0.5` | Ralenti (borne : 0,1–8, point décimal) |
| `pause on` / `pause off` | Suspend / reprend (terminer d’abord les choix de progression) |
| `teleport on` / `teleport off` | Téléportation au clic droit |
| `move 100 -50` | Position du joueur dans le monde |
| `spawn shadow_crawler 100 -50` | ID de créature et position dans le monde |
| `land next` / `land 2` | Terre suivante, ou terre de rang donné (plan 51) : la carte se vide et la suivante se construit, joueur et build intacts. Les terres sont la règle par défaut ; `--no-lands` (`tools/run_dev.sh -- --no-lands`) rend la grande carte en mosaïque, pour les mesures et comparaisons |

Pour capturer l’atelier avec le banc existant : `CAPTURE_EXTRA_ARGS=--capture-dev-tools tools/capture_run.sh /tmp/vestiges-atelier`. Il produit une vue joueur/armes et une vue temps/console.

## Scénarios automatisés

Un lancement local depuis une scène ou un script de `tools/` est identifié avant les autoloads. Sans `--dev`, il utilise `user://tests/` ; avec `--dev`, il conserve `user://dev/`. Les réglages partagés restent communs. Les lanceurs isolent en plus les données utilisateur dans un dossier temporaire. Le répertoire d'outils n’apparaît plus dans le navigateur de fichiers Godot ; les scripts de `tools/` continuent de charger leurs scènes par chemin explicite.

L'historique conserve la provenance `Normal`, `Development` ou `Test`. Les anciennes entrées sans champ de provenance sont lues comme normales. Steam n'est jamais initialisé pour un banc et les points d'envoi vérifient aussi l'éligibilité de la session. Une activation du profil dev reste disqualifiante pour Steam jusqu'au relancement, même après un retour au profil normal. Les hooks locaux refusent une utilisation hors profil dev ou banc ; la bascule de profil est refusée pendant la run.

Les kits, anciens mutateurs et systèmes retirés par V2 ne sont pas réactivés. Le futur inventaire d’objets et les futurs droits de déblocage du plan 05/06 devront reprendre cette séparation lors de leur implémentation.

## Vérification — 22 septembre 2026

`tools/test_dev_mode.sh` utilise des données XDG temporaires. Il vérifie les accès du catalogue, une vraie fin de run via `ScoreManager.SaveEndOfRun`, puis des bascules répétées via le vrai toggle du Hub. Il contrôle le rechargement des caches, la conservation des acquis normaux et le choix mémorisé au lancement suivant sans `--dev`. Après la partie dev et les bascules, les fichiers normaux de méta, historique, score et analytics sont comparés octet par octet à leur référence.

`tools/test_dev_release.sh` compile `ExportDebug` et `ExportRelease`, charge leurs assemblies et tente d’activer le mode : il doit rester indisponible et désactivé. La lecture de préférence et le gestionnaire du toggle doivent être absents de ces assemblies. Ce contrôle ne nécessite pas de template d’export ni de distribution Steam.

Résultat : tests des profils, du toggle et des deux configurations d’export verts ; builds C# zéro warning/zéro erreur ; `tools/smoke_test.sh 600` vert sur profil normal isolé. Le parcours avec rendu à 1280×720 a vérifié le toggle normal → dev → sélection des personnages débloqués → normal, ainsi que Hub → run avec la Forgeuse → pause → retour Hub et l’indicateur du mode. Les sprites actuels restent provisoires, conformément à la demande de refonte.

## Vérification de l’atelier — 14 octobre 2026

Build zéro avertissement ; smoke et profils dev verts ; commandes exercées en vraie Main sur les trois provenances (17 contrôles normal, 58 banc, 72 dev), armes vérifiées. Les arguments de lancement, les refus sans mutation, les niveaux hauts/bas, les slots, les gains reproductibles, la pause des choix et le retour à vitesse normale sont couverts. Les exports Debug/Release ne contiennent ni le service ni les nouveaux hooks ; aucune ressource de développement dans l’archive. Les captures joueur/armes et temps/console ont été relues à 720p et 1080p.
