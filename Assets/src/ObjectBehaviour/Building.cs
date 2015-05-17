using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum BuildingType {BASE=0, COLONY=1};

public class Building : MonoBehaviour {
	
	public bool dead = false;
	public Player owner;

	private float amount = 0f;
	public float maxAmount = 100f;
	public Resource resource = Resource.FOOD;
	private float productionRate = 1f;

	public float radius = 0.25f;
	
	private int maxHealth = 10;
	private int health;
	private float hpPercent = 1f;

	public Vector2 tilePos;
	public Vector3 gamePos;
	public BuildingType type;
	private Transform dock;

	private List<UnitType> trains = new List<UnitType>();

	// Use this for initialization
	void Start () {
		amount = 0f;
	}
	
	// Update is called once per frame
	void Update () {
		amount = Mathf.Min(amount + productionRate * Time.deltaTime, maxAmount);
	}

	public float collect(float capacity) {
		float amountTaken = Mathf.Min(capacity, amount); 
		amount -= amountTaken;
		return amountTaken;
	}

	public void init(Vector2 tileCoordinate, BuildingType buildingType) {
		type = buildingType;
		tilePos = tileCoordinate;
		Map map = Scene.get().map;
		gamePos = map.mapToGame(tileCoordinate);
		transform.position = gamePos;

		RandomSet<Vector2> rs = new RandomSet<Vector2>();
		foreach (Vector2 nbor in map.getNeighbours4(tileCoordinate)) {
			if (map.isWalkable(map.getTile(nbor))) {
				rs.Add(nbor);
			}
		}
		Vector2 dockSide = rs.popRandom() - tileCoordinate;
		float angle = Mathf.Rad2Deg * Mathf.Atan2(dockSide.y, dockSide.x);
		transform.FindChild("dockRotation").localEulerAngles = new Vector3(0f,0f,angle);
		dock = transform.FindChild("dockRotation/dock");

		trains.Add(UnitType.MERCHANT);
		trains.Add(UnitType.GALLEY);
		trains.Add(UnitType.LONGBOAT);
	}
	
	public void setOwner(Player p) {
		owner = p;
		p.buildings.Add(this);
		Transform teamColor = transform.FindChild("team-color");
		if (teamColor != null) {
			teamColor.GetComponent<SpriteRenderer>().color = owner.color;
		}
	}

	public List<UnitType> getTrains() {
		return trains;
	}

	// fits the "RadialMenu callback"
	public void trainUnit(int selected) {
		if (selected != -1) {
			UnitType unitType = trains[selected];
			var cost = UnitData.getCost(unitType);
			if (owner.spend(cost)) {
				Scene.get().spawnUnit(this, unitType);
			}
		}
	}

	public Vector2 getDock() {
		return dock.position;
	}
}
