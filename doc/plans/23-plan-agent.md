# Plan 23 — Des gains qui se sentent : plan d'exécution pour un agent

30 septembre 2026 · Demandé par Raphaël ([DECISIONS §36 et §37](DECISIONS.md)) pour un agent qui travaille seul, en cloud. Ce plan n'ajoute aucune règle de jeu. Les règles sont dans la [référence](21-systeme-de-jeu.md), déjà mise à jour de ces décisions ; ici, on dit **dans quel ordre les appliquer et comment vérifier**.

## 0. Ce que Raphaël a dit, en bref

- Le level-up n'est pas attrayant : les gains sont trop petits (+0,06 PV/s par niveau de Bobine). Il veut des gains francs, du type « +1 projectile », « +1 perforation ». Des totaux énormes sont acceptés ; les ennemis monteront en conséquence.
- **Copies d'attaque supprimées :** l'objet donne des projectiles supplémentaires, pleins.
- Stats entières **fractionnaires**, de +0,5 (commune) à environ +3 (légendaire) par amélioration.
- **Objets à 30 niveaux** (ni 10 ni 50).
- **Cartes de niveau à la Megabonk, validées :** une ligne de gain ; inventaire et stats à côté.
- **Plus de bouclier de départ**, invulnérabilité réduite.
- Coffres ennuyeux : **proposition non encore confirmée** (voir R8).
- Il manque des objets, et la carte fait vide (plan 22, lié).

## 1. À lire avant de coder, dans cet ordre

1. `AGENTS.md` et `CLAUDE.md` : règles du projet, commandes, pièges.
2. [TABLEAU-DE-BORD.md](TABLEAU-DE-BORD.md) : où en est chaque chantier.
3. [21-systeme-de-jeu.md](21-systeme-de-jeu.md) : **la référence**. Elle fait foi ; §2, §3, §4, §6, §11, §12 sont à jour des décisions §36–§37.
4. [../PRINCIPES-BUILD.md](../PRINCIPES-BUILD.md) : les douze principes. Toute valeur ou forme proposée se justifie par eux.
5. [DECISIONS.md](DECISIONS.md) §28 à §37 : les mots exacts de Raphaël.
6. [21-inventaire.md](21-inventaire.md) : ce qui est en jeu aujourd'hui, chiffres compris.
7. [21-historique.md](21-historique.md) §20 à §29 et [22-carte-a-explorer.md](22-carte-a-explorer.md) §12 à §14 : comptes rendus des lots précédents (objets, déclencheurs, ascensions, mesure de la carte, petits lieux).
8. `git log --oneline -20`.

## 2. Environnement nécessaire

- **Godot 4.7.2 .NET** (le binaire `godot-mono`, ou la variable `GODOT_BIN`), **.NET SDK 10**, `ripgrep` (`rg`, utilisé par les scripts de banc), `python3` avec Pillow (planches de captures).
- **Captures** (`tools/capture_run.sh`, `tools/capture_hub.sh`) : elles ouvrent une fenêtre. En cloud sans écran, les lancer sous `xvfb-run -a` avec un contexte OpenGL logiciel (Mesa). Si aucune capture n'est possible, le dire dans le compte rendu ; ne jamais écrire qu'un rendu est vérifié sans avoir ouvert et regardé l'image.
- En headless, ces avertissements sont normaux : DLL Steam absente, « MixRate mismatch », fuites ObjectDB à la fermeture.

## 3. Règles de travail

- **Un lot à la fois.** Découpage écrit ici (§5, « Compte rendu » du lot) avant de coder. Puis code, vérification, documentation, commit.
- **Avant chaque commit** qui touche `scripts/`, `scenes/` ou `data/` : relecture par le sous-agent `godot-reviewer`, puis correction de ce qu'il trouve. S'il n'est pas disponible, faire soi-même une relecture du diff selon `.claude/agents/godot-reviewer.md`.
- **Vérifier par le bon moyen :**
  - `dotnet build` : zéro avertissement ;
  - bancs : `tools/test_objects.sh`, `test_weapons.sh`, `test_perk_effects.sh`, `test_perk_acquisition.sh`, `test_perk_contracts.sh`, `test_enemy_abilities.sh`, `test_movement.sh`, `test_small_places.sh` ;
  - `tools/smoke_test.sh` ;
  - captures regardées (ouvrir les PNG) ;
  - `tools/measure_run.sh` pour tout ce qui se mesure (densité, puissance, Essence).
- **Lire la ligne `RESULT failures=0`** de chaque banc. Un `&&` derrière un `grep` ne bloque pas sur un banc en échec.
- **À chaque lot clos :**
  - la référence §12 (état d'application) ;
  - le [tableau de bord](TABLEAU-DE-BORD.md) ;
  - la roadmap V2 (`doc/VESTIGES-STRATEGIE-V2.md` §25), où l'on coche seulement ce qui est implémenté et vérifié ;
  - le compte rendu du lot ici, au §6.
- **Commits** en français (`feat:`, `fix:`, `docs:`…), **sans** trailer `Co-Authored-By`. Ne pas pousser sans que Raphaël le demande.
- **Megabonk** est la référence de qualité, mais on ne le clone pas : reprendre ses ressorts, jamais ses objets ni ses lieux.
- **Ne pas faire, sans accord explicite de Raphaël :**
  - les voies d'ascension des 20 autres armes (proposées, `21-historique.md` §28) ;
  - l'Atelier et la Trempe (plan 22 C2) ;
  - le Reliquaire (plan 22 C3) ;
  - les plans 13 et 14 ;
  - la forme des coffres (R8), tant qu'elle n'est pas confirmée.

## 4. Pièges rencontrés dans les lots précédents

- **GroupCache** garde la liste des ennemis pour toute la frame. Un banc qui fait tout dans `_Ready` doit créer ses ennemis avant le premier appel, ou forcer la relecture (`_enemiesFrame`).
- Dans un banc, les ennemis libérés par `QueueFree` restent dans l'arbre jusqu'à la fin de la frame. Placer les scènes de test loin (x ≥ 3 000) des ennemis des contrôles précédents.
- **Captures :** un maintien d'interaction se compte en temps de jeu. À plus de 100 images par seconde, 90 frames ne font pas une seconde : attendre au moins 180 frames. Mettre `AIInputOverride` à zéro, sinon le maintien s'annule.
- **Mesures :** ne pas recompiler pendant `measure_run.sh`. Les seeds suivantes chargeraient la nouvelle DLL et la mesure mélangerait deux versions. D'un passage à l'autre, le trajet du bot varie : comparer des moyennes sur 5 seeds, pas des runs isolées.
- `pkill -f <motif>` tue aussi le shell qui contient le motif.
- Le banc d'effets a fini une fois sur une exception à la fermeture, après `RESULT failures=0`, sans se reproduire. Relancer avant de conclure.
- `Player.cs`, `Enemy.cs`, `VfxFactory.cs` : ne pas les faire grossir. Le code d'objets vit dans `Player.Objects.cs`, `ObjectMilestones`, `ObjectTriggers` ; celui des ascensions dans `Player.Ascensions.cs`.

## 5. Les lots, dans l'ordre

### R0 : mesure de référence de la puissance

Avant de toucher aux valeurs, mesurer ce que vaut le joueur face aux ennemis aujourd'hui.

- `MEASURE_JOBS=2 MEASURE_EXTRA_ARGS="--nomad" tools/measure_run.sh <dossier> 900` sur les 5 seeds par défaut, puis `--nomad --visit`.
- Relever, par tranche de 3 min :
  - éliminations ;
  - dégâts reçus (`hit_damage`) ;
  - temps pour tuer : PV des créatures apparues divisés par les dégâts infligés, colonnes `spawned_hp` et `damage_dealt` du CSV ;
  - niveaux atteints ;
  - Essence gagnée.
- Consigner le tableau au §6. C'est la référence des lots R3 à R5.

### R0 : mesure de référence de la puissance

Mesuré sur `a9203af` (avant R1), dans une copie séparée du dépôt pour pouvoir compiler à côté. `tools/measure_run.sh`, 900 s de jeu, seeds 221092026, 1002, 7, 42, 20260926. Le bot prend la première carte de chaque offre ; il est invincible et compte chaque coup reçu, sans l'invulnérabilité après un coup. Le CSV de densité gagne une colonne `essence_gained` ; le dépouillement par tranche est dans `tools/summarize_power.py` (réutilisé pour R5).

**Nomade** (`--nomad`), moyenne des 5 seeds :

| Tranche | Éliminations | Dégâts reçus | PV moyen d'une créature | Dégâts infligés /s | Temps pour tuer (s) | PV apparus / dégâts | Niveau en fin de tranche | Essence gagnée |
|---|---|---|---|---|---|---|---|---|
| 0–3 min | 56 | 4 657 | 39,6 | 15,2 | 3,05 | 9,12 | 4,2 | 71 |
| 3–6 min | 245 | 14 781 | 42,9 | 69,2 | 0,76 | 4,01 | 12,0 | 308 |
| 6–9 min | 482 | 8 822 | 50,8 | 162,3 | 0,32 | 2,62 | 18,8 | 572 |
| 9–12 min | 542 | 10 987 | 58,2 | 181,6 | 0,33 | 3,33 | 25,0 | 745 |
| 12–15 min | 569 | 16 403 | 68,4 | 253,0 | 0,30 | 2,80 | 29,8 | 697 |

**Nomade qui visite les lieux** (`--nomad --visit`), moyenne des 5 seeds (deux passées avant un redémarrage du conteneur, trois après, même build) :

| Tranche | Éliminations | Dégâts reçus | PV moyen d'une créature | Dégâts infligés /s | Temps pour tuer (s) | PV apparus / dégâts | Niveau en fin de tranche | Essence gagnée |
|---|---|---|---|---|---|---|---|---|
| 0–3 min | 121 | 4 960 | 42,6 | 30,3 | 1,93 | 5,45 | 7,2 | 197 |
| 3–6 min | 471 | 6 945 | 44,1 | 123,7 | 0,37 | 2,51 | 17,4 | 587 |
| 6–9 min | 505 | 4 854 | 51,9 | 165,0 | 0,32 | 2,98 | 23,4 | 619 |
| 9–12 min | 714 | 7 419 | 55,2 | 229,3 | 0,26 | 2,56 | 30,4 | 1 051 |
| 12–15 min | 725 | 14 110 | 67,3 | 296,5 | 0,23 | 2,29 | 35,8 | 941 |

**Lecture :**
- Le temps pour tuer tombe de 2 à 3 s à 0,3 s dès 6 min, puis reste à plat : les dégâts du joueur montent aussi vite que les PV des créatures (×1,7 de PV moyen entre la première et la dernière tranche, ×10 à ×17 de dégâts infligés par seconde).
- Les dégâts reçus creusent entre 6 et 9 min, puis remontent en fin de run (foule plus dense, créatures plus dures).
- Niveau 30 environ à 15 min en nomade, 36 en visitant les lieux ; Essence gagnée : 140 à 190 par minute, jamais dépensée par le bot nomade.
- Cible de R5 : retrouver ces courbes de temps pour tuer et de dégâts reçus à ±20 %, pour le même bot.

### R1 : défense (petit lot, rapide)

- `data/characters/characters.json` : `shield` à 0 pour les trois personnages.
- `data/characters/defense.json` : `hurt_invulnerability_seconds` de 0,5 à 0,25.
- Vérifier que le HUD ne montre pas de barre de bouclier vide quand le maximum est 0 (capture `--capture-pause` et une capture de run), et que l'Écusson de pompier fait bien apparaître le bouclier.
- Bancs : déplacements, contrats (blessures), objets.

### R2 : cartes de niveau à la Megabonk (validé)

Référence §11. Voir la capture de Megabonk décrite en DECISIONS §36 : carte = icône, rareté en petit, nom, niveau à droite, une ligne « Damage: 10 → 12.5 » ; à gauche l'inventaire, à droite les stats.

- **Carte :**
  - rareté en petit en haut ;
  - nom ;
  - niveau à droite (« Niv 3 → 4 », ou « NOUVEAU ») ;
  - une ligne de gain en valeur (« Cadence 1,2 → 1,4 /s »), deux au plus. Pour une rare ou une légendaire à 2 ou 3 stats, la deuxième ligne regroupe (« et Portée +9 % ») ;
  - retirer le préfixe de propriété, la ligne « Pour : … » et le texte de palier. Un palier que la carte fait atteindre devient un badge doré « Palier ! » ;
  - carte d'ascension : nom de la voie et sa règle, en une ligne.
- **Panneaux pendant le choix :**
  - à gauche, l'inventaire : 4 armes avec niveau et voie, 6 objets avec niveau, 4 Réminiscences ;
  - à droite, les stats du joueur (celles de la fiche de la pause).

  Extraire les constructeurs de lignes de `PauseMenu` dans un composant partagé, pour ne pas dupliquer.
- Garder la navigation manette et clavier (focus sur les actions : capture `levelup-focus-actions`).
- **Vérifier :** `--capture-levelup` à 1080p ; les cinq raretés et la carte d'ascension, regardées. Chaque carte fait au plus trois lignes de texte sous le nom.

### R3 : objets à 30 niveaux, gains francs

Référence §4 (tableau des pas).

- **Modèle :** un objet monte de 1 à 30. Chaque carte donne un niveau et ajoute un gain égal au pas de l'effet × multiplicateur de rareté (commune 1, inhabituelle 1,5, rare 2, épique 2,5, légendaire 3). La valeur de l'objet est la somme de ses gains.
  - `ActivePassiveSouvenir` garde la valeur cumulée de chaque effet, en plus du niveau.
  - La formule par niveau (`base`, `per_level`, `step`, `step_levels`) et `object_levels` disparaissent au profit d'un `step` par effet.
  - Le multiplicateur de rareté existe déjà (`passive_gain` dans `upgrade_rarities.json`) : vérifier qui d'autre le lit (bénédictions de Mémorial).
- **Copies supprimées :**
  - retirer `attack_copies`, `copy_damage`, `CopiesFor`, la teinte des projectiles copiés et `copies_multiplier` des ascensions ;
  - Volée devient « projectiles × 2 », Transpercer « un seul projectile, et les projectiles en plus ne s'appliquent pas » ;
  - Papier carbone donne `projectile_count` en fractions (§3 de la référence) à toutes les armes à projectiles et à frappes de mêlée.
- **Reflet brisé réactivé :** +0,5 perforation aux tirs.
- **Paliers au niveau 15.** Coder les deux nouveaux :
  - Papier carbone : les projectiles en plus visent chacun une cible différente ;
  - Reflet brisé : +10 % de dégâts par ennemi traversé.
- **Objets de déclencheur :** recalculer chaque pas pour 30 niveaux. Au niveau 30, l'objet vaut au moins sa valeur actuelle au niveau 50, et une carte commune se sent. Écrire le nouveau tableau dans la référence §4 avant de coder.
- **Coffres :** en attendant R8, un « niveau d'objet » de coffre = une carte commune.
- **Vérifier :**
  - `tools/test_objects.sh`, à réécrire autour du nouveau modèle : somme des gains, rareté, 30 niveaux, paliers à 15, projectiles fractionnaires ;
  - captures de level-up et de pause ;
  - une capture de combat avec Papier carbone et Reflet brisé montés.

### R4 : armes, stats entières fractionnaires et pas relevés

Référence §3.

- **Pas relevés de moitié** dans `data/weapons/weapon_upgrades.json` : dégâts 0,18, cadence 0,12, portée 0,09, zone 0,12, recul 0,20, vitesses 0,15.
- **Stats entières** (projectile, perforation, saut de chaîne, orbe) :
  - tirables dès la commune, avec le gain de la référence (0,5 / 0,75 / 1 / 2 / 3) ;
  - elles entrent dans `growth` des armes qui les ont (liste `milestones` actuelle) avec un poids de départ de 0,5 ;
  - la partie entière s'applique toujours ; la partie décimale est une chance par attaque d'en avoir une de plus ;
  - les orbes de la Boîte à musique ne comptent que la partie entière (une orbe ne peut pas apparaître une attaque sur deux).
- **Cartes :** « Projectiles 1 → 1,5 ».
- **Vérifier :**
  - `tools/test_weapons.sh` : fraction tirée sur 1 000 attaques, sans perte ni doublon d'orbe ;
  - `--capture-weapons` sur l'Arc, le Lance-billes et le Trousseau, avec des niveaux montés.

### R5 : la difficulté suit

Raphaël accepte des totaux énormes à condition que les ennemis montent en conséquence (DECISIONS §37).

- Après R3 et R4, refaire la mesure de R0, mêmes seeds, même durée.
- Si le temps pour tuer s'effondre ou si les dégâts reçus tombent à presque rien en fin de run, relever la montée des ennemis. Toucher seulement aux données : `data/scaling/`, le flux d'apparition du plan 20 et les multiplicateurs de PV et de dégâts par palier de temps.
- Cible : retrouver à ±20 % la courbe de temps pour tuer et de dégâts reçus de R0 pour un bot qui prend la première carte. Le joueur qui construit bien doit, lui, prendre de l'avance.
- Consigner avant/après au §6. Aucune conclusion sur les FPS si la machine est chargée.

### R6 : objets de déclencheur restants (G2c)

Référence §4, au nouveau modèle à 30 niveaux :
- Loupe de philatéliste ;
- Stylo à quatre couleurs ;
- Tabouret de camping ;
- Chewing-gum ;
- Gilet réfléchissant ;
- Thermos ;
- Médaille cabossée ;
- Porte-monnaie usé.

Les trois objets « monde » (Presse-papier, Calendrier, Médaillon) attendent le Reliquaire.

- Réutiliser `ObjectTriggers` (coefficient d'arme, pas de récursion, chance plafonnée dont l'excédent renforce l'effet) et le statut Fragilité, déjà en jeu.
- Chaque déclencheur a un retour visuel propre (référence §11).
- Bancs dans `tools/test_objects.sh`, captures en combat.

### R7 : la carte fait moins vide (plan 22)

Direction validée par Raphaël, lots de plan 22 sans question ouverte :

- **C4 : les six autres petits lieux** du plan 22 §3 : Boîte aux lettres, Wagonnet, Voiture abandonnée, Cabine téléphonique, Abribus, Table de pique-nique.
  - Même socle que C1 : `SmallPlaceDirector`, `data/world/small_places.json`.
  - Cible : un petit lieu en vue toutes les 20 à 30 s de marche **à lui seul**. Aujourd'hui c'est environ toutes les 80 s ; toutes catégories confondues, 24 s.
  - Placer des décors là où il en manque, par exemple des boîtes aux lettres le long des routes.
- **C6 : carte agrandie en hauteur (12 800 px) et minimap** (lieux découverts, front de l'Effacement).
  - Mesurer le coût de génération et le nombre de décors avant et après.
  - Garder la règle « aucune boucle par frame sur les décors ».
- **Vérifier :**
  - `tools/measure_run.sh` avant et après, avec les mêmes seeds que le compte rendu C1 (plan 22 §14) ;
  - `--capture-places`, `--capture-map`, captures de la minimap.

### R8 : coffres, l'Essence reste, un bonus de stat en plus (révisé le 1er octobre)

Raphaël n'a pas retenu le choix parmi trois du §36 ([DECISIONS §38](DECISIONS.md)) : « Un coffre peut donner de l'essence ca me va. il doit aussi donner un bonus d'une stat au hasard. »

Découpage :
- le butin tiré reste tel quel (Essence, XP, niveau d'objet, arme) ;
- **chaque coffre ouvert donne en plus un bonus d'une stat du joueur tirée au hasard**, pour le reste de la run ;
- liste des stats et taille du bonus dans `data/chests/chest_stat_bonus.json` : les stats des objets proposés, un coffre commun valant un niveau d'objet commun, multiplié selon la rareté du coffre ;
- pas de +dégâts universel, retiré des objets pour la même raison (plan 21 §4) ; ni projectiles ni perforation, trop forts pour un tirage ;
- la ligne s'affiche à l'écran de butin avec les autres (« Cadence +16 % ») ;
- vérifier : banc d'objets ou de coffres (le bonus s'applique, une fois, à la bonne valeur), capture de l'écran de butin, mesure `--visit`.

### R9 : Porte-monnaie et Repères (laissés au choix de l'agent, DECISIONS §38)

- **Porte-monnaie usé :** l'Essence rendue est une remise, pas de l'Essence gagnée. Elle ne compte plus pour la quête « Accumuler de l'Essence ». L'Essence de la Photo de classe, gagnée, compte toujours.
- **Repères** (plan 22 §3 B) : le premier usage de chaque type de lieu dans la run donne un peu de Chance. Douze types : les neuf petits lieux, coffre, Mémorial éveillé, Faille. Valeur dans `data/world/waymarks.json` ; un texte au-dessus du joueur (« Repère : Puits · Chance +2 % »).
- Vérifier : banc des petits lieux (Repère donné une fois par type), banc d'objets (quête et Porte-monnaie).

## 6. Comptes rendus

À remplir lot par lot : ce qui a été fait, les chiffres avant/après, les vérifications avec leur résultat réel, ce qui reste. Le tableau de bord et la référence §12 se mettent à jour en même temps.

### Environnement de l'agent (30 septembre)

Conteneur cloud sans Godot ni .NET au départ : SDK .NET 10.0.401 installé par `dotnet-install.sh`, Godot 4.7.2 .NET officiel décompressé et lié en `godot-mono`, Pillow installé. Captures sous `xvfb-run -a -s "-screen 0 1920x1080x24"` avec le rendu logiciel de Mesa : elles fonctionnent (3 à 8 images par seconde, sans effet sur le rendu capturé).

**Langue :** sans `LANG`, Godot démarre en anglais et trois contrôles de `test_objects.sh` échouent déjà sur `a9203af` (ils attendent les libellés français). Tous les bancs se lancent donc avec `LANG=fr_FR.UTF-8`.

### R1 : défense

**Fait :**
- `shield` à 0 pour le Traqueur, le Vagabond et la Forgeuse (`data/characters/characters.json`) ; le bouclier ne vient plus que de l'Écusson de pompier.
- Invulnérabilité après un coup de 0,5 à 0,25 s (`data/characters/defense.json`, et la valeur par défaut de `DefenseConfig`).
- Pause : la ligne « Bouclier » n'apparaît que si le maximum dépasse 0. Le HUD et la jauge sous le joueur masquaient déjà le liseré à 0.
- Banc de contrats : le coup « encaissé par le bouclier » donne d'abord un bouclier d'objet (il supposait celui de départ).
- Capture `--capture-pause` : HUD sans bouclier au départ (`hud-no-shield`), puis l'Écusson de pompier parmi six objets (`hud-shield`, `pause`).

**Vérifié :**
- `dotnet build` : 0 avertissement.
- `test_movement.sh` : `RESULT failures=0` ; `test_perk_contracts.sh` : `RESULT failures=0` ; `test_objects.sh` : `RESULT failures=0`.
- Captures regardées : en run, la barre de PV n'a plus de liseré bleu ; avec l'Écusson de pompier, le liseré revient et la fiche affiche « Bouclier 2 / 2 ».

**Corrigé en passant :** le HUD affichait 100 / 100 pour le Traqueur (70 PV) et la Forgeuse (120 PV) jusqu'au premier coup. Il est relié au joueur avant l'application du personnage, qui n'envoyait pas ses PV ; `InitializeCharacter` les envoie maintenant.

**Reste :** la valeur de 0,25 s est à confirmer en jeu par Raphaël (tableau de bord §2).

### R2 : cartes de niveau à la Megabonk

**Fait :**
- **Carte** (`LevelUpScreen`, `UpgradeText`) :
  - bandeau : rareté en petit (symbole et nom), sinon « ARME » ou « OBJET » ; à droite « Niv 3 → 4 », « NOUVEAU », ou « DÉFINITIVE » pour une voie d'ascension ;
  - nom, suivi d'un badge doré « Palier ! » si la carte fait atteindre un palier codé ;
  - une ligne de gain en valeur (« Cadence 0,8 /s → 0,9 /s », « Durée +1,5 % »), deux au plus. La deuxième regroupe les autres stats : « et Portée +12 %, Dégâts +24 %, Perçage +1 ». Pour un objet à plusieurs effets, la ligne en valeur montre le premier effet qui bouge ;
  - carte d'ascension : « Arme : Voie », puis la règle en une ligne ;
  - retirés : préfixe de propriété (`StatCatalog.NameWithProperty` supprimé), ligne « Pour : … » ou « Aucune de tes armes », texte de palier, ligne « Voie définitive ». Les clés de traduction devenues inutiles sont retirées.
- **Panneaux pendant le choix :**
  - à gauche, l'inventaire : armes avec niveau et voie, objets avec niveau, Réminiscences, chaque section avec ses emplacements (« Armes 2/4 ») ;
  - à droite, les stats de la fiche de la pause, en plus petit, sans le détail des Oublis ;
  - la règle des armes concernées reste : au focus d'une carte d'objet, les armes qu'il renforce s'allument en doré dans l'inventaire, les autres s'éteignent.
- **Composant partagé** `UI/PlayerSheet` : fiche des stats, lignes, icônes et inventaire compact, utilisé par la pause et par le level-up. `PauseMenu` n'a plus sa propre copie des lignes de stats.
- **Navigation** inchangée (haut/bas entre cartes et actions, validation) ; l'animation d'entrée porte sur les trois colonnes.
- La Faille, qui reprend `UpgradeText.Describe`, hérite des cartes courtes.

**Vérifié :**
- `dotnet build` : 0 avertissement.
- `test_objects.sh` : `RESULT failures=0`. Les contrôles de cartes sont réécrits : un palier atteint est un badge et jamais une ligne ; aucune carte d'objet ne dépasse deux lignes ; une amélioration légendaire d'arme à quatre gains tient en deux lignes ; les armes concernées sont calculées pour l'inventaire, pas écrites sur la carte.
- Captures `--capture-levelup` à 1080p, regardées : les cinq raretés, la carte d'ascension, le focus sur les actions et le focus sur une carte d'objet (Arc et Cloueuse allumés par le Papier carbone). Chaque carte a au plus trois lignes sous le bandeau : nom, puis une ou deux lignes de gain.
- Capture `--capture-pause` : fiche et équipement identiques à avant l'extraction.

- Relecture `godot-reviewer` : rien de bloquant. Suites données : les colonnes latérales défilent au stick droit ou à Page haut/bas, comme la pause (un inventaire plein peut dépasser la hauteur) ; `PauseMenu` appelle `PlayerSheet` sans alias. Les clés `PROPERTY_*` n'ont plus de lecteur ; gardées pour l'affichage des affinités des personnages (G4).

**Reste :** la ligne « Dégâts des copies » disparaît avec les copies au lot R3. La carte d'ascension dit « DÉFINITIVE » au lieu de « l'autre voie est oubliée » : à confirmer par Raphaël.

### R3 : objets à 30 niveaux, gains francs

**Fait :**
- **Modèle** (`ActivePassiveSouvenir`, `PassiveEffectData`) : un objet monte de 1 à 30. Chaque carte donne un niveau et ajoute à chaque effet son pas multiplié par le gain de rareté (`passive_gain` : 1 / 1,5 / 2 / 2,5 / 3). L'objet neuf vaut un pas. La valeur d'un effet est la somme de ses gains. La formule par niveau (`base`, `per_level`, `step_levels`) et `object_levels` sont supprimés ; `passive_gain` sert aussi, comme avant, aux bénédictions de Mémorial (seul autre lecteur).
- **Données** (`passive_souvenirs.json`) : `max_level` 30, un `step` par effet, tous les paliers au niveau 15. Les pas suivent la référence §4 : ceux des objets de propriété y étaient déjà écrits ; ceux des objets de déclencheur sont recalculés et consignés dans la référence avant le code (au niveau 30 en communes, au moins la valeur de l'ancien niveau 50).
- **Copies supprimées :** `attack_copies`, `copy_damage`, `CopiesFor`, `StrikeMultiplierSum`, la teinte des projectiles copiés et `copies_multiplier`. Le Papier carbone donne `projectile_bonus` (+0,5 par carte commune), tiré à chaque attaque par `FractionalCount` (partie entière toujours, décimale en chance ; partagé avec R4). Les tirs en plus partent en éventail vers la cible de l'arme ; une arme de mêlée les reçoit en frappes pleines. Volée double les projectiles en plus (`bonus_projectile_multiplier` 2), Transpercer les refuse (0).
- **Reflet brisé réactivé :** +0,5 perforation par carte commune, fractionnaire aussi. 23 objets proposés.
- **Paliers nouveaux :** Papier carbone, les projectiles en plus visent chacun leur cible (`spread_targets`) ; Reflet brisé, +10 % des dégâts de départ par ennemi traversé (`pierce_damage_ramp`, dans `Projectile`, remis à zéro au lancement).
- **Coffres :** un « niveau d'objet » de coffre = une carte commune, borné au niveau 30.
- **Cartes et pause :** « Projectiles en plus +7 → +8,25 » ; niveau « Niv 14 → 15 » (une carte = un niveau) ; fiche : « Projectiles en plus », « Perforation » en fraction.

**Vérifié :**
- `dotnet build` : 0 avertissement. `Player.cs` passe de 2 078 à 2 061 lignes.
- Bancs, tous à `RESULT failures=0` : `test_objects.sh` (réécrit : 30 niveaux, somme des gains, rareté × 1 à × 3, paliers à 15, 23 objets, tirage fractionnaire sur 1 000 attaques entre 2 et 3 projectiles en plus pour 2,5, dégâts d'un tir qui perfore 10 → 11 → 12), `test_weapons.sh`, `test_perk_effects.sh`, `test_perk_acquisition.sh`, `test_perk_contracts.sh`, `test_enemy_abilities.sh`, `test_movement.sh`, `test_small_places.sh`.
- Captures regardées : `--capture-weapons` avec le Papier carbone au niveau 10 (Arc : six flèches groupées sur la cible ; Faucille : frappes cumulées, 72 au lieu de 12), puis Papier carbone et Reflet brisé au niveau 15 (flèches vers des cibles distinctes, chiffres de dégâts qui montent le long d'une perforation) ; `--capture-levelup` (badge « Palier ! » sur Niv 14 → 15) ; `--capture-pause`.

- Relecture `godot-reviewer` : aucun bug. Corrigé au passage : un objet à plus de 8 effets est refusé au chargement (tampon sans allocation du joueur), deux bancs datés des 50 niveaux.

**Reste :** les chiffres sont des valeurs de départ ; R5 dira si la difficulté suit. Un objet neuf vaut un pas quelle que soit la carte (les cartes « nouvel objet » n'ont pas de rareté), comme le veut la référence.

### R4 : armes, stats entières fractionnaires et pas relevés

**Fait :**
- **Pas relevés de moitié** (`weapon_upgrades.json`) : dégâts 0,18, cadence 0,12, portée 0,09, arc et cône 0,12, recul 0,20, vitesses de projectile et d'orbite 0,15 ; portée de chaîne et guidage montés dans la même proportion (0,12 et 0,15).
- **Stats entières** (projectile, perforation, saut de chaîne, orbe) : marquées `integer` ; elles entrent dans `growth` des armes qui les avaient en `milestones`, au poids 0,5 (la liste `milestones` disparaît des armes et du chargeur). Tirées dès la commune, elles gagnent `integer_gain` de la rareté : 0,5 / 0,75 / 1 / 2 / 3. Les anciens paliers Épique et Légendaire (`milestones`, `fallback_stats`) sont retirés des raretés, qui gardent le nombre de stats de la référence §3.
- **En combat :** projectiles, sauts et perforation (arme et Reflet brisé ensemble) tirent leur décimale à chaque attaque (`FractionalCount`) ; les orbes de la Boîte à musique ne comptent que la partie entière. La Lentille de phare, à perforation illimitée, n'a pas de perforation à monter.
- **Cartes et pause :** « Perforation 3 → 3,5 », « Projectiles 1 → 1,5 ».
- **Galerie d'armes :** option `--integer-gains N` (N fois +0,5 sur chaque stat entière de l'arme).

**Vérifié :**
- `dotnet build` : 0 avertissement.
- `test_weapons.sh` : `RESULT failures=0`, dont : Légendaire à trois stats au double du pas ou +3 pour une stat entière ; commune qui monte la perforation de la Cloueuse de 3 à 3,5 ; Lance-billes à 3,5 projectiles, 3 ou 4 par salve, 3,50 en moyenne sur 1 000 attaques ; Boîte à musique à 3,5 puis 4 orbes, soit 3 puis 4 orbes distinctes, sans perte ni doublon.
- Autres bancs à `RESULT failures=0` : objets, effets, acquisition, contrats, capacités ennemies, déplacements, petits lieux.
- Capture `--capture-weapons --integer-gains 3` sur l'Arc, le Lance-billes, le Trousseau et la Boîte à musique, regardée : chaîne de cinq sauts, quatre notes en orbite, salves plus fournies.

- Relecture `godot-reviewer` : aucun bug. Remarque gardée telle quelle : la carte et la pause affichent « Orbes 3,5 » alors que 3 orbes tournent ; la décimale montre ce qui manque avant la suivante.

**Reste :** la puissance d'ensemble se mesure en R5.

### R5 : la difficulté suit

**Mesure après R3 et R4**, mêmes seeds et même outil que R0 (5 seeds × 15 min, bot nomade qui prend la première carte, `tools/summarize_power.py`) : le joueur a pris de l'avance partout.

| Tranche | Temps pour tuer R0 → après R4 | Dégâts reçus R0 → après R4 | Niveau R0 → après R4 |
|---|---|---|---|
| 0–3 min | 3,05 → 2,04 s | 4 657 → 3 404 | 4,2 → 5,6 |
| 3–6 min | 0,76 → 0,45 s | 14 781 → 2 877 | 12,0 → 15,8 |
| 6–9 min | 0,32 → 0,30 s | 8 822 → 6 627 | 18,8 → 23,0 |
| 9–12 min | 0,33 → 0,28 s | 10 987 → 7 910 | 25,0 → 30,8 |
| 12–15 min | 0,30 → 0,25 s | 16 403 → 9 398 | 29,8 → 41,2 |

**Essais**, par surcharge `--scaling` sur l'arbre de R4 (5 seeds × 15 min chacun) :

| Essai | Réglage | Verdict |
|---|---|---|
| t1 | PV ×1,4 d'emblée, ×1,035 par minute | 3–6 min trop lent (2,03 s), fin juste |
| t2 | PV ×1,3, dégâts ennemis ×1,1 | dégâts reçus triplés en fin de run : écarté, les dégâts ne bougent pas |
| t3 | PV ×1,5, ×1,03 par minute | fin conforme, 3–6 min trop lent (1,81 s) |
| t4 | PV ×1,25, ×1,04 par minute | 0–6 min conforme, le joueur repart devant après 9 min (0,20 s, niveau 37) |
| **t5** | **PV ×1,25, ×1,04 jusqu'à 6 min, ×1,075 ensuite** | **retenu** |

La tranche 3–6 min est la plus sensible : entre ×1,5 et ×1,7 de PV à 4 min 30, le bot n'amorce plus sa montée et reste lent jusqu'à 9 min. Le premier écart se joue donc sur la pente, pas sur le multiplicateur de départ.

**Réglage retenu** (`data/scaling/spawn_flow.json`) : `flat_hp_multiplier` 1,25 (clé nouvelle, lue par `SpawnManager`), `hp_scaling_per_minute` 1,04, `late_hp_scaling_per_minute` 1,075 à partir de 6 min. Avant : ×1,05 par minute, ×1,07 après 22 min. Les PV valent ×1,25 au départ, ×1,58 à 6 min, ×3,0 à 15 min (R0 : ×1, ×1,34, ×2,08). Dégâts des créatures inchangés.

| Tranche | Temps pour tuer R0 → R5 | Dégâts reçus R0 → R5 | Niveau R0 → R5 |
|---|---|---|---|
| 0–3 min (9 runs) | 3,05 → 2,43 s (−20 %) | 4 657 → 3 861 (−17 %) | 4,2 → 5,8 |
| 3–6 min (9 runs) | 0,76 → 0,73 s (−4 %) | 14 781 → 8 029 (−46 %) | 12,0 → 13,1 |
| 6–9 min | 0,32 → 0,38 s (+19 %) | 8 822 → 11 532 (+31 %) | 18,8 → 20,6 |
| 9–12 min | 0,33 → 0,34 s (+3 %) | 10 987 → 11 795 (+7 %) | 25,0 → 26,4 |
| 12–15 min | 0,30 → 0,28 s (−7 %) | 16 403 → 16 669 (+2 %) | 29,8 → 31,8 |

Les deux premières tranches regroupent t4 et t5, identiques avant 6 min. Sur les mêmes réglages, t4 et t5 donnent 3,13 et 1,88 s de temps pour tuer à 0–3 min : cette tranche dépend surtout des premières armes tirées.

**Vérifié :**
- temps pour tuer à ±20 % de R0 sur les cinq tranches ; dégâts reçus à ±20 % sauf 3–6 min, où R0 est porté par deux runs très touchées, et 6–9 min (+31 %) ;
- la valeur lue dans les données donne les mêmes créatures que la surcharge : seed 1002, 46,1 PV par créature à 60 s contre 46,2 dans l'essai ;
- `dotnet build` : 0 avertissement.

**Reste :**
- un bot qui prend la première carte ne construit pas : le joueur qui choisit bien doit garder son avance, à juger en jeu ;
- au-delà de 15 min, la pente de 1,075 donne des PV ×5 à 22 min (×2,9 avant) : non mesuré, à surveiller avec l'endgame ;
- un plantage du moteur (signal 11, « propagate_notification » appelé depuis un autre thread sur `/root`) sur 1 run de 45, au début d'une Résurgence, sur l'arbre de R4 : non reproduit, aucun thread de notre code actif à ce moment.


### R6 : objets de déclencheur restants (G2c)

**Fait :** huit objets au modèle à 30 niveaux, pas et paliers de la référence §4 (`passive_souvenirs.json`) ; 31 objets proposés.

| Objet | Où | Retour visuel |
|---|---|---|
| Loupe de philatéliste | `ObjectTriggers`, au critique : Fragile 2 s (4 s au palier) | Éclats de verre sur la cible |
| Stylo à quatre couleurs | `ObjectTriggers`, au critique : une part du coup repart vers la cible la plus proche à 180 px (deux au palier), en effet déclenché | Rayon brisé couleur critique entre les cibles |
| Tabouret de camping | `ObjectStances` : immobile 1 s, cadence en plus (et armure +10 au palier), appliquées par écart | Étincelles laiton à l'arrêt |
| Chewing-gum | `ObjectTriggers` : pendant le dash, une tache tous les 22 px, posée le long du trajet quelle que soit la cadence d'image ; elle ralentit (et rend Fragile au palier) quatre fois par seconde, 1 s + le pas | Tache tramée au sol, le temps qu'elle dure |
| Gilet réfléchissant | `ObjectStances` : ennemis à 120 px, 10 au plus (20 au palier), relus dix fois par seconde | Étincelles laiton quand le plafond est atteint |
| Thermos | `ObjectStances` : PV à 90 % ou plus (75 % au palier) | Étincelles pâles quand l'état s'active |
| Médaille cabossée | `ObjectStances` : PV sous 35 % ; vitesse +15 % au palier | Étincelles rouge sang quand l'état s'active |
| Porte-monnaie usé | `ObjectStances` : +1 % par 10 Essence gardées, plafonné ; au palier, 20 % de chaque dépense rendue, en différé | Étincelles d'Essence à chaque tranche de 5 % ; l'Essence rendue vole vers le compteur |

- `ObjectStances` (nouveau) porte les objets d'état : il relit ses conditions dix fois par seconde, donne un multiplicateur de dégâts appliqué dans `Player.ResolveHitDamage`, et applique cadence, armure et vitesse par écart, sans fuite quand l'objet monte pendant qu'un état tient.
- Règles communes tenues : ce qui repart du Stylo est un effet déclenché, qui ne redéclenche rien ; la traînée pose ses statuts au nom du joueur (Pince à linge et Propagation les reconnaissent).
- Galerie d'armes : option `--dash`.

**Vérifié :**
- `dotnet build` : 0 avertissement.
- `test_objects.sh` : `RESULT failures=0`, avec trois contrôles nouveaux : critiques (Fragile +25 % 2 s au niveau 5, 60 % du critique vers la cible la plus proche au niveau 10 et rien au-delà de 180 px, 4 s et deux cibles au palier) ; traînée (tache posée au dash, ralentissement 0,6, effacée après 2 s) ; états (cadence +30 % après 1 s immobile, Gilet et Thermos ×1,26 avec trois ennemis, Médaille +30 % sous 35 %, Porte-monnaie plafonné à 30 %, 200 Essence rendues sur 1 000 dépensées, armure +10 et vitesse +15 % aux paliers).
- Captures regardées : `--capture-weapons` avec Lunettes, Loupe, Stylo, Gilet et Thermos (critique de 129 qui repart à 116 sur deux cibles, rayons visibles) ; `--dash` avec le Chewing-gum (traînée tramée sur tout le trajet du dash).
- Relecture `godot-reviewer` : un défaut corrigé (la traînée posait ses statuts sans source et privait la Pince à linge de son renouvellement) ; remboursement du Porte-monnaie différé pour ne pas imbriquer deux `EssenceChanged`.

**Reste :**
- l'Essence rendue par le Porte-monnaie compte, comme celle de la Photo de classe, pour la quête « Accumuler de l'Essence » : à trancher ensemble ;
- à pleine puissance, le Stylo fait repartir plus que le coup d'origine (180 % au niveau 30 en communes), conforme à la référence : à surveiller à l'équilibrage ;
- les trois objets « monde » (Presse-papier, Calendrier, Médaillon) attendent le Reliquaire.

### R7 : la carte fait moins vide (plan 22 C4 et C6)

Compte rendu détaillé au [plan 22 §15](22-carte-a-explorer.md#15-compte-rendu-c4-et-c6--six-petits-lieux-carte-agrandie-et-minimap-1er-octobre).

**Fait :**
- six petits lieux sur des décors déjà générés : Boîte aux lettres, Wagonnet, Voiture abandonnée, Cabine téléphonique, Abribus (nouveau décor le long des trottoirs), Table de pique-nique ;
- carte elliptique de 25 600 × 12 800 px (`map_radius_y` 400), décors 10 694 → 21 505 ;
- comptes de coffres, Mémoriaux, Failles et petits lieux relevés pour la surface doublée ;
- minimap : chemin révélé teinté par la phase d'Effacement, lieux découverts, coffres, Mémoriaux, Failles.

**Mesuré** (5 seeds × 10 min, bot nomade, mêmes seeds qu'en C1) : **un petit lieu en vue toutes les 26 s à lui seul** (80 s en C1), cible de 20 à 30 s atteinte ; coffres 10,2, Mémoriaux 1,2, Failles 1,8 croisés par run. Le bot qui ratisse visite 32,8 lieux (18,4 en C1). Génération du terrain 203 → 423 ms. Banc de combat A/B en rendu logiciel : −7 à −11 % de FPS avec la carte haute, à refaire sur une vraie carte graphique.

**Vérifié :** build sans avertissement, smoke vert, six bancs à `failures=0`, captures de la carte et de la minimap regardées, relecture `godot-reviewer` (corrections au plan 22 §15).

**Reste :** Repères (plan 22 §11, question 5) ; portée de la minimap à juger en jeu ; l'Essence n'est toujours jamais dépensée (Atelier, C2, en attente de Raphaël).

