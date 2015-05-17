using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Scene : MonoBehaviour {
	public GameObject unitObject;
	public Texture2D selectImg;

	public List<Unit> units;
	private HashSet<UnitGroup> groups;
	public List<Player> players = new List<Player>();
	public List<Building> buildings;

	private Vector2 rClickPos;
	private Vector2 clickPos;
	private List<Unit> hover;
	private bool selecting = false;

	private UnitGroup selected = null;
	public Map map;
	private static Scene singleton;

	public UnityEngine.UI.Text[] resources;
	public RadialMenu radial;

	public bool paused = false;
	public int HUMAN_PLAYER = 1;

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

		// spawn units
		/*
		foreach (Player p in players) {
			if (p.isNeutral) continue;
			UnitGroup newGroup = new UnitGroup();
			groups.Add(newGroup);
			Vector3 gpos = map.mapToGame(p.spawnPos);
			//Vector3 gpos = new Vector3(Random.Range(-4f, 4f),
			//                           Random.Range(-4f, 4f), 0);
			for (int n = 0; n < 3; n++) {
				Unit newUnit = Instantiate(unitObject).GetComponent<Unit>();
				newUnit.transform.position = gpos + new Vector3(Random.Range(-2f, 2f),
				                                                Random.Range(-2f, 2f), 0);
				units.Add(newUnit);
				newUnit.setGroup(newGroup);
				newUnit.setOwner(p);
			}
			newGroup.update(); // compute group center
			foreach (Unit unit in newGroup) {
				unit.moveTo(newGroup.center, newGroup.radius);
			}
		}
		*/
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
		if (unitType != UnitType.MERCHANT) {
			UnitGroup newGroup = new UnitGroup();
			newUnit.setGroup(newGroup);
			groups.Add(newGroup);
		}
		Vector3 offset = (Vector3) building.getDock() - building.gamePos;
		newUnit.moveTo(building.gamePos + 2.5f * offset, newUnit.radius);
	}

	// Update is called once per frame
	void Update () {
		handleInput();

		foreach (Player p in players) {
			p.think();
		}

		// update group positions for hud/interactions
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

	private void handleInput() {
		if (Input.GetMouseButtonDown(1)) {
			rClickPos = getWorldMousePos();
			radial.cancel();
		} else if (Input.GetMouseButton(1)) {
			Vector2 offset = rClickPos - getWorldMousePos();
			Camera.main.transform.Translate(offset);
			// in theory after the translate the old pos is accurate again
		}

		// TODO: cleanup selection code
		if (Input.GetMouseButtonDown(0)) {
			clickPos = getWorldMousePos();
			float minD = float.MaxValue;
			selected = null;
			foreach (UnitGroup grp in groups) {
				float dist = Vector2.Distance(clickPos, grp.center);
				if (dist < grp.radius && dist < minD) {
					minD = dist;
					selected = grp;
				}
			}
			
			foreach(Building building in buildings) {
				float dist = Vector2.Distance(clickPos, building.gamePos);
				if (dist < 0.75f) {
					if (building.type == BuildingType.BASE && building.owner.isHuman) {
						List<Sprite> icons = new List<Sprite>();
						foreach (UnitType uType in building.getTrains()) {
							icons.Add(UnitData.getIcon(uType));
						}
						radial.mouseDown(Camera.main.WorldToScreenPoint(building.transform.position),
						                 icons, building.trainUnit);
					} else if (building.type == BuildingType.COLONY) {
						players[1].toggleTrading(building);
					}
					break;
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
				UnitGroup newGroup = new UnitGroup();
				foreach (Unit unit in hover) {
					unit.setGroup(newGroup);
				}
				newGroup.update();
				foreach (Unit unit in hover) {
					unit.moveTo(newGroup.center, newGroup.radius);
				}
				groups.Add(newGroup);
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
			newUnit.init (players[1], UnitType.MERCHANT);
			//UnitGroup newGroup = new UnitGroup();
			//newUnit.setGroup(newGroup);
			//groups.Add(newGroup);
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
		//GUIStyle buttonStyle = GUI.skin.button;

		foreach (UnitGroup grp in groups) {
			GUI.color = grp.color;
			drawCircle(worldToGUI(grp.center), worldToGUI(grp.radius));
		}

		if (selecting) {
			Vector2 p1 = worldToGUI(clickPos);
			Vector2 p2 = getGUIMousePos();
			Vector2 pavg = (p1 + p2)/2;
			float r = (p2 - pavg).magnitude;
			GUI.color = new Color(0,1,0, 0.5f);
			drawCircle(pavg, r);
		}

		// Minimap
		Rect mapRect = new Rect(10,10,100,100);
		GUI.color = Color.gray;
		//GUI.Button(mapRect, "MiniMap");

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
}
