# VESTIGES — Décisions de Raphaël et arbitrages restants

Version 0.2 · 21 septembre 2026 · Référence de validation du dossier.

Ce registre intègre les retours structurants après la première lecture des plans. Les validations de direction ci-dessous sont acquises. Les nouvelles spécifications et mécaniques proposées restent identifiées comme propositions ; cette révision est documentaire, sans lancement de l'implémentation.

## 1. Décisions acquises

| Sujet | Décision de Raphaël | Application |
|---|---|---|
| Contrôle | Axes écran, diagonales normalisées, amplitude du stick préservée, contrôle clavier/manette | 01, socle validé |
| Mobilité | Viser une expérience très fluide et expressive ; approfondir dash/saut et/ou mobilités propres aux personnages | 01/06, détails proposés à examiner |
| Score | Visible et vivant pendant la run ; aucune progression vers le record ni annonce de dépassement pendant la partie | 02/04, record uniquement au bilan final |
| Juiciness | Trois intensités validées ; ambition très élevée sur mouvement, combat, collecte et récompenses | 02, intensité et qualité à tester |
| Mort | Refonte majeure du bilan ; inspiration Megabonk adaptée à Vestiges | 02/04 |
| Début de run | Actuellement trop facile, niveaux trop rapides, menace insuffisante | 03, ressenti utilisateur à traiter et mesurer |
| Mécaniques existantes | Peuvent être revues lorsqu'une amélioration est démontrée | Tous les plans, essais avant/après |
| Hub | Clarté maximale, peu de texte, détails à la demande | 04 |
| Collection | Page armes/objets accessible directement depuis le menu principal | 04/05 |
| Build | Quatre armes et quatre passifs conservés | 05 |
| Objets | Système distinct, capacité sans limite de design, exemplaires cumulables, raretés, déblocage partiel initial | 05 |
| Quêtes | Variées et indépendantes des Souvenirs ; déblocages directs | 06 et migration 05 |
| Casting | Plus diversifié, personnages éventuellement décalés, cohérence de l'univers conservée | 06/08 |
| Bestiaire | Ajouter quelques nouvelles créatures ; étudier des boss de familles | 07 |
| Terrain | Examiner et planifier les améliorations utiles des tiles et des systèmes voisins | Nouveau plan 10 |
| Innovation | Proposer des mécaniques originales précises | Nouveau plan 11, propositions non approuvées |
| Art | Pixel art accepté, suffisamment détaillé et uniforme entre les sprites ; choisir une méthode de production | 08, recommandation explicite |

« Les mobs avancent successivement » reste ambigu au moment de cette révision. Une clarification a été demandée : arrivée en file jugée problématique, introduction progressive des types, ou les deux. Le plan 07 sépare ces deux sujets ; aucun comportement n'est présenté comme une préférence confirmée.

## 2. Spécifications encore à examiner

| Décision | Recommandation dans les plans | Statut |
|---|---|---|
| Mobilité active | Une action de mobilité commune, dash de base ; sauts/glissades spécifiques à certains personnages | Proposition 01/06 |
| Invulnérabilité du dash | Comparer dash sans invulnérabilité et fenêtre courte, puis choisir avec le danger de début de run | Proposition de test |
| Objets | Rareté fixe par définition, compteur par ID, chaque exemplaire renforce un effet sans plafond d'exemplaires | Proposition technique 05 compatible avec la demande |
| Casting cible | Six identités pour le premier catalogue complet, production par lots ; deux nouvelles identités proposées | Proposition 06 |
| Boss | Une variante à comportement enrichi par famille retenue ; deux prototypes avant généralisation | Proposition 07 |
| Pixel art | Densité commune, tuiles 64×32 et joueur cible 48×64, conversion d'échelle commune à tester | Proposition 08 |
| Fabrication des sprites | Références dessinées/retouchées et animation maîtrisée ; automatisation pour conformité/export | Recommandation 08 |
| Innovation prioritaire | Rémanence offensive du déplacement, puis butin à sauver de l'Effacement | Prototypes proposés 11 |

Ni les seuils d'XP, ni les timings de mobilité, ni les chiffres des objets ne sont approuvés en tant qu'équilibrage final. Les choix antérieurs non explicitement arbitrés (personnage initial, kit de l'Éveillée, règles du classement) ne deviennent pas validés par défaut.

## 3. Contradictions retirées de la version 0.1

- Le record n'est plus un objectif affiché ni célébré pendant la run, y compris en pause.
- Les objets ne sont plus limités à un exemplaire ou trois stacks : les plafonds d'affichage/particules limitent uniquement la présentation.
- Les quêtes ne distribuent plus des Souvenirs comme étape obligatoire vers une arme.
- Les nouveaux personnages et ennemis font partie du périmètre ; l'audit de l'existant sert leur conception.
- Le quatrième personnage n'est plus conditionné à dix fragments de lore dans la cible proposée.
- Le terrain et les innovations possèdent des plans séparés pour être évalués et validés.

## 4. Validation et traçabilité

Les plans 01 (socle), 02 (avec correction record et extension bilan) et 03 (avec priorité à la difficulté initiale) disposent d'une direction validée. Les extensions détaillées rédigées en version 0.2 restent proposées. Raphaël pourra demander l'exécution d'un lot en citant son numéro et préciser les variantes retenues ; les décisions déjà acquises ne seront pas redemandées.

Les cases de la roadmap signifient « implémenté et vérifié ». Une validation de design n'en coche aucune.
