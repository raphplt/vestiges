# Vestiges — le jeu en une page

18 octobre 2026 · [registre §189 à §191](plans/DECISIONS.md). Cette page dit ce qu'est le jeu, dans quel ordre on le finit et comment on travaille. Elle fait autorité sur la Stratégie V2 et les plans, qui deviennent l'historique.

## En une phrase

Un survivors-like où tu remontes la nuit qu'une ville a voulu oublier : enchaîne les éliminations sans te faire toucher, pousse la horde dans le vide, et deviens monstrueux.

## La forme (verrouillée le 18 octobre)

- **Le genre** : tir automatique, hordes, montées de niveau, builds qui finissent par casser le jeu. On ne réinvente pas le genre, on le joue très bien.
- **La run** :
  - trois terres tirées au sort, un gardien chacune ;
  - puis **la Montée**, étape fixe, avec **l'Indicible** au bout. Le vaincre gagne la run, et la fin sans fin continue de là ;
  - les cinq terres sont mélangées pour l'instant, le danger vient du rang. Des réserves par rang viendront quand il y aura plus de terres.
- **Deux signatures**, chacune en une phrase :
  - **la Série** : enchaîne les éliminations sans te faire toucher. Version finie proposée ([plan 52 §15](plans/52-elan.md)) : chaque élimination la fait monter, un coup la coupe en deux, une jauge vide l'encaisse en Essence ; ses rangs donnent dégâts, vitesse et Chance ;
  - **le vide** : pousse la horde dedans, saute par-dessus. Saut et percussion sont faits ; reste à les essayer.
- **Le gambling** : coffres payants à raretés, cartes de niveau, Failles, et la Chance portée par la Série.
- **Les dizaines d'heures** : quatre difficultés, quêtes qui débloquent personnages et armes, Campement, classement. Tout existe déjà.
- **L'histoire** : les terres sont des lieux de Vaulme, chaque gardien laisse un fragment de lore, et la Montée raconte la barrière.

## Trois piliers

Chaque fonctionnalité en sert au moins un, sinon elle part en réserve.

1. **La maîtrise rend monstrueux** : la Série et le vide.
2. **La horde, la puissance et le gambling.**
3. **Vaulme** : des lieux reconnaissables, de la mélancolie, un mystère qui se révèle de run en run.

## L'ordre de travail, en effort

Pas de durées, qui sont impossibles à estimer honnêtement. Chaque étape commence quand la précédente est livrée.

| Ordre | Étape | Effort |
|---|---|---|
| 1 | **La Série, version finie** : S1 livrée et essayée le 18 octobre ; trois règles, retours, HUD et réglage proposés au [plan 52 §15](plans/52-elan.md) (lots S2 à S5), à implémenter jusqu'au bout puis jouer en vraie run | Moyen |
| 2 | **Revue de la boucle de run** (terres 1 à 3, finale, fin sans fin), aussi appuyée que celle de la Série ; la forme ci-dessus peut y changer ([§191](plans/DECISIONS.md)) | Moyen |
| 3 | **Le rythme du départ** : premier niveau vite, écran plein tôt, aimant d'XP revu ; mesuré au bot. La Série en dépend : le bot ne fait jamais ×10 en 7 minutes aujourd'hui | Faible |
| 4 | **Ton essai du vide** : la Série est essayée, le vide pas encore ; critère d'arrêt ci-dessous | Ton temps |
| 5 | **Les terres complètes** : cinq lieux de Vaulme, pièces faites main, génération structurée ; à découper dans un plan dédié | Élevé, le gros chantier |
| 6 | **La Montée et l'Indicible en finale**, un fragment de lore par gardien, la fin sans fin | Moyen |
| 7 | **Un vrai gardien de référence** (corps et attaques), puis un par terre avec l'ami artiste | Moyen, puis élevé |
| 8 | **La clarté** : cinq notions au plus dans la première terre, première run guidée, sons, lisibilité du vide | Moyen |
| 9 | **Cinq testeurs extérieurs**, puis verrouillage ; ensuite seulement la sortie (nom, page Steam, conformité des sons et des images) | Ton temps |

**Critère d'arrêt de l'essai** : Raphaël et un joueur extérieur jouent 15 min.
- Poussent-ils la horde dans le vide sans qu'on le dise ?
- La Série donne-t-elle envie de foncer ?
- Est-ce jouissif ?

Si le vide ne prend pas, on le retire et la Série reste seule.

**En réserve, désactivé mais pas supprimé** : Ateliers, Puits, stèles, Failles hors gambling, micro-événements, Résurgences, Souverains, Barrière, autres personnages, nouvelles quêtes, classement. Chacun revient après l'étape 9, s'il sert un pilier.

## Méthode

- **Boucler de bout en bout.** Une mécanique est finie quand elle est au niveau d'une sortie : règles lisibles, retours visuels et sonores à chaque changement, place au HUD, réglage mesuré, jouée en vraie run. Pas de sujet suivant avant ([§191](plans/DECISIONS.md)).
- **Simple à comprendre, dur à maîtriser** : chaque mécanique se prend en main en quelques secondes et demande des dizaines d'heures pour être maîtrisée.
- **Aucun point en suspens.** Toute question reçoit aussitôt une décision par défaut de l'agent, consignée au registre et réversible par Raphaël, ou une place dans l'ordre ci-dessus. Le §2 du tableau de bord reste vide.
- **En effort, jamais en durée.**
- **Un chantier à la fois.** Rien ne passe par défaut dans le jeu sans que Raphaël l'ait joué.
- **La boucle de Raphaël** : jouer 30 min, noter trois choses (fun, ennui, confusion), faire un seul changement, rejouer.
- **Des tests extérieurs réguliers** : on n'explique rien, on note où le joueur se perd.
- **Avec l'IA** :
  - deux options au plus, une recommandation franche, ce qu'on retire, l'avocat du diable ;
  - les sessions en autonomie portent sur du validé, jamais sur le design.
