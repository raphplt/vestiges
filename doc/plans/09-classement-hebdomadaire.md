# Plan 09 — Classement mondial hebdomadaire

Statut : **à valider, réalisation reportée** · Priorité : après score/équilibrage.
Références : V2 §4/18/25 ; [plans 02](02-juiciness-score.md), [03](03-boucle-et-rythme.md), [06](06-personnages-quetes-defis.md).

## 1. Objectif et périmètre

Présenter un classement mondial renouvelé chaque semaine, les amis, sa position et ses records. Conserver un historique local utilisable hors connexion.

Aucune création de classement distant, remise à zéro, soumission de score ou modification Steam n'est autorisée par la préparation de ce document.

## 2. Phase 0 — Existant et documentation

[SteamLeaderboards.cs](../../scripts/Infrastructure/Steam/SteamLeaderboards.cs) expose :
- `UploadScore(int score, int crisesSurvived, string characterId)`.
- `LoadEntries(string boardName, LeaderboardRange range, int count = 10)`.
- `LoadGlobalTop`, `LoadFriendsTop`, `LoadAroundPlayer`.
- Signaux `ScoreUploaded(bool success)`, `EntriesLoaded(int count)`, état `IsLoading` et `LastEntries`.

Le nom hebdomadaire est fixe, `Vestiges_Weekly` ; le commentaire suppose un reset par l'administration, mais aucun cycle hebdomadaire n'est démontré. Quatre envois successifs partagent des callbacks/champs d'attente : risque de collision d'opérations à tester. Le Hub présente l'historique local, pas ces téléchargements.

[SteamManager.cs](../../scripts/Infrastructure/Steam/SteamManager.cs) utilise un AppId de test et se désactive sans SDK natif. La disponibilité d'un AppId de production et de sa configuration devra être vérifiée au démarrage de ce chantier.

Documentation officielle consultée le 21 septembre 2026 :
- [Leaderboards Steam](https://partner.steamgames.com/doc/features/leaderboards) : classements nommés ; écritures « Trusted » réservées à la Web API.
- [UploadLeaderboardScore](https://partner.steamgames.com/doc/api/ISteamUserStats#UploadLeaderboardScore) : un envoi en attente à la fois, dix envois par dix minutes.
- [ResetLeaderboard et SetLeaderboardScore](https://partner.steamgames.com/doc/webapi/ISteamLeaderboards) : opérations avec clé éditeur côté serveur sécurisé.

Ces contraintes obligent à concevoir la période et la file d'opérations ; un nom contenant « Weekly » ne suffit pas.

## 3. Décisions proposées

| Sujet | Proposition à valider |
|---|---|
| Renouvellement | Lundi 00:00 UTC ; dates locales affichées à l'utilisateur |
| Historique | Une période identifiée distinctement, anciens résultats consultables ; nouveau tableau actif chaque semaine |
| Règles | Identifiant de version du barème, mode et période |
| Comparabilité initiale | Un défi hebdomadaire à seed, personnage et accès de départ communs |
| Progression méta | Accès temporaire au kit du défi, sans débloquer le contenu des runs normales |
| Fin de semaine | Une run doit commencer et finir dans la même période pour être classée ; avertissement clair |
| Hors connexion | Résultat local conservé ; premier lot compétitif exige validation en ligne avant clôture |
| Départage | Même score = même valeur sportive ; ne pas inventer un ordre significatif non garanti |
| Intégrité | Exclure debug, règles modifiées et versions incompatibles ; niveau de vérification à choisir |

Le défi à conditions communes est une proposition plus précise que la demande initiale. Alternative : toutes les runs normales de la semaine, avec catégories de personnages/règles. Raphaël doit choisir ; le kit commun vise à éviter que la progression méta décide du classement.

## 4. Lots d'action

### Lot A — Contrat de compétition

1. Reprendre le score stabilisé en 02, son temps actif et son figement final.
2. Définir ce qui rend une run éligible, son identifiant unique, sa période, sa version, son seed et son mode.
3. Choisir période séparée ou reset effectif ; préférence pour périodes séparées afin de préserver l'historique.
4. Décider du niveau d'intégrité : classement communautaire avec limites explicites, ou vérification par service de confiance.
5. Si serveur requis, rédiger son contrat et son coût d'exploitation avant tout choix de fournisseur.

**Vérification :** cas de début/fin à la frontière, changement de version, run debug, reprise et offline résolus sans ambiguïté.
**Garde-fou :** l'heure locale et un score client seuls ne prouvent pas la validité compétitive ; ne pas promettre une protection forte avec un simple checksum.

### Lot B — Fiabilisation locale de l'intégration

1. Reprendre les wrappers Steam existants et isoler une requête de son état/résultat.
2. Sérialiser les uploads conformément à la documentation ; limiter et regrouper les tentatives inutiles.
3. Préserver la corrélation board/run/score dans les callbacks et les retries.
4. Gérer erreurs, délais, perte de connexion, changement de scène et annulation.
5. Garder la sauvegarde locale indépendante du succès de l'envoi.

**Vérification :** quatre tableaux recevant chacun le bon résultat, réponses retardées/désordonnées simulées, doublon/retry idempotent.
**Garde-fou :** pas de réutilisation d'un champ d'attente partagé par plusieurs requêtes ; aucune clé éditeur dans le client.

### Lot C — Périodes et règles effectives

1. Implémenter la résolution de période et la sélection du tableau selon le contrat approuvé.
2. Si compétition vérifiée, obtenir période et validation d'une autorité serveur ; spécifier les preuves de run nécessaires.
3. Fermer les soumissions anciennes selon la règle ; ne pas supprimer les records locaux.
4. Vérifier que la seed commune détermine effectivement les systèmes concernés ; documenter les aléas encore variables.
5. Tester plusieurs frontières de semaine avec horloge de test contrôlée.

**Vérification :** tableau actif neuf, ancien consultable, aucun résultat dans la mauvaise période, règles identiques affichées et appliquées.
**Garde-fou :** ne pas supposer qu'une seed du monde rend tous les tirages RNG déterministes.

### Lot D — Interface et activation

1. Reprendre Écho/Chroniques de 04 : monde, amis, autour de moi, historique.
2. Afficher période exacte, règles, score/rang, chargement, absence de score, offline et échec.
3. Tester en environnement Steam de test configuré avant toute activation de production.
4. Préparer une procédure de diagnostic/retrait des scores invalides et de suivi des erreurs.
5. Présenter le résultat complet à Raphaël avant ouverture publique.

**Vérification :** fonctionnement sans Steam, première entrée, amélioration/non-amélioration, résultats longs, nouvelle période.
**Garde-fou :** la consultation du classement ne bloque jamais le lancement d'une run locale.

## 5. Recette et limites

Tests locaux des périodes/file d'opérations puis essais Steam réels sur configuration dédiée, après autorisation d'exécuter ce chantier. Build et smoke si applicable.

La vérification serveur et l'exploitation d'une compétition peuvent constituer un sous-projet ; leur coût ne doit pas être caché dans un simple écran de score. Ce plan demeure reporté tant que 02/03/05/06 ne stabilisent pas les règles.

Roadmap G et V2 §18 ; ajouter les items hebdomadaires précis dans §25 après approbation du périmètre.

