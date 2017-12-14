using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Scene : MonoBehaviour {

	// Objects to be set in the editor
	public GameObject unitObject;
	public Texture2D selectImg;
	public GameObject flagObject;

	public Map map;

	[HideInInspector]
	public List<Unit> units;
	[HideInInspector]
	public List<Player> players = new List<Player>();
	[HideInInspector]
	public List<Building> buildings;
	[HideInInspector]
	public bool paused = false;
	[HideInInspector]
	public int HUMAN_PLAYER = 1;

	private HashSet<UnitGroup> groups;

	private List<Unit> hover;
	private bool selecting = false;

	private UnitGroup selected = null;
	private static Scene singleton;

	void Awake() {
		singleton = this;
	}

	void Start () {
		units = new List<Unit>();
		hover = new List<Unit>();
		groups = new HashSet<UnitGroup>();

		// create players
		for (int i = 0; i <= 4; i++) {
			players.Add(new Player(i));
			if (players[i].isHuman) {
				HUMAN_PLAYER = i;
				GUIOverlay.get().setPlayer(players[i]);
			}
		}

		map.generateMap(players);
		buildings = map.getBuildings();
		players[HUMAN_PLAYER].tradeWithNClosest(3);
	}

	public void spawnUnit(Building building, UnitType unitType) {
        // units should be in front of buildings
		Unit newUnit = Instantiate(unitObject).GetComponent<Unit>();
		Vector2 pos = building.getDock();
		newUnit.transform.position = new Vector3(pos.x, pos.y, -1);
		newUnit.init(building.owner, unitType);
		units.Add(newUnit);
		Vector3 offset = (Vector3) building.getDock() - building.gamePos;
		if (!newUnit.canTrade()) {
			UnitGroup newGroup = createUnitGroup();
			newUnit.setGroup(newGroup);
			newGroup.setDest(building.gamePos + 2.5f * offset);
		} else {
			newUnit.moveTo(building.gamePos + 1.5f * offset, newUnit.radius);
		}
	}

	// Update is called once per frame
	void Update () {
		foreach (Player p in players) {
			p.think();
		}

		// dead units
		for (int i = units.Count - 1; i >= 0; --i) {
			Unit u = units[i];
			if (u.dead) {
				u.setGroup(null);
				hover.Remove(u);
				units.RemoveAt(i);
				Destroy(u.gameObject);
			}
		}

		// TODO: remove group-related logic
		// It provided a way to give commands to a group of units, but I want units to be autonomous
		// update groups (position/empty)
		foreach (UnitGroup grp in groups) {
			if (grp.isEmpty()) {
				grp.cleanup();
				// TODO: handle groups that are deleted while selected hovered or clicked
			}
		}
		groups.RemoveWhere(grp => grp.isEmpty());
		foreach (UnitGroup grp in groups) {
			grp.update();
		}

		// update neighbours
		foreach (Unit unit in units) {
			unit.nearbyUnits.Clear();
			unit.nearbyBuildings.Clear();
			// compare to previous units, not later units)
			foreach (Unit otherUnit in units) {
				if (otherUnit == unit) {
					break;
				}
				float dd = (otherUnit.transform.position - unit.transform.position).sqrMagnitude;
				unit.nearbyUnits.Add(otherUnit, dd);
			}
			foreach (Building b in buildings) {
				float dist = (b.gamePos - unit.transform.position).sqrMagnitude;
				unit.nearbyBuildings.Add(b, dist);
			}
		}
	}

	public IEnumerable<Clickable> getClickableObjects() {
		foreach(Building building in buildings) {
			yield return building;
		}
		foreach (UnitGroup grp in groups) {
			yield return grp;
		}
	}

	Rect getRect(Vector2 p1, Vector2 p2) {
		float x1 = Mathf.Min (p1.x, p2.x);
		float y1 = Mathf.Min (p1.y, p2.y);
		float w = Mathf.Abs (p1.x - p2.x);
		float h = Mathf.Abs (p1.y - p2.y);
		return new Rect(x1, y1, w, h);
	}

	void drawCircle(Vector2 p, float r) {
		Rect rect = getRect(p - new Vector2(r, r),
		                    p + new Vector2(r, r));
		GUI.DrawTexture(rect, selectImg);
	}
	
	void OnGUI() {

		// Unit groups
		//for (int i=0; i<4; ++i) {
		int i = 0;
		foreach (UnitGroup grp in groups) {
			GUI.color = grp.color;
			Rect groupRect = new Rect(Camera.main.pixelWidth - 55,5 + (50 + 5) * i ,50,50);
			//i.ToString()
			string label = grp.numUnits.ToString();
			if (GUI.Button(groupRect, label)) {
				//Camera.main.transform.LookAt(grp.center);
				Vector3 newPos = new Vector3(grp.center.x, grp.center.y, Camera.main.transform.position.z);
				Camera.main.transform.position = newPos;
			}
			++i;
		}
	}

	public static Scene get() {
		return singleton;
	}

	public UnitGroup createUnitGroup() {
		UnitGroup group = new UnitGroup();
		GameObject flag = Instantiate(flagObject);
		group.setFlag(flag);
		groups.Add(group);
		return group;
	}
}
