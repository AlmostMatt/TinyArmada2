using UnityEngine;
using System.Collections;

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
	}
	
	public void setOwner(Player p) {
		owner = p;
		p.buildings.Add(this);
		Transform teamColor = transform.FindChild("team-color");
		if (teamColor != null) {
			teamColor.GetComponent<SpriteRenderer>().color = owner.color;
		}
	}

	public void trainUnit() {
		//if (owner.spend(Resource.FOOD, 100)) {
		//	Scene.get().spawnUnit(this, UnitType.GALLEY);
		//}
		if (owner.spend(Resource.FOOD, 50)) {
			Scene.get().spawnUnit(this, UnitType.MERCHANT);
		}
	}
}
