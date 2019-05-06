using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityBaseCode.Steering;

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

		// Create Players (0 is neutral)
		for (int i = 0; i <= 2; i++) {
			players.Add(new Player(i));
		}

		map.generateMap(players);
		buildings = map.getBuildings();
		Player humanPlayer = players[Player.HUMAN_PLAYER];
		GUIOverlay.get().setPlayer(humanPlayer);
		humanPlayer.tradeWithNClosest(3);
		InputWrapper.FocusCameraOn(humanPlayer.getBase().getPosition());
	}

	public void spawnUnit(Building building, UnitType unitType) {
		Player.humanHasMoved = true;
        // units should be in front of buildings
		Unit newUnit = Instantiate(unitObject).GetComponent<Unit>();
		Vector2 pos = building.getDock();
		// Add slight randomization so that simultaneously trained units don't have identical positions.
		newUnit.transform.position = new Vector3(pos.x, pos.y + Random.Range(-0.1f, 0.1f), 0f);
		newUnit.init(building.owner, unitType);
		foreach (Unit otherUnit in units) {
			newUnit.nearbyUnits.Add(otherUnit.GetComponent<Steering>());
			otherUnit.nearbyUnits.Add(newUnit.GetComponent<Steering>());
		}
		newUnit.nearbyBuildings.AddRange(buildings);
		units.Add(newUnit);
		Vector2 offset = (Vector2) building.getDock() - (Vector2) building.gamePos;
		if (!newUnit.canTrade()) {
			UnitGroup newGroup = createUnitGroup(newUnit);
			newGroup.setDest(building.getDock() + 0.6f * offset);
		} else {
			newUnit.moveTo((Vector2) building.getDock() + 0.2f * offset, newUnit.radius);
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
		foreach (AILogic ai in Player.getAIPlayers()) {
			ai.think();
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

	public static Map getMap() {
		return singleton.map;
	}

	public static List<Player> getPlayers() {
		return singleton.players;
	}

	public static List<Unit> getUnits() {
		return singleton.units;
	}

	public UnitGroup createUnitGroup(Unit unit) {
		return createUnitGroup(new List<Unit>(){unit});
	}

	public UnitGroup createUnitGroup(List<Unit> units) {
		UnitGroup group = new UnitGroup(units, Instantiate(flagObject));
		groups.Add(group);
		return group;
	}
}
