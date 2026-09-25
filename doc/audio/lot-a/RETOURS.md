# Retours de Raphaël — lot A, 25 septembre 2026

Source : [export original](choix-raphael-2026-09-25.json), conservé octet pour octet. SHA256 : `16428e6bf51e01356beaf75fad28ff04cb89bf19b476473713f77e6b2a5e3f16`. Exporté le `2026-09-25T21:05:44.924Z`.

## Synthèse

Trois candidats retenus : **critical_hit_a**, **chest_open_a** pour l’ouverture physique, **dash_start_a**. Aucun n’est encore intégré. Les autres décisions restent exactement `pending` ou `none` selon l’export. La préférence pour l’XP actuelle est provisoire et ne constitue pas une sélection de candidat.

Sept recherches A2 : impact ennemi, dégât joueur, dissolution, montée de niveau, choix de perk, danger et nouvelle mélodie de révélation du coffre (`chest_reveal`). La séparation de l’ouverture et de la révélation ajoute un besoin au catalogue.

## Quand entend-on l’impact ennemi ?

Dans `scripts/Combat/Enemy.cs:989–1001`, `TakeDamage` joue le son après application des dégâts : normal ou critique. C’est le contact reçu par l’ennemi, y compris pour les projectiles (`scripts/Combat/Projectile.cs:221`). Le corps à corps aboutit aussi à `TakeDamage` (`scripts/Core/Player.cs:2367`). Une attaque dans le vide ne déclenche pas ce point. L’Indicible utilise une classe distincte, sans cet appel générique.

Les gestes d’armes (swing, corde, mécanisme, lancement) devront avoir leurs propres familles sonores. Aucun appel spécifique à chaque arme n’a été relevé dans les 24 définitions `data/weapons/weapons.json`, `PlayerAttackFx` ou les chemins d’attaque du joueur. Ce sont actuellement des besoins proposés, pas des sons déjà en jeu. Le candidat B jugé trop long reste archivé ; un bruit bref d’impact sera recherché.

## Verbatim de l’export

Les notes suivantes sont reproduites sans correction sous forme de chaînes JSON, afin de conserver aussi les espaces finaux ; le fichier exporté conserve également la représentation originale.

### enemy_hit

Décision : `none` ; candidat : `None`.

```json
"Le B est le mieux des trois mais est trop long. Apres le truc c'est que je sais pas quand ce son pop réellement. normalement les sons d'attaques se font par armes."
```

### critical_hit

Décision : `candidate` ; candidat : `critical_hit_a`.

```json
""
```

### player_hit

Décision : `pending` ; candidat : `None`.

```json
"Aucun ne le fait. rechercher autre cose. le signal absrait pouvait etre une bonne idée mais pas fan de celui là. mais chercher aussi des plus classiques"
```

### dissolution

Décision : `pending` ; candidat : `None`.

```json
"La C est la mieux mais je suis pas convaincu."
```

### xp_pickup

Décision : `pending` ; candidat : `None`.

```json
"L'actuelle est plutot bien"
```

### level_up

Décision : `pending` ; candidat : `None`.

```json
"L'actuelle est mieux que les trois mais j'aimerai quand meme trouver autre chose"
```

### perk_select

Décision : `pending` ; candidat : `None`.

```json
"essayer de trouver une seule note avec un echo ptet. "
```

### chest_open

Décision : `candidate` ; candidat : `chest_open_a`.

```json
"Le A est parfait pour le moment où on ouvre le coffre mais apres il faut une sorte de mélodie sur quelques secondes. (l'actuelle fait a peu pres le job)"
```

### dash_start

Décision : `candidate` ; candidat : `dash_start_a`.

```json
""
```

### danger_warning

Décision : `none` ; candidat : `None`.

```json
""
```

## Vérifications effectuées

- Export original et archive comparés octet pour octet ; SHA256 indiqué en tête. Les dix objets `decision`/`candidate_id`/`notes` du registre et de leur historique ont été comparés à l’export, sans normaliser l’orthographe ou les espaces.
- `python3 tools/audio/build_catalogue.py --check` : conforme ; génération répétée identique octet pour octet. Le catalogue compte 113 besoins actuels et trois sons retenus, aucun intégré/recetté.
- Reproduction en dossier temporaire avec A2 synthétique : les candidats A et A2 sont additionnés ; les dix retours et trois choix sont conservés ; les besoins redemandés ne redeviennent pas « candidats prêts ».
- Reproduction en dossier temporaire en retirant `critical_hit_a`, déjà retenu : erreur explicite ; JSON et Markdown de sortie inchangés. Aucun fichier ni choix réel modifié par cette reproduction.
- Validateur JavaScript réel de la page testé sous Node : les dix décisions sont lues fidèlement ; import d’un autre lot et candidat inconnu rejetés avant modification de l’état. Stockage, export et import utilisent un identifiant de lot distinct. Cette vérification de code complète la recette navigateur séparée ; elle ne valide pas le rendu sonore.
- `dotnet build` : réussite, 0 warning, 0 erreur. Aucun changement runtime ni asset audio du jeu ; pas de smoke test/capture requis pour ce lot documentaire et d’écoute.

- Synchronisation finale du lot A2 réel : 21 nouvelles propositions ajoutées aux 30 historiques, **51 sur 11 besoins**. Les trois candidats retenus restent présents avec leur source d’origine ; les six besoins redemandés restent à retravailler. `chest_reveal` a trois candidats ; XP conserve sa décision `pending` et sa note originale.

- Contre-vérification indépendante du panier A2 réel : **21 SHA256 d’originaux conformes**, **42 fichiers (originaux + aperçus) décodés par FFmpeg**, **21 durées d’aperçu conformes** aux métadonnées (tolérance 30 ms). Médias : **11 904 862 octets**, soit 11,35 Mio. Les liens locaux du plan, des retours et de la couverture existent.
- Licences du panier A2 comparées aux preuves locales : **19 CC0-1.0**, **2 CC-BY-3.0** (Bart Kelsey, crédit incluant la commande de Will Corwin et les modifications). La seule source originale réutilisée depuis A est `enemy_hit_b` sous `enemy_hit_a2_a` : extrait raccourci explicitement documenté. Aucun autre original refusé/recherché à nouveau n’est reproposé à l’identique. Ces contrôles techniques ne constituent pas une écoute artistique.
