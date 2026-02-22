using Godot;

namespace Vestiges.Base;

/// <summary>
/// Torche placée par le joueur. Émet de la lumière via PointLight2D.
/// S'éteint et se détruit après une durée définie par les stats JSON.
/// </summary>
public partial class Torch : Wall
{
    private PointLight2D _light;
    private float _duration;
    private float _elapsed;

    public void SetTorchStats(float radius, float duration)
    {
        _duration = duration;

        GradientTexture2D texture = new();
        Gradient gradient = new();
        gradient.Colors = new Color[] { Colors.White, new Color(1, 1, 1, 0) };
        texture.Gradient = gradient;
        texture.Width = 256;
        texture.Height = 256;
        texture.Fill = GradientTexture2D.FillEnum.Radial;
        texture.FillFrom = new Vector2(0.5f, 0.5f);
        texture.FillTo = new Vector2(0.5f, 0f);

        _light = new PointLight2D();
        _light.Texture = texture;
        _light.Color = new Color(1f, 0.75f, 0.3f);
        _light.Energy = 0.8f;
        _light.TextureScale = radius / 128f;
        AddChild(_light);
    }

    public override void _Process(double delta)
    {
        if (_duration <= 0)
            return;

        _elapsed += (float)delta;

        // Flickering effect in the last 20% of duration
        if (_elapsed > _duration * 0.8f && _light != null)
        {
            float fade = 1f - (_elapsed - _duration * 0.8f) / (_duration * 0.2f);
            _light.Energy = 0.8f * fade;
        }

        if (_elapsed >= _duration)
            OnDestroyed();
    }
}
