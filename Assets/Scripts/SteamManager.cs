using UnityEngine;
using System.Collections;
using Steamworks;

class SteamManager : MonoBehaviour {
	private static SteamManager m_instance;

	private StatsAndAchievements m_StatsAndAchievements;
	public static StatsAndAchievements StatsAndAchievements {
		get {
			return m_instance.m_StatsAndAchievements;
		}
	}

	private bool m_bInitialized = false;
	public static bool Initialized {
		get {
			return m_instance.m_bInitialized;
		}
	}

	SteamAPIWarningMessageHook_t SteamAPIWarningMessageHook;
	static void SteamAPIDebugTextHook(int nSeverity, System.Text.StringBuilder pchDebugText) {
		Debug.LogWarning(pchDebugText);
	}

	private void Awake() {
		// Only one instance of Steamworks at a time!
		if (m_instance != null) {
			Destroy(gameObject);
			return;
		}

		// We want our Steam Instance to persist across scenes.
		DontDestroyOnLoad(gameObject);

		try {
			if (SteamAPI.RestartAppIfNecessary(AppId_t.Invalid)) {
				// If Steam is not running or the game wasn't started through Steam, SteamAPI_RestartAppIfNecessary starts the 
				// local Steam client and also launches this game again.

				// Once you get a public Steam AppID assigned for this game, you need to replace k_uAppIdInvalid with it and
				// removed steam_appid.txt from the game depot.
				Application.Quit();
				return;
			}
		}
		catch (System.DllNotFoundException e) { // We catch this exception here, as it will be the first occurence of it.
			Debug.LogError("[Steamworks] Could not load [lib]steam_api.dll/so/dylib. It's likely not in the correct location. Refer to the README for more details.\n" + e, this);

			Application.Quit();
			return;
		}

		// Initialize SteamAPI, if this fails we bail out since we depend on Steam for lots of stuff.
		// You don't necessarily have to though if you write your code to check whether all the Steam
		// interfaces are NULL before using them and provide alternate paths when they are unavailable.
		//
		// This will also load the in-game steam overlay dll into your process.  That dll is normally
		// injected by steam when it launches games, but by calling this you cause it to always load,
		// even when not launched via steam.
		m_bInitialized = SteamAPI.Init();
		if (!m_bInitialized) {
			Debug.LogError("[Steamworks] SteamAPI_Init() failed", this);

			Application.Quit();
			return;
		}

		// Set up our callback to recieve warning messages from Steam.
		// You must launch with "-debug_steamapi" in the launch args to recieve warnings.
		SteamAPIWarningMessageHook = new SteamAPIWarningMessageHook_t(SteamAPIDebugTextHook);
		SteamClient.SetWarningMessageHook(SteamAPIWarningMessageHook);

		m_StatsAndAchievements = GetComponent<StatsAndAchievements>();
		if (m_StatsAndAchievements == null) {
			m_StatsAndAchievements = gameObject.AddComponent<StatsAndAchievements>();
		}

		// Tell Steam where it's overlay should show notification dialogs, this can be top right, top left,
		// bottom right, bottom left. The default position is the bottom left if you don't call this.  
		// Generally you should use the default and not call this as users will be most comfortable with 
		// the default position.  The API is provided in case the bottom right creates a serious conflict 
		// with important UI in your game.
		SteamUtils.SetOverlayNotificationPosition(ENotificationPosition.k_EPositionTopRight);
	}

	private void OnEnable() {
		// These should only get called after an Assembly reload, You should probably never Disable the Steamworks Manager yourself.
		if (m_instance == null) {
			m_instance = this;
		}

		if (SteamAPIWarningMessageHook == null) {
			SteamAPIWarningMessageHook = new SteamAPIWarningMessageHook_t(SteamAPIDebugTextHook);
			SteamClient.SetWarningMessageHook(SteamAPIWarningMessageHook);
		}
	}

	private void OnDestroy() {
		if (m_bInitialized) {
			SteamAPI.Shutdown();
		}
	}

	private void FixedUpdate() {
		// Run Steam client callbacks
		SteamAPI.RunCallbacks();
	}
}
