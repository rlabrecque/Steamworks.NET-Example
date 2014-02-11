using UnityEngine;
using System.Collections;
using Steamworks;

class SpaceWarClient : MonoBehaviour {
	private static SpaceWarClient m_instance;
	public static bool Instance {
		get {
			return m_instance;
		}
	}
	private StatsAndAchievements m_StatsAndAchievements;

	private EClientGameState m_eGameState = EClientGameState.k_EClientGameMenu;
	private bool m_bTransitionedGameState = true;
	private float m_ulStateTransitionTime;

	private void Start() {
		m_StatsAndAchievements = SteamManager.StatsAndAchievements;

		m_ulStateTransitionTime = Time.time;
	}

	private void OnEnable() {
		m_instance = this;
	}

	private void Update() {
		if (Input.GetKeyDown(KeyCode.Escape)) {
			Application.Quit();
		}

		if (m_bTransitionedGameState) {
			m_bTransitionedGameState = false;
			OnGameStateChanged(m_eGameState);
		}
	}

	private void OnGUI() {
		GUILayout.Label("Game State: " + m_eGameState);
		GUILayout.Space(10);
		m_StatsAndAchievements.Render();
		GUILayout.Space(10);

		if (GUILayout.Button("Set State to Menu")) {
			SetGameState(EClientGameState.k_EClientGameMenu);
		}
		if(GUILayout.Button("Set State to Active")) {
			SetGameState(EClientGameState.k_EClientGameActive);
		}
		if (GUILayout.Button("Set State to Winner")) {
			SetGameState(EClientGameState.k_EClientGameWinner);
		}
		if (GUILayout.Button("Travel Distance (100)")) {
			m_StatsAndAchievements.AddDistanceTraveled(100.0f);
		}
	}

	//-----------------------------------------------------------------------------
	// Purpose: Used to transition game state
	//-----------------------------------------------------------------------------
	private void SetGameState(EClientGameState eState) {
		if (m_eGameState == eState)
			return;

		m_bTransitionedGameState = true;
		m_ulStateTransitionTime = Time.time;
		m_eGameState = eState;

		// Let the stats handler check the state (so it can detect wins, losses, etc...)
		m_StatsAndAchievements.OnGameStateChange(eState);

		// update any rich presence state
		//UpdateRichPresenceConnectionInfo();
	}

	//-----------------------------------------------------------------------------
	// Purpose: does work on transitioning from one game state to another
	//-----------------------------------------------------------------------------
	private void OnGameStateChanged(EClientGameState eGameStateNew) {
		if (m_eGameState == EClientGameState.k_EClientGameMenu) {
			// we've switched out to the main menu

			// Tell the server we have left if we are connected
			//DisconnectFromServer();

			// shut down any server we were running
			//if (m_pServer) {
			//	delete m_pServer;
			//	m_pServer = NULL;
			//}

			SteamFriends.SetRichPresence("status", "Main menu");
		}
		else if (m_eGameState == EClientGameState.k_EClientGameActive) {
			// start voice chat 
			//m_pVoiceChat->StartVoiceChat();
			SteamFriends.SetRichPresence("status", "In match");
		}
	}

	public static bool BLocalPlayerWonLastGame() {
		//todo: not implemented
		return true;
	}
}
