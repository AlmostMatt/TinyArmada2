using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum UnitType {MERCHANT=0, GALLEY=1, LONGBOAT=2};

public class Unit : Steering {
	private int ATTACK = 0;

	public Sprite[] sprites;
	public float radius = 0.25f;
	public GameObject bulletObj;

	private bool selected = false;

	// movement
    private bool hasDest;
    private Path path;
	private float destRadius;

	private float targetDir; // if this object is casting an ability in a direction, turn toward this)

	private UnitGroup group = null;

	private int maxHealth = 10;
	private int health;
	private float hpPercent = 1f;

	private StatusMap statusMap;
	private ActionMap actionMap;
	public Neighbours<Unit> neighbours;

	public bool dead = false;
	public Player owner;
	public UnitType type;

	// trading
	public float carrying;
	public Resource resource;
	public float capacity = 1f;
	public Building tradeDest;
	
	public void init(Player p, UnitType unitType) {
		setOwner(p);
		type = unitType;
		transform.FindChild("Gold").GetComponent<Renderer>().enabled = false;
		switch (type) {
		case UnitType.MERCHANT:
			capacity = 50f;
			break;
		}
	}

	/*
	 * Unity Events
	 */

	void Awake() {
		neighbours = new Neighbours<Unit>();
		statusMap = new StatusMap(this);
		actionMap = new ActionMap(this);
		actionMap.add(0, new Ability(1f));
	}

	// Use this for initialization
	override public void Start () {
		base.Start();
		if (sprites != null && sprites.Length > 0) {
			Sprite spr = sprites[Random.Range(0, sprites.Length)];
			SpriteRenderer renderer = GetComponent<SpriteRenderer>();
			renderer.sprite = spr;
		}

		MAX_V = 5f;
		ACCEL = 20f;
		health = maxHealth;
	}
	
	// Update is called once per frame
	void Update () {
		//Ray ray = Camera.main.ScreenPointToRay (Input.mousePosition);
		//Vector2 offset = ray.origin - transform.position;
		//float angle = Mathf.Rad2Deg * Mathf.Atan2(offset.y, offset.x);
		//float angle = Vector2.Angle(Vector2.right, );
		//transform.localEulerAngles = new Vector3(0, 0, angle);
		if (hpPercent * maxHealth > health) {
			hpPercent -= 0.4f * Time.deltaTime;
		}
	}

	override public void FixedUpdate () {
		if (statusMap.has(State.ANIMATION)) {
			brake();
			turnToward(targetDir);
		}
		// containment (walls)
		// avoid (obstacles)
		// separate from 'too close' others
		separate(neighbours);
		// align with others in the same group
		// move to destination (queue if necessary)
		if (hasDest && canMove ()) {
            path.followPath(this);
		}
		statusMap.update(Time.fixedDeltaTime);
		actionMap.update(Time.fixedDeltaTime);
		if (canAttack()) {
			float maxdd = 8f * 8f;
			foreach (Tuple<float, Unit> tuple in neighbours) {
				if (tuple.First > maxdd ) {
					break;
				}
				Unit other = tuple.Second;
				if (other.owner != owner) {
					cast(ATTACK, other);
					// look at
					Vector2 offset = other.transform.position - transform.position;
					targetDir = Mathf.Rad2Deg * Mathf.Atan2(offset.y, offset.x);
					break;
				}
			}
		}
		if (canTrade()) {
			// traders
			// only need to rethink onSpawn, on drop off at base, on somebody else trading before me or wanting to take my trade route
			// or if someone else stops trading, changes their mind, or dies
			if (carrying > 0f) {
				// heading home
				// for now home is the 1st building owned by a player
				if (hasDest && path.arrived) {
					owner.collect(resource, carrying);
					carrying = 0f;
					transform.FindChild("Gold").GetComponent<Renderer>().enabled = false;
					hasDest = false;
				}
			} else if (tradeDest == null  && !hasDest) {
				// pick a building
				List<Building> buildings = Scene.get().buildings;
				Building building = buildings[Random.Range(0, buildings.Count)];
				tradeDest = building;
				moveTo (building.gamePos, radius);
			} else if (tradeDest != null) {
				// heading somewhere
                if (hasDest && path.arrived) {
					resource = tradeDest.resource;
					carrying = tradeDest.collect(capacity);
					moveTo(owner.buildings[0].gamePos, radius);
					tradeDest = null;
					transform.FindChild("Gold").GetComponent<Renderer>().enabled = true;
				}
			}
		}
		base.FixedUpdate();
	}

	// called after (fixed)update => after objects move
	void LateUpdate() {
		GameObject HPbar = transform.FindChild("HP").gameObject;
		HPbar.transform.rotation = Quaternion.identity;
		Renderer r = HPbar.GetComponent<Renderer>();
		r.material.color = Color.green;
		r.material.SetFloat("_Cutoff", 1f - health / (2f * maxHealth));
		GameObject HPbar2 = transform.FindChild("HPred").gameObject;
		HPbar2.transform.rotation = Quaternion.identity;
		Renderer r2 = HPbar2.GetComponent<Renderer>();
		r2.material.color = Color.red;
		r2.material.SetFloat("_Cutoff", 1f - hpPercent/2f);
	}

	/*
	 * Unit actions
	 */

	public void moveTo(Vector2 point, float radius) {
		hasDest = true;
        path = Pathing.findPath(transform.position, point, radius);
		//dest = point;
		//destRadius = radius;
		//atDest = false;
	}

	public void setGroup(UnitGroup newGroup) {
		if (group != null) {
			group.Remove(this);
			group = null;
		}
		if (newGroup != null) {
			newGroup.Add(this);
			group = newGroup;
		}
	}

	public void cast(int action, object target) {
		actionMap.use(action, target);
		statusMap.add(new Status(State.ANIMATION), actionMap.getCastTime(action));
	}

	public void select() {
		GetComponent<Renderer>().material.color = new Color(0.7f, 1f, 0.7f);
		selected = true;
	}

	public void deselect() {
		GetComponent<Renderer>().material.color = Color.white;
		selected = false;
	}

	public void damage(int amount) {
		health = Mathf.Max (0, health - amount);
		if (health == 0) {
			dead = true;
		}
	}

	public void fire(Unit target) {
		GameObject gun = transform.FindChild("Gun").gameObject;
		Shot shot = Instantiate(bulletObj).GetComponent<Shot>();
		shot.transform.position = gun.transform.position;
		shot.transform.rotation = gun.transform.rotation;
		shot.GetComponent<Rigidbody2D>().velocity = 20 * gun.transform.right;
		shot.setTarget(target);
	}
	
	/*
	 * State logic
	 */
	
	private bool canMove() {
		return !statusMap.has(State.ANIMATION);
	}
	
	private bool canCast() {
		return !statusMap.has(State.ANIMATION);
	}
	
	private bool canAttack() {
		return type != UnitType.MERCHANT && actionMap.ready(ATTACK) && !statusMap.has(State.ANIMATION);
	}
	
	private bool canTrade() {
		return type == UnitType.MERCHANT;
	}

	public void setOwner(Player p) {
		owner = p;
		p.units.Add(this);
		Transform teamColor = transform.FindChild("team-color");
		if (teamColor != null) {
			teamColor.GetComponent<SpriteRenderer>().color = owner.color;
		}
	}
}
