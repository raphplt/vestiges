# Menus Pierre et Effacement — recette du 11 octobre 2026

Branche `codex/refonte-menus`, worktree isolé. Suite des lots P1–P3 livrés dans `c5d77196` ; [plan 04 §9](../../plans/04-interfaces-et-hub.md#9-achèvement-de-tous-les-menus--11-octobre-2026), lots P4–P7. L'accueil garde son camp. Cette passe termine les interfaces de choix, le butin, le journal et le bilan, ainsi que les rencontres qui utilisent le même écran de choix.

## Vérifications automatisées

- **Validation générale : 15/15 suites**, aucune source modifiée pendant l'exécution. Smoke, navigation du Hub, choix, art UI, acquisition et contrats des Réminiscences, objets, armes, sauvegardes, trace de run, remappage, musique, quêtes, chargement et mode dev. [Résultats](validation-generale.json), [build](build-general.log).
- **Contrôle final après les derniers ajustements visuels et de focus : 5/5**, aucune source modifiée. Smoke, navigation, choix/journal/butin, art UI et remappage. [Résultats](validation-finale.json), [build final : 0 warning, 0 erreur](build-final.log).
- [41 contrôles des choix, du journal et du butin](choix-journal-butin.log), dont première validation sans gain, réouverture, attente de lecture, confirmation unique, fragment inconnu sans spoiler et restitution de la pause.
- [16 contrôles de navigation du Hub](navigation-hub.log). [Art UI](art-final.log), [smoke](smoke-final.log), [remappage](input-final.log).

Commande de la validation générale :

```sh
tools/validate.sh <dossier-neuf> smoke menu_navigation choice_screen ui_art perk_acquisition perk_contracts objects weapons saves run_trace input_remap music quests loading dev_mode
```

Le contrôle final reprend `smoke menu_navigation choice_screen ui_art input_remap`. Les preuves générales précèdent les derniers ajustements de composition ; le contrôle final porte sur les sources livrées.

## Parcours dans Main et captures examinées

Les [huit scénarios de recette](captures.json) ont tous terminé sans échec, puis les [cinq reprises visuelles](captures-vues-finales.json) également. Fenêtres Godot réelles en GL Compatibility, seed `221092026`, profils de test isolés. Les dimensions des PNG archivés ont été contrôlées.

| Parcours | Résultat vérifié | Preuves |
|---|---|---|
| Montée de niveau | Raretés, paliers, Réminiscences, offres et colonnes lisibles ; 10 contrôles de relance/bannissement/passage et file de cinq niveaux sans reprise prématurée. | [1080p](niveau-1080.png), [rendu](levelup.log), [cascade](cascade.log) |
| Butin | Coffre, révélation, lecture sans fermeture automatique, confirmation ; galerie des gains de statistiques, Essence, XP, objet et Souvenir. Fenêtre courte avec un gain, défilement des grandes listes. | [720p, texte 130 %](butin-720.png), [un gain](butin-court-720.png), [log](loot.log) |
| Journal | Six constellations, fragments connus/inconnus, réouverture, B/J ; profil neuf 0/19 sans révéler les textes. | [profil dev 720p](journal-720.png), [profil neuf 720p, texte 130 %](journal-neuf-720.png), [log du profil neuf](journal.log) |
| Pause | Entrée Journal atteignable au focus ; B revient à la pause ; paramètres ouverts/fermés, abandon annulé ; quatre armes, six objets, neuf Oublis ; défilement de la bonne colonne. | [journal depuis la pause, 720p/130 %](pause-journal-720.png), [log](pause.log) |
| Bilan | Mort réelle après combat ; Expédition/Équipement par A/B ; longues listes de gains ; Rejouer → sélection du Vagabond à la place du Traqueur → Main jouable. | [équipement 1080p/130 %](bilan-dense-1080.png), [suite des gains](bilan-gains-1080.png), [log](bilan.log) |
| Mémorial | Éveil, bénédictions, services, choix et reprise ; petite fenêtre pour un service. | [720p/130 %](memorial-court-720.png), [log](memorial.log) |
| Faille | Contreparties visibles, choix effectif : Péril 0→1, Oubli 0→1 puis 1→0 au Mémorial. Nouvelle capture après le dernier ajustement de hauteur : les trois offres et leur ligne Péril sont visibles. | [1080p final](faille-1080.png), [log final](rift-final.log) |
| Atelier | Trempe 5→4, arme 1→2, trois statistiques forgées, nouvelle trempe rare et Essence. | [log](workshop.log) |

La dernière modification ajoute 40 px à la hauteur calculée du panneau de rencontre (plafond 920 px) ; les captures Mémorial/Atelier de la reprise précédente montrent une fenêtre légèrement plus courte. La Faille a été recapturée après cet ajustement, puis les cinq suites finales ont passé.

## Vérification avant PR, après synchronisation avec main

`origin/main` (`6a353705`) a été intégré à la branche de refonte. Quatre conflits documentaires ont été résolus en conservant les autres chantiers ; les décisions de cette refonte portent désormais les numéros §114–115. Aucun fichier de code n'était en conflit.

**16/16 suites vertes**, aucune source modifiée pendant l'exécution, build **0 warning, 0 erreur** : les quinze suites générales ci-dessus et la suite `audio`, ajoutée pour contrôler les évolutions du mix reçues de main. [Résultats](pr-validation.json), [build](pr-build.log), [audio](pr-audio.log). Ces résultats portent sur la version proposée en PR ; les captures documentent les menus avant cette synchronisation.

## Portée de la recette

Les entrées clavier/manette sont injectées dans Godot ; aucune manette physique n'a été essayée. Les captures utilisent des états de test pour exercer les contenus rares ou longs. `--bilan-rich` ajoute **neuf annonces de quête fictives au seul affichage**, et quatre Réminiscences : il ne prétend pas que ces neuf quêtes ont été accomplies pendant les 25 secondes de combat. La mort, les statistiques de combat et la relance proviennent bien de Main. Le réglage de banc `--menu-font-scale 1.3` agrandit les rôles typographiques du processus, sans changer les préférences sauvegardées.

Les avertissements connus de DLL Steam absente, audio factice headless et libération de ressources à la fermeture sont distingués des échecs par les lanceurs du dépôt. Aucune mesure de FPS ni recette intégrale de traduction n'est revendiquée. Cette livraison établit l'implémentation et les vérifications fonctionnelles/visuelles ; elle ne présume pas de l'approbation artistique de Raphaël. Aucune fusion ni publication vers main.
