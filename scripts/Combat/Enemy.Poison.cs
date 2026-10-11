using Godot;
using Vestiges.Core;

namespace Vestiges.Combat;

/// <summary>
/// Poison (Flacon de pharmacie, plan 35 I6) : une réserve de dégâts que chaque coup grossit et qui se vide sur la durée
/// restante ; chaque coup relance la durée. Contrairement à la Brûlure et au saignement, il s'accumule sans plafond.
/// </summary>
public partial class Enemy
{
	private AttackContext _poisonSource;
	private float _poisonReserve;
	private float _poisonTimer;

	internal bool IsPoisoned => _poisonTimer > 0f;

	public void ApplyPoison(float damage, float duration, AttackContext source = default)
	{
		if (_isDying || damage <= 0f || duration <= 0f)
			return;
		_poisonSource = OverTime(source);
		_poisonReserve += damage;
		_poisonTimer = Mathf.Max(_poisonTimer, duration);
	}

	private void ProcessPoison(float delta)
	{
		// Terrée, la créature ne prend rien : le poison attend qu'elle ressorte au lieu de s'évaporer.
		if (_poisonTimer <= 0f || _isBurrowed)
			return;

		float share = _poisonReserve * Mathf.Min(1f, delta / _poisonTimer);
		_poisonTimer -= delta;
		_poisonReserve -= share;
		float damage = share * DamageOverTimeFactor(_poisonSource);
		DamageResult result = DamageResult.Resolve(Life, _poisonSource, _currentHp, damage, 0f);
		LoseHp(damage);
		_dotNumbers.Add(StatusKind.Poison, damage);
		_eventBus.EmitSignal(EventBus.SignalName.EntityDamaged, this, damage);
		_eventBus.PublishEnemyDamage(result);
		if (_poisonTimer <= 0f)
			ClearPoison();
		if (result.Fatal && !_isDying)
			Die(result);
	}

	private void ClearPoison()
	{
		_poisonReserve = 0f;
		_poisonTimer = 0f;
		_poisonSource = default;
	}
}
