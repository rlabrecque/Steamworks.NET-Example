using UnityEngine;
using System.Collections;
using System.ComponentModel;
using Steamworks;

class StatsAndAchievements : MonoBehaviour {
	public enum Achievement : int {
		ACH_WIN_ONE_GAME = 0,
		ACH_WIN_100_GAMES = 1,
		ACH_HEAVY_FIRE = 2,
		ACH_TRAVEL_FAR_ACCUM = 3,
		ACH_TRAVEL_FAR_SINGLE = 4,
	};

	class Achievement_t {
		public Achievement m_eAchievementID;
		public string m_rgchName;
		public string m_rgchDescription;
		public bool m_bAchieved;
		public int m_iIconImage;

		public Achievement_t(Achievement achievement, string name, string desc, bool achieved, int icon) {
			m_eAchievementID = achievement;
			m_rgchName = name;
			m_rgchDescription = desc;
			m_bAchieved = achieved;
			m_iIconImage = icon;
		}
	}

	static Achievement_t[] m_Achievements = new Achievement_t[] {
             new Achievement_t(Achievement.ACH_WIN_ONE_GAME, "Winner", "", false, 0),
             new Achievement_t(Achievement.ACH_WIN_100_GAMES, "Champion", "", false, 0),
			 new Achievement_t(Achievement.ACH_TRAVEL_FAR_ACCUM, "Interstellar", "", false, 0),
             new Achievement_t(Achievement.ACH_TRAVEL_FAR_SINGLE, "Orbiter", "", false, 0)
        };

	private static StatsAndAchievements m_instance;
	public static StatsAndAchievements Instance {
		get {
			return m_instance;
		}
	}

	SteamManager m_SteamManager;

	Callback<UserStatsReceived_t> m_CallbackUserStatsReceived;
	Callback<UserStatsStored_t> m_CallbackUserStatsStored;
	Callback<UserAchievementStored_t> m_CallbackAchievementStored;

	// our GameID
	ulong m_GameID;

	// Did we get the stats from Steam?
	bool m_bRequestedStats;
	bool m_bStatsValid;

	// Should we store stats this frame?
	bool m_bStoreStats;

	// Current Stat details
	float m_flGameFeetTraveled;
	float m_ulTickCountGameStart;
	double m_flGameDurationSeconds;

	// Persisted Stat details
	int m_nTotalGamesPlayed;
	int m_nTotalNumWins;
	int m_nTotalNumLosses;
	float m_flTotalFeetTraveled;
	float m_flMaxFeetTraveled;
	float m_flAverageSpeed;

	//-----------------------------------------------------------------------------
	// Purpose: Constructor
	//-----------------------------------------------------------------------------
	void Awake() {
		if (m_instance != null) {
			Destroy(gameObject);
			return;
		}
		m_instance = this;
	}

	void Start() {
		m_SteamManager = GetComponent<SteamManager>();
#if UNITY_EDITOR
		if (!m_SteamManager) {
			Debug.LogError("StatsAndAchievements must be added to the same Game Object as SteamManager.");
		}
#endif

		m_GameID = SteamUtils.GetAppID();

		m_CallbackUserStatsReceived = new Callback<UserStatsReceived_t>(OnUserStatsReceived);
		m_CallbackUserStatsStored = new Callback<UserStatsStored_t>(OnUserStatsStored);
		m_CallbackAchievementStored = new Callback<UserAchievementStored_t>(OnAchievementStored);
	}

	//-----------------------------------------------------------------------------
	// Purpose: Run a frame for the CStatsAndAchievements
	//-----------------------------------------------------------------------------
	void FixedUpdate() {
		if (!m_bRequestedStats) {
			// Is Steam Loaded? if no, can't get stats, done
			if (!m_SteamManager.Initialized) {
				m_bRequestedStats = true;
				return;
			}
			
			// If yes, request our stats
			bool bSuccess = SteamUserStats.RequestCurrentStats();

			// This function should only return false if we weren't logged in, and we already checked that.
			// But handle it being false again anyway, just ask again later.
			m_bRequestedStats = bSuccess;
		}

		if (!m_bStatsValid)
			return;

		// Get info from sources

		// Evaluate achievements
		foreach (Achievement_t achievement in m_Achievements) {
			EvaluateAchievement(achievement);
		}

		// Store stats
		StoreStatsIfNecessary();
	}

	//-----------------------------------------------------------------------------
	// Purpose: Accumulate distance traveled
	//-----------------------------------------------------------------------------
	public void AddDistanceTraveled(float flDistance) {
		m_flGameFeetTraveled += flDistance; // todo: convert to feet!
	}

	//-----------------------------------------------------------------------------
	// Purpose: Game state has changed
	//-----------------------------------------------------------------------------
	public void OnGameStateChange(EClientGameState eNewState) {
		if (!m_bStatsValid)
			return;

		switch (eNewState) {
			case EClientGameState.k_EClientStatsAchievements:
			case EClientGameState.k_EClientGameStartServer:
			case EClientGameState.k_EClientGameMenu:
			case EClientGameState.k_EClientGameQuitMenu:
			case EClientGameState.k_EClientGameExiting:
			case EClientGameState.k_EClientGameInstructions:
			case EClientGameState.k_EClientGameConnecting:
			case EClientGameState.k_EClientGameConnectionFailure:
			default:
				break;
			case EClientGameState.k_EClientGameActive:
				// Reset per-game stats
				m_flGameFeetTraveled = 0;
				m_ulTickCountGameStart = Time.time;
				break;
			case EClientGameState.k_EClientFindInternetServers:
				break;
			case EClientGameState.k_EClientGameWinner:
				if (SpaceWarClient.BLocalPlayerWonLastGame())
					m_nTotalNumWins++;
				else
					m_nTotalNumLosses++;
				// fall through
				goto case EClientGameState.k_EClientGameDraw;
			case EClientGameState.k_EClientGameDraw:

				// Tally games
				m_nTotalGamesPlayed++;

				// Accumulate distances
				m_flTotalFeetTraveled += m_flGameFeetTraveled;

				// New max?
				if (m_flGameFeetTraveled > m_flMaxFeetTraveled)
					m_flMaxFeetTraveled = m_flGameFeetTraveled;

				// Calc game duration
				m_flGameDurationSeconds = Time.time - m_ulTickCountGameStart;

				// We want to update stats the next frame.
				m_bStoreStats = true;

				break;
		}
	}

	//-----------------------------------------------------------------------------
	// Purpose: see if we should unlock this achievement
	//-----------------------------------------------------------------------------
	void EvaluateAchievement(Achievement_t achievement) {
		// Already have it?
		if (achievement.m_bAchieved)
			return;

		switch (achievement.m_eAchievementID) {
			case Achievement.ACH_WIN_ONE_GAME:
				if (m_nTotalNumWins != 0) {
					UnlockAchievement(achievement);
				}
				break;
			case Achievement.ACH_WIN_100_GAMES:
				if (m_nTotalNumWins >= 100) {
					UnlockAchievement(achievement);
				}
				break;
			case Achievement.ACH_TRAVEL_FAR_ACCUM:
				if (m_flTotalFeetTraveled >= 5280) {
					UnlockAchievement(achievement);
				}
				break;
			case Achievement.ACH_TRAVEL_FAR_SINGLE:
				if (m_flGameFeetTraveled > 500) {
					UnlockAchievement(achievement);
				}
				break;
		}
	}

	//-----------------------------------------------------------------------------
	// Purpose: Unlock this achievement
	//-----------------------------------------------------------------------------
	void UnlockAchievement(Achievement_t achievement) {
		achievement.m_bAchieved = true;

		// the icon may change once it's unlocked
		achievement.m_iIconImage = 0;

		// mark it down
		SteamUserStats.SetAchievement(achievement.m_eAchievementID.ToString());

		// Store stats end of frame
		m_bStoreStats = true;
	}
	
	//-----------------------------------------------------------------------------
	// Purpose: Store stats in the Steam database
	//-----------------------------------------------------------------------------
	void StoreStatsIfNecessary() {
		if (m_bStoreStats) {
			// already set any achievements in UnlockAchievement

			// set stats
			SteamUserStats.SetStat("NumGames", m_nTotalGamesPlayed);
			SteamUserStats.SetStat("NumWins", m_nTotalNumWins);
			SteamUserStats.SetStat("NumLosses", m_nTotalNumLosses);
			SteamUserStats.SetStat("FeetTraveled", m_flTotalFeetTraveled);
			SteamUserStats.SetStat("MaxFeetTraveled", m_flMaxFeetTraveled);
			// Update average feet / second stat
			SteamUserStats.UpdateAvgRateStat("AverageSpeed", m_flGameFeetTraveled, m_flGameDurationSeconds);
			// The averaged result is calculated for us
			SteamUserStats.GetStat("AverageSpeed", out m_flAverageSpeed);

			bool bSuccess = SteamUserStats.StoreStats();
			// If this failed, we never sent anything to the server, try
			// again later.
			m_bStoreStats = !bSuccess;
		}
	}

	//-----------------------------------------------------------------------------
	// Purpose: We have stats data from Steam. It is authoritative, so update
	//			our data with those results now.
	//-----------------------------------------------------------------------------
	void OnUserStatsReceived(UserStatsReceived_t pCallback) {
		if (!m_SteamManager.Initialized)
			return;

		// we may get callbacks for other games' stats arriving, ignore them
		if (m_GameID == pCallback.m_nGameID) {
			if (EResult.k_EResultOK == pCallback.m_eResult) {
				Debug.Log("Received stats and achievements from Steam\n");

				m_bStatsValid = true;

				// load achievements
				foreach (Achievement_t ach in m_Achievements) {
					SteamUserStats.GetAchievement(ach.m_eAchievementID.ToString(), out ach.m_bAchieved);
					ach.m_rgchName = SteamUserStats.GetAchievementDisplayAttribute(ach.m_eAchievementID.ToString(), "name");
					ach.m_rgchDescription = SteamUserStats.GetAchievementDisplayAttribute(ach.m_eAchievementID.ToString(), "desc");
				}

				// load stats
				SteamUserStats.GetStat("NumGames", out m_nTotalGamesPlayed);
				SteamUserStats.GetStat("NumWins", out m_nTotalNumWins);
				SteamUserStats.GetStat("NumLosses", out m_nTotalNumLosses);
				SteamUserStats.GetStat("FeetTraveled", out m_flTotalFeetTraveled);
				SteamUserStats.GetStat("MaxFeetTraveled", out m_flMaxFeetTraveled);
				SteamUserStats.GetStat("AverageSpeed", out m_flAverageSpeed);
			}
			else {
				Debug.Log("RequestStats - failed, " + pCallback.m_eResult);
			}
		}
	}

	//-----------------------------------------------------------------------------
	// Purpose: Our stats data was stored!
	//-----------------------------------------------------------------------------
	void OnUserStatsStored(UserStatsStored_t pCallback) {
		// we may get callbacks for other games' stats arriving, ignore them
		if (m_GameID == pCallback.m_nGameID) {
			if (EResult.k_EResultOK == pCallback.m_eResult) {
				Debug.Log("StoreStats - success");
			}
			else if (EResult.k_EResultInvalidParam == pCallback.m_eResult) {
				// One or more stats we set broke a constraint. They've been reverted,
				// and we should re-iterate the values now to keep in sync.
				Debug.Log("StoreStats - some failed to validate");
				// Fake up a callback here so that we re-load the values.
				UserStatsReceived_t callback = new UserStatsReceived_t();
				callback.m_eResult = EResult.k_EResultOK;
				callback.m_nGameID = m_GameID;
				OnUserStatsReceived(callback);
			}
			else {
				Debug.Log("StoreStats - failed, " + pCallback.m_eResult);
			}
		}
	}

	//-----------------------------------------------------------------------------
	// Purpose: An achievement was stored
	//-----------------------------------------------------------------------------
	void OnAchievementStored(UserAchievementStored_t pCallback) {
		// we may get callbacks for other games' stats arriving, ignore them
		if (m_GameID == pCallback.m_nGameID) {
			if (0 == pCallback.m_nMaxProgress) {
				Debug.Log("Achievement '" + pCallback.m_rgchAchievementName + "' unlocked!");
			}
			else {
				Debug.Log("Achievement '" + pCallback.m_rgchAchievementName + "' progress callback, (" + pCallback.m_nCurProgress + "," + pCallback.m_nMaxProgress + ")");
			}
		}
	}

	//-----------------------------------------------------------------------------
	// Purpose: Display the user's stats and achievements
	//-----------------------------------------------------------------------------
	public void Render() {
		GUILayout.Label("m_ulTickCountGameStart: " + m_ulTickCountGameStart);
		GUILayout.Label("m_flGameDurationSeconds: " + m_flGameDurationSeconds);
		GUILayout.Label("m_flGameFeetTraveled: " + m_flGameFeetTraveled);
		GUILayout.Space(10);
		GUILayout.Label("NumGames: " + m_nTotalGamesPlayed);
		GUILayout.Label("NumWins: " + m_nTotalNumWins);
		GUILayout.Label("NumLosses: " + m_nTotalNumLosses);
		GUILayout.Label("FeetTraveled: " + m_flTotalFeetTraveled);
		GUILayout.Label("MaxFeetTraveled: " + m_flMaxFeetTraveled);
		GUILayout.Label("AverageSpeed: " + m_flAverageSpeed);
	}
}
