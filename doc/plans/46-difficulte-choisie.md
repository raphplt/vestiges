# Plan 46 — Choisir jusqu'où pousser la difficulté

13 octobre 2026. **Conception : stèles et défi volontaire validés par Raphaël ; accord de principe sur les autres pistes ([DECISIONS §162](DECISIONS.md)). Aucun gameplay implémenté.** Suite du [plan 28](28-peril-et-difficulte.md) et du [plan 42](42-signature.md), en réponse à [DECISIONS §154](DECISIONS.md). Ce plan détaille le risque volontaire ; il ne remplace pas l'équilibrage de la difficulté normale. Les chiffres proposés restent des valeurs d'essai à mesurer.

## 1. Direction et limites

**Promesse proposée : « Quand ton build tient, appelle davantage de créatures — puis avance avant que le monde ne te rattrape. »**

Acquis à respecter :

- §74 : renforcer surtout les ennemis ; doser la difficulté devient une compétence du joueur.
- §78 : Péril sans plafond ; sa contrepartie vient des créatures supplémentaires, jamais d'un versement automatique d'Essence, d'XP ou d'une hausse de rareté.
- §151 et plan 42 §3 : pression de l'Effacement, butin à sauver, ancrage par les gestes existants ; un build en synergie peut s'emballer. Pas de nouvelle attaque manuelle, de halo au sol ni de course imposée dans une seule direction.
- §152 : retirer la puissance brute du Campement est acquis ; XP et Chance restent à trancher dans le gambling. Les fixtures de mesure n'achètent pas les rangs de puissance brute ; ne pas confondre cela avec leur retrait du jeu.
- §155 et plan 43 livré : **une créature éliminée vaut une élimination**. Aucun multiplicateur de score, aucun point pour une activation, une difficulté ou un défi. Plus d'éliminations donnent aussi plus de Vestiges selon le taux commun.
- §160 « Souverains calés sur le niveau du joueur » : leur courbe est un chantier séparé, cible de 20–40 s face à un build moyen du niveau concerné. Ne pas obtenir la difficulté choisie en annulant ce recalage. Le nouveau personnage est suivi au §161 ; cette proposition ne tranche pas son design.
- Stratégie V2 : exploration mobile, combat, montée en puissance, fuite de l'Effacement ; endgame ouvert après l'Indicible, sans mort imposée. Ni craft, ni construction, ni jour/nuit.

Les anciennes mentions de multiplicateurs de score au plan 28 et aux §14/18 de la Stratégie sont historiques : l'amendement du §11 et le §155 du registre fixent la règle actuelle. La validation des orientations est consignée au §8 ; elle ne vaut pas équilibrage vérifié des chiffres ci-dessous.

## 2. Inventaire vérifié dans les données et le code

### Avant la run

Le joueur choisit son personnage et ses achats au Campement. **Aucun Péril de départ ni réglage d'Effacement n'est exposé.** Les mutateurs sont désactivés en V2 : méthodes de compatibilité vides dans `MetaSaveManager`, listes vidées au Hub et au bootstrap. `GameManager.ActiveMutators` et le champ de l'historique subsistent, sans système jouable derrière. Le « Tableau de mutateurs » décrit dans la Stratégie n'est donc pas un acquis livré.

`--peril N`, `--meta`, `--player-profile`, `--start-build` et `--scaling` sont des **commandes du banc**, pas des choix du joueur au Hub.

### Pendant la run

| Moyen existant | Choix et coût | Gain réel | Persistance |
|---|---|---|---|
| Stèle du Péril | Maintien de 0,8 s ; +1 ; dix stèles placées, dont une proche du départ | Créatures supplémentaires selon les règles de Péril | Une activation par stèle ; Péril conservé jusqu'à la fin de run |
| Sifflet d'arbitre | Objet commun ; chaque exemplaire ajoute +1 ; occupe un type du sac | Même effet de Péril, aucun autre bonus | Copies illimitées ; oublier la pile au Puits ne retire pas le Péril acquis |
| Faille | Accepter une amélioration Épique ou mieux avec son Oubli, +1 Péril ; refus possible | L'amélioration est la récompense du pacte ; le Péril ajoute des créatures | Faille fermée après acceptation ; Oubli et Péril cumulés |
| Bannissement payant | Après les gratuits, le n-ième payant ajoute n/3 point, dette réglée par points entiers ; le Campement augmente les gratuits | Offre renouvelée, build mieux ciblé ; difficulté en contrepartie | Péril conservé |
| Mémorial | Payer pour lever un Oubli non définitif | Retire son malus du monde | **Ne retire pas le Péril** gagné à la Faille |

Le Sifflet n'est plus l'ancien objet à cartes de trente niveaux décrit au plan 28 P3 : il suit le catalogue actuel des objets cumulables (`max_copies: 0`). Son ancien champ `max_level` subsiste ; il ne faut pas concevoir un retour à ces montées de niveau.

### Ce que vaut le Péril aujourd'hui

`data/scaling/peril.json`, `PerilDataLoader`, `PerilManager` et `SpawnManager` :

- Péril entier, minimum 0, sans maximum ; affichage au HUD et dans la pause.
- Nombre : `1 + min(0,10 × Péril, 1,50)`, appliqué à la cible locale **et** au plafond de population de la run.
- PV et dégâts des nouvelles créatures : chacun `1 + 0,05 × Péril`.
- **Plafond technique actuel : 500 créatures actives**, commun à la run, Résurgences comprises. Les anciens 200/240 du plan 28 ne sont plus les données en vigueur.
- Dès Péril 15, nombre ×2,5 ; au-delà, **ni la cible locale ni le plafond ne montent par ce levier**, seuls PV et dégâts continuent. Le commentaire de `peril.json` qui annonce encore une hausse de densité au-delà est inexact face au chargeur.
- Le Péril n'accélère pas directement l'intervalle d'apparition ; relever une population cible ne garantit pas une hausse du débit d'éliminations.
- Un « Péril du temps » est câblé après 8 min, mais `time_peril_per_minute: 0` : il ne fait actuellement rien. Il serait subi, pas choisi ; l'activer relève de l'équilibrage normal.

Exemple calculé sur les données, **pas une mesure** : à 30 min, le plafond nominal vaut 230 ; Péril 3 → 299, Péril 6 → 368, Péril 10 → 460, Péril 15 → 575 ramené à 500. En Résurgence, le facteur ×1,4 fait atteindre 500 plus tôt. **Péril 20 ne permet pas plus de créatures que 15**, mais leur donne PV/dégâts ×2 au lieu de ×1,75.

### Oublis : neuf malus du monde

Ils sont proposés avec les cartes des Failles, pas sélectionnables seuls au Hub. Les effets du même nom s'additionnent ; les deux effets définitifs sont irréversibles même après un passage au Mémorial.

| Oubli | Effet des données actuelles | Levée au Mémorial |
|---|---|---|
| Peur | Une élite de plus rôde en permanence | Oui |
| Chemin | Effacement +20 % ; multiplicateur `1 + total` | Oui |
| Répit | Résurgences rapprochées de 20 %, avec borne d'intervalle dans le gestionnaire | Oui |
| Repères | Flèches de coffres supprimées ; colonnes raccourcies de 50 % | Oui |
| Regard | Révélation du brouillard réduite de 25 % | Oui |
| Lieux | Un Mémorial endormi s'effondre | Non |
| Visages | +12 points de chance de variante sur les créatures | Oui |
| Trésor | Coffres fermés abaissés d'un rang de rareté | Non |
| Traces | Détection du joueur par les créatures étendue de 30 % | Oui |

Ces Oublis ne donnent pas tous plus de cibles : rendre les coffres moins bons ou les repères invisibles est un risque de pacte, pas une récompense de classement. Ne pas les recycler en « mode difficile » en promettant davantage d'éliminations.

### Effacement, Résurgences et endgame

`erasure.json` donne déjà une densité locale jusqu'à ×2,2 à mémoire nulle, des malus de vitesse/dégâts ×0,95 en zone effilochée puis ×0,88 en zone effacée/Néant, et 6 % des PV max par seconde dans le Néant. Coffres, lieux et présence stabilisent ou ralentissent l'oubli. Les stats aléatoires temporaires du plan 42 et le butin à sauver restent en conception.

Le calendrier actuel vise l'Indicible à 30 min, éventuellement plus tôt à la cinquième Résurgence. Après sa mort, gestion de phase et tempo d'endgame existent ; le scaling infini reste décoché en roadmap. Accélérer l'Effacement peut avancer le late game ; rapprocher les Résurgences peut avancer le boss. Ce sont des effets à mesurer et à expliquer, pas des options indépendantes sans conséquence.

## 3. Diagnostic : accès et rendement sont deux problèmes

M1 du 13 octobre, 30 min et trois graines par configuration (plan 42 §5) :

- Traqueur moyen, méta complète : environ 22 000 éliminations, 10 PV perdus/min après 15 min ; arme poussée, zone et critique : 20 000–24 000, 0–2 PV/min en seconde moitié.
- Effacé ou Néant : 3–7 % du temps ; l'Effacement presse peu.
- Le profil fort sous-performe parfois à cause de son éloignement des cibles ; **le moyen est la référence**, le fort un contrôle secondaire.
- Le plan 28 avait déjà mesuré Péril 10 à 3 720 éliminations contre 3 950 sans Péril, avec l'ancien banc : durcir ne suffisait pas à améliorer le rendement. Ces chiffres du 7 octobre ne sont pas comparables directement à M1.

L'hypothèse à tester est double : les hausses unitaires dispersées donnent peu de contrôle au moment où le build décolle ; le couplage nombre/PV/plafond peut rendre un Péril élevé peu rentable. **Ajouter un bouton sans vérifier ce couplage ne répondrait qu'à la moitié du retour.** Une difficulté volontaire ne corrige pas à elle seule une difficulté normale trop faible.

## 4. Quatre pistes concrètes

Les valeurs sont des **points de départ de conception**, à mesurer avant adoption. Les hausses de Péril restent irréversibles pendant la run. Refuser est gratuit.

### A — Stèles à engagement choisi, recommandée en premier

À une stèle existante, choisir **+1, +3 ou +5 Péril**, puis elle s'éteint. Garder les dix placements ; pas de stèle réutilisable ni de nouveau lieu. Exemple : le joueur devient très fort à 12 min, vise la prochaine stèle et choisit +5 plutôt que d'espérer cinq rencontres.

L'offre montre le Péril actuel et final, les effets actuels et futurs, puis « Plus de créatures à éliminer ; PV et dégâts augmentés ». Si le nombre est saturé, **le dire explicitement** et ne pas annoncer de cibles supplémentaires. Reprendre l'écran de choix qui fige la run et le maintien d'interaction ; pas de touche supplémentaire.

**Contrepartie :** plus de cibles sur le trajet, donc éliminations, XP, Essence et Vestiges réellement gagnés au combat. Aucun paiement de stèle ; la densité est la récompense. Refuser ne consomme pas la stèle. Les créatures continuent de poursuivre le joueur qui repart : aucun cercle à défendre.

**Limite :** +5 n'est intéressant que si le débit suit et si les PV ne ralentissent pas trop les éliminations. La hausse ne doit pas réécrire instantanément les stats des créatures déjà présentes ; montée de pression avec les nouvelles apparitions. Les Failles, le Sifflet et les bannissements restent les autres façons d'accepter du Péril.

### B — Paliers avant la run, recommandés ensuite

Dans le choix de départ : **Péril 0 / 3 / 6 / 10**, sans deuxième stat ni arbre de mutateurs. Péril 0 est le défaut d'un profil neuf ; chaque départ demande un choix explicite, sans hériter silencieusement du palier précédent. Les stèles ajoutent leurs points à ce départ. Tous les paliers sont accessibles sans achat de puissance permanente.

**Pour le joueur :** obtenir immédiatement une run plus dense lorsqu'il connaît ses builds ; Péril 0 conserve l'ouverture graduelle de D1. Aucun quota d'armes ni baisse des stats du personnage.

**Contrepartie :** plus de créatures dès le départ, mêmes gains par élimination. Un seul classement hebdomadaire et tous les temps, avec éliminations brutes ; le Péril est un contexte de run, pas une pondération. Un palier risqué peut rapporter moins si le joueur meurt tôt : c'est le pari.

**Limite :** éviter quatre faux choix dont seul 10 serait rentable ; mesurer l'ouverture en méta neuve et intermédiaire. Les presets 3/6/10 peuvent changer d'après les résultats. Aucun déblocage de difficulté après vingt heures requis.

### C — Effacement volontairement accéléré, à concevoir avec le plan 42

Au départ, choix **normal / +25 % / +50 % de vitesse d'Effacement**. Ce réglage reste fixé pour la run ; pas de curseur manipulable en pause. Il agit sur le rythme, pas sur les dégâts du Néant. Affichage séparé du Péril : accélérer le monde et appeler des créatures ne signifient pas la même chose.

**Pour le joueur :** moins de temps pour ses détours ; les coffres à sauver et l'ancrage par la marche/les lieux prennent de la valeur. Il faut sortir des zones perdues, sans immobilisation ni destruction automatique de son équipement. Les malus aléatoires du plan 42 ne sont pas implémentés par ce lot.

**Contrepartie proposée :** la densité déjà liée à l'oubli offre davantage de cibles. Si les mesures montrent qu'elle sature, ajouter des **groupes finis poursuivant le joueur vers une zone encore praticable**, lors des passages de phase, une seule fois par cellule. Les gains sont leurs éliminations et leurs drops ordinaires ; aucune récompense pour le temps passé dans le Néant. Ne jamais rentabiliser le stationnement dans le vide.

**Limite :** « le monde disparaît plus vite » ne garantit ni fuite possible ni plus d'éliminations. Cette piste reste secondaire tant que le plan 42 n'a pas fixé le butin à sauver et la lisibilité. Tester seule puis avec le Péril retenu ; relever la date du late game et de l'Indicible. Les facteurs se composent explicitement avec l'Oubli du chemin, sans l'effacer ; borner le rythme total seulement sur preuve qu'une fuite devient impossible.

### D — Un défi volontaire qui accompagne la fuite

Exemple concret : à une stèle, alternative **« Appeler une poursuite »**. Pendant **60 s**, une réserve finie de **60 créatures supplémentaires** arrive par petits groupes derrière et sur les flancs. Nombres provisoires ; les groupes utilisent le budget commun, jamais une population hors plafond. Le joueur repart immédiatement ; aucun objectif de rester sur place.

**Contrat proposé :** parcourir **150 m de chemin pendant ces 60 s** et rester vivant. Le cap reste libre et les détours possibles. Distance et durée à caler sur la mobilité réelle des personnages ; le mouvement ne donne aucun bonus d'attaque. Un seul défi actif, une seule utilisation de la stèle, refus gratuit, pas de relance sur le même lieu.

**Contrepartie :** jusqu'à 60 cibles de plus, chacune à éliminer réellement ; les survivantes restent après la fin et poursuivent normalement. Réussir peut compter dans une **quête de déblocage volontaire** du plan 06 (§155), dont la récompense précise doit être proposée à part. Pas de prime d'éliminations, pas de coffre gratuit, pas de perte d'équipement à l'échec ; les récompenses déjà ramassées restent acquises.

**Limite :** si l'on ne fait que suivre le bot nomade, le défi peut devenir trivial. Vérifier que la foule crée un choix de trajet et une menace. Les défis « une seule arme » ou « sans objet » servent aussi les quêtes volontaires, mais n'ajoutent pas de créatures à eux seuls : ne pas les présenter comme un moyen rémunérateur de durcir le classement.

## 5. Recommandation et contrat de rendement

**A**, fondée sur le Péril existant, répond directement au retour sans réactiver les mutateurs V1. **D est également retenue par Raphaël** : son prototype n'est plus conditionné à un manque de décisions après A/B. B reçoit un accord de principe ; C aussi, avec sa dépendance à la conception de l'Effacement au plan 42. Commencer par la référence D0, puis avancer un lot à la fois ; l'ordre détaillé des suites n'a pas été demandé par Raphaël.

**Objet ou Écusson demandé :** le **Sifflet d'arbitre existant** couvre déjà ce besoin (+1 Péril par exemplaire, copies ordinaires illimitées, Péril conservé après oubli). Le garder comme moyen de risque porté dans le sac ; vérifier sa disponibilité et sa lisibilité lors de la recette. Aucun deuxième objet ou Écusson équivalent n'est nécessaire pour répondre à cette demande.

Avant leur livraison, régler le Péril pour que le nombre et le **flux de créatures rencontrées** portent la contrepartie. Préserver 500 comme plafond technique tant qu'un banc calme n'autorise pas autre chose. Si le plateau à 15 bloque le rendement, essayer séparément une hausse de la cible et du débit, sous le même budget simultané ; ajuster les PV après le nombre. Garder le Péril sans plafond ne justifie pas de promettre une densité sans plafond. Toute nouvelle courbe sort en JSON, avec lecture et contrôle explicites.

Contrat proposé à valider : sur un build capable d'absorber le palier, le palier supérieur doit offrir davantage de créatures et **davantage d'éliminations par minute pendant les minutes sans coup fatal**, avec une pression réelle supplémentaire. Si seuls les PV reçus montent et que les éliminations baissent, revoir la courbe plutôt que compenser par un multiplicateur ou une prime. Le rendement total d'une vraie run peut baisser avec une mort plus précoce : aucune garantie de gains pour tous les joueurs.

Un emballement de build reste possible et satisfaisant ; son joueur peut choisir d'en exploiter la puissance ou de pousser le risque. Péril 0 ne doit pas devenir une obligation de mourir, et un Péril élevé ne doit pas être une taxe imposée pour obtenir un classement normal.

## 6. Lots proposés, un seul à la fois après validation

| Lot | Travail autorisé après accord | Preuve de sortie |
|---|---|---|
| D0 — Référence du Péril actuel | Mesurer 0/3/6/10/15/20 ; puis contrôles méta, builds, personnages et profil fort | Moyennes **et étendues**, débit d'éliminations/pression/coût ; choisir les paliers à prototyper |
| D1 — Contrepartie du Péril | Seulement si D0 le justifie : courbe de nombre/flux/PV en données, sous plafond de coût ; clarifier les anciens commentaires | Avant/après identiques ; plus de cibles et de rendement sur un build qui tient, pression supérieure ; pas de régression de l'ouverture |
| D2 — Stèles A | +1/+3/+5, effets annoncés, refus, usage unique, hausse définitive | Régression des sources de Péril ; simulation d'activations en cours de run ; captures de choix et reprise mobile ; recette de Raphaël |
| D3 — Départ B | Presets validés au Hub, défaut 0, cumul avec toutes les sources | Méta neuve/milieu/complet ; départs et redémarrages cohérents ; captures du Hub, classement en éliminations brutes |
| D4 — Effacement C | Après validation du plan 42 : rythme, composition avec les Oublis, contrepartie de densité | Temps par phase, échappatoires, rendement, timing des crises/boss ; captures en mouvement |
| D5 — Défi D | Prototype unique de poursuite ; puis quête seulement après accord sur son déblocage | Menace et trajet réellement choisis ; pas de farming répété ; gains réels et coût mesurés ; recette humaine |

Pour chaque futur lot de code : `dotnet build` sans warning, suites concernées ; smoke si scènes/initialisation touchées ; captures pour tout changement visible en run et banc calme pour tout changement de coût. Cocher la roadmap V2 **uniquement après implémentation et vérification**, jamais à la validation de ce document. Reporter les réponses de Raphaël dans `DECISIONS.md` après réception.

D4/D5 ne sont pas inclus implicitement dans une validation de D0–D3. Le recalage des Souverains (§160), le retrait des rangs bruts du Campement et le scaling infini d'endgame gardent leurs propres lots.

## 7. Mesures prêtes, limites de l'outillage

Plan fourni : [`tools/measure_plans/difficulte-choisie-reference.txt`](../../tools/measure_plans/difficulte-choisie-reference.txt). Format existant : `nom | secondes | graines | profil | args`. **26 configurations × trois graines = 78 runs**, chacune de 30 min. Budget : 39 h de jeu simulé ; temps machine à chronométrer, sans reprendre automatiquement le rapport de M1.

- A : Péril 0/3/6/10/15/20, Traqueur moyen complet naturel — première passe de sélection.
- B : neuf/milieu, 0 contre 6 — ouverture et progression.
- C : cinq builds, 0 contre 6 — rendement et résistance au risque.
- D : Vagabond et Facteur milieu, 0 contre 6 — mêlée et personnage faible.
- E : Traqueur fort complet, 0 contre 6 — contrôle, sans supposer que fort tue mieux.

6 est le candidat intermédiaire de la **référence**, pas un résultat recommandé par mesure. Si A désigne un autre palier, adapter les contrôles avant leur lancement. On peut exécuter d'abord les seules lignes A dans une copie du plan hors dépôt pour éviter de calculer les 78 runs avant ce choix.

```bash
MEASURE_JOBS=2 MATRIX_JOBS=1 tools/measure_matrix.sh "$HOME/.cache/vestiges-difficulte-reference-20261013" tools/measure_plans/difficulte-choisie-reference.txt
python3 tools/summarize_matrix.py "$HOME/.cache/vestiges-difficulte-reference-20261013" > "$HOME/.cache/vestiges-difficulte-reference-20261013/synthese.md"
```

Choisir un dossier neuf à chaque passe. Conserver version du dépôt, différences locales, copie du plan et des fixtures de build avec les résultats. Les trois graines sont 7/42/1002, communes à la référence et aux variantes ; le même seed n'assure pas un replay identique (génération parallèle, rythme des images). **Trois graines minimum par configuration** ; si les étendues se recouvrent largement ou un résultat vient d'une seule graine, répéter les cases concernées avant de conclure.

Lire :

1. Éliminations totales et par minute, XP/Essence, nombre apparu et nombre proche, temps pour tuer, minutes sans coup fatal. Les totaux du bot qui se relève ne sont pas les gains d'une vraie run arrêtée à la première mort ; lire séparément l'heure du premier coup fatal.
2. PV perdus/min avant/après 15 min, coups fatals, puis source des dégâts : créatures ou Néant. Davantage de PV perdus ne prouve pas, seul, une difficulté intéressante.
3. Temps Effiloché/Effacé/Néant, distance, plus long creux, stèles vues et futures activations ; conserver la fuite praticable.
4. Souverains : temps pour tuer et DPS du joueur autour de leur apparition, en distinguant le recalage §160. Timing du late game, crises, Indicible ; **30 min ne valide pas l'endgame**, prolonger les finalistes à 45 min, trois graines minimum.
5. Plafond d'actifs atteint et coût au banc dense, calme : FPS et p99. Ne pas déduire les FPS d'une simulation headless ni mesurer le rendu pendant la matrice.

`summarize_matrix.py` affiche les étendues pour éliminations, niveaux et PV perdus, mais **seulement la moyenne** du temps Effacé/Néant et le maximum du creux ; sa sortie JSON ne conserve que des agrégats. Pour ces deux métriques, lire les trois `pace-<seed>.csv` et calculer moyenne/min/max par configuration ; compléter avec `density`, `trace`, `kills` et `decisions`. Ne pas présenter 3–7 % comme une étendue entre graines : c'est le constat transversal de M1.

**Ce que cette référence ne valide pas :** `--peril N` est injecté au départ ; le bot compte les stèles sans les viser, et peut encore gagner du Péril par les Failles. Les comparaisons concernent un Péril initial, pas un Péril fixe. Une activation à 12 min, une accélération d'Effacement et un défi mobile demandent un futur scénario de banc après accord. `--scaling` surcharge l'apparition, pas `erasure.json` ni `peril.json` ; ne pas inventer d'options déjà disponibles. L'outillage de ces scénarios est à prévoir en D2/D4/D5, sans le coder maintenant.

**État de cette livraison :** inventaire vérifié ; référence de mesure préparée, **non exécutée** ; aucune nouvelle mesure de rendement ni valeur équilibrée revendiquée. Aucun fichier de gameplay, sauvegarde ou case de roadmap modifié.

## 8. Questions initiales et réponse de Raphaël

1. **Premier périmètre :** A puis B (**recommandé**, contrôle immédiat du Péril et au départ) ; A seule (tout se décide en exploration) ; C d'abord (priorité à la pression du monde, dépend du plan 42).
2. **Stèles :** choix +1/+3/+5, usage unique et gains par les créatures (**recommandé**, conforme au §78) ; +1 fixe avec davantage de stèles (moins de réglage par rencontre) ; prime immédiate de butin contre Péril (révision explicite du §78, économie à reconcevoir avant code).
3. **Après les stèles/paliers :** C avec le plan 42, puis D seulement si besoin (**recommandé**, garde un nombre de systèmes limité) ; D avant C (poursuite courte et quête volontaire) ; s'arrêter à A/B et réévaluer après essais.

**Réponse du 13 octobre ([DECISIONS §162](DECISIONS.md)) :** « steles ca me va, défi volontaire aussi. il faut un objet ou un écusson qui permette d'augmenter le péril si cest pas déjà le cas. Le reste ca me pariat pas mal ».

- **Validés :** stèles A et défi volontaire D comme moyens de difficulté choisie.
- **Besoin d'équipement :** déjà couvert par le Sifflet d'arbitre ; informer Raphaël de son effet actuel, sans créer de doublon.
- **Accord de principe :** paliers de départ B et Effacement réglable C ; leurs détails restent liés aux mesures et au plan 42.
- **Non précisés séparément :** ordre des lots, montants exacts du Péril, durée/distance/population du défi, éventuelle quête de déblocage. Les valeurs du §4 restent des réglages proposés, à éprouver avant livraison.

Ce retour clôt les questions de direction de ce lot de conception. Aucun gameplay n'est livré par cette validation, aucune case de roadmap n'est cochée ; le plan de mesures reste préparé, non exécuté.
