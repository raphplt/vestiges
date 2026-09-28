# Plan 20 — Récompense, montée en puissance et points de dépense

Version 0.1 · 28 septembre 2026 · Statut : **retours consignés, rien de traité**. Raphaël demande de ranger ces retours dans un plan sans les traiter. Les faits de §2 ont été vérifiés dans le code le jour même. Aucune solution n'est choisie.

## 1. Retours de Raphaël (28 septembre, après les lots 8A–8C)

1. **Équilibrage** : « globalement l'équilibrage est mieux maintenant ». L'Effacement n'est pas trop rapide, et la difficulté face aux ennemis se gère bien. Vision : le jeu reste **assez dur** ; la réussite dépend en partie du **build** et des **déplacements**, sans devenir impossible. Des **pics de difficulté** au cours de la run sont bienvenus.
2. **Points de dépense** : sur une run de plus de dix minutes, il n'a vu **aucun sanctuaire ni aucun endroit où dépenser l'Essence**.
3. **Coffres** :
   - contenu souvent décevant (probablement couvert par la refonte des objets) ;
   - ennemis et coffres qui lâchent des armes, alors que les quatre emplacements se remplissent vite ;
   - certains bonus des coffres ne sont jamais expliqués.
4. **Pouvoir par personnage** : chaque personnage devrait avoir, en plus de son arme, un **pouvoir** qui lui est propre.
5. **Projectiles** : le « +1 projectile » ne s'applique pas à toutes les armes. Il faudrait un moyen d'augmenter la **taille des projectiles** (leur hitbox).
6. **Rareté des améliorations** : on ne la voit pas assez bien, ou elle n'est pas assez attrayante ni travaillée.
7. **Chance** : un système de chance pour obtenir de meilleures améliorations, en prenant le jeu de référence comme base sans le copier.
8. **Autels de mémoire** : existent-ils, ou sont-ils prévus ? Il n'en a vu aucun en jeu. Idée : en dépensant de l'Essence, on **préserve une zone** de l'oubli (ou on ralentit le processus). Pourquoi pas aussi un **bonus dans la zone proche** : régénération, dégâts, etc.
9. **Point capital : le risque n'est pas assez récompensé, et le jeu manque de dopamine et de satisfaction.**
   - Référence : dans le jeu de référence, on peut passer plusieurs niveaux d'un coup, parfois 10, 20 voire 50, en particulier à la fin d'un boss. Le jeu « scale » énormément, mais seulement si l'on a fait la bonne run, jamais automatiquement.
   - Ce jeu a des mécaniques de boost d'XP (les tomes), qui manquent sûrement à Vestiges. Pas de tomes chez nous : il faut trouver autre chose.
   - « Cela n'est pas à faire à la légère, ce point est actuellement super important. »
   - Conséquence : le jeu doit savoir gérer **un très grand nombre d'ennemis**.

## 2. État actuel vérifié

| Retour | Ce que fait le jeu aujourd'hui |
|---|---|
| 2, 8 — Où dépenser l'Essence | **5 Mémoriaux par carte** (`data/world/landmarks.json`) : un entre 8 et 14 % du rayon depuis le départ, quatre entre 20 et 90 %. On les ravive avec trois éclats (bénédiction), puis on y dépense l'Essence : raviver une arme, soin, lever un Oubli, relancer les bénédictions. Ravivés, ils **stabilisent leur zone** (mémoire remontée à 0,72 sur 2,5 cases, une seule fois). Ils ne sont signalés que par une **colonne de lumière**, sans flèche de bord d'écran : les flèches (`UI/ChestPointers.cs`) ne visent que les coffres. C'est la cause probable du « jamais vu ». Rien ne permet de payer pour préserver une zone dans la durée, ni pour y gagner un bonus local |
| 3 — Contenu des coffres | Tirage unique pondéré (`data/loot_tables/chest_*.json`). Commun : Essence 40 %, XP 20 %, perk 20 %, arme 20 %. Rare : 34 / 18 / 18 / 22. Épique : arme 35 %, perk 20 %, Essence 20 %, XP 10 %. Le « Don » est un perk de l'ancien catalogue (« Force Brute », « Berserker »…), affiché par son seul nom, sans son effet. Des créatures variantes peuvent lâcher une arme (`Enemy.TryDropWeapon`) |
| 5 — +1 projectile | Appliqué aux armes à distance (nombre de tirs) et de mêlée (nombre de coups en éventail). **Ignoré** par les motifs orbital (Boîte à musique), chaîne (Trousseau) et cône continu (Transistor) (`Player.cs`, `PerformAttack`) |
| 5 — Taille des projectiles | **Aucune** stat, aucun perk ni aucune arme ne l'augmente. Seule la zone (`aoe_radius`) grossit les attaques de zone |
| 7 — Chance | Stat `luck` : bénédiction Bonne Étoile (+0,03), passif et perks Fouineur. Au tirage d'une amélioration, chaque point de chance donne 10 « crans », et chaque cran 30 % de chance de monter d'une rareté (`upgrade_rarities.json`). L'oubli de la zone (1 à 3 crans) et le Péril en ajoutent. Elle pèse aussi sur les tiers de fragments et la rareté des perks. Avec Bonne Étoile seule (0,03 → 0,3 cran), l'effet est d'environ 9 % : **la Chance existe mais se sent à peine**, et rien ne la montre |
| 6 — Rareté visible | Couleur de rareté sur la carte du level-up (lot 1B du plan 17) ; pas de mise en scène propre aux raretés hautes. À auditer à l'écran |
| 9 — Plusieurs niveaux d'un coup | Techniquement possible (`PlayerProgression.OnXpGained` enchaîne les niveaux). Mais les sources massives d'XP n'existent pas : coffres de 15 à 100 XP, aucun multiplicateur d'XP en run hors mutateurs de difficulté, pas de pic d'XP après un boss |
| 9 — Beaucoup d'ennemis | Plafond de 110 créatures à l'écran, plus 4 par minute. Pool de créatures, banc de combat dense à 120 (`/bench`) ; ×2 FPS gagnés le 26 septembre. Il faudra mesurer bien au-delà |
| 4 — Pouvoir par personnage | Chaque personnage n'a qu'un passif signature (plan 06, fiches) et le dash commun. Mobilités spécifiques prévues (plan 01 lot E), pas de pouvoir actif |

## 3. Chantiers identifiés (à découper plus tard, un lot à la fois)

| Chantier | Retours | Plans liés | Remarque |
|---|---|---|---|
| **R1. Dopamine et montée en puissance** : sources d'XP massives et risquées, gains après un boss, « scaling » d'une bonne run | 9 | 03, 05, 11, 13 | **Priorité exprimée : capitale.** Pas de tomes. Demande une conception propre avant tout code, et des bancs avec beaucoup plus d'ennemis |
| R2. Récompense du risque | 1, 9 | 03, 12, 16, 17 | Le Péril, les Failles et les zones oubliées majorent déjà score, Essence et raretés : à rendre sensible et plus généreux |
| R3. Mémoriaux trouvables et points de dépense | 2, 8 | 17, 16 | D'abord les signaler (flèche de bord). Ensuite, l'idée d'autel qui préserve sa zone contre de l'Essence, avec un bonus local |
| R4. Coffres qui enthousiasment | 3 | 13 (non arbitré), 05, 17 | Le plan 13 couvre le butin ; les armes au sol quand les emplacements sont pleins et les Dons muets sont des défauts immédiats |
| R5. Chance ressentie | 7 | 17 §4.4, 13 | Sources plus nombreuses, effet affiché, lien avec la rareté |
| R6. Rareté attrayante au level-up | 6 | 02, 04, 17 | Mise en scène par rareté (cadre, lumière, son, attente) |
| R7. +1 projectile partout, taille des projectiles | 5 | 05 | Définir l'effet du +1 pour orbital, chaîne et cône ; nouvelle stat de taille (passif, perk ou arme) |
| R8. Pouvoir par personnage | 4 | 06, 01 E, 11 | À rapprocher des mobilités spécifiques et des mécaniques signatures des fiches |

## 4. Questions à poser avant de concevoir R1

- Dans le « scaling » recherché, qu'est-ce qui procure le plus de plaisir : la **cascade de niveaux** elle-même, l'écran de choix qui s'enchaîne, ou la puissance visible qui en résulte (écran rempli d'effets, ennemis balayés) ?
- Les pics d'XP doivent-ils venir surtout d'**actes risqués** (Failles, Péril, élites, zones effacées), d'une **récompense de boss**, ou d'un **objet de build** qui multiplie l'XP ?
- Jusqu'où monter le nombre d'ennemis simultanés : 200, 500 ou davantage ? Cela fixe le chantier de performance à mener avant.
