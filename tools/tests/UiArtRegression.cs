using System;
using Godot;
using Vestiges.Infrastructure;
using Vestiges.UI;

namespace Vestiges.Tests;

/// <summary>Contrat entre les catalogues, les PNG importés et leurs consommateurs d'interface.</summary>
public partial class UiArtRegression : Node
{
    private int _failures;

    public override void _Ready()
    {
        try
        {
            foreach (PassiveSouvenirData item in PassiveSouvenirDataLoader.GetAll())
                Check(Icon(item.Icon, 32) && Icon(item.IconSmall, 16), $"Objet {item.Id} : deux tailles importées");
            foreach (PerkSpecializationData perk in PerkSpecializationDataLoader.GetAll())
                Check(Icon(perk.Icon, 32) && Icon(perk.IconSmall, 16), $"Réminiscence {perk.Id} : deux tailles importées");
            Check(PassiveSouvenirDataLoader.GetAll().Count == 31 && PerkSpecializationDataLoader.GetAll().Count == 9,
                "Les manifestes visuels n'ajoutent pas de règles aux catalogues de jeu");
            Check(CollectionArtDataLoader.Items.Count == 34 && CollectionArtDataLoader.Perks.Count == 14,
                "Collection : tous les objets et motifs futurs sont présents");
            foreach (CollectionArtDataLoader.Entry entry in CollectionArtDataLoader.Items)
                Check(Icon(entry.Icon, 32), $"Collection objet {entry.Id}");
            foreach (CollectionArtDataLoader.Entry entry in CollectionArtDataLoader.Perks)
                Check(Icon(entry.Icon, 32), $"Collection Réminiscence {entry.Id}");

            for (int rank = 0; rank < 5; rank++)
            {
                int poses = rank == 4 ? 4 : 1;
                Check(Frames(RarityArt.Icons(rank, 12), poses, 12, 12)
                    && Frames(RarityArt.Icons(rank, 24), poses, 24, 24)
                    && Frames(RarityArt.Cards(rank), poses, 20, 20), $"Rareté {rank} : atlas complets");
                if (rank < 4)
                    Check(Frames(RarityArt.Jump(rank), 6, 32, 32), $"Transition depuis le rang {rank}");
            }
            foreach (FieldBonusData bonus in FieldBonusDataLoader.Load().Bonuses)
            {
                FieldBonusArt.Sprites art = FieldBonusArt.Get(bonus.Sprite);
                Check(Frames(art.Idle, 4, 16, 16) && Frames(art.Disappear, 3, 16, 16)
                    && art.Glow.GetWidth() == art.Glow.GetHeight() * 2, $"Bonus {bonus.Id} : animation et lueur 2:1");
            }
            for (int tint = 0; tint < 4; tint++)
                Check(ScreenArt.Background(tint).GetSize() == new Vector2(480, 270), $"Texture native du fond {tint}");
            Check(Frames(ScreenArt.Grounds, 5, 128, 32), "Les cinq sols de chargement sont importés");
            foreach (string skin in new[] { "button_normal", "button_hover", "button_pressed", "button_disabled",
                "card_normal", "card_selected", "card_locked", "panel_frame", "panel_frame_selected" })
                Check(UITheme.LoadTex(UITheme.MenusPath + $"ui_{skin}.png").ResourcePath.StartsWith(ScreenArt.Folder, StringComparison.Ordinal),
                    $"Thème : {skin} emploie le nouvel habillage");

            System.Collections.Generic.HashSet<string> statIcons = new();
            foreach (ChestStatBonus stat in ChestDataLoader.LoadStatBonus().Stats)
            {
                Texture2D icon = LootIconResolver.Get("stat", stat.Stat);
                Check(icon != null && statIcons.Add(icon.ResourcePath), $"Coffre : icône distincte pour {stat.Stat}");
            }
            foreach (string type in new[] { "essence", "xp", "souvenir" })
                Check(LootIconResolver.Get(type) != null, $"Coffre : icône de {type}");
            foreach (PassiveSouvenirData item in PassiveSouvenirDataLoader.GetAll())
                Check(LootIconResolver.Get("object_level", item.Id)?.ResourcePath == item.Icon,
                    $"Coffre : sprite propre de {item.Id}");

            RarityIcon reveal = new(3);
            AddChild(reveal);
            reveal.RevealFrom(0, 3, 0.35f);
            reveal.SetProcess(false);
            reveal._Process(0.2);
            Check(reveal.Texture == RarityArt.Icons(0, 24)[0], "Chance : rang initial avant le départ");
            reveal._Process(0.2);
            Check(reveal.Texture.GetWidth() == 32 && reveal.CustomMinimumSize.X >= 32,
                "Chance : éclatement à sa taille native");
            reveal._Process(2);
            Check(reveal.Texture == RarityArt.Icons(3, 24)[0] && !reveal.IsProcessing(),
                "Chance : trois rangs révélés, animation arrêtée au bon éclat");
            PixelBackdrop backdrop = new(PixelBackdrop.MemorialTint);
            AddChild(backdrop);
            Check(backdrop.Material is ShaderMaterial material
                && Mathf.IsEqualApprox(material.GetShaderParameter("rotation_degrees_per_second").AsSingle(), 8f)
                && backdrop.TextureFilter == CanvasItem.TextureFilterEnum.Nearest,
                "Rotation continue à 8 degrés/s, grille native et filtre nearest");
            GD.Print($"[UiArtRegression] RESULT failures={_failures}");
            GetTree().Quit(_failures == 0 ? 0 : 1);
        }
        catch (Exception exception)
        {
            GD.PushError($"[UiArtRegression] {exception}");
            GetTree().Quit(2);
        }
    }

    private static bool Icon(string path, int size) => !string.IsNullOrEmpty(path)
        && ResourceLoader.Exists(path) && GD.Load<Texture2D>(path).GetSize() == new Vector2(size, size);

    private static bool Frames(Texture2D[] frames, int count, int width, int height)
    {
        if (frames.Length != count)
            return false;
        foreach (Texture2D frame in frames)
            if (frame.GetSize() != new Vector2(width, height))
                return false;
        return true;
    }

    private void Check(bool condition, string label)
    {
        if (!condition)
            _failures++;
        GD.Print($"[UiArtRegression] {(condition ? "PASS" : "FAIL")} {label}");
    }
}
