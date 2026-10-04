# Plan 19 — Lore : une histoire qui se raconte par le jeu

Version 0.3 · 27 septembre 2026 · Statut : **N1 « La demande » retenue comme base ; réponses partielles à §10 en §11 ; travail en pause à la demande de Raphaël**. Ce document relève les incohérences et prépare les choix. Il n'écrit pas le lore, et il ne touche ni au code ni aux données.

Sources lues : [Bible](../VESTIGES-BIBLE.md) §1 à 3 et 6, [fiches de casting](06-fiches-casting.md), [Stratégie V2](../VESTIGES-STRATEGIE-V2.md) §2, 5, 8, 11, 14, 17 et 18, GDD §2, `data/weapons/weapons.json`, `data/souvenirs/*.json`, `data/perks/perks.json`, `data/quests/quests.json` et `assets/translations/translations.csv` (murmures des échos, écrans, Oublis, bénédictions).

## Étapes du plan

| Étape | Contenu                                                                                                             | Statut                       |
| ----- | ------------------------------------------------------------------------------------------------------------------- | ---------------------------- |
| **A** | Relevé des incohérences, audit du ton des textes, questions à trancher avec options                                 | **Livrée (ce document)**     |
| B     | Vérité cachée : ce qui s'est réellement passé, en une ou deux pages réservées à l'équipe                            | Après tes choix de l'étape A |
| C     | Canaux : où et comment chaque morceau de vérité se transmet dans le jeu (décor, mécanique, objet, écho, Hub, texte) | Après B                      |
| D     | Réécriture des textes existants, puis production des nouveaux                                                       | Après C, par lots            |

## 1. La direction que tu as donnée (27 septembre)

- L'histoire se raconte **à travers le jeu**, pas par des personnages qui l'exposent. Elle doit rester fine et ne jamais s'imposer.
- **Beaucoup de textes actuels sont trop directs** et sont à reprendre.
- Le ton mêle **poésie, drame et histoires cachées qui s'entremêlent**, et on doit s'y prendre vraiment.
- L'effet recherché : **des éléments présents dès le début qu'on ne comprend qu'après longtemps.** Le lore est un puzzle en fond, qui ne gêne jamais le jeu.
- Une run peut être **coupée ponctuellement** par un moment de lore, à condition que ce ne soit ni frustrant ni répétitif.
- Le jeu ne doit pas être flou et métaphorique en permanence. **Il raconte une vraie histoire, tangible.**
- **Une vraie révélation** serait idéale, à condition d'éviter un trope facile.
- Le lore déjà écrit dans les docs **n'est pas une référence absolue.**

Conséquence immédiate : la règle de la Bible « le jeu ne tranche jamais, les trois interprétations sont valides » (§3.1, constellation VI) ne s'accorde pas avec « une vraie histoire, tangible ». C'est la question 1 de §3.

## 2. Relevé des incohérences

### 2.1 Cosmologie : ce qui se contredit

| #   | Contradiction                                                                                                                                                                                                                                                                                                                | Où                                                                                                                                     |
| --- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------- |
| C1  | **Le Foyer** reste le centre de la cosmologie (« la lumière, c'est la mémoire »), alors qu'il n'existe plus en run. Une constellation entière (V) et deux Souvenirs parlent de Foyers dans le monde. Le Hub est devenu un feu de camp. Est-ce le même feu, un autre, ou un simple décor ?                                    | Bible §1 et §3 (V), §6.3 ; GDD l.191 (« le Foyer qui s'éteint ») ; `carnet_recherche_foyer`, `temoignage_irene` ; plan 04 (Hub = camp) |
| C2  | **Les créatures** mangent la mémoire (« elles cherchent à la consommer », Bible §1). Ailleurs, elles _sont faites_ de ce qui a été oublié : une Brute pleine de morceaux de voiture, une autre qui a poussé dans les étagères (§6.2, §3 IV). Elles lâchent pourtant de l'Essence, c'est-à-dire de la mémoire, à leur mort.   | Bible §1, §3 IV, §6.2 ; perk Siphon ; quête « Boire l'oubli »                                                                          |
| C3  | **Le joueur** est une conscience unique qui saute d'un fragment de réalité à l'autre (§2.4). Le casting, lui, propose six personnes avec un métier, une obsession et une phrase. Qui revient au Hub : une conscience ou six personnes ?                                                                                      | Bible §2.4 ; fiches 06 ; V2 §18 (« multivers qui s'oublie »)                                                                           |
| C4  | **L'échelle de temps.** Le monde est en ruine depuis longtemps : nature reconquise, autoroutes sous les canopées. L'Effacement, lui, a duré « des semaines, peut-être des mois », et les notes de survivants semblent récentes. Il n'y a aucun vivant sur la carte, hormis les échos.                                        | V2 §5 ; Bible §3 III                                                                                                                   |
| C5  | **Le jour et la nuit** ont disparu du jeu, mais le bestiaire les classe encore en « diurnes » et « nocturnes », et le dessin d'enfant parle des « gens de la nuit ».                                                                                                                                                         | Bible §6.2 ; `dessin_enfant`                                                                                                           |
| C6  | **L'ambiguïté sacrée** de la Bible (§2.1, §3.2 et VI : aucune vérité tranchée) contre ta demande d'une histoire tangible et d'une révélation.                                                                                                                                                                                | Bible §2.1, §3 VI                                                                                                                      |
| C7  | **La pluie.** Le Parapluie dit « il ne pleut plus depuis longtemps », la Pelle à neige « la neige ne tombe plus ». Les Craies disent pourtant « la pluie a lavé les dessins », et un murmure dit « il pleuvait, ce jour-là ». C'est peut-être un indice involontaire (voir R1 en §4).                                        | `weapons.json` ; `ECHO_WHISPER_04`                                                                                                     |
| C8  | **Ce qui rend le joueur plus fort.** « Se souvenir, c'est renforcer la réalité » (§1), mais la phrase d'origine parle de recettes de craft. La V2 fait de l'XP un « fragment de mémoire », les Souvenirs sont devenus des passifs au bilan, et l'Essence est la monnaie des Mémoriaux. Trois matières, aucune règle commune. | Bible §1 ; `LEVELUP_TITLE` ; `UI_END_PASSIVES` ; Mémoriaux                                                                             |
| C9  | **L'Indicible** « engloutit le Foyer », et le Rampant « surgit à l'intérieur de la base ». Il n'y a plus ni Foyer ni base.                                                                                                                                                                                                   | Bible §6.2                                                                                                                             |
| C10 | **Le Néant.** C'est « de l'absence, pas du noir » (§2.2) et une palette blanc-bleuté (V2). On ne sait pas ce qu'il est, seulement à quoi il ressemble. C'est la plus grande place libre pour une révélation.                                                                                                                 | Bible §2.2 ; V2 amendement                                                                                                             |

### 2.2 Restes V1 dans les textes affichés

`GAME_NIGHT_SURVIVED` et `HUD_NIGHT_SUMMARY` (« crise(s) survecue(s) », sans accent) ; `HUD_WOOD`, `HUD_STONE` et `GAME_BUILD` (« Construction ») ; la Bible §12.2 (« Déblocage : recette du four ») ; les perks de la Forgeuse déjà signalés en 06. Le plan 18 les couvre peut-être en partie. À vérifier avant l'étape D.

### 2.3 Collisions de vocabulaire

| Mot                              | Plusieurs sens aujourd'hui                                                                                                                                                                                                                                                                                                       |
| -------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Souvenir**                     | Fragment de lore du journal (`JOURNAL_TITLE`), passif affiché au bilan (`UI_END_PASSIVES`), méta-progression dans la Bible                                                                                                                                                                                                       |
| **Vestiges**                     | Titre du jeu, monnaie méta (`+{0} Vestiges`), objets qui tombent du ciel (« Un vestige tombe »)                                                                                                                                                                                                                                  |
| **Écho**                         | Perk « +1 projectile », silhouettes d'habitants (O6), Hub « L'Écho » (classements, V2 §18), Souvenir « Fragment d'écho »                                                                                                                                                                                                         |
| **Ombre**                        | Créature, et personnage de réserve du casting                                                                                                                                                                                                                                                                                    |
| **Hub / camp / Foyer / Passeur** | Le Hub s'appelle « camp » au bilan (`UI_END_HUB`), Foyer dans la V2 ; la pause parle de « l'état du Passeur », un terme qui n'apparaît nulle part ailleurs                                                                                                                                                                       |
| **Registres de noms**            | Bénédictions justes (« Chair qui se souvient », « Geste sûr ») à côté de perks génériques (« Berserker », « Kamikaze », « Vampirisme », « Canon de Verre », « Force Brute »), contraires à la charte des noms de la Bible §12. Même écart pour « Kills » et « Légendaire / Épique » à côté de « Ancien » et « Coffre ancestral » |

### 2.4 Ton : ce qui est trop direct

**Le défaut récurrent : la phrase de trop.** Un texte montre un détail concret, puis ajoute une phrase qui en tire la leçon. C'est cette dernière phrase qui alourdit. Le deuxième défaut est l'énoncé de la règle du monde, que la Bible interdisait déjà (§2.1).

| Texte                                                         | Passage en cause                                                                                                                                                                                 | Pourquoi                                                                                           |
| ------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | -------------------------------------------------------------------------------------------------- |
| Souvenir _Carnet de recherche_                                | « Il émet de la mémoire. Le rayon mesurable correspond exactement à la zone où les gens se souviennent… »                                                                                        | Énonce la thèse que la Bible demande de ne jamais écrire                                           |
| Souvenir _Note griffonnée_                                    | « Ce qu'on oublie disparaît. Ce dont on se souvient reste. Si c'est vrai, alors il suffit de se souvenir. »                                                                                      | Même chose, sous forme de leçon                                                                    |
| Souvenir _Enregistrement radio_                               | « La densité de mémoire collective semble… facteur de protection »                                                                                                                               | Exposé scientifique au lieu d'un témoignage                                                        |
| Souvenir _Connaissance du Vide_                               | « Les créatures ne VIENNENT pas du néant — elles SONT le néant… Comprendre le Vide, c'est pouvoir le trancher. »                                                                                 | Explication plus slogan d'arme                                                                     |
| Souvenirs _Billet_, _Liste_, _Photo_, _Dessin_, _Inscription_ | Dernières phrases : « Quelqu'un allait quelque part. », « Quelqu'un oubliait déjà. », « Comme si le souvenir lui-même hésitait. », « Un enfant a essayé de comprendre. », « Surtout à l'oubli. » | Commentaire d'un narrateur qui souligne l'émotion à la place du joueur                             |
| Souvenir _Journal intime_                                     | « Les murs de ma mémoire sont pleins de trous. »                                                                                                                                                 | Métaphore appuyée ; le reste du texte suffisait                                                    |
| Souvenir _Photo de laboratoire_                               | Toute la fiche (et la « dissociation » de la Bible)                                                                                                                                              | Trope du laboratoire et de l'amnésie ; voir §4                                                     |
| Constellations                                                | « Les Créatures : ce qui remplace le vide », « Le Joueur : Qui es-tu ? »                                                                                                                         | Des titres qui donnent la réponse ou qui cassent le quatrième mur                                  |
| Arme _Cloche d'école_                                         | « Ça suffit pour qu'elle existe. »                                                                                                                                                               | Énonce la règle                                                                                    |
| Arme _Scalpel_                                                | « Le métal s'en souvient — il veut encore réparer. »                                                                                                                                             | Explique l'effet de soin par le lore                                                               |
| Arme _Lampe à pétrole_                                        | « Elle se nourrit de ce qu'on se rappelle. Plus le souvenir est vif… »                                                                                                                           | Énonce la règle                                                                                    |
| Arme _Polaroïd_                                               | « Les créatures voient ce qui a été et ça les déchire. »                                                                                                                                         | Explique la mécanique                                                                              |
| Arme _Transistor_                                             | « …les créatures le comprennent, et ça les terrifie » (et la coquille « terrife »)                                                                                                               | Explique la mécanique                                                                              |
| Arme _Lentille de phare_                                      | « Les marins comptaient trop sur elle. »                                                                                                                                                         | Phrase obscure plus qu'expressive, pourtant précieuse (voir R1)                                    |
| Écrans                                                        | « La zone se souvient. Choisis une bénédiction. », « Le corps se souvient », « Le monde se fige, mais ta mémoire reste éveillée. », « L'Effacement rend ce qu'il a pris. Pas pour rien. »        | L'interface commente l'univers ; le Mémorial pourrait se passer de phrase                          |
| Murmures                                                      | « Ne m'oublie pas. », « Je connaissais ton nom… »                                                                                                                                                | Les deux seuls murmures qui parlent d'oubli ; les autres sont justes parce qu'ils n'en parlent pas |
| Fiches de casting                                             | Phrases-titres (« Tant que je marche, le chemin existe. », « Ma mer a été oubliée. Moi, non. »)                                                                                                  | Des devises. Utiles pour concevoir, mais à ne pas afficher telles quelles                          |

**Ce qui marche déjà et donne la mesure :** la plupart des murmures (« Encore cinq minutes… », « Le bus est encore en retard. », « Qui a pris mon parapluie ? »), la Faucille, le Parcmètre (« Le compteur ne descend plus. »), le Lance-billes, la Pelle à neige, la Gomme, le Trousseau, les Assiettes (« Il en manque une au service. ») et les noms des bénédictions. Tous reposent sur un objet précis et un geste humain, sans conclusion.

**Précision sur la finesse (proposition, à confirmer, voir §5) :** fin ne veut pas dire vague ni précieux. Un texte fin donne **un fait net et concret**, et c'est le lecteur qui fait le lien. On retire l'explication, jamais l'information.

Illustration de méthode seulement (ce ne sont pas des textes proposés) :

- Aujourd'hui : « Ligne 7, station Mémoria vers station Ancrage. Prix : 2.40. Validité : un aller. […] Quelqu'un a fait ce trajet. Quelqu'un allait quelque part. »
- Même méthode, sans commentaire : « Ligne 7. Aller simple. Composté deux fois. » Le lecteur se demande pourquoi deux fois.

## 3. Questions à trancher

Chaque question a deux à quatre options courtes. Une option marquée ⚠ entre en conflit avec ta direction ou avec un trope à éviter. Aucune n'est recommandée ici : tu choisis, puis l'étape B déroule.

**Q1. Quel est le statut de la vérité ?**

- **A.** Une seule vérité, entièrement reconstituable par un joueur attentif (esprit Obra Dinn, Outer Wilds).
- **B.** Une seule vérité au centre (quoi, qui, quand), avec des bords volontairement ouverts (pourquoi, qui a raison).
- **C.** ⚠ Plusieurs lectures toutes valides (Bible actuelle). Contredit « une histoire tangible ».

**Q2. Qu'est-ce que l'Effacement, à la source ?**

- **A.** Un phénomène sans auteur, avec une origine datée et localisée, comme une marée ou une érosion.
- **B.** Un oubli **choisi** par des gens, pour ne plus souffrir de quelque chose, et qui a débordé.
- **C.** ⚠ Un projet technique ou une expérience qui a mal tourné (trope du laboratoire).
- **D.** On n'explique que le mécanisme, jamais la cause.

**Q3. Qui sont les personnages jouables ?**

- **A.** Six personnes distinctes, chacune la **dernière à se souvenir d'une chose** que le monde a oubliée : la mer de la Scaphandrière, les destinataires du Facteur, l'auteur des empreintes du Traqueur, etc.
- **B.** Une seule conscience qui prend six formes (Bible §2.4).
- **C.** Six personnes **que le monde a oubliées** : vivantes, mais plus personne ne sait qu'elles existent.

**Q4. Que se passe-t-il à la mort et au retour ?**

- **A.** Pas de boucle dans la fiction : chaque run est une traversée différente, et le feu garde ce qui a été rapporté.
- **B.** Le personnage ne meurt pas : il perd le fil et revient au feu.
- **C.** Chaque run est **un récit raconté au feu** ; la mort, c'est le conteur qui perd le fil (voir R3).

**Q5. Qu'est-ce que le Hub, ce feu autour duquel veillent les personnages ?**

- **A.** Un vrai lieu du monde : le dernier campement, quelque part de précis.
- **B.** Un espace hors du monde, une conscience (Bible).
- **C.** Une veillée : l'endroit où l'on se raconte le monde pour qu'il tienne.

**Q6. Que sont les créatures ?** (tranche C2)

- **A.** Des mangeuses de mémoire.
- **B.** Des choses **faites** de ce qui a été oublié. Les abattre libère ce qu'elles retenaient, et c'est l'Essence.
- **C.** ⚠ Ce que deviennent les gens oubliés. Fort, mais c'est un trope connu (les ennemis étaient des humains).

**Q7. XP, Essence, Souvenirs : quelle matière ?** (tranche C8)

- **A.** Une seule matière, la mémoire, à trois densités.
- **B.** Deux matières : la mémoire **des autres**, qu'on prend pour devenir fort, et les souvenirs trouvés, qui racontent. Cela ouvre une question morale discrète : on devient fort en prenant ce dont d'autres se souvenaient.
- **C.** L'XP reste un simple système de jeu ; seuls l'Essence et les fragments ont un sens dans la fiction.

**Q8. Où et quand se passe le jeu ?**

- **A.** Une France reconnaissable, sans nom propre réel, des années 1990 à 2000 (parcmètre, cloueuse, Polaroïd, transistor, cloche d'école).
- **B.** Un monde presque le nôtre, avec des détails décalés (« sel de résonance », « station Mémoria »).
- **C.** Un lieu précis et inventé (une vallée, une ville, un nom) : tout le jeu se passe dans une seule région.

Sous-question sur le temps (tranche C4) : l'Effacement a-t-il eu lieu **il y a longtemps** (des décennies, ce qui colle avec la nature reconquise) ou **récemment** (des semaines, ce qui colle avec les notes de survivants) ?

**Q9. Qui « parle » dans les textes ?**

- **A.** Personne : uniquement des documents bruts, et des objets décrits au plus neutre, comme une fiche d'inventaire.
- **B.** Une voix unique et identifiable, par exemple quelqu'un qui a tout recensé. Cette voix devient elle-même une pièce du puzzle.
- **C.** Des documents bruts, plus une ligne du personnage joué, à la première personne, différente selon le personnage.

**Q10. Par où l'histoire passe-t-elle surtout ?**

- **A.** Le décor et les mécaniques d'abord, le texte en dernier recours.
- **B.** À égalité : textes courts et scènes de décor (plan 08 P4b) qui se répondent.
- **C.** Le journal de fragments d'abord (constellations).

**Q11. Quelle révélation ?** Choisir parmi les pistes R1 à R4 de §4, en combiner deux, ou n'en garder aucune.

**Q12. Moments qui interrompent la run ?**

- **A.** Aucun.
- **B.** Très rares : une fois par profil et par seuil, quelques secondes, sans texte (image, son, geste du personnage), jamais pendant une Résurgence.
- **C.** Des moments-seuils (première Faille, premier contact avec le Néant, fin du late game) avec une courte ligne.

**Q13. Existe-t-il une fin ?**

- **A.** Non : l'endgame reste infini, et la vérité se découvre à côté.
- **B.** Une fin atteignable une fois, après avoir compris, sans fermer le jeu ensuite.
- **C.** La fin narrative se joue au Hub (le feu), pas dans une run.

**Q14. Les six constellations du journal ?**

- **A.** Les garder comme catégories thématiques (l'Avant, les Signes…).
- **B.** Les remplacer par **des personnes ou des lieux** : chaque fil est une vie à reconstituer (l'écolière, le vieil homme, l'ouvrière des échos…).
- **C.** Aucun classement imposé : le journal se range selon les liens que le joueur découvre.

## 4. Propositions (non validées)

### Pistes de révélation

Tropes à éviter : tout était un rêve ; le joueur est mort ; le joueur a causé la catastrophe ; laboratoire ou simulation ; la boucle temporelle comme révélation en soi (tous les roguelites en ont une) ; le joueur est le monstre ; le monde intérieur d'un malade. La constellation VI actuelle touche déjà deux d'entre eux (laboratoire, amnésie).

**R1 — La mer oubliée.** _Cette piste est sortie des textes existants eux-mêmes._

- **La vérité :** la première chose que le monde a oubliée, c'est la mer. Il ne l'a pas perdue par accident : il a choisi de l'oublier, après ce qu'elle avait pris (un naufrage, une submersion, des gens qu'on n'a jamais retrouvés). Oublier la mer a entraîné le port, le phare, les noyés, les gens qui les aimaient, puis tout ce qui y était relié. Et tout est relié.
- **Ce qui est déjà devant le joueur sans qu'il le sache :**
  - Il n'y a aucune côte sur aucune carte, et aucun biome marin.
  - Il ne pleut plus et il ne neige plus : sans mer, plus d'eau qui monte au ciel. Le Parapluie et la Pelle à neige le disent déjà.
  - La Lentille de phare : « Le phare est oublié. »
  - Le « sel de résonance » sur la liste de courses.
  - La Scaphandrière _sans mer_, qui cherche « la côte qui manque sur toutes les cartes ».
  - Sa mécanique, surtout : **elle est la seule à pouvoir respirer dans l'Effacement.**
- **La révélation :** le Néant blanc-bleuté qu'on fuit depuis des dizaines d'heures n'est pas le vide. **C'est la mer qui revient**, sans souvenir, donc sans forme. Les Résurgences, pulsations toutes les trois à cinq minutes précédées d'un moment où « le monde retient son souffle », sont des **vagues** : le reflux, puis la déferlante. L'Effacement qui avance et recule est une **marée**. On l'a vue pendant tout le jeu sans jamais la reconnaître.
- **Pourquoi c'est tangible :** un événement précis, un lieu, des victimes, un choix humain compréhensible (ne plus souffrir). Aucun méchant.
- **Risques :** le drame d'un naufrage (des enfants ? une sortie scolaire ? La cloche d'école, l'arc du gymnase et l'écolière des échos s'y prêteraient) peut devenir lourd, et il faut doser. Autre risque : un joueur qui devine trop tôt. Il faudra garder les indices dispersés.
- **Variante française :** un **village englouti** sous un barrage (Tignes, Guerlédan). Même mécanique, eau douce, un clocher qui émerge ; il y a déjà une église dans les décors.

**R2 — L'absent.** Une même personne manque dans tous les documents :

- une place vide à table, l'assiette qui manque au service ;
- un visage flou sur la photo à deux ;
- une clé sans étiquette au Trousseau ;
- « Attends-moi au coin de la rue », « Qui a pris mon parapluie ? ».

On comprend tard que ce trou a une forme, et qu'elle est humaine. **Révélation :** l'Effacement a commencé par l'oubli d'**une seule personne**, puis s'est étendu à tout ce qui la touchait. Cette personne est peut-être le septième personnage « ??? » de la Bible, dont la place autour du feu est vide depuis le premier lancement. Piste compatible avec R1 : l'absent est le premier que la mer a pris.

**R3 — La veillée.** Chaque run est **un récit raconté au feu** par le personnage choisi. Le personnage quitte déjà le feu à pied pour entrer dans la run (plan 04). Le monde varie selon qui raconte : plus d'eau et d'échos marins pour la Scaphandrière, plus de boîtes aux lettres pour le Facteur. **Révélation :** les six racontent un monde qu'aucun d'eux n'a entièrement connu, et l'Effacement, c'est ce que plus personne autour du feu ne sait raconter. Effet « devant nous dès le début » : le joueur finit par remarquer que la carte change selon le personnage. Risque : proche du narrateur peu fiable, déjà vu (Bastion). Cette piste vaut surtout en combinaison.

**R4 — Les échos et les créatures.** L'écolière pâle qui se dissout à l'approche et le Rôdeur au cartable sont la même personne, à deux stades de l'oubli. ⚠ C'est fort mais c'est un trope connu, et cela rend chaque combat sinistre. Si on la garde, ce serait plutôt comme une lecture secondaire.

### Mécaniques qui racontent

Des idées compatibles avec plusieurs pistes, qui jouent sur « présent dès le début, compris tard » :

- **M1. Un son au Hub dès le premier lancement**, sous le crépitement du feu, presque inaudible : le ressac (si R1), ou une septième respiration (si R2). Son sens change quand on comprend.
- **M2. Des murmures qui se répondent d'une run à l'autre.** « Tu as fermé la porte ? » appelle, des runs plus tard et ailleurs, une autre voix : « Oui. À clé. » Une scène se reconstitue par morceaux.
- **M3. Le Trousseau comme index.** Chaque étiquette porte un nom. Ces noms reviennent ailleurs : sur un billet, dans un murmure, sur l'inventaire de l'arc du collège. On peut reconstituer l'immeuble et les liens entre les habitants.
- **M4. Ce que portent les créatures.** Rarement, à la mort d'une créature, un débris reconnaissable reste une seconde au sol : une casquette, une bille, une clé. Aucune explication.
- **M5. Une septième place au feu, vide.**
- **M6. Un lieu qui revient à chaque run** (l'arrêt de la ligne 7, par exemple), dont le panneau devient un peu plus lisible à mesure que le joueur trouve des fragments.
- **M7. Comprendre, plutôt que répéter** (esprit Outer Wilds, optionnel) : un nom ou un chiffre lu dans un fragment permet un geste ailleurs, par exemple au Mémorial ou dans un événement. Rien ne se débloque par la simple répétition.
- **M8. Des moments de lore brefs** (si Q12 B ou C) : le personnage s'arrête et regarde, trois secondes, sans texte, une fois dans la vie du profil.

## 5. Questions pour affiner ta vision

1. Quels jeux, films ou livres t'ont « eu » par leur façon de raconter ? Et lesquels t'ont semblé trop explicites, ou au contraire trop obscurs ?
2. Quand tu dis que la finesse ne doit pas être mal interprétée, quel écueil crains-tu le plus ? Le cryptique et le prétentieux ? Le littéraire et le précieux ? Le sentimental et le mièvre ? Autre chose ?
3. Jusqu'où peut aller le drame ? La mort d'enfants, un deuil collectif, une culpabilité partagée par toute une ville ?
4. Y a-t-il de la place pour l'humour ? La Bible voulait des gens « drôles, stupides, courageux ».
5. Les personnages jouables ont-ils une voix (une ligne, un nom propre, un passé qu'on découvre), ou restent-ils des silhouettes dans lesquelles le joueur se projette ?
6. Une histoire commune vue sous six angles, ou six histoires personnelles qui se croisent ?
7. La révélation doit-elle **changer la façon de jouer ensuite** (par exemple : on sait enfin ce qu'est le Néant, et la Scaphandrière prend un autre sens), ou seulement la façon de regarder ?

## 6. Suite proposée

Tu réponds à §3 (une lettre par question suffit) et à tout ou partie de §5. J'écris alors l'étape B : la vérité cachée en une ou deux pages, puis la liste des indices, chacun rattaché à son canal. Aucun texte en jeu n'est modifié avant la validation de B et C.

## 7. Réponses de Raphaël — 27 septembre

| Q   | Réponse                                                                                                                                                    | Ce qui en découle                                                                                                                                                                                            |
| --- | ---------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| 1   | **B**, « je préciserai »                                                                                                                                   | Une vérité centrale, des bords ouverts. La règle « trois interprétations valides » de la Bible tombe                                                                                                         |
| 2   | Entre **B et D** : « c'est la question centrale du jeu »                                                                                                   | Cause humaine (un oubli choisi) que le jeu ne dit jamais. Le joueur la déduit ou non. À affiner avec §8                                                                                                      |
| 3   | **C** : des personnes que le monde a oubliées. Pas de limite à six : **une dizaine**, avec un ou plusieurs personnages cachés ou des versions alternatives | Le casting de 06 reste valide, mais le cadre grandit. Rien ne change dans 06 pour l'instant                                                                                                                  |
| 4   | Une logique de boucle, « à penser en profondeur »                                                                                                          | §8 : questions qu'une boucle doit résoudre, réponses selon l'architecture                                                                                                                                    |
| 5   | Le Hub est un reste de la V1 ; son intérêt est à revoir ; s'il reste, **pas comme un élément de la fiction**                                               | Le feu n'a pas de sens dans le lore. L'accueil validé le 26 septembre reste un menu                                                                                                                          |
| 6   | **B** : les créatures sont faites de ce qui a été oublié                                                                                                   | Les abattre libère l'Essence                                                                                                                                                                                 |
| 7   | **B**, l'XP restant un simple système de jeu                                                                                                               | L'Essence est la mémoire des autres, qu'on prend ; les fragments sont les souvenirs trouvés, qui racontent. L'XP n'a pas de sens dans la fiction (le titre « Fragment de mémoire » du level-up est à revoir) |
| 8   | **B/C** : presque-nôtre et/ou lieu précis inventé, **sans noms de lieux liés à la mémoire**                                                                | « Station Mémoria », « station Ancrage » et leurs semblables disparaissent                                                                                                                                   |
| 9   | **A** plutôt : des documents bruts, personne ne parle                                                                                                      | Pas de narrateur ; descriptions d'objets neutres                                                                                                                                                             |
| 10  | **A** plutôt, « mais ça ne répond pas à tout »                                                                                                             | Le décor et les mécaniques d'abord ; le texte couvre ce qu'ils ne peuvent pas porter (noms, dates, voix)                                                                                                     |
| 11  | Indécis                                                                                                                                                    | §8 regroupe révélation, cause, boucle et fin en ensembles cohérents                                                                                                                                          |
| 12  | **B/C** avec parcimonie                                                                                                                                    | Rares moments-seuils, brefs, jamais répétés                                                                                                                                                                  |
| 13  | Indécidable en l'état                                                                                                                                      | Dépend de l'architecture choisie                                                                                                                                                                             |
| 14  | Question pas claire                                                                                                                                        | Reformulée en §8                                                                                                                                                                                             |

## 8. Architectures à comparer

Q2, Q4, Q11 et Q13 ne se tranchent pas séparément : la cause, la boucle, la révélation et la fin forment une seule mécanique. Voici donc des ensembles complets, déjà compatibles avec les réponses de §7. Ce ne sont pas encore des récits, seulement leur ossature.

### Ce qu'une boucle doit résoudre

1. **Pourquoi** on recommence : une règle du monde, pas « parce que c'est un roguelite ».
2. **Qui** recommence : la même personne, ou une autre.
3. **Le monde** est-il le même ? Pourquoi change-t-il de forme à chaque run (cartes procédurales, biomes en mosaïque) ?
4. **Ce qui persiste** d'une run à l'autre (méta-progression, fragments), et pourquoi.
5. Le personnage **le sait-il** ?
6. La boucle **peut-elle finir** ?

### Architecture 1 — La marée

- **Cause (B, jamais dite) :** une ville a choisi d'oublier la mer après un naufrage ou une submersion, pour ne plus en souffrir. Tout ce qui y tenait s'est défait : le port, le phare, les disparus, puis le reste.
- **Les personnages (C) :** des gens oubliés en même temps que la mer, parce qu'ils lui étaient liés : la Scaphandrière, le gardien du phare, le Facteur des îles, la veuve d'un marin, l'enfant d'un disparu… On peut aisément en faire dix.
- **La boucle :** chaque run est **une marée**. L'Effacement monte ; quand il te prend, la mer te **rend** au rivage. Elle rend toujours ce qu'elle prend, et le sous-titre actuel de la Faille le dit déjà presque. Le monde change à chaque run parce que la marée **recompose** la grève avec les débris qu'elle charrie : c'est la raison de la carte en mosaïque. Ce qui persiste, c'est ce que le personnage garde sur lui.
- **Révélation :** le Néant est la mer, les Résurgences sont des vagues, l'Effacement est une marée. Les indices sont déjà présents : aucune côte, plus de pluie, le phare oublié, la Scaphandrière qui respire dans l'Effacement.
- **Fin possible :** quelqu'un se souvient de la mer, de son nom ou de ses disparus. Dans les runs suivantes, **il pleut**.
- **Risques :** le thème marin peut étouffer la variété, et la mer se devine peut-être trop tôt.

### Architecture 2 — Ceux qu'on avait déjà oubliés

- **Cause (B, jamais dite) :** des gens ont commencé à oublier exprès, pour ne plus souffrir (un deuil, une honte, une guerre : la chose elle-même reste le centre du puzzle). L'oubli est devenu un soulagement, puis une habitude, puis une contagion. Ce qu'on oublie se défait.
- **Les personnages (C) : les seuls survivants de l'Effacement sont ceux que le monde avait oubliés avant lui.** Le vieil homme que personne ne visitait, le veilleur de nuit, le facteur d'une tournée où plus personne n'écrit, l'enfant que personne n'est venu chercher. L'Effacement défait ce dont on se souvenait et qu'on a oublié ; eux n'avaient plus rien à perdre.
- **Présent dès le début :**
  - Les personnages n'ont **pas de nom**, seulement une fonction (« le Facteur », « la Scaphandrière »). C'est déjà le cas dans le casting.
  - Le titre : **les vestiges, ce sont eux.**
  - Les échos d'habitants se dissolvent quand on approche. Ce ne sont pas eux les vivants : ce sont ceux qui ont oublié, et qui s'effacent.
- **La boucle :** le monde ne retient rien de ce que font les oubliés. **Ce dont personne ne se souvient ne peut pas finir** : quand un oublié tombe, le monde ne l'enregistre pas, et il recommence. Chaque run est recomposée à partir de ce qui tient encore. Ce qui persiste, c'est ce dont le personnage se souvient lui-même (fragments, méta-progression).
- **Révélation :** ce n'est pas un monde de survivants qui fuit une catastrophe. C'est la catastrophe qui a épargné **les seuls que personne ne regardait.**
- **Fin possible :** une fin par personnage, quand il retrouve son **nom** (quelqu'un l'avait écrit quelque part). Être à nouveau reconnu le rend de nouveau mortel, c'est-à-dire capable de finir, et aussi d'être effacé. Une fin douce-amère, sans combat final obligatoire.
- **Risques :** la mélancolie peut devenir pesante si elle n'est pas portée par des gens et des faits très concrets.

### Architecture 3 — Hybride

La cause vient de la 1 (la mer oubliée après un naufrage), les personnages et la boucle de la 2 : les survivants sont ceux qu'on avait déjà oubliés, et le naufrage est la chose que la ville a voulu oublier. C'est plus riche, mais il y a deux révélations à doser : ce qu'est le Néant, et qui sont les survivants.

### Couche facultative, compatible avec les trois : les carnets

Certains documents de « survivants » trouvés dans le monde ont été écrits par les personnages jouables eux-mêmes, lors de passages qu'ils ne se rappellent pas. On reconnaît une écriture, un dessin, un surnom. C'est proche du trope de la boucle ; à n'utiliser que comme détail.

### Q14 reformulée : le rangement du journal

Aujourd'hui, les Souvenirs trouvés en run se rangent dans le journal (Hub → Chroniques) sous six catégories fixes appelées **constellations** : l'Avant, les Signes, l'Effacement, les Créatures, le Foyer, le Joueur. La question porte sur ce rangement :

- **A.** Garder des catégories thématiques de ce genre, en les corrigeant (le Foyer et le Joueur n'ont plus de sens).
- **B.** Ranger par **personne ou lieu** : chaque fil est une vie à reconstituer (l'écolière, le gardien du phare…).
- **C.** Pas de rangement imposé : les fragments apparaissent en vrac et se relient à mesure que le joueur trouve des correspondances (même nom, même date, même objet).

## 9. Deuxième tour — 27 septembre

### Réponses de Raphaël

- **La marée** (architecture 1) : « un peu limitée narrativement, dans la portée et la puissance de son message ». Il demande d'explorer de toutes nouvelles pistes.
- **Q14** : délégué (« selon ce qui te semble le mieux »).
- **Boucle** : les personnages **savent** qu'ils recommencent, sur le principe. Les conséquences restent à examiner.
- **Structure** : **des histoires personnelles qui se croisent**, plutôt qu'une histoire commune vue sous plusieurs angles.

### Q14, choix délégué (provisoire)

Le journal se range **par personne** (option B), avec une case d'entrée « sans nom » (option C). Un fragment trouvé sans lien évident va dans « sans nom ». Il rejoint le fil d'une personne dès qu'un autre fragment partage avec lui un indice net : un nom, un objet, une adresse, une date. Le lien se fait automatiquement, sans interface de déduction à manipuler.

Raison : c'est la forme naturelle d'histoires personnelles qui se croisent, et le « sans nom » rend le puzzle visible sans rien expliquer. Les six constellations actuelles disparaissent.

### Une boucle consciente : ses conséquences

| Conséquence                                            | Détail                                                                                                                                                                                                                      |
| ------------------------------------------------------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Écarte le trope de l'amnésie**                       | Si le personnage sait qu'il recommence, il ne peut pas « découvrir qui il est » par surprise. La révélation doit venir d'ailleurs, ce qui l'oblige à être plus originale                                                    |
| **La méta-progression entre dans la fiction**          | Ce qui persiste, c'est ce que le personnage a appris. Il reste à dire pourquoi il se souvient alors que le monde, lui, oublie                                                                                               |
| **La mort perd sa terreur, gagne en usure**            | Il faut une raison intime de repartir. Chaque personnage a donc un but qui traverse les boucles (retrouver quelqu'un, remettre une lettre, retrouver un nom). Les histoires personnelles deviennent le moteur, pas un décor |
| **Le personnage peut laisser des traces**              | Pour lui-même ou pour les autres : une marque, une note, un objet posé, retrouvés dans une run suivante. C'est le lieu naturel des croisements entre personnages                                                            |
| **Le monde répète, lui**                               | Les échos redisent les mêmes phrases, les mêmes lieux reviennent recomposés. Le personnage le sait, le joueur aussi : la répétition devient un sujet et non un défaut, et une variation minime devient un événement         |
| **« Pourquoi on boucle » devient une question du jeu** | La réponse peut être la révélation elle-même                                                                                                                                                                                |
| **Le Hub peut disparaître de la fiction sans perte**   | Le retour se fait sans lieu intermédiaire : le personnage est simplement de nouveau en marche                                                                                                                               |

Trois dosages possibles :

- **(a) Implicite :** le personnage sait, mais rien ne le dit ; ça se voit à ses gestes et à ses traces.
- **(b) Des notes à soi-même,** trouvées dans le monde, de sa main, datées de passages précédents.
- **(c) Un prix :** chaque boucle lui coûte un peu de lui-même, sans jamais toucher à la progression du joueur (ses propres notes s'effritent, et on peut y remédier).

### Nouvelles pistes

**N1 — La demande.** _La piste la plus liée aux mécaniques existantes._

- **Le monde :** une région presque-nôtre. Il y existait une coutume, locale au départ : on pouvait **demander à oublier.** On déposait sa demande quelque part (le lieu reste à choisir : un bureau, une boîte, un lac, une chapelle), et c'était accordé. Personne ne sait comment (ta réponse D : le mécanisme n'est jamais expliqué).
- **Comment tout s'est défait :** on a d'abord demandé à oublier des chagrins, une dispute, un mort, une faute. Puis c'est devenu ordinaire, puis une commodité (oublier les vieux, les pauvres, les disparus), puis une peine (on faisait oublier quelqu'un au lieu de le juger). **L'Effacement n'a pas frappé le monde : c'est la somme de toutes les demandes.** Chacune est compréhensible, humaine. Il n'y a pas de méchant, seulement une responsabilité partagée.
- **Les personnages :** chacun a été oublié par une demande, la sienne ou celle d'un autre.
  - Le Facteur _sans destination_ transporte des demandes adressées à personne.
  - La Scaphandrière cherchait les corps d'un naufrage que tout un port a demandé à oublier, et elle a été oubliée avec eux.
  - Un enfant est sorti de la mémoire de ses parents quand ils ont demandé à oublier un autre deuil.
  - Un personnage caché : la première personne à avoir demandé, ou celle qui recevait les demandes.

  Les histoires se croisent par les demandes elles-mêmes : A a demandé à oublier B, et c'est C qui a porté la lettre.

- **Les créatures (Q6 B) :** elles sont faites de ce qu'on a demandé à oublier : chagrins, fautes, gens. **Ce qu'on refuse de porter revient.** Une Brute a une voiture dans le corps : quelqu'un a demandé à oublier un accident.
- **L'Essence (Q7 B) :** tu deviens fort en reprenant ce que les autres ont rejeté. Sans le savoir, le joueur porte les deuils des autres.
- **Présent dès le début :** la **Faille**. Elle offre de la puissance en échange d'un **Oubli**, et elle est toujours refusable. Le Mémorial, lui, fait payer pour « se rappeler ». Les deux existent déjà en jeu.
- **La révélation, en deux temps :**
  1. L'Effacement a été demandé, une demande à la fois.
  2. Plus tard, le joueur trouve un formulaire de demande dont la tournure est exactement celle de l'écran de la Faille. **Il fait la même chose depuis sa première run.** Aucun texte ne juge, et refuser a toujours été possible.
- **La boucle :** le personnage est oublié, donc le monde ne retient pas sa mort ; lui s'en souvient. Formule possible : « Le monde oublie ta mort. Toi, non. » Son but dans la boucle : que quelqu'un **reprenne** la demande qui l'a effacé.
- **Fins :**
  - Une par personnage, quand il est de nouveau nommé.
  - Peut-être une fin commune : à la dernière Faille, on te propose d'oublier la boucle elle-même. Refuser, c'est finir.
  - Pas de fin morale imposée.
- **Le message :** on ne se débarrasse pas de ce qu'on oublie. Oublier est humain, et le jeu ne le condamne jamais. Il montre seulement ce que ça coûte quand tout le monde le fait.
- **Risques :**
  - La proximité avec _Eternal Sunshine_ (l'effacement de souvenirs) : il faut garder la demande intime et presque folklorique, jamais clinique, et le « comment » jamais expliqué.
  - Des thèmes lourds (deuil, abandon), à tenir par des faits concrets et quelques respirations.

**N2 — La ligne de base.**

- **Le monde :** il n'y a pas eu de catastrophe unique. L'Effacement a toujours eu lieu, lentement : chaque génération reçoit un monde un peu plus pauvre et le croit complet. Il s'est accéléré ; le jeu ne dit pas pourquoi.
- **Les documents** viennent de plusieurs époques. Chacun pleure une perte que le suivant ne mentionne même plus : une grand-mère parle des martinets, sa petite-fille n'en parle jamais.
- **Les personnages :** ceux qui se souvenaient d'un état plus ancien, que personne n'a crus, et qui ont été oubliés pour ça.
- **Présent dès le début :** la palette « ancrée », que le joueur tient pour le monde intact.
- **La révélation :** tard, un Mémorial entièrement ravivé montre ce qu'aucune run n'a jamais montré (une couleur, un chant d'oiseau, une espèce). **Cent pour cent n'a jamais été cent pour cent.**
- **Le message :** on ne voit pas ce qu'on perd. Écologique et intime, sans slogan.
- **Risques :** moins de drame individuel ; une révélation plus contemplative que bouleversante ; une boucle moins évidente à justifier.

**N3 — Ce qui pousse.**

- **Le monde :** l'Effacement n'est pas une mort mais une **mue**. Le monde oublie pour que pousse autre chose. Les créatures sont ce qui vient après : maladroites, elles imitent ce qu'elles remplacent (la Bible les décrit déjà ainsi : une ombre « qui essaie de copier quelque chose qu'elle ne comprend pas »).
- **Les personnages :** la dernière mémoire de l'ancien monde, qui s'y accroche.
- **La révélation :** les échos d'habitants sont des créatures qui apprennent à être quelqu'un.
- **Le message :** laisser partir.
- **Risques :** ⚠ c'est le trope « c'était toi le méchant ». Il rend suspect le cœur du jeu, qui consiste à abattre des milliers de créatures, et le plaisir du combat en pâtit. À garder au plus comme une lecture secondaire.

**Et l'architecture 2** (§8, « ceux qu'on avait déjà oubliés ») reste compatible avec N1 : les survivants seraient les oubliés, et N1 dirait _comment_ on les a oubliés.

## 10. N1 retenue comme base — à peaufiner

Raphaël, 27 septembre : « N1 me semble le plus intéressant. C'est encore à peaufiner par contre mais bonne base pour avancer. »

### Ce qui tient déjà

Tout ce qui suit est acquis par les réponses de §7 et §9, et N1 s'y emboîte sans forcer :

| Élément | Dans N1 |
|---|---|
| Cause (Q2 B/D) | Des demandes d'oubli, humaines ; le mécanisme qui les exauce n'est jamais expliqué |
| Personnages (Q3 C) | Des gens oubliés par une demande, la leur ou celle d'un autre ; une dizaine, histoires personnelles qui se croisent |
| Créatures (Q6 B) | Faites de ce qu'on a demandé à oublier |
| Essence (Q7 B) | Ce que les autres ont rejeté, que le joueur reprend pour devenir fort ; l'XP reste un simple système de jeu |
| Documents bruts (Q9 A) | Les demandes elles-mêmes sont des documents : aucun narrateur n'est nécessaire |
| Journal par personne (Q14) | Chaque fil est une personne, et les demandes relient les fils |
| Boucle consciente | « Le monde oublie ta mort. Toi, non. » |

Les Oublis actuels des Failles vont déjà dans ce sens sans l'avoir cherché : « Oubli de la peur » fait rôder une élite de plus, et « Oubli des visages » habille les créatures de variantes. Ce qu'on oublie devient une créature ou une épreuve.

### Points à trancher pour écrire l'étape B

**P1. La forme de la demande.** C'est le geste que le joueur verra le plus souvent, en objets et en documents.
- **A. La lettre sans destinataire.** On écrit ce qu'on veut oublier, on l'adresse « à personne » et on la poste. Elle n'est jamais distribuée, et on oublie. Toutes ces lettres finissent au **service des rebuts** de la poste, qui existe vraiment en France (les lettres « tombées au rebut »). Le Facteur *sans destination* est celui qui les porte. Intime, concret, français, rien de clinique.
- **B. Un lieu naturel** (un lac, un puits, une source) où l'on jette ou dépose quelque chose. Plus folklorique, plus poétique, moins de documents.
- **C. Un guichet administratif**, avec formulaire, tampon et registre. Plus froid et plus politique.
- **D.** A au début, puis C quand la coutume devient une institution (voir P3).

**P2. L'histoire se lit dans la matière des documents** (proposition liée à P1 D). Les premières demandes sont des lettres manuscrites, hésitantes, raturées. Viennent ensuite des formulaires imprimés, des tampons, des numéros de dossier. Les derniers sont des formulaires pré-remplis au nom de quelqu'un d'autre. Le joueur reconstitue la dérive **au papier**, sans qu'aucun texte ne la raconte. C'est aussi par là qu'arrive la révélation de la Faille : le formulaire administratif reprend mot pour mot la tournure de l'écran de la Faille.

**P3. Jusqu'où va la dérive ?**
- **A. Intime seulement :** des chagrins, des deuils, des fautes personnelles.
- **B. Intime puis social :** la commodité (oublier les vieux, les pauvres, les disparus).
- **C. Intime, social, puis pénal :** on fait oublier quelqu'un au lieu de le juger. C'est la dimension la plus politique : qui décide de qui mérite qu'on se souvienne de lui ?

**P4. La chose au centre.** Il faut une demande plus grande que les autres, celle qui a fait basculer le monde, et que le joueur reconstitue en dernier.
- **A. Un deuil collectif :** tout un lieu demande à oublier une catastrophe (un naufrage, un incendie d'école). Émouvant, et personne n'est coupable.
- **B. Une faute collective :** une communauté demande à oublier **ce qu'elle a fait** à quelqu'un. Le deuil excuse, la faute non : la question morale devient réelle, sans méchant unique.
- **C. Les deux enchaînés :** une faute a causé un deuil, et on a demandé à oublier les deux ensemble.
- **D. Une seule personne :** la demande qui a fait basculer le monde visait un seul être, et c'est peut-être le personnage caché.

**P5. Ce que devient une personne qu'on a demandé à oublier.** C'est une contradiction à régler, et elle peut devenir un secret.
- **Proposition :** la personne elle-même continue d'exister, oubliée : c'est un personnage jouable. **Les souvenirs qu'on avait d'elle**, rejetés, deviennent de la matière à créatures. Quelque part rôde donc une créature faite de ce que les autres se rappelaient de toi.
- **Conséquence possible, comprise tard :** certains Souverains, les élites nommées qui existent déjà, portent un objet du personnage joué (la casquette du Facteur, le casque de la Scaphandrière). L'abattre, c'est reprendre sa propre mémoire. C'est le but caché de la boucle.
- **Autre option :** pas de lien direct entre une personne et une créature. Plus simple, moins vertigineux.

**P6. Pourquoi les oubliés bouclent, et pourquoi le monde change à chaque run.**
- **Proposition :** un oublié ne laisse de trace dans la mémoire de personne. Sa mort n'est enregistrée nulle part, donc elle ne compte pas, et il reprend la route. Lui s'en souvient. Le monde change parce qu'il n'est plus tenu que par ce que les derniers habitants retiennent encore (les échos qui se dissolvent) : chaque fois il est recomposé à partir d'un peu moins, d'où la mosaïque.
- **Le dosage de la conscience de la boucle** (question de §9, restée sans réponse) : (a) implicite, (b) notes laissées à soi-même, (c) un prix qui ne touche jamais la progression.

**P7. La Faille comme complicité.**
- **A.** Aucune mémoire des choix : la révélation se fait par la seule lecture.
- **B.** Le jeu retient discrètement combien de Failles ont été acceptées sur le profil, et quelques détails changent : une ligne d'un document, un murmure. Rien de punitif.
- **C.** Ces choix pèsent sur les fins (voir P9).

**P8. Le Mémorial**, qui existe déjà.
- **Proposition :** un Mémorial est le lieu où **quelqu'un a refusé de demander**, et a gardé son souvenir malgré tout. Ses trois éclats sont les trois choses qu'il tenait. C'est le pendant exact de la Faille : l'un a demandé, l'autre non.

**P9. Les fins.**
- **Fins personnelles :** un personnage est de nouveau nommé. Quelqu'un le reconnaît, ou il retrouve la demande qui l'a effacé.
- **Fin commune, trois idées :**
  - **A. Rendre les lettres :** atteindre le service des rebuts et renvoyer les lettres à leurs auteurs. Le monde revient, avec sa douleur.
  - **B. Refuser la dernière Faille,** qui propose d'oublier la boucle elle-même. Refuser, c'est pouvoir finir.
  - **C. Pas de fin commune :** seulement les fins personnelles, qui ensemble laissent deviner la chose au centre.

**P10. Le ton,** pour ne pas tomber dans *Eternal Sunshine*. Le mécanisme ne s'explique jamais, il n'y a ni machine ni clinique. La demande reste un geste presque banal, et c'est ce qui le rend terrible.

## 11. Réponses partielles à §10 — 27 septembre, pause

Raphaël s'arrête là : « j'ai un peu de mal à me projeter […] on reprendra ça plus tard. […] De base, c'est pas du tout un genre de jeu fait pour avoir des scénarios. »

| Point | Réponse | Suite |
|---|---|---|
| P1 | Il faut le plus cohérent et le plus « réaliste » : la lettre, puis le guichet (D) ? « Mais il y a un truc qui cloche, je ne saurais pas dire quoi. » | Piste d'explication ci-dessous ; reste ouvert |
| P2 | Oui, mais crainte que ces documents, montrés dès le début, expliquent le jeu immédiatement | Réponse ci-dessous ; à vérifier sur des exemples concrets |
| P3 | Délégué | **B, provisoire** : intime, puis sociale. La peine d'oubli n'apparaît qu'en trace (un ou deux documents), sans en faire un thème politique central. Raison : garder le drame humain et intime demandé au premier tour |
| P4 | Délégué | **C, provisoire** : une faute qui a causé un deuil, oubliés ensemble. Raison : le deuil rend la demande compréhensible, et la faute l'empêche d'être innocente |
| P5 | A, avec un point d'interrogation | Retenu comme hypothèse de travail |
| P6 | Justification et conscience de la boucle incertaines | Reste ouvert, y compris l'option où le personnage ne sait pas qu'il recommence |
| P7 | Question sur ce que sont les Failles | Rappel ci-dessous ; reste ouvert |
| P8 à P10 | Pas abordés | Restent ouverts |

**Ce qui cloche peut-être dans P1.** La prémisse est magique : une demande est exaucée sans que personne ne sache comment. Plus on la rend réaliste (un guichet, un tampon, un registre), plus on attend qu'une institution l'explique, et moins elle s'explique. Le réalisme appelle une explication que le jeu refuse de donner. Deux sorties possibles :
- Garder un geste intime et ancien, sans institution, où le réalisme tient aux gens et pas au mécanisme.
- Assumer une institution qui ne sait pas elle-même pourquoi ça marche, et qui se contente de gérer.

**Sur la crainte de P2.** Les premiers documents trouvés ne seraient pas lisibles comme des « demandes d'oubli ». Ce seraient des lettres de chagrin ordinaires, adressées à personne. Le joueur ne comprend ce qu'elles sont qu'en recoupant, beaucoup plus tard. Les formulaires n'arrivent que tard, et rarement : la rareté et l'ordre d'apparition se règlent en jeu. À valider sur des exemples concrets, pas en théorie.

**Rappel sur les Failles.** C'est une mécanique déjà en jeu (plan 17, vague 3, validée le 26 septembre), le miroir du Mémorial. Sur la carte, une Faille propose une amélioration Épique ou Légendaire. En échange, le joueur prend un **Oubli** (un malus de carte, par exemple « Oubli des repères » : plus de flèches vers les coffres) et un point de **Péril** (créatures plus fortes, meilleurs gains). On peut toujours refuser. Dans N1, accepter une Faille revient à faire une demande d'oubli pour devenir plus fort.

**Pour reprendre.** La difficulté à se projeter vient probablement de la méthode : on a posé des questions abstraites. La prochaine séance partira de matière concrète, pour réagir plutôt qu'imaginer. Par exemple : deux ou trois documents d'exemple dans le ton visé ; l'histoire d'un seul personnage de bout en bout, avec ses indices dans le jeu ; ou la liste de ce qu'un joueur verrait dans ses cinq premières runs.

## 12. Reprise sur du concret — 29 septembre

Matière à juger, **rien n'est validé**. Les textes ci-dessous sont des exemples de ton et de mécanique, pas des textes du jeu.

### 12.1 Ce qui clochait peut-être, et une correction

Dans N1, une demande d'oubli était *exaucée* : par qui ? L'histoire supposait un guichet magique sans guichetier. Plus on la rendait réaliste, plus ce trou se voyait.

**Proposition : personne n'exauce rien. L'oubli ne marche qu'à plusieurs.** Dans ce monde, une chose tient parce qu'on s'en souvient ; la Bible le disait déjà (« le pont tient parce que tout le monde le connaît »). Oublier seul ne change rien. Mais quand **assez de gens acceptent ensemble** de ne plus nommer quelque chose, elle se défait pour de bon. La « demande » n'est donc pas adressée à une puissance : c'est une démarche sociale, **obtenir des autres qu'ils oublient avec soi**.

Ce que ça règle :
- **Plus besoin d'une magie qui exauce.** La seule règle du monde est celle que le jeu montre depuis le début : ce qu'on n'entretient plus s'efface.
- **Le réalisme tient aux gens, pas au mécanisme.** On voit des rituels, des papiers, des pressions, des complicités ; jamais une machine.
- **La dérive devient naturelle.** Du deuil qu'une famille décide de taire, on passe à la rumeur qu'un village choisit d'ignorer, puis au nom qu'une commune raye de ses registres (la *damnatio memoriae* des Romains existait déjà).
- **Les personnages jouables s'expliquent**, avec une règle qu'on comprend tard : une chose oubliée disparaît, **une personne oubliée, non**, parce qu'elle, elle se souvient d'elle-même. Les oubliés sont ce qui reste quand tout le monde a lâché : les vestiges.
- **La boucle s'explique** par la même règle (P6). La mort d'un oublié n'a aucun témoin ; ce dont personne ne se souvient n'a pas eu lieu. Il reprend la route et, lui, se souvient. Il sait donc qu'il recommence (ta préférence).

### 12.2 Trois exemples de documents

Du plus tôt trouvé au plus tard. Le premier se lit comme un papier banal ; il ne prend son sens qu'une fois les autres trouvés.

**Carte postale, sans timbre** *(trouvable dès les premières runs)*
> Chers tous,
> On arrive jeudi par le car de 9 h. Ne venez pas nous chercher, on connaît le chemin.
> M.

*Au dos, d'une autre écriture, au crayon :* « Qui est M. ? »

**Faire-part, carton imprimé** *(milieu de parcours)*
> La famille Vasseur a la tristesse de vous faire part de l'oubli de
> **Lucien Vasseur**
> le 3 mars, en l'église Saint-Aubin.
> Merci de ne plus prononcer son nom.
> Ni fleurs ni visites.

Le rituel copie celui des obsèques, mais on enterre des vivants. Le joueur a peut-être déjà croisé « L. Vasseur » sur une boîte aux lettres, une étiquette du Trousseau ou le carter de la Cloueuse.

**Registre de la commune, page arrachée** *(tard, rare)*

| Nom | Motif | Témoins |
|---|---|---|
| Vasseur Lucien | à la demande de la famille | 3 signatures |
| Oriol Jeanne | trouble à l'ordre | 5 signatures |
| ~~illisible~~ | à sa propre demande | aucune — *refusé* |
| La nuit du 14 | — | la commune |

La dernière ligne n'est pas une personne. C'est la chose au centre (P4). Tout le village a signé pour l'oublier.

### 12.3 Un fil de bout en bout : le Facteur sans destination

Ce que le joueur rencontre, dans l'ordre, et par quel canal. Les numéros de run sont indicatifs.

| Quand | Canal | Ce qu'il voit | Ce qu'il comprend |
|---|---|---|---|
| Dès qu'il est débloqué | Mécanique | Le Facteur jette des lettres qui rebondissent « en cherchant une adresse » (fiche 06) | Rien : c'est une arme amusante |
| Runs 3–5 | Décor | Des boîtes aux lettres débordantes, des sacs postaux éventrés, « Retour à l'envoyeur, destinataire inconnu » | Un détail d'ambiance |
| Runs 8–15 | Documents | Des lettres jamais distribuées, toutes adressées à des noms qu'on retrouve sur des faire-part | Ce sont des lettres à des gens oubliés |
| Runs 15–25 | Écho | Un vieil homme pâle attend devant une boîte aux lettres, puis se dissout : « Rien pour moi aujourd'hui ? » | Quelqu'un attendait ; il oublie à son tour |
| Runs 25–40 | Croisement | Dans la sacoche du Facteur (fiche de pause, après un certain nombre de fragments), une lettre qu'il ne lance jamais. Adresse : le quai du port qu'aucune carte ne montre | Elle est pour quelqu'un qu'on connaît… |
| Tard | Croisement | La Scaphandrière cherche « la côte qui manque sur toutes les cartes ». Le port, c'est la nuit du 14 | La lettre du Facteur est pour elle. Les deux fils n'en font qu'un |
| Fin personnelle | Mécanique | Porter la lettre à la Scaphandrière. Chacun nomme l'autre : ils redeviennent capables de finir | Deux oubliés se sauvent en se souvenant l'un de l'autre |

Ce que ce fil montre du principe :
- **Aucune ligne n'explique.** Chaque canal apporte un fait ; c'est le recoupement qui raconte.
- **L'arme du début prend un sens tard.** Des lettres qui cherchent une adresse, c'est exactement ce qu'il est.
- **Les histoires se croisent vraiment** : une fin personnelle passe par un autre personnage, et on ne le devine pas.
- **Rien ne coupe la run**, sauf peut-être la fin personnelle (moment rare, Q12).

### 12.4 Questions

1. La correction de 12.1 (« l'oubli ne marche qu'à plusieurs ») règle-t-elle ton malaise, ou ce qui clochait était ailleurs ?
2. Le ton des trois documents te va-t-il ? Trop sombre, trop sec, trop littéraire, ou juste ?
3. Le fil du Facteur te donne-t-il l'effet « présent dès le début, compris très tard » que tu cherchais ?
4. Une fin personnelle qui passe par un autre personnage (se nommer l'un l'autre) : est-ce la bonne forme de fin, ou faut-il qu'elle soit possible seul ?

## 13. Script global — 29 septembre

Retour de Raphaël sur §12 : la correction « l'oubli ne marche qu'à plusieurs » est « mieux, à la fois plus poétique et plausible » ; le ton des documents est « top » ; le fil du Facteur est « bien ». Direction générale ajoutée : une histoire **mémorable**, qu'on peut manquer si l'on ne cherche pas, mais parfaitement ficelée, touchante, profonde, avec plusieurs niveaux de lecture. Il demande une version finale rapide du lore, « une sorte de script global et détaillé », rédigée en auteur, pour pouvoir ensuite se concentrer sur le jeu.

**Livré : [VESTIGES-LORE.md](../VESTIGES-LORE.md)**, version 1.0, à relire. Contenu :
- résumé en une page ;
- règles du monde ;
- lieu (Vaulme, provisoire) et biomes ;
- chronologie ;
- la nuit du 14 novembre heure par heure ;
- dix personnages : les six du casting et quatre ajouts (le Sonneur, l'Écolière, la Photographe, la Veilleuse en personnage caché), plus une version alternative (l'Enfant du Bas-Port) ;
- les figures non jouables ;
- le sens de chaque système du jeu ;
- les niveaux de lecture et les paliers de découverte ;
- les fins personnelles, la vraie fin, l'après-fin ;
- les règles d'écriture, ce qui reste ouvert, les conséquences pour le jeu, les trente et un noms.

Les choix d'auteur non validés y sont marqués « à valider ».

## 14. Script v1.1 validé comme base — 4 octobre

Réponses de Raphaël ([DECISIONS §62](DECISIONS.md)) :

| Point | Décision |
|---|---|
| Script v1.1 | **Validé comme base** ; il remplace la partie I de la Bible |
| Personnages ajoutés | Les quatre (Sonneur, Écolière, Photographe, Veilleuse cachée) et l'Enfant du Bas-Port sont gardés |
| Fin personnelle | À la prochaine mort, comme écrit en §9.1 du script |
| Objets du lore | Clé verte et un objet par fin personnelle, gardés |
| La Montée | Tracé discret dans chaque carte (rue en pente, pavée, barrière au bout), à produire au plan 22 |
| La Barrière | Boss intermédiaire gardé, à concevoir au plan 07 |
| P7, Failles | Le profil compte les Failles acceptées ; quelques détails changent (ligne de document, murmure), sans punition ni effet sur les fins |

**Restent ouverts :** le nom de Vaulme (provisoire), les trente et un noms, la mécanique de déclenchement des fins personnelles côté jeu (§9.1 du script). La production (documents, scènes, objets, Montée) n'est pas engagée : elle passera par des lots à proposer.

## 15. Lots de production proposés — 4 octobre (à valider)

Demandé au §64 : proposer les lots qui font passer le script v1.1 dans le jeu, avec les trois points restés ouverts en options. Rien n'est engagé.

**Ce qui existe aujourd'hui :**
- 19 Souvenirs (`data/souvenirs/souvenirs.json`) et 6 constellations ;
- 12 murmures d'écho ;
- 11 éléments de lore dans le monde (`scripts/World/Lore` : porte sans mur, pas interrompus, horloge folle…), dont plusieurs désactivés ;
- des coffres de lore (`chest_lore.json`) et un journal ;
- trois personnages jouables (Vagabond, Forgeuse, Traqueur) ; le Facteur, la Scaphandrière et les autres ne sont pas encore intégrés.

### 15.1 Les trois points ouverts

| Point | Options | Recommandation |
|---|---|---|
| **Nom de Vaulme** | A. Garder Vaulme. B. Un autre nom court, sans pays marqué (à proposer en planche de cinq). C. Ne jamais nommer la ville, seulement ses quartiers (Haute-Ville, Bas-Port). | **A** : déjà dans tout le script ; sonne vrai sans dire le pays. C reste possible plus tard sans rien casser. |
| **Les trente et un noms** | A. Valider la liste de l'annexe telle quelle. B. Tu retouches certains noms. C. Je propose une variante. | **A**, en gardant le droit de retoucher un nom quand son document est écrit. |
| **Déclenchement d'une fin personnelle** | A. **Un fil et un geste** : le profil compte les pièces du fil du personnage (documents, objets, lieux), et la fin s'arme quand le fil est complet et qu'en run le personnage atteint son lieu (porte, barrière, quai). La scène joue à la prochaine mort, comme validé. B. **Collection seule** : le fil complet suffit, sans geste en run. C. **Quête** : une quête par personnage dans le système existant (`QuestManager`), avec étapes visibles. | **A** : la fin se mérite en jouant, sans afficher de liste à cocher. B est plus simple, mais la fin tombe hors de toute action ; C montre trop le fil, contre la règle « on peut la manquer ». |

### 15.2 Lots

| Lot | Contenu | Dépend de | Vérification |
|---|---|---|---|
| **L1 — Ton des textes existants** | Réécrire selon §10 du script, sans changer de mécanique : les 19 Souvenirs, les descriptions d'armes qui énoncent la règle (Cloche, Scalpel, Lampe, Polaroïd, Transistor), les deux murmures sur l'oubli, les textes d'interface listés au §12 du script ; retirer les constellations et les restes V1 (Foyer, laboratoire, Mémoria). FR et EN. | Rien | Clés de traduction complètes ; captures Collection, journal, choix, bilan regardées |
| **L2 — Documents** | Un catalogue de documents (`data/lore/documents.json` : type, texte FR/EN, personnages liés, biome ou lieu de dépôt, palier). Dépôt en run par les coffres de lore et les petits lieux existants. Le journal se range par personne, avec la case « sans nom ». Ligne « Retrouvé » discrète au bilan. Premier contenu : les documents du fil du Vagabond. | L1 pour le ton | Fixtures de catalogue ; un document trouvé en run, rangé, relu au bilan (captures) |
| **L3 — Première fin personnelle : le Vagabond** | Tranche verticale avec un personnage jouable. Son fil : le bonnet trop petit, la photo aux deux mains d'enfant, la porte verte sans mur (élément de lore existant), avec le geste de l'option A. Puis la scène sans texte au bilan, la ligne « Élie », et l'écran de mort en une ligne après la nomination. La Veilleuse est débloquée comme personnage à venir, sans être jouable. | L2, réponse au point « déclenchement » | Profil de test qui arme la fin ; capture de la scène et de l'écran de mort ; aucune fin armée par erreur |
| **L4 — Fins du Traqueur et de la Forgeuse** | Mêmes outils que L3. Traqueur : les pas interrompus, la lanterne. Forgeuse : la chaîne brisée. Les deux se croisent (Élie nomme Julien, Julien nomme Odette). Les objets de fin rejoignent le système d'objets (plan 21), un par fin. | L3 ; la Barrière (plan 07) pour la Forgeuse | Idem L3, et ordre des fins respecté |
| **L5 — Le monde qui raconte** | Réactiver les éléments de lore éteints dans cette lecture. Ajouter la Montée, tracé discret dans chaque carte (plan 22), et les quais du marais. Les signes pendant la run restent sans arrêt ni écran (son de Faille, silhouette). | L2 | Captures de carte et de run ; coût mesuré si des décors s'ajoutent |
| **L6 — Personnages ajoutés** | Fiches au plan 06 pour le Sonneur, l'Écolière, la Photographe, la Veilleuse et l'Enfant du Bas-Port ; sprites au plan 25. Leurs fils et leurs fins suivent le modèle de L3. | Casting et sprites | Selon les plans 06 et 25 |
| **L7 — Vraie fin et après-fin** | La dernière Faille face à l'Indicible, les trente et un noms, la porte verte sur la Montée, la pluie ; puis la pluie et la mer dans les runs, l'Enfant du Bas-Port et la Clé verte. | L3 à L6, l'Indicible repris (plan 07) | Profil de test complet ; captures ; écoute de la pluie |

**Ordre recommandé : L1, puis L2 et L3 ensemble** (une première fin complète, jouable avec un personnage déjà en jeu), puis L4 et L5. L6 et L7 viennent avec le casting et la reprise de l'Indicible.
