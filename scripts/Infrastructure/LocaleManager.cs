using Godot;
using Vestiges.Infrastructure.Steam;

namespace Vestiges.Infrastructure;

/// <summary>
/// Gestionnaire de langue. Autoload.
/// Détecte la langue Steam si disponible, sinon utilise la langue système.
/// Persiste le choix en user://settings.cfg.
/// </summary>
public partial class LocaleManager : Node
{
	public static LocaleManager Instance { get; private set; }

	private const string SettingsPath = "user://settings.cfg";
	private const string DefaultLocale = "fr";

	/// <summary>Langues supportées (code ISO → nom affiché).</summary>
	public static readonly System.Collections.Generic.Dictionary<string, string> SupportedLocales = new()
	{
		["fr"] = "Français",
		["en"] = "English",
	};

	/// <summary>Mapping des language codes Steam vers les locales Godot.</summary>
	private static readonly System.Collections.Generic.Dictionary<string, string> SteamToGodot = new()
	{
		["french"] = "fr",
		["english"] = "en",
		["brazilian"] = "en",    // fallback EN
		["german"] = "en",
		["spanish"] = "en",
		["latam"] = "en",
		["italian"] = "en",
		["japanese"] = "en",
		["koreana"] = "en",
		["polish"] = "en",
		["portuguese"] = "en",
		["russian"] = "en",
		["schinese"] = "en",
		["tchinese"] = "en",
		["turkish"] = "en",
	};

	public string CurrentLocale { get; private set; } = DefaultLocale;
	public string CurrentLocaleName => SupportedLocales.TryGetValue(CurrentLocale, out string name) ? name : CurrentLocale;

	public override void _EnterTree()
	{
		Instance = this;
		DetectAndApplyLocale();
	}

	/// <summary>Change la langue et persiste le choix.</summary>
	public void SetLocale(string locale)
	{
		if (!SupportedLocales.ContainsKey(locale))
			return;

		CurrentLocale = locale;
		TranslationServer.SetLocale(locale);
		SaveLocale(locale);
		GD.Print($"[LocaleManager] Locale set to: {locale}");
	}

	/// <summary>Cycle vers la langue suivante (pour un bouton toggle).</summary>
	public string CycleLocale()
	{
		var keys = new System.Collections.Generic.List<string>(SupportedLocales.Keys);
		int idx = keys.IndexOf(CurrentLocale);
		string next = keys[(idx + 1) % keys.Count];
		SetLocale(next);
		return next;
	}

	private void DetectAndApplyLocale()
	{
		// Priorité 1 : choix sauvegardé par le joueur
		string saved = LoadSavedLocale();
		if (saved != null && SupportedLocales.ContainsKey(saved))
		{
			SetLocale(saved);
			return;
		}

		// Priorité 2 : langue Steam
		if (SteamManager.IsActive)
		{
			string steamLang = Steamworks.SteamApps.GetCurrentGameLanguage();
			if (!string.IsNullOrEmpty(steamLang) && SteamToGodot.TryGetValue(steamLang, out string mapped))
			{
				SetLocale(mapped);
				return;
			}
		}

		// Priorité 3 : langue système
		string osLocale = OS.GetLocaleLanguage();
		string godotLocale = SupportedLocales.ContainsKey(osLocale) ? osLocale : DefaultLocale;
		SetLocale(godotLocale);
	}

	private static string LoadSavedLocale()
	{
		ConfigFile cfg = new();
		if (cfg.Load(SettingsPath) != Error.Ok)
			return null;
		return cfg.GetValue("general", "locale", Variant.CreateFrom("")).AsString();
	}

	private static void SaveLocale(string locale)
	{
		ConfigFile cfg = new();
		cfg.Load(SettingsPath); // charge les autres sections existantes
		cfg.SetValue("general", "locale", locale);
		cfg.Save(SettingsPath);
	}
}
