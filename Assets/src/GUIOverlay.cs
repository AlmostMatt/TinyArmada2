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

	void OnGUI() {
		// Unit groups
		int i = 0;
		foreach (UnitGroup grp in Scene.get().getGroups()) {
			GUI.color = grp.color;
			Rect groupRect = new Rect(Camera.main.pixelWidth - 55,5 + (50 + 5) * i ,50,50);
			string label = grp.numUnits.ToString();
			if (GUI.Button(groupRect, label)) {
				//Camera.main.transform.LookAt(grp.center);
				Vector3 newPos = new Vector3(grp.center.x, grp.center.y, Camera.main.transform.position.z);
				Camera.main.transform.position = newPos;
			}
			++i;
		}
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

	/*
	 *  unused
	void drawCircle(Vector2 p, float r) {
		Rect rect = getRect(p - new Vector2(r, r),
			p + new Vector2(r, r));
		GUI.DrawTexture(rect, selectImg);
	}
	*/
}

