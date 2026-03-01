using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Progression;

namespace Vestiges.UI;

/// <summary>
/// Écran de choix au level up — affiche des Fragments de Mémoire (armes + passifs).
/// Pause le jeu, affiche 3 choix avec icônes type, reprend après sélection.
/// Supporte aussi les choix de perks via Mémorial (source monde).
/// </summary>
public partial class LevelUpScreen : CanvasLayer
{
    private PanelContainer _panel;
    private VBoxContainer _container;
    private Label _title;
    private Label _synergyNotification;
    private PerkManager _perkManager;
    private FragmentManager _fragmentManager;
    private EventBus _eventBus;

    public override void _Ready()
    {
        _panel = GetNode<PanelContainer>("Panel");
        _container = GetNode<VBoxContainer>("Panel/Padding/VBox");
        _title = GetNode<Label>("Panel/Padding/VBox/Title");

        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.FragmentChoicesReady += OnFragmentChoicesReady;

        CreateSynergyNotification();
        Hide();
    }

    public override void _ExitTree()
    {
        if (_eventBus != null)
            _eventBus.FragmentChoicesReady -= OnFragmentChoicesReady;
    }

    public void SetPerkManager(PerkManager perkManager)
    {
        _perkManager = perkManager;
        _perkManager.PerkChoicesReady += OnPerkChoicesReady;
        _perkManager.SynergyActivated += OnSynergyActivated;
    }

    public void SetFragmentManager(FragmentManager fragmentManager)
    {
        _fragmentManager = fragmentManager;
        GD.Print($"[LevelUpScreen] FragmentManager wired, listening on EventBus.FragmentChoicesReady");
    }

    private void CreateSynergyNotification()
    {
        _synergyNotification = new Label();
        _synergyNotification.HorizontalAlignment = HorizontalAlignment.Center;
        _synergyNotification.VerticalAlignment = VerticalAlignment.Center;
        _synergyNotification.AddThemeFontSizeOverride("font_size", 22);
        _synergyNotification.AddThemeColorOverride("font_color", new Color(1f, 0.85f, 0.2f));
        _synergyNotification.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.CenterTop);
        _synergyNotification.OffsetTop = 80;
        _synergyNotification.Visible = false;
        _synergyNotification.ProcessMode = ProcessModeEnum.Always;
        AddChild(_synergyNotification);
    }

    private void OnSynergyActivated(string synergyId, string notification)
    {
        _synergyNotification.Text = notification;
        _synergyNotification.Visible = true;
        _synergyNotification.Modulate = new Color(1f, 1f, 1f, 1f);

        Tween tween = CreateTween();
        tween.TweenInterval(2.0f);
        tween.TweenProperty(_synergyNotification, "modulate:a", 0f, 1.0f);
        tween.TweenCallback(Callable.From(() => _synergyNotification.Visible = false));
    }

    // --- Fragment Mode (level-up: armes + passifs) ---

    private void OnFragmentChoicesReady(int count)
    {
        GD.Print($"[LevelUpScreen] OnFragmentChoicesReady received: count={count}");

        if (count <= 0 || _fragmentManager == null)
        {
            GD.PushWarning($"[LevelUpScreen] Aborting: count={count}, fragmentManager={(_fragmentManager != null ? "OK" : "NULL")}");
            return;
        }

        System.Collections.Generic.IReadOnlyList<FragmentOption> choices = _fragmentManager.PendingChoices;
        GD.Print($"[LevelUpScreen] PendingChoices.Count={choices.Count}");
        if (choices.Count == 0)
            return;

        ClearButtons();
        BuildFragmentButtons(choices);
        Show();
        GetTree().Paused = true;
        ProcessMode = ProcessModeEnum.Always;
        GD.Print($"[LevelUpScreen] Fragment screen shown with {choices.Count} choices");
    }

    private void BuildFragmentButtons(System.Collections.Generic.IReadOnlyList<FragmentOption> choices)
    {
        _title.Text = "FRAGMENT DE MÉMOIRE";

        foreach (FragmentOption choice in choices)
        {
            string label = BuildFragmentLabel(choice.Id, choice.Type);
            Color color = GetFragmentColor(choice.Type);

            Button button = new()
            {
                CustomMinimumSize = new Vector2(380, 55),
                Text = label
            };

            button.AddThemeColorOverride("font_color", color);
            button.AddThemeColorOverride("font_hover_color", color);

            string capturedId = choice.Id;
            string capturedType = choice.Type;
            button.Pressed += () => OnFragmentSelected(capturedId, capturedType);

            _container.AddChild(button);
        }
    }

    private string BuildFragmentLabel(string id, string type)
    {
        Player player = GetTree().GetFirstNodeInGroup("player") as Player;

        switch (type)
        {
            case "weapon_new":
                WeaponData weapon = WeaponDataLoader.Get(id);
                return weapon != null
                    ? $"\u2694 {weapon.Name} — {weapon.Description ?? weapon.AttackPattern} [NOUVEAU]"
                    : $"\u2694 {id} [NOUVEAU]";

            case "weapon_upgrade":
                WeaponData wUpgrade = WeaponDataLoader.Get(id);
                int wLevel = player?.GetWeaponFragmentLevel(id) ?? 0;
                return wUpgrade != null
                    ? $"\u2694 {wUpgrade.Name} — Niveau {wLevel} \u2192 {wLevel + 1}"
                    : $"\u2694 {id} — Upgrade";

            case "passive_new":
                PassiveSouvenirData passive = PassiveSouvenirDataLoader.Get(id);
                return passive != null
                    ? $"\u25C8 {passive.Name} — {passive.Description} [NOUVEAU]"
                    : $"\u25C8 {id} [NOUVEAU]";

            case "passive_upgrade":
                PassiveSouvenirData pUpgrade = PassiveSouvenirDataLoader.Get(id);
                int pLevel = player?.GetPassiveLevel(id) ?? 0;
                return pUpgrade != null
                    ? $"\u25C8 {pUpgrade.Name} — Niveau {pLevel} \u2192 {pLevel + 1}"
                    : $"\u25C8 {id} — Upgrade";

            default:
                return id;
        }
    }

    private static Color GetFragmentColor(string type)
    {
        return type switch
        {
            "weapon_new" => new Color(1f, 0.85f, 0.3f),
            "weapon_upgrade" => new Color(1f, 0.65f, 0.2f),
            "passive_new" => new Color(0.5f, 0.85f, 1f),
            "passive_upgrade" => new Color(0.3f, 0.7f, 0.95f),
            _ => new Color(0.9f, 0.9f, 0.9f)
        };
    }

    private void OnFragmentSelected(string fragmentId, string fragmentType)
    {
        _fragmentManager?.SelectFragment(fragmentId, fragmentType);
        Hide();
        GetTree().Paused = false;
    }

    // --- Perk Mode (mémorial, world perks) ---

    private void OnPerkChoicesReady(string[] perkIds)
    {
        if (perkIds == null || perkIds.Length == 0)
            return;

        ClearButtons();
        BuildPerkButtons(perkIds);
        Show();
        GetTree().Paused = true;
        ProcessMode = ProcessModeEnum.Always;
    }

    private void BuildPerkButtons(string[] perkIds)
    {
        _title.Text = "MÉMORIAL";

        foreach (string perkId in perkIds)
        {
            PerkData data = PerkDataLoader.Get(perkId);
            if (data == null)
                continue;

            int currentStacks = _perkManager.GetStacks(perkId);
            string stackText = $"({currentStacks}/{data.MaxStacks})";

            Button button = new()
            {
                CustomMinimumSize = new Vector2(340, 55),
                Text = $"{data.Name} — {data.Description} {stackText}"
            };

            Color rarityColor = GetRarityColor(data.Rarity);
            button.AddThemeColorOverride("font_color", rarityColor);
            button.AddThemeColorOverride("font_hover_color", rarityColor);

            string capturedId = perkId;
            button.Pressed += () => OnPerkSelected(capturedId);

            _container.AddChild(button);
        }
    }

    private static Color GetRarityColor(string rarity)
    {
        return rarity switch
        {
            "common" => new Color(0.9f, 0.9f, 0.9f),
            "uncommon" => new Color(0.4f, 0.73f, 0.42f),
            "rare" => new Color(1f, 0.7f, 0f),
            _ => new Color(0.9f, 0.9f, 0.9f)
        };
    }

    private void OnPerkSelected(string perkId)
    {
        _perkManager.SelectPerk(perkId);
        Hide();
        GetTree().Paused = false;
    }

    // --- Common ---

    private void ClearButtons()
    {
        foreach (Node child in _container.GetChildren())
        {
            if (child is Button)
                child.QueueFree();
        }
    }

    private new void Show()
    {
        _panel.Visible = true;
        Visible = true;
    }

    private new void Hide()
    {
        _panel.Visible = false;
        Visible = false;
    }
}
