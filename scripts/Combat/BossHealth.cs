using System;
using System.Collections.Generic;
using Godot;
using Vestiges.Core;

namespace Vestiges.Combat;

/// <summary>
/// Réserve de PV d'un boss fait de parties (plan 07 B1). Chaque partie est une <see cref="Enemy"/> au comportement
/// <c>boss_part</c> : toutes les armes la voient comme une créature. Une partie garde ses propres PV (battant de la
/// Barrière) ou reporte ses dégâts sur une réserve commune (mains de l'Indicible) ; la réserve totale est la somme des
/// deux. Les seuils de phase sont des parts de cette réserve, franchis une seule fois, dans l'ordre.
/// </summary>
public sealed class BossHealth
{
	private readonly List<Enemy> _parts = new();
	private readonly float[] _thresholds;
	private float _sharedMax;
	private int _nextThreshold;
	private bool _depletedRaised;
	private BossBarFeed _bar;

	public string Name { get; }
	public float Max { get; private set; }
	public float Current { get; private set; }
	public float Ratio => Max > 0f ? Current / Max : 0f;
	/// <summary>Seuils franchis : 0 avant le premier.</summary>
	public int Phase => _nextThreshold;
	public bool IsDepleted { get; private set; }
	/// <summary>Parties à PV propres, brisées ou non : un cran chacune sur la barre de boss.</summary>
	public int OwnPartCount { get; private set; }
	public int BrokenPartCount { get; private set; }
	public IReadOnlyList<Enemy> Parts => _parts;

	/// <summary>Une partie a pris des dégâts : le boss la fait clignoter.</summary>
	public event Action<Enemy, float> PartHit;
	/// <summary>Une partie à PV propres est tombée : le boss l'ouvre et rend sa récompense.</summary>
	public event Action<Enemy> PartBroken;
	/// <summary>Seuil de phase franchi, numéroté à partir de 1.</summary>
	public event Action<int> PhaseReached;
	/// <summary>Réserve vide : levé à la mort de la partie qui l'a vidée.</summary>
	public event Action Depleted;

	/// <param name="name">Nom affiché sur la barre de boss.</param>
	/// <param name="sharedHp">Réserve commune des parties sans PV propres, 0 si chaque partie a les siens.</param>
	/// <param name="thresholds">Parts de la réserve totale qui ouvrent une phase, décroissantes, dans ]0 ; 1[.</param>
	public BossHealth(string name, float sharedHp, IReadOnlyList<float> thresholds = null)
	{
		Name = name;
		_sharedMax = Mathf.Max(0f, sharedHp);
		Max = Current = _sharedMax;
		_thresholds = thresholds == null ? Array.Empty<float>() : new float[thresholds.Count];
		for (int i = 0; i < _thresholds.Length; i++)
			_thresholds[i] = thresholds[i];
	}

	/// <summary>
	/// Rattache une créature tout juste initialisée sur la fiche <c>boss_part</c>. <paramref name="ownHp"/> > 0 : la
	/// partie a ses PV et tombe seule ; sinon elle reporte ses dégâts sur la réserve commune et tombe avec elle.
	/// </summary>
	public void AddPart(Enemy part, float bodyRadius, float ownHp = 0f)
	{
		bool shared = ownHp <= 0f;
		if (!shared)
		{
			Max += ownHp;
			Current += ownHp;
			OwnPartCount++;
		}
		_parts.Add(part);
		part.BindBossPart(this, bodyRadius, shared ? SharedRemaining() : ownHp, shared ? _sharedMax : ownHp, shared);
	}

	/// <summary>Ouvre la barre de boss du haut de l'écran, un cran par partie à PV propres déjà rattachée.</summary>
	public void ShowBar(EventBus bus)
	{
		_bar?.End(false);
		_bar = new BossBarFeed(bus, Name, Max, OwnPartCount);
		_bar.Update(Current, Max);
	}

	/// <summary>Le boss quitte la scène sans être vaincu (fin de run, retrait) : la barre se ferme.</summary>
	public void EndEncounter() => _bar?.End(IsDepleted);

	/// <summary>PV perdus par une partie (coup, brûlure, saignement), déjà bornés à ceux qui lui restaient.</summary>
	internal void Absorb(Enemy part, float lost, bool shared)
	{
		if (IsDepleted || lost <= 0f)
			return;
		Current = Mathf.Max(0f, Current - lost);
		if (shared)
			SyncSharedParts();
		_bar?.Update(Current, Max);
		PartHit?.Invoke(part, lost);
		while (_nextThreshold < _thresholds.Length && Current <= Max * _thresholds[_nextThreshold])
		{
			_nextThreshold++;
			PhaseReached?.Invoke(_nextThreshold);
		}
		// Le coup qui vide la réserve tue la partie frappée : Depleted part de sa mort, après la récompense du battant.
		if (Current <= 0f)
			IsDepleted = true;
	}

	/// <summary>Appelé par la partie à sa mort, avant son retour au pool.</summary>
	internal void NotifyBroken(Enemy part, bool shared)
	{
		if (!shared)
		{
			BrokenPartCount++;
			// Toutes les parties à PV propres sont tombées sans réserve commune : le boss est vaincu, même si la somme
			// flottante des PV perdus n'est pas retombée exactement à zéro.
			if (BrokenPartCount >= OwnPartCount && _sharedMax <= 0f)
			{
				Current = 0f;
				IsDepleted = true;
			}
			PartBroken?.Invoke(part);
		}
		if (IsDepleted && !_depletedRaised)
		{
			_depletedRaised = true;
			_bar?.End(true);
			Depleted?.Invoke();
		}
	}

	/// <summary>Retire une partie (main rentrée sous terre, boss fini) : elle cesse de compter comme cible du boss.</summary>
	internal void Detach(Enemy part) => _parts.Remove(part);

	/// <summary>Réserve commune rendue au boss (phase qui régénère) ; ne relève pas un boss vaincu.</summary>
	public void RestoreShared(float amount)
	{
		if (IsDepleted || amount <= 0f)
			return;
		float restored = Mathf.Min(amount, _sharedMax - SharedRemaining());
		Current += Mathf.Max(0f, restored);
		SyncSharedParts();
		_bar?.Update(Current, Max);
	}

	/// <summary>Réserve commune restante : le total moins les PV propres encore debout.</summary>
	private float SharedRemaining()
	{
		float own = 0f;
		foreach (Enemy part in _parts)
		{
			if (part.IsBoundToOwnHp && part.IsActive && !part.IsDying)
				own += part.CurrentHp;
		}
		return Mathf.Max(0f, Current - own);
	}

	private void SyncSharedParts()
	{
		float shared = SharedRemaining();
		foreach (Enemy part in _parts)
		{
			if (!part.IsBoundToOwnHp)
				part.SyncSharedHp(shared, _sharedMax);
		}
	}
}
