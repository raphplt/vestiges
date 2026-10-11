using System;
using System.Collections.Generic;
using Godot;
using Vestiges.Combat;
using Vestiges.Infrastructure;

namespace Vestiges.UI;

/// <summary>Présence des héros débloqués autour du feu. Aucun choix ou contrôle caché dans le décor.</summary>
public partial class HubCamp : Control
{
    public override void _Ready() => MouseFilter = MouseFilterEnum.Ignore;

    public void Populate(IReadOnlyList<CharacterData> characters, Func<string, bool> isUnlocked)
    {
        List<Node2D> figures = new();
        int index = 0;
        foreach (CharacterData character in characters)
        {
            int place = index++;
            if (!isUnlocked(character.Id)) continue;
            SpriteFrames frames = CharacterSpriteLoader.LoadOrGet(character.Id, character.SpriteFolder);
            if (frames == null) continue;
            // Deux couronnes décalées permettent au casting de grandir sans recouvrir tous les sprites.
            float angle = Mathf.DegToRad(35 + place % 6 * 54);
            float radius = place < 6 ? 236 : 352;
            Vector2 offset = new(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius * 0.35f);
            Vector2 feet = (HubBackdrop.FirePosition + offset).Snapped(new Vector2(4, 4));
            Node2D figure = new() { Position = feet };
            AnimatedSprite2D sprite = new()
            {
                SpriteFrames = frames, Centered = false, Offset = new Vector2(-16, -36),
                Scale = new Vector2(4, 4), TextureFilter = TextureFilterEnum.Nearest,
                Modulate = new Color(0.78f, 0.72f, 0.68f),
            };
            figure.AddChild(sprite);
            string direction = offset.X > 0 ? (offset.Y > 0 ? "NW" : "SW") : (offset.Y > 0 ? "NE" : "SE");
            string animation = direction + "_idle";
            if (!frames.HasAnimation(animation)) animation = "SE_idle";
            sprite.Play(animation);
            sprite.Frame = place % frames.GetFrameCount(animation);
            figures.Add(figure);
        }
        figures.Sort((a, b) => a.Position.Y.CompareTo(b.Position.Y));
        foreach (Node2D figure in figures) AddChild(figure);
    }
}
