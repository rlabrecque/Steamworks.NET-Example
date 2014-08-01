using UnityEngine;
using System.Collections;
using Steamworks;

// Enum for possible game states on the client
enum EClientGameState {
	k_EClientGameActive,
	k_EClientGameWinner,
	k_EClientGameLoser,
};

class SpaceWarClient : MonoBehaviour {
	private void OnEnable() {
		SteamManager.StatsAndAchievements.OnGameStateChange(EClientGameState.k_EClientGameActive);
	}

	private void OnGUI() {
		SteamManager.StatsAndAchievements.Render();
		GUILayout.Space(10);

		if(GUILayout.Button("Set State to Active")) {
			SteamManager.StatsAndAchievements.OnGameStateChange(EClientGameState.k_EClientGameActive);
		}
		if (GUILayout.Button("Set State to Winner")) {
			SteamManager.StatsAndAchievements.OnGameStateChange(EClientGameState.k_EClientGameWinner);
		}
		if (GUILayout.Button("Set State to Loser")) {
			SteamManager.StatsAndAchievements.OnGameStateChange(EClientGameState.k_EClientGameLoser);
		}
		if (GUILayout.Button("Add Distance Traveled +100")) {
			SteamManager.StatsAndAchievements.AddDistanceTraveled(100.0f);
		}
	}
}
