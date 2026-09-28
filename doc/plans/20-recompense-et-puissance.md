# Plan 20 — Récompense, montée en puissance et points de dépense

Version 0.4 · 28 septembre 2026 · Statut : **paliers de 300 à 400 niveaux validés (§6.6–§6.7) ; lots D1 (§7.2), R1-0 et R1-T (§6.8) livrés ; R1-F proposé ; autres lots à valider**. Les faits de §2 ont été vérifiés dans le code le jour même.

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

## 5. Réponses de Raphaël (28 septembre)

1. **Ce qui fait plaisir** : la cascade de niveaux, les choix qui s'enchaînent et la puissance visible, tous les trois, à des degrés différents. La cascade ne doit pas être permanente. Il demande **un document qui montre, de runs médiocres à excellentes, les niveaux attendus par palier** (5, 10, 20, 40 min…) :
   - l'exponentiel et les grosses cascades sont réservés aux très bonnes runs (chance, bon build, bonne exécution) ;
   - le gain de niveaux doit remonter un peu pour tous les archétypes ;
   - « avoir des métriques et des calculs pour ça est quelque chose d'important ».
2. **D'où vient l'XP** : des actes risqués, d'une bonne gestion de la masse et des boss battus.
   - **Un boss intermédiaire difficile à mi-parcours.** Le joueur doit pouvoir **régler la difficulté du boss**, par exemple en activant des autels ou selon son score ; un boss plus dur rapporte plus.
   - Les boss lâchent beaucoup d'XP, mais ce sont **surtout les Résurgences** qui doivent en donner plus : plus de danger et plus de créatures, plus d'XP, mais plus dur à gérer.
   - Le contrôle de la difficulté du jeu de référence est « une idée excellente », mais le recopier tel quel serait du pompage : trouver des adaptations.
   - On doit pouvoir **construire son build autour de l'XP et de la chance**, pourquoi pas d'un troisième axe (maîtrise du chaos ou de l'oubli), par les passifs ou par les objets.
3. **Nombre d'ennemis** : « plus de 1000 ?? En fait, le plus possible. »
4. **Nouveau point** : les ennemis infligent peut-être trop de dégâts à distance, ce qui gonfle la difficulté. À creuser.

## 6. Modèle de progression — 28 septembre 2026 (proposition, cibles à valider)

Réponse à §5.1 : niveaux attendus par palier, de la run médiocre à l'excellente, avec les calculs qui les justifient. Rien n'est implémenté côté jeu.

### 6.1 Outils et mesure

- `tools/progression_model.py [dossier de mesure]` : courbe d'XP lue dans `PlayerProgression.cs`, cibles par archétype, XP par minute qu'elles supposent, niveaux et XP mesurés, et **simulation des leviers** (dictionnaires `LEVERS` et `PLAY` en tête du fichier).
- `tools/damage_sources.py <dossier>` : répartition des dégâts reçus par créature et par rôle (§7).
- `RunObservation` (mesure de densité) relève désormais l'XP ramassée et les orbes au sol chaque seconde, les dégâts reçus par créature (bruts, et filtrés par l'invulnérabilité de 0,5 s), l'exposition et les morts par espèce.
- Mesure : `MEASURE_EXTRA_ARGS="--nomad" MEASURE_JOBS=2 tools/measure_run.sh <dossier> 1800 "221092026 42"`, soit 30 min de jeu en 942 s d'horloge.

**Limite du bot** : il est invincible et garde un cap. Au level-up, il prend toujours la première carte, sans chercher ni combat ni orbe. C'est une **run passive**, entre médiocre et moyenne pour le build, mais qui ne meurt jamais. Sur la seed 42, ses trois premiers choix sont des passifs : le Traqueur reste au niveau 4 de 2 à 5 min (11 morts en 3 min).

### 6.2 Courbe actuelle

XP pour passer du niveau n au suivant : 20 × n^1,35, majorée de 65 % à 20 % aux niveaux 1 à 5. **Constantes en dur** dans `PlayerProgression.cs` (contraire à la règle data-driven) ; seul le Péril (+8 % par point) multiplie l'XP. Aucune stat, aucun passif ni aucun objet n'augmente le gain d'XP ; seul l'aimant (`xp_magnet_radius`) agrandit le ramassage.

| Niveau | XP pour le suivant | XP cumulée |
|---|---|---|
| 10 | 448 | 1 839 |
| 20 | 1 141 | 9 302 |
| 30 | 1 973 | 24 362 |
| 40 | 2 909 | 48 229 |
| 50 | 3 932 | 81 860 |
| 75 | 6 798 | 213 712 |
| 100 | 10 024 | 421 695 |
| 150 | 17 328 | 1 097 556 |

L'espace d'amélioration suffit pour 150 niveaux et plus : arme jusqu'au niveau 50, quatre armes et quatre passifs (niveau 5), soit plus de 200 montées utiles. Une cascade est déjà possible techniquement : `PlayerProgression` enchaîne les niveaux, `FragmentManager` met les écrans de choix en file.

### 6.3 Ce que mesure le jeu actuel (bot, seeds 221092026 / 42)

| | 2 min | 5 min | 10 min | 15 min | 20 min | 30 min |
|---|---|---|---|---|---|---|
| Niveau | 4 / 4 | 11 / 4 | 17 / 16 | 20 / 23 | 25 / 29 | 34 / 40 |

| | 5→10 min | 10→15 min | 15→20 min | 20→30 min |
|---|---|---|---|---|
| XP ramassée par minute | 861 / 1 039 | 590 / 1 667 | 1 425 / 1 778 | 1 622 / 2 697 |
| Morts par minute | 84 / 115 | 93 / 181 | 203 / 190 | 234 / 322 |
| Orbes au sol en fin de palier | 190 / 284 | 358 / 421 | 533 / 497 | 859 / 765 |

Vérification des constats :
- **XP par créature fixe** : confirmé. Charognard 8, Ombre 5, Rôdeur 20, Brute 25, Tréant 35 ; seules les variantes la multiplient (élite ×4, champion ×14, aberration ×2) et la Harde (×1,5). Rien ne suit le temps, l'oubli ni les Résurgences. Les PV des créatures montent de ×1,05 par minute, composé (×1,63 à 10 min, ×2,65 à 20, ×4,32 à 30) : **l'XP par PV à abattre est divisée par 4 à 30 min**.
- **Orbes au sol** : 750 à 920 au maximum sur 30 min, mais **2 300 à 2 600 à 45 min** (mesure de §6.8) : les milliers apparaissent en fin de run. En comptant les morts par espèce, le bot ramasse l'essentiel de l'XP lâchée : au plus 10 à 15 % reste au sol. La vraie perte est ailleurs : **73 % des créatures apparues ne meurent jamais** (17 354 apparues, 4 568 tuées sur une seed). Le joueur les distance et elles sont recyclées au-delà de 1 400 px.
- **L'Indicible rapporte 500 XP**, moins d'un cinquième de niveau à 40 (2 909 XP pour le suivant) ; les coffres, 15 à 100 XP.

### 6.4 Cibles par archétype (à valider par Raphaël)

Durées de vie reprises de la V2 (§5) : un joueur régulier meurt entre 15 et 25 min, un bon joueur tient de 25 à 40 min.

| Archétype | Ce qui la distingue | 2 min | 5 min | 10 min | 15 min | 20 min | 30 min | 40 min |
|---|---|---|---|---|---|---|---|---|
| Médiocre | build dispersé, évite le risque, meurt vers 17 min | 4 | 10 | 18 | 25 | — | — | — |
| Moyenne | build correct, boss de rang 2, meurt vers 25 min | 4 | 11 | 21 | 31 | 38 | — | — |
| Bonne | build cohérent, Résurgences jouées au cœur, Péril 3, bat l'Indicible | 5 | 12 | 24 | 40 | 52 | 72 | 90 |
| Excellente | build XP ou oubli, Péril 6, boss de rang 5, cascades | 5 | 13 | 27 | 50 | 72 | 110 | 150 |

Principes :
- **Début inchangé** (≤ 5 min), conformément au plan 03 (début menaçant, XP pas trop rapide) : l'écart vient de la façon de jouer, pas du départ.
- **Tous les archétypes montent un peu** : +20 à +40 % de niveaux au milieu de run par rapport au jeu actuel simulé (médiocre : 15 → 18 à 10 min ; moyenne : 22 → 31 à 15 min).
- **Les cascades n'arrivent qu'aux runs qui empilent les risques** : les leviers se multiplient entre eux, et seule une run qui les cumule décolle.

XP à ramasser par minute pour tenir ces cibles, comparée au bot :

| | 10→15 min | 15→20 min | 20→30 min | 30→40 min |
|---|---|---|---|---|
| Médiocre | 1 710 | — | — | — |
| Moyenne | 3 178 | 3 273 | — | — |
| Bonne | 6 778 | 8 320 | 10 422 | 13 476 |
| Excellente | 12 578 | 22 438 | 33 404 | 56 947 |
| Bot mesuré | 590–1 667 | 1 425–1 778 | 1 622–2 697 | — |

Une run moyenne doit donc ramasser deux à trois fois plus que le bot ; une bonne, cinq fois plus ; une excellente, quinze à vingt-cinq fois plus en fin de run. Aucune de ces trois n'est atteignable aujourd'hui : la simulation du jeu actuel donne 22 niveaux à 15 min pour une run moyenne, 47 à 30 min pour une bonne, 61 pour une excellente.

### 6.5 Leviers chiffrés

La simulation part de l'XP ramassée par le bot, minute par minute. Pour chaque archétype, elle applique une façon de jouer (`PLAY`) : récolte par rapport au bot (0,8 à 1,5), temps passé en zone oubliée, engagement dans les Résurgences, Péril, bonus d'XP de build, rang du boss choisi. Ce sont des **hypothèses** à discuter ; le modèle sert à vérifier qu'un jeu de leviers donne l'écart voulu avant d'en coder un seul.

Jeu de leviers calibré : il tient les cibles à ±10 % à chaque palier, sauf l'excellente à 40 min (133 pour 150).

| Levier | Aujourd'hui | Proposé | Effet simulé en le retirant seul (moyenne à 20 min / excellente à 30 min) |
|---|---|---|---|
| XP par créature selon le temps | fixe | × (1 + 0,02 × minute) : ×1,2 à 10 min, ×1,4 à 20, ×1,6 à 30 | 38 → 35 / 109 → 96 |
| XP selon l'oubli de la zone | rien (l'oubli majore score et Essence seulement) | × (1 + 0,5 × oubli) : ×1,25 en Effilochée, jusqu'à ×1,5 en Effacée | 38 → 35 / 109 → 95 |
| XP pendant une Résurgence | rien (seulement plus de créatures) | ×2 pendant les 70 s | 38 → 36 / 109 → 99 |
| Péril | +8 % par point | +12 % par point (×1,72 à Péril 6) | 38 → 38 / 109 → 103 |
| Boss intermédiaire vers 13 min | n'existe pas | selon le rang choisi : +2, +4, +7, +10 ou +15 niveaux | 38 → 36 / 109 → 103 (et 57 → 44 à 15 min) |
| XP de l'Indicible | 500 XP | +8 niveaux (≈ 34 000 XP vers le niveau 50) | — / 109 → 103 |
| Build orienté XP | aucun moyen | jusqu'à +60 % à 20 min (hypothèse de l'excellente) | — / 109 → 93 |

**Récompenses de boss en niveaux, pas en XP.** Exprimer le gain en niveaux (« rang 5 = +15 niveaux ») garde la cascade intacte même si la courbe change. Le rang 5 vaut environ 39 000 XP au niveau 30.

**Boss intermédiaire à difficulté choisie**, adaptations possibles (le contrôle de difficulté du jeu de référence n'est pas recopié) :
- **A. Sceaux des Mémoriaux** (recommandée) : le boss s'appelle à un Mémorial, entre 10 et 16 min. Son rang, de 1 à 5, vaut le nombre de Mémoriaux ravivés et de Failles ouvertes avant l'appel. Sans appel, il vient seul à 16 min, au rang 1. Cela réutilise des lieux existants et donne enfin une raison de les chercher (retours 2 et 8).
- **B. Le boss se nourrit de l'oubli** : il absorbe les orbes laissées au sol et grossit d'autant, puis les rend doublées à sa mort. Le joueur règle la difficulté par ce qu'il abandonne derrière lui.
- **C. Rang = Péril** au moment du combat : simple, mais sans geste propre au boss.

**Résurgences plus payantes.** En plus du ×2, un **reflux à l'accalmie** : à la fin d'une Résurgence, les orbes laissées dans un large rayon (1,5 écran) rejoignent le joueur. Plus il a tué au cœur de la vague, plus la cascade est grande. C'est lisible (« la mémoire revient ») et récompense la gestion de la masse, sans coût de ramassage.

**Ramassage.** Ce n'est pas le premier gisement (≤ 15 % de l'XP lâchée). Deux actions restent utiles : la **fusion des orbes** proches (moins de nœuds, et une grosse orbe se voit mieux), et un aimant plus présent dans les tirages.

**Axes de build (pas de tomes)**, par passifs d'abord, objets ensuite (plan 13, non arbitré) :
- **XP** : un passif du type « Mémoire vive », +8 % d'XP par niveau (+40 % au niveau 5), une bénédiction de Mémorial équivalente.
- **Chance** : elle existe mais se sent à peine (≈ 9 % avec Bonne Étoile, §2) ; chantier R5.
- **Oubli (troisième axe)** : un passif qui renforce le levier d'oubli (par exemple +50 % d'XP et de dégâts en zone Effacée), et une récompense de kill en zone oubliée (une orbe de plus). Il pousse vers le danger que l'Effacement crée déjà, ce qui est propre à Vestiges.

**Mise en scène d'une cascade** : 15 niveaux d'un coup, ce sont 15 écrans de choix à la suite. C'est ce que Raphaël aime (§5.1), mais le rythme de ces écrans est à concevoir avec le plan 02 (J4). Par exemple, une entrée plus rapide à partir du deuxième, et le compteur de niveaux restants affiché.

#### Densité vers 1 000 créatures et plus

Mesure de l'audit du 27 septembre (banc de combat dense, 1080p) : de 60 à 240 créatures, le temps d'image passe de 4,71 à 11,08 ms, soit **≈ 35 µs par créature**. En prolongeant cette pente, 500 créatures donneraient ≈ 20 ms (50 FPS) et 1 000 ≈ 38 ms (26 FPS). Pour tenir 60 FPS à 1 000, il faut descendre sous ≈ 12 µs par créature, **trois fois moins** ; à 2 000, six fois moins. Aujourd'hui, le plafond est de 110 créatures à l'écran, plus 4 par minute (230 à 30 min).

Ce que coûte une créature aujourd'hui : un `CharacterBody2D` avec `MoveAndSlide`, un `AnimatedSprite2D`, ses capacités, son ombre et ses marqueurs, chacun traité dans son propre `_PhysicsProcess`. À chaque attaque, les armes parcourent toute la liste des créatures (`GroupCache.GetEnemies`, `Player.cs`) : ce coût croît avec la foule et le nombre d'armes.

Travail à prévoir, dans l'ordre :
1. **Banc de foule réaliste** : 250, 500, 1 000 et 2 000 créatures, avec morts, orbes et renouvellement (le banc actuel empêche les morts). Attribuer le coût : physique, scripts, rendu.
2. **Déplacement de foule** : un gestionnaire unique qui déplace toutes les créatures depuis des tableaux C#, avec une grille spatiale pour la séparation et une carte des obstacles précalculée, au lieu d'un `MoveAndSlide` par créature.
3. **Rendu** : des éléments de dessin directs (`RenderingServer`) plutôt que des nœuds. Un `MultiMesh` serait plus rapide mais sort du tri en Y de `Main` : les créatures ne passeraient plus derrière les décors.
4. **Niveau de détail** : les créatures hors écran se mettent à jour une image sur quatre, sans animation.
5. **Orbes et effets** : fusion des orbes proches, chiffres de dégâts agrégés (budget `fx_budget.json`).
6. **Ciblage des armes** par la même grille spatiale, au lieu d'un parcours complet de la liste.

C'est un chantier à part entière (plusieurs lots), qui touche le cœur du combat. **Réponse de Raphaël** : viser environ 500 créatures, réservées au très late game et aux Résurgences les plus dures, davantage si possible. L'optimisation est confiée à un autre agent. Le rendu d'une telle foule reste à juger en capture. **Densité et XP sont liées** : multiplier les créatures par cinq multiplie le flux d'XP. Il faudra alors rejouer le modèle et baisser l'XP par créature, sans quoi toutes les runs cascadent.

### 6.6 Révision du 28 septembre (soir) : 300 à 400 niveaux en 45 min

Retour de Raphaël sur §6.4 : une excellente run doit atteindre **300 à 400 niveaux en 45 min**, voire 1 000 si les armes plafonnent (au-delà, les niveaux ne servent plus). La difficulté des créatures doit suivre. La montée n'est pas linéaire : elle vient **par vagues** (boss, événements) et dépend beaucoup du build et du talent.

**La courbe actuelle l'interdit** : le niveau 400 coûte 11 millions d'XP, le niveau 150 déjà 1,1 million. **Courbe proposée** : identique jusqu'au niveau 41, puis **3 000 XP par niveau, fixe**. Le niveau 400 revient alors à 1,13 million d'XP. Les runs médiocres et moyennes ne dépassent pas le niveau 40 : elles ne voient aucune différence.

| Niveau | Actuelle : pour le suivant / cumulée | Proposée : pour le suivant / cumulée |
|---|---|---|
| 40 | 2 909 / 48 229 | 2 909 / 48 229 |
| 100 | 10 024 / 421 695 | 3 000 / 228 138 |
| 200 | 25 552 / 2 162 008 | 3 000 / 528 138 |
| 400 | 65 134 / 11 054 325 | 3 000 / 1 128 138 |

**Cibles révisées (à valider)** et simulation avec la courbe proposée et les leviers de §6.5, inchangés :

| Archétype | 5 min | 10 min | 15 min | 20 min | 30 min | 40 min | 45 min |
|---|---|---|---|---|---|---|---|
| Médiocre, cible / simulée | 10 / 8 | 18 / 17 | 25 / 25 | — | — | — | — |
| Moyenne | 11 / 9 | 21 / 19 | 32 / 31 | 40 / 38 | — | — | — |
| Bonne | 12 / 11 | 25 / 24 | 42 / 40 | 55 / 53 | 105 / 102 | 150 / 153 | — |
| Excellente | 13 / 13 | 30 / 30 | 60 / 59 | 90 / 86 | 190 / 181 | 300 / 288 | 380 / 358 |

La courbe plafonnée seule ne suffit pas : sans les leviers de §6.5, l'excellente run s'arrête à 112 niveaux à 45 min et la bonne à 62 à 40 min. Il faut à la fois des sources d'XP qui se multiplient et un coût de niveau qui cesse de croître.

**Conséquences à concevoir :**
1. **Cadence** : en fin de run excellente, 10 à 16 niveaux par minute, soit un toutes les 4 à 6 s. Il faut une **réserve de niveaux** (réponse de Raphaël à la question 4). Les niveaux gagnés s'empilent dans un compteur au HUD, et l'écran de choix enchaîne tous les choix en attente sans se refermer entre deux. Il s'ouvre au plus toutes les 20 à 30 s, ou quand le joueur le demande, pour ne pas hacher le combat. Les cascades de boss et de reflux passent par la même réserve.
2. **Plafond du build** : un build complet (quatre armes au niveau 50, quatre passifs au niveau 5) représente **environ 219 montées**. Au-delà, il n'y a plus rien à choisir. Proposition : des **niveaux de surplus** sans écran, chacun ajoutant un petit bonus automatique en données (par exemple +1 % de dégâts ou +2 PV max), et **moins chers** (750 XP). Avec la même XP, l'excellente run atteindrait alors **environ 860 niveaux** (1 180 à 500 XP par niveau de surplus). C'est la voie vers « 1 000 si les armes plafonnent ».
3. **La difficulté doit suivre** : les PV des créatures montent de ×1,05 par minute (×9 à 45 min). Mesure proposée : le **temps moyen pour tuer une créature**, par palier, relevé par `RunObservation`. S'il s'effondre en fin de run excellente, la difficulté ne suit plus. Deux réponses possibles : une montée plus forte en endgame (déjà ×1,55 de PV), ou une part de la montée liée au niveau du joueur. La seconde punit la montée de niveau ; elle est à éviter si possible.
4. **Récompenses de boss en niveaux** : le rang 5 (+15 niveaux) ne vaut plus que 45 000 XP au-delà du niveau 41. Le reflux et les Résurgences deviennent les vraies vagues.

### 6.7 Réponses du 28 septembre (nuit)

- **Paliers de §6.6 validés** (médiocre à excellente, jusqu'à 380 niveaux à 45 min).
- **Au-delà du build complet**, les niveaux ne servent plus à rien, ou à quelque chose de relativement trivial. Raphaël est ouvert aux idées (ci-dessous).
- **Réserve de niveaux** : un « cache » rapide, **toujours automatique, jamais manuel**. Il ne se déclenche qu'après quelques secondes, et seulement quand beaucoup de niveaux arrivent ou qu'il y a beaucoup de créatures. Sinon, l'écran s'ouvre comme aujourd'hui.
- **Montée plus forte des créatures en endgame** : retenue, plutôt qu'une part liée au niveau du joueur.
- **XP et chance** : pas de tomes, mais des **objets dédiés** (pas encore refondus ni branchés, plan 13) et des **perks**, les quatre passifs empilables sous les armes. Ils se cumulent entre eux. Les perks n'ont pas encore été refondus : seules les armes l'ont été (plan 17).

**Réserve de niveaux, règle proposée** (lot R1-G, valeurs en données) :
- un niveau gagné ouvre l'écran immédiatement, comme aujourd'hui ;
- l'écran est retenu au plus **3 s** quand un second niveau arrive dans les 3 s, ou quand plus de **60 créatures** sont à l'écran. Les niveaux arrivés entre-temps s'empilent, puis s'enchaînent dans le même écran, avec le nombre restant affiché ;
- aucun écran ne s'ouvre à moins de 3 s de la fermeture du précédent, sauf à la fin d'un boss (la cascade doit se voir tout de suite).

**Idées pour les niveaux de surplus**, triviales mais senties, à choisir (plusieurs possibles) :
- **A. Un souffle** : chaque niveau rend 5 % des PV et remplit le bouclier. Le niveau garde un effet immédiat en combat, sans rien ajouter au build.
- **B. De l'Essence** : chaque niveau rapporte de l'Essence à dépenser aux Mémoriaux. L'XP excédentaire nourrit les points de dépense (retour 2).
- **C. La mémoire repousse l'oubli** : chaque niveau remonte un peu la mémoire de la zone autour du joueur. C'est propre au monde de Vestiges, et utile au joueur qui avance loin.
- **D. Une onde tous les 10 niveaux** : une vague qui repousse et blesse les créatures proches, dans la mise en scène de la montée de niveau (plan 02 J4). Puissance visible, sans stat.
- **E. Du score seulement** : le plus simple, utile pour le classement (plan 09).

Recommandation : **A et D** ensemble, qui donnent de la dopamine sans toucher au build ; **B** si les Mémoriaux manquent encore de raisons d'être visités.

### 6.8 Lots R1-0 et R1-T livrés (28 septembre, nuit)

**R1-0** : la courbe d'XP quitte `PlayerProgression.cs` pour `data/scaling/progression.json`, lue par `XpCurveConfig` (même modèle que `DefenseConfig`) et par `tools/progression_model.py`. Le coût est identique jusqu'au niveau 41, puis plafonné à 3 000 XP par niveau. `RunEventContext.XpForLevelRatio` et la barre d'XP du HUD lisent toujours `XpToNextLevel` et suivent d'eux-mêmes.

**R1-T** : `RunObservation` relève en cumul les PV des créatures apparues et les dégâts infligés. L'outil en déduit, par palier, le **temps pour tuer** : le PV moyen d'une créature divisé par les dégâts infligés par seconde. Les dégâts comptent aussi ceux qui dépassent la vie restante : c'est une tendance, pas une durée exacte. `tools/measure_run.sh` accorde désormais à chaque seed le double de la durée demandée au-delà de 900 s (coupure à 1 800 s auparavant).

**Mesure** : `MEASURE_EXTRA_ARGS="--nomad" MEASURE_JOBS=2 tools/measure_run.sh <dossier> 2700 "221092026 42"`.

| Palier | 5→10 min | 10→15 | 15→20 | 20→30 | 30→40 | 40→45 |
|---|---|---|---|---|---|---|
| Temps pour tuer (s), deux seeds | 0,27 / 0,33 | 0,28 / 0,24 | 0,24 / 0,20 | 0,22 / 0,19 | 0,21 / 0,12 | 0,22 / 0,14 |
| XP ramassée par minute | 1 467 / 1 506 | 1 414 / 2 133 | 2 035 / 2 718 | 2 242 / 3 076 | 2 407 / 4 138 | 2 943 / 3 247 |
| Orbes au sol en fin de palier | 418 / 376 | 837 / 661 | 1 198 / 1 013 | 1 932 / 1 541 | 2 413 / 1 925 | 2 616 / 2 288 |

Niveau à 45 min : 52 et 65. Sur la seed 221092026, le bot reste au niveau 5 jusqu'à 5 min : ses premiers choix sont des passifs (§6.1).

Lecture :
- **Le bot, un build faible, dépasse déjà la montée des créatures** : son temps pour tuer baisse de moitié entre 10 et 45 min (0,30 → 0,18 s en moyenne). Une run à 380 niveaux l'effondrerait. R1-F est confirmé.
- **L'endgame n'est jamais atteint** : les deux runs restent en late game jusqu'au bout, car l'Indicible n'est pas combattable (plan 03 lot E, 27 septembre). Aujourd'hui, la montée « d'endgame » (PV ×1,55) ne s'applique donc jamais.

**Proposition R1-F** (à caler par la même mesure) : à partir de 22 min, l'heure de l'Indicible, et quelle que soit la phase, les PV des créatures montent de **×1,07 par minute au lieu de ×1,05**. Cela donne ×1,5 à 45 min par rapport à aujourd'hui, de quoi ramener le temps pour tuer du bot vers 0,27 s. Pour les runs à plusieurs centaines de niveaux, il faudra recaler après R1-A : le bot montera alors bien plus haut et donnera enfin la mesure.

### 6.9 Lot R1-A + R1-G, découpage (28 septembre, nuit)

**R1-A — XP qui suit le risque**, fixée à l'apparition de chaque créature par `SpawnManager`, qui connaît déjà la minute, la phase et la mémoire du lieu (il s'en sert pour la vitesse). Aucun calcul n'est ajouté à la mort ni par image :
- XP × (1 + 0,02 × minute) (`xp_growth_per_minute`) ;
- XP × (1 + 0,5 × oubli du lieu d'apparition) (`xp_oblivion_bonus`) ;
- XP ×2 pour une créature apparue pendant une Résurgence (`xp_crisis_multiplier`) ;
- Péril : +12 % d'XP par point au lieu de +8 % (`data/scaling/peril.json`).
Réglages dans `data/scaling/spawn_flow.json`, à côté des montées de PV et de dégâts.

**R1-G — réserve de niveaux** (règle de §6.7), dans `FragmentManager` et `LevelUpScreen` :
- l'écran **reste ouvert** tant que la file n'est pas vide : les cartes se renouvellent sans refermer ni rejouer l'entrée, avec le nombre de choix restants affiché ;
- ouverture retenue au plus 3 s quand un autre niveau arrive dans les 3 s, ou quand plus de 60 créatures sont à l'écran (`data/ui/level_up.json` ou équivalent) ; jamais à la fin d'un boss ;
- la mise en scène de la montée de niveau (onde, colonne, plan 02 J4) ne se joue qu'une fois par ouverture, avec le nombre de niveaux gagnés.

**Vérification** : mesure de 45 min avant/après (niveaux, XP par minute, temps pour tuer) ; simulation attendue pour le bot (§6.5) ; régressions ; capture d'une cascade (réserve, compteur, enchaînement) ; relecture `godot-reviewer`.

**État à la pause du 28 septembre, 17 h (non committé, dans l'arbre de travail)** :
- R1-F codé (`SpawnManager.ComputeScaling`, `late_hp_scaling_*` dans `spawn_flow.json`), build à 0 avertissement ; mesure de 45 min lancée, à relancer si perdue (référence avant : même commande, avant R1-F) ;
- R1-A codé (`ComputeXpMultiplier`, `xp_*` dans `spawn_flow.json`, Péril à 0,12) ;
- R1-G codé (`LevelReserveConfig`, bloc `level_reserve` de `progression.json`, `FragmentManager`, `LevelUpScreen`, clé `LEVELUP_TITLE_QUEUED`).
R1-A et R1-G ne sont **ni compilés, ni testés, ni mesurés**. À reprendre : build, smoke test (uid du nouveau `.cs`), régressions, mesure, capture d'une cascade, relecture. `peril.json` et `progression.json` avaient été réindentés par ailleurs (sans changement de valeur).

**Refonte des perks, en même temps** (réponse de Raphaël) : audit des passifs et de l'ancien catalogue en cours. Le catalogue proposé (XP, chance, oubli, taille des projectiles, niveaux au-delà de 5) sera soumis à validation avant d'être codé, comme les noms et icônes d'armes au plan 17.

## 7. Dégâts à distance — 28 septembre 2026 (§5.4)

Même mesure que §6 (30 min, deux seeds). Le bot n'esquive rien. Le filtrage ne garde que les coups qui passeraient l'invulnérabilité de 0,5 s ; c'est la colonne qui compte pour comparer les rôles. Rapport = part des dégâts filtrés ÷ part de l'exposition (1 = la créature blesse en proportion de sa présence).

| Rôle | Dégâts bruts | Dégâts filtrés | Exposition (à moins de 600 px) | Morts | Rapport |
|---|---|---|---|---|---|
| Mêlée | 81,9 % | 63,5 % | 68,3 % | 94,0 % | 0,93 |
| Zones du Présage | 4,8 % | **24,3 %** | **20,1 %** | 3,5 % | 1,21 |
| Tir (Cracheur, Tisseuse, Sentinelle) | 12,8 % | 10,8 % | 11,6 % | 2,5 % | 0,94 |
| Boss, Néant | 0,4 % | 1,3 % | — | — | — |

| Créature | Dégâts filtrés | Exposition | Morts | Rapport |
|---|---|---|---|---|
| Présage | 24,3 % | 20,1 % | 3,5 % | 1,21 |
| Brute du Vide | 14,6 % | 9,4 % | 3,1 % | **1,56** |
| Hurleur | 11,4 % | 9,9 % | 0,8 % | 1,15 |
| Ombre | 10,3 % | 17,5 % | 50,9 % | 0,59 |
| Charognard | 9,7 % | 14,7 % | 27,6 % | 0,66 |
| Rôdeur | 8,1 % | 5,9 % | 2,0 % | 1,37 |
| Cracheur Pâli | 7,5 % | 5,5 % | 1,4 % | 1,37 |
| Tisseuse | 2,4 % | 2,5 % | 0,8 % | 1,00 |
| Sentinelle Hurlante | 0,8 % | 3,6 % | 0,2 % | 0,23 |

Lecture :
- **La pression « à distance » vient surtout du Présage**, pas des tireurs. Il figure dans le groupe d'exploration de **tous** les biomes (une entrée sur quatre ou cinq). En moyenne, 16 Présages rôdent à moins de 600 px du joueur, et le plafond global de deux zones actives (`OmenStrikeAbility`) est donc presque toujours atteint : une pluie continue de zones de 42 px.
- **Les créatures à distance ne meurent presque pas** : 32 % de l'exposition (Présages et tireurs), 6 % des morts. Elles restent à 250–320 px, hors de portée des armes qui visent la plus proche, et s'accumulent. Elles pèsent donc par leur nombre plus que par coup.
- **Les tireurs purs ne sont pas surdosés** (rapport 0,94). Le Cracheur est le plus lourd (1,37), la Sentinelle presque inoffensive (0,23).
- **Tous les dégâts montent de ×1,035 par minute** (×2 à 20 min), tir compris.
- Le bot sous-estime ce que fait un joueur qui esquive : les tirs et les zones sont annoncés, et s'évitent mieux qu'une foule au contact. La part ressentie en jeu dépend donc de la lisibilité (lot 8C, couloirs de visée).

Pistes chiffrées (données seulement, mesurables avec la même commande) :
1. **Présage moins présent** : une entrée sur huit au lieu d'une sur quatre ou cinq dans les groupes d'exploration, et absent avant 3 min. Attendu : exposition 20 → ~10 %, dégâts filtrés 24 → ~12 %.
2. **Croissance des dégâts à distance à part** (`ranged_damage_scaling_per_minute`) : ×1,02 par minute au lieu de ×1,035, soit ×1,49 à 20 min au lieu de ×1,99 (−25 %).
3. **Tireurs qui se rapprochent** : un tireur hors de portée des armes depuis plus de 6 s avance jusqu'à 180 px. Il devient tuable et rapporte son XP (lien avec §6).
4. Si cela ne suffit pas : **jetons d'attaque**, pas plus de quatre tireurs qui visent le joueur en même temps (en plus des deux zones de Présage). C'est le moyen le plus sûr de garder des pics lisibles, mais il demande du code.

Objectif proposé : zones et tirs sous 25 % des dégâts filtrés (35 % aujourd'hui), sans toucher à la mêlée, qui porte la difficulté voulue (build et déplacements). Révisé en §7.1 après la correction sur le Hurleur.

### 7.1 Correction et lot D1 (28 septembre, soir, demandé par Raphaël : « concrétise tes suggestions »)

**Correction** : le Hurleur est typé `ranged` (`data/enemies/hurleur.json`). Sa capacité `cry` ne remplace pas l'attaque de base, donc il **tire** aussi : un projectile toutes les 1,1 s à 300 px, **sans couloir d'annonce**. Le lot B du plan 07 a annoncé les tirs du Cracheur, de la Tisseuse et de la Sentinelle, mais pas le sien. Recomptée, la pression à distance pèse donc **46 % des dégâts filtrés** (zones du Présage 24 %, tirs 22 %) pour 42 % de l'exposition et 7 % des morts. L'intuition de Raphaël est confirmée.

**Lot D1 — pression à distance** (objectif : zones et tirs sous 35 % des dégâts filtrés, chaque tireur à un rapport ≤ 1,1, mêlée inchangée) :
1. **Poids d'apparition par créature** : `spawn_weight` (1 par défaut) et `spawn_from_minute` (0 par défaut) dans les `stats` de la fiche, pris en compte au tirage du groupe du biome. Présage : poids 0,5, pas avant 3 min.
2. **Croissance des dégâts à distance à part** : `ranged_damage_scaling_per_minute` = 1,02 dans `data/scaling/spawn_flow.json`, appliquée aux créatures `ranged` (×1,49 à 20 min au lieu de ×1,99).
3. **Hurleur** : son tir passe par la visée annoncée (`aimed_shot`), comme les autres tireurs.

Vérification : même mesure de 30 min et mêmes seeds, `tools/damage_sources.py` avant/après ; régressions des capacités ennemies ; capture du tir annoncé du Hurleur.
Les pistes 3 (tireurs qui se rapprochent) et 4 (jetons d'attaque) attendent le résultat de D1.

### 7.2 Lot D1 livré (28 septembre, soir)

**Réglages** :
- `spawn_weight` et `spawn_from_minute` dans les `stats` des fiches, pris en compte au tirage des groupes de biome (exploration et Résurgences). Présage : 0,5, pas avant 3 min. Un groupe sans aucune créature éligible avertit dans la console.
- `ranged_damage_growth_share` = 0,57 dans `data/scaling/spawn_flow.json` : les créatures `ranged` (Présage, Cracheur, Tisseuse, Sentinelle, Hurleur) montent de ×1,02 par minute au lieu de ×1,035. Un mutateur qui change `damage_scaling_per_minute` s'applique toujours à elles.
- Hurleur : tir annoncé (`aimed_shot`, couloir de 0,4 s). Relecture `godot-reviewer` : le cri et la visée pouvaient s'annoncer en même temps. Désormais, une créature ne lance pas une capacité pendant qu'une autre est en cours (`IEnemyAbility.IsActive`).

**Mesure** (même commande de 30 min, mêmes seeds, `tools/damage_sources.py`) :

| | Avant | Après |
|---|---|---|
| Zones du Présage, part des dégâts filtrés | 24,3 % | 16,1 % |
| Tirs (Cracheur, Tisseuse, Sentinelle, Hurleur) | 22,3 % | 18,1 % |
| **Zones et tirs** | **46,6 %** | **34,2 %** (objectif < 35 %) |
| Exposition au Présage | 20,1 % | 11,1 % |
| Dégâts filtrés totaux (deux seeds) | 24 977 / 27 898 | 19 916 / 23 854 (−20 % / −15 %) |
| Morts | 4 568 / 5 727 | 6 029 / 6 729 |
| XP ramassée | 33 032 / 49 816 | 48 416 / 56 610 |
| Niveau à 30 min | 34 / 40 | 40 / 42 |

- La mêlée garde sa part (63,5 %) : la difficulté voulue, celle du build et des déplacements, n'est pas touchée.
- Moins de créatures à distance hors de portée, c'est plus de morts et plus d'XP : effet de bord favorable pour le plan 20.
- **Reste au-dessus de sa présence** : le Cracheur Pâli (rapport 1,66) et le Présage (1,46), moins nombreux mais aussi lourds par créature. Si la recette le confirme, suite D2 : la piste 3 (tireurs qui se rapprochent) pour le Cracheur, ou un délai de zone du Présage porté de 1,0 à 1,2 s.

**Vérifications** : build à 0 avertissement ; régressions des capacités ennemies à 0 échec (dont les quatre tests du cri du Hurleur) ; capture du tir annoncé du Hurleur regardée (couloir vert, puis tir). Réserve : la mesure « après » a pu inclure le travail non committé de l'autre agent sur les créatures lointaines (`Enemy.cs`), qui ne touche pas aux coups portés au joueur.

## 8. Lots proposés (un à la fois, à valider)

| Lot | Contenu | Nature | Vérification |
|---|---|---|---|
| **R1-0** | Courbe d'XP en JSON (`data/scaling/progression.json`), plafonnée à 3 000 XP par niveau | **Livré** (§6.8) | Formule identique jusqu'au niveau 41 |
| **D1** | Pression à distance (§7.1) : poids d'apparition du Présage, croissance des dégâts à distance à part, tir annoncé du Hurleur | **Livré** (§7.2) | Zones et tirs 46,6 → 34,2 % |
| **R1-A** | XP selon le temps, l'oubli, les Résurgences, Péril à 12 % | Données et un calcul dans `Enemy`/`XpOrb` | Mesure 30 min ; simulation pour le bot : 27 → 36 niveaux à 20 min, 37 → 54 à 30 min, quasi rien avant 5 min (8 → 9) |
| **R1-G** | Réserve de niveaux automatique (règle de §6.7) : écran retenu 3 s quand les niveaux affluent ou que la foule dépasse 60 créatures, choix enchaînés | Interface (plan 02 J4, plan 04) | Capture d'une cascade de 15 niveaux ; un seul niveau ouvre toujours l'écran tout de suite |
| **R1-B** | Reflux à l'accalmie, fusion des orbes | Code | Mesure (orbes au sol, XP par Résurgence), capture de la cascade |
| **R1-C** | Boss intermédiaire, **option A retenue** (sceaux des Mémoriaux) | Conception détaillée, puis prototype | Recette par Raphaël ; niveaux gagnés par rang |
| **R1-D** | Indicible en niveaux (+8) | Données | Mesure |
| **R1-H** | Niveaux de surplus au-delà du build complet (≈ 219 montées) : effet à choisir parmi §6.7 (A–E), coût réduit | Données et code | Simulation, puis run dev poussée au-delà de 219 |
| **R1-F** | Montée plus forte des PV des créatures après 22 min (×1,07 par minute au lieu de ×1,05, §6.8), l'endgame n'étant jamais atteint | Données et un calcul dans `SpawnManager` | Mesure 45 min : temps pour tuer du bot autour de 0,27 s après 30 min |
| **R1-T** | Temps moyen pour tuer une créature, par palier, dans `RunObservation` | **Livré** (§6.8) | Relevé sur 45 min |
| **R1-E** | XP, chance et oubli par des **perks** (les quatre passifs sous les armes, à refondre comme les armes) et des **objets dédiés** (plan 13, non branché) ; pas de tomes | Contenu (plans 05, 13, 17) | Simulation, puis recette |
| **P** | Densité : ~500 créatures en très late game et dans les Résurgences les plus dures, plus si le rendu le permet | Optimisation (**autre agent**) ; rendu à juger en capture | Banc de foule, `/bench`, captures |

Ordre recommandé : D1 (fait, voir §7.2), puis R1-0 et R1-T (données et mesure, sans risque), puis R1-F calé sur la mesure. Ensuite R1-A et R1-G ensemble : avec plus d'XP, les écrans de choix se multiplient. Puis R1-B, la conception de R1-C, R1-H et, après la refonte des perks, R1-E.

## 9. Questions ouvertes

Réponses de Raphaël du 28 septembre (soir) : excellente run à 300–400 niveaux en 45 min, voire 1 000 avec des armes plafonnées (§6.6) ; boss intermédiaire en **option A** ; axe de build de l'oubli **d'accord** ; cascades **enchaînées**, avec une réserve qui empile les niveaux ; dégâts à distance « à toi de voir » (lot D1 fait) ; densité visée d'environ 500 en très late game et dans les Résurgences les plus dures, l'optimisation étant confiée à un autre agent.

Réponses du 28 septembre (nuit) consignées en §6.7 : paliers validés, réserve automatique, montée plus forte en endgame, XP et chance par perks et objets.

Réponses du 28 septembre (fin de nuit) : niveaux de surplus **A et D**, **E** éventuellement, « mais ça ne doit pas devenir overkill » ; refonte des perks **en même temps** que les leviers d'XP (R1-A avec R1-E) ; R1-F **d'accord** (PV ×1,07 par minute après 22 min, mesure de 45 min avant/après). Mise en pause à la demande de Raphaël : rien n'est engagé.
