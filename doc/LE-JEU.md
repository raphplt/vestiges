# Vestiges — le jeu en une page

18 octobre 2026 · Revue de direction, [registre §189](plans/DECISIONS.md). Cette page dit ce qu'est le jeu, ce qui est acté, ce qui reste à explorer et comment on travaille. **Proposé : elle fait autorité** sur la Stratégie V2 et les plans, qui deviennent l'historique.

## En une phrase

Un survivors-like où le monde s'oublie sous tes pieds : ravive les Mémoriaux avant que ta terre ne tombe, et sers-toi du vide contre la horde.

## Trois piliers (proposés)

Chaque fonctionnalité en sert au moins un, sinon elle part en réserve.

1. **Le vide est la menace et l'arme.** Le sol tombe, on pousse la horde dedans, on le saute.
2. **La horde et la puissance.** Un écran plein dès les premières secondes, des builds qui finissent par casser le jeu.
3. **Vaulme.** Des lieux reconnaissables, de la mélancolie, un mystère qui se révèle de run en run.

## Acté le 18 octobre

- **Signature : le vide comme arme, avec la jauge d'Élan.**
  - Percussion qui projette la horde dans le vide (plan 52 E4).
  - Saut des trous (E3).
  - Esquive parfaite (E2, livrée).
  - Jauge à trois paliers gardée.
  - À régler dans le bac à sable : la percussion existe-t-elle sans palier, amplifiée ensuite ? Sinon le geste signature disparaît dès qu'on est touché.
- **Terres : des lieux de Vaulme, mieux générés.**
  - Le Bas-Port, la Haute-Ville, la Montée, le Bois, la Carrière, plutôt que des biomes génériques.
  - 3 à 5 pièces faites main par terre (l'église, la digue, la ferme…), reliées par des chemins procéduraux : « on se souvient des lieux, pas des chemins ».
  - Une génération qui structure : clairières et fourrés, places, repères visibles de loin, espaces ouverts et passages serrés en alternance.
- **Gel des nouveautés et tranche verticale** (ci-dessous).

## En exploration : la structure de run, jusqu'au 25 octobre

Pas de code, sur papier, en parallèle du bac à sable.

- **La référence** est la boucle jouable aujourd'hui : trois terres, un gardien par terre, puis une terre sans fin ([plan 51 §11](plans/51-objectifs-de-run.md)).
- **Le format challenger** : l'île qui dérive (plan 51 §11.5). Une seule terre vivante. Le sol le plus ancien tombe, et chaque Mémorial ravivé fait monter un morceau neuf, souvent d'un autre lieu. On choisit sa direction, et la question redevient « jusqu'où ».
- **Une place** reste libre pour une idée de Raphaël. Pas plus de deux formats face à la référence.
- **Méthode** : raconter une partie minute par minute pour chaque format, une page chacun, puis choisir.
- **Critères** :
  - les huit tests du plan 51 §11.1 ;
  - un extrait de cinq secondes compris par quelqu'un qui ne connaît pas le jeu ;
  - le coût pour un développeur seul ;
  - le réemploi de ce qui est construit (terres P0 à P3).

## La tranche verticale (gel jusqu'à fin novembre)

**Dedans** :
- le Vagabond ;
- deux terres travaillées, proposées : le Bas-Port et la Haute-Ville, les deux moitiés de Vaulme ;
- un vrai gardien ;
- environ 8 armes et 20 objets ;
- les coffres et les Mémoriaux.

**En réserve**, désactivé dans les données, pas supprimé : Ateliers, Puits, stèles, Failles, micro-événements, Résurgences, Souverains, Barrière, autres personnages, quêtes, classement. Chacun revient après la tranche s'il sert un pilier.

**Ordre** :

1. **Bac à sable du vide, une semaine.** Critère d'arrêt : Raphaël et un joueur extérieur jouent 15 min.
   - Poussent-ils la horde dans le vide sans qu'on le leur dise ?
   - Est-ce jouissif ?
   - Un extrait de 10 s se comprend-il ?
   - Le banc dense ne régresse-t-il pas ?

   Si c'est non, le front reste un chronomètre et l'innovation se cherche ailleurs.
2. **Le rythme.**
   - Revenir sur les ralentissements des 13 et 14 octobre : un départ rapide et généreux, une difficulté qui monte après.
   - Le front compte dès la première minute.
   - Cibles proposées, mesurées au bot :
     - premier niveau avant 20 s ;
     - plus de 10 créatures à l'écran en moyenne dès la deuxième minute ;
     - plus d'image vide après 30 s.
   - Mesure du 18 octobre : 3,5 créatures la première minute, premier niveau entre 23 s et 3 min 05.
3. **Les deux terres.**
4. **Le gardien**, selon la charte du plan 51 §11.4.

**Clarté** : cinq notions au plus à comprendre dans la première terre.

## Méthode

- **Un chantier à la fois.** Rien ne passe par défaut sans que Raphaël l'ait joué.
- **Boucle** : jouer 30 min, noter trois choses (fun, ennui, confusion), faire un seul changement, rejouer.
- **Un test extérieur toutes les deux semaines.** On n'explique rien, on note où le joueur se perd.
- **Avec l'IA** :
  - deux options au plus, une recommandation, ce qu'on retire, l'avocat du diable ;
  - les sessions en autonomie portent sur du validé (performance, outils, contenu de la tranche), jamais sur le design.

## Jalons

| Date | Jalon |
|---|---|
| 25 octobre | Verdict du bac à sable du vide ; structure de run choisie |
| Fin novembre | Tranche verticale jouable, fun pour cinq testeurs extérieurs |
| Ensuite | Page Steam et démo ([plan 31](plans/31-sortie-commerciale.md)) |
