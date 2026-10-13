# Plan 43 — Classement aux éliminations

Statut : **ouvert le 13 octobre 2026** ([DECISIONS §155](DECISIONS.md)).

## 1. La demande

Raphaël veut que le classement revienne, avec un affichage refait :
- des records personnels, au global et par personnage ;
- surtout, un **classement global de tous les joueurs, remis à zéro chaque semaine**, sur des graines aléatoires et tous personnages confondus.

**Une seule mesure, les éliminations, partout** : classements, records, bilan. Le score composé disparaît de l'affichage. Les Vestiges gagnés deviennent **éliminations ÷ un taux**.

## 2. Ce qui existe

- Classements Steam (plan 26 Q3) :
  - une file unique (`LeaderboardQueue`) qui envoie et télécharge ;
  - des tableaux `Vestiges_Global`, `Vestiges_Char_<id>`, `Vestiges_Crises` et `Vestiges_Weekly_<année>-W<semaine>` (semaine ISO, remise le lundi), créés au premier envoi ;
  - vérifiés avec un faux service, mais pas avec Steam réel : il manque l'App ID (`doc/STEAM-MISE-EN-PLACE.md`).
- **Aucun écran ne lit les classements.**
- Le score (`ScoreManager`) additionne des points par créature (`data/scaling/score.json`), multipliés par le Péril, le personnage, l'oubli de la zone et les mutateurs. Les Vestiges valent score ÷ 10.
- Le meilleur score est gardé dans les statistiques du profil (`MetaStats.BestScore`) ; chaque run a son score dans l'historique.

## 3. Lots

### C1 — Les éliminations comme seule mesure

- **Bilan** : les éliminations remplacent le score, en tête avec la distance et la durée, et le détail du score disparaît. Le record personnel devient celui des éliminations, au global et pour le personnage joué.
- **HUD** : le compteur de score en haut à droite disparaît ; le compteur d'éliminations reste.
- **Vestiges** = éliminations ÷ taux (`data/scaling/score.json`, `kills_per_vestige`). Le taux est calé sur la mesure M1 du plan 42 pour qu'une run moyenne rapporte autant qu'aujourd'hui.
- **Records du profil** : meilleures éliminations au global et par personnage. Les anciens profils les reprennent de leur historique de runs.
- **Classements Steam** : envoi des éliminations aux tableaux de tous les temps, de la semaine et du personnage. Le tableau des Résurgences est retiré, puisqu'on garde une seule mesure.
- **Restes du score** : succès, analytics, quêtes et historique qui lisent le score passent aux éliminations. L'ancien champ de l'historique reste lisible.

### C2 — L'écran Classement

- Une entrée « Classement » à l'accueil, avec trois onglets :
  - **Cette semaine** : les 10 premiers, puis ta place et tes voisins, et le temps restant avant la remise à zéro ;
  - **De tous les temps** : même présentation ;
  - **Mes records** : ton meilleur nombre d'éliminations au global et par personnage, avec la date et la distance.
- **Au bilan**, après l'envoi : « 12ᵉ cette semaine », « nouveau record ».
- **Sans Steam** (pas de client, mode dev, essais) : les onglets en ligne disent pourquoi ils sont vides, et les records personnels marchent toujours.
- Vérification : capture de l'accueil avec un faux service rempli, et un test de la file de téléchargement.

### C3 — Premier test réel (après l'App ID)

- Brancher l'App ID (`SteamManager.AppId`, `steam_appid.txt`) et la bibliothèque native.
- Une run normale, puis contrôler les tableaux dans Steamworks et l'écran Classement.
- Points à prouver : `CallResult` réel, création des tableaux, noms des joueurs hors amis (plan 26 Q3).

## 4. Hors de ce plan

- Le classement entre amis : possible plus tard avec Steam, à voir après C3.
- Les graines du jour, imposées : écartées pour l'instant (DECISIONS §155 : graines aléatoires).
