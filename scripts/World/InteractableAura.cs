using Godot;

namespace Vestiges.World;

/// <summary>
/// Repère discret d'un élément interactif, en pixel art (plan 41 R4) : les quatre coins d'un losange iso au sol, liserés
/// d'un pixel comme des crochets de sélection, et un éclat de 2 px qui monte et descend par pixels entiers. La pulsation ne joue que sur
/// l'opacité : une échelle qui respire sortait du grain des pixels (polygones lisses jugés hors de la direction
/// artistique par Raphaël, DECISIONS §129).
/// </summary>
public partial class InteractableAura : Node2D
{
    private Sprite2D _ground;
    private Sprite2D _mote;

    private float _pulseSpeed = 1f;
    private float _baseAlpha = 0.3f;
    private float _pulseAlpha = 0.12f;
    private float _phase;
    private Vector2 _moteBase;
    private bool _animateMote;
    private bool _isActive = true;

    public override void _Ready()
    {
        ZIndex = -1;
        ProcessMode = ProcessModeEnum.Inherit;
    }

    public void Configure(Color color, float radius, float height, bool withMote = false, float pulseSpeed = 1f,
        float baseAlpha = 0.3f, float pulseAlpha = 0.12f)
    {
        _pulseSpeed = pulseSpeed;
        _baseAlpha = baseAlpha;
        _pulseAlpha = pulseAlpha;
        _animateMote = withMote;
        _phase = (float)GD.RandRange(0.0, Mathf.Tau);

        _ground?.QueueFree();
        _ground = new Sprite2D
        {
            Name = "Ground",
            Texture = GroundDiamond(Mathf.Max(4, Mathf.RoundToInt(radius)), color),
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            Position = new Vector2(0f, 3f),
            ZIndex = -2,
        };
        AddChild(_ground);

        _mote?.QueueFree();
        _mote = null;
        if (withMote)
        {
            Image spark = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8);
            spark.Fill(color.Lightened(0.35f));
            _moteBase = new Vector2(Mathf.Round(radius * 0.15f), Mathf.Round(-height - 5f));
            _mote = new Sprite2D
            {
                Name = "Mote",
                Texture = ImageTexture.CreateFromImage(spark),
                TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
                Position = _moteBase,
                ZIndex = 1,
            };
            AddChild(_mote);
        }

        Visible = true;
        _isActive = true;
    }

    public void SetActive(bool isActive)
    {
        _isActive = isActive;
        Visible = isActive;
    }

    public override void _Process(double delta)
    {
        if (!_isActive || _ground == null)
            return;

        float time = (float)Time.GetTicksMsec() * 0.001f;
        float wave = 0.5f + 0.5f * Mathf.Sin(time * _pulseSpeed + _phase);
        _ground.Modulate = new Color(1f, 1f, 1f, _baseAlpha + wave * wave * _pulseAlpha);

        if (_animateMote && _mote != null)
        {
            float moteWave = 0.5f + 0.5f * Mathf.Sin(time * (_pulseSpeed * 1.35f) + _phase + 0.8f);
            // Pas d'un pixel entier : l'éclat reste sur la grille du monde.
            _mote.Position = _moteBase + new Vector2(0f, -Mathf.Round(moteWave * 3f));
            _mote.Modulate = new Color(1f, 1f, 1f, 0.5f + moteWave * 0.5f);
        }
    }

    /// <summary>
    /// Coins d'un losange iso de demi-largeur <paramref name="halfWidth"/> (hauteur moitié, comme une case du sol),
    /// dessinés pixel par pixel ; le milieu de chaque côté reste vide (un losange plein, rempli ou non, chargeait le sol).
    /// </summary>
    private static ImageTexture GroundDiamond(int halfWidth, Color color)
    {
        int halfHeight = Mathf.Max(2, halfWidth / 2);
        int width = halfWidth * 2 + 1;
        int height = halfHeight * 2 + 1;
        Image image = Image.CreateEmpty(width, height, false, Image.Format.Rgba8);
        Color edge = color with { A = 1f };
        for (int y = 0; y < height; y++)
        {
            int dy = Mathf.Abs(y - halfHeight);
            // Le tiers central de chaque côté reste vide : il ne reste que les coins.
            if (dy > halfHeight / 3 && dy < halfHeight - halfHeight / 3)
                continue;
            // Demi-largeur de la rangée : deux pixels de large par pixel de haut (pente 2:1 de la grille iso).
            int span = (halfHeight - dy) * 2;
            foreach (int x in new[] { halfWidth - span, halfWidth - span + 1, halfWidth + span - 1, halfWidth + span })
                if (x >= 0 && x < width)
                    image.SetPixel(x, y, edge);
        }
        return ImageTexture.CreateFromImage(image);
    }
}
