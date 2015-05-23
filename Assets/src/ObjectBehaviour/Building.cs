using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum BuildingType {BASE=0, COLONY=1};

public class Building : MonoBehaviour, Attackable, Clickable {
	
	public bool dead {get; set;}
	public Player owner;

	private float amount = 0f;
	public float maxAmount = 50f;
	public Resource resource = Resource.FOOD;
	private float productionRate = 1f;

	// attackable interface
	public float radius {get; set;}
	// clickable interface
	public ClickRegion clickArea = new CircleRegion(0.7f);

	public float influenceRadius;
	
	private int maxHealth = 10;
	private int health;
	private float hpPercent = 1f;

	public Vector2 tilePos;
	public Vector3 gamePos;
	public BuildingType type;
	private Transform dock;

	private List<UnitType> trains = new List<UnitType>();

	private SpriteRenderer tradeWith;
	private SpriteRenderer influence;

	// Use this for initialization
	void Start () {
	}
	
	// Update is called once per frame
	void Update () {
		if (type != BuildingType.BASE) {
			amount = Mathf.Min(amount + productionRate * Time.deltaTime, maxAmount);
			float sz = 0.3f + 1.2f * amount / maxAmount;
			transform.FindChild("resource").localScale = new Vector3(sz,sz,1f);
		}
	}

	public float collect(float capacity) {
		float amountTaken = Mathf.Min(capacity, amount); 
		amount -= amountTaken;
		return amountTaken;
	}

	public float expectedProfit(float arrivalDelay) {
		// TODO: take other traders into account?
		float expectedAmount = Mathf.Min(amount + productionRate * arrivalDelay, maxAmount);
		return expectedAmount;
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

		amount = 0f;
		health = maxHealth;
		radius = 0.25f;
		dead = false;
	
		Transform influenceObj = transform.FindChild("influence");
		influence = influenceObj.GetComponent<SpriteRenderer>();
		tradeWith = transform.FindChild("tradeWith").GetComponent<SpriteRenderer>();

		switch(buildingType) {
		case BuildingType.BASE:
			influenceRadius = 3f;
			break;
		case BuildingType.COLONY:
		default:
			influenceRadius = 2.5f;
			break;
		}
		// the buildign itself has a scale
		float sz = (2f * influenceRadius / influence.sprite.bounds.size.x) * (1f/transform.localScale.x);
		influenceObj.localScale = new Vector3(sz, sz, 1f);
		tradeWith.enabled = false;
	}
	
	public void setOwner(Player p) {
		if (owner != null) {
			owner.buildings.Remove(this);
			if (type == BuildingType.BASE) {
				foreach (Unit u in owner.units) {
					u.dead = true;
				}
			}
		}
		owner = p;
		p.buildings.Add(this);
		Transform teamColor = transform.FindChild("team-color");
		if (teamColor != null) {
			teamColor.GetComponent<SpriteRenderer>().color = owner.color;
		}
		float influenceAlpha = p.isNeutral ? 0.15f : 0.25f;
		influence.color = new Color(p.color.r, p.color.g, p.color.b, influenceAlpha);
	}

	public void toggleTradeWith(Player p) {
		if (p.isHuman) {
			tradeWith.enabled = !tradeWith.enabled;
			tradeWith.color = p.color;
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

	/* 
	 * ATTACKABLE 
	 */
	public void damage(Player attacker, int amount) {
		health -= amount;
		if (health <= 0) {
			health = maxHealth;
			setOwner(attacker);
		}
	}
	
	/* 
	 * CLICKABLE 
	 */
	public bool clickTest(int mouseButton, Vector2 mousePos) {
		return clickArea.Contains(mousePos - (Vector2) transform.position);
	}

	public void handleClick(int mouseButton) {
		if (type == BuildingType.COLONY) {
			Scene.get().players[Scene.get().HUMAN_PLAYER].toggleTrading(this);
		}
	}

	public void handleDrag(int mouseButton, Vector2 offset, Vector2 relativeToClick) {

	}
	
	public void setHover(bool isHovering)
	{
		if (isHovering) {
			//transform.FindChild("ring").GetComponent<SpriteRenderer>().color = new Color(1f, 0.7f, 0.7f);
		} else {
			//transform.FindChild("ring").GetComponent<SpriteRenderer>().color = Color.white;
		}
	}

	public void handleMouseDown(int mouseButton, Vector2 mousePos) {
		if (type == BuildingType.BASE && owner.isHuman) {
			List<Sprite> icons = new List<Sprite>();
			foreach (UnitType uType in getTrains()) {
				icons.Add(UnitData.getIcon(uType));
			}
			Scene.get().radial.mouseDown(Camera.main.WorldToScreenPoint(transform.position),
			                             icons, trainUnit);
		}
	}

	public void handleMouseUp(int mouseButton, Vector2 mousePos) {
	
	}

}
