using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;

namespace Vestiges.Tests;

/// <summary>
/// Plan 27 V3b : la toile de la Tisseuse ne colle qu'à un coup qui porte, et le joueur qu'elle ralentit le montre
/// (fils sur le sprite, icône de la jauge), puis cesse de le montrer à son terme.
/// </summary>
public partial class EnemyAbilityRegression
{
    private async Task RunPlayerStatusChecks()
    {
        CombatPools pools = new() { Name = "CombatPools" };
        AddChild(pools);
        BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
        FieldInfo slowTimer = typeof(Player).GetField("_slowTimer", flags);
        MethodInfo onBody = typeof(EnemyProjectile).GetMethod("HitPlayer", flags);
        bool lastWebbed = false;
        void OnWebbed(bool webbed) => lastWebbed = webbed;
        _player.WebbedChanged += OnWebbed;

        await WaitHurtRecovery();
        _player.GrantInvulnerability(1f);
        EnemyProjectile blocked = pools.TakeEnemyProjectile();
        blocked.Launch(_player.GlobalPosition + new Vector2(40f, 0f), Vector2.Left, 3f, "tisseuse", "web", FxFamily.Silk, 0.4f, 2f);
        onBody.Invoke(blocked, new object[] { _player });
        Check((float)slowTimer.GetValue(_player) <= 0f, "Toile : rien pendant l'invulnérabilité (coup annulé)");
        await Step(70);

        EnemyProjectile web = pools.TakeEnemyProjectile();
        web.Launch(_player.GlobalPosition + new Vector2(40f, 0f), Vector2.Left, 3f, "tisseuse", "web", FxFamily.Silk, 0.4f, 0.5f);
        onBody.Invoke(web, new object[] { _player });
        await Step(2);
        AnimatedSprite2D sprite = _player.GetNode<AnimatedSprite2D>("Sprite");
        bool threads = sprite.Material is ShaderMaterial material && material.GetShaderParameter("crack_spacing").AsInt32() > 0;
        Check((float)slowTimer.GetValue(_player) > 0f && threads && lastWebbed, "Toile : coup qui porte, joueur ralenti, fils et icône allumés");
        await Step(40);
        threads = sprite.Material is ShaderMaterial after && after.GetShaderParameter("crack_spacing").AsInt32() > 0;
        Check(!threads && !lastWebbed, "Toile : fils et icône éteints à son terme");

        _player.WebbedChanged -= OnWebbed;
        await WaitHurtRecovery();

        // Plan 27 V3d : une tranche du Néant consume sans le paquet d'une blessure (ni état blessé, ni chiffre).
        int numbers = VisibleNumbers(pools, out _);
        float hp = _player.CurrentHp;
        PlayerDamageResult tick = _player.TakeErasureDamage(2f);
        Check(tick.Applied && _player.CurrentHp < hp && _player.Mobility.State != MobilityState.Hurt
              && VisibleNumbers(pools, out _) == numbers,
            $"Néant : PV consumés ({hp - _player.CurrentHp:0.0}), sans état blessé ({_player.Mobility.State}) ni chiffre");
        await WaitHurtRecovery();
        pools.QueueFree();
    }
}
