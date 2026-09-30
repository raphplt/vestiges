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

### R8 : coffres (à faire seulement si Raphaël a confirmé)

Proposition de l'agent (DECISIONS §36), non encore confirmée :
- un coffre ouvre un **choix d'une amélioration parmi trois**, de rareté garantie : commun au moins inhabituelle, rare au moins rare, épique épique ou légendaire ;
- le choix porte sur les armes et les objets du joueur, et sur un objet neuf s'il reste un emplacement ;
- l'Essence et l'XP deviennent un petit bonus en plus ;
- les armes neuves ne sortent plus des coffres.

Si la confirmation n'est pas dans `DECISIONS.md` au moment d'y arriver, ne pas le faire et le signaler.

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

