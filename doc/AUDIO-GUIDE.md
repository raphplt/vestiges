# VESTIGES — Guide de production audio V2

25 septembre 2026 · Direction validée par Raphaël ; propositions musicales à écouter.

Références : [Stratégie V2 §21](VESTIGES-STRATEGIE-V2.md#21-sound-design-v2), [Bible §8](VESTIGES-BIBLE.md#8-direction-audio), [plan de production 15](plans/15-audio.md).

## 1. Direction

**Des musiques de jeu vidéo avec du rythme, de la mélodie et une progression musicale.** Le joueur doit avoir envie d'explorer et de combattre. La mélancolie, l'étrangeté et la distorsion donnent une identité à cette musique.

Les anciennes prescriptions de drones abstraits et leurs exclusions de styles sont supprimées. L'Effacement transforme une composition reconnaissable : notes qui s'effritent, échos fragmentés, instabilité légère de hauteur, timbres qui se dégradent. La mélodie et le rythme restent perceptibles. Les respirations se construisent par l'arrangement et la programmation en jeu.

Les tempos et instruments proposés ci-dessous sont des essais, pas des décisions de Raphaël. Une préférence sonore se valide à l'écoute, puis en contexte de gameplay.

## 2. Répartition du travail et budget

| Domaine | Production envisagée | Choix final |
|---|---|---|
| Bruitages concrets et ambiances | Recherche de sons gratuits enregistrés ; variantes et retouches | Raphaël |
| Effets abstraits, interface et créatures | Sources transformées, synthèse ; ElevenLabs en complément avec les crédits existants | Raphaël |
| Musique | Essais Google avec l'abonnement existant ; collaboration bénévole possible | Raphaël |
| Recherche, traçabilité et préparation | Agents : plusieurs candidats sourcés par effet, métadonnées, licences, préparation technique | Aucun choix artistique automatique |
| Intégration Godot | Après sélection, par lots ; vérification en jeu | Recette par Raphaël |

Aucun achat, abonnement supplémentaire ou recrutement rémunéré prévu. Un son gratuit doit aussi permettre l'usage dans un jeu commercial. Conserver la source, l'auteur, la licence exacte et l'attribution avec chaque candidat. Une licence inconnue reste un point à résoudre ; elle ne vaut pas autorisation.

Le forfait gratuit ElevenLabs n'inclut pas de licence commerciale. Vérifier la formule et le service utilisés au moment de la génération, notamment les fonctionnalités bêta : [documentation ElevenLabs](https://help.elevenlabs.io/hc/en-us/articles/13313564601361-Can-I-publish-the-content-I-generate-on-the-platform).

## 3. Essais Google : mode d'emploi

Utiliser l'outil de création musicale disponible dans le compte Google. Dans Gemini, choisir l'instrumental lorsque l'option est proposée. Les possibilités de durée, d'export et de réutilisation d'une référence dépendent de l'outil et de la formule ; aucun accès API n'est supposé.

Les prompts sont des briefs originaux de VESTIGES. Leur structure suit les paramètres décrits par Google (genre, tempo, instruments, dynamique), sans garantir que le modèle respectera chaque détail : [guide Lyria](https://deepmind.google/models/lyria/prompt-guide/), [aide Gemini](https://support.google.com/gemini/answer/16901237?hl=en).

Commencer par **deux versions du prompt exploration et deux du prompt combat**. Écouter sur la même séquence de jeu. Noter ce qui plaît et gêne : thème, énergie, instrumentation, distorsion, lassitude. Modifier une dimension à la fois. Les autres briefs servent ensuite à développer la direction retenue.

Chaque prompt est autonome et prêt à copier. Demander une boucle est une intention : le raccord réel sera contrôlé et, si nécessaire, monté après export. Une génération indépendante ne garantit pas de reprendre exactement le thème d'une autre.

### A — Exploration : avancer dans les vestiges

```text
Compose une musique instrumentale de jeu vidéo pour VESTIGES, un roguelite d'exploration et de combat dans des ruines envahies par la végétation. Le monde s'efface derrière un nomade qui continue d'avancer. Écris un thème mélodique court et mémorisable, mélancolique mais porté par la curiosité et l'envie de partir à l'aventure. Tempo autour de 100 BPM, mesure à quatre temps, groove régulier avec percussions sèches de bois, basse ronde et cordes pincées. Un piano légèrement patiné porte la mélodie, rejoint par un timbre électronique doux. Développe le thème en phrases et variations, avec une section plus légère puis un retour du motif. Quelques échos incomplets et fluctuations de timbre évoquent un souvenir abîmé, tout en gardant une mélodie claire et une pulsation solide. Arrangement aéré, adapté à une écoute répétée pendant le gameplay. Instrumental, sans voix. Entrée rapide dans le morceau et fin qui permet un retour naturel au début.
```

### B — Combat : puissance et mouvement

```text
Compose une musique instrumentale de combat pour un roguelite isométrique post-apocalyptique nommé VESTIGES. Le héros traverse des ruines, esquive et gagne en puissance. Donne au morceau une mélodie accrocheuse et un rythme moteur, autour de 126 BPM à quatre temps. Basse pulsée, batterie sèche, percussions métalliques accordées, motif de cordes pincées et lead électronique expressif. L'émotion associe détermination, plaisir du combat et mélancolie. Introduis le motif principal rapidement, développe-le avec un contrechant, puis allège brièvement l'arrangement avant son retour. Ajoute une saturation mesurée et des échos qui se fragmentent pour évoquer un monde qui s'oublie. Les attaques rythmiques restent nettes et la mélodie reste au premier plan. Garde de l'espace dans le mix pour les bruitages du jeu. Instrumental, sans voix, énergie régulière et structure adaptée à une boucle de gameplay.
```

### C — Résurgence : le monde se fracture

```text
Compose une musique instrumentale de crise pour VESTIGES. Une Résurgence déforme les ruines et libère une vague d'ennemis ; le joueur doit continuer d'avancer. Tempo autour de 138 BPM, rythme incisif et stable, basse énergique, percussions sèches de métal et de bois, motif mélodique bref et déterminé au synthétiseur et au piano. Développe ce motif avec des réponses et une harmonie tendue. Des échos incomplets, de légers glissements de hauteur et une saturation progressive donnent l'impression que la musique s'effrite. La pulsation et le thème restent faciles à suivre. Alterne deux degrés d'intensité pour soutenir une séquence de combat de 60 à 90 secondes. Entrée rapide, tension soutenue, sortie courte. Instrumental, sans voix, mix lisible pour un jeu d'action.
```

### D — Après la Résurgence : reprendre la route

```text
Compose une musique instrumentale de jeu vidéo pour le retour à l'exploration après un combat intense. Dans VESTIGES, le nomade a survécu et reprend sa route dans un monde beau mais fragile. Autour de 92 BPM, mélodie chaleureuse et douce-amère au piano, guitare pincée, basse légère et percussion discrète mais régulière. Construis une véritable phrase mélodique avec réponse, variation et retour. L'arrangement respire tout en conservant le mouvement. Une légère patine de bande et quelques queues de notes fragmentées rappellent l'Effacement. Émotion de soulagement, curiosité retrouvée et courage tranquille. Instrumental, sans voix, agréable sur plusieurs répétitions, avec une fin raccordable au début.
```

### E — Endgame : tenir encore

```text
Compose une musique instrumentale de fin de run pour VESTIGES, roguelite où un nomade devient très puissant tandis que la réalité disparaît autour de lui. Autour de 132 BPM, basse motrice, percussions précises, arpèges de cordes pincées, piano et lead électronique portant un thème ample et mémorisable. Associe l'énergie du combat à une beauté mélancolique : le joueur veut tenir encore. Développe le thème en plusieurs variations avec un passage moins dense puis un retour puissant. Le son se patine et les échos se désagrègent progressivement, mais la ligne mélodique et le rythme restent solides. Mix aéré pour laisser entendre les attaques. Instrumental, sans voix, intensité durable et structure cyclique adaptée au gameplay.
```

### F — Hub : la mémoire revient

```text
Compose le thème instrumental du Hub de VESTIGES, le lieu où un nomade revient entre deux expéditions dans un monde qui s'efface. Autour de 80 BPM, mélodie identifiable au piano, cordes pincées chaleureuses, basse douce et pulsation délicate. Le thème doit donner envie de revenir, avec un mélange de repos, de souvenirs et d'espoir discret. Forme musicale claire : thème, réponse, variation, reprise. Ajoute de légers échos et une patine de bande pour évoquer la mémoire. Le morceau reste vivant, mélodique et agréable à entendre pendant la navigation des menus. Instrumental, sans voix, transitions douces et fin raccordable au début.
```

### Variantes contrôlées

- Trop mou : renforcer la basse et les accents rythmiques, puis comparer au même volume.
- Trop générique : conserver le thème choisi et demander une instrumentation plus spécifique (cordes pincées, bois et métal accordés).
- Trop agressif : réduire la saturation et la densité, garder l'énergie du rythme.
- Trop répétitif : demander des réponses mélodiques et des variations d'arrangement.
- Trop étrange : garder la distorsion dans les échos et les fins de notes.

La mort et les ponctuations courtes seront produites à partir du vocabulaire musical retenu ; inutile de lancer des morceaux indépendants pour chaque récompense avant ce choix.

## 4. Recherche des effets sonores

Le [plan 15](plans/15-audio.md) organise un inventaire complet puis des lots de candidats. Pour chaque effet : besoin en jeu, sources, trois candidats si disponibles, variantes éventuelles, licence, traitement envisagé et décision de Raphaël. Une recherche infructueuse reste visible dans le registre.

Les catégories couvrent joueur, armes, impacts, bestiaire, progression, monde, événements, interface et méta. Les sons hérités de V1 ne sont pas des besoins de production ; un ancien nom peut toutefois être réutilisé par une action V2 et doit être vérifié avant classement.

## 5. Préparation et intégration

- Conserver les originaux et leurs métadonnées hors des assets de production tant que le choix n'est pas fait.
- Préparer des extraits comparables en volume ; distinguer les transformations proposées du fichier source.
- Prévoir plusieurs prises pour les effets fréquents ; trois candidats artistiques ne signifient pas trois variantes du même son.
- Contrôler bruit de fond, saturation, débuts/fins, durée et raccords de boucle.
- Partir de WAV/FLAC source lorsque disponible. Transformer un MP3 en WAV ne restaure pas sa qualité perdue.
- Préparer les formats Godot après sélection, en conservant les sources. Respecter les bus, pools et abonnements EventBus existants.
- Vérifier les choix dans une vraie run, particulièrement les sons répétés, les signaux de danger et les transitions musicales.

La présente révision documentaire n'intègre aucun nouveau son et ne valide aucune case de la roadmap.
