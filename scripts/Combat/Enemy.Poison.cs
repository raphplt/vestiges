using System.Collections.Generic;
using Godot;
using Vestiges.Core;

namespace Vestiges.Combat;

/// <summary>Chaque dose conserve sa durée et sa provenance. Une nouvelle dose ne rafraîchit jamais les anciennes.</summary>
public partial class Enemy
{
    private struct PoisonDose
    {
        public float DamagePerSecond;
        public float Remaining;
        public AttackContext Source;
    }

    private readonly List<PoisonDose> _poisonDosesList = new();
    internal bool IsPoisoned => _poisonDosesList.Count > 0;
    public int PoisonDoseCount => _poisonDosesList.Count;
    public void ApplyPoison(float damage, float duration, AttackContext source = default, int maxDoses = int.MaxValue)
    {
        if (_isDying || damage <= 0f || duration <= 0f || _poisonDosesList.Count >= maxDoses) return;
        _poisonDosesList.Add(new PoisonDose
        {
            DamagePerSecond = damage / duration,
            Remaining = duration,
            Source = OverTime(source)
        });
    }

    private void ProcessPoison(float delta)
    {
        if (_poisonDosesList.Count == 0 || _isBurrowed) return;
        for (int i = _poisonDosesList.Count - 1; i >= 0; i--)
        {
            PoisonDose dose = _poisonDosesList[i];
            float share = dose.DamagePerSecond * Mathf.Min(delta, dose.Remaining);
            dose.Remaining -= delta;
            if (dose.Remaining <= 0f)
            {
                _poisonDosesList[i] = _poisonDosesList[^1];
                _poisonDosesList.RemoveAt(_poisonDosesList.Count - 1);
            }

            else _poisonDosesList[i] = dose;
            float damage = share * DamageOverTimeFactor(dose.Source);
            DamageResult result = DamageResult.Resolve(Life, dose.Source, _currentHp, damage, 0f);
            LoseHp(damage);
            _dotNumbers.Add(StatusKind.Poison, damage);
            _eventBus.EmitSignal(EventBus.SignalName.EntityDamaged, this, damage);
            _eventBus.PublishEnemyDamage(result);
            if (result.Fatal && !_isDying)
            {
                Die(result);
                return;
            }

        }

    }
    private void ClearPoison() => _poisonDosesList.Clear();
}
