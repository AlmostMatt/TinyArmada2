using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GUIOverlay : MonoBehaviour
{
	// references to UI objects
	public RadialMenu radialMenu;
	public UnityEngine.UI.Text[] resourceTextUIObjects;

	private Player currentPlayer;
	private static GUIOverlay singleton;

	void Awake () {
		singleton = this;
	}

	public static GUIOverlay get() {
		return singleton;
	}

	public void setPlayer(Player player) {
		currentPlayer = player;
		player.setGUI(resourceTextUIObjects);
	}

	public void createRadial(Vector3 worldPosition, List<Sprite> icons, RadialMenuCallback callbackFunction) {
		radialMenu.setup(getScreenPosition(worldPosition), icons, callbackFunction);
	}

	private Vector2 getScreenPosition(Vector3 worldPosition) {
		return Camera.main.WorldToScreenPoint(worldPosition);
	}
}

