# VESTIGES — Système de Progression In-Run

> **Version :** 1.0
> **Date :** 1er mars 2026
> **Statut :** Note de design — à valider avant implémentation
> **Docs liés :** GDD v1.3, Architecture v2.1, weapons.json, perks.json

---

## Le problème

Le level-up est le moment de dopamine principal de la run. Il faut qu'il soit rapide, lisible et source de choix intéressants. Si on y met trop de choses (armes, perks de stats, perks mécaniques, upgrades d'armes), le choix devient confus et la montée en puissance devient illisible.

Le modèle Megabonk résout ça proprement : un seul écran de choix, deux types d'objets (armes et books), des slots limités, et les stats globales viennent d'ailleurs. On s'en inspire en l'ancrant dans l'univers de VESTIGES.

---

## Architecture du système

La progression in-run repose sur **trois sources distinctes** qui ne se mélangent jamais.

### Source 1 — Le Level-Up (Fragments de Mémoire)

C'est le cœur. À chaque level-up, le joueur choisit **1 Fragment parmi 3**. Un Fragment est soit une **Arme**, soit un **Souvenir Passif**.

**Armes** — Déterminent le pattern d'auto-attaque. Le joueur a **4 slots d'armes**. Prendre une arme déjà possédée l'améliore (niveau 1 → 2 → 3 → ... → max). Chaque niveau augmente ses stats (dégâts, vitesse, portée, effets) selon une courbe définie dans les données. Deux armes identiques ne sont jamais proposées si l'arme est au niveau max.

**Souvenirs Passifs** — Effets permanents qui tournent en arrière-plan. Le joueur a **4 slots de Souvenirs Passifs**. Reprendre un Souvenir déjà possédé l'améliore. Exemples : "Flamme Intérieure" (+% dégâts par stack), "Mémoire Vive" (+% vitesse d'attaque), "Ancrage" (+HP max), "Instinct" (+% vitesse de déplacement), "Résonance" (+% zone d'effet), "Siphon" (Essence drop+).

**Le choix fondamental à chaque level-up :** est-ce que je prends une nouvelle arme pour couvrir un nouveau pattern, est-ce que j'upgrade une arme existante pour la faire monter en puissance, est-ce que je prends un nouveau passif, ou est-ce que j'upgrade un passif existant ? Quatre options stratégiques, un seul écran simple.

**Quand les 8 slots sont pleins :** le joueur ne voit que des upgrades de ce qu'il possède déjà. La fin de run est consacrée à maxer son build, pas à découvrir du neuf. Ça crée un arc naturel : exploration en début de run, optimisation en fin de run.

### Source 2 — Le Monde (Perks de stats globaux)

Les perks de stats purs (les "+X% dégâts", "+Y HP", etc.) ne viennent **pas** du level-up. Ils viennent du monde :

- **Coffres** — Perk aléatoire parmi la pool commune. Les coffres épiques donnent des perks rares.
- **POI** — Certains points d'intérêt récompensent un perk spécifique (un autel donne toujours un perk d'Essence, une infirmerie donne toujours un perk de soin).
- **Événements** — Certains événements de jour/nuit donnent un perk en récompense.
- **Foyer** — Les améliorations du Foyer donnent des buffs passifs permanents pour la run (rayon de sécurité, regen dans la zone, etc.).

Ces perks s'empilent sans limite de slots — ce sont des stats globales qui se cumulent. Le joueur qui explore plus et qui ouvre plus de coffres est objectivement plus fort. Ça récompense la prise de risque (aller loin de la base pour un coffre épique) sans polluer l'écran de level-up.

### Source 3 — Les Évolutions (Fusions)

C'est le moment "oh putain" de la run.

Quand une **Arme** et un **Souvenir Passif** spécifiques atteignent **tous les deux leur niveau max**, ils peuvent fusionner en une **arme évoluée** — un Vestige. La fusion est proposée automatiquement au prochain coffre ouvert (pas au level-up, pour garder le level-up simple).

Le Vestige remplace l'arme ET le passif, libérant un slot de Souvenir Passif. Il est drastiquement plus puissant et a un effet unique.

Exemples de fusions :

| Arme (max) | Passif (max) | Vestige obtenu |
|---|---|---|
| Lame Ébréchée | Flamme Intérieure | **Lame qui se Souvient** — Frappe en arc de feu. Les ennemis tués restaurent du temps de jour. |
| Arc de Fortune | Instinct | **L'Arc du Dernier Chasseur** — Projectiles à tête chercheuse. Vitesse augmente avec la distance parcourue dans la run. |
| Fouet de Câbles | Résonance | **Le Fouet des Noms** — Frappe circulaire étendue. Chaque ennemi touché amplifie le prochain coup. |
| Boîte à Musique | Ancrage | **La Berceuse Éternelle** — 5 notes orbitales. Les ennemis dans le rayon ralentissent et subissent des dégâts croissants. |
| Arbalète Artisanale | Siphon | **Le Trait d'Oubli** — Carreau qui traverse tout et absorbe l'Essence de chaque ennemi percé. |

Le joueur ne connaît pas les recettes de fusion au départ. Il les découvre en jouant. Un Souvenir (lore) peut donner un indice ("Quand la flamme se souvient de la lame, les deux ne font plus qu'un"). Après la première découverte, la recette est visible dans le Hub (Chroniques).

C'est la raison principale du "just one more run" : le joueur veut retrouver la bonne combinaison, ou en découvrir de nouvelles.

---

## Narrativement, pourquoi ça marche

Le système est cohérent avec le lore :

- **Fragments de Mémoire** — Chaque level-up, le joueur "se souvient" de quelque chose. Une arme, un instinct, une capacité. Le monde s'efface mais lui se renforce parce qu'il se souvient.
- **Souvenirs Passifs** — Ce sont des souvenirs d'aptitudes. Se souvenir qu'on peut courir vite → on court vite. Se souvenir de la douleur → on résiste mieux.
- **Les Vestiges (fusions)** — Quand deux souvenirs convergent, ils forment quelque chose de plus réel, de plus ancré. Un Vestige c'est un souvenir si fort qu'il réécrit la réalité autour de lui.
- **Les perks du monde** — Le monde se souvient aussi. Un coffre intact dans une ruine contient un fragment de ce que le monde était. L'ouvrir c'est libérer ce souvenir.

---

## Résumé des slots et limites

| Système | Slots | Source | Améliorable |
|---|---|---|---|
| Armes | 4 max | Level-up (choix parmi 3) | Oui, en reprenant la même au level-up |
| Souvenirs Passifs | 4 max | Level-up (choix parmi 3) | Oui, en reprenant le même au level-up |
| Perks de stats | Illimité | Coffres, POI, événements, Foyer | Non (une instance par perk, stacks cumulatifs) |
| Vestiges (fusions) | Remplace 1 arme + 1 passif | Coffre une fois les conditions remplies | Non (déjà au max) |

---

## Courbe de puissance type

- **Niveau 1-5 :** Le joueur explore et découvre ses premières armes et passifs. Les slots se remplissent. Phase d'expérimentation.
- **Niveau 6-10 :** Les 8 slots sont presque pleins. Le joueur commence à upgrader. Les coffres donnent des perks de stats qui amplifient le build. Première nuit : le build est testé.
- **Niveau 11-15 :** Armes et passifs atteignent le max. Les premières fusions deviennent possibles. La puissance fait un bond significatif. Le joueur se sent puissant.
- **Niveau 16+ :** Le joueur est en mode optimisation. Il max tout, cherche les derniers coffres pour les perks rares, et gère la difficulté croissante des nuits. La question n'est plus "est-ce que je survis ?" mais "combien de temps encore ?".

---

## Impact sur l'implémentation

- Le pool de level-up tire depuis deux listes séparées (armes disponibles + passifs disponibles) et présente 3 options mélangées.
- Les armes ont une table de stats par niveau dans leur JSON (level 1 à max).
- Les Souvenirs Passifs ont une table d'effets par niveau.
- Les fusions sont une table simple : arme_id + passif_id → vestige_id.
- Les perks de stats sont un système indépendant avec leur propre pool, appliqués par les coffres/POI/événements.
- L'UI de level-up montre clairement le type (icône arme vs icône passif), le niveau actuel si c'est un upgrade, et le delta de stats.

---

## Ce qu'on ne fait PAS

- Pas de shop / marchand pour acheter des armes ou des passifs. Tout vient du level-up ou du monde.
- Pas de suppression d'arme ou de passif en cours de run. Ce qu'on prend, on le garde. Ça rend chaque choix définitif et lourd de conséquences.
- Pas de réorganisation de slots. L'ordre n'a pas d'importance.
- Pas de fusion forcée. Si le joueur ne veut pas fusionner, il garde son arme et son passif séparément.