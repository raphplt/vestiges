using Godot;

namespace Vestiges.Combat;

/// <summary>
/// Brume violette sous une créature aberrante. Le matériau de particules est partagé et gardé pour toute la partie :
/// le shader que Godot en génère reste compilé, préchauffé au chargement (<see cref="Infrastructure.ShaderWarmup"/>),
/// au lieu d'être compilé à la première aberration, en pleine Résurgence (≈ 50 ms, plan 29).
/// </summary>
public static class AberrationAura
{
	public const string NodeName = "AberrationAura";
	private static ParticleProcessMaterial _material;

	public static GpuParticles2D Create(int amount)
	{
		return new GpuParticles2D
		{
			Name = NodeName,
			Amount = amount,
			Lifetime = 1.2f,
			SpeedScale = 0.6f,
			Explosiveness = 0f,
			ZIndex = -1,
			Texture = VfxFactory.CircleTexture,
			TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
			ProcessMaterial = Material(),
		};
	}

	private static ParticleProcessMaterial Material()
	{
		if (_material != null)
			return _material;
		Gradient gradient = new();
		gradient.SetColor(0, new Color(0.15f, 0.05f, 0.2f, 0f));
		gradient.AddPoint(0.3f, new Color(0.25f, 0.08f, 0.35f, 0.35f));
		gradient.SetColor(gradient.GetPointCount() - 1, new Color(0.15f, 0.05f, 0.2f, 0f));
		_material = new ParticleProcessMaterial
		{
			EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Box,
			EmissionBoxExtents = new Vector3(14, 8, 0),
			Direction = new Vector3(0, -0.3f, 0),
			Spread = 180f,
			InitialVelocityMin = 3f,
			InitialVelocityMax = 8f,
			Gravity = new Vector3(0, -5, 0),
			ScaleMin = 0.6f,
			ScaleMax = 1.4f,
			ColorRamp = new GradientTexture1D { Gradient = gradient },
		};
		return _material;
	}
}
