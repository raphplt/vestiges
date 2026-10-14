# 49 — Atelier de test en run

Demande de Raphaël, 14 octobre 2026 : le profil admin/dev doit permettre de basculer l'invincibilité et de choisir niveaux, armes et temps ; ces outils doivent également servir aux agents de débogage.

## Lot D1 — Commandes partagées et panneau F4

- Panneau français accessible par F4 et par un bouton permanent en run dev ; sections joueur, armes, temps et monde, défilement aux petites résolutions.
- Invincibilité activable/désactivable, soin, XP et Essence à quantité choisie ; niveau exact du joueur sans choix de récompenses (le build porté reste distinct).
- Catalogue d'armes issu des données, emplacement, niveau et rareté des améliorations ; remplacement, ajout et suppression, avec refus des doublons et messages de résultat. Tirage reproductible sans consommer les flux aléatoires du gameplay.
- Pause explicite, vitesse de simulation, horloge de difficulté du spawn réglable. Le saut de cette horloge ne rejoue pas l'Effacement ni les événements passés ; l'accélération fait avancer la simulation entière.
- Téléportation par clic droit et apparition d'une créature choisie dans le catalogue.
- Service unique de commandes pour interface, bancs et arguments répétés `--dev-command "…"`. Réglages limités à la run ; vitesse restaurée à sa sortie.
- Outils et nouveaux hooks exclus des exports ; activation réservée au profil dev ou aux bancs pour le service, interface réservée au profil dev.

## Validation

- [x] Build sans avertissement, smoke et régressions outils/profils/exports.
- [x] Vérification des niveaux hauts et bas, slots/doublons, invincibilité, pause/vitesse, commandes invalides et lancement par arguments.
- [x] Captures en vraie run à 1080p et 720p ; guide DEV-MODE et roadmap V2 mis à jour après vérification.

## D1 livré — 14 octobre 2026

- Validation finale : build zéro avertissement, smoke, outils en vraie Main (17 contrôles normal, 58 banc, 72 dev) et exclusion des exports Debug/Release verts. Les suites profils dev et armes ont également passé la première validation, avant les derniers ajustements d'interface et de pause des choix.
- Captures via `CAPTURE_EXTRA_ARGS=--capture-dev-tools tools/capture_run.sh` à 1280×720 et 1920×1080 : sections joueur/armes puis temps/console ; panneau opaque, sélecteurs et défilement vérifiés. Chemins locaux : `/tmp/vestiges-dev-tools-capture-720/` et `/tmp/vestiges-dev-tools-capture-1080-final/`.
- Preuves des validations : `/tmp/vestiges-dev-tools-d1-20261014-final/` (3/3), `/tmp/vestiges-dev-tools-d1-20261014-b/` (2/2) ; première passe `/tmp/vestiges-dev-tools-d1-20261014/` (smoke, profils, armes verts ; erreur attendue d'identifiant de créature corrigée ensuite).
- Guide : [DEV-MODE.md](../DEV-MODE.md), avec recettes de commandes de lancement et limites de l'horloge de difficulté.
