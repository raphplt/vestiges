using System;
using System.Collections.Generic;
using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.World;

/// <summary>
/// Bonus lâché au sol (plan 24 C4) : un objet du quotidien qui flotte au-dessus d'une lueur, ramassé au contact.
/// Il s'efface au bout de sa durée, en clignotant à la fin. Recyclé par <see cref="FieldBonusDirector"/> : tout l'état se
/// réinitialise dans <see cref="Launch"/>. Le dessin est généré en pixels depuis un motif, une fois par sorte de bonus,
/// en attendant les sprites du plan 25 (S5).
/// </summary>
public partial class FieldBonus : Node2D
{
    private const float BobHeight = 3f;
    private const float BobPeriod = 1.4f;
    private const float IconLift = 14f;

    // Motifs : '#' couleur du bonus, 'l' clair, 'd' sombre, 'w' blanc, ' ' vide. Un caractère = deux pixels monde.
    private static readonly Dictionary<string, string[]> Patterns = new()
    {
        ["canteen"] = new[] { "   dd   ", "   ll   ", "  ####  ", " #l###d ", " #l###d ", " #l###d ", " #ww##d ", " #####d ", "  dddd  " },
        ["magnet"] = new[] { " ww  ww ", " ##  ## ", " ##  ## ", " #l  ## ", " #l  ## ", " #l##d# ", "  dddd  " },
        ["blanket"] = new[] { " lllllll ", " #######d", " #l#l#l#d", " #######d", " #l#l#l#d", " #######d", "  dddddd " },
        ["coffee"] = new[] { "  w  w  ", "   w  w ", " ###### ", " #llll#d", " ######dd", " ######d ", "  dddd   " },
        ["firecracker"] = new[] { "      w ", "     d  ", "    ##  ", "   #l#  ", "  #l#d  ", " #l#d   ", " #dd    " },
    };
    private static readonly Dictionary<string, ImageTexture> Textures = new();

    private Sprite2D _icon;
    private Action<FieldBonus> _release;
    private float _age;
    private float _lifetime;
    private float _blink;
    private Color _glow;

    public FieldBonusData Data { get; private set; }
    public bool Active { get; private set; }

    public void SetRelease(Action<FieldBonus> release) => _release = release;

    public override void _Ready()
    {
        _icon = new Sprite2D { TextureFilter = TextureFilterEnum.Nearest };
        AddChild(_icon);
        // Sorti du pool, il attend son lancement (différé) sans se montrer.
        if (!Active)
        {
            Visible = false;
            SetProcess(false);
        }
    }

    public void Launch(FieldBonusData data, Vector2 position, float lifetime, float blink)
    {
        Data = data;
        GlobalPosition = position;
        _age = 0f;
        _lifetime = lifetime;
        _blink = blink;
        _glow = data.Color with { A = 0.35f };
        _icon.Texture = TextureFor(data);
        _icon.Position = new Vector2(0f, -IconLift);
        Modulate = Colors.White;
        Visible = true;
        Active = true;
        SetProcess(true);
        QueueRedraw();
    }

    /// <summary>Ramassé ou effacé : retourne au pool.</summary>
    public void Release()
    {
        if (!Active)
            return;
        Active = false;
        Visible = false;
        SetProcess(false);
        _release?.Invoke(this);
    }

    public override void _Process(double delta)
    {
        _age += (float)delta;
        if (_age >= _lifetime)
        {
            Release();
            return;
        }
        _icon.Position = new Vector2(0f, -IconLift - Mathf.Round(BobHeight * (0.5f + 0.5f * Mathf.Sin(_age * Mathf.Tau / BobPeriod))));
        // Fin de vie : il clignote de plus en plus vite.
        float left = _lifetime - _age;
        if (left < _blink)
            Modulate = new Color(1f, 1f, 1f, Mathf.Sin(_age * Mathf.Lerp(30f, 10f, left / _blink)) > 0f ? 1f : 0.25f);
    }

    public override void _Draw()
    {
        // Lueur au sol, en ellipse 2:1 de la couleur du bonus.
        DrawSetTransform(Vector2.Zero, 0f, new Vector2(1f, 0.5f));
        DrawCircle(Vector2.Zero, 11f, _glow with { A = 0.18f });
        DrawCircle(Vector2.Zero, 7f, _glow);
        DrawSetTransform(Vector2.Zero, 0f, Vector2.One);
    }

    private static ImageTexture TextureFor(FieldBonusData data)
    {
        if (Textures.TryGetValue(data.Id, out ImageTexture texture))
            return texture;
        string[] pattern = Patterns.TryGetValue(data.Id, out string[] found) ? found : new[] { "####", "####", "####" };
        int width = 0;
        foreach (string row in pattern)
            width = Math.Max(width, row.Length);
        // Deux pixels par caractère, un pixel de contour sombre tout autour.
        Image image = Image.CreateEmpty(width * 2 + 2, pattern.Length * 2 + 2, false, Image.Format.Rgba8);
        Color outline = new(0.05f, 0.04f, 0.07f);
        for (int pass = 0; pass < 2; pass++)
        {
            for (int y = 0; y < pattern.Length; y++)
            {
                for (int x = 0; x < pattern[y].Length; x++)
                {
                    char c = pattern[y][x];
                    if (c == ' ')
                        continue;
                    Color color = pass == 0 ? outline : c switch
                    {
                        'l' => data.Color.Lightened(0.4f),
                        'd' => data.Color.Darkened(0.4f),
                        'w' => new Color(0.95f, 0.95f, 0.92f),
                        _ => data.Color,
                    };
                    int grow = pass == 0 ? 1 : 0;
                    for (int py = -grow; py < 2 + grow; py++)
                        for (int px = -grow; px < 2 + grow; px++)
                            image.SetPixel(1 + x * 2 + px, 1 + y * 2 + py, color);
                }
            }
        }
        texture = ImageTexture.CreateFromImage(image);
        Textures[data.Id] = texture;
        return texture;
    }
}
