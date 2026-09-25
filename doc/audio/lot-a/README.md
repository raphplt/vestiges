# Écoute du lot A

Les [retours de Raphaël](RETOURS.md) sont consignés séparément. Le [lot A2](../lot-a2/index.html) propose les nouvelles recherches ; cette page conserve le premier panier et ses identifiants. Les choix et exports des deux lots sont indépendants.

## Quand les effets interviennent

- **Impact ennemi / critique** : contact après dégâts acceptés dans `Enemy.TakeDamage`, quelle que soit l'arme ou la source. Le critique remplace l'impact normal. Ces retours de contact sont distincts du tir ou du mouvement propre à chaque arme ; le gestionnaire espace chaque clé d'au moins 80 ms.
- **Coffre actuel** : le son `sfx_chest_opening` démarre à l'ouverture de l'écran de butin ; les révélations de récompenses utilisent ensuite `sfx_perk_choix`. Le son mécanique retenu et la future ponctuation mélodique de révélation sont deux besoins séparés. Le lot A2 recherche la seconde, sans modifier les déclenchements actuels.

Références vérifiées : `scripts/Combat/Enemy.cs` (`TakeDamage`), `scripts/Infrastructure/AudioManager.cs` (délais de 80 ms), `scripts/UI/ChestLootScreen.cs` (`Show`, révélations). Les choix détaillés restent dans la [synthèse des retours](RETOURS.md).

## Utilisation

Ouvrir `index.html` dans un navigateur, directement depuis le disque. La page contient les données du catalogue : aucun serveur, compte ou service réseau n'est nécessaire pour écouter les fichiers locaux. Les liens vers les auteurs et les licences nécessitent Internet.

1. Commencer à volume modéré ; comparer les candidats A/B/C et le son actuel lorsqu'il est disponible. Lancer un lecteur suspend les autres.
2. Choisir un candidat, « Aucun : chercher autre chose » ou « Silence souhaité ». « À réécouter / sans choix » remet la fiche en attente. Aucun candidat n'est présélectionné.
3. Noter le contexte, les raisons du choix ou du refus, la fatigue à répétition et les retouches souhaitées.
4. Cliquer sur **Exporter mes choix** et transmettre le fichier JSON à l'agent. Il contient les identifiants des effets et des candidats, pas seulement leurs lettres.

Les choix sont conservés dans le stockage local du navigateur, lorsque celui-ci l'autorise. Un autre navigateur, le mode privé ou un déplacement de la page peuvent perdre cette mémoire : l'export constitue la sauvegarde transportable. **Importer des choix** restaure uniquement les fiches présentes dans le JSON ; un fichier invalide est refusé avant toute modification. L'import d'une fiche valide remplace son choix et ses notes actuels.

Le catalogue décrit des propositions à écouter, pas des choix artistiques validés. Les transformations éventuelles de l'aperçu sont indiquées par candidat ; les originaux restent accessibles. Une normalisation technique ne garantit pas la même sonie perçue. Les sons actuels servent de référence et peuvent avoir un niveau différent. Les appréciations d'adéquation restent des hypothèses et les nouveaux sons ne sont pas intégrés au jeu.

## Mise à jour

`candidates.json` est la source de données. Après modification du catalogue ou du modèle HTML, exécuter depuis la racine du dépôt :

```sh
python3 tools/audio/build_review.py
```

Pour reconstruire le panier suivant : `python3 tools/audio/build_review.py --batch lot-a2`.

Le script vérifie les identifiants et la présence des fichiers des candidats puis reconstruit `index.html`. Le modèle est `tools/audio/review_template.html`. Les aperçus et originaux restent dans ce dossier de travail, hors des assets chargés par le jeu.

Les identifiants doivent rester stables entre deux générations. Si des candidats ont été retirés, une sauvegarde les référençant sera refusée plutôt que de convertir silencieusement un choix.

## Recette à mener en jeu après sélection

Contrôler le déclenchement, la lisibilité face à la musique, les répétitions en combat dense, la durée, les variantes et les priorités des avertissements. Un choix « Silence souhaité » est un retour à examiner avant de retirer un signal de gameplay.
