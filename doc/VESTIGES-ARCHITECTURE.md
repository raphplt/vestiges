# VESTIGES - Principes d'Architecture

> Version : 2.1
> Derniere mise a jour : 21 fevrier 2026
> Moteur : Godot 4.x
> Langage : C#
> Statut : Reference architecture (niveau conceptuel)
> Docs lies : GDD v1.3, Bible Artistique & Narrative v1.0, Roadmap & Milestones v1.0

---

## 1. But du document

Ce document decrit comment le projet doit etre structure dans les grandes lignes.

Il ne contient volontairement ni code, ni details d'implementation, ni arborescence exhaustive.
Son role est de garder des decisions claires sur ce qui compte vraiment :

- separation des responsabilites
- circulation des donnees
- limites entre systemes
- priorites de qualite (fun, stabilite, lisibilite, evolutivite)

Ce document est donne en contexte a l'IA a chaque session de developpement pour garantir la coherence des decisions techniques.

---

## 2. Principes directeurs

1. Gameplay d'abord
Le but de l'architecture est de permettre d'iterer vite sur le fun, pas de construire une usine a gaz.

2. Data-driven
L'equilibrage et les contenus doivent vivre dans les donnees, pas dans la logique hardcodee.

3. Separation claire des responsabilites
Un systeme = une responsabilite principale = des interfaces simples.

4. Decouplage fort, cohesion forte
Chaque module est coherent en interne et faiblement couple aux autres.

5. Etat explicite
Les transitions importantes du jeu (Hub, Run, Death, cycle jour/nuit) doivent etre visibles et pilotables.

6. Robustesse avant sophistication
Le comportement doit rester comprehensible, testable et debuggable.

7. Evolutif vers le coop
Meme en solo, on evite les choix qui bloquent l'ajout d'un 2e joueur.

---

## 3. Vue d'ensemble de l'architecture

Le projet est organise en 4 couches logiques.

1. Presentation
UI, feedback, ecrans, HUD, menus. Cette couche affiche l'etat et envoie des intentions.

2. Orchestration
Gestion des etats globaux, cycle de run, transitions entre scenes/fases.

3. Domaine gameplay
Systemes coeur : combat, progression, monde, craft/base, meta-progression, score, evenements.

4. Infrastructure
Chargement des donnees, sauvegarde, audio, outils debug, integration plateforme.

Regle de dependance :
la presentation depend du domaine, le domaine ne depend pas de la presentation.
l'infrastructure sert les autres couches, elle ne porte pas la logique de gameplay.

---

## 4. Frontieres des systemes gameplay

Chaque systeme possede son propre perimetre.

Combat
- gere auto-attaque, degats, collisions de combat, capacites actives, statuts, mort
- le joueur ne declenche pas l'attaque : le systeme detecte les cibles en portee et attaque automatiquement
- n'attribue pas lui-meme progression ou score final

Progression
- gere XP, niveaux, perks, courbe de puissance du joueur
- les perks sont des modificateurs de stats charges depuis les donnees, pas du code en dur
- ne decide pas des spawns ni des regles du monde

Monde
- gere cycle jour/nuit, generation procedurale (seed deterministe), biomes, POI, fog, ressources sur la carte
- gere les etats de realite (ancre / effiloche / efface) qui affectent le rendu et la densite de menaces
- ne gere pas les regles de perk ou d'economie meta

Spawn
- gere l'apparition des creatures selon la phase (disperse le jour, vagues convergentes la nuit)
- gere le scaling par nuit (nombre, types, elites, mini-boss) depuis les courbes de difficulte en donnees
- ne gere pas les degats ni l'IA de combat

Base et Craft
- gere inventaire de ressources, recettes, placement sur grille iso, structures, reparations
- le Foyer est une entite speciale avec son propre perimetre (rayon de securite, ameliorations)
- ne pilote pas la boucle globale de run

Meta-progression
- gere vestiges, debloquages, souvenirs, persistance entre runs, objectifs long terme
- n'impacte pas directement la simulation frame-to-frame

Score
- agrege les evenements de run selon des regles centralisees (donnees)
- calcule multiplicateurs (personnage, mutateurs, streak)
- reste independant de l'UI et de la presentation

Evenements
- gere le declenchement et le deroulement des evenements aleatoires (jour et nuit)
- s'appuie sur les systemes existants (spawn, monde, loot) sans les piloter directement

---

## 5. Circulation des donnees

On distingue 3 types d'etat.

1. Donnees de reference
Configurations de contenu (ennemis, perks, recettes, loot tables, biomes, courbes de scaling, regles de score).
Format : JSON, un fichier par entite ou par collection logique.
Elles sont lues au boot, cachees en memoire, jamais modifiees au runtime.

2. Etat de run
Etat temporaire d'une partie en cours (phase jour/nuit, HP, inventaire, structures posees, carte generee, perks actifs, score courant).
Reinitialise a chaque nouvelle run. Sauvegardable a l'aube pour reprise.

3. Etat meta
Progression persistante entre runs (vestiges, souvenirs trouves, personnages debloques, records, mutateurs actives).
Priorite maximale en termes de resilience — ne doit jamais etre perdu.

Regles clefs :
- une source de verite par type de donnee
- aucune duplication d'etat sans raison explicite
- tout changement majeur passe par un point de controle clair (evenement metier)
- les donnees de reference sont la source unique pour l'equilibrage : une stat d'ennemi, un cout de recette, un seuil de scaling ne doivent exister qu'a un seul endroit

---

## 6. Orchestration et cycle de vie

Le jeu suit un flux simple et explicite :

- initialisation (boot, chargement des donnees de reference)
- hub (selection personnage, depense vestiges, mutateurs)
- run (boucle jour/nuit jusqu'a la mort)
- ecran de fin (score, records, vestiges gagnes)
- retour hub

La run suit un cycle repetitif :

- jour (exploration, combat, recolte, craft, level-up)
- crepuscule (transition visuelle et sonore, derniers preparatifs)
- nuit (vagues convergentes, defense de base)
- aube (resume, extension de la carte, increment de difficulte)

Principes a respecter :
- chaque transition a des conditions d'entree/sortie claires
- un seul proprietaire de l'etat global de run (orchestrateur)
- les systemes gameplay reagissent aux changements de phase, ils ne pilotent pas la machine d'etats
- a l'aube, la carte s'etend (nouveaux chunks en peripherie) sans reinitialiser l'existant

---

## 7. Communication entre systemes

Les systemes communiquent par evenements metier et interfaces explicites.

Bon modele :
- un systeme emet un fait de jeu (ex: "ennemi tue", "nuit commencee", "perk selectionne")
- les systemes interesses reagissent chacun de leur cote (score ajoute des points, loot drop un orbe, progression ajoute de l'XP)

Mauvais modele :
- chaines d'appels directs entre modules non relies
- dependances implicites difficiles a tracer

Evenements metier cles de VESTIGES :
- cycle : jour demarre, crepuscule, nuit demarre, aube atteinte
- combat : ennemi tue (avec type, position, valeur), joueur touche, joueur mort
- progression : XP gagne, level up, perk selectionne
- monde : zone decouverte, coffre ouvert, souvenir trouve, evenement aleatoire declenche
- base : structure placee, structure detruite, craft termine, ressource collectee
- meta : run terminee (score final, nuits, cause de mort)

Objectif :
eviter les effets de bord et faciliter les tests, le remplacement ou la desactivation d'un module.

---

## 8. Persistance

La persistance est separee en deux niveaux.

1. Sauvegarde meta
Toujours prioritaire, resiliente, orientee progression long terme.

2. Sauvegarde de run
Optionnelle selon le design produit, utile pour reprise apres interruption.

Regles :
- versionner les formats de sauvegarde
- prevoir une migration simple quand la structure evolue
- gerer les cas de corruption sans perdre la progression meta

---

## 9. Performance et scalabilite

La performance est un objectif de design, pas une correction tardive.

Principes :
- limiter allocations et destructions frequentes (pooling obligatoire pour ennemis, projectiles, orbes XP, effets visuels)
- controler le nombre d'entites actives (cible : 50-100 ennemis simultanes en nuit avancee)
- degrader intelligemment les systemes hors focus joueur (IA simplifiee hors ecran, pas d'animation, pathfinding reduit)
- mesurer en continu (FPS, temps de frame, cout des systemes)

Bottleneck anticipe principal : les nuits avancees (nuit 7+) avec de nombreux ennemis, projectiles d'auto-attaque, effets de perks, et structures actives (tourelles). C'est le scenario de stress a cibler des le prototype.

Cible : 60 FPS constant sur hardware mid-range 2020+. Minimum absolu : 45 FPS.

On cherche une stabilite ressentie en priorite :
fluidite de combat, lisibilite des vagues, reactivite des inputs.

---

## 10. Entites et composition

Le joueur n'est pas un singleton. C'est une entite composee de sous-systemes independants (mouvement, stats, combat, inventaire, progression). On doit pouvoir en instancier deux sans conflit.

Regles pour les entites :
- aucun systeme ne doit presupposer qu'il n'y a qu'un seul joueur
- les systemes globaux (score, spawn, cycle) prennent un identifiant de source quand c'est pertinent
- les ennemis et projectiles sont des entites recyclees depuis un pool, pas instancies/detruits

La generation procedurale est deterministe : meme seed = meme monde, garanti. La seed doit etre reproductible pour le debug, le partage (seed hebdomadaire), et la validation de bugs.

---

## 11. Testabilite et observabilite

L'architecture doit rendre le jeu observable et testable.

Indispensable :
- systemes coeur testables de facon isolee (combat, score, progression, generation)
- scenarios d'integration pour les boucles critiques de run
- outils debug pour visualiser l'etat interne sans modifier le gameplay (overlay F1, console de commandes)
- analytics de run : a chaque mort, exporter les donnees cles (perks choisis, cause de mort, nuit atteinte, score) pour equilibrer le jeu

Un bug doit etre reproductible (grace a la seed deterministe), localisable, puis corrige sans regressions cachees.

---

## 12. Gouvernance technique

Quand une decision structurelle est prise, elle doit etre tracee simplement :

- probleme constate
- options considerees
- choix retenu
- impact attendu

Le but est d'eviter les reorientations silencieuses et de garder une architecture coherente dans le temps.

---

## 13. Ce qu'on evite absolument

- architecture basee sur des singletons partout (seuls les services d'infrastructure sont des singletons)
- logique gameplay eparpillee dans l'UI
- valeurs d'equilibrage dupliquees a plusieurs endroits (une stat = un seul fichier JSON)
- modules qui se connaissent mutuellement en boucle
- optimisation prematuree sans mesure
- sur-ingenierie qui ralentit l'iteration gameplay
- etat joueur global qui empeche l'ajout d'un deuxieme joueur
- generation procedurale non-deterministe (impossible a reproduire ou debugger)

---

## 14. Definition d'une architecture saine pour VESTIGES

L'architecture est consideree saine si :

- une nouvelle feature peut etre ajoutee sans casser 5 systemes
- un reglage de gameplay se fait via les donnees
- un bug de run est traçable a un module clair
- les transitions de jeu sont previsibles et robustes
- le projet reste lisible apres plusieurs mois d'iteration

Si ces criteres sont respectes, le projet reste maitrisable meme en grandissant.