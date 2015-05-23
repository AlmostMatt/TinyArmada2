using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Scene : MonoBehaviour {

	// Objects to be set in the editor
	public GameObject unitObject;
	public Texture2D selectImg;
	public GameObject flagObject;
	public UnityEngine.UI.Text[] resources;
	public RadialMenu radial;

	[HideInInspector]
	public List<Unit> units;
	[HideInInspector]
	public List<Player> players = new List<Player>();
	[HideInInspector]
	public List<Building> buildings;
	[HideInInspector]
	public Map map;
	[HideInInspector]
	public bool paused = false;
	[HideInInspector]
	public int HUMAN_PLAYER = 1;

	private HashSet<UnitGroup> groups;
	private Vector2 prevMousePos;
	private Vector2 rClickPos;
	// left click / drag info
	private Vector2 clickPos;
	private Clickable clickObject;
	private Clickable hoverObject;

	private List<Unit> hover;
	private bool selecting = false;

	private UnitGroup selected = null;
	private static Scene singleton;

	// Use this for initialization
	void Start () {
		singleton = this;
		units = new List<Unit>();
		hover = new List<Unit>();
		groups = new HashSet<UnitGroup>();

		// create players
		for (int i = 0; i <= 4; i++) {
			players.Add(new Player(i));
			if (players[i].isHuman) {
				HUMAN_PLAYER = i;
				players[i].setGUI(resources);
			}
		}

		//map = GetComponent<Map>();
		map.generateMap(players);
		buildings = map.getBuildings();

		players[HUMAN_PLAYER].tradeWithNClosest(3);
	}
	
	Vector2 getWorldMousePos() {
		Ray ray = Camera.main.ScreenPointToRay (Input.mousePosition);
		return ray.origin;
	}

	Vector2 getGUIMousePos() {
		return screenToGUI(Input.mousePosition);
	}
	
	Vector2 worldToGUI(Vector2 p) {
		return screenToGUI(Camera.main.WorldToScreenPoint(p));
	}
	
	float worldToGUI(float f) {
		// TODO: compute this once and later just multiply f
		return worldToGUI(new Vector2(f, 0)).x - worldToGUI(new Vector2(0, 0)).x;
	}
	
	Vector2 screenToGUI(Vector2 p) {
		return new Vector2(p.x, Screen.height - p.y);
	}

	public void spawnUnit(Building building, UnitType unitType) {
        // units should be in front of buildings
		Unit newUnit = Instantiate(unitObject).GetComponent<Unit>();
		Vector2 pos = building.getDock();
		newUnit.transform.position = new Vector3(pos.x, pos.y, -1);
		newUnit.init(building.owner, unitType);
		units.Add(newUnit);
		Vector3 offset = (Vector3) building.getDock() - building.gamePos;
		if (unitType != UnitType.MERCHANT) {
			UnitGroup newGroup = createUnitGroup();
			newUnit.setGroup(newGroup);
			newGroup.setDest(building.gamePos + 2.5f * offset);
		} else {
			newUnit.moveTo(building.gamePos + 1.5f * offset, newUnit.radius);
		}
	}

	// Update is called once per frame
	void Update () {
		handleInput();

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

		// update groups (position/empty)
		foreach (UnitGroup grp in groups) {
			if (grp.isEmpty()) {
				grp.cleanup();
				if (clickObject == grp) {
					clickObject = null;
				}
				if (hoverObject == grp) {
					hoverObject = null;
				}
			}
		}
		groups.RemoveWhere(grp => grp.isEmpty());
		foreach (UnitGroup grp in groups) {
			grp.update();
		}

		// update neighbours
		foreach (Unit u1 in units) {
			u1.neighbours.Clear();
			u1.nearbyBuildings.Clear();
			// compare to previous units, not later units)
			foreach (Unit u2 in units) {
				if (u2 == u1) {
					break;
				}
				float dist = (u1.transform.position - u2.transform.position).sqrMagnitude;
				u1.neighbours.Add(u2, dist);
				u2.neighbours.Add(u1, dist);
			}
			foreach (Building b in buildings) {
				float dist = (b.gamePos - u1.transform.position).sqrMagnitude;
				u1.nearbyBuildings.Add(b, dist);
			}
		}
	}

	private Clickable getObject(Vector2 position) {
		Clickable ret = null;
		// TODO: pick closest object or multiple objects if they overlap
		foreach(Building building in buildings) {
			if (building.clickTest(0, position)) {
				ret = building;
			}
		}
		foreach (UnitGroup grp in groups) {
			if (grp.clickTest(0, position)) {
				ret = grp;
			}
		}
		return ret;
	}

	private void handleInput() {
		// TODO: generalize right click input and 2 finger input
		// TODO: use input events
		Vector2 worldMousePos = getWorldMousePos();
		/* Camera Pan */
		if (Input.GetMouseButtonDown(1)) {
			rClickPos = worldMousePos;
			radial.cancel();
		} else if (Input.GetMouseButton(1)) {
			Vector2 offset = rClickPos - worldMousePos;
			Camera.main.transform.Translate(offset);
		}

		if (!Input.GetMouseButton(0)) {
			Clickable obj = getObject(worldMousePos);
			if (obj != hoverObject) {
				if (hoverObject != null) {
					hoverObject.setHover(false);
				}
				if (obj != null) {
					obj.setHover(true);
				}
				hoverObject = obj;
			}
		}

		if (Input.GetMouseButtonDown(0)) {
			if (hoverObject != null) {
				hoverObject.setHover(false);
				hoverObject = null;
			}
			clickPos = worldMousePos;
			clickObject = getObject(worldMousePos);
			if (clickObject != null) {
				clickObject.handleMouseDown(0, clickPos);
			}
		} else if (Input.GetMouseButton(0) && clickObject != null) {
			Vector2 delta = worldMousePos - prevMousePos;
			if (delta.x != 0 || delta.y != 0) {
				Vector2 relativePos = worldMousePos - clickPos;
				clickObject.handleDrag(0, delta, relativePos);
			}
		} else if (Input.GetMouseButtonUp(0) && clickObject != null) {
			if ((worldMousePos - clickPos).sqrMagnitude < 0.5f * 0.5f) {
				clickObject.handleClick(0);
			}
			clickObject.handleMouseUp(0, worldMousePos);
		}
		prevMousePos = worldMousePos;
		/*
		//

			float minD = float.MaxValue;
			selected = null;
			foreach (UnitGroup grp in groups) {
				float dist = Vector2.Distance(clickPos, grp.center);
				if (dist < grp.radius && dist < minD) {
					minD = dist;
					selected = grp;
				}
			}
			
			
			if (selected == null && radial.visible == false) {
				selecting = true;
			}
		}
		
		if (selected != null) {
			if (Input.GetMouseButton(0)) {
				foreach (Unit unit in selected) {
					unit.moveTo(getWorldMousePos(), selected.radius);
					unit.tradeDest = null;
				}
			} else {
				selected = null;
			}
		}
		
		if (selecting && Input.GetMouseButton(0)) {
			foreach( Unit unit in hover) {
				unit.deselect();
			}
			hover.Clear();
			Vector2 mousePos = getWorldMousePos();
			Vector2 p = (mousePos + clickPos)/2;
			float rr = (mousePos - p).sqrMagnitude;
			//Rect rect = getRect (mousePos, clickPos);
			foreach( Unit unit in units) {
				if (!unit.owner.isHuman || unit.type == UnitType.MERCHANT) {
					continue;
				}
				//if (rect.Contains(unit.transform.position)) {
				if ((((Vector2) unit.transform.position) - p).sqrMagnitude <= rr) {
					unit.select();
					hover.Add(unit);
				}
			}
		}
		if (Input.GetMouseButtonUp(0) && hover.Count == 0) {
			// tapping on a unit should make it a singleton group
			Vector2 mousePos = getWorldMousePos();
			if (Vector2.Distance(mousePos, clickPos) < 0.25f) {
				Vector2 p = (mousePos + clickPos)/2;
				Unit closest = null;
				float minD = 0.25f;
				foreach(Unit unit in units) {
					float dist = Vector2.Distance(p, unit.transform.position);
					if (dist < minD) {
						closest = unit;
						minD = dist;
					}
				}
				if (closest != null) {
					selecting = true;
					hover.Add (closest);
				}
			}
		}
		
		if (selecting && Input.GetMouseButtonUp(0)) {
			if (hover.Count > 0) {
				UnitGroup newGroup = createUnitGroup();
				foreach (Unit unit in hover) {
					unit.setGroup(newGroup);
				}
				newGroup.update();
				foreach (Unit unit in hover) {
					unit.moveTo(newGroup.center, newGroup.radius);
				}
			}
			selecting = false;
			foreach( Unit unit in hover) {
				unit.deselect();
			}
			hover.Clear();
		}
		
		if (Input.GetMouseButtonDown(1)) {
			// whether or not you clicked on a group, check for single select
			foreach (Unit unit in hover) {
				unit.moveTo(getWorldMousePos(), 0.3f * Mathf.Sqrt(hover.Count));
			}
		}

		if (Input.GetKeyDown(KeyCode.Q)) {
			Unit newUnit = Instantiate(unitObject).GetComponent<Unit>();
			newUnit.transform.position = getWorldMousePos();
			units.Add(newUnit);
			newUnit.init (players[HUMAN_PLAYER], UnitType.MERCHANT);
		}
		 */
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

		// resources/numbers/status

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
