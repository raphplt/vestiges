using System.Collections.Generic;
using Godot;
using Vestiges.Core;

namespace Vestiges.Combat;

/// <summary>
/// Fissure au sol d'où la Barrière se lève (plan 07 B2) : un tracé dentelé le long de la ligne de la grille, révélé du
/// centre vers les bords, aux lèvres de terre retournée, avec un cœur de la lumière vert-acide des cadenas ; quelques
/// rameaux s'en échappent. Une fois la grille dressée, la lueur retombe et la fissure reste comme une cicatrice.
/// Décalque au sol (couche −1). Le tracé ne se redessine que lorsqu'il s'allonge ou s'ouvre d'un cran, et seulement près
/// du centre (au-delà, il est hors du champ) ; la lueur, dessinée par un enfant, ne varie que par son opacité.
/// </summary>
public partial class BarrierCrack : Node2D
{
	private const float Step = 14f;
	private const float Jitter = 3f;
	private const float BranchChance = 0.3f;
	private const float BranchLength = 30f;
	// Au-delà, l'entaille garde sa largeur de fil : on ne la voit s'ouvrir que sous la grille.
	private const float OpenSpread = 520f;
	// Rayon dessiné autour du centre : la levée se joue sous les yeux du joueur.
	private const float DrawRadius = 1100f;
	private const int OpenSteps = 6;

	private static readonly Color Shadow = new(0.07f, 0.055f, 0.05f, 0.9f);
	// Terre retournée au bord de l'entaille.
	private static readonly Color Lip = new(0.32f, 0.24f, 0.17f, 0.55f);
	private static readonly Color Glow = new(0.5f, 1f, 0f);

	private readonly List<Vector2> _points = new();
	private readonly List<float> _distances = new();
	private readonly List<(int Index, Vector2 Tip)> _branches = new();
	private CrackGlow _glow;
	private float _reach = -1f;
	private int _openStep = -1;

	/// <summary>
	/// Tracé le long de [<paramref name="from"/>, <paramref name="to"/>], centré sur <paramref name="center"/> ; les
	/// écarts sont tirés une fois, sur un flux à part.
	/// </summary>
	public void Setup(Vector2 from, Vector2 to, Vector2 center)
	{
		ZAsRelative = false;
		ZIndex = -1;
		// Flux à part : la fissure ne décale aucun tirage du jeu.
		RandomNumberGenerator rng = RunRandom.Create("barrier_crack");
		Vector2 axis = (to - from).Normalized();
		Vector2 across = axis.Orthogonal();
		int count = Mathf.Max(2, Mathf.CeilToInt(from.DistanceTo(to) / Step));
		for (int i = 0; i <= count; i++)
		{
			Vector2 along = from.Lerp(to, (float)i / count);
			// Écart au sol, aplati comme l'est le sol à l'écran.
			Vector2 point = along + Iso.ToScreen(across * rng.RandfRange(-Jitter, Jitter) * 2f) * 0.5f;
			_points.Add(point);
			_distances.Add(point.DistanceTo(center));
			if (i > 0 && i < count && rng.Randf() < BranchChance)
			{
				float side = rng.Randf() < 0.5f ? -1f : 1f;
				Vector2 direction = (across * side + axis * rng.RandfRange(-0.6f, 0.6f)).Normalized();
				_branches.Add((i, point + Iso.ToScreen(direction * BranchLength * rng.RandfRange(0.5f, 1f))));
			}
		}
		_glow = new CrackGlow { Name = "Glow" };
		AddChild(_glow);
	}

	/// <summary>
	/// Portée révélée de part et d'autre du centre (px), lueur de 0 (éteinte) à 1, ouverture de 0 (un fil) à 1 (une
	/// entaille où passe la lumière).
	/// </summary>
	public void SetState(float reach, float glow, float open)
	{
		_glow.Modulate = Colors.White with { A = 0.15f + 0.65f * glow };
		float drawn = Mathf.Min(reach, DrawRadius);
		int openStep = Mathf.RoundToInt(Mathf.Clamp(open, 0f, 1f) * OpenSteps);
		// Un cran de tracé à la fois : pas de redessin pour quelques pixels.
		if (Mathf.Abs(drawn - _reach) < Step && openStep == _openStep)
			return;
		_reach = drawn;
		_openStep = openStep;
		QueueRedraw();
		_glow.QueueRedraw();
	}

	public override void _Draw()
	{
		float open = (float)_openStep / OpenSteps;
		for (int i = 1; i < _points.Count; i++)
		{
			if (_distances[i] > _reach || _distances[i - 1] > _reach)
				continue;
			// Les lèvres de l'entaille, plus larges au centre, où elle s'est ouverte d'abord.
			float width = 1f + open * (1f - Mathf.Min(1f, _distances[i] / OpenSpread));
			DrawLine(_points[i - 1], _points[i], Lip, 3f + 3f * width);
			DrawLine(_points[i - 1], _points[i], Shadow, 2f + 2f * width);
		}
		foreach ((int index, Vector2 tip) in _branches)
		{
			if (_distances[index] <= _reach)
				DrawLine(_points[index], tip, Shadow with { A = 0.75f }, 2f + open);
		}
	}

	/// <summary>La lumière au fond de l'entaille : dessinée pleine, son intensité vient de l'opacité du nœud.</summary>
	private partial class CrackGlow : Node2D
	{
		public override void _Draw()
		{
			BarrierCrack crack = GetParent<BarrierCrack>();
			float open = (float)crack._openStep / OpenSteps;
			for (int i = 1; i < crack._points.Count; i++)
			{
				if (crack._distances[i] > crack._reach || crack._distances[i - 1] > crack._reach)
					continue;
				float width = 1f + open * (1f - Mathf.Min(1f, crack._distances[i] / OpenSpread));
				DrawLine(crack._points[i - 1], crack._points[i], Glow, Mathf.Max(1f, width));
			}
			foreach ((int index, Vector2 tip) in crack._branches)
			{
				if (crack._distances[index] <= crack._reach)
					DrawLine(crack._points[index], crack._points[index].Lerp(tip, 0.6f), Glow with { A = 0.7f }, 1f);
			}
		}
	}
}
