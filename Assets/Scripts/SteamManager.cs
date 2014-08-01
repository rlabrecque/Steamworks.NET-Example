using UnityEngine;
using System.Collections;
using Steamworks;

// This is a stripped down port of SpaceWar's Steam Integration.
// Make sure you set SteamManager to load first in the Script Execution Order by setting it to a negative number.
// http://docs.unity3d.com/Manual/class-ScriptExecution.html
class SteamManager : MonoBehaviour {
	private static SteamManager m_instance;

	private SteamStatsAndAchievements m_StatsAndAchievements;
	public static SteamStatsAndAchievements StatsAndAchievements { get { return m_instance.m_StatsAndAchievements; } }

	private bool m_bInitialized;
	public static bool Initialized { get { return m_instance.m_bInitialized; } }

	private SteamAPIWarningMessageHook_t m_SteamAPIWarningMessageHook;
	private static void SteamAPIDebugTextHook(int nSeverity, System.Text.StringBuilder pchDebugText) {
		Debug.LogWarning(pchDebugText);
	}

	private void Awake() {
		// Only one instance of SteamManager at a time!
		if (m_instance != null) {
			Destroy(gameObject);
			return;
		}
		m_instance = this;

		// We want our SteamManager Instance to persist across scenes.
		DontDestroyOnLoad(gameObject);

		m_StatsAndAchievements = GetComponent<SteamStatsAndAchievements>();

		try {
			// If Steam is not running or the game wasn't started through Steam, SteamAPI_RestartAppIfNecessary starts the 
			// Steam client and also launches this game again if the User owns it. This can act as a rudimentary form of DRM.

			// Once you get a Steam AppID assigned by Valve, you need to replace AppId_t.Invalid with it and
			// remove steam_appid.txt from the game depot. eg: "(AppId_t)480" or "new AppId_t(480)".
			// See the Valve documentation for more information: https://partner.steamgames.com/documentation/drm#FAQ
			if (SteamAPI.RestartAppIfNecessary(AppId_t.Invalid)) {
				Application.Quit();
				return;
			}
		}
		catch (System.DllNotFoundException e) { // We catch this exception here, as it will be the first occurence of it.
			Debug.LogError("[Steamworks.NET] Could not load [lib]steam_api.dll/so/dylib. It's likely not in the correct location. Refer to the README for more details.\n" + e, this);

			Application.Quit();
			return;
		}

		// Initialize the SteamAPI, if Init() returns false this can happen for many reasons.
		// Some examples include:
		// Steam Client is not running.
		// Launching from outside of steam without a steam_appid.txt file in place.
		// https://partner.steamgames.com/documentation/example // Under: Common Build Problems
		// https://partner.steamgames.com/documentation/bootstrap_stats // At the very bottom

		// If you're running into Init issues try running DbgView prior to launching to get the internal output from Steam.
		// http://technet.microsoft.com/en-us/sysinternals/bb896647.aspx
		m_bInitialized = SteamAPI.Init();
		if (!m_bInitialized) {
			Debug.LogError("[Steamworks.NET] SteamAPI_Init() failed. Refer to Valve's documentation or the comment above this line for more information.", this);

			return;
		}
	}

	// This should only ever get called after an Assembly reload, You should never Disable the Steamworks Manager yourself.
	private void OnEnable() {
		if (m_instance == null) {
			m_instance = this;
		}

		if (!m_bInitialized) {
			return;
		}

		if (m_SteamAPIWarningMessageHook == null) {
			// Set up our callback to recieve warning messages from Steam.
			// You must launch with "-debug_steamapi" in the launch args to recieve warnings.
			m_SteamAPIWarningMessageHook = new SteamAPIWarningMessageHook_t(SteamAPIDebugTextHook);
			SteamClient.SetWarningMessageHook(m_SteamAPIWarningMessageHook);
		}
	}

	private void OnDestroy() {
		if (!m_bInitialized) {
			return;
		}

		SteamAPI.Shutdown();
	}

	private void Update() {
		if (!m_bInitialized) {
			return;
		}

		// Run Steam client callbacks
		SteamAPI.RunCallbacks();
	}
}
