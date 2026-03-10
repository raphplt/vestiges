using Godot;
using Steamworks;

namespace Vestiges.Infrastructure.Steam;

/// <summary>
/// Gestionnaire global Steam. Autoload.
/// Initialise le SDK, pompe les callbacks, et expose l'état de connexion.
/// En mode développement (pas de Steam), tout est désactivé silencieusement.
/// </summary>
public partial class SteamManager : Node
{
	public static SteamManager Instance { get; private set; }

	/// <summary>true si le SDK Steam est initialisé et fonctionnel.</summary>
	public static bool IsActive { get; private set; }

	/// <summary>App ID Steam. 480 = Spacewar (test). Remplacer par le vrai App ID en production.</summary>
	private const uint AppId = 480;

	public override void _EnterTree()
	{
		Instance = this;
		InitializeSteam();
	}

	public override void _Process(double delta)
	{
		if (IsActive)
			SteamAPI.RunCallbacks();
	}

	public override void _ExitTree()
	{
		if (IsActive)
		{
			SteamAPI.Shutdown();
			IsActive = false;
			GD.Print("[SteamManager] Steam API shut down.");
		}
		Instance = null;
	}

	private void InitializeSteam()
	{
		if (IsActive)
			return;

		// SteamAPI.RestartAppIfNecessary relance le jeu via Steam si besoin.
		// En dev, le fichier steam_appid.txt override ce comportement.
		try
		{
			if (SteamAPI.RestartAppIfNecessary(new AppId_t(AppId)))
			{
				GD.Print("[SteamManager] Restarting via Steam client...");
				GetTree().Quit();
				return;
			}
		}
		catch (System.DllNotFoundException)
		{
			GD.PushWarning("[SteamManager] steam_api64.dll not found — Steam disabled. Place the DLL from the Steamworks SDK in the project root.");
			return;
		}

		if (!SteamAPI.Init())
		{
			GD.PushWarning("[SteamManager] SteamAPI.Init() failed — is Steam running? Steam features disabled.");
			return;
		}

		IsActive = true;
		string playerName = SteamFriends.GetPersonaName();
		CSteamID steamId = SteamUser.GetSteamID();
		GD.Print($"[SteamManager] Steam initialized. Player: {playerName} (ID: {steamId})");
	}

	/// <summary>Retourne le nom Steam du joueur, ou "Player" si Steam inactif.</summary>
	public static string GetPlayerName()
	{
		return IsActive ? SteamFriends.GetPersonaName() : "Player";
	}

	/// <summary>Retourne le Steam ID, ou CSteamID.Nil si inactif.</summary>
	public static CSteamID GetSteamId()
	{
		return IsActive ? SteamUser.GetSteamID() : CSteamID.Nil;
	}
}
