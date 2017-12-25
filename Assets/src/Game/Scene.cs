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
		Player.humanHasMoved = true;
        // units should be in front of buildings
		Unit newUnit = Instantiate(unitObject).GetComponent<Unit>();
		Vector2 pos = building.getDock();
		newUnit.transform.position = new Vector3(pos.x, pos.y, 0f);
		newUnit.init(building.owner, unitType);
		foreach (Unit otherUnit in units) {
			newUnit.nearbyUnits.Add(otherUnit.GetComponent<Steering>());
			otherUnit.nearbyUnits.Add(newUnit.GetComponent<Steering>());
		}
		newUnit.nearbyBuildings.AddRange(buildings);
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

	private void removeUnit(Unit unit, int unitIndex) {
		unit.setGroup(null);
		hover.Remove(unit);
		units.RemoveAt(unitIndex);
		foreach (Unit otherUnit in units) {
			otherUnit.nearbyUnits.Remove(unit.GetComponent<Steering>());
		}
		Destroy(unit.gameObject);
	}

	// Update is called once per frame
	void Update () {
		foreach (Player p in players) {
			p.think();
		}

		// dead units
		for (int i = units.Count - 1; i >= 0; --i) {
			Unit unit = units[i];
			if (unit.dead) {
				removeUnit(unit, i);
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
	}

	public IEnumerable<Clickable> getClickableObjects() {
		foreach(Building building in buildings) {
			yield return building;
		}
		foreach (UnitGroup grp in groups) {
			yield return grp;
		}
	}

	public IEnumerable<UnitGroup> getGroups() {
		return groups;
	}

	Rect getRect(Vector2 p1, Vector2 p2) {
		float x1 = Mathf.Min (p1.x, p2.x);
		float y1 = Mathf.Min (p1.y, p2.y);
		float w = Mathf.Abs (p1.x - p2.x);
		float h = Mathf.Abs (p1.y - p2.y);
		return new Rect(x1, y1, w, h);
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
