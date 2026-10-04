# Mettre Vestiges sur Steam

Marche à suivre pour obtenir l'App ID du jeu et faire le premier test réel (classements, plus tard succès). Établie le 4 octobre 2026 ; les montants et délais sont ceux connus à cette date, à revérifier sur le site de Valve.

## 1. Côté Raphaël : obtenir l'App ID

1. **Compte Steamworks** : sur partner.steamgames.com, s'inscrire comme partenaire avec un compte Steam (activer Steam Guard). Nom légal ou société, adresse.
2. **Formulaires** : coordonnées bancaires (pour les ventes), questionnaire fiscal (en particulier, formulaire W-8BEN pour un particulier résidant en France, qui évite la retenue américaine au titre de la convention fiscale), vérification d'identité.
3. **Steam Direct** : payer les frais par jeu (100 $, rendus après 1 000 $ de revenus bruts). **L'App ID est attribué à ce moment.** Un délai de 30 jours court ensuite avant toute sortie possible.
4. **Page « Bientôt disponible »** (facultative pour le test, nécessaire pour la sortie) : capsules, captures, description ; la page passe en revue chez Valve, puis doit être publique au moins deux semaines avant la sortie.
5. **Clé de développeur** : dans Steamworks, l'application a un paquet « Developer Comp » ; demander une clé, l'activer sur son compte Steam. Le jeu apparaît alors dans la bibliothèque et Steam accepte de l'initialiser.

Transmettre l'App ID à Claude : rien d'autre n'est nécessaire de ce côté.

## 2. Côté code : brancher l'App ID

- `scripts/Infrastructure/Steam/SteamManager.cs` : constante `AppId` (480, l'application de test de Valve, aujourd'hui).
- `steam_appid.txt` à la racine du projet, contenant le seul numéro : il permet de lancer le jeu hors du client Steam pendant le développement (sans lui, `RestartAppIfNecessary` relance par Steam). **À ne pas livrer dans le paquet final.**
- Bibliothèque native du SDK Steamworks (téléchargée depuis le site partenaire, dossier `redistributable_bin`) : `libsteam_api.so` sous Linux, `steam_api64.dll` sous Windows, à côté du binaire ou à la racine du projet en développement. Non versionnée.
- Succès : préparer la liste dans Steamworks (Stats & Achievements) en retirant les restes V1 du code, puis publier la configuration (onglet Publish).
- Classements : le jeu crée lui-même `Vestiges_Weekly_<année>-W<semaine>` et les autres tableaux au premier envoi (`FindOrCreateLeaderboard`) ; rien à déclarer à la main.

## 3. Premier test réel

Client Steam lancé et connecté au compte qui a la clé, puis `godot-mono --path .` : vérifier dans le journal que Steam s'initialise, finir une run normale (pas un essai ni le profil dev, qui n'envoient rien), puis regarder les tableaux dans Steamworks. Ce qui n'est pas prouvé aujourd'hui est listé au plan 26, Q3 : `CallResult` réel, création des tableaux, noms des joueurs hors amis.
