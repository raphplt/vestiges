# Écoute du lot A2

Ce deuxième panier répond aux [retours sur le lot A](../lot-a/RETOURS.md). Il propose trois nouveaux candidats pour sept besoins : impact ennemi, dégât joueur, dissolution, montée de niveau, choix de perk, annonce de danger et révélation du contenu d'un coffre. Cette dernière ponctuation est séparée de l'ouverture mécanique retenue au premier panier.

Ouvrir `index.html` directement dans un navigateur. Les données sont intégrées à la page et les aperçus sont locaux ; les pages des sources et licences nécessitent Internet. Chaque fiche indique quand le son intervient et quel retour guide la nouvelle recherche. Un contexte proposé pour une évolution future est explicitement distingué d'un déclenchement existant.

1. Comparer A/B/C et le son actuel, à volume modéré. Un seul lecteur joue à la fois.
2. Choisir un candidat, « Aucun : chercher autre chose », « Silence souhaité » ou laisser en attente.
3. Ajouter des notes, puis **Exporter mes choix** pour transmettre le JSON à l'agent.

Les choix du lot A restent dans leur [page historique](../lot-a/index.html). Les deux lots ont des espaces de stockage et des fichiers d'export distincts : un export du lot A est refusé par le lot A2 et réciproquement. Aucun choix de Raphaël n'est importé implicitement ; la page démarre sans candidat présélectionné si ce navigateur n'a pas déjà des choix pour ce lot précis.

Le stockage local dépend du navigateur. En cas d'indisponibilité, les choix restent exportables pendant la session ; exporter avant de fermer. Un import valide remplace uniquement les fiches présentes dans le fichier. Un import invalide est refusé avant toute modification. Les identifiants des candidats et du lot sont conservés dans le JSON exporté.

Les propositions restent à écouter et à valider en jeu. Sources, licences, préparation et éventuels découpages sont indiqués par candidat. Le gain de comparaison ne garantit pas une égalité perceptuelle. Aucun son de ce panier n'est automatiquement intégré au jeu.

## Reconstruire la page

Depuis la racine du dépôt :

```sh
python3 tools/audio/build_review.py --batch lot-a2
```

Sans argument, le générateur conserve le comportement historique et reconstruit le lot A. `candidates.json` est la source du catalogue ; le modèle partagé est `tools/audio/review_template.html`. Garder les identifiants stables pour préserver la compatibilité des choix exportés.


## Retours reçus le 26 septembre

[Décisions et export archivé](RETOURS.md) : dissolution B (version complète), perk A et danger A retenus ; impacts et level-up à rechercher ; révélation du coffre actuelle conservée. [Dissolution complète, sans coupe](preparations/dissolution_a2_b_complete.wav). Le panier ci-dessus reste l’historique exact de l’écoute ; aucun remplacement dans le jeu effectué.
