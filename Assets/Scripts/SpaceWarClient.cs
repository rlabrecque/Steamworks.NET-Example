using UnityEngine;
using System.Collections;

class SpaceWarClient : MonoBehaviour {
	private static SpaceWarClient m_instance;
	public static bool Instance {
		get {
			return m_instance;
		}
	}

	void Awake() {
		m_instance = this;
	}

	public static bool BLocalPlayerWonLastGame() {
		//todo: not implemented
		return true;
	}
}