using UnityEngine;
using System.Collections;
using Steamworks;

class SteamManager : MonoBehaviour {
	private static SteamManager m_instance;
	public static SteamManager Instance {
		get {
			return m_instance;
		}
	}

	private StatsAndAchievements m_StatsAndAchievements;
	public StatsAndAchievements StatsAndAchievements {
		get {
			return m_StatsAndAchievements;
		}
	}

	private bool m_bInitialized = false;
	public bool Initialized {
		get {
			return m_bInitialized;
		}
	}


	void Awake() {
		if (m_instance != null) {
			Destroy(gameObject);
			return;
		}

		if (SteamAPI.RestartAppIfNecessary(AppId_t.Invalid)) {
			// if Steam is not running or the game wasn't started through Steam, SteamAPI_RestartAppIfNecessary starts the 
			// local Steam client and also launches this game again.

			// Once you get a public Steam AppID assigned for this game, you need to replace k_uAppIdInvalid with it and
			// removed steam_appid.txt from the game depot.
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
		if (!(m_bInitialized = SteamAPI.InitSafe())) {
			Debug.LogError("SteamAPI_Init() failed");

			Application.Quit();
			return;
		}

		if ((m_StatsAndAchievements = GetComponent<StatsAndAchievements>()) == null) {
			m_StatsAndAchievements = gameObject.AddComponent<StatsAndAchievements>();
		}

		// Tell Steam where it's overlay should show notification dialogs, this can be top right, top left,
		// bottom right, bottom left. The default position is the bottom left if you don't call this.  
		// Generally you should use the default and not call this as users will be most comfortable with 
		// the default position.  The API is provided in case the bottom right creates a serious conflict 
		// with important UI in your game.
		SteamUtils.SetOverlayNotificationPosition(ENotificationPosition.k_EPositionTopRight);

		m_instance = this;
		DontDestroyOnLoad(gameObject);
	}

	void OnApplicationQuit() {
		SteamAPI.Shutdown();
	}

	void FixedUpdate() {
		// Run Steam client callbacks
		CallbackDispatcher.RunCallbacks();
	}
}
