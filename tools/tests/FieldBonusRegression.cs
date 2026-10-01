using System;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.World;

namespace Vestiges.Tests;

/// <summary>
/// Bonus lâchés (plan 24 C4) : limite au sol, ramassage au contact, chaque effet (soin, aimant, bouclier, frénésie,
/// pétard), retour des effets à durée, effacement en fin de vie, et les cinq bonus en données.
/// </summary>
public partial class FieldBonusRegression : Node2D
{
	private static readonly PackedScene EnemyScene = GD.Load<PackedScene>("res://scenes/enemies/Enemy.tscn");
	private int _failures;

	public override async void _Ready()
	{
		try
		{
			GetNode<GameManager>("/root/GameManager").ChangeState(GameManager.GameState.Run);
			FieldBonusConfig config = FieldBonusDataLoader.Load();
			Check(config.Enabled && config.Bonuses.Count == 5 && config.Get("canteen")?.Effect == "heal" && config.VariantChance["champion"] >= 1f,
				$"Données : {config.Bonuses.Count} bonus, gourde = soin, Souverain = toujours");

			Player player = GD.Load<PackedScene>("res://scenes/Player.tscn").Instantiate<Player>();
			AddChild(player);
			player.InitializeCharacter(CharacterDataLoader.Get("vagabond"));
			player.SetPhysicsProcess(false);
			player.DisableDefenseForTests();
			player.GlobalPosition = Vector2.Zero;
			FieldBonusDirector director = new() { Name = "FieldBonusDirector" };
			AddChild(director);
			await Frames(2);

			// Limite au sol : au plus max_on_ground, loin du joueur.
			for (int i = 0; i < config.MaxOnGround + 3; i++)
				director.Spawn(config.Get("coffee"), new Vector2(3000f + i * 40f, 0f));
			await Frames(2);
			Check(director.Active.Count == config.MaxOnGround, $"Au plus {config.MaxOnGround} bonus au sol ({director.Active.Count})");
			while (director.Active.Count > 0)
				director.Active[0].Release();

			// Soin : 20 % des PV max, au contact.
			player.TakeDamage(50f);
			float hp = player.CurrentHp;
			director.Spawn(config.Get("canteen"), player.GlobalPosition);
			await Frames(3);
			Check(Mathf.IsEqualApprox(player.CurrentHp - hp, Mathf.Min(50f, player.EffectiveMaxHp * config.Get("canteen").Param("ratio"))) && director.Active.Count == 0,
				$"Gourde : soin de 20 % des PV max ({hp:0} → {player.CurrentHp:0}), ramassée");

			// Effets à durée : appliqués puis retirés.
			float magnet = player.XpMagnetMultiplier, shield = player.MaxShield, rate = player.AttackSpeedMultiplier;
			director.Spawn(config.Get("magnet"), player.GlobalPosition);
			director.Spawn(config.Get("blanket"), player.GlobalPosition);
			director.Spawn(config.Get("coffee"), player.GlobalPosition);
			await Frames(3);
			bool applied = player.XpMagnetMultiplier > magnet * 5f && player.MaxShield > shield && Mathf.IsEqualApprox(player.AttackSpeedMultiplier, rate * 1.3f);
			director._Process(20.0);
			Check(applied && Mathf.IsEqualApprox(player.XpMagnetMultiplier, magnet) && Mathf.IsEqualApprox(player.MaxShield, shield)
				&& Mathf.IsEqualApprox(player.AttackSpeedMultiplier, rate),
				"Aimant, couverture et café : appliqués au ramassage, retirés à leur fin");

			// Pétard : blesse et repousse autour, une créature hors de portée n'est pas touchée.
			Enemy near = SpawnEnemy(new Vector2(60f, 0f));
			Enemy far = SpawnEnemy(new Vector2(900f, 0f));
			await Frames(2);
			float nearHp = near.HpRatio, farHp = far.HpRatio;
			director.Spawn(config.Get("firecracker"), player.GlobalPosition);
			await Frames(3);
			Check(near.HpRatio < nearHp && Mathf.IsEqualApprox(far.HpRatio, farHp), $"Pétard : la créature proche perd {(nearHp - near.HpRatio) * 100f:0} % de ses PV, la lointaine rien");

			// Fin de vie : un bonus jamais ramassé s'efface.
			director.Spawn(config.Get("coffee"), new Vector2(5000f, 0f));
			await Frames(2);
			FieldBonus left = director.Active[0];
			left._Process(config.LifetimeSeconds + 1f);
			Check(director.Active.Count == 0 && !left.Active && left.Visible, "Fin de vie : retiré du ramassage pendant la dissolution");
			left._Process(1f);
			Check(!left.Visible && !left.IsProcessing(), "Dissolution terminée : nœud caché et arrêté");
			director.Spawn(config.Get("canteen"), new Vector2(4000f, 20f));
			await Frames(2);
			Check(ReferenceEquals(left, director.Active[0]) && left.Active && left.Modulate == Colors.White
				&& left.GlobalPosition == new Vector2(4000f, 20f)
				&& ((Sprite2D)left.GetChild(1)).Texture == FieldBonusArt.Get("heal").Idle[0],
				"Réutilisation : même nœud, nouvelle position, autre bonus et première pose réinitialisés");

			GD.Print($"[FieldBonusRegression] RESULT failures={_failures}");
			GetTree().Quit(_failures == 0 ? 0 : 1);
		}
		catch (Exception exception)
		{
			GD.PushError(exception.ToString());
			GetTree().Quit(2);
		}
	}

	private Enemy SpawnEnemy(Vector2 position)
	{
		Enemy enemy = EnemyScene.Instantiate<Enemy>();
		AddChild(enemy);
		enemy.Initialize(EnemyDataLoader.Get("rodeur"), 1f, 1f);
		enemy.SetPhysicsProcess(false);
		enemy.GlobalPosition = position;
		return enemy;
	}

	private async System.Threading.Tasks.Task Frames(int count)
	{
		for (int i = 0; i < count; i++)
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
	}

	private void Check(bool ok, string label)
	{
		if (!ok)
			_failures++;
		GD.Print($"[FieldBonusRegression] {(ok ? "PASS" : "FAIL")} {label}");
	}
}
