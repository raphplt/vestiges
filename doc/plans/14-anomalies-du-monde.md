# Plan 14 — Anomalies du monde : l'oubli qui touche le joueur

Statut : **proposition à valider, rien d'implémenté** · Priorité : P1, après le socle objets du [plan 13](13-butin.md) pour les anomalies qui en dépendent · Dépendances : [plan 12](12-micro-evenements.md) (directeur, repères), Effacement (V2 §5), `RunHistoryManager`.

## 1. Le retour

Raphaël, le 24 septembre : « il faudrait peut-être des événements aléatoires sur la map (en nombre limité et qui se fondent bien dans le lore, comme un effondrement subit de la matière, etc.). Ça ne doit pas trop pénaliser le joueur (ne peut pas tuer sa run directement, mais peut avoir un impact). Le mieux du mieux serait que ces événements puissent être liés au joueur d'une manière ou d'une autre, et liés au thème du jeu, l'oubli. »

## 2. Ce qui les distingue des micro-événements (plan 12)

| | Micro-événements (12) | Anomalies (14) |
|---|---|---|
| Nature | Opportunité proposée au joueur | Le monde qui se dérègle **autour de lui** |
| Fréquence | Toutes les 2–3 min | 2 à 4 par run, rares et marquantes |
| Présentation | Bandeau, objectif, minuteur | Diégétique : sons, image, matière ; une seule ligne de texte au plus |
| Choix | Y aller ou non | Subie, mais toujours avec une réponse possible |
| Lien au joueur | Aucun | Tirée de **sa** run et de **ses** runs passées |

## 3. Règles de sécurité

- **Jamais mortelle seule** : une anomalie ne peut pas infliger plus de 25 % des PV max au total et ne fait pas descendre sous 1 PV.
- **Toujours annoncée** : 2 à 4 s de signe avant-coureur (craquement, distorsion de l'image, son qui s'étouffe).
- **Réversible** : ce que le joueur perd (arme, passif, vision) revient, ou se rachète par une action simple. Aucun objet possédé n'est détruit.
- **Jamais pendant une Résurgence**, ni pendant un micro-événement : le directeur du plan 12 réserve le créneau.
- **Compensée** : chaque anomalie laisse une trace positive (fragment de lore, vestige, Essence) quand on la traverse bien.

## 4. Catalogue proposé

### A. L'Effondrement — la matière oublie sa forme
Le sol autour du joueur perd sa cohérence : des dalles se dissolvent en Néant en cascade à partir d'un point, pendant 8 à 12 s, puis se reforment.
- **Impact** : le joueur doit bouger. Le Néant pousse et blesse légèrement, comme au bord de l'Effacement ; les ennemis pris dedans tombent.
- **Réponse** : suivre les dalles qui tiennent (plus lumineuses). Le dash devient précieux.
- **Trace** : l'effondrement met au jour un Vestige figé (plan 13) au centre, accessible quand le sol se reforme.
- **Lien au thème** : le monde oublie sa propre géographie.

### B. L'Écho de ta dernière run — le monde se souvient de toi
Utilise `RunHistoryManager` : à l'endroit correspondant à la distance où le joueur est mort la dernière fois, une silhouette pâle rejoue sa dernière posture : **son personnage, avec son arme de l'époque**, et la cause de sa mort dans la bulle (« Emporté par un Rôdeur Souverain »).
- **Impact** : l'écho attire pendant 20 s la créature (ou la variante) qui l'avait tué, en version renforcée.
- **Réponse** : vaincre ce qui t'avait vaincu.
- **Trace** : l'écho se dissout en laissant l'arme de cette run, ou un vestige de rareté supérieure, et un fragment de lore personnel (« Tu étais déjà passé par ici »).
- **Lien au joueur** : direct, c'est son histoire. Sans run précédente, l'anomalie n'est pas tirée.

### C. L'Oubli de soi — le joueur oublie un geste
L'Effacement effleure le joueur : **une de ses armes (ou un passif) est oubliée** pendant 25 s. Son icône grisée se fissure dans le HUD, et un éclat lumineux de cette arme s'échappe et s'enfuit à quelques dizaines de pixels.
- **Impact** : puissance réduite, temporairement.
- **Réponse** : rattraper l'éclat (il fuit et se cache derrière les ennemis) rend l'arme aussitôt, **avec un bonus temporaire** (« Tu t'en souviens mieux ») ; sinon, elle revient seule à la fin du délai.
- **Lien au joueur** : touche son build à lui ; l'arme visée est tirée parmi les plus utilisées de la run.

### D. Le Lieu qui se souvient — un îlot du monde d'avant
Pendant 20 s, une zone de quelques écrans retrouve son état d'avant l'apocalypse : couleurs chaudes, musique étouffée d'avant, bâtiments intacts en surimpression. Les créatures y sont ralenties et affaiblies (elles « n'ont pas leur place »).
- **Impact** : positif, une respiration ; mais la zone s'efface ensuite **plus vite** que la normale.
- **Réponse** : en profiter pour ramasser, puis sortir à temps.
- **Trace** : fragment de lore du lieu (la Bible décrit les biomes d'avant), garanti.
- **Lien au thème** : l'oubli est une perte, ce moment montre ce qui est perdu.

### E. Le Double — l'Oublié qui porte ton visage
Un **Oublié** apparaît : une copie grisée du personnage du joueur, avec les mêmes armes (dégâts réduits) et un nom effacé.
- **Impact** : combat exigeant, qui lit ton build contre toi.
- **Réponse** : le vaincre, ou le semer (il se dissout après 40 s).
- **Trace** : un exemplaire supplémentaire d'un objet que tu possèdes (plan 13), ou une montée de niveau d'arme.
- **Lien au joueur** : maximal, mais c'est l'anomalie la plus chère à produire (sprites du joueur réutilisés avec un shader, IA de combat du joueur). À garder pour un second lot.

### F. Le Mirage — une mémoire fausse
Un coffre ou un Triptyque apparaît… mais c'est un souvenir faux : au contact, il se révèle vide et libère des Présages ; ou, une fois sur deux, il est vrai. Un indice subtil (reflet qui tremble, ombre absente) permet au joueur attentif de le deviner.
- **Impact** : faible, une embuscade.
- **Lien au thème** : la mémoire trompe. Récompense l'observation.

## 5. Directeur

- Géré par le `RunEventDirector` du plan 12 (même créneau, catégorie distincte), avec son propre budget : 2 à 4 anomalies par run de 15 min, jamais deux fois la même, écart minimal de 4 min.
- Poids selon le contexte : l'Écho seulement si une run précédente existe ; l'Oubli de soi seulement avec au moins 2 armes ; le Lieu qui se souvient plutôt en début de run ; l'Effondrement plutôt en zone d'Effacement avancé.
- Données dans `data/events/anomalies.json`, même format que `run_events.json`.

## 6. Lots proposés

| Lot | Contenu | Dépend de |
|---|---|---|
| **A** | Effondrement + Oubli de soi + budget du directeur | Plan 12 (fait) ; Vestige figé du plan 13 pour la trace de l'Effondrement (sinon Essence en attendant) |
| **B** | Écho de ta dernière run (enregistrer la position de mort et la créature tueuse dans `RunRecord`) | Historique de runs, sprites de personnages |
| **C** | Lieu qui se souvient (surimpression, palette, musique) | Plan 08 (art), plan 10 (tiles) |
| **D** | Mirage, puis Double | Plan 13 (Triptyque, objets) |

Recommandation : A d'abord, parce qu'il est peu coûteux, qu'il touche directement le thème et qu'il valide les règles de sécurité. B ensuite : c'est le plus fort pour le lien au joueur, et il demande seulement d'enrichir `RunRecord` (position et tueur).

## 7. Décisions demandées à Raphaël

1. Quelles anomalies garder parmi les six ? Mes préférées : Écho de ta dernière run, Oubli de soi, Effondrement.
2. Le plafond de pénalité (25 % des PV max, rien de détruit) convient-il ?
3. Faut-il une ligne de texte à l'écran (« La matière oublie sa forme… ») ou tout rendre sans texte ?
