using System;
using System.Collections.Generic;
using System.Reflection;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Progression;
using Vestiges.UI;

namespace Vestiges.Tests;

/// <summary>Les nouvelles fiches passent par le vrai sac, puis leurs consommateurs de combat, mobilité et soin.</summary>
public partial class ObjectsRegression
{
    private void CheckCatalogueStatItems()
    {
        (string Id, Func<Player, float> Read, float Gain)[] items =
        {
            ("dossard_froisse", p => p.SpeedMultiplier, 0.08f),
            ("pastille_mentholee", p => p.BonusRegenRate, 0.08f),
            ("bouillotte", p => p.EffectiveMaxHp, 12f),
            ("agrafe_rouillee", p => p.Armor, 1f),
            ("bons_points", p => p.XpGainMultiplier, 0.06f),
            ("capsule_porte_bonheur", p => p.LuckBonus, 0.03f),
            ("bande_magnetique", p => p.XpMagnetMultiplier, 0.15f),
            ("petite_cle", p => p.AttackSpeedMultiplier, 0.06f),
            ("compas", p => p.AoeMultiplier, 0.10f),
            ("longue_vue", p => p.AttackRangeMultiplier, 0.10f),
            ("verre_fele", p => p.CritChance, 0.03f),
            ("yoyo", p => p.Mobility.RechargeMultiplier, 0.08f),
            ("bocal_de_conserve", p => p.StatusDurationMultiplier, 0.12f),
            ("peigne_casse", p => p.ProjectilePierce, 1f),
            ("ampoule_rouge", p => p.Lifesteal, 0.0015f),
            ("bille_irisee", p => p.CritMultiplier, 0.25f),
            ("boucle_cabossee", p => p.MaxShield, 15f),
            ("diapositive_en_double", p => p.BonusProjectiles, 1f),
        };
        foreach ((string id, Func<Player, float> read, float gain) in items)
        {
            Setup();
            _player.SetBagCapacity(20);
            float baseline = read(_player);
            bool linear = true;
            for (int n = 1; n <= 8; n++)
                linear &= _player.AddItemCopy(id) && Near(read(_player), baseline + n * gain);
            for (int n = 7; n >= 0; n--)
                linear &= _player.ForgetItemCopy(id) && Near(read(_player), baseline + n * gain);
            Check(linear && _player.BagCount == 0, $"{id} : huit copies réelles, gains linéaires, huit retraits sans résidu");
        }
        CheckStatItemConsumers();
        CheckStatItemRecovery();
        CheckNewItemPresentation();
    }

    private void CheckStatItemConsumers()
    {
        Setup();
        float Velocity() => _player.Mobility.Step(0.1f, Vector2.Right, _player.Speed * _player.SpeedMultiplier, 1f, true).Length();
        float baseVelocity = Velocity();
        _player.AddItemCopy("dossard_froisse");
        _player.AddItemCopy("dossard_froisse");
        Check(Near(Velocity(), baseVelocity * 1.16f), "Dossard : la mobilité produit une vitesse réellement augmentée de 16 % avec deux copies");
        _player.AddItemCopy("yoyo");
        _player.AddItemCopy("yoyo");
        _player.Mobility.Request();
        _player.Mobility.Step(0.01f, Vector2.Right, _player.Speed, 1f, true);
        Check(_player.Mobility.StartedThisStep && Near(_player.Mobility.CooldownRemaining, _player.Mobility.Config.CooldownSeconds / 1.16f),
            "Yo-yo : démarrer un dash consomme la recharge réellement divisée par 1,16");
        _player.ForgetItemCopy("yoyo");
        _player.ForgetItemCopy("yoyo");
        Check(Near(_player.Mobility.CooldownSeconds, _player.Mobility.Config.CooldownSeconds), "Yo-yo retiré : les prochains dashs reprennent la recharge de base");

        Setup();
        PlayerProgression progression = new() { Name = "PlayerProgression" };
        _player.AddChild(progression);
        _player.AddItemCopy("bons_points");
        _player.AddItemCopy("bons_points");
        float xp = progression.CurrentXp;
        GetNode<EventBus>("/root/EventBus").EmitSignal(EventBus.SignalName.XpGained, 1f);
        Check(Near(progression.CurrentXp - xp, 1.12f), "Bons points : une orbe de 1 XP donne réellement 1,12 XP avec deux copies");
        _player.ForgetItemCopy("bons_points");
        xp = progression.CurrentXp;
        GetNode<EventBus>("/root/EventBus").EmitSignal(EventBus.SignalName.XpGained, 1f);
        Check(Near(progression.CurrentXp - xp, 1.06f), "Bons points : retirer une copie retire seulement sa part d’XP");
        float radius = XpOrb.AttractionRadius(_player);
        _player.AddItemCopy("bande_magnetique");
        _player.AddItemCopy("bande_magnetique");
        Check(Near(XpOrb.AttractionRadius(_player), radius * 1.3f), "Bande magnétique : rayon réellement lu par les orbes augmenté de 30 %");

        Setup();
        WeaponInstance bow = _player.EquippedWeapon;
        float cadence = _player.GetWeaponStatForDisplay(bow, "attack_speed");
        float range = _player.GetWeaponStatForDisplay(bow, "range");
        _player.AddItemCopy("petite_cle");
        _player.AddItemCopy("longue_vue");
        List<Timer> timers = (List<Timer>)typeof(Player).GetField("_weaponTimers", Private).GetValue(_player);
        Check(Near(_player.GetWeaponStatForDisplay(bow, "attack_speed"), cadence * 1.06f)
            && Near((float)timers[0].WaitTime, 1f / (cadence * 1.06f))
            && Near(_player.GetWeaponStatForDisplay(bow, "range"), range * 1.1f), "Petite clé et Longue-vue : cadence et portée effectives de l’arme modifiées");
        _player.AddWeapon(WeaponDataLoader.Get("teachers_bell"));
        WeaponInstance bell = _player.WeaponSlots[1];
        _player.AddItemCopy("bocal_de_conserve");
        _player.AddItemCopy("bocal_de_conserve");
        Enemy target = SpawnEnemy();
        _player.OnProjectileHit(target, 1f, false, bell);
        Check(Near((float)typeof(Enemy).GetField("_slowTimer", Private).GetValue(target), 2f * 1.24f),
            "Bocal : la Cloche ralentit réellement pendant 2,48 s au lieu de 2 s");
        target.QueueFree();
        _player.AddItemCopy("bille_irisee");
        float critical = (float)typeof(Player).GetMethod("ProjectileDamage", Private).Invoke(_player, new object[] { 10f, true });
        Check(Near(critical, 22.5f), "Bille irisée : un projectile critique de base 10 inflige réellement 22,5 au lieu de 20");
    }

    private void CheckStatItemRecovery()
    {
        Setup();
        _player.DisableDefenseForTests();
        _player.TakeDamage(40f);
        _player.AddItemCopy("pastille_mentholee");
        _player.AddItemCopy("pastille_mentholee");
        MethodInfo regen = typeof(Player).GetMethod("ApplyRegen", Private);
        float hp = _player.CurrentHp;
        regen.Invoke(_player, new object[] { 2f });
        bool blocked = Near(hp, _player.CurrentHp);
        regen.Invoke(_player, new object[] { 10f });
        Check(blocked && Near(_player.CurrentHp - hp, (_player.BaseRegenRate + 0.16f) * 10f),
            "Pastilles : aucun soin pendant les deux secondes après blessure ; puis 9,6 PV/min ajoutés au soin réel");
        hp = _player.CurrentHp;
        _player.AddItemCopy("bouillotte");
        bool noHeal = Near(_player.CurrentHp, hp);
        _player.ForgetItemCopy("bouillotte");
        _player.AddItemCopy("bouillotte");
        Check(noHeal && Near(_player.CurrentHp, hp), "Bouillotte : acquisition et échange ne rendent aucun PV");

        Setup();
        _player.AddItemCopy("agrafe_rouillee");
        _player.AddItemCopy("agrafe_rouillee");
        hp = _player.CurrentHp;
        _player.TakeDamage(10f);
        Check(Near(hp - _player.CurrentHp, 10f * 30f / 32f), "Agrafes : deux points d’armure réduisent réellement un coup de 10 à 9,38 PV");
        _player.AddItemCopy("boucle_cabossee");
        PlayerDefense defense = (PlayerDefense)typeof(Player).GetField("_defense", Private).GetValue(_player);
        bool empty = Near(_player.Shield, 0f);
        defense.Step(10f);
        hp = _player.CurrentHp;
        _player.TakeDamage(5f);
        Check(empty && Near(_player.MaxShield, 15f) && Near(_player.CurrentHp, hp) && _player.Shield < 15f,
            "Boucle : d’abord vide, recharge réelle puis absorption d’un coup sans perte de PV");

        Setup();
        _player.DisableDefenseForTests();
        _player.TakeDamage(40f);
        _player.AddItemCopy("ampoule_rouge");
        _player.AddItemCopy("ampoule_rouge");
        Enemy target = SpawnEnemy();
        CurrentHp.SetValue(target, 10000f);
        hp = _player.CurrentHp;
        target.TakeDamage(20f, source: _player.BeginAttack(_player.EquippedWeapon, 20f));
        typeof(Player).GetMethod("StepLifesteal", Private).Invoke(_player, new object[] { 0.25f });
        Check(Near(_player.CurrentHp - hp, 0.06f), "Ampoules : deux copies soignent réellement 0,06 PV sur un coup de 20, loin du plafond de haut build");
        target.QueueFree();
    }

    private void CheckNewItemPresentation()
    {
        Setup();
        bool described = true;
        foreach (PassiveSouvenirData data in PassiveSouvenirDataLoader.GetAll())
        {
            if (data.IsBadge) continue;
            int copies = data.HasCopyLimit ? data.MaxCopies : 8;
            string text = ItemPresentation.Describe(data, copies, _player) + data.Flavor;
            described &= text.Length > 0 && !text.Contains('{') && !text.Contains("STAT_", StringComparison.Ordinal);
        }
        Check(described, "Les 56 fiches décrivent leurs copies sans clé brute ni paramètre manquant");
        Check(ItemPresentation.Describe(PassiveSouvenirDataLoader.Get("pastille_mentholee"), 2).Contains("9,6 PV/min", StringComparison.Ordinal),
            "Deux Pastilles : la fiche annonce 9,6 PV/min, pas un soin chaque minute");
        Check(ItemPresentation.Describe(PassiveSouvenirDataLoader.Get("pied_de_biche"), 8).StartsWith("60 %", StringComparison.Ordinal)
            && ItemPresentation.Describe(PassiveSouvenirDataLoader.Get("etiquette_perdue"), 8).Contains("jusqu’à 3 à la fois", StringComparison.Ordinal),
            "Copies illimitées : gratuité plafonnée à 60 % et directions à trois dans les descriptions");
    }
}
