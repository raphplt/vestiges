using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Godot;
using Steamworks;

namespace Vestiges.Infrastructure.Steam;

/// <summary>
/// Gestionnaire global Steam. Autoload.
/// Initialise le SDK, pompe les callbacks, et expose l'état de connexion.
/// En mode développement (pas de Steam), tout est désactivé silencieusement.
/// L'assemblage Steamworks.NET n'existe qu'en x86/x64 : sur un autre processus (Mac Apple Silicon),
/// le simple JIT d'une méthode qui nomme un type Steam lève une exception. Tout appel au SDK passe donc
/// par une méthode <c>NoInlining</c> atteinte seulement quand Steam est actif.
/// </summary>
public partial class SteamManager : Node
{
	public static SteamManager Instance { get; private set; }

	/// <summary>true si le SDK Steam est initialisé et fonctionnel.</summary>
	public static bool IsActive { get; private set; }

	/// <summary>Une session ayant servi aux essais ne soumet plus de résultats avant relancement.</summary>
	internal void DisableForDevelopmentSession()
	{
		if (IsActive)
			ShutdownSdk();
		IsActive = false;
		SetProcess(false);
	}

	private static bool IsSupportedProcess =>
		RuntimeInformation.ProcessArchitecture is Architecture.X64 or Architecture.X86;

	/// <summary>App ID Steam. 480 = Spacewar (test). Remplacer par le vrai App ID en production.</summary>
	private const uint AppId = 480;

	public override void _EnterTree()
	{
		Instance = this;
		InitializeSteam();
	}

	public override void _Ready()
	{
		SetProcess(IsActive);
	}

	public override void _Process(double delta)
	{
		if (IsActive)
			RunCallbacks();
	}

	public override void _ExitTree()
	{
		if (IsActive)
		{
			ShutdownSdk();
			IsActive = false;
			GD.Print("[SteamManager] Steam API shut down.");
		}
		Instance = null;
	}

	private void InitializeSteam()
	{
		if (DevelopmentMode.IsEnabled)
		{
			GD.Print("[SteamManager] Profil dev : succès et classements Steam désactivés.");
			return;
		}

		if (IsActive)
			return;

		if (!IsSupportedProcess)
		{
			GD.Print($"[SteamManager] Steamworks.NET indisponible en {RuntimeInformation.ProcessArchitecture} : Steam désactivé.");
			return;
		}

		IsActive = TryInitializeSdk();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private bool TryInitializeSdk()
	{
		// SteamAPI.RestartAppIfNecessary relance le jeu via Steam si besoin.
		// En dev, le fichier steam_appid.txt override ce comportement.
		try
		{
			if (SteamAPI.RestartAppIfNecessary(new AppId_t(AppId)))
			{
				GD.Print("[SteamManager] Restarting via Steam client...");
				GetTree().Quit();
				return false;
			}
		}
		catch (System.DllNotFoundException)
		{
			GD.PushWarning("[SteamManager] steam_api64.dll not found — Steam disabled. Place the DLL from the Steamworks SDK in the project root.");
			return false;
		}

		if (!SteamAPI.Init())
		{
			GD.PushWarning("[SteamManager] SteamAPI.Init() failed — is Steam running? Steam features disabled.");
			return false;
		}

		string playerName = SteamFriends.GetPersonaName();
		CSteamID steamId = SteamUser.GetSteamID();
		GD.Print($"[SteamManager] Steam initialized. Player: {playerName} (ID: {steamId})");
		return true;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void RunCallbacks() => SteamAPI.RunCallbacks();

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void ShutdownSdk() => SteamAPI.Shutdown();

	/// <summary>Langue du jeu choisie dans Steam, ou null si Steam inactif.</summary>
	public static string GetGameLanguage()
	{
		return IsActive ? ReadGameLanguage() : null;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static string ReadGameLanguage() => SteamApps.GetCurrentGameLanguage();

	/// <summary>Retourne le nom Steam du joueur, ou "Player" si Steam inactif.</summary>
	public static string GetPlayerName()
	{
		return IsActive ? ReadPersonaName() : "Player";
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static string ReadPersonaName() => SteamFriends.GetPersonaName();

	/// <summary>Retourne le Steam ID, ou CSteamID.Nil si inactif.</summary>
	public static CSteamID GetSteamId()
	{
		return IsActive ? SteamUser.GetSteamID() : CSteamID.Nil;
	}
}
