# Retours de Raphaël — lot A2, 26 septembre 2026

Source : [export original](choix-raphael-2026-09-26.json), conservé octet pour octet. SHA256 : `2ffb133bf9d53dc42af129236a53f33b175fb00fc301b242f0a590304067cbef`. Exporté le `2026-09-26T08:42:17.958Z`.

## Décisions

| Effet | Décision à appliquer |
|---|---|
| Impact ennemi | Aucun candidat A2 ; reprendre la recherche. |
| Dégât joueur | Aucun candidat A2 ; reprendre la recherche. |
| Dissolution | B retenu, conserver toute sa durée. |
| Montée de niveau | Aucun candidat A2 ; reprendre la recherche. |
| Choix de perk | A retenu. |
| Annonce de danger | A retenu. |
| Révélation du coffre | Garder l’actuel, conformément à la note. Le champ exporté reste `pending`. |

Les trois sélections A restent valides : critique, ouverture physique du coffre, départ de dash. Total : **six candidats retenus**, **un son actuel conservé**, **trois besoins à retravailler**, XP en attente et 102 autres besoins à rechercher. Aucun candidat intégré ni recetté en jeu. Les retours A restent dans l’historique ; les refus A2 ne donnent pas une nouvelle raison artistique au-delà de celles déjà exprimées.

## Dissolution complète

[Écouter la préparation complète](preparations/dissolution_a2_b_complete.wav) — **1,395828 s**, contre 0,85 s pour l’aperçu historique. Source Ogrebane, Dark Ambiences, CC0-1.0 ; preuve et original conservés dans le lot A2. Traitement : gain constant −11 dB, conversion PCM16 / 44,1 kHz, sans coupe ni fondu ajouté. SHA256 : `ce2322b7222aa0666c7a7db62e232129f2437cf851fbbc13c22b778b4a5d2ec2`.

Le panier et son aperçu historique restent intacts pour conserver exactement ce qui a été évalué. La préparation complète est référencée dans `integration.preparation` du registre et sera à utiliser lors de l’intégration. La durée complète demandée prévaut sur le brief générique de durée courte.

## Verbatim de l’export

### enemy_hit

Décision : `none` ; candidat : `null`.

```json
""
```

### player_hit

Décision : `none` ; candidat : `null`.

```json
""
```

### dissolution

Décision : `candidate` ; candidat : `dissolution_a2_b`.

```json
"le B mais ne pas le cropper à un seconde"
```

### level_up

Décision : `none` ; candidat : `null`.

```json
""
```

### perk_select

Décision : `candidate` ; candidat : `perk_select_a2_a`.

```json
""
```

### danger_warning

Décision : `candidate` ; candidat : `danger_warning_a2_a`.

```json
""
```

### chest_reveal

Décision : `pending` ; candidat : `null`.

```json
"garder l'actuel"
```

## Vérifications

- Export archivé identique octet pour octet ; sept décisions/notes et leurs entrées d’historique comparées à l’export. Historique A, choix hors A2 et paniers d’écoute préservés.
- Préparation de dissolution et original : 61 556 trames chacun, stéréo, 44,1 kHz, durée 1,395828 s ; aucun échantillon de la durée source supprimé. SHA256 de l’original conforme aux métadonnées. Décodage FFmpeg réussi ; crête −11,1 dBFS, moyenne −23,8 dBFS après gain constant.
- `python3 tools/audio/build_catalogue.py --check` : conforme. Le résumé des choix est désormais calculé depuis le registre, sans total de trois sélections figé dans le générateur.
- Synchronisation avec le dépôt actuel : quatre composants du nouveau Hub ajoutés aux correspondances (`HubBackdrop`, `HubCamp`, `HubChroniquesPanel`, `HubMenuButton`). Le fond partage seulement le contexte musical ; les trois autres utilisent les sons UI existants. 23 composants UI suivis, toujours 113 besoins audio ; aucune fonctionnalité sonore nouvelle.
- `dotnet build` : réussite, zéro warning et zéro erreur. `git diff --check` : conforme. Aucun code runtime, scène ou asset audio du jeu modifié par ce sous-lot ; pas de smoke test ni de recette sonore en jeu effectués.
