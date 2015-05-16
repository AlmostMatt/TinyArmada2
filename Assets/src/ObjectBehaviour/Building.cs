using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum BuildingType {BASE=0, COLONY=1};

public class Building : MonoBehaviour {
	
	public bool dead = false;
	public Player owner;

	public float amount = 100f;
	public float maxAmount = 100f;
	public Resource resource = Resource.FOOD;
	private float productionRate = 10f;

	public float radius = 0.25f;
	
	private int maxHealth = 10;
	private int health;
	private float hpPercent = 1f;

	public Vector2 tilePos;
	public Vector3 gamePos;
	public BuildingType type;

	private List<UnitType> trains = new List<UnitType>();

	// Use this for initialization
	void Start () {
	
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
		gamePos = Scene.get().map.mapToGame(tileCoordinate);
		transform.position = gamePos;

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
}
